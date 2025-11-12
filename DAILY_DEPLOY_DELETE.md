# 🚀 Daily Deploy & Delete - Quick Reference

## ☀️ MORNING - Deploy (~10 min)

```powershell
# Quick test (Free tier, minimal)
.\scripts\deploy-production-demo.ps1 -QuickMode

# Full test (all features)
.\scripts\deploy-production-demo.ps1
```

**What happens:**
- ✅ Creates resource group: `rg-smartcost-demo`
- ✅ Deploys infrastructure (App Service, Storage, etc.)
- ✅ Builds and deploys API
- ✅ Configures secrets in Key Vault
- ✅ Runs health checks
- ✅ Shows API URL and costs

**Output**: API URL like `https://smartcost-api-demo-abc123.azurewebsites.net`

---

## 🌙 NIGHT - Delete (~30 seconds)

```powershell
# Delete everything
.\scripts\delete-demo.ps1 -Force
```

**One-liner alternative:**
```powershell
az group delete --name rg-smartcost-demo --yes --no-wait
```

**What happens:**
- 🗑️ Deletes ALL resources in the group
- 💰 Saves ~$2/day
- ⏱️ Completes in ~5-10 minutes (background)

---

## 💰 Costs

| Mode | Per Day | If Forgotten Overnight |
|------|---------|------------------------|
| Quick | $0.50 | $0.50 |
| Full | $2.00 | $2.00 |
| Production | $5.60 | $5.60 |

**Strategy**: Delete every night = Max $2/day for unlimited testing!

---

## 📋 Commands Cheat Sheet

### Deploy
```powershell
# Basic
.\scripts\deploy-production-demo.ps1

# Quick mode (free tier)
.\scripts\deploy-production-demo.ps1 -QuickMode

# Custom location
.\scripts\deploy-production-demo.ps1 -Location brazilsouth

# Skip tests
.\scripts\deploy-production-demo.ps1 -SkipTests
```

### Delete
```powershell
# With confirmation
.\scripts\delete-demo.ps1

# No confirmation
.\scripts\delete-demo.ps1 -Force

# Direct command
az group delete --name rg-smartcost-demo --yes --no-wait
```

### Check Status
```powershell
# List resources
az resource list --resource-group rg-smartcost-demo -o table

# Check costs
az consumption usage list --resource-group rg-smartcost-demo

# View in portal
start https://portal.azure.com/#@/resource/subscriptions/YOUR-SUB/resourceGroups/rg-smartcost-demo
```

---

## ⚡ Quick Workflows

### 1. Test Feature (2 hours)
```powershell
.\scripts\deploy-production-demo.ps1 -QuickMode
# Test feature...
.\scripts\delete-demo.ps1 -Force
```
💰 Cost: ~$0.05

### 2. Full Day Development
```powershell
.\scripts\deploy-production-demo.ps1
# Work all day...
.\scripts\delete-demo.ps1 -Force
```
💰 Cost: ~$2

### 3. Week of Testing
```powershell
# Monday morning
.\scripts\deploy-production-demo.ps1 -Environment week1

# Use Mon-Fri

# Friday night
.\scripts\delete-demo.ps1 -Environment week1 -Force
```
💰 Cost: ~$10 (5 days)

---

## 🎯 Pro Tips

✅ **Set calendar reminder**: "Delete Azure Demo" at 11 PM daily  
✅ **Use Quick mode** for simple tests ($0.50 vs $2)  
✅ **Check orphaned RGs weekly**: `az group list`  
✅ **Monitor costs**: Portal → Cost Management  

❌ **Never leave running** if not using  
❌ **Don't use Premium tiers** for demo  
❌ **Don't forget old resource groups**  

---

**Ready?** Run when subscription is enabled! 🚀
