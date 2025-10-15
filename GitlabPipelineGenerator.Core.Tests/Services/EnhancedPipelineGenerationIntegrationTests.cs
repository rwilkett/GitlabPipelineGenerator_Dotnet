using FluentAssertions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Services;
using Moq;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

/// <summary>
/// Integration tests for the complete enhanced pipeline generation workflow
/// </summary>
public class EnhancedPipelineGenerationIntegrationTests
{
    private readonly Mock<IPipelineGenerator> _mockBasePipelineGenerator;
    private readonly AnalysisToPipelineMappingService _mappingService;
    private readonly Mock<YamlSerializationService> _mockYamlService;
    private readonly IntelligentPipelineGenerator _intelligentGenerator;

    public EnhancedPipelineGenerationIntegrationTests()
    {
        _mockBasePipelineGenerator = new Mock<IPipelineGenerator>();
        _mappingService = new AnalysisToPipelineMappingService();
        _mockYamlService = new Mock<YamlSerializationService>();
        
        _intelligentGenerator = new IntelligentPipelineGenerator(
            _mockBasePipelineGenerator.Object,
            _mappingService,
            _mockYamlService.Object);
    }

    #region End-to-End Workflow Tests

    [Fact]
    public async Task CompleteWorkflow_DotNetProjectWithAnalysis_GeneratesOptimizedPipeline()
    {
        // Arrange
        var analysisResult = CreateComprehensiveDotNetAnalysis();
        var basePipeline = CreateBaseDotNetPipeline();

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateFromAnalysisAsync(analysisResult);

        // Assert
        result.Should().NotBeNull();
        
        // Verify framework enhancements
        result.Variables.Should().ContainKey("DOTNET_VERSION");
        result.Variables["DOTNET_VERSION"].Should().Be("8.0");
        result.Variables.Should().ContainKey("BUILD_TOOL");
        result.Variables["BUILD_TOOL"].Should().Be("dotnet");
        
        // Verify build enhancements
        var buildJob = result.Jobs["build"];
        buildJob.Script.Should().Contain("dotnet restore");
        buildJob.Script.Should().Contain("dotnet build --configuration Release");
        buildJob.BeforeScript.Should().Contain("dotnet --version");
        
        // Verify test enhancements
        var testJob = result.Jobs["test"];
        testJob.Script.Should().Contain("dotnet test --configuration Release --collect:\"XPlat Code Coverage\"");
        
        // Verify cache enhancements
        buildJob.Cache.Should().NotBeNull();
        buildJob.Cache!.Key.Should().Be("$CI_COMMIT_REF_SLUG-dotnet");
        buildJob.Cache.Paths.Should().Contain("~/.nuget/packages/");
        
        // Verify security enhancements
        result.Jobs.Should().ContainKey("security-sast");
        result.Jobs.Should().ContainKey("security-dependency-scanning");
        
        // Verify Docker enhancements
        result.Jobs.Should().ContainKey("docker-build");
        var dockerJob = result.Jobs["docker-build"];
        dockerJob.Script.Should().Contain(s => s.Contains("docker build"));
        dockerJob.Script.Should().Contain(s => s.Contains("--build-arg BUILD_CONFIGURATION=Release"));
        
        // Verify deployment enhancements
        result.Variables.Should().ContainKey("# Required secret: KUBE_CONFIG");
        result.Variables.Should().ContainKey("# Required secret: DATABASE_URL");
    }

    [Fact]
    public async Task CompleteWorkflow_NodeJsProjectWithAnalysis_GeneratesOptimizedPipeline()
    {
        // Arrange
        var analysisResult = CreateComprehensiveNodeJsAnalysis();
        var basePipeline = CreateBaseNodeJsPipeline();

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateFromAnalysisAsync(analysisResult);

        // Assert
        result.Should().NotBeNull();
        
        // Verify framework enhancements
        result.Variables.Should().ContainKey("NODE_VERSION");
        result.Variables["NODE_VERSION"].Should().Be("18.0");
        result.Variables.Should().ContainKey("PACKAGE_MANAGER");
        result.Variables["PACKAGE_MANAGER"].Should().Be("npm");
        
        // Verify build enhancements
        var buildJob = result.Jobs["build"];
        buildJob.BeforeScript.Should().Contain("node --version");
        buildJob.BeforeScript.Should().Contain("npm --version");
        buildJob.BeforeScript.Should().Contain("npm ci");
        buildJob.Script.Should().Contain("npm run build");
        
        // Verify test enhancements
        var testJob = result.Jobs["test"];
        testJob.Script.Should().Contain("npm test");
        
        // Verify cache enhancements
        buildJob.Cache.Should().NotBeNull();
        buildJob.Cache!.Key.Should().Be("$CI_COMMIT_REF_SLUG-npm");
        buildJob.Cache.Paths.Should().Contain("node_modules/");
        buildJob.Cache.Paths.Should().Contain(".npm/");
    }

    [Fact]
    public async Task CompleteWorkflow_HybridModePreferManual_MergesCorrectly()
    {
        // Arrange
        var analysisResult = CreateComprehensiveDotNetAnalysis();
        var manualOptions = new PipelineOptions
        {
            ProjectType = "nodejs", // Override analysis
            DotNetVersion = "9.0", // Override analysis
            IncludePerformance = true, // Add to analysis
            CustomVariables = new Dictionary<string, string>
            {
                ["MANUAL_VAR"] = "manual_value",
                ["BUILD_CONFIGURATION"] = "Debug" // Override analysis
            },
            RunnerTags = new List<string> { "docker", "linux" }
        };

        var basePipeline = CreateBaseDotNetPipeline();
        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateFromAnalysisAsync(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.PreferManual);

        // Assert
        result.Should().NotBeNull();
        
        // Verify manual options took precedence
        _mockBasePipelineGenerator.Verify(x => x.GenerateAsync(It.Is<PipelineOptions>(o => 
            o.ProjectType == "nodejs" && // Manual override
            o.DotNetVersion == "9.0" && // Manual override
            o.IncludePerformance == true && // Manual addition
            o.CustomVariables.ContainsKey("MANUAL_VAR") &&
            o.CustomVariables["BUILD_CONFIGURATION"] == "Debug" && // Manual override
            o.RunnerTags.Contains("docker") &&
            o.RunnerTags.Contains("linux"))), Times.Once);
        
        // Verify analysis enhancements were still applied where not overridden
        result.Variables.Should().ContainKey("PACKAGE_MANAGER"); // From analysis
        result.Variables["PACKAGE_MANAGER"].Should().Be("nuget");
    }

    [Fact]
    public async Task CompleteWorkflow_HybridModeIntelligentMerge_UsesConfidenceBasedMerging()
    {
        // Arrange
        var analysisResult = CreateComprehensiveDotNetAnalysis();
        analysisResult.Confidence = AnalysisConfidence.High;
        analysisResult.Framework.Confidence = AnalysisConfidence.High;
        analysisResult.BuildConfig.Confidence = AnalysisConfidence.Medium;
        
        var manualOptions = new PipelineOptions
        {
            ProjectType = "nodejs", // Should be overridden by high-confidence analysis
            DotNetVersion = "9.0", // Should be overridden by high-confidence analysis
            IncludeTests = false, // Should be overridden by medium-confidence analysis
            CustomVariables = new Dictionary<string, string>
            {
                ["MANUAL_VAR"] = "manual_value" // Should be preserved
            }
        };

        var basePipeline = CreateBaseDotNetPipeline();
        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateFromAnalysisAsync(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.IntelligentMerge);

        // Assert
        result.Should().NotBeNull();
        
        // Verify high-confidence analysis took precedence
        _mockBasePipelineGenerator.Verify(x => x.GenerateAsync(It.Is<PipelineOptions>(o => 
            o.ProjectType == "dotnet" && // High confidence analysis preferred
            o.DotNetVersion == "8.0" && // High confidence analysis preferred
            o.IncludeTests == true && // Medium confidence analysis preferred
            o.CustomVariables.ContainsKey("MANUAL_VAR"))), Times.Once); // Manual variables preserved
    }

    #endregion

    #region Performance and Optimization Tests

    [Fact]
    public async Task CompleteWorkflow_LargeProjectAnalysis_HandlesComplexityEfficiently()
    {
        // Arrange
        var analysisResult = CreateLargeProjectAnalysis();
        var basePipeline = CreateComplexBasePipeline();

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        var startTime = DateTime.UtcNow;

        // Act
        var result = await _intelligentGenerator.GenerateFromAnalysisAsync(analysisResult);

        // Assert
        var executionTime = DateTime.UtcNow - startTime;
        executionTime.Should().BeLessThan(TimeSpan.FromSeconds(5)); // Should complete quickly
        
        result.Should().NotBeNull();
        result.Jobs.Should().HaveCountGreaterThan(5); // Should handle multiple jobs
        result.Variables.Should().HaveCountGreaterThan(10); // Should handle many variables
    }

    [Fact]
    public async Task CompleteWorkflow_MultipleFrameworkProject_HandlesComplexAnalysis()
    {
        // Arrange
        var analysisResult = CreateMultiFrameworkAnalysis();
        var basePipeline = CreateBaseDotNetPipeline();

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateFromAnalysisAsync(analysisResult);

        // Assert
        result.Should().NotBeNull();
        
        // Should handle primary framework (.NET)
        result.Variables.Should().ContainKey("DOTNET_VERSION");
        result.Variables["DOTNET_VERSION"].Should().Be("8.0");
        
        // Should handle secondary framework (Node.js for frontend)
        result.Variables.Should().ContainKey("NODE_VERSION");
        result.Variables["NODE_VERSION"].Should().Be("18.0");
        
        // Should include jobs for both frameworks
        result.Jobs.Should().ContainKey("build");
        result.Jobs.Should().ContainKey("build-frontend");
    }

    #endregion

    #region Error Handling and Edge Cases

    [Fact]
    public async Task CompleteWorkflow_PartialAnalysisFailure_ContinuesWithAvailableData()
    {
        // Arrange
        var analysisResult = CreatePartialAnalysisResult();
        var basePipeline = CreateBaseDotNetPipeline();

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateFromAnalysisAsync(analysisResult);

        // Assert
        result.Should().NotBeNull();
        
        // Should use available analysis data
        result.Variables.Should().ContainKey("DOTNET_VERSION");
        
        // Should handle missing data gracefully
        result.Jobs.Should().ContainKey("build");
        result.Jobs.Should().NotContainKey("security-sast"); // Security analysis failed
    }

    [Fact]
    public async Task CompleteWorkflow_ConflictingAnalysisData_ResolvesIntelligently()
    {
        // Arrange
        var analysisResult = CreateConflictingAnalysisResult();
        var basePipeline = CreateBaseDotNetPipeline();

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateFromAnalysisAsync(analysisResult);

        // Assert
        result.Should().NotBeNull();
        
        // Should resolve conflicts based on confidence
        result.Variables.Should().ContainKey("DOTNET_VERSION");
        result.Variables["DOTNET_VERSION"].Should().Be("8.0"); // Higher confidence version
        
        // Should include warnings about conflicts
        analysisResult.Warnings.Should().NotBeEmpty();
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public async Task CompleteWorkflow_SerializeToYaml_ProducesValidYaml()
    {
        // Arrange
        var analysisResult = CreateComprehensiveDotNetAnalysis();
        var basePipeline = CreateBaseDotNetPipeline();

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        var expectedYaml = "stages:\n  - build\n  - test\n  - security\n  - deploy\n\nvariables:\n  DOTNET_VERSION: \"8.0\"";
        _mockYamlService.Setup(x => x.SerializePipeline(It.IsAny<PipelineConfiguration>()))
            .Returns(expectedYaml);

        // Act
        var pipeline = await _intelligentGenerator.GenerateFromAnalysisAsync(analysisResult);
        var yaml = _intelligentGenerator.SerializeToYaml(pipeline);

        // Assert
        yaml.Should().NotBeNullOrEmpty();
        yaml.Should().Contain("stages:");
        yaml.Should().Contain("DOTNET_VERSION");
        _mockYamlService.Verify(x => x.SerializePipeline(It.IsAny<PipelineConfiguration>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static ProjectAnalysisResult CreateComprehensiveDotNetAnalysis()
    {
        return new ProjectAnalysisResult
        {
            DetectedType = ProjectType.DotNet,
            Framework = new FrameworkInfo
            {
                Name = ".NET",
                Version = "8.0",
                DetectedFeatures = new List<string> { "web", "api", "test" },
                Configuration = new Dictionary<string, string>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Development",
                    ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "true"
                },
                Confidence = AnalysisConfidence.High
            },
            BuildConfig = new BuildConfiguration
            {
                BuildTool = "dotnet",
                BuildToolVersion = "8.0.100",
                BuildCommands = new List<string> { "dotnet restore", "dotnet build --configuration Release" },
                TestCommands = new List<string> { "dotnet test --configuration Release --collect:\"XPlat Code Coverage\"" },
                ArtifactPaths = new List<string> { "bin/Release/", "TestResults/" },
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["BUILD_CONFIGURATION"] = "Release",
                    ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "true"
                },
                Confidence = AnalysisConfidence.High
            },
            Dependencies = new DependencyInfo
            {
                PackageManager = "nuget",
                TotalDependencies = 25,
                Dependencies = new List<PackageDependency>(),
                DevDependencies = new List<PackageDependency>(),
                Runtime = new RuntimeInfo { Name = ".NET", Version = "8.0" },
                CacheRecommendation = new CacheRecommendation 
                { 
                    IsRecommended = true,
                    CacheKey = "$CI_COMMIT_REF_SLUG-dotnet",
                    CachePaths = new List<string> { "~/.nuget/packages/" }
                },
                SecurityScanRecommendation = new SecurityScanConfiguration 
                { 
                    IsRecommended = true,
                    RecommendedScanners = new List<SecurityScanner>
                    {
                        new SecurityScanner { Name = "SAST", Type = SecurityScanType.SAST },
                        new SecurityScanner { Name = "Dependency Scanning", Type = SecurityScanType.DependencyScanning }
                    }
                },
                HasSecuritySensitiveDependencies = true
            },
            Docker = new DockerConfiguration
            {
                BaseImage = "mcr.microsoft.com/dotnet/sdk:8.0",
                HasDockerfile = true,
                BuildArgs = new Dictionary<string, string>
                {
                    ["BUILD_CONFIGURATION"] = "Release"
                }
            },
            Deployment = new DeploymentInfo
            {
                HasDeploymentConfig = true,
                DeploymentCommands = new List<string> { "kubectl apply -f deployment.yaml" },
                DetectedEnvironments = new List<string> { "staging", "production" },
                RequiredSecrets = new List<string> { "KUBE_CONFIG", "DATABASE_URL" }
            },
            Confidence = AnalysisConfidence.High,
            Warnings = new List<AnalysisWarning>(),
            Recommendations = new List<string> { "Consider adding integration tests", "Enable code coverage reporting" },
            Metadata = new AnalysisMetadata
            {
                AnalyzedAt = DateTime.UtcNow,
                AnalysisVersion = "1.0.0",
                Branch = "main"
            }
        };
    }

    private static ProjectAnalysisResult CreateComprehensiveNodeJsAnalysis()
    {
        return new ProjectAnalysisResult
        {
            DetectedType = ProjectType.NodeJs,
            Framework = new FrameworkInfo
            {
                Name = "Node.js",
                Version = "18.0",
                DetectedFeatures = new List<string> { "web", "api", "test" },
                Configuration = new Dictionary<string, string>
                {
                    ["NODE_ENV"] = "development"
                },
                Confidence = AnalysisConfidence.High
            },
            BuildConfig = new BuildConfiguration
            {
                BuildTool = "npm",
                BuildCommands = new List<string> { "npm run build" },
                TestCommands = new List<string> { "npm test" },
                ArtifactPaths = new List<string> { "dist/", "coverage/" },
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["NODE_ENV"] = "production"
                },
                Confidence = AnalysisConfidence.High
            },
            Dependencies = new DependencyInfo
            {
                PackageManager = "npm",
                TotalDependencies = 50,
                Dependencies = new List<PackageDependency>(),
                DevDependencies = new List<PackageDependency>(),
                Runtime = new RuntimeInfo { Name = "Node.js", Version = "18.0" },
                CacheRecommendation = new CacheRecommendation 
                { 
                    IsRecommended = true,
                    CacheKey = "$CI_COMMIT_REF_SLUG-npm",
                    CachePaths = new List<string> { "node_modules/", ".npm/" }
                },
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

    private static ProjectAnalysisResult CreateLargeProjectAnalysis()
    {
        var analysis = CreateComprehensiveDotNetAnalysis();
        
        // Simulate a large project
        analysis.Dependencies.TotalDependencies = 150;
        analysis.BuildConfig.ArtifactPaths.AddRange(Enumerable.Range(1, 20).Select(i => $"module{i}/bin/"));
        analysis.Framework.DetectedFeatures.AddRange(new[] { "microservices", "messaging", "caching", "monitoring" });
        analysis.Deployment.DetectedEnvironments.AddRange(new[] { "dev", "test", "staging", "production" });
        
        return analysis;
    }

    private static ProjectAnalysisResult CreateMultiFrameworkAnalysis()
    {
        var analysis = CreateComprehensiveDotNetAnalysis();
        
        // Add secondary framework detection
        analysis.Framework.DetectedFeatures.Add("frontend");
        analysis.BuildConfig.BuildCommands.Add("npm run build:frontend");
        analysis.Dependencies.TotalDependencies = 75; // More dependencies due to multiple frameworks
        
        // Add Node.js specific variables
        analysis.Framework.Configuration["NODE_VERSION"] = "18.0";
        analysis.BuildConfig.EnvironmentVariables["NODE_ENV"] = "production";
        
        return analysis;
    }

    private static ProjectAnalysisResult CreatePartialAnalysisResult()
    {
        var analysis = CreateComprehensiveDotNetAnalysis();
        
        // Simulate partial analysis failure
        analysis.Dependencies.SecurityScanRecommendation = new SecurityScanConfiguration { IsRecommended = false };
        analysis.Docker = null; // Docker analysis failed
        analysis.Confidence = AnalysisConfidence.Medium; // Lower confidence due to failures
        
        analysis.Warnings.Add(new AnalysisWarning
        {
            Severity = WarningSeverity.Warning,
            Message = "Docker analysis failed - Dockerfile not accessible",
            Component = "DockerAnalyzer"
        });
        
        return analysis;
    }

    private static ProjectAnalysisResult CreateConflictingAnalysisResult()
    {
        var analysis = CreateComprehensiveDotNetAnalysis();
        
        // Add conflicting data
        analysis.Framework.Version = "8.0"; // High confidence
        analysis.BuildConfig.EnvironmentVariables["DOTNET_VERSION"] = "7.0"; // Conflicting lower confidence
        
        analysis.Warnings.Add(new AnalysisWarning
        {
            Severity = WarningSeverity.Warning,
            Message = "Conflicting .NET versions detected: 8.0 (project file) vs 7.0 (build config)",
            Component = "FrameworkAnalyzer",
            Resolution = "Using project file version (8.0) due to higher confidence"
        });
        
        return analysis;
    }

    private static PipelineConfiguration CreateBaseDotNetPipeline()
    {
        return new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test", "deploy" },
            Jobs = new Dictionary<string, Job>
            {
                ["build"] = new Job 
                { 
                    Stage = "build", 
                    Script = new List<string> { "echo 'Building...'" },
                    BeforeScript = new List<string>(),
                    Variables = new Dictionary<string, object>()
                },
                ["test"] = new Job 
                { 
                    Stage = "test", 
                    Script = new List<string> { "echo 'Testing...'" },
                    BeforeScript = new List<string>(),
                    Variables = new Dictionary<string, object>()
                }
            },
            Variables = new Dictionary<string, object>()
        };
    }

    private static PipelineConfiguration CreateBaseNodeJsPipeline()
    {
        return new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test" },
            Jobs = new Dictionary<string, Job>
            {
                ["build"] = new Job 
                { 
                    Stage = "build", 
                    Script = new List<string> { "echo 'Building...'" },
                    BeforeScript = new List<string>(),
                    Variables = new Dictionary<string, object>()
                },
                ["test"] = new Job 
                { 
                    Stage = "test", 
                    Script = new List<string> { "echo 'Testing...'" },
                    BeforeScript = new List<string>(),
                    Variables = new Dictionary<string, object>()
                }
            },
            Variables = new Dictionary<string, object>()
        };
    }

    private static PipelineConfiguration CreateComplexBasePipeline()
    {
        var pipeline = CreateBaseDotNetPipeline();
        
        // Add more jobs to simulate complexity
        for (int i = 1; i <= 10; i++)
        {
            pipeline.Jobs[$"job{i}"] = new Job
            {
                Stage = i <= 3 ? "build" : i <= 6 ? "test" : "deploy",
                Script = new List<string> { $"echo 'Job {i}'" },
                Variables = new Dictionary<string, object>()
            };
        }
        
        // Add many variables
        for (int i = 1; i <= 20; i++)
        {
            pipeline.Variables[$"VAR_{i}"] = $"value_{i}";
        }
        
        return pipeline;
    }

    #endregion
}