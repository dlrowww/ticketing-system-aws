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

variable "ecr_force_delete" {
  description = "Whether Terraform may delete non-empty ECR repositories. Null enables this automatically only for dev."
  type        = bool
  default     = null
  nullable    = true
}

variable "application_secrets_recovery_window_in_days" {
  description = "Secrets Manager deletion recovery window. Null selects 0 days for dev and 7 days for other environments."
  type        = number
  default     = null
  nullable    = true

  validation {
    condition = var.application_secrets_recovery_window_in_days == null ? true : (
      var.application_secrets_recovery_window_in_days == 0 ||
      (
        var.application_secrets_recovery_window_in_days >= 7 &&
        var.application_secrets_recovery_window_in_days <= 30
      )
    )
    error_message = "application_secrets_recovery_window_in_days must be 0 or between 7 and 30."
  }
}

locals {
  effective_ecr_force_delete = coalesce(
    var.ecr_force_delete,
    var.environment == "dev"
  )
  effective_application_secrets_recovery_window_in_days = coalesce(
    var.application_secrets_recovery_window_in_days,
    var.environment == "dev" ? 0 : 7
  )
  effective_db_backup_retention_days = coalesce(
    var.db_backup_retention_days,
    var.environment == "dev" ? 0 : 7
  )
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
  description = "Number of days that automated RDS backups are retained. Null selects 0 days for dev and 7 days for other environments."
  type        = number
  default     = null
  nullable    = true

  validation {
    condition = var.db_backup_retention_days == null ? true : (
      var.db_backup_retention_days >= 0 &&
      var.db_backup_retention_days <= 35
    )
    error_message = "db_backup_retention_days must be between 0 and 35."
  }
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
