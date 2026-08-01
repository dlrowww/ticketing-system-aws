# ============================================================
# External Secrets Operator variables
# ============================================================

variable "external_secrets_namespace" {
  description = "Kubernetes namespace in which External Secrets Operator is installed."
  type        = string
  default     = "external-secrets"
}

variable "external_secrets_service_account_name" {
  description = "Kubernetes ServiceAccount used by External Secrets Operator through IRSA."
  type        = string
  default     = "external-secrets"
}

variable "external_secrets_chart_version" {
  description = "Pinned External Secrets Operator Helm chart version."
  type        = string
  default     = "2.7.0"
}
