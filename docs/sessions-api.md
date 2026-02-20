# Sessions API

The Sessions API provides access to raw session-level data from Piwik PRO, including visitor behavior, traffic sources, device information, and geographic data.

## Setup

Register analytics services (the Sessions API is included):

```csharp
services.AddPiwikProAnalytics(options =>
{
    options.BaseUrl = "https://your-instance.piwik.pro";
    options.ClientId = "your-client-id";
    options.ClientSecret = "your-client-secret";
    options.WebSiteId = "your-website-id";
});
```

Then inject `ISessionsService`.

## Basic Usage

```csharp
var request = sessionsService.CreateRequest();
request.DateFrom = "2024-01-01";
request.DateTo = "2024-01-31";
request.Limit = 100;

// Add columns (session_id, visitor_id, and timestamp are returned by default)
request.Columns = new List<SessionColumn>
{
    new SessionColumn { ColumnId = "source" },
    new SessionColumn { ColumnId = "medium" },
    new SessionColumn { ColumnId = "device_type" },
    new SessionColumn { ColumnId = "browser_name" },
    new SessionColumn { ColumnId = "location_country_name" }
};

var response = await sessionsService.FetchSessionsDataAsync(request);

foreach (var session in response.Sessions)
{
    Console.WriteLine($"{session.SessionId} - {session.Timestamp}");
    Console.WriteLine($"  Source: {session.Data["source"]}");
    Console.WriteLine($"  Device: {session.Data["device_type"]}");
}
```

## Date Ranges

```csharp
// Absolute dates
request.DateFrom = "2024-01-01";
request.DateTo = "2024-01-31";

// Relative dates
request.RelativeDate = "last_7_days";
// Options: today, yesterday, last_week, last_month, last_year, last_X_days
```

## Filtering

```csharp
request.Filters = new RawDataFilter
{
    Operator = "and",
    Conditions = new List<FilterCondition>
    {
        new FilterCondition
        {
            ColumnId = "source",
            Condition = new ConditionOperator { Operator = "eq", Value = "google" }
        }
    }
};
```

## Sorting

```csharp
request.OrderBy = new List<OrderBy>
{
    new OrderBy { ColumnId = "timestamp", Direction = "desc" }
};
```

## Pagination

```csharp
request.Limit = 100;   // Results per page (1-100000)
request.Offset = 0;    // Skip this many results
```

## Available Columns

**Default columns** (always returned, do not include in `Columns`):
- `session_id`, `visitor_id`, `timestamp`

**Traffic source**: `source`, `medium`, `campaign_name`, `campaign_id`

**Device**: `device_type`, `browser_name`, `browser_version`, `operating_system`

**Geography**: `location_country_name`, `location_subdivision_1_name`, `location_city_name`

## Events API

The Events API works similarly for event-level data. Inject `IEventsService`:

```csharp
var request = eventsService.CreateRequest();
request.DateFrom = "2024-01-01";
request.DateTo = "2024-01-31";
request.Limit = 1000;

request.Columns = new List<EventColumn>
{
    new EventColumn { ColumnId = "event_type" },
    new EventColumn { ColumnId = "event_url" }
};

// Filter to a specific session
request.Filters = new RawDataFilter
{
    Operator = "and",
    Conditions = new List<FilterCondition>
    {
        new FilterCondition
        {
            ColumnId = "session_id",
            Condition = new ConditionOperator { Operator = "eq", Value = sessionId }
        }
    }
};

request.OrderBy = new List<EventOrderBy>
{
    new EventOrderBy { ColumnId = "timestamp", Direction = "asc" }
};

var response = await eventsService.FetchEventsDataAsync(request);
```

## Real-Time Events API

For near-real-time data, inject `IRealTimeEventsService`:

```csharp
var request = realTimeEventsService.CreateRequest();
request.DateFrom = DateTime.UtcNow.AddMinutes(-60).ToString("yyyy-MM-dd HH:mm:ss");
request.DateTo = DateTime.UtcNow.AddMinutes(-3).ToString("yyyy-MM-dd HH:mm:ss");
request.Limit = 100;

request.Columns = new List<RealTimeEventColumn>
{
    new RealTimeEventColumn { ColumnId = "visitor_id" },
    new RealTimeEventColumn { ColumnId = "event_type" },
    new RealTimeEventColumn { ColumnId = "event_url" },
    new RealTimeEventColumn { ColumnId = "source" },
    new RealTimeEventColumn { ColumnId = "device_type" },
    new RealTimeEventColumn { ColumnId = "location_country_name" }
};

var response = await realTimeEventsService.FetchRealTimeEventsDataAsync(request);
```

## See Also

- [Blazor Sample](../samples/PiwikPROSamples.BlazorSample.Server/) - Sessions page with interactive data grid
- [Analytics Queries](analytics-queries.md) - Aggregated query API
