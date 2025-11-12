using System;
using System.Collections.Generic;

namespace AzureSmartCost.Shared.Models;

/// <summary>
/// Plano de pricing do Stripe
/// </summary>
public class SubscriptionPlan
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StripePriceId { get; set; } = string.Empty;
    public string StripeProductId { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public string Currency { get; set; } = "usd";
    public string Tier { get; set; } = "Free"; // Free, Pro, Enterprise
    
    // Features
    public int MaxUsers { get; set; }
    public int MaxAzureSubscriptions { get; set; }
    public int MaxMonthlyApiCalls { get; set; }
    public bool HasAdvancedAnalytics { get; set; }
    public bool HasMLPredictions { get; set; }
    public bool HasPowerBIIntegration { get; set; }
    public bool HasCustomBranding { get; set; }
    public bool HasSSOSupport { get; set; }
    public bool HasPrioritySupport { get; set; }
    
    // Display
    public string Description { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new();
    public bool IsPopular { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Invoice gerado pelo Stripe
/// </summary>
public class StripeInvoice
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string StripeInvoiceId { get; set; } = string.Empty;
    public string StripeCustomerId { get; set; } = string.Empty;
    public string StripeSubscriptionId { get; set; } = string.Empty;
    
    public decimal Amount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "usd";
    
    public string Status { get; set; } = "draft"; // draft, open, paid, void, uncollectible
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? DueDate { get; set; }
    
    public string InvoicePdfUrl { get; set; } = string.Empty;
    public string HostedInvoiceUrl { get; set; } = string.Empty;
    
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Evento de webhook do Stripe
/// </summary>
public class StripeWebhookEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty; // JSON payload
    public bool Processed { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessingError { get; set; }
}
