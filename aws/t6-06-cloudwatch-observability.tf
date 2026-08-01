# ============================================================
# CloudWatch application/container log collection for EKS
# ============================================================

locals {
  cloudwatch_observability_namespace       = "amazon-cloudwatch"
  cloudwatch_observability_service_account = "cloudwatch-agent"
}

resource "aws_cloudwatch_log_group" "container_application" {
  count = var.enable_cloudwatch_observability ? 1 : 0

  name              = "/aws/containerinsights/${local.eks_cluster_name}/application"
  retention_in_days = var.cloudwatch_log_retention_days

  tags = merge(
    local.common_tags,
    {
      Name = "${local.eks_cluster_name}-application-logs"
    }
  )
}

data "aws_iam_policy_document" "cloudwatch_observability_assume_role" {
  statement {
    sid     = "AllowCloudWatchAgentServiceAccount"
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
        "system:serviceaccount:${local.cloudwatch_observability_namespace}:${local.cloudwatch_observability_service_account}"
      ]
    }
  }
}

resource "aws_iam_role" "cloudwatch_observability" {
  count = var.enable_cloudwatch_observability ? 1 : 0

  name               = "${local.name}-cloudwatch-observability-role"
  assume_role_policy = data.aws_iam_policy_document.cloudwatch_observability_assume_role.json

  tags = merge(
    local.common_tags,
    {
      Name = "${local.name}-cloudwatch-observability-role"
    }
  )
}

resource "aws_iam_role_policy_attachment" "cloudwatch_agent" {
  count = var.enable_cloudwatch_observability ? 1 : 0

  role       = aws_iam_role.cloudwatch_observability[0].name
  policy_arn = "arn:${data.aws_partition.current.partition}:iam::aws:policy/CloudWatchAgentServerPolicy"
}

resource "aws_iam_role_policy_attachment" "cloudwatch_xray" {
  count = var.enable_cloudwatch_observability ? 1 : 0

  role       = aws_iam_role.cloudwatch_observability[0].name
  policy_arn = "arn:${data.aws_partition.current.partition}:iam::aws:policy/AWSXrayWriteOnlyAccess"
}

resource "aws_eks_addon" "cloudwatch_observability" {
  count = var.enable_cloudwatch_observability ? 1 : 0

  cluster_name             = aws_eks_cluster.eks_cluster.name
  addon_name               = "amazon-cloudwatch-observability"
  service_account_role_arn = aws_iam_role.cloudwatch_observability[0].arn

  resolve_conflicts_on_create = "OVERWRITE"
  resolve_conflicts_on_update = "PRESERVE"

  depends_on = [
    aws_eks_node_group.eks_ng_private,
    aws_iam_role_policy_attachment.cloudwatch_agent,
    aws_iam_role_policy_attachment.cloudwatch_xray,
    aws_cloudwatch_log_group.container_application,
  ]
}

output "cloudwatch_application_log_group" {
  description = "CloudWatch log group receiving Kubernetes container stdout/stderr."
  value       = var.enable_cloudwatch_observability ? aws_cloudwatch_log_group.container_application[0].name : null
}

