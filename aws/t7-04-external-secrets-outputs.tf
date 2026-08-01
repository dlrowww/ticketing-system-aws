# ============================================================
# External Secrets Operator outputs
# ============================================================

output "external_secrets_irsa_role_arn" {
  description = "IAM role assumed by the External Secrets Operator controller ServiceAccount."
  value       = aws_iam_role.external_secrets.arn
}
