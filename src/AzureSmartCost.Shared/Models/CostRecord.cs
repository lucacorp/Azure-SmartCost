using System;
using System.Text.Json.Serialization;

namespace AzureSmartCost.Shared.Models
{
    public class CostRecord
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [JsonPropertyName("partitionKey")]
        public string PartitionKey { get; set; } = string.Empty;
        
        [JsonPropertyName("subscriptionId")]
        public string SubscriptionId { get; set; } = string.Empty;
        
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }
        
        [JsonPropertyName("totalCost")]
        public decimal TotalCost { get; set; }
        
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "USD";
        
        [JsonPropertyName("resourceGroup")]
        public string ResourceGroup { get; set; } = string.Empty;

        // Backwards-compatible aliases and extended metadata used by Power BI integration
        [JsonIgnore]
        public decimal Cost
        {
            get => TotalCost;
            set => TotalCost = value;
        }

        [JsonPropertyName("resourceGroupName")]
        public string ResourceGroupName
        {
            get => ResourceGroup;
            set => ResourceGroup = value;
        }

        [JsonPropertyName("resourceName")]
        public string ResourceName { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        public string Location { get; set; } = string.Empty;

        [JsonPropertyName("subscriptionName")]
        public string SubscriptionName { get; set; } = string.Empty;

        [JsonPropertyName("meterCategory")]
        public string? MeterCategory { get; set; }

        [JsonPropertyName("meterSubCategory")]
        public string? MeterSubCategory { get; set; }

        [JsonPropertyName("meterName")]
        public string? MeterName { get; set; }

        [JsonPropertyName("unitOfMeasure")]
        public string? UnitOfMeasure { get; set; }

        [JsonPropertyName("consumedQuantity")]
        public decimal? ConsumedQuantity { get; set; }

        [JsonPropertyName("tags")]
        public System.Collections.Generic.Dictionary<string, string>? Tags { get; set; }
        
        [JsonPropertyName("serviceName")]
        public string ServiceName { get; set; } = string.Empty;
        
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [JsonPropertyName("_ts")]
        public long Timestamp { get; set; }

        public void SetPartitionKey()
        {
            PartitionKey = Date.ToString("yyyy-MM");
        }
    }
}
