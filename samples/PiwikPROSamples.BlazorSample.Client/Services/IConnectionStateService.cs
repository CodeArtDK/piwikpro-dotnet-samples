namespace PiwikPROSamples.BlazorSample.Client.Services;

/// <summary>
/// Service to manage PiwikPRO connection state in browser storage
/// </summary>
public interface IConnectionStateService
{
    Task<List<Models.PiwikConnection>> GetConnectionsAsync();
    Task<Models.PiwikConnection?> GetActiveConnectionAsync();
    Task SaveConnectionAsync(Models.PiwikConnection connection);
    Task DeleteConnectionAsync(string connectionId);
    Task SetActiveConnectionAsync(string connectionId);
    Task<bool> HasActiveConnectionAsync();
    
    /// <summary>
    /// Initializes the connection state by syncing any connection defined in appsettings
    /// to localStorage, and ensuring an active connection is set if connections exist.
    /// Should be called on application startup.
    /// </summary>
    Task InitializeAsync();
}
