# Getting Started with Azure SmartCost

Welcome to Azure SmartCost! This guide will help you get up and running with your FinOps platform in minutes.

## What is Azure SmartCost?

Azure SmartCost is a comprehensive cloud cost management platform that helps you:
- 📊 **Monitor** your Azure spending in real-time
- 💰 **Optimize** costs with AI-powered recommendations
- 🎯 **Budget** and set alerts to prevent overspending
- 📈 **Forecast** future costs with machine learning
- 👥 **Collaborate** with your team on cost optimization

---

## Step 1: Sign Up

### Option A: Azure Marketplace (Recommended)

1. **Find Azure SmartCost in the Marketplace**
   - Go to [Azure Portal](https://portal.azure.com)
   - Navigate to **Marketplace**
   - Search for "Azure SmartCost"

2. **Subscribe**
   - Click **Get It Now**
   - Select your subscription plan:
     - **Free**: Up to $1,000/month spend (perfect for testing)
     - **Basic**: Up to $10,000/month ($49/month)
     - **Premium**: Unlimited ($199/month)
   - Click **Subscribe**

3. **Configure**
   - Select Azure subscription to monitor
   - Choose resource group
   - Set up admin account
   - Click **Review + Subscribe**

4. **Activate**
   - Wait 2-5 minutes for provisioning
   - You'll receive an email when ready
   - Click the activation link

### Option B: Direct Registration

1. Visit [app.smartcost.com/signup](https://app.smartcost.com/signup)
2. Sign in with your **Microsoft work account**
3. Grant permissions to read cost data
4. Choose your subscription plan
5. Complete payment (if applicable)

---

## Step 2: Connect Your Azure Subscription

### Automatic Setup (Marketplace Users)
If you subscribed via Azure Marketplace, your subscription is already connected! Skip to Step 3.

### Manual Setup

1. **Navigate to Settings**
   - Click your profile picture (top right)
   - Select **Settings** → **Azure Subscriptions**

2. **Add Subscription**
   ```
   Click "Add Azure Subscription"
   
   You'll be redirected to Azure for authentication
   ↓
   Sign in with account that has "Reader" role on subscription
   ↓
   Grant permissions:
   - Read cost and usage data
   - Read resource information
   - Read tags
   ↓
   Select subscriptions to monitor
   ↓
   Click "Connect"
   ```

3. **Verify Connection**
   - Status should show "✅ Connected"
   - First data sync takes 15-30 minutes
   - You'll see a notification when complete

### Required Permissions

Your Azure account needs these permissions:
```
- Cost Management Reader (to read cost data)
- Reader (to read resource metadata)
```

**How to grant permissions:**
```bash
# Azure CLI
az role assignment create \
  --assignee "user@company.com" \
  --role "Cost Management Reader" \
  --scope "/subscriptions/{subscription-id}"
```

Or via Azure Portal:
1. Go to **Subscriptions** → Your Subscription
2. Click **Access Control (IAM)**
3. Click **Add role assignment**
4. Select **Cost Management Reader**
5. Add the user/service principal

---

## Step 3: Explore Your Dashboard

### Overview Dashboard

When you first log in, you'll see:

1. **Total Spend (This Month)**
   - Current month-to-date spending
   - Comparison vs. last month
   - Trend indicator (↑ increasing / ↓ decreasing)

2. **Cost by Service**
   - Pie chart showing top spending services
   - Click any slice to drill down

3. **Daily Spend Trend**
   - Line chart showing daily costs
   - Forecast for next 7 days

4. **Top Resources**
   - List of most expensive resources
   - Quick actions to optimize

### Quick Actions

**View Cost Details**
```
Click any chart → See detailed breakdown
Filter by:
- Date range
- Service (VM, Storage, Database, etc.)
- Resource group
- Location
- Tags
```

**Set Up Your First Budget**
```
1. Click "Budgets" in sidebar
2. Click "Create Budget"
3. Enter details:
   Name: "Monthly Budget"
   Amount: $5,000
   Period: Monthly
   Alert at: 80%, 90%, 100%
4. Click "Create"
```

**Create Cost Alert**
```
1. Click "Alerts" in sidebar
2. Click "New Alert"
3. Choose condition:
   - Daily spend > $500
   - Anomaly detected
   - Budget threshold reached
4. Add notification email
5. Click "Save"
```

---

## Step 4: Invite Your Team

1. **Go to Team Management**
   - Settings → Team Members

2. **Add Members**
   ```
   Click "Invite Member"
   
   Enter email: teammate@company.com
   Select role:
   - Admin: Full access
   - Manager: View and edit budgets/alerts
   - Viewer: Read-only access
   
   Click "Send Invitation"
   ```

3. **Team Member Receives Email**
   - Click invitation link
   - Sign in with Microsoft account
   - Accept invitation
   - Immediately get access

### Role Permissions

| Action | Admin | Manager | Viewer |
|--------|-------|---------|--------|
| View costs | ✅ | ✅ | ✅ |
| Create budgets | ✅ | ✅ | ❌ |
| Edit budgets | ✅ | ✅ | ❌ |
| Delete budgets | ✅ | ❌ | ❌ |
| Invite members | ✅ | ❌ | ❌ |
| Change settings | ✅ | ❌ | ❌ |
| Export data | ✅ | ✅ | ✅ |

---

## Step 5: Set Up Alerts

### Budget Alert

1. Navigate to **Budgets** → Select a budget
2. Click **Edit**
3. Scroll to **Alert Thresholds**
4. Add thresholds:
   ```
   50%  → Warning (email notification)
   80%  → Critical (email + push notification)
   100% → Over budget (email + webhook)
   ```
5. Add notification emails
6. Click **Save**

### Anomaly Detection (Premium)

Automatically detect unusual spending:

1. Go to **Settings** → **Alerts**
2. Enable **Anomaly Detection**
3. Configure sensitivity:
   - High: Alert on 20%+ deviation
   - Medium: Alert on 50%+ deviation
   - Low: Alert on 100%+ deviation
4. Click **Save**

Example alert:
```
🚨 Cost Anomaly Detected

Service: Azure Virtual Machines
Date: January 15, 2025
Expected: $450
Actual: $1,200
Deviation: +167%

Possible causes:
- VM instance count increased
- Larger VM sizes deployed
- Higher usage hours

Recommended actions:
→ Review recent deployments
→ Check autoscaling configuration
→ Consider reserved instances
```

---

## Step 6: Install Mobile App (PWA)

### Android / Chrome

1. Open [app.smartcost.com](https://app.smartcost.com) in Chrome
2. Look for install prompt at bottom of screen
3. Click **Install**
4. App icon appears on home screen
5. Open for native-like experience

### iPhone / iPad

1. Open [app.smartcost.com](https://app.smartcost.com) in Safari
2. Tap **Share** button (square with arrow)
3. Scroll down and tap **Add to Home Screen**
4. Tap **Add**
5. App icon appears on home screen

### Desktop (Windows/Mac)

1. Open [app.smartcost.com](https://app.smartcost.com) in Chrome/Edge
2. Look for install icon in address bar
3. Click **Install**
4. App opens in standalone window

**Benefits of Installing:**
- ⚡ Faster loading
- 📴 Offline access to cached data
- 🔔 Push notifications for alerts
- 📱 Native app experience

---

## Step 7: Enable Push Notifications

1. **Grant Permission**
   ```
   When prompted, click "Allow" for notifications
   
   If you missed it:
   - Chrome: Settings → Site Settings → Notifications
   - Safari: Preferences → Websites → Notifications
   ```

2. **Configure Notifications**
   - Go to **Settings** → **Notifications**
   - Choose what to receive:
     - Budget alerts
     - Anomaly detection
     - Weekly summary
     - Daily digest
   
3. **Test Notification**
   - Click **Send Test Notification**
   - You should see a notification appear

---

## Next Steps

### Recommended Actions

**Week 1: Baseline**
- [ ] Connect all Azure subscriptions
- [ ] Review current spending by service
- [ ] Identify top 10 most expensive resources
- [ ] Create monthly budget

**Week 2: Optimize**
- [ ] Review optimization recommendations
- [ ] Right-size underutilized VMs
- [ ] Delete unused resources
- [ ] Consider reserved instances

**Week 3: Automate**
- [ ] Set up budget alerts
- [ ] Enable anomaly detection
- [ ] Configure weekly reports
- [ ] Integrate with Slack/Teams (webhook)

**Week 4: Report**
- [ ] Generate monthly cost report
- [ ] Share dashboard with leadership
- [ ] Schedule executive review meeting
- [ ] Set cost optimization goals

### Learning Resources

📚 **Documentation**
- [Cost Analytics Guide](./cost-analytics-guide.md)
- [Budget & Alerts Setup](./budgets-alerts-guide.md)
- [API Documentation](../API_DOCUMENTATION.md)

🎥 **Video Tutorials**
- Getting Started (5 min)
- Setting Up Budgets (10 min)
- Advanced Analytics (15 min)

💬 **Support**
- Email: support@smartcost.com
- Live Chat: Available in app (bottom right)
- Community Forum: https://community.smartcost.com

---

## Frequently Asked Questions

### How often is cost data updated?
Cost data syncs every 6 hours from Azure Cost Management API. The last update time is shown in the dashboard header.

### Can I connect multiple Azure subscriptions?
Yes! Premium and Enterprise plans support unlimited subscriptions. Free and Basic plans support up to 5.

### Is my data secure?
Absolutely. We use:
- Azure AD authentication (no passwords stored)
- Encryption at rest and in transit (TLS 1.2+)
- Azure Key Vault for secrets
- SOC 2 Type II certified

### Can I export my data?
Yes. Go to **Reports** → **Export** and choose format (CSV, PDF, Excel).

### How do I cancel my subscription?
Settings → Billing → Cancel Subscription. Your data is retained for 30 days.

### What happens after the free trial?
After 30 days, you can:
- Upgrade to a paid plan
- Continue with limited Free tier
- Cancel (no charges)

---

## Get Help

**Something not working?**
1. Check [Troubleshooting Guide](../TROUBLESHOOTING.md)
2. Search [Knowledge Base](https://help.smartcost.com)
3. Contact Support: support@smartcost.com

**Want to give feedback?**
We'd love to hear from you! feedback@smartcost.com

---

Welcome to smarter cloud cost management! 🚀
