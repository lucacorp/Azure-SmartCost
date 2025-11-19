using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AzureSmartCost.Functions.Models;

namespace AzureSmartCost.Functions.Services;

public class CostService
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _costsContainer;
    private readonly DefaultAzureCredential _credential;
    private readonly ILogger _logger;
    private static readonly HttpClient _httpClient = new HttpClient();

    public CostService(ILogger logger)
    {
        _logger = logger;
        
        // Cosmos DB
        var cosmosEndpoint = Environment.GetEnvironmentVariable("CosmosDb__Endpoint") 
            ?? throw new InvalidOperationException("CosmosDb__Endpoint não configurado");
        var cosmosKey = Environment.GetEnvironmentVariable("CosmosDb__Key")
            ?? throw new InvalidOperationException("CosmosDb__Key não configurado");
        
        _cosmosClient = new CosmosClient(cosmosEndpoint, cosmosKey);
        _costsContainer = _cosmosClient.GetContainer("SmartCost", "Costs");
        
        // Azure Managed Identity
        _credential = new DefaultAzureCredential();
    }

    public async Task<CostData> GetCostsAsync(string subscriptionId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            // Tentar buscar do cache primeiro
            var cached = await GetFromCacheAsync(subscriptionId);
            if (cached != null && cached.CachedAt > DateTime.UtcNow.AddHours(-1))
            {
                _logger.LogInformation("Retornando custos do cache para subscription {SubscriptionId}", subscriptionId);
                return cached;
            }

            // Cache expirado ou não existe, buscar da API
            _logger.LogInformation("Buscando custos da Azure Cost Management API para subscription {SubscriptionId}", subscriptionId);
            
            var costData = await FetchFromAzureAsync(subscriptionId, startDate, endDate);
            
            // Salvar no cache
            await SaveToCacheAsync(costData);
            
            return costData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar custos para subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    private async Task<CostData?> GetFromCacheAsync(string subscriptionId)
    {
        try
        {
            var id = $"{subscriptionId}_{DateTime.UtcNow:yyyyMMdd}";
            var response = await _costsContainer.ReadItemAsync<CostData>(id, new PartitionKey(subscriptionId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task SaveToCacheAsync(CostData costData)
    {
        try
        {
            await _costsContainer.UpsertItemAsync(costData, new PartitionKey(costData.SubscriptionId));
            _logger.LogInformation("Custos salvos no cache para subscription {SubscriptionId}", costData.SubscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao salvar no cache (não crítico)");
        }
    }

    private async Task<CostData> FetchFromAzureAsync(string subscriptionId, DateTime? startDate, DateTime? endDate)
    {
        var start = (startDate ?? DateTime.UtcNow.AddDays(-30)).ToString("yyyy-MM-dd");
        var end = (endDate ?? DateTime.UtcNow).ToString("yyyy-MM-dd");

        _logger.LogInformation("Buscando custos via Cost Management REST API de {Start} a {End}", start, end);

        try
        {
            // Obter token de acesso
            var tokenRequestContext = new TokenRequestContext(new[] { "https://management.azure.com/.default" });
            var token = await _credential.GetTokenAsync(tokenRequestContext);

            // Montar query para Cost Management API
            var scope = $"/subscriptions/{subscriptionId}";
            var url = $"https://management.azure.com{scope}/providers/Microsoft.CostManagement/query?api-version=2023-11-01";

            var queryBody = new
            {
                type = "ActualCost",
                timeframe = "Custom",
                timePeriod = new
                {
                    from = start,
                    to = end
                },
                dataset = new
                {
                    granularity = "Daily",
                    aggregation = new Dictionary<string, object>
                    {
                        ["totalCost"] = new { name = "PreTaxCost", function = "Sum" }
                    },
                    grouping = new[]
                    {
                        new { type = "Dimension", name = "ResourceId" },
                        new { type = "Dimension", name = "ResourceType" },
                        new { type = "Dimension", name = "ResourceGroupName" }
                    }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            request.Content = new StringContent(JsonConvert.SerializeObject(queryBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro ao chamar Cost Management API: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new Exception($"Cost Management API retornou {response.StatusCode}: {responseContent}");
            }

            // Parse response
            var result = JObject.Parse(responseContent);
            var rows = result["properties"]?["rows"] as JArray;
            var columns = result["properties"]?["columns"] as JArray;

            var costData = new CostData
            {
                Id = $"{subscriptionId}_{DateTime.UtcNow:yyyyMMdd}",
                SubscriptionId = subscriptionId,
                Date = DateTime.UtcNow,
                Currency = "BRL",
                CachedAt = DateTime.UtcNow
            };

            if (rows != null && rows.Count > 0)
            {
                decimal totalCost = 0;
                var resourcesDict = new Dictionary<string, ResourceCost>();

                foreach (var row in rows)
                {
                    var cost = row[0]?.ToObject<decimal>() ?? 0;
                    var resourceId = row[1]?.ToString() ?? "";
                    var resourceType = row[2]?.ToString() ?? "";
                    var resourceGroup = row[3]?.ToString() ?? "";

                    totalCost += cost;

                    if (!string.IsNullOrEmpty(resourceId))
                    {
                        if (!resourcesDict.ContainsKey(resourceId))
                        {
                            resourcesDict[resourceId] = new ResourceCost
                            {
                                ResourceId = resourceId,
                                ResourceName = ExtractResourceName(resourceId),
                                ResourceType = resourceType,
                                Cost = 0,
                                Tags = new Dictionary<string, string>
                                {
                                    ["ResourceGroup"] = resourceGroup
                                }
                            };
                        }
                        resourcesDict[resourceId].Cost += cost;
                    }
                }

                costData.TotalCost = totalCost;
                costData.Resources = resourcesDict.Values.OrderByDescending(r => r.Cost).Take(20).ToList();
                costData.Recommendations = GenerateRecommendations(costData.Resources.ToList(), totalCost);

                _logger.LogInformation("✅ Custos obtidos: R$ {TotalCost:F2} ({ResourceCount} recursos)", totalCost, resourcesDict.Count);
            }
            else
            {
                _logger.LogWarning("Nenhum custo encontrado no período");
                costData.TotalCost = 0;
                costData.Resources = new List<ResourceCost>();
                costData.Recommendations = new List<string> { "Nenhum custo registrado no período selecionado" };
            }

            return costData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar custos via REST API");
            
            // Fallback para dados básicos
            return new CostData
            {
                Id = $"{subscriptionId}_{DateTime.UtcNow:yyyyMMdd}",
                SubscriptionId = subscriptionId,
                Date = DateTime.UtcNow,
                Currency = "BRL",
                CachedAt = DateTime.UtcNow,
                TotalCost = 0,
                Resources = new List<ResourceCost>(),
                Recommendations = new List<string> { $"Erro ao obter custos: {ex.Message}" }
            };
        }
    }

    private string ExtractResourceName(string resourceId)
    {
        if (string.IsNullOrEmpty(resourceId)) return "Unknown";
        
        var parts = resourceId.Split('/');
        return parts.Length > 0 ? parts[^1] : resourceId;
    }

    private List<string> GenerateRecommendations(List<ResourceCost> resources, decimal totalCost)
    {
        var recommendations = new List<string>();
        
        if (resources.Count == 0)
        {
            recommendations.Add("✅ Nenhum recurso com custo no período");
            return recommendations;
        }

        // Top 3 recursos mais caros
        var topExpensive = resources.OrderByDescending(r => r.Cost).Take(3).ToList();
        if (topExpensive.Any())
        {
            recommendations.Add($"💰 Top recursos: {string.Join(", ", topExpensive.Select(r => $"{r.ResourceName} (R$ {r.Cost:F2})"))}");
        }

        // Análise por tipo
        var byType = resources.GroupBy(r => r.ResourceType)
            .Select(g => new { Type = g.Key, Cost = g.Sum(r => r.Cost), Count = g.Count() })
            .OrderByDescending(x => x.Cost)
            .ToList();

        if (byType.Any())
        {
            var topType = byType.First();
            var percentage = (topType.Cost / totalCost) * 100;
            recommendations.Add($"📊 {topType.Type}: {topType.Count} recurso(s), {percentage:F1}% do total");
        }

        // Budget warnings
        if (totalCost > 100)
        {
            recommendations.Add($"⚠️ Custo mensal estimado: R$ {totalCost:F2} - considere revisar recursos não utilizados");
        }
        else if (totalCost > 50)
        {
            recommendations.Add($"💡 Custo sob controle: R$ {totalCost:F2}/mês");
        }
        else
        {
            recommendations.Add($"✅ Custo otimizado: R$ {totalCost:F2}/mês");
        }

        // Recursos sem tags (governance)
        var untagged = resources.Where(r => r.Tags.Count <= 1).Count(); // <= 1 porque ResourceGroup é adicionada
        if (untagged > 0)
        {
            recommendations.Add($"🏷️ {untagged} recurso(s) sem tags - adicione para melhor governança");
        }

        return recommendations;
    }
}
