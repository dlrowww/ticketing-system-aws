terraform {
  required_version = ">= 1.15.0, < 2.0.0"

  backend "s3" {
    bucket       = "simon-terraform-state-2026"
    key          = "ticketing-system/dev/terraform.tfstate"
    region       = "us-east-1"
    encrypt      = true
    use_lockfile = true
  }

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.54.0"
    }

    helm = {
      source  = "hashicorp/helm"
      version = "~> 3.2.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}
