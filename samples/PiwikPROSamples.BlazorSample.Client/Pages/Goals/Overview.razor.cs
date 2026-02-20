using Microsoft.AspNetCore.Components;
using MudBlazor;
using PiwikPROSamples.BlazorSample.Client.Models;
using PiwikPROSamples.BlazorSample.Client.Services;

namespace PiwikPROSamples.BlazorSample.Client.Pages.Goals;

public partial class Overview
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
    private List<GoalData>? _data;
    private List<ChartSeries> _chartSeries = new();
    private string[] _xAxisLabels = Array.Empty<string>();
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
            _data = await DataService.GetGoalsDataAsync(_startDate, _endDate, _filters);
            
            if (_data != null && _data.Any())
            {
                // Sample labels to avoid overcrowding - show every Nth label
                var labelSamplingRate = _data.Count > 15 ? (int)Math.Ceiling(_data.Count / 15.0) : 1;
                _xAxisLabels = _data.Select((x, index) => index % labelSamplingRate == 0 ? x.Date : "").ToArray();
                
                _chartSeries = new List<ChartSeries>
                {
                    new ChartSeries
                    {
                        Name = "Conversions",
                        Data = _data.Select(x => (double)x.Conversions).ToArray()
                    },
                    new ChartSeries
                    {
                        Name = "Conversion Rate (%)",
                        Data = _data.Select(x => x.ConversionRate * 100).ToArray()
                    }
                };
            }
            else
            {
                Snackbar.Add("No goal data found for the selected date range.", Severity.Info);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading goal data: {ex.Message}", Severity.Error);
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
