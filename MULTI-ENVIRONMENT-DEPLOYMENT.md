# Multi-Environment Deployment Guide for PayFlowPro

This guide explains how to deploy PayFlowPro to different environments (staging, production) or different Azure accounts.

## 🎯 **Deployment Options**

### Option A: Same Azure Account, Different Environment (Recommended)
- Deploy to staging/production in the same subscription
- Use different resource groups and naming conventions
- Easier management and cost control

### Option B: Different Azure Account/Subscription
- Completely separate Azure subscription
- Independent billing and management
- Enhanced security isolation

## 📋 **Prerequisites Checklist**

Before starting deployment to a new environment:

### 1. **Azure Account Setup**
- [ ] Azure subscription with sufficient permissions
- [ ] Contributor or Owner role on the subscription
- [ ] Azure CLI installed and configured
- [ ] PowerShell or Bash terminal access

### 2. **Code Repository**
- [ ] PayFlowPro source code accessible
- [ ] Terraform files from `C:\sal\PayFlowPro\terraform\`
- [ ] CI/CD pipeline files from `.azure-pipelines\`

### 3. **Environment Planning**
- [ ] Choose environment name (staging/prod/test)
- [ ] Define resource naming convention
- [ ] Plan Azure region (East US, West US, etc.)
- [ ] Determine resource sizing (Basic/Standard/Premium)

## 🚀 **Step-by-Step Deployment Process**

### **Step 1: Prepare New Environment Configuration**

#### 1.1 Create Environment-Specific Variables File

Create a new `.tfvars` file for your target environment:

**For Staging Environment:**
```hcl
# terraform/staging.tfvars
project_name     = "payflowpro"
environment      = "staging"
location         = "East US"  # Or your preferred region
app_service_sku  = "S1"       # Upgrade from B1 for staging
sql_database_sku = "S1"       # Upgrade from S0 for staging
sql_admin_username = "sqladmin"
sql_admin_password = "PayFlowPro2024!Staging"  # Use different password

tags = {
  Environment = "staging"
  Project     = "PayFlowPro"
  ManagedBy   = "Terraform"
  Owner       = "YourTeam"
}
```

**For Production Environment:**
```hcl
# terraform/prod.tfvars
project_name     = "payflowpro"
environment      = "prod"
location         = "East US"  # Or your preferred region
app_service_sku  = "P1V2"     # Premium for production
sql_database_sku = "S2"       # Higher tier for production
sql_admin_username = "sqladmin"
sql_admin_password = "PayFlowPro2024!Production"  # Strong unique password

tags = {
  Environment = "production"
  Project     = "PayFlowPro"
  ManagedBy   = "Terraform"
  Owner       = "YourTeam"
}
```

### **Step 2: Azure Account Setup**

#### 2.1 Login to Target Azure Account
```powershell
# Login to the target Azure account
az login

# List available subscriptions
az account list --output table

# Set the target subscription
az account set --subscription "Your-Subscription-ID"

# Verify current context
az account show
```

#### 2.2 Create Terraform State Storage (One-time per subscription)
```powershell
# Create resource group for Terraform state
az group create --name "payflowpro-terraform-rg" --location "East US"

# Create storage account for Terraform state (name must be globally unique)
az storage account create \
  --name "payflowproterraform$(Get-Random)" \
  --resource-group "payflowpro-terraform-rg" \
  --location "East US" \
  --sku "Standard_LRS"

# Create container for state files
az storage container create \
  --name "tfstate" \
  --account-name "your-storage-account-name"
```

### **Step 3: Infrastructure Deployment**

#### 3.1 Initialize Terraform for New Environment
```powershell
cd C:\sal\PayFlowPro\terraform

# Initialize with new backend (update storage account name)
terraform init \
  -backend-config="resource_group_name=payflowpro-terraform-rg" \
  -backend-config="storage_account_name=your-storage-account-name" \
  -backend-config="container_name=tfstate" \
  -backend-config="key=staging.terraform.tfstate" \
  -reconfigure
```

#### 3.2 Plan and Apply Infrastructure
```powershell
# Plan the deployment
terraform plan -var-file="staging.tfvars"

# Apply the deployment
terraform apply -var-file="staging.tfvars" -auto-approve
```

### **Step 4: Application Deployment**

#### 4.1 Build Application
```powershell
cd C:\sal\PayFlowPro

# Build and publish for the new environment
dotnet publish src/PayFlowPro.Web -c Release -o ./publish-staging

# Create deployment package
Compress-Archive -Path ".\publish-staging\*" -DestinationPath ".\payflowpro-staging.zip" -Force
```

#### 4.2 Deploy to Azure App Service
```powershell
# Deploy application
az webapp deployment source config-zip \
  --name "payflowpro-staging-app" \
  --resource-group "payflowpro-staging-rg" \
  --src ".\payflowpro-staging.zip"
```

#### 4.3 Configure Application Settings
```powershell
# Set environment variables
az webapp config appsettings set \
  --name "payflowpro-staging-app" \
  --resource-group "payflowpro-staging-rg" \
  --settings ASPNETCORE_ENVIRONMENT="Staging"

# Configure connection string
az webapp config connection-string set \
  --name "payflowpro-staging-app" \
  --resource-group "payflowpro-staging-rg" \
  --connection-string-type "SQLAzure" \
  --settings DefaultConnection="Server=payflowpro-staging-sql.database.windows.net;Database=payflowpro-db;User Id=sqladmin;Password=PayFlowPro2024!Staging;Encrypt=True;TrustServerCertificate=False;"
```

### **Step 5: Database Setup**

#### 5.1 Add Firewall Rule for Your IP
```powershell
# Add firewall rule (replace with your IP)
az sql server firewall-rule create \
  --name "AllowMyIP" \
  --server "payflowpro-staging-sql" \
  --resource-group "payflowpro-staging-rg" \
  --start-ip-address "YOUR-IP-ADDRESS" \
  --end-ip-address "YOUR-IP-ADDRESS"
```

#### 5.2 Run Database Migrations
```powershell
# Apply database migrations
dotnet ef database update \
  --project src/PayFlowPro.Web \
  --connection "Server=payflowpro-staging-sql.database.windows.net;Database=payflowpro-db;User Id=sqladmin;Password=PayFlowPro2024!Staging;Encrypt=True;TrustServerCertificate=False;"
```

## 🔧 **Environment-Specific Configurations**

### **Resource Sizing Recommendations**

| Environment | App Service | SQL Database | Use Case |
|-------------|-------------|--------------|----------|
| Development | B1 ($13/mo) | S0 ($15/mo) | Testing, development |
| Staging | S1 ($56/mo) | S1 ($30/mo) | Pre-production testing |
| Production | P1V2 ($109/mo) | S2 ($75/mo) | Live production workload |

### **Security Considerations by Environment**

#### Development
- Basic firewall rules
- Simple passwords (still secure)
- Minimal monitoring

#### Staging
- Restricted firewall rules
- Complex passwords
- Enhanced monitoring
- SSL certificates

#### Production
- Strict network security
- Key Vault for secrets
- Advanced monitoring
- Custom domains
- Backup strategies

## 📄 **Configuration Files for Each Environment**

### **appsettings.Staging.json**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "FeatureFlags": {
    "EnableDetailedErrors": true,
    "EnableSwagger": true,
    "EnableDeveloperExceptionPage": false
  }
}
```

### **appsettings.Production.json**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  },
  "AllowedHosts": "your-domain.com",
  "ConnectionStrings": {
    "DefaultConnection": "@Microsoft.KeyVault(VaultName={keyVaultName};SecretName=ConnectionStrings--DefaultConnection)"
  },
  "FeatureFlags": {
    "EnableDetailedErrors": false,
    "EnableSwagger": false,
    "EnableDeveloperExceptionPage": false
  },
  "Security": {
    "RequireHttps": true,
    "CookieSecure": true
  }
}
```

## 🚀 **CI/CD Pipeline for Multi-Environment**

### **Pipeline Structure**
```
CI Pipeline (Build Once)
├── Build Application
├── Run Tests
├── Create Artifacts
└── Store Artifacts

CD Pipeline (Deploy to Multiple Environments)
├── Deploy to Development (Auto)
├── Deploy to Staging (Manual Approval)
└── Deploy to Production (Manual Approval)
```

### **Environment-Specific Pipeline Variables**

Create variable groups for each environment:

**Development Variables:**
- `azureServiceConnection`: azure-dev-connection
- `resourceGroupName`: payflowpro-dev-rg
- `webAppName`: payflowpro-dev-app
- `sqlServerName`: payflowpro-dev-sql

**Staging Variables:**
- `azureServiceConnection`: azure-staging-connection
- `resourceGroupName`: payflowpro-staging-rg
- `webAppName`: payflowpro-staging-app
- `sqlServerName`: payflowpro-staging-sql

**Production Variables:**
- `azureServiceConnection`: azure-prod-connection
- `resourceGroupName`: payflowpro-prod-rg
- `webAppName`: payflowpro-prod-app
- `sqlServerName`: payflowpro-prod-sql

## ✅ **Deployment Checklist**

### Pre-Deployment
- [ ] Azure subscription ready
- [ ] Environment variables configured
- [ ] Resource naming convention defined
- [ ] Terraform state storage created
- [ ] Firewall rules planned

### During Deployment
- [ ] Terraform infrastructure deployed
- [ ] Application built and deployed
- [ ] Database migrations applied
- [ ] Application settings configured
- [ ] SSL/TLS certificates configured (if needed)

### Post-Deployment
- [ ] Application accessibility verified
- [ ] Database connectivity tested
- [ ] Monitoring and logging configured
- [ ] Performance testing completed
- [ ] Security scanning performed
- [ ] Backup strategies implemented

## 🔐 **Security Best Practices**

### Secrets Management
- Use Azure Key Vault for production secrets
- Never commit passwords to source control
- Rotate passwords regularly
- Use managed identities where possible

### Network Security
- Configure SQL firewall rules restrictively
- Use private endpoints for production
- Implement Web Application Firewall (WAF)
- Enable DDoS protection

### Monitoring & Logging
- Configure Application Insights
- Set up alerts for critical metrics
- Enable SQL auditing
- Monitor for security events

## 📞 **Support & Troubleshooting**

### Common Issues
1. **Storage account name conflicts** - Use random suffix
2. **Firewall connection issues** - Add current IP to SQL firewall
3. **Migration conflicts** - Check existing database schema
4. **Resource naming conflicts** - Use unique naming convention

### Useful Commands
```powershell
# Check resource status
az resource list --resource-group "payflowpro-staging-rg" --output table

# Test application
Invoke-WebRequest -Uri "https://payflowpro-staging-app.azurewebsites.net"

# Check logs
az webapp log tail --name "payflowpro-staging-app" --resource-group "payflowpro-staging-rg"
```

This guide ensures consistent, secure, and reliable deployments across all your environments! 🚀