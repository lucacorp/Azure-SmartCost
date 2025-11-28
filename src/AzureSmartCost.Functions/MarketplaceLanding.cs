using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Web;
using Microsoft.Azure.Cosmos;
using AzureSmartCost.Functions.Services;
using AzureSmartCost.Functions.Models;

namespace AzureSmartCost.Functions;

public class MarketplaceLanding
{
    private readonly ILogger<MarketplaceLanding> _logger;
    private readonly MarketplaceService _marketplaceService;
    private readonly Container _tenantsContainer;

    public MarketplaceLanding(ILogger<MarketplaceLanding> logger)
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

            // 1. Resolve token with Marketplace API
            var subscription = await _marketplaceService.ResolveTokenAsync(token);
            
            if (subscription == null)
            {
                _logger.LogError("Failed to resolve marketplace token");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Invalid marketplace token");
                return errorResponse;
            }

            _logger.LogInformation("Resolved subscription: {SubscriptionId}, Plan: {PlanId}, Status: {Status}", 
                subscription.Id, subscription.PlanId, subscription.SaasSubscriptionStatus);

            // 2. Create/update tenant in Cosmos DB
            var tenant = await CreateOrUpdateTenantAsync(subscription);
            
            // 3. Activate subscription if pending
            if (subscription.SaasSubscriptionStatus == "PendingFulfillmentStart")
            {
                _logger.LogInformation("Activating subscription {SubscriptionId}", subscription.Id);
                var activated = await _marketplaceService.ActivateSubscriptionAsync(
                    subscription.Id!,
                    subscription.PlanId ?? "basic"
                );

                if (activated)
                {
                    tenant.Status = "Subscribed";
                    tenant.ActivatedAt = DateTime.UtcNow;
                    await _tenantsContainer.UpsertItemAsync(tenant, new PartitionKey(tenant.MarketplaceSubscriptionId));
                    
                    _logger.LogInformation("Subscription {SubscriptionId} activated successfully", subscription.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to activate subscription {SubscriptionId}", subscription.Id);
                }
            }
            
            // 4. Redirect to dashboard with subscription info
            var dashboardUrl = Environment.GetEnvironmentVariable("DASHBOARD_URL") 
                ?? "https://blue-flower-0414b9b0f.3.azurestaticapps.net";
            
            var redirectUrl = $"{dashboardUrl}?subscription={tenant.MarketplaceSubscriptionId}&plan={tenant.PlanId}&status=activated";
            
            var response = req.CreateResponse(HttpStatusCode.Redirect);
            response.Headers.Add("Location", redirectUrl);
            
            _logger.LogInformation("Redirecting to dashboard: {Url}", redirectUrl);
            
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

    private async Task<Tenant> CreateOrUpdateTenantAsync(MarketplaceSubscription subscription)
    {
        try
        {
            // Try to get existing tenant
            var tenantId = $"tenant_{subscription.Id}";
            
            try
            {
                var existingResponse = await _tenantsContainer.ReadItemAsync<Tenant>(
                    tenantId, 
                    new PartitionKey(subscription.Id!)
                );
                
                var existingTenant = existingResponse.Resource;
                
                // Update existing tenant
                existingTenant.PlanId = subscription.PlanId ?? existingTenant.PlanId;
                existingTenant.Quantity = subscription.Quantity;
                existingTenant.Status = subscription.SaasSubscriptionStatus ?? existingTenant.Status;
                existingTenant.LastModifiedAt = DateTime.UtcNow;
                existingTenant.Email = subscription.Beneficiary ?? existingTenant.Email;
                
                await _tenantsContainer.UpsertItemAsync(existingTenant, new PartitionKey(existingTenant.MarketplaceSubscriptionId));
                
                _logger.LogInformation("Updated existing tenant {TenantId}", tenantId);
                return existingTenant;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Create new tenant
                var newTenant = new Tenant
                {
                    Id = tenantId,
                    MarketplaceSubscriptionId = subscription.Id!,
                    Name = subscription.SubscriptionName ?? "New Customer",
                    Email = subscription.Beneficiary ?? "",
                    OfferId = subscription.OfferId,
                    PlanId = subscription.PlanId ?? "basic",
                    Quantity = subscription.Quantity,
                    Status = subscription.SaasSubscriptionStatus ?? "PendingFulfillmentStart",
                    CreatedAt = DateTime.UtcNow,
                    LastModifiedAt = DateTime.UtcNow,
                    LicenseKey = Guid.NewGuid().ToString("N")
                };
                
                await _tenantsContainer.CreateItemAsync(newTenant, new PartitionKey(newTenant.MarketplaceSubscriptionId));
                
                _logger.LogInformation("Created new tenant {TenantId}", tenantId);
                return newTenant;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating tenant for subscription {SubscriptionId}", subscription.Id);
            throw;
        }
    }
}
