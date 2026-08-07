# CI/CD

The repository uses three path-scoped GitHub Actions workflows:

| Changed path | Workflow | Responsibility |
|---|---|---|
| `aws/**` | `Infrastructure` | Automatic Terraform format/validate; manual plan/apply |
| `backend/**`, `frontend/**` | `Application CI and CD` | Automatic tests; manual immutable image build, migration and rollout |
| `k8s_deploy/**`, `devops_scripts/**` | `Kubernetes Deployment` | Automatic validation; manual Kubernetes-only rollout |

Both deployment workflows use `devops_scripts/deploy-k8s.sh`, so database
migrations always finish before the API Deployment is updated. Application CD
passes newly built commit-SHA images. Kubernetes CD reads and reuses the images
already running in the cluster. Re-running Application CD for the same commit
reuses the existing immutable ECR tags instead of trying to overwrite them.

## One-time GitHub configuration

Create these GitHub Environments:

- `dev-infrastructure`
- `dev`

Configure the following **Environment variables** (not long-lived AWS access
keys):

| Environment | Variable | Value |
|---|---|---|
| `dev-infrastructure` | `AWS_INFRASTRUCTURE_ROLE_ARN` | IAM role assumed by the Terraform workflow |
| `dev-infrastructure` | `AWS_REGION` | `us-east-1` |
| `dev-infrastructure` | `DEPLOY_ENVIRONMENT` | `dev` |
| `dev` | `AWS_APPLICATION_ROLE_ARN` | IAM role allowed to push ECR images and deploy to EKS |
| `dev` | `AWS_KUBERNETES_ROLE_ARN` | IAM role allowed to deploy manifests to EKS |
| `dev` | `AWS_REGION` | `us-east-1` |

Protect both environments with required reviewers if deployment approval is
needed. Protect `main` with the CI checks `Backend CI`, `Frontend CI`,
`Terraform validate`, and `Validate Kubernetes manifests and scripts`.
Pull requests never receive AWS credentials: infrastructure PRs run local
format/validate checks, while a real state-aware plan is an explicitly approved
`workflow_dispatch` operation.

All three jobs that change AWS or Kubernetes state currently run only through
`workflow_dispatch`. A merge to `main` still runs the corresponding validation,
but it cannot start Terraform apply or either deployment job. This prevents a
first merge from racing infrastructure creation, image deployment and a
Kubernetes-only rollout.

## AWS OIDC roles

Use GitHub's OIDC provider (`token.actions.githubusercontent.com`) instead of
AWS access-key secrets. Restrict the role trust policies with the audience
`sts.amazonaws.com` and these `sub` claims:

```text
repo:dlrowww/ticketing-system-aws:environment:dev-infrastructure
repo:dlrowww/ticketing-system-aws:environment:dev
```

The infrastructure role needs access to the Terraform S3 state and to manage
the AWS resources declared under `aws/`. Because Terraform also manages Helm
releases, this role must have EKS cluster access. It also applies
`k8s_deploy/namespace.yaml` after Terraform succeeds, so it owns creation of the
cluster-scoped `ticketing-system` Namespace.

The application role needs:

- read access to the Terraform state object;
- ECR push access to `ticketing-system-dev/ticketing-backend` and
  `ticketing-system-dev/ticketing-frontend`;
- `eks:DescribeCluster` and Kubernetes RBAC/EKS access for the
  `ticketing-system` namespace.

The Kubernetes role needs read access to the Terraform state object,
`eks:DescribeCluster`, and the same namespace deployment access. It does not
need ECR push access. Neither application role needs permission to create,
patch or delete Namespace objects.

For a first-time bootstrap, create the OIDC roles/EKS access entries using an
administrator identity, then add their ARNs to the GitHub Environments. No AWS
access key is stored in GitHub.

## First deployment

1. Manually run the Infrastructure workflow with `apply`. It creates the AWS
   resources and then ensures the `ticketing-system` Namespace exists.
2. Populate the JWT, SMTP and initial-admin Secrets Manager values locally with
   `devops_scripts/bootstrap-secrets.sh`. This interactive operation is never
   run by CI.
3. Run `Application CI and CD` once. It creates both ECR images and the initial
   Kubernetes Deployments.
4. After that, `Kubernetes Deployment` can safely reuse the currently deployed
   image references for manifest-only changes.

Keep deployments manual until this sequence has completed successfully. If
automatic CD is enabled later, retain path filters and a shared deployment
concurrency group, and do not make Terraform apply an unconditional `main`
push action.

If `route53_zone_id` and `application_domain_name` are configured, Terraform
outputs the validated ACM certificate and domain. The deployment script renders
and applies the Ingress automatically. Without both values, it intentionally
skips Ingress.
