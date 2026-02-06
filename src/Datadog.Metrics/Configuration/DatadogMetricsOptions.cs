namespace Datadog.Metrics.Configuration;

/// <summary>
/// Configuration options for Datadog metrics reporting.
/// </summary>
public class DatadogMetricsOptions
{
    /// <summary>
    /// Datadog API key. Can also be set via DD_API_KEY or DATADOG_API_KEY environment variable.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Datadog site/region. Default: datadoghq.com
    /// Can also be set via DD_SITE or DATADOG_SITE environment variable.
    /// </summary>
    public string Site { get; set; } = DatadogSites.US;

    /// <summary>
    /// Hostname to report with metrics. Default: machine name.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// Prefix prepended to all metric names.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Default tags applied to all metrics.
    /// </summary>
    public IReadOnlyList<string> DefaultTags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Flush interval in seconds. Set to 0 to disable auto-flush.
    /// Default: 15 seconds.
    /// </summary>
    public int FlushIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Maximum number of retry attempts for failed requests.
    /// Default: 2 (total of 3 attempts including initial).
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// Base delay for exponential backoff in seconds.
    /// Default: 1 second.
    /// </summary>
    public double RetryBackoffSeconds { get; set; } = 1.0;

    /// <summary>
    /// Default histogram aggregation options.
    /// </summary>
    public HistogramOptions Histogram { get; set; } = new();

    /// <summary>
    /// Maximum buffer size before forcing a flush (safety limit).
    /// Default: 10000 metrics.
    /// </summary>
    public int MaxBufferSize { get; set; } = 10000;

    /// <summary>
    /// HTTP request timeout in seconds. Default: 30 seconds.
    /// </summary>
    public int HttpTimeoutSeconds { get; set; } = 30;
}
