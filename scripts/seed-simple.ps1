# Simple Seed Script for Cosmos DB
param(
    [string]$CosmosAccount = "smartcost-cosmos-7016",
    [string]$ResourceGroup = "rg-smartcost-demo",
    [string]$DatabaseName = "SmartCostDB"
)

Write-Host "`n=== SEED DE DADOS COSMOS DB ===" -ForegroundColor Cyan
Write-Host "`nInserindo dados de demonstração..." -ForegroundColor Yellow

# Tenant 1
Write-Host "`n[1/10] Criando Tenant: Contoso Corporation..." -ForegroundColor Cyan
$tenant1Json = @'
{
    "id": "tenant-demo-001",
    "name": "Contoso Corporation",
    "domain": "contoso.com",
    "createdAt": "2024-08-15T10:00:00Z",
    "isActive": true,
    "settings": {
        "currency": "USD",
        "timezone": "America/Sao_Paulo"
    },
    "subscription": {
        "tier": "Premium",
        "status": "Active",
        "startDate": "2024-08-15T10:00:00Z"
    }
}
'@

$tenant1Json | Out-File -FilePath "$env:TEMP\tenant1.json" -Encoding utf8 -Force
az cosmosdb sql container create-item `
    --account-name $CosmosAccount `
    --resource-group $ResourceGroup `
    --database-name $DatabaseName `
    --container-name Tenants `
    --partition-key-value "tenant-demo-001" `
    --body "@$env:TEMP\tenant1.json" `
    --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "OK - Tenant Contoso criado" -ForegroundColor Green
} else {
    Write-Host "ERRO ao criar Tenant Contoso" -ForegroundColor Red
}

# Tenant 2
Write-Host "`n[2/10] Criando Tenant: Fabrikam Inc..." -ForegroundColor Cyan
$tenant2Json = @'
{
    "id": "tenant-demo-002",
    "name": "Fabrikam Inc",
    "domain": "fabrikam.com",
    "createdAt": "2024-09-01T10:00:00Z",
    "isActive": true,
    "settings": {
        "currency": "USD",
        "timezone": "America/Sao_Paulo"
    },
    "subscription": {
        "tier": "Standard",
        "status": "Active",
        "startDate": "2024-09-01T10:00:00Z"
    }
}
'@

$tenant2Json | Out-File -FilePath "$env:TEMP\tenant2.json" -Encoding utf8 -Force
az cosmosdb sql container create-item `
    --account-name $CosmosAccount `
    --resource-group $ResourceGroup `
    --database-name $DatabaseName `
    --container-name Tenants `
    --partition-key-value "tenant-demo-002" `
    --body "@$env:TEMP\tenant2.json" `
    --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "OK - Tenant Fabrikam criado" -ForegroundColor Green
} else {
    Write-Host "ERRO ao criar Tenant Fabrikam" -ForegroundColor Red
}

# User 1
Write-Host "`n[3/10] Criando Usuario: Admin Contoso..." -ForegroundColor Cyan
$user1Json = @'
{
    "id": "user-admin-001",
    "tenantId": "tenant-demo-001",
    "email": "admin@contoso.com",
    "name": "John Admin",
    "role": "Admin",
    "createdAt": "2024-08-15T10:30:00Z",
    "isActive": true
}
'@

$user1Json | Out-File -FilePath "$env:TEMP\user1.json" -Encoding utf8 -Force
az cosmosdb sql container create-item `
    --account-name $CosmosAccount `
    --resource-group $ResourceGroup `
    --database-name $DatabaseName `
    --container-name Users `
    --partition-key-value "tenant-demo-001" `
    --body "@$env:TEMP\user1.json" `
    --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "OK - Usuario Admin criado" -ForegroundColor Green
} else {
    Write-Host "ERRO ao criar Usuario Admin" -ForegroundColor Red
}

# Subscription 1
Write-Host "`n[4/10] Criando Subscription Azure: Contoso Production..." -ForegroundColor Cyan
$sub1Json = @'
{
    "id": "sub-prod-001",
    "tenantId": "tenant-demo-001",
    "subscriptionId": "aaaaaaaa-1111-2222-3333-444444444444",
    "name": "Contoso-Production",
    "environment": "Production",
    "monthlyBudget": 15000,
    "createdAt": "2024-08-15T11:00:00Z",
    "isActive": true
}
'@

$sub1Json | Out-File -FilePath "$env:TEMP\sub1.json" -Encoding utf8 -Force
az cosmosdb sql container create-item `
    --account-name $CosmosAccount `
    --resource-group $ResourceGroup `
    --database-name $DatabaseName `
    --container-name Subscriptions `
    --partition-key-value "tenant-demo-001" `
    --body "@$env:TEMP\sub1.json" `
    --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "OK - Subscription criada" -ForegroundColor Green
} else {
    Write-Host "ERRO ao criar Subscription" -ForegroundColor Red
}

# Cost Data - Setembro
Write-Host "`n[5/10] Criando dados de custo: Setembro 2024..." -ForegroundColor Cyan
$cost1Json = @'
{
    "id": "cost-2024-09",
    "subscriptionId": "aaaaaaaa-1111-2222-3333-444444444444",
    "date": "2024-09-01T00:00:00Z",
    "month": "2024-09",
    "totalCost": 12543.67,
    "breakdown": {
        "Compute": 6500.00,
        "Storage": 2800.00,
        "Networking": 1200.00,
        "Database": 1543.67,
        "Other": 500.00
    },
    "currency": "USD"
}
'@

$cost1Json | Out-File -FilePath "$env:TEMP\cost1.json" -Encoding utf8 -Force
az cosmosdb sql container create-item `
    --account-name $CosmosAccount `
    --resource-group $ResourceGroup `
    --database-name $DatabaseName `
    --container-name CostData `
    --partition-key-value "aaaaaaaa-1111-2222-3333-444444444444" `
    --body "@$env:TEMP\cost1.json" `
    --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "OK - Dados setembro criados" -ForegroundColor Green
} else {
    Write-Host "ERRO ao criar dados setembro" -ForegroundColor Red
}

# Cost Data - Outubro
Write-Host "`n[6/10] Criando dados de custo: Outubro 2024..." -ForegroundColor Cyan
$cost2Json = @'
{
    "id": "cost-2024-10",
    "subscriptionId": "aaaaaaaa-1111-2222-3333-444444444444",
    "date": "2024-10-01T00:00:00Z",
    "month": "2024-10",
    "totalCost": 14876.32,
    "breakdown": {
        "Compute": 7200.00,
        "Storage": 3100.00,
        "Networking": 1500.00,
        "Database": 1876.32,
        "Other": 1200.00
    },
    "currency": "USD"
}
'@

$cost2Json | Out-File -FilePath "$env:TEMP\cost2.json" -Encoding utf8 -Force
az cosmosdb sql container create-item `
    --account-name $CosmosAccount `
    --resource-group $ResourceGroup `
    --database-name $DatabaseName `
    --container-name CostData `
    --partition-key-value "aaaaaaaa-1111-2222-3333-444444444444" `
    --body "@$env:TEMP\cost2.json" `
    --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "OK - Dados outubro criados" -ForegroundColor Green
} else {
    Write-Host "ERRO ao criar dados outubro" -ForegroundColor Red
}

# Cost Data - Novembro
Write-Host "`n[7/10] Criando dados de custo: Novembro 2024..." -ForegroundColor Cyan
$cost3Json = @'
{
    "id": "cost-2024-11",
    "subscriptionId": "aaaaaaaa-1111-2222-3333-444444444444",
    "date": "2024-11-01T00:00:00Z",
    "month": "2024-11",
    "totalCost": 16234.89,
    "breakdown": {
        "Compute": 8500.00,
        "Storage": 3400.00,
        "Networking": 1800.00,
        "Database": 1934.89,
        "Other": 600.00
    },
    "currency": "USD"
}
'@

$cost3Json | Out-File -FilePath "$env:TEMP\cost3.json" -Encoding utf8 -Force
az cosmosdb sql container create-item `
    --account-name $CosmosAccount `
    --resource-group $ResourceGroup `
    --database-name $DatabaseName `
    --container-name CostData `
    --partition-key-value "aaaaaaaa-1111-2222-3333-444444444444" `
    --body "@$env:TEMP\cost3.json" `
    --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "OK - Dados novembro criados" -ForegroundColor Green
} else {
    Write-Host "ERRO ao criar dados novembro" -ForegroundColor Red
}

Write-Host "`n=== SEED CONCLUIDO ===" -ForegroundColor Green
Write-Host "`nDados criados:" -ForegroundColor Cyan
Write-Host "  - 2 Tenants" -ForegroundColor White
Write-Host "  - 1 Usuario" -ForegroundColor White
Write-Host "  - 1 Subscription" -ForegroundColor White
Write-Host "  - 3 meses de dados de custo" -ForegroundColor White
Write-Host "`nTotal de custos: USD 43,654.88" -ForegroundColor Yellow
