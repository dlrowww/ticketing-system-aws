# DevOps helper scripts

These scripts bootstrap the AWS Secrets Manager values and generate the raw
Kubernetes `SecretStore`/`ExternalSecret` manifest without manually copying
Terraform outputs.

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
follow-up architecture changes:

- the API connection string is still built from the RDS master credential;
- initial-admin credentials are still exposed to the long-running API Pod.

For production, introduce a lower-privilege application database role and move
database migration/admin bootstrap into one-shot Jobs. After that, remove the
RDS master and initial-admin values from the API runtime Secret.
