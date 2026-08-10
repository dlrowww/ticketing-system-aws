# Generated from Terraform outputs by devops_scripts/render-external-secrets.sh.
# This file contains identifiers, not Secret values. Do not commit rendered
# environment-specific manifests.
apiVersion: external-secrets.io/v1
kind: SecretStore
metadata:
  name: ticketing-system-aws-secrets-manager
  namespace: "${K8S_NAMESPACE}"
spec:
  provider:
    aws:
      service: SecretsManager
      region: "${AWS_REGION}"
---
apiVersion: external-secrets.io/v1
kind: ExternalSecret
metadata:
  name: ticketing-system-runtime
  namespace: "${K8S_NAMESPACE}"
spec:
  refreshPolicy: Periodic
  refreshInterval: 1h
  secretStoreRef:
    name: ticketing-system-aws-secrets-manager
    kind: SecretStore
  target:
    name: ticketing-system-runtime
    creationPolicy: Owner
    deletionPolicy: Retain
    template:
      engineVersion: v2
      type: Opaque
      data:
        connection-string: 'Host=${RDS_ADDRESS};Port=${RDS_PORT};Database=${DATABASE_NAME};Username={{ .dbUsername }};Password="{{ .dbPassword }}";SSL Mode=Require'
        jwt-key: '{{ .jwtKey }}'
        smtp-username: '{{ .smtpUsername }}'
        smtp-password: '{{ .smtpPassword }}'
        admin-email: '{{ .adminEmail }}'
        admin-password: '{{ .adminPassword }}'
  data:
    - secretKey: dbUsername
      remoteRef:
        key: "${RDS_MASTER_SECRET_ARN}"
        property: username
    - secretKey: dbPassword
      remoteRef:
        key: "${RDS_MASTER_SECRET_ARN}"
        property: password
    - secretKey: jwtKey
      remoteRef:
        key: "${JWT_SECRET_NAME}"
        property: key
    - secretKey: smtpUsername
      remoteRef:
        key: "${SMTP_SECRET_NAME}"
        property: username
    - secretKey: smtpPassword
      remoteRef:
        key: "${SMTP_SECRET_NAME}"
        property: password
    - secretKey: adminEmail
      remoteRef:
        key: "${INITIAL_ADMIN_SECRET_NAME}"
        property: email
    - secretKey: adminPassword
      remoteRef:
        key: "${INITIAL_ADMIN_SECRET_NAME}"
        property: password
