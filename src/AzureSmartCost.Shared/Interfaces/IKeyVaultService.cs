using System.Threading.Tasks;

namespace AzureSmartCost.Shared.Interfaces;

/// <summary>
/// Service for securely retrieving secrets from Azure Key Vault
/// </summary>
public interface IKeyVaultService
{
    /// <summary>
    /// Retrieves a secret value from Azure Key Vault
    /// </summary>
    /// <param name="secretName">The name of the secret to retrieve</param>
    /// <returns>The secret value</returns>
    Task<string> GetSecretAsync(string secretName);

    /// <summary>
    /// Checks if Key Vault is properly configured and accessible
    /// </summary>
    /// <returns>True if Key Vault is accessible, false otherwise</returns>
    Task<bool> IsConfiguredAsync();
}
