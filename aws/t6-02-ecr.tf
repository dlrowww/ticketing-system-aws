# ============================================================
# ECR repositories for the two deployable application images
# ============================================================

locals {
  application_ecr_repositories = toset([
    "ticketing-backend",
    "ticketing-frontend",
  ])
}

resource "aws_ecr_repository" "application" {
  for_each = local.application_ecr_repositories

  name                 = "${local.name}/${each.value}"
  image_tag_mutability = var.ecr_image_tag_mutability
  force_delete         = local.effective_ecr_force_delete

  encryption_configuration {
    encryption_type = "AES256"
  }

  image_scanning_configuration {
    scan_on_push = true
  }

  tags = merge(
    local.common_tags,
    {
      Name      = "${local.name}/${each.value}"
      Component = each.value
    }
  )
}

resource "aws_ecr_lifecycle_policy" "application" {
  for_each = aws_ecr_repository.application

  repository = each.value.name
  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Remove untagged images after ${var.ecr_untagged_image_retention_days} days"
        selection = {
          tagStatus   = "untagged"
          countType   = "sinceImagePushed"
          countUnit   = "days"
          countNumber = var.ecr_untagged_image_retention_days
        }
        action = {
          type = "expire"
        }
      }
    ]
  })
}
