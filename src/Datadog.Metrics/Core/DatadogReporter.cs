using Datadog.Metrics.Configuration;
using Datadog.Metrics.Transport;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Datadog.Metrics.Core;

/// <summary>
/// Background service that periodically flushes metrics to Datadog.
/// </summary>
internal sealed class DatadogReporter : BackgroundService
{
    private readonly MetricsAggregator _aggregator;
    private readonly HttpApi _httpApi;
    private readonly DatadogMetricsOptions _options;
    private readonly ILogger<DatadogReporter> _logger;
    private readonly TimeSpan _flushInterval;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatadogReporter"/> class.
    /// </summary>
    /// <param name="aggregator">Metrics aggregator.</param>
    /// <param name="httpApi">HTTP API client.</param>
    /// <param name="options">Datadog metrics options.</param>
    /// <param name="logger">Logger instance.</param>
    public DatadogReporter(
        MetricsAggregator aggregator,
        HttpApi httpApi,
        IOptions<DatadogMetricsOptions> options,
        ILogger<DatadogReporter> logger)
    {
        _aggregator = aggregator;
        _httpApi = httpApi;
        _options = options.Value;
        _logger = logger;
        _flushInterval = TimeSpan.FromSeconds(_options.FlushIntervalSeconds);
    }

    /// <summary>
    /// Executes the background reporter service.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Starting Datadog metrics reporter with flush interval of {FlushInterval} seconds",
            _options.FlushIntervalSeconds);

        try
        {
            // Wait for the first flush interval before starting
            await Task.Delay(_flushInterval, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await FlushMetricsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error flushing metrics to Datadog");
                }

                // Wait for next flush interval
                await Task.Delay(_flushInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
            _logger.LogInformation("Datadog metrics reporter is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Datadog metrics reporter");
            throw;
        }
    }

    /// <summary>
    /// Performs final flush when the service is stopping.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Performing final metrics flush before shutdown");

        try
        {
            await FlushMetricsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during final metrics flush");
        }

        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Flushes metrics from aggregator and submits to Datadog.
    /// </summary>
    private async Task FlushMetricsAsync(CancellationToken cancellationToken)
    {
        var metrics = _aggregator.Flush();

        if (metrics.Count == 0)
        {
            _logger.LogDebug("No metrics to flush");
            return;
        }

        _logger.LogDebug("Flushing {Count} metric series to Datadog", metrics.Count);

        var success = await _httpApi.SubmitMetricsAsync(metrics, cancellationToken);

        if (success)
        {
            _logger.LogDebug("Successfully flushed {Count} metric series", metrics.Count);
        }
        else
        {
            _logger.LogWarning("Failed to flush {Count} metric series", metrics.Count);
        }
    }
}
