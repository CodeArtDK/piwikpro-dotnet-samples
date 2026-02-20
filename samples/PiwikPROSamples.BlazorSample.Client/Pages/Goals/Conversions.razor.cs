using Microsoft.AspNetCore.Components;
using MudBlazor;
using PiwikPROSamples.BlazorSample.Client.Models;
using PiwikPROSamples.BlazorSample.Client.Services;

namespace PiwikPROSamples.BlazorSample.Client.Pages.Goals;

public partial class Conversions
{
    [Inject]
    private IAnalyticsDataService DataService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private bool _loading = true;
    private List<GoalData>? _data;
    private int _totalConversions;
    private double _avgConversionRate;
    private DateTime _startDate = DateTime.Now.AddDays(-30);
    private DateTime _endDate = DateTime.Now;

    protected override async Task OnInitializedAsync()
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
            _data = await DataService.GetGoalsDataAsync(_startDate, _endDate);
            
            if (_data != null && _data.Any())
            {
                _totalConversions = _data.Sum(x => x.Conversions);
                _avgConversionRate = _data.Average(x => x.ConversionRate);
            }
            else
            {
                Snackbar.Add("No conversion data found for the selected date range.", Severity.Info);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading conversion data: {ex.Message}", Severity.Error);
            Console.WriteLine($"Error details: {ex}");
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }
}
