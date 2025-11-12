using System;
using System.Collections.Generic;

namespace AzureSmartCost.Shared.Models;

/// <summary>
/// Representa uma organização/empresa usando a plataforma (multi-tenant)
/// </summary>
public class Tenant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty; // ex: contoso.com
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    
    // Subscription & Billing
    public string SubscriptionTier { get; set; } = "Free"; // Free, Pro, Enterprise
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsTrialActive { get; set; } = true;
    public DateTime? TrialEndDate { get; set; }
    
    // Limits & Quotas by Tier
    public int MaxUsers { get; set; } = 5; // Free: 5, Pro: 50, Enterprise: unlimited
    public int MaxAzureSubscriptions { get; set; } = 1; // Free: 1, Pro: 5, Enterprise: unlimited
    public int MaxMonthlyApiCalls { get; set; } = 10000; // Free: 10k, Pro: 100k, Enterprise: unlimited
    public bool HasAdvancedAnalytics { get; set; } = false; // Pro+
    public bool HasMLPredictions { get; set; } = false; // Enterprise only
    public bool HasPowerBIIntegration { get; set; } = false; // Pro+
    public bool HasCustomBranding { get; set; } = false; // Enterprise only
    public bool HasSSOSupport { get; set; } = false; // Enterprise only
    
    // Usage Tracking
    public int CurrentUserCount { get; set; } = 0;
    public int CurrentAzureSubscriptionCount { get; set; } = 0;
    public int CurrentMonthApiCalls { get; set; } = 0;
    public DateTime LastApiCallReset { get; set; } = DateTime.UtcNow;
    
    // Azure Configuration
    public string? AzureTenantId { get; set; }
    public string? AzureClientId { get; set; }
    public string? AzureClientSecret { get; set; } // Encrytped
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
