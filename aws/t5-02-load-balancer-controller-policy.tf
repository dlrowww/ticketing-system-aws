# ============================================================
# IAM Policy for AWS Load Balancer Controller
#
# 这个 Policy 决定 Controller 可以调用哪些 AWS API，
# 例如创建 ALB、Target Group、Listener 和 Security Group。
# ============================================================

resource "aws_iam_policy" "aws_load_balancer_controller" {
  name = "${local.eks_cluster_name}-aws-load-balancer-controller-policy"

  description = "IAM policy for AWS Load Balancer Controller in EKS cluster ${local.eks_cluster_name}"

  # 读取保存在项目中的 AWS 官方 IAM Policy JSON
  policy = file(
    "${path.module}/policies/aws-load-balancer-controller-v2.14.1.json"
  )

  tags = merge(
    local.common_tags,
    {
      Name = "${local.eks_cluster_name}-aws-load-balancer-controller-policy"
    }
  )
}

output "aws_load_balancer_controller_policy_arn" {
  description = "ARN of the AWS Load Balancer Controller IAM policy"
  value       = aws_iam_policy.aws_load_balancer_controller.arn
}