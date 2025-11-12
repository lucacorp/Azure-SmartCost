# Azure SmartCost - Deployment Guide

## 📋 Table of Contents
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Development Environment](#development-environment)
- [Production Deployment](#production-deployment)
- [Post-Deployment Configuration](#post-deployment-configuration)
- [Monitoring & Maintenance](#monitoring--maintenance)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Tools
- **Azure CLI** (v2.50+): `az --version`
- **.NET 8 SDK**: `dotnet --version`
- **Node.js** (v18+): `node --version`
- **PowerShell** (v7+) or Bash
- **Azure Subscription** with Owner/Contributor role
- **Git**: `git --version`

### Azure Services Required
- Azure App Service (B1 or higher)
- Azure Functions (Consumption or Premium)
- Azure Cosmos DB (Serverless recommended for dev)
- Azure Key Vault
- Azure Cache for Redis (Basic C0+ for prod)
- Azure AD (for SSO)
- Azure Storage Account
- Application Insights

### Estimated Costs (Monthly)
| Environment | App Service | Cosmos DB | Redis | Functions | Storage | Total |
|-------------|-------------|-----------|-------|-----------|---------|-------|
| **Dev** | Free/B1 ($13) | Serverless (~$5) | N/A | Consumption (~$0) | ~$1 | **$19** |
| **Prod** | S1 ($74) | Autoscale (~$50) | C1 ($73) | Premium (~$150) | ~$5 | **$352** |

---

## Quick Start

### 1. Clone Repository
```bash
git clone https://github.com/your-org/Azure-SmartCost.git
cd Azure-SmartCost
```

### 2. Login to Azure
```bash
az login
az account set --subscription "Your Subscription Name"
```

### 3. Deploy Infrastructure (Development)
```bash
cd infra
./deploy.sh dev

# Or on Windows PowerShell:
.\deploy.ps1 -Environment dev
```

### 4. Configure Application Secrets
```bash
# Set Cosmos DB connection string
az keyvault secret set \
  --vault-name "smartcost-kv-dev-XXXXX" \
  --name "CosmosDb-ConnectionString" \
  --value "AccountEndpoint=https://...;AccountKey=..."

# Set Stripe API key
az keyvault secret set \
  --vault-name "smartcost-kv-dev-XXXXX" \
  --name "Stripe-ApiKey" \
  --value "sk_test_..."
```

### 5. Deploy Application Code
```bash
# Deploy API
cd ../src/AzureSmartCost.Api
dotnet publish -c Release
az webapp deployment source config-zip \
  --resource-group "rg-smartcost-dev" \
  --name "app-smartcost-api-dev-XXXXX" \
  --src "./bin/Release/net8.0/publish.zip"

# Deploy Functions
cd ../AzureSmartCost.Functions
func azure functionapp publish "func-smartcost-dev-XXXXX"

# Deploy Frontend
cd ../../smartcost-dashboard
npm install
npm run build
az storage blob upload-batch \
  --account-name "stsmartcostdevXXXXX" \
  --destination '$web' \
  --source ./build
```

---

## Development Environment

### Local Development Setup

#### 1. Backend (API + Functions)
```bash
# Install dependencies
cd src/AzureSmartCost.Api
dotnet restore

# Update local.settings.json
cat > local.settings.json <<EOF
{
  "ConnectionStrings": {
    "CosmosDb": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv...",
    "Redis": "localhost:6379"
  },
  "Stripe": {
    "ApiKey": "sk_test_YOUR_KEY",
    "WebhookSecret": "whsec_YOUR_SECRET"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  }
}
EOF

# Run API
dotnet run

# Run Functions (in separate terminal)
cd ../AzureSmartCost.Functions
func start
```

#### 2. Frontend (Dashboard)
```bash
cd smartcost-dashboard

# Create .env.local
cat > .env.local <<EOF
REACT_APP_API_URL=http://localhost:5000
REACT_APP_AZURE_AD_CLIENT_ID=YOUR_CLIENT_ID
REACT_APP_AZURE_AD_TENANT_ID=YOUR_TENANT_ID
REACT_APP_VAPID_PUBLIC_KEY=YOUR_VAPID_PUBLIC_KEY
EOF

# Install and run
npm install
npm start
```

#### 3. Local Database (Cosmos DB Emulator)
```powershell
# Download and install Cosmos DB Emulator
# https://aka.ms/cosmosdb-emulator

# Start emulator
& "C:\Program Files\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe" /AllowNetworkAccess /Key=YOUR_KEY
```

#### 4. Local Cache (Redis)
```bash
# Using Docker
docker run -d -p 6379:6379 --name redis redis:latest

# Or install Redis locally (Windows)
choco install redis-64

# Or use WSL
wsl
sudo apt-get install redis-server
redis-server
```

---

## Production Deployment

### Infrastructure Deployment

#### 1. Prepare Azure Resources
```bash
# Set variables
ENVIRONMENT="prod"
LOCATION="eastus"
UNIQUE_SUFFIX=$(openssl rand -hex 3)

# Create resource group
az group create \
  --name "rg-smartcost-${ENVIRONMENT}" \
  --location "$LOCATION"

# Deploy Bicep template
az deployment group create \
  --resource-group "rg-smartcost-${ENVIRONMENT}" \
  --template-file infra/main.bicep \
  --parameters infra/parameters.prod.json \
  --parameters uniqueSuffix=$UNIQUE_SUFFIX
```

#### 2. Configure Key Vault Secrets
```bash
KEYVAULT_NAME=$(az keyvault list -g "rg-smartcost-prod" --query "[0].name" -o tsv)

# Cosmos DB
COSMOS_CONN=$(az cosmosdb keys list \
  --name "cosmos-smartcost-prod-${UNIQUE_SUFFIX}" \
  --resource-group "rg-smartcost-prod" \
  --type connection-strings \
  --query "connectionStrings[0].connectionString" -o tsv)

az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "CosmosDb-ConnectionString" \
  --value "$COSMOS_CONN"

# Redis
REDIS_CONN=$(az redis list-keys \
  --name "redis-smartcost-prod-${UNIQUE_SUFFIX}" \
  --resource-group "rg-smartcost-prod" \
  --query "primaryKey" -o tsv)

az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "Redis-ConnectionString" \
  --value "redis-smartcost-prod.redis.cache.windows.net:6380,password=${REDIS_CONN},ssl=True"

# Stripe (set manually)
az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "Stripe-ApiKey" \
  --value "sk_live_YOUR_PRODUCTION_KEY"

# Azure AD
az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "AzureAd-ClientSecret" \
  --value "YOUR_PRODUCTION_CLIENT_SECRET"

# Application Insights
APPINSIGHTS_KEY=$(az monitor app-insights component show \
  --app "appi-smartcost-prod-${UNIQUE_SUFFIX}" \
  --resource-group "rg-smartcost-prod" \
  --query "instrumentationKey" -o tsv)

az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "ApplicationInsights-InstrumentationKey" \
  --value "$APPINSIGHTS_KEY"
```

#### 3. Deploy Application Code

##### API Deployment
```bash
cd src/AzureSmartCost.Api

# Build for production
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
zip -r ../api-deploy.zip .
cd ..

# Deploy to App Service
APP_NAME=$(az webapp list -g "rg-smartcost-prod" --query "[?contains(name, 'api')].name" -o tsv)

az webapp deployment source config-zip \
  --resource-group "rg-smartcost-prod" \
  --name "$APP_NAME" \
  --src "./api-deploy.zip"

# Restart app
az webapp restart --resource-group "rg-smartcost-prod" --name "$APP_NAME"
```

##### Functions Deployment
```bash
cd ../AzureSmartCost.Functions

# Get function app name
FUNC_NAME=$(az functionapp list -g "rg-smartcost-prod" --query "[0].name" -o tsv)

# Deploy
func azure functionapp publish "$FUNC_NAME" --csharp

# Verify deployment
az functionapp function show \
  --resource-group "rg-smartcost-prod" \
  --name "$FUNC_NAME" \
  --function-name "CollectCostData"
```

##### Frontend Deployment
```bash
cd ../../smartcost-dashboard

# Install dependencies
npm ci --production

# Build with production config
REACT_APP_API_URL="https://${APP_NAME}.azurewebsites.net" \
REACT_APP_ENVIRONMENT="production" \
npm run build

# Get storage account name
STORAGE_NAME=$(az storage account list -g "rg-smartcost-prod" --query "[0].name" -o tsv)

# Enable static website
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

# Get CDN endpoint
FRONTEND_URL=$(az storage account show \
  --name "$STORAGE_NAME" \
  --resource-group "rg-smartcost-prod" \
  --query "primaryEndpoints.web" -o tsv)

echo "Frontend deployed to: $FRONTEND_URL"
```

#### 4. Configure Custom Domain & SSL

```bash
# Add custom domain to App Service
az webapp config hostname add \
  --webapp-name "$APP_NAME" \
  --resource-group "rg-smartcost-prod" \
  --hostname "api.smartcost.yourdomain.com"

# Enable HTTPS only
az webapp update \
  --resource-group "rg-smartcost-prod" \
  --name "$APP_NAME" \
  --https-only true

# Create SSL binding (requires certificate)
az webapp config ssl bind \
  --resource-group "rg-smartcost-prod" \
  --name "$APP_NAME" \
  --certificate-thumbprint "YOUR_CERT_THUMBPRINT" \
  --ssl-type SNI
```

---

## Post-Deployment Configuration

### 1. Azure AD App Registration

#### Create App Registration
```bash
# Create app
APP_REGISTRATION=$(az ad app create \
  --display-name "Azure SmartCost" \
  --sign-in-audience "AzureADMultipleOrgs" \
  --web-redirect-uris "https://${FRONTEND_URL}/auth/callback" \
  --required-resource-accesses @manifest.json)

CLIENT_ID=$(echo $APP_REGISTRATION | jq -r '.appId')

# Create client secret
CLIENT_SECRET=$(az ad app credential reset \
  --id "$CLIENT_ID" \
  --query "password" -o tsv)

# Store in Key Vault
az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "AzureAd-ClientId" \
  --value "$CLIENT_ID"

az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "AzureAd-ClientSecret" \
  --value "$CLIENT_SECRET"
```

#### Configure API Permissions
```bash
# Add Microsoft Graph permissions
az ad app permission add \
  --id "$CLIENT_ID" \
  --api 00000003-0000-0000-c000-000000000000 \
  --api-permissions \
    e1fe6dd8-ba31-4d61-89e7-88639da4683d=Scope \
    64a6cdd6-aab1-4aaf-94b8-3cc8405e90d0=Scope

# Grant admin consent
az ad app permission admin-consent --id "$CLIENT_ID"
```

### 2. Stripe Webhook Configuration

```bash
# Create webhook endpoint
curl https://api.stripe.com/v1/webhook_endpoints \
  -u "sk_live_YOUR_KEY:" \
  -d "url=https://${APP_NAME}.azurewebsites.net/api/stripe/webhook" \
  -d "enabled_events[]=customer.subscription.created" \
  -d "enabled_events[]=customer.subscription.updated" \
  -d "enabled_events[]=customer.subscription.deleted" \
  -d "enabled_events[]=invoice.payment_succeeded" \
  -d "enabled_events[]=invoice.payment_failed"

# Get webhook secret and store in Key Vault
WEBHOOK_SECRET="whsec_..." # From Stripe Dashboard
az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "Stripe-WebhookSecret" \
  --value "$WEBHOOK_SECRET"
```

### 3. Azure Marketplace Publishing

See [MARKETPLACE_GUIDE.md](./MARKETPLACE_GUIDE.md) for complete instructions.

### 4. VAPID Keys for Push Notifications

```bash
# Generate VAPID keys
npx web-push generate-vapid-keys

# Store in Key Vault
az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "WebPush-PublicKey" \
  --value "YOUR_PUBLIC_KEY"

az keyvault secret set \
  --vault-name "$KEYVAULT_NAME" \
  --name "WebPush-PrivateKey" \
  --value "YOUR_PRIVATE_KEY"

# Update frontend .env
echo "REACT_APP_VAPID_PUBLIC_KEY=YOUR_PUBLIC_KEY" >> .env.production
```

---

## Monitoring & Maintenance

### Application Insights Queries

#### Check API Health
```kql
requests
| where timestamp > ago(1h)
| summarize 
    Total = count(),
    Success = countif(success == true),
    Failed = countif(success == false),
    AvgDuration = avg(duration)
| extend SuccessRate = (Success * 100.0) / Total
```

#### Monitor Function Executions
```kql
traces
| where timestamp > ago(24h)
| where message contains "CollectCostData"
| summarize count() by bin(timestamp, 1h)
| render timechart
```

#### Track Errors
```kql
exceptions
| where timestamp > ago(24h)
| summarize count() by type, outerMessage
| order by count_ desc
```

### Redis Cache Monitoring

```bash
# Check cache stats via API
curl -X GET "https://${APP_NAME}.azurewebsites.net/api/cache/stats" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Azure CLI
az redis show \
  --name "redis-smartcost-prod-${UNIQUE_SUFFIX}" \
  --resource-group "rg-smartcost-prod" \
  --query "{status: provisioningState, size: sku.capacity, memory: sku.name}"
```

### Database Maintenance

```bash
# Check Cosmos DB metrics
az cosmosdb show \
  --name "cosmos-smartcost-prod-${UNIQUE_SUFFIX}" \
  --resource-group "rg-smartcost-prod" \
  --query "{status: provisioningState, consistency: consistencyPolicy.defaultConsistencyLevel}"

# Monitor RU consumption
az monitor metrics list \
  --resource "/subscriptions/.../cosmosdb/cosmos-smartcost-prod-..." \
  --metric "TotalRequestUnits" \
  --start-time "2025-01-01T00:00:00Z" \
  --end-time "2025-01-02T00:00:00Z" \
  --interval PT1H
```

### Backup & Disaster Recovery

#### Database Backups
```bash
# Cosmos DB automatic backups (enabled by default)
# Configure backup policy
az cosmosdb update \
  --name "cosmos-smartcost-prod-${UNIQUE_SUFFIX}" \
  --resource-group "rg-smartcost-prod" \
  --backup-interval 240 \
  --backup-retention 720

# Restore from backup (if needed)
az cosmosdb restore \
  --resource-group "rg-smartcost-prod" \
  --name "cosmos-smartcost-prod-restored" \
  --source-database-account-name "cosmos-smartcost-prod-${UNIQUE_SUFFIX}" \
  --restore-timestamp "2025-01-15T10:00:00Z"
```

#### Application Backups
```bash
# Backup App Service
az webapp config backup create \
  --resource-group "rg-smartcost-prod" \
  --webapp-name "$APP_NAME" \
  --backup-name "backup-$(date +%Y%m%d)" \
  --container-url "https://stbackup.blob.core.windows.net/backups?SAS_TOKEN"
```

### Scaling

#### App Service Scaling
```bash
# Manual scale up
az appservice plan update \
  --resource-group "rg-smartcost-prod" \
  --name "plan-smartcost-prod" \
  --sku P1V2

# Enable autoscale
az monitor autoscale create \
  --resource-group "rg-smartcost-prod" \
  --resource "$APP_SERVICE_RESOURCE_ID" \
  --min-count 2 \
  --max-count 10 \
  --count 2

# Add CPU-based rule
az monitor autoscale rule create \
  --resource-group "rg-smartcost-prod" \
  --autoscale-name "autoscale-smartcost-api" \
  --condition "Percentage CPU > 70 avg 5m" \
  --scale out 1
```

---

## Troubleshooting

See [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) for comprehensive troubleshooting guide.

### Quick Fixes

#### API Not Responding
```bash
# Check app logs
az webapp log tail \
  --resource-group "rg-smartcost-prod" \
  --name "$APP_NAME"

# Restart app
az webapp restart \
  --resource-group "rg-smartcost-prod" \
  --name "$APP_NAME"
```

#### Functions Not Triggering
```bash
# Check function logs
func azure functionapp logstream "$FUNC_NAME"

# Verify timer trigger
az functionapp function show \
  --resource-group "rg-smartcost-prod" \
  --name "$FUNC_NAME" \
  --function-name "CollectCostData" \
  --query "config.bindings[?type=='timerTrigger'].schedule"
```

#### Redis Connection Issues
```bash
# Test connection
redis-cli -h "redis-smartcost-prod.redis.cache.windows.net" -p 6380 -a "YOUR_KEY" --tls ping

# Check firewall rules
az redis firewall-rules list \
  --name "redis-smartcost-prod-${UNIQUE_SUFFIX}" \
  --resource-group "rg-smartcost-prod"
```

---

## Security Checklist

- [ ] All secrets stored in Key Vault
- [ ] HTTPS enforced on all endpoints
- [ ] Azure AD authentication configured
- [ ] CORS properly configured
- [ ] Rate limiting enabled
- [ ] SQL injection protection (parameterized queries)
- [ ] XSS protection headers enabled
- [ ] CSRF tokens implemented
- [ ] API keys rotated regularly
- [ ] Backup strategy in place
- [ ] Monitoring & alerts configured
- [ ] Private endpoints for databases (optional)
- [ ] Network security groups configured
- [ ] DDoS protection enabled (premium)

---

## Additional Resources

- [API Documentation](./API_DOCUMENTATION.md)
- [Architecture Overview](./ARCHITECTURE.md)
- [Troubleshooting Guide](./TROUBLESHOOTING.md)
- [Marketplace Publishing Guide](./MARKETPLACE_GUIDE.md)
- [Power BI Setup](../POWERBI_SETUP.md)
- [Configuration Guide](../CONFIGURATION.md)

---

**Last Updated**: January 2025  
**Version**: 2.0  
**Maintained By**: Azure SmartCost Team
