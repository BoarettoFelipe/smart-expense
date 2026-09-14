provider "aws" {
  region = var.aws_region

  default_tags {
    tags = merge(var.common_tags, {
      Project     = "SmartExpense"
      Environment = "demo"
      ManagedBy   = "Terraform"
    })
  }
}

data "aws_partition" "current" {}

locals {
  name = "smart-expense-demo"
}
