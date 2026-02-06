using System.Collections.Concurrent;
using Datadog.Metrics.Configuration;
using Datadog.Metrics.Metrics;
using Datadog.Metrics.Transport.Models;

namespace Datadog.Metrics.Core;

/// <summary>
/// Buffers and aggregates metrics by key+tags combination.
/// </summary>
internal sealed class MetricsAggregator
{
    private readonly ConcurrentDictionary<string, IMetric> _buffer = new();
    private readonly string _host;
    private readonly string? _prefix;
    private readonly IReadOnlyList<string> _defaultTags;
    private readonly int _maxBufferSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsAggregator"/> class.
    /// </summary>
    /// <param name="host">Hostname for metrics.</param>
    /// <param name="prefix">Prefix for metric names.</param>
    /// <param name="defaultTags">Default tags applied to all metrics.</param>
    /// <param name="maxBufferSize">Maximum buffer size before warning.</param>
    public MetricsAggregator(
        string host,
        string? prefix,
        IReadOnlyList<string> defaultTags,
        int maxBufferSize)
    {
        _host = host;
        _prefix = prefix;
        _defaultTags = defaultTags;
        _maxBufferSize = maxBufferSize;
    }

    /// <summary>
    /// Add a metric point to the buffer.
    /// </summary>
    /// <param name="type">Metric type.</param>
    /// <param name="key">Metric name.</param>
    /// <param name="value">Metric value.</param>
    /// <param name="tags">Metric tags.</param>
    /// <param name="timestamp">Timestamp for the point.</param>
    /// <param name="histogramOptions">Histogram options (only for histograms).</param>
    public void AddPoint(
        MetricType type,
        string key,
        double value,
        string[]? tags,
        DateTimeOffset timestamp,
        HistogramOptions? histogramOptions = null)
    {
        // Apply prefix to key
        var fullKey = string.IsNullOrEmpty(_prefix) ? key : $"{_prefix}{key}";

        // Generate buffer key from metric name and sorted tags
        var bufferKey = MakeBufferKey(fullKey, tags);

        // Get or create metric instance
        var metric = _buffer.GetOrAdd(bufferKey, _ => CreateMetric(
            type,
            fullKey,
            tags,
            _host,
            histogramOptions));

        // Add the point to the metric
        metric.AddPoint(value, timestamp);

        // Safety check: warn if buffer is too large
        if (_buffer.Count > _maxBufferSize)
        {
            // TODO: Log warning about large buffer
        }
    }

    /// <summary>
    /// Flush all buffered metrics and return serialized series.
    /// </summary>
    /// <returns>List of metric series.</returns>
    public List<SeriesMetric> Flush()
    {
        var metrics = new List<SeriesMetric>();

        // Flush each metric in the buffer
        foreach (var kvp in _buffer)
        {
            var metricData = kvp.Value.Flush();

            // Apply default tags to each series
            foreach (var series in metricData)
            {
                var allTags = new List<string>(_defaultTags);
                if (series.Tags != null)
                {
                    allTags.AddRange(series.Tags);
                }
                series.Tags = allTags.ToArray();
                metrics.Add(series);
            }
        }

        // Clear the buffer
        _buffer.Clear();

        return metrics;
    }

    /// <summary>
    /// Generate a buffer key from metric name and tags.
    /// Tags are sorted to ensure consistent keys.
    /// </summary>
    /// <param name="key">Metric name.</param>
    /// <param name="tags">Metric tags.</param>
    /// <returns>Buffer key string.</returns>
    private string MakeBufferKey(string key, string[]? tags)
    {
        if (tags == null || tags.Length == 0)
        {
            return $"{key}#";
        }

        // Sort tags for consistent key generation
        // This ensures ["tag:a", "tag:b"] and ["tag:b", "tag:a"] map to same key
        var sortedTags = tags.OrderBy(t => t, StringComparer.Ordinal).ToArray();
        return $"{key}#{string.Join(".", sortedTags)}";
    }

    /// <summary>
    /// Factory method to create metric instances based on type.
    /// </summary>
    /// <param name="type">Metric type.</param>
    /// <param name="key">Metric name.</param>
    /// <param name="tags">Metric tags.</param>
    /// <param name="host">Hostname.</param>
    /// <param name="histogramOptions">Histogram options.</param>
    /// <returns>New metric instance.</returns>
    private IMetric CreateMetric(
        MetricType type,
        string key,
        string[]? tags,
        string host,
        HistogramOptions? histogramOptions)
    {
        return type switch
        {
            MetricType.Gauge => new GaugeMetric(key, tags, host),
            MetricType.Counter => new CounterMetric(key, tags, host),
            MetricType.Histogram => new HistogramMetric(key, tags, host, histogramOptions!),
            MetricType.Distribution => new DistributionMetric(key, tags, host),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown metric type")
        };
    }
}
