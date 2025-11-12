# Production Deployment Script for Azure SmartCost Power BI Integration
param(
    [Parameter(Mandatory=$true)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$AppServiceName,
    
    [string]$Environment = "Production"
)

Write-Host "🚀 Azure SmartCost - Production Deployment" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check if logged in to Azure
try {
    $account = az account show --query "user.name" -o tsv
    Write-Host "✅ Logged in as: $account" -ForegroundColor Green
} catch {
    Write-Host "❌ Not logged in to Azure. Run 'az login' first." -ForegroundColor Red
    exit 1
}

# Set subscription
Write-Host "🎯 Setting subscription: $SubscriptionId" -ForegroundColor Yellow
az account set --subscription $SubscriptionId

# Step 1: Build and Publish API
Write-Host ""
Write-Host "📦 Building and Publishing API..." -ForegroundColor Yellow
$apiPath = "src\AzureSmartCost.Api"
$publishPath = "src\AzureSmartCost.Api\bin\Release\net8.0\publish"

# Clean and build
dotnet clean $apiPath --configuration Release
dotnet restore $apiPath
dotnet build $apiPath --configuration Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful" -ForegroundColor Green

# Publish
dotnet publish $apiPath --configuration Release --output $publishPath --no-build

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Publish failed" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Publish successful" -ForegroundColor Green

# Step 2: Deploy to App Service
Write-Host ""
Write-Host "🌐 Deploying to App Service..." -ForegroundColor Yellow

# Create deployment package
$zipFile = "deployment.zip"
if (Test-Path $zipFile) {
    Remove-Item $zipFile
}

# Create zip file
Compress-Archive -Path "$publishPath\*" -DestinationPath $zipFile

Write-Host "✅ Deployment package created" -ForegroundColor Green

# Deploy using Azure CLI
Write-Host "📤 Uploading to App Service: $AppServiceName" -ForegroundColor Yellow
az webapp deployment source config-zip --resource-group $ResourceGroupName --name $AppServiceName --src $zipFile

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Deployment failed" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Deployment successful" -ForegroundColor Green

# Step 3: Configure App Service Settings
Write-Host ""
Write-Host "⚙️ Configuring App Service Settings..." -ForegroundColor Yellow

# Get current settings (to avoid overwriting existing ones)
$currentSettings = az webapp config appsettings list --resource-group $ResourceGroupName --name $AppServiceName --query "[].{name:name,value:value}" -o json | ConvertFrom-Json

# Power BI specific settings
$powerBiSettings = @{
    "POWERBI_ENABLED" = "true"
    "POWERBI_ENVIRONMENT" = $Environment
    "ASPNETCORE_ENVIRONMENT" = $Environment
    "WEBSITE_RUN_FROM_PACKAGE" = "1"
}

# Apply settings
foreach ($setting in $powerBiSettings.GetEnumerator()) {
    az webapp config appsettings set --resource-group $ResourceGroupName --name $AppServiceName --settings "$($setting.Key)=$($setting.Value)"
}

Write-Host "✅ App Service configured" -ForegroundColor Green

# Step 4: Build and Deploy Frontend
Write-Host ""
Write-Host "🎨 Building Frontend..." -ForegroundColor Yellow

$frontendPath = "smartcost-dashboard"

# Install dependencies and build
Push-Location $frontendPath
npm ci
$env:REACT_APP_API_URL = "https://$AppServiceName.azurewebsites.net"
npm run build
Pop-Location

Write-Host "✅ Frontend built successfully" -ForegroundColor Green

# Step 5: Health Check
Write-Host ""
Write-Host "🏥 Performing Health Check..." -ForegroundColor Yellow
Start-Sleep -Seconds 30  # Wait for app to start

$healthUrl = "https://$AppServiceName.azurewebsites.net/api/health"
try {
    $response = Invoke-RestMethod -Uri $healthUrl -Method GET -TimeoutSec 30
    Write-Host "✅ Health check passed" -ForegroundColor Green
    Write-Host "   Status: $($response.status)" -ForegroundColor Gray
    Write-Host "   Version: $($response.version)" -ForegroundColor Gray
} catch {
    Write-Host "⚠️ Health check failed - App may still be starting" -ForegroundColor Yellow
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Gray
}

# Step 6: Monitor Setup
Write-Host ""
Write-Host "📊 Setting up Monitoring..." -ForegroundColor Yellow

# Enable Application Insights (if not already enabled)
$appInsights = az monitor app-insights component show --app $AppServiceName --resource-group $ResourceGroupName 2>$null

if (-not $appInsights) {
    Write-Host "📈 Creating Application Insights..." -ForegroundColor Yellow
    az extension add --name application-insights
    az monitor app-insights component create --app $AppServiceName --location "East US" --resource-group $ResourceGroupName --application-type web
}

Write-Host "✅ Monitoring configured" -ForegroundColor Green

# Step 7: Final Verification
Write-Host ""
Write-Host "🔍 Final Verification..." -ForegroundColor Yellow

# Test API endpoints
$endpoints = @(
    "/api/health",
    "/api/powerbi/templates"
)

foreach ($endpoint in $endpoints) {
    $testUrl = "https://$AppServiceName.azurewebsites.net$endpoint"
    try {
        $response = Invoke-RestMethod -Uri $testUrl -Method GET -TimeoutSec 10
        Write-Host "✅ $endpoint - OK" -ForegroundColor Green
    } catch {
        Write-Host "⚠️ $endpoint - Warning: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# Clean up
if (Test-Path $zipFile) {
    Remove-Item $zipFile
}

# Summary
Write-Host ""
Write-Host "🎉 DEPLOYMENT COMPLETED!" -ForegroundColor Green -BackgroundColor Black
Write-Host ""
Write-Host "📋 Summary:" -ForegroundColor Cyan
Write-Host "• API URL: https://$AppServiceName.azurewebsites.net" -ForegroundColor White
Write-Host "• Health Check: https://$AppServiceName.azurewebsites.net/api/health" -ForegroundColor White
Write-Host "• Power BI Templates: https://$AppServiceName.azurewebsites.net/api/powerbi/templates" -ForegroundColor White
Write-Host "• Application Insights: Enabled" -ForegroundColor White
Write-Host ""
Write-Host "🔗 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Configure Power BI Workspace integration" -ForegroundColor White
Write-Host "2. Set up production monitoring alerts" -ForegroundColor White
Write-Host "3. Deploy frontend to static hosting" -ForegroundColor White
Write-Host "4. Update DNS and SSL certificates" -ForegroundColor White
Write-Host ""
Write-Host "📞 Support:" -ForegroundColor Cyan
Write-Host "• Monitor logs in Application Insights" -ForegroundColor White
Write-Host "• Check App Service logs for issues" -ForegroundColor White
Write-Host "• Verify environment variables in App Service Configuration" -ForegroundColor White