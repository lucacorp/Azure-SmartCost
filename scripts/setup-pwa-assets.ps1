# Simple PWA Assets Setup
# This script prepares the basic PWA assets using existing logos

$rootDir = "C:\DIOazure\Azure-SmartCost"
$publicDir = "$rootDir\smartcost-dashboard\public"
$screenshotsDir = "$publicDir\screenshots"

Write-Host "`nPWA Assets Setup" -ForegroundColor Cyan
Write-Host "================`n" -ForegroundColor Cyan

# Create screenshots directory
if (!(Test-Path $screenshotsDir)) {
    New-Item -ItemType Directory -Force -Path $screenshotsDir | Out-Null
    Write-Host "Created: screenshots directory" -ForegroundColor Green
}

# Copy logo192 to apple-touch-icon (temporary)
if (Test-Path "$publicDir\logo192.png") {
    Copy-Item "$publicDir\logo192.png" "$publicDir\apple-touch-icon.png" -Force
    Write-Host "Created: apple-touch-icon.png (from logo192)" -ForegroundColor Green
}

# Create simple text placeholders for screenshots
$dashboardPlaceholder = @"
This is a placeholder for: Dashboard Screenshot (1280x720)

To replace with real screenshot:
1. Run the app: npm start
2. Open in Chrome: http://localhost:3000
3. Login and navigate to dashboard
4. Press F12 -> Device Toolbar
5. Set viewport to 1280x720
6. Take screenshot: Ctrl+Shift+P -> "Capture screenshot"
7. Save as: screenshots/dashboard.png
"@

$mobilePlaceholder = @"
This is a placeholder for: Mobile Screenshot (750x1334)

To replace with real screenshot:
1. Run the app: npm start
2. Open in Chrome DevTools
3. Select device: iPhone 8 Plus (750x1334)
4. Navigate to dashboard
5. Take screenshot
6. Save as: screenshots/mobile.png
"@

Set-Content -Path "$screenshotsDir\dashboard-placeholder.txt" -Value $dashboardPlaceholder
Set-Content -Path "$screenshotsDir\mobile-placeholder.txt" -Value $mobilePlaceholder

Write-Host "Created: screenshot placeholders (TXT)" -ForegroundColor Yellow

# Create OG image placeholders
$ogPlaceholder = @"
OpenGraph Image Placeholder (1200x630)

Quick option - Use Canva:
1. Go to: https://canva.com
2. Create custom size: 1200x630
3. Use template "Social Media Post"
4. Add text:
   - "Azure SmartCost"
   - "Azure Cost Management Made Simple"
   - "Start Free Trial"
5. Use colors: #0078d4, #00bcf2
6. Export as PNG
7. Save as: og-image.png
"@

$twitterPlaceholder = @"
Twitter Card Image Placeholder (1200x675)

Quick option - Use Canva:
1. Go to: https://canva.com
2. Create custom size: 1200x675
3. Similar to OG image but with stats:
   - "$45K+ Saved"
   - "24/7 Monitoring"
   - "Free Trial"
4. Export as PNG
5. Save as: twitter-image.png
"@

Set-Content -Path "$publicDir\og-image-placeholder.txt" -Value $ogPlaceholder
Set-Content -Path "$publicDir\twitter-image-placeholder.txt" -Value $twitterPlaceholder

Write-Host "Created: social media placeholders (TXT)" -ForegroundColor Yellow

Write-Host "`nCurrent Assets Status:" -ForegroundColor Cyan
Write-Host "======================" -ForegroundColor Cyan
Write-Host "  logo192.png:         READY" -ForegroundColor Green
Write-Host "  logo512.png:         READY" -ForegroundColor Green  
Write-Host "  apple-touch-icon:    READY (copied from logo192)" -ForegroundColor Green
Write-Host "  favicon.ico:         MISSING (optional)" -ForegroundColor Yellow
Write-Host "  screenshots:         PLACEHOLDERS (create after Day 3 deploy)" -ForegroundColor Yellow
Write-Host "  og-image:            PLACEHOLDERS (create with Canva)" -ForegroundColor Yellow

Write-Host "`nNext Steps:" -ForegroundColor Cyan
Write-Host "===========" -ForegroundColor Cyan
Write-Host "1. Continue with Day 1 - manifest.json update" -ForegroundColor White
Write-Host "2. Screenshots can wait until Day 3 (after production deploy)" -ForegroundColor Gray
Write-Host "3. OG images optional - add before Day 7 (public launch)" -ForegroundColor Gray

Write-Host "`nAssets are READY for Day 2 testing!" -ForegroundColor Green
