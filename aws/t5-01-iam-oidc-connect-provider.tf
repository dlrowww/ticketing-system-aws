# 查询当前 AWS Partition。
# 在普通 AWS 区域中，dns_suffix 最终是 amazonaws.com。
data "aws_partition" "current" {}

# 在 AWS IAM 中注册当前 EKS Cluster 的 OIDC Provider。
resource "aws_iam_openid_connect_provider" "eks" {
  # 直接从 EKS Cluster 获取 OIDC Issuer URL。
  # 不需要手动复制，也不需要通过 output 传递。
  url = aws_eks_cluster.eks_cluster.identity[0].oidc[0].issuer

  # 表示通过这个 OIDC Token 向 AWS STS 申请临时凭证。
  client_id_list = [
    "sts.${data.aws_partition.current.dns_suffix}"
  ]

  tags = merge(
    local.common_tags,
    {
      Name = "${local.eks_cluster_name}-oidc-provider"
    }
  )
}

# 去掉 OIDC URL 前面的 https://。
# 后面编写 IAM Role Trust Policy 时会用到。
locals {
  eks_oidc_provider_url = replace(
    aws_eks_cluster.eks_cluster.identity[0].oidc[0].issuer,
    "https://",
    ""
  )
}

# 可选：方便 terraform apply 后查看 OIDC Provider ARN。
output "eks_oidc_provider_arn" {
  description = "ARN of the IAM OIDC provider for the EKS cluster"
  value       = aws_iam_openid_connect_provider.eks.arn
}

# 可选：方便查看去掉 https:// 后的 OIDC Provider 地址。
output "eks_oidc_provider_url" {
  description = "EKS OIDC provider URL without https://"
  value       = local.eks_oidc_provider_url
}