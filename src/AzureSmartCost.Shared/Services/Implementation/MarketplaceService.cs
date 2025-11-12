using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzureSmartCost.Shared.Interfaces;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Shared.Services.Implementation;

public class MarketplaceService : IMarketplaceService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MarketplaceService> _logger;
    private readonly ITenantService _tenantService;
    private readonly Container _container;
    private readonly HttpClient _httpClient;
    private readonly string _marketplaceApiVersion = "2018-08-31";
    private readonly string _marketplaceBaseUrl = "https://marketplaceapi.microsoft.com/api";

    public MarketplaceService(
        IConfiguration configuration,
        ILogger<MarketplaceService> logger,
        ITenantService tenantService,
        CosmosClient cosmosClient,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _tenantService = tenantService;
        _httpClient = httpClient;
        _container = cosmosClient.GetContainer("SmartCostDB", "MarketplaceSubscriptions");
    }

    public async Task<ResolvedSubscription> ResolveSubscriptionAsync(string token)
    {
        try
        {
            var accessToken = await GetMarketplaceAccessTokenAsync();
            
            var request = new HttpRequestMessage(HttpMethod.Post, 
                $"{_marketplaceBaseUrl}/saas/subscriptions/resolve?api-version={_marketplaceApiVersion}");
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("x-ms-marketplace-token", token);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var resolved = JsonSerializer.Deserialize<ResolvedSubscription>(content);

            if (resolved == null)
            {
                throw new InvalidOperationException("Failed to deserialize resolved subscription");
            }

            _logger.LogInformation("Resolved marketplace subscription {SubscriptionId} for offer {OfferId}", 
                resolved.Id, resolved.OfferId);

            return resolved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving marketplace token");
            throw;
        }
    }

    public async Task<bool> ActivateSubscriptionAsync(string subscriptionId, string planId, int quantity)
    {
        try
        {
            var accessToken = await GetMarketplaceAccessTokenAsync();
            
            var request = new HttpRequestMessage(HttpMethod.Post, 
                $"{_marketplaceBaseUrl}/saas/subscriptions/{subscriptionId}/activate?api-version={_marketplaceApiVersion}");
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var activation = new ActivationRequest
            {
                PlanId = planId,
                Quantity = quantity
            };
            
            var json = JsonSerializer.Serialize(activation);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Activated marketplace subscription {SubscriptionId} with plan {PlanId}", 
                    subscriptionId, planId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to activate subscription {SubscriptionId}: {Error}", 
                    subscriptionId, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating marketplace subscription {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    public async Task<bool> UpdateOperationStatusAsync(
        string subscriptionId, 
        string operationId, 
        string status, 
        string? planId = null, 
        int? quantity = null)
    {
        try
        {
            var accessToken = await GetMarketplaceAccessTokenAsync();
            
            var request = new HttpRequestMessage(HttpMethod.Patch, 
                $"{_marketplaceBaseUrl}/saas/subscriptions/{subscriptionId}/operations/{operationId}?api-version={_marketplaceApiVersion}");
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var update = new OperationStatusUpdate
            {
                Status = status,
                PlanId = planId,
                Quantity = quantity
            };
            
            var json = JsonSerializer.Serialize(update);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Updated operation {OperationId} status to {Status}", operationId, status);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update operation {OperationId}: {Error}", operationId, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating operation status {OperationId}", operationId);
            return false;
        }
    }

    public async Task ProcessWebhookEventAsync(MarketplaceWebhookEvent webhookEvent)
    {
        _logger.LogInformation("Processing marketplace webhook event {EventId} - Action: {Action}", 
            webhookEvent.EventId, webhookEvent.Action);

        try
        {
            switch (webhookEvent.Action)
            {
                case "Subscribe":
                    await HandleSubscribeAsync(webhookEvent);
                    break;

                case "Unsubscribe":
                    await HandleUnsubscribeAsync(webhookEvent);
                    break;

                case "ChangePlan":
                    await HandleChangePlanAsync(webhookEvent);
                    break;

                case "ChangeQuantity":
                    await HandleChangeQuantityAsync(webhookEvent);
                    break;

                case "Suspend":
                    await HandleSuspendAsync(webhookEvent);
                    break;

                case "Reinstate":
                    await HandleReinstateAsync(webhookEvent);
                    break;

                default:
                    _logger.LogWarning("Unknown webhook action: {Action}", webhookEvent.Action);
                    break;
            }

            webhookEvent.Processed = true;
            webhookEvent.ProcessedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event {EventId}", webhookEvent.EventId);
            webhookEvent.ProcessingError = ex.Message;
            throw;
        }
    }

    public async Task<SubscriptionDetails?> GetSubscriptionAsync(string subscriptionId)
    {
        try
        {
            var accessToken = await GetMarketplaceAccessTokenAsync();
            
            var request = new HttpRequestMessage(HttpMethod.Get, 
                $"{_marketplaceBaseUrl}/saas/subscriptions/{subscriptionId}?api-version={_marketplaceApiVersion}");
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SubscriptionDetails>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting marketplace subscription {SubscriptionId}", subscriptionId);
            return null;
        }
    }

    public async Task<List<SubscriptionDetails>> ListSubscriptionsAsync()
    {
        try
        {
            var accessToken = await GetMarketplaceAccessTokenAsync();
            
            var request = new HttpRequestMessage(HttpMethod.Get, 
                $"{_marketplaceBaseUrl}/saas/subscriptions?api-version={_marketplaceApiVersion}");
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var subscriptions = JsonSerializer.Deserialize<List<SubscriptionDetails>>(content);

            return subscriptions ?? new List<SubscriptionDetails>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing marketplace subscriptions");
            return new List<SubscriptionDetails>();
        }
    }

    public async Task<string> GetMarketplaceAccessTokenAsync()
    {
        try
        {
            var tenantId = _configuration["Marketplace:TenantId"] 
                ?? throw new InvalidOperationException("Marketplace:TenantId not configured");
            var clientId = _configuration["Marketplace:ClientId"] 
                ?? throw new InvalidOperationException("Marketplace:ClientId not configured");
            var clientSecret = _configuration["Marketplace:ClientSecret"] 
                ?? throw new InvalidOperationException("Marketplace:ClientSecret not configured");

            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var tokenRequestContext = new TokenRequestContext(new[] { "20e940b3-4c77-4b0b-9a53-9e16a1b010a7/.default" }); // Azure Marketplace resource ID
            
            var token = await credential.GetTokenAsync(tokenRequestContext);
            return token.Token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting marketplace access token");
            throw;
        }
    }

    public async Task<MarketplaceSubscription> SaveMarketplaceSubscriptionAsync(MarketplaceSubscription subscription)
    {
        subscription.Id = subscription.MarketplaceSubscriptionId;
        
        var response = await _container.UpsertItemAsync(
            subscription, 
            new PartitionKey(subscription.MarketplaceSubscriptionId));
        
        _logger.LogInformation("Saved marketplace subscription {SubscriptionId}", subscription.MarketplaceSubscriptionId);
        
        return response.Resource;
    }

    public async Task<MarketplaceSubscription?> GetMarketplaceSubscriptionByIdAsync(string marketplaceSubscriptionId)
    {
        try
        {
            var response = await _container.ReadItemAsync<MarketplaceSubscription>(
                marketplaceSubscriptionId, 
                new PartitionKey(marketplaceSubscriptionId));
            
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<MarketplaceSubscription?> GetMarketplaceSubscriptionByTenantIdAsync(string tenantId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.TenantId = @tenantId")
            .WithParameter("@tenantId", tenantId);

        var iterator = _container.GetItemQueryIterator<MarketplaceSubscription>(query);
        var results = new List<MarketplaceSubscription>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results.FirstOrDefault();
    }

    // Private helper methods for webhook handling

    private async Task HandleSubscribeAsync(MarketplaceWebhookEvent webhookEvent)
    {
        _logger.LogInformation("Handling Subscribe event for subscription {SubscriptionId}", 
            webhookEvent.SubscriptionId);

        // Subscription já foi criada via landing page
        // Este evento confirma que usuário completou purchase no Azure Portal
        var subscription = await GetMarketplaceSubscriptionByIdAsync(webhookEvent.SubscriptionId);
        
        if (subscription != null)
        {
            subscription.Status = "Subscribed";
            await SaveMarketplaceSubscriptionAsync(subscription);
        }
    }

    private async Task HandleUnsubscribeAsync(MarketplaceWebhookEvent webhookEvent)
    {
        _logger.LogInformation("Handling Unsubscribe event for subscription {SubscriptionId}", 
            webhookEvent.SubscriptionId);

        var subscription = await GetMarketplaceSubscriptionByIdAsync(webhookEvent.SubscriptionId);
        
        if (subscription != null)
        {
            subscription.Status = "Unsubscribed";
            subscription.UnsubscribedAt = DateTime.UtcNow;
            await SaveMarketplaceSubscriptionAsync(subscription);

            // Cancel tenant subscription
            if (!string.IsNullOrEmpty(subscription.TenantId))
            {
                await _tenantService.CancelSubscriptionAsync(subscription.TenantId);
            }
        }
    }

    private async Task HandleChangePlanAsync(MarketplaceWebhookEvent webhookEvent)
    {
        _logger.LogInformation("Handling ChangePlan event for subscription {SubscriptionId} to plan {PlanId}", 
            webhookEvent.SubscriptionId, webhookEvent.PlanId);

        var subscription = await GetMarketplaceSubscriptionByIdAsync(webhookEvent.SubscriptionId);
        
        if (subscription != null && !string.IsNullOrEmpty(webhookEvent.PlanId))
        {
            var oldPlan = subscription.PlanId;
            subscription.PlanId = webhookEvent.PlanId;
            await SaveMarketplaceSubscriptionAsync(subscription);

            // Update tenant tier
            if (!string.IsNullOrEmpty(subscription.TenantId))
            {
                var tier = MapPlanToTier(webhookEvent.PlanId);
                
                if (string.Compare(tier, MapPlanToTier(oldPlan), StringComparison.OrdinalIgnoreCase) > 0)
                {
                    await _tenantService.UpgradeTenantAsync(subscription.TenantId, tier);
                }
                else
                {
                    await _tenantService.DowngradeTenantAsync(subscription.TenantId, tier);
                }
            }
        }
    }

    private async Task HandleChangeQuantityAsync(MarketplaceWebhookEvent webhookEvent)
    {
        _logger.LogInformation("Handling ChangeQuantity event for subscription {SubscriptionId} to quantity {Quantity}", 
            webhookEvent.SubscriptionId, webhookEvent.Quantity);

        var subscription = await GetMarketplaceSubscriptionByIdAsync(webhookEvent.SubscriptionId);
        
        if (subscription != null && webhookEvent.Quantity.HasValue)
        {
            subscription.Quantity = webhookEvent.Quantity.Value;
            await SaveMarketplaceSubscriptionAsync(subscription);
        }
    }

    private async Task HandleSuspendAsync(MarketplaceWebhookEvent webhookEvent)
    {
        _logger.LogWarning("Handling Suspend event for subscription {SubscriptionId}", 
            webhookEvent.SubscriptionId);

        var subscription = await GetMarketplaceSubscriptionByIdAsync(webhookEvent.SubscriptionId);
        
        if (subscription != null)
        {
            subscription.Status = "Suspended";
            subscription.SuspendedAt = DateTime.UtcNow;
            await SaveMarketplaceSubscriptionAsync(subscription);

            // Suspend tenant
            if (!string.IsNullOrEmpty(subscription.TenantId))
            {
                var tenant = await _tenantService.GetTenantByIdAsync(subscription.TenantId);
                if (tenant != null)
                {
                    tenant.IsActive = false;
                    await _tenantService.UpdateTenantAsync(tenant);
                }
            }
        }
    }

    private async Task HandleReinstateAsync(MarketplaceWebhookEvent webhookEvent)
    {
        _logger.LogInformation("Handling Reinstate event for subscription {SubscriptionId}", 
            webhookEvent.SubscriptionId);

        var subscription = await GetMarketplaceSubscriptionByIdAsync(webhookEvent.SubscriptionId);
        
        if (subscription != null)
        {
            subscription.Status = "Subscribed";
            subscription.SuspendedAt = null;
            await SaveMarketplaceSubscriptionAsync(subscription);

            // Reactivate tenant
            if (!string.IsNullOrEmpty(subscription.TenantId))
            {
                await _tenantService.ReactivateSubscriptionAsync(subscription.TenantId);
            }
        }
    }

    private string MapPlanToTier(string planId)
    {
        return planId.ToLower() switch
        {
            "free" => "Free",
            "pro" or "professional" => "Pro",
            "enterprise" => "Enterprise",
            _ => "Free"
        };
    }
}
