# PayFlowPro Azure Deployment Guide

This guide provides step-by-step instructions for deploying PayFlowPro to Azure using Terraform Infrastructure as Code (IaC) and Azure DevOps CI/CD pipelines.

## 🏗️ Architecture Overview

The deployment consists of:
- **Azure App Service**: Hosts the .NET 8 Blazor application
- **Azure SQL Database**: Stores application data
- **Azure Key Vault**: Manages secrets and connection strings
- **Application Insights**: Provides monitoring and telemetry
- **Log Analytics**: Centralized logging

## 📋 Prerequisites

### Local Development
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Terraform](https://www.terraform.io/downloads.html) (v1.5.0 or later)
- [PowerShell](https://github.com/PowerShell/PowerShell) (Core 7.0 or later)

### Azure Requirements
- Azure Subscription with Owner or Contributor permissions
- Azure DevOps organization and project
- Service Principal with appropriate permissions

## 🚀 Quick Start

### 1. Setup Local Development

```powershell
# Clone the repository
git clone https://github.com/Surinder1206/PayFlowPro.git
cd PayFlowPro

# Run the local setup script
.\scripts\setup-local-dev.ps1
```

### 2. Setup Azure Infrastructure

```powershell
# Login to Azure
az login

# Run the Azure setup script
.\scripts\setup-azure-devops.ps1 -SubscriptionId "your-subscription-id" -ResourceGroupName "payflowpro-terraform-rg" -ServiceConnectionName "azure-payflowpro-connection"
```

### 3. Configure Azure DevOps

1. **Create Azure DevOps Project**
   - Go to your Azure DevOps organization
   - Create a new project named "PayFlowPro"

2. **Create Service Connection**
   - Go to Project Settings > Service connections
   - Create new Azure Resource Manager connection
   - Name it exactly as specified in setup script
   - Grant access to all pipelines

3. **Import Repository**
   - Import your Git repository to Azure Repos
   - Or connect to your external Git repository

4. **Create Variable Groups**
   - Go to Pipelines > Library
   - Create variable group named "PayFlowPro-Variables"
   - Add the following variables:

   ```
   azureServiceConnection: azure-payflowpro-connection
   terraformStateResourceGroup: payflowpro-terraform-rg
   terraformStateStorageAccount: payflowproterraform
   sqlAdminUsername: sqladmin
   sqlAdminPassword: [generated-password] (mark as secret)
   subscriptionId: your-subscription-id
   ```

### 4. Create Pipelines

1. **CI Pipeline**
   - Go to Pipelines > Pipelines
   - Create new pipeline
   - Select your repository
   - Choose "Existing Azure Pipelines YAML file"
   - Select `.azure-pipelines/ci-pipeline.yml`
   - Save and run

2. **CD Pipeline**
   - Create another new pipeline
   - Select `.azure-pipelines/cd-pipeline.yml`
   - Configure environment approvals if needed
   - Save

## 🔧 Environment Configuration

### Development Environment
- **App Service SKU**: B1
- **SQL Database SKU**: S0
- **Features**: Debug mode, detailed errors enabled

### Production Environment
- **App Service SKU**: P1V2
- **SQL Database SKU**: S2
- **Features**: Optimized for performance and security

## 📁 Project Structure

```
PayFlowPro/
├── .azure-pipelines/          # CI/CD pipeline definitions
│   ├── ci-pipeline.yml        # Continuous Integration
│   └── cd-pipeline.yml        # Continuous Deployment
├── terraform/                 # Infrastructure as Code
│   ├── main.tf               # Main Terraform configuration
│   ├── variables.tf          # Variable definitions
│   ├── outputs.tf            # Output definitions
│   ├── dev.tfvars           # Development environment variables
│   └── prod.tfvars          # Production environment variables
├── scripts/                   # Deployment scripts
│   ├── setup-azure-devops.ps1
│   └── setup-local-dev.ps1
├── src/                      # Application source code
└── tests/                    # Unit and integration tests
```

## 🔄 CI/CD Pipeline Details

### CI Pipeline (Continuous Integration)
**Triggers**: Push to main/master/develop branches, Pull Requests
**Steps**:
1. **Setup**: Install .NET 8 SDK
2. **Restore**: Download NuGet packages
3. **Build**: Compile the application
4. **Test**: Run unit tests with code coverage
5. **Publish**: Create deployment artifacts
6. **Archive**: Store artifacts for CD pipeline

**Artifacts Produced**:
- Web application package
- Terraform configuration files
- Database migration scripts

### CD Pipeline (Continuous Deployment)
**Triggers**: Manual or automated after CI completion
**Stages**:

1. **Infrastructure**: Deploy/update Azure resources using Terraform
2. **Database**: Run migrations and schema updates
3. **Deploy**: Deploy application to Azure App Service
4. **Post-Deploy**: Run smoke tests and health checks

## 🛡️ Security Considerations

### Secrets Management
- SQL passwords stored in Azure Key Vault
- Application secrets managed via Key Vault references
- Service principal authentication for deployments

### Network Security
- HTTPS only enforcement
- SQL firewall rules configured
- Managed identity for Key Vault access

### Application Security
- Authentication via Azure AD (configurable)
- Role-based access control (RBAC)
- Secure cookie settings in production

## 📊 Monitoring & Logging

### Application Insights
- Real-time performance monitoring
- Exception tracking and alerting
- User behavior analytics
- Custom telemetry and metrics

### Log Analytics
- Centralized application logging
- Query and analyze logs with KQL
- Create custom dashboards and alerts

## 🔧 Troubleshooting

### Common Issues

**1. Terraform State Lock**
```powershell
# If Terraform state is locked
az storage blob lease break --container-name tfstate --blob-name dev.terraform.tfstate --account-name payflowproterraform
```

**2. Database Connection Issues**
- Verify SQL firewall rules allow Azure services
- Check connection string in Key Vault
- Ensure managed identity has Key Vault access

**3. App Service Deployment Fails**
- Check deployment logs in Azure portal
- Verify app settings and connection strings
- Ensure correct runtime stack (.NET 8)

**4. Pipeline Permissions**
```powershell
# Grant additional permissions to service principal if needed
az role assignment create --assignee <service-principal-id> --role "Key Vault Secrets Officer" --scope /subscriptions/<subscription-id>
```

### Useful Commands

```powershell
# Test local application
dotnet run --project src/PayFlowPro.Web

# Run Terraform locally
cd terraform
terraform init
terraform plan -var-file="dev.tfvars" -var="sql_admin_password=YourPassword123!"

# Check Azure resources
az group list --output table
az webapp list --output table

# View Key Vault secrets
az keyvault secret list --vault-name payflowpro-dev-kv
```

## 🚀 Advanced Configuration

### Custom Domains
1. Purchase domain and configure DNS
2. Add custom domain in App Service
3. Configure SSL certificate
4. Update application settings

### Scaling
- Configure auto-scaling rules in App Service Plan
- Implement application performance monitoring
- Consider Azure SQL elastic pools for multiple databases

### Backup & Disaster Recovery
- Configure automated SQL database backups
- Setup App Service backup policies
- Implement cross-region disaster recovery

## 🎯 Best Practices

1. **Version Control**: Tag releases and maintain changelog
2. **Environment Parity**: Keep dev/staging/prod configurations similar
3. **Security**: Regularly rotate secrets and update dependencies
4. **Monitoring**: Set up alerting for critical application metrics
5. **Documentation**: Keep deployment docs updated with changes

## 📞 Support

For deployment issues:
1. Check Azure DevOps pipeline logs
2. Review Azure resource logs in portal
3. Consult Application Insights for application errors
4. Check this documentation for common solutions

## 🔄 Updates & Maintenance

### Regular Maintenance
- Update Terraform provider versions
- Patch application dependencies
- Review and rotate secrets
- Monitor costs and optimize resources

### Deployment Updates
- Test infrastructure changes in dev first
- Use feature flags for application changes
- Maintain rollback procedures
- Document all changes