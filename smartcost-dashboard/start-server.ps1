# Script para iniciar o servidor React
Set-Location $PSScriptRoot
Write-Host "Diretório atual: $(Get-Location)" -ForegroundColor Cyan
Write-Host "Limpando cache..." -ForegroundColor Yellow
Remove-Item -Recurse -Force node_modules\.cache -ErrorAction SilentlyContinue
Write-Host "Iniciando servidor React..." -ForegroundColor Green
npm start
