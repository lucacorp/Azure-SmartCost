using System.Collections.Generic;
using System.Threading.Tasks;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Shared.Interfaces;

public interface ITenantService
{
    Task<Tenant?> GetTenantByIdAsync(string tenantId);
    Task<Tenant?> GetTenantAsync(string tenantId); // Alias for GetTenantByIdAsync
    Task<Tenant?> GetTenantByDomainAsync(string domain);
    Task<Tenant> CreateTenantAsync(Tenant tenant);
    Task<Tenant> UpdateTenantAsync(Tenant tenant);
    Task<bool> DeleteTenantAsync(string tenantId);
    Task<List<Tenant>> GetAllTenantsAsync();
    
    // Tenant User Management
    Task<List<TenantUser>> GetTenantUsersAsync(string tenantId);
    Task<TenantUser?> GetTenantUserByIdAsync(string userId);
    Task<TenantUser?> GetTenantUserByEmailAsync(string tenantId, string email);
    Task<TenantUser> AddTenantUserAsync(TenantUser user);
    Task<TenantUser> UpdateTenantUserAsync(TenantUser user);
    Task<bool> DeleteTenantUserAsync(string userId);
    
    // Subscription management
    Task<bool> UpgradeTenantAsync(string tenantId, string newTier);
    Task<bool> DowngradeTenantAsync(string tenantId, string newTier);
    Task<bool> CancelSubscriptionAsync(string tenantId);
    Task<bool> ReactivateSubscriptionAsync(string tenantId);
    
    // Usage tracking
    Task IncrementApiCallCountAsync(string tenantId);
    Task<bool> HasReachedApiLimitAsync(string tenantId);
    Task<bool> CanAddUserAsync(string tenantId);
    Task<bool> CanAddAzureSubscriptionAsync(string tenantId);
    
    // Feature checks
    Task<bool> HasFeatureAccessAsync(string tenantId, string featureName);
}
