using PiwikPRO.Analytics.Services;
using Microsoft.Extensions.Options;
using PiwikPRO.Core;
using AppModel = PiwikPRO.Analytics.Models.Apps.App;

namespace PiwikPROSamples.BlazorSample.Client.Services;

/// <summary>
/// Interface for app selection service
/// </summary>
public interface IAppSelectorService
{
    /// <summary>
    /// Event raised when the selected app changes
    /// </summary>
    event Action? OnAppChanged;

    /// <summary>
    /// Get all available apps
    /// </summary>
    Task<List<AppModel>> GetAppsAsync();

    /// <summary>
    /// Get the currently selected app ID
    /// </summary>
    string? GetSelectedAppId();

    /// <summary>
    /// Set the selected app by ID
    /// </summary>
    Task SetSelectedAppAsync(string? appId);

    /// <summary>
    /// Initialize the service (loads apps from API)
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Get whether the service has been initialized
    /// </summary>
    bool IsInitialized { get; }
}

/// <summary>
/// Service for managing app selection in the UI
/// </summary>
public class AppSelectorService : IAppSelectorService
{
    private readonly IAppsService _appsService;
    private readonly PiwikProConfiguration _configuration;
    private List<AppModel> _apps = new();
    private string? _selectedAppId;
    private bool _isInitialized;

    public event Action? OnAppChanged;

    public bool IsInitialized => _isInitialized;

    public AppSelectorService(IAppsService appsService, IOptions<PiwikProConfiguration> configuration)
    {
        _appsService = appsService;
        _configuration = configuration.Value;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            // Fetch all apps (using a large limit to get all apps)
            var response = await _appsService.GetAppsAsync(offset: 0, limit: 1000, sort: "name");
            if (response?.Data != null)
            {
                _apps = response.Data;
                
                // If default WebSiteId is configured and exists in the apps list, preselect it
                if (!string.IsNullOrWhiteSpace(_configuration.WebSiteId))
                {
                    var defaultApp = _apps.FirstOrDefault(a => a.Id == _configuration.WebSiteId);
                    if (defaultApp != null)
                    {
                        _selectedAppId = _configuration.WebSiteId;
                    }
                }
                
                _isInitialized = true;
            }
        }
        catch
        {
            // If fetching apps fails (e.g., no connection configured), keep empty list
            _apps = new();
            _isInitialized = true;
        }
    }

    public Task<List<AppModel>> GetAppsAsync()
    {
        if (!_isInitialized)
            return InitializeAndReturnAppsAsync();

        return Task.FromResult(_apps);
    }

    private async Task<List<AppModel>> InitializeAndReturnAppsAsync()
    {
        await InitializeAsync();
        return _apps;
    }

    public string? GetSelectedAppId()
    {
        return _selectedAppId;
    }

    public Task SetSelectedAppAsync(string? appId)
    {
        if (_selectedAppId != appId)
        {
            _selectedAppId = appId;
            OnAppChanged?.Invoke();
        }

        return Task.CompletedTask;
    }
}
