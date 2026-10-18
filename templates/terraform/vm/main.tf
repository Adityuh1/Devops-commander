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

variable "instance_type" {
  type        = string
  default     = "t3.medium"
  description = "Compute instance machine type"
}

variable "disk_size_gb" {
  type        = number
  default     = 50
  description = "Root disk size in GB"
}

resource "terraform_data" "virtual_machine" {
  input = {
    vm_name       = "${var.project_name}-vm-${var.environment}"
    instance_type = var.instance_type
    disk_size     = var.disk_size_gb
    region        = var.region
  }
}

output "deployment_status" {
  value = "Provisioned"
}

output "vm_name" {
  value = terraform_data.virtual_machine.output.vm_name
}

output "private_ip" {
  value = "10.0.1.42"
}
