using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MinimalAPIs.Filters;

// Controller way to write a filter: implement IAsyncActionFilter, attach via options.Filters.Add<T>() or [ServiceFilter].
public class LoggingActionFilter : IAsyncActionFilter
{
    private readonly ILogger<LoggingActionFilter> _logger;

    public LoggingActionFilter(ILogger<LoggingActionFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var executedContext = await next();
        stopwatch.Stop();

        _logger.LogInformation(
            "[Controller] {Method} {Path} -> {ElapsedMs}ms",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path,
            stopwatch.Elapsed.TotalMilliseconds);
    }
}
