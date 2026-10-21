terraform {
  required_version = ">= 1.0.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

provider "aws" {
  region = var.region
}

variable "project_name" {
  type        = string
  description = "Project name"
}

variable "environment" {
  type        = string
  description = "Environment (dev, staging, production)"
}

variable "region" {
  type        = string
  default     = "ap-south-1"
  description = "AWS region"
}

# Unique suffix to guarantee globally unique S3 bucket name
resource "random_id" "suffix" {
  byte_length = 4
}

locals {
  clean_project = lower(replace(var.project_name, "_", "-"))
  clean_env     = lower(replace(var.environment, "_", "-"))
  bucket_name   = "${local.clean_project}-${local.clean_env}-${random_id.suffix.hex}"
}

resource "aws_s3_bucket" "webapp" {
  bucket        = local.bucket_name
  force_destroy = true

  tags = {
    Name        = local.bucket_name
    Environment = var.environment
    ManagedBy   = "DevOps-Commander"
  }
}

resource "aws_s3_bucket_website_configuration" "webapp" {
  bucket = aws_s3_bucket.webapp.id

  index_document {
    suffix = "index.html"
  }
}

resource "aws_s3_bucket_public_access_block" "webapp" {
  bucket = aws_s3_bucket.webapp.id

  block_public_acls       = false
  block_public_policy     = false
  ignore_public_acls      = false
  restrict_public_buckets = false
}

resource "aws_s3_bucket_policy" "webapp" {
  depends_on = [aws_s3_bucket_public_access_block.webapp]
  bucket     = aws_s3_bucket.webapp.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid       = "PublicReadGetObject"
        Effect    = "Allow"
        Principal = "*"
        Action    = "s3:GetObject"
        Resource  = "${aws_s3_bucket.webapp.arn}/*"
      }
    ]
  })
}

resource "aws_s3_object" "index" {
  bucket       = aws_s3_bucket.webapp.id
  key          = "index.html"
  content_type = "text/html"
  content      = <<-HTML
    <!DOCTYPE html>
    <html lang="en">
    <head>
      <meta charset="UTF-8">
      <title>${var.project_name} - DevOps Commander</title>
      <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #0f172a; color: #f8fafc; text-align: center; padding: 50px; }
        .card { background: #1e293b; border-radius: 12px; padding: 40px; display: inline-block; box-shadow: 0 10px 25px rgba(0,0,0,0.5); max-width: 600px; border: 1px solid #334155; }
        h1 { color: #38bdf8; margin-bottom: 8px; }
        .badge { background: #22c55e; color: #000; font-weight: bold; padding: 6px 14px; border-radius: 20px; font-size: 14px; text-transform: uppercase; }
        .details { text-align: left; margin-top: 30px; background: #0f172a; padding: 18px; border-radius: 8px; font-family: monospace; font-size: 14px; line-height: 1.6; }
      </style>
    </head>
    <body>
      <div class="card">
        <span class="badge">&#10004; Live on AWS</span>
        <h1>${var.project_name}</h1>
        <p>Infrastructure successfully provisioned to <strong>AWS ${var.region}</strong> by <strong>DevOps Commander</strong>.</p>
        <div class="details">
          <div><strong>Project:</strong> ${var.project_name}</div>
          <div><strong>Environment:</strong> ${var.environment}</div>
          <div><strong>Cloud Provider:</strong> Amazon Web Services (AWS)</div>
          <div><strong>Region:</strong> ${var.region}</div>
          <div><strong>Orchestrator:</strong> .NET 10 Minimal API + Worker</div>
          <div><strong>IaC Engine:</strong> Terraform CLI Child Process</div>
        </div>
      </div>
    </body>
    </html>
  HTML
}

output "deployment_status" {
  value = "Provisioned"
}

output "bucket_name" {
  value = aws_s3_bucket.webapp.id
}

output "bucket_arn" {
  value = aws_s3_bucket.webapp.arn
}

output "live_website_url" {
  value = "http://${aws_s3_bucket_website_configuration.webapp.website_endpoint}"
}

output "aws_console_url" {
  value = "https://${var.region}.console.aws.amazon.com/s3/buckets/${aws_s3_bucket.webapp.id}"
}
