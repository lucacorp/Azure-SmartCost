# Azure SmartCost - Deploy Simples
$ErrorActionPreference = "Continue"

Write-Host "`n=== AZURE SMARTCOST - DEPLOY DEMO ===" -ForegroundColor Cyan
Write-Host "Criando infraestrutura basica..." -ForegroundColor Yellow

# Variaveis
$rg = "rg-smartcost-demo"
$location = "brazilsouth"
$random = Get-Random -Minimum 1000 -Maximum 9999
$api = "smartcost-api-$random"
$plan = "smartcost-plan-demo"

Write-Host "`nRecursos:" -ForegroundColor White
Write-Host "  Resource Group: $rg" -ForegroundColor Gray
Write-Host "  API Web App: $api" -ForegroundColor Gray
Write-Host "  Location: $location" -ForegroundColor Gray

# 1. Resource Group
Write-Host "`n[1/4] Criando Resource Group..." -ForegroundColor Cyan
az group create --name $rg --location $location --output none
if ($LASTEXITCODE -eq 0) { Write-Host "✅ Resource Group criado" -ForegroundColor Green }

# 2. App Service Plan
Write-Host "`n[2/4] Criando App Service Plan..." -ForegroundColor Cyan
az appservice plan create --name $plan --resource-group $rg --location $location --sku B1 --is-linux --output none
if ($LASTEXITCODE -eq 0) { Write-Host "✅ App Service Plan criado" -ForegroundColor Green }

# 3. Web App
Write-Host "`n[3/4] Criando API Web App..." -ForegroundColor Cyan
az webapp create --name $api --resource-group $rg --plan $plan --runtime "DOTNETCORE:8.0" --output none
if ($LASTEXITCODE -eq 0) { Write-Host "✅ Web App criado" -ForegroundColor Green }

# 4. Build e Deploy
Write-Host "`n[4/4] Build e Deploy..." -ForegroundColor Cyan
Push-Location ..\src\AzureSmartCost.Api

Write-Host "  Building..." -ForegroundColor Gray
dotnet publish -c Release -o publish --nologo --verbosity quiet
if ($LASTEXITCODE -eq 0) { Write-Host "✅ Build completo" -ForegroundColor Green }

Write-Host "  Criando pacote..." -ForegroundColor Gray
if (Test-Path deploy-simple.zip) { Remove-Item deploy-simple.zip -Force }
Compress-Archive -Path publish\* -DestinationPath deploy-simple.zip -Force
Write-Host "✅ Pacote criado" -ForegroundColor Green

Write-Host "  Fazendo deploy..." -ForegroundColor Gray
az webapp deploy --resource-group $rg --name $api --src-path deploy-simple.zip --type zip --timeout 600 --output none
if ($LASTEXITCODE -eq 0) { 
    Write-Host "✅ Deploy completo!" -ForegroundColor Green 
} else {
    Write-Host "⚠️  Deploy com avisos, mas pode ter funcionado" -ForegroundColor Yellow
}

Pop-Location

Write-Host "`n=== DEPLOY FINALIZADO ===" -ForegroundColor Magenta
Write-Host "`nURL da API: https://$api.azurewebsites.net" -ForegroundColor White
Write-Host "`nTestando em 30 segundos..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

Write-Host "`nTestando API..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "https://$api.azurewebsites.net" -Method Get -TimeoutSec 30
    Write-Host "✅ API respondeu: $($response.StatusCode)" -ForegroundColor Green
    if ($response.Content -match "SmartCost|swagger|api") {
        Write-Host "✅ Conteúdo OK!" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Pagina padrao do Azure" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ API não respondeu: $_" -ForegroundColor Red
}

Write-Host "`nFIM!" -ForegroundColor Cyan
