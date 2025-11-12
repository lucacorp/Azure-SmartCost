using System.Collections.Generic;
using System.Threading.Tasks;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Shared.Interfaces;

/// <summary>
/// Service para integração com Azure Marketplace SaaS Fulfillment API v2
/// </summary>
public interface IMarketplaceService
{
    /// <summary>
    /// Resolve marketplace token para obter detalhes da subscription
    /// Chamado quando usuário clica em "Configure Account" no Azure Portal
    /// </summary>
    Task<ResolvedSubscription> ResolveSubscriptionAsync(string token);

    /// <summary>
    /// Ativa uma subscription no Azure Marketplace
    /// Deve ser chamado após tenant ser criado/configurado
    /// </summary>
    Task<bool> ActivateSubscriptionAsync(string subscriptionId, string planId, int quantity);

    /// <summary>
    /// Atualiza status de uma operation no Marketplace
    /// </summary>
    Task<bool> UpdateOperationStatusAsync(string subscriptionId, string operationId, string status, string? planId = null, int? quantity = null);

    /// <summary>
    /// Processa webhook event do Marketplace
    /// </summary>
    Task ProcessWebhookEventAsync(MarketplaceWebhookEvent webhookEvent);

    /// <summary>
    /// Obtém detalhes de uma subscription do Marketplace
    /// </summary>
    Task<SubscriptionDetails?> GetSubscriptionAsync(string subscriptionId);

    /// <summary>
    /// Lista todas as subscriptions ativas
    /// </summary>
    Task<List<SubscriptionDetails>> ListSubscriptionsAsync();

    /// <summary>
    /// Obtém access token para Marketplace API usando Azure AD
    /// </summary>
    Task<string> GetMarketplaceAccessTokenAsync();

    /// <summary>
    /// Cria ou atualiza MarketplaceSubscription no nosso banco
    /// </summary>
    Task<MarketplaceSubscription> SaveMarketplaceSubscriptionAsync(MarketplaceSubscription subscription);

    /// <summary>
    /// Obtém MarketplaceSubscription pelo ID do Marketplace
    /// </summary>
    Task<MarketplaceSubscription?> GetMarketplaceSubscriptionByIdAsync(string marketplaceSubscriptionId);

    /// <summary>
    /// Obtém MarketplaceSubscription pelo TenantId
    /// </summary>
    Task<MarketplaceSubscription?> GetMarketplaceSubscriptionByTenantIdAsync(string tenantId);
}
