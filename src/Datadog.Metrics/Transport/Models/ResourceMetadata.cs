using System.Text.Json.Serialization;

namespace Datadog.Metrics.Transport.Models;

/// <summary>
/// Resource metadata for v2 API.
/// </summary>
public class ResourceMetadata
{
    /// <summary>
    /// Resource name (e.g., hostname).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Resource type (e.g., "host").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}
