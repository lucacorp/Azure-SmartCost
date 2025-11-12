# Azure Marketplace - Guia de Publicação

## Visão Geral
Este guia fornece instruções completas para publicar o Azure SmartCost no Azure Marketplace como uma oferta SaaS (Software as a Service). A integração segue a **SaaS Fulfillment API v2** da Microsoft.

---

## 📋 Pré-requisitos

### 1. Conta de Publisher no Partner Center
- Acesse [Partner Center](https://partner.microsoft.com/dashboard)
- Crie uma conta de publisher (requer verificação)
- Complete o processo de verificação de identidade
- Configure informações fiscais e bancárias

### 2. Azure AD App Registration
- Tenant ID do Azure AD
- Application (Client) ID
- Client Secret
- Permissões: Azure Marketplace API

### 3. Certificações Necessárias
- [ ] Certificação Microsoft Partner Network (MPN)
- [ ] Acordo de Publisher do Azure Marketplace
- [ ] Informações fiscais W-8/W-9 (conforme localização)
- [ ] Conta bancária para pagamentos

---

## 🛠️ Configuração Técnica

### 1. Criar App Registration no Azure AD

```powershell
# Login no Azure
az login

# Criar App Registration
az ad app create \
  --display-name "Azure SmartCost Marketplace" \
  --sign-in-audience "AzureADMultipleOrgs"

# Anotar o Application ID e Tenant ID
APP_ID=$(az ad app list --display-name "Azure SmartCost Marketplace" --query "[0].appId" -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)

# Criar Client Secret
az ad app credential reset \
  --id $APP_ID \
  --append \
  --display-name "Marketplace Secret" \
  --years 2

# Configurar Redirect URIs
az ad app update \
  --id $APP_ID \
  --web-redirect-uris "https://smartcost-api.azurewebsites.net/api/marketplace/landing"

# Configurar API Permissions
az ad app permission add \
  --id $APP_ID \
  --api 20e940b3-4c77-4b0b-9a53-9e16a1b010a7 \
  --api-permissions 62d94f6c-d599-489b-a797-3e10e42fbe22=Scope

# Conceder permissões admin
az ad app permission admin-consent --id $APP_ID
```

### 2. Configurar Key Vault

Adicione os seguintes secrets ao Azure Key Vault:

```bash
# Marketplace Credentials
az keyvault secret set \
  --vault-name "smartcost-keyvault" \
  --name "marketplace-tenant-id" \
  --value "$TENANT_ID"

az keyvault secret set \
  --vault-name "smartcost-keyvault" \
  --name "marketplace-client-id" \
  --value "$APP_ID"

az keyvault secret set \
  --vault-name "smartcost-keyvault" \
  --name "marketplace-client-secret" \
  --value "YOUR_CLIENT_SECRET"
```

### 3. Atualizar appsettings.Production.json

```json
{
  "Marketplace": {
    "TenantId": "@Microsoft.KeyVault(SecretUri=https://smartcost-keyvault.vault.azure.net/secrets/marketplace-tenant-id/)",
    "ClientId": "@Microsoft.KeyVault(SecretUri=https://smartcost-keyvault.vault.azure.net/secrets/marketplace-client-id/)",
    "ClientSecret": "@Microsoft.KeyVault(SecretUri=https://smartcost-keyvault.vault.azure.net/secrets/marketplace-client-secret/)",
    "OfferId": "azure-smartcost",
    "PublisherId": "YourPublisherName",
    "LandingPageUrl": "https://smartcost-api.azurewebsites.net/api/marketplace/landing",
    "WebhookUrl": "https://smartcost-api.azurewebsites.net/api/marketplace/webhook",
    "Plans": [
      {
        "PlanId": "free",
        "DisplayName": "Free",
        "Price": 0
      },
      {
        "PlanId": "pro",
        "DisplayName": "Pro",
        "Price": 99
      },
      {
        "PlanId": "enterprise",
        "DisplayName": "Enterprise",
        "Price": 499
      }
    ]
  }
}
```

---

## 📦 Criação da Oferta no Partner Center

### 1. Acessar Partner Center
1. Login em [Partner Center](https://partner.microsoft.com/dashboard/marketplace-offers/overview)
2. Clique em **"Marketplace offers"** → **"+ New offer"** → **"Azure Application"** → **"SaaS"**

### 2. Offer Setup
- **Offer ID**: `azure-smartcost`
- **Offer alias**: `Azure SmartCost`
- **Customer leads**: Configure Azure Table Storage ou CRM
- **Test drive**: Desabilitado (opcional para demo)

### 3. Properties
- **Categories**: 
  - Primary: DevOps
  - Secondary: IT & Management Tools
- **Industries**: Financial Services, Technology
- **Legal**: Link para Terms of Use e Privacy Policy
- **App version**: 1.0.0

### 4. Offer Listing
- **Name**: Azure SmartCost - Otimização Inteligente de Custos
- **Search results summary** (100 chars): Reduza custos Azure em até 40% com análise preditiva e alertas inteligentes
- **Description**: Copie de `docs/marketplace/manifest.json` → `listing.description`
- **Getting started instructions**: 
  ```
  1. Clique em "Configure Account" após a compra
  2. Autentique com Azure AD
  3. Conecte suas assinaturas Azure
  4. Comece a economizar!
  ```
- **Search keywords**: azure cost, finops, cloud optimization
- **Privacy policy URL**: https://smartcost.io/privacy
- **Support URL**: https://support.smartcost.io
- **Engineering contact**: engineering@smartcost.io
- **Support contact**: support@smartcost.io

### 5. Preview Audience
Adicione Subscription IDs de assinaturas Azure que poderão testar antes do lançamento:
```
subscription-id-1
subscription-id-2
```

### 6. Technical Configuration
**Landing page URL**: `https://smartcost-api.azurewebsites.net/api/marketplace/landing`
**Connection webhook**: `https://smartcost-api.azurewebsites.net/api/marketplace/webhook`
**Azure AD tenant ID**: (seu Tenant ID)
**Azure AD application ID**: (seu App ID)

### 7. Plans

#### Plan 1: Free
- **Plan ID**: `free`
- **Plan name**: Free Plan
- **Description**: Funcionalidades básicas de monitoramento
- **Pricing**: $0/month
- **Markets**: Brazil, United States, Portugal, Spain
- **Trial**: Não aplicável

#### Plan 2: Pro
- **Plan ID**: `pro`
- **Plan name**: Pro Plan
- **Description**: Análises avançadas com Power BI
- **Pricing**: $99/month (flat rate)
- **Markets**: Brazil, United States, Portugal, Spain
- **Trial**: 14 dias grátis

#### Plan 3: Enterprise
- **Plan ID**: `enterprise`
- **Plan name**: Enterprise Plan
- **Description**: Solução enterprise com SSO e suporte 24/7
- **Pricing**: $499/month (flat rate)
- **Markets**: Brazil, United States, Portugal, Spain
- **Trial**: 30 dias grátis

### 8. Marketing Assets

Upload dos seguintes arquivos em `docs/marketplace/assets/`:

#### Logos (obrigatório)
- **Small**: 48x48px PNG
- **Medium**: 216x216px PNG
- **Large**: 815x415px PNG (hero image)
- **Wide**: 255x115px PNG

#### Screenshots (mínimo 3, recomendado 5)
1. Dashboard principal (1280x720px)
2. Análise preditiva (1280x720px)
3. Alertas inteligentes (1280x720px)
4. Recomendações (1280x720px)
5. Integração Power BI (1280x720px)

#### Vídeo (opcional mas recomendado)
- URL do YouTube/Vimeo com demo de 2-5 minutos
- Exemplo: https://www.youtube.com/watch?v=DEMO_VIDEO_ID

### 9. Co-sell with Microsoft (opcional)
- Configure se tiver parceria Microsoft
- Adicione materiais de vendas conjunta
- Pode aumentar visibilidade no marketplace

---

## 🧪 Testes no Sandbox

### 1. Ambiente de Testes
```bash
# Deploy em ambiente de staging
az webapp deployment slot create \
  --name smartcost-api \
  --resource-group smartcost-rg \
  --slot sandbox

# Configurar URL de webhook para sandbox
# https://smartcost-api-sandbox.azurewebsites.net/api/marketplace/webhook
```

### 2. Testar Fluxo de Compra

#### a) Simular Compra
1. Acesse Partner Center → sua oferta → "Preview"
2. Use subscription ID de teste
3. Clique em "Go to offer"
4. Simule compra no Azure Portal

#### b) Validar Landing Page
```bash
# Obter token de teste do webhook
curl -X POST https://marketplaceapi.microsoft.com/api/saas/subscriptions/resolve \
  -H "x-ms-marketplace-token: YOUR_TEST_TOKEN" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"

# Testar landing page
curl "https://smartcost-api.azurewebsites.net/api/marketplace/landing?token=YOUR_TEST_TOKEN"
```

#### c) Validar Webhook Events
```bash
# Simular evento de Subscribe
curl -X POST https://smartcost-api.azurewebsites.net/api/marketplace/webhook \
  -H "Content-Type: application/json" \
  -d '{
    "id": "test-event-001",
    "activityId": "activity-001",
    "subscriptionId": "sub-001",
    "publisherId": "YourPublisherName",
    "offerId": "azure-smartcost",
    "planId": "pro",
    "quantity": 1,
    "action": "Subscribe",
    "timeStamp": "2024-01-15T10:00:00Z",
    "status": "Succeeded"
  }'
```

### 3. Checklist de Testes

- [ ] Landing page redireciona corretamente após token resolution
- [ ] Subscription é ativada no Marketplace após criação do tenant
- [ ] Webhook recebe eventos de Subscribe/Unsubscribe
- [ ] Webhook processa ChangePlan corretamente
- [ ] Webhook processa Suspend/Reinstate
- [ ] Tenant é criado com plano correto
- [ ] Integração com Cosmos DB persiste dados
- [ ] Logs são gerados corretamente
- [ ] Erros retornam status 200 OK ao webhook

---

## ✅ Certificação e Publicação

### 1. Submeter para Revisão
1. Partner Center → sua oferta → "Review and publish"
2. Verificar todos os campos obrigatórios
3. Clicar em "Submit for review"

### 2. Processo de Certificação
**Duração**: 3-5 dias úteis

Microsoft verificará:
- ✅ Segurança da aplicação (HTTPS, autenticação)
- ✅ Integração com SaaS Fulfillment API
- ✅ Landing page funcional
- ✅ Webhook respondendo corretamente
- ✅ Compliance com políticas do Marketplace
- ✅ Qualidade dos assets (logos, screenshots)
- ✅ Precisão das descrições e pricing

### 3. Correções (se necessário)
- Microsoft enviará feedback por email
- Corrija os problemas apontados
- Re-submeta para nova revisão

### 4. Go-Live
Após aprovação:
1. Receba email de aprovação
2. Clique em "Go live" no Partner Center
3. Oferta estará disponível no Marketplace em até 4 horas

---

## 🔄 Fluxo de Integração (Resumo)

```
┌─────────────┐
│ User compra │
│ no Azure    │
│ Portal      │
└──────┬──────┘
       │
       ▼
┌─────────────────────────────────────┐
│ 1. Azure Portal gera token          │
│ 2. User clica "Configure Account"   │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│ 3. Redirecionado para Landing Page  │
│    /api/marketplace/landing?token=  │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│ 4. SmartCost resolve token via API  │
│    - Obtém subscription details     │
│    - Email do comprador              │
│    - Plan ID                         │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│ 5. Cria/encontra tenant             │
│    - TenantService.CreateTenantAsync│
│    - Associa email do comprador     │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│ 6. Salva MarketplaceSubscription    │
│    - Cosmos DB container            │
│    - Status: PendingActivation      │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│ 7. Ativa subscription no Marketplace│
│    POST /saas/subscriptions/{id}/   │
│         activate                     │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│ 8. Atualiza status: Subscribed      │
│    - SaveMarketplaceSubscriptionAsync│
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│ 9. Redireciona para dashboard       │
│    https://smartcost.io/dashboard   │
└─────────────────────────────────────┘
```

### Eventos de Webhook (após ativação)

```
Marketplace Event → POST /api/marketplace/webhook
                          │
                          ▼
                    ProcessWebhookEventAsync
                          │
        ┌─────────────────┼─────────────────┐
        │                 │                 │
        ▼                 ▼                 ▼
  Subscribe         ChangePlan        Unsubscribe
  Suspend           ChangeQuantity    Reinstate
        │                 │                 │
        └─────────────────┼─────────────────┘
                          ▼
                 Update Tenant Status
                 Update Cosmos DB
```

---

## 📊 Monitoramento Pós-Lançamento

### 1. Métricas no Partner Center
- Número de assinaturas ativas
- Taxa de conversão de trial
- Receita mensal
- Churn rate
- Avaliações de clientes

### 2. Application Insights
```csharp
// Já configurado no MarketplaceService.cs
_logger.LogInformation("Marketplace subscription activated: {SubscriptionId}", subscriptionId);
_logger.LogError("Failed to process webhook event: {EventId}", webhookEvent.Id);
```

### 3. Alertas Recomendados
- [ ] Falhas em webhook (> 5% em 5 minutos)
- [ ] Tempo de resposta landing page (> 3 segundos)
- [ ] Erros de autenticação Marketplace API
- [ ] Subscriptions não ativadas em 24h

---

## 🆘 Troubleshooting

### Problema: Landing page retorna 401 Unauthorized
**Solução**: Verificar Client Secret no Key Vault e permissões da App Registration

### Problema: Webhook não recebe eventos
**Solução**: 
1. Verificar URL do webhook no Partner Center
2. Confirmar endpoint retorna 200 OK
3. Validar logs do Application Insights

### Problema: Token resolution falha
**Solução**:
1. Validar scope de autenticação: `20e940b3-4c77-4b0b-9a53-9e16a1b010a7/.default`
2. Verificar se token não expirou (válido por 1 hora)

### Problema: Subscription não ativa
**Solução**:
1. Verificar logs: `GET /api/marketplace/test` (endpoint admin)
2. Confirmar ActivateSubscriptionAsync está chamando API corretamente
3. Validar planId corresponde ao configurado no Partner Center

---

## 📚 Recursos Adicionais

- [SaaS Fulfillment API v2 Documentation](https://docs.microsoft.com/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2)
- [Partner Center Guide](https://docs.microsoft.com/azure/marketplace/partner-center-portal/create-new-saas-offer)
- [Marketplace Policies](https://docs.microsoft.com/legal/marketplace/certification-policies)
- [Azure AD Authentication](https://docs.microsoft.com/azure/active-directory/develop/v2-overview)

---

## 📞 Suporte

**Microsoft Partner Support**: https://partner.microsoft.com/support
**Azure Marketplace Team**: marketplace@microsoft.com
**SmartCost Engineering**: engineering@smartcost.io
