# AWS 架构可视化笔记

> **AWS 架构不只是服务器列表，而是：组件 + 放置位置 + 通信规则 + 身份权限 + 数据存储 + 流量入口 + 监控运维。**

返回：[AWS 知识地图](../README.md)

## 1. AWS 架构的基本组成

以当前 EKS 项目为例，可以先问四个核心问题：

1. 有哪些组件？
2. 组件放在哪里？
3. 组件之间怎么通信？
4. 每个组件分别有权做什么？

再补充三个运行问题：数据保存在哪里、用户怎样访问、系统如何监控。

| 要思考的问题 | AWS 中对应的内容 |
|---|---|
| 有哪些组件？ | EKS、EC2、RDS、ALB、ECR、S3 |
| 组件放在哪里？ | Region、Availability Zone、VPC、Subnet |
| 组件之间能否通信？ | Route Table、Security Group、NACL |
| 组件能否调用 AWS API？ | IAM Role、IAM Policy |
| 数据保存在哪里？ | RDS、EBS、S3 |
| 用户怎样访问？ | Route 53、ALB、Ingress |
| 系统如何监控？ | CloudWatch、Prometheus、日志与告警 |

## 2. 一张图看懂整体架构

```mermaid
flowchart TB
    USER["用户 / 浏览器"]
    DNS["Route 53<br/>域名解析"]
    ALB["Application Load Balancer<br/>公网流量入口"]
    ECR["Amazon ECR<br/>容器镜像"]
    S3["Amazon S3<br/>对象/附件"]
    CW["CloudWatch / Prometheus<br/>日志、指标、告警"]

    subgraph REGION["AWS Region"]
        subgraph VPC["VPC：项目的私有网络边界"]
            subgraph PUB["Public Subnets（跨 AZ）"]
                ALB
                NAT["NAT Gateway<br/>私网资源访问公网"]
            end

            subgraph PRIVATE["Private Subnets（跨 AZ）"]
                EKS["EKS Control Plane 入口"]
                NODES["EC2 Worker Nodes<br/>EKS Node Groups"]
                INGRESS["Ingress Controller"]
                PODS["Ticketing System Pods"]
            end

            subgraph DATA["Database Subnets（跨 AZ）"]
                RDS["Amazon RDS<br/>业务数据库"]
            end
        end
    end

    USER --> DNS --> ALB
    ALB --> INGRESS --> PODS
    EKS -->|"调度"| NODES
    NODES --> PODS
    NODES -->|"拉取镜像"| ECR
    PODS -->|"读写业务数据"| RDS
    PODS -.->|"按需上传附件"| S3
    NODES --> NAT
    PODS -.-> CW
    NODES -.-> CW

    classDef external fill:#f8fafc,stroke:#64748b,color:#334155;
    classDef compute fill:#dbeafe,stroke:#2563eb,color:#1e3a8a;
    classDef data fill:#dcfce7,stroke:#16a34a,color:#14532d;
    classDef entry fill:#fef3c7,stroke:#d97706,color:#78350f;
    class USER,DNS,ECR,S3,CW external;
    class EKS,NODES,INGRESS,PODS compute;
    class RDS data;
    class ALB,NAT entry;
```

这是一张概念图，用于理解各组件职责；最终部署以 Terraform 实际配置为准。

## 3. 组件：系统由什么组成？

```mermaid
mindmap
  root((AWS 架构))
    计算
      EKS
      EC2 Worker Nodes
      Pods
    网络
      VPC
      Subnet
      Route Table
      Security Group
      NACL
    入口
      Route 53
      ALB
      Ingress
    存储
      RDS
      EBS
      S3
    镜像
      ECR
    权限
      IAM Role
      IAM Policy
    可观测性
      CloudWatch
      Prometheus
      Logs
      Alerts
```

### 计算组件

| 组件 | 通俗理解 | 在项目中的作用 |
|---|---|---|
| EKS | Kubernetes 托管服务 | 提供和管理集群控制面 |
| EC2 Worker Node | 真正干活的服务器 | 承载和运行 Pod |
| Pod | 应用运行单元 | 运行前端、后端等容器 |

### 数据与镜像

| 组件 | 适合保存什么 |
|---|---|
| RDS | 关系型业务数据，例如用户、工单、评论 |
| EBS | EC2 或 Kubernetes 有状态工作负载使用的块存储 |
| S3 | 文件、附件、备份等对象数据 |
| ECR | Docker/OCI 容器镜像 |

## 4. 位置：组件放在哪里？

```mermaid
flowchart TB
    REGION["Region<br/>例如 eu-central-1"]
    AZA["Availability Zone A"]
    AZB["Availability Zone B"]
    VPC["VPC"]
    PUBA["Public Subnet A"]
    PUBB["Public Subnet B"]
    PRIA["Private Subnet A"]
    PRIB["Private Subnet B"]
    DBA["Database Subnet A"]
    DBB["Database Subnet B"]

    REGION --> AZA
    REGION --> AZB
    REGION --> VPC
    VPC --> PUBA
    VPC --> PUBB
    VPC --> PRIA
    VPC --> PRIB
    VPC --> DBA
    VPC --> DBB

    AZA -.-> PUBA
    AZA -.-> PRIA
    AZA -.-> DBA
    AZB -.-> PUBB
    AZB -.-> PRIB
    AZB -.-> DBB
```

最短记忆：

```text
Region  = 一个地理区域
AZ      = Region 内相互隔离的数据中心区域
VPC     = 你的 AWS 私有网络
Subnet  = VPC 中按用途和 AZ 划分的网段
```

通常把互联网入口放在 Public Subnet，把 Worker Node 放在 Private Subnet，把 RDS 放在 Database Subnet，并跨多个 AZ 提高可用性。

## 5. 通信：组件之间能不能互相到达？

网络通信需要分层判断：

```mermaid
flowchart LR
    SRC["来源组件"]
    ROUTE{"Route Table<br/>有没有路径？"}
    NACL{"NACL<br/>子网边界允许吗？"}
    SG{"Security Group<br/>资源端口允许吗？"}
    DEST["目标组件"]

    SRC --> ROUTE
    ROUTE -->|"有"| NACL
    NACL -->|"允许"| SG
    SG -->|"允许"| DEST
```

| 控制项 | 通俗理解 | 主要作用层级 |
|---|---|---|
| Route Table | 道路指示牌 | 决定流量往哪里走 |
| NACL | 子网门卫 | 控制整个 Subnet 的进出流量 |
| Security Group | 资源门卫 | 控制 ALB、EC2、RDS 等资源的进出流量 |

IAM Role 不负责判断 HTTP 或数据库连接能不能通过。IAM Role 管的是 AWS API 权限，详见 [IAM 权限边界](../iam-role/access-boundaries.md)。

## 6. 权限：组件可以调用哪些 AWS API？

```mermaid
flowchart LR
    EKS["EKS Control Plane"] --> CR["Cluster IAM Role"]
    NODE["EC2 Worker Node"] --> NR["Node IAM Role"]
    POD["业务 Pod"] --> PR["Pod IAM Role"]
    CI["Terraform / GitHub Actions"] --> DR["Deployment IAM Role"]

    CR --> API["AWS API"]
    NR --> API
    PR --> API
    DR --> API

    classDef role fill:#fef3c7,stroke:#d97706,color:#78350f;
    class CR,NR,PR,DR role;
```

不同组件应该使用不同 Role，因为它们需要的权限不同。详细说明见 [IAM Role 可视化笔记](../iam-role/README.md)。

## 7. 用户请求怎样进入系统？

```mermaid
sequenceDiagram
    participant U as 用户
    participant DNS as Route 53 / DNS
    participant ALB as ALB
    participant ING as Ingress
    participant SVC as Kubernetes Service
    participant POD as Ticketing Pod
    participant DB as RDS

    U->>DNS: 查询系统域名
    DNS-->>U: 返回 ALB 地址
    U->>ALB: HTTPS 请求
    ALB->>ING: 按监听规则转发
    ING->>SVC: 按 Host/Path 路由
    SVC->>POD: 负载均衡到 Pod
    POD->>DB: 查询或写入数据
    DB-->>POD: 返回结果
    POD-->>U: 经原路径返回响应
```

要让这条链路工作，DNS、监听器、Ingress 规则、Service selector、Pod 端口和各层 Security Group 都需要正确配置。

## 8. 系统如何被观察和维护？

```mermaid
flowchart LR
    APP["应用与基础设施"]
    LOG["Logs<br/>发生了什么？"]
    METRIC["Metrics<br/>状态和趋势怎样？"]
    ALERT["Alerts<br/>什么时候需要处理？"]
    DASH["Dashboard<br/>集中查看"]

    APP --> LOG --> DASH
    APP --> METRIC --> DASH
    METRIC --> ALERT
    LOG --> ALERT
```

| 类型 | 示例 |
|---|---|
| 日志 | 应用日志、ALB 访问日志、控制面日志 |
| 指标 | CPU、内存、请求量、错误率、延迟 |
| 告警 | Pod 不健康、5xx 增多、数据库空间不足 |
| 工具 | CloudWatch、Prometheus、Grafana 等 |

## 9. 一页速记

```text
AWS 架构
= 计算组件
+ 网络结构
+ 身份权限
+ 数据存储
+ 流量入口
+ 监控运维

组件是什么？       → EKS / EC2 / RDS / ALB / ECR / S3
组件放在哪里？     → Region / AZ / VPC / Subnet
组件能否通信？     → Route Table / NACL / Security Group
组件能调 AWS API？ → IAM Role / IAM Policy
数据放在哪里？     → RDS / EBS / S3
用户怎么访问？     → Route 53 / ALB / Ingress
系统怎么观察？     → Logs / Metrics / Alerts
```

## 10. 后续内容入口

内容增长后，可以继续在当前目录添加：

| 后续专题 | 建议文件名 | 状态 |
|---|---|---|
| Region、AZ 与高可用 | `region-and-az.md` | 待添加 |
| VPC 与三类 Subnet | `vpc-and-subnets.md` | 待添加 |
| 用户请求完整链路 | `request-flow.md` | 待添加 |
| 数据与备份架构 | `data-and-backup.md` | 待添加 |
| 日志、指标与告警 | `observability.md` | 待添加 |
