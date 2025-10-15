using FluentAssertions;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Services;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

/// <summary>
/// Integration tests for analysis components to verify task 3.5 implementation
/// </summary>
public class AnalysisComponentsIntegrationTests
{
    #region File Pattern Analysis Tests

    [Fact]
    public async Task FilePatternAnalyzer_WithDotNetProject_ShouldDetectCorrectly()
    {
        // Arrange
        var analyzer = new FilePatternAnalyzer();
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "MyProject.csproj", Path = "MyProject.csproj", Type = "blob" },
            new() { Name = "Program.cs", Path = "src/Program.cs", Type = "blob" },
            new() { Name = "appsettings.json", Path = "appsettings.json", Type = "blob" }
        };

        // Act
        var projectType = await analyzer.DetectProjectTypeAsync(files);
        var framework = await analyzer.DetectFrameworksAsync(files);
        var buildTools = await analyzer.DetectBuildToolsAsync(files);
        var testFramework = await analyzer.DetectTestFrameworksAsync(files);

        // Assert
        projectType.Should().Be(ProjectType.DotNet);
        framework.Name.Should().Be("Unknown"); // No ASP.NET Core content detected
        buildTools.Name.Should().Be("dotnet");
        buildTools.BuildCommands.Should().Contain("dotnet build");
        testFramework.Name.Should().Be("Unknown"); // No test framework content detected
    }

    [Fact]
    public async Task FilePatternAnalyzer_WithNodeJsProject_ShouldDetectCorrectly()
    {
        // Arrange
        var analyzer = new FilePatternAnalyzer();
        var files = new List<GitLabRepositoryFile>
        {
            new() 
            { 
                Name = "package.json", 
                Path = "package.json", 
                Type = "blob",
                Content = """
                {
                  "name": "test-project",
                  "dependencies": {
                    "react": "^18.0.0"
                  },
                  "scripts": {
                    "build": "webpack",
                    "test": "jest"
                  }
                }
                """
            },
            new() { Name = "index.js", Path = "src/index.js", Type = "blob" },
            new() { Name = "App.jsx", Path = "src/App.jsx", Type = "blob" }
        };

        // Act
        var projectType = await analyzer.DetectProjectTypeAsync(files);
        var framework = await analyzer.DetectFrameworksAsync(files);
        var buildTools = await analyzer.DetectBuildToolsAsync(files);

        // Assert
        projectType.Should().Be(ProjectType.NodeJs);
        framework.Name.Should().Be("React");
        buildTools.Name.Should().Be("npm");
        buildTools.BuildCommands.Should().Contain("npm run build");
        buildTools.TestCommands.Should().Contain("npm run test");
    }

    #endregion

    #region Dependency Analysis Tests

    [Fact]
    public async Task DependencyAnalyzer_WithPackageJson_ShouldAnalyzeCorrectly()
    {
        // Arrange
        var analyzer = new DependencyAnalyzer();
        var content = """
        {
          "name": "test-project",
          "dependencies": {
            "express": "^4.18.0",
            "lodash": "^4.17.21"
          },
          "devDependencies": {
            "jest": "^28.0.0"
          }
        }
        """;

        // Act
        var result = await analyzer.AnalyzePackageFileAsync("package.json", content);
        var cacheConfig = await analyzer.RecommendCacheConfigurationAsync(result);
        var securityConfig = await analyzer.RecommendSecurityScanningAsync(result);
        var runtimeInfo = await analyzer.DetectRuntimeRequirementsAsync(result);

        // Assert
        result.PackageManager.Should().Be("npm");
        result.Dependencies.Should().HaveCount(2);
        result.DevDependencies.Should().HaveCount(1);
        result.HasSecuritySensitiveDependencies.Should().BeTrue();
        
        cacheConfig.CachePaths.Should().Contain("node_modules/");
        securityConfig.IsRecommended.Should().BeTrue();
        runtimeInfo.Name.Should().Be("node");
    }

    [Fact]
    public async Task DependencyAnalyzer_WithCsprojFile_ShouldAnalyzeCorrectly()
    {
        // Arrange
        var analyzer = new DependencyAnalyzer();
        var content = """
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Microsoft.AspNetCore.App" />
            <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
          </ItemGroup>
        </Project>
        """;

        // Act
        var result = await analyzer.AnalyzePackageFileAsync("MyProject.csproj", content);
        var cacheConfig = await analyzer.RecommendCacheConfigurationAsync(result);
        var runtimeInfo = await analyzer.DetectRuntimeRequirementsAsync(result);

        // Assert
        result.PackageManager.Should().Be("dotnet");
        result.Dependencies.Should().HaveCount(2);
        result.HasSecuritySensitiveDependencies.Should().BeTrue();
        
        cacheConfig.CachePaths.Should().Contain("~/.nuget/packages/");
        runtimeInfo.Name.Should().Be("dotnet");
        runtimeInfo.Version.Should().Be("8.0");
    }

    [Fact]
    public async Task DependencyAnalyzer_WithRequirementsTxt_ShouldAnalyzeCorrectly()
    {
        // Arrange
        var analyzer = new DependencyAnalyzer();
        var content = """
        django>=4.0.0
        requests>=2.28.0
        pytest>=7.0.0
        """;

        // Act
        var result = await analyzer.AnalyzePackageFileAsync("requirements.txt", content);
        var securityConfig = await analyzer.RecommendSecurityScanningAsync(result);
        var runtimeInfo = await analyzer.DetectRuntimeRequirementsAsync(result);

        // Assert
        result.PackageManager.Should().Be("pip");
        result.Dependencies.Should().HaveCount(3);
        result.HasSecuritySensitiveDependencies.Should().BeTrue();
        
        securityConfig.RecommendedScanners.Should().Contain(s => s.Name == "safety");
        runtimeInfo.Name.Should().Be("python");
    }

    #endregion

    #region Configuration Analysis Tests

    [Fact]
    public async Task ConfigurationAnalyzer_WithGitLabProject_ShouldAnalyzeCI()
    {
        // Arrange
        var analyzer = new ConfigurationAnalyzer();
        var project = new GitLabProject
        {
            Id = 123,
            Name = "test-project",
            Path = "test-project",
            FullPath = "group/test-project",
            DefaultBranch = "main"
        };

        // Act
        var ciConfig = await analyzer.AnalyzeExistingCIConfigAsync(project);
        var dockerConfig = await analyzer.AnalyzeDockerConfigurationAsync(project);
        var deploymentConfig = await analyzer.AnalyzeDeploymentConfigurationAsync(project);
        var envConfig = await analyzer.DetectEnvironmentsAsync(project);

        // Assert
        ciConfig.Should().NotBeNull();
        ciConfig.HasExistingConfig.Should().BeTrue();
        ciConfig.SystemType.Should().Be(CISystemType.GitLabCI);
        
        dockerConfig.Should().NotBeNull();
        dockerConfig.HasDockerConfig.Should().BeTrue();
        
        deploymentConfig.Should().NotBeNull();
        deploymentConfig.HasDeploymentConfig.Should().BeTrue();
        
        envConfig.Should().NotBeNull();
        envConfig.Environments.Should().NotBeEmpty();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task AnalysisComponents_WithComplexProject_ShouldWorkTogether()
    {
        // Arrange
        var fileAnalyzer = new FilePatternAnalyzer();
        var dependencyAnalyzer = new DependencyAnalyzer();
        var configAnalyzer = new ConfigurationAnalyzer();
        
        var files = new List<GitLabRepositoryFile>
        {
            new() 
            { 
                Name = "package.json", 
                Path = "package.json", 
                Type = "blob",
                Content = """
                {
                  "name": "complex-project",
                  "dependencies": {
                    "express": "^4.18.0",
                    "react": "^18.0.0"
                  },
                  "devDependencies": {
                    "jest": "^28.0.0"
                  }
                }
                """
            },
            new() { Name = "Dockerfile", Path = "Dockerfile", Type = "blob" },
            new() { Name = "index.js", Path = "src/index.js", Type = "blob" },
            new() { Name = "App.jsx", Path = "src/App.jsx", Type = "blob" }
        };

        var project = new GitLabProject
        {
            Id = 456,
            Name = "complex-project",
            Path = "complex-project",
            FullPath = "group/complex-project",
            DefaultBranch = "main"
        };

        // Act
        var projectType = await fileAnalyzer.DetectProjectTypeAsync(files);
        var framework = await fileAnalyzer.DetectFrameworksAsync(files);
        var buildTools = await fileAnalyzer.DetectBuildToolsAsync(files);
        
        var packageFile = files.First(f => f.Name == "package.json");
        var dependencies = await dependencyAnalyzer.AnalyzePackageFileAsync(packageFile.Name, packageFile.Content!);
        var cacheConfig = await dependencyAnalyzer.RecommendCacheConfigurationAsync(dependencies);
        var securityConfig = await dependencyAnalyzer.RecommendSecurityScanningAsync(dependencies);
        
        var dockerConfig = await configAnalyzer.AnalyzeDockerConfigurationAsync(project);

        // Assert - File Analysis
        projectType.Should().Be(ProjectType.NodeJs);
        framework.Name.Should().Be("React");
        buildTools.Name.Should().Be("npm");

        // Assert - Dependency Analysis
        dependencies.PackageManager.Should().Be("npm");
        dependencies.TotalDependencies.Should().Be(3);
        dependencies.HasSecuritySensitiveDependencies.Should().BeTrue();
        
        // Assert - Cache Recommendations
        cacheConfig.CachePaths.Should().Contain("node_modules/");
        dependencies.CacheRecommendation.IsRecommended.Should().BeTrue();
        
        // Assert - Security Recommendations
        securityConfig.IsRecommended.Should().BeTrue();
        securityConfig.RecommendedScanners.Should().Contain(s => s.Name == "npm audit");
        
        // Assert - Docker Configuration
        dockerConfig.HasDockerConfig.Should().BeTrue();
        dockerConfig.DockerfilePath.Should().Be("Dockerfile");
    }

    #endregion
}