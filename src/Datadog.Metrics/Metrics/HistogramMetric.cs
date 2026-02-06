using Datadog.Metrics.Configuration;
using Datadog.Metrics.Transport.Models;

namespace Datadog.Metrics.Metrics;

/// <summary>
/// Histogram metric implementation - client-side statistical aggregation.
/// </summary>
internal sealed class HistogramMetric : MetricBase
{
    private readonly List<double> _samples = new();
    private readonly HistogramOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistogramMetric"/> class.
    /// </summary>
    /// <param name="key">Metric name.</param>
    /// <param name="tags">Metric tags.</param>
    /// <param name="host">Hostname.</param>
    /// <param name="options">Histogram aggregation options.</param>
    public HistogramMetric(string key, string[]? tags, string host, HistogramOptions options)
        : base(key, tags, host)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public override void AddPoint(double value, DateTimeOffset timestamp)
    {
        _samples.Add(value);
        Timestamp = timestamp;
    }

    /// <inheritdoc/>
    public override List<SeriesMetric> Flush()
    {
        if (_samples.Count == 0)
        {
            return new List<SeriesMetric>();
        }

        var result = new List<SeriesMetric>();

        // Sort samples for percentile calculation
        _samples.Sort();

        var count = _samples.Count;
        var sum = _samples.Sum();

        // Basic aggregates
        if (_options.Aggregates.Contains(HistogramAggregate.Min))
        {
            result.Add(CreateSeriesMetric(_samples[0], DatadogMetricType.Gauge, "min"));
        }

        if (_options.Aggregates.Contains(HistogramAggregate.Max))
        {
            result.Add(CreateSeriesMetric(_samples[count - 1], DatadogMetricType.Gauge, "max"));
        }

        if (_options.Aggregates.Contains(HistogramAggregate.Sum))
        {
            result.Add(CreateSeriesMetric(sum, DatadogMetricType.Gauge, "sum"));
        }

        if (_options.Aggregates.Contains(HistogramAggregate.Count))
        {
            result.Add(CreateSeriesMetric(count, DatadogMetricType.Count, "count"));
        }

        if (_options.Aggregates.Contains(HistogramAggregate.Avg))
        {
            result.Add(CreateSeriesMetric(sum / count, DatadogMetricType.Gauge, "avg"));
        }

        if (_options.Aggregates.Contains(HistogramAggregate.Median))
        {
            var median = CalculatePercentile(0.5);
            result.Add(CreateSeriesMetric(median, DatadogMetricType.Gauge, "median"));
        }

        // Percentiles
        foreach (var percentile in _options.Percentiles)
        {
            var value = CalculatePercentile(percentile);
            var label = $"{percentile * 100:0}percentile";
            result.Add(CreateSeriesMetric(value, DatadogMetricType.Gauge, label));
        }

        return result;
    }

    /// <summary>
    /// Calculate a percentile from sorted samples.
    /// </summary>
    /// <param name="percentile">Percentile value (0.0 to 1.0).</param>
    /// <returns>Percentile value.</returns>
    private double CalculatePercentile(double percentile)
    {
        // Formula: index = round(percentile × count) - 1
        var index = (int)Math.Round(percentile * _samples.Count) - 1;

        // Clamp to valid range
        index = Math.Max(0, Math.Min(_samples.Count - 1, index));

        return _samples[index];
    }
}
