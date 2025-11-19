# 🚀 Upload Manual do Build - RÁPIDO (5 min)

## STATIC WEB APP CRIADO ✅

**Nome:** smartcost-webapp  
**URL:** https://blue-flower-0414b9b0f.3.azurestaticapps.net  
**Resource Group:** rg-smartcost-beta

---

## MÉTODO 1: Portal Azure (MAIS FÁCIL - 5 min)

### Passo a Passo:

1. **Abra Portal Azure**
   - https://portal.azure.com

2. **Vá para o Static Web App**
   - Resource Groups → `rg-smartcost-beta`
   - Click em `smartcost-webapp`

3. **Deploy Manual**
   - Menu esquerdo → **"Deployment"**
   - Click **"Browse for files"**
   - Selecione a pasta: `C:\DIOazure\Azure-SmartCost\smartcost-dashboard\build`
   - **Upload all files** (arrastar pasta inteira)
   - Click **"Deploy"**

4. **Aguarde**
   - Status: Building... (1-2 min)
   - Status: Deploying... (1 min)
   - Status: Ready ✅

5. **Acesse**
   - https://blue-flower-0414b9b0f.3.azurestaticapps.net

---

## MÉTODO 2: Azure CLI (ALTERNATIVO)

Se portal não funcionar, use ZIP deploy:

```powershell
# 1. Criar ZIP do build
cd C:\DIOazure\Azure-SmartCost\smartcost-dashboard
Compress-Archive -Path .\build\* -DestinationPath deploy.zip -Force

# 2. Upload via CLI
az staticwebapp deploy `
  --name smartcost-webapp `
  --resource-group rg-smartcost-beta `
  --source deploy.zip
```

---

## MÉTODO 3: GitHub Actions (AUTOMÁTICO)

Se quiser CI/CD:

1. **Commit e Push** tudo para GitHub:
```bash
git add .
git commit -m "Production build ready"
git push origin main
```

2. **Portal Azure** → Static Web App → **"Deployment" → "GitHub Actions"**
   - Link repository: `lucacorp/Azure-SmartCost`
   - Branch: `main`
   - App location: `/smartcost-dashboard`
   - Output location: `build`

3. **GitHub Actions** vai deployar automaticamente!

---

## ⚙️ DEPOIS DO DEPLOY

### Configure Environment Variables

**Portal Azure** → smartcost-webapp → **Configuration** → **Application settings**

Adicione:
```
REACT_APP_API_BASE_URL = https://smartcost-func-beta.azurewebsites.net/api
REACT_APP_SUBSCRIPTION_ID = e6b85c41-c45d-42a5-955f-d4dfb3b13ce9
REACT_APP_AZURE_AD_CLIENT_ID = b44694e0-2fa0-49e5-b6ac-1978b04e433e
REACT_APP_AZURE_AD_AUTHORITY = https://login.microsoftonline.com/common
```

Save → **Restart**

---

## 🔒 CONFIGURAR CORS

**Portal Azure** → smartcost-func-beta → **CORS**

Adicione:
```
https://blue-flower-0414b9b0f.3.azurestaticapps.net
```

Save

---

## 🔑 CONFIGURAR AZURE AD

**Portal Azure** → Azure Active Directory → **App registrations** → SmartCost-SPA

**Authentication** → **Platform configurations** → **Single-page application**

Adicione Redirect URIs:
```
https://blue-flower-0414b9b0f.3.azurestaticapps.net
https://blue-flower-0414b9b0f.3.azurestaticapps.net/
```

Save

---

## ✅ TESTAR

Abra: https://blue-flower-0414b9b0f.3.azurestaticapps.net

Deve:
- ✅ Carregar dashboard
- ✅ Login funcionar
- ✅ Mostrar dados R$ 137.07

---

## 🆘 SE DER ERRO

### Erro CORS:
```
Access-Control-Allow-Origin
```
**Solução:** Configure CORS no Function App (acima)

### Erro Auth:
```
AADSTS50011: Redirect URI mismatch
```
**Solução:** Configure Redirect URIs no Azure AD (acima)

### App não carrega:
**Solução:** Verifique se fez upload da pasta `build` completa

---

**URL PRODUÇÃO:** https://blue-flower-0414b9b0f.3.azurestaticapps.net

**Faça upload agora pelo Portal Azure (MÉTODO 1) - é o mais rápido!**
