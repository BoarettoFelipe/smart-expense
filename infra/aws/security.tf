resource "aws_security_group" "ec2" {
  name_prefix = "${local.name}-ec2-"
  description = "HTTP demo access; administration uses SSM instead of SSH."
  vpc_id      = aws_vpc.demo.id
  tags        = { Name = "${local.name}-ec2" }
}

resource "aws_vpc_security_group_ingress_rule" "http" {
  security_group_id = aws_security_group.ec2.id
  description       = "Public demo frontend"
  cidr_ipv4         = "0.0.0.0/0"
  from_port         = 80
  to_port           = 80
  ip_protocol       = "tcp"
}

resource "aws_vpc_security_group_egress_rule" "ec2" {
  security_group_id = aws_security_group.ec2.id
  description       = "Package installation, ECR, SSM HTTPS, DNS, and RDS"
  cidr_ipv4         = "0.0.0.0/0"
  ip_protocol       = "-1"
}

resource "aws_security_group" "rds" {
  name_prefix = "${local.name}-rds-"
  description = "PostgreSQL reachable only from the demo EC2 security group."
  vpc_id      = aws_vpc.demo.id
  tags        = { Name = "${local.name}-rds" }
}

resource "aws_vpc_security_group_ingress_rule" "postgres" {
  security_group_id            = aws_security_group.rds.id
  referenced_security_group_id = aws_security_group.ec2.id
  description                  = "EC2 to RDS only"
  from_port                    = 5432
  to_port                      = 5432
  ip_protocol                  = "tcp"
}
