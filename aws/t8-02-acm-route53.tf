# ============================================================
# Optional ACM certificate and Route 53 validation record
#
# ACM is created once route53_zone_id and application_domain_name are set.
# ExternalDNS creates and reconciles the application Alias after Kubernetes
# AWS Load Balancer Controller publishes the ALB address on the Ingress.
# ============================================================

locals {
  enable_application_dns = (
    var.route53_zone_id != null &&
    var.application_domain_name != null
  )
}

check "application_dns_inputs" {
  assert {
    condition = (
      (var.route53_zone_id == null && var.application_domain_name == null) ||
      (var.route53_zone_id != null && var.application_domain_name != null)
    )
    error_message = "Set route53_zone_id and application_domain_name together, or leave both null."
  }
}

resource "aws_acm_certificate" "application" {
  count = local.enable_application_dns ? 1 : 0

  domain_name       = var.application_domain_name
  validation_method = "DNS"

  tags = merge(
    local.common_tags,
    {
      Name = var.application_domain_name
    }
  )

  lifecycle {
    create_before_destroy = true
  }
}

resource "aws_route53_record" "certificate_validation" {
  count = local.enable_application_dns ? 1 : 0

  zone_id = var.route53_zone_id
  name    = tolist(aws_acm_certificate.application[0].domain_validation_options)[0].resource_record_name
  type    = tolist(aws_acm_certificate.application[0].domain_validation_options)[0].resource_record_type
  records = [tolist(aws_acm_certificate.application[0].domain_validation_options)[0].resource_record_value]
  ttl     = 60
}

resource "aws_acm_certificate_validation" "application" {
  count = local.enable_application_dns ? 1 : 0

  certificate_arn         = aws_acm_certificate.application[0].arn
  validation_record_fqdns = [aws_route53_record.certificate_validation[0].fqdn]
}

# Preserve the existing application Alias while transferring ownership from
# Terraform to ExternalDNS. After the first apply, Terraform forgets the old
# resource without deleting the live DNS record; ExternalDNS then reconciles
# the same endpoint and adds its TXT ownership record.
removed {
  from = aws_route53_record.application

  lifecycle {
    destroy = false
  }
}
