# Blazor Analytics Dashboard Sample

A full-featured analytics dashboard built as a Progressive Web App (PWA) using Blazor WebAssembly and MudBlazor. It demonstrates the Piwik PRO .NET SDK's Analytics Query API, Sessions API, Events API, and Real-Time Events API.

## Architecture

This sample consists of two projects:

- **PiwikPROSamples.BlazorSample.Client** - Blazor WebAssembly app that runs in the browser. All SDK calls and business logic execute client-side.
- **PiwikPROSamples.BlazorSample.Server** - ASP.NET Core host that serves the WASM app and acts as a YARP reverse proxy to the Piwik PRO API (to bypass browser CORS restrictions).

You must run the **Server** project. It hosts the client and proxies API requests.

## Running

```bash
dotnet run --project samples/PiwikPROSamples.BlazorSample.Server
```

Open `https://localhost:5001` (or the port shown in the console).

## Configuration

On first launch, the app will redirect you to the **Add Connection** page where you can enter your Piwik PRO credentials. Connections are saved in browser localStorage.

Alternatively, edit `samples/PiwikPROSamples.BlazorSample.Client/wwwroot/appsettings.json`:

```json
{
  "PiwikPRO": {
    "BaseUrl": "https://your-instance.piwik.pro",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "WebSiteId": "your-website-id"
  }
}
```

## Features

### Analytics Modules

- **Dashboard** - Key metrics (sessions, page views, bounce rate), traffic source chart, trends
- **Real-Time** - Live event stream and live page map
- **Acquisition** - Traffic sources, channels, campaigns
- **Behavior** - Top pages, page detail (funnel: previous/next pages), outlinks, downloads
- **Visitors** - Overview, technology breakdown, locations, engagement (session time distribution)
- **Sessions** - Raw session data table with drill-down to individual events
- **Goals** - Overview and conversion tracking
- **Trends** - Time-series analysis of sessions and page views

### UI Features

- Date range picker with quick presets (7/30/90 days)
- Dimensional filtering - click filter icons on data rows to cross-filter all views
- App/site switcher for multi-site accounts
- Interactive data grids with sorting, filtering, and search
- Line, bar, pie, and donut charts
- Installable as a PWA on desktop and mobile

## SDK APIs Demonstrated

| API | Service Interface | Used In |
|-----|-------------------|---------|
| [Analytics Query](../../docs/analytics-queries.md) | `IAnalyticsService` | Dashboard, Acquisition, Behavior, Visitors, Trends, Goals |
| [Sessions](../../docs/sessions-api.md) | `ISessionsService` | Sessions page |
| Events | `IEventsService` | Session detail dialog |
| Real-Time Events | `IRealTimeEventsService` | Real-Time Events page, Live Map |
| Apps | `IAppsService` | App selector in header |

## Key Code Paths

- `Services/AnalyticsDataService.cs` - Wraps SDK services into page-specific data methods using the fluent `QueryBuilder`
- `Services/ConnectionStateService.cs` - Manages Piwik PRO connections in localStorage
- `Services/FilterStateService.cs` - In-memory dimensional filter state shared across pages
- `Program.cs` - Reads connection from localStorage before host startup via JS interop

## Adding a New Page

1. Create a `.razor` file (and optional `.razor.cs` code-behind) in `Pages/`
2. Add the `@page "/your-route"` directive
3. Inject `IAnalyticsDataService` (or any SDK service directly)
4. Add a nav link in `Layout/NavMenu.razor`
