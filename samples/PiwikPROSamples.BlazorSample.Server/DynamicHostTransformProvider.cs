using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

public class DynamicHostTransformProvider : ITransformProvider
{
    public void ValidateRoute(TransformRouteValidationContext context)
    {
        // No validation needed
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
        // No validation needed
    }

    public void Apply(TransformBuilderContext context)
    {
        // Add the dynamic host transform to all routes
        context.AddRequestTransform(transformContext =>
        {
            var hostname = transformContext.HttpContext.Request.RouteValues["hostname"]?.ToString();

            if (!string.IsNullOrEmpty(hostname))
            {
                var request = transformContext.ProxyRequest;
                var catchAllPath = transformContext.HttpContext.Request.RouteValues["catch-all"]?.ToString() ?? string.Empty;

                // Ensure path starts with '/'
                var path = catchAllPath.StartsWith('/') ? catchAllPath : "/" + catchAllPath;

                // Build absolute upstream URI and preserve query string
                var baseUri = new Uri($"https://{hostname}");
                var targetUri = new Uri(baseUri, path);

                var queryString = transformContext.HttpContext.Request.QueryString.Value;
                if (!string.IsNullOrEmpty(queryString))
                {
                    var ub = new UriBuilder(targetUri)
                    {
                        Query = queryString!.TrimStart('?')
                    };
                    targetUri = ub.Uri;
                }

                request.RequestUri = targetUri;

                // Set the Host header for upstream
                request.Headers.Host = hostname;
            }

            return ValueTask.CompletedTask;
        });
    }
}
