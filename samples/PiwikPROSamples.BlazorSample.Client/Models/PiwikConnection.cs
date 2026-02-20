namespace PiwikPROSamples.BlazorSample.Client.Models;

/// <summary>
/// Represents a PiwikPRO connection configuration
/// </summary>
public class PiwikConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string WebSiteId { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
