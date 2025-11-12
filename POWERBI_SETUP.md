# 📊 Power BI Integration Setup Guide

Este guia detalha como configurar a integração com o Power BI para o Azure SmartCost em produção.

## 🎯 Pré-requisitos

- Azure Active Directory Tenant
- Power BI Pro ou Premium license
- Permissões de administrador no Power BI Service
- Azure subscription para deploy da aplicação

## 📋 Passo 1: Criar Azure AD App Registration

### 1.1. Registrar aplicação no Azure AD

1. Acesse o **Azure Portal** (https://portal.azure.com)
2. Navegue para **Azure Active Directory** > **App registrations**
3. Clique em **New registration**
4. Configure:
   - **Name**: `AzureSmartCost-PowerBI`
   - **Account types**: `Accounts in this organizational directory only`
   - **Redirect URI**: `https://your-api-domain/auth/callback`

### 1.2. Configurar API Permissions

1. No app registration criado, vá para **API permissions**
2. Clique em **Add a permission**
3. Selecione **Power BI Service**
4. Adicione as seguintes **Delegated permissions**:
   - `Dataset.Read.All`
   - `Dataset.ReadWrite.All`
   - `Report.Read.All`
   - `Workspace.Read.All`
   - `Content.Create`
5. Clique em **Grant admin consent**

### 1.3. Criar Client Secret

1. Vá para **Certificates & secrets**
2. Clique em **New client secret**
3. Configure:
   - **Description**: `AzureSmartCost Production Secret`
   - **Expires**: `24 months`
4. **⚠️ IMPORTANTE**: Copie e guarde o valor do secret (só aparece uma vez!)

### 1.4. Anotar informações importantes

Guarde as seguintes informações:
- **Application (client) ID**
- **Directory (tenant) ID**
- **Client secret value**

## 📊 Passo 2: Configurar Power BI Workspace

### 2.1. Criar Workspace

1. Acesse **Power BI Service** (https://app.powerbi.com)
2. Navegue para **Workspaces**
3. Clique em **Create a workspace**
4. Configure:
   - **Workspace name**: `Azure SmartCost Analytics`
   - **Description**: `Cost analytics and optimization reports for Azure SmartCost platform`

### 2.2. Configurar permissões do Workspace

1. No workspace criado, clique em **Settings** > **Access**
2. Adicione a aplicação Azure AD criada:
   - **Email**: Use o Application (client) ID
   - **Role**: `Contributor` ou `Admin`

### 2.3. Anotar Workspace ID

1. Na URL do workspace, copie o Workspace ID:
   ```
   https://app.powerbi.com/groups/{WORKSPACE_ID}/...
   ```

## 📈 Passo 3: Criar Dataset e Reports

### 3.1. Criar Dataset via API (Automático)

O dataset será criado automaticamente pela aplicação usando os templates definidos em:
- `PowerBiModels.cs` > `SmartCostPowerBiTemplates.GetSmartCostDataset()`

### 3.2. Criar Reports Manualmente (Opcional)

Se preferir criar reports personalizados:

1. No Power BI Service, vá para o workspace
2. Clique em **New** > **Report**
3. Conecte ao dataset `Azure SmartCost Analysis`
4. Crie os seguintes reports:
   - **Executive Dashboard**: Visão geral executiva
   - **Detailed Cost Analysis**: Análise detalhada de custos
   - **Cost Optimization**: Recomendações de otimização
   - **Budget Analysis**: Análise orçamentária

## ⚙️ Passo 4: Configurar Variáveis de Ambiente

### 4.1. Variáveis para Azure App Service

Configure as seguintes variáveis de ambiente no Azure App Service:

```bash
# Power BI Configuration
POWERBI_CLIENT_ID=your-app-registration-client-id
POWERBI_CLIENT_SECRET=your-client-secret-value
AZURE_TENANT_ID=your-tenant-id
POWERBI_WORKSPACE_ID=your-workspace-id
POWERBI_DATASET_ID=your-dataset-id-after-creation

# Enable Real Power BI API
USE_REAL_POWERBI_API=true

# CosmosDB (Production)
COSMOSDB_CONNECTION_STRING=AccountEndpoint=https://your-cosmosdb.documents.azure.com:443/;AccountKey=your-key;
```

### 4.2. Configuração Local de Desenvolvimento

Para desenvolvimento local, crie um arquivo `appsettings.Development.json`:

```json
{
  "PowerBI": {
    "ClientId": "your-app-registration-client-id",
    "ClientSecret": "your-client-secret-value",
    "TenantId": "your-tenant-id",
    "WorkspaceId": "your-workspace-id",
    "DatasetId": "your-dataset-id-after-creation",
    "BaseUrl": "https://api.powerbi.com/v1.0/myorg"
  },
  "USE_REAL_POWERBI_API": true
}
```

## 🔧 Passo 5: Atualizar Configuração da Aplicação

### 5.1. Atualizar appsettings.Production.json

```json
{
  "PowerBI": {
    "ClientId": "${POWERBI_CLIENT_ID}",
    "ClientSecret": "${POWERBI_CLIENT_SECRET}",
    "TenantId": "${AZURE_TENANT_ID}",
    "WorkspaceId": "${POWERBI_WORKSPACE_ID}",
    "DatasetId": "${POWERBI_DATASET_ID}",
    "BaseUrl": "https://api.powerbi.com/v1.0/myorg"
  },
  "USE_REAL_POWERBI_API": true
}
```

### 5.2. Verificar Program.cs

Confirme que o código em `Program.cs` está correto:

```csharp
// Power BI Service configuration
if (builder.Configuration.GetValue<bool>("USE_REAL_POWERBI_API"))
{
    builder.Services.AddHttpClient<IPowerBiService, PowerBiService>();
}
else
{
    builder.Services.AddScoped<IPowerBiService, MockPowerBiService>();
}
```

## 🧪 Passo 6: Testar a Integração

### 6.1. Teste Local

1. Configure as variáveis de ambiente localmente
2. Execute a aplicação:
   ```bash
   dotnet run --project src/AzureSmartCost.Api
   ```
3. Teste os endpoints:
   ```bash
   # Testar embed config
   curl -H "Authorization: Bearer YOUR_TOKEN" \
        "https://localhost:7001/api/powerbi/embed-config?reportId=test"

   # Testar templates
   curl -H "Authorization: Bearer YOUR_TOKEN" \
        "https://localhost:7001/api/powerbi/templates"
   ```

### 6.2. Teste de Produção

1. Deploy a aplicação para Azure App Service
2. Configure as variáveis de ambiente
3. Acesse o dashboard frontend
4. Verifique se a aba "Power BI" carrega corretamente

## 📋 Passo 7: Monitoramento e Troubleshooting

### 7.1. Logs da Aplicação

Monitore os logs para verificar:
- Autenticação Azure AD
- Criação automática do dataset
- Refresh de dados

### 7.2. Problemas Comuns

**Erro de Autenticação:**
- Verificar se o client secret não expirou
- Confirmar se as permissões foram concedidas

**Dataset não encontrado:**
- Verificar se o workspace ID está correto
- Confirmar se a aplicação tem permissões no workspace

**Reports não carregam:**
- Verificar se os report IDs existem
- Confirmar se o embed token tem as permissões corretas

## ✅ Checklist de Produção

- [ ] Azure AD App Registration criado
- [ ] Permissões Power BI configuradas
- [ ] Client secret criado e guardado
- [ ] Power BI Workspace criado
- [ ] Permissões do workspace configuradas
- [ ] Variáveis de ambiente configuradas
- [ ] Aplicação deployada
- [ ] Testes de integração realizados
- [ ] Monitoramento configurado

## 📚 Referências

- [Power BI REST API Documentation](https://docs.microsoft.com/en-us/rest/api/power-bi/)
- [Azure AD App Registration](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)
- [Power BI Embedded Analytics](https://docs.microsoft.com/en-us/power-bi/developer/embedded/)
- [Power BI .NET SDK](https://www.nuget.org/packages/Microsoft.PowerBI.Api/)

---
🚀 **Azure SmartCost** - Intelligent Cost Management Platform