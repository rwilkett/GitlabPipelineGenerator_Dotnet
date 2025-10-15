using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using GitlabPipelineGenerator.CLI.Models;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Services;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Models;
using GitlabPipelineGenerator.Core.Exceptions;
using System.Text.Json;

namespace GitlabPipelineGenerator.CLI.IntegrationTests;

/// <summary>
/// Comprehensive end-to-end integration tests for GitLab API integration
/// </summary>
[TestClass]
public class EndToEndIntegrationTests
{
    private IServiceProvider? _serviceProvider;
    private ILogger<EndToEndIntegrationTests>? _logger;
    private readonly string _testGitLabUrl = "https://gitlab.com";
    private readonly string? _testToken = Environment.GetEnvironmentVariable("GITLAB_TEST_TOKEN");
    private readonly string? _testProjectPath = Environment.GetEnvironmentVariable("GITLAB_TEST_PROJECT");

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Configure test configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GitLab:DefaultInstanceUrl"] = "https://gitlab.com",
                ["GitLab:ApiVersion"] = "v4",
                ["GitLab:RequestTimeout"] = "00:00:30",
                ["GitLab:MaxRetryAttempts"] = "3",
                ["GitLab:RateLimitRespectful"] = "true",
                ["GitLab:Analysis:MaxFileSize"] = "1048576",
                ["GitLab:Analysis:MaxFilesAnalyzed"] = "1000",
                ["GitLab:CircuitBreaker:FailureThreshold"] = "5",
                ["GitLab:CircuitBreaker:RecoveryTimeout"] = "00:01:00",
                ["GitLab:Retry:MaxAttempts"] = "3",
                ["GitLab:Retry:BaseDelay"] = "00:00:01"
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

        // Register all GitLab services
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

        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<EndToEndIntegrationTests>>();
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
    public async Task CompleteWorkflow_WithValidCredentials_ShouldGeneratePipeline()
    {
        // Skip test if no credentials provided
        if (string.IsNullOrEmpty(_testToken) || string.IsNullOrEmpty(_testProjectPath))
        {
            Assert.Inconclusive("Test requires GITLAB_TEST_TOKEN and GITLAB_TEST_PROJECT environment variables");
            return;
        }

        _logger?.LogInformation("Starting complete workflow integration test");

        // Step 1: Authentication
        var authService = _serviceProvider!.GetRequiredService<IGitLabAuthenticationService>();
        var connectionOptions = new GitLabConnectionOptions
        {
            PersonalAccessToken = _testToken,
            InstanceUrl = _testGitLabUrl,
            StoreCredentials = false
        };

        var gitlabClient = await authService.AuthenticateAsync(connectionOptions);
        Assert.IsNotNull(gitlabClient, "GitLab client should be created");

        var userInfo = await authService.GetCurrentUserAsync();
        Assert.IsNotNull(userInfo, "User info should be retrieved");
        Assert.IsFalse(string.IsNullOrEmpty(userInfo.Username), "Username should not be empty");

        _logger?.LogInformation("Authentication successful for user: {Username}", userInfo.Username);

        // Step 2: Project Discovery
        var projectService = _serviceProvider!.GetRequiredService<IGitLabProjectService>();
        var project = await projectService.GetProjectAsync(_testProjectPath);
        
        Assert.IsNotNull(project, "Project should be found");
        Assert.IsFalse(string.IsNullOrEmpty(project.Name), "Project name should not be empty");
        Assert.IsTrue(project.Id > 0, "Project ID should be positive");

        _logger?.LogInformation("Project retrieved: {ProjectName} (ID: {ProjectId})", project.Name, project.Id);

        // Step 3: Permission Validation
        var permissions = await projectService.GetProjectPermissionsAsync(project.Id);
        Assert.IsNotNull(permissions, "Permissions should be retrieved");

        var hasPermissions = await projectService.HasSufficientPermissionsAsync(
            project.Id, 
            RequiredPermissions.ReadRepository | RequiredPermissions.ReadProject);
        Assert.IsTrue(hasPermissions, "Should have sufficient permissions for analysis");

        _logger?.LogInformation("Permission validation successful");

        // Step 4: Project Analysis
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
        
        Assert.IsNotNull(analysisResult, "Analysis result should not be null");
        Assert.AreNotEqual(ProjectType.Unknown, analysisResult.DetectedType, "Should detect project type");
        Assert.IsNotNull(analysisResult.Framework, "Framework info should be detected");
        Assert.IsNotNull(analysisResult.BuildConfig, "Build configuration should be analyzed");

        _logger?.LogInformation("Project analysis completed: {ProjectType} with {Framework}", 
            analysisResult.DetectedType, analysisResult.Framework.Name);

        // Step 5: Pipeline Generation
        var intelligentGenerator = _serviceProvider!.GetRequiredService<IntelligentPipelineGenerator>();
        var pipelineOptions = new AnalysisBasedPipelineOptions
        {
            ProjectType = analysisResult.DetectedType,
            AnalysisResult = analysisResult,
            Stages = new List<string> { "build", "test", "deploy" },
            Variables = new Dictionary<string, string>(),
            Environments = new Dictionary<string, string>()
        };

        var pipeline = await intelligentGenerator.GenerateAsync(pipelineOptions);
        
        Assert.IsNotNull(pipeline, "Pipeline should be generated");
        Assert.IsTrue(pipeline.Jobs.Count > 0, "Pipeline should have jobs");
        Assert.IsTrue(pipeline.Stages.Count > 0, "Pipeline should have stages");

        _logger?.LogInformation("Pipeline generated with {JobCount} jobs and {StageCount} stages", 
            pipeline.Jobs.Count, pipeline.Stages.Count);

        // Step 6: YAML Serialization
        var yamlContent = intelligentGenerator.SerializeToYaml(pipeline);
        
        Assert.IsFalse(string.IsNullOrEmpty(yamlContent), "YAML content should not be empty");
        Assert.IsTrue(yamlContent.Contains("stages:"), "YAML should contain stages");
        Assert.IsTrue(yamlContent.Contains("script:"), "YAML should contain scripts");

        _logger?.LogInformation("YAML serialization successful ({Length} characters)", yamlContent.Length);

        // Verify YAML is valid
        Assert.IsTrue(yamlContent.Length > 100, "YAML should be substantial");
        Assert.IsFalse(yamlContent.Contains("null"), "YAML should not contain null values");

        _logger?.LogInformation("Complete workflow integration test completed successfully");
    }

    [TestMethod]
    public async Task ProjectListingWorkflow_WithValidCredentials_ShouldListProjects()
    {
        // Skip test if no credentials provided
        if (string.IsNullOrEmpty(_testToken))
        {
            Assert.Inconclusive("Test requires GITLAB_TEST_TOKEN environment variable");
            return;
        }

        _logger?.LogInformation("Starting project listing workflow test");

        // Authenticate
        var authService = _serviceProvider!.GetRequiredService<IGitLabAuthenticationService>();
        var connectionOptions = new GitLabConnectionOptions
        {
            PersonalAccessToken = _testToken,
            InstanceUrl = _testGitLabUrl,
            StoreCredentials = false
        };

        await authService.AuthenticateAsync(connectionOptions);

        // List projects
        var projectService = _serviceProvider!.GetRequiredService<IGitLabProjectService>();
        var listOptions = new ProjectListOptions
        {
            MemberOnly = true,
            MaxResults = 10,
            OrderBy = ProjectOrderBy.LastActivity
        };

        var projects = await projectService.ListProjectsAsync(listOptions);
        
        Assert.IsNotNull(projects, "Projects list should not be null");
        Assert.IsTrue(projects.Any(), "Should find at least one project");

        foreach (var project in projects.Take(5))
        {
            Assert.IsTrue(project.Id > 0, "Project ID should be positive");
            Assert.IsFalse(string.IsNullOrEmpty(project.Name), "Project name should not be empty");
            Assert.IsFalse(string.IsNullOrEmpty(project.FullPath), "Project path should not be empty");
            
            _logger?.LogInformation("Found project: {ProjectName} ({ProjectPath})", project.Name, project.FullPath);
        }

        _logger?.LogInformation("Project listing workflow completed successfully");
    }

    [TestMethod]
    public async Task ProjectSearchWorkflow_WithValidCredentials_ShouldFindProjects()
    {
        // Skip test if no credentials provided
        if (string.IsNullOrEmpty(_testToken))
        {
            Assert.Inconclusive("Test requires GITLAB_TEST_TOKEN environment variable");
            return;
        }

        _logger?.LogInformation("Starting project search workflow test");

        // Authenticate
        var authService = _serviceProvider!.GetRequiredService<IGitLabAuthenticationService>();
        var connectionOptions = new GitLabConnectionOptions
        {
            PersonalAccessToken = _testToken,
            InstanceUrl = _testGitLabUrl,
            StoreCredentials = false
        };

        await authService.AuthenticateAsync(connectionOptions);

        // Search projects
        var projectService = _serviceProvider!.GetRequiredService<IGitLabProjectService>();
        var searchResults = await projectService.SearchProjectsAsync("test");
        
        Assert.IsNotNull(searchResults, "Search results should not be null");
        
        if (searchResults.Any())
        {
            var firstProject = searchResults.First();
            Assert.IsTrue(firstProject.Id > 0, "Project ID should be positive");
            Assert.IsFalse(string.IsNullOrEmpty(firstProject.Name), "Project name should not be empty");
            
            _logger?.LogInformation("Search found project: {ProjectName}", firstProject.Name);
        }

        _logger?.LogInformation("Project search workflow completed successfully");
    }

    [TestMethod]
    public async Task ErrorHandlingWorkflow_WithInvalidCredentials_ShouldHandleGracefully()
    {
        _logger?.LogInformation("Starting error handling workflow test");

        var authService = _serviceProvider!.GetRequiredService<IGitLabAuthenticationService>();
        var connectionOptions = new GitLabConnectionOptions
        {
            PersonalAccessToken = "invalid-token",
            InstanceUrl = _testGitLabUrl,
            StoreCredentials = false
        };

        // Should throw authentication exception
        await Assert.ThrowsExceptionAsync<GitLabApiException>(async () =>
        {
            await authService.AuthenticateAsync(connectionOptions);
        });

        _logger?.LogInformation("Error handling workflow completed successfully");
    }

    [TestMethod]
    public async Task FallbackWorkflow_WithUnavailableAPI_ShouldFallbackGracefully()
    {
        _logger?.LogInformation("Starting fallback workflow test");

        var fallbackService = _serviceProvider!.GetRequiredService<IGitLabFallbackService>();
        
        // Test fallback to manual mode
        var canFallback = await fallbackService.CanFallbackToManualModeAsync();
        Assert.IsTrue(canFallback, "Should be able to fallback to manual mode");

        // Test degraded analysis
        var degradedAnalysis = await fallbackService.GetDegradedAnalysisAsync(ProjectType.DotNet);
        Assert.IsNotNull(degradedAnalysis, "Should provide degraded analysis");
        Assert.AreEqual(ProjectType.DotNet, degradedAnalysis.DetectedType, "Should maintain project type");

        _logger?.LogInformation("Fallback workflow completed successfully");
    }

    [TestMethod]
    public async Task PerformanceTest_LargeProjectAnalysis_ShouldCompleteWithinTimeout()
    {
        // Skip test if no credentials provided
        if (string.IsNullOrEmpty(_testToken) || string.IsNullOrEmpty(_testProjectPath))
        {
            Assert.Inconclusive("Test requires GITLAB_TEST_TOKEN and GITLAB_TEST_PROJECT environment variables");
            return;
        }

        _logger?.LogInformation("Starting performance test for large project analysis");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Authenticate
            var authService = _serviceProvider!.GetRequiredService<IGitLabAuthenticationService>();
            var connectionOptions = new GitLabConnectionOptions
            {
                PersonalAccessToken = _testToken,
                InstanceUrl = _testGitLabUrl,
                StoreCredentials = false
            };

            await authService.AuthenticateAsync(connectionOptions);

            // Get project
            var projectService = _serviceProvider!.GetRequiredService<IGitLabProjectService>();
            var project = await projectService.GetProjectAsync(_testProjectPath);

            // Perform analysis with timeout
            var analysisService = _serviceProvider!.GetRequiredService<IProjectAnalysisService>();
            var analysisOptions = new AnalysisOptions
            {
                AnalyzeFiles = true,
                AnalyzeDependencies = true,
                AnalyzeExistingCI = true,
                AnalyzeDeployment = true,
                MaxFileAnalysisDepth = 3,
                IncludeSecurityAnalysis = true
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var analysisResult = await analysisService.AnalyzeProjectAsync(project, analysisOptions);

            stopwatch.Stop();

            Assert.IsNotNull(analysisResult, "Analysis should complete");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 120000, "Analysis should complete within 2 minutes");

            _logger?.LogInformation("Performance test completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            Assert.Fail("Analysis timed out - performance issue detected");
        }
    }

    [TestMethod]
    public async Task ConfigurationManagement_ShouldHandleProfiles()
    {
        _logger?.LogInformation("Starting configuration management test");

        var configService = _serviceProvider!.GetRequiredService<IConfigurationManagementService>();
        var profileService = _serviceProvider!.GetRequiredService<IConfigurationProfileService>();

        // Test profile creation
        var testProfile = new ConfigurationProfile
        {
            Name = "test-profile",
            GitLabUrl = "https://test.gitlab.com",
            Description = "Test profile for integration tests",
            IsDefault = false
        };

        await profileService.SaveProfileAsync(testProfile);

        // Test profile retrieval
        var profiles = await profileService.GetAllProfilesAsync();
        Assert.IsTrue(profiles.Any(p => p.Name == "test-profile"), "Profile should be saved");

        // Test profile deletion
        await profileService.DeleteProfileAsync("test-profile");
        profiles = await profileService.GetAllProfilesAsync();
        Assert.IsFalse(profiles.Any(p => p.Name == "test-profile"), "Profile should be deleted");

        _logger?.LogInformation("Configuration management test completed successfully");
    }
}