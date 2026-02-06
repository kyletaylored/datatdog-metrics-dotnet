using System.Text.Json.Serialization;

namespace Datadog.Metrics.Transport.Models;

/// <summary>
/// Represents a single metric series.
/// </summary>
public class SeriesMetric
{
    /// <summary>
    /// Metric name.
    /// </summary>
    [JsonPropertyName("metric")]
    public string Metric { get; set; } = string.Empty;

    /// <summary>
    /// Metric type (0=gauge, 1=rate, 2=count).
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }

    /// <summary>
    /// Array of metric points.
    /// </summary>
    [JsonPropertyName("points")]
    public MetricPoint[] Points { get; set; } = Array.Empty<MetricPoint>();

    /// <summary>
    /// Resource metadata (v2 API).
    /// </summary>
    [JsonPropertyName("resources")]
    public ResourceMetadata[]? Resources { get; set; }

    /// <summary>
    /// Metric tags.
    /// </summary>
    [JsonPropertyName("tags")]
    public string[]? Tags { get; set; }

    /// <summary>
    /// Indicates if this is a distribution metric (uses v1 API).
    /// Not serialized - used internally for routing.
    /// </summary>
    [JsonIgnore]
    public bool IsDistribution { get; set; }
}
