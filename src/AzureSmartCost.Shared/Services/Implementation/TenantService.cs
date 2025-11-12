using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureSmartCost.Shared.Interfaces;
using AzureSmartCost.Shared.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace AzureSmartCost.Shared.Services.Implementation;

public class TenantService : ITenantService
{
    private readonly Container _container;
    private readonly Container _usersContainer;
    private readonly ILogger<TenantService> _logger;
    private readonly ICacheService? _cacheService;

    public TenantService(
        CosmosClient cosmosClient, 
        ILogger<TenantService> logger,
        ICacheService? cacheService = null)
    {
        _container = cosmosClient.GetContainer("SmartCostDB", "Tenants");
        _usersContainer = cosmosClient.GetContainer("SmartCostDB", "TenantUsers");
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Tenant?> GetTenantByIdAsync(string tenantId)
    {
        // Try cache first if available
        if (_cacheService != null)
        {
            var cacheKey = CacheKeys.Tenant(tenantId);
            return await _cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    try
                    {
                        var response = await _container.ReadItemAsync<Tenant>(tenantId, new PartitionKey(tenantId));
                        return response.Resource;
                    }
                    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return null;
                    }
                },
                TimeSpan.FromMinutes(15)
            );
        }

        // Fallback to direct database query if cache not available
        try
        {
            var response = await _container.ReadItemAsync<Tenant>(tenantId, new PartitionKey(tenantId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Tenant?> GetTenantAsync(string tenantId)
    {
        // Alias for GetTenantByIdAsync
        return await GetTenantByIdAsync(tenantId);
    }

    public async Task<Tenant?> GetTenantByDomainAsync(string domain)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.Domain = @domain")
            .WithParameter("@domain", domain);

        var iterator = _container.GetItemQueryIterator<Tenant>(query);
        var results = new List<Tenant>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results.FirstOrDefault();
    }

    public async Task<Tenant> CreateTenantAsync(Tenant tenant)
    {
        tenant.Id = Guid.NewGuid().ToString();
        tenant.CreatedAt = DateTime.UtcNow;
        tenant.UpdatedAt = DateTime.UtcNow;
        
        // Set trial period (14 days)
        tenant.IsTrialActive = true;
        tenant.TrialEndDate = DateTime.UtcNow.AddDays(14);
        
        // Set default limits for Free tier
        SetTierLimits(tenant, "Free");

        var response = await _container.CreateItemAsync(tenant, new PartitionKey(tenant.Id));
        _logger.LogInformation("Created tenant {TenantId} - {CompanyName}", tenant.Id, tenant.CompanyName);
        
        return response.Resource;
    }

    public async Task<Tenant> UpdateTenantAsync(Tenant tenant)
    {
        tenant.UpdatedAt = DateTime.UtcNow;
        var response = await _container.ReplaceItemAsync(tenant, tenant.Id, new PartitionKey(tenant.Id));
        _logger.LogInformation("Updated tenant {TenantId}", tenant.Id);
        
        // Invalidate cache
        if (_cacheService != null)
        {
            await _cacheService.RemoveAsync(CacheKeys.Tenant(tenant.Id));
        }
        
        return response.Resource;
    }

    public async Task<bool> DeleteTenantAsync(string tenantId)
    {
        try
        {
            await _container.DeleteItemAsync<Tenant>(tenantId, new PartitionKey(tenantId));
            _logger.LogInformation("Deleted tenant {TenantId}", tenantId);
            
            // Invalidate all cache for this tenant
            if (_cacheService != null)
            {
                await _cacheService.InvalidatePatternAsync(CacheKeys.TenantPattern(tenantId));
            }
            
            return true;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Error deleting tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<List<Tenant>> GetAllTenantsAsync()
    {
        var query = new QueryDefinition("SELECT * FROM c");
        var iterator = _container.GetItemQueryIterator<Tenant>(query);
        var results = new List<Tenant>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<bool> UpgradeTenantAsync(string tenantId, string newTier)
    {
        var tenant = await GetTenantByIdAsync(tenantId);
        if (tenant == null) return false;

        tenant.SubscriptionTier = newTier;
        SetTierLimits(tenant, newTier);
        tenant.SubscriptionStartDate = DateTime.UtcNow;
        
        await UpdateTenantAsync(tenant);
        _logger.LogInformation("Upgraded tenant {TenantId} to {Tier}", tenantId, newTier);
        return true;
    }

    public async Task<bool> DowngradeTenantAsync(string tenantId, string newTier)
    {
        var tenant = await GetTenantByIdAsync(tenantId);
        if (tenant == null) return false;

        tenant.SubscriptionTier = newTier;
        SetTierLimits(tenant, newTier);
        
        await UpdateTenantAsync(tenant);
        _logger.LogInformation("Downgraded tenant {TenantId} to {Tier}", tenantId, newTier);
        return true;
    }

    public async Task<bool> CancelSubscriptionAsync(string tenantId)
    {
        var tenant = await GetTenantByIdAsync(tenantId);
        if (tenant == null) return false;

        tenant.IsActive = false;
        tenant.SubscriptionEndDate = DateTime.UtcNow;
        
        await UpdateTenantAsync(tenant);
        _logger.LogWarning("Cancelled subscription for tenant {TenantId}", tenantId);
        return true;
    }

    public async Task<bool> ReactivateSubscriptionAsync(string tenantId)
    {
        var tenant = await GetTenantByIdAsync(tenantId);
        if (tenant == null) return false;

        tenant.IsActive = true;
        tenant.SubscriptionEndDate = null;
        
        await UpdateTenantAsync(tenant);
        _logger.LogInformation("Reactivated subscription for tenant {TenantId}", tenantId);
        return true;
    }

    public async Task IncrementApiCallCountAsync(string tenantId)
    {
        var tenant = await GetTenantByIdAsync(tenantId);
        if (tenant == null) return;

        // Reset counter if it's a new month
        if (tenant.LastApiCallReset.Month != DateTime.UtcNow.Month)
        {
            tenant.CurrentMonthApiCalls = 0;
            tenant.LastApiCallReset = DateTime.UtcNow;
        }

        tenant.CurrentMonthApiCalls++;
        await UpdateTenantAsync(tenant);
    }

    public async Task<bool> HasReachedApiLimitAsync(string tenantId)
    {
        var tenant = await GetTenantByIdAsync(tenantId);
        if (tenant == null) return true;

        // Unlimited for Enterprise
        if (tenant.SubscriptionTier == "Enterprise") return false;

        return tenant.CurrentMonthApiCalls >= tenant.MaxMonthlyApiCalls;
    }

    public async Task<bool> CanAddUserAsync(string tenantId)
    {
        var tenant = await GetTenantByIdAsync(tenantId);
        if (tenant == null) return false;

        // Unlimited for Enterprise
        if (tenant.SubscriptionTier == "Enterprise") return true;

        return tenant.CurrentUserCount < tenant.MaxUsers;
    }

    public async Task<bool> CanAddAzureSubscriptionAsync(string tenantId)
    {
        var tenant = await GetTenantByIdAsync(tenantId);
        if (tenant == null) return false;

        // Unlimited for Enterprise
        if (tenant.SubscriptionTier == "Enterprise") return true;

        return tenant.CurrentAzureSubscriptionCount < tenant.MaxAzureSubscriptions;
    }

    public async Task<bool> HasFeatureAccessAsync(string tenantId, string featureName)
    {
        var tenant = await GetTenantByIdAsync(tenantId);
        if (tenant == null) return false;

        return featureName switch
        {
            "AdvancedAnalytics" => tenant.HasAdvancedAnalytics,
            "MLPredictions" => tenant.HasMLPredictions,
            "PowerBI" => tenant.HasPowerBIIntegration,
            "CustomBranding" => tenant.HasCustomBranding,
            "SSO" => tenant.HasSSOSupport,
            _ => false
        };
    }

    private void SetTierLimits(Tenant tenant, string tier)
    {
        switch (tier)
        {
            case "Free":
                tenant.MaxUsers = 5;
                tenant.MaxAzureSubscriptions = 1;
                tenant.MaxMonthlyApiCalls = 10000;
                tenant.HasAdvancedAnalytics = false;
                tenant.HasMLPredictions = false;
                tenant.HasPowerBIIntegration = false;
                tenant.HasCustomBranding = false;
                tenant.HasSSOSupport = false;
                break;
                
            case "Pro":
                tenant.MaxUsers = 50;
                tenant.MaxAzureSubscriptions = 5;
                tenant.MaxMonthlyApiCalls = 100000;
                tenant.HasAdvancedAnalytics = true;
                tenant.HasMLPredictions = false;
                tenant.HasPowerBIIntegration = true;
                tenant.HasCustomBranding = false;
                tenant.HasSSOSupport = false;
                break;
                
            case "Enterprise":
                tenant.MaxUsers = int.MaxValue;
                tenant.MaxAzureSubscriptions = int.MaxValue;
                tenant.MaxMonthlyApiCalls = int.MaxValue;
                tenant.HasAdvancedAnalytics = true;
                tenant.HasMLPredictions = true;
                tenant.HasPowerBIIntegration = true;
                tenant.HasCustomBranding = true;
                tenant.HasSSOSupport = true;
                break;
        }
    }

    // Tenant User Management
    public async Task<List<TenantUser>> GetTenantUsersAsync(string tenantId)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.tenantId = @tenantId")
                .WithParameter("@tenantId", tenantId);

            var iterator = _usersContainer.GetItemQueryIterator<TenantUser>(query);
            var results = new List<TenantUser>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for tenant {TenantId}", tenantId);
            return new List<TenantUser>();
        }
    }

    public async Task<TenantUser?> GetTenantUserByIdAsync(string userId)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @userId")
                .WithParameter("@userId", userId);

            var iterator = _usersContainer.GetItemQueryIterator<TenantUser>(query);
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return null;
        }
    }

    public async Task<TenantUser?> GetTenantUserByEmailAsync(string tenantId, string email)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.tenantId = @tenantId AND c.email = @email")
                .WithParameter("@tenantId", tenantId)
                .WithParameter("@email", email);

            var iterator = _usersContainer.GetItemQueryIterator<TenantUser>(query);
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email {Email} in tenant {TenantId}", email, tenantId);
            return null;
        }
    }

    public async Task<TenantUser> AddTenantUserAsync(TenantUser user)
    {
        try
        {
            if (string.IsNullOrEmpty(user.Id))
            {
                user.Id = Guid.NewGuid().ToString();
            }

            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            var response = await _usersContainer.CreateItemAsync(user, new PartitionKey(user.TenantId));
            _logger.LogInformation("Created user {UserId} in tenant {TenantId}", user.Id, user.TenantId);

            // Increment tenant user count
            var tenant = await GetTenantByIdAsync(user.TenantId);
            if (tenant != null)
            {
                tenant.CurrentUserCount++;
                await UpdateTenantAsync(tenant);
            }

            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user to tenant {TenantId}", user.TenantId);
            throw;
        }
    }

    public async Task<TenantUser> UpdateTenantUserAsync(TenantUser user)
    {
        try
        {
            user.UpdatedAt = DateTime.UtcNow;
            var response = await _usersContainer.ReplaceItemAsync(user, user.Id, new PartitionKey(user.TenantId));
            _logger.LogInformation("Updated user {UserId}", user.Id);
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<bool> DeleteTenantUserAsync(string userId)
    {
        try
        {
            // First get the user to know the tenantId for partition key
            var user = await GetTenantUserByIdAsync(userId);
            if (user == null) return false;

            await _usersContainer.DeleteItemAsync<TenantUser>(userId, new PartitionKey(user.TenantId));
            _logger.LogInformation("Deleted user {UserId}", userId);

            // Decrement tenant user count
            var tenant = await GetTenantByIdAsync(user.TenantId);
            if (tenant != null && tenant.CurrentUserCount > 0)
            {
                tenant.CurrentUserCount--;
                await UpdateTenantAsync(tenant);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return false;
        }
    }
}
