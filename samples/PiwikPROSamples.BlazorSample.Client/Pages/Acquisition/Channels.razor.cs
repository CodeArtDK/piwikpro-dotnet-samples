using Microsoft.AspNetCore.Components;
using MudBlazor;
using PiwikPROSamples.BlazorSample.Client.Models;
using PiwikPROSamples.BlazorSample.Client.Services;

namespace PiwikPROSamples.BlazorSample.Client.Pages.Acquisition;

public partial class Channels
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
    private List<ChannelData>? _data;
    private List<ChartSeries> _bounceRateSeries = new();
    private ChartOptions _bounceRateChartOptions = new ChartOptions { MaxNumYAxisTicks = 5, YAxisTicks = 25 };
    private DateTime _startDate = DateTime.Now.AddDays(-30);
    private DateTime _endDate = DateTime.Now;
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
        StateHasChanged();

        try
        {
            _data = await DataService.GetChannelDataAsync(_startDate, _endDate, _filters);
            
            if (_data == null || !_data.Any())
            {
                Snackbar.Add("No channel data found for the selected date range.", Severity.Info);
            }
            else
            {
                // Prepare chart series for bounce rate bar chart
                // Convert decimal (0-1) to percentage (0-100) for proper display
                _bounceRateSeries = new List<ChartSeries>
                {
                    new ChartSeries
                    {
                        Name = "Bounce Rate %",
                        Data = _data.Select(x => x.BounceRate * 100).ToArray()
                    }
                };
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading channel data: {ex.Message}", Severity.Error);
            Console.WriteLine($"Error details: {ex}");
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private async Task AddChannelFilter(ChannelData channel)
    {
        var filter = new DimensionalFilter("referrer_type", "eq", channel.ChannelKey, $"Channel: {channel.Channel}");
        await FilterState.AddFilterAsync(filter);
    }

    public void Dispose()
    {
        FilterState.OnFilterChanged -= OnFiltersChanged;
        AppSelector.OnAppChanged -= OnAppChanged;
    }
}
