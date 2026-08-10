# GitHub Actions IAM roles are bootstrapped outside this Terraform state so
# they can assume AWS credentials before the infrastructure exists.
data "aws_iam_role" "github_deployment" {
  for_each = toset([
    "application",
    "kubernetes",
  ])

  name = "ticketing-system-github-${each.key}"
}

# Authenticate both deployment workflows through the EKS Access Entry API.
# aws_eks_cluster.eks_cluster enables API_AND_CONFIG_MAP before these entries
# are created, while retaining compatibility with the managed-node aws-auth
# mappings.
resource "aws_eks_access_entry" "github_deployment" {
  for_each = data.aws_iam_role.github_deployment

  cluster_name  = aws_eks_cluster.eks_cluster.name
  principal_arn = each.value.arn
  type          = "STANDARD"
}

# The deployment roles may administer all namespaced resources required by
# this project, including External Secrets custom resources, but cannot manage
# resources in other namespaces or cluster-scoped resources.
resource "aws_eks_access_policy_association" "github_deployment" {
  for_each = aws_eks_access_entry.github_deployment

  cluster_name  = aws_eks_cluster.eks_cluster.name
  principal_arn = each.value.principal_arn
  policy_arn    = "arn:${data.aws_partition.current.partition}:eks::aws:cluster-access-policy/AmazonEKSClusterAdminPolicy"

  access_scope {
    type       = "namespace"
    namespaces = ["ticketing-system"]
  }
}

# Allow the local AWS CLI operator to inspect workloads without granting
# write access or permissions outside the application namespace.
data "aws_iam_user" "local_operator" {
  user_name = "Simon"
}

resource "aws_eks_access_entry" "local_operator" {
  cluster_name  = aws_eks_cluster.eks_cluster.name
  principal_arn = data.aws_iam_user.local_operator.arn
  type          = "STANDARD"
}

resource "aws_eks_access_policy_association" "local_operator" {
  cluster_name  = aws_eks_cluster.eks_cluster.name
  principal_arn = aws_eks_access_entry.local_operator.principal_arn
  policy_arn    = "arn:${data.aws_partition.current.partition}:eks::aws:cluster-access-policy/AmazonEKSAdminViewPolicy"

  access_scope {
    type       = "namespace"
    namespaces = ["ticketing-system"]
  }
}
