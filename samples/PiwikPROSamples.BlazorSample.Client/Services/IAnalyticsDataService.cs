using PiwikPROSamples.BlazorSample.Client.Models;

namespace PiwikPROSamples.BlazorSample.Client.Services;

public interface IAnalyticsDataService
{
    Task<DashboardMetrics> GetDashboardMetricsAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null);
    Task<List<AcquisitionData>> GetAcquisitionDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null);
    Task<List<BehaviorData>> GetBehaviorDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null);
    Task<List<VisitorData>> GetVisitorDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null);
    Task<List<GoalData>> GetGoalsDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null);
    Task<List<TrendData>> GetTrendsDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null);
    Task<List<ChannelData>> GetChannelDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null);
    Task<List<LocationData>> GetLocationDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null);
    Task<List<EngagementData>> GetEngagementDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null);
    Task<PageDetailData?> GetPageDetailDataAsync(string pageUrl, DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null);
    Task<List<OutlinkData>> GetOutlinkDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null);
    Task<List<DownloadData>> GetDownloadDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null);
    Task<List<EventTrendData>> GetEventTrendsDataAsync(DateTime startDate, DateTime endDate, List<DimensionalFilter>? filters = null);
    Task<List<SessionData>> GetSessionsDataAsync(DateTime startDate, DateTime endDate, int limit = 100, string? sortColumn = null, string? sortDirection = null, List<DimensionalFilter>? filters = null);
    Task<List<EventData>> GetSessionEventsAsync(string sessionId, DateTime startDate, DateTime endDate);
    Task<List<RealTimeEventData>> GetRealTimeEventsAsync(DateTime startDateTime, DateTime endDateTime, int limit = 100, List<DimensionalFilter>? filters = null);
    
    // Live Map methods
    Task<List<BehaviorData>> GetTopPagesAsync(DateTime startDate, DateTime endDate, int limit = 50, List<DimensionalFilter>? filters = null);
    Task<LiveMapPageInfo?> GetLiveMapPageInfoAsync(string pageUrl, DateTime startDate, DateTime endDate);
}
