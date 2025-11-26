using System;
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

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(analytics);
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

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(breakdown);
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

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(trend);
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

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(topResources);
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
