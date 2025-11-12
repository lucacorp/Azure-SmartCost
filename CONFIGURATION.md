# Configuração do Azure SmartCost

## Variáveis de Ambiente Necessárias

### Para Desenvolvimento Local (local.settings.json):
```json
{
  "Values": {
    "AZURE_SUBSCRIPTION_ID": "sua-subscription-id",
    "AZURE_TENANT_ID": "seu-tenant-id", 
    "AZURE_CLIENT_ID": "seu-client-id"
  },
  "ConnectionStrings": {
    "CosmosDb": "AccountEndpoint=https://conta.documents.azure.com:443/;AccountKey=chave;"
  }
}
```

### Para Produção (Azure Function App Settings):
- `AZURE_SUBSCRIPTION_ID`: ID da subscription para monitorar custos
- `AZURE_TENANT_ID`: ID do tenant do Azure AD
- `AZURE_CLIENT_ID`: ID do cliente (Managed Identity ou Service Principal)
- `CosmosDb__ConnectionString`: Connection string do Cosmos DB
- `CosmosDb__DatabaseName`: Nome do banco (padrão: SmartCostDB)

## Configuração de Identidade Gerenciada

### 1. System Assigned Managed Identity:
```bash
# Habilitar System Assigned Identity na Function App
az functionapp identity assign --name sua-function-app --resource-group seu-rg

# Dar permissões de leitura no Cost Management
az role assignment create \
  --assignee $(az functionapp identity show --name sua-function-app --resource-group seu-rg --query principalId -o tsv) \
  --role "Cost Management Reader" \
  --scope "/subscriptions/sua-subscription-id"
```

### 2. User Assigned Managed Identity:
```bash
# Criar User Assigned Identity
az identity create --name smartcost-identity --resource-group seu-rg

# Atribuir à Function App
az functionapp identity assign \
  --name sua-function-app \
  --resource-group seu-rg \
  --identities "/subscriptions/sua-sub/resourcegroups/seu-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/smartcost-identity"
```

## Permissões Necessárias

### Cost Management:
- Cost Management Reader (para ler dados de custo)
- Billing Reader (para acessar dados de faturamento)

### Cosmos DB:
- DocumentDB Account Contributor (para criar containers)
- Cosmos DB Account Reader Role (para ler dados)

### Key Vault (se usado):
- Key Vault Reader
- Key Vault Secrets User

## Recursos de Infraestrutura

### Azure Function App:
- SKU: Consumption ou Premium
- Runtime: .NET 8 Isolated
- Managed Identity habilitada

### Cosmos DB:
- API: SQL (Core)
- Throughput: 400 RU/s por container (mínimo)
- Partitioning: Por data (yyyy-MM)

### Key Vault (opcional):
- Soft delete habilitado
- Purge protection habilitado
- Access policies para Managed Identity

## Exemplo de Deploy com Bicep

Veja o arquivo `infra/main.bicep` para exemplo completo de infraestrutura.

## Troubleshooting

### Erro: "AZURE_SUBSCRIPTION_ID not configured"
- Verificar se a variável está configurada no local.settings.json ou App Settings
- Para Managed Identity, não é necessário AZURE_CLIENT_SECRET

### Erro: "Cosmos DB connection failed"  
- Verificar connection string
- Verificar permissões da Managed Identity
- Verificar se o Cosmos DB permite acesso da Function App

### Erro: "Access denied to Cost Management API"
- Verificar se a Managed Identity tem role "Cost Management Reader"
- Verificar se o scope inclui a subscription correta