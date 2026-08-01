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

# The Ticketing API can later use this IAM role through its Kubernetes
# ServiceAccount. The Helm/Kubernetes manifest must add:
# eks.amazonaws.com/role-arn = <api_secrets_irsa_role_arn output>
data "aws_iam_policy_document" "ticketing_api_assume_role" {
  statement {
    sid     = "AllowTicketingApiServiceAccount"
    effect  = "Allow"
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [aws_iam_openid_connect_provider.eks.arn]
    }

    condition {
      test     = "StringEquals"
      variable = "${local.eks_oidc_provider_url}:aud"
      values   = ["sts.${data.aws_partition.current.dns_suffix}"]
    }

    condition {
      test     = "StringEquals"
      variable = "${local.eks_oidc_provider_url}:sub"
      values   = ["system:serviceaccount:${var.application_namespace}:${var.api_service_account_name}"]
    }
  }
}

resource "aws_iam_role" "ticketing_api" {
  name               = "${local.name}-ticketing-api-role"
  assume_role_policy = data.aws_iam_policy_document.ticketing_api_assume_role.json

  tags = merge(
    local.common_tags,
    {
      Name = "${local.name}-ticketing-api-role"
    }
  )
}

data "aws_iam_policy_document" "ticketing_api_secrets" {
  statement {
    sid    = "ReadOnlyTicketingApplicationSecrets"
    effect = "Allow"
    actions = [
      "secretsmanager:DescribeSecret",
      "secretsmanager:GetSecretValue",
    ]
    resources = concat(
      [aws_db_instance.ticketing.master_user_secret[0].secret_arn],
      [for secret in aws_secretsmanager_secret.application : secret.arn],
    )
  }
}

resource "aws_iam_policy" "ticketing_api_secrets" {
  name        = "${local.name}-ticketing-api-secrets-policy"
  description = "Read only the database and runtime secrets required by Ticketing API"
  policy      = data.aws_iam_policy_document.ticketing_api_secrets.json

  tags = local.common_tags
}

resource "aws_iam_role_policy_attachment" "ticketing_api_secrets" {
  role       = aws_iam_role.ticketing_api.name
  policy_arn = aws_iam_policy.ticketing_api_secrets.arn
}

output "application_secret_arns" {
  description = "Secrets Manager ARNs to populate outside Terraform."
  value = {
    for name, secret in aws_secretsmanager_secret.application :
    name => secret.arn
  }
}

output "api_secrets_irsa_role_arn" {
  description = "IAM role ARN to annotate on the Ticketing API Kubernetes ServiceAccount."
  value       = aws_iam_role.ticketing_api.arn
}

