# ============================================================
# RDS PostgreSQL
#
# The VPC module already creates the DB subnet group from
# module.vpc.database_subnets. RDS reuses that group here.
# ============================================================

resource "aws_security_group" "rds_postgresql" {
  name_prefix = "${local.name}-rds-postgresql-"
  description = "Allow PostgreSQL only from EKS private subnet workloads"
  vpc_id      = module.vpc.vpc_id

  ingress {
    description = "PostgreSQL from EKS nodes and VPC-CNI pod IPs"
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = var.vpc_private_subnets
  }

  egress {
    description = "Allow response and service traffic"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(
    local.common_tags,
    {
      Name = "${local.name}-rds-postgresql-sg"
    }
  )

  lifecycle {
    create_before_destroy = true
  }
}

resource "aws_db_instance" "ticketing" {
  identifier = "${local.name}-postgresql"

  engine         = "postgres"
  engine_version = var.db_engine_version
  instance_class = var.db_instance_class

  db_name  = var.db_name
  username = var.db_master_username
  port     = 5432

  # RDS generates the password and stores/rotates it in Secrets Manager.
  # No database password is committed to Git or passed as a Terraform variable.
  manage_master_user_password = true

  allocated_storage     = var.db_allocated_storage
  max_allocated_storage = var.db_max_allocated_storage
  storage_type          = "gp3"
  storage_encrypted     = true

  db_subnet_group_name   = module.vpc.database_subnet_group_name
  vpc_security_group_ids = [aws_security_group.rds_postgresql.id]
  publicly_accessible    = false
  multi_az               = var.db_multi_az

  backup_retention_period = var.db_backup_retention_days
  backup_window           = "02:00-03:00"
  maintenance_window      = "sun:03:30-sun:04:30"
  copy_tags_to_snapshot   = true

  auto_minor_version_upgrade = true
  apply_immediately          = var.db_apply_immediately
  deletion_protection        = var.db_deletion_protection
  skip_final_snapshot        = var.db_skip_final_snapshot
  final_snapshot_identifier  = var.db_skip_final_snapshot ? null : "${local.name}-postgresql-final"

  enabled_cloudwatch_logs_exports = ["postgresql", "upgrade"]

  tags = merge(
    local.common_tags,
    {
      Name = "${local.name}-postgresql"
    }
  )
}

