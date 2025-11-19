using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using AzureSmartCost.Functions.Services;
using AzureSmartCost.Functions.Models;
using Newtonsoft.Json;

namespace AzureSmartCost.Functions;

public class GetDashboard
{
    private readonly ILogger _logger;

    public GetDashboard(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GetDashboard>();
    }

    [Function("GetDashboard")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dashboard/{subscriptionId?}")] HttpRequestData req,
        string? subscriptionId = null)
    {
        _logger.LogInformation("GetDashboard endpoint chamado");

        try
        {
            // Obter subscriptionId
            if (string.IsNullOrEmpty(subscriptionId))
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                subscriptionId = query["subscriptionId"] ?? Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            }

            if (string.IsNullOrEmpty(subscriptionId))
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { success = false, message = "subscriptionId é obrigatório" });
                return errorResponse;
            }

            // Buscar custos
            var costService = new CostService(_logger);
            var currentCosts = await costService.GetCostsAsync(subscriptionId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
            var previousCosts = await costService.GetCostsAsync(subscriptionId, DateTime.UtcNow.AddDays(-60), DateTime.UtcNow.AddDays(-30));

            // Montar dashboard
            var dashboard = new DashboardData
            {
                SubscriptionId = subscriptionId,
                Period = "Últimos 30 dias",
                Summary = new CostSummary
                {
                    Total = currentCosts.TotalCost,
                    Previous = previousCosts.TotalCost,
                    Change = currentCosts.TotalCost - previousCosts.TotalCost,
                    ChangePercent = previousCosts.TotalCost > 0 
                        ? ((currentCosts.TotalCost - previousCosts.TotalCost) / previousCosts.TotalCost) * 100 
                        : 0,
                    Forecast = currentCosts.TotalCost * 1.1m, // Estimativa simples: +10%
                    Currency = "BRL"
                },
                TopResources = currentCosts.Resources.Take(10).ToList(),
                CostByService = currentCosts.Resources
                    .GroupBy(r => r.ResourceType)
                    .Select(g => new ServiceCost
                    {
                        Service = g.Key,
                        Cost = g.Sum(r => r.Cost),
                        Percentage = (g.Sum(r => r.Cost) / currentCosts.TotalCost) * 100,
                        ResourceCount = g.Count()
                    })
                    .OrderByDescending(s => s.Cost)
                    .Take(10)
                    .ToList(),
                DailyTrend = new List<DailyCost>(), // TODO: Implementar trend diário
                Recommendations = currentCosts.Recommendations,
                Alerts = new List<BudgetAlert>() // TODO: Buscar do Cosmos
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                data = dashboard
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar dashboard");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                success = false,
                message = "Erro ao gerar dashboard: " + ex.Message
            });

            return errorResponse;
        }
    }
}
