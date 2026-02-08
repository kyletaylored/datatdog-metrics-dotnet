using System.Text;
using System.Text.Json;
using Datadog.Metrics.Configuration;
using Datadog.Metrics.Transport.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Datadog.Metrics.Transport;

/// <summary>
/// HTTP API client for submitting metrics to Datadog.
/// Handles both v1 (distributions) and v2 (series) endpoints with retry logic.
/// </summary>
internal sealed class HttpApi : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly DatadogMetricsOptions _options;
    private readonly ILogger<HttpApi> _logger;
    private readonly string _v1DistributionUrl;
    private readonly string _v2SeriesUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpApi"/> class.
    /// </summary>
    /// <param name="options">Datadog metrics options.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="httpClient">Optional HTTP client (for testing).</param>
    public HttpApi(
        IOptions<DatadogMetricsOptions> options,
        ILogger<HttpApi> logger,
        HttpClient? httpClient = null)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient ?? CreateHttpClient();

        // Build API URLs based on site
        var baseUrl = $"https://api.{_options.Site}";
        _v1DistributionUrl = $"{baseUrl}/api/v1/distribution_points";
        _v2SeriesUrl = $"{baseUrl}/api/v2/series";
    }

    /// <summary>
    /// Submits metrics to Datadog API with retry logic.
    /// Routes distributions to v1 endpoint, everything else to v2.
    /// </summary>
    /// <param name="metrics">Metrics to submit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> SubmitMetricsAsync(List<SeriesMetric> metrics, CancellationToken cancellationToken = default)
    {
        if (metrics.Count == 0)
        {
            return true;
        }

        // Separate distributions from regular metrics
        var distributions = metrics.Where(m => m.IsDistribution).ToList();
        var regularMetrics = metrics.Where(m => !m.IsDistribution).ToList();

        var success = true;

        // Submit distributions to v1 endpoint
        if (distributions.Count > 0)
        {
            // Convert SeriesMetric to DistributionSeriesMetric format
            // Distribution points must be tuples: [[timestamp, [values]]]
            var distributionSeries = distributions.Select(d => new DistributionSeriesMetric
            {
                Metric = d.Metric,
                Host = d.Resources?.FirstOrDefault()?.Name,
                // Convert MetricPoint objects to tuple format: [[timestamp, [values]]]
                Points = d.Points.Select(p => new object[]
                {
                    p.Timestamp,
                    new[] { p.Value }  // Wrap single value in array
                }).ToArray(),
                Tags = d.Tags,
                Type = "distribution"
            }).ToArray();

            var distributionPayload = new DistributionPayload { Series = distributionSeries };
            success &= await SubmitWithRetryAsync(_v1DistributionUrl, distributionPayload, cancellationToken);
        }

        // Submit regular metrics to v2 endpoint
        if (regularMetrics.Count > 0)
        {
            var seriesPayload = new SeriesPayload { Series = regularMetrics.ToArray() };
            success &= await SubmitWithRetryAsync(_v2SeriesUrl, seriesPayload, cancellationToken);
        }

        return success;
    }

    /// <summary>
    /// Submits payload with exponential backoff retry logic.
    /// </summary>
    private async Task<bool> SubmitWithRetryAsync<T>(string url, T payload, CancellationToken cancellationToken)
    {
        var attempt = 0;
        var maxRetries = _options.MaxRetries;
        var backoffSeconds = _options.RetryBackoffSeconds;

        while (attempt <= maxRetries)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogDebug("Submitting metrics to {Url} (attempt {Attempt}/{MaxRetries})", url, attempt + 1, maxRetries + 1);

                var response = await _httpClient.PostAsync(url, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Successfully submitted metrics to {Url}", url);
                    return true;
                }

                // Log error details
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Failed to submit metrics to {Url}. Status: {StatusCode}, Body: {Body}",
                    url, response.StatusCode, errorBody);

                // Don't retry on client errors (4xx)
                if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                {
                    _logger.LogError("Client error {StatusCode}, not retrying", response.StatusCode);
                    return false;
                }

                // Retry on server errors (5xx) or network issues
                if (attempt < maxRetries)
                {
                    var delaySeconds = backoffSeconds * Math.Pow(2, attempt);
                    _logger.LogInformation(
                        "Retrying in {DelaySeconds} seconds (attempt {Attempt}/{MaxRetries})",
                        delaySeconds, attempt + 1, maxRetries);

                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                }

                attempt++;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Network error submitting metrics to {Url} (attempt {Attempt}/{MaxRetries})",
                    url, attempt + 1, maxRetries + 1);

                if (attempt >= maxRetries)
                {
                    _logger.LogError(ex, "Failed to submit metrics after {MaxRetries} retries", maxRetries + 1);
                    return false;
                }

                var delaySeconds = backoffSeconds * Math.Pow(2, attempt);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                attempt++;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Request timeout submitting metrics to {Url}", url);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error submitting metrics to {Url}", url);
                return false;
            }
        }

        _logger.LogError("Failed to submit metrics to {Url} after {MaxRetries} retries", url, maxRetries + 1);
        return false;
    }

    /// <summary>
    /// Creates configured HttpClient instance.
    /// </summary>
    private HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds)
        };

        // Add API key header
        client.DefaultRequestHeaders.Add("DD-API-KEY", _options.ApiKey);

        // Add user agent
        client.DefaultRequestHeaders.Add("User-Agent", "Datadog.Metrics/1.0.0 (.NET)");

        return client;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
