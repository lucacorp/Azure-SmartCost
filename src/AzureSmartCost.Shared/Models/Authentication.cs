using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using System;
using System.Collections.Generic;
using System.Linq;

namespace AzureSmartCost.Shared.Models
{
    // User information from Azure AD
    public class UserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;
        
        [JsonPropertyName("mail")]
        public string Email { get; set; } = string.Empty;
        
        [JsonPropertyName("userPrincipalName")]
        public string UserPrincipalName { get; set; } = string.Empty;
        
        [JsonPropertyName("jobTitle")]
        public string? JobTitle { get; set; }
        
        [JsonPropertyName("department")]
        public string? Department { get; set; }
        
        [JsonPropertyName("roles")]
        public List<string> Roles { get; set; } = new List<string>();
        
        [JsonPropertyName("permissions")]
        public List<string> Permissions { get; set; } = new List<string>();
        
        [JsonPropertyName("tenantId")]
        public string? TenantId { get; set; }
    }

    // Login request model
    public class LoginRequest
    {
        [Required]
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;
    }

    // Login response model
    public class LoginResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("token")]
        public string? Token { get; set; }
        
        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }
        
        [JsonPropertyName("expiresAt")]
        public DateTime? ExpiresAt { get; set; }
        
        [JsonPropertyName("user")]
        public UserInfo? User { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        
        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new List<string>();
    }

    // JWT Claims model
    public class JwtClaims
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public string TenantId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    // Role definitions for RBAC
    public static class Roles
    {
        public const string Admin = "SmartCost.Admin";
        public const string FinanceManager = "SmartCost.FinanceManager";
        public const string CostAnalyst = "SmartCost.CostAnalyst";
        public const string Viewer = "SmartCost.Viewer";
        public const string AuditReader = "SmartCost.AuditReader";
    }

    // Permission definitions
    public static class Permissions
    {
        // Cost Management
        public const string ReadCosts = "costs:read";
        public const string WriteCosts = "costs:write";
        public const string DeleteCosts = "costs:delete";
        
        // Alerts
        public const string ReadAlerts = "alerts:read";
        public const string WriteAlerts = "alerts:write";
        public const string DeleteAlerts = "alerts:delete";
        public const string ManageThresholds = "alerts:manage-thresholds";
        
        // Dashboard
        public const string ReadDashboard = "dashboard:read";
        public const string ConfigureDashboard = "dashboard:configure";
        
        // System Administration
        public const string ManageUsers = "system:manage-users";
        public const string ViewAuditLogs = "system:view-audit-logs";
        public const string ManageSystem = "system:manage";
        
        // Reports
        public const string ReadReports = "reports:read";
        public const string CreateReports = "reports:create";
        public const string ExportReports = "reports:export";
    }

    // Role-Permission mappings
    public static class RolePermissions
    {
        public static readonly Dictionary<string, List<string>> Mappings = new()
        {
            [Roles.Admin] = new List<string>
            {
                Permissions.ReadCosts, Permissions.WriteCosts, Permissions.DeleteCosts,
                Permissions.ReadAlerts, Permissions.WriteAlerts, Permissions.DeleteAlerts, Permissions.ManageThresholds,
                Permissions.ReadDashboard, Permissions.ConfigureDashboard,
                Permissions.ManageUsers, Permissions.ViewAuditLogs, Permissions.ManageSystem,
                Permissions.ReadReports, Permissions.CreateReports, Permissions.ExportReports
            },
            
            [Roles.FinanceManager] = new List<string>
            {
                Permissions.ReadCosts, Permissions.WriteCosts,
                Permissions.ReadAlerts, Permissions.WriteAlerts, Permissions.ManageThresholds,
                Permissions.ReadDashboard, Permissions.ConfigureDashboard,
                Permissions.ReadReports, Permissions.CreateReports, Permissions.ExportReports
            },
            
            [Roles.CostAnalyst] = new List<string>
            {
                Permissions.ReadCosts,
                Permissions.ReadAlerts, Permissions.WriteAlerts,
                Permissions.ReadDashboard,
                Permissions.ReadReports, Permissions.CreateReports
            },
            
            [Roles.Viewer] = new List<string>
            {
                Permissions.ReadCosts,
                Permissions.ReadAlerts,
                Permissions.ReadDashboard,
                Permissions.ReadReports
            },
            
            [Roles.AuditReader] = new List<string>
            {
                Permissions.ReadCosts,
                Permissions.ReadAlerts,
                Permissions.ReadDashboard,
                Permissions.ViewAuditLogs,
                Permissions.ReadReports
            }
        };

        public static List<string> GetPermissionsForRole(string role)
        {
            return Mappings.TryGetValue(role, out var permissions) ? permissions : new List<string>();
        }

        public static List<string> GetAllPermissionsForRoles(IEnumerable<string> roles)
        {
            return roles.SelectMany(GetPermissionsForRole).Distinct().ToList();
        }
    }

    // Audit log model for tracking user actions
    public class AuditLog
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;
        
        [JsonPropertyName("userEmail")]
        public string UserEmail { get; set; } = string.Empty;
        
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;
        
        [JsonPropertyName("resource")]
        public string Resource { get; set; } = string.Empty;
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [JsonPropertyName("ipAddress")]
        public string? IpAddress { get; set; }
        
        [JsonPropertyName("userAgent")]
        public string? UserAgent { get; set; }
        
        [JsonPropertyName("details")]
        public string? Details { get; set; }
        
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;
        
        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }
}