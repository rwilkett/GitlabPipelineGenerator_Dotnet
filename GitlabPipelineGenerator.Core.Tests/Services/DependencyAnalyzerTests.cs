using FluentAssertions;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Services;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

/// <summary>
/// Unit tests for DependencyAnalyzer
/// </summary>
public class DependencyAnalyzerTests
{
    private readonly DependencyAnalyzer _analyzer;

    public DependencyAnalyzerTests()
    {
        _analyzer = new DependencyAnalyzer();
    }

    #region AnalyzePackageFileAsync Tests - package.json

    [Fact]
    public async Task AnalyzePackageFileAsync_WithValidPackageJson_ShouldParseDependencies()
    {
        // Arrange
        var content = """
        {
          "name": "test-project",
          "version": "1.0.0",
          "dependencies": {
            "express": "^4.18.0",
            "lodash": "^4.17.21",
            "axios": "^1.0.0"
          },
          "devDependencies": {
            "jest": "^28.0.0",
            "nodemon": "^2.0.0"
          }
        }
        """;

        // Act
        var result = await _analyzer.AnalyzePackageFileAsync("package.json", content);

        // Assert
        result.PackageManager.Should().Be("npm");
        result.Dependencies.Should().HaveCount(3);
        result.DevDependencies.Should().HaveCount(2);
        result.Confidence.Should().Be(AnalysisConfidence.High);
        
        var expressDep = result.Dependencies.First(d => d.Name == "express");
        expressDep.Version.Should().Be("^4.18.0");
        expressDep.Type.Should().Be(DependencyType.Production);
        expressDep.IsSecuritySensitive.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzePackageFileAsync_WithMalformedPackageJson_ShouldReturnLowConfidence()
    {
        // Arrange
        var content = "{ invalid json content";

        // Act
        var result = await _analyzer.AnalyzePackageFileAsync("package.json", content);

        // Assert
        result.PackageManager.Should().Be("npm");
        result.Confidence.Should().Be(AnalysisConfidence.Low);
        result.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzePackageFileAsync_WithEmptyPackageJson_ShouldReturnHighConfidenceWithNoDependencies()
    {
        // Arrange
        var content = "{}";

        // Act
        var result = await _analyzer.AnalyzePackageFileAsync("package.json", content);

        // Assert
        result.PackageManager.Should().Be("npm");
        result.Confidence.Should().Be(AnalysisConfidence.High);
        result.Dependencies.Should().BeEmpty();
        result.DevDependencies.Should().BeEmpty();
    }

    #endregion

    #region AnalyzePackageFileAsync Tests - .csproj

    [Fact]
    public async Task AnalyzePackageFileAsync_WithValidCsproj_ShouldParseDependencies()
    {
        // Arrange
        var content = """
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Microsoft.AspNetCore.App" />
            <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
            <PackageReference Include="Serilog" Version="2.12.0" />
          </ItemGroup>
        </Project>
        """;

        // Act
        var result = await _analyzer.AnalyzePackageFileAsync("MyProject.csproj", content);

        // Assert
        result.PackageManager.Should().Be("dotnet");
        result.Dependencies.Should().HaveCount(3);
        result.Confidence.Should().Be(AnalysisConfidence.High);
        
        var aspNetCoreDep = result.Dependencies.First(d => d.Name == "Microsoft.AspNetCore.App");
        aspNetCoreDep.Type.Should().Be(DependencyType.Production);
        aspNetCoreDep.IsSecuritySensitive.Should().BeTrue();
        
        var newtonsoftDep = result.Dependencies.First(d => d.Name == "Newtonsoft.Json");
        newtonsoftDep.Version.Should().Be("13.0.1");
        newtonsoftDep.IsSecuritySensitive.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzePackageFileAsync_WithMalformedCsproj_ShouldReturnLowConfidence()
    {
        // Arrange
        var content = "<Project><InvalidXml>";

        // Act
        var result = await _analyzer.AnalyzePackageFileAsync("MyProject.csproj", content);

        // Assert
        result.PackageManager.Should().Be("dotnet");
        result.Confidence.Should().Be(AnalysisConfidence.Low);
        result.Dependencies.Should().BeEmpty();
    }

    #endregion

    #region AnalyzePackageFileAsync Tests - requirements.txt

    [Fact]
    public async Task AnalyzePackageFileAsync_WithValidRequirementsTxt_ShouldParseDependencies()
    {
        // Arrange
        var content = """
        django>=4.0.0
        requests>=2.28.0
        pytest>=7.0.0
        # This is a comment
        flask==2.2.0
        cryptography~=3.4.0
        """;

        // Act
        var result = await _analyzer.AnalyzePackageFileAsync("requirements.txt", content);

        // Assert
        result.PackageManager.Should().Be("pip");
        result.Dependencies.Should().HaveCount(5);
        result.Confidence.Should().Be(AnalysisConfidence.Medium);
        
        var djangoDep = result.Dependencies.First(d => d.Name == "django");
        djangoDep.Version.Should().Be(">=4.0.0");
        djangoDep.IsSecuritySensitive.Should().BeTrue();
        
        var flaskDep = result.Dependencies.First(d => d.Name == "flask");
        flaskDep.Version.Should().Be("==2.2.0");
        flaskDep.IsSecuritySensitive.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzePackageFileAsync_WithRequirementsTxtCommentsOnly_ShouldReturnNoDependencies()
    {
        // Arrange
        var content = """
        # This is a comment
        # Another comment
        
        # More comments
        """;

        // Act
        var result = await _analyzer.AnalyzePackageFileAsync("requirements.txt", content);

        // Assert
        result.PackageManager.Should().Be("pip");
        result.Dependencies.Should().BeEmpty();
        result.Confidence.Should().Be(AnalysisConfidence.Medium);
    }

    #endregion

    #region AnalyzePackageFileAsync Tests - pom.xml

    [Fact]
    public async Task AnalyzePackageFileAsync_WithValidPomXml_ShouldParseDependencies()
    {
        // Arrange
        var content = """
        <?xml version="1.0" encoding="UTF-8"?>
        <project xmlns="http://maven.apache.org/POM/4.0.0">
          <dependencies>
            <dependency>
              <groupId>org.springframework.boot</groupId>
              <artifactId>spring-boot-starter-web</artifactId>
              <version>2.7.0</version>
            </dependency>
            <dependency>
              <groupId>junit</groupId>
              <artifactId>junit</artifactId>
              <version>4.13.2</version>
              <scope>test</scope>
            </dependency>
            <dependency>
              <groupId>com.fasterxml.jackson.core</groupId>
              <artifactId>jackson-core</artifactId>
              <version>2.13.0</version>
              <scope>runtime</scope>
            </dependency>
          </dependencies>
        </project>
        """;

        // Act
        var result = await _analyzer.AnalyzePackageFileAsync("pom.xml", content);

        // Assert
        result.PackageManager.Should().Be("maven");
        result.Dependencies.Should().HaveCount(3);
        result.Confidence.Should().Be(AnalysisConfidence.High);
        
        var springBootDep = result.Dependencies.First(d => d.Name == "org.springframework.boot:spring-boot-starter-web");
        springBootDep.Version.Should().Be("2.7.0");
        springBootDep.Type.Should().Be(DependencyType.Production);
        springBootDep.IsSecuritySensitive.Should().BeTrue();
        
        var junitDep = result.Dependencies.First(d => d.Name == "junit:junit");
        junitDep.Type.Should().Be(DependencyType.Test);
        junitDep.IsSecuritySensitive.Should().BeTrue();
    }

    #endregion

    #region AnalyzePackageFileAsync Tests - build.gradle

    [Fact]
    public async Task AnalyzePackageFileAsync_WithValidBuildGradle_ShouldParseDependencies()
    {
        // Arrange
        var content = """
        dependencies {
            implementation 'org.springframework.boot:spring-boot-starter-web:2.7.0'
            testImplementation 'junit:junit:4.13.2'
            api 'com.google.guava:guava:31.0.1-jre'
            compileOnly 'org.projectlombok:lombok:1.18.24'
        }
        """;

        // Act
        var result = await _analyzer.AnalyzePackageFileAsync("build.gradle", content);

        // Assert
        result.PackageManager.Should().Be("gradle");
        result.Dependencies.Should().HaveCount(4);
        result.Confidence.Should().Be(AnalysisConfidence.Medium);
        
        var springBootDep = result.Dependencies.First(d => d.Name == "org.springframework.boot:spring-boot-starter-web");
        springBootDep.Version.Should().Be("2.7.0");
        springBootDep.Type.Should().Be(DependencyType.Production);
        
        var junitDep = result.Dependencies.First(d => d.Name == "junit:junit");
        junitDep.Type.Should().Be(DependencyType.Test);
        
        var lombokDep = result.Dependencies.First(d => d.Name == "org.projectlombok:lombok");
        lombokDep.Type.Should().Be(DependencyType.Build);
    }

    #endregion

    #region AnalyzePackageFileAsync Tests - Other Package Managers

    [Fact]
    public async Task AnalyzePackageFileAsync_WithValidGemfile_ShouldParseDependencies()
    {
        // Arrange
        var content = """
        gem 'rails', '~> 7.0.0'
        gem 'pg', '~> 1.1'
        gem 'puma'
        gem 'bootsnap', '>= 1.4.4', require: false
        """;

        // Act
        var result = await _analyzer.AnalyzePackageFileAsync("Gemfile", content);

        // Assert
        result.PackageManager.Should().Be("bundler");
        result.Dependencies.Should().HaveCount(4);
        result.Confidence.Should().Be(AnalysisConfidence.Medium);
        
        var railsDep = result.Dependencies.First(d => d.Name == "rails");
        railsDep.Version.Should().Be("~> 7.0.0");
    }

    [Fact]
    public async Task AnalyzePackageFileAsync_WithValidComposerJson_ShouldParseDependencies()
    {
        // Arrange
        var content = """
        {
          "require": {
            "php": "^8.0",
            "laravel/framework": "^9.0"
          },
          "require-dev": {
            "phpunit/phpunit": "^9.5"
          }
        }
        """;

        // Act
        var result = await _analyzer.AnalyzePackageFileAsync("composer.json", content);

        // Assert
        result.PackageManager.Should().Be("composer");
        result.Dependencies.Should().HaveCount(2);
        result.DevDependencies.Should().HaveCount(1);
        result.Confidence.Should().Be(AnalysisConfidence.High);
    }

    [Fact]
    public async Task AnalyzePackageFileAsync_WithValidGoMod_ShouldParseDependencies()
    {
        // Arrange
        var content = """
        module example.com/myproject

        go 1.19

        require (
            github.com/gin-gonic/gin v1.8.1
            github.com/stretchr/testify v1.8.0
        )
        """;

        // Act
        var result = await _analyzer.AnalyzePackageFileAsync("go.mod", content);

        // Assert
        result.PackageManager.Should().Be("go");
        result.Dependencies.Should().HaveCount(2);
        result.Confidence.Should().Be(AnalysisConfidence.Medium);
        
        var ginDep = result.Dependencies.First(d => d.Name == "github.com/gin-gonic/gin");
        ginDep.Version.Should().Be("v1.8.1");
    }

    [Fact]
    public async Task AnalyzePackageFileAsync_WithUnknownFileType_ShouldReturnLowConfidence()
    {
        // Arrange
        var content = "some random content";

        // Act
        var result = await _analyzer.AnalyzePackageFileAsync("unknown.txt", content);

        // Assert
        result.Confidence.Should().Be(AnalysisConfidence.Low);
        result.Dependencies.Should().BeEmpty();
    }

    #endregion

    #region RecommendCacheConfigurationAsync Tests

    [Fact]
    public async Task RecommendCacheConfigurationAsync_WithNpmDependencies_ShouldRecommendNodeCache()
    {
        // Arrange
        var dependencyInfo = new DependencyInfo
        {
            PackageManager = "npm",
            Dependencies = new List<PackageDependency>
            {
                new() { Name = "express", Version = "^4.18.0" },
                new() { Name = "lodash", Version = "^4.17.21" }
            }
        };

        // Act
        var result = await _analyzer.RecommendCacheConfigurationAsync(dependencyInfo);

        // Assert
        result.CachePaths.Should().Contain("node_modules/");
        result.CachePaths.Should().Contain(".npm/");
        result.CacheKey.Should().Be("package-lock.json");
        result.FallbackKeys.Should().Contain("package.json");
        result.Effectiveness.Should().Be(CacheEffectiveness.High);
        
        dependencyInfo.CacheRecommendation.IsRecommended.Should().BeTrue();
        dependencyInfo.CacheRecommendation.EstimatedTimeSavings.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public async Task RecommendCacheConfigurationAsync_WithDotNetDependencies_ShouldRecommendDotNetCache()
    {
        // Arrange
        var dependencyInfo = new DependencyInfo
        {
            PackageManager = "dotnet",
            Dependencies = new List<PackageDependency>
            {
                new() { Name = "Microsoft.AspNetCore.App" },
                new() { Name = "Newtonsoft.Json", Version = "13.0.1" }
            }
        };

        // Act
        var result = await _analyzer.RecommendCacheConfigurationAsync(dependencyInfo);

        // Assert
        result.CachePaths.Should().Contain("~/.nuget/packages/");
        result.CachePaths.Should().Contain("obj/");
        result.CacheKey.Should().Be("*.csproj");
        result.Effectiveness.Should().Be(CacheEffectiveness.Medium);
        
        dependencyInfo.CacheRecommendation.IsRecommended.Should().BeTrue();
    }

    [Fact]
    public async Task RecommendCacheConfigurationAsync_WithMavenDependencies_ShouldRecommendMavenCache()
    {
        // Arrange
        var dependencyInfo = new DependencyInfo
        {
            PackageManager = "maven",
            Dependencies = new List<PackageDependency>
            {
                new() { Name = "org.springframework.boot:spring-boot-starter-web" }
            }
        };

        // Act
        var result = await _analyzer.RecommendCacheConfigurationAsync(dependencyInfo);

        // Assert
        result.CachePaths.Should().Contain("~/.m2/repository/");
        result.CacheKey.Should().Be("pom.xml");
        result.Effectiveness.Should().Be(CacheEffectiveness.High);
        
        dependencyInfo.CacheRecommendation.EstimatedTimeSavings.Should().Be(TimeSpan.FromMinutes(3));
    }

    [Fact]
    public async Task RecommendCacheConfigurationAsync_WithFewDependencies_ShouldNotRecommendCache()
    {
        // Arrange
        var dependencyInfo = new DependencyInfo
        {
            PackageManager = "npm",
            Dependencies = new List<PackageDependency>
            {
                new() { Name = "express", Version = "^4.18.0" }
            }
        };

        // Act
        var result = await _analyzer.RecommendCacheConfigurationAsync(dependencyInfo);

        // Assert
        dependencyInfo.CacheRecommendation.IsRecommended.Should().BeFalse();
        dependencyInfo.CacheRecommendation.Reason.Should().Contain("Moderate number of dependencies detected");
    }

    [Fact]
    public async Task RecommendCacheConfigurationAsync_WithUnknownPackageManager_ShouldNotRecommendCache()
    {
        // Arrange
        var dependencyInfo = new DependencyInfo
        {
            PackageManager = "unknown",
            Dependencies = new List<PackageDependency>
            {
                new() { Name = "some-package" }
            }
        };

        // Act
        var result = await _analyzer.RecommendCacheConfigurationAsync(dependencyInfo);

        // Assert
        dependencyInfo.CacheRecommendation.IsRecommended.Should().BeFalse();
        dependencyInfo.CacheRecommendation.Reason.Should().Be("Unknown package manager");
    }

    #endregion

    #region RecommendSecurityScanningAsync Tests

    [Fact]
    public async Task RecommendSecurityScanningAsync_WithSecuritySensitiveDependencies_ShouldRecommendHighRiskScanning()
    {
        // Arrange
        var dependencyInfo = new DependencyInfo
        {
            PackageManager = "npm",
            Dependencies = new List<PackageDependency>
            {
                new() { Name = "express", IsSecuritySensitive = true },
                new() { Name = "lodash", IsSecuritySensitive = true }
            },
            HasSecuritySensitiveDependencies = true
        };

        // Act
        var result = await _analyzer.RecommendSecurityScanningAsync(dependencyInfo);

        // Assert
        result.RiskLevel.Should().Be(SecurityRiskLevel.High);
        result.IsRecommended.Should().BeTrue();
        result.Reason.Should().Be("Security-sensitive dependencies detected");
        result.RecommendedScanners.Should().NotBeEmpty();
        
        var npmAuditScanner = result.RecommendedScanners.FirstOrDefault(s => s.Name == "npm audit");
        npmAuditScanner.Should().NotBeNull();
        npmAuditScanner!.Type.Should().Be(SecurityScanType.DependencyScanning);
        npmAuditScanner.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task RecommendSecurityScanningAsync_WithManyDependencies_ShouldRecommendMediumRiskScanning()
    {
        // Arrange
        var dependencyInfo = new DependencyInfo
        {
            PackageManager = "dotnet",
            Dependencies = Enumerable.Range(1, 60).Select(i => new PackageDependency { Name = $"Package{i}" }).ToList(),
            HasSecuritySensitiveDependencies = false
        };

        // Act
        var result = await _analyzer.RecommendSecurityScanningAsync(dependencyInfo);

        // Assert
        result.RiskLevel.Should().Be(SecurityRiskLevel.Medium);
        result.IsRecommended.Should().BeTrue();
        result.Reason.Should().Be("Large number of dependencies increases security risk");
        
        var securityCodeScanScanner = result.RecommendedScanners.FirstOrDefault(s => s.Name == "Security Code Scan");
        securityCodeScanScanner.Should().NotBeNull();
        securityCodeScanScanner!.Type.Should().Be(SecurityScanType.SAST);
    }

    [Fact]
    public async Task RecommendSecurityScanningAsync_WithPythonDependencies_ShouldRecommendPythonScanners()
    {
        // Arrange
        var dependencyInfo = new DependencyInfo
        {
            PackageManager = "pip",
            Dependencies = new List<PackageDependency>
            {
                new() { Name = "django", IsSecuritySensitive = true },
                new() { Name = "requests", IsSecuritySensitive = true }
            },
            HasSecuritySensitiveDependencies = true
        };

        // Act
        var result = await _analyzer.RecommendSecurityScanningAsync(dependencyInfo);

        // Assert
        result.RiskLevel.Should().Be(SecurityRiskLevel.High);
        
        var safetyScanner = result.RecommendedScanners.FirstOrDefault(s => s.Name == "safety");
        safetyScanner.Should().NotBeNull();
        safetyScanner!.Type.Should().Be(SecurityScanType.DependencyScanning);
        
        var banditScanner = result.RecommendedScanners.FirstOrDefault(s => s.Name == "bandit");
        banditScanner.Should().NotBeNull();
        banditScanner!.Type.Should().Be(SecurityScanType.SAST);
    }

    [Fact]
    public async Task RecommendSecurityScanningAsync_WithJavaDependencies_ShouldRecommendJavaScanners()
    {
        // Arrange
        var dependencyInfo = new DependencyInfo
        {
            PackageManager = "maven",
            Dependencies = new List<PackageDependency>
            {
                new() { Name = "org.springframework.boot:spring-boot-starter-web", IsSecuritySensitive = true }
            },
            HasSecuritySensitiveDependencies = true
        };

        // Act
        var result = await _analyzer.RecommendSecurityScanningAsync(dependencyInfo);

        // Assert
        result.RiskLevel.Should().Be(SecurityRiskLevel.High);
        
        var owaspScanner = result.RecommendedScanners.FirstOrDefault(s => s.Name == "OWASP Dependency Check");
        owaspScanner.Should().NotBeNull();
        owaspScanner!.Type.Should().Be(SecurityScanType.DependencyScanning);
        
        var spotBugsScanner = result.RecommendedScanners.FirstOrDefault(s => s.Name == "SpotBugs");
        spotBugsScanner.Should().NotBeNull();
        spotBugsScanner!.Type.Should().Be(SecurityScanType.SAST);
    }

    [Fact]
    public async Task RecommendSecurityScanningAsync_ShouldAlwaysIncludeSecretDetection()
    {
        // Arrange
        var dependencyInfo = new DependencyInfo
        {
            PackageManager = "npm",
            Dependencies = new List<PackageDependency>
            {
                new() { Name = "express" }
            }
        };

        // Act
        var result = await _analyzer.RecommendSecurityScanningAsync(dependencyInfo);

        // Assert
        var secretDetectionScanner = result.RecommendedScanners.FirstOrDefault(s => s.Type == SecurityScanType.SecretDetection);
        secretDetectionScanner.Should().NotBeNull();
        secretDetectionScanner!.Name.Should().Be("GitLab Secret Detection");
        secretDetectionScanner.IsDefault.Should().BeTrue();
    }

    #endregion

    #region DetectRuntimeRequirementsAsync Tests

    [Fact]
    public async Task DetectRuntimeRequirementsAsync_WithNpmDependencies_ShouldDetectNodeRuntime()
    {
        // Arrange
        var dependencyInfo = new DependencyInfo
        {
            PackageManager = "npm"
        };

        // Act
        var result = await _analyzer.DetectRuntimeRequirementsAsync(dependencyInfo);

        // Assert
        result.Name.Should().Be("node");
        result.Version.Should().Be(">=16.0.0");
        result.RecommendedBaseImages.Should().Contain("node:18-alpine");
        result.RecommendedBaseImages.Should().Contain("node:16-alpine");
    }

    [Fact]
    public async Task DetectRuntimeRequirementsAsync_WithDotNetDependencies_ShouldDetectDotNetRuntime()
    {
        // Arrange
        var dependencyInfo = new DependencyInfo
        {
            PackageManager = "dotnet"
        };

        // Act
        var result = await _analyzer.DetectRuntimeRequirementsAsync(dependencyInfo);

        // Assert
        result.Name.Should().Be("dotnet");
        result.Version.Should().Be("8.0");
        result.RecommendedBaseImages.Should().Contain("mcr.microsoft.com/dotnet/aspnet:8.0");
        result.RecommendedBaseImages.Should().Contain("mcr.microsoft.com/dotnet/runtime:8.0");
    }

    [Fact]
    public async Task DetectRuntimeRequirementsAsync_WithPythonDependencies_ShouldDetectPythonRuntime()
    {
        // Arrange
        var dependencyInfo = new DependencyInfo
        {
            PackageManager = "pip"
        };

        // Act
        var result = await _analyzer.DetectRuntimeRequirementsAsync(dependencyInfo);

        // Assert
        result.Name.Should().Be("python");
        result.Version.Should().Be(">=3.8");
        result.RecommendedBaseImages.Should().Contain("python:3.11-slim");
        result.RecommendedBaseImages.Should().Contain("python:3.10-slim");
    }

    [Fact]
    public async Task DetectRuntimeRequirementsAsync_WithJavaDependencies_ShouldDetectJavaRuntime()
    {
        // Arrange
        var dependencyInfo = new DependencyInfo
        {
            PackageManager = "maven"
        };

        // Act
        var result = await _analyzer.DetectRuntimeRequirementsAsync(dependencyInfo);

        // Assert
        result.Name.Should().Be("java");
        result.Version.Should().Be("17");
        result.RecommendedBaseImages.Should().Contain("openjdk:17-jre-slim");
        result.RecommendedBaseImages.Should().Contain("eclipse-temurin:17-jre");
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task AnalyzePackageFileAsync_WithNullContent_ShouldHandleGracefully()
    {
        // Act
        var result = await _analyzer.AnalyzePackageFileAsync("package.json", null!);

        // Assert
        result.PackageManager.Should().Be("npm");
        result.Confidence.Should().Be(AnalysisConfidence.Low);
        result.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzePackageFileAsync_WithEmptyContent_ShouldHandleGracefully()
    {
        // Act
        var result = await _analyzer.AnalyzePackageFileAsync("package.json", "");

        // Assert
        result.PackageManager.Should().Be("npm");
        result.Confidence.Should().Be(AnalysisConfidence.Low);
        result.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public async Task RecommendCacheConfigurationAsync_WithNullDependencyInfo_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _analyzer.RecommendCacheConfigurationAsync(null!));
    }

    [Fact]
    public async Task RecommendSecurityScanningAsync_WithNullDependencyInfo_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _analyzer.RecommendSecurityScanningAsync(null!));
    }

    [Fact]
    public async Task DetectRuntimeRequirementsAsync_WithNullDependencyInfo_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _analyzer.DetectRuntimeRequirementsAsync(null!));
    }

    #endregion
}