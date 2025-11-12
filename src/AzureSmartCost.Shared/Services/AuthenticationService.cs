using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.IdentityModel.Tokens;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Shared.Services
{
    public interface IAuthenticationService
    {
        Task<LoginResponse> AuthenticateAsync(string accessToken);
        Task<UserInfo?> GetUserInfoAsync(string accessToken);
        string GenerateJwtToken(UserInfo user);
        ClaimsPrincipal? ValidateToken(string token);
        Task<bool> ValidatePermissionAsync(string userId, string permission);
        Task LogUserActionAsync(AuditLog auditLog);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly GraphServiceClient? _graphServiceClient;
        private readonly string _jwtSecret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _tokenExpirationHours;

        public AuthenticationService(ILogger<AuthenticationService> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // JWT Configuration
            _jwtSecret = _configuration["Jwt:Secret"] ?? "SmartCost-Default-Secret-Key-Change-In-Production-2024";
            _issuer = _configuration["Jwt:Issuer"] ?? "AzureSmartCost";
            _audience = _configuration["Jwt:Audience"] ?? "AzureSmartCost-API";
            _tokenExpirationHours = int.TryParse(_configuration["Jwt:ExpirationHours"], out var hours) ? hours : 24;

            // For now, we'll use a simplified approach without Graph API client
            // In production, you would initialize the GraphServiceClient with proper credentials
            _logger.LogInformation("🔐 Authentication service initialized. Issuer: {Issuer}, Audience: {Audience}, TokenExpiration: {Hours}h", 
                _issuer, _audience, _tokenExpirationHours);
        }

        public async Task<LoginResponse> AuthenticateAsync(string accessToken)
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogInformation("🔐 Starting authentication process. OperationId: {OperationId}", operationId);

            try
            {
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Access token is required",
                        Errors = new List<string> { "No access token provided" }
                    };
                }

                // Get user information from Azure AD
                var userInfo = await GetUserInfoAsync(accessToken);
                if (userInfo == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Failed to retrieve user information",
                        Errors = new List<string> { "Invalid or expired access token" }
                    };
                }

                // Assign roles based on business logic
                userInfo.Roles = await AssignUserRolesAsync(userInfo);
                userInfo.Permissions = RolePermissions.GetAllPermissionsForRoles(userInfo.Roles);

                // Generate JWT token
                var jwtToken = GenerateJwtToken(userInfo);
                var expiresAt = DateTime.UtcNow.AddHours(_tokenExpirationHours);

                _logger.LogInformation("✅ Authentication successful for user: {Email}. Roles: [{Roles}]. OperationId: {OperationId}", 
                    userInfo.Email, string.Join(", ", userInfo.Roles), operationId);

                // Log successful authentication
                await LogUserActionAsync(new AuditLog
                {
                    UserId = userInfo.Id,
                    UserEmail = userInfo.Email,
                    Action = "Authentication",
                    Resource = "Login",
                    Details = $"Successful login with roles: {string.Join(", ", userInfo.Roles)}",
                    Success = true
                });

                return new LoginResponse
                {
                    Success = true,
                    Token = jwtToken,
                    ExpiresAt = expiresAt,
                    User = userInfo,
                    Message = "Authentication successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Authentication failed. OperationId: {OperationId}", operationId);
                
                await LogUserActionAsync(new AuditLog
                {
                    UserEmail = "unknown",
                    Action = "Authentication",
                    Resource = "Login",
                    Details = "Authentication attempt failed",
                    Success = false,
                    ErrorMessage = ex.Message
                });

                return new LoginResponse
                {
                    Success = false,
                    Message = "Authentication failed",
                    Errors = new List<string> { "Internal authentication error" }
                };
            }
        }

        public async Task<UserInfo?> GetUserInfoAsync(string accessToken)
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogInformation("📋 Retrieving user information from Azure AD. OperationId: {OperationId}", operationId);

            try
            {
                // For demo purposes, we'll create mock user info based on token validation
                // In production, you would use Microsoft Graph API to get real user data
                
                // Basic token validation (simplified)
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(accessToken))
                {
                    _logger.LogWarning("⚠️ Invalid token format. OperationId: {OperationId}", operationId);
                    return null;
                }

                var jsonToken = handler.ReadJwtToken(accessToken);
                var email = jsonToken.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == "preferred_username")?.Value;
                var name = jsonToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
                var objectId = jsonToken.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
                var tenantId = jsonToken.Claims.FirstOrDefault(c => c.Type == "tid")?.Value;

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(objectId))
                {
                    _logger.LogWarning("⚠️ Required claims not found in token. OperationId: {OperationId}", operationId);
                    return null;
                }

                var userInfo = new UserInfo
                {
                    Id = objectId,
                    Email = email,
                    DisplayName = name ?? email.Split('@')[0],
                    UserPrincipalName = email,
                    TenantId = tenantId,
                    JobTitle = "Cost Analyst", // Mock data
                    Department = "Finance" // Mock data
                };

                _logger.LogInformation("✅ User information retrieved: {Email}. OperationId: {OperationId}", email, operationId);
                return userInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to retrieve user information. OperationId: {OperationId}", operationId);
                return null;
            }
        }

        public string GenerateJwtToken(UserInfo user)
        {
            _logger.LogDebug("🔑 Generating JWT token for user: {Email}", user.Email);

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.DisplayName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("userPrincipalName", user.UserPrincipalName),
                new Claim("tenantId", user.TenantId ?? ""),
                new Claim("department", user.Department ?? ""),
                new Claim("jobTitle", user.JobTitle ?? "")
            };

            // Add roles as claims
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Add permissions as claims
            foreach (var permission in user.Permissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(_tokenExpirationHours),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogDebug("✅ JWT token generated successfully for user: {Email}", user.Email);
            return tokenString;
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Token validation failed: {Error}", ex.Message);
                return null;
            }
        }

        public async Task<bool> ValidatePermissionAsync(string userId, string permission)
        {
            try
            {
                // For demo purposes, we'll implement a simple permission check
                // In production, you would check permissions from database or cache
                
                _logger.LogDebug("🔍 Validating permission '{Permission}' for user: {UserId}", permission, userId);
                
                // Mock validation - in real implementation, you'd check user's actual permissions
                await Task.Delay(10); // Simulate async operation
                
                return true; // For demo, allow all permissions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Permission validation failed for user: {UserId}, permission: {Permission}", userId, permission);
                return false;
            }
        }

        public async Task LogUserActionAsync(AuditLog auditLog)
        {
            try
            {
                // For now, just log to the application logger
                // In production, you would save to database or audit service
                
                var logLevel = auditLog.Success ? LogLevel.Information : LogLevel.Warning;
                var status = auditLog.Success ? "✅" : "❌";
                
                _logger.Log(logLevel, "{Status} Audit: User {Email} performed {Action} on {Resource} at {Timestamp}. Success: {Success}", 
                    status, auditLog.UserEmail, auditLog.Action, auditLog.Resource, auditLog.Timestamp, auditLog.Success);

                if (!auditLog.Success && !string.IsNullOrWhiteSpace(auditLog.ErrorMessage))
                {
                    _logger.LogWarning("Audit Error Details: {ErrorMessage}", auditLog.ErrorMessage);
                }

                await Task.CompletedTask; // Simulate async operation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to log audit entry for user: {Email}", auditLog.UserEmail);
            }
        }

        private async Task<List<string>> AssignUserRolesAsync(UserInfo user)
        {
            var operationId = Guid.NewGuid().ToString();
            _logger.LogDebug("👥 Assigning roles for user: {Email}. OperationId: {OperationId}", user.Email, operationId);

            // Mock role assignment logic based on email domain or department
            // In production, you would fetch roles from Azure AD groups or database
            
            var roles = new List<string>();
            
            try
            {
                // Demo role assignment logic
                var emailDomain = user.Email.Split('@').LastOrDefault()?.ToLowerInvariant();
                
                if (emailDomain == "admin.company.com" || user.Email.ToLowerInvariant().Contains("admin"))
                {
                    roles.Add(Roles.Admin);
                }
                else if (user.Department?.ToLowerInvariant().Contains("finance") == true || 
                         user.JobTitle?.ToLowerInvariant().Contains("manager") == true)
                {
                    roles.Add(Roles.FinanceManager);
                }
                else if (user.JobTitle?.ToLowerInvariant().Contains("analyst") == true)
                {
                    roles.Add(Roles.CostAnalyst);
                }
                else
                {
                    roles.Add(Roles.Viewer); // Default role
                }

                // Special audit role for specific users
                if (user.JobTitle?.ToLowerInvariant().Contains("audit") == true)
                {
                    roles.Add(Roles.AuditReader);
                }

                _logger.LogInformation("👥 Assigned roles to {Email}: [{Roles}]. OperationId: {OperationId}", 
                    user.Email, string.Join(", ", roles), operationId);

                await Task.CompletedTask; // Simulate async operation
                return roles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error assigning roles to user: {Email}. OperationId: {OperationId}", user.Email, operationId);
                return new List<string> { Roles.Viewer }; // Fallback to viewer role
            }
        }
    }
}