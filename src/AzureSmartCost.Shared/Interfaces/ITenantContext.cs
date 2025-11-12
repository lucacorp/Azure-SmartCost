using System;

namespace AzureSmartCost.Shared.Interfaces;

/// <summary>
/// Contexto do tenant atual na requisição (scoped per request)
/// </summary>
public interface ITenantContext
{
    string? TenantId { get; set; }
    string? UserId { get; set; }
    string? UserEmail { get; set; }
    string? UserRole { get; set; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}

public class TenantContext : ITenantContext
{
    public string? TenantId { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
    public bool IsAdmin => UserRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false;
}
