using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AzureSmartCost.Functions.Models;

public class CostData
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [JsonProperty("subscriptionId")]
    public string SubscriptionId { get; set; } = string.Empty;
    
    [JsonProperty("date")]
    public DateTime Date { get; set; }
    
    [JsonProperty("totalCost")]
    public decimal TotalCost { get; set; }
    
    [JsonProperty("currency")]
    public string Currency { get; set; } = "BRL";
    
    [JsonProperty("resources")]
    public List<ResourceCost> Resources { get; set; } = new();
    
    [JsonProperty("recommendations")]
    public List<string> Recommendations { get; set; } = new();
    
    [JsonProperty("cachedAt")]
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    
    [JsonProperty("ttl")]
    public int Ttl { get; set; } = 3600; // 1 hora de cache
}

public class ResourceCost
{
    [JsonProperty("resourceId")]
    public string ResourceId { get; set; } = string.Empty;
    
    [JsonProperty("resourceName")]
    public string ResourceName { get; set; } = string.Empty;
    
    [JsonProperty("resourceType")]
    public string ResourceType { get; set; } = string.Empty;
    
    [JsonProperty("cost")]
    public decimal Cost { get; set; }
    
    [JsonProperty("tags")]
    public Dictionary<string, string> Tags { get; set; } = new();
}
