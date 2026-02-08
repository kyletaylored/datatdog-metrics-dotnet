using System.Net;
using System.Text;
using System.Text.Json;
using Datadog.Metrics.Configuration;
using Datadog.Metrics.Core;
using Datadog.Metrics.Transport;
using Datadog.Metrics.Transport.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Datadog.Metrics.Tests.Transport;

public class HttpApiTests
{
    [Fact]
    public async Task SubmitMetricsAsync_WithDistributions_UsesCorrectTupleFormat()
    {
        // Arrange
        var options = Options.Create(new DatadogMetricsOptions
        {
            ApiKey = "test-key",
            Site = "datadoghq.com"
        });

        string? capturedRequestBody = null;
        var mockHttpClient = CreateMockHttpClient((request, body) =>
        {
            if (request.RequestUri?.AbsolutePath.Contains("/distribution_points") == true)
            {
                capturedRequestBody = body;
            }
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        var httpApi = new HttpApi(options, NullLogger<HttpApi>.Instance, mockHttpClient);

        var metrics = new List<SeriesMetric>
        {
            new SeriesMetric
            {
                Metric = "test.distribution",
                Points = new[]
                {
                    new MetricPoint { Timestamp = 1234567890L, Value = 42.5 }
                },
                Tags = new[] { "env:test" },
                IsDistribution = true
            }
        };

        // Act
        var result = await httpApi.SubmitMetricsAsync(metrics);

        // Assert
        Assert.True(result);
        Assert.NotNull(capturedRequestBody);

        // Verify the payload structure matches expected tuple format
        var doc = JsonDocument.Parse(capturedRequestBody);
        var series = doc.RootElement.GetProperty("series");
        Assert.True(series.GetArrayLength() > 0);

        var firstMetric = series[0];
        Assert.Equal("test.distribution", firstMetric.GetProperty("metric").GetString());
        Assert.Equal("distribution", firstMetric.GetProperty("type").GetString());

        // Verify points are in tuple format: [[timestamp, [values]]]
        var points = firstMetric.GetProperty("points");
        Assert.True(points.GetArrayLength() > 0);

        var firstPoint = points[0];
        Assert.Equal(JsonValueKind.Array, firstPoint.ValueKind);
        Assert.Equal(2, firstPoint.GetArrayLength()); // [timestamp, [values]]

        // First element should be timestamp
        Assert.Equal(1234567890, firstPoint[0].GetInt64());

        // Second element should be array of values
        Assert.Equal(JsonValueKind.Array, firstPoint[1].ValueKind);
        Assert.Single(firstPoint[1].EnumerateArray());
        Assert.Equal(42.5, firstPoint[1][0].GetDouble());
    }

    [Fact]
    public async Task SubmitMetricsAsync_WithRegularMetrics_UsesV2Endpoint()
    {
        // Arrange
        var options = Options.Create(new DatadogMetricsOptions
        {
            ApiKey = "test-key",
            Site = "us3.datadoghq.com"
        });

        string? capturedUrl = null;
        var mockHttpClient = CreateMockHttpClient((request, body) =>
        {
            capturedUrl = request.RequestUri?.ToString();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        var httpApi = new HttpApi(options, NullLogger<HttpApi>.Instance, mockHttpClient);

        var metrics = new List<SeriesMetric>
        {
            new SeriesMetric
            {
                Metric = "test.gauge",
                Points = new[] { new MetricPoint { Timestamp = 1234567890L, Value = 100 } },
                Type = 0, // gauge
                IsDistribution = false
            }
        };

        // Act
        var result = await httpApi.SubmitMetricsAsync(metrics);

        // Assert
        Assert.True(result);
        Assert.NotNull(capturedUrl);
        Assert.Contains("https://api.us3.datadoghq.com/api/v2/series", capturedUrl);
    }

    [Fact]
    public async Task SubmitMetricsAsync_WithMixedMetrics_SubmitsToBothEndpoints()
    {
        // Arrange
        var options = Options.Create(new DatadogMetricsOptions
        {
            ApiKey = "test-key",
            Site = "datadoghq.com"
        });

        var capturedUrls = new List<string>();
        var mockHttpClient = CreateMockHttpClient((request, body) =>
        {
            capturedUrls.Add(request.RequestUri?.ToString() ?? "");
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        var httpApi = new HttpApi(options, NullLogger<HttpApi>.Instance, mockHttpClient);

        var metrics = new List<SeriesMetric>
        {
            new SeriesMetric
            {
                Metric = "test.distribution",
                Points = new[] { new MetricPoint { Timestamp = 1234567890L, Value = 42 } },
                IsDistribution = true
            },
            new SeriesMetric
            {
                Metric = "test.gauge",
                Points = new[] { new MetricPoint { Timestamp = 1234567890L, Value = 100 } },
                Type = 0, // gauge
                IsDistribution = false
            }
        };

        // Act
        var result = await httpApi.SubmitMetricsAsync(metrics);

        // Assert
        Assert.True(result);
        Assert.Equal(2, capturedUrls.Count);
        Assert.Contains(capturedUrls, url => url.Contains("/distribution_points"));
        Assert.Contains(capturedUrls, url => url.Contains("/api/v2/series"));
    }

    [Fact]
    public async Task SubmitMetricsAsync_WithEmptyList_ReturnsTrue()
    {
        // Arrange
        var options = Options.Create(new DatadogMetricsOptions
        {
            ApiKey = "test-key",
            Site = "datadoghq.com"
        });

        var mockHttpClient = CreateMockHttpClient((request, body) =>
        {
            Assert.Fail("Should not make HTTP request for empty metrics list");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var httpApi = new HttpApi(options, NullLogger<HttpApi>.Instance, mockHttpClient);

        // Act
        var result = await httpApi.SubmitMetricsAsync(new List<SeriesMetric>());

        // Assert
        Assert.True(result);
    }

    private static HttpClient CreateMockHttpClient(Func<HttpRequestMessage, string, HttpResponseMessage> handler)
    {
        var mockHandler = new MockHttpMessageHandler(async (request, cancellationToken) =>
        {
            var body = "";
            if (request.Content != null)
            {
                body = await request.Content.ReadAsStringAsync();
            }
            return handler(request, body);
        });

        return new HttpClient(mockHandler);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
