# ============================================================
# Application DNS and TLS outputs
# ============================================================

output "acm_certificate_arn" {
  description = "Validated ACM certificate ARN to place in the ALB Ingress annotation; null when DNS is disabled."
  value       = local.enable_application_dns ? aws_acm_certificate_validation.application[0].certificate_arn : null
}
