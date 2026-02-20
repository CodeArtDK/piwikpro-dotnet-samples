using Microsoft.AspNetCore.Components;
using MudBlazor;
using PiwikPROSamples.BlazorSample.Client.Models;
using PiwikPROSamples.BlazorSample.Client.Services;

namespace PiwikPROSamples.BlazorSample.Client.Pages;

public partial class AddConnection
{
    [Parameter]
    public string? ConnectionId { get; set; }

    private PiwikConnection _connection = new();
    private MudForm? _form;
    private bool _isValid;
    private bool _saving;
    private bool _hasExistingConnections;
    private bool IsEditing => !string.IsNullOrEmpty(ConnectionId);

    protected override async Task OnParametersSetAsync()
    {
        var connections = await ConnectionStateService.GetConnectionsAsync();
        _hasExistingConnections = connections.Any();
        
        if (IsEditing)
        {
            var existing = connections.FirstOrDefault(c => c.Id == ConnectionId);
            if (existing != null)
            {
                _connection = new PiwikConnection
                {
                    Id = existing.Id,
                    Name = existing.Name,
                    BaseUrl = existing.BaseUrl,
                    ClientId = existing.ClientId,
                    ClientSecret = existing.ClientSecret,
                    WebSiteId = existing.WebSiteId,
                    IsDefault = existing.IsDefault
                };
            }
        }
    }

    private async Task SaveConnection()
    {
        if (_form == null || !_isValid)
        {
            return;
        }

        _saving = true;
        try
        {
            await ConnectionStateService.SaveConnectionAsync(_connection);
            Snackbar.Add("Connection saved successfully!", Severity.Success);
            
            // Reload the page to reinitialize the SDK with new connection
            NavigationManager.NavigateTo("/", forceLoad: true);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving connection: {ex.Message}", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }

    private void Cancel()
    {
        // Only navigate to settings if there are existing connections
        // Otherwise, stay on this page as the user needs to create a connection
        if (_hasExistingConnections)
        {
            NavigationManager.NavigateTo("/settings");
        }
    }
}
