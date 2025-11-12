using System;
using System.Collections.Generic;

namespace AzureSmartCost.Shared.Models;

/// <summary>
/// Usuário pertencente a um Tenant específico
/// </summary>
public class TenantUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty; // FK to Tenant
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty; // Display name from Azure AD
    public string FullName => !string.IsNullOrEmpty(Name) ? Name : $"{FirstName} {LastName}";
    
    // Role-based access
    public string Role { get; set; } = "User"; // Admin, Manager, User, Viewer
    public List<string> Permissions { get; set; } = new();
    
    // Status
    public bool IsActive { get; set; } = true;
    public bool EmailVerified { get; set; } = false;
    public DateTime? LastLoginAt { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    
    // Azure AD SSO
    public string? AzureAdUserId { get; set; } // Azure AD Object ID
    public string? AzureAdObjectId { get; set; } // Deprecated, use AzureAdUserId
    public List<string> AzureAdGroups { get; set; } = new(); // Cached group IDs
    public DateTime? LastSyncedAt { get; set; } // Last time groups were synced
    
    // SAML SSO
    public string? SamlNameId { get; set; }
    public string? SamlIssuer { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
