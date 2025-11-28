using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AzureSmartCost.Functions
{
    public class GetAnalytics
    {
        private readonly ILogger _logger;
        private readonly AnalyticsService _analyticsService;

        public GetAnalytics(ILoggerFactory loggerFactory, AnalyticsService analyticsService)
        {
            _logger = loggerFactory.CreateLogger<GetAnalytics>();
            _analyticsService = analyticsService;
        }

        [Function("GetCostAnalytics")]
        public async Task<HttpResponseData> GetCostAnalytics(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "analytics/cost")] HttpRequestData req)
        {
            _logger.LogInformation("Getting cost analytics");

            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var subscriptionId = query["subscriptionId"];
                var startDateStr = query["startDate"];
                var endDateStr = query["endDate"];

                if (string.IsNullOrEmpty(subscriptionId))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("subscriptionId is required");
                    return badResponse;
                }

                var startDate = string.IsNullOrEmpty(startDateStr) 
                    ? DateTime.UtcNow.AddDays(-30) 
                    : DateTime.Parse(startDateStr);
                
                var endDate = string.IsNullOrEmpty(endDateStr) 
                    ? DateTime.UtcNow 
                    : DateTime.Parse(endDateStr);

                var analytics = await _analyticsService.GetCostAnalytics(subscriptionId, startDate, endDate);
                
                if (analytics == null || analytics.RecordCount == 0)
                {
                    var noDataResponse = req.CreateResponse(HttpStatusCode.OK);
                    await noDataResponse.WriteAsJsonAsync(new 
                    {
                        message = "Dados insuficientes para gerar relatórios de análise de custos. Aguarde a coleta de dados do Azure Cost Management.",
                        hasData = false
                    });
                    return noDataResponse;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    totalCost = analytics.TotalCost,
                    currency = analytics.Currency,
                    period = new { startDate = analytics.StartDate, endDate = analytics.EndDate },
                    dailyAverage = analytics.DailyAverage,
                    trend = analytics.TrendPercentage > 0 ? "increasing" : analytics.TrendPercentage < 0 ? "decreasing" : "stable",
                    percentageChange = Math.Abs(analytics.TrendPercentage),
                    topService = analytics.TopService,
                    hasData = true
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cost analytics");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetServiceBreakdown")]
        public async Task<HttpResponseData> GetServiceBreakdown(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "analytics/services")] HttpRequestData req)
        {
            _logger.LogInformation("Getting service breakdown");

            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var subscriptionId = query["subscriptionId"];
                var startDateStr = query["startDate"];
                var endDateStr = query["endDate"];

                if (string.IsNullOrEmpty(subscriptionId))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("subscriptionId is required");
                    return badResponse;
                }

                var startDate = string.IsNullOrEmpty(startDateStr) 
                    ? DateTime.UtcNow.AddDays(-30) 
                    : DateTime.Parse(startDateStr);
                
                var endDate = string.IsNullOrEmpty(endDateStr) 
                    ? DateTime.UtcNow 
                    : DateTime.Parse(endDateStr);

                var breakdown = await _analyticsService.GetServiceBreakdown(subscriptionId, startDate, endDate);
                
                if (breakdown == null || !breakdown.Any())
                {
                    var noDataResponse = req.CreateResponse(HttpStatusCode.OK);
                    await noDataResponse.WriteAsJsonAsync(new 
                    {
                        message = "Dados insuficientes para gerar relatório de custos por serviço. Aguarde a coleta de dados do Azure Cost Management.",
                        hasData = false,
                        services = new object[] { }
                    });
                    return noDataResponse;
                }

                var total = breakdown.Sum(s => s.TotalCost);
                var result = breakdown.Select(s => new 
                {
                    serviceName = s.ServiceName,
                    totalCost = s.TotalCost,
                    currency = s.Currency,
                    percentage = total > 0 ? (s.TotalCost / total) * 100 : 0,
                    resourceCount = s.ResourceCount
                });

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { hasData = true, services = result });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service breakdown");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetDailyCostTrend")]
        public async Task<HttpResponseData> GetDailyCostTrend(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "analytics/trend")] HttpRequestData req)
        {
            _logger.LogInformation("Getting daily cost trend");

            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var subscriptionId = query["subscriptionId"];
                var startDateStr = query["startDate"];
                var endDateStr = query["endDate"];

                if (string.IsNullOrEmpty(subscriptionId))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("subscriptionId is required");
                    return badResponse;
                }

                var startDate = string.IsNullOrEmpty(startDateStr) 
                    ? DateTime.UtcNow.AddDays(-30) 
                    : DateTime.Parse(startDateStr);
                
                var endDate = string.IsNullOrEmpty(endDateStr) 
                    ? DateTime.UtcNow 
                    : DateTime.Parse(endDateStr);

                var trend = await _analyticsService.GetDailyCostTrend(subscriptionId, startDate, endDate);
                
                if (trend == null || !trend.Any())
                {
                    var noDataResponse = req.CreateResponse(HttpStatusCode.OK);
                    await noDataResponse.WriteAsJsonAsync(new 
                    {
                        message = "Dados insuficientes para gerar relatório de tendência de custos. Aguarde a coleta de dados do Azure Cost Management.",
                        hasData = false,
                        trend = new object[] { }
                    });
                    return noDataResponse;
                }

                var result = trend.Select(t => new 
                {
                    date = t.Date.ToString("yyyy-MM-dd"),
                    cost = t.TotalCost,
                    currency = t.Currency
                });

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { hasData = true, trend = result });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily cost trend");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("GetTopCostResources")]
        public async Task<HttpResponseData> GetTopCostResources(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "analytics/top-resources")] HttpRequestData req)
        {
            _logger.LogInformation("Getting top cost resources");

            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var subscriptionId = query["subscriptionId"];
                var startDateStr = query["startDate"];
                var endDateStr = query["endDate"];
                var topNStr = query["top"];

                if (string.IsNullOrEmpty(subscriptionId))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("subscriptionId is required");
                    return badResponse;
                }

                var startDate = string.IsNullOrEmpty(startDateStr) 
                    ? DateTime.UtcNow.AddDays(-30) 
                    : DateTime.Parse(startDateStr);
                
                var endDate = string.IsNullOrEmpty(endDateStr) 
                    ? DateTime.UtcNow 
                    : DateTime.Parse(endDateStr);

                var topN = string.IsNullOrEmpty(topNStr) ? 10 : int.Parse(topNStr);

                var topResources = await _analyticsService.GetTopCostResources(subscriptionId, startDate, endDate, topN);
                
                if (topResources == null || !topResources.Any())
                {
                    var noDataResponse = req.CreateResponse(HttpStatusCode.OK);
                    await noDataResponse.WriteAsJsonAsync(new 
                    {
                        message = "Dados insuficientes para gerar relatório dos recursos de maior custo. Aguarde a coleta de dados do Azure Cost Management.",
                        hasData = false,
                        resources = new object[] { }
                    });
                    return noDataResponse;
                }

                var result = topResources.Select(r => new 
                {
                    resourceName = r.ResourceName,
                    resourceType = r.ServiceName,
                    cost = r.TotalCost,
                    currency = r.Currency
                });

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { hasData = true, resources = result });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top cost resources");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }
}
