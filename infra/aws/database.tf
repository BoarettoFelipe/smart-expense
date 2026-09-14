data "aws_rds_engine_version" "postgres" {
  engine  = "postgres"
  version = var.db_engine_version
  latest  = true
}

resource "aws_db_subnet_group" "demo" {
  name       = local.name
  subnet_ids = aws_subnet.public[*].id
  tags       = { Name = local.name }
}

resource "aws_db_instance" "demo" {
  identifier                  = local.name
  engine                      = "postgres"
  engine_version              = data.aws_rds_engine_version.postgres.version_actual
  instance_class              = var.db_instance_class
  db_name                     = var.db_name
  username                    = var.db_username
  password                    = var.db_password
  port                        = 5432
  allocated_storage           = 20
  storage_type                = "gp3"
  storage_encrypted           = true
  db_subnet_group_name        = aws_db_subnet_group.demo.name
  vpc_security_group_ids      = [aws_security_group.rds.id]
  publicly_accessible         = false
  multi_az                    = false
  deletion_protection         = false
  skip_final_snapshot         = true
  delete_automated_backups    = true
  backup_retention_period     = 0
  auto_minor_version_upgrade  = true
  allow_major_version_upgrade = false
  engine_lifecycle_support    = "open-source-rds-extended-support-disabled"
  tags                        = { Name = local.name }
}

resource "aws_ssm_parameter" "db_password" {
  name        = "/smart-expense/demo/db-password"
  description = "Demo database password"
  type        = "SecureString"
  tier        = "Standard"
  value       = var.db_password
}

resource "aws_ssm_parameter" "jwt_signing_key" {
  name        = "/smart-expense/demo/jwt-signing-key"
  description = "Demo JWT signing key"
  type        = "SecureString"
  tier        = "Standard"
  value       = var.jwt_signing_key
}
