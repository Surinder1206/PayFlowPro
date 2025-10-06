# Local Development Setup Script
# Sets up local development environment for PayFlowPro

param(
    [Parameter(Mandatory=$false)]
    [string]$Environment = "Development"
)

Write-Host "🚀 Setting up PayFlowPro local development environment..." -ForegroundColor Green

# Check if .NET 8 SDK is installed
Write-Host "🔍 Checking .NET 8 SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    if ($dotnetVersion -like "8.*") {
        Write-Host "✅ .NET 8 SDK found: $dotnetVersion" -ForegroundColor Green
    } else {
        Write-Host "❌ .NET 8 SDK not found. Please install .NET 8 SDK" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ .NET SDK not found. Please install .NET 8 SDK" -ForegroundColor Red
    exit 1
}

# Check if SQL Server LocalDB is available
Write-Host "🔍 Checking SQL Server LocalDB..." -ForegroundColor Yellow
try {
    $sqllocaldb = sqllocaldb info MSSQLLocalDB 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ SQL Server LocalDB found" -ForegroundColor Green
    } else {
        Write-Host "❌ SQL Server LocalDB not found. Please install SQL Server LocalDB" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ SQL Server LocalDB not found. Please install SQL Server LocalDB" -ForegroundColor Red
}

# Restore NuGet packages
Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ NuGet packages restored successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to restore NuGet packages" -ForegroundColor Red
    exit 1
}

# Build the solution
Write-Host "🔨 Building solution..." -ForegroundColor Yellow
dotnet build --configuration Debug
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Solution built successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}

# Run database migrations
Write-Host "🗄️  Setting up database..." -ForegroundColor Yellow
try {
    Push-Location "src/PayFlowPro.Web"
    dotnet ef database update
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Database migrations applied successfully" -ForegroundColor Green
    } else {
        Write-Host "❌ Database migration failed" -ForegroundColor Red
    }
    Pop-Location
} catch {
    Write-Host "❌ Database setup failed" -ForegroundColor Red
    Pop-Location
}

# Run tests
Write-Host "🧪 Running tests..." -ForegroundColor Yellow
dotnet test --configuration Debug --no-build
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ All tests passed" -ForegroundColor Green
} else {
    Write-Host "⚠️  Some tests failed" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎉 Local development environment setup completed!" -ForegroundColor Green
Write-Host ""
Write-Host "🚀 To start the application:" -ForegroundColor Cyan
Write-Host "   cd src/PayFlowPro.Web"
Write-Host "   dotnet run"
Write-Host ""
Write-Host "🌐 Application will be available at:" -ForegroundColor Cyan
Write-Host "   https://localhost:7001"
Write-Host "   http://localhost:5000"