# Tracking API

The Tracking API lets you send events to Piwik PRO from your server and generate JavaScript tracking code for client-side use. Unlike the Analytics Query API, tracking endpoints do not require OAuth authentication.

## Setup

```csharp
using PiwikPRO.Tracking.Extensions;

// If you've already called AddPiwikProAnalytics, tracking can reuse the core services:
services.AddPiwikProTracking();

// Or standalone:
services.AddPiwikProTracking(options =>
{
    options.BaseUrl = "https://your-instance.piwik.pro";
    options.WebSiteId = "your-website-id";
});
```

## Server-Side Tracking

Inject `ITrackingService` to track events from your backend.

### Page View

```csharp
var result = await trackingService.TrackPageViewAsync(
    url: "https://example.com/products",
    actionName: "Product Listing Page");
```

### Goal Conversion

```csharp
var result = await trackingService.TrackGoalAsync(
    goalId: "abc-123",
    revenue: 99.99m);
```

### Download

```csharp
var result = await trackingService.TrackDownloadAsync(
    downloadUrl: "https://example.com/files/whitepaper.pdf");
```

### Outlink

```csharp
var result = await trackingService.TrackOutlinkAsync(
    linkUrl: "https://external-site.com");
```

### Site Search

```csharp
var result = await trackingService.TrackSiteSearchAsync(
    searchQuery: "wireless headphones",
    categories: new List<string> { "Electronics", "Audio" },
    resultCount: 25);
```

### Advanced: Full TrackingRequest

For complete control over all parameters:

```csharp
var request = new TrackingRequest
{
    Url = "https://example.com/checkout",
    ActionName = "Checkout - Step 2",
    Uid = "user@example.com",
    CustomDimensions = new Dictionary<string, string>
    {
        ["dimension1"] = "Premium User",
        ["dimension2"] = "US-West"
    },
    Ua = "Mozilla/5.0 ...",
    Lang = "en-US"
};

var result = await trackingService.TrackEventAsync(request);
```

### Response Handling

All tracking methods return a `TrackingResponse`:

```csharp
if (result.Success)
    logger.LogInformation("Tracked: {Message}", result.Message);
else
    logger.LogWarning("Failed: {StatusCode} - {Message}", result.StatusCode, result.Message);
```

## JavaScript Code Generation

Inject `IJavaScriptTrackingCodeGenerator` to generate tracking snippets for your HTML pages.

### Standard Tracking Code

```csharp
var script = codeGenerator.GenerateTrackingCode();
// Uses _paq variable by default
```

### Matomo-Compatible (\_ppas Variable)

```csharp
var script = codeGenerator.GenerateTrackingCode(trackerVariableName: "_ppas");
```

### Tag Manager + Consent Manager

```csharp
var script = codeGenerator.GenerateTagManagerTrackingCode();
```

### Custom Options

```csharp
var options = new TrackingCodeOptions
{
    TrackerVariableName = "_ppas",
    EnableHeartBeatTimer = true,
    HeartBeatTimerDelay = 15,
    EnableJSErrorTracking = true,
    SetUserIsAnonymous = true,
    DisableCookies = true
};

var script = codeGenerator.GenerateTrackingCode(options);
```

### Privacy-Focused Preset

```csharp
var script = codeGenerator.GenerateTrackingCode(TrackingCodeOptions.Anonymous);
```

### Embedding in Razor Pages

```cshtml
@inject IJavaScriptTrackingCodeGenerator CodeGenerator

<head>
    @Html.Raw(CodeGenerator.GenerateTagManagerTrackingCode())
</head>
```

### Custom Site ID

```csharp
var script = codeGenerator.GenerateTrackingCode(
    siteId: "custom-site-id",
    accountAddress: "custom.containers.piwik.pro");
```

## Dynamic Client-Side Tracking

Use `IClientSideTrackingScriptBuilder` to queue tracking commands during page processing and render them at the end:

```csharp
@inject IClientSideTrackingScriptBuilder ScriptBuilder

@{
    ScriptBuilder.TrackEvent("Button", "Click", "Submit");
    ScriptBuilder.SetCustomDimension(1, "Premium");
    ScriptBuilder.TrackGoal(1, 99.99m);
    ScriptBuilder.SetUserId("user@example.com");
}

@if (ScriptBuilder.HasPendingCommands)
{
    @Html.Raw(ScriptBuilder.BuildDynamicTrackingScript("_ppas"))
}
```

## TrackingCodeOptions Reference

| Option | Default | Description |
|--------|---------|-------------|
| `TrackerVariableName` | `"_paq"` | JS variable name (`"_ppas"` for Matomo compatibility) |
| `TrackPageView` | `true` | Auto-track page views |
| `EnableLinkTracking` | `true` | Auto-track link clicks |
| `SetUserIsAnonymous` | `false` | Anonymous tracking |
| `TrackVisibleContentImpressions` | `false` | Track visible content blocks |
| `TrackAllContentImpressions` | `false` | Track all content blocks |
| `EnableCrossDomainLinking` | `false` | Cross-domain linking |
| `EnableJSErrorTracking` | `false` | Track JS errors |
| `EnableHeartBeatTimer` | `false` | Heartbeat timer for time-on-page |
| `HeartBeatTimerDelay` | `15` | Heartbeat interval (seconds) |
| `DisableCookies` | `false` | Disable all cookies |
| `CustomCommandsBefore` | `null` | Commands before `trackPageView` |
| `CustomCommandsAfter` | `null` | Commands after `trackPageView` |

## See Also

- [Console Sample](../samples/PiwikPROSamples.ConsoleSample/) - Server-side tracking and code generation examples
- [Piwik PRO Tracking HTTP API](https://help.piwik.pro/support/developers/tracking-http-api/)
- [Piwik PRO JavaScript Tracking Client](https://help.piwik.pro/support/developers/javascript-tracking-client/)
