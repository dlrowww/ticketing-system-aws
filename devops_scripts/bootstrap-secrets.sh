#!/usr/bin/env bash
set -Eeuo pipefail
# Never allow caller-provided `bash -x` to print expanded credential values.
set +x

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd)"
# shellcheck source=lib/common.sh
source "$SCRIPT_DIR/lib/common.sh"

terraform_dir="$REPO_ROOT/aws"
aws_region=""
aws_profile=""
force=false
temp_dir=""
jwt_json_file=""
smtp_json_file=""
admin_json_file=""

usage() {
  cat <<'USAGE'
Usage: devops_scripts/bootstrap-secrets.sh [options]

Read Secret identifiers from Terraform outputs, generate a JWT key, securely
prompt for SMTP/admin credentials, and create new AWS Secrets Manager versions.

Options:
  --terraform-dir PATH  Terraform root directory (default: <repo>/aws)
  --region REGION       AWS region (default: parsed from the RDS Secret ARN)
  --profile PROFILE     AWS CLI profile
  --force               Rotate/overwrite Secrets that already have AWSCURRENT
  -h, --help            Show this help

The script is interactive by design. Secret values are sent through temporary
0600 JSON files and never placed in AWS CLI command arguments.
USAGE
}

cleanup() {
  local file

  for file in "$jwt_json_file" "$smtp_json_file" "$admin_json_file"; do
    if [[ -n "$file" && -f "$file" ]]; then
      chmod u+w "$file" 2>/dev/null || true
      : >"$file" 2>/dev/null || true
      rm -f -- "$file"
    fi
  done

  if [[ -n "$temp_dir" && -d "$temp_dir" ]]; then
    rmdir -- "$temp_dir" 2>/dev/null || true
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
    --force)
      force=true
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
require_command aws
require_command jq
require_command openssl
require_command mktemp
require_directory "$terraform_dir"
[[ -t 0 ]] || die "Interactive terminal input is required"
if [[ -n "$aws_profile" ]]; then
  export AWS_PROFILE="$aws_profile"
fi

info "Reading Secrets Manager identifiers from Terraform outputs"
application_secret_names_json="$(terraform_output_json "$terraform_dir" application_secret_names)"
rds_master_secret_arn="$(terraform_output_raw "$terraform_dir" rds_master_user_secret_arn)"

jwt_secret_name="$(jq -er '.jwt' <<<"$application_secret_names_json")" ||
  die "Terraform output application_secret_names does not contain 'jwt'"
smtp_secret_name="$(jq -er '.smtp' <<<"$application_secret_names_json")" ||
  die "Terraform output application_secret_names does not contain 'smtp'"
admin_secret_name="$(jq -er '.["initial-admin"]' <<<"$application_secret_names_json")" ||
  die "Terraform output application_secret_names does not contain 'initial-admin'"

if [[ -z "$aws_region" ]]; then
  aws_region="$(region_from_secrets_manager_arn "$rds_master_secret_arn")"
fi

aws_args=(--region "$aws_region" --no-cli-pager)
if [[ -n "$aws_profile" ]]; then
  aws_args+=(--profile "$aws_profile")
fi

aws_cmd() {
  aws "${aws_args[@]}" "$@"
}

info "Verifying AWS identity and Secret resources in $aws_region"
aws_cmd sts get-caller-identity --output json >/dev/null

secret_names=("$jwt_secret_name" "$smtp_secret_name" "$admin_secret_name")
existing_current=()

for secret_name in "${secret_names[@]}"; do
  aws_cmd secretsmanager describe-secret --secret-id "$secret_name" --output json >/dev/null
  version_count="$(
    aws_cmd secretsmanager list-secret-version-ids --secret-id "$secret_name" --output json |
      jq '[.Versions[]? | select(.VersionStages | index("AWSCURRENT"))] | length'
  )"
  if ((version_count > 0)); then
    existing_current+=("$secret_name")
  fi
done

if ((${#existing_current[@]} > 0)) && [[ "$force" != true ]]; then
  printf 'The following Secrets already have an AWSCURRENT version:\n' >&2
  printf '  - %s\n' "${existing_current[@]}" >&2
  die "Refusing to rotate existing credentials without --force"
fi

if [[ "$force" == true && ${#existing_current[@]} -gt 0 ]]; then
  printf 'WARNING: --force will create new AWSCURRENT versions for:\n' >&2
  printf '  - %s\n' "${existing_current[@]}" >&2
  IFS= read -r -p "Type ROTATE to continue: " confirmation
  [[ "$confirmation" == "ROTATE" ]] || die "Rotation cancelled"
fi

read_nonempty() {
  local prompt="$1"
  local value=""

  while [[ -z "$value" ]]; do
    IFS= read -r -p "$prompt" value
  done
  printf '%s' "$value"
}

read_secret() {
  local prompt="$1"
  local value=""

  while [[ -z "$value" ]]; do
    IFS= read -r -s -p "$prompt" value
    printf '\n' >&2
  done
  printf '%s' "$value"
}

smtp_username="$(read_nonempty 'SMTP username: ')"
smtp_password="$(read_secret 'SMTP password: ')"
admin_email="$(read_nonempty 'Initial admin email: ')"
admin_password="$(read_secret 'Initial admin password (minimum 12 characters): ')"
if ((${#admin_password} < 12)); then
  die "Initial admin password must be at least 12 characters"
fi
admin_password_confirmation="$(read_secret 'Confirm initial admin password: ')"
[[ "$admin_password" == "$admin_password_confirmation" ]] || die "Admin passwords do not match"

jwt_key="$(openssl rand -base64 64)"
[[ ${#jwt_key} -ge 64 ]] || die "Generated JWT key is unexpectedly short"

umask 077
temp_dir="$(mktemp -d "${TMPDIR:-/tmp}/ticketing-secrets.XXXXXX")"
jwt_json_file="$temp_dir/jwt.json"
smtp_json_file="$temp_dir/smtp.json"
admin_json_file="$temp_dir/initial-admin.json"

printf '%s' "$jwt_key" |
  jq -Rs '{key: .}' >"$jwt_json_file"
printf '%s\n%s' "$smtp_username" "$smtp_password" |
  jq -Rs 'split("\n") | {username: .[0], password: .[1]}' >"$smtp_json_file"
printf '%s\n%s' "$admin_email" "$admin_password" |
  jq -Rs 'split("\n") | {email: .[0], password: .[1]}' >"$admin_json_file"

info "Writing new Secrets Manager versions"
aws_cmd secretsmanager put-secret-value \
  --secret-id "$jwt_secret_name" \
  --secret-string "file://$jwt_json_file" \
  --query VersionId \
  --output text >/dev/null
info "Updated $jwt_secret_name"

aws_cmd secretsmanager put-secret-value \
  --secret-id "$smtp_secret_name" \
  --secret-string "file://$smtp_json_file" \
  --query VersionId \
  --output text >/dev/null
info "Updated $smtp_secret_name"

aws_cmd secretsmanager put-secret-value \
  --secret-id "$admin_secret_name" \
  --secret-string "file://$admin_json_file" \
  --query VersionId \
  --output text >/dev/null
info "Updated $admin_secret_name"

unset jwt_key smtp_password admin_password admin_password_confirmation

printf '\nSecret bootstrap completed. No secret values were printed or persisted in the repository.\n'
printf 'RDS master Secret (managed by RDS, unchanged): %s\n' "$rds_master_secret_arn"
