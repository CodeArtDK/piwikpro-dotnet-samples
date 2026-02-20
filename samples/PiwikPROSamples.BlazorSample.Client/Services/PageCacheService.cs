using PiwikPROSamples.BlazorSample.Client.Models;

namespace PiwikPROSamples.BlazorSample.Client.Services;

/// <summary>
/// Cache service for storing page data to avoid repeated API calls
/// </summary>
public interface IPageCacheService
{
    Task<List<BehaviorData>?> GetTopPagesAsync();
    Task SetTopPagesAsync(List<BehaviorData> pages);
    Task<LiveMapPageInfo?> GetPageInfoAsync(string url);
    Task SetPageInfoAsync(string url, LiveMapPageInfo pageInfo);
    void ClearCache();
    bool IsCacheValid();
}

public class PageCacheService : IPageCacheService
{
    private List<BehaviorData>? _topPagesCache;
    private readonly Dictionary<string, (LiveMapPageInfo Info, DateTime CacheTime)> _pageInfoCache = new();
    private DateTime _lastCacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public Task<List<BehaviorData>?> GetTopPagesAsync()
    {
        if (!IsCacheValid())
        {
            return Task.FromResult<List<BehaviorData>?>(null);
        }
        return Task.FromResult(_topPagesCache);
    }

    public Task SetTopPagesAsync(List<BehaviorData> pages)
    {
        _topPagesCache = pages;
        _lastCacheTime = DateTime.UtcNow;
        // Clear page info cache when top pages are refreshed to ensure consistency
        _pageInfoCache.Clear();
        return Task.CompletedTask;
    }

    public Task<LiveMapPageInfo?> GetPageInfoAsync(string url)
    {
        if (!IsCacheValid())
        {
            return Task.FromResult<LiveMapPageInfo?>(null);
        }

        if (_pageInfoCache.TryGetValue(url, out var cached))
        {
            // Check if this specific cache entry is still valid
            if (DateTime.UtcNow - cached.CacheTime < _cacheExpiration)
            {
                return Task.FromResult<LiveMapPageInfo?>(cached.Info);
            }
            // Entry expired, remove it
            _pageInfoCache.Remove(url);
        }

        return Task.FromResult<LiveMapPageInfo?>(null);
    }

    public Task SetPageInfoAsync(string url, LiveMapPageInfo pageInfo)
    {
        _pageInfoCache[url] = (pageInfo, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public void ClearCache()
    {
        _topPagesCache = null;
        _pageInfoCache.Clear();
        _lastCacheTime = DateTime.MinValue;
    }

    public bool IsCacheValid()
    {
        return _topPagesCache != null && 
               DateTime.UtcNow - _lastCacheTime < _cacheExpiration;
    }
}
