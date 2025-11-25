# 📧 Configuração de Alertas por Email - Azure SmartCost

## 🎯 Visão Geral

O Azure SmartCost envia alertas automáticos por email quando os orçamentos atingem os limites configurados. O sistema suporta **SendGrid** (recomendado) e **SMTP** como fallback.

## 📋 Opções de Configuração

### Opção 1: SendGrid (Recomendado) ✅

**Vantagens:**
- ✅ Infraestrutura gerenciada
- ✅ Alta taxa de entrega
- ✅ Templates HTML profissionais
- ✅ Analytics e tracking
- ✅ Tier gratuito: 100 emails/dia

**Setup:**

1. **Criar conta SendGrid:**
   - Acesse: https://signup.sendgrid.com/
   - Plano Free: 100 emails/dia (suficiente para alertas)

2. **Obter API Key:**
   ```bash
   # No portal SendGrid:
   Settings → API Keys → Create API Key
   Nome: AzureSmartCost-Production
   Permissões: Full Access (Mail Send)
   ```

3. **Configurar variáveis de ambiente:**
   ```bash
   # Azure Function App → Configuration → Application Settings
   SENDGRID_API_KEY=SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
   EMAIL_FROM=noreply@azuresmartcost.com
   EMAIL_FROM_NAME=Azure SmartCost
   ```

4. **Verificar domínio do remetente (opcional mas recomendado):**
   ```bash
   # SendGrid → Settings → Sender Authentication
   # Adicionar domínio: azuresmartcost.com
   # Configurar DNS records (SPF, DKIM, DMARC)
   ```

### Opção 2: SMTP Tradicional (Fallback)

**Quando usar:**
- Já possui servidor SMTP
- Necessita usar email corporativo
- Requisitos de compliance específicos

**Setup:**

```bash
# Azure Function App → Configuration
SMTP_HOST=smtp.gmail.com (ou seu servidor)
SMTP_PORT=587
SMTP_USER=seu-email@gmail.com
SMTP_PASSWORD=sua-senha-ou-app-password
EMAIL_FROM=seu-email@gmail.com
EMAIL_FROM_NAME=Azure SmartCost
```

**Gmail Setup:**
1. Ativar 2FA: https://myaccount.google.com/security
2. Criar App Password: https://myaccount.google.com/apppasswords
3. Usar App Password como SMTP_PASSWORD

**Outlook/Office 365:**
```bash
SMTP_HOST=smtp.office365.com
SMTP_PORT=587
```

## 🔧 Instalação do Pacote SendGrid

O pacote já foi adicionado ao projeto:

```xml
<PackageReference Include="SendGrid" Version="9.29.3" />
```

Para instalar localmente:

```bash
cd src/AzureSmartCost.Functions
dotnet add package SendGrid --version 9.29.3
dotnet restore
```

## 📊 Como Funciona

### 1. Monitoramento Automático

Quando um alerta é consultado (`GET /api/alerts/{subscriptionId}`):
```csharp
// Verifica automaticamente todos os alertas
foreach (var alert in alerts)
{
    var percentage = (alert.CurrentSpend / alert.Amount) * 100;
    
    if (percentage >= alert.Threshold) 
    {
        // Envia email se não enviou nas últimas 24h
        SendBudgetAlertEmail(...);
    }
}
```

### 2. Proteção Anti-Spam

- ✅ **Limite de frequência**: Máximo 1 email por alerta a cada 24 horas
- ✅ **Throttling**: Previne envio excessivo
- ✅ **Logging**: Rastreamento de todos os emails enviados

### 3. Template do Email

**Assunto:**
```
⚠️ Azure SmartCost - Alerta de Orçamento: [Nome do Alerta]
```

**Conteúdo HTML:**
- Header com branding Azure SmartCost
- Alert box destacando o status (warning/crítico)
- Métricas principais:
  - Orçamento Total
  - Gasto Atual
  - Percentual Utilizado
  - Limite de Alerta
- Recomendações inteligentes baseadas no percentual
- Botão para acessar dashboard
- Footer profissional

**Exemplo Visual:**
```
┌─────────────────────────────────────┐
│   🔔 ALERTA DE ORÇAMENTO            │
│   Azure SmartCost                   │
├─────────────────────────────────────┤
│ ⚠️ Produção - Main Account          │
│ Orçamento atingiu 85.3% do limite   │
│                                     │
│ Orçamento Total:    R$ 500,00      │
│ Gasto Atual:        R$ 426,50      │
│ Percentual:         85.3%          │
│ Limite Alerta:      80%            │
│                                     │
│ 💡 Recomendações:                  │
│ • ⚠️ Orçamento atingindo limite    │
│ • 📊 Acesse dashboard              │
│ • 🔍 Identifique recursos          │
│                                     │
│     [ 📊 Acessar Dashboard ]       │
└─────────────────────────────────────┘
```

## 🧪 Testando Alertas

### Teste Local

```bash
# 1. Configurar variáveis de ambiente local
# Criar: src/AzureSmartCost.Functions/local.settings.json

{
  "Values": {
    "SENDGRID_API_KEY": "SG.xxxxxxxxx",
    "EMAIL_FROM": "test@example.com",
    "EMAIL_FROM_NAME": "SmartCost Test",
    "CosmosDb__Endpoint": "https://smartcost-cosmos-beta.documents.azure.com:443/",
    "CosmosDb__Key": "your-key"
  }
}

# 2. Executar Azure Function localmente
cd src/AzureSmartCost.Functions
func start

# 3. Criar alerta de teste
curl -X POST http://localhost:7071/api/alerts \
  -H "Content-Type: application/json" \
  -d '{
    "subscriptionId": "e6b85c41-c45d-42a5-955f-d4dfb3b13ce9",
    "name": "Teste Email",
    "amount": 10.00,
    "currentSpend": 9.50,
    "threshold": 80,
    "email": "seu-email@teste.com"
  }'

# 4. Consultar alertas (triggers email check)
curl http://localhost:7071/api/alerts/e6b85c41-c45d-42a5-955f-d4dfb3b13ce9
```

### Teste em Produção

```bash
# 1. Deploy com variáveis configuradas
# Azure Portal → Function App → Configuration

# 2. Criar alerta via Dashboard
# https://blue-flower-0414b9b0f.3.azurestaticapps.net
# Budget Alerts → Novo Alerta

# 3. Aguardar próxima consulta (ou forçar refresh)
```

## 📋 Checklist de Produção

- [ ] Conta SendGrid criada (ou SMTP configurado)
- [ ] API Key gerada e testada
- [ ] Variáveis de ambiente configuradas no Azure
- [ ] Domínio verificado no SendGrid (opcional)
- [ ] Email de teste enviado com sucesso
- [ ] Dashboard exibindo alertas corretamente
- [ ] Logs verificados (sem erros)
- [ ] Frequência de emails testada (24h limit)

## 🔍 Troubleshooting

### Email não está sendo enviado

**Verificar:**
1. Logs da Azure Function:
   ```bash
   az functionapp logs tail --name smartcost-func-beta --resource-group rg-smartcost-beta
   ```

2. Variáveis de ambiente:
   ```bash
   az functionapp config appsettings list --name smartcost-func-beta --resource-group rg-smartcost-beta --query "[?name=='SENDGRID_API_KEY']"
   ```

3. Status do alerta:
   ```json
   {
     "lastNotificationSent": "2025-11-19T15:30:00Z", // < 24h?
     "currentSpend": 85.00,
     "amount": 100.00,
     "threshold": 80 // 85% > 80% = deveria enviar
   }
   ```

### SendGrid retornando erro 401

- ❌ API Key inválida ou expirada
- ✅ Gerar nova API Key no SendGrid
- ✅ Atualizar variável `SENDGRID_API_KEY`

### Emails indo para Spam

**Melhorias:**
1. ✅ Verificar domínio no SendGrid (SPF/DKIM)
2. ✅ Usar domínio próprio em `EMAIL_FROM`
3. ✅ Pedir usuários adicionarem remetente nos contatos
4. ✅ Configurar DMARC

## 📊 Métricas e Monitoring

### Application Insights Queries

```kusto
// Emails enviados nas últimas 24h
traces
| where timestamp > ago(24h)
| where message contains "Email de alerta enviado"
| summarize count() by bin(timestamp, 1h)

// Erros de envio
traces
| where timestamp > ago(24h)
| where severityLevel >= 3
| where message contains "Erro ao enviar email"
```

## 🚀 Próximas Melhorias

**v1.1:**
- [ ] Templates customizáveis por usuário
- [ ] Multi-canal (Email + SMS + Teams)
- [ ] Notificações no dashboard (toast)
- [ ] Histórico de alertas enviados
- [ ] Configuração de timezone

**v1.2:**
- [ ] Webhooks para integração externa
- [ ] Escalação de alertas (hierarquia)
- [ ] Relatórios semanais automáticos
- [ ] Machine Learning para previsões

---

## 📞 Suporte

**Documentação:** [TROUBLESHOOTING.md](./TROUBLESHOOTING.md)
**Logs:** Application Insights
**Issues:** GitHub Issues

🎯 **Azure SmartCost** - Intelligent Cost Management Platform
