# ============================================================
# External Secrets Operator
#
# The chart installs and manages its CRDs. Its controller ServiceAccount uses
# the IRSA role above, so SecretStore resources do not contain AWS credentials.
# ============================================================

resource "helm_release" "external_secrets" {
  name = "external-secrets"

  repository = "https://charts.external-secrets.io"
  chart      = "external-secrets"
  version    = var.external_secrets_chart_version

  namespace        = var.external_secrets_namespace
  create_namespace = true

  atomic  = true
  timeout = 600

  values = [
    yamlencode({
      installCRDs = true

      serviceAccount = {
        create = true
        name   = var.external_secrets_service_account_name
        annotations = {
          "eks.amazonaws.com/role-arn" = aws_iam_role.external_secrets.arn
        }
      }
    })
  ]

  depends_on = [
    aws_eks_node_group.eks_ng_private,
    aws_iam_role_policy_attachment.external_secrets_read,
  ]
}
