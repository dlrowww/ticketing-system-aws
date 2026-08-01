# ============================================================
# Secrets Manager containers
#
# Secret values are intentionally NOT managed here. Adding secret values to
# Terraform would persist them in Terraform state. Populate these secrets via
# an approved operational process after terraform apply.
# ============================================================

locals {
  application_secret_names = toset([
    "jwt",
    "smtp",
    "initial-admin",
  ])
}

resource "aws_secretsmanager_secret" "application" {
  for_each = local.application_secret_names

  name                    = "${local.name}/${each.value}"
  description             = "Runtime ${each.value} configuration for ${local.name}; value is populated outside Terraform"
  recovery_window_in_days = 7

  tags = merge(
    local.common_tags,
    {
      Name      = "${local.name}/${each.value}"
      Component = "ticketing-api"
    }
  )
}
