namespace PiwikPROSamples.BlazorSample.Client.Models;

public class DashboardMetrics
{
    public int TotalSessions { get; set; }
    public int TotalPageViews { get; set; }
    public int TotalConversions { get; set; }
    public double BounceRate { get; set; }
}

public class AcquisitionData
{
    public string Source { get; set; } = string.Empty;
    public object SourceKey { get; set; } = string.Empty;
    public int Sessions { get; set; }
    public int PageViews { get; set; }
    public double BounceRate { get; set; }
}

public class ChannelData
{
    public string Channel { get; set; } = string.Empty;
    public object ChannelKey { get; set; } = string.Empty;
    public int Sessions { get; set; }
    public int PageViews { get; set; }
    public double BounceRate { get; set; }
}

public class BehaviorData
{
    public string PageUrl { get; set; } = string.Empty;
    public string PageTitle { get; set; } = string.Empty;
    public int PageViews { get; set; }
    public int UniquePageViews { get; set; }
}

public class PageDetailData
{
    public string PageUrl { get; set; } = string.Empty;
    public int PageViews { get; set; }
    public int UniquePageViews { get; set; }
    public int Entries { get; set; }
    public int Exits { get; set; }
    public double BounceRate { get; set; }
    public string NextPageUrl { get; set; } = string.Empty;
    public int NextPageViews { get; set; }

    // Extended info
    public List<TrendData> Trend { get; set; } = new();
    public List<AcquisitionData> Acquisition { get; set; } = new();
    public List<NextPageData> NextPages { get; set; } = new();
    public List<PreviousPageData> PreviousPages { get; set; } = new();

    // Engagement
    public double AverageTimeOnPageSeconds { get; set; }
}

public class NextPageData
{
    public string Url { get; set; } = string.Empty;
    public int PageViews { get; set; }
}

public class PreviousPageData
{
    public string Url { get; set; } = string.Empty;
    public int PageViews { get; set; }
}

public class VisitorData
{
    public string Browser { get; set; } = string.Empty;
    public string BrowserKey { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string DeviceTypeKey { get; set; } = string.Empty;
    public int Sessions { get; set; }
}

public class GoalData
{
    public string Date { get; set; } = string.Empty;
    public int Conversions { get; set; }
    public double ConversionRate { get; set; }
}

public class TrendData
{
    public string Date { get; set; } = string.Empty;
    public int Sessions { get; set; }
    public int PageViews { get; set; }
}

public class LocationData
{
    public string Country { get; set; } = string.Empty;
    public string CountryKey { get; set; } = string.Empty;
    public int Sessions { get; set; }
    public int PageViews { get; set; }
    public double BounceRate { get; set; }
}

public class EngagementData
{
    public string TimeRange { get; set; } = string.Empty;
    public int Sessions { get; set; }
    public double Percentage { get; set; }
}

public class OutlinkData
{
    public string OutlinkUrl { get; set; } = string.Empty;
    public int Clicks { get; set; }
    public int UniqueClicks { get; set; }
}

public class DownloadData
{
    public string DownloadUrl { get; set; } = string.Empty;
    public int Downloads { get; set; }
    public int UniqueDownloads { get; set; }
}

public class EventTrendData
{
    public string Date { get; set; } = string.Empty;
    public int Outlinks { get; set; }
    public int Downloads { get; set; }
}

public class SessionData
{
    public string SessionId { get; set; } = string.Empty;
    public string VisitorId { get; set; } = string.Empty;
    public DateTime? Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Medium { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
}

public class EventData
{
    public DateTime? Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventUrl { get; set; } = string.Empty;
    public string EventAction { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string EventCategory { get; set; } = string.Empty;
}

public class RealTimeEventData
{
    public string EventId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string VisitorId { get; set; } = string.Empty;
    public DateTime? Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventUrl { get; set; } = string.Empty;
    public string EventAction { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Medium { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

// Live Map Models
public class LiveMapNode
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string UrlPath { get; set; } = string.Empty; // The path segment (e.g., "/blog/2022")
    public int Traffic { get; set; }
    public int TotalTraffic { get; set; } // Includes child traffic when collapsed
    public bool Expanded { get; set; }
    public string? ParentId { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
    public int Depth { get; set; } // Tree depth level
    public List<string> ChildIds { get; set; } = new(); // Direct child node IDs
}

public class LiveMapLink
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public int Traffic { get; set; }
}

public class LiveMapPageInfo
{
    public string PageUrl { get; set; } = string.Empty;
    public string PageTitle { get; set; } = string.Empty;
    public int ActiveUsers { get; set; }
    public int TrafficLastHour { get; set; }
    public List<LiveMapVisitor> LatestVisitors { get; set; } = new();
    public List<LiveMapPath> TypicalPaths { get; set; } = new();
}

public class LiveMapVisitor
{
    public string Location { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
}

public class LiveMapPath
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public int Count { get; set; }
}
