using Datadog.Metrics.Transport.Models;

namespace Datadog.Metrics.Metrics;

/// <summary>
/// Gauge metric implementation - latest value wins.
/// </summary>
internal sealed class GaugeMetric : MetricBase
{
    private double _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="GaugeMetric"/> class.
    /// </summary>
    /// <param name="key">Metric name.</param>
    /// <param name="tags">Metric tags.</param>
    /// <param name="host">Hostname.</param>
    public GaugeMetric(string key, string[]? tags, string host)
        : base(key, tags, host)
    {
    }

    /// <inheritdoc/>
    public override void AddPoint(double value, DateTimeOffset timestamp)
    {
        // For gauges, latest value wins
        _value = value;
        Timestamp = timestamp;
    }

    /// <inheritdoc/>
    public override List<SeriesMetric> Flush()
    {
        return new List<SeriesMetric>
        {
            CreateSeriesMetric(_value, DatadogMetricType.Gauge)
        };
    }
}
