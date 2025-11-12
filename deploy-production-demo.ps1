# Production Deployment - DEMO
Write-Host "Azure SmartCost - Production Deployment DEMO" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Logged in as: admin@empresa.com" -ForegroundColor Green
Write-Host "Setting subscription: 12345678-abcd-1234-5678-123456789abc" -ForegroundColor Yellow
Start-Sleep -Seconds 1

Write-Host ""
Write-Host "Building and Publishing API..." -ForegroundColor Yellow
Write-Host "  Cleaning previous builds..." -ForegroundColor Gray
Start-Sleep -Seconds 1
Write-Host "  Restoring NuGet packages..." -ForegroundColor Gray
Start-Sleep -Seconds 1
Write-Host "  Building Release configuration..." -ForegroundColor Gray
Start-Sleep -Seconds 2
Write-Host "  Publishing artifacts..." -ForegroundColor Gray
Start-Sleep -Seconds 1
Write-Host "Build successful" -ForegroundColor Green

Write-Host ""
Write-Host "Deploying to App Service..." -ForegroundColor Yellow
Write-Host "  Creating deployment package..." -ForegroundColor Gray
Start-Sleep -Seconds 1
Write-Host "  Uploading to App Service: smartcost-prod" -ForegroundColor Gray
Start-Sleep -Seconds 3
Write-Host "  Extracting files..." -ForegroundColor Gray
Start-Sleep -Seconds 2
Write-Host "Deployment successful" -ForegroundColor Green

Write-Host ""
Write-Host "Configuring App Service Settings..." -ForegroundColor Yellow
Write-Host "  Setting POWERBI_ENABLED=true" -ForegroundColor Gray
Write-Host "  Setting POWERBI_ENVIRONMENT=Production" -ForegroundColor Gray
Write-Host "  Setting ASPNETCORE_ENVIRONMENT=Production" -ForegroundColor Gray
Start-Sleep -Seconds 1
Write-Host "App Service configured" -ForegroundColor Green

Write-Host ""
Write-Host "Building Frontend..." -ForegroundColor Yellow
Write-Host "  Installing npm packages..." -ForegroundColor Gray
Start-Sleep -Seconds 2
Write-Host "  Building React production bundle..." -ForegroundColor Gray
Start-Sleep -Seconds 3
Write-Host "  Optimizing assets..." -ForegroundColor Gray
Start-Sleep -Seconds 1
Write-Host "Frontend built successfully" -ForegroundColor Green

Write-Host ""
Write-Host "Performing Health Check..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
Write-Host "Health check passed" -ForegroundColor Green
Write-Host "   Status: healthy" -ForegroundColor Gray
Write-Host "   Version: 1.0.0" -ForegroundColor Gray

Write-Host ""
Write-Host "Setting up Monitoring..." -ForegroundColor Yellow
Write-Host "  Application Insights already configured" -ForegroundColor Gray
Write-Host "  Enabling performance monitoring..." -ForegroundColor Gray
Start-Sleep -Seconds 1
Write-Host "Monitoring configured" -ForegroundColor Green

Write-Host ""
Write-Host "Final Verification..." -ForegroundColor Yellow
Write-Host "/api/health - OK" -ForegroundColor Green
Write-Host "/api/powerbi/templates - OK" -ForegroundColor Green

Write-Host ""
Write-Host "DEPLOYMENT COMPLETED!" -ForegroundColor Green -BackgroundColor Black
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "• API URL: https://smartcost-prod.azurewebsites.net" -ForegroundColor White
Write-Host "• Health Check: https://smartcost-prod.azurewebsites.net/api/health" -ForegroundColor White
Write-Host "• Power BI Templates: https://smartcost-prod.azurewebsites.net/api/powerbi/templates" -ForegroundColor White
Write-Host "• Application Insights: Enabled" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Configure Power BI Workspace integration" -ForegroundColor White
Write-Host "2. Set up production monitoring alerts" -ForegroundColor White
Write-Host "3. Deploy frontend to static hosting" -ForegroundColor White
Write-Host "4. Update DNS and SSL certificates" -ForegroundColor White