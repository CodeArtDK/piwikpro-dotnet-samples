using Microsoft.AspNetCore.Components;
using MudBlazor;
using PiwikPROSamples.BlazorSample.Client.Models;
using PiwikPROSamples.BlazorSample.Client.Services;
using PiwikPROSamples.BlazorSample.Client.Shared;

namespace PiwikPROSamples.BlazorSample.Client.Pages;

public partial class Sessions
{
    [Inject]
    private IAnalyticsDataService DataService { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    private bool _loading = true;
    private List<SessionData>? _sessions;
    private DateTime _startDate = DateTime.Now.AddDays(-7);
    private DateTime _endDate = DateTime.Now;
    private string? _errorMessage;
    private int _limit = 100;
    private string? _firstSessionCampaign;
    private string? _sortColumn;
    private string? _sortDirection;
    private List<DimensionalFilter> _filters = new();
    
    // Filter builder fields
    private string _filterDimension = "source";
    private string _filterOperator = "eq";
    private string _filterValue = string.Empty;

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
        _errorMessage = null;
        StateHasChanged();

        try
        {
            _sessions = await DataService.GetSessionsDataAsync(_startDate, _endDate, _limit, _sortColumn, _sortDirection, _filters);
            
            if (_sessions != null && _sessions.Any())
            {
                _firstSessionCampaign = _sessions.FirstOrDefault()?.CampaignName;
                Snackbar.Add($"Loaded {_sessions.Count} sessions successfully", Severity.Success);
            }
            else
            {
                Snackbar.Add("No sessions found for the selected date range", Severity.Info);
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading sessions: {ex.Message}";
            Snackbar.Add(_errorMessage, Severity.Error);
            Console.WriteLine($"Error details: {ex}");
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private async Task OnSortChanged(string column)
    {
        if (_sortColumn == column)
        {
            // Toggle direction
            _sortDirection = _sortDirection == "asc" ? "desc" : "asc";
        }
        else
        {
            _sortColumn = column;
            _sortDirection = "desc";
        }
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
        "source" => "Source",
        "medium" => "Medium",
        "device_type" => "Device Type",
        "browser_name" => "Browser",
        "operating_system" => "Operating System",
        "location_country_name" => "Country",
        "location_city_name" => "City",
        "referrer_url" => "Referrer URL",
        "campaign_name" => "Campaign Name",
        _ => dimension
    };

    private void ShowSessionDetails(SessionData session)
    {
        var parameters = new DialogParameters<SessionDetailDialog>
        {
            { x => x.Session, session },
            { x => x.StartDate, _startDate },
            { x => x.EndDate, _endDate }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        DialogService.ShowAsync<SessionDetailDialog>("Session Details", parameters, options);
    }
}
