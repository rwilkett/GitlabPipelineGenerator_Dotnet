using FluentAssertions;
using GitlabPipelineGenerator.Core.Models;
using GitlabPipelineGenerator.Core.Models.GitLab;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Models;

public class AnalysisBasedPipelineOptionsTests
{
    #region CreateFromAnalysis Tests

    [Fact]
    public void CreateFromAnalysis_WithDotNetProject_SetsCorrectDefaults()
    {
        // Arrange
        var analysisResult = CreateDotNetAnalysisResult();

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(analysisResult);

        // Assert
        options.Should().NotBeNull();
        options.ProjectType.Should().Be("dotnet");
        options.DotNetVersion.Should().Be("8.0");
        options.IncludeTests.Should().BeTrue();
        options.IncludeCodeQuality.Should().BeTrue();
        options.Stages.Should().Contain(new[] { "build", "test" });
        options.UseAnalysisDefaults.Should().BeTrue();
        options.MergeStrategy.Should().Be(ConfigurationMergeStrategy.PreferManual);
        options.CustomVariables.Should().ContainKey("DOTNET_VERSION");
        options.CustomVariables["DOTNET_VERSION"].Should().Be("8.0");
    }

    [Fact]
    public void CreateFromAnalysis_WithNodeJsProject_SetsCorrectDefaults()
    {
        // Arrange
        var analysisResult = CreateNodeJsAnalysisResult();

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(analysisResult);

        // Assert
        options.Should().NotBeNull();
        options.ProjectType.Should().Be("nodejs");
        options.IncludeTests.Should().BeTrue();
        options.IncludeCodeQuality.Should().BeTrue();
        options.Stages.Should().Contain(new[] { "build", "test" });
        options.CustomVariables.Should().ContainKey("NODE.JS_VERSION");
        options.CustomVariables["NODE.JS_VERSION"].Should().Be("18.0");
        options.CustomVariables.Should().ContainKey("BUILD_TOOL");
        options.CustomVariables["BUILD_TOOL"].Should().Be("npm");
    }

    [Fact]
    public void CreateFromAnalysis_WithSecurityRecommendations_EnablesSecurity()
    {
        // Arrange
        var analysisResult = CreateAnalysisResultWithSecurity();

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(analysisResult);

        // Assert
        options.IncludeSecurity.Should().BeTrue();
        options.Stages.Should().Contain("security");
    }

    [Fact]
    public void CreateFromAnalysis_WithDeploymentConfig_EnablesDeployment()
    {
        // Arrange
        var analysisResult = CreateAnalysisResultWithDeployment();

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(analysisResult);

        // Assert
        options.IncludeDeployment.Should().BeTrue();
        options.Stages.Should().Contain("deploy");
        options.DeploymentEnvironments.Should().HaveCount(2);
        options.DeploymentEnvironments.Should().Contain(e => e.Name == "staging" && !e.IsManual);
        options.DeploymentEnvironments.Should().Contain(e => e.Name == "production" && e.IsManual);
    }

    [Fact]
    public void CreateFromAnalysis_WithCacheRecommendation_ConfiguresCache()
    {
        // Arrange
        var analysisResult = CreateAnalysisResultWithCache();

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(analysisResult);

        // Assert
        options.Cache.Should().NotBeNull();
        options.Cache!.Key.Should().Be("$CI_COMMIT_REF_SLUG-dotnet");
        options.Cache.Paths.Should().Contain("~/.nuget/packages/");
    }

    [Fact]
    public void CreateFromAnalysis_WithDockerConfig_SetsDockerImage()
    {
        // Arrange
        var analysisResult = CreateAnalysisResultWithDocker();

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(analysisResult);

        // Assert
        options.DockerImage.Should().Be("mcr.microsoft.com/dotnet/sdk:8.0");
        options.CustomVariables.Should().ContainKey("BUILD_CONFIGURATION");
        options.CustomVariables["BUILD_CONFIGURATION"].Should().Be("Release");
    }

    #endregion

    #region Merge Strategy Tests

    [Fact]
    public void CreateFromAnalysis_WithPreferManualStrategy_PrefersManualOptions()
    {
        // Arrange
        var analysisResult = CreateDotNetAnalysisResult();
        var manualOptions = new PipelineOptions
        {
            ProjectType = "nodejs", // Different from analysis
            DotNetVersion = "9.0", // Different from analysis
            IncludeTests = false, // Different from analysis
            CustomVariables = new Dictionary<string, string>
            {
                ["MANUAL_VAR"] = "manual_value",
                ["DOTNET_VERSION"] = "9.0" // Override analysis
            }
        };

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.PreferManual);

        // Assert
        options.ProjectType.Should().Be("nodejs"); // Manual preferred
        options.DotNetVersion.Should().Be("9.0"); // Manual preferred
        options.IncludeTests.Should().BeFalse(); // Manual preferred
        options.CustomVariables["MANUAL_VAR"].Should().Be("manual_value");
        options.CustomVariables["DOTNET_VERSION"].Should().Be("9.0"); // Manual override
    }

    [Fact]
    public void CreateFromAnalysis_WithPreferAnalysisStrategy_PrefersAnalysisOptions()
    {
        // Arrange
        var analysisResult = CreateDotNetAnalysisResult();
        analysisResult.BuildConfig.TestCommands = new List<string> { "dotnet test" };
        
        var manualOptions = new PipelineOptions
        {
            ProjectType = "nodejs", // Different from analysis
            DotNetVersion = "9.0", // Different from analysis
            IncludeTests = false, // Different from analysis
            CustomVariables = new Dictionary<string, string>
            {
                ["MANUAL_VAR"] = "manual_value"
            }
        };

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.PreferAnalysis);

        // Assert
        options.ProjectType.Should().Be("dotnet"); // Analysis preferred
        options.DotNetVersion.Should().Be("8.0"); // Analysis preferred
        options.IncludeTests.Should().BeTrue(); // Analysis preferred
        options.CustomVariables["MANUAL_VAR"].Should().Be("manual_value"); // Manual variables still added
        options.CustomVariables["DOTNET_VERSION"].Should().Be("8.0"); // Analysis version
    }

    [Fact]
    public void CreateFromAnalysis_WithIntelligentMergeHighConfidence_PrefersAnalysis()
    {
        // Arrange
        var analysisResult = CreateDotNetAnalysisResult();
        analysisResult.Confidence = AnalysisConfidence.High;
        
        var manualOptions = new PipelineOptions
        {
            ProjectType = "nodejs",
            DotNetVersion = "9.0"
        };

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.IntelligentMerge);

        // Assert
        options.ProjectType.Should().Be("dotnet"); // High confidence analysis preferred
        options.DotNetVersion.Should().Be("8.0"); // High confidence analysis preferred
    }

    [Fact]
    public void CreateFromAnalysis_WithIntelligentMergeLowConfidence_PrefersManual()
    {
        // Arrange
        var analysisResult = CreateDotNetAnalysisResult();
        analysisResult.Confidence = AnalysisConfidence.Low;
        
        var manualOptions = new PipelineOptions
        {
            ProjectType = "nodejs",
            DotNetVersion = "9.0"
        };

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.IntelligentMerge);

        // Assert
        options.ProjectType.Should().Be("nodejs"); // Low confidence, manual preferred
        options.DotNetVersion.Should().Be("9.0"); // Low confidence, manual preferred
    }

    [Fact]
    public void CreateFromAnalysis_WithAnalysisOnlyStrategy_IgnoresManualOptions()
    {
        // Arrange
        var analysisResult = CreateDotNetAnalysisResult();
        var manualOptions = new PipelineOptions
        {
            ProjectType = "nodejs",
            DotNetVersion = "9.0",
            CustomVariables = new Dictionary<string, string>
            {
                ["MANUAL_VAR"] = "manual_value"
            }
        };

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.AnalysisOnly);

        // Assert
        options.ProjectType.Should().Be("dotnet"); // Analysis only
        options.DotNetVersion.Should().Be("8.0"); // Analysis only
        options.CustomVariables.Should().NotContainKey("MANUAL_VAR"); // Manual ignored
        options.CustomVariables.Should().ContainKey("DOTNET_VERSION"); // Analysis variables included
    }

    [Fact]
    public void CreateFromAnalysis_WithManualOnlyStrategy_UsesManualOptionsOnly()
    {
        // Arrange
        var analysisResult = CreateDotNetAnalysisResult();
        var manualOptions = new PipelineOptions
        {
            ProjectType = "nodejs",
            DotNetVersion = "9.0",
            IncludeTests = false,
            CustomVariables = new Dictionary<string, string>
            {
                ["MANUAL_VAR"] = "manual_value"
            }
        };

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.ManualOnly);

        // Assert
        options.ProjectType.Should().Be("nodejs"); // Manual only
        options.DotNetVersion.Should().Be("9.0"); // Manual only
        options.IncludeTests.Should().BeFalse(); // Manual only
        options.CustomVariables["MANUAL_VAR"].Should().Be("manual_value");
        options.UseAnalysisDefaults.Should().BeFalse(); // Analysis disabled
    }

    #endregion

    #region Framework-Specific Docker Image Tests

    [Fact]
    public void CreateFromAnalysis_WithDotNetFramework_SetsCorrectDockerImage()
    {
        // Arrange
        var analysisResult = CreateDotNetAnalysisResult();

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(analysisResult);

        // Assert
        options.DockerImage.Should().Be("mcr.microsoft.com/dotnet/sdk:8.0");
    }

    [Fact]
    public void CreateFromAnalysis_WithNodeJsFramework_SetsCorrectDockerImage()
    {
        // Arrange
        var analysisResult = CreateNodeJsAnalysisResult();

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(analysisResult);

        // Assert
        options.DockerImage.Should().Be("node:18.0-alpine");
    }

    [Fact]
    public void CreateFromAnalysis_WithPythonFramework_SetsCorrectDockerImage()
    {
        // Arrange
        var analysisResult = CreatePythonAnalysisResult();

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(analysisResult);

        // Assert
        options.DockerImage.Should().Be("python:3.11-slim");
    }

    [Fact]
    public void CreateFromAnalysis_WithJavaFramework_SetsCorrectDockerImage()
    {
        // Arrange
        var analysisResult = CreateJavaAnalysisResult();

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(analysisResult);

        // Assert
        options.DockerImage.Should().Be("openjdk:17-jdk-slim");
    }

    [Fact]
    public void CreateFromAnalysis_WithUnknownFramework_DoesNotSetDockerImage()
    {
        // Arrange
        var analysisResult = new ProjectAnalysisResult
        {
            DetectedType = ProjectType.Unknown,
            Framework = new FrameworkInfo { Name = "Unknown Framework" },
            BuildConfig = new BuildConfiguration(),
            Dependencies = new DependencyInfo(),
            Deployment = new DeploymentInfo(),
            Confidence = AnalysisConfidence.Low
        };

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(analysisResult);

        // Assert
        options.DockerImage.Should().BeNull();
    }

    #endregion

    #region Variable Merging Tests

    [Fact]
    public void CreateFromAnalysis_WithFrameworkConfiguration_AddsFrameworkVariables()
    {
        // Arrange
        var analysisResult = CreateDotNetAnalysisResult();
        analysisResult.Framework.Configuration = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "true"
        };

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(analysisResult);

        // Assert
        options.CustomVariables.Should().ContainKey("ASPNETCORE_ENVIRONMENT");
        options.CustomVariables["ASPNETCORE_ENVIRONMENT"].Should().Be("Development");
        options.CustomVariables.Should().ContainKey("DOTNET_SKIP_FIRST_TIME_EXPERIENCE");
        options.CustomVariables["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"].Should().Be("true");
    }

    [Fact]
    public void CreateFromAnalysis_WithBuildToolInformation_AddsBuildToolVariables()
    {
        // Arrange
        var analysisResult = CreateDotNetAnalysisResult();
        analysisResult.BuildConfig.BuildTool = "dotnet";
        analysisResult.BuildConfig.BuildToolVersion = "8.0.100";

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(analysisResult);

        // Assert
        options.CustomVariables.Should().ContainKey("BUILD_TOOL");
        options.CustomVariables["BUILD_TOOL"].Should().Be("dotnet");
        options.CustomVariables.Should().ContainKey("BUILD_TOOL_VERSION");
        options.CustomVariables["BUILD_TOOL_VERSION"].Should().Be("8.0.100");
    }

    [Fact]
    public void CreateFromAnalysis_WithManualVariableConflicts_ManualTakesPrecedence()
    {
        // Arrange
        var analysisResult = CreateDotNetAnalysisResult();
        analysisResult.Framework.Version = "8.0";
        
        var manualOptions = new PipelineOptions
        {
            CustomVariables = new Dictionary<string, string>
            {
                ["DOTNET_VERSION"] = "9.0", // Conflicts with analysis
                ["MANUAL_VAR"] = "manual_value"
            }
        };

        // Act
        var options = AnalysisBasedPipelineOptions.CreateFromAnalysis(
            analysisResult, 
            manualOptions, 
            ConfigurationMergeStrategy.PreferManual);

        // Assert
        options.CustomVariables["DOTNET_VERSION"].Should().Be("9.0"); // Manual wins
        options.CustomVariables["MANUAL_VAR"].Should().Be("manual_value");
        options.CustomVariables.Should().ContainKey("BUILD_TOOL"); // Analysis variables still added
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

    private static ProjectAnalysisResult CreatePythonAnalysisResult()
    {
        return new ProjectAnalysisResult
        {
            DetectedType = ProjectType.Python,
            Framework = new FrameworkInfo
            {
                Name = "Python",
                Version = "3.11",
                DetectedFeatures = new List<string> { "web", "api" },
                Configuration = new Dictionary<string, string>(),
                Confidence = AnalysisConfidence.High
            },
            BuildConfig = new BuildConfiguration
            {
                BuildTool = "pip",
                BuildCommands = new List<string> { "pip install -r requirements.txt" },
                TestCommands = new List<string> { "pytest" },
                ArtifactPaths = new List<string>(),
                EnvironmentVariables = new Dictionary<string, string>(),
                Confidence = AnalysisConfidence.High
            },
            Dependencies = new DependencyInfo
            {
                PackageManager = "pip",
                Dependencies = new List<PackageDependency>(),
                DevDependencies = new List<PackageDependency>(),
                Runtime = new RuntimeInfo { Name = "Python", Version = "3.11" },
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

    private static ProjectAnalysisResult CreateJavaAnalysisResult()
    {
        return new ProjectAnalysisResult
        {
            DetectedType = ProjectType.Java,
            Framework = new FrameworkInfo
            {
                Name = "Java",
                Version = "17",
                DetectedFeatures = new List<string> { "web", "api" },
                Configuration = new Dictionary<string, string>(),
                Confidence = AnalysisConfidence.High
            },
            BuildConfig = new BuildConfiguration
            {
                BuildTool = "maven",
                BuildCommands = new List<string> { "mvn compile" },
                TestCommands = new List<string> { "mvn test" },
                ArtifactPaths = new List<string> { "target/" },
                EnvironmentVariables = new Dictionary<string, string>(),
                Confidence = AnalysisConfidence.High
            },
            Dependencies = new DependencyInfo
            {
                PackageManager = "maven",
                Dependencies = new List<PackageDependency>(),
                DevDependencies = new List<PackageDependency>(),
                Runtime = new RuntimeInfo { Name = "Java", Version = "17" },
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
            Configuration = new CacheConfiguration
            {
                CacheKey = "$CI_COMMIT_REF_SLUG-dotnet",
                CachePaths = new List<string> { "~/.nuget/packages/" }
            }
        };
        return result;
    }

    private static ProjectAnalysisResult CreateAnalysisResultWithDocker()
    {
        var result = CreateDotNetAnalysisResult();
        result.Docker = new DockerConfiguration
        {
            BaseImage = "mcr.microsoft.com/dotnet/sdk:8.0",
            HasDockerConfig = true,
            BuildArgs = new Dictionary<string, string>
            {
                ["BUILD_CONFIGURATION"] = "Release"
            }
        };
        return result;
    }

    #endregion
}