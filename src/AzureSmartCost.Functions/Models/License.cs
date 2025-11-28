using System;
using System.Collections.Generic;

namespace AzureSmartCost.Functions.Models;

public class License
{
    public string id { get; set; } = string.Empty; // subscriptionId
    public string SubscriptionId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public LicenseStatus Status { get; set; } = LicenseStatus.Trial;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public decimal MonthlyFee { get; set; } = 40.00m;
    public string Currency { get; set; } = "USD";
    public int TrialDays { get; set; } = 14;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public enum LicenseStatus
{
    Trial,
    Active,
    Expired,
    Suspended,
    Cancelled
}
