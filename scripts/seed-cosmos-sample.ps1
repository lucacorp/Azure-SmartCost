# Script para popular Cosmos DB com dados de exemplo para Analytics

$cosmosName = "smartcost-cosmos-beta"
$resourceGroup = "rg-smartcost-beta"
$databaseName = "SmartCostDB"
$containerName = "CostRecords"
$subscriptionId = "e6b85c41-c45d-42a5-955f-d4dfb3b13ce9"

Write-Host "🔄 Criando dados de exemplo no Cosmos DB..." -ForegroundColor Cyan

# Gerar 30 dias de dados de exemplo
$today = Get-Date
$services = @(
    @{Name="Azure Kubernetes Service"; BaseCost=620.50},
    @{Name="Azure SQL Database"; BaseCost=385.25},
    @{Name="Virtual Machines"; BaseCost=285.40},
    @{Name="Storage Accounts"; BaseCost=180.75},
    @{Name="Application Gateway"; BaseCost=150.30},
    @{Name="Azure Functions"; BaseCost=95.60},
    @{Name="Cosmos DB"; BaseCost=75.20},
    @{Name="Azure Monitor"; BaseCost=45.80}
)

$documents = @()

for ($day = 0; $day -lt 30; $day++) {
    $date = $today.AddDays(-$day)
    $dateStr = $date.ToString("yyyy-MM-dd")
    
    foreach ($service in $services) {
        # Adicionar variação aleatória de ±20%
        $variation = (Get-Random -Minimum 80 -Maximum 120) / 100.0
        $dailyCost = [math]::Round(($service.BaseCost / 30) * $variation, 2)
        
        $resourceId = "/subscriptions/$subscriptionId/resourceGroups/rg-prod/providers/Microsoft.Compute/virtualMachines/$($service.Name -replace ' ','-')-$day"
        
        $doc = @{
            id = [Guid]::NewGuid().ToString()
            subscriptionId = $subscriptionId
            date = $dateStr
            resourceId = $resourceId
            resourceName = "$($service.Name) - Resource $day"
            serviceName = $service.Name
            cost = $dailyCost
            currency = "USD"
            partitionKey = $subscriptionId
        }
        
        $documents += $doc
    }
}

Write-Host "📊 Total de documentos a criar: $($documents.Count)" -ForegroundColor Yellow

# Criar JSON file temporário
$jsonFile = "c:\temp\cosmos-seed-data.json"
New-Item -Path "c:\temp" -ItemType Directory -Force | Out-Null
$documents | ConvertTo-Json -Depth 10 | Out-File $jsonFile -Encoding UTF8

Write-Host "✅ Arquivo JSON criado: $jsonFile" -ForegroundColor Green
Write-Host "📝 Agora você pode usar o Azure Portal ou Azure CLI para importar os dados" -ForegroundColor Cyan
Write-Host ""
Write-Host "💡 Opções para importar:" -ForegroundColor Yellow
Write-Host "1. Azure Portal > Cosmos DB > Data Explorer > Upload Item" -ForegroundColor White
Write-Host "2. Ou use o script abaixo para importar via PowerShell:" -ForegroundColor White
Write-Host ""

# Script alternativo usando REST API
Write-Host "# Script para importar via REST API (execute manualmente):" -ForegroundColor Cyan
Write-Host @"
`$endpoint = "https://$cosmosName.documents.azure.com"
`$masterKey = (az cosmosdb keys list --name $cosmosName --resource-group $resourceGroup --query primaryMasterKey -o tsv)

foreach (`$doc in `$documents) {
    # Criar documento via REST API
    `$date = [DateTime]::UtcNow.ToString("r")
    `$verb = "POST"
    `$resourceType = "docs"
    `$resourceLink = "dbs/$databaseName/colls/$containerName"
    
    # Criar assinatura
    `$stringToSign = `$verb.ToLowerInvariant() + "`n" + `$resourceType.ToLowerInvariant() + "`n" + `$resourceLink + "`n" + `$date.ToLowerInvariant() + "`n" + "" + "`n"
    `$hmac = New-Object System.Security.Cryptography.HMACSHA256
    `$hmac.Key = [Convert]::FromBase64String(`$masterKey)
    `$hashPayload = `$hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes(`$stringToSign))
    `$signature = [Convert]::ToBase64String(`$hashPayload)
    
    `$authHeader = [Uri]::EscapeDataString("type=master&ver=1.0&sig=`$signature")
    
    `$headers = @{
        "Authorization" = `$authHeader
        "x-ms-date" = `$date
        "x-ms-version" = "2018-12-31"
        "Content-Type" = "application/json"
        "x-ms-documentdb-partitionkey" = "[\"`$(`$doc.partitionKey)\"]"
    }
    
    `$uri = "`$endpoint/dbs/$databaseName/colls/$containerName/docs"
    `$body = `$doc | ConvertTo-Json -Depth 10
    
    try {
        Invoke-RestMethod -Method POST -Uri `$uri -Headers `$headers -Body `$body
        Write-Host "✅ Documento criado: `$(`$doc.id)" -ForegroundColor Green
    } catch {
        Write-Host "❌ Erro ao criar documento: `$_" -ForegroundColor Red
    }
}
"@

Write-Host ""
Write-Host "📄 Dados de exemplo salvos em: $jsonFile" -ForegroundColor Green
Write-Host "🎯 Próximo passo: Importe os dados no Cosmos DB usando uma das opções acima" -ForegroundColor Cyan
