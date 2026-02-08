using Datadog.Metrics;
using Datadog.Metrics.Configuration;
using Datadog.Metrics.Core;
using Datadog.Metrics.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Datadog.Metrics.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDatadogMetrics_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddDatadogMetrics(options =>
        {
            options.ApiKey = "test-key";
            options.Site = "datadoghq.com";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify all core services can be resolved
        var aggregator = serviceProvider.GetService<MetricsAggregator>();
        Assert.NotNull(aggregator);

        var logger = serviceProvider.GetService<DatadogMetricsLogger>();
        Assert.NotNull(logger);
    }

    [Fact]
    public void AddDatadogMetrics_AppliesConfigurationOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddDatadogMetrics(options =>
        {
            options.ApiKey = "test-key-123";
            options.Site = "us3.datadoghq.com";
            options.Host = "custom-host";
            options.Prefix = "myapp.";
            options.DefaultTags = new[] { "env:test", "service:api" };
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var aggregator = serviceProvider.GetRequiredService<MetricsAggregator>();
        Assert.NotNull(aggregator);

        // Verify the logger can be created with the configured options
        var logger = serviceProvider.GetRequiredService<DatadogMetricsLogger>();
        Assert.NotNull(logger);
    }

    [Fact]
    public void AddDatadogMetrics_RegistersSingletons()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddDatadogMetrics(options =>
        {
            options.ApiKey = "test-key";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify services are singletons (same instance)
        var aggregator1 = serviceProvider.GetRequiredService<MetricsAggregator>();
        var aggregator2 = serviceProvider.GetRequiredService<MetricsAggregator>();
        Assert.Same(aggregator1, aggregator2);

        var logger1 = serviceProvider.GetRequiredService<DatadogMetricsLogger>();
        var logger2 = serviceProvider.GetRequiredService<DatadogMetricsLogger>();
        Assert.Same(logger1, logger2);
    }

    [Fact]
    public void AddDatadogMetricsWithDiagnostics_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddDatadogMetricsWithDiagnostics(options =>
        {
            options.ApiKey = "test-key";
            options.Site = "datadoghq.com";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify all services including diagnostics can be resolved
        var aggregator = serviceProvider.GetService<MetricsAggregator>();
        Assert.NotNull(aggregator);

        var logger = serviceProvider.GetService<DatadogMetricsLogger>();
        Assert.NotNull(logger);

        // Verify hosted services are registered
        var hostedServices = serviceProvider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
        Assert.NotEmpty(hostedServices);
        Assert.True(hostedServices.Count() >= 2); // DatadogReporter + DatadogMeterListener
    }
}
