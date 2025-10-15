using FluentAssertions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Services;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

public class AnalysisToPipelineMappingServiceTests
{
    private readonly AnalysisToPipelineMappingService _mappingService;

    public AnalysisToPipelineMappingServiceTests()
    {
        _mappingService = new AnalysisToPipelineMappingService();
    }

    #region MapAnalysisToPipelineOptionsAsync Tests

    [Fact]
    public async Task MapAnalysisToPipelineOptionsAsync_WithDotNetProject_ReturnsCorrectOptions()
    {
        // Arrange
        var analysisResult = CreateDotNetAnalysisResult();

        // Act
        var result = await _mappingService.MapAnalysisToPipelineOptionsAsync(analysisResult);

        // Assert
        result.Should().NotBeNull();
        result.ProjectType.Should().Be("dotnet");
        result.Stages.Should().Contain(new[] { "build", "test" });
        result.IncludeTests.Should().BeTrue();
        result.IncludeCodeQuality.Should().BeTrue();
        result.CustomVariables.Should().ContainKey("DOTNET_VERSION");
        result.CustomVariables["DOTNET_VERSION"].Should().Be("8.0");
    }

    [Fact]
    public async Task MapAnalysisToPipelineOptionsAsync_WithNodeJsProject_ReturnsCorrectOptions()
    {
        // Arrange
        var analysisResult = CreateNodeJsAnalysisResult();

        // Act
        var result = await _mappingService.MapAnalysisToPipelineOptionsAsync(analysisResult);

        // Assert
        result.Should().NotBeNull();
        result.ProjectType.Should().Be("nodejs");
        result.Stages.Should().Contain(new[] { "build", "test" });
        result.IncludeTests.Should().BeTrue();
        result.IncludeCodeQuality.Should().BeTrue();
        result.CustomVariables.Should().ContainKey("NODE_VERSION");
        result.CustomVariables["NODE_VERSION"].Should().Be("18.0");
        result.CustomVariables.Should().ContainKey("PACKAGE_MANAGER");
        result.CustomVariables["PACKAGE_MANAGER"].Should().Be("npm");
    }

    [Fact]
    public async Task MapAnalysisToPipelineOptionsAsync_WithSecurityRecommendations_EnablesSecurity()
    {
        // Arrange
        var analysisResult = CreateAnalysisResultWithSecurity();

        // Act
        var result = await _mappingService.MapAnalysisToPipelineOptionsAsync(analysisResult);

        // Assert
        result.IncludeSecurity.Should().BeTrue();
        result.Stages.Should().Contain("security");
    }

    [Fact]
    public async Task MapAnalysisToPipelineOptionsAsync_WithDeploymentConfig_EnablesDeployment()
    {
        // Arrange
        var analysisResult = CreateAnalysisResultWithDeployment();

        // Act
        var result = await _mappingService.MapAnalysisToPipelineOptionsAsync(analysisResult);

        // Assert
        result.IncludeDeployment.Should().BeTrue();
        result.Stages.Should().Contain("deploy");
        result.DeploymentEnvironments.Should().HaveCount(2);
        result.DeploymentEnvironments.Should().Contain(e => e.Name == "staging");
        result.DeploymentEnvironments.Should().Contain(e => e.Name == "production");
    }

    [Fact]
    public async Task MapAnalysisToPipelineOptionsAsync_WithCacheRecommendation_ConfiguresCache()
    {
        // Arrange
        var analysisResult = CreateAnalysisResultWithCache();

        // Act
        var result = await _mappingService.MapAnalysisToPipelineOptionsAsync(analysisResult);

        // Assert
        result.Cache.Should().NotBeNull();
        result.Cache!.Key.Should().Be("$CI_COMMIT_REF_SLUG-dotnet");
        result.Cache.Paths.Should().Contain("~/.nuget/packages/");
    }

    [Fact]
    public async Task MapAnalysisToPipelineOptionsAsync_WithDockerConfig_SetsDockerImage()
    {
        // Arrange
        var analysisResult = CreateAnalysisResultWithDocker();

        // Act
        var result = await _mappingService.MapAnalysisToPipelineOptionsAsync(analysisResult);

        // Assert
        result.DockerImage.Should().Be("mcr.microsoft.com/dotnet/sdk:8.0");
        result.CustomVariables.Should().ContainKey("BUILD_CONFIGURATION");
        result.CustomVariables["BUILD_CONFIGURATION"].Should().Be("Release");
    }

    #endregion

    #region GetRecommendedTemplatesAsync Tests

    [Fact]
    public async Task GetRecommendedTemplatesAsync_WithDotNetProject_ReturnsCorrectTemplates()
    {
        // Arrange
        var framework = new FrameworkInfo
        {
            Name = ".NET",
            Version = "8.0",
            Confidence = AnalysisConfidence.High
        };

        // Act
        var result = await _mappingService.GetRecommendedTemplatesAsync(ProjectType.DotNet, framework);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(new[] { "dotnet-core", "dotnet-framework", "aspnet-core" });
        result.Should().Contain(new[] { "optimized", "advanced" }); // High confidence templates
    }

    [Fact]
    public async Task GetRecommendedTemplatesAsync_WithReactFramework_ReturnsReactTemplate()
    {
        // Arrange
        var framework = new FrameworkInfo
        {
            Name = "React",
            Version = "18.0",
            Confidence = AnalysisConfidence.Medium
        };

        // Act
        var result = await _mappingService.GetRecommendedTemplatesAsync(ProjectType.NodeJs, framework);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("react");
        result.Should().Contain(new[] { "standard", "balanced" }); // Medium confidence templates
    }

    [Fact]
    public async Task GetRecommendedTemplatesAsync_WithLowConfidence_ReturnsBasicTemplates()
    {
        // Arrange
        var framework = new FrameworkInfo
        {
            Name = "Unknown",
            Confidence = AnalysisConfidence.Low
        };

        // Act
        var result = await _mappingService.GetRecommendedTemplatesAsync(ProjectType.Unknown, framework);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(new[] { "basic", "minimal" }); // Low confidence templates
    }

    #endregion

    #region MapBuildConfigurationToJobsAsync Tests

    [Fact]
    public async Task MapBuildConfigurationToJobsAsync_WithBuildCommands_CreatesBuildJob()
    {
        // Arrange
        var buildConfig = new BuildConfiguration
        {
            BuildCommands = new List<string> { "dotnet restore", "dotnet build --configuration Release" },
            TestCommands = new List<string> { "dotnet test --configuration Release" },
            ArtifactPaths = new List<string> { "bin/", "obj/" }
        };

        // Act
        var result = await _mappingService.MapBuildConfigurationToJobsAsync(buildConfig, ProjectType.DotNet);

        // Assert
        result.Should().ContainKey("build");
        var buildJob = result["build"];
        buildJob.Stage.Should().Be("build");
        buildJob.Script.Should().Contain("dotnet restore");
        buildJob.Script.Should().Contain("dotnet build --configuration Release");
        buildJob.Artifacts.Should().NotBeNull();
        buildJob.Artifacts!.Paths.Should().Contain("bin/");
    }

    [Fact]
    public async Task MapBuildConfigurationToJobsAsync_WithTestCommands_CreatesTestJob()
    {
        // Arrange
        var buildConfig = new BuildConfiguration
        {
            TestCommands = new List<string> { "dotnet test --configuration Release" }
        };

        // Act
        var result = await _mappingService.MapBuildConfigurationToJobsAsync(buildConfig, ProjectType.DotNet);

        // Assert
        result.Should().ContainKey("test");
        var testJob = result["test"];
        testJob.Stage.Should().Be("test");
        testJob.Script.Should().Contain("dotnet test --configuration Release");
        testJob.Artifacts.Should().NotBeNull();
        testJob.Artifacts!.Reports.Should().NotBeNull();
        testJob.Artifacts.Reports!.Junit.Should().Contain("test-results.xml");
    }

    [Fact]
    public async Task MapBuildConfigurationToJobsAsync_WithLintCommands_CreatesLintJob()
    {
        // Arrange
        var buildConfig = new BuildConfiguration
        {
            LintCommands = new List<string> { "eslint src/" }
        };

        // Act
        var result = await _mappingService.MapBuildConfigurationToJobsAsync(buildConfig, ProjectType.NodeJs);

        // Assert
        result.Should().ContainKey("lint");
        var lintJob = result["lint"];
        lintJob.Stage.Should().Be("test");
        lintJob.Script.Should().Contain("eslint src/");
        lintJob.AllowFailure.Should().BeTrue();
    }

    #endregion

    #region MapDependenciesToCacheConfigAsync Tests

    [Fact]
    public async Task MapDependenciesToCacheConfigAsync_WithCacheRecommendation_ReturnsCorrectConfig()
    {
        // Arrange
        var dependencies = new DependencyInfo
        {
            PackageManager = "npm",
            CacheRecommendation = new CacheRecommendation
            {
                IsRecommended = true,
                CacheKey = "$CI_COMMIT_REF_SLUG-npm",
                CachePaths = new List<string> { "node_modules/", ".npm/" }
            }
        };

        // Act
        var result = await _mappingService.MapDependenciesToCacheConfigAsync(dependencies);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("key");
        result["key"].Should().Be("$CI_COMMIT_REF_SLUG-npm");
        result.Should().ContainKey("paths");
        var paths = result["paths"] as List<string>;
        paths.Should().Contain("node_modules/");
        paths.Should().Contain(".npm/");
        result["policy"].Should().Be("pull-push");
    }

    [Fact]
    public async Task MapDependenciesToCacheConfigAsync_WithNuGetPackageManager_AppliesNuGetOptimizations()
    {
        // Arrange
        var dependencies = new DependencyInfo
        {
            PackageManager = "nuget",
            CacheRecommendation = new CacheRecommendation
            {
                IsRecommended = true
            }
        };

        // Act
        var result = await _mappingService.MapDependenciesToCacheConfigAsync(dependencies);

        // Assert
        result.Should().ContainKey("key");
        result["key"].Should().Be("$CI_COMMIT_REF_SLUG-nuget");
        var paths = result["paths"] as List<string>;
        paths.Should().Contain("~/.nuget/packages/");
    }

    [Fact]
    public async Task MapDependenciesToCacheConfigAsync_WithoutRecommendation_ReturnsEmptyConfig()
    {
        // Arrange
        var dependencies = new DependencyInfo
        {
            CacheRecommendation = new CacheRecommendation
            {
                IsRecommended = false
            }
        };

        // Act
        var result = await _mappingService.MapDependenciesToCacheConfigAsync(dependencies);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region MapSecurityConfigToJobsAsync Tests

    [Fact]
    public async Task MapSecurityConfigToJobsAsync_WithSASTScanner_CreatesSASTJob()
    {
        // Arrange
        var securityConfig = new SecurityScanConfiguration
        {
            IsRecommended = true,
            RecommendedScanners = new List<SecurityScanner>
            {
                new SecurityScanner { Name = "SAST", Type = SecurityScanType.SAST }
            }
        };

        // Act
        var result = await _mappingService.MapSecurityConfigToJobsAsync(securityConfig);

        // Assert
        result.Should().ContainKey("security-sast");
        var sastJob = result["security-sast"];
        sastJob.Stage.Should().Be("security");
        sastJob.AllowFailure.Should().BeTrue();
        sastJob.Image!.Name.Should().Contain("semgrep");
        sastJob.Artifacts!.Reports!.Sast.Should().Contain("sast-report.json");
    }

    [Fact]
    public async Task MapSecurityConfigToJobsAsync_WithDependencyScanning_CreatesDependencyScanJob()
    {
        // Arrange
        var securityConfig = new SecurityScanConfiguration
        {
            IsRecommended = true,
            RecommendedScanners = new List<SecurityScanner>
            {
                new SecurityScanner { Name = "Dependency Scanning", Type = SecurityScanType.DependencyScanning }
            }
        };

        // Act
        var result = await _mappingService.MapSecurityConfigToJobsAsync(securityConfig);

        // Assert
        result.Should().ContainKey("security-dependency-scanning");
        var depScanJob = result["security-dependency-scanning"];
        depScanJob.Stage.Should().Be("security");
        depScanJob.AllowFailure.Should().BeTrue();
        depScanJob.Image!.Name.Should().Contain("gemnasium");
        depScanJob.Artifacts!.Reports!.DependencyScanning.Should().Contain("dependency-scanning-report.json");
    }

    [Fact]
    public async Task MapSecurityConfigToJobsAsync_WithoutRecommendation_ReturnsEmptyJobs()
    {
        // Arrange
        var securityConfig = new SecurityScanConfiguration
        {
            IsRecommended = false
        };

        // Act
        var result = await _mappingService.MapSecurityConfigToJobsAsync(securityConfig);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region MapDeploymentConfigToJobsAsync Tests

    [Fact]
    public async Task MapDeploymentConfigToJobsAsync_WithEnvironments_CreatesDeploymentJobs()
    {
        // Arrange
        var deploymentInfo = new DeploymentInfo
        {
            HasDeploymentConfig = true,
            DeploymentCommands = new List<string> { "kubectl apply -f deployment.yaml" }
        };

        var environments = new List<DeploymentEnvironment>
        {
            new DeploymentEnvironment { Name = "staging", IsManual = false },
            new DeploymentEnvironment { Name = "production", IsManual = true }
        };

        // Act
        var result = await _mappingService.MapDeploymentConfigToJobsAsync(deploymentInfo, environments);

        // Assert
        result.Should().ContainKey("deploy-staging");
        result.Should().ContainKey("deploy-production");

        var stagingJob = result["deploy-staging"];
        stagingJob.Stage.Should().Be("deploy");
        stagingJob.Script.Should().Contain("kubectl apply -f deployment.yaml");
        stagingJob.When.Should().BeNull(); // Automatic deployment

        var productionJob = result["deploy-production"];
        productionJob.When.Should().Be("manual"); // Manual deployment
    }

    [Fact]
    public async Task MapDeploymentConfigToJobsAsync_WithoutDeploymentConfig_ReturnsEmptyJobs()
    {
        // Arrange
        var deploymentInfo = new DeploymentInfo
        {
            HasDeploymentConfig = false
        };

        var environments = new List<DeploymentEnvironment>();

        // Act
        var result = await _mappingService.MapDeploymentConfigToJobsAsync(deploymentInfo, environments);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetRecommendedDockerImageAsync Tests

    [Fact]
    public async Task GetRecommendedDockerImageAsync_WithDockerConfig_ReturnsDockerImage()
    {
        // Arrange
        var framework = new FrameworkInfo { Name = ".NET", Version = "8.0" };
        var dockerConfig = new DockerConfiguration
        {
            BaseImage = "mcr.microsoft.com/dotnet/sdk:8.0"
        };

        // Act
        var result = await _mappingService.GetRecommendedDockerImageAsync(framework, dockerConfig);

        // Assert
        result.Should().Be("mcr.microsoft.com/dotnet/sdk:8.0");
    }

    [Fact]
    public async Task GetRecommendedDockerImageAsync_WithDotNetFramework_ReturnsCorrectImage()
    {
        // Arrange
        var framework = new FrameworkInfo { Name = ".NET", Version = "8.0" };

        // Act
        var result = await _mappingService.GetRecommendedDockerImageAsync(framework);

        // Assert
        result.Should().Be("mcr.microsoft.com/dotnet/sdk:8.0");
    }

    [Fact]
    public async Task GetRecommendedDockerImageAsync_WithNodeFramework_ReturnsNodeImage()
    {
        // Arrange
        var framework = new FrameworkInfo { Name = "Node.js", Version = "18" };

        // Act
        var result = await _mappingService.GetRecommendedDockerImageAsync(framework);

        // Assert
        result.Should().Be("node:18-alpine");
    }

    [Fact]
    public async Task GetRecommendedDockerImageAsync_WithUnknownFramework_ReturnsNull()
    {
        // Arrange
        var framework = new FrameworkInfo { Name = "Unknown Framework" };

        // Act
        var result = await _mappingService.GetRecommendedDockerImageAsync(framework);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region MapConfidenceToStrategyAsync Tests

    [Fact]
    public async Task MapConfidenceToStrategyAsync_WithHighConfidence_ReturnsAggressive()
    {
        // Act
        var result = await _mappingService.MapConfidenceToStrategyAsync(AnalysisConfidence.High);

        // Assert
        result.Should().Be(PipelineGenerationStrategy.Aggressive);
    }

    [Fact]
    public async Task MapConfidenceToStrategyAsync_WithMediumConfidence_ReturnsBalanced()
    {
        // Act
        var result = await _mappingService.MapConfidenceToStrategyAsync(AnalysisConfidence.Medium);

        // Assert
        result.Should().Be(PipelineGenerationStrategy.Balanced);
    }

    [Fact]
    public async Task MapConfidenceToStrategyAsync_WithLowConfidence_ReturnsConservative()
    {
        // Act
        var result = await _mappingService.MapConfidenceToStrategyAsync(AnalysisConfidence.Low);

        // Assert
        result.Should().Be(PipelineGenerationStrategy.Conservative);
    }

    #endregion

    #region Helper Methods

    private static ProjectAnalysisResult CreateDotNetAnalysisResult()
    {
        return new ProjectAnalysisResult
        {
            DetectedType = ProjectType.DotNet,
            Framework = new FrameworkInfo
            {
                Name = ".NET",
                Version = "8.0",
                DetectedFeatures = new List<string> { "web", "api" },
                Configuration = new Dictionary<string, string>(),
                Confidence = AnalysisConfidence.High
            },
            BuildConfig = new BuildConfiguration
            {
                BuildTool = "dotnet",
                BuildToolVersion = "8.0",
                BuildCommands = new List<string> { "dotnet build" },
                TestCommands = new List<string> { "dotnet test" },
                ArtifactPaths = new List<string> { "bin/", "obj/" },
                EnvironmentVariables = new Dictionary<string, string>(),
                Confidence = AnalysisConfidence.High
            },
            Dependencies = new DependencyInfo
            {
                PackageManager = "nuget",
                TotalDependencies = 10,
                Dependencies = new List<PackageDependency>(),
                DevDependencies = new List<PackageDependency>(),
                Runtime = new RuntimeInfo { Name = ".NET", Version = "8.0" },
                CacheRecommendation = new CacheRecommendation { IsRecommended = false },
                SecurityScanRecommendation = new SecurityScanConfiguration { IsRecommended = false },
                HasSecuritySensitiveDependencies = false
            },
            Deployment = new DeploymentInfo
            {
                HasDeploymentConfig = false,
                DeploymentCommands = new List<string>(),
                DetectedEnvironments = new List<string>(),
                RequiredSecrets = new List<string>()
            },
            Confidence = AnalysisConfidence.High,
            Warnings = new List<AnalysisWarning>(),
            Recommendations = new List<string>(),
            Metadata = new AnalysisMetadata()
        };
    }

    private static ProjectAnalysisResult CreateNodeJsAnalysisResult()
    {
        return new ProjectAnalysisResult
        {
            DetectedType = ProjectType.NodeJs,
            Framework = new FrameworkInfo
            {
                Name = "Node.js",
                Version = "18.0",
                DetectedFeatures = new List<string> { "web", "api" },
                Configuration = new Dictionary<string, string>(),
                Confidence = AnalysisConfidence.High
            },
            BuildConfig = new BuildConfiguration
            {
                BuildTool = "npm",
                BuildCommands = new List<string> { "npm run build" },
                TestCommands = new List<string> { "npm test" },
                ArtifactPaths = new List<string> { "dist/" },
                EnvironmentVariables = new Dictionary<string, string>(),
                Confidence = AnalysisConfidence.High
            },
            Dependencies = new DependencyInfo
            {
                PackageManager = "npm",
                TotalDependencies = 25,
                Dependencies = new List<PackageDependency>(),
                DevDependencies = new List<PackageDependency>(),
                Runtime = new RuntimeInfo { Name = "Node.js", Version = "18.0" },
                CacheRecommendation = new CacheRecommendation { IsRecommended = false },
                SecurityScanRecommendation = new SecurityScanConfiguration { IsRecommended = false },
                HasSecuritySensitiveDependencies = false
            },
            Deployment = new DeploymentInfo
            {
                HasDeploymentConfig = false,
                DeploymentCommands = new List<string>(),
                DetectedEnvironments = new List<string>(),
                RequiredSecrets = new List<string>()
            },
            Confidence = AnalysisConfidence.High,
            Warnings = new List<AnalysisWarning>(),
            Recommendations = new List<string>(),
            Metadata = new AnalysisMetadata()
        };
    }

    private static ProjectAnalysisResult CreateAnalysisResultWithSecurity()
    {
        var result = CreateDotNetAnalysisResult();
        result.Dependencies.SecurityScanRecommendation = new SecurityScanConfiguration
        {
            IsRecommended = true,
            RecommendedScanners = new List<SecurityScanner>
            {
                new SecurityScanner { Name = "SAST", Type = SecurityScanType.SAST }
            }
        };
        return result;
    }

    private static ProjectAnalysisResult CreateAnalysisResultWithDeployment()
    {
        var result = CreateDotNetAnalysisResult();
        result.Deployment = new DeploymentInfo
        {
            HasDeploymentConfig = true,
            DeploymentCommands = new List<string> { "kubectl apply -f deployment.yaml" },
            DetectedEnvironments = new List<string> { "staging", "production" },
            RequiredSecrets = new List<string> { "KUBE_CONFIG" }
        };
        return result;
    }

    private static ProjectAnalysisResult CreateAnalysisResultWithCache()
    {
        var result = CreateDotNetAnalysisResult();
        result.Dependencies.CacheRecommendation = new CacheRecommendation
        {
            IsRecommended = true,
            CacheKey = "$CI_COMMIT_REF_SLUG-dotnet",
            CachePaths = new List<string> { "~/.nuget/packages/" }
        };
        return result;
    }

    private static ProjectAnalysisResult CreateAnalysisResultWithDocker()
    {
        var result = CreateDotNetAnalysisResult();
        result.Docker = new DockerConfiguration
        {
            BaseImage = "mcr.microsoft.com/dotnet/sdk:8.0",
            HasDockerfile = true,
            BuildArgs = new Dictionary<string, string>
            {
                ["BUILD_CONFIGURATION"] = "Release"
            }
        };
        return result;
    }

    #endregion
}