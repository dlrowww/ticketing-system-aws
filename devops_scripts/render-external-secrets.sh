#!/usr/bin/env bash
set -Eeuo pipefail
set +x

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd)"
# shellcheck source=lib/common.sh
source "$SCRIPT_DIR/lib/common.sh"

terraform_dir="$REPO_ROOT/aws"
template_file="$SCRIPT_DIR/templates/external-secrets.yaml.tpl"
k8s_namespace="ticketing-system"
aws_region=""
aws_profile=""
output_file=""
kube_context=""
wait_timeout="120s"
apply_manifest=false
force_output=false
skip_cluster_preflight=false
temp_file=""

usage() {
  cat <<'USAGE'
Usage: devops_scripts/render-external-secrets.sh [options]

Render ExternalSecret YAML from Terraform outputs. With no output/apply option,
the manifest is written to stdout.

Options:
  --terraform-dir PATH  Terraform root directory (default: <repo>/aws)
  --template PATH       envsubst template (default: devops_scripts/templates/...)
  --namespace NAME      Kubernetes namespace (default: ticketing-system)
  --region REGION       AWS region (default: parsed from RDS Secret ARN)
  --profile PROFILE     AWS profile used to read the Terraform S3 backend
  --output PATH         Persist rendered YAML at PATH
  --force               Allow overwriting --output PATH
  --apply               Apply a temporary rendered manifest with kubectl
  --context NAME        kubectl context used with --apply
  --wait-timeout VALUE  ExternalSecret wait timeout (default: 120s)
  --skip-cluster-preflight
                        Do not read cluster-scoped Namespace/CRD resources
  -h, --help            Show this help

Do not use --apply when the Helm release already owns the SecretStore and
ExternalSecret. In that case pass the same Terraform outputs as Helm values.
USAGE
}

cleanup() {
  if [[ -n "$temp_file" && -f "$temp_file" ]]; then
    rm -f -- "$temp_file"
  fi
}

trap cleanup EXIT

while (($# > 0)); do
  case "$1" in
    --terraform-dir)
      (($# >= 2)) || die "--terraform-dir requires a value"
      terraform_dir="$2"
      shift 2
      ;;
    --template)
      (($# >= 2)) || die "--template requires a value"
      template_file="$2"
      shift 2
      ;;
    --namespace)
      (($# >= 2)) || die "--namespace requires a value"
      k8s_namespace="$2"
      shift 2
      ;;
    --region)
      (($# >= 2)) || die "--region requires a value"
      aws_region="$2"
      shift 2
      ;;
    --profile)
      (($# >= 2)) || die "--profile requires a value"
      aws_profile="$2"
      shift 2
      ;;
    --output)
      (($# >= 2)) || die "--output requires a value"
      output_file="$2"
      shift 2
      ;;
    --force)
      force_output=true
      shift
      ;;
    --apply)
      apply_manifest=true
      shift
      ;;
    --context)
      (($# >= 2)) || die "--context requires a value"
      kube_context="$2"
      shift 2
      ;;
    --wait-timeout)
      (($# >= 2)) || die "--wait-timeout requires a value"
      wait_timeout="$2"
      shift 2
      ;;
    --skip-cluster-preflight)
      skip_cluster_preflight=true
      shift
      ;;
    -h | --help)
      usage
      exit 0
      ;;
    *)
      die "Unknown argument: $1"
      ;;
  esac
done

require_command terraform
require_command jq
require_command envsubst
require_command mktemp
require_directory "$terraform_dir"
[[ -f "$template_file" ]] || die "Template not found: $template_file"
[[ -n "$k8s_namespace" ]] || die "Kubernetes namespace cannot be empty"
if [[ -n "$aws_profile" ]]; then
  export AWS_PROFILE="$aws_profile"
fi

if [[ -n "$output_file" && -e "$output_file" && "$force_output" != true ]]; then
  die "Output already exists: $output_file (use --force to overwrite)"
fi
if [[ "$apply_manifest" == true && -n "$output_file" ]]; then
  die "--apply and --output are mutually exclusive; --apply always uses and removes a temporary file"
fi

info "Reading ExternalSecret references from Terraform outputs" >&2
application_secret_names_json="$(terraform_output_json "$terraform_dir" application_secret_names)"
RDS_MASTER_SECRET_ARN="$(terraform_output_raw "$terraform_dir" rds_master_user_secret_arn)"
DATABASE_NAME="$(terraform_output_raw "$terraform_dir" rds_database_name)"
JWT_SECRET_NAME="$(jq -er '.jwt' <<<"$application_secret_names_json")" ||
  die "Terraform output application_secret_names does not contain 'jwt'"
SMTP_SECRET_NAME="$(jq -er '.smtp' <<<"$application_secret_names_json")" ||
  die "Terraform output application_secret_names does not contain 'smtp'"
INITIAL_ADMIN_SECRET_NAME="$(jq -er '.["initial-admin"]' <<<"$application_secret_names_json")" ||
  die "Terraform output application_secret_names does not contain 'initial-admin'"

if [[ -z "$aws_region" ]]; then
  AWS_REGION="$(region_from_secrets_manager_arn "$RDS_MASTER_SECRET_ARN")"
else
  AWS_REGION="$aws_region"
fi
K8S_NAMESPACE="$k8s_namespace"

[[ "$K8S_NAMESPACE" =~ ^[a-z0-9]([-a-z0-9]*[a-z0-9])?$ ]] ||
  die "Invalid Kubernetes namespace: $K8S_NAMESPACE"
[[ "$AWS_REGION" =~ ^[a-z]{2}(-[a-z0-9]+)+-[0-9]+$ ]] ||
  die "Invalid AWS region: $AWS_REGION"
[[ "$DATABASE_NAME" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]] ||
  die "Database name is unsafe for direct template substitution: $DATABASE_NAME"

export AWS_REGION K8S_NAMESPACE RDS_MASTER_SECRET_ARN DATABASE_NAME
export JWT_SECRET_NAME SMTP_SECRET_NAME INITIAL_ADMIN_SECRET_NAME

render_manifest() {
  envsubst \
    '${AWS_REGION} ${K8S_NAMESPACE} ${RDS_MASTER_SECRET_ARN} ${DATABASE_NAME} ${JWT_SECRET_NAME} ${SMTP_SECRET_NAME} ${INITIAL_ADMIN_SECRET_NAME}' \
    <"$template_file"
}

validate_rendered_manifest() {
  local manifest_file="$1"

  if grep -Eq '\$\{(AWS_REGION|K8S_NAMESPACE|RDS_MASTER_SECRET_ARN|DATABASE_NAME|JWT_SECRET_NAME|SMTP_SECRET_NAME|INITIAL_ADMIN_SECRET_NAME)\}' "$manifest_file"; then
    die "Rendered manifest still contains unresolved template variables"
  fi
}

if [[ "$apply_manifest" == true ]]; then
  require_command kubectl
  umask 077
  temp_file="$(mktemp "${TMPDIR:-/tmp}/ticketing-external-secrets.XXXXXX.yaml")"
  render_manifest >"$temp_file"
  validate_rendered_manifest "$temp_file"

  kubectl_args=()
  if [[ -n "$kube_context" ]]; then
    kubectl_args+=(--context "$kube_context")
  fi

  if [[ "$skip_cluster_preflight" != true ]]; then
    kubectl "${kubectl_args[@]}" get namespace "$k8s_namespace" >/dev/null
    kubectl "${kubectl_args[@]}" get crd externalsecrets.external-secrets.io >/dev/null
  fi

  for resource in \
    secretstore/ticketing-system-aws-secrets-manager \
    externalsecret/ticketing-system-runtime; do
    managed_by="$(
      kubectl "${kubectl_args[@]}" get \
        --namespace "$k8s_namespace" \
        "$resource" \
        --output jsonpath='{.metadata.labels.app\.kubernetes\.io/managed-by}' \
        2>/dev/null || true
    )"
    if [[ "$managed_by" == "Helm" ]]; then
      die "$resource is managed by Helm; update the Helm release instead of using --apply"
    fi
  done

  info "Applying SecretStore and ExternalSecret to namespace $k8s_namespace"
  kubectl "${kubectl_args[@]}" apply -f "$temp_file"
  kubectl "${kubectl_args[@]}" wait \
    --namespace "$k8s_namespace" \
    --for=condition=Ready \
    secretstore/ticketing-system-aws-secrets-manager \
    --timeout="$wait_timeout"
  if ! kubectl "${kubectl_args[@]}" wait \
    --namespace "$k8s_namespace" \
    --for=condition=Ready \
    externalsecret/ticketing-system-runtime \
    --timeout="$wait_timeout"; then
    printf '::error::ExternalSecret ticketing-system-runtime failed to become Ready\n' >&2
    printf '::group::ExternalSecret status\n'
    kubectl "${kubectl_args[@]}" get \
      --namespace "$k8s_namespace" \
      externalsecret/ticketing-system-runtime \
      --output yaml || true
    printf '::endgroup::\n'

    printf '::group::ExternalSecret description\n'
    kubectl "${kubectl_args[@]}" describe \
      --namespace "$k8s_namespace" \
      externalsecret/ticketing-system-runtime || true
    printf '::endgroup::\n'

    printf '::group::ExternalSecret events\n'
    kubectl "${kubectl_args[@]}" get events \
      --namespace "$k8s_namespace" \
      --field-selector involvedObject.name=ticketing-system-runtime \
      --sort-by=.lastTimestamp || true
    printf '::endgroup::\n'

    die "ExternalSecret ticketing-system-runtime synchronization failed"
  fi
  info "ticketing-system-runtime is synchronized"
elif [[ -n "$output_file" ]]; then
  output_parent="$(dirname -- "$output_file")"
  require_directory "$output_parent"
  umask 077
  render_manifest >"$output_file"
  validate_rendered_manifest "$output_file"
  info "Rendered manifest: $output_file" >&2
else
  render_manifest
fi
