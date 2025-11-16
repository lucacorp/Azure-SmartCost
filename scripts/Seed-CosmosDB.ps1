param(
    [Parameter(Mandatory=$false)]
    [string]$CosmosConnectionString
)

Write-Host "`n🚀 Azure SmartCost - Seed de Dados" -ForegroundColor Cyan
Write-Host "=" * 50 -ForegroundColor Cyan

# Obter connection string se não fornecida
if ([string]::IsNullOrEmpty($CosmosConnectionString)) {
    Write-Host "`n🔑 Obtendo connection string do Cosmos DB..." -ForegroundColor Yellow
    $CosmosConnectionString = az cosmosdb keys list --name smartcost-cosmos-7016 --resource-group rg-smartcost-demo --type connection-strings --query "connectionStrings[0].connectionString" -o tsv
    Write-Host "✅ Connection string obtida" -ForegroundColor Green
}

# Parse connection string
$connParts = @{}
$CosmosConnectionString.Split(';') | ForEach-Object {
    if ($_ -match '(.+?)=(.+)') {
        $connParts[$matches[1]] = $matches[2]
    }
}

$accountEndpoint = $connParts['AccountEndpoint']
$accountKey = $connParts['AccountKey']
$databaseName = "SmartCostDB"

Write-Host "`n📡 Conectando ao Cosmos DB..." -ForegroundColor Yellow
Write-Host "   Endpoint: $accountEndpoint" -ForegroundColor Gray

# Função para criar item no Cosmos DB
function New-CosmosDbItem {
    param(
        [string]$Container,
        [string]$PartitionKey,
        [hashtable]$Item
    )
    
    $uri = "$accountEndpoint/dbs/$databaseName/colls/$Container/docs"
    $date = [DateTime]::UtcNow.ToString("r")
    $method = "POST"
    $resourceType = "docs"
    $resourceLink = "dbs/$databaseName/colls/$Container"
    
    # Gerar signature
    $keyBytes = [Convert]::FromBase64String($accountKey)
    $text = "$($method.ToLower())`n$($resourceType.ToLower())`n$resourceLink`n$($date.ToLower())`n`n"
    $body = [Text.Encoding]::UTF8.GetBytes($text)
    $hmac = New-Object System.Security.Cryptography.HMACSHA256 -ArgumentList @(,$keyBytes)
    $hash = $hmac.ComputeHash($body)
    $signature = [Convert]::ToBase64String($hash)
    $authHeader = [Uri]::EscapeDataString("type=master&ver=1.0&sig=$signature")
    
    $headers = @{
        "Authorization" = $authHeader
        "x-ms-date" = $date
        "x-ms-version" = "2018-12-31"
        "x-ms-documentdb-partitionkey" = "[$([System.Web.HttpUtility]::JavaScriptStringEncode($PartitionKey, $true))]"
        "Content-Type" = "application/json"
    }
    
    $json = $Item | ConvertTo-Json -Depth 10 -Compress
    
    try {
        Invoke-RestMethod -Uri $uri -Method $method -Headers $headers -Body $json -ContentType "application/json" | Out-Null
        return $true
    }
    catch {
        Write-Host "      ⚠️  Erro: $($_.Exception.Message)" -ForegroundColor Yellow
        return $false
    }
}

# Seed Tenants
Write-Host "`n📁 Criando Tenants..." -ForegroundColor Yellow
$tenants = @(
    @{ id = "tenant-contoso"; name = "Contoso Ltd"; createdAt = (Get-Date).AddMonths(-6).ToString("o"); plan = "Premium"; isActive = $true },
    @{ id = "tenant-fabrikam"; name = "Fabrikam Inc"; createdAt = (Get-Date).AddMonths(-3).ToString("o"); plan = "Professional"; isActive = $true },
    @{ id = "tenant-adventure"; name = "Adventure Works"; createdAt = (Get-Date).AddMonths(-1).ToString("o"); plan = "Starter"; isActive = $true }
)

foreach ($tenant in $tenants) {
    if (New-CosmosDbItem -Container "Tenants" -PartitionKey $tenant.id -Item $tenant) {
        Write-Host "   ✅ $($tenant.name)" -ForegroundColor Green
    }
}

# Seed Users
Write-Host "`n👥 Criando Usuários..." -ForegroundColor Yellow
$users = @(
    @{ id = (New-Guid).ToString(); tenantId = "tenant-contoso"; email = "admin@contoso.com"; name = "John Admin"; role = "Admin" },
    @{ id = (New-Guid).ToString(); tenantId = "tenant-contoso"; email = "finance@contoso.com"; name = "Mary Finance"; role = "FinanceManager" },
    @{ id = (New-Guid).ToString(); tenantId = "tenant-contoso"; email = "analyst@contoso.com"; name = "Bob Analyst"; role = "CostAnalyst" },
    @{ id = (New-Guid).ToString(); tenantId = "tenant-fabrikam"; email = "cfo@fabrikam.com"; name = "Alice CFO"; role = "Admin" },
    @{ id = (New-Guid).ToString(); tenantId = "tenant-fabrikam"; email = "dev@fabrikam.com"; name = "Charlie Dev"; role = "Viewer" },
    @{ id = (New-Guid).ToString(); tenantId = "tenant-adventure"; email = "owner@adventure.com"; name = "Diana Owner"; role = "Admin" },
    @{ id = (New-Guid).ToString(); tenantId = "tenant-adventure"; email = "ops@adventure.com"; name = "Eve Ops"; role = "CostAnalyst" },
    @{ id = (New-Guid).ToString(); tenantId = "tenant-adventure"; email = "intern@adventure.com"; name = "Frank Intern"; role = "Viewer" }
)

foreach ($user in $users) {
    if (New-CosmosDbItem -Container "Users" -PartitionKey $user.tenantId -Item $user) {
        Write-Host "   ✅ $($user.name) ($($user.email))" -ForegroundColor Green
    }
}

# Seed Subscriptions
Write-Host "`n☁️  Criando Subscrições Azure..." -ForegroundColor Yellow
$subscriptions = @(
    @{ id = "sub-contoso-prod"; tenantId = "tenant-contoso"; name = "Contoso Production"; azureSubscriptionId = (New-Guid).ToString(); monthlyBudget = 15000; currency = "USD" },
    @{ id = "sub-contoso-dev"; tenantId = "tenant-contoso"; name = "Contoso Development"; azureSubscriptionId = (New-Guid).ToString(); monthlyBudget = 5000; currency = "USD" },
    @{ id = "sub-fabrikam-main"; tenantId = "tenant-fabrikam"; name = "Fabrikam Main"; azureSubscriptionId = (New-Guid).ToString(); monthlyBudget = 8000; currency = "USD" },
    @{ id = "sub-fabrikam-test"; tenantId = "tenant-fabrikam"; name = "Fabrikam Test"; azureSubscriptionId = (New-Guid).ToString(); monthlyBudget = 2000; currency = "USD" },
    @{ id = "sub-adventure-prod"; tenantId = "tenant-adventure"; name = "Adventure Production"; azureSubscriptionId = (New-Guid).ToString(); monthlyBudget = 3000; currency = "USD" },
    @{ id = "sub-adventure-dev"; tenantId = "tenant-adventure"; name = "Adventure Dev/Test"; azureSubscriptionId = (New-Guid).ToString(); monthlyBudget = 1000; currency = "USD" }
)

foreach ($sub in $subscriptions) {
    if (New-CosmosDbItem -Container "Subscriptions" -PartitionKey $sub.tenantId -Item $sub) {
        Write-Host "   ✅ $($sub.name) (Budget: `$$($sub.monthlyBudget))" -ForegroundColor Green
    }
}

# Seed Cost Data (últimos 90 dias)
Write-Host "`n💰 Criando dados de custo (últimos 90 dias)..." -ForegroundColor Yellow
Write-Host "   ⏳ Isso pode demorar alguns minutos..." -ForegroundColor Gray

$services = @("Virtual Machines", "App Service", "Storage", "Cosmos DB", "SQL Database", "Networking", "Functions", "Monitor")
$subscriptionCosts = @(
    @{ id = "sub-contoso-prod"; min = 450; max = 550 },
    @{ id = "sub-contoso-dev"; min = 150; max = 200 },
    @{ id = "sub-fabrikam-main"; min = 240; max = 300 },
    @{ id = "sub-fabrikam-test"; min = 60; max = 80 },
    @{ id = "sub-adventure-prod"; min = 90; max = 110 },
    @{ id = "sub-adventure-dev"; min = 30; max = 40 }
)

$totalRecords = 0
$startDate = (Get-Date).AddDays(-90)

foreach ($subCost in $subscriptionCosts) {
    Write-Host "   📊 Processando $($subCost.id)..." -ForegroundColor Cyan
    
    for ($day = 0; $day -lt 90; $day++) {
        $date = $startDate.AddDays($day)
        $dailyCost = $subCost.min + ((Get-Random -Maximum 1000) / 1000.0 * ($subCost.max - $subCost.min))
        
        # Distribuir custo entre 3 serviços principais por dia
        $selectedServices = $services | Get-Random -Count 3
        
        foreach ($service in $selectedServices) {
            $cost = [Math]::Round($dailyCost / 3 * (0.8 + (Get-Random -Maximum 40) / 100.0), 2)
            
            $costRecord = @{
                id = (New-Guid).ToString()
                subscriptionId = $subCost.id
                date = $date.ToString("yyyy-MM-dd")
                service = $service
                cost = $cost
                currency = "USD"
                resourceGroup = "rg-$($service.ToLower().Replace(' ', '-'))"
                region = "Brazil South"
            }
            
            if (New-CosmosDbItem -Container "CostData" -PartitionKey $costRecord.subscriptionId -Item $costRecord) {
                $totalRecords++
            }
        }
        
        if ($day % 30 -eq 29) {
            Write-Host "      ✅ ${day}+ dias processados" -ForegroundColor Gray
        }
    }
    
    Write-Host "   ✅ $($subCost.id): 270 registros criados" -ForegroundColor Green
}

Write-Host "`n✅ Seed concluído com sucesso!" -ForegroundColor Green
Write-Host "`n📊 Resumo dos dados criados:" -ForegroundColor Cyan
Write-Host "   • 3 Tenants (empresas)" -ForegroundColor White
Write-Host "   • 8 Usuários" -ForegroundColor White
Write-Host "   • 6 Subscrições Azure" -ForegroundColor White
Write-Host "   • $totalRecords registros de custo (~3 meses)" -ForegroundColor White
Write-Host "`n🎯 Próximo passo: Acesse o dashboard e veja os dados!" -ForegroundColor Yellow
