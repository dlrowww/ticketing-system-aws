# Create AWS EKS Node Group - Private

resource "aws_eks_node_group" "eks_ng_private" {
  cluster_name = aws_eks_cluster.eks_cluster.name

  node_group_name = "${local.name}-eks-ng-private"
  node_role_arn   = aws_iam_role.eks_nodegroup_role.arn
  subnet_ids      = module.vpc.private_subnets
  #决定“真正的 EC2 Worker Nodes”创建在哪里
  #version = var.cluster_version #(Optional: Defaults to EKS Cluster Kubernetes version)    

  ami_type = "AL2023_x86_64_STANDARD"
  #节点安装什么操作系统
  capacity_type = "ON_DEMAND"
  # EC2 按什么方式购买
  disk_size = 20
  # 每台节点的系统硬盘大小
  instance_types = ["t3.small"]
  # 每台节点的 CPU、内存规格

  /*
  remote_access {
    ec2_ssh_key = "eks-terraform-key"    
  }
  */

  scaling_config {
    desired_size = 2
    min_size     = 2
    max_size     = 2
  }

  # Desired max percentage of unavailable worker nodes during node group update.
  update_config {
    max_unavailable = 1
    #max_unavailable_percentage = 50    # ANY ONE TO USE
  }

  # Ensure that IAM Role permissions are created before and deleted after EKS Node Group handling.
  # Otherwise, EKS will not be able to properly delete EC2 Instances and Elastic Network Interfaces.
  depends_on = [
    aws_iam_role_policy_attachment.eks-AmazonEKSWorkerNodePolicy,
    aws_iam_role_policy_attachment.eks-AmazonEKS_CNI_Policy,
    aws_iam_role_policy_attachment.eks-AmazonEC2ContainerRegistryReadOnly,
    aws_iam_role_policy_attachment.eks_nodegroup_ssm,
  ]
  tags = {
    Name = "Private-Node-Group"
  }
}

