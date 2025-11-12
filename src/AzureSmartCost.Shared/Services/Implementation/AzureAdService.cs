using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using AzureSmartCost.Shared.Interfaces;
using AzureSmartCost.Shared.Models;
using Microsoft.Azure.Cosmos;

namespace AzureSmartCost.Shared.Services.Implementation;

/// <summary>
/// Service for Azure AD integration with group sync and auto-provisioning
/// </summary>
public class AzureAdService : IAzureAdService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureAdService> _logger;
    private readonly ITenantService _tenantService;
    private readonly GraphServiceClient _graphClient;
    private readonly Container _roleMappingsContainer;

    public AzureAdService(
        IConfiguration configuration,
        ILogger<AzureAdService> logger,
        ITenantService tenantService,
        CosmosClient cosmosClient)
    {
        _configuration = configuration;
        _logger = logger;
        _tenantService = tenantService;

        // Initialize Graph Client with DefaultAzureCredential
        var tenantId = configuration["AzureAd:TenantId"];
        var clientId = configuration["AzureAd:ClientId"];
        var clientSecret = configuration["AzureAd:ClientSecret"];

        var options = new ClientSecretCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
        };

        var clientSecretCredential = new ClientSecretCredential(
            tenantId, clientId, clientSecret, options);

        _graphClient = new GraphServiceClient(clientSecretCredential, new[] { "https://graph.microsoft.com/.default" });

        // Initialize Cosmos container for role mappings
        var databaseName = configuration["CosmosDb:DatabaseName"];
        var database = cosmosClient.GetDatabase(databaseName);
        _roleMappingsContainer = database.GetContainer("RoleMappings");
    }

    /// <inheritdoc/>
    public async Task<List<string>> SyncUserGroupsAsync(string tenantId, string userId)
    {
        try
        {
            _logger.LogInformation("Syncing groups for user {UserId} in tenant {TenantId}", userId, tenantId);

            // Get user's groups from Azure AD
            var groupIds = await GetUserGroupsAsync(userId);

            // Get role mappings for this tenant
            var roleMappings = await GetRoleMappingsAsync(tenantId);

            // Map groups to roles
            var roles = new List<string>();
            foreach (var groupId in groupIds)
            {
                if (roleMappings.TryGetValue(groupId, out var role))
                {
                    roles.Add(role);
                }
            }

            // If no roles mapped, assign default "User" role
            if (roles.Count == 0)
            {
                roles.Add("User");
            }

            _logger.LogInformation("User {UserId} has roles: {Roles}", userId, string.Join(", ", roles));

            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing groups for user {UserId}", userId);
            // Return default role on error
            return new List<string> { "User" };
        }
    }

    /// <inheritdoc/>
    public async Task<TenantUser> AutoProvisionUserAsync(string tenantId, string azureAdUserId, string email, string displayName)
    {
        try
        {
            _logger.LogInformation("Auto-provisioning user {Email} from Azure AD to tenant {TenantId}", email, tenantId);

            // Get tenant
            var tenant = await _tenantService.GetTenantAsync(tenantId);
            if (tenant == null)
            {
                throw new InvalidOperationException($"Tenant {tenantId} not found");
            }

            // Check if user already exists
            var existingUsers = await _tenantService.GetTenantUsersAsync(tenantId);
            var existingUser = existingUsers.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (existingUser != null)
            {
                _logger.LogInformation("User {Email} already exists in tenant {TenantId}, updating sync info", email, tenantId);
                
                // Update last sync time
                existingUser.AzureAdUserId = azureAdUserId;
                existingUser.LastSyncedAt = DateTime.UtcNow;
                
                // Sync roles from groups
                var roles = await SyncUserGroupsAsync(tenantId, azureAdUserId);
                existingUser.Role = roles.Contains("Admin") ? "Admin" : roles.Contains("User") ? "User" : "Viewer";

                await _tenantService.UpdateTenantUserAsync(existingUser);
                return existingUser;
            }

            // Create new user
            var roles2 = await SyncUserGroupsAsync(tenantId, azureAdUserId);
            var role = roles2.Contains("Admin") ? "Admin" : roles2.Contains("User") ? "User" : "Viewer";

            var newUser = new TenantUser
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                Email = email,
                Name = displayName,
                Role = role,
                IsActive = true,
                AzureAdUserId = azureAdUserId,
                CreatedAt = DateTime.UtcNow,
                LastSyncedAt = DateTime.UtcNow
            };

            await _tenantService.AddTenantUserAsync(newUser);

            _logger.LogInformation("User {Email} auto-provisioned to tenant {TenantId} with role {Role}", email, tenantId, role);

            return newUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-provisioning user {Email} to tenant {TenantId}", email, tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<AzureAdUser?> GetUserAsync(string userId)
    {
        try
        {
            var user = await _graphClient.Users[userId].GetAsync();
            
            if (user == null)
            {
                return null;
            }

            return new AzureAdUser
            {
                Id = user.Id ?? string.Empty,
                Email = user.Mail ?? user.UserPrincipalName ?? string.Empty,
                DisplayName = user.DisplayName ?? string.Empty,
                JobTitle = user.JobTitle ?? string.Empty,
                Department = user.Department ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId} from Azure AD", userId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetUserGroupsAsync(string userId)
    {
        try
        {
            var groups = new List<string>();
            var result = await _graphClient.Users[userId].MemberOf.GetAsync();

            if (result?.Value != null)
            {
                foreach (var directoryObject in result.Value)
                {
                    if (directoryObject is Microsoft.Graph.Models.Group group && group.Id != null)
                    {
                        groups.Add(group.Id);
                    }
                }
            }

            return groups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting groups for user {UserId}", userId);
            return new List<string>();
        }
    }

    /// <inheritdoc/>
    public async Task<List<AzureAdGroup>> GetAllGroupsAsync()
    {
        try
        {
            var groups = new List<AzureAdGroup>();
            var result = await _graphClient.Groups.GetAsync();

            if (result?.Value != null)
            {
                foreach (var group in result.Value)
                {
                    groups.Add(new AzureAdGroup
                    {
                        Id = group.Id ?? string.Empty,
                        DisplayName = group.DisplayName ?? string.Empty,
                        Description = group.Description ?? string.Empty
                    });
                }
            }

            return groups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting groups from Azure AD");
            return new List<AzureAdGroup>();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> MapGroupToRoleAsync(string tenantId, string groupId, string role)
    {
        try
        {
            var mapping = new RoleMapping
            {
                Id = $"{tenantId}:{groupId}",
                TenantId = tenantId,
                GroupId = groupId,
                Role = role,
                CreatedAt = DateTime.UtcNow
            };

            await _roleMappingsContainer.UpsertItemAsync(mapping, new PartitionKey(tenantId));

            _logger.LogInformation("Mapped group {GroupId} to role {Role} for tenant {TenantId}", groupId, role, tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping group {GroupId} to role {Role}", groupId, role);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> GetRoleMappingsAsync(string tenantId)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.tenantId = @tenantId")
                .WithParameter("@tenantId", tenantId);

            var iterator = _roleMappingsContainer.GetItemQueryIterator<RoleMapping>(query);
            var mappings = new Dictionary<string, string>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var mapping in response)
                {
                    mappings[mapping.GroupId] = mapping.Role;
                }
            }

            return mappings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role mappings for tenant {TenantId}", tenantId);
            return new Dictionary<string, string>();
        }
    }

    /// <inheritdoc/>
    public async Task<AzureAdTokenInfo?> ValidateTokenAsync(string accessToken)
    {
        try
        {
            // TODO: Implement proper token validation using Microsoft.IdentityModel.Tokens
            // For now, we'll extract user info from Graph API using the token
            
            // This would require creating a new GraphServiceClient with the provided token
            // and calling /me endpoint
            
            await Task.CompletedTask;
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Azure AD token");
            return null;
        }
    }
}

/// <summary>
/// Role mapping model for Cosmos DB
/// </summary>
public class RoleMapping
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
