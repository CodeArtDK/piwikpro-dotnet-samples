# Analytics Query API

The Analytics Query API lets you retrieve aggregated analytics data from Piwik PRO using a fluent query builder. You can query dimensions (e.g., page URL, browser, country), metrics (e.g., sessions, page views, bounce rate), and apply filters, sorting, and pagination.

## Setup

```csharp
using PiwikPRO.Analytics.Extensions;

services.AddPiwikProAnalytics(options =>
{
    options.BaseUrl = "https://your-instance.piwik.pro";
    options.ClientId = "your-client-id";
    options.ClientSecret = "your-client-secret";
    options.WebSiteId = "your-website-id";
    options.TimeoutSeconds = 30; // optional, default 30
});
```

Then inject `IAnalyticsService` wherever you need it.

## Basic Query

```csharp
var response = await analyticsService.QueryAsync(builder =>
    builder.AddDimension(Dimensions.Date.DateDimension)
           .AddMetric(Metrics.Sessions.TotalSessions)
           .AddMetric(Metrics.Pages.PageViews)
           .SetLastDays(7)
           .OrderByDescending(Metrics.Sessions.TotalSessions)
           .Limit(10));

foreach (var row in response.Data)
{
    var date = row.Dimensions["date"];
    var sessions = row.Metrics["sessions"];
    var pageViews = row.Metrics["page_views"];
}
```

## Query Builder API

### Adding Dimensions and Metrics

```csharp
// Single
builder.AddDimension(Dimensions.Page.PageUrl);
builder.AddMetric(Metrics.Sessions.TotalSessions);

// Multiple at once
builder.AddDimensions(Dimensions.Page.PageUrl, Dimensions.Device.Browser);
builder.AddMetrics(Metrics.Sessions.TotalSessions, Metrics.Sessions.BounceRate);
```

### Date Ranges

```csharp
// Shorthand
builder.SetLastDays(30);

// Explicit range
builder.SetDateRange(startDate, endDate);

// Using DateRangeUtilities
var range = DateRangeUtilities.CurrentMonth();
builder.SetDateRange(range.Start, range.End);
```

Available utilities: `Today()`, `Yesterday()`, `LastDays(n)`, `CurrentMonth()`, `PreviousMonth()`, `CurrentQuarter()`, `CurrentYear()`.

### Filtering

```csharp
builder.WhereContains(Dimensions.Page.PageUrl, "/product");
builder.WhereEquals("event_url", "https://example.com/checkout");
builder.WhereIn(Dimensions.Device.Browser, "Chrome", "Firefox", "Safari");
```

### Sorting and Pagination

```csharp
builder.OrderByDescending(Metrics.Sessions.TotalSessions);
builder.OrderBy("timestamp");
builder.Limit(50);
builder.Paginate(page: 1, pageSize: 50);
```

### Column Transformations

```csharp
// Compute average time on page as a derived metric
builder.Average(Dimensions.Timing.TimeOnPage, "avg_time_on_page");
```

### Building Queries Separately

```csharp
var query = analyticsService.CreateQuery()
    .AddDimensions(Dimensions.Page.PageUrl, Dimensions.Device.Browser)
    .AddMetrics(Metrics.Sessions.TotalSessions, Metrics.Sessions.BounceRate)
    .SetDateRange(startDate, endDate)
    .WhereContains(Dimensions.Page.PageUrl, "/product")
    .OrderByDescending(Metrics.Sessions.TotalSessions)
    .Paginate(page: 1, pageSize: 50);

var response = await analyticsService.QueryAsync(query.Build());
```

## Streaming Large Results

For queries that return many rows, use streaming to process results incrementally without loading everything into memory:

```csharp
var query = analyticsService.CreateQuery()
    .AddDimension(Dimensions.Date.DateDimension)
    .AddDimension(Dimensions.Page.PageUrl)
    .AddMetric(Metrics.Sessions.TotalSessions)
    .SetLastDays(30)
    .Build();

await foreach (var row in analyticsService.QueryStreamAsync(query, pageSize: 100))
{
    // Process each row as it arrives
    var pageUrl = row.Dimensions["page_url"];
    var sessions = row.Metrics["sessions"];
}
```

## Dimension Values

The Piwik PRO API returns dimension values as JSON arrays with the format `[key, label]`. For example, a country dimension might return `[45, "Denmark"]`.

When reading response data:
- The **last element** is the human-readable label
- The **first element** is the machine key (used for filtering)

## Common Dimensions

| Category | Constant | API Column |
|----------|----------|------------|
| Date | `Dimensions.Date.DateDimension` | `timestamp` |
| Pages | `Dimensions.Page.PageUrl` | `event_url` |
| Pages | `Dimensions.Page.PageTitle` | `page_title` |
| Device | `Dimensions.Device.Browser` | `browser_name` |
| Device | `Dimensions.Device.DeviceType` | `device_type` |
| Geography | `Dimensions.Geography.Country` | `location_country_name` |
| Traffic | `Dimensions.TrafficSource.Source` | `source` |
| Traffic | `Dimensions.TrafficSource.ReferrerType` | `referrer_type` |
| Session | `Dimensions.Session.SessionTotalTime` | `session_total_time` |
| Links | `Dimensions.Links.OutlinkUrl` | `outlink_url` |
| Links | `Dimensions.Links.DownloadUrl` | `download_url` |
| Links | `Dimensions.Links.NextEventUrl` | `next_event_url` |
| Links | `Dimensions.Links.PreviousEventUrl` | `previous_event_url` |

## Common Metrics

| Category | Constant | API Column |
|----------|----------|------------|
| Sessions | `Metrics.Sessions.TotalSessions` | `sessions` |
| Sessions | `Metrics.Sessions.BounceRate` | `bounce_rate` |
| Pages | `Metrics.Pages.PageViews` | `page_views` |
| Pages | `Metrics.Pages.UniquePageViews` | `unique_page_views` |
| Pages | `Metrics.Pages.Entries` | `entries` |
| Pages | `Metrics.Pages.Exits` | `exits` |
| Events | `Metrics.Events.Outlinks` | `outlinks` |
| Events | `Metrics.Events.Downloads` | `downloads` |

## Custom Dimensions

```csharp
var customDimensions = await analyticsService.ListCustomDimensionsAsync();

foreach (var dim in customDimensions.Data)
{
    Console.WriteLine($"{dim.Attributes.Name} (Slot: {dim.Attributes.Slot}, Scope: {dim.Attributes.Scope})");
}
```

## API Info

```csharp
var queryInfo = await analyticsService.GetQueryApiInfoAsync();
var reportsInfo = await analyticsService.GetReportsApiInfoAsync();
```

## See Also

- [Console Sample](../samples/PiwikPROSamples.ConsoleSample/) - Working examples of all query patterns
- [Sessions API](sessions-api.md) - Raw session-level data
- [Goals API](goals-api.md) - Goal management and conversions
