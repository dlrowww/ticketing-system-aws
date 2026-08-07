# DevOps helper scripts

These scripts bootstrap the AWS Secrets Manager values and generate the raw
Kubernetes `SecretStore`/`ExternalSecret` manifest without manually copying
Terraform outputs.

## CI/CD Kubernetes deployment

`deploy-k8s.sh` is the shared deployment entry point used by both application
CD and Kubernetes-only CD. It renders temporary immutable image references,
synchronizes External Secrets, runs the database migration Job, and only then
rolls out the API and frontend Deployments.

```bash
terraform -chdir=aws init
./devops_scripts/deploy-k8s.sh \
  --backend-image ACCOUNT.dkr.ecr.us-east-1.amazonaws.com/ticketing-system-dev/ticketing-backend:GIT_SHA \
  --frontend-image ACCOUNT.dkr.ecr.us-east-1.amazonaws.com/ticketing-system-dev/ticketing-frontend:GIT_SHA \
  --domain tickets.example.com \
  --certificate-arn arn:aws:acm:us-east-1:ACCOUNT:certificate/ID
```

The domain and certificate options are optional as a pair. If omitted, Ingress
is not applied. The script does not initialize secret values and never invokes
the interactive `bootstrap-secrets.sh`.

## Prerequisites

- Terraform has already been initialized and applied in `aws/`.
- The caller can read Terraform state and update the three application Secrets.
- `terraform`, `aws`, `jq`, `openssl`, `envsubst` and optionally `kubectl` are
  installed.
- External Secrets Operator is installed before applying the generated YAML.

## 1. Bootstrap secret values

```bash
./devops_scripts/bootstrap-secrets.sh
```

The script:

1. reads `application_secret_names` and `rds_master_user_secret_arn` from
   Terraform;
2. verifies the AWS identity and the three Secret resources;
3. generates a 64-byte random JWT key;
4. securely prompts for SMTP and initial-admin credentials;
5. builds JSON with `jq` in temporary `0600` files;
6. creates new Secrets Manager versions and immediately removes the files.

It refuses to replace an existing `AWSCURRENT` version by default. Intentional
rotation requires both `--force` and typing `ROTATE`:

```bash
./devops_scripts/bootstrap-secrets.sh --force
```

Use `--profile` or `--region` when the default AWS/Terraform context is not correct.
Secret values are never passed directly in command arguments or printed.

## 2. Render or apply ExternalSecret YAML

Render to stdout for review:

```bash
./devops_scripts/render-external-secrets.sh
```

Write an environment-specific file (the script refuses to overwrite it unless
`--force` is present):

```bash
./devops_scripts/render-external-secrets.sh \
  --output /tmp/ticketing-external-secrets.yaml
```

Render into a temporary file, apply it, wait for synchronization and delete the
temporary file:

```bash
./devops_scripts/render-external-secrets.sh --apply
```

Use `--context` when the current kubectl context is not the intended cluster.
The script checks the Namespace and ESO CRD before applying anything.

Do not use `--apply` if the application Helm release already owns these two
resources. The script checks the existing `app.kubernetes.io/managed-by` label
and refuses to overwrite Helm-owned objects. The Helm chart has its own
equivalent `externalSecrets` values.

## Current architecture boundary

These scripts automate the current project design; they do not solve two
follow-up architecture change:

- the API connection string is still built from the RDS master credential;

For production, introduce a lower-privilege application database role. Database
migration and initial-admin bootstrap already run in the one-shot migration Job;
the long-running API Deployment does not import the initial-admin values.
