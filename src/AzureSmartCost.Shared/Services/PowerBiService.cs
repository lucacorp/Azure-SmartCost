using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzureSmartCost.Shared.Models;

namespace AzureSmartCost.Shared.Services
{
    /// <summary>
    /// Interface for Power BI integration service
    /// </summary>
    public interface IPowerBiService
    {
        Task<PowerBiEmbedConfig> GetEmbedTokenAsync(string reportId, string workspaceId);
        Task<bool> RefreshDatasetAsync(string datasetId, string workspaceId);
        Task<PowerBiDataset> CreateDatasetAsync(PowerBiDataset dataset, string workspaceId);
        Task<bool> UpdateDatasetAsync(string datasetId, PowerBiDataset dataset, string workspaceId);
        Task<List<dynamic>> GetDatasetDataAsync(string datasetId, string tableName, string workspaceId);
        Task<bool> PostDataToDatasetAsync(string datasetId, string tableName, object data, string workspaceId);
        Task<string> GetAccessTokenAsync();
    }

    /// <summary>
    /// Service for integrating with Power BI REST API
    /// </summary>
    public class PowerBiService : IPowerBiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PowerBiService> _logger;
        private const string PowerBiApiBaseUrl = "https://api.powerbi.com/v1.0/myorg";

        public PowerBiService(HttpClient httpClient, IConfiguration configuration, ILogger<PowerBiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Get embed token for Power BI report
        /// </summary>
        public async Task<PowerBiEmbedConfig> GetEmbedTokenAsync(string reportId, string workspaceId)
        {
            try
            {
                _logger.LogInformation("🔐 Getting Power BI embed token for report {ReportId} in workspace {WorkspaceId}", reportId, workspaceId);

                // Get access token
                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new InvalidOperationException("Failed to obtain Power BI access token");
                }

                // Prepare embed token request
                var embedTokenRequest = new
                {
                    reports = new[]
                    {
                        new { id = reportId }
                    },
                    datasets = new[]
                    {
                        new { id = _configuration["PowerBI:DatasetId"] ?? "default-dataset-id" }
                    },
                    targetWorkspaces = new[]
                    {
                        new { id = workspaceId }
                    }
                };

                // Call Power BI API to get embed token
                var requestContent = new StringContent(
                    JsonSerializer.Serialize(embedTokenRequest),
                    Encoding.UTF8,
                    "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.PostAsync(
                    $"{PowerBiApiBaseUrl}/groups/{workspaceId}/reports/{reportId}/GenerateToken",
                    requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Failed to get embed token. Status: {StatusCode}, Error: {Error}", 
                        response.StatusCode, errorContent);
                    
                    // Return fallback configuration for development
                    return GetFallbackEmbedConfig(reportId);
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var embedToken = tokenResponse.GetProperty("token").GetString();
                var embedUrl = $"https://app.powerbi.com/reportEmbed?reportId={reportId}&groupId={workspaceId}";

                var embedConfig = new PowerBiEmbedConfig
                {
                    Type = "report",
                    Id = reportId,
                    EmbedUrl = embedUrl,
                    AccessToken = embedToken ?? "",
                    TokenType = "Embed",
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    Settings = new PowerBiEmbedSettings
                    {
                        FilterPaneEnabled = true,
                        NavContentPaneEnabled = true,
                        Background = "transparent",
                        BookmarksPane = new PowerBiPane { Visible = false },
                        FieldsPane = new PowerBiPane { Visible = false }
                    }
                };

                _logger.LogInformation("✅ Successfully generated Power BI embed token");
                return embedConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting Power BI embed token");
                return GetFallbackEmbedConfig(reportId);
            }
        }

        /// <summary>
        /// Refresh Power BI dataset
        /// </summary>
        public async Task<bool> RefreshDatasetAsync(string datasetId, string workspaceId)
        {
            try
            {
                _logger.LogInformation("🔄 Refreshing Power BI dataset {DatasetId} in workspace {WorkspaceId}", datasetId, workspaceId);

                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("❌ Failed to obtain access token for dataset refresh");
                    return false;
                }

                var refreshRequest = new PowerBiRefreshRequest
                {
                    DatasetId = datasetId,
                    RefreshType = "Full",
                    CommitMode = "Transactional",
                    MaxParallelism = 2,
                    RetryCount = 0
                };

                var requestContent = new StringContent(
                    JsonSerializer.Serialize(refreshRequest),
                    Encoding.UTF8,
                    "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.PostAsync(
                    $"{PowerBiApiBaseUrl}/groups/{workspaceId}/datasets/{datasetId}/refreshes",
                    requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Failed to refresh dataset. Status: {StatusCode}, Error: {Error}", 
                        response.StatusCode, errorContent);
                    return false;
                }

                _logger.LogInformation("✅ Successfully initiated Power BI dataset refresh");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error refreshing Power BI dataset");
                return false;
            }
        }

        /// <summary>
        /// Create new Power BI dataset
        /// </summary>
        public async Task<PowerBiDataset> CreateDatasetAsync(PowerBiDataset dataset, string workspaceId)
        {
            try
            {
                _logger.LogInformation("📊 Creating Power BI dataset {DatasetName} in workspace {WorkspaceId}", 
                    dataset.Name, workspaceId);

                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new InvalidOperationException("Failed to obtain Power BI access token");
                }

                var requestContent = new StringContent(
                    JsonSerializer.Serialize(dataset),
                    Encoding.UTF8,
                    "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.PostAsync(
                    $"{PowerBiApiBaseUrl}/groups/{workspaceId}/datasets",
                    requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Failed to create dataset. Status: {StatusCode}, Error: {Error}", 
                        response.StatusCode, errorContent);
                    throw new InvalidOperationException($"Failed to create Power BI dataset: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var createdDataset = JsonSerializer.Deserialize<PowerBiDataset>(responseContent);

                _logger.LogInformation("✅ Successfully created Power BI dataset {DatasetId}", createdDataset?.Id);
                return createdDataset ?? dataset;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating Power BI dataset");
                throw;
            }
        }

        /// <summary>
        /// Update existing Power BI dataset
        /// </summary>
        public async Task<bool> UpdateDatasetAsync(string datasetId, PowerBiDataset dataset, string workspaceId)
        {
            try
            {
                _logger.LogInformation("🔧 Updating Power BI dataset {DatasetId} in workspace {WorkspaceId}", 
                    datasetId, workspaceId);

                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("❌ Failed to obtain access token for dataset update");
                    return false;
                }

                var requestContent = new StringContent(
                    JsonSerializer.Serialize(dataset),
                    Encoding.UTF8,
                    "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.PatchAsync(
                    $"{PowerBiApiBaseUrl}/groups/{workspaceId}/datasets/{datasetId}",
                    requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Failed to update dataset. Status: {StatusCode}, Error: {Error}", 
                        response.StatusCode, errorContent);
                    return false;
                }

                _logger.LogInformation("✅ Successfully updated Power BI dataset");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating Power BI dataset");
                return false;
            }
        }

        /// <summary>
        /// Get data from Power BI dataset table
        /// </summary>
        public async Task<List<dynamic>> GetDatasetDataAsync(string datasetId, string tableName, string workspaceId)
        {
            try
            {
                _logger.LogInformation("📥 Getting data from Power BI dataset {DatasetId}, table {TableName}", 
                    datasetId, tableName);

                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("❌ Failed to obtain access token for data retrieval");
                    return new List<dynamic>();
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.GetAsync(
                    $"{PowerBiApiBaseUrl}/groups/{workspaceId}/datasets/{datasetId}/tables/{tableName}/rows");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Failed to get dataset data. Status: {StatusCode}, Error: {Error}", 
                        response.StatusCode, errorContent);
                    return new List<dynamic>();
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var dataResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                var rows = dataResponse.GetProperty("value").EnumerateArray().ToList();
                var result = new List<dynamic>();
                
                foreach (var row in rows)
                {
                    result.Add(row);
                }

                _logger.LogInformation("✅ Successfully retrieved {RowCount} rows from Power BI dataset", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting Power BI dataset data");
                return new List<dynamic>();
            }
        }

        /// <summary>
        /// Post data to Power BI dataset table
        /// </summary>
        public async Task<bool> PostDataToDatasetAsync(string datasetId, string tableName, object data, string workspaceId)
        {
            try
            {
                _logger.LogInformation("📤 Posting data to Power BI dataset {DatasetId}, table {TableName}", 
                    datasetId, tableName);

                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("❌ Failed to obtain access token for data posting");
                    return false;
                }

                var requestContent = new StringContent(
                    JsonSerializer.Serialize(new { rows = data }),
                    Encoding.UTF8,
                    "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.PostAsync(
                    $"{PowerBiApiBaseUrl}/groups/{workspaceId}/datasets/{datasetId}/tables/{tableName}/rows",
                    requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Failed to post data to dataset. Status: {StatusCode}, Error: {Error}", 
                        response.StatusCode, errorContent);
                    return false;
                }

                _logger.LogInformation("✅ Successfully posted data to Power BI dataset");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error posting data to Power BI dataset");
                return false;
            }
        }

        /// <summary>
        /// Get Power BI access token using service principal
        /// </summary>
        public async Task<string> GetAccessTokenAsync()
        {
            try
            {
                var clientId = _configuration["PowerBI:ClientId"];
                var clientSecret = _configuration["PowerBI:ClientSecret"];
                var tenantId = _configuration["PowerBI:TenantId"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId))
                {
                    _logger.LogWarning("⚠️ Power BI credentials not configured, using fallback mode");
                    return "fallback-token"; // For development/testing
                }

                var tokenRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("scope", "https://analysis.windows.net/powerbi/api/.default")
                });

                var response = await _httpClient.PostAsync(
                    $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
                    tokenRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ Failed to get access token. Status: {StatusCode}, Error: {Error}", 
                        response.StatusCode, errorContent);
                    return "";
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var accessToken = tokenResponse.GetProperty("access_token").GetString();

                _logger.LogInformation("✅ Successfully obtained Power BI access token");
                return accessToken ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting Power BI access token");
                return "";
            }
        }

        /// <summary>
        /// Get fallback embed configuration for development
        /// </summary>
        private PowerBiEmbedConfig GetFallbackEmbedConfig(string reportId)
        {
            _logger.LogInformation("🔧 Using fallback Power BI embed configuration");
            
            return new PowerBiEmbedConfig
            {
                Type = "report",
                Id = reportId,
                EmbedUrl = $"https://app.powerbi.com/reportEmbed?reportId={reportId}",
                AccessToken = "fallback-token-for-development",
                TokenType = "Embed",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Settings = new PowerBiEmbedSettings
                {
                    FilterPaneEnabled = true,
                    NavContentPaneEnabled = true,
                    Background = "transparent",
                    BookmarksPane = new PowerBiPane { Visible = false },
                    FieldsPane = new PowerBiPane { Visible = false }
                }
            };
        }
    }

    /// <summary>
    /// Mock Power BI service for development and testing
    /// </summary>
    public class MockPowerBiService : IPowerBiService
    {
        private readonly ILogger<MockPowerBiService> _logger;

        public MockPowerBiService(ILogger<MockPowerBiService> logger)
        {
            _logger = logger;
        }

        public async Task<PowerBiEmbedConfig> GetEmbedTokenAsync(string reportId, string workspaceId)
        {
            _logger.LogInformation("🔧 Mock: Getting embed token for report {ReportId}", reportId);
            await Task.Delay(100); // Simulate API call
            
            return SmartCostPowerBiTemplates.GetSmartCostEmbedConfig(
                reportId, 
                $"https://app.powerbi.com/reportEmbed?reportId={reportId}", 
                "mock-embed-token");
        }

        public async Task<bool> RefreshDatasetAsync(string datasetId, string workspaceId)
        {
            _logger.LogInformation("🔧 Mock: Refreshing dataset {DatasetId}", datasetId);
            await Task.Delay(100);
            return true;
        }

        public async Task<PowerBiDataset> CreateDatasetAsync(PowerBiDataset dataset, string workspaceId)
        {
            _logger.LogInformation("🔧 Mock: Creating dataset {DatasetName}", dataset.Name);
            await Task.Delay(100);
            dataset.Id = Guid.NewGuid().ToString();
            return dataset;
        }

        public async Task<bool> UpdateDatasetAsync(string datasetId, PowerBiDataset dataset, string workspaceId)
        {
            _logger.LogInformation("🔧 Mock: Updating dataset {DatasetId}", datasetId);
            await Task.Delay(100);
            return true;
        }

        public async Task<List<dynamic>> GetDatasetDataAsync(string datasetId, string tableName, string workspaceId)
        {
            _logger.LogInformation("🔧 Mock: Getting data from table {TableName}", tableName);
            await Task.Delay(100);
            return new List<dynamic>();
        }

        public async Task<bool> PostDataToDatasetAsync(string datasetId, string tableName, object data, string workspaceId)
        {
            _logger.LogInformation("🔧 Mock: Posting data to table {TableName}", tableName);
            await Task.Delay(100);
            return true;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            _logger.LogInformation("🔧 Mock: Getting access token");
            await Task.Delay(100);
            return "mock-access-token";
        }
    }
}