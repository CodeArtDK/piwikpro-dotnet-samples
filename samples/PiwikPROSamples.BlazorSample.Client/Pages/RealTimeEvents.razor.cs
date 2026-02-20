using Microsoft.AspNetCore.Components;
using MudBlazor;
using PiwikPROSamples.BlazorSample.Client.Models;
using PiwikPROSamples.BlazorSample.Client.Services;

namespace PiwikPROSamples.BlazorSample.Client.Pages;

public partial class RealTimeEvents : IAsyncDisposable
{
    private const int AutoRefreshIntervalSeconds = 30;
    
    [Inject]
    private IAnalyticsDataService DataService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IAppSettingsService AppSettingsService { get; set; } = default!;

    private bool _loading = true;
    private List<RealTimeEventData>? _events;
    private string? _errorMessage;
    private int _limit = 100;
    private int _timeRange = 10; // minutes
    private bool _autoRefresh = false;
    private List<DimensionalFilter> _filters = new();
    private CancellationTokenSource? _autoRefreshCts;
    private Task? _autoRefreshTask;
    private DateTime? _lastRefreshTime;
    private int _autoRefreshCountdown = AutoRefreshIntervalSeconds;
    private bool _isCountingDown = false;
    private int _serverTimezoneOffset = 0; // UTC offset in hours for the Piwik Pro server
    
    // Filter builder fields
    private string _filterDimension = "event_type";
    private string _filterOperator = "eq";
    private string _filterValue = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        // Load timezone from global settings
        _serverTimezoneOffset = await AppSettingsService.GetServerTimezoneOffsetAsync();
        AppSettingsService.OnSettingsChanged += OnSettingsChanged;
        await LoadData();
    }

    private async void OnSettingsChanged()
    {
        _serverTimezoneOffset = await AppSettingsService.GetServerTimezoneOffsetAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnTimeRangeChanged(int newValue)
    {
        _timeRange = newValue;
        await LoadData();
    }

    private async Task AddFilter()
    {
        if (string.IsNullOrWhiteSpace(_filterDimension) || string.IsNullOrWhiteSpace(_filterValue))
            return;

        var filter = new DimensionalFilter(_filterDimension, _filterOperator, _filterValue);
        _filters.Add(filter);
        Snackbar.Add($"Added filter: {GetDimensionDisplayName(filter.Dimension)} {FilterOperators.GetDisplayName(filter.Operator)} \"{filter.Value}\"", Severity.Info);
        _filterValue = string.Empty;
        await LoadData();
    }

    private async Task RemoveFilter(DimensionalFilter filter)
    {
        _filters.Remove(filter);
        Snackbar.Add($"Removed filter: {GetDimensionDisplayName(filter.Dimension)}", Severity.Info);
        await LoadData();
    }

    private async Task ClearFilters()
    {
        var count = _filters.Count;
        _filters.Clear();
        Snackbar.Add($"Cleared {count} filter(s)", Severity.Info);
        await LoadData();
    }

    private string GetDimensionDisplayName(string dimension) => dimension switch
    {
        "event_type" => "Event Type",
        "event_url" => "Event URL",
        "source" => "Source",
        "medium" => "Medium",
        "device_type" => "Device Type",
        "browser_name" => "Browser",
        "location_country_name" => "Country",
        "location_city_name" => "City",
        _ => dimension
    };

    private async Task OnAutoRefreshToggled(bool value)
    {
        _autoRefresh = value;
        
        if (_autoRefresh)
        {
            _autoRefreshCts = new CancellationTokenSource();
            _autoRefreshTask = RunAutoRefreshLoop(_autoRefreshCts.Token);
            Snackbar.Add($"Auto-refresh enabled (every {AutoRefreshIntervalSeconds} seconds)", Severity.Info);
        }
        else
        {
            await StopAutoRefresh();
            _isCountingDown = false;
            Snackbar.Add("Auto-refresh disabled", Severity.Info);
        }
    }

    private async Task RunAutoRefreshLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _isCountingDown = true;
                for (int i = AutoRefreshIntervalSeconds; i > 0; i--)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    _autoRefreshCountdown = i;
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(1000, cancellationToken);
                }
                
                if (cancellationToken.IsCancellationRequested) break;
                
                await InvokeAsync(LoadData);
            }
        }
        catch (OperationCanceledException) { }
        finally { _isCountingDown = false; }
    }

    private async Task StopAutoRefresh()
    {
        if (_autoRefreshCts != null)
        {
            await _autoRefreshCts.CancelAsync();
            if (_autoRefreshTask != null)
            {
                try { await _autoRefreshTask; }
                catch (OperationCanceledException) { }
            }
            _autoRefreshCts.Dispose();
            _autoRefreshCts = null;
            _autoRefreshTask = null;
        }
    }

    private async Task LoadData()
    {
        _loading = true;
        _errorMessage = null;
        StateHasChanged();

        try
        {
            // Calculate the date range in the server's timezone
            // 1. Start with UTC now
            // 2. Apply the server timezone offset to get server's local time
            // 3. Then apply the time range and 3-minute delay
            var serverNow = DateTime.UtcNow.AddHours(_serverTimezoneOffset);
            var endDateTime = serverNow.AddMinutes(-3);
            var startDateTime = endDateTime.AddMinutes(-_timeRange);

            var events = await DataService.GetRealTimeEventsAsync(startDateTime, endDateTime, _limit, _filters);
            
            // Sort by timestamp descending (latest first)
            _events = events?.OrderByDescending(e => e.Timestamp).ToList() ?? new List<RealTimeEventData>();
            _lastRefreshTime = DateTime.Now;
            
            if (_events.Any())
            {
                Snackbar.Add($"Loaded {_events.Count} events", Severity.Success);
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error: {ex.Message}";
            Snackbar.Add(_errorMessage, Severity.Error);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private Color GetTimestampColor(DateTime? timestamp)
    {
        if (!timestamp.HasValue) return Color.Default;
        // The timestamp from the server is in the server's timezone
        // Convert to UTC first (subtract server offset), then calculate age
        var timestampUtc = timestamp.Value.AddHours(-_serverTimezoneOffset);
        var age = DateTime.UtcNow - timestampUtc;
        if (age.TotalMinutes < 5) return Color.Success;
        if (age.TotalMinutes < 15) return Color.Info;
        if (age.TotalMinutes < 30) return Color.Warning;
        return Color.Default;
    }

    private string GetTimeAgo(DateTime? timestamp)
    {
        if (!timestamp.HasValue) return "N/A";
        // The timestamp from the server is in the server's timezone
        // Convert to UTC first (subtract server offset), then calculate age
        var timestampUtc = timestamp.Value.AddHours(-_serverTimezoneOffset);
        var age = DateTime.UtcNow - timestampUtc;
        if (age.TotalMinutes < 1) return "just now";
        if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes}m ago";
        if (age.TotalHours < 24) return $"{(int)age.TotalHours}h ago";
        return $"{(int)age.TotalDays}d ago";
    }

    /// <summary>
    /// Converts a server timestamp to the user's local time for display
    /// </summary>
    private DateTime? ConvertToLocalTime(DateTime? serverTimestamp)
    {
        if (!serverTimestamp.HasValue) return null;
        // The timestamp from the server is in the server's timezone
        // 1. Convert to UTC by subtracting the server offset
        // 2. Convert to local time
        var timestampUtc = serverTimestamp.Value.AddHours(-_serverTimezoneOffset);
        return timestampUtc.ToLocalTime();
    }

    private Color GetEventTypeColor(string eventType) => eventType.ToLowerInvariant() switch
    {
        "pageview" => Color.Primary,
        "click" => Color.Info,
        "download" => Color.Success,
        "outlink" => Color.Warning,
        _ => Color.Default
    };

    private string TruncateUrl(string url, int maxLength) =>
        string.IsNullOrEmpty(url) || url.Length <= maxLength ? url : url.Substring(0, maxLength) + "...";

    public async ValueTask DisposeAsync()
    {
        AppSettingsService.OnSettingsChanged -= OnSettingsChanged;
        await StopAutoRefresh();
    }
}
