using System.Diagnostics.Metrics;
using Datadog.Metrics.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Datadog.Metrics.Integration;

/// <summary>
/// Listens to System.Diagnostics.Metrics and forwards measurements to Datadog.
/// </summary>
internal sealed class DatadogMeterListener : IHostedService, IDisposable
{
    private readonly DatadogMetricsLogger _logger;
    private readonly DatadogMetricsOptions _options;
    private readonly ILogger<DatadogMeterListener> _systemLogger;
    private MeterListener? _meterListener;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatadogMeterListener"/> class.
    /// </summary>
    /// <param name="logger">Datadog metrics logger.</param>
    /// <param name="options">Datadog metrics options.</param>
    /// <param name="systemLogger">System logger.</param>
    public DatadogMeterListener(
        DatadogMetricsLogger logger,
        IOptions<DatadogMetricsOptions> options,
        ILogger<DatadogMeterListener> systemLogger)
    {
        _logger = logger;
        _options = options.Value;
        _systemLogger = systemLogger;
    }

    /// <summary>
    /// Starts listening to meters.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                // Subscribe to all instruments
                listener.EnableMeasurementEvents(instrument);
                _systemLogger.LogDebug(
                    "Subscribed to instrument: {InstrumentName} ({InstrumentType})",
                    instrument.Name, instrument.GetType().Name);
            }
        };

        // Set up callbacks for different instrument types
        _meterListener.SetMeasurementEventCallback<double>(OnMeasurement);
        _meterListener.SetMeasurementEventCallback<int>(OnMeasurement);
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurement);
        _meterListener.SetMeasurementEventCallback<float>(OnMeasurement);

        _meterListener.Start();

        _systemLogger.LogInformation("Started System.Diagnostics.Metrics integration");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops listening to meters.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _meterListener?.Dispose();
        _meterListener = null;

        _systemLogger.LogInformation("Stopped System.Diagnostics.Metrics integration");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when a measurement is recorded.
    /// </summary>
    private void OnMeasurement<T>(
        Instrument instrument,
        T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state) where T : struct
    {
        var value = Convert.ToDouble(measurement);
        var tagArray = ConvertTags(tags);

        // Determine metric type based on instrument type
        switch (instrument)
        {
            case Counter<T>:
            case ObservableCounter<T>:
                _logger.Counter(instrument.Name, value, tagArray);
                break;

            case Histogram<T>:
                _logger.Histogram(instrument.Name, value, tagArray);
                break;

            case ObservableGauge<T>:
                _logger.Gauge(instrument.Name, value, tagArray);
                break;

            case UpDownCounter<T>:
            case ObservableUpDownCounter<T>:
                // Treat as gauge since it can go up or down
                _logger.Gauge(instrument.Name, value, tagArray);
                break;

            default:
                _systemLogger.LogWarning(
                    "Unknown instrument type: {InstrumentType} for {InstrumentName}",
                    instrument.GetType().Name, instrument.Name);
                break;
        }
    }

    /// <summary>
    /// Converts System.Diagnostics.Metrics tags to Datadog tag format.
    /// </summary>
    private static string[] ConvertTags(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (tags.Length == 0)
        {
            return Array.Empty<string>();
        }

        var result = new string[tags.Length];
        for (var i = 0; i < tags.Length; i++)
        {
            var tag = tags[i];
            result[i] = $"{tag.Key}:{tag.Value}";
        }

        return result;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _meterListener?.Dispose();
    }
}
