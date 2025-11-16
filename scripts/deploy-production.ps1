# ============================================================================
# Azure SmartCost - Complete Production Deployment Script
# ============================================================================

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("brazilsouth", "eastus", "westeurope")]
    [string]$Location = "brazilsouth",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("demo", "production")]
    [string]$Environment = "demo",
    
    [Parameter(Mandatory=$false)]
    [string]$ProjectName = "smartcost",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipInfrastructure,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipDeployments
)

$ErrorActionPreference = "Stop"

# Helper Functions
function Write-Step { param([string]$Message) Write-Host "`n=== $Message ===`n" -ForegroundColor Cyan }
function Write-Success { param([string]$Message) Write-Host "[OK] $Message" -ForegroundColor Green }
function Write-Info { param([string]$Message) Write-Host "[INFO] $Message" -ForegroundColor Blue }
function Write-Warn { param([string]$Message) Write-Host "[WARN] $Message" -ForegroundColor Yellow }

function Test-AzureLogin {
    $account = az account show --query "user.name" -o tsv 2>$null
    if (-not $account) {
        Write-Host "[ERROR] Not logged in. Run: az login" -ForegroundColor Red
        exit 1
    }
    Write-Success "Logged in as: $account"
}

Write-Host "`nAzure SmartCost - Deployment Script v2.0" -ForegroundColor Cyan
Write-Host "Environment: $Environment | Location: $Location`n" -ForegroundColor Cyan

Test-AzureLogin

# Generate names
$suffix = Get-Random -Minimum 1000 -Maximum 9999
$rgName = "rg-$ProjectName-$Environment"
$planName = "$ProjectName-plan-$Environment"
$apiName = "$ProjectName-api-$Environment-$suffix"
$funcName = "$ProjectName-func-$Environment-$suffix"
$cosmosName = "$ProjectName-cosmos-$suffix"
$storageName = "$ProjectName" + "stg" + $suffix
$kvName = "$ProjectName-kv-$suffix"
$appInsightsName = "$ProjectName-insights-$Environment"

Write-Info "Resources:"
Write-Host "  RG: $rgName | API: $apiName | Functions: $funcName"

# PHASE 1: Infrastructure
if (-not $SkipInfrastructure) {
    Write-Step "PHASE 1: Creating Infrastructure"
    
    Write-Info "Creating Resource Group..."
    az group create --name $rgName --location $Location --tags Environment=$Environment | Out-Null
    Write-Success "Resource Group: $rgName"
    
    Write-Info "Creating App Service Plan..."
    $sku = if ($Environment -eq "production") { "B2" } else { "B1" }
    az appservice plan create --name $planName --resource-group $rgName --location $Location --sku $sku --is-linux | Out-Null
    Write-Success "App Service Plan: $planName ($sku)"
    
    Write-Info "Creating API Web App..."
    az webapp create --name $apiName --resource-group $rgName --plan $planName --runtime "DOTNETCORE:8.0" | Out-Null
    Write-Success "API: $apiName"
    
    Write-Info "Creating Cosmos DB..."
    az cosmosdb create --name $cosmosName --resource-group $rgName --locations regionName=$Location --capabilities EnableServerless --kind GlobalDocumentDB | Out-Null
    Write-Success "Cosmos DB: $cosmosName"
    
    Write-Info "Creating Storage Account..."
    az storage account create --name $storageName --resource-group $rgName --location $Location --sku Standard_LRS | Out-Null
    Write-Success "Storage: $storageName"
    
    Write-Info "Creating Application Insights..."
    az monitor app-insights component create --app $appInsightsName --resource-group $rgName --location $Location | Out-Null
    Write-Success "App Insights: $appInsightsName"
    
    Write-Info "Creating Key Vault..."
    az keyvault create --name $kvName --resource-group $rgName --location $Location --sku standard --enable-rbac-authorization true | Out-Null
    Write-Success "Key Vault: $kvName"
    
    Write-Info "Creating Functions..."
    az functionapp create --name $funcName --resource-group $rgName --storage-account $storageName --plan $planName --runtime dotnet-isolated --runtime-version 8 --functions-version 4 --os-type Linux | Out-Null
    Write-Success "Functions: $funcName"
    
    # PHASE 2: Managed Identity
    Write-Step "PHASE 2: Managed Identity and RBAC"
    
    Write-Info "Enabling identities..."
    $apiIdentity = az webapp identity assign --name $apiName --resource-group $rgName --query principalId -o tsv
    $funcIdentity = az functionapp identity assign --name $funcName --resource-group $rgName --query principalId -o tsv
    $userId = az ad signed-in-user show --query id -o tsv
    $kvId = az keyvault show --name $kvName --query id -o tsv
    
    Write-Info "Waiting 30s for propagation..."
    Start-Sleep -Seconds 30
    
    Write-Info "Assigning RBAC roles..."
    az role assignment create --role "Key Vault Secrets Officer" --assignee $userId --scope $kvId | Out-Null
    az role assignment create --role "Key Vault Secrets User" --assignee $apiIdentity --scope $kvId | Out-Null
    az role assignment create --role "Key Vault Secrets User" --assignee $funcIdentity --scope $kvId | Out-Null
    Write-Success "RBAC configured"
    
    Start-Sleep -Seconds 15
    
    # PHASE 3: Secrets
    Write-Step "PHASE 3: Configuring Secrets"
    
    Write-Info "Retrieving connection strings..."
    $cosmosConnStr = az cosmosdb keys list --name $cosmosName --resource-group $rgName --type connection-strings --query "connectionStrings[0].connectionString" -o tsv
    $storageConnStr = az storage account show-connection-string --name $storageName --resource-group $rgName --query connectionString -o tsv
    $appInsightsKey = az monitor app-insights component show --app $appInsightsName --resource-group $rgName --query instrumentationKey -o tsv
    
    $vapidPrivate = "ibHdFJkO63D8jDYCMQt8bezunwsVAmz4qX4iffd5pJg"
    $vapidPublic = "BM2DiML-aPHdmwHL8GbkcQoeEHzczh6Cp56M4Gs58FFhFwqQzo5gyT0Cc4pmO4QoOwHb_wx3x-jzGXsS-YlDqvM"
    $jwtSecret = if ($Environment -eq "production") { -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_}) } else { "DEMO-JWT-SECRET" }
    $stripeSecret = if ($Environment -eq "production") { "STRIPE_KEY_REQUIRED" } else { "placeholder" }
    
    Write-Info "Storing 7 secrets..."
    az keyvault secret set --vault-name $kvName --name "CosmosDB-ConnectionString" --value $cosmosConnStr | Out-Null
    az keyvault secret set --vault-name $kvName --name "Storage-ConnectionString" --value $storageConnStr | Out-Null
    az keyvault secret set --vault-name $kvName --name "ApplicationInsights-InstrumentationKey" --value $appInsightsKey | Out-Null
    az keyvault secret set --vault-name $kvName --name "JWT-Secret" --value $jwtSecret | Out-Null
    az keyvault secret set --vault-name $kvName --name "WebPush-PrivateKey" --value $vapidPrivate | Out-Null
    az keyvault secret set --vault-name $kvName --name "WebPush-PublicKey" --value $vapidPublic | Out-Null
    az keyvault secret set --vault-name $kvName --name "Stripe-SecretKey" --value $stripeSecret | Out-Null
    Write-Success "Secrets configured"
    
    # PHASE 4: App Settings
    Write-Step "PHASE 4: App Settings"
    
    $kvUri = "https://$kvName.vault.azure.net/"
    
    Write-Info "Configuring API..."
    az webapp config appsettings set --name $apiName --resource-group $rgName --settings ASPNETCORE_ENVIRONMENT=Production KeyVaultUri=$kvUri WEBSITE_RUN_FROM_PACKAGE=1 | Out-Null
    
    Write-Info "Configuring Functions..."
    az functionapp config appsettings set --name $funcName --resource-group $rgName --settings AZURE_FUNCTIONS_ENVIRONMENT=Production KeyVaultUri=$kvUri | Out-Null
    Write-Success "App settings configured"
}

# PHASE 5: Deployments
if (-not $SkipDeployments) {
    Write-Step "PHASE 5: Building and Deploying"
    
    $workspaceRoot = $PSScriptRoot | Split-Path -Parent
    
    # API
    Write-Info "Building API..."
    Push-Location "$workspaceRoot\src\AzureSmartCost.Api"
    dotnet clean --configuration Release | Out-Null
    dotnet restore | Out-Null
    dotnet build --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { Write-Host "[ERROR] Build failed" -ForegroundColor Red; exit 1 }
    dotnet publish --configuration Release --output ./bin/Publish
    if ($LASTEXITCODE -ne 0) { Write-Host "[ERROR] Publish failed" -ForegroundColor Red; exit 1 }
    Write-Success "API built"
    
    Write-Info "Creating API package..."
    Push-Location bin\Publish
    if (Test-Path api-deploy.zip) { Remove-Item api-deploy.zip -Force }
    Compress-Archive -Path * -DestinationPath api-deploy.zip -Force
    Pop-Location
    
    Write-Info "Deploying API..."
    try {
        az webapp deployment source config-zip --resource-group $rgName --name $apiName --src "bin\Publish\api-deploy.zip" --timeout 300 | Out-Null
        Write-Success "API deployed"
    } catch {
        Write-Warn "API deploy failed - use Portal to upload bin\Publish\api-deploy.zip"
    }
    Pop-Location
    
    # Functions
    Write-Info "Building Functions..."
    Push-Location "$workspaceRoot\src\AzureSmartCost.Functions"
    dotnet clean --configuration Release | Out-Null
    dotnet restore | Out-Null
    dotnet build --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { Write-Host "[ERROR] Build failed" -ForegroundColor Red; exit 1 }
    dotnet publish --configuration Release --output ./publish
    if ($LASTEXITCODE -ne 0) { Write-Host "[ERROR] Publish failed" -ForegroundColor Red; exit 1 }
    Write-Success "Functions built"
    
    Write-Info "Creating Functions package..."
    Push-Location publish
    if (Test-Path ..\functions-deploy.zip) { Remove-Item ..\functions-deploy.zip -Force }
    Compress-Archive -Path * -DestinationPath ..\functions-deploy.zip -Force
    Pop-Location
    
    Write-Info "Deploying Functions..."
    az functionapp deployment source config-zip --resource-group $rgName --name $funcName --src functions-deploy.zip --build-remote true --timeout 300 | Out-Null
    Write-Success "Functions deployed"
    Pop-Location
    
    # Frontend
    Write-Info "Building Frontend..."
    Push-Location "$workspaceRoot\smartcost-dashboard"
    
    if (-not (Test-Path node_modules)) { npm install }
    
    $envContent = @"
REACT_APP_API_URL=https://$apiName.azurewebsites.net
REACT_APP_VAPID_PUBLIC_KEY=$vapidPublic
REACT_APP_ENVIRONMENT=$Environment
"@
    Set-Content -Path .env.production.local -Value $envContent
    
    npm run build
    if ($LASTEXITCODE -ne 0) { Write-Host "[ERROR] Build failed" -ForegroundColor Red; exit 1 }
    Write-Success "Frontend built"
    Pop-Location
}

# PHASE 6: Validation
Write-Step "PHASE 6: Validation"

Write-Info "Waiting 45s for startup..."
Start-Sleep -Seconds 45

Write-Info "Testing API health..."
try {
    Invoke-RestMethod -Uri "https://$apiName.azurewebsites.net/health" -Method GET -TimeoutSec 15 | Out-Null
    Write-Success "API health check passed"
} catch {
    Write-Warn "API not responding yet"
}

Write-Info "Checking Functions..."
$functions = az functionapp function list --resource-group $rgName --name $funcName --query "[].name" -o tsv
if ($functions) {
    Write-Success "Functions deployed: $($functions -split "`n" | Measure-Object | Select -ExpandProperty Count)"
} else {
    Write-Warn "No functions detected"
}

# Summary
Write-Host "`n========================================" -ForegroundColor Green
Write-Host "DEPLOYMENT COMPLETED!" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Green

Write-Host "URLs:" -ForegroundColor Yellow
Write-Host "  API:       https://$apiName.azurewebsites.net"
Write-Host "  Functions: https://$funcName.azurewebsites.net"
Write-Host "  Key Vault: https://$kvName.vault.azure.net"
Write-Host ""
Write-Host "Resources:" -ForegroundColor Yellow
Write-Host "  RG:     $rgName"
Write-Host "  Cosmos: $cosmosName"
Write-Host "  Storage: $storageName"
Write-Host ""
Write-Host "Commands:" -ForegroundColor Cyan
Write-Host "  Logs:   az webapp log tail --name $apiName --resource-group $rgName"
Write-Host "  Delete: az group delete --name $rgName --yes --no-wait"
Write-Host ""

# Save info
$deploymentInfo = @{
    Environment = $Environment
    Location = $Location
    ResourceGroup = $rgName
    API = "https://$apiName.azurewebsites.net"
    Functions = "https://$funcName.azurewebsites.net"
    KeyVault = "https://$kvName.vault.azure.net"
} | ConvertTo-Json

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$deploymentInfo | Out-File -FilePath "deployment-$Environment-$timestamp.json"
Write-Success "Info saved: deployment-$Environment-$timestamp.json"
Write-Host ""
