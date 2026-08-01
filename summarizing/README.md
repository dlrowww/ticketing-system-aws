# 项目知识笔记

这里存放按领域整理的可视化学习笔记。

## 主题导航

- [AWS 知识地图](./aws/README.md)

## 目录约定

```text
summarizing/
├── README.md                 # 全部笔记的总入口
└── aws/
    ├── README.md             # AWS 主题入口
    ├── iam-role/             # IAM Role 专题
    ├── eks/                  # 以后可添加：EKS 专题
    ├── vpc/                  # 以后可添加：VPC 专题
    └── security-group/       # 以后可添加：安全组专题
```

新增知识时，先判断它是已有主题的后续内容，还是一个新的同级主题：

- IAM Role 的 Trust Policy、Pod Role 等内容放进 `aws/iam-role/`。
- EKS、VPC 等独立知识放进 `aws/` 下各自的目录。

