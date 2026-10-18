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

variable "container_image" {
  type        = string
  default     = "nginx:alpine"
  description = "Container image to deploy"
}

variable "replica_count" {
  type        = number
  default     = 1
  description = "Number of container replicas"
}

# Local provisioning resource simulating cloud container provisioning
resource "terraform_data" "container_cluster" {
  input = {
    project_name = var.project_name
    environment  = var.environment
    region       = var.region
    image        = var.container_image
    replicas     = var.replica_count
    cluster_id   = "${var.project_name}-${var.environment}-${var.region}"
  }
}

output "deployment_status" {
  value = "Provisioned"
}

output "cluster_identifier" {
  value = terraform_data.container_cluster.output.cluster_id
}

output "endpoint" {
  value = "https://${var.project_name}.${var.environment}.${var.region}.internal"
}
