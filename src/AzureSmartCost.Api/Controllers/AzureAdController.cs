using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AzureSmartCost.Shared.Interfaces;
using AzureSmartCost.Shared.Models;
using System.Security.Claims;

namespace AzureSmartCost.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AzureAdController : ControllerBase
{
    private readonly IAzureAdService _azureAdService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<AzureAdController> _logger;

    public AzureAdController(
        IAzureAdService azureAdService,
        ITenantService tenantService,
        ILogger<AzureAdController> logger)
    {
        _azureAdService = azureAdService;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Auto-provision current Azure AD user to tenant
    /// </summary>
    [HttpPost("provision")]
    public async Task<ActionResult<TenantUser>> AutoProvisionCurrentUser([FromBody] ProvisionRequest request)
    {
        try
        {
            // Extract user info from Azure AD token claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                      ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                      ?? User.FindFirst("oid")?.Value;
            
            var email = User.FindFirst(ClaimTypes.Email)?.Value 
                     ?? User.FindFirst("preferred_username")?.Value
                     ?? User.FindFirst("upn")?.Value;
            
            var name = User.FindFirst(ClaimTypes.Name)?.Value 
                    ?? User.FindFirst("name")?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            {
                return BadRequest(new { error = "User ID or email not found in token claims" });
            }

            var user = await _azureAdService.AutoProvisionUserAsync(
                request.TenantId, 
                userId, 
                email, 
                name ?? email
            );

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-provisioning user");
            return StatusCode(500, new { error = "Error auto-provisioning user", details = ex.Message });
        }
    }

    /// <summary>
    /// Sync Azure AD groups for current user
    /// </summary>
    [HttpPost("sync-groups")]
    public async Task<ActionResult<SyncGroupsResponse>> SyncGroups([FromBody] SyncGroupsRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                      ?? User.FindFirst("oid")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "User ID not found in token" });
            }

            var roles = await _azureAdService.SyncUserGroupsAsync(request.TenantId, userId);

            return Ok(new SyncGroupsResponse
            {
                UserId = userId,
                Roles = roles,
                SyncedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing groups");
            return StatusCode(500, new { error = "Error syncing groups", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all Azure AD groups from tenant
    /// </summary>
    [HttpGet("groups")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<AzureAdGroup>>> GetGroups()
    {
        try
        {
            var groups = await _azureAdService.GetAllGroupsAsync();
            return Ok(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting groups");
            return StatusCode(500, new { error = "Error getting groups", details = ex.Message });
        }
    }

    /// <summary>
    /// Map Azure AD group to SmartCost role
    /// </summary>
    [HttpPost("groups/{groupId}/map-role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> MapGroupToRole(string groupId, [FromBody] MapGroupRequest request)
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(new { error = "Tenant ID not found" });
            }

            if (!new[] { "Admin", "User", "Viewer" }.Contains(request.Role))
            {
                return BadRequest(new { error = "Invalid role. Must be Admin, User, or Viewer" });
            }

            var success = await _azureAdService.MapGroupToRoleAsync(tenantId, groupId, request.Role);

            if (success)
            {
                return Ok(new { message = $"Group {groupId} mapped to role {request.Role}" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to map group to role" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping group to role");
            return StatusCode(500, new { error = "Error mapping group to role", details = ex.Message });
        }
    }

    /// <summary>
    /// Get role mappings for current tenant
    /// </summary>
    [HttpGet("role-mappings")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Dictionary<string, string>>> GetRoleMappings()
    {
        try
        {
            var tenantId = User.FindFirst("tenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(new { error = "Tenant ID not found" });
            }

            var mappings = await _azureAdService.GetRoleMappingsAsync(tenantId);
            return Ok(mappings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role mappings");
            return StatusCode(500, new { error = "Error getting role mappings", details = ex.Message });
        }
    }

    /// <summary>
    /// Get current user info from Azure AD
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<AzureAdUser>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                      ?? User.FindFirst("oid")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "User ID not found in token" });
            }

            var user = await _azureAdService.GetUserAsync(userId);

            if (user == null)
            {
                return NotFound(new { error = "User not found in Azure AD" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { error = "Error getting current user", details = ex.Message });
        }
    }

    /// <summary>
    /// Get user's Azure AD groups
    /// </summary>
    [HttpGet("me/groups")]
    public async Task<ActionResult<List<string>>> GetCurrentUserGroups()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                      ?? User.FindFirst("oid")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "User ID not found in token" });
            }

            var groups = await _azureAdService.GetUserGroupsAsync(userId);
            return Ok(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user groups");
            return StatusCode(500, new { error = "Error getting user groups", details = ex.Message });
        }
    }
}

// DTOs
public class ProvisionRequest
{
    public string TenantId { get; set; } = string.Empty;
}

public class SyncGroupsRequest
{
    public string TenantId { get; set; } = string.Empty;
}

public class SyncGroupsResponse
{
    public string UserId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public DateTime SyncedAt { get; set; }
}

public class MapGroupRequest
{
    public string Role { get; set; } = string.Empty;
}
