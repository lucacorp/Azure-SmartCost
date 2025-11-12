using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzureSmartCost.Shared.Models;

/// <summary>
/// Azure Marketplace SaaS Subscription
/// Representa uma assinatura comprada via Azure Marketplace
/// </summary>
public class MarketplaceSubscription
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MarketplaceSubscriptionId { get; set; } = string.Empty; // ID do Azure Marketplace
    public string MarketplaceSubscriptionName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty; // Link para nosso Tenant
    public string PurchaserEmail { get; set; } = string.Empty;
    public string PurchaserTenantId { get; set; } = string.Empty; // Azure AD Tenant do comprador
    public string OfferId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty; // free, pro, enterprise
    public int Quantity { get; set; } = 1;
    public string Status { get; set; } = "PendingFulfillmentStart"; // PendingFulfillmentStart, Subscribed, Suspended, Unsubscribed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ActivatedAt { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public DateTime? UnsubscribedAt { get; set; }
    public string? SaasSubscriptionStatus { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Marketplace Landing Page Token
/// Token recebido quando usuário clica em "Configure Account" no Azure Portal
/// </summary>
public class MarketplaceLandingToken
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
    
    [JsonPropertyName("subscription_id")]
    public string? SubscriptionId { get; set; }
}

/// <summary>
/// Resolved Subscription Details
/// Detalhes obtidos ao resolver o token via Marketplace API
/// </summary>
public class ResolvedSubscription
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("subscriptionName")]
    public string SubscriptionName { get; set; } = string.Empty;
    
    [JsonPropertyName("offerId")]
    public string OfferId { get; set; } = string.Empty;
    
    [JsonPropertyName("planId")]
    public string PlanId { get; set; } = string.Empty;
    
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
    
    [JsonPropertyName("subscription")]
    public SubscriptionDetails? Subscription { get; set; }
    
    [JsonPropertyName("purchaser")]
    public PurchaserDetails? Purchaser { get; set; }
    
    [JsonPropertyName("term")]
    public TermDetails? Term { get; set; }
}

public class SubscriptionDetails
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("publisherId")]
    public string PublisherId { get; set; } = string.Empty;
    
    [JsonPropertyName("offerId")]
    public string OfferId { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("saasSubscriptionStatus")]
    public string SaasSubscriptionStatus { get; set; } = string.Empty;
    
    [JsonPropertyName("beneficiary")]
    public BeneficiaryDetails? Beneficiary { get; set; }
    
    [JsonPropertyName("purchaser")]
    public PurchaserDetails? Purchaser { get; set; }
    
    [JsonPropertyName("planId")]
    public string PlanId { get; set; } = string.Empty;
    
    [JsonPropertyName("term")]
    public TermDetails? Term { get; set; }
    
    [JsonPropertyName("isTest")]
    public bool IsTest { get; set; }
    
    [JsonPropertyName("isFreeTrial")]
    public bool IsFreeTrial { get; set; }
    
    [JsonPropertyName("allowedCustomerOperations")]
    public List<string>? AllowedCustomerOperations { get; set; }
    
    [JsonPropertyName("sessionMode")]
    public string? SessionMode { get; set; }
    
    [JsonPropertyName("sandboxType")]
    public string? SandboxType { get; set; }
}

public class BeneficiaryDetails
{
    [JsonPropertyName("emailId")]
    public string EmailId { get; set; } = string.Empty;
    
    [JsonPropertyName("objectId")]
    public string ObjectId { get; set; } = string.Empty;
    
    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;
    
    [JsonPropertyName("puid")]
    public string? Puid { get; set; }
}

public class PurchaserDetails
{
    [JsonPropertyName("emailId")]
    public string EmailId { get; set; } = string.Empty;
    
    [JsonPropertyName("objectId")]
    public string ObjectId { get; set; } = string.Empty;
    
    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;
    
    [JsonPropertyName("puid")]
    public string? Puid { get; set; }
}

public class TermDetails
{
    [JsonPropertyName("termUnit")]
    public string TermUnit { get; set; } = string.Empty; // P1M, P1Y
    
    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }
    
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Marketplace Webhook Event
/// Eventos recebidos do Azure Marketplace para lifecycle da subscription
/// </summary>
public class MarketplaceWebhookEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("id")]
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("activityId")]
    public string ActivityId { get; set; } = string.Empty;
    
    [JsonPropertyName("subscriptionId")]
    public string SubscriptionId { get; set; } = string.Empty;
    
    [JsonPropertyName("offerId")]
    public string OfferId { get; set; } = string.Empty;
    
    [JsonPropertyName("planId")]
    public string PlanId { get; set; } = string.Empty;
    
    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }
    
    [JsonPropertyName("timeStamp")]
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty; // Subscribe, Unsubscribe, ChangePlan, ChangeQuantity, Suspend, Reinstate
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // InProgress, Succeeded, Failed
    
    [JsonPropertyName("operationRequestSource")]
    public string? OperationRequestSource { get; set; }
    
    public bool Processed { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessingError { get; set; }
}

/// <summary>
/// Activation Request
/// Enviado para Azure Marketplace para ativar uma subscription
/// </summary>
public class ActivationRequest
{
    [JsonPropertyName("planId")]
    public string PlanId { get; set; } = string.Empty;
    
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}

/// <summary>
/// Operation Status Update
/// Atualizar status de uma operation no Marketplace
/// </summary>
public class OperationStatusUpdate
{
    [JsonPropertyName("planId")]
    public string? PlanId { get; set; }
    
    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // Success, Failure, Conflict
}

/// <summary>
/// Marketplace Offer Configuration
/// Configuração do offer no Partner Center
/// </summary>
public class MarketplaceOfferConfig
{
    public string OfferId { get; set; } = "azure-smartcost";
    public string PublisherId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = "Azure SmartCost";
    public string ShortDescription { get; set; } = "Intelligent Azure cost optimization and analytics platform";
    public string LongDescription { get; set; } = string.Empty;
    public List<MarketplacePlan> Plans { get; set; } = new();
    public List<string> Categories { get; set; } = new() { "DevOps", "Management" };
    public List<string> Industries { get; set; } = new() { "Cloud Services" };
    public string SupportUrl { get; set; } = string.Empty;
    public string PrivacyUrl { get; set; } = string.Empty;
    public string TermsOfUseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Marketplace Plan
/// Plano de pricing no Marketplace
/// </summary>
public class MarketplacePlan
{
    public string PlanId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PricingModel { get; set; } = "flat-rate"; // flat-rate, per-user
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public bool IsPrivate { get; set; }
    public List<string> Features { get; set; } = new();
}
