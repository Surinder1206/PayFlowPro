# Deployment Script for PayFlowPro
# This script sets up the Azure DevOps pipeline and initial resources

param(
    [Parameter(Mandatory=$true)]
    [string]$SubscriptionId,

    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory=$true)]
    [string]$Location = "East US",

    [Parameter(Mandatory=$true)]
    [string]$ServiceConnectionName,

    [Parameter(Mandatory=$false)]
    [string]$StorageAccountName = "payflowproterraform",

    [Parameter(Mandatory=$false)]
    [string]$Environment = "dev"
)

Write-Host "🚀 Setting up PayFlowPro deployment infrastructure..." -ForegroundColor Green

# Login to Azure (if not already logged in)
Write-Host "📋 Checking Azure login status..." -ForegroundColor Yellow
$context = Get-AzContext
if (!$context) {
    Write-Host "🔐 Please login to Azure..." -ForegroundColor Yellow
    Connect-AzAccount
}

# Set subscription
Write-Host "🎯 Setting subscription to: $SubscriptionId" -ForegroundColor Yellow
Set-AzContext -SubscriptionId $SubscriptionId

# Create resource group for Terraform state
Write-Host "📂 Creating resource group: $ResourceGroupName" -ForegroundColor Yellow
$rg = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
if (!$rg) {
    New-AzResourceGroup -Name $ResourceGroupName -Location $Location
    Write-Host "✅ Resource group created successfully" -ForegroundColor Green
} else {
    Write-Host "ℹ️  Resource group already exists" -ForegroundColor Cyan
}

# Create storage account for Terraform state
Write-Host "💾 Creating storage account: $StorageAccountName" -ForegroundColor Yellow
$storageAccount = Get-AzStorageAccount -ResourceGroupName $ResourceGroupName -Name $StorageAccountName -ErrorAction SilentlyContinue
if (!$storageAccount) {
    $storageAccount = New-AzStorageAccount -ResourceGroupName $ResourceGroupName `
                                         -Name $StorageAccountName `
                                         -Location $Location `
                                         -SkuName "Standard_LRS" `
                                         -Kind "StorageV2"
    Write-Host "✅ Storage account created successfully" -ForegroundColor Green
} else {
    Write-Host "ℹ️  Storage account already exists" -ForegroundColor Cyan
}

# Create container for Terraform state
Write-Host "📦 Creating blob container for Terraform state..." -ForegroundColor Yellow
$ctx = $storageAccount.Context
$container = Get-AzStorageContainer -Name "tfstate" -Context $ctx -ErrorAction SilentlyContinue
if (!$container) {
    New-AzStorageContainer -Name "tfstate" -Context $ctx -Permission Blob
    Write-Host "✅ Blob container created successfully" -ForegroundColor Green
} else {
    Write-Host "ℹ️  Blob container already exists" -ForegroundColor Cyan
}

# Generate strong password for SQL Server
$sqlPassword = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 16 | % {[char]$_}) + "A1!"

Write-Host "🔧 Deployment setup completed!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Configuration Summary:" -ForegroundColor Cyan
Write-Host "├─ Subscription ID: $SubscriptionId"
Write-Host "├─ Resource Group: $ResourceGroupName"
Write-Host "├─ Storage Account: $StorageAccountName"
Write-Host "├─ Location: $Location"
Write-Host "└─ Environment: $Environment"
Write-Host ""
Write-Host "🔐 Secrets to configure in Azure DevOps:" -ForegroundColor Yellow
Write-Host "├─ azureServiceConnection: $ServiceConnectionName"
Write-Host "├─ terraformStateResourceGroup: $ResourceGroupName"
Write-Host "├─ terraformStateStorageAccount: $StorageAccountName"
Write-Host "├─ sqlAdminUsername: sqladmin"
Write-Host "├─ sqlAdminPassword: $sqlPassword"
Write-Host "└─ subscriptionId: $SubscriptionId"
Write-Host ""
Write-Host "📖 Next Steps:" -ForegroundColor Green
Write-Host "1. Create Azure DevOps project"
Write-Host "2. Create service connection named '$ServiceConnectionName'"
Write-Host "3. Add the above variables to your pipeline library/variable groups"
Write-Host "4. Import the CI/CD pipeline YAML files"
Write-Host "5. Run the CI pipeline to build and deploy"
Write-Host ""
Write-Host "🎉 Ready to deploy PayFlowPro!" -ForegroundColor Green