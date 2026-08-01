# ============================================================
# Application infrastructure variables
# ============================================================

variable "ecr_image_tag_mutability" {
  description = "Whether ECR image tags can be overwritten. IMMUTABLE is recommended for commit-SHA/version tags."
  type        = string
  default     = "IMMUTABLE"

  validation {
    condition     = contains(["MUTABLE", "IMMUTABLE"], var.ecr_image_tag_mutability)
    error_message = "ecr_image_tag_mutability must be MUTABLE or IMMUTABLE."
  }
}

variable "ecr_untagged_image_retention_days" {
  description = "Delete untagged ECR images after this many days."
  type        = number
  default     = 14
}

variable "db_name" {
  description = "Initial PostgreSQL database name."
  type        = string
  default     = "ticketing_system"
}

variable "db_master_username" {
  description = "RDS master username. The password is generated and managed by RDS in Secrets Manager."
  type        = string
  default     = "ticketing_admin"
}

variable "db_engine_version" {
  description = "PostgreSQL major version. Pin a full minor version only when you intentionally need one."
  type        = string
  default     = "16"
}

variable "db_instance_class" {
  description = "RDS instance class. db.t4g.micro is intended for a small development environment."
  type        = string
  default     = "db.t4g.micro"
}

variable "db_allocated_storage" {
  description = "Initial RDS storage size in GiB."
  type        = number
  default     = 20
}

variable "db_max_allocated_storage" {
  description = "Maximum storage size in GiB to which RDS storage autoscaling may grow."
  type        = number
  default     = 100
}

variable "db_multi_az" {
  description = "Create a synchronous standby in another Availability Zone. Recommended for production."
  type        = bool
  default     = false
}

variable "db_backup_retention_days" {
  description = "Number of days that automated RDS backups are retained."
  type        = number
  default     = 7
}

variable "db_deletion_protection" {
  description = "Protect the RDS instance from accidental deletion. Enable for production."
  type        = bool
  default     = false
}

variable "db_skip_final_snapshot" {
  description = "Skip the final RDS snapshot when deleting the instance. Keep true only for disposable development environments."
  type        = bool
  default     = true
}

variable "db_apply_immediately" {
  description = "Apply RDS changes immediately instead of during the maintenance window."
  type        = bool
  default     = false
}

variable "application_namespace" {
  description = "Kubernetes namespace in which the ticketing application will run."
  type        = string
  default     = "ticketing-system"
}

variable "api_service_account_name" {
  description = "Kubernetes ServiceAccount name used by the Ticketing API pods."
  type        = string
  default     = "ticketing-api"
}

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

