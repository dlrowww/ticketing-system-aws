# ============================================================
# Application DNS and TLS outputs
# ============================================================

output "acm_certificate_arn" {
  description = "Validated ACM certificate ARN to place in the ALB Ingress annotation; null when DNS is disabled."
  value       = local.enable_application_dns ? aws_acm_certificate_validation.application[0].certificate_arn : null
}

output "application_domain_name" {
  description = "Application hostname used to render the Kubernetes ConfigMaps and Ingress; null when DNS is disabled."
  value       = local.enable_application_dns ? var.application_domain_name : null
}
