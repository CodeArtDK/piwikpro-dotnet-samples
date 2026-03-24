# Piwik PRO .NET SDK Samples

Sample applications demonstrating how to use the [Piwik PRO .NET SDK](https://www.nuget.org/packages/PiwikPRO) (`PiwikPRO` NuGet package) for analytics queries, server-side tracking, and JavaScript tracking code generation.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- A Piwik PRO account with API access (Client ID, Client Secret, and Website ID)

## Samples

| Project | Description |
|---------|-------------|
| [Blazor Sample](samples/PiwikPROSamples.BlazorSample.Server/) | Full-featured analytics dashboard PWA built with Blazor WebAssembly and MudBlazor. Demonstrates the Analytics Query API, Sessions API, Events API, and Real-Time Events API. |
| [Console Sample](samples/PiwikPROSamples.ConsoleSample/) | Console application showing SDK basics: queries, streaming, date ranges, server-side tracking, JavaScript code generation, goals, and custom dimensions. |

## Quick Start

### 1. Install the SDK

```bash
dotnet add package PiwikPRO
```

This meta-package includes both `PiwikPRO.Analytics` and `PiwikPRO.Tracking`.

### 2. Configure Services

```csharp
using PiwikPRO.Analytics.Extensions;
using PiwikPRO.Tracking.Extensions;

// Analytics (queries, sessions, goals, events)
services.AddPiwikProAnalytics(options =>
{
    options.BaseUrl = "https://your-instance.piwik.pro";
    options.ClientId = "your-client-id";
    options.ClientSecret = "your-client-secret";
    options.WebSiteId = "your-website-id";
});

// Tracking (server-side page views, events, goals, downloads)
services.AddPiwikProTracking();
```

### 3. Run a Sample

```bash
# Blazor dashboard
dotnet run --project samples/PiwikPROSamples.BlazorSample.Server

# Console examples
dotnet run --project samples/PiwikPROSamples.ConsoleSample
```

> **Important:** For the Blazor sample, always start the **`BlazorSample.Server`** project (set it as startup project in Visual Studio). The Server hosts the Blazor WebAssembly client and provides a YARP reverse proxy that forwards API calls to Piwik PRO — without it, browser CORS restrictions will block all API requests.

## SDK Documentation

- [Analytics Query API](docs/analytics-queries.md) - Fluent query builder, dimensions, metrics, filtering, streaming
- [Sessions API](docs/sessions-api.md) - Raw session data with configurable columns and filters
- [Goals API](docs/goals-api.md) - Goal management, conversions, and funnel analysis
- [Tracking API](docs/tracking-api.md) - Server-side tracking, JavaScript code generation, Tag Manager integration

## Building

```bash
# Build all samples
dotnet build PiwikPROSamples.slnx

# Build individual projects
dotnet build samples/PiwikPROSamples.BlazorSample.Server
dotnet build samples/PiwikPROSamples.ConsoleSample
```

## Getting Your Credentials

[Get a Piwik PRO trial here](https://piwik.pro/business-plan/?utm_campaign=codeart)
1. Log into your Piwik PRO account
2. Go to **Menu > Profile > API Keys** or **Settings > API**
3. Create a new API client (or use an existing one)
4. Copy the **Client ID** and **Client Secret**
5. Find your **Website ID** in **Administration > Sites & Apps**

## License

See the [LICENSE](LICENSE) file for details.
