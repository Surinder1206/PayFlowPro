# Variables for PayFlowPro Infrastructure

variable "project_name" {
  description = "Name of the project"
  type        = string
  default     = "payflowpro"
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "dev"
  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be dev, staging, or prod."
  }
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "East US"
}

variable "app_service_sku" {
  description = "SKU for App Service Plan"
  type        = string
  default     = "B1"
  validation {
    condition     = contains(["F1", "B1", "B2", "S1", "S2", "P1V2", "P2V2", "P3V2"], var.app_service_sku)
    error_message = "App Service SKU must be one of: F1, B1, B2, S1, S2, P1V2, P2V2, P3V2."
  }
}

variable "sql_database_sku" {
  description = "SKU for SQL Database"
  type        = string
  default     = "S0"
  validation {
    condition     = contains(["Basic", "S0", "S1", "S2", "P1", "P2"], var.sql_database_sku)
    error_message = "SQL Database SKU must be one of: Basic, S0, S1, S2, P1, P2."
  }
}

variable "sql_admin_username" {
  description = "SQL Server administrator username"
  type        = string
  default     = "sqladmin"
}

variable "sql_admin_password" {
  description = "SQL Server administrator password"
  type        = string
  sensitive   = true
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default = {
    Environment = "dev"
    Project     = "PayFlowPro"
    ManagedBy   = "Terraform"
  }
}