using Microsoft.AspNetCore.Components;
using MudBlazor;
using PiwikPROSamples.BlazorSample.Client.Models;
using PiwikPROSamples.BlazorSample.Client.Services;

namespace PiwikPROSamples.BlazorSample.Client.Pages.Visitors;

public partial class Locations
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
    private List<LocationData>? _data;
    private List<ChartSeries> _barChartSeries = new();
    private string[] _xAxisLabels = Array.Empty<string>();
    private DateTime _startDate = DateTime.Now.AddDays(-30);
    private DateTime _endDate = DateTime.Now;
    private string _searchString = "";
    private List<DimensionalFilter> _filters = new();

    private Func<LocationData, bool> _quickFilter => x =>
    {
        if (string.IsNullOrWhiteSpace(_searchString))
            return true;
        return x.Country.Contains(_searchString, StringComparison.OrdinalIgnoreCase);
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
            _data = await DataService.GetLocationDataAsync(_startDate, _endDate, _filters);
            
            if (_data != null && _data.Any())
            {
                // Prepare chart data for top 10 countries
                var top10 = _data.Take(10).ToList();
                _xAxisLabels = top10.Select(x => x.Country).ToArray();
                _barChartSeries = new List<ChartSeries>
                {
                    new ChartSeries
                    {
                        Name = "Sessions",
                        Data = top10.Select(x => (double)x.Sessions).ToArray()
                    }
                };
            }
            else
            {
                Snackbar.Add("No location data found for the selected date range.", Severity.Info);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading location data: {ex.Message}", Severity.Error);
            Console.WriteLine($"Error details: {ex}");
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private async Task AddCountryFilter(LocationData location)
    {
        var filter = new DimensionalFilter("location_country_name", location.CountryKey, $"Country: {location.Country}");
        await FilterState.AddFilterAsync(filter);
    }

    public void Dispose()
    {
        FilterState.OnFilterChanged -= OnFiltersChanged;
        AppSelector.OnAppChanged -= OnAppChanged;
    }
}
