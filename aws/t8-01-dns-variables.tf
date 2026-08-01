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

variable "create_route53_alb_record" {
  description = "Create the application Route 53 alias after the Kubernetes Ingress has created its ALB."
  type        = bool
  default     = false
}

variable "alb_dns_name" {
  description = "DNS name of the ALB created by AWS Load Balancer Controller. Required when create_route53_alb_record is true."
  type        = string
  default     = null
  nullable    = true
}

variable "alb_zone_id" {
  description = "Canonical hosted zone ID of the ALB. Required when create_route53_alb_record is true."
  type        = string
  default     = null
  nullable    = true
}
