using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AzureSmartCost.Shared.Services;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Login endpoint - authenticates user and returns JWT token
        /// </summary>
        /// <param name="request">Login credentials with Azure AD access token</param>
        /// <returns>Authentication response with JWT token</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.AccessToken))
                {
                    return BadRequest(new { message = "Azure AD access token is required" });
                }

                var response = await _authService.AuthenticateAsync(request.AccessToken);
                
                if (!response.Success)
                {
                    return Unauthorized(new { message = response.Message });
                }

                _logger.LogInformation("User authenticated successfully with Azure AD token");
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication");
                return StatusCode(500, new { message = "Internal server error during authentication" });
            }
        }

        /// <summary>
        /// Get current user information from JWT token
        /// </summary>
        /// <returns>Current user information</returns>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserInfo>> GetCurrentUser()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "Invalid token format" });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var userInfo = await _authService.GetUserInfoAsync(token);

                if (userInfo == null)
                {
                    return Unauthorized(new { message = "Invalid or expired token" });
                }

                return Ok(userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user information");
                return StatusCode(500, new { message = "Error retrieving user information" });
            }
        }

        /// <summary>
        /// Validate user permission for specific action
        /// </summary>
        /// <param name="permission">Permission to validate</param>
        /// <returns>Permission validation result</returns>
        [HttpGet("validate-permission")]
        [Authorize]
        public async Task<ActionResult<bool>> ValidatePermission([FromQuery] string permission)
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "Invalid token format" });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var hasPermission = await _authService.ValidatePermissionAsync(token, permission);

                return Ok(new { permission, hasPermission });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating permission {Permission}", permission);
                return StatusCode(500, new { message = "Error validating permission" });
            }
        }

        /// <summary>
        /// Get all available roles and their permissions (Admin only)
        /// </summary>
        /// <returns>Roles and permissions information</returns>
        [HttpGet("roles")]
        [Authorize(Policy = "AdminOnly")]
        public ActionResult<object> GetRolesAndPermissions()
        {
            try
            {
                var rolesInfo = new
                {
                    Roles = new[]
                    {
                        new { Role = Roles.Admin, Description = "Full system access and user management" },
                        new { Role = Roles.FinanceManager, Description = "Finance team management and cost optimization" },
                        new { Role = Roles.CostAnalyst, Description = "Cost analysis and reporting capabilities" },
                        new { Role = Roles.Viewer, Description = "Read-only access to dashboards and reports" },
                        new { Role = Roles.AuditReader, Description = "Access to audit logs and compliance reports" }
                    },
                    Permissions = new[]
                    {
                        new { Permission = Permissions.ReadCosts, Description = "View cost data and analytics" },
                        new { Permission = Permissions.WriteCosts, Description = "Modify cost data and settings" },
                        new { Permission = Permissions.DeleteCosts, Description = "Delete cost records" },
                        new { Permission = Permissions.ManageThresholds, Description = "Configure cost thresholds and alerts" },
                        new { Permission = Permissions.ReadReports, Description = "View cost reports" },
                        new { Permission = Permissions.ExportReports, Description = "Export cost data and reports" },
                        new { Permission = Permissions.ManageUsers, Description = "User management and role assignment" },
                        new { Permission = Permissions.ViewAuditLogs, Description = "Access to audit logs" },
                        new { Permission = Permissions.ConfigureDashboard, Description = "Configure dashboard settings" },
                        new { Permission = Permissions.ManageSystem, Description = "System configuration and settings" },
                        new { Permission = Permissions.CreateReports, Description = "API access and integration" },
                        new { Permission = Permissions.ReadDashboard, Description = "Advanced analytics and forecasting" }
                    },
                    RolePermissions = new
                    {
                        Admin = RolePermissions.GetPermissionsForRole(Roles.Admin),
                        FinanceManager = RolePermissions.GetPermissionsForRole(Roles.FinanceManager),
                        CostAnalyst = RolePermissions.GetPermissionsForRole(Roles.CostAnalyst),
                        Viewer = RolePermissions.GetPermissionsForRole(Roles.Viewer),
                        AuditReader = RolePermissions.GetPermissionsForRole(Roles.AuditReader)
                    }
                };

                return Ok(rolesInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles and permissions");
                return StatusCode(500, new { message = "Error retrieving roles and permissions" });
            }
        }

        /// <summary>
        /// Health check endpoint for authentication service
        /// </summary>
        /// <returns>Authentication service health status</returns>
        [HttpGet("health")]
        [AllowAnonymous]
        public ActionResult<object> GetAuthHealth()
        {
            try
            {
                return Ok(new
                {
                    Service = "Authentication",
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Features = new[]
                    {
                        "JWT Authentication",
                        "Role-Based Access Control",
                        "Azure AD Integration",
                        "Permission Validation",
                        "Audit Logging"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking authentication health");
                return StatusCode(500, new { message = "Authentication service unhealthy" });
            }
        }
    }
}