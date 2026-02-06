using Datadog.Metrics.Transport.Models;

namespace Datadog.Metrics.Metrics;

/// <summary>
/// Base class for all metric implementations.
/// </summary>
internal abstract class MetricBase : IMetric
{
    /// <summary>
    /// Metric name/key.
    /// </summary>
    protected string Key { get; }

    /// <summary>
    /// Metric tags.
    /// </summary>
    protected string[]? Tags { get; }

    /// <summary>
    /// Hostname for this metric.
    /// </summary>
    protected string Host { get; }

    /// <summary>
    /// Timestamp for the metric (updated on each point).
    /// </summary>
    protected DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricBase"/> class.
    /// </summary>
    /// <param name="key">Metric name.</param>
    /// <param name="tags">Metric tags.</param>
    /// <param name="host">Hostname.</param>
    protected MetricBase(string key, string[]? tags, string host)
    {
        Key = key;
        Tags = tags;
        Host = host;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc/>
    public abstract void AddPoint(double value, DateTimeOffset timestamp);

    /// <inheritdoc/>
    public abstract List<SeriesMetric> Flush();

    /// <summary>
    /// Helper method to create a SeriesMetric object.
    /// </summary>
    /// <param name="value">Metric value.</param>
    /// <param name="type">Datadog metric type.</param>
    /// <param name="keySuffix">Optional suffix to append to key (for histogram aggregates).</param>
    /// <returns>SeriesMetric instance.</returns>
    protected SeriesMetric CreateSeriesMetric(double value, DatadogMetricType type, string? keySuffix = null)
    {
        var metricName = keySuffix != null ? $"{Key}.{keySuffix}" : Key;

        return new SeriesMetric
        {
            Metric = metricName,
            Type = (int)type,
            Points = new[]
            {
                new MetricPoint
                {
                    Timestamp = Timestamp.ToUnixTimeSeconds(),
                    Value = value
                }
            },
            Resources = new[]
            {
                new ResourceMetadata
                {
                    Name = Host,
                    Type = "host"
                }
            },
            Tags = Tags
        };
    }
}
