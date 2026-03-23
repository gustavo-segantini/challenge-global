using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Devices.Api.Observability;

public sealed class HttpRequestBenchmarkCollector
{
    public const string MeterName = "Devices.Api.Benchmark";

    private readonly object _sync = new();
    private readonly ConcurrentQueue<double> _recentDurationsMs = new();
    private readonly Histogram<double> _durationHistogram;
    private readonly Counter<long> _requestCounter;
    private readonly int _maxWindowSize;

    private long _totalRequests;
    private double _totalDurationMs;
    private double _maxDurationMs;

    public HttpRequestBenchmarkCollector(IMeterFactory meterFactory, int maxWindowSize = 500)
    {
        var meter = meterFactory.Create(MeterName);
        _durationHistogram = meter.CreateHistogram<double>("http.server.request.duration.ms", unit: "ms");
        _requestCounter = meter.CreateCounter<long>("http.server.request.count");
        _maxWindowSize = maxWindowSize;
    }

    public void Record(string method, string route, int statusCode, double elapsedMs)
    {
        _requestCounter.Add(1,
            new KeyValuePair<string, object?>("http.method", method),
            new KeyValuePair<string, object?>("http.route", route),
            new KeyValuePair<string, object?>("http.status_code", statusCode));

        _durationHistogram.Record(elapsedMs,
            new KeyValuePair<string, object?>("http.method", method),
            new KeyValuePair<string, object?>("http.route", route),
            new KeyValuePair<string, object?>("http.status_code", statusCode));

        lock (_sync)
        {
            _totalRequests++;
            _totalDurationMs += elapsedMs;
            _maxDurationMs = Math.Max(_maxDurationMs, elapsedMs);

            _recentDurationsMs.Enqueue(elapsedMs);
            while (_recentDurationsMs.Count > _maxWindowSize)
            {
                _recentDurationsMs.TryDequeue(out _);
            }
        }
    }

    public HttpRequestBenchmarkSnapshot Snapshot()
    {
        lock (_sync)
        {
            var samples = _recentDurationsMs.OrderBy(value => value).ToArray();

            var averageMs = _totalRequests == 0 ? 0 : _totalDurationMs / _totalRequests;
            var p95Ms = samples.Length == 0 ? 0 : Percentile(samples, 0.95);

            return new HttpRequestBenchmarkSnapshot(
                DateTimeOffset.UtcNow,
                _totalRequests,
                samples.Length,
                Math.Round(averageMs, 2),
                Math.Round(_maxDurationMs, 2),
                Math.Round(p95Ms, 2));
        }
    }

    private static double Percentile(double[] sortedValues, double percentile)
    {
        if (sortedValues.Length == 0)
        {
            return 0;
        }

        var index = (int)Math.Ceiling(sortedValues.Length * percentile) - 1;
        index = Math.Clamp(index, 0, sortedValues.Length - 1);

        return sortedValues[index];
    }
}

public sealed record HttpRequestBenchmarkSnapshot(
    DateTimeOffset TimestampUtc,
    long TotalRequests,
    int WindowSize,
    double AverageMs,
    double MaxMs,
    double P95Ms);