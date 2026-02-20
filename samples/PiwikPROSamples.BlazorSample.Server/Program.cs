using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;
using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);

// Enable verbose proxy logging in Development via code (can also be done in appsettings)
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddFilter("Yarp.ReverseProxy", LogLevel.Debug);
    builder.Logging.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Debug);
}

// Request/response HTTP logging (headers, bodies) for troubleshooting
builder.Services.AddHttpLogging(o =>
{
    o.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                      HttpLoggingFields.ResponsePropertiesAndHeaders |
                      HttpLoggingFields.RequestQuery |
                      HttpLoggingFields.RequestBody |
                      HttpLoggingFields.ResponseBody;
    o.RequestBodyLogLimit = 4096;
    o.ResponseBodyLogLimit = 4096;
});

// Register custom transform provider
builder.Services.AddSingleton<ITransformProvider, DynamicHostTransformProvider>();

// Add YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseHttpsRedirection();

// Log all requests for /api/proxy/* for quick tracing
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api/proxy"))
    {
        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ProxyTrace");
        logger.LogInformation("Incoming proxy request: {Method} {Path}{Query}", ctx.Request.Method, ctx.Request.Path, ctx.Request.QueryString);
    }
    await next();
});

app.UseHttpLogging();

app.UseCors();

// Serve Blazor WebAssembly framework files from the client project
app.UseBlazorFrameworkFiles();

// Serve static files from both the server and client projects
// UseStaticFiles() enables serving static files from wwwroot and static web assets from referenced projects
app.UseStaticFiles();

app.UseRouting();

// Map YARP reverse proxy routes
app.MapReverseProxy();

// Fallback to index.html for client-side routing
app.MapFallbackToFile("index.html");

app.Run();
