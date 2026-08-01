locals {
  owner        = var.owner
  environment  = var.environment
  name         = "${var.project_name}-${var.environment}"
  project_name = var.project_name
  common_tags = {
    Owner       = local.owner
    Environment = local.environment
    Project     = local.project_name
  }
  eks_cluster_name = "${local.name}-${var.cluster_name}"
}