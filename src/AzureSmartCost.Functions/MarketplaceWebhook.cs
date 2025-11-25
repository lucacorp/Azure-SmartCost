using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AzureSmartCost.Functions;

public class MarketplaceWebhook
{
    private readonly ILogger<MarketplaceWebhook> _logger;

    public MarketplaceWebhook(ILogger<MarketplaceWebhook> logger)
    {
        _logger = logger;
    }

    [Function("MarketplaceWebhook")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "marketplace/webhook")] HttpRequestData req)
    {
        _logger.LogInformation("Marketplace webhook received");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation("Webhook payload: {Payload}", requestBody);

            // Parse the webhook event
            var webhookEvent = JsonSerializer.Deserialize<MarketplaceWebhookEvent>(requestBody);
            
            if (webhookEvent != null)
            {
                _logger.LogInformation("Webhook event - Action: {Action}, Subscription: {SubscriptionId}", 
                    webhookEvent.Action, webhookEvent.SubscriptionId);

                // TODO: Process different webhook actions
                // - Subscribe
                // - Unsubscribe
                // - ChangePlan
                // - ChangeQuantity
                // - Suspend
                // - Reinstate
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Webhook processed");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing marketplace webhook");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }
}

public class MarketplaceWebhookEvent
{
    public string? Action { get; set; }
    public string? SubscriptionId { get; set; }
    public string? PlanId { get; set; }
    public int Quantity { get; set; }
    public DateTime TimeStamp { get; set; }
}
