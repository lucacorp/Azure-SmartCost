# Azure SmartCost - Beta Launch Plan

**Target Launch Date**: November 19, 2025 (7 days)  
**Beta Duration**: 30 days  
**Beta Participants**: 20-50 users  
**Pricing**: 50% discount on all paid plans

---

## 📅 Timeline Overview

```
Day 1 (Nov 12) - Polimento & Assets      ████░░░ 
Day 2 (Nov 13) - Testes Finais           ░████░░
Day 3 (Nov 14) - Deploy Produção         ░░████░
Day 4 (Nov 15) - Validação & Smoke Tests ░░░███░
Day 5 (Nov 16) - Beta Testers Setup      ░░░░███
Day 6 (Nov 17) - Onboarding Primeiros    ░░░░░██
Day 7 (Nov 19) - Public Beta Launch!     ░░░░░░█
```

---

## 🎯 Day 1 (Nov 12) - Polimento & Assets

### Morning (3h)
**Task 1.1: Generate VAPID Keys**
```bash
# Instalar web-push
npm install -g web-push

# Gerar keys
web-push generate-vapid-keys

# Output example:
# Public Key: BEl62iUYgUivxIkv69yViEuiBIa...
# Private Key: UUxI4O8dlDQxdBuL4DPmvqRuD...

# Salvar em Key Vault
az keyvault secret set \
  --vault-name "kv-smartcost-prod" \
  --name "WebPush-PublicKey" \
  --value "BEl62iUYgUivxIkv69yViEuiBIa..."

az keyvault secret set \
  --vault-name "kv-smartcost-prod" \
  --name "WebPush-PrivateKey" \
  --value "UUxI4O8dlDQxdBuL4DPmvqRuD..."

# Update frontend .env.production
echo "REACT_APP_VAPID_PUBLIC_KEY=BEl62iUYgUivxIkv69yViEuiBIa..." >> .env.production
```

**Task 1.2: Create PWA Assets**
```bash
# Logo design (use Figma/Canva)
# Criar:
# - logo192.png (192x192) - App icon
# - logo512.png (512x512) - High-res icon
# - favicon.ico (32x32) - Browser tab
# - apple-touch-icon.png (180x180) - iOS home screen

# Screenshots para manifest.json
# Desktop: 1280x720 (landscape)
# - screenshot-dashboard.png
# - screenshot-analytics.png

# Mobile: 750x1334 (portrait)
# - screenshot-mobile-dashboard.png
# - screenshot-mobile-alerts.png

# Salvar em: smartcost-dashboard/public/
```

**Tool Recommendation:**
```
Design Tool: Canva (free)
Color Scheme: 
  - Primary: #0078d4 (Azure blue)
  - Secondary: #50e6ff (Azure light blue)
  - Accent: #00bcf2

Template: Use Azure branding guidelines
Time: 2-3 hours
```

### Afternoon (3h)
**Task 1.3: Update Manifest & Meta Tags**
```javascript
// smartcost-dashboard/public/manifest.json
{
  "short_name": "SmartCost",
  "name": "Azure SmartCost - FinOps Platform",
  "icons": [
    {
      "src": "favicon.ico",
      "sizes": "64x64 32x32 24x24 16x16",
      "type": "image/x-icon"
    },
    {
      "src": "logo192.png",
      "type": "image/png",
      "sizes": "192x192",
      "purpose": "any maskable"
    },
    {
      "src": "logo512.png",
      "type": "image/png",
      "sizes": "512x512",
      "purpose": "any maskable"
    }
  ],
  "screenshots": [
    {
      "src": "screenshot-dashboard.png",
      "type": "image/png",
      "sizes": "1280x720",
      "form_factor": "wide"
    },
    {
      "src": "screenshot-mobile-dashboard.png",
      "type": "image/png",
      "sizes": "750x1334",
      "form_factor": "narrow"
    }
  ],
  "start_url": "/",
  "display": "standalone",
  "theme_color": "#0078d4",
  "background_color": "#ffffff",
  "description": "Monitor and optimize your Azure cloud costs in real-time"
}
```

```html
<!-- smartcost-dashboard/public/index.html -->
<head>
  <!-- Primary Meta Tags -->
  <title>Azure SmartCost - FinOps Platform</title>
  <meta name="title" content="Azure SmartCost - Cloud Cost Management">
  <meta name="description" content="Monitor, optimize, and control your Azure cloud spending with AI-powered recommendations">
  
  <!-- Open Graph / Facebook -->
  <meta property="og:type" content="website">
  <meta property="og:url" content="https://app.smartcost.com/">
  <meta property="og:title" content="Azure SmartCost - FinOps Platform">
  <meta property="og:description" content="Real-time Azure cost monitoring and optimization">
  <meta property="og:image" content="https://app.smartcost.com/og-image.png">

  <!-- Twitter -->
  <meta property="twitter:card" content="summary_large_image">
  <meta property="twitter:url" content="https://app.smartcost.com/">
  <meta property="twitter:title" content="Azure SmartCost">
  <meta property="twitter:description" content="Cloud cost management made simple">
  <meta property="twitter:image" content="https://app.smartcost.com/twitter-image.png">

  <!-- iOS -->
  <meta name="apple-mobile-web-app-capable" content="yes">
  <meta name="apple-mobile-web-app-status-bar-style" content="default">
  <meta name="apple-mobile-web-app-title" content="SmartCost">
  <link rel="apple-touch-icon" href="/apple-touch-icon.png">
</head>
```

**Task 1.4: Create OG Images**
```bash
# Open Graph image (1200x630)
# Twitter card image (1200x675)

# Tools: Canva, Figma
# Content:
# - Logo
# - Tagline: "Azure Cost Management Made Simple"
# - Screenshot of dashboard
# - Call-to-action: "Start Your Free Trial"

# Save as:
# - smartcost-dashboard/public/og-image.png
# - smartcost-dashboard/public/twitter-image.png
```

**Deliverables Day 1:**
- [x] VAPID keys generated and stored
- [x] Logo files created (192, 512, favicon)
- [x] Screenshots captured (desktop + mobile)
- [x] OG images designed
- [x] Manifest.json updated
- [x] Meta tags added

---

## 🧪 Day 2 (Nov 13) - Testes Finais

### Morning (4h)
**Task 2.1: Lighthouse Audit**
```bash
# Install Lighthouse CLI
npm install -g lighthouse

# Build production frontend
cd smartcost-dashboard
npm run build

# Serve locally for testing
npx serve -s build -p 3000

# Run Lighthouse audit
lighthouse http://localhost:3000 \
  --output html \
  --output-path ./lighthouse-report.html \
  --chrome-flags="--headless"

# Target Scores:
# - Performance: >90
# - Accessibility: >90
# - Best Practices: >90
# - SEO: >90
# - PWA: 100
```

**Common Issues & Fixes:**
```javascript
// Issue: Large JavaScript bundles
// Fix: Code splitting
import React, { lazy, Suspense } from 'react';

const Dashboard = lazy(() => import('./components/Dashboard'));
const Analytics = lazy(() => import('./components/Analytics'));

// Issue: Unoptimized images
// Fix: Use WebP format + lazy loading
<img 
  src="image.webp" 
  loading="lazy" 
  alt="Dashboard preview"
/>

// Issue: Missing cache headers
// Fix: Update service-worker.js cache times
const CACHE_DURATION = 31536000; // 1 year for static assets
```

**Task 2.2: End-to-End Testing**
```bash
# Scenario 1: User Signup Flow
1. Visit https://app.smartcost.com
2. Click "Sign In with Microsoft"
3. Authenticate with Azure AD
4. Grant permissions
5. Redirect to dashboard
✅ Expected: User sees empty dashboard with onboarding

# Scenario 2: Connect Azure Subscription
1. Navigate to Settings → Subscriptions
2. Click "Add Azure Subscription"
3. Authenticate with subscription Owner/Contributor
4. Grant Cost Management Reader permissions
5. Select subscriptions to monitor
✅ Expected: Subscriptions appear in list with "Connected" status

# Scenario 3: View Cost Data
1. Wait 15-30 minutes for first data sync
2. Refresh dashboard
3. View cost charts
✅ Expected: Cost data displays, charts render

# Scenario 4: Create Budget
1. Navigate to Budgets
2. Click "Create Budget"
3. Fill: Name="Monthly Budget", Amount=$5000, Period=Monthly
4. Set thresholds: 80%, 90%, 100%
5. Add notification email
6. Save
✅ Expected: Budget created, appears in list

# Scenario 5: Install PWA (Android)
1. Open Chrome on Android
2. Visit https://app.smartcost.com
3. Wait for install prompt
4. Click "Install"
✅ Expected: App installs, icon appears on home screen

# Scenario 6: Offline Mode
1. Open installed PWA
2. Enable airplane mode
3. Navigate to Dashboard
✅ Expected: Cached data displays, offline banner shows

# Scenario 7: Push Notification
1. Grant notification permissions
2. Trigger test notification via API
✅ Expected: Notification appears on device
```

### Afternoon (3h)
**Task 2.3: Load Testing**
```bash
# Install k6 (load testing tool)
choco install k6  # Windows
# or
brew install k6   # Mac

# Create load test script
cat > load-test.js <<EOF
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 10 },   // Ramp up to 10 users
    { duration: '1m', target: 50 },    // Ramp up to 50 users
    { duration: '2m', target: 50 },    // Stay at 50 users
    { duration: '30s', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],  // 95% of requests < 500ms
    http_req_failed: ['rate<0.01'],    // Error rate < 1%
  },
};

export default function () {
  const token = 'YOUR_TEST_TOKEN';
  const headers = { Authorization: \`Bearer \${token}\` };

  // Test 1: Get tenants
  let res = http.get('https://api.smartcost.com/api/tenants', { headers });
  check(res, { 'tenants status 200': (r) => r.status === 200 });

  sleep(1);

  // Test 2: Get costs
  res = http.get(
    'https://api.smartcost.com/api/costs?tenantId=test-123&startDate=2025-01-01&endDate=2025-01-31',
    { headers }
  );
  check(res, { 'costs status 200': (r) => r.status === 200 });

  sleep(2);
}
EOF

# Run load test
k6 run load-test.js

# Expected Results:
# - 0% error rate
# - p95 response time < 500ms
# - All health checks passing
```

**Task 2.4: Security Scan**
```bash
# Run OWASP ZAP (security scanner)
docker run -t owasp/zap2docker-stable zap-baseline.py \
  -t https://api.smartcost.com \
  -r security-report.html

# Check for:
# - SQL injection vulnerabilities
# - XSS vulnerabilities
# - Insecure headers
# - SSL/TLS issues

# Manual checks:
# ✅ All secrets in Key Vault (not in code)
# ✅ HTTPS enforced
# ✅ CORS properly configured
# ✅ Rate limiting enabled
# ✅ Input validation on all endpoints
```

**Deliverables Day 2:**
- [x] Lighthouse score >90 on all metrics
- [x] E2E tests passing (7 scenarios)
- [x] Load test results documented
- [x] Security scan completed
- [x] Performance baseline established

---

## 🚀 Day 3 (Nov 14) - Deploy Produção

### Morning (3h)
**Task 3.1: Create Production Environment**
```bash
# Set variables
ENVIRONMENT="prod"
LOCATION="eastus"
UNIQUE_SUFFIX=$(openssl rand -hex 3)

# Login to Azure
az login
az account set --subscription "YOUR_SUBSCRIPTION_ID"

# Create resource group
az group create \
  --name "rg-smartcost-${ENVIRONMENT}" \
  --location "$LOCATION" \
  --tags "Environment=Production" "Project=SmartCost" "ManagedBy=Bicep"

# Deploy infrastructure
cd infra
az deployment group create \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --template-file main.bicep \
  --parameters parameters.prod.json \
  --parameters uniqueSuffix=$UNIQUE_SUFFIX \
  --mode Incremental

# Save outputs
az deployment group show \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --name main \
  --query "properties.outputs" > deployment-outputs.json

# Extract values
KEYVAULT_NAME=$(cat deployment-outputs.json | jq -r '.keyVaultName.value')
APP_NAME=$(cat deployment-outputs.json | jq -r '.apiAppName.value')
FUNC_NAME=$(cat deployment-outputs.json | jq -r '.functionAppName.value')
STORAGE_NAME=$(cat deployment-outputs.json | jq -r '.storageAccountName.value')
```

**Task 3.2: Configure Secrets**
```bash
# Cosmos DB
COSMOS_CONN=$(az cosmosdb keys list \
  --name "cosmos-smartcost-${ENVIRONMENT}-${UNIQUE_SUFFIX}" \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --type connection-strings \
  --query "connectionStrings[0].connectionString" -o tsv)

az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "CosmosDb-ConnectionString" \
  --value "$COSMOS_CONN"

# Redis
REDIS_KEY=$(az redis list-keys \
  --name "redis-smartcost-${ENVIRONMENT}-${UNIQUE_SUFFIX}" \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --query "primaryKey" -o tsv)

REDIS_HOST="redis-smartcost-${ENVIRONMENT}-${UNIQUE_SUFFIX}.redis.cache.windows.net"

az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "Redis-ConnectionString" \
  --value "${REDIS_HOST}:6380,password=${REDIS_KEY},ssl=True,abortConnect=False"

# Stripe (replace with your keys)
az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "Stripe-ApiKey" \
  --value "sk_live_YOUR_PRODUCTION_KEY"

az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "Stripe-WebhookSecret" \
  --value "whsec_YOUR_WEBHOOK_SECRET"

# Azure AD (from app registration)
az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "AzureAd-ClientId" \
  --value "YOUR_CLIENT_ID"

az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "AzureAd-ClientSecret" \
  --value "YOUR_CLIENT_SECRET"

# Web Push
az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "WebPush-PublicKey" \
  --value "YOUR_PUBLIC_VAPID_KEY"

az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "WebPush-PrivateKey" \
  --value "YOUR_PRIVATE_VAPID_KEY"

# Application Insights
APPINSIGHTS_KEY=$(az monitor app-insights component show \
  --app "appi-smartcost-${ENVIRONMENT}-${UNIQUE_SUFFIX}" \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --query "instrumentationKey" -o tsv)

az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "ApplicationInsights-InstrumentationKey" \
  --value "$APPINSIGHTS_KEY"
```

### Afternoon (4h)
**Task 3.3: Deploy API**
```bash
cd src/AzureSmartCost.Api

# Build Release
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
Compress-Archive -Path * -DestinationPath ../api-deploy.zip -Force
cd ..

# Deploy to App Service
az webapp deployment source config-zip \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --name "$APP_NAME" \
  --src "./api-deploy.zip"

# Verify deployment
az webapp browse \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --name "$APP_NAME"

# Test health endpoint
curl https://${APP_NAME}.azurewebsites.net/api/health
# Expected: {"status":"healthy","version":"2.0.0"}
```

**Task 3.4: Deploy Functions**
```bash
cd ../AzureSmartCost.Functions

# Deploy
func azure functionapp publish "$FUNC_NAME" --csharp

# Verify functions
az functionapp function list \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --name "$FUNC_NAME" \
  --query "[].{Name:name, Type:type}" -o table

# Expected:
# Name              Type
# ----------------  ----------
# CollectCostData   timerTrigger
# ProcessAlerts     timerTrigger
# GenerateReports   queueTrigger

# Test timer trigger manually
az functionapp function start \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --name "$FUNC_NAME" \
  --function-name "CollectCostData"
```

**Task 3.5: Deploy Frontend**
```bash
cd ../../smartcost-dashboard

# Install dependencies
npm ci --production

# Build with production config
REACT_APP_API_URL="https://${APP_NAME}.azurewebsites.net" \
REACT_APP_AZURE_AD_CLIENT_ID="YOUR_CLIENT_ID" \
REACT_APP_AZURE_AD_TENANT_ID="common" \
REACT_APP_VAPID_PUBLIC_KEY="YOUR_PUBLIC_VAPID_KEY" \
REACT_APP_ENVIRONMENT="production" \
npm run build

# Enable static website on Storage
az storage blob service-properties update \
  --account-name "$STORAGE_NAME" \
  --static-website \
  --index-document index.html \
  --404-document index.html

# Upload build
az storage blob upload-batch \
  --account-name "$STORAGE_NAME" \
  --destination '$web' \
  --source ./build \
  --overwrite

# Get frontend URL
FRONTEND_URL=$(az storage account show \
  --name "$STORAGE_NAME" \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --query "primaryEndpoints.web" -o tsv)

echo "Frontend URL: $FRONTEND_URL"
```

**Task 3.6: Configure Custom Domain (Optional)**
```bash
# If you have a custom domain (e.g., app.smartcost.com)

# Add custom domain to Storage
az storage account update \
  --name "$STORAGE_NAME" \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --custom-domain "app.smartcost.com"

# Create CNAME record in your DNS:
# app.smartcost.com → ${STORAGE_NAME}.z13.web.core.windows.net

# Add custom domain to App Service (API)
az webapp config hostname add \
  --webapp-name "$APP_NAME" \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --hostname "api.smartcost.com"

# Enable HTTPS
az webapp update \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --name "$APP_NAME" \
  --https-only true

# Create SSL certificate (free with App Service Managed Certificate)
az webapp config ssl create \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --name "$APP_NAME" \
  --hostname "api.smartcost.com"
```

**Deliverables Day 3:**
- [x] Production infrastructure deployed
- [x] All secrets configured in Key Vault
- [x] API deployed and health check passing
- [x] Functions deployed and verified
- [x] Frontend deployed to Storage
- [x] Custom domain configured (if applicable)

---

## ✅ Day 4 (Nov 15) - Validação & Smoke Tests

### Morning (3h)
**Task 4.1: Smoke Tests**
```bash
# Create smoke test script
cat > smoke-tests.sh <<'EOF'
#!/bin/bash

API_URL="https://api.smartcost.com"
FRONTEND_URL="https://app.smartcost.com"

echo "🧪 Running Smoke Tests..."
echo ""

# Test 1: Frontend loads
echo "Test 1: Frontend loads"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" $FRONTEND_URL)
if [ $HTTP_CODE -eq 200 ]; then
  echo "✅ PASS - Frontend returned 200"
else
  echo "❌ FAIL - Frontend returned $HTTP_CODE"
fi

# Test 2: API health endpoint
echo "Test 2: API health endpoint"
HEALTH=$(curl -s "$API_URL/api/health" | jq -r '.status')
if [ "$HEALTH" = "healthy" ]; then
  echo "✅ PASS - API is healthy"
else
  echo "❌ FAIL - API status: $HEALTH"
fi

# Test 3: Database connectivity
echo "Test 3: Database connectivity"
DB_STATUS=$(curl -s "$API_URL/api/health" | jq -r '.services.database.status')
if [ "$DB_STATUS" = "healthy" ]; then
  echo "✅ PASS - Database connected"
else
  echo "❌ FAIL - Database status: $DB_STATUS"
fi

# Test 4: Cache connectivity
echo "Test 4: Cache connectivity"
CACHE_STATUS=$(curl -s "$API_URL/api/health" | jq -r '.services.cache.status')
if [ "$CACHE_STATUS" = "healthy" ]; then
  echo "✅ PASS - Cache connected"
else
  echo "❌ FAIL - Cache status: $CACHE_STATUS"
fi

# Test 5: PWA manifest
echo "Test 5: PWA manifest"
MANIFEST=$(curl -s "$FRONTEND_URL/manifest.json" | jq -r '.name')
if [ "$MANIFEST" = "Azure SmartCost - FinOps Platform" ]; then
  echo "✅ PASS - PWA manifest valid"
else
  echo "❌ FAIL - PWA manifest invalid"
fi

# Test 6: Service worker
echo "Test 6: Service worker"
SW_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$FRONTEND_URL/service-worker.js")
if [ $SW_CODE -eq 200 ]; then
  echo "✅ PASS - Service worker available"
else
  echo "❌ FAIL - Service worker returned $SW_CODE"
fi

echo ""
echo "✅ Smoke tests completed!"
EOF

chmod +x smoke-tests.sh
./smoke-tests.sh
```

**Task 4.2: Monitoring Setup**
```bash
# Create Application Insights alerts

# Alert 1: High error rate
az monitor metrics alert create \
  --name "api-high-error-rate" \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --scopes "/subscriptions/.../providers/Microsoft.Web/sites/${APP_NAME}" \
  --condition "avg Http5xx > 10" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --description "API error rate > 10 per minute"

# Alert 2: High response time
az monitor metrics alert create \
  --name "api-slow-response" \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --scopes "/subscriptions/.../providers/Microsoft.Web/sites/${APP_NAME}" \
  --condition "avg ResponseTime > 2000" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --description "API response time > 2 seconds"

# Alert 3: Function failures
az monitor metrics alert create \
  --name "function-failures" \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --scopes "/subscriptions/.../providers/Microsoft.Web/sites/${FUNC_NAME}" \
  --condition "sum FunctionExecutionFailures > 5" \
  --window-size 15m \
  --evaluation-frequency 5m \
  --description "Function execution failures"

# Create Application Insights dashboard
az portal dashboard create \
  --name "SmartCost Production Dashboard" \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --input-path ./dashboard-template.json
```

### Afternoon (3h)
**Task 4.3: Create Monitoring Dashboard**

Create `dashboard-template.json`:
```json
{
  "properties": {
    "lenses": [
      {
        "parts": [
          {
            "metadata": {
              "type": "Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart",
              "settings": {
                "content": {
                  "metrics": [
                    {
                      "resourceId": "/subscriptions/.../sites/${APP_NAME}",
                      "name": "Http5xx",
                      "aggregationType": 1
                    }
                  ]
                }
              }
            }
          }
        ]
      }
    ]
  }
}
```

**Task 4.4: Setup Log Analytics Queries**
```kql
// Save these queries in Application Insights

// Query 1: Error Summary (last 24h)
exceptions
| where timestamp > ago(24h)
| summarize count() by type, outerMessage
| order by count_ desc

// Query 2: Slow Requests (>1s)
requests
| where timestamp > ago(1h)
| where duration > 1000
| project timestamp, name, duration, resultCode
| order by duration desc

// Query 3: User Activity
customEvents
| where timestamp > ago(24h)
| where name in ("UserLogin", "DashboardView", "BudgetCreated")
| summarize count() by name, bin(timestamp, 1h)
| render timechart

// Query 4: API Usage by Endpoint
requests
| where timestamp > ago(24h)
| summarize count() by operation_Name
| order by count_ desc
| render piechart

// Query 5: Function Execution Success Rate
traces
| where message contains "Function"
| where timestamp > ago(24h)
| summarize 
    Total = count(),
    Success = countif(severityLevel <= 1),
    Failed = countif(severityLevel >= 3)
| extend SuccessRate = (Success * 100.0) / Total
```

**Deliverables Day 4:**
- [x] All smoke tests passing
- [x] Monitoring alerts configured
- [x] Application Insights dashboard created
- [x] Log Analytics queries saved
- [x] Health checks automated

---

## 👥 Day 5 (Nov 16) - Beta Testers Setup

### Morning (3h)
**Task 5.1: Create Beta Tester List**

```markdown
# Beta Tester Target Profile
- Azure users (personal or company)
- Cloud spending: $500-$50,000/month
- FinOps interest (cost optimization)
- Tech-savvy (can give detailed feedback)
- Active on LinkedIn/Twitter

# Recruitment Sources:
1. Personal network (5-10 people)
2. LinkedIn connections (10-15 people)
3. Azure community forums (5-10 people)
4. Twitter DMs (5-10 people)
5. Reddit r/Azure (5-10 people)

Target: 20-30 beta testers
```

**Task 5.2: Create Beta Invitation Email**

Create `beta-invitation-email.html`:
```html
<!DOCTYPE html>
<html>
<head>
  <style>
    body { font-family: Arial, sans-serif; line-height: 1.6; }
    .container { max-width: 600px; margin: 0 auto; padding: 20px; }
    .header { background: #0078d4; color: white; padding: 20px; text-align: center; }
    .content { padding: 20px; }
    .cta { background: #0078d4; color: white; padding: 15px 30px; text-decoration: none; display: inline-block; margin: 20px 0; }
    .benefits { background: #f5f5f5; padding: 15px; margin: 20px 0; }
  </style>
</head>
<body>
  <div class="container">
    <div class="header">
      <h1>🚀 You're Invited to Azure SmartCost Beta!</h1>
    </div>
    
    <div class="content">
      <p>Hi [Name],</p>
      
      <p>I'm excited to invite you to the exclusive beta of <strong>Azure SmartCost</strong> - a new FinOps platform that helps you monitor and optimize Azure cloud costs.</p>
      
      <div class="benefits">
        <h3>As a beta tester, you'll get:</h3>
        <ul>
          <li>✅ <strong>50% lifetime discount</strong> (lock in beta pricing forever!)</li>
          <li>✅ <strong>Priority support</strong> (direct line to our team)</li>
          <li>✅ <strong>Early access</strong> to all new features</li>
          <li>✅ <strong>Shape the product</strong> (your feedback matters!)</li>
          <li>✅ <strong>Free Premium plan</strong> for 30 days ($199 value)</li>
        </ul>
      </div>
      
      <p><strong>What we need from you:</strong></p>
      <ul>
        <li>Use the platform for 30 days</li>
        <li>Connect at least 1 Azure subscription</li>
        <li>Provide feedback (weekly 15-min call or async)</li>
        <li>Report any bugs you find</li>
      </ul>
      
      <p><strong>Key Features:</strong></p>
      <ul>
        <li>📊 Real-time cost monitoring dashboard</li>
        <li>💰 AI-powered optimization recommendations</li>
        <li>🎯 Budget tracking with smart alerts</li>
        <li>📱 Mobile app (works offline!)</li>
        <li>🔐 Enterprise SSO with Azure AD</li>
      </ul>
      
      <a href="https://app.smartcost.com/beta?code=[BETA_CODE]" class="cta">
        Accept Beta Invitation →
      </a>
      
      <p>Limited to first 50 testers - claim your spot now!</p>
      
      <p>Questions? Reply to this email or schedule a call: [Calendly link]</p>
      
      <p>Looking forward to your feedback!</p>
      
      <p>Best regards,<br>
      [Your Name]<br>
      Founder, Azure SmartCost</p>
    </div>
  </div>
</body>
</html>
```

**Task 5.3: Create Beta Landing Page**

```typescript
// smartcost-dashboard/src/pages/BetaSignup.tsx
import React, { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';

export const BetaSignup: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const betaCode = searchParams.get('code');
  
  const [loading, setLoading] = useState(false);

  const handleSignup = async () => {
    setLoading(true);
    
    // Validate beta code
    const response = await fetch('/api/beta/validate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ code: betaCode })
    });
    
    if (response.ok) {
      // Redirect to Azure AD login
      window.location.href = '/api/auth/login?beta=true';
    } else {
      alert('Invalid beta code. Please check your invitation email.');
    }
    
    setLoading(false);
  };

  return (
    <div className="beta-signup">
      <header>
        <h1>Welcome to Azure SmartCost Beta! 🎉</h1>
        <p>You're one of the first 50 users to get exclusive access.</p>
      </header>
      
      <section className="benefits">
        <h2>Your Beta Benefits</h2>
        <ul>
          <li>✅ 50% lifetime discount on all plans</li>
          <li>✅ Priority support with direct team access</li>
          <li>✅ Free Premium plan for 30 days ($199 value)</li>
          <li>✅ Early access to new features</li>
          <li>✅ Influence product roadmap</li>
        </ul>
      </section>
      
      <section className="getting-started">
        <h2>Getting Started</h2>
        <ol>
          <li>Click "Accept Invitation" below</li>
          <li>Sign in with your Microsoft account</li>
          <li>Connect your Azure subscription</li>
          <li>Explore your cost dashboard</li>
          <li>Join our Slack channel for feedback</li>
        </ol>
      </section>
      
      <button onClick={handleSignup} disabled={loading || !betaCode}>
        {loading ? 'Loading...' : 'Accept Beta Invitation →'}
      </button>
      
      {!betaCode && (
        <p className="error">Missing beta code. Please use the link from your invitation email.</p>
      )}
    </div>
  );
};
```

### Afternoon (3h)
**Task 5.4: Setup Beta Tracking**

```csharp
// src/AzureSmartCost.Api/Models/BetaTester.cs
public class BetaTester
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string BetaCode { get; set; }
    public DateTime InvitedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? FirstLoginAt { get; set; }
    public DateTime? FirstSubscriptionConnectedAt { get; set; }
    public string Status { get; set; } // Invited, Accepted, Active, Churned
    public List<string> FeedbackNotes { get; set; }
    public int FeedbackSessionsCompleted { get; set; }
    public bool LifetimeDiscountApplied { get; set; }
}

// src/AzureSmartCost.Api/Controllers/BetaController.cs
[ApiController]
[Route("api/beta")]
public class BetaController : ControllerBase
{
    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateBetaCode([FromBody] ValidateBetaCodeRequest request)
    {
        var betaTester = await _betaService.GetByCodeAsync(request.Code);
        
        if (betaTester == null || betaTester.Status == "Expired")
        {
            return BadRequest(new { error = "Invalid or expired beta code" });
        }
        
        return Ok(new { valid = true, email = betaTester.Email });
    }
    
    [HttpPost("accept")]
    [Authorize]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptBetaRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        
        var betaTester = await _betaService.AcceptInvitationAsync(request.Code, userId, email);
        
        // Apply lifetime 50% discount
        await _subscriptionService.ApplyLifetimeDiscountAsync(userId, 0.5);
        
        // Send welcome email
        await _emailService.SendBetaWelcomeEmailAsync(email);
        
        return Ok(new { success = true, discountApplied = true });
    }
}
```

**Task 5.5: Create Feedback Collection System**

```markdown
# Beta Feedback Channels

## 1. Weekly Check-in Email (Automated)
Subject: "Week [N] Check-in - How's SmartCost working for you?"

Template:
- What features did you use this week?
- What worked well?
- What was confusing or frustrating?
- What feature would you add?
- Bugs encountered?

## 2. In-App Feedback Widget
- Floating button: "💬 Give Feedback"
- Quick rating (1-5 stars)
- Comment field
- Screenshot tool
- Auto-sends to Slack #beta-feedback

## 3. Slack Channel
- Create: #smartcost-beta
- Invite all beta testers
- Daily engagement
- Quick bug reports

## 4. Bi-weekly Video Calls
- Schedule: Every other Friday
- 30-minute group demo
- Q&A session
- Feature voting

## 5. Google Form (Exit Survey)
After 30 days:
- Would you pay for this? (Y/N)
- What price seems fair?
- Must-have features?
- Deal-breaker issues?
- NPS score (0-10)
```

**Deliverables Day 5:**
- [x] Beta tester list (30+ prospects)
- [x] Invitation email template
- [x] Beta landing page
- [x] Beta code system implemented
- [x] Feedback collection setup

---

## 🎉 Day 6 (Nov 17) - Onboarding Primeiros Beta Testers

### All Day (6h)
**Task 6.1: Send Invitations**

```bash
# Send first batch of 10 invitations
# Stagger throughout the day to manage support load

# Generate beta codes
for i in {1..10}; do
  CODE=$(openssl rand -hex 8)
  echo "BETA-$CODE" >> beta-codes.txt
done

# Send emails via SendGrid/Mailchimp
# Personalize each email with recipient name + beta code
```

**Task 6.2: Monitor Signups**

```bash
# Real-time dashboard for beta signups
# Track:
# - Invitations sent
# - Codes validated
# - Accounts created
# - Subscriptions connected
# - First dashboard views

# Alert on Slack for each signup
```

**Task 6.3: Personal Onboarding**

```markdown
# First 10 Beta Testers - White Glove Treatment

For each signup:
1. Welcome email within 15 minutes
2. Schedule 30-min onboarding call (optional)
3. Offer to screen-share for first setup
4. Add to Slack #smartcost-beta
5. Send getting started guide
6. Check in after 24 hours

Goals:
- 100% activation rate (connect subscription)
- Zero blockers
- Build relationships
- Get early feedback
```

**Task 6.4: Support Readiness**

```markdown
# Support Channels During Beta

## Email: beta@smartcost.com
- Response time: <2 hours
- Available: 9am-9pm EST

## Slack: #smartcost-beta
- Response time: <30 minutes
- Available: 24/7 (automated alerts)

## Video Call: Calendly link
- 15 or 30-minute slots
- Available: Mon-Fri 9am-6pm EST

## Documentation:
- Getting Started guide
- FAQ page
- Video tutorials
- Troubleshooting tips
```

**Deliverables Day 6:**
- [x] 10 invitations sent
- [x] 5-10 signups (50% conversion target)
- [x] All beta testers onboarded
- [x] First feedback collected
- [x] No critical bugs blocking usage

---

## 🚀 Day 7 (Nov 19) - Public Beta Launch!

### Morning (3h)
**Task 7.1: Launch Announcement**

**LinkedIn Post:**
```markdown
🚀 Excited to announce: Azure SmartCost is now in Public Beta!

After 3 months of development, I'm launching a FinOps platform to help companies monitor and optimize Azure cloud spending.

✨ What it does:
• Real-time cost monitoring dashboard
• AI-powered optimization recommendations  
• Smart budgets with multi-threshold alerts
• Mobile app (works offline!)
• Enterprise SSO with Azure AD

🎁 Beta Launch Special:
• 50% lifetime discount for first 100 users
• Free Premium plan for 30 days
• Priority support

Perfect for:
• DevOps teams managing cloud costs
• Finance teams tracking cloud budgets
• CTOs optimizing cloud spending

Try it free → https://app.smartcost.com/beta

#Azure #FinOps #CloudCost #SaaS #Launch

P.S. I'm looking for feedback! DM me if you'd like a personal demo.
```

**Twitter Thread:**
```markdown
1/ 🚀 Launching Azure SmartCost today - a FinOps platform to monitor and optimize your Azure cloud spending in real-time.

2/ 📊 Features:
• Real-time cost dashboard
• Budget tracking with smart alerts
• Cost forecasting with ML
• Mobile PWA (offline support)
• Azure AD SSO

3/ 🎁 Beta pricing:
50% lifetime discount for first 100 users
Free Premium ($199/mo) for 30 days

4/ Try it free → https://app.smartcost.com/beta

Looking for feedback from Azure users!

#Azure #FinOps #CloudCost
```

**Task 7.2: Community Outreach**

Post in:
- [ ] Reddit r/Azure
- [ ] Reddit r/devops
- [ ] Hacker News Show HN
- [ ] Azure Community Forums
- [ ] LinkedIn Azure Groups
- [ ] Twitter #Azure hashtag
- [ ] Dev.to blog post

**Task 7.3: Product Hunt Launch (Optional)**

```markdown
# Product Hunt Submission

Title: Azure SmartCost - FinOps platform for Azure cost optimization

Tagline: Monitor and optimize your Azure cloud spending in real-time

Description:
Azure SmartCost helps companies reduce cloud costs by 20-40% with:
• Real-time cost monitoring
• AI-powered recommendations
• Smart budget alerts
• Mobile app with offline support
• Enterprise SSO

Perfect for DevOps teams, finance teams, and CTOs managing Azure spending.

Beta launch: 50% lifetime discount for first 100 users!

Category: Developer Tools, SaaS
```

### Afternoon (3h)
**Task 7.4: Monitor Launch Metrics**

```markdown
# Key Metrics to Track (Day 1)

Traffic:
- Website visitors
- Landing page conversions
- Social media clicks

Signups:
- Beta code requests
- Account creations
- Azure subscription connections

Engagement:
- Dashboard views
- Budget creations
- Time in app

Feedback:
- Support tickets
- Slack messages
- Bug reports

Goals Day 1:
- 50+ website visitors
- 10+ signups
- 5+ active users (connected subscription)
- 0 critical bugs
- 5+ feedback items collected
```

**Task 7.5: First Retrospective**

```markdown
# End of Day 1 Review

Questions:
1. How many signups did we get?
2. What was the conversion rate?
3. What feedback did we receive?
4. Any critical bugs?
5. What worked well?
6. What needs improvement?
7. What to prioritize tomorrow?

Actions for Week 2:
- Fix top 3 bugs
- Implement most requested feature
- Improve onboarding flow
- Send weekly update to beta testers
```

**Deliverables Day 7:**
- [x] Public beta launched
- [x] Marketing posts published
- [x] Community outreach completed
- [x] Metrics dashboard tracking
- [x] Day 1 retrospective documented

---

## 📊 Success Metrics (30-Day Beta)

### Targets
- **Signups**: 50-100 beta testers
- **Activation**: 70%+ (connected Azure subscription)
- **Retention**: 50%+ still active after 30 days
- **NPS Score**: 40+ (Promoters minus Detractors)
- **Bug Reports**: <20 critical bugs
- **Feedback Items**: 100+ pieces of actionable feedback

### Weekly Milestones
```
Week 1: 20 signups, 14 active users
Week 2: 40 signups, 28 active users
Week 3: 60 signups, 42 active users
Week 4: 80 signups, 56 active users
```

---

## 🎁 Beta Pricing

### Special Beta Offer
```
All Plans: 50% LIFETIME Discount

Free Plan:
- $0 forever
- Up to $1,000/month Azure spend
- 1 user
- Basic features

Basic Plan (Beta):
- $24.50/month (normally $49)
- Up to $10,000/month Azure spend
- 5 users
- Budgets & alerts
- Email support

Premium Plan (Beta):
- $99.50/month (normally $199)
- Unlimited Azure spend
- 25 users
- All features
- Priority support
- Azure AD SSO
- Advanced analytics

FREE for first 30 days on any plan!
```

---

## 📞 Support Plan

### Response Times
- **Critical bugs**: <2 hours
- **Email support**: <4 hours
- **Slack questions**: <30 minutes (business hours)
- **Feature requests**: <24 hours (acknowledgment)

### Escalation Path
1. Slack #smartcost-beta (fastest)
2. Email beta@smartcost.com
3. Schedule call (Calendly)
4. Emergency: Direct WhatsApp/phone

---

## 🔄 Post-Launch Iteration

### Week 2 Priorities
1. Fix top 5 bugs from beta feedback
2. Implement #1 requested feature
3. Improve onboarding flow
4. Add video tutorials
5. Optimize performance bottlenecks

### Week 3-4 Priorities
1. Refine pricing based on feedback
2. Build case studies from beta users
3. Prepare for public launch (post-beta)
4. Scale infrastructure if needed
5. Recruit next 50 users

---

## ✅ Pre-Launch Checklist

**Technical:**
- [ ] Production infrastructure deployed
- [ ] All services healthy
- [ ] Smoke tests passing
- [ ] Monitoring alerts configured
- [ ] Backups scheduled

**Product:**
- [ ] PWA installable
- [ ] Push notifications working
- [ ] All critical features functional
- [ ] Documentation complete
- [ ] Help center live

**Marketing:**
- [ ] Beta landing page live
- [ ] Invitation emails ready
- [ ] Social media posts drafted
- [ ] Beta codes generated
- [ ] Analytics tracking setup

**Operations:**
- [ ] Support channels staffed
- [ ] Feedback system ready
- [ ] Bug tracking setup
- [ ] Customer success playbook
- [ ] Escalation procedures documented

---

## 🎯 Next Steps After Beta

### Month 2: Iterate & Scale
- Implement top beta feedback
- Onboard 100-200 users
- Achieve product-market fit
- Build case studies

### Month 3: Public Launch
- Remove beta limitations
- Full marketing campaign
- Press releases
- Partnership outreach

### Month 4-6: Growth
- Paid advertising
- Content marketing
- SEO optimization
- Consider Fase 3 features

---

**Ready to launch! 🚀**

Questions or need help with any step? Let me know!
