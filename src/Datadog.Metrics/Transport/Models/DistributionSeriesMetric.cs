using System.Text.Json.Serialization;

namespace Datadog.Metrics.Transport.Models;

/// <summary>
/// Represents a distribution metric series for v1 API.
/// Distribution points use a different format: [[timestamp, [values]]]
/// </summary>
public class DistributionSeriesMetric
{
    /// <summary>
    /// Metric name.
    /// </summary>
    [JsonPropertyName("metric")]
    public string Metric { get; set; } = string.Empty;

    /// <summary>
    /// Host name.
    /// </summary>
    [JsonPropertyName("host")]
    public string? Host { get; set; }

    /// <summary>
    /// Array of distribution points as tuples: [[timestamp, [values]]]
    /// </summary>
    [JsonPropertyName("points")]
    public object[][] Points { get; set; } = Array.Empty<object[]>();

    /// <summary>
    /// Metric tags.
    /// </summary>
    [JsonPropertyName("tags")]
    public string[]? Tags { get; set; }

    /// <summary>
    /// Type is always "distribution" for distribution metrics.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "distribution";
}
