# ============================================================
# Application DNS and TLS variables
# ============================================================

variable "route53_zone_id" {
  description = "Existing public Route 53 hosted zone ID. Leave null until a real domain is available."
  type        = string
  default     = null
  nullable    = true
}

variable "application_domain_name" {
  description = "Application hostname, for example tickets.example.com. Leave null to skip ACM and Route 53 resources."
  type        = string
  default     = null
  nullable    = true
}

variable "external_dns_namespace" {
  description = "Kubernetes namespace in which ExternalDNS is installed."
  type        = string
  default     = "external-dns"
}

variable "external_dns_service_account_name" {
  description = "Kubernetes ServiceAccount used by ExternalDNS through IRSA."
  type        = string
  default     = "external-dns"
}

variable "external_dns_chart_version" {
  description = "Pinned ExternalDNS Helm chart version."
  type        = string
  default     = "1.21.1"
}
