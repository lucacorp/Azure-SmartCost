using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace AzureSmartCost.Functions.Services;

/// <summary>
/// Service for Azure Marketplace SaaS Fulfillment API v2
/// https://docs.microsoft.com/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2
/// </summary>
public class MarketplaceService
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly DefaultAzureCredential _credential;
    private const string MarketplaceApiBaseUrl = "https://marketplaceapi.microsoft.com/api/saas";
    private const string MarketplaceApiVersion = "2018-08-31";

    public MarketplaceService(ILogger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _credential = new DefaultAzureCredential();
    }

    /// <summary>
    /// Resolve marketplace purchase token to get subscription details
    /// </summary>
    public async Task<MarketplaceSubscription?> ResolveTokenAsync(string token)
    {
        try
        {
            _logger.LogInformation("Resolving marketplace token");

            var accessToken = await GetMarketplaceAccessTokenAsync();
            var url = $"{MarketplaceApiBaseUrl}/subscriptions/resolve?api-version={MarketplaceApiVersion}";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("x-ms-marketplace-token", token);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to resolve token: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var subscription = JsonSerializer.Deserialize<MarketplaceSubscription>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            _logger.LogInformation("Token resolved - Subscription ID: {SubscriptionId}, Name: {SubscriptionName}", 
                subscription?.Id, subscription?.SubscriptionName);

            return subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving marketplace token");
            return null;
        }
    }

    /// <summary>
    /// Activate a marketplace subscription
    /// </summary>
    public async Task<bool> ActivateSubscriptionAsync(string subscriptionId, string planId)
    {
        try
        {
            _logger.LogInformation("Activating subscription {SubscriptionId} with plan {PlanId}", subscriptionId, planId);

            var accessToken = await GetMarketplaceAccessTokenAsync();
            var url = $"{MarketplaceApiBaseUrl}/subscriptions/{subscriptionId}/activate?api-version={MarketplaceApiVersion}";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var payload = new { planId, quantity = 1 };
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload), 
                Encoding.UTF8, 
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to activate subscription: {StatusCode} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Subscription {SubscriptionId} activated successfully", subscriptionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating subscription {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    /// <summary>
    /// Get subscription details
    /// </summary>
    public async Task<MarketplaceSubscription?> GetSubscriptionAsync(string subscriptionId)
    {
        try
        {
            _logger.LogInformation("Getting subscription {SubscriptionId}", subscriptionId);

            var accessToken = await GetMarketplaceAccessTokenAsync();
            var url = $"{MarketplaceApiBaseUrl}/subscriptions/{subscriptionId}?api-version={MarketplaceApiVersion}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get subscription: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var subscription = JsonSerializer.Deserialize<MarketplaceSubscription>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            return subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription {SubscriptionId}", subscriptionId);
            return null;
        }
    }

    /// <summary>
    /// List all marketplace subscriptions
    /// </summary>
    public async Task<List<MarketplaceSubscription>> ListSubscriptionsAsync()
    {
        try
        {
            _logger.LogInformation("Listing all marketplace subscriptions");

            var accessToken = await GetMarketplaceAccessTokenAsync();
            var url = $"{MarketplaceApiBaseUrl}/subscriptions?api-version={MarketplaceApiVersion}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to list subscriptions: {StatusCode} - {Error}", response.StatusCode, error);
                return new List<MarketplaceSubscription>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MarketplaceSubscriptionsResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            return result?.Subscriptions ?? new List<MarketplaceSubscription>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing subscriptions");
            return new List<MarketplaceSubscription>();
        }
    }

    /// <summary>
    /// Update subscription (change plan or quantity)
    /// </summary>
    public async Task<bool> UpdateSubscriptionAsync(string subscriptionId, string? newPlanId = null, int? newQuantity = null)
    {
        try
        {
            _logger.LogInformation("Updating subscription {SubscriptionId}", subscriptionId);

            var accessToken = await GetMarketplaceAccessTokenAsync();
            var url = $"{MarketplaceApiBaseUrl}/subscriptions/{subscriptionId}?api-version={MarketplaceApiVersion}";

            var request = new HttpRequestMessage(HttpMethod.Patch, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var payload = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(newPlanId)) payload["planId"] = newPlanId;
            if (newQuantity.HasValue) payload["quantity"] = newQuantity.Value;

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload), 
                Encoding.UTF8, 
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update subscription: {StatusCode} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Subscription {SubscriptionId} updated successfully", subscriptionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    /// <summary>
    /// Cancel/Delete a marketplace subscription
    /// </summary>
    public async Task<bool> DeleteSubscriptionAsync(string subscriptionId)
    {
        try
        {
            _logger.LogInformation("Deleting subscription {SubscriptionId}", subscriptionId);

            var accessToken = await GetMarketplaceAccessTokenAsync();
            var url = $"{MarketplaceApiBaseUrl}/subscriptions/{subscriptionId}?api-version={MarketplaceApiVersion}";

            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete subscription: {StatusCode} - {Error}", response.StatusCode, error);
                return false;
            }

            _logger.LogInformation("Subscription {SubscriptionId} deleted successfully", subscriptionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    /// <summary>
    /// Get Azure AD access token for Marketplace API
    /// </summary>
    private async Task<string> GetMarketplaceAccessTokenAsync()
    {
        try
        {
            // Marketplace API requires specific resource scope
            var tokenRequestContext = new TokenRequestContext(new[] { "20e940b3-4c77-4b0b-9a53-9e16a1b010a7/.default" });
            var token = await _credential.GetTokenAsync(tokenRequestContext);
            return token.Token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting marketplace access token");
            throw;
        }
    }
}

/// <summary>
/// Marketplace subscription model
/// </summary>
public class MarketplaceSubscription
{
    public string? Id { get; set; }
    public string? SubscriptionName { get; set; }
    public string? OfferId { get; set; }
    public string? PlanId { get; set; }
    public int Quantity { get; set; }
    public string? Beneficiary { get; set; }
    public string? Purchaser { get; set; }
    public string? SaasSubscriptionStatus { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? LastModified { get; set; }
    public MarketplaceTerm? Term { get; set; }
}

public class MarketplaceTerm
{
    public string? TermUnit { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class MarketplaceSubscriptionsResponse
{
    public List<MarketplaceSubscription>? Subscriptions { get; set; }
    public string? NextLink { get; set; }
}
