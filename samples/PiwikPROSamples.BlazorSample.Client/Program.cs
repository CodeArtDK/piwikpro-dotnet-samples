using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using PiwikPROSamples.BlazorSample.Client;
using PiwikPROSamples.BlazorSample.Client.Services;
using PiwikPRO.Analytics.Extensions;
using PiwikPRO.Core;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add MudBlazor services
builder.Services.AddMudServices();

// Add connection state service
builder.Services.AddScoped<IConnectionStateService, ConnectionStateService>();

// Add application services
builder.Services.AddScoped<IAnalyticsDataService, AnalyticsDataService>();
builder.Services.AddSingleton<IFilterStateService, FilterStateService>();
// App selector depends on IAppsService (scoped), so it must be scoped too
builder.Services.AddScoped<IAppSelectorService, AppSelectorService>();
// Global settings and caching services
builder.Services.AddScoped<IAppSettingsService, AppSettingsService>();
builder.Services.AddSingleton<IPageCacheService, PageCacheService>();

// Get configuration section once for reuse
var config = builder.Configuration.GetSection("PiwikPRO");

// Store the host base address for use in configuration
var hostBase = builder.HostEnvironment.BaseAddress ?? string.Empty;

// Create a shared configuration instance that we can populate before registering services
var sharedConfig = new PiwikProConfiguration
{
    BaseUrl = "https://placeholder.piwik.pro",
    ClientId = "",
    ClientSecret = "",
    WebSiteId = "",
    TimeoutSeconds = 30
};

// Try to read the active connection from localStorage BEFORE registering services
// This uses JavaScript interop that's available before the host is built
try
{
    // Read localStorage values directly using JavaScript interop
    var activeId = GetLocalStorageItem("piwikpro_active_connection");
    var connectionsJson = GetLocalStorageItem("piwikpro_connections");
    
    if (!string.IsNullOrEmpty(activeId) && !string.IsNullOrEmpty(connectionsJson))
    {
        List<ActiveConnectionDto>? connections = null;
        try
        {
            connections = System.Text.Json.JsonSerializer.Deserialize<List<ActiveConnectionDto>>(connectionsJson);
        }
        catch (System.Text.Json.JsonException jsonEx)
        {
            Console.WriteLine($"Error parsing connections JSON: {jsonEx.Message}");
        }
        
        var activeConnection = connections?.FirstOrDefault(c => c.Id == activeId);
        
        if (activeConnection != null && 
            !string.IsNullOrEmpty(activeConnection.BaseUrl) &&
            !string.IsNullOrEmpty(activeConnection.ClientId) &&
            !string.IsNullOrEmpty(activeConnection.ClientSecret))
        {
            // Build the proxy URL
            var uri = new Uri(activeConnection.BaseUrl);
            var hostname = uri.Host;
            
            if (hostBase.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                hostBase.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                sharedConfig.BaseUrl = $"{hostBase.TrimEnd('/')}/api/proxy/{hostname}";
            }
            else
            {
                sharedConfig.BaseUrl = $"/api/proxy/{hostname}";
            }
            
            sharedConfig.ClientId = activeConnection.ClientId;
            sharedConfig.ClientSecret = activeConnection.ClientSecret;
            sharedConfig.WebSiteId = activeConnection.WebSiteId ?? "";
            
            Console.WriteLine($"SDK configured with connection from localStorage: {activeConnection.Name}");
        }
    }
    else
    {
        // Try appsettings.json as fallback
        var baseUrl = config["BaseUrl"];
        var clientId = config["ClientId"];
        var clientSecret = config["ClientSecret"];
        
        if (!string.IsNullOrEmpty(baseUrl) && !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
        {
            var uri = new Uri(baseUrl);
            var hostname = uri.Host;
            
            if (hostBase.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                hostBase.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                sharedConfig.BaseUrl = $"{hostBase.TrimEnd('/')}/api/proxy/{hostname}";
            }
            else
            {
                sharedConfig.BaseUrl = $"/api/proxy/{hostname}";
            }
            
            sharedConfig.ClientId = clientId;
            sharedConfig.ClientSecret = clientSecret;
            sharedConfig.WebSiteId = config["WebSiteId"] ?? "";
            
            Console.WriteLine("SDK configured from appsettings.json");
        }
        else
        {
            Console.WriteLine("No active connection found. User needs to create a connection.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading connection configuration: {ex.Message}");
}

// Configure PiwikPRO Analytics SDK with the pre-loaded configuration
builder.Services.AddPiwikProAnalytics(options =>
{
    options.BaseUrl = sharedConfig.BaseUrl;
    options.ClientId = sharedConfig.ClientId;
    options.ClientSecret = sharedConfig.ClientSecret;
    options.WebSiteId = sharedConfig.WebSiteId;
    options.TimeoutSeconds = sharedConfig.TimeoutSeconds;
});

// Build and run the host
var host = builder.Build();
await host.RunAsync();

// Helper method to read localStorage using JavaScript interop
// This works before the host is built in Blazor WebAssembly
static string? GetLocalStorageItem(string key)
{
    // Validate key to prevent potential issues
    if (string.IsNullOrEmpty(key) || key.Contains('\0'))
    {
        return null;
    }
    
    try
    {
        return LocalStorageInterop.GetItem(key);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error reading localStorage key '{key}': {ex.Message}");
        return null;
    }
}

// JavaScript interop for localStorage access before host is built
internal static partial class LocalStorageInterop
{
    [System.Runtime.InteropServices.JavaScript.JSImport("globalThis.localStorage.getItem")]
    public static partial string? GetItem(string key);
}

// DTO for JavaScript interop
// Properties must match the PascalCase names used in localStorage by ConnectionStateService
public record ActiveConnectionDto
{
    [System.Text.Json.Serialization.JsonPropertyName("Id")]
    public string? Id { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("Name")]
    public string? Name { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("BaseUrl")]
    public string? BaseUrl { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("ClientId")]
    public string? ClientId { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("ClientSecret")]
    public string? ClientSecret { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("WebSiteId")]
    public string? WebSiteId { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("IsDefault")]
    public bool IsDefault { get; init; }
}
