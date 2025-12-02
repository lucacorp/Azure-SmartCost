using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AzureSmartCost.Functions.Models;

/// <summary>
/// Represents a customer tenant from Azure Marketplace
/// </summary>
public class Tenant
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = "tenant";

    /// <summary>
    /// Marketplace subscription ID
    /// </summary>
    [JsonProperty("marketplaceSubscriptionId")]
    public string MarketplaceSubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Azure subscription ID where resources are deployed
    /// </summary>
    [JsonProperty("azureSubscriptionId")]
    public string? AzureSubscriptionId { get; set; }

    /// <summary>
    /// Tenant/Organization name
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Company/Organization name
    /// </summary>
    [JsonProperty("companyName")]
    public string? CompanyName { get; set; }

    /// <summary>
    /// Primary contact email
    /// </summary>
    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Marketplace offer ID
    /// </summary>
    [JsonProperty("offerId")]
    public string? OfferId { get; set; }

    /// <summary>
    /// Current plan ID (basic, premium, enterprise)
    /// </summary>
    [JsonProperty("planId")]
    public string PlanId { get; set; } = "basic";

    /// <summary>
    /// Seat quantity (number of users)
    /// </summary>
    [JsonProperty("quantity")]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Subscription status from Marketplace
    /// </summary>
    [JsonProperty("status")]
    public string Status { get; set; } = "PendingFulfillmentStart";

    /// <summary>
    /// Beneficiary information
    /// </summary>
    [JsonProperty("beneficiary")]
    public TenantContact? Beneficiary { get; set; }

    /// <summary>
    /// Purchaser information
    /// </summary>
    [JsonProperty("purchaser")]
    public TenantContact? Purchaser { get; set; }

    /// <summary>
    /// Term information
    /// </summary>
    [JsonProperty("term")]
    public TenantTerm? Term { get; set; }

    /// <summary>
    /// License key (for validation)
    /// </summary>
    [JsonProperty("licenseKey")]
    public string? LicenseKey { get; set; }

    /// <summary>
    /// Is trial subscription
    /// </summary>
    [JsonProperty("isTrial")]
    public bool IsTrial { get; set; } = false;

    /// <summary>
    /// Trial expiration date
    /// </summary>
    [JsonProperty("trialExpiresAt")]
    public DateTime? TrialExpiresAt { get; set; }

    /// <summary>
    /// Trial end date (from Stripe)
    /// </summary>
    [JsonProperty("trialEndDate")]
    public DateTime? TrialEndDate { get; set; }

    /// <summary>
    /// Stripe customer ID
    /// </summary>
    [JsonProperty("stripeCustomerId")]
    public string? StripeCustomerId { get; set; }

    /// <summary>
    /// Stripe subscription ID
    /// </summary>
    [JsonProperty("stripeSubscriptionId")]
    public string? StripeSubscriptionId { get; set; }

    /// <summary>
    /// Last payment date
    /// </summary>
    [JsonProperty("lastPaymentAt")]
    public DateTime? LastPaymentAt { get; set; }

    /// <summary>
    /// Subscription created date
    /// </summary>
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified date
    /// </summary>
    [JsonProperty("lastModifiedAt")]
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Activation date
    /// </summary>
    [JsonProperty("activatedAt")]
    public DateTime? ActivatedAt { get; set; }

    /// <summary>
    /// Cancellation date
    /// </summary>
    [JsonProperty("cancelledAt")]
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Suspension date
    /// </summary>
    [JsonProperty("suspendedAt")]
    public DateTime? SuspendedAt { get; set; }

    /// <summary>
    /// Settings/Configuration
    /// </summary>
    [JsonProperty("settings")]
    public Dictionary<string, object> Settings { get; set; } = new();

    /// <summary>
    /// Custom tags
    /// </summary>
    [JsonProperty("tags")]
    public Dictionary<string, string> Tags { get; set; } = new();

    /// <summary>
    /// Cosmos DB partition key (uses marketplaceSubscriptionId)
    /// </summary>
    [JsonProperty("partitionKey")]
    public string PartitionKey => MarketplaceSubscriptionId;

    /// <summary>
    /// Is subscription active
    /// </summary>
    public bool IsActive => Status == "Subscribed" || Status == "PendingFulfillmentStart";

    /// <summary>
    /// Is subscription suspended
    /// </summary>
    public bool IsSuspended => Status == "Suspended";

    /// <summary>
    /// Is subscription cancelled
    /// </summary>
    public bool IsCancelled => Status == "Unsubscribed";
}

/// <summary>
/// Contact information
/// </summary>
public class TenantContact
{
    [JsonProperty("emailId")]
    public string? EmailId { get; set; }

    [JsonProperty("tenantId")]
    public string? TenantId { get; set; }

    [JsonProperty("objectId")]
    public string? ObjectId { get; set; }

    [JsonProperty("aadObjectId")]
    public string? AadObjectId { get; set; }

    [JsonProperty("aadTenantId")]
    public string? AadTenantId { get; set; }
}

/// <summary>
/// Subscription term
/// </summary>
public class TenantTerm
{
    [JsonProperty("termUnit")]
    public string? TermUnit { get; set; }

    [JsonProperty("startDate")]
    public DateTime? StartDate { get; set; }

    [JsonProperty("endDate")]
    public DateTime? EndDate { get; set; }
}
