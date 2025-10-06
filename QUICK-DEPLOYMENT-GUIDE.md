# 🚀 Quick Deployment Checklist - New Environment

## **Option 1: Same Azure Account (Recommended)**

### **1. Prepare New Environment File**
```powershell
# Copy and modify terraform variables
cp terraform/terraform.tfvars terraform/prod.tfvars

# Update values in prod.tfvars:
# - environment = "prod"
# - app_service_sku = "P1V2"  # Production tier
# - sql_database_sku = "S2"   # Production tier
# - sql_admin_password = "NewUniquePassword2024!"
```

### **2. Deploy Infrastructure**
```powershell
cd C:\sal\PayFlowPro\terraform

# Initialize for new environment
terraform init -reconfigure \
  -backend-config="key=prod.terraform.tfstate"

# Deploy infrastructure
terraform plan -var-file="prod.tfvars"
terraform apply -var-file="prod.tfvars"
```

### **3. Deploy Application**
```powershell
cd C:\sal\PayFlowPro

# Build for production
dotnet publish src/PayFlowPro.Web -c Release -o ./publish-prod

# Package and deploy
Compress-Archive -Path ".\publish-prod\*" -DestinationPath ".\payflowpro-prod.zip" -Force

az webapp deployment source config-zip \
  --name "payflowpro-prod-app" \
  --resource-group "payflowpro-prod-rg" \
  --src ".\payflowpro-prod.zip"
```

### **4. Setup Database**
```powershell
# Add firewall rule
az sql server firewall-rule create \
  --name "AllowMyIP" \
  --server "payflowpro-prod-sql" \
  --resource-group "payflowpro-prod-rg" \
  --start-ip-address "YOUR-IP" \
  --end-ip-address "YOUR-IP"

# Run migrations
dotnet ef database update --project src/PayFlowPro.Web \
  --connection "Server=payflowpro-prod-sql.database.windows.net;Database=payflowpro-db;User Id=sqladmin;Password=NewUniquePassword2024!;Encrypt=True;"
```

---

## **Option 2: Different Azure Account**

### **1. Setup New Azure Account**
```powershell
# Login to new account
az login
az account set --subscription "new-subscription-id"

# Create Terraform state storage
az group create --name "payflowpro-terraform-rg" --location "East US"
az storage account create --name "payflowproterraform$(Get-Random)" --resource-group "payflowpro-terraform-rg" --location "East US" --sku "Standard_LRS"
az storage container create --name "tfstate" --account-name "YOUR-STORAGE-NAME"
```

### **2. Update Backend Configuration**
```powershell
# Initialize with new backend
terraform init -reconfigure \
  -backend-config="resource_group_name=payflowpro-terraform-rg" \
  -backend-config="storage_account_name=YOUR-NEW-STORAGE-NAME" \
  -backend-config="container_name=tfstate" \
  -backend-config="key=prod.terraform.tfstate"
```

### **3. Follow Steps 2-4 from Option 1**

---

## **🎯 Key Differences by Environment**

| Setting | Development | Staging | Production |
|---------|-------------|---------|------------|
| App Service SKU | B1 ($13/mo) | S1 ($56/mo) | P1V2 ($109/mo) |
| SQL Database SKU | S0 ($15/mo) | S1 ($30/mo) | S2 ($75/mo) |
| Environment Name | dev | staging | prod |
| ASPNETCORE_ENVIRONMENT | Development | Staging | Production |
| Error Details | Enabled | Enabled | Disabled |
| Swagger | Enabled | Enabled | Disabled |

---

## **🔍 Verification Steps**

After deployment, verify:

```powershell
# Check resource status
az resource list --resource-group "payflowpro-prod-rg" --output table

# Test application
Invoke-WebRequest -Uri "https://payflowpro-prod-app.azurewebsites.net"

# Test login page
Invoke-WebRequest -Uri "https://payflowpro-prod-app.azurewebsites.net/Account/Login"
```

## **⚠️ Important Notes**

1. **Resource Names**: Each environment gets unique names (dev/staging/prod)
2. **Passwords**: Use different strong passwords for each environment
3. **Firewall Rules**: Add your IP address to SQL Server firewall
4. **Storage Names**: Must be globally unique across all Azure accounts
5. **Backup State**: Always backup terraform.tfstate files

## **💡 Pro Tips**

- Use different Azure regions for production (disaster recovery)
- Implement Azure Key Vault for production secrets
- Set up monitoring and alerts for production
- Use custom domains for production environments
- Enable SSL certificates for custom domains

**Total Time**: ~30-45 minutes per new environment

Your PayFlowPro application will be ready in the new environment! 🎉