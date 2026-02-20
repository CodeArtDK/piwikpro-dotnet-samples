using System.Text.Json;
using Microsoft.JSInterop;
using PiwikPROSamples.BlazorSample.Client.Models;

namespace PiwikPROSamples.BlazorSample.Client.Services;

/// <summary>
/// Service to manage PiwikPRO connection state in browser localStorage
/// </summary>
public class ConnectionStateService : IConnectionStateService
{
    private const string ConnectionsKey = "piwikpro_connections";
    private const string ActiveConnectionKey = "piwikpro_active_connection";
    
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;
    private bool _initialized;

    public ConnectionStateService(IJSRuntime jsRuntime, IConfiguration configuration)
    {
        _jsRuntime = jsRuntime;
        _configuration = configuration;
    }

    /// <summary>
    /// Initializes the connection state by syncing any connection defined in appsettings
    /// to localStorage, and ensuring an active connection is set if connections exist.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        
        try
        {
            var connections = await GetConnectionsInternalAsync();
            var defaultConnection = GetDefaultConnectionFromConfig();
            var needsSave = false;
            
            // Sync appsettings connection if it doesn't already exist
            if (defaultConnection != null)
            {
                var existingMatch = connections.FirstOrDefault(c => 
                    c.BaseUrl.Equals(defaultConnection.BaseUrl, StringComparison.OrdinalIgnoreCase) &&
                    c.ClientId == defaultConnection.ClientId);
                    
                if (existingMatch == null)
                {
                    // Add the appsettings connection if it doesn't exist
                    connections.Add(defaultConnection);
                    needsSave = true;
                }
            }
            
            if (needsSave)
            {
                await SaveConnectionsAsync(connections);
            }
            
            // Ensure an active connection is set if connections exist but none is active
            if (connections.Any())
            {
                var activeId = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", ActiveConnectionKey);
                var activeConnection = connections.FirstOrDefault(c => c.Id == activeId);
                
                if (activeConnection == null)
                {
                    // Set the default connection or the first available connection as active
                    var connectionToActivate = connections.FirstOrDefault(c => c.IsDefault) ?? connections.First();
                    await SetActiveConnectionAsync(connectionToActivate.Id);
                }
            }
            
            _initialized = true;
        }
        catch (Exception ex)
        {
            // Log initialization errors to browser console for debugging
            Console.WriteLine($"ConnectionStateService initialization error: {ex.Message}");
        }
    }

    public async Task<List<PiwikConnection>> GetConnectionsAsync()
    {
        // Ensure initialization is done first
        await InitializeAsync();
        return await GetConnectionsInternalAsync();
    }

    private async Task<List<PiwikConnection>> GetConnectionsInternalAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", ConnectionsKey);
            
            if (string.IsNullOrEmpty(json))
            {
                return new List<PiwikConnection>();
            }

            return JsonSerializer.Deserialize<List<PiwikConnection>>(json) ?? new List<PiwikConnection>();
        }
        catch
        {
            return new List<PiwikConnection>();
        }
    }

    public async Task<PiwikConnection?> GetActiveConnectionAsync()
    {
        try
        {
            // Ensure initialization is done first
            await InitializeAsync();
            
            var activeId = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", ActiveConnectionKey);
            if (string.IsNullOrEmpty(activeId))
            {
                return null;
            }

            var connections = await GetConnectionsInternalAsync();
            return connections.FirstOrDefault(c => c.Id == activeId);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveConnectionAsync(PiwikConnection connection)
    {
        var connections = await GetConnectionsAsync();
        
        var existing = connections.FirstOrDefault(c => c.Id == connection.Id);
        if (existing != null)
        {
            connections.Remove(existing);
        }
        
        connections.Add(connection);
        await SaveConnectionsAsync(connections);

        // If this is the first connection or marked as default, set it as active
        if (connections.Count == 1 || connection.IsDefault)
        {
            await SetActiveConnectionAsync(connection.Id);
        }
    }

    public async Task DeleteConnectionAsync(string connectionId)
    {
        var connections = await GetConnectionsAsync();
        connections.RemoveAll(c => c.Id == connectionId);
        await SaveConnectionsAsync(connections);

        // If we deleted the active connection, clear it
        var activeId = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", ActiveConnectionKey);
        if (activeId == connectionId)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ActiveConnectionKey);
        }
    }

    public async Task SetActiveConnectionAsync(string connectionId)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ActiveConnectionKey, connectionId);
    }

    public async Task<bool> HasActiveConnectionAsync()
    {
        var connection = await GetActiveConnectionAsync();
        return connection != null;
    }

    private async Task SaveConnectionsAsync(List<PiwikConnection> connections)
    {
        var json = JsonSerializer.Serialize(connections);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ConnectionsKey, json);
    }

    private PiwikConnection? GetDefaultConnectionFromConfig()
    {
        var config = _configuration.GetSection("PiwikPRO");
        var baseUrl = config["BaseUrl"];
        var clientId = config["ClientId"];
        var clientSecret = config["ClientSecret"];
        var webSiteId = config["WebSiteId"];

        if (!string.IsNullOrEmpty(baseUrl) && 
            !string.IsNullOrEmpty(clientId) && 
            !string.IsNullOrEmpty(clientSecret))
        {
            return new PiwikConnection
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Default (from appsettings)",
                BaseUrl = baseUrl,
                ClientId = clientId,
                ClientSecret = clientSecret,
                WebSiteId = webSiteId ?? string.Empty,
                IsDefault = true
            };
        }

        return null;
    }
}
