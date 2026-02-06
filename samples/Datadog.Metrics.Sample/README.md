# Datadog.Metrics Sample Application

This sample demonstrates all features of the Datadog.Metrics library with secure API key configuration and real-time console output.

## Features Demonstrated

- ✅ **All metric types**: Gauge, Counter, Histogram, Distribution
- ✅ **Secure API key loading**: Environment variables, User Secrets, configuration files
- ✅ **System.Diagnostics.Metrics integration**: Automatic metric capture
- ✅ **Custom tags**: Per-metric and default tags
- ✅ **Background reporting**: Automatic flushing at regular intervals
- ✅ **Console output**: See metrics being recorded in real-time

## Running the Sample

### Option 1: Environment Variable (Recommended for Local Development)

```bash
# Set API key
export DD_API_KEY=your-datadog-api-key

# Run the sample
cd dotnet
make run-sample

# Or without make:
dotnet run --project samples/Datadog.Metrics.Sample
```

### Option 2: User Secrets (Recommended for Development)

```bash
cd samples/Datadog.Metrics.Sample

# Set your API key securely
dotnet user-secrets set "DatadogMetrics:ApiKey" "your-datadog-api-key"

# Optionally, override other settings
dotnet user-secrets set "DatadogMetrics:Site" "datadoghq.eu"
dotnet user-secrets set "DatadogMetrics:Prefix" "myapp."

# Run the sample
dotnet run
```

### Option 3: Configuration File (Not Recommended for Production)

Edit `appsettings.json` and add your API key:

```json
{
  "DatadogMetrics": {
    "ApiKey": "your-datadog-api-key"
  }
}
```

**⚠️ Warning**: Never commit API keys to source control! Add `appsettings.json` to `.gitignore` if you use this method.

## Configuration Priority

The sample loads configuration in this order (later sources override earlier ones):

1. `appsettings.json` - Base configuration
2. Environment variables prefixed with `DD_` - e.g., `DD_API_KEY`
3. User Secrets - For development only

## What You'll See

When you run the sample, you'll see console output like this:

```
╔══════════════════════════════════════════════════════╗
║          Datadog.Metrics Sample Application          ║
╚══════════════════════════════════════════════════════╝

✓ API Key loaded: dd123456... (showing first 8 characters)
✓ Target Site: datadoghq.com

Datadog Metrics Configuration:
   • Flush Interval: 5s
   • Prefix: sample_app.
   • Default Tags: env:development, service:datadog-metrics-sample, version:1.0.0
   • Max Retries: 3

Starting application...
   Press Ctrl+C to stop

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Iteration 1 - Recording metrics...
   Gauge: temperature.celsius = 70.23°C
   Gauge: memory.usage.percent = 65.45%
   Counter: errors.total += 2
   Counter: requests.total += 1
   Histogram: response.time.ms = 145.67ms
   Histogram: database.query.time.ms = 45.23ms
   Distribution: request.size.bytes = 5432 bytes
   System.Diagnostics.Metrics: Recorded request #1
```

Every 5 seconds (configurable via `FlushIntervalSeconds`), the library automatically sends buffered metrics to Datadog. You'll see debug logs from the reporter:

```
[14:23:45 DBG] Flushing 8 metric series to Datadog
[14:23:45 DBG] Submitting metrics to https://api.datadoghq.com/api/v2/series (attempt 1/4)
[14:23:45 DBG] Successfully submitted metrics to https://api.datadoghq.com/api/v2/series
[14:23:45 DBG] Successfully flushed 8 metric series
```

## Metrics Sent to Datadog

The sample sends these metrics (all prefixed with `sample_app.`):

### Gauges (Latest Value)

- `sample_app.temperature.celsius` - Simulated temperature readings
- `sample_app.memory.usage.percent` - Simulated memory usage
- `sample.cpu.usage` - Observable gauge from System.Diagnostics.Metrics

### Counters (Accumulated)

- `sample_app.errors.total` - Error count (tags: `type`, `severity`)
- `sample_app.requests.total` - Request count (tags: `endpoint`, `method`, `status`)
- `sample.requests.total` - From System.Diagnostics.Metrics (tags: `status`, `method`)

### Histograms (Client-side Aggregation)

- `sample_app.response.time.ms.{aggregate}` - Response time statistics
  - `.max`, `.median`, `.avg`, `.count`, `.95percentile`, `.99percentile`
- `sample_app.database.query.time.ms.{aggregate}` - Query time statistics
  - `.min`, `.max`, `.avg`, `.50percentile`, `.95percentile`, `.99percentile`
- `sample.request.duration.{aggregate}` - From System.Diagnostics.Metrics

### Distributions (Server-side Aggregation)

- `sample_app.request.size.bytes` - Request payload sizes (raw values sent to Datadog)

## Viewing Metrics in Datadog

After running the sample for a minute or two:

1. Go to **Metrics → Explorer** in your Datadog dashboard
2. Search for metrics starting with `sample_app.`
3. Create visualizations:
   - **Timeseries**: `sample_app.temperature.celsius` - See temperature over time
   - **Query Value**: `sample_app.requests.total` - Total request count
   - **Heatmap**: `sample_app.request.size.bytes` - Distribution of request sizes

## Customizing the Sample

### Change Flush Interval

Edit `appsettings.json`:

```json
{
  "DatadogMetrics": {
    "FlushIntervalSeconds": 10
  }
}
```

Or use environment variable:

```bash
export DD_FLUSHINTERVALSECONDS=10
```

### Change Datadog Site

For EU datacenter:

```bash
export DD_SITE=datadoghq.eu
```

For US3:

```bash
export DD_SITE=us3.datadoghq.com
```

### Add Custom Tags

Edit `appsettings.json`:

```json
{
  "DatadogMetrics": {
    "DefaultTags": ["env:production", "team:platform", "region:us-east-1"]
  }
}
```

### Change Metric Prefix

```bash
export DD_PREFIX=mycompany.myapp.
```

## Code Examples

### Recording a Gauge

```csharp
// Latest value wins
metrics.Gauge("cpu.usage", 45.2, new[] { "core:1" });
metrics.Gauge("cpu.usage", 52.1, new[] { "core:1" });
// Result: cpu.usage = 52.1
```

### Recording a Counter

```csharp
// Values accumulate
metrics.Counter("requests", 5);
metrics.Counter("requests", 3);
// Result: requests = 8

// Or use Increment for +1
metrics.Increment("page_views");
```

### Recording a Histogram

```csharp
// Configure aggregates and percentiles
var options = new HistogramOptions
{
    Aggregates = new[] { HistogramAggregate.Min, HistogramAggregate.Max, HistogramAggregate.P95 },
    Percentiles = new[] { 0.95, 0.99 }
};

metrics.Histogram("api.latency", 123.45, new[] { "endpoint:/users" }, null, options);
```

### Recording a Distribution

```csharp
// All raw values sent to Datadog for server-side aggregation
metrics.Distribution("payment.amount", 49.99, new[] { "currency:USD" });
```

### Using System.Diagnostics.Metrics

```csharp
// Create a meter and instruments
var meter = new Meter("MyApp", "1.0.0");
var counter = meter.CreateCounter<int>("operations.count");
var histogram = meter.CreateHistogram<double>("operation.duration");

// Record values - automatically captured by Datadog.Metrics
counter.Add(1, new KeyValuePair<string, object?>("operation", "save"));
histogram.Record(45.2, new KeyValuePair<string, object?>("operation", "load"));
```

## Troubleshooting

### No metrics appearing in Datadog

1. **Check API key**: Ensure it's valid and has the correct permissions
2. **Check site**: Verify you're using the correct Datadog site (`.com`, `.eu`, etc.)
3. **Wait 1-2 minutes**: Metrics are buffered and may take time to appear
4. **Enable debug logging**: Set log level to `Debug` in `appsettings.json`

### API key errors

```
Error: Datadog API key not found. Please provide it via:
  1. Environment variable: DD_API_KEY=your-key
  2. User secrets: dotnet user-secrets set "DatadogMetrics:ApiKey" "your-key"
  3. appsettings.json (not recommended for production)
```

**Solution**: Follow one of the methods above to set your API key.

### Connection errors

If you see HTTP errors:

1. **Check network**: Ensure you can reach `api.datadoghq.com`
2. **Check firewall**: Verify outbound HTTPS is allowed
3. **Check proxy**: If behind a proxy, configure HTTP client accordingly

## Production Deployment

For production, use one of these secure methods:

### Azure App Service

```bash
az webapp config appsettings set \
  --name myapp \
  --resource-group mygroup \
  --settings DD_API_KEY=your-key DD_SITE=datadoghq.com
```

### Kubernetes Secret

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: datadog-api-key
type: Opaque
stringData:
  api-key: your-datadog-api-key
---
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      containers:
        - name: myapp
          env:
            - name: DD_API_KEY
              valueFrom:
                secretKeyRef:
                  name: datadog-api-key
                  key: api-key
```

### AWS ECS/Fargate

Use AWS Secrets Manager or Parameter Store:

```json
{
  "containerDefinitions": [
    {
      "secrets": [
        {
          "name": "DD_API_KEY",
          "valueFrom": "arn:aws:secretsmanager:region:account:secret:datadog-api-key"
        }
      ]
    }
  ]
}
```

## Learn More

- [Main README](../../README.md) - Full library documentation
- [Datadog Metrics API](https://docs.datadoghq.com/api/latest/metrics/) - API reference
- [System.Diagnostics.Metrics](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics) - .NET metrics instrumentation
