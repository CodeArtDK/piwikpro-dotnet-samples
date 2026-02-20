using Microsoft.AspNetCore.Components;
using MudBlazor;
using PiwikPROSamples.BlazorSample.Client.Models;
using PiwikPROSamples.BlazorSample.Client.Services;

namespace PiwikPROSamples.BlazorSample.Client.Pages.Behavior;

public partial class Downloads
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
    private List<DownloadData>? _data;
    private List<EventTrendData>? _trendData;
    private DateTime _startDate = DateTime.Now.AddDays(-30);
    private DateTime _endDate = DateTime.Now;
    private List<DimensionalFilter> _filters = new();
    private string _searchString = "";
    private List<ChartSeries> _chartSeries = new();
    private string[] _xAxisLabels = Array.Empty<string>();

    private Func<DownloadData, bool> _quickFilter => x =>
    {
        if (string.IsNullOrWhiteSpace(_searchString))
            return true;
        return x.DownloadUrl.Contains(_searchString, StringComparison.OrdinalIgnoreCase);
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
            _data = await DataService.GetDownloadDataAsync(_startDate, _endDate, _filters);
            _trendData = await DataService.GetEventTrendsDataAsync(_startDate, _endDate, _filters);
            
            // Prepare chart data
            if (_trendData != null && _trendData.Any())
            {
                // Sample labels to avoid overcrowding - show every Nth label
                var labelSamplingRate = _trendData.Count > 15 ? (int)Math.Ceiling(_trendData.Count / 15.0) : 1;
                _xAxisLabels = _trendData.Select((x, index) => index % labelSamplingRate == 0 ? x.Date : "").ToArray();
                
                _chartSeries = new List<ChartSeries>
                {
                    new ChartSeries
                    {
                        Name = "Downloads",
                        Data = _trendData.Select(x => (double)x.Downloads).ToArray()
                    }
                };
            }
            
            if ((_data == null || !_data.Any()) && (_trendData == null || !_trendData.Any()))
            {
                Snackbar.Add("No download data found for the selected date range.", Severity.Info);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading download data: {ex.Message}", Severity.Error);
            Console.WriteLine($"Error details: {ex}");
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }
    public void Dispose()
    {
        FilterState.OnFilterChanged -= OnFiltersChanged;
        AppSelector.OnAppChanged -= OnAppChanged;
    }
}
