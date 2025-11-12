using System;
using System.Threading.Tasks;
using AzureSmartCost.Shared.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AzureSmartCost.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CacheController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheController> _logger;

    public CacheController(
        ICacheService cacheService,
        ILogger<CacheController> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Get cache statistics (hit rate, memory usage, connection status)
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CacheStatistics), 200)]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var stats = await _cacheService.GetStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return StatusCode(500, new { error = "Failed to get cache statistics" });
        }
    }

    /// <summary>
    /// Check Redis health status
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var isHealthy = await _cacheService.IsHealthyAsync();
            
            if (isHealthy)
            {
                return Ok(new { status = "healthy", message = "Redis connection is active" });
            }
            else
            {
                return StatusCode(503, new { status = "unhealthy", message = "Redis connection failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return StatusCode(503, new { status = "unhealthy", error = ex.Message });
        }
    }

    /// <summary>
    /// Invalidate all cache keys matching a pattern
    /// Example: DELETE /api/cache/invalidate?pattern=tenant:123:*
    /// </summary>
    [HttpDelete("invalidate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> InvalidatePattern([FromQuery] string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return BadRequest(new { error = "Pattern is required" });
        }

        try
        {
            var deletedCount = await _cacheService.InvalidatePatternAsync(pattern);
            
            _logger.LogInformation("User invalidated {Count} cache keys with pattern: {Pattern}", deletedCount, pattern);
            
            return Ok(new 
            { 
                message = $"Invalidated {deletedCount} cache keys",
                pattern,
                deletedCount 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache pattern: {Pattern}", pattern);
            return StatusCode(500, new { error = "Failed to invalidate cache" });
        }
    }

    /// <summary>
    /// Invalidate cache for specific tenant
    /// Example: DELETE /api/cache/tenant/123
    /// </summary>
    [HttpDelete("tenant/{tenantId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> InvalidateTenant(string tenantId)
    {
        try
        {
            var pattern = $"tenant:{tenantId}:*";
            var deletedCount = await _cacheService.InvalidatePatternAsync(pattern);
            
            _logger.LogInformation("User invalidated tenant cache for: {TenantId}, {Count} keys deleted", tenantId, deletedCount);
            
            return Ok(new 
            { 
                message = $"Invalidated cache for tenant {tenantId}",
                tenantId,
                deletedCount 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating tenant cache: {TenantId}", tenantId);
            return StatusCode(500, new { error = "Failed to invalidate tenant cache" });
        }
    }

    /// <summary>
    /// Flush entire cache (WARNING: Deletes ALL cached data!)
    /// Admin only - requires confirmation
    /// </summary>
    [HttpDelete("flush")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> FlushAll([FromQuery] bool confirm = false)
    {
        if (!confirm)
        {
            return BadRequest(new 
            { 
                error = "Flush requires confirmation",
                message = "Add ?confirm=true to flush all cache data" 
            });
        }

        try
        {
            await _cacheService.FlushAllAsync();
            
            _logger.LogWarning("User flushed entire cache database");
            
            return Ok(new { message = "Cache flushed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing cache");
            return StatusCode(500, new { error = "Failed to flush cache" });
        }
    }

    /// <summary>
    /// Remove specific cache key
    /// Example: DELETE /api/cache/key?key=tenant:123:user:456
    /// </summary>
    [HttpDelete("key")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> RemoveKey([FromQuery] string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest(new { error = "Key is required" });
        }

        try
        {
            var removed = await _cacheService.RemoveAsync(key);
            
            if (removed)
            {
                _logger.LogInformation("User removed cache key: {Key}", key);
                return Ok(new { message = "Cache key removed", key });
            }
            else
            {
                return NotFound(new { error = "Cache key not found", key });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
            return StatusCode(500, new { error = "Failed to remove cache key" });
        }
    }

    /// <summary>
    /// Check if a cache key exists
    /// Example: GET /api/cache/exists?key=tenant:123
    /// </summary>
    [HttpGet("exists")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> KeyExists([FromQuery] string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest(new { error = "Key is required" });
        }

        try
        {
            var exists = await _cacheService.ExistsAsync(key);
            return Ok(new { key, exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence: {Key}", key);
            return StatusCode(500, new { error = "Failed to check cache key" });
        }
    }
}
