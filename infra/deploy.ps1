# Azure SmartCost Deployment Script - PowerShell
# This script deploys the infrastructure and applications to Azure

param(
    [string]$Environment = "dev",
    [string]$SubscriptionId = "",
    [string]$ResourceGroup = "rg-smartcost-dev",
    [string]$Location = "eastus"
)

# Configuration
$BicepFile = "./main.bicep"
$ParametersFile = "./parameters.$Environment.json"

Write-Host "🚀 Azure SmartCost Deployment Script" -ForegroundColor Blue
Write-Host "=====================================" -ForegroundColor Blue

# Check if user is logged into Azure CLI
Write-Host "📋 Checking Azure CLI login..." -ForegroundColor Yellow
try {
    $account = az account show 2>$null | ConvertFrom-Json
    if (!$account) {
        throw "Not logged in"
    }
}
catch {
    Write-Host "❌ You are not logged into Azure CLI. Please run 'az login' first." -ForegroundColor Red
    exit 1
}

# Set subscription if provided
if ($SubscriptionId) {
    Write-Host "🔧 Setting Azure subscription to: $SubscriptionId" -ForegroundColor Yellow
    az account set --subscription $SubscriptionId
}

# Get current subscription info
$currentSubscription = az account show --query "name" -o tsv
Write-Host "✅ Using Azure subscription: $currentSubscription" -ForegroundColor Green

# Create resource group
Write-Host "📦 Creating resource group: $ResourceGroup" -ForegroundColor Yellow
az group create --name $ResourceGroup --location $Location --output table

# Deploy Bicep template
Write-Host "🔧 Deploying infrastructure with Bicep..." -ForegroundColor Yellow
$deploymentName = "smartcost-deploy-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

az deployment group create `
    --resource-group $ResourceGroup `
    --template-file $BicepFile `
    --parameters "@$ParametersFile" `
    --name $deploymentName `
    --output table

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Bicep deployment failed!" -ForegroundColor Red
    exit 1
}

# Get deployment outputs
Write-Host "📋 Getting deployment outputs..." -ForegroundColor Yellow
$apiAppName = az deployment group show --resource-group $ResourceGroup --name $deploymentName --query "properties.outputs.apiAppName.value" -o tsv
$apiAppUrl = az deployment group show --resource-group $ResourceGroup --name $deploymentName --query "properties.outputs.apiAppUrl.value" -o tsv
$staticWebAppName = az deployment group show --resource-group $ResourceGroup --name $deploymentName --query "properties.outputs.staticWebAppName.value" -o tsv
$staticWebAppUrl = az deployment group show --resource-group $ResourceGroup --name $deploymentName --query "properties.outputs.staticWebAppUrl.value" -o tsv
$functionAppName = az deployment group show --resource-group $ResourceGroup --name $deploymentName --query "properties.outputs.functionAppName.value" -o tsv

Write-Host "✅ Infrastructure deployment completed!" -ForegroundColor Green
Write-Host "📋 Deployment Summary:" -ForegroundColor Blue
Write-Host "   • Resource Group: $ResourceGroup"
Write-Host "   • API App: $apiAppName"
Write-Host "   • API URL: $apiAppUrl"
Write-Host "   • Static Web App: $staticWebAppName"
Write-Host "   • Frontend URL: $staticWebAppUrl"
Write-Host "   • Function App: $functionAppName"

# Deploy API application
Write-Host "🚀 Building and deploying API application..." -ForegroundColor Yellow
$apiPath = "../src/AzureSmartCost.Api"
Push-Location $apiPath

try {
    # Build and publish
    dotnet publish -c Release -o ./publish
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }

    # Create deployment package
    Write-Host "📦 Creating API deployment package..." -ForegroundColor Yellow
    Push-Location "./publish"
    Compress-Archive -Path "." -DestinationPath "../api-deploy.zip" -Force
    Pop-Location

    # Deploy to App Service
    Write-Host "🚀 Deploying API to App Service..." -ForegroundColor Yellow
    az webapp deploy --resource-group $ResourceGroup --name $apiAppName --src-path "./api-deploy.zip" --type zip
    
    if ($LASTEXITCODE -ne 0) {
        throw "Deployment failed"
    }

    # Clean up
    Remove-Item "./api-deploy.zip" -ErrorAction SilentlyContinue
    Remove-Item "./publish" -Recurse -ErrorAction SilentlyContinue

    Write-Host "✅ API deployment completed!" -ForegroundColor Green
}
catch {
    Write-Host "❌ API deployment failed: $_" -ForegroundColor Red
}
finally {
    Pop-Location
}

# Deploy Frontend information
Write-Host "📋 Frontend deployment information:" -ForegroundColor Yellow
Write-Host "   • Static Web App deployment token is needed for GitHub Actions"
Write-Host "   • Get the deployment token with:"
Write-Host "     az staticwebapp secrets list --name $staticWebAppName --resource-group $ResourceGroup --query 'properties.apiKey' -o tsv"

Write-Host "🎉 Azure SmartCost deployment completed successfully!" -ForegroundColor Green
Write-Host "📋 Next Steps:" -ForegroundColor Blue
Write-Host "   1. Configure Azure AD application registration"
Write-Host "   2. Update Azure AD Client ID and Secret in Key Vault"
Write-Host "   3. Set up GitHub Actions for Static Web App deployment"
Write-Host "   4. Configure custom domain (if needed)"
Write-Host "   5. Set up monitoring and alerts"

Write-Host "🔗 Useful URLs:" -ForegroundColor Blue
Write-Host "   • API Swagger: $apiAppUrl/swagger"
Write-Host "   • Frontend: $staticWebAppUrl"
Write-Host "   • Azure Portal: https://portal.azure.com"