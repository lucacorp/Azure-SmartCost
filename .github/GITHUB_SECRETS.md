Adicionar salvamento no Cosmos DB (remover comentários do código)
Atualizar landing page com novo endpoint da Function
Testar signup end-to-end

Este documento lista todos os secrets necessários para os workflows de CI/CD do Azure SmartCost.

## 📋 Required Secrets

Configure os seguintes secrets em **Settings → Secrets and variables → Actions** no GitHub:

### Azure Infrastructure Secrets

| Secret Name | Description | Example / How to Get |
|------------|-------------|---------------------|
| `AZURE_CREDENTIALS` | Azure Service Principal credentials | JSON com `clientId`, `clientSecret`, `subscriptionId`, `tenantId` |
| `AZURE_SUBSCRIPTION_ID` | Azure Subscription ID | Portal Azure → Subscriptions → Subscription ID |
| `AZURE_RESOURCE_GROUP` | Azure Resource Group name | `smartcost-rg-prod` |
| `AZURE_WEBAPP_NAME` | Azure App Service name for API | `smartcost-api-prod` |
| `AZURE_FUNCTIONAPP_NAME` | Azure Functions App name | `smartcost-func-prod` |
| `KEYVAULT_NAME` | Azure Key Vault name | `smartcost-kv-prod` |

### Application Secrets

| Secret Name | Description | Example / How to Get |
|------------|-------------|---------------------|
| `JWT_SECRET` | Secret key for JWT token generation | Use: `openssl rand -base64 32` |
| `AZURE_AD_CLIENT_ID` | Azure AD Application Client ID | Azure Portal → App Registrations → Application (client) ID |
| `AZURE_AD_CLIENT_SECRET` | Azure AD Application Client Secret | Azure Portal → App Registrations → Certificates & secrets |
| `COSMOS_CONNECTION_STRING` | Cosmos DB connection string | Azure Portal → Cosmos DB → Keys → Primary Connection String |
| `APPINSIGHTS_CONNECTION_STRING` | Application Insights connection string | Azure Portal → Application Insights → Properties |

### Stripe Secrets

| Secret Name | Description | How to Get |
|------------|-------------|-----------|
| `STRIPE_API_KEY` | Stripe Secret API Key | Stripe Dashboard → Developers → API keys → Secret key |
| `STRIPE_PUBLISHABLE_KEY` | Stripe Publishable Key | Stripe Dashboard → Developers → API keys → Publishable key |
| `STRIPE_WEBHOOK_SECRET` | Stripe Webhook Signing Secret | Stripe Dashboard → Developers → Webhooks → Signing secret |

### Frontend Deployment

| Secret Name | Description | How to Get |
|------------|-------------|-----------|
| `AZURE_STATIC_WEB_APPS_API_TOKEN_SMARTCOST` | Azure Static Web Apps deployment token | Azure Portal → Static Web App → Manage deployment token |
| `STATIC_WEB_APP_URL` | Static Web App URL | Azure Portal → Static Web App → URL |

### Optional - Code Quality

| Secret Name | Description | How to Get |
|------------|-------------|-----------|
| `SONAR_TOKEN` | SonarCloud authentication token | SonarCloud.io → Account → Security → Generate token |

---

## 🔧 How to Create Azure Service Principal

Para criar o `AZURE_CREDENTIALS`:

```bash
# Login to Azure
az login

# Create Service Principal with Contributor role
az ad sp create-for-rbac \
  --name "smartcost-github-actions" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group} \
  --sdk-auth
```

Isso retornará um JSON no formato:

```json
{
  "clientId": "<GUID>",
  "clientSecret": "<STRING>",
  "subscriptionId": "<GUID>",
  "tenantId": "<GUID>",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

**Copie todo esse JSON e adicione como secret `AZURE_CREDENTIALS`**

---

## 🔐 Security Best Practices

1. **Never commit secrets to source code**
2. **Use environment-specific secrets** (dev, staging, prod)
3. **Rotate secrets regularly** (every 90 days recommended)
4. **Use GitHub Environments** for additional protection
5. **Enable required reviewers** for production deployments
6. **Monitor secret usage** in GitHub Actions logs

---

## 🌍 Environment-Specific Configuration

Recomendamos criar **GitHub Environments** para cada ambiente:

### Development Environment
- Name: `dev`
- Protection rules: None
- Secrets: Use `-dev` suffix

### Staging Environment
- Name: `staging`
- Protection rules: Optional reviewers
- Secrets: Use `-staging` suffix

### Production Environment
- Name: `prod`
- Protection rules: **Required reviewers** (at least 1)
- Secrets: Use `-prod` suffix

---

## ✅ Verification Checklist

Antes de executar o deploy, verifique:

- [ ] Todos os secrets estão configurados no GitHub
- [ ] Azure Service Principal tem permissões corretas
- [ ] Key Vault está criado e acessível
- [ ] Stripe keys são do ambiente correto (test/live)
- [ ] Cosmos DB está provisionado
- [ ] Azure AD App Registration está configurado
- [ ] Static Web Apps deployment token está válido

---

## 🚀 Testing the Setup

Para testar a configuração:

```bash
# Test Azure credentials
az login --service-principal \
  -u <clientId> \
  -p <clientSecret> \
  --tenant <tenantId>

# Test Key Vault access
az keyvault secret list --vault-name <KEYVAULT_NAME>

# Test Stripe API
curl https://api.stripe.com/v1/customers \
  -u <STRIPE_API_KEY>:
```

---

## 📞 Support

Se encontrar problemas:

1. Verifique os logs do GitHub Actions
2. Teste as credenciais localmente
3. Confirme as permissões no Azure Portal
4. Revise a documentação do Azure CLI

---

**Last Updated:** $(Get-Date -Format "yyyy-MM-dd")
