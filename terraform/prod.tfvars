# Production environment configuration
project_name       = "payflowpro"
environment        = "prod"
location          = "East US"
app_service_sku   = "P1V2"
sql_database_sku  = "S2"
sql_admin_username = "sqladmin"

tags = {
  Environment = "prod"
  Project     = "PayFlowPro"
  ManagedBy   = "Terraform"
  Owner       = "DevTeam"
  CostCenter  = "Production"
}