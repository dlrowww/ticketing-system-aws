# ============================================================
# ExternalDNS
#
# The controller reads the hostname and ALB address from Kubernetes Ingress,
# then creates and maintains the matching Route 53 Alias and ownership TXT
# record. This removes the manual ALB DNS/zone-ID feedback loop.
# ============================================================

resource "helm_release" "external_dns" {
  count = local.enable_application_dns ? 1 : 0

  name       = "external-dns"
  repository = "https://kubernetes-sigs.github.io/external-dns/"
  chart      = "external-dns"
  version    = var.external_dns_chart_version

  namespace        = var.external_dns_namespace
  create_namespace = true

  atomic        = true
  timeout       = 600
  wait          = true
  wait_for_jobs = true

  values = [
    yamlencode({
      provider = {
        name = "aws"
      }

      sources            = ["ingress"]
      #domainFilters      = [var.application_domain_name]
      policy             = "sync"
      registry           = "txt"
      txtOwnerId         = "${local.name}-external-dns"
      interval           = "1m"
      triggerLoopOnEvent = true

      extraArgs = {
        "aws-zone-type"  = "public"
        "namespace"      = "ticketing-system"
        "zone-id-filter" = var.route53_zone_id
      }

      serviceAccount = {
        create = true
        name   = var.external_dns_service_account_name
        annotations = {
          "eks.amazonaws.com/role-arn" = aws_iam_role.external_dns[0].arn
        }
      }
    })
  ]

  depends_on = [
    aws_eks_node_group.eks_ng_private,
    aws_iam_role_policy_attachment.external_dns_route53,
    helm_release.aws_load_balancer_controller,
  ]
}
