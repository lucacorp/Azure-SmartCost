# Script Automatizado - Criação de Azure AD App Registration para Power BI
# Este script cria e configura automaticamente o App Registration necessário

param(
    [Parameter(Mandatory=$false)]
    [string]$AppName = "AzureSmartCost-PowerBI",
    
    [Parameter(Mandatory=$false)]
    [string]$RedirectUri = "https://your-app.azurewebsites.net/auth/callback",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipPermissions
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Azure AD App Registration - Azure SmartCost Power BI" -ForegroundColor Green
Write-Host "=========================================================" -ForegroundColor Green
Write-Host ""

# Check if Azure CLI is installed
try {
    az --version | Out-Null
    Write-Host "✅ Azure CLI verified" -ForegroundColor Green
}
catch {
    Write-Host "❌ Azure CLI is not installed. Please install it first." -ForegroundColor Red
    exit 1
}

# Check Azure login
Write-Host "🔐 Checking Azure login status..." -ForegroundColor Yellow
try {
    $account = az account show | ConvertFrom-Json
    Write-Host "✅ Logged in as: $($account.user.name)" -ForegroundColor Green
    Write-Host "📍 Tenant: $($account.tenantId)" -ForegroundColor Cyan
}
catch {
    Write-Host "🔑 Please log in to Azure..." -ForegroundColor Yellow
    az login
    $account = az account show | ConvertFrom-Json
}

Write-Host ""
Write-Host "📝 Configuration:" -ForegroundColor Cyan
Write-Host "App Name: $AppName" -ForegroundColor White
Write-Host "Redirect URI: $RedirectUri" -ForegroundColor White
Write-Host "Tenant ID: $($account.tenantId)" -ForegroundColor White
Write-Host ""

# Confirm before proceeding
$confirm = Read-Host "🤔 Proceed with App Registration creation? (y/N)"
if ($confirm -notmatch "^[Yy]") {
    Write-Host "❌ Operation cancelled" -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "🔨 Step 1: Creating Azure AD App Registration..." -ForegroundColor Cyan

try {
    # Create App Registration
    $appManifest = @"
[
    {
        "resourceAppId": "00000009-0000-0000-c000-000000000000",
        "resourceAccess": [
            {
                "id": "7f33e027-4039-419b-938e-2f8ca153e68e",
                "type": "Scope"
            },
            {
                "id": "b2f1b2fa-f35c-407c-979c-a858a808ba85",
                "type": "Scope"
            },
            {
                "id": "d56682ec-c09e-4743-aaf4-1a3aac4caa21",
                "type": "Scope"
            },
            {
                "id": "f3076109-ca66-412a-be10-d4ee1be95d47",
                "type": "Scope"
            },
            {
                "id": "2448370f-f988-42cd-909c-6528336b5a5a",
                "type": "Scope"
            }
        ]
    }
]
"@

    $manifestFile = "temp-manifest.json"
    $appManifest | Out-File -FilePath $manifestFile -Encoding UTF8

    $appRegistration = az ad app create `
        --display-name $AppName `
        --web-redirect-uris $RedirectUri `
        --required-resource-accesses $manifestFile `
        --output json | ConvertFrom-Json

    Remove-Item $manifestFile -Force

    Write-Host "✅ App Registration created successfully!" -ForegroundColor Green
    Write-Host "📋 Application ID: $($appRegistration.appId)" -ForegroundColor Cyan
    Write-Host "📋 Object ID: $($appRegistration.id)" -ForegroundColor Cyan
    
    # Create Service Principal
    Write-Host ""
    Write-Host "🔨 Step 2: Creating Service Principal..." -ForegroundColor Cyan
    
    $servicePrincipal = az ad sp create --id $appRegistration.appId --output json | ConvertFrom-Json
    Write-Host "✅ Service Principal created!" -ForegroundColor Green
    Write-Host "📋 Service Principal ID: $($servicePrincipal.id)" -ForegroundColor Cyan

    # Create Client Secret
    Write-Host ""
    Write-Host "🔨 Step 3: Creating Client Secret..." -ForegroundColor Cyan
    
    $clientSecret = az ad app credential reset --id $appRegistration.appId --append --display-name "AzureSmartCost-Secret" --output json | ConvertFrom-Json
    Write-Host "✅ Client Secret created!" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "🎯 IMPORTANT CREDENTIALS - SAVE THESE VALUES:" -ForegroundColor Yellow
    Write-Host "=============================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "🆔 Tenant ID: $($account.tenantId)" -ForegroundColor White
    Write-Host "🔑 Application (Client) ID: $($appRegistration.appId)" -ForegroundColor White  
    Write-Host "🔐 Client Secret: $($clientSecret.password)" -ForegroundColor White
    Write-Host "⏰ Secret Expires: $($clientSecret.endDateTime)" -ForegroundColor Gray
    Write-Host ""
    
    # Grant admin consent if not skipped
    if (-not $SkipPermissions) {
        Write-Host "🔨 Step 4: Granting Admin Consent..." -ForegroundColor Cyan
        Write-Host "⚠️ This requires Global Administrator permissions" -ForegroundColor Yellow
        
        $grantConsent = Read-Host "🤔 Grant admin consent for Power BI permissions? (y/N)"
        if ($grantConsent -match "^[Yy]") {
            try {
                az ad app permission admin-consent --id $appRegistration.appId
                Write-Host "✅ Admin consent granted!" -ForegroundColor Green
            }
            catch {
                Write-Host "⚠️ Could not grant admin consent automatically." -ForegroundColor Yellow
                Write-Host "   Please grant consent manually in Azure Portal:" -ForegroundColor White
                Write-Host "   https://portal.azure.com/#view/Microsoft_AAD_IAM/ManagedAppMenuBlade/~/Permissions/appId/$($appRegistration.appId)" -ForegroundColor Gray
            }
        }
    }

    Write-Host ""
    Write-Host "📋 Next Steps:" -ForegroundColor Cyan
    Write-Host "==============" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. Save the credentials above in a secure location" -ForegroundColor White
    Write-Host "2. Create Power BI Workspace:" -ForegroundColor White
    Write-Host "   • Go to https://app.powerbi.com" -ForegroundColor Gray
    Write-Host "   • Create new workspace: 'Azure SmartCost Analytics'" -ForegroundColor Gray
    Write-Host "   • Add the service principal as Admin/Contributor" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Configure environment variables in App Service:" -ForegroundColor White
    Write-Host "   AZURE_TENANT_ID=$($account.tenantId)" -ForegroundColor Gray
    Write-Host "   AZURE_CLIENT_ID=$($appRegistration.appId)" -ForegroundColor Gray  
    Write-Host "   AZURE_CLIENT_SECRET=$($clientSecret.password)" -ForegroundColor Gray
    Write-Host "   POWERBI_CLIENT_ID=$($appRegistration.appId)" -ForegroundColor Gray
    Write-Host "   POWERBI_CLIENT_SECRET=$($clientSecret.password)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "4. Run the configuration script:" -ForegroundColor White
    Write-Host "   .\scripts\setup-powerbi-production.ps1" -ForegroundColor Gray
    Write-Host ""

    Write-Host "✅ Azure AD App Registration completed successfully!" -ForegroundColor Green
    
    # Save configuration to file
    $configOutput = @{
        TenantId = $account.tenantId
        ClientId = $appRegistration.appId
        ClientSecret = $clientSecret.password
        SecretExpiry = $clientSecret.endDateTime
        ServicePrincipalId = $servicePrincipal.id
        CreatedDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
    
    $configOutput | ConvertTo-Json -Depth 2 | Out-File "azure-ad-config.json" -Encoding UTF8
    Write-Host "💾 Configuration saved to: azure-ad-config.json" -ForegroundColor Cyan
}
catch {
    Write-Host ""
    Write-Host "❌ Error creating App Registration!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "🔧 Manual steps:" -ForegroundColor Yellow
    Write-Host "1. Go to Azure Portal: https://portal.azure.com" -ForegroundColor White
    Write-Host "2. Navigate to Azure Active Directory > App registrations" -ForegroundColor White
    Write-Host "3. Click 'New registration'" -ForegroundColor White
    Write-Host "4. Follow the manual guide in POWERBI_SETUP.md" -ForegroundColor White
    exit 1
}