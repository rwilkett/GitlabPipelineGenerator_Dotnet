using FluentAssertions;
using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Services;
using Moq;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

public class IntelligentPipelineGeneratorTests
{
    private readonly Mock<IPipelineGenerator> _mockBasePipelineGenerator;
    private readonly Mock<IAnalysisToPipelineMappingService> _mockMappingService;
    private readonly Mock<YamlSerializationService> _mockYamlService;
    private readonly IntelligentPipelineGenerator _intelligentGenerator;

    public IntelligentPipelineGeneratorTests()
    {
        _mockBasePipelineGenerator = new Mock<IPipelineGenerator>();
        _mockMappingService = new Mock<IAnalysisToPipelineMappingService>();
        _mockYamlService = new Mock<YamlSerializationService>();
        
        _intelligentGenerator = new IntelligentPipelineGenerator(
            _mockBasePipelineGenerator.Object,
            _mockMappingService.Object,
            _mockYamlService.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullBasePipelineGenerator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new IntelligentPipelineGenerator(
            null!,
            _mockMappingService.Object,
            _mockYamlService.Object));
    }

    [Fact]
    public void Constructor_WithNullMappingService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new IntelligentPipelineGenerator(
            _mockBasePipelineGenerator.Object,
            null!,
            _mockYamlService.Object));
    }

    [Fact]
    public void Constructor_WithNullYamlService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new IntelligentPipelineGenerator(
            _mockBasePipelineGenerator.Object,
            _mockMappingService.Object,
            null!));
    }

    #endregion

    #region GenerateAsync Tests

    [Fact]
    public async Task GenerateAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _intelligentGenerator.GenerateAsync(null!));
        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public async Task GenerateAsync_WithoutAnalysisResult_CallsBasePipelineGenerator()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build", "test" },
            UseAnalysisDefaults = false
        };

        var expectedPipeline = new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test" },
            Jobs = new Dictionary<string, Job>
            {
                ["build"] = new Job { Stage = "build", Script = new List<string> { "dotnet build" } }
            }
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(options))
            .ReturnsAsync(expectedPipeline);

        // Act
        var result = await _intelligentGenerator.GenerateAsync(options);

        // Assert
        result.Should().Be(expectedPipeline);
        _mockBasePipelineGenerator.Verify(x => x.GenerateAsync(options), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_WithAnalysisResultButUseAnalysisDefaultsFalse_CallsBasePipelineGenerator()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build", "test" },
            AnalysisResult = analysisResult,
            UseAnalysisDefaults = false
        };

        var expectedPipeline = new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test" },
            Jobs = new Dictionary<string, Job>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(options))
            .ReturnsAsync(expectedPipeline);

        // Act
        var result = await _intelligentGenerator.GenerateAsync(options);

        // Assert
        result.Should().Be(expectedPipeline);
        _mockBasePipelineGenerator.Verify(x => x.GenerateAsync(options), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_WithAnalysisResultAndUseAnalysisDefaultsTrue_GeneratesIntelligentPipeline()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build", "test" },
            AnalysisResult = analysisResult,
            UseAnalysisDefaults = true
        };

        var basePipeline = new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test" },
            Jobs = new Dictionary<string, Job>
            {
                ["build"] = new Job { Stage = "build", Script = new List<string> { "echo 'Building...'" } }
            },
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(options))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateAsync(options);

        // Assert
        result.Should().NotBeNull();
        result.Variables.Should().ContainKey("DOTNET_VERSION");
        result.Variables["DOTNET_VERSION"].Should().Be("8.0");
        result.Variables.Should().ContainKey("BUILD_TOOL");
        result.Variables["BUILD_TOOL"].Should().Be("dotnet");
        _mockBasePipelineGenerator.Verify(x => x.GenerateAsync(options), Times.Once);
    }

    #endregion

    #region GenerateFromAnalysisAsync Tests

    [Fact]
    public async Task GenerateFromAnalysisAsync_WithNullAnalysisResult_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _intelligentGenerator.GenerateFromAnalysisAsync(null!));
        Assert.Equal("analysisResult", ex.ParamName);
    }

    [Fact]
    public async Task GenerateFromAnalysisAsync_WithValidAnalysisResult_GeneratesIntelligentPipeline()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        var basePipeline = new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test" },
            Jobs = new Dictionary<string, Job>
            {
                ["build"] = new Job { Stage = "build", Script = new List<string> { "echo 'Building...'" } }
            },
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateFromAnalysisAsync(analysisResult);

        // Assert
        result.Should().NotBeNull();
        result.Variables.Should().ContainKey("DOTNET_VERSION");
        result.Variables["DOTNET_VERSION"].Should().Be("8.0");
        _mockBasePipelineGenerator.Verify(x => x.GenerateAsync(It.IsAny<PipelineOptions>()), Times.Once);
    }

    [Fact]
    public async Task GenerateFromAnalysisAsync_WithManualOptionsPreferManual_PrefersManualSettings()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        var manualOptions = new PipelineOptions
        {
            ProjectType = "nodejs", // Different from analysis
            DotNetVersion = "9.0", // Different from analysis
            CustomVariables = new Dictionary<string, string> { ["MANUAL_VAR"] = "manual_value" }
        };

        var basePipeline = new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test" },
            Jobs = new Dictionary<string, Job>(),
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateFromAnalysisAsync(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.PreferManual);

        // Assert
        result.Should().NotBeNull();
        _mockBasePipelineGenerator.Verify(x => x.GenerateAsync(It.Is<PipelineOptions>(o => 
            o.ProjectType == "nodejs" && 
            o.DotNetVersion == "9.0" &&
            o.CustomVariables.ContainsKey("MANUAL_VAR"))), Times.Once);
    }

    [Fact]
    public async Task GenerateFromAnalysisAsync_WithExceptionInGeneration_ThrowsPipelineGenerationException()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        var innerException = new InvalidOperationException("Test exception");

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ThrowsAsync(innerException);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<PipelineGenerationException>(() => 
            _intelligentGenerator.GenerateFromAnalysisAsync(analysisResult));
        
        ex.Message.Should().Contain("Failed to generate pipeline from analysis");
        ex.InnerException.Should().Be(innerException);
    }

    #endregion

    #region Framework Enhancement Tests

    [Fact]
    public async Task GenerateAsync_WithDotNetFramework_AddsFrameworkSpecificEnhancements()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.Framework.Name = ".NET";
        analysisResult.Framework.Version = "8.0";
        analysisResult.Framework.Configuration["ASPNETCORE_ENVIRONMENT"] = "Development";

        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            AnalysisResult = analysisResult,
            UseAnalysisDefaults = true
        };

        var basePipeline = new PipelineConfiguration
        {
            Jobs = new Dictionary<string, Job>
            {
                ["build"] = new Job { Stage = "build", Script = new List<string> { "echo 'Building...'" } }
            },
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(options))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateAsync(options);

        // Assert
        result.Variables.Should().ContainKey("DOTNET_VERSION");
        result.Variables["DOTNET_VERSION"].Should().Be("8.0");
        result.Variables.Should().ContainKey("ASPNETCORE_ENVIRONMENT");
        result.Variables["ASPNETCORE_ENVIRONMENT"].Should().Be("Development");
        
        var buildJob = result.Jobs["build"];
        buildJob.BeforeScript.Should().Contain("dotnet --version");
    }

    [Fact]
    public async Task GenerateAsync_WithNodeJsFramework_AddsNodeSpecificEnhancements()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.DetectedType = ProjectType.NodeJs;
        analysisResult.Framework.Name = "Node.js";
        analysisResult.Framework.Version = "18.0";
        analysisResult.Dependencies.PackageManager = "npm";

        var options = new PipelineOptions
        {
            ProjectType = "nodejs",
            AnalysisResult = analysisResult,
            UseAnalysisDefaults = true
        };

        var basePipeline = new PipelineConfiguration
        {
            Jobs = new Dictionary<string, Job>
            {
                ["build"] = new Job { Stage = "build", Script = new List<string> { "echo 'Building...'" } }
            },
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(options))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateAsync(options);

        // Assert
        result.Variables.Should().ContainKey("NODE_VERSION");
        result.Variables["NODE_VERSION"].Should().Be("18.0");
        result.Variables.Should().ContainKey("PACKAGE_MANAGER");
        result.Variables["PACKAGE_MANAGER"].Should().Be("npm");
        
        var buildJob = result.Jobs["build"];
        buildJob.BeforeScript.Should().Contain("node --version");
        buildJob.BeforeScript.Should().Contain("npm --version");
        buildJob.BeforeScript.Should().Contain("npm ci");
    }

    #endregion

    #region Build Enhancement Tests

    [Fact]
    public async Task GenerateAsync_WithDetectedBuildCommands_ReplacesBuildScript()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.BuildConfig.BuildCommands = new List<string> { "dotnet restore", "dotnet build --configuration Release" };
        analysisResult.BuildConfig.TestCommands = new List<string> { "dotnet test --configuration Release" };

        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            AnalysisResult = analysisResult,
            UseAnalysisDefaults = true
        };

        var basePipeline = new PipelineConfiguration
        {
            Jobs = new Dictionary<string, Job>
            {
                ["build"] = new Job { Stage = "build", Script = new List<string> { "echo 'Generic build'" } },
                ["test"] = new Job { Stage = "test", Script = new List<string> { "echo 'Generic test'" } }
            },
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(options))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateAsync(options);

        // Assert
        var buildJob = result.Jobs["build"];
        buildJob.Script.Should().Contain("dotnet restore");
        buildJob.Script.Should().Contain("dotnet build --configuration Release");
        
        var testJob = result.Jobs["test"];
        testJob.Script.Should().Contain("dotnet test --configuration Release");
    }

    [Fact]
    public async Task GenerateAsync_WithBuildEnvironmentVariables_AddsVariablesToPipeline()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.BuildConfig.EnvironmentVariables = new Dictionary<string, string>
        {
            ["BUILD_CONFIGURATION"] = "Release",
            ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "true"
        };

        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            AnalysisResult = analysisResult,
            UseAnalysisDefaults = true
        };

        var basePipeline = new PipelineConfiguration
        {
            Jobs = new Dictionary<string, Job>(),
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(options))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateAsync(options);

        // Assert
        result.Variables.Should().ContainKey("BUILD_CONFIGURATION");
        result.Variables["BUILD_CONFIGURATION"].Should().Be("Release");
        result.Variables.Should().ContainKey("DOTNET_SKIP_FIRST_TIME_EXPERIENCE");
        result.Variables["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"].Should().Be("true");
    }

    #endregion

    #region Docker Enhancement Tests

    [Fact]
    public async Task GenerateAsync_WithDockerConfiguration_AddsDockerEnhancements()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.Docker = new DockerConfiguration
        {
            BaseImage = "mcr.microsoft.com/dotnet/sdk:8.0",
            HasDockerConfig = true,
            BuildArgs = new Dictionary<string, string>
            {
                ["BUILD_CONFIGURATION"] = "Release"
            }
        };

        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            AnalysisResult = analysisResult,
            UseAnalysisDefaults = true
        };

        var basePipeline = new PipelineConfiguration
        {
            Jobs = new Dictionary<string, Job>
            {
                ["build"] = new Job { Stage = "build", Script = new List<string> { "echo 'Building...'" } }
            },
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(options))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateAsync(options);

        // Assert
        result.Variables.Should().ContainKey("BUILD_CONFIGURATION");
        result.Variables["BUILD_CONFIGURATION"].Should().Be("Release");
        
        var buildJob = result.Jobs["build"];
        buildJob.Image?.Name.Should().Be("mcr.microsoft.com/dotnet/sdk:8.0");
        
        result.Jobs.Should().ContainKey("docker-build");
        var dockerJob = result.Jobs["docker-build"];
        dockerJob.Stage.Should().Be("build");
        dockerJob.Script.Should().Contain(s => s.Contains("docker build"));
    }

    #endregion

    #region Cache Enhancement Tests

    [Fact]
    public async Task GenerateAsync_WithCacheRecommendation_AppliesCacheToJobs()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.Dependencies.CacheRecommendation = new CacheRecommendation
        {
            IsRecommended = true,
            Configuration = new CacheConfiguration
            {
                CacheKey = "$CI_COMMIT_REF_SLUG-dotnet",
                CachePaths = new List<string> { "~/.nuget/packages/" }
            }
        };

        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            AnalysisResult = analysisResult,
            UseAnalysisDefaults = true
        };

        var basePipeline = new PipelineConfiguration
        {
            Jobs = new Dictionary<string, Job>
            {
                ["build"] = new Job { Stage = "build", Script = new List<string> { "dotnet build" } },
                ["test"] = new Job { Stage = "test", Script = new List<string> { "dotnet test" } }
            },
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(options))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateAsync(options);

        // Assert
        var buildJob = result.Jobs["build"];
        buildJob.Cache.Should().NotBeNull();
        buildJob.Cache!.Key.Should().Be("$CI_COMMIT_REF_SLUG-dotnet");
        buildJob.Cache.Paths.Should().Contain("~/.nuget/packages/");
        buildJob.Cache.Policy.Should().Be("pull-push");
        
        var testJob = result.Jobs["test"];
        testJob.Cache.Should().NotBeNull();
    }

    #endregion

    #region Security Enhancement Tests

    [Fact]
    public async Task GenerateAsync_WithSecurityScanRecommendation_AddsSecurityJobs()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.Dependencies.SecurityScanRecommendation = new SecurityScanConfiguration
        {
            IsRecommended = true,
            RecommendedScanners = new List<SecurityScanner>
            {
                new SecurityScanner { Name = "SAST", Type = SecurityScanType.SAST },
                new SecurityScanner { Name = "Dependency Scanning", Type = SecurityScanType.DependencyScanning }
            }
        };

        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            AnalysisResult = analysisResult,
            UseAnalysisDefaults = true
        };

        var basePipeline = new PipelineConfiguration
        {
            Jobs = new Dictionary<string, Job>(),
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(options))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateAsync(options);

        // Assert
        result.Jobs.Should().ContainKey("security-sast");
        result.Jobs.Should().ContainKey("security-dependency-scanning");
        
        var sastJob = result.Jobs["security-sast"];
        sastJob.Stage.Should().Be("security");
        sastJob.AllowFailure.Should().BeTrue();
        sastJob.Image?.Name.Should().Contain("semgrep");
        
        var depScanJob = result.Jobs["security-dependency-scanning"];
        depScanJob.Stage.Should().Be("security");
        depScanJob.AllowFailure.Should().BeTrue();
        depScanJob.Image?.Name.Should().Contain("gemnasium");
    }

    #endregion

    #region Deployment Enhancement Tests

    [Fact]
    public async Task GenerateAsync_WithDeploymentConfiguration_EnhancesDeploymentJobs()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.Deployment = new DeploymentInfo
        {
            HasDeploymentConfig = true,
            DeploymentCommands = new List<string> { "kubectl apply -f deployment.yaml" },
            DetectedEnvironments = new List<string> { "staging", "production" },
            RequiredSecrets = new List<string> { "KUBE_CONFIG", "DATABASE_URL" }
        };

        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            AnalysisResult = analysisResult,
            UseAnalysisDefaults = true
        };

        var basePipeline = new PipelineConfiguration
        {
            Jobs = new Dictionary<string, Job>
            {
                ["deploy"] = new Job { Stage = "deploy", Script = new List<string> { "echo 'Deploying...'" } }
            },
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(options))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateAsync(options);

        // Assert
        var deployJob = result.Jobs["deploy"];
        deployJob.Script.Should().Contain("kubectl apply -f deployment.yaml");
        
        result.Variables.Should().ContainKey("# Required secret: KUBE_CONFIG");
        result.Variables.Should().ContainKey("# Required secret: DATABASE_URL");
    }

    #endregion

    #region SerializeToYaml Tests

    [Fact]
    public void SerializeToYaml_WithNullPipeline_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _intelligentGenerator.SerializeToYaml(null!));
        Assert.Equal("pipeline", ex.ParamName);
    }

    [Fact]
    public void SerializeToYaml_WithValidPipeline_CallsYamlService()
    {
        // Arrange
        var pipeline = new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test" },
            Jobs = new Dictionary<string, Job>()
        };

        var expectedYaml = "stages:\n  - build\n  - test";
        _mockYamlService.Setup(x => x.SerializePipeline(pipeline))
            .Returns(expectedYaml);

        // Act
        var result = _intelligentGenerator.SerializeToYaml(pipeline);

        // Assert
        result.Should().Be(expectedYaml);
        _mockYamlService.Verify(x => x.SerializePipeline(pipeline), Times.Once);
    }

    #endregion

    #region Hybrid Mode Configuration Merging Tests

    [Fact]
    public async Task GenerateFromAnalysisAsync_WithPreferManualStrategy_PrefersManualOverAnalysis()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.Framework.Version = "8.0"; // Analysis suggests 8.0
        
        var manualOptions = new PipelineOptions
        {
            ProjectType = "nodejs", // Manual overrides to nodejs
            DotNetVersion = "9.0", // Manual overrides version
            IncludeTests = false, // Manual disables tests
            CustomVariables = new Dictionary<string, string>
            {
                ["MANUAL_VAR"] = "manual_value",
                ["DOTNET_VERSION"] = "9.0" // Manual override
            }
        };

        var basePipeline = new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test" },
            Jobs = new Dictionary<string, Job>(),
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateFromAnalysisAsync(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.PreferManual);

        // Assert
        result.Should().NotBeNull();
        
        // Verify that manual options took precedence
        _mockBasePipelineGenerator.Verify(x => x.GenerateAsync(It.Is<PipelineOptions>(o => 
            o.ProjectType == "nodejs" && 
            o.DotNetVersion == "9.0" &&
            o.IncludeTests == false &&
            o.CustomVariables.ContainsKey("MANUAL_VAR"))), Times.Once);
    }

    [Fact]
    public async Task GenerateFromAnalysisAsync_WithPreferAnalysisStrategy_PrefersAnalysisOverManual()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.Framework.Version = "8.0";
        analysisResult.BuildConfig.TestCommands = new List<string> { "dotnet test" };
        
        var manualOptions = new PipelineOptions
        {
            ProjectType = "nodejs", // Manual suggests nodejs
            DotNetVersion = "9.0", // Manual suggests different version
            IncludeTests = false, // Manual disables tests
            CustomVariables = new Dictionary<string, string>
            {
                ["MANUAL_VAR"] = "manual_value"
            }
        };

        var basePipeline = new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test" },
            Jobs = new Dictionary<string, Job>(),
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateFromAnalysisAsync(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.PreferAnalysis);

        // Assert
        result.Should().NotBeNull();
        
        // Verify that analysis options took precedence for core settings
        _mockBasePipelineGenerator.Verify(x => x.GenerateAsync(It.Is<PipelineOptions>(o => 
            o.ProjectType == "dotnet" && // Analysis type preferred
            o.IncludeTests == true && // Analysis enables tests
            o.CustomVariables.ContainsKey("MANUAL_VAR"))), Times.Once); // Manual variables still added
    }

    [Fact]
    public async Task GenerateFromAnalysisAsync_WithIntelligentMergeHighConfidence_PrefersAnalysis()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.Confidence = AnalysisConfidence.High;
        analysisResult.Framework.Confidence = AnalysisConfidence.High;
        
        var manualOptions = new PipelineOptions
        {
            ProjectType = "nodejs",
            DotNetVersion = "9.0"
        };

        var basePipeline = new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test" },
            Jobs = new Dictionary<string, Job>(),
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateFromAnalysisAsync(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.IntelligentMerge);

        // Assert
        result.Should().NotBeNull();
        
        // With high confidence, analysis should be preferred
        _mockBasePipelineGenerator.Verify(x => x.GenerateAsync(It.Is<PipelineOptions>(o => 
            o.ProjectType == "dotnet")), Times.Once); // Analysis type preferred due to high confidence
    }

    [Fact]
    public async Task GenerateFromAnalysisAsync_WithIntelligentMergeLowConfidence_PrefersManual()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.Confidence = AnalysisConfidence.Low;
        analysisResult.Framework.Confidence = AnalysisConfidence.Low;
        
        var manualOptions = new PipelineOptions
        {
            ProjectType = "nodejs",
            DotNetVersion = "9.0"
        };

        var basePipeline = new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test" },
            Jobs = new Dictionary<string, Job>(),
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateFromAnalysisAsync(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.IntelligentMerge);

        // Assert
        result.Should().NotBeNull();
        
        // With low confidence, manual should be preferred
        _mockBasePipelineGenerator.Verify(x => x.GenerateAsync(It.Is<PipelineOptions>(o => 
            o.ProjectType == "nodejs" && // Manual type preferred due to low confidence
            o.DotNetVersion == "9.0")), Times.Once);
    }

    [Fact]
    public async Task GenerateFromAnalysisAsync_WithAnalysisOnlyStrategy_IgnoresManualOptions()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        
        var manualOptions = new PipelineOptions
        {
            ProjectType = "nodejs",
            DotNetVersion = "9.0",
            CustomVariables = new Dictionary<string, string>
            {
                ["MANUAL_VAR"] = "manual_value"
            }
        };

        var basePipeline = new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test" },
            Jobs = new Dictionary<string, Job>(),
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateFromAnalysisAsync(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.AnalysisOnly);

        // Assert
        result.Should().NotBeNull();
        
        // Only analysis options should be used
        _mockBasePipelineGenerator.Verify(x => x.GenerateAsync(It.Is<PipelineOptions>(o => 
            o.ProjectType == "dotnet" && // Analysis type only
            !o.CustomVariables.ContainsKey("MANUAL_VAR"))), Times.Once); // Manual variables ignored
    }

    [Fact]
    public async Task GenerateFromAnalysisAsync_WithManualOnlyStrategy_IgnoresAnalysisResults()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        
        var manualOptions = new PipelineOptions
        {
            ProjectType = "nodejs",
            DotNetVersion = "9.0",
            IncludeTests = false
        };

        var basePipeline = new PipelineConfiguration
        {
            Stages = new List<string> { "build" },
            Jobs = new Dictionary<string, Job>(),
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(It.IsAny<PipelineOptions>()))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateFromAnalysisAsync(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.ManualOnly);

        // Assert
        result.Should().NotBeNull();
        
        // Only manual options should be used
        _mockBasePipelineGenerator.Verify(x => x.GenerateAsync(It.Is<PipelineOptions>(o => 
            o.ProjectType == "nodejs" && // Manual type only
            o.DotNetVersion == "9.0" &&
            o.IncludeTests == false &&
            !o.UseAnalysisDefaults)), Times.Once); // Analysis defaults disabled
    }

    #endregion

    #region Intelligent Defaults and Override Behavior Tests

    [Fact]
    public async Task GenerateAsync_WithAnalysisBasedPipelineOptions_AppliesIntelligentDefaults()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.Framework.Version = "8.0";
        analysisResult.BuildConfig.BuildTool = "dotnet";
        analysisResult.Dependencies.PackageManager = "nuget";
        
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(analysisResult);

        var basePipeline = new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test" },
            Jobs = new Dictionary<string, Job>
            {
                ["build"] = new Job { Stage = "build", Script = new List<string> { "echo 'Building...'" } }
            },
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(options))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateAsync(options);

        // Assert
        result.Should().NotBeNull();
        result.Variables.Should().ContainKey("DOTNET_VERSION");
        result.Variables["DOTNET_VERSION"].Should().Be("8.0");
        result.Variables.Should().ContainKey("BUILD_TOOL");
        result.Variables["BUILD_TOOL"].Should().Be("dotnet");
        result.Variables.Should().ContainKey("PACKAGE_MANAGER");
        result.Variables["PACKAGE_MANAGER"].Should().Be("nuget");
    }

    [Fact]
    public async Task GenerateAsync_WithFrameworkSpecificConfiguration_AppliesFrameworkVariables()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.Framework.Configuration = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "true"
        };
        
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            AnalysisResult = analysisResult,
            UseAnalysisDefaults = true
        };

        var basePipeline = new PipelineConfiguration
        {
            Jobs = new Dictionary<string, Job>(),
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(options))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateAsync(options);

        // Assert
        result.Variables.Should().ContainKey("ASPNETCORE_ENVIRONMENT");
        result.Variables["ASPNETCORE_ENVIRONMENT"].Should().Be("Development");
        result.Variables.Should().ContainKey("DOTNET_SKIP_FIRST_TIME_EXPERIENCE");
        result.Variables["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"].Should().Be("true");
    }

    [Fact]
    public async Task GenerateAsync_WithConflictingManualAndAnalysisVariables_ManualTakesPrecedence()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.Framework.Version = "8.0";
        analysisResult.BuildConfig.EnvironmentVariables = new Dictionary<string, string>
        {
            ["BUILD_CONFIGURATION"] = "Debug" // Analysis suggests Debug
        };
        
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            AnalysisResult = analysisResult,
            UseAnalysisDefaults = true,
            CustomVariables = new Dictionary<string, string>
            {
                ["BUILD_CONFIGURATION"] = "Release" // Manual overrides to Release
            }
        };

        var basePipeline = new PipelineConfiguration
        {
            Jobs = new Dictionary<string, Job>(),
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(options))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateAsync(options);

        // Assert
        result.Variables.Should().ContainKey("BUILD_CONFIGURATION");
        result.Variables["BUILD_CONFIGURATION"].Should().Be("Release"); // Manual value should win
    }

    [Fact]
    public async Task GenerateAsync_WithMultipleFrameworkDetection_AppliesCorrectEnhancements()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.DetectedType = ProjectType.NodeJs;
        analysisResult.Framework.Name = "Node.js";
        analysisResult.Framework.Version = "18.0";
        analysisResult.Dependencies.PackageManager = "yarn";
        
        var options = new PipelineOptions
        {
            ProjectType = "nodejs",
            AnalysisResult = analysisResult,
            UseAnalysisDefaults = true
        };

        var basePipeline = new PipelineConfiguration
        {
            Jobs = new Dictionary<string, Job>
            {
                ["build"] = new Job { Stage = "build", Script = new List<string> { "echo 'Building...'" } }
            },
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(options))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateAsync(options);

        // Assert
        result.Variables.Should().ContainKey("NODE_VERSION");
        result.Variables["NODE_VERSION"].Should().Be("18.0");
        result.Variables.Should().ContainKey("PACKAGE_MANAGER");
        result.Variables["PACKAGE_MANAGER"].Should().Be("yarn");
        
        var buildJob = result.Jobs["build"];
        buildJob.BeforeScript.Should().Contain("node --version");
        buildJob.BeforeScript.Should().Contain("npm --version");
        buildJob.BeforeScript.Should().Contain("yarn install --frozen-lockfile");
    }

    [Fact]
    public async Task GenerateAsync_WithComplexAnalysisResult_AppliesAllEnhancements()
    {
        // Arrange
        var analysisResult = CreateComplexAnalysisResult();
        
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            AnalysisResult = analysisResult,
            UseAnalysisDefaults = true
        };

        var basePipeline = new PipelineConfiguration
        {
            Jobs = new Dictionary<string, Job>
            {
                ["build"] = new Job { Stage = "build", Script = new List<string> { "echo 'Building...'" } },
                ["test"] = new Job { Stage = "test", Script = new List<string> { "echo 'Testing...'" } }
            },
            Variables = new Dictionary<string, object>()
        };

        _mockBasePipelineGenerator.Setup(x => x.GenerateAsync(options))
            .ReturnsAsync(basePipeline);

        // Act
        var result = await _intelligentGenerator.GenerateAsync(options);

        // Assert
        // Framework enhancements
        result.Variables.Should().ContainKey("DOTNET_VERSION");
        result.Variables["DOTNET_VERSION"].Should().Be("8.0");
        
        // Build enhancements
        var buildJob = result.Jobs["build"];
        buildJob.Script.Should().Contain("dotnet restore");
        buildJob.Script.Should().Contain("dotnet build --configuration Release");
        
        // Test enhancements
        var testJob = result.Jobs["test"];
        testJob.Script.Should().Contain("dotnet test --configuration Release");
        
        // Cache enhancements
        buildJob.Cache.Should().NotBeNull();
        buildJob.Cache!.Key.Should().Be("$CI_COMMIT_REF_SLUG-dotnet");
        buildJob.Cache.Paths.Should().Contain("~/.nuget/packages/");
        
        // Security enhancements
        result.Jobs.Should().ContainKey("security-sast");
        
        // Docker enhancements
        result.Jobs.Should().ContainKey("docker-build");
        
        // Deployment enhancements
        result.Variables.Should().ContainKey("# Required secret: KUBE_CONFIG");
    }

    #endregion

    #region Analysis-Based Pipeline Options Tests

    [Fact]
    public void AnalysisBasedPipelineOptions_CreateFromAnalysis_SetsCorrectDefaults()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.Framework.Version = "8.0";
        analysisResult.BuildConfig.TestCommands = new List<string> { "dotnet test" };
        analysisResult.Deployment.HasDeploymentConfig = true;
        analysisResult.Deployment.DetectedEnvironments = new List<string> { "staging", "production" };

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(analysisResult);

        // Assert
        options.Should().NotBeNull();
        options.ProjectType.Should().Be("dotnet");
        options.DotNetVersion.Should().Be("8.0");
        options.IncludeTests.Should().BeTrue();
        options.IncludeDeployment.Should().BeTrue();
        options.IncludeCodeQuality.Should().BeTrue();
        options.Stages.Should().Contain(new[] { "build", "test", "deploy" });
        options.DeploymentEnvironments.Should().HaveCount(2);
        options.UseAnalysisDefaults.Should().BeTrue();
    }

    [Fact]
    public void AnalysisBasedPipelineOptions_CreateFromAnalysisWithManualOptions_MergesCorrectly()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        analysisResult.Framework.Version = "8.0";
        
        var manualOptions = new PipelineOptions
        {
            ProjectType = "nodejs", // Different from analysis
            CustomVariables = new Dictionary<string, string>
            {
                ["MANUAL_VAR"] = "manual_value"
            },
            RunnerTags = new List<string> { "docker" }
        };

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.PreferManual);

        // Assert
        options.Should().NotBeNull();
        options.ProjectType.Should().Be("nodejs"); // Manual preferred
        options.DotNetVersion.Should().Be("8.0"); // Analysis fills gap
        options.CustomVariables.Should().ContainKey("MANUAL_VAR");
        options.CustomVariables.Should().ContainKey("DOTNET_VERSION"); // Analysis adds this
        options.RunnerTags.Should().Contain("docker");
    }

    #endregion

    #region Helper Methods

    private static ProjectAnalysisResult CreateSampleAnalysisResult()
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

    private static ProjectAnalysisResult CreateComplexAnalysisResult()
    {
        return new ProjectAnalysisResult
        {
            DetectedType = ProjectType.DotNet,
            Framework = new FrameworkInfo
            {
                Name = ".NET",
                Version = "8.0",
                DetectedFeatures = new List<string> { "web", "api" },
                Configuration = new Dictionary<string, string>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Development"
                },
                Confidence = AnalysisConfidence.High
            },
            BuildConfig = new BuildConfiguration
            {
                BuildTool = "dotnet",
                BuildToolVersion = "8.0",
                BuildCommands = new List<string> { "dotnet restore", "dotnet build --configuration Release" },
                TestCommands = new List<string> { "dotnet test --configuration Release" },
                ArtifactPaths = new List<string> { "bin/", "obj/" },
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["BUILD_CONFIGURATION"] = "Release"
                },
                Confidence = AnalysisConfidence.High
            },
            Dependencies = new DependencyInfo
            {
                PackageManager = "nuget",
                Dependencies = new List<PackageDependency>(),
                DevDependencies = new List<PackageDependency>(),
                Runtime = new RuntimeInfo { Name = ".NET", Version = "8.0" },
                CacheRecommendation = new CacheRecommendation 
                { 
                    IsRecommended = true,
                    Configuration = new CacheConfiguration
                    {
                        CacheKey = "$CI_COMMIT_REF_SLUG-dotnet",
                        CachePaths = new List<string> { "~/.nuget/packages/" }
                    }
                },
                SecurityScanRecommendation = new SecurityScanConfiguration 
                { 
                    IsRecommended = true,
                    RecommendedScanners = new List<SecurityScanner>
                    {
                        new SecurityScanner { Name = "SAST", Type = SecurityScanType.SAST }
                    }
                },
                HasSecuritySensitiveDependencies = false
            },
            Docker = new DockerConfiguration
            {
                BaseImage = "mcr.microsoft.com/dotnet/sdk:8.0",
                HasDockerConfig = true,
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
            Recommendations = new List<string>(),
            Metadata = new AnalysisMetadata()
        };
    }

    #endregion
}