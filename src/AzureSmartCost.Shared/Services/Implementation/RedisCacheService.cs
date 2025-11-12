using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AzureSmartCost.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AzureSmartCost.Shared.Services.Implementation;

/// <summary>
/// Redis-based distributed cache service
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _defaultExpiration;
    private long _hitCount;
    private long _missCount;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger,
        IConfiguration configuration)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;
        _configuration = configuration;
        
        var expirationMinutes = _configuration.GetValue<int>("Redis:DefaultExpirationMinutes", 60);
        _defaultExpiration = TimeSpan.FromMinutes(expirationMinutes);
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            
            if (!value.HasValue)
            {
                System.Threading.Interlocked.Increment(ref _missCount);
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            System.Threading.Interlocked.Increment(ref _hitCount);
            _logger.LogDebug("Cache hit for key: {Key}", key);
            
            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            var expirationTime = expiration ?? _defaultExpiration;
            
            await _database.StringSetAsync(key, serialized, expirationTime);
            _logger.LogDebug("Cache set for key: {Key} with expiration: {Expiration}", key, expirationTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key: {Key}", key);
            // Don't throw - cache failures shouldn't break the app
        }
    }

    public async Task SetWithSlidingExpirationAsync<T>(string key, T value, TimeSpan slidingExpiration) where T : class
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            
            // Redis doesn't have native sliding expiration, so we set absolute and refresh on access
            await _database.StringSetAsync(key, serialized, slidingExpiration);
            _logger.LogDebug("Cache set with sliding expiration for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache with sliding expiration key: {Key}", key);
        }
    }

    public async Task<bool> RemoveAsync(string key)
    {
        try
        {
            var result = await _database.KeyDeleteAsync(key);
            _logger.LogDebug("Cache removed for key: {Key}, Result: {Result}", key, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence: {Key}", key);
            return false;
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
    {
        // Try to get from cache first
        var cached = await GetAsync<T>(key);
        if (cached != null)
        {
            return cached;
        }

        // Not in cache, execute factory to get value
        try
        {
            var value = await factory();
            
            // Cache the result
            await SetAsync(key, value, expiration);
            
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing factory for cache key: {Key}", key);
            throw;
        }
    }

    public async Task<long> InvalidatePatternAsync(string pattern)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern).ToArray();
            
            if (keys.Length == 0)
            {
                _logger.LogDebug("No keys found matching pattern: {Pattern}", pattern);
                return 0;
            }

            var deleted = await _database.KeyDeleteAsync(keys);
            _logger.LogInformation("Invalidated {Count} cache keys matching pattern: {Pattern}", deleted, pattern);
            
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache pattern: {Pattern}", pattern);
            return 0;
        }
    }

    public async Task FlushAllAsync()
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                await server.FlushDatabaseAsync();
            }
            
            _logger.LogWarning("Flushed entire cache database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing cache");
            throw;
        }
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var info = await server.InfoAsync("stats");
            var memory = await server.InfoAsync("memory");
            
            var stats = new CacheStatistics
            {
                IsConnected = _redis.IsConnected,
                Version = server.Version?.ToString() ?? "Unknown",
                HitCount = _hitCount,
                MissCount = _missCount
            };

            // Parse info sections
            foreach (var section in info)
            {
                foreach (var item in section)
                {
                    if (item.Key == "keyspace_hits")
                    {
                        stats.HitCount = long.Parse(item.Value);
                    }
                    else if (item.Key == "keyspace_misses")
                    {
                        stats.MissCount = long.Parse(item.Value);
                    }
                }
            }

            foreach (var section in memory)
            {
                foreach (var item in section)
                {
                    if (item.Key == "used_memory")
                    {
                        stats.UsedMemoryBytes = long.Parse(item.Value);
                    }
                    else if (item.Key == "used_memory_human")
                    {
                        stats.UsedMemoryHuman = item.Value;
                    }
                }
            }

            // Get total keys count
            stats.TotalKeys = server.DatabaseSize();

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return new CacheStatistics { IsConnected = false };
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            await _database.PingAsync();
            return _redis.IsConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return false;
        }
    }
}

/// <summary>
/// Helper class for generating cache keys with consistent prefixes
/// </summary>
public static class CacheKeys
{
    public static string Tenant(string tenantId) => $"tenant:{tenantId}";
    public static string TenantUsers(string tenantId) => $"tenant:{tenantId}:users";
    public static string TenantUser(string tenantId, string userId) => $"tenant:{tenantId}:user:{userId}";
    public static string Costs(string tenantId, string subscriptionId) => $"tenant:{tenantId}:costs:{subscriptionId}";
    public static string CostAnalytics(string tenantId, string period) => $"tenant:{tenantId}:analytics:{period}";
    public static string Budget(string tenantId, string budgetId) => $"tenant:{tenantId}:budget:{budgetId}";
    public static string MarketplaceSubscription(string tenantId) => $"marketplace:{tenantId}";
    public static string AzureAdGroups(string tenantId) => $"azuread:{tenantId}:groups";
    public static string PowerBiReport(string tenantId, string reportId) => $"powerbi:{tenantId}:report:{reportId}";
    
    /// <summary>
    /// Pattern to invalidate all keys for a tenant
    /// </summary>
    public static string TenantPattern(string tenantId) => $"tenant:{tenantId}:*";
}
