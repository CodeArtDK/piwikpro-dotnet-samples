using Microsoft.AspNetCore.Components;
using MudBlazor;
using PiwikPROSamples.BlazorSample.Client.Models;
using PiwikPROSamples.BlazorSample.Client.Services;

namespace PiwikPROSamples.BlazorSample.Client.Pages.Behavior;

public partial class Pages
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
    private List<BehaviorData>? _data;
    private DateTime _startDate = DateTime.Now.AddDays(-30);
    private DateTime _endDate = DateTime.Now;
    private List<DimensionalFilter> _filters = new();
    private string _searchString = "";

    private Func<BehaviorData, bool> _quickFilter => x =>
    {
        if (string.IsNullOrWhiteSpace(_searchString))
            return true;
        return x.PageUrl.Contains(_searchString, StringComparison.OrdinalIgnoreCase);
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
            _data = await DataService.GetBehaviorDataAsync(_startDate, _endDate, _filters);
            
            if (_data == null || !_data.Any())
            {
                Snackbar.Add("No page behavior data found for the selected date range.", Severity.Info);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading behavior data: {ex.Message}", Severity.Error);
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
