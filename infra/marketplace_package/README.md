# Azure SmartCost - Marketplace Package

**Version:** 1.1.0  
**Date:** December 1, 2025  
**Status:** ✅ Ready for Upload

---

## 📦 Package Contents

```
marketplace_package/
├── mainTemplate.json          (28.3 KB) - ARM template
├── createUiDefinition.json    (2.09 KB) - Portal UI definition
└── README.md                  (this file)
```

---

## ✅ Pre-Upload Checklist

### Template Validation
- [x] mainTemplate.json generated from main.bicep
- [x] All parameters properly defined
- [x] Default values set for optional parameters
- [x] Secure strings for secrets
- [x] Resource naming with uniqueString()
- [x] Dependencies properly configured

### UI Definition
- [x] Valid JSON schema
- [x] Basics section with appName
- [x] Azure Configuration step
- [x] InfoBox with instructions
- [x] Subscription ID input
- [x] Outputs mapped correctly

### Resource Quotas (Brazil South)
- [x] vCPU Dv3: 10 (APPROVED ✅)
- [x] Storage accounts: 250 (default)
- [x] Cosmos DB: 50 (default)
- [x] App Services: 100 (default)

---

## 🚀 Upload Instructions

### 1. Access Partner Center
```
https://partner.microsoft.com/dashboard/marketplace-offers/overview
```

### 2. Navigate to Technical Configuration
- Go to your Azure SmartCost offer
- Click "Technical configuration"
- Scroll to "Package details"

### 3. Upload Package Files

**Option A: Individual Upload**
- Upload `mainTemplate.json`
- Upload `createUiDefinition.json`
- Click "Save draft"

**Option B: ZIP Upload**
```powershell
# Create ZIP package
Compress-Archive -Path mainTemplate.json, createUiDefinition.json -DestinationPath smartcost-v1.1.zip

# Upload smartcost-v1.1.zip in Partner Center
```

### 4. Validate Package
- Click "Validate"
- Wait for validation results
- Fix any errors reported
- Re-upload if needed

### 5. Test Deployment
- Click "Preview deployment"
- Select test subscription: `e6b85c41-c45d-42a5-955f-d4dfb3b13ce9`
- Select resource group: `rg-smartcost-test`
- Click "Deploy"
- Verify all resources created successfully

---

## 🧪 Testing Checklist

### Post-Deployment Tests
- [ ] Function App deployed and running
- [ ] API App Service responding
- [ ] Cosmos DB created with containers
- [ ] Storage account accessible
- [ ] Key Vault created
- [ ] Application Insights logging
- [ ] Redis cache connected
- [ ] All app settings configured

### Functional Tests
- [ ] Navigate to Function App URL
- [ ] Test API health endpoint: `/api/health`
- [ ] Test marketplace landing: `/api/marketplace/landing?token=test`
- [ ] Test webhook: `/api/marketplace/webhook` (POST)
- [ ] Verify Cosmos DB containers:
  - Tenants
  - Users
  - Costs
  - BudgetAlerts
  - PushSubscriptions

### Marketplace Integration Tests
- [ ] Landing page resolves token
- [ ] Subscription activation works
- [ ] Webhook events processed
- [ ] Tenant created in Cosmos DB

---

## 🔧 Common Validation Errors & Fixes

### Error: "Template validation failed"
**Cause:** Invalid JSON syntax or schema  
**Fix:** Validate JSON with `az deployment group validate`

### Error: "Parameter not defined"
**Cause:** Output references non-existent parameter  
**Fix:** Check createUiDefinition.json outputs section

### Error: "Resource quota exceeded"
**Cause:** vCPU quota insufficient  
**Fix:** ✅ Already approved (10 vCPU Dv3)

### Error: "Unique name conflict"
**Cause:** Resource name already exists  
**Fix:** Uses `uniqueString()` - should auto-resolve

### Error: "Invalid location"
**Cause:** Resource not available in selected region  
**Fix:** Ensure all resources support Brazil South

---

## 📋 Validation Commands

### Local Validation (Before Upload)
```powershell
# Validate ARM template syntax
az deployment group validate `
  --resource-group rg-smartcost-test `
  --template-file mainTemplate.json `
  --parameters projectName=smartcost `
               jwtSecret=test-secret-123 `
               azureAdClientId=00000000-0000-0000-0000-000000000000 `
               azureAdClientSecret=test-secret-456 `
               marketplaceClientId=00000000-0000-0000-0000-000000000000 `
               marketplaceClientSecret=test-secret-789 `
               marketplacePublisherId=smartcoast

# Validate createUiDefinition
# (Use Portal sandbox: https://portal.azure.com/?feature.customPortal=false&#blade/Microsoft_Azure_CreateUIDef/SandboxBlade)
```

### Test Deployment
```powershell
# Deploy to test resource group
az deployment group create `
  --name smartcost-test-deployment `
  --resource-group rg-smartcost-test `
  --template-file mainTemplate.json `
  --parameters projectName=smartcosttest `
               subscriptionId=e6b85c41-c45d-42a5-955f-d4dfb3b13ce9 `
               jwtSecret=test-jwt-secret-$(New-Guid) `
               azureAdClientId=00000000-0000-0000-0000-000000000000 `
               azureAdClientSecret=test-ad-secret-$(New-Guid) `
               marketplaceClientId=00000000-0000-0000-0000-000000000000 `
               marketplaceClientSecret=test-mp-secret-$(New-Guid) `
               marketplacePublisherId=smartcoast
```

---

## 🎯 Expected Resources After Deployment

### Resource Group Contents
```
rg-smartcost-test/
├── smartcosttest-func-dev-xxxxx (Function App)
├── smartcosttest-api-dev-xxxxx (App Service)
├── smartcosttest-web-dev-xxxxx (Static Web App)
├── smartcostteststgxxxxx (Storage Account)
├── smartcosttest-cosmos-dev-xxxxx (Cosmos DB)
├── smartcosttest-kv-dev-xxxxx (Key Vault)
├── smartcosttest-redis-dev-xxxxx (Redis Cache)
├── smartcosttest-func-dev-xxxxx-insights (App Insights)
└── smartcosttest-func-dev-xxxxx-logs (Log Analytics)
```

### Total Resources: 9
### Estimated Cost: ~$50-100/month (dev tier)

---

## 📊 Marketplace Listing Info

### Offer Details
- **Offer ID:** azure-smartcost
- **Publisher ID:** smartcoast
- **Offer Type:** Azure Application (Solution Template)
- **Pricing Model:** Bring Your Own License (BYOL)
- **Categories:** Developer Tools, Monitoring + Management
- **Industries:** All

### Plans
- **Plan ID:** standard-plan
- **Plan Name:** Standard Plan
- **Pricing:** Free (pay Azure resources only)

### Regions Supported
- Brazil South ✅
- East US
- West Europe
- (Add more after validation)

---

## 🚨 Important Notes

1. **Quotas:** vCPU Dv3 quota approved for 10 in Brazil South
2. **Naming:** All resources use `uniqueString()` to avoid conflicts
3. **Security:** Secrets passed as secure parameters (never stored in template)
4. **SKUs:** Using B1 for App Service (can scale up later)
5. **Cosmos DB:** Serverless mode (no throughput provisioning needed)
6. **Redis:** Basic C0 tier for dev (upgrade to Standard for prod)

---

## 📞 Support

- **Technical Issues:** Review deployment logs in Portal
- **Validation Errors:** Check ARM template validation output
- **Quota Issues:** Already resolved (approved)
- **Partner Center:** https://aka.ms/marketplacesupport

---

## 🎉 Next Steps After Successful Upload

1. ✅ Validate package in Partner Center
2. ✅ Test deployment in sandbox subscription
3. ✅ Submit for Microsoft certification
4. ⏳ Wait 3-5 business days for approval
5. 🚀 Go live on Marketplace!

---

**Package ready for upload!** 🚀

Upload to Partner Center → Validate → Test → Submit for Certification
