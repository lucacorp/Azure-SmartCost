using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Web;

namespace AzureSmartCost.Functions;

public class MarketplaceLanding
{
    private readonly ILogger<MarketplaceLanding> _logger;

    public MarketplaceLanding(ILogger<MarketplaceLanding> logger)
    {
        _logger = logger;
    }

    [Function("MarketplaceLanding")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "marketplace/landing")] HttpRequestData req)
    {
        _logger.LogInformation("Marketplace landing page requested");

        try
        {
            // Get the token from query string
            var query = HttpUtility.ParseQueryString(req.Url.Query);
            var token = query["token"];

            if (string.IsNullOrEmpty(token))
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Missing marketplace token");
                return errorResponse;
            }

            _logger.LogInformation("Received marketplace token: {Token}", token.Substring(0, Math.Min(10, token.Length)) + "...");

            // TODO: Resolve token with Marketplace API
            // TODO: Create/update tenant in Cosmos DB
            // TODO: Activate subscription
            
            // For now, redirect to the dashboard
            var dashboardUrl = Environment.GetEnvironmentVariable("DASHBOARD_URL") 
                ?? "https://blue-flower-0414b9b0f.3.azurestaticapps.net";
            
            var response = req.CreateResponse(HttpStatusCode.Redirect);
            response.Headers.Add("Location", $"{dashboardUrl}?subscription=activated");
            
            _logger.LogInformation("Redirecting to dashboard: {DashboardUrl}", dashboardUrl);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing marketplace landing");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }
}
