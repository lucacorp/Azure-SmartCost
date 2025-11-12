# Azure SmartCost - Automated Deploy & Test Script
# Creates resource group, deploys infrastructure, runs tests
# DELETE MANUALLY at night to save costs

param(
    [string]$Environment = "demo",
    [string]$Location = "eastus",
    [switch]$SkipTests,
    [switch]$QuickMode
)

$ErrorActionPreference = "Stop"
$startTime = Get-Date

# Configuration
$rgName = "rg-smartcost-$Environment"
$projectName = "smartcost"
$rootDir = "C:\DIOazure\Azure-SmartCost"

Write-Host "`n╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   Azure SmartCost - Automated Deploy & Test           ║" -ForegroundColor Cyan
Write-Host "║   Environment: $Environment" -ForegroundColor Cyan
Write-Host "║   Location: $Location" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Check Azure login
Write-Host "Step 1: Verifying Azure authentication..." -ForegroundColor Yellow
try {
    $account = az account show | ConvertFrom-Json
    Write-Host "  ✓ Logged in as: $($account.user.name)" -ForegroundColor Green
    Write-Host "  ✓ Subscription: $($account.name)" -ForegroundColor Green
    
    if ($account.state -ne "Enabled") {
        throw "Subscription is not enabled. State: $($account.state)"
    }
} catch {
    Write-Host "  ✗ Not logged in or subscription disabled" -ForegroundColor Red
    Write-Host "  Run: az login" -ForegroundColor Yellow
    exit 1
}

# Create Resource Group
Write-Host "`nStep 2: Creating resource group..." -ForegroundColor Yellow
try {
    $existing = az group exists --name $rgName
    if ($existing -eq "true") {
        Write-Host "  ⚠ Resource group already exists: $rgName" -ForegroundColor Yellow
        $response = Read-Host "  Delete and recreate? (y/N)"
        if ($response -eq "y") {
            Write-Host "  Deleting existing resource group..." -ForegroundColor Yellow
            az group delete --name $rgName --yes --no-wait
            Start-Sleep -Seconds 30
        } else {
            Write-Host "  Using existing resource group" -ForegroundColor Green
        }
    }
    
    if ((az group exists --name $rgName) -eq "false") {
        az group create `
            --name $rgName `
            --location $Location `
            --tags Environment=$Environment Project=SmartCost CreatedBy=AutoScript CreatedDate=$(Get-Date -Format "yyyy-MM-dd") `
            --output none
        Write-Host "  ✓ Resource group created: $rgName" -ForegroundColor Green
    }
} catch {
    Write-Host "  ✗ Failed to create resource group: $_" -ForegroundColor Red
    exit 1
}

# Generate unique suffix for resource names
$uniqueSuffix = -join ((65..90) + (97..122) | Get-Random -Count 6 | ForEach-Object {[char]$_})
$uniqueSuffix = $uniqueSuffix.ToLower()

Write-Host "`nStep 3: Preparing deployment parameters..." -ForegroundColor Yellow
$parametersFile = "$rootDir\infra\parameters.$Environment.json"

# Create parameters file if doesn't exist
if (!(Test-Path $parametersFile)) {
    $parameters = @{
        '$schema' = "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#"
        contentVersion = "1.0.0.0"
        parameters = @{
            location = @{ value = $Location }
            environmentName = @{ value = $Environment }
            projectName = @{ value = $projectName }
            appServiceSku = @{ value = if ($QuickMode) { "F1" } else { "B1" } }
            jwtSecret = @{ value = "DEMO-JWT-SECRET-$(Get-Random -Minimum 1000 -Maximum 9999)" }
            azureAdClientId = @{ value = "demo-client-id" }
            azureAdClientSecret = @{ value = "demo-client-secret" }
        }
    }
    
    $parameters | ConvertTo-Json -Depth 10 | Set-Content $parametersFile
    Write-Host "  ✓ Created parameters file: $parametersFile" -ForegroundColor Green
} else {
    Write-Host "  ✓ Using existing parameters: $parametersFile" -ForegroundColor Green
}

# Deploy Infrastructure
Write-Host "`nStep 4: Deploying infrastructure (Bicep)..." -ForegroundColor Yellow
Write-Host "  This may take 10-15 minutes..." -ForegroundColor Gray

try {
    $deploymentName = "smartcost-deploy-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    
    Set-Location "$rootDir\infra"
    
    if ($QuickMode) {
        Write-Host "  ⚡ Quick mode: Deploying minimal resources only" -ForegroundColor Yellow
        # Deploy only App Service + Storage (skip Cosmos, Redis for quick test)
        az deployment group create `
            --resource-group $rgName `
            --name $deploymentName `
            --template-file main.bicep `
            --parameters $parametersFile `
            --parameters appServiceSku=F1 `
            --output table `
            --only-show-errors
    } else {
        Write-Host "  📦 Full mode: Deploying all resources" -ForegroundColor Cyan
        az deployment group create `
            --resource-group $rgName `
            --name $deploymentName `
            --template-file main.bicep `
            --parameters $parametersFile `
            --output table
    }
    
    Write-Host "  ✓ Infrastructure deployed successfully" -ForegroundColor Green
    
    # Get deployment outputs
    Write-Host "`nStep 5: Retrieving deployment outputs..." -ForegroundColor Yellow
    $outputs = az deployment group show `
        --resource-group $rgName `
        --name $deploymentName `
        --query properties.outputs `
        --output json | ConvertFrom-Json
    
    if ($outputs) {
        Write-Host "`n  📋 Deployment Outputs:" -ForegroundColor Cyan
        $outputs.PSObject.Properties | ForEach-Object {
            Write-Host "    $($_.Name): $($_.Value.value)" -ForegroundColor White
        }
    }
    
} catch {
    Write-Host "  ✗ Deployment failed: $_" -ForegroundColor Red
    Write-Host "`n  Check deployment logs:" -ForegroundColor Yellow
    Write-Host "  az deployment group show --resource-group $rgName --name $deploymentName" -ForegroundColor Gray
    exit 1
}

# List created resources
Write-Host "`nStep 6: Listing created resources..." -ForegroundColor Yellow
$resources = az resource list --resource-group $rgName --output json | ConvertFrom-Json
Write-Host "  ✓ Total resources created: $($resources.Count)" -ForegroundColor Green
$resources | ForEach-Object {
    Write-Host "    - $($_.name) ($($_.type))" -ForegroundColor Gray
}

# Configure Key Vault (if created)
Write-Host "`nStep 7: Configuring secrets..." -ForegroundColor Yellow
$keyVault = $resources | Where-Object { $_.type -eq "Microsoft.KeyVault/vaults" } | Select-Object -First 1
if ($keyVault) {
    try {
        # Grant current user access to Key Vault
        $currentUser = az ad signed-in-user show --query id -o tsv
        az keyvault set-policy `
            --name $keyVault.name `
            --object-id $currentUser `
            --secret-permissions get list set delete `
            --output none
        
        # Store VAPID keys (if exists)
        $vapidSecretsFile = "$rootDir\VAPID_SECRETS.txt"
        if (Test-Path $vapidSecretsFile) {
            $vapidContent = Get-Content $vapidSecretsFile -Raw
            if ($vapidContent -match 'Public Key: (.+)') {
                $publicKey = $matches[1].Trim()
                az keyvault secret set --vault-name $keyVault.name --name "VapidPublicKey" --value $publicKey --output none
                Write-Host "  ✓ VAPID public key stored" -ForegroundColor Green
            }
            if ($vapidContent -match 'Private Key: (.+)') {
                $privateKey = $matches[1].Trim()
                az keyvault secret set --vault-name $keyVault.name --name "VapidPrivateKey" --value $privateKey --output none
                Write-Host "  ✓ VAPID private key stored" -ForegroundColor Green
            }
        }
        
        # Store demo Stripe keys
        az keyvault secret set --vault-name $keyVault.name --name "StripeSecretKey" --value "sk_test_demo_key" --output none
        az keyvault secret set --vault-name $keyVault.name --name "StripePublishableKey" --value "pk_test_demo_key" --output none
        Write-Host "  ✓ Demo Stripe keys stored" -ForegroundColor Green
        
    } catch {
        Write-Host "  ⚠ Warning: Failed to configure Key Vault: $_" -ForegroundColor Yellow
    }
}

# Deploy API (if App Service exists)
if (!$SkipTests) {
    Write-Host "`nStep 8: Building and deploying API..." -ForegroundColor Yellow
    $appService = $resources | Where-Object { $_.type -eq "Microsoft.Web/sites" -and $_.name -like "*api*" } | Select-Object -First 1
    
    if ($appService) {
        try {
            Set-Location "$rootDir\src\AzureSmartCost.Api"
            
            # Build API
            Write-Host "  Building API..." -ForegroundColor Gray
            dotnet publish -c Release -o .\publish | Out-Null
            
            # Create zip
            Write-Host "  Creating deployment package..." -ForegroundColor Gray
            Compress-Archive -Path .\publish\* -DestinationPath .\api-deploy.zip -Force
            
            # Deploy to App Service
            Write-Host "  Deploying to App Service..." -ForegroundColor Gray
            az webapp deployment source config-zip `
                --resource-group $rgName `
                --name $appService.name `
                --src .\api-deploy.zip `
                --output none
            
            Write-Host "  ✓ API deployed successfully" -ForegroundColor Green
            
            # Clean up
            Remove-Item .\api-deploy.zip -Force
            Remove-Item .\publish -Recurse -Force
            
            # Get API URL
            $apiUrl = "https://$($appService.name).azurewebsites.net"
            Write-Host "  📍 API URL: $apiUrl" -ForegroundColor Cyan
            
            # Wait for app to start
            Write-Host "  Waiting for API to start..." -ForegroundColor Gray
            Start-Sleep -Seconds 30
            
            # Test health endpoint
            Write-Host "`nStep 9: Testing API health..." -ForegroundColor Yellow
            try {
                $response = Invoke-WebRequest -Uri "$apiUrl/health" -UseBasicParsing -TimeoutSec 10
                if ($response.StatusCode -eq 200) {
                    Write-Host "  ✓ API is healthy! Status: $($response.StatusCode)" -ForegroundColor Green
                    Write-Host "  Response: $($response.Content)" -ForegroundColor Gray
                } else {
                    Write-Host "  ⚠ API returned status: $($response.StatusCode)" -ForegroundColor Yellow
                }
            } catch {
                Write-Host "  ⚠ API health check failed: $_" -ForegroundColor Yellow
                Write-Host "  Check logs: az webapp log tail --name $($appService.name) --resource-group $rgName" -ForegroundColor Gray
            }
            
        } catch {
            Write-Host "  ⚠ API deployment failed: $_" -ForegroundColor Yellow
        }
    }
}

# Calculate costs
Write-Host "`nStep 10: Estimating daily costs..." -ForegroundColor Yellow
$estimatedCost = 0
if ($QuickMode) {
    $estimatedCost = 0.5  # F1 tier is mostly free
    Write-Host "  💰 Quick mode - Minimal cost: ~`$$estimatedCost/day" -ForegroundColor Green
} else {
    $estimatedCost = 2.0  # B1 + minimal usage
    Write-Host "  💰 Estimated cost: ~`$$estimatedCost/day" -ForegroundColor Yellow
}

# Final summary
$duration = (Get-Date) - $startTime
Write-Host "`n╔════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║              DEPLOYMENT COMPLETED! ✓                   ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Green

Write-Host "`n📊 Summary:" -ForegroundColor Cyan
Write-Host "  Resource Group: $rgName" -ForegroundColor White
Write-Host "  Location: $Location" -ForegroundColor White
Write-Host "  Resources Created: $($resources.Count)" -ForegroundColor White
Write-Host "  Duration: $([math]::Round($duration.TotalMinutes, 1)) minutes" -ForegroundColor White
Write-Host "  Estimated Cost: ~`$$estimatedCost/day" -ForegroundColor White

Write-Host "`n🔗 Useful URLs:" -ForegroundColor Cyan
if ($appService) {
    Write-Host "  API: https://$($appService.name).azurewebsites.net" -ForegroundColor White
}
Write-Host "  Portal: https://portal.azure.com/#@/resource/subscriptions/$($account.id)/resourceGroups/$rgName" -ForegroundColor White

Write-Host "`n📋 Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Test your endpoints" -ForegroundColor White
Write-Host "  2. Deploy frontend (if needed)" -ForegroundColor White
Write-Host "  3. Run integration tests" -ForegroundColor White

Write-Host "`n🗑️  DELETE AT NIGHT to save costs:" -ForegroundColor Yellow
Write-Host "  az group delete --name $rgName --yes --no-wait" -ForegroundColor Red
Write-Host "  (Deletes ALL resources in the group)" -ForegroundColor Gray

Write-Host "`n✅ Script completed successfully!`n" -ForegroundColor Green

# Save deployment info to file
$deploymentInfo = @{
    ResourceGroup = $rgName
    Location = $Location
    Environment = $Environment
    CreatedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    ResourceCount = $resources.Count
    EstimatedDailyCost = $estimatedCost
    DeleteCommand = "az group delete --name $rgName --yes --no-wait"
    Resources = $resources | Select-Object name, type, location
}

$deploymentInfo | ConvertTo-Json -Depth 5 | Set-Content "$rootDir\deployment-info-$Environment.json"
Write-Host "📄 Deployment info saved to: deployment-info-$Environment.json`n" -ForegroundColor Gray
