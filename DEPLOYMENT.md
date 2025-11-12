# Azure SmartCost - Deployment Guide

Este guia contém instruções completas para fazer o deploy do Azure SmartCost na Azure usando infraestrutura como código (Bicep) e pipelines de CI/CD (GitHub Actions).

## 📋 Pré-requisitos

### 1. Ferramentas Necessárias
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) v2.50+
- [Node.js](https://nodejs.org/) v18+
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Git](https://git-scm.com/)
- Conta Azure com permissões de Contributor

### 2. Configuração do Azure AD
Antes do deploy, você precisa criar um App Registration no Azure AD:

```bash
# 1. Criar App Registration
az ad app create --display-name "Azure SmartCost API" --web-redirect-uris "https://your-api-domain/signin-oidc"

# 2. Obter Client ID
APP_ID=$(az ad app list --display-name "Azure SmartCost API" --query "[0].appId" -o tsv)

# 3. Criar Client Secret
az ad app credential reset --id $APP_ID --display-name "SmartCost-Secret"
```

### 3. Configuração do GitHub
Configure os seguintes secrets no seu repositório GitHub:

| Secret Name | Description | Example |
|------------|-------------|---------|
| `AZURE_CREDENTIALS` | Service Principal para deploy | `{"clientId":"...","clientSecret":"...","subscriptionId":"...","tenantId":"..."}` |
| `JWT_SECRET` | Chave secreta para JWT tokens | `SuperSecretJWTKey2024!` |
| `AZURE_AD_CLIENT_ID` | Client ID do Azure AD App Registration | `12345678-1234-1234-1234-123456789012` |
| `AZURE_AD_CLIENT_SECRET` | Client Secret do Azure AD | `your-client-secret` |

## 🚀 Deploy Manual

### 1. Deploy da Infraestrutura

```bash
# Clone o repositório
git clone <your-repo-url>
cd Azure-SmartCost/infra

# Login no Azure
az login

# Definir variáveis
RESOURCE_GROUP="rg-smartcost-dev"
LOCATION="eastus"
ENVIRONMENT="dev"

# Criar Resource Group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Deploy da infraestrutura usando PowerShell
.\deploy.ps1 -Environment $ENVIRONMENT -ResourceGroup $RESOURCE_GROUP -Location $LOCATION

# Ou usando Bash
chmod +x deploy.sh
./deploy.sh
```

### 2. Configuração Pós-Deploy

```bash
# Obter informações do deployment
az deployment group show --resource-group $RESOURCE_GROUP --name <deployment-name> --query "properties.outputs"
```

## 🔄 Deploy Automatizado (CI/CD)

O projeto inclui workflows do GitHub Actions para deploy automatizado:

### Workflows Disponíveis

1. **🏗️ Infrastructure** (`.github/workflows/deploy-infrastructure.yml`)
   - Triggered por mudanças em `infra/**`
   - Deploy da infraestrutura Azure usando Bicep
   - Criação de Resource Groups, App Services, Static Web Apps, etc.

2. **🚀 API** (`.github/workflows/deploy-api.yml`)
   - Triggered por mudanças em `src/AzureSmartCost.Api/**`
   - Build e deploy da API .NET para Azure App Service
   - Inclui testes automatizados

3. **🌐 Frontend** (`.github/workflows/deploy-frontend.yml`)
   - Triggered por mudanças em `smartcost-dashboard/**`
   - Build e deploy do React para Azure Static Web Apps
   - Deploy automático para pull requests (preview)

4. **⚡ Functions** (`.github/workflows/deploy-functions.yml`)
   - Triggered por mudanças em `src/AzureSmartCost.Functions/**`
   - Deploy das Azure Functions para coleta de dados

### Configuração dos Workflows

1. **Configure Service Principal para GitHub:**
```bash
# Criar Service Principal
az ad sp create-for-rbac --name "smartcost-github-actions" \
    --role contributor \
    --scopes /subscriptions/{subscription-id} \
    --sdk-auth
```

2. **Configure secrets no GitHub:**
   - Vá para Settings > Secrets and variables > Actions
   - Adicione todos os secrets listados na seção de pré-requisitos

3. **Configure ambientes:**
   - Crie ambientes `development` e `production` no GitHub
   - Configure protection rules conforme necessário

## 🏗️ Arquitetura da Infraestrutura

### Recursos Criados

| Recurso | Tipo | Propósito |
|---------|------|-----------|
| **App Service Plan** | B1/S1 | Hospeda a API REST |
| **App Service** | Web App | API .NET 8 com autenticação |
| **Static Web App** | Standard | Frontend React com CI/CD |
| **Function App** | Consumption | Coleta automática de dados de custo |
| **Cosmos DB** | Serverless | Banco de dados NoSQL para custos |
| **Key Vault** | Standard | Armazenamento seguro de secrets |
| **Application Insights** | - | Monitoramento e telemetria |
| **Log Analytics** | - | Logs centralizados |
| **Storage Account** | Standard LRS | Storage para Functions |

### Configurações de Segurança

- **HTTPS Only** em todos os endpoints
- **TLS 1.2+** obrigatório
- **Managed Identity** para acesso entre serviços
- **RBAC** granular com least privilege
- **Key Vault** para secrets sensíveis
- **CORS** configurado para domínios específicos

## 🔧 Configurações por Ambiente

### Development
- **SKU**: B1 (básico)
- **Cosmos DB**: Serverless
- **Static Web App**: Free tier
- **SSL**: Azure-managed
- **Custom Domain**: Não configurado

### Production
- **SKU**: S1 (standard)
- **Cosmos DB**: Serverless com backup
- **Static Web App**: Standard tier
- **SSL**: Azure-managed + Custom domain
- **CDN**: Azure Front Door (opcional)

## 🔍 Monitoramento e Logs

### Application Insights
```bash
# Visualizar métricas da aplicação
az monitor app-insights component show --app <app-insights-name> --resource-group <rg-name>
```

### Log Analytics
```kusto
// Query para logs de erro
AppRequests
| where Success == false
| project TimeGenerated, Name, ResultCode, DurationMs
| order by TimeGenerated desc
```

### Health Checks
- **API**: `https://{api-app}.azurewebsites.net/api/health`
- **Auth**: `https://{api-app}.azurewebsites.net/api/auth/health`
- **Functions**: Monitored via Application Insights

## 🚨 Troubleshooting

### Problemas Comuns

1. **Deploy falha com erro de permissão**
   ```bash
   # Verificar permissões do Service Principal
   az role assignment list --assignee <sp-object-id> --output table
   ```

2. **API retorna 500**
   ```bash
   # Verificar logs do App Service
   az webapp log tail --name <app-name> --resource-group <rg-name>
   ```

3. **Autenticação falha**
   ```bash
   # Verificar configurações do Azure AD
   az ad app show --id <app-id> --query "web.redirectUris"
   ```

4. **Frontend não conecta na API**
   - Verificar configurações de CORS
   - Confirmar variáveis de ambiente no build
   - Validar SSL certificates

### Comandos Úteis

```bash
# Reiniciar App Service
az webapp restart --name <app-name> --resource-group <rg-name>

# Verificar status dos recursos
az resource list --resource-group <rg-name> --output table

# Obter logs em tempo real
az webapp log tail --name <app-name> --resource-group <rg-name>

# Listar secrets no Key Vault
az keyvault secret list --vault-name <kv-name> --output table
```

## 📚 Próximos Passos

Após o deploy bem-sucedido:

1. **✅ Configure Azure AD** com os redirect URLs corretos
2. **🔧 Update DNS** se usando domínio customizado
3. **📊 Configure alertas** no Application Insights
4. **🔒 Review security** settings e RBAC
5. **📈 Setup monitoring** dashboards
6. **🚀 Deploy Power BI** integration (próxima prioridade)

## 🆘 Suporte

Para problemas ou dúvidas:
- 📖 Consulte a [documentação oficial do Azure](https://docs.microsoft.com/azure/)
- 🐛 Abra uma issue neste repositório
- 💬 Entre em contato com a equipe de DevOps

---

**🎉 Parabéns! Sua infraestrutura Azure SmartCost está pronta para produção!**