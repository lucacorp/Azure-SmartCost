using System.Collections.Generic;
using System.Threading.Tasks;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Shared.Interfaces;

/// <summary>
/// Service for Azure AD integration including group sync and user provisioning
/// </summary>
public interface IAzureAdService
{
    /// <summary>
    /// Synchronize Azure AD groups to tenant roles
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="userId">User ID to sync groups for</param>
    /// <returns>List of roles assigned to user</returns>
    Task<List<string>> SyncUserGroupsAsync(string tenantId, string userId);

    /// <summary>
    /// Auto-provision user from Azure AD to SmartCost tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="azureAdUserId">Azure AD User Object ID</param>
    /// <param name="email">User email</param>
    /// <param name="displayName">User display name</param>
    /// <returns>Created or updated TenantUser</returns>
    Task<TenantUser> AutoProvisionUserAsync(string tenantId, string azureAdUserId, string email, string displayName);

    /// <summary>
    /// Get user from Azure AD by object ID
    /// </summary>
    /// <param name="userId">Azure AD User Object ID</param>
    /// <returns>Azure AD user details</returns>
    Task<AzureAdUser?> GetUserAsync(string userId);

    /// <summary>
    /// Get groups for a user from Azure AD
    /// </summary>
    /// <param name="userId">Azure AD User Object ID</param>
    /// <returns>List of group IDs</returns>
    Task<List<string>> GetUserGroupsAsync(string userId);

    /// <summary>
    /// Get all groups from Azure AD tenant
    /// </summary>
    /// <returns>List of Azure AD groups</returns>
    Task<List<AzureAdGroup>> GetAllGroupsAsync();

    /// <summary>
    /// Map Azure AD group to SmartCost role
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="groupId">Azure AD Group ID</param>
    /// <param name="role">SmartCost role (Admin, User, Viewer)</param>
    /// <returns>Success status</returns>
    Task<bool> MapGroupToRoleAsync(string tenantId, string groupId, string role);

    /// <summary>
    /// Get role mappings for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Dictionary of group ID to role</returns>
    Task<Dictionary<string, string>> GetRoleMappingsAsync(string tenantId);

    /// <summary>
    /// Validate Azure AD token and extract user info
    /// </summary>
    /// <param name="accessToken">Azure AD access token</param>
    /// <returns>User info from token</returns>
    Task<AzureAdTokenInfo?> ValidateTokenAsync(string accessToken);
}

/// <summary>
/// Azure AD user information
/// </summary>
public class AzureAdUser
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

/// <summary>
/// Azure AD group information
/// </summary>
public class AzureAdGroup
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Azure AD token information
/// </summary>
public class AzureAdTokenInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
