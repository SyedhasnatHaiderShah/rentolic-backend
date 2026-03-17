using System.Diagnostics;
using System.Text.Json;

namespace Rentolic.Api.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;

        try
        {
            await _next(context);
            sw.Stop();

            _logger.LogInformation("API Call: {Method} {Path} responded {StatusCode} in {Elapsed}ms",
                method, path, context.Response.StatusCode, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "API Call: {Method} {Path} failed after {Elapsed}ms",
                method, path, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
