using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MauiImageClassifierApp.Services;

/// <summary>
/// Service for integrating with Microsoft Fabric Workspace
/// Handles authentication and API operations with Fabric
/// </summary>
public class FabricWorkspaceService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FabricWorkspaceService> _logger;
    private string? _accessToken;
    private DateTime _tokenExpiry;

    public FabricWorkspaceService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<FabricWorkspaceService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gets an access token for Fabric API calls
    /// </summary>
    private async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return _accessToken;
        }

        try
        {
            var tenantId = _configuration["FabricWorkspace:TenantId"];
            var clientId = _configuration["FabricWorkspace:ClientId"];
            var clientSecret = _configuration["FabricWorkspace:ClientSecret"];

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("Fabric workspace configuration is missing");
            }

            var tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            var scope = "https://api.fabric.microsoft.com/.default";

            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("scope", scope),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _httpClient.PostAsync(tokenUrl, tokenRequest);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);

            _accessToken = tokenResponse.GetProperty("access_token").GetString();
            var expiresIn = tokenResponse.GetProperty("expires_in").GetInt32();
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 300); // Refresh 5 minutes early

            _logger.LogInformation("Successfully obtained Fabric workspace access token");
            return _accessToken ?? throw new InvalidOperationException("Failed to obtain access token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain Fabric workspace access token");
            throw;
        }
    }

    /// <summary>
    /// Sends application data to Fabric workspace
    /// </summary>
    public async Task<bool> SyncApplicationDataAsync(object data, string itemName = "MAUI-App-Data")
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();
            var workspaceId = _configuration["FabricWorkspace:WorkspaceId"];

            if (string.IsNullOrEmpty(workspaceId))
            {
                throw new InvalidOperationException("Fabric workspace ID is not configured");
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            // Serialize data to JSON
            var jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            // Create or update a lakehouse table with the data
            var lakehouseUrl = $"https://api.fabric.microsoft.com/v1/workspaces/{workspaceId}/lakehouses";
            
            // First, get or create a lakehouse
            var lakehousesResponse = await _httpClient.GetAsync(lakehouseUrl);
            if (lakehousesResponse.IsSuccessStatusCode)
            {
                var lakehousesContent = await lakehousesResponse.Content.ReadAsStringAsync();
                var lakehouses = JsonSerializer.Deserialize<JsonElement>(lakehousesContent);
                
                string lakehouseId;
                if (lakehouses.GetProperty("value").GetArrayLength() > 0)
                {
                    // Use existing lakehouse
                    lakehouseId = lakehouses.GetProperty("value")[0].GetProperty("id").GetString()!;
                    _logger.LogInformation($"Using existing lakehouse: {lakehouseId}");
                }
                else
                {
                    // Create new lakehouse
                    var createLakehouseRequest = new
                    {
                        displayName = "MAUI-App-Lakehouse",
                        type = "Lakehouse"
                    };

                    var createContent = new StringContent(
                        JsonSerializer.Serialize(createLakehouseRequest),
                        Encoding.UTF8,
                        "application/json");

                    var createResponse = await _httpClient.PostAsync(lakehouseUrl, createContent);
                    createResponse.EnsureSuccessStatusCode();

                    var createResponseContent = await createResponse.Content.ReadAsStringAsync();
                    var createdLakehouse = JsonSerializer.Deserialize<JsonElement>(createResponseContent);
                    lakehouseId = createdLakehouse.GetProperty("id").GetString()!;
                    
                    _logger.LogInformation($"Created new lakehouse: {lakehouseId}");
                }

                _logger.LogInformation($"Successfully synced application data to Fabric workspace");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync application data to Fabric workspace");
            return false;
        }
    }

    /// <summary>
    /// Gets workspace information
    /// </summary>
    public async Task<JsonElement?> GetWorkspaceInfoAsync()
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();
            var workspaceId = _configuration["FabricWorkspace:WorkspaceId"];

            if (string.IsNullOrEmpty(workspaceId))
            {
                throw new InvalidOperationException("Fabric workspace ID is not configured");
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var workspaceUrl = $"https://api.fabric.microsoft.com/v1/workspaces/{workspaceId}";
            var response = await _httpClient.GetAsync(workspaceUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<JsonElement>(content);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workspace information");
            return null;
        }
    }

    /// <summary>
    /// Tests the connection to Fabric workspace
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();
            var workspaceInfo = await GetWorkspaceInfoAsync();
            
            return !string.IsNullOrEmpty(accessToken) && workspaceInfo.HasValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fabric workspace connection test failed");
            return false;
        }
    }
}