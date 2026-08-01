# Ticketing System Helm Chart

This chart deploys the real frontend and API project components:

```text
AWS ALB Ingress
├── /api → API Service → ASP.NET Core API Pods :8080
└── /    → Frontend Service → SvelteKit Pods :3000

API Pods → private RDS PostgreSQL :5432
```

Templates are grouped by component:

```text
templates/
├── _helpers.tpl
├── namespace.yaml              # optional; disabled by default
├── configmap.yaml              # frontend and API non-secret configuration
├── ingress.yaml
├── frontend/
│   ├── deployment.yaml
│   ├── service.yaml
│   ├── hpa.yaml
│   └── pdb.yaml
├── api/
│   ├── deployment.yaml
│   ├── service.yaml
│   ├── serviceaccount.yaml
│   ├── hpa.yaml
│   └── pdb.yaml
└── tests/
    ├── frontend-connection.yaml
    └── api-connection.yaml
```

`namespace.create` defaults to `false`. The recommended installation command
uses `--namespace ticketing-system --create-namespace`, so uninstalling the
release does not make Helm delete a shared Namespace. Set
`--set namespace.create=true` only when the release should own it.

## Before installation

Build and push the two project images to the ECR repositories created by
Terraform. Override both image repositories and immutable tags.

Create or synchronize a Kubernetes Secret named `ticketing-system-runtime`
with these keys:

```text
connection-string   Required Npgsql connection string
jwt-key             Required; same value is used by API and frontend
smtp-username       Optional
smtp-password       Optional
admin-email         Optional
admin-password      Optional
```

The AWS Secrets Manager resources alone do not automatically become Kubernetes
environment variables. Use External Secrets/Secrets Store CSI Driver, or create
the Kubernetes Secret through an approved deployment process.

Do not commit real secret values to this repository.

## Current frontend image constraint

The frontend currently imports `JWT_SECRET`, `BACKEND_URL`, and `LOOKUPS_API`
from SvelteKit `$env/static/private`, while its Dockerfile supplies them during
the image build. Those values are compiled into the image and cannot reliably
be replaced by Deployment runtime environment variables.

For compatibility, the API Service defaults to the fixed name `api`, matching
the currently compiled `http://api:8080` backend URL. Before production:

1. change private runtime settings to `$env/dynamic/private`;
2. remove the development JWT value from the frontend Dockerfile;
3. rebuild the frontend image;
4. then runtime Secret/ConfigMap values in this chart become authoritative.

## Render locally

```bash
helm lint example/ticketing-system

helm template ticketing-system example/ticketing-system \
  --namespace ticketing-system \
  --set frontend.image.repository=<frontend-ecr-url> \
  --set frontend.image.tag=<immutable-tag> \
  --set api.image.repository=<backend-ecr-url> \
  --set api.image.tag=<immutable-tag>
```

## Install

```bash
helm upgrade --install ticketing-system example/ticketing-system \
  --namespace ticketing-system \
  --create-namespace \
  -f example/ticketing-system/values-prod.yaml \
  --set global.domain=<real-domain> \
  --set frontend.config.origin=https://<real-domain> \
  --set api.config.corsAllowedOrigin=https://<real-domain> \
  --set api.config.emailBaseUrl=https://<real-domain> \
  --set frontend.image.repository=<frontend-ecr-url> \
  --set frontend.image.tag=<immutable-tag> \
  --set api.image.repository=<backend-ecr-url> \
  --set api.image.tag=<immutable-tag> \
  --set ingress.certificateArn=<terraform-acm-certificate-arn>
```

## Current scaling constraint

`Program.cs` calls `Database.MigrateAsync()` during API startup. Therefore the
chart intentionally defaults to one API replica with API HPA disabled. Before
scaling the API, add a migration-only application command, run it as a
pre-deployment Job, and remove migrations from normal web-process startup.

The current `/health` endpoint includes the database check. It is used for all
API probes for now. A later application change should expose separate live and
ready endpoints so a temporary database outage does not cause liveness restarts.
