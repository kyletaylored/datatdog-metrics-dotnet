using System.Text.Json.Serialization;

namespace Datadog.Metrics.Transport.Models;

/// <summary>
/// Represents a single data point for a metric.
/// </summary>
public class MetricPoint
{
    /// <summary>
    /// Unix timestamp in seconds.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Metric value.
    /// </summary>
    [JsonPropertyName("value")]
    public double Value { get; set; }
}

/// <summary>
/// Represents a distribution point with multiple values.
/// </summary>
public class DistributionPoint : MetricPoint
{
    /// <summary>
    /// Array of values for distributions.
    /// </summary>
    [JsonPropertyName("value")]
    public double[] Values { get; set; } = Array.Empty<double>();
}
