# Development environment configuration
project_name       = "payflowpro"
environment        = "dev"
location          = "East US"
app_service_sku   = "B1"
sql_database_sku  = "S0"
sql_admin_username = "sqladmin"

tags = {
  Environment = "dev"
  Project     = "PayFlowPro"
  ManagedBy   = "Terraform"
  Owner       = "DevTeam"
  CostCenter  = "Development"
}