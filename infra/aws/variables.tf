variable "aws_region" {
  description = "AWS region for the temporary demo."
  type        = string
  default     = "us-east-1"
}

variable "common_tags" {
  description = "Additional tags; the required project/environment/management tags take precedence."
  type        = map(string)
  default     = {}
}

variable "ec2_instance_type" {
  description = "Small x86_64 instance compatible with the AL2023 AMI; increase RAM if the demo needs it."
  type        = string
  default     = "t3.micro"
}

variable "docker_compose_version" {
  description = "Pinned official Docker Compose release installed by bootstrap."
  type        = string
  default     = "v5.5.1"

  validation {
    condition     = can(regex("^v[0-9]+\\.[0-9]+\\.[0-9]+$", var.docker_compose_version))
    error_message = "Use a stable version such as v5.5.1."
  }
}

variable "db_instance_class" {
  description = "Single-AZ PostgreSQL instance class; verify orderability in the chosen region before applying."
  type        = string
  default     = "db.t3.micro"
}

variable "db_engine_version" {
  description = "Available PostgreSQL major or full version; resolves the latest matching minor at plan time."
  type        = string
  default     = "18"
}

variable "db_name" {
  description = "Initial RDS database name (RDS requires alphanumeric characters)."
  type        = string
  default     = "smartexpense"

  validation {
    condition     = can(regex("^[A-Za-z][A-Za-z0-9]{0,62}$", var.db_name))
    error_message = "Database name must start with a letter and contain at most 63 alphanumeric characters."
  }
}

variable "db_username" {
  description = "RDS master username for the disposable demo; avoid PostgreSQL reserved names."
  type        = string
  default     = "smartexpense"

  validation {
    condition     = can(regex("^[A-Za-z][A-Za-z0-9]{0,15}$", var.db_username))
    error_message = "Username must start with a letter and contain at most 16 alphanumeric characters."
  }
}

variable "db_password" {
  description = "Required RDS password, supplied locally; also stored as an SSM SecureString. Present in Terraform state."
  type        = string
  sensitive   = true

  validation {
    condition     = can(regex("^[!-~]{8,128}$", var.db_password)) && length(regexall("[/@\"]", var.db_password)) == 0
    error_message = "RDS password must be 8-128 printable ASCII characters without spaces, /, @, or double quotes."
  }
}

variable "jwt_signing_key" {
  description = "Required JWT signing key, at least 32 ASCII characters; stored as an SSM SecureString. Present in Terraform state."
  type        = string
  sensitive   = true

  validation {
    condition     = can(regex("^[!-~]+$", var.jwt_signing_key)) && length(var.jwt_signing_key) >= 32 && length(var.jwt_signing_key) <= 4096
    error_message = "Supply 32-4096 printable non-space ASCII characters for the signing key."
  }
}

variable "github_oidc_provider_arn" {
  description = "Existing token.actions.githubusercontent.com provider ARN in this account, or null to create a demo-owned provider."
  type        = string
  default     = null

  validation {
    condition     = var.github_oidc_provider_arn == null ? true : can(regex("^arn:[^:]+:iam::[0-9]{12}:oidc-provider/token\\.actions\\.githubusercontent\\.com$", var.github_oidc_provider_arn))
    error_message = "Use the ARN of this account's GitHub OIDC provider."
  }
}

variable "monthly_budget_usd" {
  description = "Account-wide monthly USD cost alert threshold; this is not a spending cap."
  type        = number
  default     = 5

  validation {
    condition     = var.monthly_budget_usd > 0
    error_message = "Budget must be greater than zero."
  }
}

variable "budget_alert_email" {
  description = "Optional notification address; omit to create a budget without email alerts."
  type        = string
  default     = null

  validation {
    condition     = var.budget_alert_email == null ? true : can(regex("^[^ @]+@[^ @]+\\.[^ @]+$", var.budget_alert_email))
    error_message = "Supply a valid email address or null."
  }
}
