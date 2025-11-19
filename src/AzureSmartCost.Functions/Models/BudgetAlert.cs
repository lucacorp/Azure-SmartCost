using System;
using Newtonsoft.Json;

namespace AzureSmartCost.Functions.Models;

public class BudgetAlert
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [JsonProperty("subscriptionId")]
    public string SubscriptionId { get; set; } = string.Empty;
    
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonProperty("amount")]
    public decimal Amount { get; set; }
    
    [JsonProperty("currentSpend")]
    public decimal CurrentSpend { get; set; }
    
    [JsonProperty("threshold")]
    public int Threshold { get; set; } = 80; // Alerta em 80%
    
    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonProperty("isActive")]
    public bool IsActive { get; set; } = true;
    
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [JsonProperty("lastCheckedAt")]
    public DateTime? LastCheckedAt { get; set; }
}
