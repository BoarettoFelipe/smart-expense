output "aws_region" {
  description = "Region containing the demo."
  value       = var.aws_region
}

output "ec2_instance_id" {
  description = "SSM Run Command deployment target."
  value       = aws_instance.app.id
}

output "ec2_public_ip" {
  description = "Temporary public IPv4; it can change after stop/start or replacement."
  value       = aws_instance.app.public_ip
}

output "application_url" {
  description = "Future demo URL; serves nothing until the later deployment binds frontend to host port 80."
  value       = "http://${aws_instance.app.public_ip}"
}

output "api_ecr_repository_url" {
  value = aws_ecr_repository.app["api"].repository_url
}

output "frontend_ecr_repository_url" {
  value = aws_ecr_repository.app["frontend"].repository_url
}

output "github_deploy_role_arn" {
  value = aws_iam_role.github_deploy.arn
}

output "rds_endpoint" {
  description = "Private database hostname and port; reachable only from the demo EC2 security group."
  value       = aws_db_instance.demo.endpoint
}

output "database_name" {
  value = aws_db_instance.demo.db_name
}

output "database_username" {
  value = var.db_username
}

output "ssm_parameter_names" {
  description = "Names only; retrieve values on EC2 using GetParameter(s) with decryption."
  value = {
    db_password     = aws_ssm_parameter.db_password.name
    jwt_signing_key = aws_ssm_parameter.jwt_signing_key.name
  }
}
