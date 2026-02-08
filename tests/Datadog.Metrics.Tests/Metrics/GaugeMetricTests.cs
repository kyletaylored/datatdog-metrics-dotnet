using Datadog.Metrics.Metrics;

namespace Datadog.Metrics.Tests.Metrics;

public class GaugeMetricTests
{
    [Fact]
    public void Gauge_LatestValueWins()
    {
        // Arrange
        var gauge = new GaugeMetric("test.metric", null, "testhost");
        var timestamp1 = DateTimeOffset.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(5);

        // Act
        gauge.AddPoint(42, timestamp1);
        gauge.AddPoint(100, timestamp2);
        var result = gauge.Flush();

        // Assert
        Assert.Single(result);
        Assert.Equal("test.metric", result[0].Metric);
        Assert.Equal(100, result[0].Points[0].Value);
        Assert.Equal(timestamp2.ToUnixTimeSeconds(), result[0].Points[0].Timestamp);
    }

    [Fact]
    public void Gauge_WithTags_IncludesTagsInResult()
    {
        // Arrange
        var tags = new[] { "env:prod", "service:api" };
        var gauge = new GaugeMetric("test.metric", tags, "testhost");

        // Act
        gauge.AddPoint(42, DateTimeOffset.UtcNow);
        var result = gauge.Flush();

        // Assert
        Assert.Single(result);
        Assert.Equal(tags, result[0].Tags);
    }

    [Fact]
    public void Gauge_IncludesHostInResources()
    {
        // Arrange
        var gauge = new GaugeMetric("test.metric", null, "myhost");

        // Act
        gauge.AddPoint(42, DateTimeOffset.UtcNow);
        var result = gauge.Flush();

        // Assert
        Assert.Single(result);
        Assert.NotNull(result[0].Resources);
        var resources = result[0].Resources!;
        Assert.Single(resources);
        Assert.Equal("myhost", resources[0].Name);
        Assert.Equal("host", resources[0].Type);
    }

    [Fact]
    public void Gauge_TypeIsZero()
    {
        // Arrange
        var gauge = new GaugeMetric("test.metric", null, "testhost");

        // Act
        gauge.AddPoint(42, DateTimeOffset.UtcNow);
        var result = gauge.Flush();

        // Assert
        Assert.Equal(0, result[0].Type); // Gauge = 0
    }
}
