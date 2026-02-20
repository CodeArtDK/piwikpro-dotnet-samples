# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Sample applications and documentation for the Piwik PRO .NET SDK. The SDK is consumed as the `PiwikPRO` NuGet package (meta-package containing `PiwikPRO.Analytics` and `PiwikPRO.Tracking`).

## Build & Run Commands

```bash
# Build all samples
dotnet build PiwikPROSamples.slnx

# Run the Blazor WASM app (must use the Server project to avoid CORS issues)
dotnet run --project samples/PiwikPROSamples.BlazorSample.Server

# Run the console example
dotnet run --project samples/PiwikPROSamples.ConsoleSample

# Publish for production
dotnet publish -c Release samples/PiwikPROSamples.BlazorSample.Server
```

There are no test projects.

## Architecture

### Three Sample Projects

1. **PiwikPROSamples.BlazorSample.Client** — Blazor WebAssembly PWA (net8.0) that runs entirely in the browser. Uses MudBlazor for UI. All SDK calls and business logic execute client-side. Connects to Piwik PRO via `AddPiwikProAnalytics()`.

2. **PiwikPROSamples.BlazorSample.Server** — ASP.NET Core host for the Blazor WASM app. Its sole purpose is to act as a YARP reverse proxy (`/api/proxy/{hostname}/{**catch-all}`) to bypass browser CORS restrictions when the WASM client calls the Piwik PRO API. Contains no business logic.

3. **PiwikPROSamples.ConsoleSample** — Console app demonstrating SDK usage for both Analytics (query/streaming) and Tracking (page views, goals, downloads, search, outlinks) plus JavaScript tracking code generation. Configured via environment variables `PIWIKPRO_BASE_URL`, `PIWIKPRO_CLIENT_ID`, `PIWIKPRO_CLIENT_SECRET`, `PIWIKPRO_WEBSITE_ID`.

### Blazor App Key Patterns

- **Connection management**: Credentials are stored in browser localStorage (managed by `ConnectionStateService`). On startup, `Program.cs` reads localStorage before the host is built via JS interop (`JSImport`) to configure the SDK. Falls back to `wwwroot/appsettings.json`.
- **Proxy URL construction**: The WASM client rewrites `BaseUrl` to `{hostBase}/api/proxy/{hostname}` so API calls route through the YARP proxy on the server.
- **Service layer**: `IAnalyticsDataService` / `AnalyticsDataService` wraps the SDK's `IAnalyticsService`, `ISessionsService`, `IEventsService`, and `IRealTimeEventsService` to provide page-specific data methods. Each method builds queries using the SDK's fluent `QueryBuilder`.
- **Dimension values**: Piwik PRO API returns dimensions as JSON arrays `[key, label]`. The `GetDictionaryValue` helper extracts the last (human-readable) element; `GetDimensionKeyAsObject` extracts the first (machine key).
- **Filters**: `DimensionalFilter` model with `ApplyFilters()` (for aggregate queries) and `BuildRawDataFilters()` (for raw data/sessions APIs).
- **App selection**: `AppSelectorService` lets users switch between Piwik PRO apps/sites; `ApplyAppSelection()` sets the website ID on queries.
- **Pages use code-behind pattern**: Each `.razor` page has a corresponding `.razor.cs` partial class.

### SDK Extension Points (from NuGet package)

- `services.AddPiwikProAnalytics(options => ...)` — registers analytics services
- `services.AddPiwikProTracking()` — registers tracking services
- Key SDK types: `Dimensions.*`, `Metrics.*`, `DateRangeUtilities`, `QueryBuilder`

## Configuration

The Blazor app credentials go in `samples/PiwikPROSamples.BlazorSample.Client/wwwroot/appsettings.json` under the `PiwikPRO` section, or are managed at runtime through the Settings/AddConnection pages stored in localStorage.

The YARP proxy configuration lives in `samples/PiwikPROSamples.BlazorSample.Server/appsettings.json` under `ReverseProxy`.
