using System;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzureSmartCost.Shared.Interfaces;

namespace AzureSmartCost.Shared.Services.Implementation;

/// <summary>
/// Azure Key Vault service implementation for secure secret management
/// Uses DefaultAzureCredential for authentication (supports Managed Identity in production)
/// </summary>
public class KeyVaultService : IKeyVaultService
{
    private readonly SecretClient? _secretClient;
    private readonly ILogger<KeyVaultService> _logger;
    private readonly bool _isConfigured;

    public KeyVaultService(IConfiguration configuration, ILogger<KeyVaultService> logger)
    {
        _logger = logger;

        var keyVaultUrl = configuration["KeyVault:VaultUrl"];
        
        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            _logger.LogWarning("Key Vault URL not configured. Secret retrieval will fail.");
            _isConfigured = false;
            return;
        }

        try
        {
            // DefaultAzureCredential will try multiple authentication methods:
            // 1. EnvironmentCredential (dev with env vars)
            // 2. ManagedIdentityCredential (production in Azure)
            // 3. VisualStudioCredential (local dev)
            // 4. AzureCliCredential (local dev with az login)
            var credential = new DefaultAzureCredential();
            _secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
            _isConfigured = true;

            _logger.LogInformation("Key Vault client initialized successfully. Vault URL: {VaultUrl}", keyVaultUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Key Vault client. Vault URL: {VaultUrl}", keyVaultUrl);
            _isConfigured = false;
        }
    }

    public async Task<string> GetSecretAsync(string secretName)
    {
        if (!_isConfigured || _secretClient == null)
        {
            throw new InvalidOperationException("Key Vault is not properly configured. Check VaultUrl in appsettings.json");
        }

        try
        {
            _logger.LogDebug("Retrieving secret '{SecretName}' from Key Vault", secretName);
            
            KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName);
            
            _logger.LogDebug("Successfully retrieved secret '{SecretName}'", secretName);
            
            return secret.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogError("Secret '{SecretName}' not found in Key Vault", secretName);
            throw new InvalidOperationException($"Secret '{secretName}' not found in Key Vault", ex);
        }
        catch (RequestFailedException ex) when (ex.Status == 403)
        {
            _logger.LogError("Access denied to secret '{SecretName}'. Check Key Vault access policies or RBAC roles", secretName);
            throw new UnauthorizedAccessException($"Access denied to secret '{secretName}'. Check access policies.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret '{SecretName}' from Key Vault", secretName);
            throw;
        }
    }

    public async Task<bool> IsConfiguredAsync()
    {
        if (!_isConfigured || _secretClient == null)
        {
            return false;
        }

        try
        {
            // Try to list secrets to verify access (doesn't retrieve values)
            await foreach (var _ in _secretClient.GetPropertiesOfSecretsAsync())
            {
                // If we can enumerate at least one secret, Key Vault is accessible
                return true;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Key Vault health check failed");
            return false;
        }
    }
}
