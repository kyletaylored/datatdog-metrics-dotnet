using System.Diagnostics.Metrics;
using Datadog.Metrics;
using Datadog.Metrics.Configuration;
using Datadog.Metrics.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ===================================================================
// Datadog.Metrics Sample Application
// ===================================================================
// This sample demonstrates:
// 1. How to configure Datadog metrics with secure API key loading
// 2. All metric types (Gauge, Counter, Histogram, Distribution)
// 3. System.Diagnostics.Metrics integration
// 4. Custom tags and configuration
// ===================================================================

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║          Datadog.Metrics Sample Application          ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();

// Create host builder with configuration
// Note: CreateApplicationBuilder automatically adds:
//   1. appsettings.json
//   2. appsettings.{Environment}.json (e.g., appsettings.Development.json)
//   3. User secrets (in Development environment)
//   4. Environment variables
//   5. Command-line arguments
var builder = Host.CreateApplicationBuilder(args);

// Validate API key is provided
// Priority: 1. DatadogMetrics__ApiKey or DD_API_KEY from env, 2. appsettings.json
// Note: Use string.IsNullOrWhiteSpace because appsettings.json may have empty strings
var apiKey = builder.Configuration["DatadogMetrics:ApiKey"];
if (string.IsNullOrWhiteSpace(apiKey))
{
    apiKey = builder.Configuration["DD_API_KEY"];  // Flat env var
}
if (string.IsNullOrWhiteSpace(apiKey))
{
    apiKey = builder.Configuration["DATADOG_API_KEY"];  // Alternative name
}

if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException(
        "Datadog API key not found. Please provide it via:\n" +
        "  1. Environment variable: DD_API_KEY=your-key\n" +
        "  2. User secrets: dotnet user-secrets set \"DatadogMetrics:ApiKey\" \"your-key\"\n" +
        "  3. appsettings.json (not recommended for production)");
}

var maskedKey = apiKey.Length >= 8 ? $"{apiKey[..8]}..." : $"{apiKey[..Math.Min(4, apiKey.Length)]}...";
Console.WriteLine($"✓ API Key loaded: {maskedKey} (showing first characters)");
Console.WriteLine($"✓ Target Site: {builder.Configuration["DatadogMetrics:Site"] ?? "datadoghq.com"}");
Console.WriteLine();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Register Datadog metrics with System.Diagnostics.Metrics integration
builder.Services.AddDatadogMetricsWithDiagnostics(options =>
{
    builder.Configuration.GetSection("DatadogMetrics").Bind(options);

    // Override API key from secure source
    options.ApiKey = apiKey;

    // Display configuration
    Console.WriteLine("Datadog Metrics Configuration:");
    Console.WriteLine($"   • Site: {options.Site}");
    Console.WriteLine($"   • Flush Interval: {options.FlushIntervalSeconds}s");
    Console.WriteLine($"   • Prefix: {options.Prefix ?? "(none)"}");
    Console.WriteLine($"   • Default Tags: {string.Join(", ", options.DefaultTags)}");
    Console.WriteLine($"   • Max Retries: {options.MaxRetries}");
    Console.WriteLine();
});

// Register the sample worker service
builder.Services.AddHostedService<MetricsSampleWorker>();

var host = builder.Build();

Console.WriteLine("Starting application...");
Console.WriteLine("   Press Ctrl+C to stop");
Console.WriteLine();
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

await host.RunAsync();

// ===================================================================
// Sample Worker Service - Demonstrates All Metric Types
// ===================================================================
public class MetricsSampleWorker : BackgroundService
{
    private readonly DatadogMetricsLogger _metrics;
    private readonly ILogger<MetricsSampleWorker> _logger;
    private readonly Meter _meter;
    private readonly Counter<int> _requestCounter;
    private readonly Histogram<double> _requestDuration;
    private readonly ObservableGauge<double> _cpuUsage;
    private int _requestCount;
    private readonly Random _random = new();

    public MetricsSampleWorker(
        DatadogMetricsLogger metrics,
        ILogger<MetricsSampleWorker> logger)
    {
        _metrics = metrics;
        _logger = logger;

        // System.Diagnostics.Metrics integration example
        _meter = new Meter("DatadogMetricsSample", "1.0.0");
        _requestCounter = _meter.CreateCounter<int>("sample.requests.total");
        _requestDuration = _meter.CreateHistogram<double>("sample.request.duration");

        // Observable gauge that reports CPU usage
        _cpuUsage = _meter.CreateObservableGauge("sample.cpu.usage",
            () => new Measurement<double>(
                _random.NextDouble() * 100,
                new KeyValuePair<string, object?>("host", Environment.MachineName)));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MetricsSampleWorker started");

        var iteration = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            iteration++;

            Console.WriteLine($"Iteration {iteration} - Recording metrics...");

            // ═══════════════════════════════════════════════════════════
            // 1. GAUGE - Records current value (latest wins)
            // ═══════════════════════════════════════════════════════════
            var temperature = 65 + _random.NextDouble() * 10;
            _metrics.Gauge("temperature.celsius", temperature,
                new[] { "location:datacenter", "sensor:1" });
            Console.WriteLine($"Gauge: temperature.celsius = {temperature:F2}°C");

            var memoryUsage = 50 + _random.NextDouble() * 30;
            _metrics.Gauge("memory.usage.percent", memoryUsage,
                new[] { "type:physical" });
            Console.WriteLine($"Gauge: memory.usage.percent = {memoryUsage:F2}%");

            // ═══════════════════════════════════════════════════════════
            // 2. COUNTER - Accumulates values over time
            // ═══════════════════════════════════════════════════════════
            var errorCount = _random.Next(0, 3);
            if (errorCount > 0)
            {
                _metrics.Counter("errors.total", errorCount,
                    new[] { "type:validation", "severity:warning" });
                Console.WriteLine($"Counter: errors.total += {errorCount}");
            }

            _metrics.Increment("requests.total",
                new[] { "endpoint:/api/sample", "method:GET", "status:200" });
            Console.WriteLine("Counter: requests.total += 1");

            // ═══════════════════════════════════════════════════════════
            // 3. HISTOGRAM - Client-side aggregation with percentiles
            // ═══════════════════════════════════════════════════════════
            var responseTime = 50 + _random.NextDouble() * 200;
            _metrics.Histogram("response.time.ms", responseTime,
                new[] { "endpoint:/api/sample" });
            Console.WriteLine($"Histogram: response.time.ms = {responseTime:F2}ms");

            // Another histogram with custom options
            var customHistogramOptions = new HistogramOptions
            {
                Aggregates = new[] {
                    HistogramAggregate.Min,
                    HistogramAggregate.Max,
                    HistogramAggregate.Avg
                },
                Percentiles = new[] { 0.50, 0.95, 0.99 }
            };
            var queryTime = 10 + _random.NextDouble() * 100;
            _metrics.Histogram("database.query.time.ms", queryTime,
                new[] { "query:select", "table:users" },
                null,
                customHistogramOptions);
            Console.WriteLine($"Histogram: database.query.time.ms = {queryTime:F2}ms");

            // ═══════════════════════════════════════════════════════════
            // 4. DISTRIBUTION - Server-side aggregation (all raw values)
            // ═══════════════════════════════════════════════════════════
            var requestSize = _random.Next(100, 10000);
            _metrics.Distribution("request.size.bytes", requestSize,
                new[] { "content_type:json" });
            Console.WriteLine($"Distribution: request.size.bytes = {requestSize} bytes");

            // ═══════════════════════════════════════════════════════════
            // 5. SYSTEM.DIAGNOSTICS.METRICS Integration
            // ═══════════════════════════════════════════════════════════
            // These metrics are automatically captured and sent to Datadog
            _requestCounter.Add(1,
                new KeyValuePair<string, object?>("status", "success"),
                new KeyValuePair<string, object?>("method", "GET"));

            var duration = 50 + _random.NextDouble() * 150;
            _requestDuration.Record(duration,
                new KeyValuePair<string, object?>("endpoint", "/api/metrics"));

            _requestCount++;
            Console.WriteLine($"System.Diagnostics.Metrics: Recorded request #{_requestCount}");

            // ═══════════════════════════════════════════════════════════
            // Separator
            // ═══════════════════════════════════════════════════════════
            Console.WriteLine();

            // Wait before next iteration
            // Note: Metrics are automatically flushed every FlushIntervalSeconds
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }

        _logger.LogInformation("MetricsSampleWorker stopped");
    }

    public override void Dispose()
    {
        _meter?.Dispose();
        base.Dispose();
    }
}
