# ============================================================
# IRSA IAM Role for AWS Load Balancer Controller
# ============================================================

# Controller 使用的 Kubernetes 身份
locals {
  aws_load_balancer_controller_namespace       = "kube-system"
  aws_load_balancer_controller_service_account = "aws-load-balancer-controller"
}


# ============================================================
# Trust Policy
#
# 决定谁可以 Assume 这个 IAM Role。
# 这里只允许：
#
# Namespace: kube-system
# ServiceAccount: aws-load-balancer-controller
# ============================================================

data "aws_iam_policy_document" "aws_load_balancer_controller_assume_role" {
  statement {
    sid    = "AllowAWSLoadBalancerControllerServiceAccount"
    effect = "Allow"

    actions = [
      "sts:AssumeRoleWithWebIdentity"
    ]

    principals {
      type = "Federated"

      identifiers = [
        aws_iam_openid_connect_provider.eks.arn
      ]
    }

    # 限制 Token 的 audience 必须是 AWS STS
    condition {
      test = "StringEquals"

      variable = "${local.eks_oidc_provider_url}:aud"

      values = [
        "sts.${data.aws_partition.current.dns_suffix}"
      ]
    }

    # 只允许指定 Namespace 中指定的 ServiceAccount
    condition {
      test = "StringEquals"

      variable = "${local.eks_oidc_provider_url}:sub"

      values = [
        "system:serviceaccount:${local.aws_load_balancer_controller_namespace}:${local.aws_load_balancer_controller_service_account}"
      ]
    }
  }
}


# ============================================================
# Create IAM Role
# ============================================================

resource "aws_iam_role" "aws_load_balancer_controller" {
  name = "${local.eks_cluster_name}-aws-load-balancer-controller-role"

  assume_role_policy = data.aws_iam_policy_document.aws_load_balancer_controller_assume_role.json

  tags = merge(
    local.common_tags,
    {
      Name = "${local.eks_cluster_name}-aws-load-balancer-controller-role"
    }
  )
}


# ============================================================
# Attach IAM Policy to IAM Role
# ============================================================

resource "aws_iam_role_policy_attachment" "aws_load_balancer_controller" {
  role = aws_iam_role.aws_load_balancer_controller.name

  policy_arn = aws_iam_policy.aws_load_balancer_controller.arn
}


output "aws_load_balancer_controller_role_arn" {
  description = "IAM Role ARN used by AWS Load Balancer Controller"
  value       = aws_iam_role.aws_load_balancer_controller.arn
}