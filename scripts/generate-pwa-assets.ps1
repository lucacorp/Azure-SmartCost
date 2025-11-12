# Generate PWA Assets - Simple Placeholder Creator
# This script creates basic placeholder files for PWA

$rootDir = Split-Path -Parent $PSScriptRoot
$publicDir = Join-Path $rootDir "smartcost-dashboard\public"
$screenshotsDir = Join-Path $publicDir "screenshots"

# Create screenshots directory
New-Item -ItemType Directory -Force -Path $screenshotsDir | Out-Null

Write-Host "Generating PWA Assets..." -ForegroundColor Cyan

# Function to create SVG placeholder
function New-SVGPlaceholder {
    param(
        [int]$Width,
        [int]$Height,
        [string]$Text,
        [string]$OutputPath
    )
    
    $svg = @"
<svg width="$Width" height="$Height" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="grad" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" style="stop-color:#0078d4;stop-opacity:1" />
      <stop offset="100%" style="stop-color:#00bcf2;stop-opacity:1" />
    </linearGradient>
  </defs>
  <rect width="$Width" height="$Height" fill="url(#grad)"/>
  <text x="50%" y="50%" font-family="Segoe UI, Arial, sans-serif" font-size="$(if($Width -lt 300){48}elseif($Width -lt 600){72}else{120})" font-weight="bold" fill="white" text-anchor="middle" dominant-baseline="middle">$Text</text>
</svg>
"@
    
    Set-Content -Path $OutputPath -Value $svg -Encoding UTF8
    Write-Host "  ✓ Created: $OutputPath" -ForegroundColor Green
}

# Function to create dashboard screenshot SVG
function New-DashboardScreenshot {
    param(
        [int]$Width,
        [int]$Height,
        [string]$OutputPath
    )
    
    $svg = @"
<svg width="$Width" height="$Height" xmlns="http://www.w3.org/2000/svg">
  <!-- Background -->
  <rect width="$Width" height="$Height" fill="#f5f5f5"/>
  
  <!-- Header -->
  <rect width="$Width" height="64" fill="#0078d4"/>
  <text x="24" y="40" font-family="Segoe UI" font-size="24" font-weight="600" fill="white">Azure SmartCost</text>
  
  <!-- Cards Row -->
  <rect x="20" y="84" width="$(($Width-60)/3)" height="120" rx="8" fill="white" stroke="#e0e0e0" stroke-width="1"/>
  <text x="40" y="114" font-family="Segoe UI" font-size="14" fill="#666">Total Cost</text>
  <text x="40" y="160" font-family="Segoe UI" font-size="32" font-weight="bold" fill="#0078d4">`$12,450</text>
  
  <rect x="$(20+($Width-60)/3+20)" y="84" width="$(($Width-60)/3)" height="120" rx="8" fill="white" stroke="#e0e0e0" stroke-width="1"/>
  <text x="$(40+($Width-60)/3+20)" y="114" font-family="Segoe UI" font-size="14" fill="#666">This Month</text>
  <text x="$(40+($Width-60)/3+20)" y="160" font-family="Segoe UI" font-size="32" font-weight="bold" fill="#00bcf2">`$3,280</text>
  
  <rect x="$(20+2*(($Width-60)/3+20))" y="84" width="$(($Width-60)/3)" height="120" rx="8" fill="white" stroke="#e0e0e0" stroke-width="1"/>
  <text x="$(40+2*(($Width-60)/3+20))" y="114" font-family="Segoe UI" font-size="14" fill="#666">Forecast</text>
  <text x="$(40+2*(($Width-60)/3+20))" y="160" font-family="Segoe UI" font-size="32" font-weight="bold" fill="#50e6ff">`$4,100</text>
  
  <!-- Chart Area -->
  <rect x="20" y="224" width="$($Width-40)" height="$(($Height-244))" rx="8" fill="white" stroke="#e0e0e0" stroke-width="1"/>
  <text x="40" y="254" font-family="Segoe UI" font-size="16" font-weight="600" fill="#333">Cost Trend (Last 30 Days)</text>
  
  <!-- Simple line chart -->
  <polyline points="40,$(($Height-80)) 140,$(($Height-120)) 240,$(($Height-100)) 340,$(($Height-140)) 440,$(($Height-110)) 540,$(($Height-150)) 640,$(($Height-130)) 740,$(($Height-160)) 840,$(($Height-140))" 
            fill="none" stroke="#0078d4" stroke-width="3"/>
  
  <!-- Dots on chart -->
  <circle cx="40" cy="$(($Height-80))" r="4" fill="#0078d4"/>
  <circle cx="140" cy="$(($Height-120))" r="4" fill="#0078d4"/>
  <circle cx="240" cy="$(($Height-100))" r="4" fill="#0078d4"/>
  <circle cx="340" cy="$(($Height-140))" r="4" fill="#0078d4"/>
  <circle cx="440" cy="$(($Height-110))" r="4" fill="#0078d4"/>
  <circle cx="540" cy="$(($Height-150))" r="4" fill="#0078d4"/>
  <circle cx="640" cy="$(($Height-130))" r="4" fill="#0078d4"/>
  <circle cx="740" cy="$(($Height-160))" r="4" fill="#0078d4"/>
  <circle cx="840" cy="$(($Height-140))" r="4" fill="#0078d4"/>
</svg>
"@
    
    Set-Content -Path $OutputPath -Value $svg -Encoding UTF8
    Write-Host "  ✓ Created: $OutputPath" -ForegroundColor Green
}

# Function to create mobile screenshot SVG
function New-MobileScreenshot {
    param(
        [int]$Width,
        [int]$Height,
        [string]$OutputPath
    )
    
    $svg = @"
<svg width="$Width" height="$Height" xmlns="http://www.w3.org/2000/svg">
  <!-- Background -->
  <rect width="$Width" height="$Height" fill="#f5f5f5"/>
  
  <!-- Header -->
  <rect width="$Width" height="60" fill="#0078d4"/>
  <text x="24" y="38" font-family="Segoe UI" font-size="20" font-weight="600" fill="white">SmartCost</text>
  
  <!-- Summary Card -->
  <rect x="16" y="76" width="$(($Width-32))" height="140" rx="12" fill="white" stroke="#e0e0e0" stroke-width="1"/>
  <text x="32" y="106" font-family="Segoe UI" font-size="14" fill="#666">Total Cost This Month</text>
  <text x="32" y="150" font-family="Segoe UI" font-size="36" font-weight="bold" fill="#0078d4">`$3,280</text>
  <text x="32" y="198" font-family="Segoe UI" font-size="12" fill="#00bcf2">↑ 12% vs last month</text>
  
  <!-- Alerts Section -->
  <text x="16" y="242" font-family="Segoe UI" font-size="16" font-weight="600" fill="#333">Recent Alerts</text>
  
  <!-- Alert 1 -->
  <rect x="16" y="256" width="$(($Width-32))" height="80" rx="8" fill="white" stroke="#e0e0e0" stroke-width="1"/>
  <circle cx="36" cy="296" r="8" fill="#ff4444"/>
  <text x="56" y="286" font-family="Segoe UI" font-size="14" font-weight="600" fill="#333">Budget Alert</text>
  <text x="56" y="306" font-family="Segoe UI" font-size="12" fill="#666">80% of monthly budget reached</text>
  
  <!-- Alert 2 -->
  <rect x="16" y="348" width="$(($Width-32))" height="80" rx="8" fill="white" stroke="#e0e0e0" stroke-width="1"/>
  <circle cx="36" cy="388" r="8" fill="#ffaa00"/>
  <text x="56" y="378" font-family="Segoe UI" font-size="14" font-weight="600" fill="#333">Cost Spike</text>
  <text x="56" y="398" font-family="Segoe UI" font-size="12" fill="#666">Unusual spending detected</text>
  
  <!-- Quick Actions -->
  <text x="16" y="454" font-family="Segoe UI" font-size="16" font-weight="600" fill="#333">Quick Actions</text>
  
  <rect x="16" y="468" width="$(($Width-48)/2)" height="64" rx="8" fill="#0078d4"/>
  <text x="$(24+($Width-48)/4)" y="508" font-family="Segoe UI" font-size="14" font-weight="600" fill="white" text-anchor="middle">View Details</text>
  
  <rect x="$(32+($Width-48)/2)" y="468" width="$(($Width-48)/2)" height="64" rx="8" fill="white" stroke="#0078d4" stroke-width="2"/>
  <text x="$(40+3*($Width-48)/4)" y="508" font-family="Segoe UI" font-size="14" font-weight="600" fill="#0078d4" text-anchor="middle">Export</text>
</svg>
"@
    
    Set-Content -Path $OutputPath -Value $svg -Encoding UTF8
    Write-Host "  ✓ Created: $OutputPath" -ForegroundColor Green
}

# Generate Icons
Write-Host "`n📱 Icons:" -ForegroundColor Yellow
New-SVGPlaceholder -Width 180 -Height 180 -Text "SC" -OutputPath "$publicDir\apple-touch-icon.svg"
New-SVGPlaceholder -Width 64 -Height 64 -Text "SC" -OutputPath "$publicDir\favicon.svg"

# Generate Screenshots
Write-Host "`n📸 Screenshots:" -ForegroundColor Yellow
New-DashboardScreenshot -Width 1280 -Height 720 -OutputPath "$screenshotsDir\dashboard.svg"
New-MobileScreenshot -Width 750 -Height 1334 -OutputPath "$screenshotsDir\mobile.svg"

# Generate OG Images
Write-Host "`n🌐 Social Media Images:" -ForegroundColor Yellow

$ogSvg = @"
<svg width="1200" height="630" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="bgGrad" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" style="stop-color:#0078d4;stop-opacity:1" />
      <stop offset="100%" style="stop-color:#00bcf2;stop-opacity:1" />
    </linearGradient>
  </defs>
  <rect width="1200" height="630" fill="url(#bgGrad)"/>
  
  <!-- Logo -->
  <circle cx="150" cy="315" r="80" fill="white" opacity="0.2"/>
  <text x="150" y="345" font-family="Segoe UI" font-size="72" font-weight="bold" fill="white" text-anchor="middle">SC</text>
  
  <!-- Text Content -->
  <text x="280" y="260" font-family="Segoe UI" font-size="56" font-weight="bold" fill="white">Azure SmartCost</text>
  <text x="280" y="330" font-family="Segoe UI" font-size="32" fill="white" opacity="0.9">Azure Cost Management Made Simple</text>
  
  <!-- Features -->
  <text x="280" y="400" font-family="Segoe UI" font-size="24" fill="white" opacity="0.8">✓ Real-time monitoring</text>
  <text x="280" y="440" font-family="Segoe UI" font-size="24" fill="white" opacity="0.8">✓ Budget alerts</text>
  <text x="280" y="480" font-family="Segoe UI" font-size="24" fill="white" opacity="0.8">✓ Cost optimization</text>
  
  <!-- CTA -->
  <rect x="280" y="520" width="280" height="60" rx="30" fill="white"/>
  <text x="420" y="560" font-family="Segoe UI" font-size="28" font-weight="600" fill="#0078d4" text-anchor="middle">Start Free Trial</text>
</svg>
"@
Set-Content -Path "$publicDir\og-image.svg" -Value $ogSvg -Encoding UTF8
Write-Host "  ✓ Created: $publicDir\og-image.svg" -ForegroundColor Green

$twitterSvg = @"
<svg width="1200" height="675" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="twitterGrad" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" style="stop-color:#0078d4;stop-opacity:1" />
      <stop offset="100%" style="stop-color:#00bcf2;stop-opacity:1" />
    </linearGradient>
  </defs>
  <rect width="1200" height="675" fill="url(#twitterGrad)"/>
  
  <!-- Logo -->
  <circle cx="140" cy="337" r="70" fill="white" opacity="0.2"/>
  <text x="140" y="362" font-family="Segoe UI" font-size="64" font-weight="bold" fill="white" text-anchor="middle">SC</text>
  
  <!-- Text Content -->
  <text x="250" y="280" font-family="Segoe UI" font-size="52" font-weight="bold" fill="white">Azure SmartCost</text>
  <text x="250" y="340" font-family="Segoe UI" font-size="28" fill="white" opacity="0.9">Reduce your Azure costs by up to 30%</text>
  
  <!-- Stats -->
  <rect x="250" y="380" width="180" height="100" rx="12" fill="white" opacity="0.15"/>
  <text x="340" y="420" font-family="Segoe UI" font-size="36" font-weight="bold" fill="white" text-anchor="middle">$45K+</text>
  <text x="340" y="455" font-family="Segoe UI" font-size="18" fill="white" opacity="0.8" text-anchor="middle">Saved by users</text>
  
  <rect x="450" y="380" width="180" height="100" rx="12" fill="white" opacity="0.15"/>
  <text x="540" y="420" font-family="Segoe UI" font-size="36" font-weight="bold" fill="white" text-anchor="middle">24/7</text>
  <text x="540" y="455" font-family="Segoe UI" font-size="18" fill="white" opacity="0.8" text-anchor="middle">Monitoring</text>
  
  <rect x="650" y="380" width="180" height="100" rx="12" fill="white" opacity="0.15"/>
  <text x="740" y="420" font-family="Segoe UI" font-size="36" font-weight="bold" fill="white" text-anchor="middle">Free</text>
  <text x="740" y="455" font-family="Segoe UI" font-size="18" fill="white" opacity="0.8" text-anchor="middle">30-day trial</text>
  
  <!-- CTA -->
  <text x="250" y="560" font-family="Segoe UI" font-size="24" fill="white" opacity="0.9">🚀 Join the Beta - 50% lifetime discount!</text>
</svg>
"@
Set-Content -Path "$publicDir\twitter-image.svg" -Value $twitterSvg -Encoding UTF8
Write-Host "  ✓ Created: $publicDir\twitter-image.svg" -ForegroundColor Green

Write-Host "`n✅ All SVG placeholders created successfully!" -ForegroundColor Green
Write-Host "`nNote: SVG files work in most browsers. For PNG conversion, you can:" -ForegroundColor Cyan
Write-Host "  1. Use an online converter (e.g., cloudconvert.com)" -ForegroundColor Gray
Write-Host "  2. Open each SVG in browser and save as PNG" -ForegroundColor Gray
Write-Host "  3. Install Inkscape and batch convert" -ForegroundColor Gray
Write-Host "`nFor now, SVG files are sufficient for testing! 🎉" -ForegroundColor Cyan
