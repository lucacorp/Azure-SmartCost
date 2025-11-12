# Delete Azure SmartCost Demo Environment
# Run this at night to save costs

param(
    [string]$Environment = "demo",
    [switch]$Force
)

$rgName = "rg-smartcost-$Environment"

Write-Host "`n⚠️  DELETING RESOURCE GROUP: $rgName" -ForegroundColor Red
Write-Host "This will delete ALL resources permanently!`n" -ForegroundColor Yellow

# Check if resource group exists
$exists = az group exists --name $rgName
if ($exists -eq "false") {
    Write-Host "✓ Resource group doesn't exist: $rgName" -ForegroundColor Green
    Write-Host "Nothing to delete.`n" -ForegroundColor Gray
    exit 0
}

# Show resources that will be deleted
Write-Host "📋 Resources to be DELETED:" -ForegroundColor Yellow
az resource list --resource-group $rgName --query "[].{Name:name, Type:type}" -o table

# Show estimated savings
Write-Host "`n💰 Estimated savings: ~$2-5 per day" -ForegroundColor Green

# Confirm deletion
if (!$Force) {
    $response = Read-Host "`nAre you SURE you want to delete ALL resources? (yes/NO)"
    if ($response -ne "yes") {
        Write-Host "`n❌ Deletion cancelled" -ForegroundColor Yellow
        exit 0
    }
}

# Delete resource group
Write-Host "`n🗑️  Deleting resource group..." -ForegroundColor Red
az group delete --name $rgName --yes --no-wait

Write-Host "✓ Deletion started (running in background)" -ForegroundColor Green
Write-Host "`nThe resource group will be fully deleted in ~5-10 minutes." -ForegroundColor Gray
Write-Host "You can close this window.`n" -ForegroundColor Gray

# Verify deletion started
Start-Sleep -Seconds 5
$state = az group show --name $rgName --query "properties.provisioningState" -o tsv 2>$null
if ($state -eq "Deleting") {
    Write-Host "✓ Deletion in progress: $rgName`n" -ForegroundColor Green
} else {
    Write-Host "⚠️  Check deletion status:" -ForegroundColor Yellow
    Write-Host "az group show --name $rgName`n" -ForegroundColor Gray
}
