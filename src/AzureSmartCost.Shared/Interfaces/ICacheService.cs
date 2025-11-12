using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureSmartCost.Shared.Interfaces;

/// <summary>
/// Interface for distributed cache service using Redis
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get a cached value by key
    /// </summary>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Set a cached value with optional expiration
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Set a cached value with sliding expiration (resets on access)
    /// </summary>
    Task SetWithSlidingExpirationAsync<T>(string key, T value, TimeSpan slidingExpiration) where T : class;

    /// <summary>
    /// Remove a cached value by key
    /// </summary>
    Task<bool> RemoveAsync(string key);

    /// <summary>
    /// Check if a key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Get or set pattern: Try to get from cache, if not found execute factory and cache result
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Invalidate all keys matching a pattern (e.g., "tenant:123:*")
    /// </summary>
    Task<long> InvalidatePatternAsync(string pattern);

    /// <summary>
    /// Flush entire cache (use with caution!)
    /// </summary>
    Task FlushAllAsync();

    /// <summary>
    /// Get cache statistics
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync();

    /// <summary>
    /// Check if Redis connection is healthy
    /// </summary>
    Task<bool> IsHealthyAsync();
}

/// <summary>
/// Cache statistics model
/// </summary>
public class CacheStatistics
{
    public long TotalKeys { get; set; }
    public long UsedMemoryBytes { get; set; }
    public string UsedMemoryHuman { get; set; } = string.Empty;
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public double HitRate => TotalRequests > 0 ? (double)HitCount / TotalRequests * 100 : 0;
    public long TotalRequests => HitCount + MissCount;
    public bool IsConnected { get; set; }
    public string Version { get; set; } = string.Empty;
}
