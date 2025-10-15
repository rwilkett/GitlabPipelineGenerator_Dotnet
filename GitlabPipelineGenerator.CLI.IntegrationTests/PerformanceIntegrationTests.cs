using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Services;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Models;
using System.Diagnostics;

namespace GitlabPipelineGenerator.CLI.IntegrationTests;

/// <summary>
/// Performance integration tests for GitLab API operations and analysis
/// </summary>
[TestClass]
public class PerformanceIntegrationTests
{
    private IServiceProvider? _serviceProvider;
    private ILogger<PerformanceIntegrationTests>? _logger;
    private readonly string _testGitLabUrl = "https://gitlab.com";
    private readonly string? _testToken = Environment.GetEnvironmentVariable("GITLAB_TEST_TOKEN");
    private readonly string? _testProjectPath = Environment.GetEnvironmentVariable("GITLAB_TEST_PROJECT");

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Configure test configuration with performance-optimized settings
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GitLab:DefaultInstanceUrl"] = "https://gitlab.com",
                ["GitLab:ApiVersion"] = "v4",
                ["GitLab:RequestTimeout"] = "00:01:00", // Longer timeout for performance tests
                ["GitLab:MaxRetryAttempts"] = "2", // Fewer retries for performance tests
                ["GitLab:RateLimitRespectful"] = "true",
                ["GitLab:Analysis:MaxFileSize"] = "2097152", // 2MB for performance tests
                ["GitLab:Analysis:MaxFilesAnalyzed"] = "2000", // More files for performance tests
                ["GitLab:CircuitBreaker:FailureThreshold"] = "10",
                ["GitLab:CircuitBreaker:RecoveryTimeout"] = "00:02:00",
                ["GitLab:Retry:MaxAttempts"] = "2",
                ["GitLab:Retry:BaseDelay"] = "00:00:01"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<GitLabApiSettings>(configuration.GetSection("GitLab"));

        // Configure logging for performance monitoring
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Register all services
        RegisterAllServices(services);

        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<PerformanceIntegrationTests>>();
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
        services.AddTransient<IAnalysisToPipelineMappingService, AnalysisToPipelineMappingService>();
        services.AddTransient<IntelligentPipelineGenerator>();

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
    public async Task AuthenticationPerformance_ShouldCompleteQuickly()
    {
        // Skip test if no credentials provided
        if (string.IsNullOrEmpty(_testToken))
        {
            Assert.Inconclusive("Test requires GITLAB_TEST_TOKEN environment variable");
            return;
        }

        _logger?.LogInformation("Starting authentication performance test");

        var stopwatch = Stopwatch.StartNew();
        var authService = _serviceProvider!.GetRequiredService<IGitLabAuthenticationService>();
        
        var connectionOptions = new GitLabConnectionOptions
        {
            PersonalAccessToken = _testToken,
            InstanceUrl = _testGitLabUrl,
            StoreCredentials = false
        };

        // Perform multiple authentication attempts to test caching/performance
        for (int i = 0; i < 3; i++)
        {
            var client = await authService.AuthenticateAsync(connectionOptions);
            Assert.IsNotNull(client, $"Authentication attempt {i + 1} should succeed");
        }

        stopwatch.Stop();
        
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10000, "Authentication should complete within 10 seconds");
        _logger?.LogInformation("Authentication performance test completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
    }

    [TestMethod]
    public async Task ProjectListingPerformance_ShouldHandleLargeResults()
    {
        // Skip test if no credentials provided
        if (string.IsNullOrEmpty(_testToken))
        {
            Assert.Inconclusive("Test requires GITLAB_TEST_TOKEN environment variable");
            return;
        }

        _logger?.LogInformation("Starting project listing performance test");

        var stopwatch = Stopwatch.StartNew();
        
        // Authenticate
        var authService = _serviceProvider!.GetRequiredService<IGitLabAuthenticationService>();
        var connectionOptions = new GitLabConnectionOptions
        {
            PersonalAccessToken = _testToken,
            InstanceUrl = _testGitLabUrl,
            StoreCredentials = false
        };

        await authService.AuthenticateAsync(connectionOptions);

        // List projects with large result set
        var projectService = _serviceProvider!.GetRequiredService<IGitLabProjectService>();
        var listOptions = new ProjectListOptions
        {
            MemberOnly = false, // Include all accessible projects
            MaxResults = 100, // Large result set
            OrderBy = ProjectOrderBy.LastActivity
        };

        var projects = await projectService.ListProjectsAsync(listOptions);
        stopwatch.Stop();

        Assert.IsNotNull(projects, "Projects should be retrieved");
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 30000, "Project listing should complete within 30 seconds");
        
        _logger?.LogInformation("Project listing performance test completed in {ElapsedMs}ms, found {ProjectCount} projects", 
            stopwatch.ElapsedMilliseconds, projects.Count());
    }

    [TestMethod]
    public async Task ProjectAnalysisPerformance_ShouldHandleComplexProjects()
    {
        // Skip test if no credentials provided
        if (string.IsNullOrEmpty(_testToken) || string.IsNullOrEmpty(_testProjectPath))
        {
            Assert.Inconclusive("Test requires GITLAB_TEST_TOKEN and GITLAB_TEST_PROJECT environment variables");
            return;
        }

        _logger?.LogInformation("Starting project analysis performance test");

        var stopwatch = Stopwatch.StartNew();
        
        // Authenticate and get project
        var authService = _serviceProvider!.GetRequiredService<IGitLabAuthenticationService>();
        var connectionOptions = new GitLabConnectionOptions
        {
            PersonalAccessToken = _testToken,
            InstanceUrl = _testGitLabUrl,
            StoreCredentials = false
        };

        await authService.AuthenticateAsync(connectionOptions);

        var projectService = _serviceProvider!.GetRequiredService<IGitLabProjectService>();
        var project = await projectService.GetProjectAsync(_testProjectPath);

        // Perform comprehensive analysis
        var analysisService = _serviceProvider!.GetRequiredService<IProjectAnalysisService>();
        var analysisOptions = new AnalysisOptions
        {
            AnalyzeFiles = true,
            AnalyzeDependencies = true,
            AnalyzeExistingCI = true,
            AnalyzeDeployment = true,
            MaxFileAnalysisDepth = 3, // Deep analysis
            IncludeSecurityAnalysis = true
        };

        var analysisResult = await analysisService.AnalyzeProjectAsync(project, analysisOptions);
        stopwatch.Stop();

        Assert.IsNotNull(analysisResult, "Analysis should complete");
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 60000, "Analysis should complete within 60 seconds");
        
        _logger?.LogInformation("Project analysis performance test completed in {ElapsedMs}ms, detected {ProjectType}", 
            stopwatch.ElapsedMilliseconds, analysisResult.DetectedType);
    }

    [TestMethod]
    public async Task PipelineGenerationPerformance_ShouldGenerateQuickly()
    {
        // Skip test if no credentials provided
        if (string.IsNullOrEmpty(_testToken) || string.IsNullOrEmpty(_testProjectPath))
        {
            Assert.Inconclusive("Test requires GITLAB_TEST_TOKEN and GITLAB_TEST_PROJECT environment variables");
            return;
        }

        _logger?.LogInformation("Starting pipeline generation performance test");

        var totalStopwatch = Stopwatch.StartNew();
        
        // Setup (authentication and analysis)
        var authService = _serviceProvider!.GetRequiredService<IGitLabAuthenticationService>();
        var connectionOptions = new GitLabConnectionOptions
        {
            PersonalAccessToken = _testToken,
            InstanceUrl = _testGitLabUrl,
            StoreCredentials = false
        };

        await authService.AuthenticateAsync(connectionOptions);

        var projectService = _serviceProvider!.GetRequiredService<IGitLabProjectService>();
        var project = await projectService.GetProjectAsync(_testProjectPath);

        var analysisService = _serviceProvider!.GetRequiredService<IProjectAnalysisService>();
        var analysisOptions = new AnalysisOptions
        {
            AnalyzeFiles = true,
            AnalyzeDependencies = true,
            AnalyzeExistingCI = true,
            AnalyzeDeployment = true,
            MaxFileAnalysisDepth = 2,
            IncludeSecurityAnalysis = true
        };

        var analysisResult = await analysisService.AnalyzeProjectAsync(project, analysisOptions);

        // Measure pipeline generation specifically
        var generationStopwatch = Stopwatch.StartNew();
        
        var intelligentGenerator = _serviceProvider!.GetRequiredService<IntelligentPipelineGenerator>();
        var pipelineOptions = new AnalysisBasedPipelineOptions
        {
            ProjectType = analysisResult.DetectedType,
            AnalysisResult = analysisResult,
            Stages = new List<string> { "build", "test", "security", "deploy" },
            Variables = new Dictionary<string, string>
            {
                ["BUILD_CONFIG"] = "Release",
                ["NODE_ENV"] = "production"
            },
            Environments = new Dictionary<string, string>
            {
                ["staging"] = "https://staging.example.com",
                ["production"] = "https://production.example.com"
            }
        };

        var pipeline = await intelligentGenerator.GenerateAsync(pipelineOptions);
        var yamlContent = intelligentGenerator.SerializeToYaml(pipeline);
        
        generationStopwatch.Stop();
        totalStopwatch.Stop();

        Assert.IsNotNull(pipeline, "Pipeline should be generated");
        Assert.IsFalse(string.IsNullOrEmpty(yamlContent), "YAML should be generated");
        Assert.IsTrue(generationStopwatch.ElapsedMilliseconds < 5000, "Pipeline generation should complete within 5 seconds");
        Assert.IsTrue(totalStopwatch.ElapsedMilliseconds < 90000, "Total workflow should complete within 90 seconds");
        
        _logger?.LogInformation("Pipeline generation performance test completed - Generation: {GenerationMs}ms, Total: {TotalMs}ms", 
            generationStopwatch.ElapsedMilliseconds, totalStopwatch.ElapsedMilliseconds);
    }

    [TestMethod]
    public async Task ConcurrentOperationsPerformance_ShouldHandleMultipleRequests()
    {
        // Skip test if no credentials provided
        if (string.IsNullOrEmpty(_testToken))
        {
            Assert.Inconclusive("Test requires GITLAB_TEST_TOKEN environment variable");
            return;
        }

        _logger?.LogInformation("Starting concurrent operations performance test");

        var stopwatch = Stopwatch.StartNew();
        
        // Authenticate once
        var authService = _serviceProvider!.GetRequiredService<IGitLabAuthenticationService>();
        var connectionOptions = new GitLabConnectionOptions
        {
            PersonalAccessToken = _testToken,
            InstanceUrl = _testGitLabUrl,
            StoreCredentials = false
        };

        await authService.AuthenticateAsync(connectionOptions);

        // Perform concurrent project listing operations
        var projectService = _serviceProvider!.GetRequiredService<IGitLabProjectService>();
        var tasks = new List<Task>();

        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var listOptions = new ProjectListOptions
                {
                    MemberOnly = true,
                    MaxResults = 20,
                    OrderBy = ProjectOrderBy.LastActivity
                };

                var projects = await projectService.ListProjectsAsync(listOptions);
                Assert.IsNotNull(projects, "Projects should be retrieved in concurrent operation");
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 30000, "Concurrent operations should complete within 30 seconds");
        
        _logger?.LogInformation("Concurrent operations performance test completed in {ElapsedMs}ms", 
            stopwatch.ElapsedMilliseconds);
    }

    [TestMethod]
    public async Task MemoryUsageTest_ShouldNotLeakMemory()
    {
        // Skip test if no credentials provided
        if (string.IsNullOrEmpty(_testToken) || string.IsNullOrEmpty(_testProjectPath))
        {
            Assert.Inconclusive("Test requires GITLAB_TEST_TOKEN and GITLAB_TEST_PROJECT environment variables");
            return;
        }

        _logger?.LogInformation("Starting memory usage test");

        var initialMemory = GC.GetTotalMemory(true);
        
        // Perform multiple analysis cycles
        for (int i = 0; i < 3; i++)
        {
            var authService = _serviceProvider!.GetRequiredService<IGitLabAuthenticationService>();
            var connectionOptions = new GitLabConnectionOptions
            {
                PersonalAccessToken = _testToken,
                InstanceUrl = _testGitLabUrl,
                StoreCredentials = false
            };

            await authService.AuthenticateAsync(connectionOptions);

            var projectService = _serviceProvider!.GetRequiredService<IGitLabProjectService>();
            var project = await projectService.GetProjectAsync(_testProjectPath);

            var analysisService = _serviceProvider!.GetRequiredService<IProjectAnalysisService>();
            var analysisOptions = new AnalysisOptions
            {
                AnalyzeFiles = true,
                AnalyzeDependencies = true,
                AnalyzeExistingCI = true,
                AnalyzeDeployment = false, // Reduce memory usage
                MaxFileAnalysisDepth = 1,
                IncludeSecurityAnalysis = false
            };

            var analysisResult = await analysisService.AnalyzeProjectAsync(project, analysisOptions);
            Assert.IsNotNull(analysisResult, $"Analysis cycle {i + 1} should complete");

            // Force garbage collection between cycles
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;
        
        // Memory increase should be reasonable (less than 50MB)
        Assert.IsTrue(memoryIncrease < 50 * 1024 * 1024, 
            $"Memory increase should be less than 50MB, actual: {memoryIncrease / 1024 / 1024}MB");
        
        _logger?.LogInformation("Memory usage test completed - Initial: {InitialMB}MB, Final: {FinalMB}MB, Increase: {IncreaseMB}MB", 
            initialMemory / 1024 / 1024, finalMemory / 1024 / 1024, memoryIncrease / 1024 / 1024);
    }

    [TestMethod]
    public async Task ErrorRecoveryPerformance_ShouldRecoverQuickly()
    {
        _logger?.LogInformation("Starting error recovery performance test");

        var stopwatch = Stopwatch.StartNew();
        
        var authService = _serviceProvider!.GetRequiredService<IGitLabAuthenticationService>();
        
        // Test recovery from authentication errors
        var invalidConnectionOptions = new GitLabConnectionOptions
        {
            PersonalAccessToken = "invalid-token",
            InstanceUrl = _testGitLabUrl,
            StoreCredentials = false
        };

        // Should fail quickly
        var errorStopwatch = Stopwatch.StartNew();
        try
        {
            await authService.AuthenticateAsync(invalidConnectionOptions);
            Assert.Fail("Should have thrown authentication exception");
        }
        catch (Exception)
        {
            errorStopwatch.Stop();
            Assert.IsTrue(errorStopwatch.ElapsedMilliseconds < 10000, "Error should be detected within 10 seconds");
        }

        // Test fallback service performance
        var fallbackService = _serviceProvider!.GetRequiredService<IGitLabFallbackService>();
        var fallbackStopwatch = Stopwatch.StartNew();
        
        var canFallback = await fallbackService.CanFallbackToManualModeAsync();
        var degradedAnalysis = await fallbackService.GetDegradedAnalysisAsync(ProjectType.DotNet);
        
        fallbackStopwatch.Stop();
        stopwatch.Stop();

        Assert.IsTrue(canFallback, "Should be able to fallback");
        Assert.IsNotNull(degradedAnalysis, "Should provide degraded analysis");
        Assert.IsTrue(fallbackStopwatch.ElapsedMilliseconds < 1000, "Fallback should be fast (< 1 second)");
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 15000, "Total error recovery should complete within 15 seconds");
        
        _logger?.LogInformation("Error recovery performance test completed in {ElapsedMs}ms", 
            stopwatch.ElapsedMilliseconds);
    }
}