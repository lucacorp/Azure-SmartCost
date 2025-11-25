using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AzureSmartCost.Functions;

public class MarketplaceTest
{
    private readonly ILogger<MarketplaceTest> _logger;

    public MarketplaceTest(ILogger<MarketplaceTest> logger)
    {
        _logger = logger;
    }

    [Function("MarketplaceTest")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "marketplace/test")] HttpRequestData req)
    {
        _logger.LogInformation("Marketplace test endpoint called");

        var config = new
        {
            Status = "OK",
            Endpoints = new
            {
                Landing = "https://smartcost-func-beta.azurewebsites.net/api/marketplace/landing",
                Webhook = "https://smartcost-func-beta.azurewebsites.net/api/marketplace/webhook",
                Test = "https://smartcost-func-beta.azurewebsites.net/api/marketplace/test"
            },
            Configuration = new
            {
                TenantId = Environment.GetEnvironmentVariable("Marketplace__TenantId"),
                ClientId = Environment.GetEnvironmentVariable("Marketplace__ClientId"),
                PublisherId = Environment.GetEnvironmentVariable("Marketplace__PublisherId"),
                HasClientSecret = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Marketplace__ClientSecret"))
            },
            Timestamp = DateTime.UtcNow
        };

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(config);
        
        return response;
    }
}
