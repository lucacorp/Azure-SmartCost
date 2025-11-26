# Guia de Upload da Nova Versão - Azure Marketplace

## ✅ Validação Concluída
- **49/49 testes aprovados** pelo ARM-TTK
- Todos os requisitos do Marketplace atendidos
- Template otimizado e atualizado

## 📦 Arquivo para Upload
**Arquivo:** `infra/marketplace.zip`  
**Tamanho:** ~4.5 KB  
**Conteúdo:**
- `mainTemplate.json` (ARM template principal)
- `createUiDefinition.json` (Interface do usuário)

## 🔄 Mudanças na Nova Versão

### 1. **Analytics com Dados Reais**
- ✅ Removida dependência do Cosmos DB para analytics
- ✅ Integração direta com Azure Cost Management API
- ✅ Managed Identity configurada automaticamente
- ✅ Permissões "Cost Management Reader" atribuídas
- ✅ 4 endpoints de analytics funcionais:
  - `/api/analytics/cost` - Resumo de custos
  - `/api/analytics/services` - Breakdown por serviço
  - `/api/analytics/trend` - Tendência diária
  - `/api/analytics/top-resources` - Top 10 recursos

### 2. **Correções Técnicas**
- ✅ apiVersions atualizadas (Storage: 2023-04-01, Cosmos: 2023-11-15)
- ✅ Outputs usando `uri()` ao invés de `concat()`
- ✅ Todos os testes ARM-TTK aprovados

### 3. **Power BI Removido**
- ✅ Dashboard nativo com Chart.js
- ✅ Visualizações em React/Material-UI
- ✅ Sem necessidade de conta organizacional

## 📋 Passo a Passo - Partner Center

### 1. Acessar Partner Center
1. Vá para: https://partner.microsoft.com/dashboard/marketplace-offers/overview
2. Faça login com sua conta
3. Localize a oferta "Azure SmartCost"

### 2. Criar Nova Versão
1. Clique na oferta existente
2. Vá para **"Technical configuration"**
3. Role até **"Package details"**

### 3. Upload do Pacote
1. Clique em **"+ New package"** ou **"Upload package"**
2. Selecione o arquivo: `c:\DIOazure\Azure-SmartCost\infra\marketplace.zip`
3. Aguarde o upload (arquivo pequeno, < 5 KB)
4. Aguarde a validação automática

### 4. Preencher Detalhes da Versão

**Version number:** `1.1.0` (ou incrementar a atual)

**Release notes (Notas de Versão):**
```markdown
## Versão 1.1.0 - Analytics com Dados Reais

### Novos Recursos
- **Analytics Nativo**: Dashboard integrado com dados reais do Azure Cost Management
- **Visualizações Interativas**: Gráficos de custos, serviços, tendências e recursos
- **Managed Identity**: Autenticação automática e segura com APIs do Azure
- **Sem Dependências Externas**: Removida necessidade do Power BI

### Melhorias Técnicas
- Atualização de APIs para versões mais recentes
- Performance otimizada nas consultas de custo
- Segurança aprimorada com identidades gerenciadas

### Correções
- Resolvidos todos os warnings de validação ARM-TTK
- Outputs de URL otimizados com função uri()
- ApiVersions atualizadas para conformidade
```

### 5. Configuração de Permissões

**IMPORTANTE:** Adicionar na descrição técnica:

```
PERMISSÕES AUTOMÁTICAS:
A solução configura automaticamente:
- System-assigned Managed Identity na Function App
- Role "Cost Management Reader" na subscrição do cliente
- Acesso à Cost Management API para leitura de dados de custo

NENHUMA AÇÃO MANUAL NECESSÁRIA DO CLIENTE.
```

### 6. Atualizar Descrição (Opcional)

Se quiser destacar o novo Analytics, adicione na descrição principal:

```markdown
## 🎯 Analytics Nativo com Dados Reais

Visualize seus custos do Azure em tempo real:
- 💰 Resumo de custos com tendências
- 📊 Breakdown detalhado por serviço
- 📈 Gráficos de tendência diária
- 🏆 Top 10 recursos mais caros

Tudo integrado, sem configuração adicional!
```

### 7. Review e Publish

1. Clique em **"Review and publish"**
2. Revise todas as seções
3. Verifique se não há erros de validação
4. Clique em **"Publish"**
5. Aguarde aprovação da Microsoft (2-5 dias úteis)

## 🧪 Teste Antes de Publicar

**Ambiente de Preview:**
Depois do upload, você pode testar no ambiente de preview antes de publicar para produção.

1. Após upload, clique em **"Preview"**
2. Teste a implantação em uma subscrição de teste
3. Verifique se os endpoints de Analytics funcionam:
   - Acesse: `https://<sua-function-app>.azurewebsites.net/api/analytics/cost?subscriptionId=<id>`
4. Verifique o dashboard em: `https://<sua-static-web-app>.azurestaticapps.net`
5. Vá na aba **Analytics** e confirme que os gráficos carregam

## ✅ Checklist Final

Antes de publicar, confirme:

- [ ] Arquivo `marketplace.zip` gerado com sucesso
- [ ] 49/49 testes ARM-TTK aprovados
- [ ] Version number incrementado
- [ ] Release notes preenchidas
- [ ] Descrição técnica atualizada com permissões
- [ ] Preview testado (recomendado)
- [ ] Screenshots atualizados com novo Analytics (opcional)

## 📊 Métricas de Sucesso

Após publicação, monitore:
- Instalações da nova versão
- Feedback de usuários sobre Analytics
- Uso dos endpoints de Analytics (via Application Insights)
- Taxa de conversão no Marketplace

## 🔗 Links Úteis

- **Partner Center:** https://partner.microsoft.com/dashboard/marketplace-offers
- **Documentação ARM Templates:** https://docs.microsoft.com/azure/azure-resource-manager/templates/
- **Guia de Publicação:** https://docs.microsoft.com/azure/marketplace/plan-azure-app-offer

## 📞 Suporte

Em caso de problemas:
1. Verifique os logs de validação no Partner Center
2. Re-execute ARM-TTK localmente: `Test-AzTemplate -TemplatePath .\marketplace_exploded\`
3. Revise a documentação do Azure Marketplace

---

**Data de Preparação:** 26/11/2025  
**Status:** ✅ Pronto para Upload  
**Versão:** 1.1.0
