# ============================================================
# Observability outputs
# ============================================================

output "cloudwatch_application_log_group" {
  description = "CloudWatch log group receiving Kubernetes container stdout/stderr."
  value       = var.enable_cloudwatch_observability ? aws_cloudwatch_log_group.container_application[0].name : null
}
