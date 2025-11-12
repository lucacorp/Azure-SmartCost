# 🧪 Power BI Integration Test Suite

Este documento descreve como testar a integração Power BI após a configuração de produção.

## 🎯 Pré-requisitos para Teste

- Azure App Service configurado e rodando
- Power BI Workspace criado e configurado
- Azure AD App Registration com permissões corretas
- Todas as variáveis de ambiente configuradas

## 🧪 Testes Manuais

### 1. Teste de Saúde da API

```bash
# Teste básico de saúde
curl https://your-app.azurewebsites.net/api/health

# Teste detalhado de saúde
curl https://your-app.azurewebsites.net/api/health/detailed
```

**Resultado esperado**: Status 200 com informações de saúde do sistema

### 2. Teste de Templates Power BI

```bash
# Obter templates disponíveis
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     https://your-app.azurewebsites.net/api/powerbi/templates
```

**Resultado esperado**: Lista de templates com 4 relatórios predefinidos

### 3. Teste de Configuração de Embed

```bash
# Testar configuração de embed para relatório
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     "https://your-app.azurewebsites.net/api/powerbi/embed-config?reportId=smartcost-executive-dashboard"
```

**Resultado esperado**: Objeto JSON com embed URL e access token

### 4. Teste de Dados de Custo

```bash
# Obter dados de custo para Power BI
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     "https://your-app.azurewebsites.net/api/powerbi/cost-data?startDate=2024-10-01&endDate=2024-11-01"
```

**Resultado esperado**: Array de registros de custo formatados para Power BI

### 5. Teste de Refresh do Dataset

```bash
# Executar refresh do dataset
curl -X POST \
     -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     -H "Content-Type: application/json" \
     https://your-app.azurewebsites.net/api/powerbi/refresh-dataset
```

**Resultado esperado**: Status 200 confirmando o refresh iniciado

## 🌐 Testes do Frontend

### 1. Teste de Carregamento da Dashboard

1. Acesse `https://your-frontend-url`
2. Faça login no sistema
3. Navegue para a aba "Power BI"
4. Verifique se os relatórios carregam corretamente

### 2. Teste de Interatividade

1. Teste o botão "Refresh" nos relatórios
2. Teste o modo fullscreen
3. Teste a funcionalidade de export (se disponível)
4. Verifique se os filtros funcionam

### 3. Teste de Responsividade

1. Teste em diferentes tamanhos de tela
2. Verifique se os relatórios se ajustam corretamente
3. Teste em dispositivos móveis

## 🔧 Scripts de Teste Automatizado

### Teste PowerShell

```powershell
# Test-PowerBiIntegration.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$BaseUrl,
    
    [Parameter(Mandatory=$true)]
    [string]$JwtToken
)

$headers = @{
    "Authorization" = "Bearer $JwtToken"
    "Content-Type" = "application/json"
}

Write-Host "🧪 Testing Power BI Integration..." -ForegroundColor Green

# Test 1: Health Check
try {
    $healthResponse = Invoke-RestMethod -Uri "$BaseUrl/api/health" -Method GET
    Write-Host "✅ Health Check: PASSED" -ForegroundColor Green
} catch {
    Write-Host "❌ Health Check: FAILED" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Templates
try {
    $templatesResponse = Invoke-RestMethod -Uri "$BaseUrl/api/powerbi/templates" -Method GET -Headers $headers
    if ($templatesResponse.data.Count -ge 4) {
        Write-Host "✅ Templates Test: PASSED ($($templatesResponse.data.Count) templates found)" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Templates Test: WARNING (Expected 4+ templates, found $($templatesResponse.data.Count))" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Templates Test: FAILED" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Embed Config
try {
    $embedResponse = Invoke-RestMethod -Uri "$BaseUrl/api/powerbi/embed-config?reportId=smartcost-executive-dashboard" -Method GET -Headers $headers
    if ($embedResponse.embedUrl -and $embedResponse.accessToken) {
        Write-Host "✅ Embed Config Test: PASSED" -ForegroundColor Green
    } else {
        Write-Host "❌ Embed Config Test: FAILED (Missing embed URL or access token)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Embed Config Test: FAILED" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎯 Test Summary Complete" -ForegroundColor Cyan
```

### Teste Node.js/JavaScript

```javascript
// test-powerbi-integration.js
const axios = require('axios');

async function testPowerBiIntegration(baseUrl, jwtToken) {
    const headers = {
        'Authorization': `Bearer ${jwtToken}`,
        'Content-Type': 'application/json'
    };

    console.log('🧪 Testing Power BI Integration...');

    // Test 1: Health Check
    try {
        const response = await axios.get(`${baseUrl}/api/health`);
        console.log('✅ Health Check: PASSED');
    } catch (error) {
        console.log('❌ Health Check: FAILED');
        console.log(`Error: ${error.message}`);
    }

    // Test 2: Templates
    try {
        const response = await axios.get(`${baseUrl}/api/powerbi/templates`, { headers });
        if (response.data.data && response.data.data.length >= 4) {
            console.log(`✅ Templates Test: PASSED (${response.data.data.length} templates found)`);
        } else {
            console.log(`⚠️ Templates Test: WARNING (Expected 4+ templates, found ${response.data.data?.length || 0})`);
        }
    } catch (error) {
        console.log('❌ Templates Test: FAILED');
        console.log(`Error: ${error.message}`);
    }

    // Test 3: Embed Config
    try {
        const response = await axios.get(`${baseUrl}/api/powerbi/embed-config?reportId=smartcost-executive-dashboard`, { headers });
        if (response.data.embedUrl && response.data.accessToken) {
            console.log('✅ Embed Config Test: PASSED');
        } else {
            console.log('❌ Embed Config Test: FAILED (Missing embed URL or access token)');
        }
    } catch (error) {
        console.log('❌ Embed Config Test: FAILED');
        console.log(`Error: ${error.message}`);
    }

    console.log('\n🎯 Test Summary Complete');
}

// Usage: node test-powerbi-integration.js
const baseUrl = process.env.API_BASE_URL || 'https://your-app.azurewebsites.net';
const jwtToken = process.env.JWT_TOKEN || 'your-jwt-token';

testPowerBiIntegration(baseUrl, jwtToken);
```

## 🔍 Troubleshooting Common Issues

### Issue 1: "Access token invalid or expired"

**Causa**: Credenciais Azure AD incorretas ou expiradas
**Solução**:
1. Verificar `POWERBI_CLIENT_ID` e `POWERBI_CLIENT_SECRET`
2. Verificar se o app registration tem as permissões corretas
3. Regenerar client secret se necessário

### Issue 2: "Workspace not found"

**Causa**: `POWERBI_WORKSPACE_ID` incorreto ou permissões insuficientes
**Solução**:
1. Verificar se o Workspace ID está correto
2. Adicionar o service principal como contribuidor no workspace
3. Verificar se o workspace não foi deletado

### Issue 3: "Dataset not found"

**Causa**: Dataset ainda não foi criado ou `POWERBI_DATASET_ID` incorreto
**Solução**:
1. Executar a aplicação para criar o dataset automaticamente
2. Verificar se o dataset foi criado no Power BI Service
3. Atualizar `POWERBI_DATASET_ID` com o ID correto

### Issue 4: "CORS errors in browser"

**Causa**: Frontend URL não configurada no CORS
**Solução**:
1. Adicionar `FRONTEND_URL` nas configurações
2. Verificar se o URL está na lista de origens permitidas
3. Reiniciar o App Service após mudanças

### Issue 5: "Reports not loading in frontend"

**Causa**: Problemas de rede, autenticação ou configuração
**Solução**:
1. Verificar se o JWT token é válido
2. Verificar logs do browser para erros
3. Testar endpoints da API diretamente
4. Verificar se Power BI service está funcionando

## ✅ Checklist de Verificação

- [ ] API de saúde responde corretamente
- [ ] Templates Power BI são retornados (4 templates)
- [ ] Embed config é gerado com sucesso
- [ ] Dados de custo são retornados pela API
- [ ] Refresh do dataset funciona
- [ ] Frontend carrega sem erros
- [ ] Relatórios Power BI são exibidos corretamente
- [ ] Funcionalidades interativas funcionam
- [ ] Responsividade está adequada
- [ ] Logs não mostram erros críticos
- [ ] Performance está adequada (<5s para carregar relatórios)

## 📊 Métricas de Performance

- **Tempo de carregamento da página**: < 3 segundos
- **Tempo de carregamento dos relatórios**: < 5 segundos
- **Tempo de resposta da API**: < 2 segundos
- **Taxa de sucesso das chamadas**: > 95%

## 🚨 Monitoramento de Produção

### Application Insights Queries

```kql
// Erros relacionados ao Power BI
exceptions
| where timestamp > ago(24h)
| where outerMessage contains "PowerBI" or outerMessage contains "power bi"
| summarize count() by bin(timestamp, 1h), outerMessage
| order by timestamp desc

// Performance das chamadas Power BI API
requests
| where timestamp > ago(24h)
| where url contains "/api/powerbi/"
| summarize avg(duration), count() by bin(timestamp, 1h), name
| order by timestamp desc

// Taxa de sucesso dos embeds
customEvents
| where timestamp > ago(24h)
| where name == "PowerBI.EmbedSuccess" or name == "PowerBI.EmbedFailure"
| summarize success_rate = todouble(countif(name == "PowerBI.EmbedSuccess")) * 100 / count()
    by bin(timestamp, 1h)
| order by timestamp desc
```

---
🎯 **Objetivo**: Garantir que a integração Power BI funcione perfeitamente em produção, oferecendo uma experiência rica de analytics para os usuários do Azure SmartCost.