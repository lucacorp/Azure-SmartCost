# Azure SmartCost - Support & Help

## 📞 Contact Support

### Primary Support Channels

**Email Support**  
📧 **lucacorp1@outlook.com**  
- Response time: 24-48 hours
- Include: Product ID, screenshot, error details

**GitHub Issues** (Technical Support)  
🐛 https://github.com/lucacorp/Azure-SmartCost/issues  
- For bugs, feature requests, technical questions
- Public community support
- Response time: 1-3 business days

**GitHub Discussions** (Community)  
💬 https://github.com/lucacorp/Azure-SmartCost/discussions  
- General questions
- Best practices
- Community-driven support

---

## 🚀 Quick Start Resources

### Getting Started
1. **Installation Guide**: https://github.com/lucacorp/Azure-SmartCost/blob/main/docs/DEPLOYMENT_GUIDE.md
2. **Configuration**: https://github.com/lucacorp/Azure-SmartCost/blob/main/CONFIGURATION.md
3. **Video Tutorial**: (Coming soon)

### Common Tasks
- **Setup Budget Alerts**: [Email Setup Guide](https://github.com/lucacorp/Azure-SmartCost/blob/main/EMAIL_SETUP.md)
- **Power BI Integration**: [PowerBI Setup](https://github.com/lucacorp/Azure-SmartCost/blob/main/POWERBI_SETUP.md)
- **Troubleshooting**: https://github.com/lucacorp/Azure-SmartCost/blob/main/docs/TROUBLESHOOTING.md

---

## 📚 Documentation

### Full Documentation
https://github.com/lucacorp/Azure-SmartCost/tree/main/docs

**Key Documents:**
- [Architecture Overview](https://github.com/lucacorp/Azure-SmartCost/blob/main/docs/ARCHITECTURE.md)
- [API Documentation](https://github.com/lucacorp/Azure-SmartCost/blob/main/docs/API_DOCUMENTATION.md)
- [Pricing Strategy](https://github.com/lucacorp/Azure-SmartCost/blob/main/docs/PRICING_STRATEGY.md)

---

## ❓ Frequently Asked Questions

### General Questions

**Q: How much does Azure SmartCost cost?**  
A: The application itself is free. You only pay for Azure infrastructure costs (estimated $20-50/month for Function App, Cosmos DB, and Storage). Premium features and support may require additional licensing.

**Q: What data does SmartCost access?**  
A: Only cost data from Azure Cost Management API (read-only). We do NOT access resource data, VM contents, databases, or any sensitive information.

**Q: Is my data secure?**  
A: Yes. All data is encrypted at rest (Cosmos DB) and in transit (HTTPS/TLS 1.2+). Azure AD authentication is required. We only request "Cost Management Reader" role (least privilege).

### Technical Questions

**Q: Why is the dashboard not showing costs?**  
A: 
1. Ensure "Cost Management Reader" role is assigned to the Managed Identity
2. Wait 5-10 minutes after deployment for initial data sync
3. Check Application Insights logs for errors
4. Verify subscription ID is correct

**Q: Email alerts not working?**  
A:
1. Configure `SENDGRID_API_KEY` in Function App settings
2. Verify email address in alert configuration
3. Check spam folder
4. Review EMAIL_SETUP.md for detailed setup

**Q: Daily trend chart is empty?**  
A: Azure Cost Management API needs 24-48 hours to populate historical daily data. This is normal for new deployments.

**Q: How do I add multiple subscriptions?**  
A: Currently, each deployment monitors one subscription. For multi-subscription support, deploy multiple instances or contact us for enterprise licensing.

---

## 🐛 Report a Bug

### Before Reporting
1. Check [existing issues](https://github.com/lucacorp/Azure-SmartCost/issues)
2. Review [troubleshooting guide](https://github.com/lucacorp/Azure-SmartCost/blob/main/docs/TROUBLESHOOTING.md)
3. Collect error logs from Application Insights

### Bug Report Template
```markdown
**Describe the bug**
Clear description of the issue

**To Reproduce**
Steps to reproduce:
1. Go to '...'
2. Click on '...'
3. See error

**Expected behavior**
What should happen

**Screenshots**
If applicable

**Environment:**
- Azure Region:
- Subscription ID (first 8 chars only):
- SmartCost Version:
- Browser (if frontend issue):

**Logs**
Application Insights query results or error messages
```

Submit to: https://github.com/lucacorp/Azure-SmartCost/issues/new

---

## 💡 Feature Requests

Have an idea to improve Azure SmartCost?

1. Check [feature requests](https://github.com/lucacorp/Azure-SmartCost/issues?q=is%3Aissue+is%3Aopen+label%3Aenhancement)
2. If not exists, create new: https://github.com/lucacorp/Azure-SmartCost/issues/new
3. Label: `enhancement`

---

## 🆘 Emergency Support

For critical production issues:

**Email**: lucacorp1@outlook.com (Subject: URGENT - Production Issue)  
Include:
- Product ID: 488c761f-546a-4d71-afb1-cf332d94ca40
- Severity: Critical/High/Medium/Low
- Impact: Number of users/subscriptions affected
- Error logs from Application Insights

---

## 🤝 Community & Contribute

### Join the Community
- **GitHub Discussions**: https://github.com/lucacorp/Azure-SmartCost/discussions
- **Star the project**: https://github.com/lucacorp/Azure-SmartCost

### Contributing
We welcome contributions! See [CONTRIBUTING.md](https://github.com/lucacorp/Azure-SmartCost/blob/main/CONTRIBUTING.md)

---

## 📞 Commercial Licensing

For enterprise features, custom SLAs, or white-label solutions:

**Email**: lucacorp1@outlook.com  
**Subject**: Enterprise Licensing Inquiry

Enterprise features include:
- Multi-subscription support
- Custom branding
- Dedicated support (SLA)
- Priority feature requests
- On-premises deployment options

---

## 📖 Additional Resources

### Microsoft Azure Support
- **Azure Cost Management**: https://docs.microsoft.com/azure/cost-management-billing/
- **Azure Support Plans**: https://azure.microsoft.com/support/plans/

### Third-Party Services
- **SendGrid Support**: https://support.sendgrid.com/
- **Power BI Support**: https://support.powerbi.com/

---

**Last Updated**: November 20, 2025  
**Support Portal Version**: 1.0
