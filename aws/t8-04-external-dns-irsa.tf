# ============================================================
# ExternalDNS IRSA
#
# ExternalDNS observes the application Ingress and reconciles its hostname to
# the ALB address published by AWS Load Balancer Controller. AWS access is
# restricted to the configured public Route 53 hosted zone.
# ============================================================

data "aws_iam_policy_document" "external_dns_assume_role" {
  count = local.enable_application_dns ? 1 : 0

  statement {
    sid     = "AllowExternalDNSServiceAccount"
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
        "system:serviceaccount:${var.external_dns_namespace}:${var.external_dns_service_account_name}"
      ]
    }
  }
}

resource "aws_iam_role" "external_dns" {
  count = local.enable_application_dns ? 1 : 0

  name               = "${local.name}-external-dns-role"
  assume_role_policy = data.aws_iam_policy_document.external_dns_assume_role[0].json

  tags = merge(
    local.common_tags,
    {
      Name = "${local.name}-external-dns-role"
    }
  )
}

data "aws_iam_policy_document" "external_dns_route53" {
  count = local.enable_application_dns ? 1 : 0

  statement {
    sid    = "ChangeRecordsInApplicationHostedZone"
    effect = "Allow"
    actions = [
      "route53:ChangeResourceRecordSets",
      "route53:ListResourceRecordSets",
      "route53:ListTagsForResources",
    ]
    resources = [
      "arn:${data.aws_partition.current.partition}:route53:::hostedzone/${var.route53_zone_id}"
    ]
  }

  statement {
    sid       = "DiscoverHostedZones"
    effect    = "Allow"
    actions   = ["route53:ListHostedZones"]
    resources = ["*"]
  }
}

resource "aws_iam_policy" "external_dns_route53" {
  count = local.enable_application_dns ? 1 : 0

  name        = "${local.name}-external-dns-policy"
  description = "Manage only the Route 53 hosted zone configured for the ticketing application"
  policy      = data.aws_iam_policy_document.external_dns_route53[0].json

  tags = local.common_tags
}

resource "aws_iam_role_policy_attachment" "external_dns_route53" {
  count = local.enable_application_dns ? 1 : 0

  role       = aws_iam_role.external_dns[0].name
  policy_arn = aws_iam_policy.external_dns_route53[0].arn
}
