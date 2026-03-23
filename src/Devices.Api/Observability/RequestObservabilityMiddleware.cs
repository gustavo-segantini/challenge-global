using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Devices.Api.Observability;

public sealed class RequestObservabilityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestObservabilityMiddleware> _logger;
    private readonly HttpRequestBenchmarkCollector _collector;
    private readonly int _slowRequestThresholdMs;

    public RequestObservabilityMiddleware(
        RequestDelegate next,
        ILogger<RequestObservabilityMiddleware> logger,
        HttpRequestBenchmarkCollector collector,
        IOptions<ObservabilityOptions> options)
    {
        _next = next;
        _logger = logger;
        _collector = collector;
        _slowRequestThresholdMs = options.Value.SlowRequestThresholdMs;
    }

    public async Task Invoke(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var elapsedMs = 0d;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Response-Time-Ms"] = Math.Round(elapsedMs, 2).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            return Task.CompletedTask;
        });

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            var method = context.Request.Method;
            var route = context.Request.Path.Value ?? "/";
            var statusCode = context.Response.StatusCode;

            _collector.Record(method, route, statusCode, elapsedMs);

            var scope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["requestMethod"] = method,
                ["requestPath"] = route,
                ["statusCode"] = statusCode,
                ["elapsedMs"] = Math.Round(elapsedMs, 2),
                ["traceId"] = Activity.Current?.TraceId.ToString()
            });

            using (scope)
            {
                if (elapsedMs >= _slowRequestThresholdMs)
                {
                    _logger.LogWarning("Slow HTTP request {RequestMethod} {RequestPath} returned {StatusCode} in {ElapsedMs} ms", method, route, statusCode, elapsedMs);
                }
                else
                {
                    _logger.LogInformation("HTTP request {RequestMethod} {RequestPath} returned {StatusCode} in {ElapsedMs} ms", method, route, statusCode, elapsedMs);
                }
            }
        }
    }
}