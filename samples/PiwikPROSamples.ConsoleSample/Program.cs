using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PiwikPRO.Analytics;
using PiwikPRO.Analytics.Extensions;
using PiwikPRO.Analytics.Constants;
using PiwikPRO.Analytics.Utilities;
using PiwikPRO.Tracking;
using PiwikPRO.Tracking.Extensions;
using System.Linq;

namespace PiwikPROSamples.ConsoleSample;

class Program
{
    static async Task Main(string[] args)
    {
        // Create host builder
        var builder = Host.CreateDefaultBuilder(args);
        
        // Configure services
        builder.ConfigureServices((context, services) =>
        {
            // Configure PiwikPRO SDK for Analytics
            services.AddPiwikProAnalytics(options =>
            {
                // Configure with environment variables or app settings
                options.BaseUrl = Environment.GetEnvironmentVariable("PIWIKPRO_BASE_URL") ?? "https://your-instance.piwik.pro";
                options.ClientId = Environment.GetEnvironmentVariable("PIWIKPRO_CLIENT_ID") ?? "your-client-id";
                options.ClientSecret = Environment.GetEnvironmentVariable("PIWIKPRO_CLIENT_SECRET") ?? "your-client-secret";
                options.WebSiteId = Environment.GetEnvironmentVariable("PIWIKPRO_WEBSITE_ID") ?? "your-website-id";
                options.TimeoutSeconds = 30;
            });
            
            // Add tracking services (will reuse Core services already registered)
            services.AddPiwikProTracking();

            // Register the example service
            services.AddScoped<ExampleService>();
        });

        // Build and run
        using var host = builder.Build();
        
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var exampleService = host.Services.GetRequiredService<ExampleService>();

        try
        {
            logger.LogInformation("Starting PiwikPRO SDK Example");
            await exampleService.RunExamplesAsync();
            logger.LogInformation("Example completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while running the example");
        }
    }
}

public class ExampleService
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ITrackingService _trackingService;
    private readonly IJavaScriptTrackingCodeGenerator _codeGenerator;
    private readonly ILogger<ExampleService> _logger;

    public ExampleService(
        IAnalyticsService analyticsService,
        ITrackingService trackingService,
        IJavaScriptTrackingCodeGenerator codeGenerator,
        ILogger<ExampleService> logger)
    {
        _analyticsService = analyticsService;
        _trackingService = trackingService;
        _codeGenerator = codeGenerator;
        _logger = logger;
    }

    public async Task RunExamplesAsync()
    {
        _logger.LogInformation("Running Analytics API examples...");

        try
        {
            // Example 1: Get Query API information
            _logger.LogInformation("Fetching Query API information...");
            var queryInfo = await _analyticsService.GetQueryApiInfoAsync();
            _logger.LogInformation("Query API Info: {QueryInfo}", queryInfo?.ToString() ?? "No data");

            // Example 2: Get Reports API information
            _logger.LogInformation("Fetching Reports API information...");
            var reportsInfo = await _analyticsService.GetReportsApiInfoAsync();
            _logger.LogInformation("Reports API Info: {ReportsInfo}", reportsInfo?.ToString() ?? "No data");

            // Example 3: Simple query using fluent API
            _logger.LogInformation("Running simple query example...");
            await RunSimpleQueryExample();

            // Example 4: Complex query with filters
            _logger.LogInformation("Running complex query example...");
            await RunComplexQueryExample();

            // Example 5: Date range examples
            _logger.LogInformation("Running date range examples...");
            await RunDateRangeExamples();

            // Example 6: Streaming large results
            _logger.LogInformation("Running streaming example...");
            await RunStreamingExample();

            // Example 7: Server-side tracking
            _logger.LogInformation("Running tracking examples...");
            await RunTrackingExamples();

            // Example 8: JavaScript code generation
            _logger.LogInformation("Running JavaScript code generation examples...");
            RunJavaScriptCodeGenerationExamples();
            // Example 7: List custom dimensions
            _logger.LogInformation("Running custom dimensions example...");
            await RunCustomDimensionsExample();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while calling PiwikPRO API");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred");
        }
    }

    private async Task RunSimpleQueryExample()
    {
        _logger.LogInformation("Creating a simple query for visits by date...");
        
        var response = await _analyticsService.QueryAsync(builder =>
            builder.AddDimension(Dimensions.Date.DateDimension)
                   .AddMetric(Metrics.Sessions.TotalSessions)
                   .AddMetric(Metrics.Pages.PageViews)
                   .SetLastDays(7)
                   .OrderByDescending(Metrics.Sessions.TotalSessions)
                   .Limit(10));

        if (response != null)
        {
            _logger.LogInformation("Query returned {RowCount} rows out of {TotalRows} total", 
                response.Data.Count, response.TotalRows);
            
            foreach (var row in response.Data.Take(3)) // Show first 3 rows
            {
                var date = row.Dimensions.TryGetValue("date", out var dateValue) ? dateValue?.ToString() : "Unknown";
                var visits = row.Metrics.TryGetValue("sessions", out var visitsValue) ? visitsValue : 0;
                var pageViews = row.Metrics.TryGetValue("page_views", out var pageViewsValue) ? pageViewsValue : 0;
                
                _logger.LogInformation("Date: {Date}, Visits: {Visits}, Page Views: {PageViews}", 
                    date, visits, pageViews);
            }
        }
    }

    private async Task RunComplexQueryExample()
    {
        _logger.LogInformation("Creating a complex query with filters...");

        var query = _analyticsService.CreateQuery()
            .AddDimensions(Dimensions.Page.PageUrl, Dimensions.Device.Browser)
            .AddMetrics(Metrics.Sessions.TotalSessions, Metrics.Sessions.BounceRate, Metrics.Sessions.BounceRate)
            .SetDateRange(DateRangeUtilities.CurrentMonth().Start, DateRangeUtilities.CurrentMonth().End)
            .WhereContains(Dimensions.Page.PageUrl, "/product")
            .WhereIn(Dimensions.Device.Browser, "Chrome", "Firefox", "Safari")
            .OrderByDescending(Metrics.Sessions.TotalSessions)
            .Paginate(page: 1, pageSize: 50);

        var response = await _analyticsService.QueryAsync(query.Build());

        if (response != null)
        {
            _logger.LogInformation("Complex query returned {RowCount} rows", response.Data.Count);
            
            foreach (var row in response.Data.Take(2)) // Show first 2 rows
            {
                var pageUrl = row.Dimensions.TryGetValue("page_url", out var pageUrlValue) ? pageUrlValue?.ToString() : "Unknown";
                var browser = row.Dimensions.TryGetValue("browser", out var browserValue) ? browserValue?.ToString() : "Unknown";
                var visits = row.Metrics.TryGetValue("sessions", out var visitsValue) ? visitsValue : 0;
                
                _logger.LogInformation("Page: {PageUrl}, Browser: {Browser}, Visits: {Visits}", 
                    pageUrl, browser, visits);
            }
        }
    }

    private async Task RunDateRangeExamples()
    {
        _logger.LogInformation("Demonstrating various date range utilities...");

        // Example with different date ranges
        var dateRanges = new[]
        {
            ("Today", DateRangeUtilities.Today()),
            ("Yesterday", DateRangeUtilities.Yesterday()),
            ("Last 7 days", DateRangeUtilities.LastDays(7)),
            ("Current month", DateRangeUtilities.CurrentMonth()),
            ("Previous month", DateRangeUtilities.PreviousMonth()),
            ("Current quarter", DateRangeUtilities.CurrentQuarter()),
            ("Current year", DateRangeUtilities.CurrentYear())
        };

        foreach (var (name, dateRange) in dateRanges)
        {
            _logger.LogInformation("{Name}: {Start} to {End}", name, dateRange.Start, dateRange.End);
        }

        // Run a query with one of the date ranges
        var response = await _analyticsService.QueryAsync(builder =>
            builder.AddDimension(Dimensions.Date.DateDimension)
                   .AddMetric(Metrics.Sessions.TotalSessions)
                   .SetDateRange(DateRangeUtilities.LastDays(30).Start, DateRangeUtilities.LastDays(30).End)
                   .Limit(5));

        if (response != null)
        {
            _logger.LogInformation("Last 30 days query returned {RowCount} rows", response.Data.Count);
        }
    }

    private async Task RunStreamingExample()
    {
        _logger.LogInformation("Demonstrating streaming for large datasets...");

        var query = _analyticsService.CreateQuery()
            .AddDimension(Dimensions.Date.DateDimension)
            .AddDimension(Dimensions.Page.PageUrl)
            .AddMetric(Metrics.Sessions.TotalSessions)
            .SetLastDays(30)
            .Build();

        var processedRows = 0;
        await foreach (var row in _analyticsService.QueryStreamAsync(query, pageSize: 100))
        {
            processedRows++;
            
            // Process each row (in practice, you might save to database, export to file, etc.)
            if (processedRows <= 3) // Log first few rows
            {
                var date = row.Dimensions.TryGetValue("date", out var dateValue) ? dateValue?.ToString() : "Unknown";
                var pageUrl = row.Dimensions.TryGetValue("page_url", out var pageUrlValue) ? pageUrlValue?.ToString() : "Unknown";
                var visits = row.Metrics.TryGetValue("sessions", out var visitsValue) ? visitsValue : 0;
                
                _logger.LogInformation("Streaming row {RowNumber}: {Date}, {PageUrl}, {Visits} visits", 
                    processedRows, date, pageUrl, visits);
            }
            
            // Break after processing a reasonable number for demo
            if (processedRows >= 10)
            {
                _logger.LogInformation("Stopping stream after {ProcessedRows} rows for demo purposes", processedRows);
                break;
            }
        }

        _logger.LogInformation("Streaming completed. Processed {ProcessedRows} rows total", processedRows);
    }

    private async Task RunTrackingExamples()
    {
        _logger.LogInformation("Demonstrating server-side tracking...");

        // Example 1: Track a page view
        _logger.LogInformation("Tracking a page view...");
        var pageViewResult = await _trackingService.TrackPageViewAsync(
            url: "https://example.com/products/item-123",
            actionName: "Product Detail Page"
        );
        _logger.LogInformation("Page view tracking: {Success}, Status: {StatusCode}", 
            pageViewResult.Success, pageViewResult.StatusCode);

        // Example 2: Track a goal conversion
        _logger.LogInformation("Tracking a goal conversion...");
        var goalResult = await _trackingService.TrackGoalAsync(
            goalId: "purchase-goal-uuid",
            revenue: 149.99m
        );
        _logger.LogInformation("Goal tracking: {Success}, Status: {StatusCode}", 
            goalResult.Success, goalResult.StatusCode);

        // Example 3: Track a download
        _logger.LogInformation("Tracking a file download...");
        var downloadResult = await _trackingService.TrackDownloadAsync(
            downloadUrl: "https://example.com/files/user-manual.pdf"
        );
        _logger.LogInformation("Download tracking: {Success}, Status: {StatusCode}", 
            downloadResult.Success, downloadResult.StatusCode);

        // Example 4: Track a site search
        _logger.LogInformation("Tracking a site search...");
        var searchResult = await _trackingService.TrackSiteSearchAsync(
            searchQuery: "wireless headphones",
            categories: new List<string> { "Electronics", "Audio" },
            resultCount: 25
        );
        _logger.LogInformation("Search tracking: {Success}, Status: {StatusCode}", 
            searchResult.Success, searchResult.StatusCode);

        // Example 5: Track an outlink
        _logger.LogInformation("Tracking an external link click...");
        var outlinkResult = await _trackingService.TrackOutlinkAsync(
            linkUrl: "https://partner-site.com"
        );
        _logger.LogInformation("Outlink tracking: {Success}, Status: {StatusCode}", 
            outlinkResult.Success, outlinkResult.StatusCode);
    }

    private void RunJavaScriptCodeGenerationExamples()
    {
        _logger.LogInformation("Demonstrating JavaScript tracking code generation...");

        // Example 1: Standard tracking code with default _paq variable
        _logger.LogInformation("Generating standard tracking code...");
        var standardCode = _codeGenerator.GenerateTrackingCode();
        _logger.LogInformation("Standard code generated ({Length} characters)", standardCode.Length);
        _logger.LogInformation("Preview: {Preview}...", standardCode[..Math.Min(100, standardCode.Length)]);

        // Example 2: Standard tracking code with _ppas variable (to avoid Matomo conflicts)
        _logger.LogInformation("Generating tracking code with _ppas variable (Matomo-compatible)...");
        var ppasCode = _codeGenerator.GenerateTrackingCode(trackerVariableName: "_ppas");
        _logger.LogInformation("PPAS code generated ({Length} characters)", ppasCode.Length);

        // Example 3: Tag Manager tracking code (includes Tag Manager + Consent Manager)
        _logger.LogInformation("Generating Tag Manager tracking code...");
        var tagManagerCode = _codeGenerator.GenerateTagManagerTrackingCode();
        _logger.LogInformation("Tag Manager code generated ({Length} characters)", tagManagerCode.Length);

        // Example 4: Custom site ID
        _logger.LogInformation("Generating tracking code with custom site ID...");
        var customCode = _codeGenerator.GenerateTrackingCode(
            siteId: "custom-site-id-123"
        );
        _logger.LogInformation("Custom code generated with site ID");
    }
    private async Task RunCustomDimensionsExample()
    {
        _logger.LogInformation("Fetching custom dimensions for the website...");

        try
        {
            var customDimensions = await _analyticsService.ListCustomDimensionsAsync();
            
            if (customDimensions != null)
            {
                _logger.LogInformation("Total custom dimensions: {Total}", customDimensions.Meta.Total);
                
                if (customDimensions.Data.Count > 0)
                {
                    foreach (var dimension in customDimensions.Data.Take(5)) // Show first 5
                    {
                        _logger.LogInformation("Custom Dimension:");
                        _logger.LogInformation("  ID: {Id}", dimension.Id);
                        _logger.LogInformation("  Name: {Name}", dimension.Attributes.Name);
                        _logger.LogInformation("  Scope: {Scope}", dimension.Attributes.Scope);
                        _logger.LogInformation("  Active: {Active}", dimension.Attributes.Active);
                        _logger.LogInformation("  Slot: {Slot}", dimension.Attributes.Slot);
                        _logger.LogInformation("  Case Sensitive: {CaseSensitive}", dimension.Attributes.CaseSensitive);
                        _logger.LogInformation("  Tracking ID: {TrackingId}", dimension.Attributes.TrackingId);
                        
                        if (dimension.Attributes.Extractions.Count > 0)
                        {
                            _logger.LogInformation("  Extractions:");
                            foreach (var extraction in dimension.Attributes.Extractions)
                            {
                                _logger.LogInformation("    - Target: {Target}, Pattern: {Pattern}", 
                                    extraction.Target, extraction.Pattern);
                            }
                        }
                        _logger.LogInformation(""); // Empty line for readability
                    }
                    
                    if (customDimensions.Data.Count > 5)
                    {
                        _logger.LogInformation("... and {More} more custom dimensions", 
                            customDimensions.Data.Count - 5);
                    }
                }
                else
                {
                    _logger.LogInformation("No custom dimensions found for this website.");
                }
            }
            else
            {
                _logger.LogWarning("Failed to fetch custom dimensions - received null response");
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Configuration error: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching custom dimensions");
        }
    }
}
