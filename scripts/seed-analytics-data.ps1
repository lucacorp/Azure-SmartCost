# Script simplificado para criar dados de Analytics no Cosmos DB
param(
    [string]$ResourceGroup = "rg-smartcost-beta",
    [string]$CosmosAccount = "smartcost-cosmos-beta",
    [string]$DatabaseName = "SmartCostDB",
    [string]$ContainerName = "CostRecords",
    [string]$SubscriptionId = "e6b85c41-c45d-42a5-955f-d4dfb3b13ce9"
)

Write-Host "🚀 Criando dados de Analytics para Cosmos DB..." -ForegroundColor Cyan

# Obter connection string
Write-Host "🔑 Obtendo connection string do Cosmos DB..." -ForegroundColor Yellow
$connStr = az cosmosdb keys list `
    --name $CosmosAccount `
    --resource-group $ResourceGroup `
    --type connection-strings `
    --query "connectionStrings[0].connectionString" `
    -o tsv

if (-not $connStr) {
    Write-Host "❌ Erro ao obter connection string" -ForegroundColor Red
    exit 1
}

# Parsear connection string para obter endpoint e key
if ($connStr -match "AccountEndpoint=([^;]+);AccountKey=([^;]+)") {
    $endpoint = $matches[1]
    $masterKey = $matches[2]
} else {
    Write-Host "❌ Erro ao parsear connection string" -ForegroundColor Red
    exit 1
}

Write-Host "📊 Endpoint: $endpoint" -ForegroundColor Gray

# Função para criar assinatura Cosmos DB
function Get-CosmosSignature {
    param($verb, $resourceType, $resourceLink, $date, $key)
    
    $keyBytes = [Convert]::FromBase64String($key)
    $text = @($verb.ToLowerInvariant() + "`n" + 
              $resourceType.ToLowerInvariant() + "`n" + 
              $resourceLink + "`n" + 
              $date.ToLowerInvariant() + "`n" + 
              "" + "`n")
    
    $body = [Text.Encoding]::UTF8.GetBytes($text)
    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = $keyBytes
    $hash = $hmac.ComputeHash($body)
    $signature = [Convert]::ToBase64String($hash)
    
    return [System.Web.HttpUtility]::UrlEncode("type=master&ver=1.0&sig=$signature")
}

# Carregar assembly para HttpUtility
Add-Type -AssemblyName System.Web

# Gerar dados de exemplo
Write-Host "📝 Gerando dados de exemplo (30 dias, 8 serviços)..." -ForegroundColor Yellow

$services = @(
    @{Name="Azure Kubernetes Service"; DailyCost=620.50/30},
    @{Name="Azure SQL Database"; DailyCost=385.25/30},
    @{Name="Virtual Machines"; DailyCost=285.40/30},
    @{Name="Storage Accounts"; DailyCost=180.75/30},
    @{Name="Application Gateway"; DailyCost=150.30/30},
    @{Name="Azure Functions"; DailyCost=95.60/30},
    @{Name="Cosmos DB"; DailyCost=75.20/30},
    @{Name="Azure Monitor"; DailyCost=45.80/30}
)

$today = Get-Date
$documentsCreated = 0
$errors = 0

Write-Host "💾 Criando documentos no Cosmos DB..." -ForegroundColor Yellow

for ($day = 0; $day -lt 30; $day++) {
    $date = $today.AddDays(-$day)
    $dateStr = $date.ToString("yyyy-MM-dd")
    
    foreach ($service in $services) {
        # Adicionar variação aleatória de ±20%
        $variation = (Get-Random -Minimum 80 -Maximum 120) / 100.0
        $cost = [math]::Round($service.DailyCost * $variation, 2)
        
        $resourceId = "/subscriptions/$SubscriptionId/resourceGroups/rg-prod-$day/providers/Microsoft.Compute/resources/$($service.Name -replace ' ','-')-res$day"
        
        $doc = @{
            id = [Guid]::NewGuid().ToString()
            subscriptionId = $SubscriptionId
            date = $dateStr
            resourceId = $resourceId
            resourceName = "$($service.Name) - Resource $day"
            serviceName = $service.Name
            cost = $cost
            currency = "USD"
        } | ConvertTo-Json -Compress
        
        # Criar headers para requisição
        $utcDate = ([DateTime]::UtcNow).ToString("r")
        $resourceType = "docs"
        $resourceLink = "dbs/$DatabaseName/colls/$ContainerName"
        
        $authSignature = Get-CosmosSignature -verb "POST" -resourceType $resourceType -resourceLink $resourceLink -date $utcDate -key $masterKey
        
        $headers = @{
            "Authorization" = $authSignature
            "x-ms-date" = $utcDate
            "x-ms-version" = "2018-12-31"
            "x-ms-documentdb-partitionkey" = "[`"$SubscriptionId`"]"
            "Content-Type" = "application/json"
        }
        
        $uri = "$endpoint/dbs/$DatabaseName/colls/$ContainerName/docs"
        
        try {
            $null = Invoke-RestMethod -Method POST -Uri $uri -Headers $headers -Body $doc -ErrorAction Stop
            $documentsCreated++
            
            if ($documentsCreated % 10 -eq 0) {
                Write-Host "   ✅ $documentsCreated documentos criados..." -ForegroundColor Green
            }
        } catch {
            $errors++
            if ($errors -le 3) {
                Write-Host "   ⚠️  Erro ao criar documento: $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }
    }
}

Write-Host ""
Write-Host "✅ Seed concluído!" -ForegroundColor Green
Write-Host "📊 Total de documentos criados: $documentsCreated" -ForegroundColor Cyan
if ($errors -gt 0) {
    Write-Host "⚠️  Total de erros: $errors" -ForegroundColor Yellow
}
Write-Host ""
Write-Host "🎯 Próximo passo: Testar Analytics API em:" -ForegroundColor Cyan
Write-Host "   https://smartcost-func-beta.azurewebsites.net/api/analytics/cost?subscriptionId=$SubscriptionId" -ForegroundColor White
