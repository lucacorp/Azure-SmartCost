# Privacy Policy for Azure SmartCost

**Effective Date:** November 20, 2025  
**Last Updated:** November 20, 2025

## 1. Introduction

SmartCoast ("we", "our", or "us") operates the Azure SmartCost application (the "Service"). This Privacy Policy explains how we collect, use, disclose, and safeguard your information when you use our Service.

## 2. Information We Collect

### 2.1 Information You Provide
- **Azure Subscription ID**: Used to fetch cost data from Azure Cost Management API
- **Email Address**: Optional, used only for budget alert notifications
- **Budget Configurations**: Alert thresholds, budget limits, and notification preferences

### 2.2 Automatically Collected Information
- **Azure Cost Data**: Retrieved from Azure Cost Management API (read-only access)
- **Usage Analytics**: Application Insights logs for performance monitoring
- **Authentication Data**: Azure AD tokens (not stored, only validated)

### 2.3 Information We Do NOT Collect
- ❌ Resource data (VM details, database contents, etc.)
- ❌ Personal data from your Azure resources
- ❌ Credentials or secrets
- ❌ Payment information (billing handled by Microsoft Azure)

## 3. How We Use Your Information

We use collected information solely to:
- Display cost analytics and trends
- Send budget alert notifications (if configured)
- Improve Service performance and reliability
- Troubleshoot technical issues

## 4. Data Storage and Security

### 4.1 Data Storage
- **Location**: Data stored in Azure Cosmos DB (encrypted at rest)
- **Region**: Same region as your deployment (configurable)
- **Retention**: Budget configurations stored indefinitely; cost data cached for 90 days

### 4.2 Security Measures
- ✅ **Encryption at Rest**: Azure Cosmos DB built-in encryption
- ✅ **Encryption in Transit**: HTTPS/TLS 1.2+ only
- ✅ **Authentication**: Azure AD (Entra ID) authentication required
- ✅ **Access Control**: Role-Based Access Control (RBAC)
- ✅ **Least Privilege**: Only "Cost Management Reader" role assigned

## 5. Data Sharing and Disclosure

We **DO NOT**:
- ❌ Sell your data to third parties
- ❌ Share data with advertisers
- ❌ Use data for marketing purposes

We **MAY** disclose data:
- ✅ To comply with legal obligations
- ✅ To Microsoft Azure (infrastructure provider)
- ✅ To SendGrid (for email alerts, if configured)

## 6. Third-Party Services

### 6.1 Microsoft Azure
- **Purpose**: Infrastructure hosting, cost data retrieval
- **Privacy Policy**: https://privacy.microsoft.com/

### 6.2 SendGrid (Optional)
- **Purpose**: Email alert delivery
- **Privacy Policy**: https://www.twilio.com/legal/privacy
- **Note**: Only used if you configure email alerts

## 7. Your Rights (GDPR Compliance)

If you are in the European Economic Area (EEA), you have the right to:
- ✅ **Access**: Request a copy of your data
- ✅ **Rectification**: Correct inaccurate data
- ✅ **Erasure**: Delete your data ("right to be forgotten")
- ✅ **Portability**: Export your data in JSON format
- ✅ **Object**: Opt-out of email notifications

To exercise these rights, contact: **lucacorp1@outlook.com**

## 8. Data Retention

- **Budget Configurations**: Retained until you delete them
- **Cost Data Cache**: Automatically deleted after 90 days
- **Application Logs**: Retained for 30 days in Application Insights

## 9. Children's Privacy

Our Service is not directed to individuals under 18 years of age. We do not knowingly collect personal information from children.

## 10. California Privacy Rights (CCPA)

California residents have additional rights under the California Consumer Privacy Act (CCPA):
- Right to know what personal information is collected
- Right to delete personal information
- Right to opt-out of data sales (we do not sell data)

## 11. International Data Transfers

Data is stored in the Azure region you select during deployment. Cross-border transfers comply with:
- EU-US Data Privacy Framework
- Standard Contractual Clauses (SCCs)
- Azure data residency commitments

## 12. Changes to This Privacy Policy

We may update this Privacy Policy periodically. Changes will be posted at:
- **GitHub**: https://github.com/lucacorp/Azure-SmartCost/blob/main/PRIVACY_POLICY.md
- **Notification**: Via email (if configured) for material changes

## 13. Contact Information

For privacy-related questions or concerns:

**Email**: lucacorp1@outlook.com  
**GitHub**: https://github.com/lucacorp/Azure-SmartCost/issues  
**Address**: SmartCoast (provide full address if required for EU compliance)

## 14. Compliance Certifications

- ✅ **GDPR Compliant** (EU General Data Protection Regulation)
- ✅ **Azure Security Benchmark** Aligned
- ✅ **SOC 2 Type II** (Azure infrastructure certified)

## 15. Open Source Transparency

Azure SmartCost is open source. You can inspect our data handling practices at:
https://github.com/lucacorp/Azure-SmartCost

---

**Last Reviewed**: November 20, 2025  
**Version**: 1.0
