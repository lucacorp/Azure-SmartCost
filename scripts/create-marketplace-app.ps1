param(
    [string]$AppName = "SmartCost-Marketplace-API",
    [string]$LandingUrl = "https://smartcost-func-beta.azurewebsites.net/api/marketplace/landing"
)

Write-Host "Creating Marketplace App Registration..." -ForegroundColor Cyan

$account = az account show | ConvertFrom-Json
Write-Host "Tenant: $($account.tenantId)" -ForegroundColor Green

# Microsoft.Marketplace API permissions
$manifest = @"
[{"resourceAppId":"62d94f6c-d599-489b-a797-3e4d03f39e5b","resourceAccess":[{"id":"62d0a1bd-ffc9-443c-8120-2ba9ad90f1c7","type":"Scope"}]}]
"@

$manifest | Out-File "temp-manifest.json" -Encoding UTF8

$app = az ad app create --display-name $AppName --sign-in-audience "AzureADMyOrg" --web-redirect-uris $LandingUrl --required-resource-accesses "temp-manifest.json" --output json | ConvertFrom-Json

Remove-Item "temp-manifest.json" -Force

az ad sp create --id $app.appId | Out-Null

$secret = az ad app credential reset --id $app.appId --append --display-name "MarketplaceSecret" --output json | ConvertFrom-Json

Write-Host ""
Write-Host "=== SAVE THESE VALUES ===" -ForegroundColor Yellow
Write-Host "Tenant ID: $($account.tenantId)" -ForegroundColor White
Write-Host "Client ID: $($app.appId)" -ForegroundColor White
Write-Host "Client Secret: $($secret.password)" -ForegroundColor White
Write-Host "Expires: $($secret.endDateTime)" -ForegroundColor Gray
Write-Host ""

$config = @{
    TenantId = $account.tenantId
    ClientId = $app.appId
    ClientSecret = $secret.password
    LandingPageUrl = $LandingUrl
    WebhookUrl = "https://smartcost-func-beta.azurewebsites.net/api/marketplace/webhook"
    Created = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
}

$config | ConvertTo-Json | Out-File "marketplace-config.json" -Encoding UTF8
Write-Host "Config saved to: marketplace-config.json" -ForegroundColor Green
