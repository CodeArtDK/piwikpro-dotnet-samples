using Microsoft.AspNetCore.Components;
using MudBlazor;
using PiwikPROSamples.BlazorSample.Client.Models;
using PiwikPROSamples.BlazorSample.Client.Services;

namespace PiwikPROSamples.BlazorSample.Client.Pages;

public partial class Home
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
    private DashboardMetrics? _metrics;
    private List<AcquisitionData>? _acquisitionData;
    private List<TrendData>? _trendData;
    private List<ChartSeries> _chartSeries = new();
    private string[] _xAxisLabels = Array.Empty<string>();
    private DateTime _startDate = DateTime.Now.AddDays(-30);
    private DateTime _endDate = DateTime.Now;
    private string? _errorMessage;
    private List<DimensionalFilter> _filters = new();

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
        _errorMessage = null;
        StateHasChanged();

        try
        {
            _metrics = await DataService.GetDashboardMetricsAsync(_startDate, _endDate, _filters);
            _acquisitionData = await DataService.GetAcquisitionDataAsync(_startDate, _endDate, _filters);
            _trendData = await DataService.GetTrendsDataAsync(_startDate, _endDate, _filters);

            if (_trendData != null && _trendData.Any())
            {
                // Sample labels to avoid overcrowding - show every Nth label
                var labelSamplingRate = _trendData.Count > 15 ? (int)Math.Ceiling(_trendData.Count / 15.0) : 1;
                _xAxisLabels = _trendData.Select((x, index) => index % labelSamplingRate == 0 ? x.Date : "").ToArray();
                
                _chartSeries = new List<ChartSeries>
                {
                    new ChartSeries
                    {
                        Name = "Sessions",
                        Data = _trendData.Select(x => (double)x.Sessions).ToArray()
                    },
                    new ChartSeries
                    {
                        Name = "Page Views",
                        Data = _trendData.Select(x => (double)x.PageViews).ToArray()
                    }
                };
            }

            // Check if we have no data
            if (_metrics != null && _metrics.TotalSessions == 0 && _metrics.TotalPageViews == 0)
            {
                Snackbar.Add("No analytics data found for the selected date range. Please check your configuration or try a different date range.", Severity.Info);
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading analytics data: {ex.Message}";
            Snackbar.Add(_errorMessage, Severity.Error);
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
