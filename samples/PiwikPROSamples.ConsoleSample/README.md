# Console Sample

A console application demonstrating the core capabilities of the Piwik PRO .NET SDK: analytics queries, streaming, server-side tracking, JavaScript code generation, goals management, and custom dimensions.

## Running

Set your credentials as environment variables:

```bash
export PIWIKPRO_BASE_URL="https://your-instance.piwik.pro"
export PIWIKPRO_CLIENT_ID="your-client-id"
export PIWIKPRO_CLIENT_SECRET="your-client-secret"
export PIWIKPRO_WEBSITE_ID="your-website-id"
```

Then run:

```bash
dotnet run --project samples/PiwikPROSamples.ConsoleSample
```

## What It Demonstrates

### Analytics Queries (`Program.cs`)

- **Simple query** - Sessions and page views by date, last 7 days
- **Complex query** - Multi-dimension query with filters (`WhereContains`, `WhereIn`), sorting, and pagination
- **Date range utilities** - `Today()`, `Yesterday()`, `LastDays()`, `CurrentMonth()`, `PreviousMonth()`, `CurrentQuarter()`, `CurrentYear()`
- **Streaming** - `QueryStreamAsync` for processing large result sets incrementally
- **Custom dimensions** - `ListCustomDimensionsAsync` to enumerate custom dimensions configured on a site

### Server-Side Tracking (`Program.cs`)

- Page view tracking
- Goal conversion with revenue
- File download tracking
- Site search tracking with categories
- Outlink tracking

### JavaScript Code Generation (`Program.cs`)

- Standard tracking code (`_paq` variable)
- Matomo-compatible code (`_ppas` variable)
- Tag Manager + Consent Manager integration
- Custom site ID

### Goals Management (`GoalsExamples.cs`)

- List all goals
- Create a goal
- Update a goal
- Delete a goal

## SDK Services Used

| Service | NuGet Package | Description |
|---------|---------------|-------------|
| `IAnalyticsService` | `PiwikPRO.Analytics` | Query API, streaming, custom dimensions |
| `ITrackingService` | `PiwikPRO.Tracking` | Server-side event tracking |
| `IJavaScriptTrackingCodeGenerator` | `PiwikPRO.Tracking` | JS snippet generation |
| `IGoalsService` | `PiwikPRO.Analytics` | Goal CRUD operations |

## Related Documentation

- [Analytics Query API](../../docs/analytics-queries.md)
- [Tracking API](../../docs/tracking-api.md)
- [Goals API](../../docs/goals-api.md)
