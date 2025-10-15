using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Services;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Exceptions;

namespace GitlabPipelineGenerator.CLI.IntegrationTests;

/// <summary>
/// Integration tests for resilience patterns and error handling
/// </summary>
[TestClass]
public class ResilienceIntegrationTests
{
    private IServiceProvider? _serviceProvider;
    private ILogger<ResilienceIntegrationTests>? _logger;

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Configure test configuration with resilience settings
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GitLab:DefaultInstanceUrl"] = "https://gitlab.com",
                ["GitLab:ApiVersion"] = "v4",
                ["GitLab:RequestTimeout"] = "00:00:10",
                ["GitLab:MaxRetryAttempts"] = "2",
                ["GitLab:RateLimitRespectful"] = "true",
                ["GitLab:CircuitBreaker:FailureThreshold"] = "3",
                ["GitLab:CircuitBreaker:RecoveryTimeout"] = "00:00:30",
                ["GitLab:CircuitBreaker:HalfOpenMaxCalls"] = "2",
                ["GitLab:Retry:MaxAttempts"] = "2",
                ["GitLab:Retry:BaseDelay"] = "00:00:01",
                ["GitLab:Retry:MaxDelay"] = "00:00:05"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<GitLabApiSettings>(configuration.GetSection("GitLab"));

        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Register all services
        RegisterAllServices(services);

        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<ResilienceIntegrationTests>>();
    }

    private void RegisterAllServices(IServiceCollection services)
    {
        // Register GitLab services
        services.AddTransient<IGitLabAuthenticationService, GitLabAuthenticationService>();
        services.AddTransient<IGitLabProjectService, GitLabProjectService>();
        services.AddTransient<IProjectAnalysisService, ProjectAnalysisService>();
        services.AddTransient<IFilePatternAnalyzer, FilePatternAnalyzer>();
        services.AddTransient<IDependencyAnalyzer, DependencyAnalyzer>();
        services.AddTransient<IConfigurationAnalyzer, ConfigurationAnalyzer>();

        // Register error handling and resilience services
        services.AddSingleton<GitLabApiErrorHandler>();
        services.AddSingleton<CircuitBreaker>();
        services.AddTransient<ResilientGitLabService>();
        services.AddTransient<IGitLabFallbackService, GitLabFallbackService>();
        services.AddTransient<DegradedAnalysisService>();

        // Register configuration management services
        services.AddSingleton<ICredentialStorageService, CrossPlatformCredentialStorageService>();
        services.AddTransient<IConfigurationProfileService, ConfigurationProfileService>();
        services.AddTransient<IConfigurationManagementService, ConfigurationManagementService>();

        // Register validation services
        services.AddTransient<GitLabConnectionValidator>();
        services.AddTransient<IGitLabPermissionValidator, GitLabPermissionValidator>();
        services.AddTransient<IGitLabApiErrorHandler, GitLabApiErrorHandler>();

        // Register core pipeline services
        services.AddTransient<IPipelineGenerator, PipelineGenerator>();
        services.AddTransient<IStageBuilder, StageBuilder>();
        services.AddTransient<IJobBuilder, JobBuilder>();
        services.AddTransient<IVariableBuilder, VariableBuilder>();
        services.AddTransient<YamlSerializationService>();
        services.AddTransient<ValidationService>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [TestMethod]
    public async Task CircuitBreakerTest_ShouldOpenAfterFailures()
    {
        _logger?.LogInformation("Starting circuit breaker test");

        var circuitBreaker = _serviceProvider!.GetRequiredService<CircuitBreaker>();
        var errorHandler = _serviceProvider!.GetRequiredService<IGitLabApiErrorHandler>();

        // Simulate multiple failures to trigger circuit breaker
        for (int i = 0; i < 4; i++)
        {
            try
            {
                await errorHandler.ExecuteWithRetryAsync(async () =>
                {
                    throw new GitLabApiException("Simulated API failure", 500);
                }, new RetryPolicy { MaxAttempts = 1, BaseDelay = TimeSpan.FromMilliseconds(100) });
            }
            catch (GitLabApiException)
            {
                // Expected
            }
        }

        // Circuit breaker should be open now
        Assert.IsTrue(circuitBreaker.IsOpen, "Circuit breaker should be open after failures");

        // Next call should fail fast
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await errorHandler.ExecuteWithRetryAsync(async () =>
            {
                return "success";
            }, new RetryPolicy { MaxAttempts = 3, BaseDelay = TimeSpan.FromSeconds(1) });
        }
        catch (Exception)
        {
            // Expected - circuit breaker should fail fast
        }
        stopwatch.Stop();

        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, "Circuit breaker should fail fast");
        _logger?.LogInformation("Circuit breaker test completed - failed fast in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
    }

    [TestMethod]
    public async Task RetryPolicyTest_ShouldRetryTransientFailures()
    {
        _logger?.LogInformation("Starting retry policy test");

        var errorHandler = _serviceProvider!.GetRequiredService<IGitLabApiErrorHandler>();
        int attemptCount = 0;

        var result = await errorHandler.ExecuteWithRetryAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new GitLabApiException("Transient failure", 503); // Service unavailable
            }
            return "success";
        }, new RetryPolicy { MaxAttempts = 3, BaseDelay = TimeSpan.FromMilliseconds(100) });

        Assert.AreEqual("success", result, "Should succeed after retries");
        Assert.AreEqual(3, attemptCount, "Should have made 3 attempts");
        
        _logger?.LogInformation("Retry policy test completed - succeeded after {Attempts} attempts", attemptCount);
    }

    [TestMethod]
    public async Task FallbackServiceTest_ShouldProvideGracefulDegradation()
    {
        _logger?.LogInformation("Starting fallback service test");

        var fallbackService = _serviceProvider!.GetRequiredService<IGitLabFallbackService>();

        // Test fallback to manual mode
        var canFallback = await fallbackService.CanFallbackToManualModeAsync();
        Assert.IsTrue(canFallback, "Should be able to fallback to manual mode");

        // Test degraded analysis for different project types
        var projectTypes = new[] { ProjectType.DotNet, ProjectType.NodeJs, ProjectType.Python, ProjectType.Docker };
        
        foreach (var projectType in projectTypes)
        {
            var degradedAnalysis = await fallbackService.GetDegradedAnalysisAsync(projectType);
            
            Assert.IsNotNull(degradedAnalysis, $"Should provide degraded analysis for {projectType}");
            Assert.AreEqual(projectType, degradedAnalysis.DetectedType, $"Should maintain project type for {projectType}");
            Assert.IsNotNull(degradedAnalysis.Framework, $"Should provide framework info for {projectType}");
            Assert.IsNotNull(degradedAnalysis.BuildConfig, $"Should provide build config for {projectType}");
            
            _logger?.LogInformation("Degraded analysis provided for {ProjectType}: {Framework}", 
                projectType, degradedAnalysis.Framework.Name);
        }

        // Test fallback pipeline generation
        var fallbackPipeline = await fallbackService.GenerateFallbackPipelineAsync(ProjectType.DotNet);
        
        Assert.IsNotNull(fallbackPipeline, "Should provide fallback pipeline");
        Assert.IsTrue(fallbackPipeline.Jobs.Count > 0, "Fallback pipeline should have jobs");
        Assert.IsTrue(fallbackPipeline.Stages.Count > 0, "Fallback pipeline should have stages");

        _logger?.LogInformation("Fallback service test completed successfully");
    }

    [TestMethod]
    public async Task ResilientServiceTest_ShouldHandleNetworkIssues()
    {
        _logger?.LogInformation("Starting resilient service test");

        var resilientService = _serviceProvider!.GetRequiredService<ResilientGitLabService>();

        // Test with invalid URL to simulate network issues
        var connectionOptions = new GitLabConnectionOptions
        {
            PersonalAccessToken = "test-token",
            InstanceUrl = "https://invalid-gitlab-instance.example.com",
            StoreCredentials = false
        };

        // Should handle network errors gracefully
        var result = await resilientService.TryAuthenticateAsync(connectionOptions);
        
        Assert.IsFalse(result.IsSuccess, "Should fail with invalid URL");
        Assert.IsNotNull(result.Error, "Should provide error information");
        Assert.IsTrue(result.Error.Contains("network") || result.Error.Contains("connection") || result.Error.Contains("resolve"), 
            "Error should indicate network/connection issue");

        _logger?.LogInformation("Resilient service test completed - handled network error: {Error}", result.Error);
    }

    [TestMethod]
    public async Task ErrorTranslationTest_ShouldProvideUserFriendlyMessages()
    {
        _logger?.LogInformation("Starting error translation test");

        var errorHandler = _serviceProvider!.GetRequiredService<IGitLabApiErrorHandler>();

        // Test different error scenarios
        var testCases = new[]
        {
            new { Exception = new GitLabApiException("Unauthorized", 401), ExpectedKeywords = new[] { "authentication", "token", "credentials" } },
            new { Exception = new GitLabApiException("Forbidden", 403), ExpectedKeywords = new[] { "permission", "access", "forbidden" } },
            new { Exception = new GitLabApiException("Not Found", 404), ExpectedKeywords = new[] { "not found", "project", "exists" } },
            new { Exception = new GitLabApiException("Rate Limited", 429), ExpectedKeywords = new[] { "rate limit", "too many", "requests" } },
            new { Exception = new GitLabApiException("Internal Server Error", 500), ExpectedKeywords = new[] { "server error", "try again", "later" } }
        };

        foreach (var testCase in testCases)
        {
            var friendlyMessage = errorHandler.TranslateGitLabError(testCase.Exception);
            
            Assert.IsFalse(string.IsNullOrEmpty(friendlyMessage), "Should provide friendly error message");
            
            var containsKeyword = testCase.ExpectedKeywords.Any(keyword => 
                friendlyMessage.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            
            Assert.IsTrue(containsKeyword, 
                $"Error message '{friendlyMessage}' should contain one of: {string.Join(", ", testCase.ExpectedKeywords)}");
            
            _logger?.LogInformation("Error {StatusCode} translated to: {FriendlyMessage}", 
                testCase.Exception.StatusCode, friendlyMessage);
        }

        _logger?.LogInformation("Error translation test completed successfully");
    }

    [TestMethod]
    public async Task DegradedAnalysisTest_ShouldProvideBasicAnalysis()
    {
        _logger?.LogInformation("Starting degraded analysis test");

        var degradedService = _serviceProvider!.GetRequiredService<DegradedAnalysisService>();

        // Test degraded analysis for different scenarios
        var testScenarios = new[]
        {
            new { ProjectType = ProjectType.DotNet, ExpectedFramework = ".NET" },
            new { ProjectType = ProjectType.NodeJs, ExpectedFramework = "Node.js" },
            new { ProjectType = ProjectType.Python, ExpectedFramework = "Python" },
            new { ProjectType = ProjectType.Docker, ExpectedFramework = "Docker" }
        };

        foreach (var scenario in testScenarios)
        {
            var analysisResult = await degradedService.GetBasicAnalysisAsync(scenario.ProjectType);
            
            Assert.IsNotNull(analysisResult, $"Should provide analysis for {scenario.ProjectType}");
            Assert.AreEqual(scenario.ProjectType, analysisResult.DetectedType, "Should maintain project type");
            Assert.IsNotNull(analysisResult.Framework, "Should provide framework information");
            Assert.IsTrue(analysisResult.Framework.Name.Contains(scenario.ExpectedFramework, StringComparison.OrdinalIgnoreCase), 
                $"Framework should be {scenario.ExpectedFramework}");
            Assert.IsNotNull(analysisResult.BuildConfig, "Should provide build configuration");
            Assert.IsTrue(analysisResult.BuildConfig.BuildCommands.Count > 0, "Should provide build commands");
            
            _logger?.LogInformation("Degraded analysis for {ProjectType}: {Framework} with {CommandCount} build commands", 
                scenario.ProjectType, analysisResult.Framework.Name, analysisResult.BuildConfig.BuildCommands.Count);
        }

        _logger?.LogInformation("Degraded analysis test completed successfully");
    }

    [TestMethod]
    public async Task TimeoutHandlingTest_ShouldHandleTimeouts()
    {
        _logger?.LogInformation("Starting timeout handling test");

        var errorHandler = _serviceProvider!.GetRequiredService<IGitLabApiErrorHandler>();

        // Test timeout handling
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            await errorHandler.ExecuteWithRetryAsync(async () =>
            {
                // Simulate long-running operation
                await Task.Delay(TimeSpan.FromSeconds(15)); // Longer than configured timeout
                return "success";
            }, new RetryPolicy { MaxAttempts = 1, BaseDelay = TimeSpan.FromMilliseconds(100) });
            
            Assert.Fail("Should have timed out");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Should timeout within reasonable time (configured timeout + buffer)
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 20000, "Should timeout within 20 seconds");
            Assert.IsTrue(ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) || 
                         ex is OperationCanceledException, "Should indicate timeout");
            
            _logger?.LogInformation("Timeout handling test completed - timed out in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
    }

    [TestMethod]
    public async Task ConcurrentResilienceTest_ShouldHandleConcurrentFailures()
    {
        _logger?.LogInformation("Starting concurrent resilience test");

        var errorHandler = _serviceProvider!.GetRequiredService<IGitLabApiErrorHandler>();
        var tasks = new List<Task<string>>();

        // Create multiple concurrent operations that will fail
        for (int i = 0; i < 5; i++)
        {
            int operationId = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    return await errorHandler.ExecuteWithRetryAsync(async () =>
                    {
                        if (operationId < 3)
                        {
                            throw new GitLabApiException($"Simulated failure {operationId}", 503);
                        }
                        return $"success-{operationId}";
                    }, new RetryPolicy { MaxAttempts = 2, BaseDelay = TimeSpan.FromMilliseconds(100) });
                }
                catch (Exception ex)
                {
                    return $"failed-{operationId}: {ex.Message}";
                }
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Should handle all operations without crashing
        Assert.AreEqual(5, results.Length, "Should complete all operations");
        
        var successCount = results.Count(r => r.StartsWith("success"));
        var failureCount = results.Count(r => r.StartsWith("failed"));
        
        Assert.AreEqual(2, successCount, "Should have 2 successful operations");
        Assert.AreEqual(3, failureCount, "Should have 3 failed operations");
        
        _logger?.LogInformation("Concurrent resilience test completed - {SuccessCount} successes, {FailureCount} failures", 
            successCount, failureCount);
    }
}