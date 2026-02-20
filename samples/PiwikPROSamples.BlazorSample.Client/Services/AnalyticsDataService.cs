using PiwikPRO.Analytics;
using PiwikPROSamples.BlazorSample.Client.Models;
using PiwikPRO.Analytics.Builders;
using PiwikPRO.Analytics.Constants;
using PiwikPRO.Analytics.Models;
using PiwikPRO.Analytics.Models.Events;
using PiwikPRO.Analytics.Models.RealTimeEvents;
using PiwikPRO.Analytics.Models.Sessions;
using PiwikPRO.Analytics.Services;

namespace PiwikPROSamples.BlazorSample.Client.Services;

public class AnalyticsDataService : IAnalyticsDataService
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ISessionsService _sessionsService;
    private readonly IEventsService _eventsService;
    private readonly IRealTimeEventsService _realTimeEventsService;
    private readonly IAppSelectorService _appSelectorService;

    public AnalyticsDataService(
        IAnalyticsService analyticsService,
        ISessionsService sessionsService,
        IEventsService eventsService,
        IRealTimeEventsService realTimeEventsService,
        IAppSelectorService appSelectorService)
    {
        _analyticsService = analyticsService;
        _sessionsService = sessionsService;
        _eventsService = eventsService;
        _realTimeEventsService = realTimeEventsService;
        _appSelectorService = appSelectorService;
    }

    private void ApplyFilters(QueryBuilder builder, List<DimensionalFilter>? filters)
    {
        if (filters == null || !filters.Any())
            return;

        foreach (var filter in filters)
        {
            builder.WhereEquals(filter.Dimension, filter.Value);
        }
    }

    private RawDataFilter? BuildRawDataFilters(List<DimensionalFilter>? filters)
    {
        if (filters == null || !filters.Any())
            return null;

        return new RawDataFilter
        {
            Operator = "and",
            Conditions = filters.Select(f => new FilterCondition
            {
                ColumnId = f.Dimension,
                Condition = new ConditionOperator
                {
                    Operator = f.Operator,
                    Value = f.Value
                }
            }).ToList()
        };
    }

    private void ApplyAppSelection(QueryBuilder builder)
    {
        var selectedAppId = _appSelectorService.GetSelectedAppId();
        if (!string.IsNullOrWhiteSpace(selectedAppId))
        {
            builder.SetWebSiteId(selectedAppId);
        }
    }

    public async Task<DashboardMetrics> GetDashboardMetricsAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null)
    {
        var response = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddMetric(Metrics.Sessions.TotalSessions)
                   .AddMetric(Metrics.Pages.PageViews)
                   .AddMetric(Metrics.Sessions.BounceRate)
                   .SetDateRange(startDate, endDate);
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);
        });

        if (response == null || !response.Data.Any())
        {
            return new DashboardMetrics
            {
                TotalSessions = 0,
                TotalPageViews = 0,
                TotalConversions = 0,
                BounceRate = 0.0
            };
        }

        var row = response.Data.First();
        return new DashboardMetrics
        {
            TotalSessions = GetDictionaryValue(row.Metrics, "sessions", 0),
            TotalPageViews = GetDictionaryValue(row.Metrics, "page_views", 0),
            TotalConversions = 0,
            BounceRate = GetDictionaryValue(row.Metrics, "bounce_rate", 0.0)
        };
    }

    public async Task<List<AcquisitionData>> GetAcquisitionDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null)
    {
        var response = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.TrafficSource.Source)
                   .AddMetric(Metrics.Sessions.TotalSessions)
                   .AddMetric(Metrics.Pages.PageViews)
                   .AddMetric(Metrics.Sessions.BounceRate)
                   .SetDateRange(startDate, endDate)
                   .OrderByDescending("sessions");
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);

        });

        if (response == null || !response.Data.Any())
        {
            return new List<AcquisitionData>();
        }

        return response.Data.Select(row => new AcquisitionData
        {
            Source = GetDictionaryValue(row.Dimensions, "source", "Unknown"),
            SourceKey = GetDimensionKeyAsObject(row.Dimensions, "source", "Unknown"),
            Sessions = GetDictionaryValue(row.Metrics, "sessions", 0),
            PageViews = GetDictionaryValue(row.Metrics, "page_views", 0),
            BounceRate = GetDictionaryValue(row.Metrics, "bounce_rate", 0.0)
        }).ToList();
    }

    public async Task<List<BehaviorData>> GetBehaviorDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null)
    {
        var response = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.Page.PageUrl)
                   .AddMetric(Metrics.Pages.PageViews)
                   .AddMetric(Metrics.Pages.UniquePageViews)
                   .SetDateRange(startDate, endDate)
                   .OrderByDescending("page_views")
                   .Limit(20);
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);

        });

        if (response == null || !response.Data.Any())
        {
            return new List<BehaviorData>();
        }

        return response.Data.Select(row => new BehaviorData
        {
            PageUrl = GetDictionaryValue(row.Dimensions, "event_url", "Unknown"),
            PageViews = GetDictionaryValue(row.Metrics, "page_views", 0),
            UniquePageViews = GetDictionaryValue(row.Metrics, "unique_page_views", 0)
        }).ToList();
    }

    public async Task<List<VisitorData>> GetVisitorDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null)
    {
        var response = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.Device.Browser)
                   .AddDimension(Dimensions.Device.DeviceType)
                   .AddMetric(Metrics.Sessions.TotalSessions)
                   .SetDateRange(startDate, endDate)
                   .OrderByDescending("sessions")
                   .Limit(20);
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);

        });

        if (response == null || !response.Data.Any())
        {
            return new List<VisitorData>();
        }

        return response.Data.Select(row => new VisitorData
        {
            Browser = GetDictionaryValue(row.Dimensions, "browser_name", "Unknown"),
            BrowserKey = GetDimensionKey(row.Dimensions, "browser_name", "Unknown"),
            DeviceType = GetDictionaryValue(row.Dimensions, "device_type", "Unknown"),
            DeviceTypeKey = GetDimensionKey(row.Dimensions, "device_type", "Unknown"),
            Sessions = GetDictionaryValue(row.Metrics, "sessions", 0)
        }).ToList();
    }

    public async Task<List<GoalData>> GetGoalsDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null)
    {
        var response = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.Date.DateDimension)
                   .AddMetric(Metrics.Sessions.TotalSessions)
                   .SetDateRange(startDate, endDate)
                   .OrderBy("timestamp");
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);

        });

        if (response == null || !response.Data.Any())
        {
            return new List<GoalData>();
        }

        return response.Data.Select(row => new GoalData
        {
            Date = GetDictionaryValue(row.Dimensions, "timestamp", DateTime.Today.ToString()),
            Conversions = 0,
            ConversionRate = 0.0
        }).ToList();
    }

    public async Task<List<TrendData>> GetTrendsDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null)
    {
        var response = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.Date.DateDimension)
                   .AddMetric(Metrics.Sessions.TotalSessions)
                   .AddMetric(Metrics.Pages.PageViews)
                   .SetDateRange(startDate, endDate)
                   .OrderBy("timestamp");
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);

        });

        if (response == null || !response.Data.Any())
        {
            return new List<TrendData>();
        }

        return response.Data.Select(row =>
        {
            var timestamp = GetDictionaryValue(row.Dimensions, "timestamp", DateTime.Today.ToString());
            // Try to parse the timestamp and format it as a short date (dd/MM)
            DateTime parsedDate;
            string formattedDate;
            if (DateTime.TryParse(timestamp, out parsedDate))
            {
                formattedDate = parsedDate.ToString("dd/MM");
            }
            else
            {
                formattedDate = timestamp;
            }

            return new TrendData
            {
                Date = formattedDate,
                Sessions = GetDictionaryValue(row.Metrics, "sessions", 0),
                PageViews = GetDictionaryValue(row.Metrics, "page_views", 0)
            };
        }).ToList();
    }

    public async Task<List<ChannelData>> GetChannelDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null)
    {
        var response = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.TrafficSource.ReferrerType)
                   .AddMetric(Metrics.Sessions.TotalSessions)
                   .AddMetric(Metrics.Pages.PageViews)
                   .AddMetric(Metrics.Sessions.BounceRate)
                   .SetDateRange(startDate, endDate)
                   .OrderByDescending("sessions");
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);

        });

        if (response == null || !response.Data.Any())
        {
            return new List<ChannelData>();
        }

        return response.Data.Select(row => new ChannelData
        {
            Channel = GetDictionaryValue(row.Dimensions, "referrer_type", "Unknown"),
            ChannelKey = GetDimensionKeyAsObject(row.Dimensions, "referrer_type", "Unknown"),
            Sessions = GetDictionaryValue(row.Metrics, "sessions", 0),
            PageViews = GetDictionaryValue(row.Metrics, "page_views", 0),
            BounceRate = GetDictionaryValue(row.Metrics, "bounce_rate", 0.0)
        }).ToList();
    }

    public async Task<List<LocationData>> GetLocationDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null)
    {
        var response = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.Geography.Country)
                   .AddMetric(Metrics.Sessions.TotalSessions)
                   .AddMetric(Metrics.Pages.PageViews)
                   .AddMetric(Metrics.Sessions.BounceRate)
                   .SetDateRange(startDate, endDate)
                   .OrderByDescending("sessions")
                   .Limit(20);
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);

        });

        if (response == null || !response.Data.Any())
        {
            return new List<LocationData>();
        }

        return response.Data.Select(row => new LocationData
        {
            Country = GetDictionaryValue(row.Dimensions, "location_country_name", "Unknown"),
            CountryKey = GetDimensionKey(row.Dimensions, "location_country_name", "Unknown"),
            Sessions = GetDictionaryValue(row.Metrics, "sessions", 0),
            PageViews = GetDictionaryValue(row.Metrics, "page_views", 0),
            BounceRate = GetDictionaryValue(row.Metrics, "bounce_rate", 0.0)
        }).ToList();
    }

    public async Task<List<EngagementData>> GetEngagementDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null)
    {
        // Get session time data
        var response = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.Session.SessionTotalTime)
                   .AddMetric(Metrics.Sessions.TotalSessions)
                   .SetDateRange(startDate, endDate)
                   .OrderBy("session_total_time");
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);

        });

        if (response == null || !response.Data.Any())
        {
            return new List<EngagementData>();
        }

        // Group session times into ranges
        var engagementData = new Dictionary<string, int>
        {
            ["0-10s"] = 0,
            ["11-30s"] = 0,
            ["31-60s"] = 0,
            ["1-3m"] = 0,
            ["3-10m"] = 0,
            ["10m+"] = 0
        };

        foreach (var row in response.Data)
        {
            var sessionTime = GetDictionaryValue(row.Dimensions, "session_total_time", 0);
            var sessions = GetDictionaryValue(row.Metrics, "sessions", 0);

            if (sessionTime <= 10)
                engagementData["0-10s"] += sessions;
            else if (sessionTime <= 30)
                engagementData["11-30s"] += sessions;
            else if (sessionTime <= 60)
                engagementData["31-60s"] += sessions;
            else if (sessionTime <= 180)
                engagementData["1-3m"] += sessions;
            else if (sessionTime <= 600)
                engagementData["3-10m"] += sessions;
            else
                engagementData["10m+"] += sessions;
        }

        var totalSessions = engagementData.Values.Sum();
        if (totalSessions == 0)
        {
            return new List<EngagementData>();
        }

        return engagementData.Select(kvp => new EngagementData
        {
            TimeRange = kvp.Key,
            Sessions = kvp.Value,
            Percentage = (double)kvp.Value / totalSessions * 100
        }).ToList();
    }

    public async Task<PageDetailData?> GetPageDetailDataAsync(string pageUrl, DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null)
    {
        // Get page metrics (include avg time on page via transformation)
        var response = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.Page.PageUrl)
                   .AddMetric(Metrics.Pages.PageViews)
                   .AddMetric(Metrics.Pages.UniquePageViews)
                   .AddMetric(Metrics.Pages.Entries)
                   .AddMetric(Metrics.Pages.Exits)
                   .AddMetric(Metrics.Pages.BounceRateEvents)
                   .Average(Dimensions.Timing.TimeOnPage, "avg_time_on_page")
                   .SetDateRange(startDate, endDate)
                   .WhereEquals("event_url", pageUrl)
                   .Limit(1);
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);

        });

        if (response == null || !response.Data.Any())
        {
            return null;
        }

        var row = response.Data.First();
        var pageDetail = new PageDetailData
        {
            PageUrl = GetDictionaryValue(row.Dimensions, "event_url", pageUrl),
            PageViews = GetDictionaryValue(row.Metrics, "page_views", 0),
            UniquePageViews = GetDictionaryValue(row.Metrics, "unique_page_views", 0),
            Entries = GetDictionaryValue(row.Metrics, "entries", 0),
            Exits = GetDictionaryValue(row.Metrics, "exits", 0),
            BounceRate = GetDictionaryValue(row.Metrics, "bounce_rate_events", 0.0),
            AverageTimeOnPageSeconds = GetDictionaryValue(row.Metrics, "avg_time_on_page", 0.0)
        };

        // Get daily trend for this page
        var trendResponse = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.Date.DateDimension)
                   .AddMetric(Metrics.Sessions.TotalSessions)
                   .AddMetric(Metrics.Pages.PageViews)
                   .SetDateRange(startDate, endDate)
                   .WhereEquals("event_url", pageUrl)
                   .OrderBy("timestamp");
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);

        });
        if (trendResponse != null && trendResponse.Data.Any())
        {
            pageDetail.Trend = trendResponse.Data.Select(tr =>
            {
                var timestamp = GetDictionaryValue(tr.Dimensions, "timestamp", DateTime.Today.ToString());
                DateTime parsedDate;
                var label = DateTime.TryParse(timestamp, out parsedDate) ? parsedDate.ToString("dd/MM") : timestamp;
                return new TrendData
                {
                    Date = label,
                    Sessions = GetDictionaryValue(tr.Metrics, "sessions", 0),
                    PageViews = GetDictionaryValue(tr.Metrics, "page_views", 0)
                };
            }).ToList();
        }

        // Get acquisition breakdown for this page
        var acqResponse = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.TrafficSource.Source)
                   .AddMetric(Metrics.Sessions.TotalSessions)
                   .AddMetric(Metrics.Pages.PageViews)
                   .AddMetric(Metrics.Sessions.BounceRate)
                   .SetDateRange(startDate, endDate)
                   .WhereEquals("event_url", pageUrl)
                   .OrderByDescending("sessions");
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);

        });
        if (acqResponse != null && acqResponse.Data.Any())
        {
            pageDetail.Acquisition = acqResponse.Data.Select(ar => new AcquisitionData
            {
                Source = GetDictionaryValue(ar.Dimensions, "source", "Unknown"),
                Sessions = GetDictionaryValue(ar.Metrics, "sessions", 0),
                PageViews = GetDictionaryValue(ar.Metrics, "page_views", 0),
                BounceRate = GetDictionaryValue(ar.Metrics, "bounce_rate", 0.0)
            }).ToList();
        }

        // Get next pages in funnel (top 10)
        var nextPagesResponse = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.Links.NextEventUrl)
                   .AddMetric(Metrics.Pages.PageViews)
                   .SetDateRange(startDate, endDate)
                   .WhereEquals("event_url", pageUrl)
                   .OrderByDescending("page_views")
                   .Limit(10);
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);

        });

        if (nextPagesResponse != null && nextPagesResponse.Data.Any())
        {
            // Legacy single next page (keep it for display if needed)
            var nextRow = nextPagesResponse.Data.First();
            pageDetail.NextPageUrl = GetDictionaryValue(nextRow.Dimensions, "next_event_url", string.Empty);
            pageDetail.NextPageViews = GetDictionaryValue(nextRow.Metrics, "page_views", 0);

            // Full list
            pageDetail.NextPages = nextPagesResponse.Data
                .Select(r => new NextPageData
                {
                    Url = GetDictionaryValue(r.Dimensions, "next_event_url", string.Empty),
                    PageViews = GetDictionaryValue(r.Metrics, "page_views", 0)
                })
                .Where(p => !string.IsNullOrWhiteSpace(p.Url))
                .ToList();
        }

        // Get previous pages in funnel (top 10)
        var previousPagesResponse = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.Links.PreviousEventUrl)
                   .AddMetric(Metrics.Pages.PageViews)
                   .SetDateRange(startDate, endDate)
                   .WhereEquals("event_url", pageUrl)
                   .OrderByDescending("page_views")
                   .Limit(10);
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);

        });

        if (previousPagesResponse != null && previousPagesResponse.Data.Any())
        {
            pageDetail.PreviousPages = previousPagesResponse.Data
                .Select(r => new PreviousPageData
                {
                    Url = GetDictionaryValue(r.Dimensions, "previous_event_url", string.Empty),
                    PageViews = GetDictionaryValue(r.Metrics, "page_views", 0)
                })
                .Where(p => !string.IsNullOrWhiteSpace(p.Url))
                .ToList();
        }

        return pageDetail;
    }

    public async Task<List<OutlinkData>> GetOutlinkDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null)
    {
        var response = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.Links.OutlinkUrl)
                   .AddMetric(Metrics.Events.Outlinks)
                   .AddMetric(Metrics.Events.UniqueOutlinks)
                   .SetDateRange(startDate, endDate)
                   .OrderByDescending("outlinks")
                   .Limit(20);
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);

        });

        if (response == null || !response.Data.Any())
        {
            return new List<OutlinkData>();
        }

        return response.Data.Select(row => new OutlinkData
        {
            OutlinkUrl = GetDictionaryValue(row.Dimensions, "outlink_url", "Unknown"),
            Clicks = GetDictionaryValue(row.Metrics, "outlinks", 0),
            UniqueClicks = GetDictionaryValue(row.Metrics, "unique_outlinks", 0)
        }).ToList();
    }

    public async Task<List<DownloadData>> GetDownloadDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null)
    {
        var response = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.Links.DownloadUrl)
                   .AddMetric(Metrics.Events.Downloads)
                   .AddMetric(Metrics.Events.UniqueDownloads)
                   .SetDateRange(startDate, endDate)
                   .OrderByDescending("downloads")
                   .Limit(20);
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);

        });

        if (response == null || !response.Data.Any())
        {
            return new List<DownloadData>();
        }

        return response.Data.Select(row => new DownloadData
        {
            DownloadUrl = GetDictionaryValue(row.Dimensions, "download_url", "Unknown"),
            Downloads = GetDictionaryValue(row.Metrics, "downloads", 0),
            UniqueDownloads = GetDictionaryValue(row.Metrics, "unique_downloads", 0)
        }).ToList();
    }

    public async Task<List<EventTrendData>> GetEventTrendsDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null)
    {
        var response = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.Date.DateDimension)
                   .AddMetric(Metrics.Events.Outlinks)
                   .AddMetric(Metrics.Events.Downloads)
                   .SetDateRange(startDate, endDate)
                   .OrderBy("timestamp");
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);

        });

        if (response == null || !response.Data.Any())
        {
            return new List<EventTrendData>();
        }

        return response.Data.Select(row =>
        {
            var timestamp = GetDictionaryValue(row.Dimensions, "timestamp", DateTime.Today.ToString());
            // Try to parse the timestamp and format it as a short date (dd/MM)
            DateTime parsedDate;
            string formattedDate;
            if (DateTime.TryParse(timestamp, out parsedDate))
            {
                formattedDate = parsedDate.ToString("dd/MM");
            }
            else
            {
                formattedDate = timestamp;
            }

            return new EventTrendData
            {
                Date = formattedDate,
                Outlinks = GetDictionaryValue(row.Metrics, "outlinks", 0),
                Downloads = GetDictionaryValue(row.Metrics, "downloads", 0)
            };
        }).ToList();
    }

    private static T GetDictionaryValue<T>(IDictionary<string, object?> dict, string key, T defaultValue)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            try
            {
                // Handle JSON elements (from System.Text.Json)
                if (value is System.Text.Json.JsonElement jsonElement)
                {
                    // Handle arrays - extract the last element which is typically the readable value
                    if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var arrayLength = jsonElement.GetArrayLength();
                        if (arrayLength > 0)
                        {
                            // Get the last element which typically contains the human-readable value
                            var lastElement = jsonElement[arrayLength - 1];

                            if (typeof(T) == typeof(string))
                            {
                                // For strings, use GetString() to get the actual string value without quotes
                                var stringValue = lastElement.ValueKind == System.Text.Json.JsonValueKind.String
                                    ? lastElement.GetString()
                                    : lastElement.ToString();
                                return (T)(object)(stringValue ?? (defaultValue?.ToString() ?? ""));
                            }

                            // Try to deserialize to the target type
                            var deserializedValue = System.Text.Json.JsonSerializer.Deserialize<T>(lastElement.GetRawText());
                            if (deserializedValue != null)
                            {
                                return deserializedValue;
                            }
                        }
                    }
                    // Handle other JSON types
                    else
                    {
                        var deserializedValue = System.Text.Json.JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                        if (deserializedValue != null)
                        {
                            return deserializedValue;
                        }
                    }
                }

                // Default conversion for simple types
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    private static string GetDimensionKey(IDictionary<string, object?> dict, string key, string defaultValue)
    {
        var result = GetDimensionKeyAsObject(dict, key, defaultValue);
        return result?.ToString() ?? defaultValue;
    }

    private static object GetDimensionKeyAsObject(IDictionary<string, object?> dict, string key, object defaultValue)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            try
            {
                // Handle JSON elements (from System.Text.Json)
                if (value is System.Text.Json.JsonElement jsonElement)
                {
                    // Handle arrays - extract the dimension key
                    if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var arrayLength = jsonElement.GetArrayLength();
                        if (arrayLength > 0)
                        {
                            // Get the first element
                            var firstElement = jsonElement[0];

                            // If first element is a number, return it as an integer
                            if (firstElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                            {
                                if (firstElement.TryGetInt32(out var intValue))
                                {
                                    return intValue;
                                }
                                if (firstElement.TryGetInt64(out var longValue))
                                {
                                    return longValue;
                                }
                                return firstElement.GetDouble();
                            }

                            // For string first elements, return as string
                            if (firstElement.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                return firstElement.GetString() ?? defaultValue;
                            }

                            // Fallback
                            return firstElement.ToString() ?? defaultValue;
                        }
                    }
                    // Handle non-array values - use the value itself as the key
                    else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        return jsonElement.GetString() ?? defaultValue;
                    }
                    else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                    {
                        if (jsonElement.TryGetInt32(out var intValue))
                        {
                            return intValue;
                        }
                        if (jsonElement.TryGetInt64(out var longValue))
                        {
                            return longValue;
                        }
                        return jsonElement.GetDouble();
                    }
                    else
                    {
                        // For other JSON types, try to convert to string
                        return jsonElement.ToString() ?? defaultValue;
                    }
                }

                // If value is already the right type, use it directly
                return value;
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    public async Task<List<SessionData>> GetSessionsDataAsync(DateTime startDate, DateTime endDate, int limit = 100, string? sortColumn = null, string? sortDirection = null, List<DimensionalFilter>? filters = null)
    {
        var request = _sessionsService.CreateRequest();
        request.DateFrom = startDate.ToString("yyyy-MM-dd");
        request.DateTo = endDate.ToString("yyyy-MM-dd");
        request.Limit = limit;

        // Add columns for session data (session_id, visitor_id, and timestamp are returned by default)
        request.Columns = new List<SessionColumn>
        {
            new SessionColumn { ColumnId = "source" },
            new SessionColumn { ColumnId = "medium" },
            new SessionColumn { ColumnId = "campaign_name" },
            new SessionColumn { ColumnId = "device_type" },
            new SessionColumn { ColumnId = "browser_name" },
            new SessionColumn { ColumnId = "operating_system" },
            new SessionColumn { ColumnId = "location_country_name" },
            new SessionColumn { ColumnId = "location_city_name" }
        };

        // Add sorting if specified
        if (!string.IsNullOrEmpty(sortColumn))
        {
            request.OrderBy = new List<PiwikPRO.Analytics.Models.Sessions.OrderBy>
            {
                new PiwikPRO.Analytics.Models.Sessions.OrderBy
                {
                    ColumnId = sortColumn,
                    Direction = sortDirection?.ToLower() ?? "desc"
                }
            };
        }

        // Apply filters if provided
        request.Filters = BuildRawDataFilters(filters);

        var response = await _sessionsService.FetchSessionsDataAsync(request);

        if (response?.Sessions != null && response.Sessions.Any())
        {
            return response.Sessions.Select(s => new SessionData
            {
                SessionId = s.SessionId ?? string.Empty,
                VisitorId = s.VisitorId ?? string.Empty,
                Timestamp = s.Timestamp,
                Source = s.Data.GetValueOrDefault("source")?.ToString() ?? "N/A",
                Medium = s.Data.GetValueOrDefault("medium")?.ToString() ?? "N/A",
                DeviceType = s.Data.GetValueOrDefault("device_type")?.ToString() ?? "N/A",
                Browser = s.Data.GetValueOrDefault("browser_name")?.ToString() ?? "N/A",
                OperatingSystem = s.Data.GetValueOrDefault("operating_system")?.ToString() ?? "N/A",
                Country = s.Data.GetValueOrDefault("location_country_name")?.ToString() ?? "N/A",
                City = s.Data.GetValueOrDefault("location_city_name")?.ToString() ?? "N/A",
                CampaignName = s.Data.GetValueOrDefault("campaign_name")?.ToString() ?? string.Empty
            }).ToList();
        }

        return new List<SessionData>();
    }

    public async Task<List<EventData>> GetSessionEventsAsync(string sessionId, DateTime startDate, DateTime endDate)
    {
        var request = _eventsService.CreateRequest();
        request.DateFrom = startDate.ToString("yyyy-MM-dd");
        request.DateTo = endDate.ToString("yyyy-MM-dd");
        request.Limit = 1000; // Get more events for a session

        // Add columns for event data (event_id, session_id, visitor_id, timestamp are returned by default)
        // Note: Only requesting columns that are known to exist in the Events API
        request.Columns = new List<EventColumn>
        {
            new EventColumn { ColumnId = "event_type" },
            new EventColumn { ColumnId = "event_url" }
        };

        // Filter by session_id
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

        // Order by timestamp ascending to show chronological order
        request.OrderBy = new List<EventOrderBy>
        {
            new EventOrderBy { ColumnId = "timestamp", Direction = "asc" }
        };

        var response = await _eventsService.FetchEventsDataAsync(request);

        if (response?.Events != null && response.Events.Any())
        {
            return response.Events.Select(e => new EventData
            {
                Timestamp = e.Timestamp,
                EventType = e.Data.GetValueOrDefault("event_type")?.ToString() ?? "N/A",
                EventUrl = e.Data.GetValueOrDefault("event_url")?.ToString() ?? string.Empty,
                EventAction = string.Empty, // Not available in Events API
                EventName = string.Empty,   // Not available in Events API
                EventCategory = string.Empty // Not available in Events API
            }).ToList();
        }

        return new List<EventData>();
    }

    public async Task<List<RealTimeEventData>> GetRealTimeEventsAsync(DateTime startDateTime, DateTime endDateTime, int limit = 100, List<DimensionalFilter>? filters = null)
    {
        var request = _realTimeEventsService.CreateRequest();
        request.DateFrom = startDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        request.DateTo = endDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        request.Limit = limit;

        // Add columns - visitor_id is returned by default along with event_id, session_id, timestamp
        request.Columns = new List<RealTimeEventColumn>
        {
            new RealTimeEventColumn { ColumnId = "visitor_id" },
            new RealTimeEventColumn { ColumnId = "event_type" },
            new RealTimeEventColumn { ColumnId = "event_url" },
            new RealTimeEventColumn { ColumnId = "source" },
            new RealTimeEventColumn { ColumnId = "medium" },
            new RealTimeEventColumn { ColumnId = "device_type" },
            new RealTimeEventColumn { ColumnId = "browser_name" },
            new RealTimeEventColumn { ColumnId = "location_country_name" },
            new RealTimeEventColumn { ColumnId = "location_city_name" }
        };

        request.Filters = BuildRawDataFilters(filters);

        var response = await _realTimeEventsService.FetchRealTimeEventsDataAsync(request);

        if (response?.Events != null && response.Events.Any())
        {
            return response.Events.Select(e => new RealTimeEventData
            {
                EventId = e.EventId ?? string.Empty,
                SessionId = e.SessionId ?? string.Empty,
                VisitorId = e.VisitorId ?? e.Data.GetValueOrDefault("visitor_id")?.ToString() ?? string.Empty,
                Timestamp = e.Timestamp,
                EventType = e.Data.GetValueOrDefault("event_type")?.ToString() ?? "N/A",
                EventUrl = e.Data.GetValueOrDefault("event_url")?.ToString() ?? string.Empty,
                EventAction = e.Data.GetValueOrDefault("event_action")?.ToString() ?? string.Empty,
                Source = e.Data.GetValueOrDefault("source")?.ToString() ?? "N/A",
                Medium = e.Data.GetValueOrDefault("medium")?.ToString() ?? "N/A",
                DeviceType = e.Data.GetValueOrDefault("device_type")?.ToString() ?? "N/A",
                Browser = e.Data.GetValueOrDefault("browser_name")?.ToString() ?? "N/A",
                Country = e.Data.GetValueOrDefault("location_country_name")?.ToString() ?? "N/A",
                City = e.Data.GetValueOrDefault("location_city_name")?.ToString() ?? "N/A"
            }).ToList();
        }

        return new List<RealTimeEventData>();
    }

    public async Task<List<BehaviorData>> GetTopPagesAsync(DateTime startDate, DateTime endDate, int limit = 50, List<DimensionalFilter>? filters = null)
    {
        var response = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.Page.PageUrl)
                   .AddDimension(Dimensions.Page.PageTitle)
                   .AddMetric(Metrics.Pages.PageViews)
                   .AddMetric(Metrics.Pages.UniquePageViews)
                   .SetDateRange(startDate, endDate)
                   .OrderByDescending("page_views")
                   .Limit(limit);
            ApplyAppSelection(builder);
            ApplyFilters(builder, filters);
        });

        if (response == null || !response.Data.Any())
        {
            return new List<BehaviorData>();
        }

        return response.Data.Select(row => new BehaviorData
        {
            PageUrl = GetDictionaryValue(row.Dimensions, "event_url", "Unknown"),
            PageTitle = GetDictionaryValue(row.Dimensions, "page_title", ""),
            PageViews = GetDictionaryValue(row.Metrics, "page_views", 0),
            UniquePageViews = GetDictionaryValue(row.Metrics, "unique_page_views", 0)
        }).ToList();
    }

    public async Task<LiveMapPageInfo?> GetLiveMapPageInfoAsync(string pageUrl, DateTime startDate, DateTime endDate)
    {
        var pageInfo = new LiveMapPageInfo
        {
            PageUrl = pageUrl
        };

        // Get page title and traffic
        var response = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.Page.PageTitle)
                   .AddMetric(Metrics.Pages.PageViews)
                   .SetDateRange(startDate, endDate)
                   .WhereEquals("event_url", pageUrl)
                   .Limit(1);
            ApplyAppSelection(builder);
        });

        if (response != null && response.Data.Any())
        {
            var row = response.Data.First();
            pageInfo.PageTitle = GetDictionaryValue(row.Dimensions, "page_title", pageUrl);
            pageInfo.TrafficLastHour = GetDictionaryValue(row.Metrics, "page_views", 0);
        }

        // Get recent visitors for this page
        var endDateTime = DateTime.UtcNow.AddMinutes(-3);
        var startDateTime = endDateTime.AddMinutes(-60);

        try
        {
            var request = _realTimeEventsService.CreateRequest();
            request.DateFrom = startDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            request.DateTo = endDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            request.Limit = 10;
            request.Columns = new List<RealTimeEventColumn>
            {
                new RealTimeEventColumn { ColumnId = "visitor_id" },
                new RealTimeEventColumn { ColumnId = "source" },
                new RealTimeEventColumn { ColumnId = "location_country_name" },
                new RealTimeEventColumn { ColumnId = "location_city_name" }
            };
            request.Filters = new RawDataFilter
            {
                Operator = "and",
                Conditions = new List<FilterCondition>
                {
                    new FilterCondition
                    {
                        ColumnId = "event_url",
                        Condition = new ConditionOperator { Operator = "eq", Value = pageUrl }
                    }
                }
            };

            var eventsResponse = await _realTimeEventsService.FetchRealTimeEventsDataAsync(request);

            if (eventsResponse?.Events != null)
            {
                pageInfo.ActiveUsers = eventsResponse.Events.Select(e => e.VisitorId).Distinct().Count();
                pageInfo.LatestVisitors = eventsResponse.Events.Take(5).Select(e =>
                {
                    var city = e.Data.GetValueOrDefault("location_city_name")?.ToString() ?? "";
                    var country = e.Data.GetValueOrDefault("location_country_name")?.ToString() ?? "Unknown";
                    var location = string.IsNullOrEmpty(city) ? country : $"{city}, {country}";
                    var source = e.Data.GetValueOrDefault("source")?.ToString() ?? "Direct";
                    var timeAgo = e.Timestamp.HasValue
                        ? GetTimeAgoString(DateTime.UtcNow - e.Timestamp.Value)
                        : "recently";

                    return new LiveMapVisitor
                    {
                        Location = location,
                        Source = source,
                        TimeAgo = timeAgo
                    };
                }).ToList();
            }
        }
        catch
        {
            // Continue without real-time data
        }

        // Get typical paths (next pages)
        var nextPagesResponse = await _analyticsService.QueryAsync(builder =>
        {
            builder.AddDimension(Dimensions.Links.NextEventUrl)
                   .AddMetric(Metrics.Pages.PageViews)
                   .SetDateRange(startDate, endDate)
                   .WhereEquals("event_url", pageUrl)
                   .OrderByDescending("page_views")
                   .Limit(5);
            ApplyAppSelection(builder);
        });

        if (nextPagesResponse != null && nextPagesResponse.Data.Any())
        {
            pageInfo.TypicalPaths = nextPagesResponse.Data
                .Select(r => new LiveMapPath
                {
                    From = pageUrl,
                    To = GetDictionaryValue(r.Dimensions, "next_event_url", string.Empty),
                    Count = GetDictionaryValue(r.Metrics, "page_views", 0)
                })
                .Where(p => !string.IsNullOrWhiteSpace(p.To))
                .ToList();
        }

        return pageInfo;
    }

    private static string GetTimeAgoString(TimeSpan elapsed)
    {
        if (elapsed.TotalMinutes < 1) return "just now";
        if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes} mins ago";
        if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours} hours ago";
        return $"{(int)elapsed.TotalDays} days ago";
    }
}
