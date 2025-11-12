# Azure SmartCost Monitoring Application

API para monitoramento e análise de custos do Azure usando Azure Cost Management.

## Funcionalidades

- ✅ Consulta de informações da assinatura Azure
- ✅ Resumo de custos por período
- ✅ Análise de custos por grupo de recursos
- ✅ Previsão de custos
- ✅ API RESTful com FastAPI

## Requisitos

- Python 3.8+
- Azure Subscription
- Service Principal com permissões de leitura no Cost Management

## Configuração

### 1. Criar Service Principal no Azure

```bash
az ad sp create-for-rbac --name "smartcost-monitor" --role "Cost Management Reader" --scopes /subscriptions/{subscription-id}
```

### 2. Configurar Variáveis de Ambiente

Copie `.env.example` para `.env` e preencha com suas credenciais:

```bash
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret
AZURE_SUBSCRIPTION_ID=your-subscription-id
```

### 3. Instalar Dependências

```bash
pip install -r requirements.txt
```

## Executar a Aplicação

### Desenvolvimento

```bash
python main.py
```

### Produção

```bash
uvicorn main:app --host 0.0.0.0 --port 8000
```

## Endpoints da API

### Informações Gerais

- `GET /api/v1/` - Informações da API
- `GET /api/v1/health` - Health check

### Azure Cost Management

- `GET /api/v1/subscription` - Informações da assinatura Azure
- `GET /api/v1/costs/summary?days=30` - Resumo de custos (últimos N dias)
- `GET /api/v1/costs/by-resource-group` - Custos por grupo de recursos
- `GET /api/v1/costs/forecast?days=30` - Previsão de custos

## Documentação Interativa

Após iniciar a aplicação, acesse:

- Swagger UI: `http://localhost:8000/docs`
- ReDoc: `http://localhost:8000/redoc`

## Estrutura do Projeto

```
appSmartCost/
├── api/                    # API routes
│   ├── __init__.py
│   └── routes.py
├── config/                 # Configuration
│   ├── __init__.py
│   └── settings.py
├── services/               # Business logic
│   ├── __init__.py
│   └── azure_cost_service.py
├── .env.example           # Environment variables template
├── .gitignore            # Git ignore rules
├── main.py               # Application entry point
├── requirements.txt      # Python dependencies
└── README.md            # This file
```

## Desenvolvimento

### Criar Ambiente Virtual

```bash
python -m venv venv
venv\Scripts\activate  # Windows
source venv/bin/activate  # Linux/Mac
```

### Instalar Dependências de Desenvolvimento

```bash
pip install -r requirements.txt
```

## Licença

MIT License
