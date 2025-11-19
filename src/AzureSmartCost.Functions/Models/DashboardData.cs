using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AzureSmartCost.Functions.Models;

public class DashboardData
{
    [JsonProperty("subscriptionId")]
    public string SubscriptionId { get; set; } = string.Empty;
    
    [JsonProperty("period")]
    public string Period { get; set; } = "Last 30 days";
    
    [JsonProperty("summary")]
    public CostSummary Summary { get; set; } = new();
    
    [JsonProperty("topResources")]
    public List<ResourceCost> TopResources { get; set; } = new();
    
    [JsonProperty("costByService")]
    public List<ServiceCost> CostByService { get; set; } = new();
    
    [JsonProperty("dailyTrend")]
    public List<DailyCost> DailyTrend { get; set; } = new();
    
    [JsonProperty("recommendations")]
    public List<string> Recommendations { get; set; } = new();
    
    [JsonProperty("alerts")]
    public List<BudgetAlert> Alerts { get; set; } = new();
}

public class CostSummary
{
    [JsonProperty("total")]
    public decimal Total { get; set; }
    
    [JsonProperty("previous")]
    public decimal Previous { get; set; }
    
    [JsonProperty("change")]
    public decimal Change { get; set; }
    
    [JsonProperty("changePercent")]
    public decimal ChangePercent { get; set; }
    
    [JsonProperty("forecast")]
    public decimal Forecast { get; set; }
    
    [JsonProperty("currency")]
    public string Currency { get; set; } = "BRL";
}

public class ServiceCost
{
    [JsonProperty("service")]
    public string Service { get; set; } = string.Empty;
    
    [JsonProperty("cost")]
    public decimal Cost { get; set; }
    
    [JsonProperty("percentage")]
    public decimal Percentage { get; set; }
    
    [JsonProperty("resourceCount")]
    public int ResourceCount { get; set; }
}

public class DailyCost
{
    [JsonProperty("date")]
    public string Date { get; set; } = string.Empty;
    
    [JsonProperty("cost")]
    public decimal Cost { get; set; }
}
