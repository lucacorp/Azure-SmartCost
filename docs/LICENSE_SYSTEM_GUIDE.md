# 🔐 Sistema de Licenças - Azure SmartCost

## 📋 Visão Geral

Sistema de licenciamento com **trial de 14 dias** e **licença mensal de $40**.

### ✅ Funcionalidades

1. **Trial Automático**: Primeira instalação = 14 dias grátis
2. **Validação Automática**: Todas as APIs checam licença antes de responder
3. **Gestão Manual**: Você ativa/suspende licenças via APIs administrativas
4. **Armazenamento**: Licenças salvas no Cosmos DB (container `Licenses`)

---

## 🎯 Fluxo do Cliente

### 1. Cliente Instala do Marketplace
- ARM template cria recursos
- Cliente acessa dashboard
- **Primeira chamada à API** → Licença trial criada automaticamente
- **14 dias para testar grátis**

### 2. Trial Expira (Após 14 Dias)
- APIs retornam erro `403 Forbidden`
- Mensagem: `"Trial period expired on 2025-12-10. Please activate your license."`
- Cliente te contata para ativar

### 3. Cliente Paga $40
- Você recebe pagamento (Stripe/PayPal/Transferência)
- Você ativa licença via API admin
- Cliente volta a ter acesso imediatamente

---

## 🔧 APIs Administrativas

### Base URL
```
https://seu-function-app.azurewebsites.net/api/admin
```

### 1. Listar Todas as Licenças
```bash
GET /admin/licenses
Authorization: Function key (x-functions-key: SUA_FUNCTION_KEY)
```

**Resposta:**
```json
[
  {
    "id": "abc123-subscription-id",
    "subscriptionId": "abc123-subscription-id",
    "customerEmail": "cliente@empresa.com",
    "customerName": "João Silva",
    "status": "Trial",
    "createdAt": "2025-11-26T10:00:00Z",
    "activatedAt": null,
    "expiresAt": null,
    "monthlyFee": 40.00,
    "currency": "USD",
    "trialDays": 14
  }
]
```

### 2. Ativar Licença (Após Pagamento)
```bash
POST /admin/license/{subscriptionId}/activate
Authorization: Function key
Content-Type: application/json

{
  "durationMonths": 1
}
```

**Resposta:**
```json
{
  "message": "License activated for 1 month(s)",
  "license": {
    "id": "abc123-subscription-id",
    "status": "Active",
    "activatedAt": "2025-11-26T15:30:00Z",
    "expiresAt": "2025-12-26T15:30:00Z"
  }
}
```

### 3. Suspender Licença (Não Pagou)
```bash
POST /admin/license/{subscriptionId}/suspend
Authorization: Function key
```

### 4. Criar Licença Manualmente
```bash
POST /admin/license
Authorization: Function key
Content-Type: application/json

{
  "subscriptionId": "abc123-subscription-id",
  "customerEmail": "cliente@empresa.com",
  "customerName": "João Silva"
}
```

---

## 📊 Status de Licença

| Status | Descrição | Cliente Pode Usar? |
|--------|-----------|-------------------|
| `Trial` | Primeiros 14 dias | ✅ Sim |
| `Active` | Pago e ativo | ✅ Sim |
| `Expired` | Trial ou assinatura expirou | ❌ Não |
| `Suspended` | Você suspendeu manualmente | ❌ Não |
| `Cancelled` | Cliente cancelou | ❌ Não |

---

## 💰 Processo de Cobrança Manual

### Opção 1: Stripe/PayPal

1. Cliente trial expira
2. Cliente te contata
3. Você envia link de pagamento (Stripe/PayPal)
4. Cliente paga $40
5. Você ativa licença via API:
   ```bash
   POST /admin/license/{subscriptionId}/activate
   {"durationMonths": 1}
   ```

### Opção 2: Transferência Bancária

1. Cliente paga via PIX/TED
2. Você confirma pagamento
3. Ativa licença via API

### Opção 3: Nota Fiscal

1. Cliente solicita NF
2. Você emite NF de $40 (+ impostos)
3. Cliente paga
4. Você ativa licença

---

## 🔑 Como Pegar a Function Key

1. Acesse Azure Portal
2. Vá no seu Function App
3. **App Keys** → **Host keys (all functions)**
4. Copie o valor de `default` ou crie uma nova key chamada `admin`
5. Use nos headers:
   ```
   x-functions-key: SEU_FUNCTION_KEY_AQUI
   ```

---

## 🚀 Exemplo de Uso (PowerShell)

### Listar licenças
```powershell
$key = "SUA_FUNCTION_KEY"
$url = "https://smartcost-func-prod.azurewebsites.net/api/admin/licenses"

Invoke-RestMethod -Uri $url -Headers @{"x-functions-key"=$key} | ConvertTo-Json
```

### Ativar licença após pagamento
```powershell
$key = "SUA_FUNCTION_KEY"
$subscriptionId = "abc123-def456"
$url = "https://smartcost-func-prod.azurewebsites.net/api/admin/license/$subscriptionId/activate"

$body = @{ durationMonths = 1 } | ConvertTo-Json

Invoke-RestMethod -Uri $url -Method Post -Headers @{
    "x-functions-key" = $key
    "Content-Type" = "application/json"
} -Body $body | ConvertTo-Json
```

---

## 📧 Email para Clientes (Trial Expirando)

Você pode criar uma Azure Function com timer trigger para enviar emails automáticos:

**Assunto:** Azure SmartCost - Trial expirando em 3 dias

```
Olá {CustomerName},

Seu trial do Azure SmartCost expira em 3 dias (2025-12-10).

Para continuar usando:
1. Acesse: https://seusite.com/pricing
2. Escolha plano mensal ($40/mês)
3. Sua licença será ativada automaticamente

Dúvidas? Responda este email.

Atenciosamente,
Equipe Azure SmartCost
```

---

## 🎯 Configuração Inicial

### 1. Deploy do Código
```bash
cd src/AzureSmartCost.Functions
func azure functionapp publish smartcost-func-prod
```

### 2. Verificar Container Cosmos DB
- Container `Licenses` será criado automaticamente na primeira validação
- Partition Key: `/SubscriptionId`
- Throughput: 400 RU/s

### 3. Testar Sistema
```bash
# Validar licença inexistente (cria trial automático)
curl https://smartcost-func-prod.azurewebsites.net/api/license/validate/test-sub-123

# Ver licenças
curl -H "x-functions-key: SUA_KEY" \
  https://smartcost-func-prod.azurewebsites.net/api/admin/licenses

# Ativar
curl -X POST -H "x-functions-key: SUA_KEY" -H "Content-Type: application/json" \
  -d '{"durationMonths":1}' \
  https://smartcost-func-prod.azurewebsites.net/api/admin/license/test-sub-123/activate
```

---

## 💡 Próximos Passos (Automação Futura)

1. **Webhook de Pagamento**: Integrar Stripe para ativar automaticamente
2. **Email Automático**: Timer Function para avisar trials expirando
3. **Dashboard Admin**: Painel web para gerenciar licenças
4. **Renovação Automática**: Cobrar todo mês automaticamente

---

## ⚠️ Importante

- **Function Keys são secretas**: Nunca commite no Git
- **Subscription ID**: É a Azure Subscription do cliente (pega do query parameter)
- **Cosmos DB**: Já está no ARM template, container será criado automaticamente
- **Trial de 14 dias**: É hard-coded, pode mudar em `License.cs`

---

## 📞 Suporte

Se tiver dúvidas sobre o sistema de licenças, me chama!
