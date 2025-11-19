#!/usr/bin/env pwsh
# Script para iniciar o dashboard React

Write-Host "Iniciando Azure SmartCost Dashboard..." -ForegroundColor Cyan

# Navegar para o diretorio do dashboard
Set-Location $PSScriptRoot\smartcost-dashboard

Write-Host "Diretorio atual: $(Get-Location)" -ForegroundColor Green

# Verificar se package.json existe
if (-not (Test-Path "package.json")) {
    Write-Host "Erro: package.json nao encontrado!" -ForegroundColor Red
    exit 1
}

# Limpar cache
Write-Host "Limpando cache..." -ForegroundColor Yellow
Remove-Item -Recurse -Force node_modules\.cache -ErrorAction SilentlyContinue

# Iniciar servidor
Write-Host "Iniciando servidor em http://localhost:3000..." -ForegroundColor Cyan
$env:BROWSER = "none"
npm start

