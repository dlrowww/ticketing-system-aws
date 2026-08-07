# Helm Chart 对应的普通 Kubernetes YAML

这里不使用 Helm 模板语法。所有 `{{ .Values... }}` 都已经替换成具体示例值，方便与
`example/ticketing-system` 对照学习。

## 对照关系

| Helm Chart | 普通 Kubernetes YAML |
|---|---|
| `templates/namespace.yaml` | `namespace.yaml` |
| `templates/configmap.yaml` | `configmap.yaml` |
| `templates/external-secrets.yaml` | `external-secrets.yaml` |
| `templates/frontend/deployment.yaml` | `frontend/deployment.yaml` |
| `templates/frontend/service.yaml` | `frontend/service.yaml` |
| `templates/frontend/hpa.yaml` | `frontend/hpa.yaml` |
| `templates/frontend/pdb.yaml` | `frontend/pdb.yaml` |
| `templates/api/deployment.yaml` | `api/deployment.yaml` |
| 独立数据库迁移资源 | `api/migration-job.yaml` |
| `templates/api/service.yaml` | `api/service.yaml` |
| `templates/api/serviceaccount.yaml` | `api/serviceaccount.yaml` |
| `templates/api/hpa.yaml` | `api/hpa.yaml` |
| `templates/api/pdb.yaml` | `api/pdb.yaml` |
| `templates/ingress.yaml` | `ingress.yaml` |
| `templates/tests/*` | `tests/*` |
| `values.yaml` | 具体值已经写进各 YAML |

`secret.example.yaml` 是不使用 External Secrets Operator 时的本地/教学备用方案。
AWS/EKS 部署推荐使用 `external-secrets.yaml`，由 Terraform 安装的 Operator 自动生成
`ticketing-system-runtime`。

## 资源关系

```text
Namespace: ticketing-system
│
├── ALB Ingress
│   ├── /api → Service/api:8080
│   │              ↓ selector: component=api
│   │         API Deployment → API Pod:8080
│   │
│   └── / → Service/ticketing-system-frontend:80
│                    ↓ selector: component=frontend
│              Frontend Deployment → Frontend Pod:3000
│
├── ConfigMaps → 非敏感环境变量
├── ExternalSecret
│   └── AWS Secrets Manager → ticketing-system-runtime
└── API Migration Job
    └── EF Migration + 初始管理员 + 可选 Demo Seed
```

## 先替换示例值

正式部署前至少修改：

- `frontend/deployment.yaml` 中的前端 ECR 镜像；
- `api/deployment.yaml` 和 `api/migration-job.yaml` 中的后端 ECR 镜像，并确保二者使用
  完全相同的不可变 tag；
- 如果不使用自动化脚本，修改 `external-secrets.yaml` 中四个 `REPLACE_WITH_*` 引用；
- `ingress.yaml` 中的域名和 ACM ARN；
- `configmap.yaml` 中的域名、邮件地址。

不要把包含真实密码的 Secret YAML 提交到 Git。

## 基础部署顺序

先确保 Terraform 已安装 External Secrets Operator，并且 AWS Secrets Manager 的三个
应用 Secret 已填入值。然后依次部署：

推荐使用仓库脚本自动读取 Terraform outputs、初始化 Secret 值并渲染/应用清单：

```bash
# Infrastructure workflow 的 apply 已负责创建 Namespace。
./devops_scripts/bootstrap-secrets.sh
./devops_scripts/render-external-secrets.sh --apply
```

`Namespace` 是集群级资源。Application/Kubernetes GitHub Role 只管理
`ticketing-system` Namespace 内部的资源，`deploy-k8s.sh` 不会创建或修改 Namespace。
如果不使用 Infrastructure workflow，应由集群管理员预先执行：

```bash
kubectl apply -f k8s_deploy/namespace.yaml
```

如果不用脚本，才需要手动替换 `external-secrets.yaml` 中的占位符。

```bash
kubectl apply -f k8s_deploy/configmap.yaml
kubectl apply -f k8s_deploy/api/serviceaccount.yaml
kubectl get secret ticketing-system-runtime -n ticketing-system

# Job 名称固定；每次发布新镜像前删除上一次已完成的 Job，再重新创建。
kubectl delete -f k8s_deploy/api/migration-job.yaml --ignore-not-found
kubectl apply -f k8s_deploy/api/migration-job.yaml
kubectl wait --for=condition=complete \
  job/ticketing-system-api-migration \
  -n ticketing-system \
  --timeout=15m
kubectl logs -n ticketing-system job/ticketing-system-api-migration

# 只有 Migration Job 成功后才更新 API Deployment。
kubectl apply -f k8s_deploy/api/deployment.yaml
kubectl apply -f k8s_deploy/api/service.yaml
kubectl apply -f k8s_deploy/frontend/deployment.yaml
kubectl apply -f k8s_deploy/frontend/service.yaml
kubectl apply -f k8s_deploy/ingress.yaml
```

手动模式才执行 `kubectl apply -f k8s_deploy/external-secrets.yaml`；使用脚本时不要重复
应用这个占位符版本。

如果只做本地教学且不使用 AWS，可以继续复制 `secret.example.yaml` 到临时文件并手动
创建 Secret，但不要同时应用手动 Secret 与 ExternalSecret。

检查：

```bash
kubectl get deploy,pod,svc,ingress -n ticketing-system
kubectl get endpoints -n ticketing-system
```

`migration-job.yaml` 给 API 镜像传入 `--migrate-only`。该模式执行所有待处理 EF
Migration，然后完成一次性管理员创建和可选 Demo Seed，成功后以退出码 `0` 结束。普通
API Pod 不再执行这些数据库初始化操作。如果 Job 失败，不要继续发布 Deployment；先查看
Job 日志并修复数据库连接或迁移错误。

不要对整个 `k8s_deploy` 目录执行递归 apply；普通 Kubernetes YAML 不提供 Job 与
Deployment 之间的依赖关系，递归 apply 可能让新 API Pod 在迁移完成前启动。请保持上面的
`apply Job → wait complete → apply Deployment` 顺序。

## 可选资源

HPA 和 PDB 没有包含在基础部署命令中：

```bash
kubectl apply -f k8s_deploy/frontend/hpa.yaml
kubectl apply -f k8s_deploy/frontend/pdb.yaml
kubectl apply -f k8s_deploy/api/hpa.yaml
kubectl apply -f k8s_deploy/api/pdb.yaml
```

API Deployment 默认运行两个副本。启用 API HPA/PDB 前，应先确认 Migration Job 成功、
两个 API Pod 均为 Ready，并确认集群已安装 Metrics Server。

## 测试

基础资源正常后，可以分别运行：

```bash
kubectl apply -f k8s_deploy/tests/frontend-connection.yaml
kubectl apply -f k8s_deploy/tests/api-connection.yaml
kubectl logs -n ticketing-system pod/ticketing-system-frontend-test
kubectl logs -n ticketing-system pod/ticketing-system-api-test
```

测试结束后：

```bash
kubectl delete -f k8s_deploy/tests/frontend-connection.yaml --ignore-not-found
kubectl delete -f k8s_deploy/tests/api-connection.yaml --ignore-not-found
```

## Helm 与普通 YAML 的核心差别

普通 YAML 直接写死：

```yaml
replicas: 1
image: ticketing-backend:1.0.0
```

Helm 模板使用变量：

```yaml
replicas: {{ .Values.api.replicaCount }}
image: {{ include "ticketing-system.image" (list . .Values.api.image) }}
```

Helm 最终还是把模板渲染成类似本目录的普通 Kubernetes YAML，再提交给 Kubernetes。
