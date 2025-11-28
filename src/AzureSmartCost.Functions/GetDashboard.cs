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

            // Buscar custos (apenas período atual para evitar rate limit)
            var costService = new CostService(_logger);
            var currentCosts = await costService.GetCostsAsync(subscriptionId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

            // Calcular estatísticas baseadas nos dados diários
            decimal previousTotal = 0;
            if (currentCosts.DailyTrend != null && currentCosts.DailyTrend.Count >= 7)
            {
                // Comparar última semana com semana anterior
                var lastWeek = currentCosts.DailyTrend.TakeLast(7).Sum(d => d.Cost);
                var previousWeek = currentCosts.DailyTrend.Skip(Math.Max(0, currentCosts.DailyTrend.Count - 14)).Take(7).Sum(d => d.Cost);
                previousTotal = previousWeek;
            }

            var totalCost = currentCosts.TotalCost;
            var forecast = totalCost > 0 ? (totalCost / 30) * DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month) : 0;

            // Montar dashboard
            var dashboard = new DashboardData
            {
                SubscriptionId = subscriptionId,
                Period = "Últimos 30 dias",
                Summary = new CostSummary
                {
                    Total = totalCost,
                    Previous = previousTotal,
                    Change = totalCost - previousTotal,
                    ChangePercent = previousTotal > 0 
                        ? ((totalCost - previousTotal) / previousTotal) * 100 
                        : 0,
                    Forecast = forecast,
                    Currency = currentCosts.Currency ?? "BRL"
                },
                TopResources = currentCosts.Resources?.Take(10).ToList() ?? new List<ResourceCost>(),
                CostByService = currentCosts.Resources?
                    .GroupBy(r => r.ResourceType)
                    .Select(g => new ServiceCost
                    {
                        Service = g.Key ?? "Unknown",
                        Cost = g.Sum(r => r.Cost),
                        Percentage = totalCost > 0 ? (g.Sum(r => r.Cost) / totalCost) * 100 : 0,
                        ResourceCount = g.Count()
                    })
                    .OrderByDescending(s => s.Cost)
                    .Take(10)
                    .ToList() ?? new List<ServiceCost>(),
                DailyTrend = currentCosts.DailyTrend ?? new List<DailyCost>(),
                Recommendations = currentCosts.Recommendations ?? new List<string>(),
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
