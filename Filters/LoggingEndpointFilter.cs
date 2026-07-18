using System.Diagnostics;

namespace MinimalAPIs.Filters;

// Minimal API way to write a filter: implement IEndpointFilter, attach via .AddEndpointFilter<T>().
public class LoggingEndpointFilter : IEndpointFilter
{
    private readonly ILogger<LoggingEndpointFilter> _logger;

    public LoggingEndpointFilter(ILogger<LoggingEndpointFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await next(context);
        stopwatch.Stop();

        _logger.LogInformation(
            "[Minimal] {Method} {Path} -> {ElapsedMs}ms",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path,
            stopwatch.Elapsed.TotalMilliseconds);

        return result;
    }
}
