using System;
using System.Collections.Generic;

namespace AzureSmartCost.Shared.Models
{
    public enum AlertLevel
    {
        Info = 1,
        Warning = 2,
        Critical = 3
    }

    public enum AlertType
    {
        DailyCostThreshold,
        MonthlyCostThreshold,
        AnomalyCostSpike,
        ServiceCostIncrease
    }

    public class CostThreshold
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ResourceGroup { get; set; }
        public string? ServiceName { get; set; }
        public decimal ThresholdAmount { get; set; }
        public AlertLevel AlertLevel { get; set; }
        public AlertType AlertType { get; set; }
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastTriggered { get; set; }
        public int TriggerCount { get; set; } = 0;
    }

    public class CostAlert
    {
        public string Id { get; set; } = string.Empty;
        public string ThresholdId { get; set; } = string.Empty;
        public AlertLevel Level { get; set; }
        public AlertType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public decimal CurrentCost { get; set; }
        public decimal ThresholdAmount { get; set; }
        public decimal PercentageOver { get; set; }
        public string? ResourceGroup { get; set; }
        public string? ServiceName { get; set; }
        public DateTime TriggeredAt { get; set; }
        public bool IsResolved { get; set; } = false;
        public DateTime? ResolvedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}