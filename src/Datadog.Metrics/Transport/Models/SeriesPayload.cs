using System.Text.Json.Serialization;

namespace Datadog.Metrics.Transport.Models;

/// <summary>
/// Payload for Datadog series API v2 (regular metrics).
/// </summary>
public class SeriesPayload
{
    /// <summary>
    /// Array of metric series.
    /// </summary>
    [JsonPropertyName("series")]
    public SeriesMetric[] Series { get; set; } = Array.Empty<SeriesMetric>();
}

/// <summary>
/// Payload for Datadog distribution API v1 (distribution metrics).
/// </summary>
public class DistributionPayload
{
    /// <summary>
    /// Array of distribution series.
    /// </summary>
    [JsonPropertyName("series")]
    public SeriesMetric[] Series { get; set; } = Array.Empty<SeriesMetric>();
}
