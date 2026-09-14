data "aws_ssm_parameter" "al2023_ami" {
  name = "/aws/service/ami-amazon-linux-latest/al2023-ami-kernel-default-x86_64"
}

resource "aws_instance" "app" {
  ami                         = nonsensitive(data.aws_ssm_parameter.al2023_ami.value)
  instance_type               = var.ec2_instance_type
  subnet_id                   = aws_subnet.public[0].id
  associate_public_ip_address = true
  vpc_security_group_ids      = [aws_security_group.ec2.id]
  iam_instance_profile        = aws_iam_instance_profile.ec2.name
  user_data_replace_on_change = true
  # Keep Linux user data valid even when checked out with Windows line endings.
  user_data = replace(templatefile("${path.module}/bootstrap.sh.tftpl", {
    docker_compose_version = var.docker_compose_version
  }), "\r\n", "\n")

  metadata_options {
    http_endpoint               = "enabled"
    http_tokens                 = "required"
    http_put_response_hop_limit = 1
  }

  root_block_device {
    volume_type           = "gp3"
    volume_size           = 12
    encrypted             = true
    delete_on_termination = true
    tags                  = { Name = "${local.name}-root" }
  }

  # Bound burst CPU cost; sustained load may throttle this deliberately small demo.
  credit_specification {
    cpu_credits = "standard"
  }

  depends_on = [
    aws_route.internet,
    aws_route_table_association.public,
    aws_iam_role_policy_attachment.ssm_core,
    aws_iam_role_policy.ec2,
  ]
  tags = { Name = local.name }
}
