# Create AWS EKS Cluster
resource "aws_eks_cluster" "eks_cluster" {
  name     = "${local.name}-${var.cluster_name}"
  role_arn = aws_iam_role.eks_master_role.arn
  #EKS 控制平面用什么 AWS 身份
  version = var.cluster_version

  # Keep legacy aws-auth compatibility while enabling EKS Access Entries.
  # The infrastructure role creates the cluster and needs administrator access
  # during the same apply to install controllers and cluster-scoped resources.
  access_config {
    authentication_mode                         = "API_AND_CONFIG_MAP"
    bootstrap_cluster_creator_admin_permissions = true
  }

  vpc_config {
    subnet_ids = module.vpc.public_subnets
    #表示 EKS 在这些 Subnet 中创建网络接口，用来让 AWS 管理的 Control Plane 与你的 Worker Nodes 通信。
    endpoint_private_access = var.cluster_endpoint_private_access
    #表示允许通过 VPC 内部私有网络访问 Kubernetes API Server。
    endpoint_public_access = var.cluster_endpoint_public_access
    #表示允许通过互联网访问 Kubernetes API Server。
    public_access_cidrs = var.cluster_endpoint_public_access_cidrs
    #表示允许访问 Kubernetes API Server 的 CIDR 块列表。只有来自这些 CIDR 块的请求才能访问 EKS 控制平面。
  }
  # EKS 控制平面怎样接入 AWS 网络

  kubernetes_network_config {
    service_ipv4_cidr = var.cluster_service_ipv4_cidr
  }
  # EKS 控制平面如何分配 Kubernetes Service 的 IP 地址范围

  # Enable EKS Cluster Control Plane Logging
  enabled_cluster_log_types = ["api", "audit", "authenticator", "controllerManager", "scheduler"] # 这些日志类型可以根据需要进行调整
  # 把 EKS Control Plane 的这些日志发送到 CloudWatch Logs。
  /*
    api	谁向 Kubernetes API Server 发了请求，以及 API 是否正常
    audit	谁在什么时间操作了什么 Kubernetes 资源
    authenticator	IAM 身份登录和认证是否成功
    controllerManager	Kubernetes 控制器如何维持 Pod、Node 等资源状态
    scheduler	Pod 为什么被安排到某台 Worker Node，或为什么无法调度
  */

  # Ensure that IAM Role permissions are created before and deleted after EKS Cluster handling.
  # Otherwise, EKS will not be able to properly delete EKS managed EC2 infrastructure such as Security Groups.
  depends_on = [
    aws_iam_role_policy_attachment.eks-AmazonEKSClusterPolicy,
    aws_iam_role_policy_attachment.eks-AmazonEKSVPCResourceController,
  ]
}
