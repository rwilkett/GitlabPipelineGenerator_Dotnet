using FluentAssertions;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Services;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

/// <summary>
/// Unit tests for FilePatternAnalyzer
/// </summary>
public class FilePatternAnalyzerTests
{
    private readonly FilePatternAnalyzer _analyzer;

    public FilePatternAnalyzerTests()
    {
        _analyzer = new FilePatternAnalyzer();
    }

    #region DetectProjectTypeAsync Tests

    [Fact]
    public async Task DetectProjectTypeAsync_WithDotNetFiles_ShouldReturnDotNet()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "MyProject.csproj", Path = "MyProject.csproj", Type = "blob" },
            new() { Name = "Program.cs", Path = "src/Program.cs", Type = "blob" },
            new() { Name = "appsettings.json", Path = "appsettings.json", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectProjectTypeAsync(files);

        // Assert
        result.Should().Be(ProjectType.DotNet);
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithNodeJsFiles_ShouldReturnNodeJs()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "package.json", Path = "package.json", Type = "blob" },
            new() { Name = "index.js", Path = "src/index.js", Type = "blob" },
            new() { Name = "package-lock.json", Path = "package-lock.json", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectProjectTypeAsync(files);

        // Assert
        result.Should().Be(ProjectType.NodeJs);
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithPythonFiles_ShouldReturnPython()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "main.py", Path = "src/main.py", Type = "blob" },
            new() { Name = "requirements.txt", Path = "requirements.txt", Type = "blob" },
            new() { Name = "setup.py", Path = "setup.py", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectProjectTypeAsync(files);

        // Assert
        result.Should().Be(ProjectType.Python);
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithJavaFiles_ShouldReturnJava()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "pom.xml", Path = "pom.xml", Type = "blob" },
            new() { Name = "Application.java", Path = "src/main/java/Application.java", Type = "blob" },
            new() { Name = "Test.java", Path = "src/test/java/Test.java", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectProjectTypeAsync(files);

        // Assert
        result.Should().Be(ProjectType.Java);
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithGoFiles_ShouldReturnGo()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "go.mod", Path = "go.mod", Type = "blob" },
            new() { Name = "main.go", Path = "main.go", Type = "blob" },
            new() { Name = "go.sum", Path = "go.sum", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectProjectTypeAsync(files);

        // Assert
        result.Should().Be(ProjectType.Go);
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithDockerFiles_ShouldReturnDocker()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "Dockerfile", Path = "Dockerfile", Type = "blob" },
            new() { Name = "docker-compose.yml", Path = "docker-compose.yml", Type = "blob" },
            new() { Name = ".dockerignore", Path = ".dockerignore", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectProjectTypeAsync(files);

        // Assert
        result.Should().Be(ProjectType.Docker);
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithStaticFiles_ShouldReturnStatic()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "index.html", Path = "index.html", Type = "blob" },
            new() { Name = "style.css", Path = "css/style.css", Type = "blob" },
            new() { Name = "script.js", Path = "js/script.js", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectProjectTypeAsync(files);

        // Assert
        result.Should().Be(ProjectType.Static);
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithMixedFiles_ShouldReturnMixed()
    {
        // Arrange - Files with similar scores
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "package.json", Path = "package.json", Type = "blob" },
            new() { Name = "index.js", Path = "src/index.js", Type = "blob" },
            new() { Name = "MyProject.csproj", Path = "MyProject.csproj", Type = "blob" },
            new() { Name = "Program.cs", Path = "src/Program.cs", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectProjectTypeAsync(files);

        // Assert
        result.Should().BeOneOf(ProjectType.Mixed, ProjectType.NodeJs, ProjectType.DotNet);
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithNoRecognizableFiles_ShouldReturnUnknown()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "README.md", Path = "README.md", Type = "blob" },
            new() { Name = "LICENSE", Path = "LICENSE", Type = "blob" },
            new() { Name = "random.txt", Path = "random.txt", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectProjectTypeAsync(files);

        // Assert
        result.Should().Be(ProjectType.Unknown);
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithEmptyFileList_ShouldReturnUnknown()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>();

        // Act
        var result = await _analyzer.DetectProjectTypeAsync(files);

        // Assert
        result.Should().Be(ProjectType.Unknown);
    }

    #endregion

    #region DetectFrameworksAsync Tests

    [Fact]
    public async Task DetectFrameworksAsync_WithAspNetCoreFiles_ShouldDetectAspNetCore()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() 
            { 
                Name = "MyProject.csproj", 
                Path = "MyProject.csproj", 
                Type = "blob",
                Content = "<Project><ItemGroup><PackageReference Include=\"Microsoft.AspNetCore.App\" /></ItemGroup></Project>"
            },
            new() { Name = "Program.cs", Path = "Program.cs", Type = "blob" },
            new() { Name = "Startup.cs", Path = "Startup.cs", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectFrameworksAsync(files);

        // Assert
        result.Name.Should().Be("ASP.NET Core");
        result.Confidence.Should().Be(AnalysisConfidence.High);
    }

    [Fact]
    public async Task DetectFrameworksAsync_WithReactFiles_ShouldDetectReact()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() 
            { 
                Name = "package.json", 
                Path = "package.json", 
                Type = "blob",
                Content = "{\"dependencies\": {\"react\": \"^18.0.0\"}}"
            },
            new() { Name = "App.jsx", Path = "src/App.jsx", Type = "blob" },
            new() { Name = "index.tsx", Path = "src/index.tsx", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectFrameworksAsync(files);

        // Assert
        result.Name.Should().Be("React");
        result.Confidence.Should().Be(AnalysisConfidence.High);
    }

    [Fact]
    public async Task DetectFrameworksAsync_WithAngularFiles_ShouldDetectAngular()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() 
            { 
                Name = "package.json", 
                Path = "package.json", 
                Type = "blob",
                Content = "{\"dependencies\": {\"@angular/core\": \"^15.0.0\"}}"
            },
            new() { Name = "angular.json", Path = "angular.json", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectFrameworksAsync(files);

        // Assert
        result.Name.Should().Be("Angular");
        result.Confidence.Should().Be(AnalysisConfidence.High);
    }

    [Fact]
    public async Task DetectFrameworksAsync_WithDjangoFiles_ShouldDetectDjango()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() 
            { 
                Name = "requirements.txt", 
                Path = "requirements.txt", 
                Type = "blob",
                Content = "Django>=4.0.0\npsycopg2>=2.8.0"
            },
            new() { Name = "manage.py", Path = "manage.py", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectFrameworksAsync(files);

        // Assert
        result.Name.Should().Be("Django");
        result.Confidence.Should().Be(AnalysisConfidence.High);
    }

    [Fact]
    public async Task DetectFrameworksAsync_WithNoFrameworkFiles_ShouldReturnUnknown()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "README.md", Path = "README.md", Type = "blob" },
            new() { Name = "LICENSE", Path = "LICENSE", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectFrameworksAsync(files);

        // Assert
        result.Name.Should().Be("Unknown");
        result.Confidence.Should().Be(AnalysisConfidence.Low);
    }

    #endregion

    #region DetectBuildToolsAsync Tests

    [Fact]
    public async Task DetectBuildToolsAsync_WithDotNetProject_ShouldDetectDotNetBuildTool()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "MyProject.csproj", Path = "MyProject.csproj", Type = "blob" },
            new() { Name = "global.json", Path = "global.json", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectBuildToolsAsync(files);

        // Assert
        result.Name.Should().Be("dotnet");
        result.BuildCommands.Should().Contain("dotnet build");
        result.BuildCommands.Should().Contain("dotnet restore");
        result.TestCommands.Should().Contain("dotnet test");
        result.Confidence.Should().Be(AnalysisConfidence.High);
    }

    [Fact]
    public async Task DetectBuildToolsAsync_WithPackageJson_ShouldDetectNpmBuildTool()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() 
            { 
                Name = "package.json", 
                Path = "package.json", 
                Type = "blob",
                Content = "{\"scripts\": {\"build\": \"webpack\", \"test\": \"jest\", \"start\": \"node server.js\"}}"
            }
        };

        // Act
        var result = await _analyzer.DetectBuildToolsAsync(files);

        // Assert
        result.Name.Should().Be("npm");
        result.BuildCommands.Should().Contain("npm run build");
        result.BuildCommands.Should().Contain("npm run start");
        result.TestCommands.Should().Contain("npm run test");
        result.Confidence.Should().Be(AnalysisConfidence.High);
    }

    [Fact]
    public async Task DetectBuildToolsAsync_WithMavenProject_ShouldDetectMavenBuildTool()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "pom.xml", Path = "pom.xml", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectBuildToolsAsync(files);

        // Assert
        result.Name.Should().Be("maven");
        result.BuildCommands.Should().Contain("mvn compile");
        result.BuildCommands.Should().Contain("mvn package");
        result.TestCommands.Should().Contain("mvn test");
        result.Confidence.Should().Be(AnalysisConfidence.High);
    }

    [Fact]
    public async Task DetectBuildToolsAsync_WithGradleProject_ShouldDetectGradleBuildTool()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "build.gradle", Path = "build.gradle", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectBuildToolsAsync(files);

        // Assert
        result.Name.Should().Be("gradle");
        result.BuildCommands.Should().Contain("./gradlew build");
        result.TestCommands.Should().Contain("./gradlew test");
        result.Confidence.Should().Be(AnalysisConfidence.High);
    }

    [Fact]
    public async Task DetectBuildToolsAsync_WithPythonSetup_ShouldDetectPythonBuildTool()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "setup.py", Path = "setup.py", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectBuildToolsAsync(files);

        // Assert
        result.Name.Should().Be("python");
        result.BuildCommands.Should().Contain("python setup.py build");
        result.TestCommands.Should().Contain("python -m pytest");
        result.TestCommands.Should().Contain("python -m unittest");
        result.Confidence.Should().Be(AnalysisConfidence.Medium);
    }

    [Fact]
    public async Task DetectBuildToolsAsync_WithNoBuildFiles_ShouldReturnUnknown()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "README.md", Path = "README.md", Type = "blob" },
            new() { Name = "main.py", Path = "main.py", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectBuildToolsAsync(files);

        // Assert
        result.Name.Should().Be("Unknown");
        result.Confidence.Should().Be(AnalysisConfidence.Low);
    }

    #endregion

    #region DetectTestFrameworksAsync Tests

    [Fact]
    public async Task DetectTestFrameworksAsync_WithXUnitProject_ShouldDetectXUnit()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() 
            { 
                Name = "Tests.csproj", 
                Path = "Tests.csproj", 
                Type = "blob",
                Content = "<Project><ItemGroup><PackageReference Include=\"Microsoft.NET.Test.Sdk\" /><PackageReference Include=\"xunit\" /></ItemGroup></Project>"
            }
        };

        // Act
        var result = await _analyzer.DetectTestFrameworksAsync(files);

        // Assert
        result.Name.Should().Be("xUnit");
        result.TestCommands.Should().Contain("dotnet test");
        result.Confidence.Should().Be(AnalysisConfidence.High);
    }

    [Fact]
    public async Task DetectTestFrameworksAsync_WithNUnitProject_ShouldDetectNUnit()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() 
            { 
                Name = "Tests.csproj", 
                Path = "Tests.csproj", 
                Type = "blob",
                Content = "<Project><ItemGroup><PackageReference Include=\"Microsoft.NET.Test.Sdk\" /><PackageReference Include=\"NUnit\" /></ItemGroup></Project>"
            }
        };

        // Act
        var result = await _analyzer.DetectTestFrameworksAsync(files);

        // Assert
        result.Name.Should().Be("NUnit");
        result.TestCommands.Should().Contain("dotnet test");
        result.Confidence.Should().Be(AnalysisConfidence.High);
    }

    [Fact]
    public async Task DetectTestFrameworksAsync_WithJestProject_ShouldDetectJest()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() 
            { 
                Name = "package.json", 
                Path = "package.json", 
                Type = "blob",
                Content = "{\"devDependencies\": {\"jest\": \"^28.0.0\"}}"
            }
        };

        // Act
        var result = await _analyzer.DetectTestFrameworksAsync(files);

        // Assert
        result.Name.Should().Be("Jest");
        result.TestCommands.Should().Contain("npm test");
        result.TestCommands.Should().Contain("jest");
        result.Confidence.Should().Be(AnalysisConfidence.High);
    }

    [Fact]
    public async Task DetectTestFrameworksAsync_WithPytestFiles_ShouldDetectPytest()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "pytest.ini", Path = "pytest.ini", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectTestFrameworksAsync(files);

        // Assert
        result.Name.Should().Be("pytest");
        result.TestCommands.Should().Contain("pytest");
        result.TestCommands.Should().Contain("python -m pytest");
        result.Confidence.Should().Be(AnalysisConfidence.High);
    }

    [Fact]
    public async Task DetectTestFrameworksAsync_WithTestDirectories_ShouldDetectTestDirectories()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "test_main.py", Path = "tests/test_main.py", Type = "blob" },
            new() { Name = "spec_helper.js", Path = "spec/spec_helper.js", Type = "blob" },
            new() { Name = "UnitTest1.cs", Path = "test/UnitTest1.cs", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectTestFrameworksAsync(files);

        // Assert
        result.TestDirectories.Should().Contain("tests");
        result.TestDirectories.Should().Contain("spec");
        result.TestDirectories.Should().Contain("test");
    }

    [Fact]
    public async Task DetectTestFrameworksAsync_WithNoTestFiles_ShouldReturnUnknown()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "main.py", Path = "src/main.py", Type = "blob" },
            new() { Name = "README.md", Path = "README.md", Type = "blob" }
        };

        // Act
        var result = await _analyzer.DetectTestFrameworksAsync(files);

        // Assert
        result.Name.Should().Be("Unknown");
        result.Confidence.Should().Be(AnalysisConfidence.Low);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task DetectProjectTypeAsync_WithNullFiles_ShouldReturnUnknown()
    {
        // Act
        var result = await _analyzer.DetectProjectTypeAsync(null!);

        // Assert
        result.Should().Be(ProjectType.Unknown);
    }

    [Fact]
    public async Task DetectFrameworksAsync_WithNullFiles_ShouldReturnUnknownFramework()
    {
        // Act
        var result = await _analyzer.DetectFrameworksAsync(null!);

        // Assert
        result.Name.Should().Be("Unknown");
        result.Confidence.Should().Be(AnalysisConfidence.Low);
    }

    [Fact]
    public async Task DetectBuildToolsAsync_WithNullFiles_ShouldReturnUnknownBuildTool()
    {
        // Act
        var result = await _analyzer.DetectBuildToolsAsync(null!);

        // Assert
        result.Name.Should().Be("Unknown");
        result.Confidence.Should().Be(AnalysisConfidence.Low);
    }

    [Fact]
    public async Task DetectTestFrameworksAsync_WithNullFiles_ShouldReturnUnknownTestFramework()
    {
        // Act
        var result = await _analyzer.DetectTestFrameworksAsync(null!);

        // Assert
        result.Name.Should().Be("Unknown");
        result.Confidence.Should().Be(AnalysisConfidence.Low);
    }

    [Fact]
    public async Task DetectProjectTypeAsync_WithFilesContainingNullPaths_ShouldHandleGracefully()
    {
        // Arrange
        var files = new List<GitLabRepositoryFile>
        {
            new() { Name = "package.json", Path = null!, Type = "blob" },
            new() { Name = "index.js", Path = "src/index.js", Type = "blob" }
        };

        // Act & Assert
        var result = await _analyzer.DetectProjectTypeAsync(files);
        result.Should().NotBe(ProjectType.Unknown); // Should still detect based on valid files
    }

    #endregion
}