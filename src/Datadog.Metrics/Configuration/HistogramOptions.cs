namespace Datadog.Metrics.Configuration;

/// <summary>
/// Configuration options for histogram metric aggregation.
/// </summary>
public class HistogramOptions
{
    /// <summary>
    /// Statistical aggregates to calculate.
    /// Default: min, max, avg, count, sum, median
    /// </summary>
    public HistogramAggregate[] Aggregates { get; set; } =
    {
        HistogramAggregate.Min,
        HistogramAggregate.Max,
        HistogramAggregate.Avg,
        HistogramAggregate.Count,
        HistogramAggregate.Sum,
        HistogramAggregate.Median
    };

    /// <summary>
    /// Percentiles to calculate (0.0 to 1.0).
    /// Default: 75th, 85th, 95th, 99th percentiles.
    /// </summary>
    public double[] Percentiles { get; set; } = { 0.75, 0.85, 0.95, 0.99 };
}

/// <summary>
/// Available histogram aggregation types.
/// </summary>
public enum HistogramAggregate
{
    /// <summary>Minimum value</summary>
    Min,

    /// <summary>Maximum value</summary>
    Max,

    /// <summary>Average value</summary>
    Avg,

    /// <summary>Count of samples</summary>
    Count,

    /// <summary>Sum of all values</summary>
    Sum,

    /// <summary>Median value (50th percentile)</summary>
    Median
}
