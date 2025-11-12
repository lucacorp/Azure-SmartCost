# Lighthouse Audit Script for Azure SmartCost
# Day 2 - Testing

Write-Host "`n=== LIGHTHOUSE AUDIT - Day 2 ===" -ForegroundColor Cyan
Write-Host "Testing production build for PWA compliance`n" -ForegroundColor White

$rootDir = "C:\DIOazure\Azure-SmartCost"
$buildDir = "$rootDir\smartcost-dashboard\build"
$reportDir = "$rootDir\smartcost-dashboard"

# Check if build exists
if (!(Test-Path $buildDir)) {
    Write-Host "ERROR: Build directory not found. Run 'npm run build' first." -ForegroundColor Red
    exit 1
}

Write-Host "Step 1: Starting production server..." -ForegroundColor Yellow

# Start server in background
$serverJob = Start-Job -ScriptBlock {
    param($dir)
    Set-Location $dir
    npx serve -s build -l 3000 2>&1
} -ArgumentList $buildDir

Start-Sleep -Seconds 5

# Check if server is running
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000" -TimeoutSec 5 -UseBasicParsing
    Write-Host "  Server is running on port 3000" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: Server failed to start" -ForegroundColor Red
    Stop-Job $serverJob
    Remove-Job $serverJob
    exit 1
}

Write-Host "`nStep 2: Running Lighthouse audit..." -ForegroundColor Yellow
Write-Host "  Categories: PWA, Performance, Accessibility, Best Practices, SEO" -ForegroundColor Gray

try {
    # Run Lighthouse
    Set-Location $reportDir
    
    $lighthouseArgs = @(
        "http://localhost:3000",
        "--output=html",
        "--output=json",
        "--output-path=lighthouse-report",
        "--chrome-flags=`"--headless --no-sandbox`"",
        "--only-categories=pwa,performance,accessibility,best-practices,seo",
        "--quiet"
    )
    
    & lighthouse $lighthouseArgs
    
    Write-Host "`n  Lighthouse audit completed!" -ForegroundColor Green
    
    # Parse JSON results
    if (Test-Path "lighthouse-report.report.json") {
        $results = Get-Content "lighthouse-report.report.json" | ConvertFrom-Json
        
        Write-Host "`n=== LIGHTHOUSE SCORES ===" -ForegroundColor Cyan
        
        $categories = $results.categories
        
        foreach ($category in $categories.PSObject.Properties) {
            $name = $category.Name
            $score = [math]::Round($category.Value.score * 100)
            
            $color = "Red"
            if ($score -ge 90) { $color = "Green" }
            elseif ($score -ge 70) { $color = "Yellow" }
            
            Write-Host "  $($category.Value.title): $score/100" -ForegroundColor $color
        }
        
        Write-Host "`nReport saved: lighthouse-report.report.html" -ForegroundColor Cyan
        Write-Host "Open in browser to see detailed results.`n" -ForegroundColor Gray
    }
    
} catch {
    Write-Host "  ERROR: Lighthouse audit failed - $_" -ForegroundColor Red
} finally {
    Write-Host "`nStep 3: Stopping server..." -ForegroundColor Yellow
    Stop-Job $serverJob -ErrorAction SilentlyContinue
    Remove-Job $serverJob -ErrorAction SilentlyContinue
    Write-Host "  Server stopped" -ForegroundColor Green
}

Write-Host "`n=== AUDIT COMPLETE ===" -ForegroundColor Cyan
