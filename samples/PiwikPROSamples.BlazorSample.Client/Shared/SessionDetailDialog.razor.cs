using Microsoft.AspNetCore.Components;
using MudBlazor;
using PiwikPROSamples.BlazorSample.Client.Models;
using PiwikPROSamples.BlazorSample.Client.Services;

namespace PiwikPROSamples.BlazorSample.Client.Shared;

public partial class SessionDetailDialog
{
    [Inject]
    private IAnalyticsDataService DataService { get; set; } = default!;

    [Parameter]
    public SessionData? Session { get; set; }

    [Parameter]
    public DateTime StartDate { get; set; } = DateTime.Now.AddDays(-7);

    [Parameter]
    public DateTime EndDate { get; set; } = DateTime.Now;

    private List<EventData>? _events;
    private bool _loadingEvents = true;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        if (Session != null && !string.IsNullOrEmpty(Session.SessionId))
        {
            await LoadEvents();
        }
    }

    private async Task LoadEvents()
    {
        _loadingEvents = true;
        StateHasChanged();

        try
        {
            _events = await DataService.GetSessionEventsAsync(Session!.SessionId, StartDate, EndDate);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading events: {ex.Message}";
            Console.WriteLine($"Error details: {ex}");
        }
        finally
        {
            _loadingEvents = false;
            StateHasChanged();
        }
    }
}
