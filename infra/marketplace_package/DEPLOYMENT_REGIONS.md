# 🌍 Azure SmartCost - Recommended Deployment Regions

## ✅ **RECOMMENDED REGIONS** (Full Feature Support)

Deploy to these regions for complete functionality including Static Web App:

### Americas 🇺🇸
- **eastus2** - East US 2 (Virginia) ⭐ **RECOMMENDED**
- **centralus** - Central US (Iowa)
- **westus2** - West US 2 (Washington)

### Europe 🇪🇺
- **westeurope** - West Europe (Netherlands) ⭐ **RECOMMENDED**

### Asia 🌏
- **eastasia** - East Asia (Hong Kong)

---

## ⚠️ Other Regions (Limited Support)

If you deploy to **non-recommended regions** (e.g., `brazilsouth`, `canadacentral`, `uksouth`):

### What Works ✅
- Function App (Consumption Y1)
- API App Service (Basic B1)
- Cosmos DB (Serverless)
- Storage Account
- Key Vault
- Redis Cache
- Application Insights
- All backend functionality

### What Changes ⚠️
- **Static Web App** will auto-deploy to **eastus2**
- Dashboard location different from API location
- Slightly higher latency between dashboard ↔ API (~50-100ms)

---

## 💼 Enterprise Considerations

### VM Quotas
**For recommended regions**, most enterprise Azure subscriptions already have adequate quotas:
- Basic B1 VM: 1 vCore required
- Consumption Functions: No quota needed

### When You Might Need Quota Increase
- Brand new Azure subscriptions
- Free trial accounts
- Subscriptions with custom quota policies

**Solution:** Use recommended regions first. They typically have higher default quotas.

---

## 🚀 Deployment Strategy

### Recommended Approach
1. **Start with `eastus2`** or **`westeurope`**
2. All resources in same region = lowest latency
3. No quota issues for most enterprise subscriptions

### Alternative Approach (Multi-Region)
If you have specific regional requirements:
1. Deploy backend to your preferred region
2. Static Web App auto-deploys to nearest supported region
3. Configure CORS and API endpoints accordingly

---

## 📊 Performance Comparison

| Scenario | All in eastus2 | Backend brazilsouth + Dashboard eastus2 |
|----------|---------------|------------------------------------------|
| Dashboard Load Time | ~500ms | ~500ms (cached) |
| API Call Latency | ~20ms | ~70ms (+50ms cross-region) |
| Cost | $X/month | $X/month (same) |
| Complexity | Simple ✅ | Moderate ⚠️ |

**Recommendation:** Use recommended regions unless you have specific compliance/data residency requirements.

---

## 🔍 How to Check Your Subscription Quotas

```bash
# Check available quotas in a region
az vm list-usage --location eastus2 --output table | grep "Standard Bs Family"

# Check if you can deploy B1 (needs 1 core)
az vm list-skus --location eastus2 --size Standard_B1 --output table
```

**If you see:**
- Current: 0, Limit: 10+ → ✅ **Ready to deploy**
- Current: 0, Limit: 0 → ⚠️ **Need quota increase**

---

## 📝 Summary

✅ **Use recommended regions** → No issues, full features
⚠️ **Use other regions** → Works, but dashboard in different location
❌ **Don't use** → Regions without any App Service support

**For 95% of deployments: Choose `eastus2` or `westeurope` and you're good to go! 🚀**
