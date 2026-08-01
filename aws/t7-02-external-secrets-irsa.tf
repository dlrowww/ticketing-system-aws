# ============================================================
# External Secrets Operator IRSA
#
# The operator, rather than the application pods, reads the four approved
# Secrets Manager secrets and materializes a Kubernetes Secret.
# ============================================================

data "aws_iam_policy_document" "external_secrets_assume_role" {
  statement {
    sid     = "AllowExternalSecretsServiceAccount"
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
      values = [
        "system:serviceaccount:${var.external_secrets_namespace}:${var.external_secrets_service_account_name}"
      ]
    }
  }
}

resource "aws_iam_role" "external_secrets" {
  name               = "${local.name}-external-secrets-role"
  assume_role_policy = data.aws_iam_policy_document.external_secrets_assume_role.json

  tags = merge(
    local.common_tags,
    {
      Name = "${local.name}-external-secrets-role"
    }
  )
}

data "aws_iam_policy_document" "external_secrets_read" {
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

resource "aws_iam_policy" "external_secrets_read" {
  name        = "${local.name}-external-secrets-policy"
  description = "Read only the Secrets Manager entries synchronized into the ticketing application namespace"
  policy      = data.aws_iam_policy_document.external_secrets_read.json

  tags = local.common_tags
}

resource "aws_iam_role_policy_attachment" "external_secrets_read" {
  role       = aws_iam_role.external_secrets.name
  policy_arn = aws_iam_policy.external_secrets_read.arn
}
