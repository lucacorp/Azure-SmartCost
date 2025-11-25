# 📊 Analytics API - Guia de Uso

## Visão Geral

Como a conta gratuita do Power BI/Fabric não está disponível, implementei uma solução alternativa com **APIs de Analytics nativas** que fornecem dados estruturados para criar visualizações no frontend usando bibliotecas JavaScript como Chart.js, Recharts ou D3.js.

## 🎯 Benefícios desta Solução

✅ **Totalmente gratuito** - Sem necessidade de licenças Power BI  
✅ **Flexível** - Você controla 100% das visualizações  
✅ **Escalável** - Usa Cosmos DB que já está no seu stack  
✅ **Marketplace-Ready** - Funciona para todos os clientes sem configurações extras  

## 📡 Endpoints Disponíveis

### 1. **Cost Analytics Summary**
Retorna métricas agregadas de custo para um período.

```http
GET /api/analytics/cost?subscriptionId={id}&startDate={date}&endDate={date}
```

**Parâmetros:**
- `subscriptionId` (required): Azure Subscription ID
- `startDate` (optional): Data inicial (formato: YYYY-MM-DD). Default: 30 dias atrás
- `endDate` (optional): Data final (formato: YYYY-MM-DD). Default: hoje

**Resposta:**
```json
{
  "startDate": "2025-10-26T00:00:00Z",
  "endDate": "2025-11-25T00:00:00Z",
  "totalCost": 1542.75,
  "currency": "USD",
  "dailyAverage": 51.425,
  "trendPercentage": 12.5,
  "topService": "Azure Kubernetes Service",
  "recordCount": 450
}
```

**Campos:**
- `totalCost`: Custo total no período
- `dailyAverage`: Média de custo por dia
- `trendPercentage`: Variação percentual (primeira metade vs segunda metade do período)
- `topService`: Serviço com maior custo
- `recordCount`: Número de registros analisados

---

### 2. **Service Cost Breakdown**
Retorna breakdown de custos por serviço Azure.

```http
GET /api/analytics/services?subscriptionId={id}&startDate={date}&endDate={date}
```

**Resposta:**
```json
[
  {
    "serviceName": "Azure Kubernetes Service",
    "totalCost": 620.50,
    "currency": "USD",
    "resourceCount": 12,
    "averageDailyCost": 20.68
  },
  {
    "serviceName": "Azure SQL Database",
    "totalCost": 385.25,
    "currency": "USD",
    "resourceCount": 3,
    "averageDailyCost": 12.84
  }
]
```

**Uso:** Ideal para criar **gráficos de pizza** ou **gráficos de barras** mostrando distribuição de custos por serviço.

---

### 3. **Daily Cost Trend**
Retorna tendência de custos dia a dia.

```http
GET /api/analytics/trend?subscriptionId={id}&startDate={date}&endDate={date}
```

**Resposta:**
```json
[
  {
    "date": "2025-10-26T00:00:00Z",
    "totalCost": 45.30,
    "currency": "USD"
  },
  {
    "date": "2025-10-27T00:00:00Z",
    "totalCost": 52.15,
    "currency": "USD"
  },
  {
    "date": "2025-10-28T00:00:00Z",
    "totalCost": 48.90,
    "currency": "USD"
  }
]
```

**Uso:** Ideal para criar **gráficos de linha** mostrando evolução temporal dos custos.

---

### 4. **Top Cost Resources**
Retorna os recursos com maior custo.

```http
GET /api/analytics/top-resources?subscriptionId={id}&startDate={date}&endDate={date}&top=10
```

**Parâmetros:**
- `top` (optional): Número de recursos a retornar. Default: 10

**Resposta:**
```json
[
  {
    "resourceId": "/subscriptions/.../resourceGroups/rg-prod/providers/Microsoft.Compute/virtualMachines/vm-web-01",
    "resourceName": "vm-web-01",
    "serviceName": "Virtual Machines",
    "totalCost": 285.40,
    "currency": "USD"
  },
  {
    "resourceId": "/subscriptions/.../resourceGroups/rg-prod/providers/Microsoft.Sql/servers/sql-prod/databases/db-main",
    "resourceName": "db-main",
    "serviceName": "Azure SQL Database",
    "totalCost": 180.75,
    "currency": "USD"
  }
]
```

**Uso:** Ideal para criar **tabelas** ou **gráficos de barras horizontais** mostrando recursos mais caros.

---

## 🔧 Como Integrar no Frontend

### Exemplo com Chart.js

```javascript
// 1. Buscar dados do endpoint
async function loadCostTrend(subscriptionId) {
  const startDate = new Date();
  startDate.setDate(startDate.getDate() - 30);
  
  const response = await fetch(
    `https://smartcost-func-beta.azurewebsites.net/api/analytics/trend?` +
    `subscriptionId=${subscriptionId}&` +
    `startDate=${startDate.toISOString().split('T')[0]}&` +
    `endDate=${new Date().toISOString().split('T')[0]}`
  );
  
  const data = await response.json();
  return data;
}

// 2. Criar gráfico
async function renderCostChart(subscriptionId) {
  const trendData = await loadCostTrend(subscriptionId);
  
  const ctx = document.getElementById('costChart').getContext('2d');
  new Chart(ctx, {
    type: 'line',
    data: {
      labels: trendData.map(d => new Date(d.date).toLocaleDateString()),
      datasets: [{
        label: 'Daily Cost (USD)',
        data: trendData.map(d => d.totalCost),
        borderColor: 'rgb(75, 192, 192)',
        tension: 0.1
      }]
    },
    options: {
      responsive: true,
      plugins: {
        title: {
          display: true,
          text: 'Cost Trend - Last 30 Days'
        }
      }
    }
  });
}
```

### Exemplo com Recharts (React)

```jsx
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend } from 'recharts';

function CostTrendChart({ subscriptionId }) {
  const [data, setData] = useState([]);

  useEffect(() => {
    fetch(`/api/analytics/trend?subscriptionId=${subscriptionId}`)
      .then(res => res.json())
      .then(data => setData(data));
  }, [subscriptionId]);

  return (
    <LineChart width={800} height={400} data={data}>
      <CartesianGrid strokeDasharray="3 3" />
      <XAxis dataKey="date" />
      <YAxis />
      <Tooltip />
      <Legend />
      <Line type="monotone" dataKey="totalCost" stroke="#8884d8" />
    </LineChart>
  );
}
```

---

## 🚀 Próximos Passos

1. **Deploy do Function App atualizado** com os novos endpoints
2. **Integrar no frontend** usando Chart.js ou biblioteca similar
3. **Testar com dados reais** do Cosmos DB
4. **Criar dashboards interativos** no smartcost-dashboard

---

## 📝 Exemplo Completo de Dashboard

Vou criar um exemplo HTML completo que você pode usar como base:

```html
<!DOCTYPE html>
<html>
<head>
    <title>SmartCost Analytics Dashboard</title>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <style>
        body {
            font-family: Arial, sans-serif;
            margin: 20px;
            background: #f5f5f5;
        }
        .dashboard {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
        }
        .card {
            background: white;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        .metric {
            font-size: 2em;
            font-weight: bold;
            color: #0078d4;
        }
        canvas {
            max-height: 300px;
        }
    </style>
</head>
<body>
    <h1>🎯 SmartCost Analytics Dashboard</h1>
    
    <div class="dashboard">
        <!-- Summary Card -->
        <div class="card">
            <h2>Cost Summary (Last 30 Days)</h2>
            <div id="summary">Loading...</div>
        </div>
        
        <!-- Trend Card -->
        <div class="card">
            <h2>Cost Trend</h2>
            <canvas id="trendChart"></canvas>
        </div>
        
        <!-- Service Breakdown Card -->
        <div class="card">
            <h2>Cost by Service</h2>
            <canvas id="serviceChart"></canvas>
        </div>
        
        <!-- Top Resources Card -->
        <div class="card">
            <h2>Top 10 Resources</h2>
            <canvas id="resourceChart"></canvas>
        </div>
    </div>

    <script>
        const API_BASE = 'https://smartcost-func-beta.azurewebsites.net/api';
        const SUBSCRIPTION_ID = 'YOUR_SUBSCRIPTION_ID';

        // Load Summary
        async function loadSummary() {
            const res = await fetch(`${API_BASE}/analytics/cost?subscriptionId=${SUBSCRIPTION_ID}`);
            const data = await res.json();
            
            document.getElementById('summary').innerHTML = `
                <p class="metric">$${data.totalCost.toFixed(2)}</p>
                <p>Daily Average: $${data.dailyAverage.toFixed(2)}</p>
                <p>Trend: ${data.trendPercentage > 0 ? '📈' : '📉'} ${Math.abs(data.trendPercentage).toFixed(1)}%</p>
                <p>Top Service: ${data.topService}</p>
            `;
        }

        // Load Trend Chart
        async function loadTrendChart() {
            const res = await fetch(`${API_BASE}/analytics/trend?subscriptionId=${SUBSCRIPTION_ID}`);
            const data = await res.json();
            
            new Chart(document.getElementById('trendChart'), {
                type: 'line',
                data: {
                    labels: data.map(d => new Date(d.date).toLocaleDateString()),
                    datasets: [{
                        label: 'Daily Cost',
                        data: data.map(d => d.totalCost),
                        borderColor: '#0078d4',
                        fill: true,
                        backgroundColor: 'rgba(0, 120, 212, 0.1)'
                    }]
                }
            });
        }

        // Load Service Chart
        async function loadServiceChart() {
            const res = await fetch(`${API_BASE}/analytics/services?subscriptionId=${SUBSCRIPTION_ID}`);
            const data = await res.json();
            
            new Chart(document.getElementById('serviceChart'), {
                type: 'pie',
                data: {
                    labels: data.map(s => s.serviceName),
                    datasets: [{
                        data: data.map(s => s.totalCost),
                        backgroundColor: [
                            '#0078d4', '#00b294', '#ffb900', '#e81123',
                            '#5c2d91', '#008272', '#d83b01', '#107c10'
                        ]
                    }]
                }
            });
        }

        // Load Resource Chart
        async function loadResourceChart() {
            const res = await fetch(`${API_BASE}/analytics/top-resources?subscriptionId=${SUBSCRIPTION_ID}&top=10`);
            const data = await res.json();
            
            new Chart(document.getElementById('resourceChart'), {
                type: 'bar',
                data: {
                    labels: data.map(r => r.resourceName),
                    datasets: [{
                        label: 'Cost',
                        data: data.map(r => r.totalCost),
                        backgroundColor: '#0078d4'
                    }]
                },
                options: {
                    indexAxis: 'y'
                }
            });
        }

        // Initialize
        loadSummary();
        loadTrendChart();
        loadServiceChart();
        loadResourceChart();
    </script>
</body>
</html>
```

---

## ✅ Vantagens desta Implementação

1. **Sem custos de licença** - Não precisa de Power BI Pro/Premium
2. **Controle total** - Você define como os dados são exibidos
3. **Melhor para SaaS** - Cada cliente vê apenas seus dados
4. **Performance** - Queries otimizadas no Cosmos DB
5. **Customizável** - Fácil adicionar novos tipos de análise

---

## 🔐 Segurança

Para produção, adicione autenticação JWT nos endpoints:

```csharp
// Adicionar em GetAnalytics.cs
[Function("GetCostAnalytics")]
public async Task<HttpResponseData> GetCostAnalytics(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "analytics/cost")] HttpRequestData req)
{
    // Validar JWT token
    var token = req.Headers.GetValues("Authorization").FirstOrDefault();
    if (!ValidateJwtToken(token))
    {
        return req.CreateResponse(HttpStatusCode.Unauthorized);
    }
    
    // ... resto do código
}
```

---

**Pronto para deploy! 🚀**
