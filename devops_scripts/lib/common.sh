#!/usr/bin/env bash

# Shared helpers for scripts in devops_scripts. This file is sourced; callers
# are responsible for enabling their preferred shell options.

die() {
  printf 'ERROR: %s\n' "$*" >&2
  exit 1
}

info() {
  printf '==> %s\n' "$*"
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || die "Required command not found: $1"
}

require_directory() {
  [[ -d "$1" ]] || die "Directory not found: $1"
}

terraform_output_raw() {
  local terraform_dir="$1"
  local output_name="$2"

  terraform -chdir="$terraform_dir" output -raw "$output_name" 2>/dev/null ||
    die "Unable to read Terraform output '$output_name' from $terraform_dir. Run terraform init/apply and verify AWS credentials."
}

terraform_output_json() {
  local terraform_dir="$1"
  local output_name="$2"

  terraform -chdir="$terraform_dir" output -json "$output_name" 2>/dev/null ||
    die "Unable to read Terraform output '$output_name' from $terraform_dir. Run terraform init/apply and verify AWS credentials."
}

region_from_secrets_manager_arn() {
  local secret_arn="$1"
  local arn_prefix partition service region account resource

  IFS=: read -r arn_prefix partition service region account resource <<<"$secret_arn"
  [[ "$arn_prefix" == "arn" && "$service" == "secretsmanager" && -n "$region" ]] ||
    die "Terraform output is not a valid Secrets Manager ARN: $secret_arn"

  printf '%s\n' "$region"
}
