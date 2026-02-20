using Microsoft.AspNetCore.Components;

namespace PiwikPROSamples.BlazorSample.Client.Pages.Acquisition;

public partial class Campaigns
{
    private bool _loading = true;
    private DateTime _startDate = DateTime.Now.AddDays(-30);
    private DateTime _endDate = DateTime.Now;

    protected override async Task OnInitializedAsync()
    {
        await Task.Delay(500);
        _loading = false;
    }

    private async Task OnDateRangeChanged((DateTime start, DateTime end) range)
    {
        _startDate = range.start;
        _endDate = range.end;
        await Task.Delay(500);
    }
}
