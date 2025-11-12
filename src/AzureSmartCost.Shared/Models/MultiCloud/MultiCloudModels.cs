using System;
using System.Collections.Generic;

namespace AzureSmartCost.Shared.Models.MultiCloud
{
    /// <summary>
    /// Multi-cloud cost management models
    /// </summary>
    public class MultiCloudAccount
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public CloudProvider Provider { get; set; }
        public string AccountId { get; set; } = string.Empty;
        public Dictionary<string, string> Credentials { get; set; } = new Dictionary<string, string>();
        public bool IsActive { get; set; } = true;
        public DateTime LastSyncDate { get; set; }
        public ConnectionStatus Status { get; set; }
    }

    public enum CloudProvider
    {
        Azure,
        AWS,
        GoogleCloud,
        OCI,
        Alibaba
    }

    public enum ConnectionStatus
    {
        Connected,
        Disconnected,
        Error,
        Authenticating
    }

    public class UnifiedCostRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public CloudProvider Provider { get; set; }
        public string AccountId { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Cost { get; set; }
        public string Currency { get; set; } = "USD";
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceCategory { get; set; } = string.Empty;
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
        public CloudSpecificData ProviderData { get; set; } = new CloudSpecificData();
    }

    public class CloudSpecificData
    {
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        
        // Azure specific
        public string? SubscriptionId { get; set; }
        public string? ResourceGroup { get; set; }
        
        // AWS specific
        public string? AwsAccount { get; set; }
        public string? UsageType { get; set; }
        
        // GCP specific
        public string? ProjectId { get; set; }
        public string? BillingAccount { get; set; }
    }

    public class MultiCloudDashboard
    {
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public decimal TotalCost { get; set; }
        public Dictionary<CloudProvider, decimal> CostByProvider { get; set; } = new Dictionary<CloudProvider, decimal>();
        public Dictionary<string, decimal> CostByService { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> CostByRegion { get; set; } = new Dictionary<string, decimal>();
        public List<CloudOptimizationRecommendation> Recommendations { get; set; } = new List<CloudOptimizationRecommendation>();
        public List<CrossCloudComparison> Comparisons { get; set; } = new List<CrossCloudComparison>();
    }

    public class CloudOptimizationRecommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public CloudProvider Provider { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PotentialSavings { get; set; }
        public RecommendationType Type { get; set; }
        public string ResourceId { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new List<string>();
    }

    public enum RecommendationType
    {
        CrossCloudMigration,
        RegionOptimization,
        ServiceReplacement,
        ArchitectureOptimization,
        CostModelComparison
    }

    public class CrossCloudComparison
    {
        public string ServiceName { get; set; } = string.Empty;
        public Dictionary<CloudProvider, decimal> ProviderCosts { get; set; } = new Dictionary<CloudProvider, decimal>();
        public CloudProvider RecommendedProvider { get; set; }
        public decimal PotentialSavings { get; set; }
        public string Reasoning { get; set; } = string.Empty;
    }
}