terraform {
  required_version = ">= 1.0.0"
}

variable "project_name" {
  type        = string
  description = "Project or service name"
}

variable "environment" {
  type        = string
  description = "Target environment (dev, staging, production)"
}

variable "region" {
  type        = string
  description = "Cloud deployment region"
}

variable "runtime" {
  type        = string
  default     = "dotnet8"
  description = "Serverless function runtime"
}

variable "memory_mb" {
  type        = number
  default     = 256
  description = "Memory allocation in MB"
}

resource "terraform_data" "serverless_function" {
  input = {
    function_name = "${var.project_name}-fn-${var.environment}"
    runtime       = var.runtime
    memory        = var.memory_mb
    region        = var.region
  }
}

output "deployment_status" {
  value = "Provisioned"
}

output "function_name" {
  value = terraform_data.serverless_function.output.function_name
}

output "invoke_url" {
  value = "https://${var.region}.functions.internal/${var.project_name}-${var.environment}"
}
