using Microsoft.AspNetCore.Components;
using MudBlazor;
using PiwikPROSamples.BlazorSample.Client.Models;
using PiwikPROSamples.BlazorSample.Client.Services;

namespace PiwikPROSamples.BlazorSample.Client.Pages.Behavior;

public partial class PageDetail : IDisposable
{
    [Inject]
    private IAnalyticsDataService DataService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IFilterStateService FilterState { get; set; } = default!;

    [Inject]
    private IAppSelectorService AppSelector { get; set; } = default!;

    private bool _loading = false;
    private PageDetailData? _data;
    private DateTime _startDate = DateTime.Now.AddDays(-30);
    private DateTime _endDate = DateTime.Now;
    private string _pageUrl = string.Empty;
    private List<DimensionalFilter> _filters = new();

    // Chart state for daily traffic
    private List<ChartSeries> _chartSeries = new();
    private string[] _xAxisLabels = Array.Empty<string>();

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to navigation changes
        NavigationManager.LocationChanged += OnLocationChanged;

        // Subscribe to filter changes
        FilterState.OnFilterChanged += OnFiltersChanged;
        AppSelector.OnAppChanged += OnAppChanged;
        _filters = await FilterState.GetFiltersAsync();

        // Load initial data
        await LoadPageFromUrl();
    }

    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        // Reload page when URL changes
        InvokeAsync(async () => await LoadPageFromUrl());
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

    private async Task LoadPageFromUrl()
    {
        // Get page URL from query string
        var uri = new Uri(NavigationManager.Uri);
        var query = uri.Query;
        if (!string.IsNullOrEmpty(query))
        {
            var queryParams = System.Web.HttpUtility.ParseQueryString(query);
            var pageUrl = queryParams["url"];
            if (!string.IsNullOrEmpty(pageUrl) && pageUrl != _pageUrl)
            {
                _pageUrl = pageUrl;
                await LoadData();
            }
        }
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        FilterState.OnFilterChanged -= OnFiltersChanged;
        AppSelector.OnAppChanged -= OnAppChanged;
    }

    private async Task OnDateRangeChanged((DateTime start, DateTime end) range)
    {
        _startDate = range.start;
        _endDate = range.end;
        if (!string.IsNullOrEmpty(_pageUrl))
        {
            await LoadData();
        }
    }

    private async Task LoadData()
    {
        if (string.IsNullOrEmpty(_pageUrl))
            return;

        _loading = true;
        StateHasChanged();

        try
        {
            _data = await DataService.GetPageDetailDataAsync(_pageUrl, _startDate, _endDate, _filters);

            // Build chart data
            _chartSeries.Clear();
            _xAxisLabels = Array.Empty<string>();
            if (_data?.Trend != null && _data.Trend.Any())
            {
                var labelSamplingRate = _data.Trend.Count > 15 ? (int)Math.Ceiling(_data.Trend.Count / 15.0) : 1;
                _xAxisLabels = _data.Trend.Select((x, index) => index % labelSamplingRate == 0 ? x.Date : "").ToArray();

                _chartSeries = new List<ChartSeries>
                {
                    new ChartSeries
                    {
                        Name = "Sessions",
                        Data = _data.Trend.Select(x => (double)x.Sessions).ToArray()
                    },
                    new ChartSeries
                    {
                        Name = "Page Views",
                        Data = _data.Trend.Select(x => (double)x.PageViews).ToArray()
                    }
                };
            }

            if (_data == null)
            {
                Snackbar.Add("No data found for this page in the selected date range.", Severity.Info);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading page details: {ex.Message}", Severity.Error);
            Console.WriteLine($"Error details: {ex}");
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }
}
