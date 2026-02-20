using Microsoft.AspNetCore.Components;
using MudBlazor;
using PiwikPROSamples.BlazorSample.Client.Models;
using PiwikPROSamples.BlazorSample.Client.Services;

namespace PiwikPROSamples.BlazorSample.Client.Pages.Visitors;

public partial class Technology
{
    [Inject]
    private IAnalyticsDataService DataService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IFilterStateService FilterState { get; set; } = default!;

    [Inject]
    private IAppSelectorService AppSelector { get; set; } = default!;

    private bool _loading = true;
    private List<VisitorData>? _data;
    private DateTime _startDate = DateTime.Now.AddDays(-30);
    private DateTime _endDate = DateTime.Now;
    private string _searchString = "";
    private List<DimensionalFilter> _filters = new();

    private Func<VisitorData, bool> _quickFilter => x =>
    {
        if (string.IsNullOrWhiteSpace(_searchString))
            return true;
        return x.Browser.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ||
               x.DeviceType.Contains(_searchString, StringComparison.OrdinalIgnoreCase);
    };

    protected override async Task OnInitializedAsync()
    {
        FilterState.OnFilterChanged += OnFiltersChanged;
        AppSelector.OnAppChanged += OnAppChanged;
        _filters = await FilterState.GetFiltersAsync();
        await LoadData();
    }

    private async void OnFiltersChanged()
    {
        _filters = await FilterState.GetFiltersAsync();
        await LoadData();
    }

    private async void OnAppChanged()
    {
        await LoadData();
    }

    private async Task OnDateRangeChanged((DateTime start, DateTime end) range)
    {
        _startDate = range.start;
        _endDate = range.end;
        await LoadData();
    }

    private async Task LoadData()
    {
        _loading = true;
        StateHasChanged();

        try
        {
            _data = await DataService.GetVisitorDataAsync(_startDate, _endDate, _filters);
            
            if (_data == null || !_data.Any())
            {
                Snackbar.Add("No technology data found for the selected date range.", Severity.Info);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading technology data: {ex.Message}", Severity.Error);
            Console.WriteLine($"Error details: {ex}");
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private async Task AddBrowserFilter(VisitorData data)
    {
        var filter = new DimensionalFilter("browser_name", data.BrowserKey, $"Browser: {data.Browser}");
        await FilterState.AddFilterAsync(filter);
    }

    private async Task AddDeviceTypeFilter(VisitorData data)
    {
        var filter = new DimensionalFilter("device_type", data.DeviceTypeKey, $"Device: {data.DeviceType}");
        await FilterState.AddFilterAsync(filter);
    }

    public void Dispose()
    {
        FilterState.OnFilterChanged -= OnFiltersChanged;
        AppSelector.OnAppChanged -= OnAppChanged;
    }
}
