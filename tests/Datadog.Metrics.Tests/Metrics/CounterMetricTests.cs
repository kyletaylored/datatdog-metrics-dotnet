using Datadog.Metrics.Metrics;

namespace Datadog.Metrics.Tests.Metrics;

public class CounterMetricTests
{
    [Fact]
    public void Counter_AccumulatesValues()
    {
        // Arrange
        var counter = new CounterMetric("test.counter", null, "testhost");

        // Act
        counter.AddPoint(5, DateTimeOffset.UtcNow);
        counter.AddPoint(3, DateTimeOffset.UtcNow);
        counter.AddPoint(2, DateTimeOffset.UtcNow);
        var result = counter.Flush();

        // Assert
        Assert.Single(result);
        Assert.Equal("test.counter", result[0].Metric);
        Assert.Equal(10, result[0].Points[0].Value);
    }

    [Fact]
    public void Counter_TypeIsCount()
    {
        // Arrange
        var counter = new CounterMetric("test.counter", null, "testhost");

        // Act
        counter.AddPoint(1, DateTimeOffset.UtcNow);
        var result = counter.Flush();

        // Assert
        Assert.Equal(2, result[0].Type); // Count = 2
    }

    [Fact]
    public void Counter_UpdatesTimestampWithEachPoint()
    {
        // Arrange
        var counter = new CounterMetric("test.counter", null, "testhost");
        var timestamp1 = DateTimeOffset.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(10);

        // Act
        counter.AddPoint(1, timestamp1);
        counter.AddPoint(1, timestamp2);
        var result = counter.Flush();

        // Assert
        Assert.Equal(timestamp2.ToUnixTimeSeconds(), result[0].Points[0].Timestamp);
    }
}
