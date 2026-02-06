# Datadog.Metrics

A .NET library for buffered metrics reporting to Datadog HTTP API with System.Diagnostics.Metrics integration.

## Features

- **Buffered metrics aggregation** - Metrics are buffered and flushed at regular intervals
- **Multiple metric types** - Gauge, Counter, Histogram, and Distribution support
- **Client-side aggregation** - Histograms compute percentiles locally to reduce API calls
- **Server-side aggregation** - Distributions send raw values for server-side calculation
- **Tag sorting** - Consistent buffer keys regardless of tag order
- **Retry logic** - Exponential backoff for transient failures
- **Dual API support** - Uses Datadog API v2 for regular metrics, v1 for distributions
- **System.Diagnostics.Metrics integration** - Automatically captures standard .NET metrics
- **ASP.NET Core integration** - Easy setup with dependency injection
- **Multi-framework support** - Targets .NET 10.0, 9.0, 8.0, 6.0, and .NET Standard 2.0

## Installation

```bash
dotnet add package Datadog.Metrics
```

## Quick Start

### 1. Basic Usage with Dependency Injection

```csharp
using Datadog.Metrics.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register Datadog metrics
builder.Services.AddDatadogMetrics(options =>
{
    options.ApiKey = "your-api-key";
    options.Site = "datadoghq.com";
    options.Prefix = "myapp.";
    options.DefaultTags = new[] { "env:production", "service:api" };
    options.FlushIntervalSeconds = 10;
});

var app = builder.Build();

// Inject and use DatadogMetricsLogger
app.MapGet("/", (DatadogMetricsLogger metrics) =>
{
    metrics.Counter("requests", 1, new[] { "endpoint:/" });
    metrics.Gauge("active_connections", 42);
    return "Hello World!";
});

app.Run();
```

### 2. With System.Diagnostics.Metrics Integration

```csharp
// Enable automatic capture of System.Diagnostics.Metrics
builder.Services.AddDatadogMetricsWithDiagnostics(options =>
{
    options.ApiKey = "your-api-key";
    options.Site = "datadoghq.com";
});

// Now any code using System.Diagnostics.Metrics will automatically send to Datadog
var meter = new Meter("MyApp");
var counter = meter.CreateCounter<int>("requests");

counter.Add(1, new KeyValuePair<string, object?>("endpoint", "/api"));
```

## Metric Types

### Gauge
Records a point-in-time value. Latest value wins.

```csharp
metrics.Gauge("temperature", 72.5, new[] { "location:office" });
```

### Counter
Accumulates values over time.

```csharp
metrics.Counter("errors", 1, new[] { "type:validation" });
metrics.Increment("requests"); // Shorthand for Counter(..., 1)
```

### Histogram
Client-side aggregation with configurable percentiles.

```csharp
metrics.Histogram("response_time", 145.2, new[] { "endpoint:/api" });

// Custom histogram options
var options = new HistogramOptions
{
    Aggregates = new[] { HistogramAggregate.Min, HistogramAggregate.Max, HistogramAggregate.Avg, HistogramAggregate.P95 },
    Percentiles = new[] { 0.95, 0.99 }
};
metrics.Histogram("latency", 250, null, null, options);
```

### Distribution
Server-side aggregation. Sends all raw values to Datadog.

```csharp
metrics.Distribution("request_size", 1024, new[] { "method:POST" });
```

## Configuration Options

```csharp
services.AddDatadogMetrics(options =>
{
    // Required
    options.ApiKey = "your-api-key";

    // Optional (with defaults)
    options.Site = "datadoghq.com"; // or "datadoghq.eu", "us3.datadoghq.com", etc.
    options.Host = Environment.MachineName; // Hostname for metrics
    options.Prefix = null; // Prefix for all metric names
    options.DefaultTags = Array.Empty<string>(); // Applied to all metrics

    // Flush settings
    options.FlushIntervalSeconds = 10; // How often to send metrics
    options.MaxBufferSize = 1000; // Warning threshold for buffer size

    // HTTP settings
    options.HttpTimeoutSeconds = 10;
    options.MaxRetries = 3;
    options.RetryBackoffSeconds = 1; // Exponential backoff base

    // Histogram defaults
    options.Histogram = new HistogramOptions
    {
        Aggregates = new[] { HistogramAggregate.Max, HistogramAggregate.Median, HistogramAggregate.Avg, HistogramAggregate.Count },
        Percentiles = new[] { 0.95 }
    };
});
```

## Architecture

### Component Overview

- **DatadogMetricsLogger** - Main API for recording metrics
- **MetricsAggregator** - Buffers and aggregates metrics by unique key+tags combination
- **DatadogReporter** - Background service that flushes metrics at regular intervals
- **HttpApi** - Handles communication with Datadog API (v1 and v2) with retry logic
- **DatadogMeterListener** - Integrates with System.Diagnostics.Metrics
- **Metric Types** - GaugeMetric, CounterMetric, HistogramMetric, DistributionMetric

### Buffer Key Generation

Metrics with the same name and tags (regardless of order) share the same buffer entry:

```csharp
// These create the SAME buffer entry
metrics.Gauge("cpu", 50, new[] { "host:web1", "region:us-east" });
metrics.Gauge("cpu", 55, new[] { "region:us-east", "host:web1" });
// Result: Single gauge with value 55 (latest wins)
```

### API Routing

- **Regular metrics** (Gauge, Counter, Histogram) → `/api/v2/series`
- **Distributions** → `/api/v1/distribution_points`

## Testing

```bash
dotnet test
```

Current test coverage includes:
- Buffer key generation with tag sorting
- Metric type behaviors (gauge, counter, histogram)
- Default tags and prefixes
- Aggregator flush behavior

## Advanced Usage

### Manual Flush

Metrics are automatically flushed at regular intervals, but you can force a flush:

```csharp
var flushedMetrics = metrics.Flush();
```

### Custom Tags Per Metric

```csharp
metrics.Counter("api.requests", 1, new[] { "endpoint:/users", "method:GET", "status:200" });
```

### Histogram Percentiles

Configure which percentiles to calculate:

```csharp
services.AddDatadogMetrics(options =>
{
    options.Histogram.Percentiles = new[] { 0.50, 0.75, 0.95, 0.99 };
    options.Histogram.Aggregates = new[]
    {
        HistogramAggregate.Min,
        HistogramAggregate.Max,
        HistogramAggregate.Avg,
        HistogramAggregate.Count
    };
});
```

## Differences from Node.js Version

This .NET port maintains feature parity with the Node.js `node-datadog-metrics` package:

- ✅ Same buffering and aggregation behavior
- ✅ Same tag sorting algorithm for consistent buffer keys
- ✅ Same flush interval and retry logic
- ✅ Dual API endpoint support (v1 for distributions, v2 for series)
- ➕ **Bonus**: Native System.Diagnostics.Metrics integration
- ➕ **Bonus**: Built-in ASP.NET Core dependency injection

## Development

### Quick Start for Contributors

```bash
# Clone the repository
git clone https://github.com/dbader/node-datadog-metrics.git
cd node-datadog-metrics/dotnet

# Initialize development environment
make init

# Build the solution
make build

# Run tests
make test

# Run the sample app (requires DD_API_KEY env var)
export DD_API_KEY=your-api-key
make run-sample
```

### Available Make Commands

```bash
make help              # Show all available commands
make build             # Build the solution
make test              # Run all tests
make test-watch        # Run tests in watch mode
make pack              # Create NuGet package
make format            # Format code
make lint              # Run code analysis
make run-sample        # Run sample application
make clean             # Clean build artifacts
make ci                # Run full CI pipeline locally
```

### Project Structure

```
dotnet/
├── .github/               # GitHub Actions workflows and issue templates
├── samples/               # Sample applications
│   └── Datadog.Metrics.Sample/
├── src/                   # Main library source
│   └── Datadog.Metrics/
│       ├── Configuration/  # Configuration models
│       ├── Core/          # Aggregation logic
│       ├── Extensions/    # DI extensions
│       ├── Integration/   # System.Diagnostics.Metrics
│       ├── Metrics/       # Metric implementations
│       └── Transport/     # HTTP API client
├── tests/                 # Unit tests
│   └── Datadog.Metrics.Tests/
├── .editorconfig         # Code style configuration
├── .gitignore            # Git ignore rules
├── .gitattributes        # Git line ending settings
├── Makefile              # Development automation
├── CONTRIBUTING.md       # Contribution guidelines
└── README.md             # This file
```

## License

MIT - See [LICENSE](LICENSE) for details

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Quick Contribution Checklist

- [ ] Fork the repository
- [ ] Create a feature branch
- [ ] Write tests for your changes
- [ ] Ensure all tests pass: `make test`
- [ ] Format your code: `make format`
- [ ] Update documentation as needed
- [ ] Submit a pull request

## CI/CD

The project uses GitHub Actions for continuous integration:

- **CI Pipeline** ([`.github/workflows/ci.yml`](.github/workflows/ci.yml))
  - Runs on: push to main/develop, pull requests
  - Matrix testing across: Ubuntu, Windows, macOS
  - Tests on: .NET 6.0 and .NET 8.0
  - Includes code formatting and quality checks

- **Publish Pipeline** ([`.github/workflows/publish.yml`](.github/workflows/publish.yml))
  - Triggered by: GitHub releases
  - Automatically builds and publishes to NuGet.org

## Support

- 📖 **Documentation**: [samples/Datadog.Metrics.Sample/README.md](samples/Datadog.Metrics.Sample/README.md)
- 🐛 **Bug Reports**: [GitHub Issues](https://github.com/dbader/node-datadog-metrics/issues)
- 💡 **Feature Requests**: [GitHub Issues](https://github.com/dbader/node-datadog-metrics/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/dbader/node-datadog-metrics/discussions)
