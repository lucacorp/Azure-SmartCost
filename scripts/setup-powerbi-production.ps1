# Azure SmartCost - Power BI Environment Configuration Script (PowerShell)
# This script sets up all required environment variables for Power BI integration

param(
    [Parameter(Mandatory=$false)]
    [string]$AppName,
    
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup,
    
    [Parameter(Mandatory=$false)]
    [switch]$NonInteractive
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Azure SmartCost - Power BI Configuration Setup" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

# Check if Azure CLI is installed
try {
    az --version | Out-Null
}
catch {
    Write-Host "❌ Azure CLI is not installed. Please install it first." -ForegroundColor Red
    Write-Host "   Visit: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli" -ForegroundColor Yellow
    exit 1
}

# Function to prompt for input with validation
function Get-UserInput {
    param(
        [string]$Prompt,
        [bool]$Required = $true,
        [bool]$Secure = $false
    )
    
    do {
        if ($Secure) {
            $value = Read-Host -Prompt "📝 $Prompt" -AsSecureString
            $value = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($value))
        } else {
            $value = Read-Host -Prompt "📝 $Prompt"
        }
        
        if ($Required -and [string]::IsNullOrEmpty($value)) {
            Write-Host "❌ This field is required. Please try again." -ForegroundColor Red
        } else {
            Write-Host "✅ Value set" -ForegroundColor Green
            break
        }
    } while ($true)
    
    return $value
}

# Function to set app service environment variable
function Set-AppSetting {
    param(
        [string]$AppName,
        [string]$ResourceGroup,
        [string]$SettingName,
        [string]$SettingValue
    )
    
    try {
        az webapp config appsettings set --name $AppName --resource-group $ResourceGroup --settings "$SettingName=$SettingValue" --output none
        Write-Host "✅ Set $SettingName" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Failed to set $SettingName" -ForegroundColor Red
        throw
    }
}

Write-Host ""
Write-Host "🔧 Step 1: Azure App Service Information" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Get Azure App Service details
if (-not $AppName) {
    $AppName = Get-UserInput "Enter Azure App Service name"
}

if (-not $ResourceGroup) {
    $ResourceGroup = Get-UserInput "Enter Resource Group name"
}

# Check Azure login status
Write-Host ""
Write-Host "🔐 Checking Azure login status..." -ForegroundColor Yellow

try {
    az account show --output none
    Write-Host "✅ Azure login verified" -ForegroundColor Green
}
catch {
    Write-Host "🔑 Please log in to Azure..." -ForegroundColor Yellow
    az login
}

# Verify app service exists
Write-Host "🔍 Verifying App Service exists..." -ForegroundColor Yellow
try {
    az webapp show --name $AppName --resource-group $ResourceGroup --output none
    Write-Host "✅ App Service found: $AppName" -ForegroundColor Green
}
catch {
    Write-Host "❌ App Service '$AppName' not found in resource group '$ResourceGroup'" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🎯 Step 2: Azure AD Configuration" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Azure AD Configuration
$AzureTenantId = Get-UserInput "Enter Azure Tenant ID"
$AzureClientId = Get-UserInput "Enter Azure AD App Registration Client ID"
$AzureClientSecret = Get-UserInput "Enter Azure AD App Registration Client Secret" -Secure $true

Write-Host ""
Write-Host "📊 Step 3: Power BI Configuration" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Power BI Configuration
$PowerBiWorkspaceId = Get-UserInput "Enter Power BI Workspace ID"
$PowerBiDatasetId = Get-UserInput "Enter Power BI Dataset ID (optional)" -Required $false

# Use same Azure AD app for Power BI if not specified differently
Write-Host ""
$useSameApp = Read-Host "🤔 Use the same Azure AD app for Power BI? (y/N)"
if ($useSameApp -match "^[Yy]") {
    $PowerBiClientId = $AzureClientId
    $PowerBiClientSecret = $AzureClientSecret
    Write-Host "✅ Using same Azure AD app for Power BI" -ForegroundColor Green
} else {
    $PowerBiClientId = Get-UserInput "Enter Power BI Client ID"
    $PowerBiClientSecret = Get-UserInput "Enter Power BI Client Secret" -Secure $true
}

Write-Host ""
Write-Host "🗄️ Step 4: Database Configuration" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan

# Database Configuration
$CosmosDbConnectionString = Get-UserInput "Enter CosmosDB Connection String" -Secure $true

Write-Host ""
Write-Host "🔐 Step 5: Security Configuration" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Security Configuration
do {
    $JwtSecret = Get-UserInput "Enter JWT Secret (min 32 characters)" -Secure $true
    if ($JwtSecret.Length -lt 32) {
        Write-Host "❌ JWT Secret must be at least 32 characters long" -ForegroundColor Red
    }
} while ($JwtSecret.Length -lt 32)

Write-Host ""
Write-Host "🌐 Step 6: Frontend Configuration" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Frontend Configuration
$FrontendUrl = Get-UserInput "Enter Frontend URL (e.g., https://your-domain.com)"

Write-Host ""
Write-Host "📈 Step 7: Application Insights (Optional)" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Application Insights
$setupAppInsights = Read-Host "📊 Do you want to configure Application Insights? (y/N)"
$AppInsightsConnection = ""
if ($setupAppInsights -match "^[Yy]") {
    $AppInsightsConnection = Get-UserInput "Enter Application Insights Connection String" -Secure $true
}

Write-Host ""
Write-Host "🚀 Step 8: Applying Configuration" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

Write-Host "⚙️ Setting environment variables..." -ForegroundColor Yellow

try {
    # Core Azure AD Settings
    Set-AppSetting $AppName $ResourceGroup "AZURE_TENANT_ID" $AzureTenantId
    Set-AppSetting $AppName $ResourceGroup "AZURE_CLIENT_ID" $AzureClientId
    Set-AppSetting $AppName $ResourceGroup "AZURE_CLIENT_SECRET" $AzureClientSecret

    # Power BI Settings
    Set-AppSetting $AppName $ResourceGroup "POWERBI_CLIENT_ID" $PowerBiClientId
    Set-AppSetting $AppName $ResourceGroup "POWERBI_CLIENT_SECRET" $PowerBiClientSecret
    Set-AppSetting $AppName $ResourceGroup "POWERBI_WORKSPACE_ID" $PowerBiWorkspaceId

    if (-not [string]::IsNullOrEmpty($PowerBiDatasetId)) {
        Set-AppSetting $AppName $ResourceGroup "POWERBI_DATASET_ID" $PowerBiDatasetId
    }

    # Database Settings
    Set-AppSetting $AppName $ResourceGroup "COSMOSDB_CONNECTION_STRING" $CosmosDbConnectionString

    # Security Settings
    Set-AppSetting $AppName $ResourceGroup "JWT_SECRET" $JwtSecret

    # Frontend Settings
    Set-AppSetting $AppName $ResourceGroup "FRONTEND_URL" $FrontendUrl

    # Feature Flags
    Set-AppSetting $AppName $ResourceGroup "USE_REAL_POWERBI_API" "true"
    Set-AppSetting $AppName $ResourceGroup "USE_REAL_COST_API" "true"
    Set-AppSetting $AppName $ResourceGroup "FEATURE_POWERBI" "true"
    Set-AppSetting $AppName $ResourceGroup "FEATURE_COST_ALERTS" "true"
    Set-AppSetting $AppName $ResourceGroup "FEATURE_BUDGET_FORECASTING" "true"
    Set-AppSetting $AppName $ResourceGroup "FEATURE_OPTIMIZATION" "true"

    # Application Insights (if configured)
    if (-not [string]::IsNullOrEmpty($AppInsightsConnection)) {
        Set-AppSetting $AppName $ResourceGroup "APPLICATIONINSIGHTS_CONNECTION_STRING" $AppInsightsConnection
    }

    # Environment Info
    Set-AppSetting $AppName $ResourceGroup "ENVIRONMENT_NAME" "Production"
    Set-AppSetting $AppName $ResourceGroup "APP_VERSION" "1.0.0"
    Set-AppSetting $AppName $ResourceGroup "DEPLOYMENT_DATE" (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")

    Write-Host ""
    Write-Host "✅ Configuration Complete!" -ForegroundColor Green
    Write-Host "=========================" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Summary of configured settings:" -ForegroundColor Cyan
    Write-Host "  🆔 Azure Tenant ID: $AzureTenantId" -ForegroundColor White
    Write-Host "  🔑 Azure Client ID: $($AzureClientId.Substring(0, 8))..." -ForegroundColor White
    Write-Host "  📊 Power BI Workspace: $PowerBiWorkspaceId" -ForegroundColor White
    Write-Host "  🌐 Frontend URL: $FrontendUrl" -ForegroundColor White
    Write-Host "  🗄️ CosmosDB: Configured" -ForegroundColor White
    Write-Host "  🔐 JWT Secret: Configured" -ForegroundColor White
    if (-not [string]::IsNullOrEmpty($AppInsightsConnection)) {
        Write-Host "  📈 Application Insights: Configured" -ForegroundColor White
    }
    Write-Host ""
    Write-Host "🔄 Next Steps:" -ForegroundColor Yellow
    Write-Host "1. Restart your App Service for settings to take effect" -ForegroundColor White
    Write-Host "2. Monitor application logs for any configuration issues" -ForegroundColor White
    Write-Host "3. Test Power BI integration through the dashboard" -ForegroundColor White
    Write-Host "4. Set up monitoring and alerts" -ForegroundColor White
    Write-Host ""
    Write-Host "🔄 To restart the App Service, run:" -ForegroundColor Yellow
    Write-Host "   az webapp restart --name $AppName --resource-group $ResourceGroup" -ForegroundColor White
    Write-Host ""

    if (-not $NonInteractive) {
        $restartApp = Read-Host "🔄 Do you want to restart the App Service now? (y/N)"
        if ($restartApp -match "^[Yy]") {
            Write-Host "🔄 Restarting App Service..." -ForegroundColor Yellow
            az webapp restart --name $AppName --resource-group $ResourceGroup
            Write-Host "✅ App Service restarted successfully!" -ForegroundColor Green
        }
    }

    Write-Host ""
    Write-Host "🎉 Azure SmartCost Power BI integration is now configured!" -ForegroundColor Green
    Write-Host "   You can access your application at: https://$AppName.azurewebsites.net" -ForegroundColor Cyan
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "❌ Configuration failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please check the error message above and try again." -ForegroundColor Yellow
    exit 1
}