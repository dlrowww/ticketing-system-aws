#!/usr/bin/env bash
set -Eeuo pipefail
set +x

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd)"
# shellcheck source=lib/common.sh
source "$SCRIPT_DIR/lib/common.sh"

terraform_dir="$REPO_ROOT/aws"
k8s_dir="$REPO_ROOT/k8s_deploy"
namespace="ticketing-system"
backend_image=""
frontend_image=""
application_domain=""
certificate_arn=""
kube_context=""
rollout_timeout="10m"
secret_timeout="5m"
temp_dir=""

usage() {
  cat <<'USAGE'
Usage: devops_scripts/deploy-k8s.sh --backend-image IMAGE --frontend-image IMAGE [options]

Deploy the plain Kubernetes manifests in dependency order. The script renders
temporary manifests for immutable image references and, when configured, the
application domain and ACM certificate. Generated manifests are never committed.

Required:
  --backend-image IMAGE    Full backend image reference with an immutable tag
  --frontend-image IMAGE   Full frontend image reference with an immutable tag

Options:
  --terraform-dir PATH     Terraform root (default: <repo>/aws)
  --k8s-dir PATH           Kubernetes manifest root (default: <repo>/k8s_deploy)
  --domain NAME            Public application hostname
  --certificate-arn ARN    ACM certificate ARN used by the ALB Ingress
  --context NAME           kubectl context
  --rollout-timeout VALUE  Migration/rollout timeout (default: 10m)
  --secret-timeout VALUE   External Secrets timeout (default: 5m)
  -h, --help               Show this help

--domain and --certificate-arn must be supplied together. Without them, the
Ingress is intentionally not applied and the existing example ConfigMap values
are left unchanged.
USAGE
}

cleanup() {
  if [[ -n "$temp_dir" && -d "$temp_dir" ]]; then
    rm -f -- \
      "$temp_dir/api-deployment.yaml" \
      "$temp_dir/frontend-deployment.yaml" \
      "$temp_dir/migration-job.yaml" \
      "$temp_dir/configmap.yaml" \
      "$temp_dir/ingress.yaml"
    rmdir -- "$temp_dir" 2>/dev/null || true
  fi
}

trap cleanup EXIT

while (($# > 0)); do
  case "$1" in
    --backend-image)
      (($# >= 2)) || die "--backend-image requires a value"
      backend_image="$2"
      shift 2
      ;;
    --frontend-image)
      (($# >= 2)) || die "--frontend-image requires a value"
      frontend_image="$2"
      shift 2
      ;;
    --terraform-dir)
      (($# >= 2)) || die "--terraform-dir requires a value"
      terraform_dir="$2"
      shift 2
      ;;
    --k8s-dir)
      (($# >= 2)) || die "--k8s-dir requires a value"
      k8s_dir="$2"
      shift 2
      ;;
    --domain)
      (($# >= 2)) || die "--domain requires a value"
      application_domain="$2"
      shift 2
      ;;
    --certificate-arn)
      (($# >= 2)) || die "--certificate-arn requires a value"
      certificate_arn="$2"
      shift 2
      ;;
    --context)
      (($# >= 2)) || die "--context requires a value"
      kube_context="$2"
      shift 2
      ;;
    --rollout-timeout)
      (($# >= 2)) || die "--rollout-timeout requires a value"
      rollout_timeout="$2"
      shift 2
      ;;
    --secret-timeout)
      (($# >= 2)) || die "--secret-timeout requires a value"
      secret_timeout="$2"
      shift 2
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

require_command kubectl
require_command terraform
require_command jq
require_command envsubst
require_command mktemp
require_command sed
require_directory "$terraform_dir"
require_directory "$k8s_dir"

[[ -n "$backend_image" ]] || die "--backend-image is required"
[[ -n "$frontend_image" ]] || die "--frontend-image is required"
[[ "$backend_image" =~ ^[A-Za-z0-9._/:@-]+$ ]] || die "Backend image reference contains unsafe characters"
[[ "$frontend_image" =~ ^[A-Za-z0-9._/:@-]+$ ]] || die "Frontend image reference contains unsafe characters"
[[ "$backend_image" == *:* || "$backend_image" == *@sha256:* ]] || die "Backend image must use an explicit tag or digest"
[[ "$frontend_image" == *:* || "$frontend_image" == *@sha256:* ]] || die "Frontend image must use an explicit tag or digest"
[[ "$namespace" =~ ^[a-z0-9]([-a-z0-9]*[a-z0-9])?$ ]] || die "Invalid Kubernetes namespace: $namespace"
[[ "$rollout_timeout" =~ ^[1-9][0-9]*[smh]$ ]] || die "Invalid rollout timeout: $rollout_timeout"
[[ "$secret_timeout" =~ ^[1-9][0-9]*[smh]$ ]] || die "Invalid secret timeout: $secret_timeout"

if [[ -n "$application_domain" || -n "$certificate_arn" ]]; then
  [[ -n "$application_domain" && -n "$certificate_arn" ]] ||
    die "--domain and --certificate-arn must be supplied together"
  [[ "$application_domain" =~ ^[A-Za-z0-9]([A-Za-z0-9.-]*[A-Za-z0-9])$ ]] ||
    die "Invalid application domain: $application_domain"
  [[ "$certificate_arn" =~ ^arn:[a-z0-9-]+:acm:[a-z0-9-]+:[0-9]{12}:certificate/[A-Za-z0-9-]+$ ]] ||
    die "Invalid ACM certificate ARN"
fi

for manifest in \
  namespace.yaml \
  configmap.yaml \
  api/serviceaccount.yaml \
  api/service.yaml \
  api/deployment.yaml \
  api/migration-job.yaml \
  frontend/service.yaml \
  frontend/deployment.yaml \
  ingress.yaml; do
  [[ -f "$k8s_dir/$manifest" ]] || die "Manifest not found: $k8s_dir/$manifest"
done

temp_dir="$(mktemp -d "${TMPDIR:-/tmp}/ticketing-k8s-deploy.XXXXXX")"
chmod 700 "$temp_dir"

sed "s#image: ticketing-backend:1.0.0#image: $backend_image#g" \
  "$k8s_dir/api/deployment.yaml" >"$temp_dir/api-deployment.yaml"
sed "s#image: ticketing-backend:1.0.0#image: $backend_image#g" \
  "$k8s_dir/api/migration-job.yaml" >"$temp_dir/migration-job.yaml"
sed "s#image: ticketing-frontend:1.0.0#image: $frontend_image#g" \
  "$k8s_dir/frontend/deployment.yaml" >"$temp_dir/frontend-deployment.yaml"

grep -Fq "image: $backend_image" "$temp_dir/api-deployment.yaml" ||
  die "Backend image placeholder was not found in api/deployment.yaml"
grep -Fq "image: $backend_image" "$temp_dir/migration-job.yaml" ||
  die "Backend image placeholder was not found in api/migration-job.yaml"
grep -Fq "image: $frontend_image" "$temp_dir/frontend-deployment.yaml" ||
  die "Frontend image placeholder was not found in frontend/deployment.yaml"

if [[ -n "$application_domain" ]]; then
  sed \
    -e "s#tickets\.example\.com#$application_domain#g" \
    -e "s#noreply@example\.com#noreply@$application_domain#g" \
    "$k8s_dir/configmap.yaml" >"$temp_dir/configmap.yaml"
  sed \
    -e "s#tickets\.example\.com#$application_domain#g" \
    -e "s#arn:aws:acm:us-east-1:123456789012:certificate/REPLACE_ME#$certificate_arn#g" \
    "$k8s_dir/ingress.yaml" >"$temp_dir/ingress.yaml"
else
  cp -- "$k8s_dir/configmap.yaml" "$temp_dir/configmap.yaml"
  info "DNS is not configured; Ingress will not be applied"
fi

kubectl_args=()
if [[ -n "$kube_context" ]]; then
  kubectl_args+=(--context "$kube_context")
fi

info "Applying namespace and shared configuration"
kubectl "${kubectl_args[@]}" apply -f "$k8s_dir/namespace.yaml"
kubectl "${kubectl_args[@]}" apply -f "$temp_dir/configmap.yaml"
kubectl "${kubectl_args[@]}" apply -f "$k8s_dir/api/serviceaccount.yaml"
kubectl "${kubectl_args[@]}" apply -f "$k8s_dir/api/service.yaml"
kubectl "${kubectl_args[@]}" apply -f "$k8s_dir/frontend/service.yaml"

external_secret_args=(
  --terraform-dir "$terraform_dir"
  --namespace "$namespace"
  --wait-timeout "$secret_timeout"
  --apply
)
if [[ -n "$kube_context" ]]; then
  external_secret_args+=(--context "$kube_context")
fi
"$SCRIPT_DIR/render-external-secrets.sh" "${external_secret_args[@]}"

info "Waiting for the synchronized runtime Secret"
kubectl "${kubectl_args[@]}" wait \
  --namespace "$namespace" \
  --for=create \
  secret/ticketing-system-runtime \
  --timeout="$secret_timeout"

info "Running the database migration before the API rollout"
kubectl "${kubectl_args[@]}" delete \
  --namespace "$namespace" \
  job/ticketing-system-api-migration \
  --ignore-not-found \
  --wait=true
kubectl "${kubectl_args[@]}" apply -f "$temp_dir/migration-job.yaml"

if ! kubectl "${kubectl_args[@]}" wait \
  --namespace "$namespace" \
  --for=condition=complete \
  job/ticketing-system-api-migration \
  --timeout="$rollout_timeout"; then
  kubectl "${kubectl_args[@]}" logs \
    --namespace "$namespace" \
    job/ticketing-system-api-migration \
    --all-containers=true || true
  kubectl "${kubectl_args[@]}" describe \
    --namespace "$namespace" \
    job/ticketing-system-api-migration || true
  die "Database migration failed; API and frontend Deployments were not updated"
fi
kubectl "${kubectl_args[@]}" logs \
  --namespace "$namespace" \
  job/ticketing-system-api-migration \
  --all-containers=true

info "Applying application Deployments"
kubectl "${kubectl_args[@]}" apply -f "$temp_dir/api-deployment.yaml"
kubectl "${kubectl_args[@]}" apply -f "$temp_dir/frontend-deployment.yaml"
kubectl "${kubectl_args[@]}" rollout status \
  --namespace "$namespace" \
  deployment/ticketing-system-api \
  --timeout="$rollout_timeout"
kubectl "${kubectl_args[@]}" rollout status \
  --namespace "$namespace" \
  deployment/ticketing-system-frontend \
  --timeout="$rollout_timeout"

if [[ -n "$application_domain" ]]; then
  info "Applying ALB Ingress for $application_domain"
  kubectl "${kubectl_args[@]}" apply -f "$temp_dir/ingress.yaml"
fi

info "Deployment completed"
kubectl "${kubectl_args[@]}" get \
  --namespace "$namespace" \
  deployment,service,ingress
