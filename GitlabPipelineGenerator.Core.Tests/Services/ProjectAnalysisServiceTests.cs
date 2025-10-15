using FluentAssertions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Services;
using Moq;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

/// <summary>
/// Unit tests for ProjectAnalysisService
/// </summary>
public class ProjectAnalysisServiceTests
{
    private readonly Mock<IFilePatternAnalyzer> _mockFilePatternAnalyzer;
    private readonly Mock<IDependencyAnalyzer> _mockDependencyAnalyzer;
    private readonly Mock<IConfigurationAnalyzer> _mockConfigurationAnalyzer;
    private readonly ProjectAnalysisService _analysisService;

    public ProjectAnalysisServiceTests()
    {
        _mockFilePatternAnalyzer = new Mock<IFilePatternAnalyzer>();
        _mockDependencyAnalyzer = new Mock<IDependencyAnalyzer>();
        _mockConfigurationAnalyzer = new Mock<IConfigurationAnalyzer>();
        
        _analysisService = new ProjectAnalysisService(
            _mockFilePatternAnalyzer.Object,
            _mockDependencyAnalyzer.Object,
            _mockConfigurationAnalyzer.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullFilePatternAnalyzer_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProjectAnalysisService(
            null!,
            _mockDependencyAnalyzer.Object,
            _mockConfigurationAnalyzer.Object));
    }

    [Fact]
    public void Constructor_WithNullDependencyAnalyzer_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProjectAnalysisService(
            _mockFilePatternAnalyzer.Object,
            null!,
            _mockConfigurationAnalyzer.Object));
    }

    [Fact]
    public void Constructor_WithNullConfigurationAnalyzer_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ProjectAnalysisService(
            _mockFilePatternAnalyzer.Object,
            _mockDependencyAnalyzer.Object,
            null!));
    }

    #endregion

    #region AnalyzeProjectAsync Tests

    [Fact]
    public async Task AnalyzeProjectAsync_WithValidProject_ShouldReturnCompleteAnalysis()
    {
        // Arrange
        var project = CreateSampleProject();
        var options = new AnalysisOptions();

        SetupMockAnalyzers();

        // Act
        var result = await _analysisService.AnalyzeProjectAsync(project, options);

        // Assert
        result.Should().NotBeNull();
        result.DetectedType.Should().Be(ProjectType.DotNet);
        result.Framework.Name.Should().Be("ASP.NET Core");
        result.BuildConfig.BuildTool.Should().Be("dotnet");
        result.Dependencies.PackageManager.Should().Be("dotnet");
        result.Confidence.Should().BeOneOf(AnalysisConfidence.Medium, AnalysisConfidence.High);
        result.AnalysisTime.Should().BeGreaterThan(TimeSpan.Zero);
        result.FilesAnalyzed.Should().BeGreaterThan(0);
        result.Metadata.AnalyzedComponents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithFileAnalysisDisabled_ShouldSkipFileAnalysis()
    {
        // Arrange
        var project = CreateSampleProject();
        var options = new AnalysisOptions { AnalyzeFiles = false };

        SetupMockAnalyzers();

        // Act
        var result = await _analysisService.AnalyzeProjectAsync(project, options);

        // Assert
        result.Should().NotBeNull();
        result.DetectedType.Should().Be(ProjectType.Unknown);
        result.Framework.Name.Should().Be("Unknown");
        result.Metadata.AnalyzedComponents.Should().NotContain("FilePatternAnalyzer");
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithDependencyAnalysisDisabled_ShouldSkipDependencyAnalysis()
    {
        // Arrange
        var project = CreateSampleProject();
        var options = new AnalysisOptions { AnalyzeDependencies = false };

        SetupMockAnalyzers();

        // Act
        var result = await _analysisService.AnalyzeProjectAsync(project, options);

        // Assert
        result.Should().NotBeNull();
        result.Dependencies.TotalDependencies.Should().Be(0);
        result.Metadata.AnalyzedComponents.Should().NotContain("DependencyAnalyzer");
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithCIAnalysisDisabled_ShouldSkipCIAnalysis()
    {
        // Arrange
        var project = CreateSampleProject();
        var options = new AnalysisOptions { AnalyzeExistingCI = false };

        SetupMockAnalyzers();

        // Act
        var result = await _analysisService.AnalyzeProjectAsync(project, options);

        // Assert
        result.Should().NotBeNull();
        result.ExistingCI.Should().BeNull();
        result.Metadata.AnalyzedComponents.Should().NotContain("ConfigurationAnalyzer-CI");
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithDeploymentAnalysisDisabled_ShouldSkipDeploymentAnalysis()
    {
        // Arrange
        var project = CreateSampleProject();
        var options = new AnalysisOptions { AnalyzeDeployment = false };

        SetupMockAnalyzers();

        // Act
        var result = await _analysisService.AnalyzeProjectAsync(project, options);

        // Assert
        result.Should().NotBeNull();
        result.Deployment.HasDeploymentConfig.Should().BeFalse();
        result.Metadata.AnalyzedComponents.Should().NotContain("ConfigurationAnalyzer-Deployment");
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WhenAnalysisThrows_ShouldReturnResultWithWarnings()
    {
        // Arrange
        var project = CreateSampleProject();
        var options = new AnalysisOptions();

        _mockFilePatternAnalyzer
            .Setup(x => x.DetectProjectTypeAsync(It.IsAny<IEnumerable<GitLabRepositoryFile>>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var result = await _analysisService.AnalyzeProjectAsync(project, options);

        // Assert
        result.Should().NotBeNull();
        result.Confidence.Should().Be(AnalysisConfidence.Low);
        result.Warnings.Should().NotBeEmpty();
        result.Warnings.Should().Contain(w => w.Severity == WarningSeverity.Error);
        result.Warnings.Should().Contain(w => w.Message.Contains("Analysis failed"));
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithSecuritySensitiveDependencies_ShouldGenerateSecurityRecommendations()
    {
        // Arrange
        var project = CreateSampleProject();
        var options = new AnalysisOptions();

        SetupMockAnalyzersWithSecuritySensitiveDependencies();

        // Act
        var result = await _analysisService.AnalyzeProjectAsync(project, options);

        // Assert
        result.Should().NotBeNull();
        result.Warnings.Should().Contain(w => w.Message.Contains("Security-sensitive dependencies detected"));
        result.Recommendations.Should().Contain(r => r.Contains("Security scanning recommended"));
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithExistingNonGitLabCI_ShouldGenerateMigrationWarning()
    {
        // Arrange
        var project = CreateSampleProject();
        var options = new AnalysisOptions();

        SetupMockAnalyzersWithExistingCI();

        // Act
        var result = await _analysisService.AnalyzeProjectAsync(project, options);

        // Assert
        result.Should().NotBeNull();
        result.Warnings.Should().Contain(w => w.Message.Contains("GitHub Actions configuration detected"));
        result.Warnings.Should().Contain(w => w.Resolution.Contains("Consider migrating to GitLab CI/CD"));
    }

    #endregion

    #region DetectProjectTypeAsync Tests

    [Fact]
    public async Task DetectProjectTypeAsync_ShouldCallFilePatternAnalyzer()
    {
        // Arrange
        var project = CreateSampleProject();
        
        _mockFilePatternAnalyzer
            .Setup(x => x.DetectProjectTypeAsync(It.IsAny<IEnumerable<GitLabRepositoryFile>>()))
            .ReturnsAsync(ProjectType.NodeJs);

        // Act
        var result = await _analysisService.DetectProjectTypeAsync(project);

        // Assert
        result.Should().Be(ProjectType.NodeJs);
        _mockFilePatternAnalyzer.Verify(
            x => x.DetectProjectTypeAsync(It.IsAny<IEnumerable<GitLabRepositoryFile>>()),
            Times.Once);
    }

    #endregion

    #region AnalyzeBuildConfigurationAsync Tests

    [Fact]
    public async Task AnalyzeBuildConfigurationAsync_ShouldReturnBuildConfiguration()
    {
        // Arrange
        var project = CreateSampleProject();
        
        var buildToolInfo = new BuildToolInfo
        {
            Name = "npm",
            BuildCommands = new List<string> { "npm run build" },
            TestCommands = new List<string> { "npm test" },
            Confidence = AnalysisConfidence.High
        };

        var testFrameworkInfo = new TestFrameworkInfo
        {
            Name = "Jest",
            TestCommands = new List<string> { "jest" },
            Confidence = AnalysisConfidence.High
        };

        _mockFilePatternAnalyzer
            .Setup(x => x.DetectBuildToolsAsync(It.IsAny<IEnumerable<GitLabRepositoryFile>>()))
            .ReturnsAsync(buildToolInfo);

        _mockFilePatternAnalyzer
            .Setup(x => x.DetectTestFrameworksAsync(It.IsAny<IEnumerable<GitLabRepositoryFile>>()))
            .ReturnsAsync(testFrameworkInfo);

        // Act
        var result = await _analysisService.AnalyzeBuildConfigurationAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.BuildTool.Should().Be("npm");
        result.BuildCommands.Should().Contain("npm run build");
        result.TestCommands.Should().Contain("npm test");
        result.TestCommands.Should().Contain("jest");
        result.Confidence.Should().Be(AnalysisConfidence.High);
    }

    #endregion

    #region AnalyzeDependenciesAsync Tests

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithPackageFiles_ShouldAnalyzeDependencies()
    {
        // Arrange
        var project = CreateSampleProject();
        
        var dependencyInfo = new DependencyInfo
        {
            PackageManager = "npm",
            Dependencies = new List<PackageDependency>
            {
                new() { Name = "express", Version = "^4.18.0" }
            },
            Confidence = AnalysisConfidence.High
        };

        _mockDependencyAnalyzer
            .Setup(x => x.AnalyzePackageFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(dependencyInfo);

        // Act
        var result = await _analysisService.AnalyzeDependenciesAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.PackageManager.Should().Be("npm");
        result.Dependencies.Should().HaveCount(1);
        result.Confidence.Should().Be(AnalysisConfidence.High);
    }

    [Fact]
    public async Task AnalyzeDependenciesAsync_WithNoPackageFiles_ShouldReturnLowConfidenceDependencyInfo()
    {
        // Arrange
        var project = CreateSampleProjectWithoutPackageFiles();

        // Act
        var result = await _analysisService.AnalyzeDependenciesAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.Confidence.Should().Be(AnalysisConfidence.Low);
        result.Dependencies.Should().BeEmpty();
    }

    #endregion

    #region AnalyzeDeploymentConfigurationAsync Tests

    [Fact]
    public async Task AnalyzeDeploymentConfigurationAsync_ShouldCombineDeploymentAndEnvironmentAnalysis()
    {
        // Arrange
        var project = CreateSampleProject();
        
        var deploymentConfig = new DeploymentConfiguration
        {
            HasDeploymentConfig = true,
            Confidence = AnalysisConfidence.High
        };

        var environmentConfig = new EnvironmentConfiguration
        {
            Environments = new List<EnvironmentInfo>
            {
                new() { Name = "production", Type = EnvironmentType.Production }
            },
            Confidence = AnalysisConfidence.Medium
        };

        _mockConfigurationAnalyzer
            .Setup(x => x.AnalyzeDeploymentConfigurationAsync(It.IsAny<GitLabProject>()))
            .ReturnsAsync(deploymentConfig);

        _mockConfigurationAnalyzer
            .Setup(x => x.DetectEnvironmentsAsync(It.IsAny<GitLabProject>()))
            .ReturnsAsync(environmentConfig);

        // Act
        var result = await _analysisService.AnalyzeDeploymentConfigurationAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.HasDeploymentConfig.Should().BeTrue();
        result.Configuration.Should().Be(deploymentConfig);
        result.Environment.Should().Be(environmentConfig);
        result.Confidence.Should().Be(AnalysisConfidence.Medium); // Combined confidence
    }

    #endregion

    #region Private Helper Methods

    private GitLabProject CreateSampleProject()
    {
        return new GitLabProject
        {
            Id = 123,
            Name = "sample-project",
            Path = "sample-project",
            FullPath = "group/sample-project",
            DefaultBranch = "main",
            WebUrl = "https://gitlab.example.com/group/sample-project"
        };
    }

    private GitLabProject CreateSampleProjectWithoutPackageFiles()
    {
        return new GitLabProject
        {
            Id = 124,
            Name = "simple-project",
            Path = "simple-project",
            FullPath = "group/simple-project",
            DefaultBranch = "main",
            WebUrl = "https://gitlab.example.com/group/simple-project"
        };
    }

    private void SetupMockAnalyzers()
    {
        // File Pattern Analyzer
        _mockFilePatternAnalyzer
            .Setup(x => x.DetectProjectTypeAsync(It.IsAny<IEnumerable<GitLabRepositoryFile>>()))
            .ReturnsAsync(ProjectType.DotNet);

        _mockFilePatternAnalyzer
            .Setup(x => x.DetectFrameworksAsync(It.IsAny<IEnumerable<GitLabRepositoryFile>>()))
            .ReturnsAsync(new FrameworkInfo
            {
                Name = "ASP.NET Core",
                Confidence = AnalysisConfidence.High
            });

        _mockFilePatternAnalyzer
            .Setup(x => x.DetectBuildToolsAsync(It.IsAny<IEnumerable<GitLabRepositoryFile>>()))
            .ReturnsAsync(new BuildToolInfo
            {
                Name = "dotnet",
                BuildCommands = new List<string> { "dotnet build" },
                TestCommands = new List<string> { "dotnet test" },
                Confidence = AnalysisConfidence.High
            });

        _mockFilePatternAnalyzer
            .Setup(x => x.DetectTestFrameworksAsync(It.IsAny<IEnumerable<GitLabRepositoryFile>>()))
            .ReturnsAsync(new TestFrameworkInfo
            {
                Name = "xUnit",
                TestCommands = new List<string> { "dotnet test" },
                Confidence = AnalysisConfidence.High
            });

        // Dependency Analyzer
        _mockDependencyAnalyzer
            .Setup(x => x.AnalyzePackageFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new DependencyInfo
            {
                PackageManager = "dotnet",
                Dependencies = new List<PackageDependency>
                {
                    new() { Name = "Microsoft.AspNetCore.App", Type = DependencyType.Production }
                },
                Confidence = AnalysisConfidence.High
            });

        _mockDependencyAnalyzer
            .Setup(x => x.RecommendCacheConfigurationAsync(It.IsAny<DependencyInfo>()))
            .ReturnsAsync(new CacheConfiguration());

        _mockDependencyAnalyzer
            .Setup(x => x.RecommendSecurityScanningAsync(It.IsAny<DependencyInfo>()))
            .ReturnsAsync(new SecurityScanConfiguration
            {
                IsRecommended = false,
                RiskLevel = SecurityRiskLevel.Low
            });

        // Configuration Analyzer
        _mockConfigurationAnalyzer
            .Setup(x => x.AnalyzeExistingCIConfigAsync(It.IsAny<GitLabProject>()))
            .ReturnsAsync(new ExistingCIConfig
            {
                HasExistingConfig = false,
                SystemType = CISystemType.None,
                Confidence = AnalysisConfidence.Medium
            });

        _mockConfigurationAnalyzer
            .Setup(x => x.AnalyzeDockerConfigurationAsync(It.IsAny<GitLabProject>()))
            .ReturnsAsync(new DockerConfiguration
            {
                HasDockerConfig = false,
                Confidence = AnalysisConfidence.Medium
            });

        _mockConfigurationAnalyzer
            .Setup(x => x.AnalyzeDeploymentConfigurationAsync(It.IsAny<GitLabProject>()))
            .ReturnsAsync(new DeploymentConfiguration
            {
                HasDeploymentConfig = false,
                Confidence = AnalysisConfidence.Medium
            });

        _mockConfigurationAnalyzer
            .Setup(x => x.DetectEnvironmentsAsync(It.IsAny<GitLabProject>()))
            .ReturnsAsync(new EnvironmentConfiguration
            {
                Environments = new List<EnvironmentInfo>(),
                Confidence = AnalysisConfidence.Medium
            });
    }

    private void SetupMockAnalyzersWithSecuritySensitiveDependencies()
    {
        SetupMockAnalyzers();

        _mockDependencyAnalyzer
            .Setup(x => x.AnalyzePackageFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new DependencyInfo
            {
                PackageManager = "npm",
                Dependencies = new List<PackageDependency>
                {
                    new() { Name = "express", IsSecuritySensitive = true }
                },
                HasSecuritySensitiveDependencies = true,
                Confidence = AnalysisConfidence.High
            });

        _mockDependencyAnalyzer
            .Setup(x => x.RecommendSecurityScanningAsync(It.IsAny<DependencyInfo>()))
            .ReturnsAsync(new SecurityScanConfiguration
            {
                IsRecommended = true,
                RiskLevel = SecurityRiskLevel.High,
                Reason = "Security-sensitive dependencies detected"
            });
    }

    private void SetupMockAnalyzersWithExistingCI()
    {
        SetupMockAnalyzers();

        _mockConfigurationAnalyzer
            .Setup(x => x.AnalyzeExistingCIConfigAsync(It.IsAny<GitLabProject>()))
            .ReturnsAsync(new ExistingCIConfig
            {
                HasExistingConfig = true,
                SystemType = CISystemType.GitHubActions,
                Confidence = AnalysisConfidence.High
            });
    }

    #endregion
}