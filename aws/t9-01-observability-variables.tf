# ============================================================
# Observability variables
# ============================================================

variable "enable_cloudwatch_observability" {
  description = "Install the Amazon CloudWatch Observability EKS add-on and collect container stdout/stderr logs."
  type        = bool
  default     = true
}

variable "cloudwatch_log_retention_days" {
  description = "Retention period for EKS application container logs."
  type        = number
  default     = 30
}
