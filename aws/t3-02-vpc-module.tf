data "aws_availability_zones" "available" {
}

module "vpc" {
  source  = "terraform-aws-modules/vpc/aws"
  version = "6.6.1"

  name = local.eks_cluster_name
  cidr = var.vpc_cidr
  azs  = slice(data.aws_availability_zones.available.names, 0, 2)
  # 这里的azs是可用区的列表，使用了AWS提供的数据源aws_availability_zones来获取可用区的名称，并使用slice函数取前两个可用区。

  public_subnets               = var.vpc_public_subnets
  private_subnets              = var.vpc_private_subnets
  database_subnets             = var.vpc_database_subnets
  create_database_subnet_group = var.vpc_create_database_subnet_group
  enable_dns_hostnames         = true
  enable_dns_support           = true
  # 这里的enable_dns_hostnames和enable_dns_support都是布尔值，表示是否启用DNS主机名和DNS支持，设置为true表示启用。

  enable_nat_gateway     = var.vpc_enable_nat_gateway
  single_nat_gateway     = var.vpc_single_nat_gateway
  one_nat_gateway_per_az = var.vpc_one_nat_gateway_per_az


  tags     = local.common_tags
  vpc_tags = merge(local.common_tags, { Type = "EKS-VPC" })

  map_public_ip_on_launch = false
  # 这里的map_public_ip_on_launch是一个布尔值，表示是否在启动实例时自动分配公共IP地址，设置为false表示不分配。

  public_subnet_tags = {
    Type                                              = "EKS-Public-Subnet"
    "kubernetes.io/role/elb"                          = 1
    "kubernetes.io/cluster/${local.eks_cluster_name}" = "shared"
  }

  private_subnet_tags = {
    Type                                              = "EKS-Private-Subnet"
    "kubernetes.io/role/internal-elb"                 = 1
    "kubernetes.io/cluster/${local.eks_cluster_name}" = "shared"
  }

  database_subnet_tags = {
    Type = "EKS-Database-Subnet"
  }
}

