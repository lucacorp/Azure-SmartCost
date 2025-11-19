# Start React Dev Server
Push-Location $PSScriptRoot

Write-Host "Iniciando SmartCost Dashboard..." -ForegroundColor Cyan
Write-Host "Diretorio: $(Get-Location)" -ForegroundColor Gray
Write-Host "URL: http://localhost:3000" -ForegroundColor Yellow

# Set environment variable to prevent browser from opening
$env:BROWSER = 'none'

# Start the development server
npm start

Pop-Location
