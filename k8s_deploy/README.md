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
└── ExternalSecret
    └── AWS Secrets Manager → ticketing-system-runtime
```

## 先替换示例值

正式部署前至少修改：

- `frontend/deployment.yaml` 中的前端 ECR 镜像；
- `api/deployment.yaml` 中的后端 ECR 镜像；
- 如果不使用自动化脚本，修改 `external-secrets.yaml` 中四个 `REPLACE_WITH_*` 引用；
- `ingress.yaml` 中的域名和 ACM ARN；
- `configmap.yaml` 中的域名、邮件地址。

不要把包含真实密码的 Secret YAML 提交到 Git。

## 基础部署顺序

先确保 Terraform 已安装 External Secrets Operator，并且 AWS Secrets Manager 的三个
应用 Secret 已填入值。然后依次部署：

推荐使用仓库脚本自动读取 Terraform outputs、初始化 Secret 值并渲染/应用清单：

```bash
kubectl apply -f k8s_deploy/namespace.yaml
./devops_scripts/bootstrap-secrets.sh
./devops_scripts/render-external-secrets.sh --apply
```

如果不用脚本，才需要手动替换 `external-secrets.yaml` 中的占位符。

```bash
kubectl apply -f k8s_deploy/configmap.yaml
kubectl apply -f k8s_deploy/api/serviceaccount.yaml
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

## 可选资源

HPA 和 PDB 没有包含在基础部署命令中：

```bash
kubectl apply -f k8s_deploy/frontend/hpa.yaml
kubectl apply -f k8s_deploy/frontend/pdb.yaml
```

API 当前会在每个进程启动时执行 EF Migration，所以暂时不要部署 API HPA，也不要把
API 扩展为多个副本。`api/hpa.yaml` 和 `api/pdb.yaml` 仅用于和 Helm 模板对照。

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
