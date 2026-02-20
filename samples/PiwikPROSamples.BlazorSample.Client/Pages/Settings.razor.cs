using Microsoft.AspNetCore.Components;
using MudBlazor;
using PiwikPROSamples.BlazorSample.Client.Models;
using PiwikPROSamples.BlazorSample.Client.Services;

namespace PiwikPROSamples.BlazorSample.Client.Pages;

public partial class Settings
{
    private bool _loading = true;
    private List<PiwikConnection>? _connections;
    private string? _activeConnectionId;
    private int _serverTimezoneOffset = 0;

    protected override async Task OnInitializedAsync()
    {
        await LoadSettings();
    }

    private async Task LoadSettings()
    {
        _loading = true;
        try
        {
            _connections = await ConnectionStateService.GetConnectionsAsync();
            var activeConnection = await ConnectionStateService.GetActiveConnectionAsync();
            _activeConnectionId = activeConnection?.Id;
            _serverTimezoneOffset = await AppSettingsService.GetServerTimezoneOffsetAsync();
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OnTimezoneChanged(int newValue)
    {
        _serverTimezoneOffset = newValue;
        await AppSettingsService.SetServerTimezoneOffsetAsync(newValue);
        Snackbar.Add($"Server timezone set to UTC{(_serverTimezoneOffset >= 0 ? "+" : "")}{_serverTimezoneOffset}", Severity.Success);
    }

    private void AddConnection()
    {
        NavigationManager.NavigateTo("/add-connection");
    }

    private void EditConnection(string connectionId)
    {
        NavigationManager.NavigateTo($"/add-connection/{connectionId}");
    }

    private async Task SetActiveConnection(string connectionId)
    {
        try
        {
            await ConnectionStateService.SetActiveConnectionAsync(connectionId);
            Snackbar.Add("Active connection changed. Reloading...", Severity.Success);
            
            // Reload the page to reinitialize the SDK with new connection
            await Task.Delay(500);
            NavigationManager.NavigateTo("/", forceLoad: true);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error setting active connection: {ex.Message}", Severity.Error);
        }
    }

    private async Task DeleteConnection(PiwikConnection connection)
    {
        var result = await DialogService.ShowMessageBox(
            "Delete Connection",
            $"Are you sure you want to delete the connection '{connection.Name}'?",
            yesText: "Delete",
            cancelText: "Cancel");

        if (result == true)
        {
            try
            {
                await ConnectionStateService.DeleteConnectionAsync(connection.Id);
                Snackbar.Add("Connection deleted successfully", Severity.Success);
                await LoadSettings();
                
                // If we deleted the active connection, reload to show the setup page
                if (connection.Id == _activeConnectionId)
                {
                    await Task.Delay(500);
                    NavigationManager.NavigateTo("/", forceLoad: true);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error deleting connection: {ex.Message}", Severity.Error);
            }
        }
    }
}
