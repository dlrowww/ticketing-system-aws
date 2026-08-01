# Ticketing System AWS application infrastructure

The `t6-*` Terraform files extend the existing VPC/EKS foundation with the
AWS resources required by the application.

## Managed resources

- Two private ECR repositories: backend and frontend.
- Private RDS for PostgreSQL in the database subnet group created by the VPC
  module.
- RDS-managed master password in AWS Secrets Manager.
- Empty Secrets Manager containers for JWT, SMTP and initial-admin runtime
  configuration.
- A least-privilege IRSA role that the future `ticketing-api` Kubernetes
  ServiceAccount can use to read only those secrets.
- Optional ACM DNS-validated certificate.
- Optional Route 53 alias from the application hostname to the ALB.
- CloudWatch Observability EKS add-on, IAM role and application log retention.

## Important safety defaults

The defaults are sized for a development environment:

- RDS class is `db.t4g.micro`.
- RDS is Single-AZ.
- RDS deletion protection is disabled.
- Final snapshot is skipped.
- EKS application container logs are retained for 30 days.

Production should normally override at least:

```hcl
db_instance_class        = "<production-sized-class>"
db_multi_az              = true
db_deletion_protection   = true
db_skip_final_snapshot   = false
db_backup_retention_days = 14
```

## Secrets

Terraform intentionally creates the JWT, SMTP and initial-admin Secret
resources without creating Secret versions. Supplying secret values through
Terraform would store those values in Terraform state.

Populate the secrets after creation using an approved process. Suggested JSON
shapes are:

```json
{"key":"a-random-JWT-key-of-at-least-64-characters"}
```

```json
{"username":"smtp-user","password":"smtp-password"}
```

```json
{"email":"admin@example.com","password":"initial-password"}
```

RDS creates and rotates its master credential secret itself. The application
still needs Kubernetes-side integration (for example External Secrets or the
Secrets Store CSI Driver) to transform these values into the ASP.NET
environment variables expected by the current application.

## DNS deployment is intentionally two-phase

The AWS Load Balancer Controller creates the ALB only after the application
Ingress exists, so Terraform cannot know the ALB address during the first
infrastructure apply.

1. Set `route53_zone_id` and `application_domain_name`, then apply Terraform.
   Use the `acm_certificate_arn` output in the Kubernetes ALB Ingress.
2. Deploy the Ingress and wait for the controller to create the ALB.
3. Read the ALB DNS name and canonical hosted zone ID.
4. Set `create_route53_alb_record`, `alb_dns_name`, and `alb_zone_id`, then
   apply Terraform again.

See `terraform.tfvars.example` for the expected variables.

## Before applying

The current backend state must be reviewed first:

```bash
terraform -chdir=aws state list
terraform -chdir=aws plan
```

Do not apply a plan that unexpectedly recreates the existing VPC or EKS
cluster. Import existing resources or select the correct backend/workspace
before applying.

