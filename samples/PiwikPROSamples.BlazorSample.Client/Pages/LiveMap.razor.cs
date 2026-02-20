using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using PiwikPROSamples.BlazorSample.Client.Models;
using PiwikPROSamples.BlazorSample.Client.Services;

namespace PiwikPROSamples.BlazorSample.Client.Pages;

public partial class LiveMap : IAsyncDisposable
{
    private const int AutoRefreshIntervalSeconds = 60; // Refresh once per minute to avoid rate limiting
    private const int MaxTopPages = 100; // Fetch top 100 pages to build the tree

    [Inject]
    private IAnalyticsDataService DataService { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IAppSelectorService AppSelector { get; set; } = default!;

    [Inject]
    private IAppSettingsService AppSettingsService { get; set; } = default!;

    [Inject]
    private IPageCacheService PageCache { get; set; } = default!;

    private bool _loading = true;
    private string? _errorMessage;
    private bool _isFullScreen = false;
    private LiveMapPageInfo? _selectedPage;
    private DotNetObjectReference<LiveMap>? _dotnetRef;
    private CancellationTokenSource? _refreshCts;
    private Task? _refreshTask;

    // URL-based tree structure
    private Dictionary<string, LiveMapNode> _nodeMap = new();
    private Dictionary<string, LiveMapNode> _pathToNodeMap = new(); // Maps URL paths to nodes
    private List<LiveMapNode> _visibleNodes = new(); // Currently visible nodes in the tree
    private List<LiveMapLink> _visibleLinks = new();

    private DateTime _startDate = DateTime.Now.AddHours(-1);
    private DateTime _endDate = DateTime.Now;
    private int _serverTimezoneOffset = 0;

    // Cache for page data with their URL info
    private List<BehaviorData>? _cachedTopPages;
    private Dictionary<string, string> _urlToTitleMap = new(); // Maps URL to page title

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetRef = DotNetObjectReference.Create(this);
            
            // Try to initialize with retry in case D3.js is still loading
            var maxRetries = 5;
            for (var i = 0; i < maxRetries; i++)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("liveMap.initialize", "live-map-container", _dotnetRef);
                    break;
                }
                catch (JSException ex) when (ex.Message.Contains("liveMap") && i < maxRetries - 1)
                {
                    // Wait for D3.js and livemap.js to load
                    await Task.Delay(500);
                }
            }
            
            // Load settings
            _serverTimezoneOffset = await AppSettingsService.GetServerTimezoneOffsetAsync();
            AppSettingsService.OnSettingsChanged += OnSettingsChanged;
            AppSelector.OnAppChanged += OnAppChanged;
            
            await LoadInitialData();
            StartAutoRefresh();
        }
    }

    private async void OnSettingsChanged()
    {
        _serverTimezoneOffset = await AppSettingsService.GetServerTimezoneOffsetAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async void OnAppChanged()
    {
        // Clear cache when app changes
        PageCache.ClearCache();
        _nodeMap.Clear();
        _pathToNodeMap.Clear();
        _urlToTitleMap.Clear();
        await LoadInitialData();
    }

    private async Task LoadInitialData()
    {
        _loading = true;
        _errorMessage = null;
        StateHasChanged();

        try
        {
            // Try to get from cache first
            var topPages = await PageCache.GetTopPagesAsync();
            
            if (topPages == null)
            {
                // Fetch top 100 pages from API
                topPages = await DataService.GetTopPagesAsync(_startDate, _endDate, MaxTopPages);
                if (topPages != null && topPages.Any())
                {
                    await PageCache.SetTopPagesAsync(topPages);
                }
            }

            if (topPages == null || !topPages.Any())
            {
                _errorMessage = "No page data found for the selected time period.";
                _loading = false;
                StateHasChanged();
                return;
            }

            _cachedTopPages = topPages;

            // Build URL to title mapping
            BuildUrlTitleMap(topPages);

            // Build the URL-based tree structure
            BuildUrlTree(topPages);

            // Update the visualization with root and first-level nodes
            UpdateVisibleNodes();
            await UpdateVisualization();

            Snackbar.Add($"Loaded {topPages.Count} pages into tree structure", Severity.Success);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading data: {ex.Message}";
            Snackbar.Add(_errorMessage, Severity.Error);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private void BuildUrlTitleMap(List<BehaviorData> pages)
    {
        _urlToTitleMap.Clear();
        foreach (var page in pages)
        {
            if (!string.IsNullOrEmpty(page.PageUrl) && !string.IsNullOrEmpty(page.PageTitle))
            {
                _urlToTitleMap[page.PageUrl] = page.PageTitle;
            }
        }
    }

    private void BuildUrlTree(List<BehaviorData> pages)
    {
        _nodeMap.Clear();
        _pathToNodeMap.Clear();

        // Find the homepage (root "/" path) to get its traffic
        var homepage = pages.FirstOrDefault(p => 
        {
            if (string.IsNullOrEmpty(p.PageUrl)) return false;
            try
            {
                var uri = new Uri(p.PageUrl);
                var path = uri.AbsolutePath.TrimEnd('/');
                return string.IsNullOrEmpty(path) || path == "/";
            }
            catch { return false; }
        });

        // Create root node with homepage title and traffic
        var rootNode = new LiveMapNode
        {
            Id = "root",
            Url = homepage?.PageUrl ?? "/",
            UrlPath = "/",
            Title = !string.IsNullOrEmpty(homepage?.PageTitle) ? TruncateTitle(homepage.PageTitle) : "⭐ Start",
            Traffic = homepage?.PageViews ?? 0,
            TotalTraffic = homepage?.PageViews ?? 0,
            Expanded = true,
            Depth = 0
        };
        _nodeMap["root"] = rootNode;
        _pathToNodeMap["/"] = rootNode;

        // Process each page URL and build the hierarchical tree
        foreach (var page in pages.OrderBy(p => p.PageUrl?.Length ?? 0))
        {
            if (string.IsNullOrEmpty(page.PageUrl)) continue;

            try
            {
                var uri = new Uri(page.PageUrl);
                var path = uri.AbsolutePath.TrimEnd('/');
                if (string.IsNullOrEmpty(path)) path = "/";

                // Skip homepage as it's already the root
                if (path == "/") continue;

                // Split path into segments
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                
                // Build each level of the path
                var currentPath = "";
                LiveMapNode? parentNode = rootNode;

                for (int i = 0; i < segments.Length; i++)
                {
                    currentPath += "/" + segments[i];
                    var isLeaf = i == segments.Length - 1;

                    if (!_pathToNodeMap.TryGetValue(currentPath, out var existingNode))
                    {
                        // Create new node for this path segment
                        var nodeId = GenerateNodeId(currentPath);
                        var nodeTitle = GetNodeTitle(page.PageUrl, currentPath, segments[i], isLeaf);
                        
                        var newNode = new LiveMapNode
                        {
                            Id = nodeId,
                            Url = isLeaf ? page.PageUrl : $"[virtual]{currentPath}",
                            UrlPath = currentPath,
                            Title = nodeTitle,
                            Traffic = isLeaf ? page.PageViews : 0,
                            TotalTraffic = isLeaf ? page.PageViews : 0,
                            Expanded = false,
                            ParentId = parentNode?.Id,
                            Depth = i + 1
                        };

                        _nodeMap[nodeId] = newNode;
                        _pathToNodeMap[currentPath] = newNode;

                        // Add to parent's children
                        if (parentNode != null)
                        {
                            parentNode.ChildIds.Add(nodeId);
                        }

                        parentNode = newNode;
                    }
                    else
                    {
                        // Node exists, update traffic if this is the actual page
                        if (isLeaf && !existingNode.Url.StartsWith("[virtual]"))
                        {
                            existingNode.Traffic += page.PageViews;
                            existingNode.TotalTraffic += page.PageViews;
                        }
                        else if (isLeaf)
                        {
                            // This was a virtual node, now it's a real page
                            existingNode.Url = page.PageUrl;
                            existingNode.Traffic = page.PageViews;
                            existingNode.TotalTraffic = page.PageViews;
                            // Update title if we have a better one
                            if (!string.IsNullOrEmpty(page.PageTitle))
                            {
                                existingNode.Title = page.PageTitle;
                            }
                        }
                        parentNode = existingNode;
                    }
                }

                // Aggregate traffic up the tree
                AggregateTrafficToParents(parentNode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing URL {page.PageUrl}: {ex.Message}");
            }
        }
    }

    private string GetNodeTitle(string fullUrl, string path, string segment, bool isLeaf)
    {
        // Try to get the actual page title first
        if (isLeaf && _urlToTitleMap.TryGetValue(fullUrl, out var title) && !string.IsNullOrEmpty(title))
        {
            return TruncateTitle(title);
        }

        // Fall back to formatting the URL segment
        return FormatSegmentAsTitle(segment);
    }

    private static string FormatSegmentAsTitle(string segment)
    {
        if (string.IsNullOrEmpty(segment)) return "Page";

        // Clean up common patterns
        var title = segment
            .Replace("-", " ")
            .Replace("_", " ")
            .Replace("%20", " ");

        // Capitalize first letter of each word
        var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        title = string.Join(" ", words.Select(w => 
            w.Length > 0 ? char.ToUpper(w[0]) + (w.Length > 1 ? w.Substring(1) : "") : w));

        return TruncateTitle(title);
    }

    private static string TruncateTitle(string title)
    {
        if (string.IsNullOrEmpty(title)) return "Page";
        return title.Length > 20 ? title.Substring(0, 17) + "..." : title;
    }

    private void AggregateTrafficToParents(LiveMapNode? node)
    {
        while (node?.ParentId != null)
        {
            var parent = _nodeMap.GetValueOrDefault(node.ParentId);
            if (parent != null)
            {
                // Recalculate total traffic from all children
                parent.TotalTraffic = parent.Traffic + parent.ChildIds
                    .Select(id => _nodeMap.GetValueOrDefault(id))
                    .Where(child => child != null)
                    .Sum(child => child!.TotalTraffic);
            }
            node = parent;
        }
    }

    private void UpdateVisibleNodes()
    {
        _visibleNodes.Clear();
        _visibleLinks.Clear();

        // Add root node
        var rootNode = _nodeMap.GetValueOrDefault("root");
        if (rootNode == null) return;

        _visibleNodes.Add(rootNode);

        // Traverse tree and add visible nodes
        AddVisibleChildren(rootNode);
    }

    private void AddVisibleChildren(LiveMapNode parentNode)
    {
        if (!parentNode.Expanded) return;

        foreach (var childId in parentNode.ChildIds)
        {
            var childNode = _nodeMap.GetValueOrDefault(childId);
            if (childNode == null) continue;

            _visibleNodes.Add(childNode);
            _visibleLinks.Add(new LiveMapLink
            {
                Source = parentNode.Id,
                Target = childNode.Id,
                Traffic = childNode.TotalTraffic / 10
            });

            // Recursively add children if expanded
            if (childNode.Expanded)
            {
                AddVisibleChildren(childNode);
            }
        }
    }

    private async Task UpdateVisualization()
    {
        var nodesData = _visibleNodes.Select(n => new
        {
            id = n.Id,
            url = n.Url,
            title = n.Title,
            traffic = n.Expanded ? n.Traffic : n.TotalTraffic,
            expanded = n.Expanded,
            hasChildren = n.ChildIds.Count > 0,
            depth = n.Depth,
            x = n.X,
            y = n.Y
        }).ToArray();

        var linksData = _visibleLinks.Select(l => new
        {
            source = l.Source,
            target = l.Target,
            traffic = l.Traffic
        }).ToArray();

        await JSRuntime.InvokeVoidAsync("liveMap.updateData", nodesData, linksData);
    }

    private void StartAutoRefresh()
    {
        _refreshCts = new CancellationTokenSource();
        _refreshTask = RunAutoRefreshLoop(_refreshCts.Token);
    }

    private async Task RunAutoRefreshLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait 60 seconds between refreshes to avoid rate limiting
                await Task.Delay(AutoRefreshIntervalSeconds * 1000, cancellationToken);

                if (cancellationToken.IsCancellationRequested) break;

                await RefreshTrafficData();
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task RefreshTrafficData()
    {
        try
        {
            _startDate = DateTime.Now.AddHours(-1);
            _endDate = DateTime.Now;

            var topPages = await DataService.GetTopPagesAsync(_startDate, _endDate, MaxTopPages);

            if (topPages != null)
            {
                // Update cache
                await PageCache.SetTopPagesAsync(topPages);
                _cachedTopPages = topPages;

                // Update URL to title map
                BuildUrlTitleMap(topPages);

                // Update traffic for existing nodes
                foreach (var page in topPages)
                {
                    if (string.IsNullOrEmpty(page.PageUrl)) continue;

                    try
                    {
                        var uri = new Uri(page.PageUrl);
                        var path = uri.AbsolutePath.TrimEnd('/');
                        if (string.IsNullOrEmpty(path)) path = "/";

                        if (_pathToNodeMap.TryGetValue(path, out var node))
                        {
                            node.Traffic = page.PageViews;
                        }
                        else
                        {
                            // New page not in tree - extend the structure
                            ExtendTreeForNewPage(page);
                        }
                    }
                    catch { }
                }

                // Recalculate total traffic for all nodes
                RecalculateAllTraffic();

                // Update visualization
                UpdateVisibleNodes();
                
                var trafficUpdates = _visibleNodes.Select(n => new 
                { 
                    nodeId = n.Id, 
                    traffic = n.Expanded ? n.Traffic : n.TotalTraffic 
                }).ToList();
                
                await JSRuntime.InvokeVoidAsync("liveMap.updateTraffic", trafficUpdates);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing traffic data: {ex.Message}");
        }
    }

    private void ExtendTreeForNewPage(BehaviorData page)
    {
        if (string.IsNullOrEmpty(page.PageUrl)) return;

        try
        {
            var uri = new Uri(page.PageUrl);
            var path = uri.AbsolutePath.TrimEnd('/');
            if (string.IsNullOrEmpty(path)) path = "/";

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentPath = "";
            LiveMapNode? parentNode = _nodeMap.GetValueOrDefault("root");

            for (int i = 0; i < segments.Length; i++)
            {
                currentPath += "/" + segments[i];
                var isLeaf = i == segments.Length - 1;

                if (!_pathToNodeMap.TryGetValue(currentPath, out var existingNode))
                {
                    var nodeId = GenerateNodeId(currentPath);
                    var nodeTitle = GetNodeTitle(page.PageUrl, currentPath, segments[i], isLeaf);

                    var newNode = new LiveMapNode
                    {
                        Id = nodeId,
                        Url = isLeaf ? page.PageUrl : $"[virtual]{currentPath}",
                        UrlPath = currentPath,
                        Title = nodeTitle,
                        Traffic = isLeaf ? page.PageViews : 0,
                        TotalTraffic = isLeaf ? page.PageViews : 0,
                        Expanded = false,
                        ParentId = parentNode?.Id,
                        Depth = i + 1
                    };

                    _nodeMap[nodeId] = newNode;
                    _pathToNodeMap[currentPath] = newNode;

                    if (parentNode != null && !parentNode.ChildIds.Contains(nodeId))
                    {
                        parentNode.ChildIds.Add(nodeId);
                    }

                    parentNode = newNode;
                }
                else
                {
                    parentNode = existingNode;
                }
            }
        }
        catch { }
    }

    private void RecalculateAllTraffic()
    {
        // Reset total traffic for all nodes
        foreach (var node in _nodeMap.Values)
        {
            node.TotalTraffic = node.Traffic;
        }

        // Calculate from leaves up
        var sortedByDepth = _nodeMap.Values.OrderByDescending(n => n.Depth).ToList();
        foreach (var node in sortedByDepth)
        {
            if (node.ParentId != null && _nodeMap.TryGetValue(node.ParentId, out var parent))
            {
                parent.TotalTraffic += node.TotalTraffic;
            }
        }
    }

    [JSInvokable]
    public async Task OnNodeSelected(string nodeId, string url, string title)
    {
        try
        {
            // Clear any previous highways
            await JSRuntime.InvokeVoidAsync("liveMap.updateHighways", Array.Empty<object>());

            // Skip virtual nodes
            if (url.StartsWith("[virtual]"))
            {
                _selectedPage = null;
                await InvokeAsync(StateHasChanged);
                return;
            }

            // Check cache first
            var cachedInfo = await PageCache.GetPageInfoAsync(url);
            if (cachedInfo != null)
            {
                _selectedPage = cachedInfo;
                await InvokeAsync(StateHasChanged);
                
                // Show highways for this page
                await UpdateHighwaysForPage(nodeId, cachedInfo);
                return;
            }

            // Fetch on demand
            _selectedPage = await DataService.GetLiveMapPageInfoAsync(url, _startDate, _endDate);
            
            if (_selectedPage != null)
            {
                await PageCache.SetPageInfoAsync(url, _selectedPage);
                
                // Show highways for this page
                await UpdateHighwaysForPage(nodeId, _selectedPage);
            }
            
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading page info: {ex.Message}", Severity.Error);
        }
    }

    private async Task UpdateHighwaysForPage(string sourceNodeId, LiveMapPageInfo pageInfo)
    {
        if (pageInfo?.TypicalPaths == null || !pageInfo.TypicalPaths.Any()) return;

        var highways = new List<object>();

        foreach (var path in pageInfo.TypicalPaths)
        {
            // Try to find the target node in our visible nodes
            var targetNode = FindNodeByUrl(path.To);
            if (targetNode != null && _visibleNodes.Any(n => n.Id == targetNode.Id))
            {
                highways.Add(new
                {
                    source = sourceNodeId,
                    target = targetNode.Id,
                    count = path.Count
                });
            }
        }

        if (highways.Any())
        {
            await JSRuntime.InvokeVoidAsync("liveMap.updateHighways", highways);
        }
    }

    private LiveMapNode? FindNodeByUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath.TrimEnd('/');
            if (string.IsNullOrEmpty(path)) path = "/";

            return _pathToNodeMap.GetValueOrDefault(path);
        }
        catch
        {
            return null;
        }
    }

    [JSInvokable]
    public async Task OnNodeDoubleClick(string nodeId, bool currentlyExpanded)
    {
        var node = _nodeMap.GetValueOrDefault(nodeId);
        if (node == null) return;

        // Toggle expanded state
        node.Expanded = !currentlyExpanded;

        if (!node.Expanded)
        {
            // Collapse all descendants
            CollapseDescendants(node);
        }

        // Update visible nodes and visualization
        UpdateVisibleNodes();
        await UpdateVisualization();

        var action = node.Expanded ? "Expanded" : "Collapsed";
        Snackbar.Add($"{action} {node.Title}", Severity.Info);
    }

    private void CollapseDescendants(LiveMapNode node)
    {
        foreach (var childId in node.ChildIds)
        {
            if (_nodeMap.TryGetValue(childId, out var child))
            {
                child.Expanded = false;
                CollapseDescendants(child);
            }
        }
    }

    private async Task ToggleFullScreen()
    {
        _isFullScreen = !_isFullScreen;

        if (_isFullScreen)
        {
            await JSRuntime.InvokeVoidAsync("liveMap.enterFullScreen", "live-map-container");
        }
        else
        {
            await JSRuntime.InvokeVoidAsync("liveMap.exitFullScreen");
        }

        StateHasChanged();

        // Resize after a small delay to let the DOM update
        await Task.Delay(100);
        await JSRuntime.InvokeVoidAsync("liveMap.resize");
    }

    private async Task ResetView()
    {
        await JSRuntime.InvokeVoidAsync("liveMap.resetView");
    }

    private static string GenerateNodeId(string path)
    {
        // Create a stable ID from the path
        return $"node_{Math.Abs(path?.GetHashCode() ?? 0)}";
    }

    private static string TruncateUrl(string url, int maxLength)
    {
        if (string.IsNullOrEmpty(url)) return "";
        return url.Length > maxLength ? url.Substring(0, maxLength) + "..." : url;
    }

    public async ValueTask DisposeAsync()
    {
        AppSettingsService.OnSettingsChanged -= OnSettingsChanged;
        AppSelector.OnAppChanged -= OnAppChanged;

        if (_refreshCts != null)
        {
            await _refreshCts.CancelAsync();
            if (_refreshTask != null)
            {
                try { await _refreshTask; }
                catch (OperationCanceledException) { }
            }
            _refreshCts.Dispose();
        }

        try
        {
            await JSRuntime.InvokeVoidAsync("liveMap.dispose");
        }
        catch (JSDisconnectedException) { }

        _dotnetRef?.Dispose();
    }
}
