using Datadog.Metrics.Transport.Models;

namespace Datadog.Metrics.Metrics;

/// <summary>
/// Base interface for all metric types.
/// </summary>
internal interface IMetric
{
    /// <summary>
    /// Add a data point to this metric.
    /// </summary>
    /// <param name="value">The value to add.</param>
    /// <param name="timestamp">The timestamp for this point.</param>
    void AddPoint(double value, DateTimeOffset timestamp);

    /// <summary>
    /// Flush this metric and return the serialized series.
    /// </summary>
    /// <returns>List of series metrics to send to Datadog.</returns>
    List<SeriesMetric> Flush();
}

/// <summary>
/// Metric type enumeration.
/// </summary>
internal enum MetricType
{
    /// <summary>Gauge metric (point-in-time value)</summary>
    Gauge,

    /// <summary>Counter metric (cumulative)</summary>
    Counter,

    /// <summary>Histogram metric (client-side aggregation)</summary>
    Histogram,

    /// <summary>Distribution metric (server-side aggregation)</summary>
    Distribution
}

/// <summary>
/// Datadog metric type codes for v2 API.
/// </summary>
internal enum DatadogMetricType
{
    /// <summary>Gauge (0)</summary>
    Gauge = 0,

    /// <summary>Rate (1)</summary>
    Rate = 1,

    /// <summary>Count (2)</summary>
    Count = 2
}
