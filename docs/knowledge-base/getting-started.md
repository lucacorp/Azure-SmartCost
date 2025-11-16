# 🚀 Azure SmartCost - Guia de Início Rápido

**Bem-vindo ao Azure SmartCost!** Este guia vai te ajudar a configurar tudo em menos de 5 minutos.

---

## 📋 Pré-requisitos

Antes de começar, você precisa:

✅ **Conta Azure ativa** com pelo menos uma subscription  
✅ **Permissões** de Reader ou superior na subscription  
✅ **Navegador moderno** (Chrome, Edge, Firefox, Safari)  
✅ **Email corporativo ou Microsoft Account**

---

## 🎯 Passo 1: Primeiro Acesso

### 1.1 Acessar a Plataforma

Abra seu navegador e acesse: **https://smartcost.azurestaticapps.net**

### 1.2 Fazer Login

1. Clique no botão **"Entrar com Microsoft"**
2. Você será redirecionado para login Azure AD
3. Use suas credenciais Microsoft (mesmo email do Azure)
4. Autorize o acesso quando solicitado

**🔒 Segurança:** Usamos autenticação oficial Microsoft (Azure AD). Suas credenciais nunca passam por nossos servidores.

---

## 🔗 Passo 2: Conectar sua Subscription Azure

### 2.1 Primeira Conexão

Após o login, você verá a tela de boas-vindas:

1. Clique em **"Conectar Subscription"**
2. Selecione a subscription que deseja monitorar
3. Clique em **"Autorizar"**

### 2.2 Permissões Necessárias

O Azure SmartCost precisa de permissões de **LEITURA** para:

- ✅ Cost Management (ler dados de custo)
- ✅ Resources (listar recursos)
- ✅ Resource Groups (agrupar custos)

**❌ NÃO precisamos de:** Permissões de escrita, exclusão ou modificação de recursos.

### 2.3 Múltiplas Subscriptions

Quer monitorar mais de uma subscription?

1. Vá em **Configurações** → **Subscriptions**
2. Clique em **"+ Adicionar Subscription"**
3. Repita o processo de autorização

---

## 📊 Passo 3: Importar Dados de Custo

### 3.1 Primeira Importação

Após conectar a subscription:

1. Aguarde 30-60 segundos (importação automática)
2. Ou clique em **"Importar Dados Agora"** no dashboard

### 3.2 O que é Importado?

📅 **Período:** Últimos 30 dias por padrão  
📈 **Dados:** Custos diários por serviço, resource group e tipo de recurso  
🔄 **Atualização:** Automática a cada 6 horas  

### 3.3 Primeira Visualização

Aguarde a importação concluir (1-2 minutos). Você verá:

- **Total de gastos** do período
- **Breakdown por serviço** (VMs, Storage, etc.)
- **Breakdown por resource group**
- **Tendência diária** de custos

---

## 🔔 Passo 4: Configurar seu Primeiro Alerta

### 4.1 Criar Alerta de Budget

1. No menu lateral, clique em **"Alertas"**
2. Clique em **"+ Novo Alerta"**
3. Escolha **"Alerta de Budget"**

### 4.2 Configurar Thresholds

```
Nome: Budget Mensal Produção
Subscription: [Sua subscription]
Budget Mensal: R$ 5.000
Alertar em: 80% (R$ 4.000)
```

### 4.3 Escolher Canal de Notificação

Marque onde quer receber alertas:

- ✅ **Email** (recomendado para começar)
- ⬜ **Slack** (configurar depois)
- ⬜ **Teams** (configurar depois)
- ⬜ **Webhook** (integrações avançadas)

### 4.4 Salvar e Ativar

1. Clique em **"Salvar"**
2. Teste clicando em **"Enviar Teste"**
3. Verifique seu email

---

## 💡 Passo 5: Ver Recomendações de Economia

### 5.1 Acessar Recomendações

1. No menu lateral, clique em **"Recomendações"**
2. Você verá lista de oportunidades de economia

### 5.2 Tipos de Recomendações

🟢 **Baixo Risco** - Pode aplicar sem medo  
🟡 **Médio Risco** - Requer validação  
🔴 **Alto Impacto** - Economias significativas  

### 5.3 Aplicar uma Recomendação

**Exemplo: VM ociosa**

```
💡 Problema: VM "vm-app-dev" com 3% de uso de CPU
💰 Economia: R$ 450/mês
🎯 Ação: Downsize para Standard_B2s

[Ver Detalhes] [Aplicar Agora] [Ignorar]
```

1. Clique em **"Ver Detalhes"** para análise completa
2. Se concordar, clique em **"Aplicar Agora"**
3. Acompanhe o progresso na aba **"Tarefas"**

---

## 🎨 Passo 6: Personalizar seu Dashboard

### 6.1 Escolher Período

No topo do dashboard, selecione:

- **Últimos 7 dias**
- **Últimos 30 dias** (padrão)
- **Últimos 90 dias**
- **Custom** (escolha datas específicas)

### 6.2 Filtrar por Subscription

Se você tem múltiplas subscriptions:

1. Use o dropdown **"Todas as Subscriptions"**
2. Selecione a que deseja visualizar
3. Dashboard atualiza automaticamente

### 6.3 Exportar Relatórios

Para compartilhar com sua equipe:

1. Clique em **"Exportar"** no canto superior direito
2. Escolha formato:
   - **PDF** (para apresentações)
   - **Excel** (para análises)
   - **CSV** (para BI tools)

---

## 📱 Passo 7: Instalar PWA (Mobile)

### 7.1 No Chrome (Desktop)

1. Clique no ícone **⊕** na barra de endereço
2. Clique em **"Instalar Azure SmartCost"**
3. Pronto! Agora tem atalho no desktop

### 7.2 No Mobile (iOS/Android)

**iOS (Safari):**
1. Abra o site
2. Toque em **Compartilhar** (🔗)
3. Role e toque em **"Adicionar à Tela Inicial"**

**Android (Chrome):**
1. Abra o site
2. Toque no menu **⋮**
3. Toque em **"Adicionar à tela inicial"**

### 7.3 Benefícios do PWA

✅ Funciona offline (visualiza dados já carregados)  
✅ Notificações push (alertas em tempo real)  
✅ Abre como app nativo  
✅ Mais rápido que browser  

---

## 🔧 Configurações Avançadas

### Integração com Slack

1. **Configurações** → **Integrações**
2. Clique em **"Conectar Slack"**
3. Escolha o canal (ex: #finance)
4. Autorize a integração
5. Teste enviando notificação

### Integração com Microsoft Teams

1. **Configurações** → **Integrações**
2. Clique em **"Conectar Teams"**
3. Cole o Webhook URL do canal
4. Teste a conexão

### API Access (Desenvolvedores)

1. **Configurações** → **API**
2. Clique em **"Gerar API Key"**
3. Copie e guarde em local seguro
4. Veja documentação: `/swagger`

---

## ❓ FAQ - Perguntas Frequentes

### Por que meus dados estão vazios?

**R:** Aguarde 2-5 minutos após conectar a subscription. A primeira importação leva um tempo. Se persistir, clique em "Importar Dados Agora" manualmente.

### Posso usar com Azure Government?

**R:** Atualmente suportamos apenas Azure Commercial. Azure Government no roadmap para Q1/2026.

### Os dados são atualizados em tempo real?

**R:** Quase! Atualizamos a cada 6 horas automaticamente. Você pode forçar importação manual clicando em "Atualizar".

### Quanto custa o Azure SmartCost?

**R:** Beta testers (primeiros 50) têm acesso **GRATUITO VITALÍCIO**! Após beta: R$ 99/mês (PRO) ou R$ 399/mês (Enterprise).

### Vocês armazenam minhas credenciais Azure?

**R:** NÃO! Usamos Azure AD (OAuth 2.0). Suas credenciais ficam 100% na Microsoft. Só recebemos tokens de acesso temporários.

### Posso cancelar a qualquer momento?

**R:** Sim! Sem multa, sem pegadinha. Cancele em **Configurações** → **Assinatura** → **Cancelar**.

### Como funciona o suporte?

**Beta testers:** Suporte prioritário via Discord/Slack  
**PRO:** Email (48h SLA)  
**Enterprise:** Email (4h SLA) + Account Manager

---

## 🆘 Precisa de Ajuda?

### Suporte Beta Testers

🔹 **Discord:** [discord.gg/azuresmartcost](#) (resposta em minutos)  
🔹 **Email:** beta@azuresmartcost.com  
🔹 **Telegram:** @AzureSmartCostSupport  

### Recursos Úteis

📖 **Documentação Completa:** [docs.azuresmartcost.com](#)  
🎥 **Vídeo Tutoriais:** [youtube.com/@azuresmartcost](#)  
💬 **Comunidade:** [community.azuresmartcost.com](#)  

---

## 🎉 Próximos Passos

Agora que você configurou tudo:

1. ✅ Explore o dashboard e se familiarize
2. ✅ Configure alertas para não ter surpresas
3. ✅ Aplique pelo menos 1 recomendação de economia
4. ✅ Compartilhe com sua equipe
5. ✅ Dê seu feedback no Discord!

**Bem-vindo à comunidade Azure SmartCost! 🚀**

---

*Última atualização: 16/11/2025 | Versão Beta 1.0* with Azure SmartCost

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
