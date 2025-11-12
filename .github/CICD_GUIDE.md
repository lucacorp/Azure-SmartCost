# 🚀 CI/CD Deployment Guide

Guia completo para configurar e executar os pipelines de CI/CD do Azure SmartCost.

## 📁 Workflows Disponíveis

### 1. **CI - Continuous Integration** (`ci.yml`)
- **Trigger:** Push/PR para `main` ou `develop`
- **Executado:** Em todas as mudanças de código
- **Jobs:**
  - ✅ Build Backend (.NET 8.0)
  - ✅ Build Frontend (React)
  - 🔒 Security Scan (Trivy)
  - 📊 Code Quality (SonarScanner)
  - 🏗️ Bicep Validation
  - 📝 Build Summary

### 2. **CD - Continuous Deployment** (`cd.yml`)
- **Trigger:** Push para `main` ou manual
- **Executado:** Deploy completo para Azure
- **Jobs:**
  - 🏗️ Deploy Infrastructure (Bicep)
  - 🌐 Deploy API (App Service)
  - ⚡ Deploy Functions
  - 💻 Deploy Frontend (Static Web Apps)
  - 🔐 Populate Key Vault Secrets
  - ✅ Smoke Tests
  - 📊 Deployment Summary

### 3. **Deploy Functions** (`deploy-functions.yml`)
- **Trigger:** Mudanças em `src/AzureSmartCost.Functions/**`
- **Executado:** Deploy isolado das Functions
- **Jobs:**
  - 📦 Build & Deploy Functions App

### 4. **Deploy Infrastructure** (`deploy-infrastructure.yml`)
- **Trigger:** Mudanças em `infra/**`
- **Executado:** Deploy apenas da infraestrutura
- **Jobs:**
  - 🏗️ Bicep Template Deployment

---

## 🔐 Configuração Inicial

### Passo 1: Configurar GitHub Secrets

Siga o guia [GITHUB_SECRETS.md](.github/GITHUB_SECRETS.md) para configurar todos os secrets necessários.

**Secrets Essenciais:**
```
AZURE_CREDENTIALS
AZURE_SUBSCRIPTION_ID
AZURE_RESOURCE_GROUP
AZURE_WEBAPP_NAME
AZURE_FUNCTIONAPP_NAME
KEYVAULT_NAME
JWT_SECRET
AZURE_AD_CLIENT_ID
AZURE_AD_CLIENT_SECRET
STRIPE_API_KEY
STRIPE_PUBLISHABLE_KEY
STRIPE_WEBHOOK_SECRET
```

### Passo 2: Criar Service Principal

```bash
# Login no Azure
az login

# Criar Service Principal
az ad sp create-for-rbac \
  --name "smartcost-github-actions" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/smartcost-rg-prod \
  --sdk-auth

# Copiar o JSON output para AZURE_CREDENTIALS
```

### Passo 3: Configurar GitHub Environments

Crie 3 environments no GitHub:

1. **dev** - Desenvolvimento
   - Protection rules: None
   - Auto-deploy: Yes

2. **staging** - Homologação
   - Protection rules: Optional reviewers
   - Auto-deploy: After dev

3. **prod** - Produção
   - Protection rules: **Required reviewers** (mínimo 1)
   - Auto-deploy: Manual only

---

## 🏗️ Arquitetura do Pipeline

```
┌─────────────────────────────────────────────────────────┐
│                     CI PIPELINE                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐             │
│  │  Build   │  │  Build   │  │ Security │             │
│  │ Backend  │  │ Frontend │  │   Scan   │             │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘             │
│       │             │              │                    │
│       └─────────────┴──────────────┘                    │
│                     │                                   │
│              ┌──────▼───────┐                          │
│              │  Artifacts   │                          │
│              └──────┬───────┘                          │
└─────────────────────┼─────────────────────────────────┘
                      │
┌─────────────────────▼─────────────────────────────────┐
│                   CD PIPELINE                          │
│  ┌──────────────────────────────────────────────┐     │
│  │        Deploy Infrastructure (Bicep)         │     │
│  └────────────────────┬─────────────────────────┘     │
│                       │                               │
│       ┌───────────────┼───────────────┐               │
│       │               │               │               │
│  ┌────▼────┐    ┌────▼────┐    ┌────▼────┐          │
│  │  Deploy │    │  Deploy │    │  Deploy │          │
│  │   API   │    │Functions│    │Frontend │          │
│  └────┬────┘    └────┬────┘    └────┬────┘          │
│       │              │              │                │
│       └──────────────┴──────────────┘                │
│                      │                               │
│               ┌──────▼───────┐                       │
│               │ Smoke Tests  │                       │
│               └──────────────┘                       │
└───────────────────────────────────────────────────────┘
```

---

## 🚦 Workflow de Desenvolvimento

### Desenvolvimento Diário

```bash
# 1. Criar feature branch
git checkout -b feature/nova-funcionalidade

# 2. Desenvolver e commitar
git add .
git commit -m "feat: adiciona nova funcionalidade"

# 3. Push para GitHub
git push origin feature/nova-funcionalidade

# 4. Criar Pull Request
# - CI workflow é executado automaticamente
# - Code quality scan
# - Security scan
# - Todos os testes devem passar
```

### Deploy para Staging

```bash
# 1. Merge para develop
git checkout develop
git merge feature/nova-funcionalidade
git push origin develop

# 2. CI Pipeline executa
# 3. Se aprovado, manual deploy para staging via workflow_dispatch
```

### Deploy para Produção

```bash
# 1. Merge para main
git checkout main
git merge develop
git push origin main

# 2. CD Pipeline executa automaticamente
# 3. Deploy para ambiente production
# 4. Smoke tests verificam saúde
```

---

## 📊 Monitoramento do Pipeline

### Visualizar Status

1. GitHub → Actions tab
2. Selecionar workflow (CI/CD)
3. Ver execução em tempo real
4. Download logs se necessário

### Métricas Importantes

- ✅ **Build Success Rate:** Deve ser > 95%
- ⏱️ **Build Duration:** CI < 5min, CD < 10min
- 🔒 **Security Issues:** 0 critical vulnerabilities
- 📊 **Code Coverage:** > 60% (Phase 1.5)

---

## 🛠️ Troubleshooting

### Build Failed

```bash
# Verificar erros de compilação localmente
dotnet build --configuration Release

# Verificar testes
dotnet test
```

### Deploy Failed

```bash
# Verificar credenciais Azure
az login --service-principal \
  -u <clientId> \
  -p <clientSecret> \
  --tenant <tenantId>

# Testar deploy manual
az webapp deploy \
  --resource-group <RG> \
  --name <APP_NAME> \
  --src-path ./publish
```

### Key Vault Access Denied

```bash
# Adicionar permissão ao Service Principal
az keyvault set-policy \
  --name <KEYVAULT_NAME> \
  --spn <clientId> \
  --secret-permissions get list
```

### Functions Not Starting

```bash
# Verificar logs
az functionapp log tail \
  --name <FUNCTIONAPP_NAME> \
  --resource-group <RG>

# Restart
az functionapp restart \
  --name <FUNCTIONAPP_NAME> \
  --resource-group <RG>
```

---

## 🔄 Rollback Process

Se o deploy falhar ou causar problemas:

### Opção 1: Revert via Git

```bash
# Reverter último commit
git revert HEAD
git push origin main

# Pipeline executará deploy com versão anterior
```

### Opção 2: Deploy Slot Swap

```bash
# Trocar slots (staging <-> production)
az webapp deployment slot swap \
  --resource-group smartcost-rg-prod \
  --name smartcost-api-prod \
  --slot staging \
  --target-slot production
```

### Opção 3: Manual Rollback

```bash
# Fazer checkout de versão estável
git checkout <commit-hash>

# Trigger manual deploy
gh workflow run cd.yml
```

---

## 📈 Melhorias Futuras (Phase 2+)

- [ ] **Blue/Green Deployment**
- [ ] **Canary Releases** (10% → 50% → 100%)
- [ ] **Automated Performance Testing**
- [ ] **Rollback Automation** (baseado em métricas)
- [ ] **Multi-Region Deployment**
- [ ] **Feature Flags** (LaunchDarkly/Azure App Config)

---

## ✅ Checklist de Deploy

Antes de cada deploy para produção:

- [ ] Todos os testes passando localmente
- [ ] Code review aprovado por 2+ pessoas
- [ ] Documentação atualizada
- [ ] Changelog atualizado (COMMERCIALIZATION_PROGRESS.md)
- [ ] Secrets configurados no ambiente
- [ ] Bicep templates validados
- [ ] Backup do banco de dados realizado
- [ ] Stakeholders notificados
- [ ] Rollback plan documentado
- [ ] Monitoring/alerts configurados

---

## 📞 Suporte

- **CI/CD Issues:** Verificar GitHub Actions logs
- **Azure Issues:** Verificar Application Insights
- **Infraestrutura:** Verificar Azure Portal
- **Logs:** `az webapp log tail` ou Application Insights

---

**Última Atualização:** 2024-01-XX  
**Versão Pipeline:** 1.0.0  
**Maintainer:** DevOps Team
