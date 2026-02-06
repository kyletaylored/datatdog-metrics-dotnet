using Datadog.Metrics.Configuration;
using Datadog.Metrics.Core;
using Datadog.Metrics.Integration;
using Datadog.Metrics.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace Datadog.Metrics.Extensions;

/// <summary>
/// Extension methods for configuring Datadog metrics in dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Datadog metrics services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure Datadog metrics options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDatadogMetrics(
        this IServiceCollection services,
        Action<DatadogMetricsOptions> configureOptions)
    {
        // Configure options
        services.Configure(configureOptions);

        // Register core services as singletons
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatadogMetricsOptions>>();
            var config = options.Value;

            return new MetricsAggregator(
                config.Host ?? Environment.MachineName,
                config.Prefix,
                config.DefaultTags,
                config.MaxBufferSize);
        });

        services.AddSingleton<HttpApi>();
        services.AddSingleton<DatadogMetricsLogger>();

        // Register background services
        services.AddHostedService<DatadogReporter>();

        return services;
    }

    /// <summary>
    /// Adds Datadog metrics services with System.Diagnostics.Metrics integration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure Datadog metrics options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDatadogMetricsWithDiagnostics(
        this IServiceCollection services,
        Action<DatadogMetricsOptions> configureOptions)
    {
        // Add base metrics services
        services.AddDatadogMetrics(configureOptions);

        // Add MeterListener integration
        services.AddHostedService<DatadogMeterListener>();

        return services;
    }
}
