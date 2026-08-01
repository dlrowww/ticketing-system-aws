# ============================================================
# Helm Provider
#
# 通过 AWS CLI 动态获得 EKS 登录 Token，
# 让 Terraform/Helm 能连接 Kubernetes API Server。
# ============================================================

provider "helm" {
  kubernetes = {
    host = aws_eks_cluster.eks_cluster.endpoint

    cluster_ca_certificate = base64decode(
      aws_eks_cluster.eks_cluster.certificate_authority[0].data
    )

    exec = {
      api_version = "client.authentication.k8s.io/v1"
      command     = "aws"

      args = [
        "eks",
        "get-token",
        "--cluster-name",
        aws_eks_cluster.eks_cluster.name,
        "--region",
        var.aws_region
      ]
    }
  }
}


# ============================================================
# Install AWS Load Balancer Controller using Helm
# ============================================================

resource "helm_release" "aws_load_balancer_controller" {
  name = "aws-load-balancer-controller"

  repository = "https://aws.github.io/eks-charts"
  chart      = "aws-load-balancer-controller"

  # AWS EKS 官方当前文档使用的 Chart 版本
  version = "1.14.0"

  namespace = local.aws_load_balancer_controller_namespace

  # 等价于 helm install 失败时回滚
  atomic = true

  # 最长等待时间，单位为秒
  timeout = 600

  values = [
    yamlencode({
      clusterName = aws_eks_cluster.eks_cluster.name

      region = var.aws_region

      vpcId = module.vpc.vpc_id

      serviceAccount = {
        create = true

        name = local.aws_load_balancer_controller_service_account

        annotations = {
          "eks.amazonaws.com/role-arn" = aws_iam_role.aws_load_balancer_controller.arn
        }
      }
    })
  ]

  depends_on = [
    aws_eks_node_group.eks_ng_private,
    aws_iam_role_policy_attachment.aws_load_balancer_controller
  ]
}