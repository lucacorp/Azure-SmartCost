using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AzureSmartCost.Shared.Interfaces;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        ITenantService tenantService,
        ITenantContext tenantContext,
        ILogger<TenantsController> logger)
    {
        _tenantService = tenantService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a new tenant (organization signup)
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<Tenant>> CreateTenant([FromBody] Tenant tenant)
    {
        try
        {
            // Check if domain already exists
            var existing = await _tenantService.GetTenantByDomainAsync(tenant.Domain);
            if (existing != null)
            {
                return Conflict(new { message = "Domain already registered" });
            }

            var created = await _tenantService.CreateTenantAsync(tenant);
            _logger.LogInformation("Tenant created: {TenantId} - {CompanyName}", created.Id, created.CompanyName);
            
            return CreatedAtAction(nameof(GetTenant), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant");
            return StatusCode(500, new { message = "Error creating tenant", error = ex.Message });
        }
    }

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<Tenant>> GetTenant(string id)
    {
        try
        {
            // Users can only see their own tenant unless they're system admin
            if (_tenantContext.TenantId != id && _tenantContext.UserRole != "SystemAdmin")
            {
                return Forbid();
            }

            var tenant = await _tenantService.GetTenantByIdAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }

            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant {TenantId}", id);
            return StatusCode(500, new { message = "Error retrieving tenant" });
        }
    }

    /// <summary>
    /// Get current tenant info
    /// </summary>
    [HttpGet("current")]
    [Authorize]
    public async Task<ActionResult<Tenant>> GetCurrentTenant()
    {
        try
        {
            if (string.IsNullOrEmpty(_tenantContext.TenantId))
            {
                return BadRequest(new { message = "No tenant context found" });
            }

            var tenant = await _tenantService.GetTenantByIdAsync(_tenantContext.TenantId);
            if (tenant == null)
            {
                return NotFound();
            }

            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current tenant");
            return StatusCode(500, new { message = "Error retrieving tenant" });
        }
    }

    /// <summary>
    /// Get all tenants (System Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<List<Tenant>>> GetAllTenants()
    {
        try
        {
            var tenants = await _tenantService.GetAllTenantsAsync();
            return Ok(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tenants");
            return StatusCode(500, new { message = "Error retrieving tenants" });
        }
    }

    /// <summary>
    /// Update tenant information
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Tenant>> UpdateTenant(string id, [FromBody] Tenant tenant)
    {
        try
        {
            // Validate tenant access
            if (_tenantContext.TenantId != id)
            {
                return Forbid();
            }

            tenant.Id = id;
            var updated = await _tenantService.UpdateTenantAsync(tenant);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", id);
            return StatusCode(500, new { message = "Error updating tenant" });
        }
    }

    /// <summary>
    /// Upgrade tenant subscription
    /// </summary>
    [HttpPost("{id}/upgrade")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpgradeTenant(string id, [FromBody] UpgradeRequest request)
    {
        try
        {
            if (_tenantContext.TenantId != id)
            {
                return Forbid();
            }

            var success = await _tenantService.UpgradeTenantAsync(id, request.NewTier);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new { message = $"Upgraded to {request.NewTier} tier" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading tenant {TenantId}", id);
            return StatusCode(500, new { message = "Error upgrading tenant" });
        }
    }

    /// <summary>
    /// Downgrade tenant subscription
    /// </summary>
    [HttpPost("{id}/downgrade")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DowngradeTenant(string id, [FromBody] DowngradeRequest request)
    {
        try
        {
            if (_tenantContext.TenantId != id)
            {
                return Forbid();
            }

            var success = await _tenantService.DowngradeTenantAsync(id, request.NewTier);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new { message = $"Downgraded to {request.NewTier} tier" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downgrading tenant {TenantId}", id);
            return StatusCode(500, new { message = "Error downgrading tenant" });
        }
    }

    /// <summary>
    /// Cancel tenant subscription
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CancelSubscription(string id)
    {
        try
        {
            if (_tenantContext.TenantId != id)
            {
                return Forbid();
            }

            var success = await _tenantService.CancelSubscriptionAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new { message = "Subscription cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription for tenant {TenantId}", id);
            return StatusCode(500, new { message = "Error cancelling subscription" });
        }
    }

    /// <summary>
    /// Check if tenant has access to a specific feature
    /// </summary>
    [HttpGet("{id}/features/{featureName}")]
    [Authorize]
    public async Task<ActionResult<bool>> HasFeatureAccess(string id, string featureName)
    {
        try
        {
            if (_tenantContext.TenantId != id)
            {
                return Forbid();
            }

            var hasAccess = await _tenantService.HasFeatureAccessAsync(id, featureName);
            return Ok(new { feature = featureName, hasAccess });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature access for tenant {TenantId}", id);
            return StatusCode(500, new { message = "Error checking feature access" });
        }
    }

    /// <summary>
    /// Get tenant usage statistics
    /// </summary>
    [HttpGet("{id}/usage")]
    [Authorize]
    public async Task<ActionResult<object>> GetUsageStats(string id)
    {
        try
        {
            if (_tenantContext.TenantId != id)
            {
                return Forbid();
            }

            var tenant = await _tenantService.GetTenantByIdAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }

            var stats = new
            {
                tier = tenant.SubscriptionTier,
                users = new
                {
                    current = tenant.CurrentUserCount,
                    max = tenant.MaxUsers,
                    percentage = tenant.MaxUsers > 0 ? (tenant.CurrentUserCount * 100.0 / tenant.MaxUsers) : 0
                },
                azureSubscriptions = new
                {
                    current = tenant.CurrentAzureSubscriptionCount,
                    max = tenant.MaxAzureSubscriptions,
                    percentage = tenant.MaxAzureSubscriptions > 0 ? (tenant.CurrentAzureSubscriptionCount * 100.0 / tenant.MaxAzureSubscriptions) : 0
                },
                apiCalls = new
                {
                    current = tenant.CurrentMonthApiCalls,
                    max = tenant.MaxMonthlyApiCalls,
                    percentage = tenant.MaxMonthlyApiCalls > 0 ? (tenant.CurrentMonthApiCalls * 100.0 / tenant.MaxMonthlyApiCalls) : 0,
                    resetDate = tenant.LastApiCallReset.AddMonths(1)
                },
                trial = new
                {
                    isActive = tenant.IsTrialActive,
                    endDate = tenant.TrialEndDate,
                    daysRemaining = tenant.TrialEndDate.HasValue ? (tenant.TrialEndDate.Value - DateTime.UtcNow).Days : 0
                }
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage stats for tenant {TenantId}", id);
            return StatusCode(500, new { message = "Error retrieving usage statistics" });
        }
    }
}

public record UpgradeRequest(string NewTier);
public record DowngradeRequest(string NewTier);
