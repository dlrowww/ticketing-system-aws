# ============================================================
# Optional ACM certificate and Route 53 records
#
# ACM is created once route53_zone_id and application_domain_name are set.
# The ALB alias is a second step because the ALB is created later by the
# Kubernetes AWS Load Balancer Controller.
# ============================================================

locals {
  enable_application_dns = (
    var.route53_zone_id != null &&
    var.application_domain_name != null
  )

  enable_route53_alb_alias = (
    local.enable_application_dns &&
    var.create_route53_alb_record &&
    var.alb_dns_name != null &&
    var.alb_zone_id != null
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

check "route53_alb_alias_inputs" {
  assert {
    condition = (
      !var.create_route53_alb_record ||
      (
        local.enable_application_dns &&
        var.alb_dns_name != null &&
        var.alb_zone_id != null
      )
    )
    error_message = "When create_route53_alb_record is true, application DNS, alb_dns_name, and alb_zone_id must all be set."
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

resource "aws_route53_record" "application" {
  count = local.enable_route53_alb_alias ? 1 : 0

  zone_id = var.route53_zone_id
  name    = var.application_domain_name
  type    = "A"

  alias {
    name                   = var.alb_dns_name
    zone_id                = var.alb_zone_id
    evaluate_target_health = true
  }
}

output "acm_certificate_arn" {
  description = "Validated ACM certificate ARN to place in the ALB Ingress annotation; null when DNS is disabled."
  value       = local.enable_application_dns ? aws_acm_certificate_validation.application[0].certificate_arn : null
}
