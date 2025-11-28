using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using AzureSmartCost.Functions.Services;
using AzureSmartCost.Functions.Models;

namespace AzureSmartCost.Functions;

public class MarketplaceWebhook
{
    private readonly ILogger<MarketplaceWebhook> _logger;
    private readonly MarketplaceService _marketplaceService;
    private readonly Container _tenantsContainer;

    public MarketplaceWebhook(ILogger<MarketplaceWebhook> logger)
    {
        _logger = logger;
        _marketplaceService = new MarketplaceService(logger);
        
        // Initialize Cosmos DB
        var cosmosEndpoint = Environment.GetEnvironmentVariable("CosmosDb__Endpoint") 
            ?? throw new InvalidOperationException("CosmosDb__Endpoint not configured");
        var cosmosKey = Environment.GetEnvironmentVariable("CosmosDb__Key")
            ?? throw new InvalidOperationException("CosmosDb__Key not configured");
        
        var cosmosClient = new CosmosClient(cosmosEndpoint, cosmosKey);
        _tenantsContainer = cosmosClient.GetContainer("SmartCost", "Tenants");
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
            
            if (webhookEvent != null && !string.IsNullOrEmpty(webhookEvent.SubscriptionId))
            {
                _logger.LogInformation("Processing webhook - Action: {Action}, Subscription: {SubscriptionId}, Plan: {PlanId}", 
                    webhookEvent.Action, webhookEvent.SubscriptionId, webhookEvent.PlanId);

                // Process webhook action
                await ProcessWebhookActionAsync(webhookEvent);
            }
            else
            {
                _logger.LogWarning("Invalid webhook payload received");
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

    private async Task ProcessWebhookActionAsync(MarketplaceWebhookEvent webhookEvent)
    {
        try
        {
            var tenantId = $"tenant_{webhookEvent.SubscriptionId}";

            switch (webhookEvent.Action?.ToLower())
            {
                case "subscribe":
                    _logger.LogInformation("Processing Subscribe event for {SubscriptionId}", webhookEvent.SubscriptionId);
                    // Subscription already created in landing page
                    // Just ensure it's activated
                    await EnsureSubscriptionActivatedAsync(webhookEvent.SubscriptionId!);
                    break;

                case "unsubscribe":
                    _logger.LogInformation("Processing Unsubscribe event for {SubscriptionId}", webhookEvent.SubscriptionId);
                    await CancelSubscriptionAsync(tenantId, webhookEvent.SubscriptionId!);
                    break;

                case "changeplan":
                    _logger.LogInformation("Processing ChangePlan event for {SubscriptionId} to plan {PlanId}", 
                        webhookEvent.SubscriptionId, webhookEvent.PlanId);
                    await ChangePlanAsync(tenantId, webhookEvent.SubscriptionId!, webhookEvent.PlanId!);
                    break;

                case "changequantity":
                    _logger.LogInformation("Processing ChangeQuantity event for {SubscriptionId} to quantity {Quantity}", 
                        webhookEvent.SubscriptionId, webhookEvent.Quantity);
                    await ChangeQuantityAsync(tenantId, webhookEvent.SubscriptionId!, webhookEvent.Quantity);
                    break;

                case "suspend":
                    _logger.LogInformation("Processing Suspend event for {SubscriptionId}", webhookEvent.SubscriptionId);
                    await SuspendSubscriptionAsync(tenantId, webhookEvent.SubscriptionId!);
                    break;

                case "reinstate":
                    _logger.LogInformation("Processing Reinstate event for {SubscriptionId}", webhookEvent.SubscriptionId);
                    await ReinstateSubscriptionAsync(tenantId, webhookEvent.SubscriptionId!);
                    break;

                default:
                    _logger.LogWarning("Unknown webhook action: {Action}", webhookEvent.Action);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook action {Action} for subscription {SubscriptionId}", 
                webhookEvent.Action, webhookEvent.SubscriptionId);
        }
    }

    private async Task EnsureSubscriptionActivatedAsync(string subscriptionId)
    {
        try
        {
            var subscription = await _marketplaceService.GetSubscriptionAsync(subscriptionId);
            
            if (subscription?.SaasSubscriptionStatus == "PendingFulfillmentStart")
            {
                await _marketplaceService.ActivateSubscriptionAsync(subscriptionId, subscription.PlanId!);
            }

            await UpdateTenantStatusAsync($"tenant_{subscriptionId}", subscriptionId, "Subscribed", tenant =>
            {
                tenant.ActivatedAt = DateTime.UtcNow;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring subscription activated: {SubscriptionId}", subscriptionId);
        }
    }

    private async Task CancelSubscriptionAsync(string tenantId, string subscriptionId)
    {
        await UpdateTenantStatusAsync(tenantId, subscriptionId, "Unsubscribed", tenant =>
        {
            tenant.CancelledAt = DateTime.UtcNow;
        });
    }

    private async Task SuspendSubscriptionAsync(string tenantId, string subscriptionId)
    {
        await UpdateTenantStatusAsync(tenantId, subscriptionId, "Suspended", tenant =>
        {
            tenant.SuspendedAt = DateTime.UtcNow;
        });
    }

    private async Task ReinstateSubscriptionAsync(string tenantId, string subscriptionId)
    {
        await UpdateTenantStatusAsync(tenantId, subscriptionId, "Subscribed", tenant =>
        {
            tenant.SuspendedAt = null;
        });
    }

    private async Task ChangePlanAsync(string tenantId, string subscriptionId, string newPlanId)
    {
        await UpdateTenantAsync(tenantId, subscriptionId, tenant =>
        {
            tenant.PlanId = newPlanId;
        });
    }

    private async Task ChangeQuantityAsync(string tenantId, string subscriptionId, int newQuantity)
    {
        await UpdateTenantAsync(tenantId, subscriptionId, tenant =>
        {
            tenant.Quantity = newQuantity;
        });
    }

    private async Task UpdateTenantStatusAsync(string tenantId, string subscriptionId, string status, Action<Tenant>? additionalUpdates = null)
    {
        await UpdateTenantAsync(tenantId, subscriptionId, tenant =>
        {
            tenant.Status = status;
            additionalUpdates?.Invoke(tenant);
        });
    }

    private async Task UpdateTenantAsync(string tenantId, string subscriptionId, Action<Tenant> updates)
    {
        try
        {
            var response = await _tenantsContainer.ReadItemAsync<Tenant>(tenantId, new PartitionKey(subscriptionId));
            var tenant = response.Resource;

            updates(tenant);
            tenant.LastModifiedAt = DateTime.UtcNow;

            await _tenantsContainer.UpsertItemAsync(tenant, new PartitionKey(tenant.MarketplaceSubscriptionId));
            
            _logger.LogInformation("Updated tenant {TenantId}", tenantId);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Tenant not found: {TenantId}. Creating placeholder.", tenantId);
            
            // Create placeholder tenant if it doesn't exist
            var subscription = await _marketplaceService.GetSubscriptionAsync(subscriptionId);
            if (subscription != null)
            {
                var tenant = new Tenant
                {
                    Id = tenantId,
                    MarketplaceSubscriptionId = subscriptionId,
                    Name = subscription.SubscriptionName ?? "Unknown",
                    Email = "",
                    PlanId = subscription.PlanId ?? "basic",
                    Quantity = subscription.Quantity,
                    Status = subscription.SaasSubscriptionStatus ?? "Unknown",
                    CreatedAt = DateTime.UtcNow,
                    LastModifiedAt = DateTime.UtcNow,
                    LicenseKey = Guid.NewGuid().ToString("N")
                };

                updates(tenant);
                
                await _tenantsContainer.CreateItemAsync(tenant, new PartitionKey(tenant.MarketplaceSubscriptionId));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", tenantId);
        }
    }
}

/// <summary>
/// Marketplace webhook event model
/// </summary>
public class MarketplaceWebhookEvent
{
    public string? Action { get; set; }
    public string? SubscriptionId { get; set; }
    public string? PlanId { get; set; }
    public int Quantity { get; set; }
    public DateTime TimeStamp { get; set; }
}
