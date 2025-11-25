# 🚀 Guia Completo - Deploy Azure Marketplace

## Status Atual: 95% Completo ✅

**Backend:** Funcionando 100%
**Frontend:** Funcionando 100% (local)
**Falta:** Deploy de produção

---

## PASSO 1️⃣: Build de Produção (15 min)

### No VS Code Terminal (Terminal Node):

```bash
cd C:\DIOazure\Azure-SmartCost\smartcost-dashboard
npm run build
```

**Aguarde:** 2-3 minutos para build completar

**Resultado esperado:**
```
Creating an optimized production build...
Compiled successfully.

File sizes after gzip:
  build/static/js/main.xxxxx.js
  build/static/css/main.xxxxx.css
```

**Pasta criada:** `C:\DIOazure\Azure-SmartCost\smartcost-dashboard\build\`

---

## PASSO 2️⃣: Deploy Frontend Azure Static Web Apps (20 min)

### Option A: Via Portal Azure (Mais Fácil)

1. **Portal Azure** → Create Resource → "Static Web Apps"

2. **Configuração:**
   - **Name:** `smartcost-webapp`
   - **Region:** East US
   - **Plan:** Free (para começar)
   - **Deployment:** GitHub (ou Upload Build Manual)

3. **GitHub Integration (se usar):**
   - Repository: `lucacorp/Azure-SmartCost`
   - Branch: `main`
   - Build Presets: `React`
   - App location: `/smartcost-dashboard`
   - Output location: `build`

4. **Environment Variables** (depois do deploy):
   ```
   REACT_APP_API_BASE_URL=https://smartcost-func-beta.azurewebsites.net/api
   REACT_APP_SUBSCRIPTION_ID=e6b85c41-c45d-42a5-955f-d4dfb3b13ce9
   REACT_APP_AZURE_AD_CLIENT_ID=b44694e0-2fa0-49e5-b6ac-1978b04e433e
   REACT_APP_AZURE_AD_AUTHORITY=https://login.microsoftonline.com/common
   ```

### Option B: Via Azure CLI (Mais Rápido)

```powershell
# Login
az login

# Criar Static Web App
az staticwebapp create `
  --name smartcost-webapp `
  --resource-group smartcost-rg-beta `
  --location eastus `
  --source https://github.com/lucacorp/Azure-SmartCost `
  --branch main `
  --app-location "/smartcost-dashboard" `
  --output-location "build" `
  --login-with-github

# Obter URL
az staticwebapp show --name smartcost-webapp --query "defaultHostname" -o tsv
```

**URL de Produção:** `https://smartcost-webapp.azurestaticapps.net`

---

## PASSO 3️⃣: Atualizar CORS no Backend (5 min)

### Portal Azure → Function App `smartcost-func-beta`:

1. **Settings** → **CORS**
2. **Add allowed origin:**
   ```
   https://smartcost-webapp.azurestaticapps.net
   ```
3. **Save**

### Ou via Azure CLI:

```powershell
az functionapp cors add `
  --name smartcost-func-beta `
  --resource-group smartcost-rg-beta `
  --allowed-origins https://smartcost-webapp.azurestaticapps.net
```

---

## PASSO 4️⃣: Atualizar Azure AD Redirect URIs (3 min)

### Portal Azure → Azure AD → App Registrations → SmartCost-SPA:

1. **Authentication** → **Platform configurations** → **Single-page application**
2. **Add URI:**
   ```
   https://smartcost-webapp.azurestaticapps.net
   https://smartcost-webapp.azurestaticapps.net/
   ```
3. **Save**

---

## PASSO 5️⃣: Criar Azure Managed Application (30 min)

### Arquivos Necessários (já criados no projeto):

```
infra/
├── main.bicep                    ✅ Já existe
├── createUiDefinition.json       ⏳ Criar agora
└── mainTemplate.json             ⏳ Gerar do bicep
```

### Criar `createUiDefinition.json`:

Vou criar esse arquivo agora...

---

## PASSO 6️⃣: Preparar Assets Marketplace (20 min)

### Logos Necessários:

- **Small:** 48x48 px (PNG)
- **Medium:** 90x90 px (PNG)
- **Large:** 115x115 px (PNG)
- **Wide:** 255x115 px (PNG)
- **Hero:** 815x290 px (PNG)

### Screenshots (mínimo 3):

1. Dashboard Overview
2. Cost Analysis
3. Budget Alerts

**Localização:** `marketing/marketplace-assets/`

---

## PASSO 7️⃣: Partner Center Setup (60 min)

### Pré-requisitos:

1. **Microsoft Partner Network Account** (gratuito)
   - https://partner.microsoft.com/dashboard
   - Verificação pode levar 1-2 dias

2. **Publisher Profile**
   - Criar em Partner Center
   - Preencher informações da empresa

### Criar Offer:

1. **Partner Center** → **Marketplace offers** → **+ New offer** → **Azure application**

2. **Offer Setup:**
   - **Offer ID:** `azure-smartcost`
   - **Alias:** Azure SmartCost - Cost Optimization Tool

3. **Properties:**
   - **Category:** Monitoring and Diagnostics ✅ (ou "Developer Tools" se não encontrar)
   - **Industries:** Cross Industry
   - **Legal:** Standard Contract
   
   > 📌 **Categorias alternativas aceitas:**
   > - Monitoring and Diagnostics (recomendado)
   > - Developer Tools
   > - Analytics
   > - Management Tools

4. **Offer Listing:**
   ```
   Name: Azure SmartCost
   
   Summary: Otimize custos Azure com análises em tempo real, 
   recomendações acionáveis e alertas de budget inteligentes.
   
   Description: [Copiar de docs/MARKETPLACE_GUIDE.md]
   
   Keywords: azure, cost, optimization, management, budget
   ```

5. **Preview Audience:**
   - Adicionar seu Azure AD Tenant ID
   - Adicionar email para testes

6. **Technical Configuration:**
   - **Package Type:** Managed Application
   - **Deployment mode:** Complete
   - **ARM Template:** Upload `mainTemplate.json`
   - **UI Definition:** Upload `createUiDefinition.json`

7. **Plans:**
   
   **Plan 1: Free Trial (30 days)**
   ```
   - Todas as features
   - Suporte por email
   - Price: $0/month
   ```
   
   **Plan 2: Starter**
   ```
   - Até 3 subscriptions
   - Dashboard + Alerts
   - Suporte por email
   - Price: $49/month
   ```
   
   **Plan 3: Professional**
   ```
   - Até 10 subscriptions
   - Dashboard + Alerts + Power BI
   - Email + Chat support
   - Price: $149/month
   ```
   
   **Plan 4: Enterprise**
   ```
   - Unlimited subscriptions
   - Todas as features
   - Priority support + SLA
   - Price: $499/month
   ```

8. **Co-sell (Opcional):**
   - Pular por enquanto
   - Pode adicionar depois

---

## PASSO 8️⃣: Testes Finais (30 min)

### Checklist de Testes:

```bash
# 1. Frontend carrega
curl https://smartcost-webapp.azurestaticapps.net

# 2. API responde
curl https://smartcost-func-beta.azurewebsites.net/api/health

# 3. Dashboard mostra dados
# Abrir navegador e verificar

# 4. CORS funcionando
# DevTools → Network → Sem erros CORS

# 5. Auth funcionando
# Login → Dashboard deve carregar
```

---

## PASSO 9️⃣: Submeter para Certificação (10 min)

### No Partner Center:

1. **Review and publish**
2. **Check for errors**
3. **Submit**

**Tempo de certificação:** 3-5 dias úteis

**Microsoft vai verificar:**
- ✅ ARM Template válido
- ✅ UI Definition funcional
- ✅ Screenshots de qualidade
- ✅ Documentação completa
- ✅ Compliance e segurança

---

## TIMELINE ESTIMADO

| Etapa | Tempo | Status |
|-------|-------|--------|
| Build de produção | 15 min | ⏳ A fazer |
| Deploy Static Web App | 20 min | ⏳ A fazer |
| CORS + Azure AD | 8 min | ⏳ A fazer |
| Managed App Definition | 30 min | ⏳ A fazer |
| Assets (logos/screenshots) | 20 min | ⏳ A fazer |
| Partner Center Account | 1-2 dias | ⏳ Verificação |
| Criar Offer | 60 min | ⏳ A fazer |
| Testes | 30 min | ⏳ A fazer |
| Submit | 10 min | ⏳ A fazer |
| **Certificação Microsoft** | **3-5 dias** | ⏳ Aguardar |

**Total trabalho ativo:** ~3-4 horas
**Certificação:** 3-5 dias úteis

---

## PRÓXIMOS COMANDOS

**Estou pronto para:**

1. ✅ Gerar `createUiDefinition.json`
2. ✅ Gerar `mainTemplate.json` do bicep
3. ✅ Criar logos placeholder
4. ✅ Criar script de deploy completo
5. ✅ Documentar pricing strategy

**Qual você quer que eu faça primeiro?**

---

## AJUDA RÁPIDA

**Problema:** Build falha
**Solução:** 
```bash
cd smartcost-dashboard
rm -rf node_modules build
npm install
npm run build
```

**Problema:** CORS error em produção
**Solução:** 
```bash
az functionapp cors add \
  --name smartcost-func-beta \
  --allowed-origins https://smartcost-webapp.azurestaticapps.net
```

**Problema:** Auth redirect não funciona
**Solução:** Adicionar URL em Azure AD → App Registrations → Authentication

---

## RECURSOS

- ✅ Backend: https://smartcost-func-beta.azurewebsites.net
- ⏳ Frontend (prod): A criar
- ✅ Docs: `docs/MARKETPLACE_GUIDE.md`
- ✅ Templates: `infra/main.bicep`
- ✅ Credentials: `POWERBI_CREDENTIALS.txt`

---

**👉 Pronto para começar? Digite qual passo quer fazer primeiro!**
