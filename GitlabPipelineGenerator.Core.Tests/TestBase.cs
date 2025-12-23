using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using GitlabPipelineGenerator.Core.Configuration;

namespace GitlabPipelineGenerator.Core.Tests;

/// <summary>
/// Base class for unit tests with common setup and utilities
/// </summary>
public abstract class TestBase : IDisposable
{
    protected ServiceProvider ServiceProvider { get; private set; }
    protected ILogger<TestBase> Logger { get; private set; }
    protected Mock<IConfiguration> MockConfiguration { get; private set; }

    protected TestBase()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
        Logger = ServiceProvider.GetRequiredService<ILogger<TestBase>>();
    }

    /// <summary>
    /// Configure services for testing
    /// </summary>
    /// <param name="services">Service collection</param>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Add mock configuration
        MockConfiguration = new Mock<IConfiguration>();
        services.AddSingleton(MockConfiguration.Object);

        // Add default options
        services.Configure<PipelineGeneratorOptions>(_ => { });

        // Allow derived classes to add more services
        ConfigureTestServices(services);
    }

    /// <summary>
    /// Override this method to add test-specific services
    /// </summary>
    /// <param name="services">Service collection</param>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // Override in derived classes
    }

    /// <summary>
    /// Get a service from the test service provider
    /// </summary>
    /// <typeparam name="T">Service type</typeparam>
    /// <returns>Service instance</returns>
    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Create a mock of the specified type
    /// </summary>
    /// <typeparam name="T">Type to mock</typeparam>
    /// <returns>Mock instance</returns>
    protected static Mock<T> CreateMock<T>() where T : class
    {
        return new Mock<T>();
    }

    /// <summary>
    /// Create a mock with strict behavior
    /// </summary>
    /// <typeparam name="T">Type to mock</typeparam>
    /// <returns>Strict mock instance</returns>
    protected static Mock<T> CreateStrictMock<T>() where T : class
    {
        return new Mock<T>(MockBehavior.Strict);
    }

    public virtual void Dispose()
    {
        ServiceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}