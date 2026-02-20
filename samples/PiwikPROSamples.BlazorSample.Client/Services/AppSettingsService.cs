using Microsoft.JSInterop;

namespace PiwikPROSamples.BlazorSample.Client.Services;

/// <summary>
/// Service to manage app-level settings stored in browser localStorage
/// </summary>
public interface IAppSettingsService
{
    Task<int> GetServerTimezoneOffsetAsync();
    Task SetServerTimezoneOffsetAsync(int offset);
    event Action? OnSettingsChanged;
}

public class AppSettingsService : IAppSettingsService
{
    private const string TimezoneOffsetKey = "piwikpro_server_timezone_offset";
    
    private readonly IJSRuntime _jsRuntime;
    private int _cachedTimezoneOffset = 0;
    private bool _isInitialized = false;

    public event Action? OnSettingsChanged;

    public AppSettingsService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<int> GetServerTimezoneOffsetAsync()
    {
        if (_isInitialized)
        {
            return _cachedTimezoneOffset;
        }

        try
        {
            var value = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TimezoneOffsetKey);
            if (!string.IsNullOrEmpty(value) && int.TryParse(value, out var offset))
            {
                _cachedTimezoneOffset = offset;
            }
            _isInitialized = true;
        }
        catch
        {
            _cachedTimezoneOffset = 0;
        }

        return _cachedTimezoneOffset;
    }

    public async Task SetServerTimezoneOffsetAsync(int offset)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TimezoneOffsetKey, offset.ToString());
            _cachedTimezoneOffset = offset;
            _isInitialized = true;
            OnSettingsChanged?.Invoke();
        }
        catch
        {
            // Ignore storage errors
        }
    }
}
