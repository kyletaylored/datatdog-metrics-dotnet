using Datadog.Metrics.Transport.Models;

namespace Datadog.Metrics.Metrics;

/// <summary>
/// Counter metric implementation - accumulates values.
/// </summary>
internal sealed class CounterMetric : MetricBase
{
    private long _sum;

    /// <summary>
    /// Initializes a new instance of the <see cref="CounterMetric"/> class.
    /// </summary>
    /// <param name="key">Metric name.</param>
    /// <param name="tags">Metric tags.</param>
    /// <param name="host">Hostname.</param>
    public CounterMetric(string key, string[]? tags, string host)
        : base(key, tags, host)
    {
    }

    /// <inheritdoc/>
    public override void AddPoint(double value, DateTimeOffset timestamp)
    {
        // Accumulate values
        _sum += (long)value;
        Timestamp = timestamp;
    }

    /// <inheritdoc/>
    public override List<SeriesMetric> Flush()
    {
        return new List<SeriesMetric>
        {
            CreateSeriesMetric(_sum, DatadogMetricType.Count)
        };
    }
}
