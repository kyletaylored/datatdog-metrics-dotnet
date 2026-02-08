using Datadog.Metrics.Configuration;
using Datadog.Metrics.Core;
using Datadog.Metrics.Metrics;
using Datadog.Metrics.Transport.Models;

namespace Datadog.Metrics.Tests.Core;

public class MetricsAggregatorTests
{
    [Fact]
    public void AddPoint_WithSortedTags_CreatesConsistentBufferKey()
    {
        // Arrange
        var aggregator = new MetricsAggregator("testhost", null, Array.Empty<string>(), 1000);

        // Act - Add same metric with tags in different order
        aggregator.AddPoint(MetricType.Gauge, "test.metric", 10, new[] { "tag:a", "tag:b" }, DateTimeOffset.UtcNow);
        aggregator.AddPoint(MetricType.Gauge, "test.metric", 20, new[] { "tag:b", "tag:a" }, DateTimeOffset.UtcNow);

        var result = aggregator.Flush();

        // Assert - Should have only one metric (tags were sorted and deduplicated)
        Assert.Single(result);
        Assert.Equal(20, result[0].Points[0].Value); // Latest value wins for gauge
    }

    [Fact]
    public void AddPoint_WithDifferentTags_CreatesSeparateMetrics()
    {
        // Arrange
        var aggregator = new MetricsAggregator("testhost", null, Array.Empty<string>(), 1000);

        // Act
        aggregator.AddPoint(MetricType.Gauge, "test.metric", 10, new[] { "env:prod" }, DateTimeOffset.UtcNow);
        aggregator.AddPoint(MetricType.Gauge, "test.metric", 20, new[] { "env:dev" }, DateTimeOffset.UtcNow);

        var result = aggregator.Flush();

        // Assert - Should have two separate metrics
        Assert.Equal(2, result.Count);
        Assert.Contains(result, m => m.Points[0].Value == 10);
        Assert.Contains(result, m => m.Points[0].Value == 20);
    }

    [Fact]
    public void AddPoint_WithNullTags_TreatsSameAsEmptyTags()
    {
        // Arrange
        var aggregator = new MetricsAggregator("testhost", null, Array.Empty<string>(), 1000);

        // Act
        aggregator.AddPoint(MetricType.Gauge, "test.metric", 10, null, DateTimeOffset.UtcNow);
        aggregator.AddPoint(MetricType.Gauge, "test.metric", 20, Array.Empty<string>(), DateTimeOffset.UtcNow);

        var result = aggregator.Flush();

        // Assert - Should be same metric
        Assert.Single(result);
        Assert.Equal(20, result[0].Points[0].Value);
    }

    [Fact]
    public void AddPoint_WithPrefix_PrependsToMetricName()
    {
        // Arrange
        var aggregator = new MetricsAggregator("testhost", "myapp.", Array.Empty<string>(), 1000);

        // Act
        aggregator.AddPoint(MetricType.Gauge, "memory.used", 100, null, DateTimeOffset.UtcNow);
        var result = aggregator.Flush();

        // Assert
        Assert.Single(result);
        Assert.Equal("myapp.memory.used", result[0].Metric);
    }

    [Fact]
    public void Flush_AppliesDefaultTags()
    {
        // Arrange
        var defaultTags = new[] { "env:prod", "service:api" };
        var aggregator = new MetricsAggregator("testhost", null, defaultTags, 1000);

        // Act
        aggregator.AddPoint(MetricType.Gauge, "test.metric", 100, new[] { "custom:tag" }, DateTimeOffset.UtcNow);
        var result = aggregator.Flush();

        // Assert
        Assert.Single(result);
        Assert.NotNull(result[0].Tags);
        var tags = result[0].Tags!;
        Assert.Equal(3, tags.Length);
        Assert.Contains("env:prod", tags);
        Assert.Contains("service:api", tags);
        Assert.Contains("custom:tag", tags);
    }

    [Fact]
    public void Flush_ClearsBuffer()
    {
        // Arrange
        var aggregator = new MetricsAggregator("testhost", null, Array.Empty<string>(), 1000);

        // Act
        aggregator.AddPoint(MetricType.Gauge, "test.metric", 100, null, DateTimeOffset.UtcNow);
        var result1 = aggregator.Flush();
        var result2 = aggregator.Flush();

        // Assert
        Assert.Single(result1);
        Assert.Empty(result2); // Buffer was cleared
    }

    [Fact]
    public void AddPoint_CounterAccumulates()
    {
        // Arrange
        var aggregator = new MetricsAggregator("testhost", null, Array.Empty<string>(), 1000);

        // Act
        aggregator.AddPoint(MetricType.Counter, "test.counter", 5, null, DateTimeOffset.UtcNow);
        aggregator.AddPoint(MetricType.Counter, "test.counter", 3, null, DateTimeOffset.UtcNow);
        var result = aggregator.Flush();

        // Assert
        Assert.Single(result);
        Assert.Equal(8, result[0].Points[0].Value);
    }

    [Fact]
    public void AddPoint_Histogram_GeneratesMultipleMetrics()
    {
        // Arrange
        var aggregator = new MetricsAggregator("testhost", null, Array.Empty<string>(), 1000);
        var histogramOptions = new HistogramOptions
        {
            Aggregates = new[] { HistogramAggregate.Min, HistogramAggregate.Max, HistogramAggregate.Count },
            Percentiles = Array.Empty<double>()
        };

        // Act
        aggregator.AddPoint(MetricType.Histogram, "test.histogram", 1.0, null, DateTimeOffset.UtcNow, histogramOptions);
        aggregator.AddPoint(MetricType.Histogram, "test.histogram", 5.0, null, DateTimeOffset.UtcNow, histogramOptions);
        aggregator.AddPoint(MetricType.Histogram, "test.histogram", 3.0, null, DateTimeOffset.UtcNow, histogramOptions);
        var result = aggregator.Flush();

        // Assert - min, max, count
        Assert.Equal(3, result.Count);
        Assert.Contains(result, m => m.Metric == "test.histogram.min" && m.Points[0].Value == 1.0);
        Assert.Contains(result, m => m.Metric == "test.histogram.max" && m.Points[0].Value == 5.0);
        Assert.Contains(result, m => m.Metric == "test.histogram.count" && m.Points[0].Value == 3);
    }

    [Fact]
    public void AddPoint_Distribution_MarksAsDistribution()
    {
        // Arrange
        var aggregator = new MetricsAggregator("testhost", null, Array.Empty<string>(), 1000);

        // Act
        aggregator.AddPoint(MetricType.Distribution, "test.distribution", 42.5, new[] { "env:test" }, DateTimeOffset.UtcNow);
        var result = aggregator.Flush();

        // Assert
        Assert.Single(result);
        Assert.Equal("test.distribution", result[0].Metric);
        Assert.True(result[0].IsDistribution);

        // Distribution points are DistributionPoint objects with Values array
        var distPoint = Assert.IsType<DistributionPoint>(result[0].Points[0]);
        Assert.Single(distPoint.Values);
        Assert.Equal(42.5, distPoint.Values[0]);
    }

    [Fact]
    public void AddPoint_Distribution_AccumulatesMultipleValues()
    {
        // Arrange
        var aggregator = new MetricsAggregator("testhost", null, Array.Empty<string>(), 1000);
        var timestamp = DateTimeOffset.UtcNow;

        // Act - Add multiple distribution values at same timestamp (they'll be grouped)
        aggregator.AddPoint(MetricType.Distribution, "request.size", 100, null, timestamp);
        aggregator.AddPoint(MetricType.Distribution, "request.size", 250, null, timestamp);
        aggregator.AddPoint(MetricType.Distribution, "request.size", 500, null, timestamp);
        var result = aggregator.Flush();

        // Assert - Distributions group values by timestamp into a single DistributionPoint
        Assert.Single(result);
        Assert.Single(result[0].Points); // One point containing all values
        Assert.True(result[0].IsDistribution);

        var distPoint = Assert.IsType<DistributionPoint>(result[0].Points[0]);
        Assert.Equal(3, distPoint.Values.Length);
        Assert.Contains(100, distPoint.Values);
        Assert.Contains(250, distPoint.Values);
        Assert.Contains(500, distPoint.Values);
    }
}
