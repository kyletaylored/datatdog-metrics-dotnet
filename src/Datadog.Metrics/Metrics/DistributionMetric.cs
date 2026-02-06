using Datadog.Metrics.Transport.Models;

namespace Datadog.Metrics.Metrics;

/// <summary>
/// Distribution metric implementation - server-side aggregation.
/// Sends all individual values to Datadog for server-side calculation.
/// </summary>
internal sealed class DistributionMetric : MetricBase
{
    private readonly Dictionary<long, List<double>> _points = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributionMetric"/> class.
    /// </summary>
    /// <param name="key">Metric name.</param>
    /// <param name="tags">Metric tags.</param>
    /// <param name="host">Hostname.</param>
    public DistributionMetric(string key, string[]? tags, string host)
        : base(key, tags, host)
    {
    }

    /// <inheritdoc/>
    public override void AddPoint(double value, DateTimeOffset timestamp)
    {
        var timestampSeconds = timestamp.ToUnixTimeSeconds();

        // Group values by timestamp (in seconds)
        if (!_points.TryGetValue(timestampSeconds, out var values))
        {
            values = new List<double>();
            _points[timestampSeconds] = values;
        }

        values.Add(value);
        Timestamp = timestamp;
    }

    /// <inheritdoc/>
    public override List<SeriesMetric> Flush()
    {
        // Distributions send all points to server for aggregation
        var points = new List<DistributionPoint>();

        foreach (var kvp in _points.OrderBy(x => x.Key))
        {
            points.Add(new DistributionPoint
            {
                Timestamp = kvp.Key,
                Values = kvp.Value.ToArray()
            });
        }

        return new List<SeriesMetric>
        {
            new SeriesMetric
            {
                Metric = Key,
                Type = (int)DatadogMetricType.Gauge, // Not used for distributions
                Points = points.Cast<MetricPoint>().ToArray(),
                Resources = new[]
                {
                    new ResourceMetadata { Name = Host, Type = "host" }
                },
                Tags = Tags,
                IsDistribution = true // Flag for v1 API routing
            }
        };
    }
}
