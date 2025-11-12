using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stripe;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Shared.Interfaces;

public interface IStripeService
{
    // Customer Management
    Task<Customer> CreateCustomerAsync(string email, string name, string tenantId);
    Task<Customer> GetCustomerAsync(string customerId);
    Task<Customer> UpdateCustomerAsync(string customerId, Dictionary<string, string> metadata);
    
    // Subscription Management
    Task<Subscription> CreateSubscriptionAsync(string customerId, string priceId, string tenantId);
    Task<Subscription> GetSubscriptionAsync(string subscriptionId);
    Task<Subscription> UpdateSubscriptionAsync(string subscriptionId, string newPriceId);
    Task<Subscription> CancelSubscriptionAsync(string subscriptionId, bool immediately = false);
    Task<Subscription> ReactivateSubscriptionAsync(string subscriptionId);
    
    // Pricing & Products
    Task<List<SubscriptionPlan>> GetAvailablePlansAsync();
    Task<SubscriptionPlan?> GetPlanByTierAsync(string tier);
    
    // Checkout & Portal
    Task<string> CreateCheckoutSessionAsync(string customerId, string priceId, string successUrl, string cancelUrl);
    Task<string> CreateCustomerPortalSessionAsync(string customerId, string returnUrl);
    
    // Invoices
    Task<Invoice> GetInvoiceAsync(string invoiceId);
    Task<List<Invoice>> GetCustomerInvoicesAsync(string customerId, int limit = 10);
    
    // Webhook Processing
    Task<Event> ConstructWebhookEventAsync(string json, string signature, string secret);
    Task ProcessWebhookEventAsync(Event stripeEvent);
}
