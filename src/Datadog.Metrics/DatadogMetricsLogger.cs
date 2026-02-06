using Datadog.Metrics.Configuration;
using Datadog.Metrics.Core;
using Datadog.Metrics.Metrics;
using Microsoft.Extensions.Options;

namespace Datadog.Metrics;

/// <summary>
/// Main class for logging metrics to Datadog.
/// </summary>
public sealed class DatadogMetricsLogger
{
    private readonly MetricsAggregator _aggregator;
    private readonly HistogramOptions _defaultHistogramOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatadogMetricsLogger"/> class.
    /// </summary>
    /// <param name="aggregator">Metrics aggregator.</param>
    /// <param name="options">Datadog metrics options.</param>
    internal DatadogMetricsLogger(MetricsAggregator aggregator, IOptions<DatadogMetricsOptions> options)
    {
        _aggregator = aggregator;
        _defaultHistogramOptions = options.Value.Histogram;
    }

    /// <summary>
    /// Records a gauge value. Latest value wins.
    /// </summary>
    /// <param name="key">Metric name.</param>
    /// <param name="value">Metric value.</param>
    /// <param name="tags">Optional tags.</param>
    /// <param name="timestamp">Optional timestamp (defaults to now).</param>
    public void Gauge(string key, double value, string[]? tags = null, DateTimeOffset? timestamp = null)
    {
        _aggregator.AddPoint(MetricType.Gauge, key, value, tags, timestamp ?? DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Records a counter value. Values are accumulated.
    /// </summary>
    /// <param name="key">Metric name.</param>
    /// <param name="value">Metric value.</param>
    /// <param name="tags">Optional tags.</param>
    /// <param name="timestamp">Optional timestamp (defaults to now).</param>
    public void Counter(string key, double value, string[]? tags = null, DateTimeOffset? timestamp = null)
    {
        _aggregator.AddPoint(MetricType.Counter, key, value, tags, timestamp ?? DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Increments a counter by 1.
    /// </summary>
    /// <param name="key">Metric name.</param>
    /// <param name="tags">Optional tags.</param>
    /// <param name="timestamp">Optional timestamp (defaults to now).</param>
    public void Increment(string key, string[]? tags = null, DateTimeOffset? timestamp = null)
    {
        Counter(key, 1, tags, timestamp);
    }

    /// <summary>
    /// Records a histogram value with client-side aggregation.
    /// </summary>
    /// <param name="key">Metric name.</param>
    /// <param name="value">Metric value.</param>
    /// <param name="tags">Optional tags.</param>
    /// <param name="timestamp">Optional timestamp (defaults to now).</param>
    /// <param name="options">Optional histogram options (defaults to configuration).</param>
    public void Histogram(
        string key,
        double value,
        string[]? tags = null,
        DateTimeOffset? timestamp = null,
        HistogramOptions? options = null)
    {
        _aggregator.AddPoint(
            MetricType.Histogram,
            key,
            value,
            tags,
            timestamp ?? DateTimeOffset.UtcNow,
            options ?? _defaultHistogramOptions);
    }

    /// <summary>
    /// Records a distribution value for server-side aggregation.
    /// </summary>
    /// <param name="key">Metric name.</param>
    /// <param name="value">Metric value.</param>
    /// <param name="tags">Optional tags.</param>
    /// <param name="timestamp">Optional timestamp (defaults to now).</param>
    public void Distribution(string key, double value, string[]? tags = null, DateTimeOffset? timestamp = null)
    {
        _aggregator.AddPoint(MetricType.Distribution, key, value, tags, timestamp ?? DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Manually flushes all buffered metrics.
    /// Note: Metrics are automatically flushed at regular intervals.
    /// </summary>
    /// <returns>List of flushed metrics.</returns>
    public List<Transport.Models.SeriesMetric> Flush()
    {
        return _aggregator.Flush();
    }
}
