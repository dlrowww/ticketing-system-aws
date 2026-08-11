# Ticketing System AWS application infrastructure

The `t6-*` through `t9-*` Terraform stages extend the existing VPC/EKS
foundation with application resources, secret synchronization, DNS/TLS and
observability.

## Managed resources

- Two private ECR repositories: backend and frontend.
- Private RDS for PostgreSQL in the database subnet group created by the VPC
  module.
- RDS-managed master password in AWS Secrets Manager.
- Empty Secrets Manager containers for JWT, SMTP and initial-admin runtime
  configuration.
- External Secrets Operator, installed through Helm with its CRDs.
- A least-privilege IRSA role used only by the External Secrets Operator
  ServiceAccount to read those secrets.
- Optional ACM DNS-validated certificate.
- Optional Route 53 alias from the application hostname to the ALB.
- CloudWatch Observability EKS add-on, IAM role and application log retention.

## Important safety defaults

The defaults are sized for a development environment:

- RDS class is `db.t4g.micro`.
- RDS is Single-AZ.
- RDS deletion protection is disabled.
- Final snapshot is skipped.
- RDS automated backups are disabled.
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

RDS creates and rotates its master credential secret itself. Terraform installs
External Secrets Operator with a dedicated IRSA role. The application Helm
chart then creates a `SecretStore` and `ExternalSecret` which combine the four
AWS secrets into the `ticketing-system-runtime` Kubernetes Secret expected by
the Deployments.

The API ServiceAccount has no AWS role. Secret-reading responsibility belongs
only to the External Secrets Operator controller in the `external-secrets`
namespace.

Pass these Terraform outputs to the application Helm release:

```text
rds_master_user_secret_arn
application_secret_names["jwt"]
application_secret_names["smtp"]
application_secret_names["initial-admin"]
```

External Secrets refreshes the Kubernetes Secret hourly by default. Kubernetes
does not update environment variables inside already-running containers, so
restart or roll out the application Deployments after rotating a secret.

## Terraform stage structure

The application layer keeps declarations separated by responsibility:

```text
t6  Application resources
    t6-01  variables
    t6-02  ECR
    t6-03  RDS PostgreSQL
    t6-04  application Secrets Manager containers
    t6-05  outputs

t7  External Secrets Operator
    t7-01  variables
    t7-02  IAM/IRSA
    t7-03  Helm release
    t7-04  outputs

t8  DNS and TLS
    t8-01  variables
    t8-02  ACM and Route 53 validation
    t8-03  outputs
    t8-04  ExternalDNS IAM / IRSA
    t8-05  ExternalDNS Helm release

t9  Observability
    t9-01  variables
    t9-02  CloudWatch Observability
    t9-03  outputs
```

Terraform still loads every `.tf` file as one configuration. These stage names
document responsibility and dependency flow; they do not control apply order.

## DNS deployment is automatic after the Ingress exists

Set `route53_zone_id` and `application_domain_name`, then apply Terraform. This
creates and validates the ACM certificate and installs ExternalDNS with an IRSA
role restricted to that Route 53 hosted zone. The Application workflow renders
the certificate and hostname into the Ingress. AWS Load Balancer Controller
then creates the ALB, and ExternalDNS automatically reconciles the Ingress host
to a Route 53 Alias plus its ownership TXT record.

No ALB DNS name or canonical hosted zone ID needs to be copied into Terraform.
The existing Terraform-managed Alias is preserved during the first migration
apply and then reconciled by ExternalDNS, avoiding a DNS deletion window.
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
