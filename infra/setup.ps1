# Azure SmartCost - Pre-deployment Setup Script (PowerShell)
# This script prepares the environment for deployment to Azure

param(
    [string]$ProjectName = "smartcost",
    [string]$AzureRegion = "eastus"
)

$ScriptDir = $PSScriptRoot
$ProjectRoot = Split-Path $ScriptDir -Parent

Write-Host "🚀 Azure SmartCost - Pre-deployment Setup" -ForegroundColor Blue
Write-Host "========================================" -ForegroundColor Blue

function Write-Status($Message) {
    Write-Host "📋 $Message" -ForegroundColor Yellow
}

function Write-Success($Message) {
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error($Message) {
    Write-Host "❌ $Message" -ForegroundColor Red
}

# Check prerequisites
Write-Status "Checking prerequisites..."

# Check Azure CLI
try {
    $azVersion = az --version 2>$null
    if (!$azVersion) { throw }
}
catch {
    Write-Error "Azure CLI is not installed. Please install it from https://docs.microsoft.com/cli/azure/"
    exit 1
}

# Check .NET
try {
    $dotnetVersion = dotnet --version 2>$null
    if (!$dotnetVersion) { throw }
}
catch {
    Write-Error ".NET SDK is not installed. Please install .NET 8 SDK"
    exit 1
}

# Check Node.js
try {
    $nodeVersion = node --version 2>$null
    if (!$nodeVersion) { throw }
}
catch {
    Write-Error "Node.js is not installed. Please install Node.js 18+"
    exit 1
}

Write-Success "All prerequisites are installed"

# Check Azure login
Write-Status "Checking Azure CLI login..."
try {
    $account = az account show 2>$null | ConvertFrom-Json
    if (!$account) { throw }
}
catch {
    Write-Error "You are not logged into Azure CLI. Please run 'az login'"
    exit 1
}

$subscriptionName = $account.name
$subscriptionId = $account.id
$tenantId = $account.tenantId
Write-Success "Logged into Azure subscription: $subscriptionName ($subscriptionId)"

# Create Azure AD App Registration
Write-Status "Creating Azure AD App Registration..."

$appName = "Azure SmartCost API"
$existingApp = az ad app list --display-name $appName --query "[0].appId" -o tsv 2>$null

if ([string]::IsNullOrEmpty($existingApp)) {
    Write-Status "Creating new Azure AD App Registration..."
    
    # Create the app registration
    $appId = az ad app create `
        --display-name $appName `
        --web-redirect-uris "https://localhost:5001/signin-oidc" "https://smartcost-api-prod.azurewebsites.net/signin-oidc" `
        --query "appId" -o tsv
    
    Write-Success "Created Azure AD App Registration: $appId"
    
    # Create client secret
    Write-Status "Creating client secret..."
    $clientSecret = az ad app credential reset `
        --id $appId `
        --display-name "SmartCost-Secret" `
        --query "password" -o tsv
    
    Write-Success "Created client secret (save this securely!)"
}
else {
    $appId = $existingApp
    Write-Success "Using existing Azure AD App Registration: $appId"
    
    # Reset client secret
    Write-Status "Resetting client secret..."
    $clientSecret = az ad app credential reset `
        --id $appId `
        --display-name "SmartCost-Secret-$(Get-Date -Format 'yyyyMMdd')" `
        --query "password" -o tsv
    
    Write-Success "Reset client secret (save this securely!)"
}

# Create Service Principal for GitHub Actions
Write-Status "Creating Service Principal for GitHub Actions..."

$spName = "smartcost-github-actions"
$existingSp = az ad sp list --display-name $spName --query "[0].appId" -o tsv 2>$null

if ([string]::IsNullOrEmpty($existingSp)) {
    Write-Status "Creating new Service Principal..."
}
else {
    Write-Status "Resetting Service Principal credentials..."
}

$spCredentials = az ad sp create-for-rbac `
    --name $spName `
    --role contributor `
    --scopes "/subscriptions/$subscriptionId" `
    --sdk-auth

Write-Success "Service Principal configured for GitHub Actions"

# Generate JWT secret
$jwtSecret = [System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))

# Update parameter files
Write-Status "Updating parameter files..."

# Update dev parameters
$devParamsFile = Join-Path $ScriptDir "parameters.dev.json"
$devParams = Get-Content $devParamsFile | ConvertFrom-Json
$devParams.parameters.azureAdClientId.value = $appId
$devParams.parameters.azureAdClientSecret.value = $clientSecret
$devParams.parameters.jwtSecret.value = $jwtSecret
$devParams | ConvertTo-Json -Depth 10 | Set-Content $devParamsFile

Write-Success "Updated dev parameters"

# Create GitHub secrets template
Write-Status "Creating GitHub secrets template..."

$secretsFile = Join-Path $ScriptDir "github-secrets.env"
$secretsContent = @"
# GitHub Secrets for Azure SmartCost
# Add these secrets to your GitHub repository: Settings > Secrets and variables > Actions

# Service Principal for infrastructure deployment
AZURE_CREDENTIALS='$spCredentials'

# Authentication secrets
JWT_SECRET='$jwtSecret'
AZURE_AD_CLIENT_ID='$appId'
AZURE_AD_CLIENT_SECRET='$clientSecret'

# Azure subscription info
AZURE_SUBSCRIPTION_ID='$subscriptionId'
AZURE_TENANT_ID='$tenantId'
"@

Set-Content -Path $secretsFile -Value $secretsContent

Write-Success "Created GitHub secrets template: $secretsFile"

# Validate Bicep files
Write-Status "Validating Bicep templates..."

try {
    $bicepFile = Join-Path $ScriptDir "main.bicep"
    az bicep build --file $bicepFile --stdout > $null
    Write-Success "Bicep template validation passed"
}
catch {
    Write-Error "Bicep validation failed. Install Bicep with: az bicep install"
}

# Build and test the application
Write-Status "Building and testing the application..."

Push-Location $ProjectRoot

try {
    # Build the solution
    dotnet restore
    dotnet build --configuration Release
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    
    Write-Success "Application build completed"
    
    # Build frontend (if Node.js modules are installed)
    $frontendPath = Join-Path $ProjectRoot "smartcost-dashboard"
    $nodeModulesPath = Join-Path $frontendPath "node_modules"
    
    if (Test-Path $nodeModulesPath) {
        Write-Status "Building frontend..."
        Push-Location $frontendPath
        try {
            npm run build
            if ($LASTEXITCODE -ne 0) {
                throw "Frontend build failed"
            }
            Write-Success "Frontend build completed"
        }
        finally {
            Pop-Location
        }
    }
    else {
        Write-Status "Skipping frontend build (node_modules not found)"
    }
}
catch {
    Write-Error "Build failed: $_"
    exit 1
}
finally {
    Pop-Location
}

Write-Success "Pre-deployment setup completed!"

Write-Host "📋 Summary:" -ForegroundColor Blue
Write-Host "   • Azure AD App ID: $appId"
Write-Host "   • Tenant ID: $tenantId"
Write-Host "   • Subscription: $subscriptionName"
Write-Host "   • Service Principal: Created for GitHub Actions"
Write-Host "   • Secrets File: $secretsFile"

Write-Host "🔧 Next Steps:" -ForegroundColor Blue
Write-Host "   1. Add GitHub secrets from: $secretsFile"
Write-Host "   2. Review and customize parameter files"
Write-Host "   3. Run deployment script: .\infra\deploy.ps1"
Write-Host "   4. Configure custom domains (if needed)"
Write-Host "   5. Set up monitoring alerts"

Write-Host "⚠️  Important:" -ForegroundColor Yellow
Write-Host "   • Save the client secret securely - it won't be shown again"
Write-Host "   • Review the parameter files before deployment"
Write-Host "   • Delete the secrets file after adding to GitHub: Remove-Item $secretsFile"

Write-Success "Setup complete! Ready for deployment to Azure."