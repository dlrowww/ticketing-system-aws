# ============================================================
# Application resource outputs
# ============================================================

output "ecr_repository_urls" {
  description = "ECR repository URLs used by the backend and frontend Kubernetes Deployments."
  value = {
    for name, repository in aws_ecr_repository.application :
    name => repository.repository_url
  }
}

output "rds_endpoint" {
  description = "Private PostgreSQL endpoint in host:port form."
  value       = aws_db_instance.ticketing.endpoint
}

output "rds_address" {
  description = "Private PostgreSQL hostname, without the port."
  value       = aws_db_instance.ticketing.address
}

output "rds_database_name" {
  description = "Initial PostgreSQL database name."
  value       = aws_db_instance.ticketing.db_name
}

output "rds_master_user_secret_arn" {
  description = "ARN of the RDS-managed Secrets Manager secret containing the master database credentials."
  value       = aws_db_instance.ticketing.master_user_secret[0].secret_arn
}

output "application_secret_arns" {
  description = "Secrets Manager ARNs whose values must be populated outside Terraform."
  value = {
    for name, secret in aws_secretsmanager_secret.application :
    name => secret.arn
  }
}

output "application_secret_names" {
  description = "Secrets Manager names to pass to the application Helm chart ExternalSecret configuration."
  value = {
    for name, secret in aws_secretsmanager_secret.application :
    name => secret.name
  }
}
