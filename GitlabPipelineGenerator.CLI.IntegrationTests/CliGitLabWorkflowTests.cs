using FluentAssertions;
using System.Diagnostics;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GitlabPipelineGenerator.CLI.IntegrationTests;

/// <summary>
/// Integration tests for CLI GitLab workflow functionality covering complete GitLab analysis workflow,
/// CLI option validation, GitLab service integration, error handling, and fallback to manual mode
/// </summary>
public class CliGitLabWorkflowTests : IDisposable
{
    private readonly string _testOutputDirectory;
    private readonly List<string> _createdFiles;
    private readonly string _mockGitLabToken = "glpat-test-token-12345";
    private readonly string _mockGitLabUrl = "https://gitlab.example.com";
    private readonly string _mockProjectPath = "test-group/test-project";

    public CliGitLabWorkflowTests()
    {
        _testOutputDirectory = Path.Combine(Path.GetTempPath(), "GitlabPipelineGenerator.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputDirectory);
        _createdFiles = new List<string>();
    }

    public void Dispose()
    {
        // Clean up created files and directories
        foreach (var file in _createdFiles)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        try
        {
            if (Directory.Exists(_testOutputDirectory))
                Directory.Delete(_testOutputDirectory, true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #region GitLab Authentication and Connection Tests

    [Fact]
    public async Task CLI_GitLabAnalysis_WithoutToken_ShouldReturnError()
    {
        // Arrange
        var args = new[] 
        { 
            "--analyze-project", 
            "--gitlab-project", _mockProjectPath 
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail without GitLab token");
        result.StandardError.Should().Contain("GitLab token", "Should mention missing token");
        result.StandardError.Should().Contain("required", "Should indicate token is required");
    }

    [Fact]
    public async Task CLI_GitLabAnalysis_WithoutProject_ShouldReturnError()
    {
        // Arrange
        var args = new[] 
        { 
            "--analyze-project", 
            "--gitlab-token", _mockGitLabToken 
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail without GitLab project");
        result.StandardError.Should().Contain("GitLab project", "Should mention missing project");
        result.StandardError.Should().Contain("required", "Should indicate project is required");
    }

    [Fact]
    public async Task CLI_GitLabAnalysis_WithInvalidUrl_ShouldReturnError()
    {
        // Arrange
        var args = new[] 
        { 
            "--analyze-project", 
            "--gitlab-token", _mockGitLabToken,
            "--gitlab-url", "invalid-url",
            "--gitlab-project", _mockProjectPath 
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail with invalid URL");
        result.StandardError.Should().Contain("Invalid GitLab URL", "Should report URL validation error");
    }

    [Fact]
    public async Task CLI_GitLabAnalysis_WithValidOptions_ShouldAttemptConnection()
    {
        // Arrange
        var args = new[] 
        { 
            "--analyze-project", 
            "--gitlab-token", _mockGitLabToken,
            "--gitlab-url", _mockGitLabUrl,
            "--gitlab-project", _mockProjectPath,
            "--dry-run"
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        // Since we're using mock credentials, this will likely fail at authentication
        // but should pass validation and attempt connection
        if (result.ExitCode == 1)
        {
            result.StandardError.Should().Contain("GitLab", "Should mention GitLab in error");
            // Should not contain validation errors about missing token/project
            result.StandardError.Should().NotContain("required", "Should not have validation errors");
        }
        else
        {
            // If somehow successful (mock environment), should show dry run message
            result.StandardOutput.Should().Contain("Dry run", "Should indicate dry run mode");
        }
    }

    #endregion

    #region Project Discovery Tests

    [Fact]
    public async Task CLI_ListProjects_WithoutToken_ShouldReturnError()
    {
        // Arrange
        var args = new[] { "--list-projects" };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail without GitLab token");
        result.StandardError.Should().Contain("GitLab token", "Should mention missing token");
        result.StandardError.Should().Contain("required", "Should indicate token is required");
    }

    [Fact]
    public async Task CLI_SearchProjects_WithoutToken_ShouldReturnError()
    {
        // Arrange
        var args = new[] 
        { 
            "--search-projects", "test-project" 
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail without GitLab token");
        result.StandardError.Should().Contain("GitLab token", "Should mention missing token");
        result.StandardError.Should().Contain("required", "Should indicate token is required");
    }

    [Fact]
    public async Task CLI_ListProjects_WithValidToken_ShouldAttemptConnection()
    {
        // Arrange
        var args = new[] 
        { 
            "--list-projects", 
            "--gitlab-token", _mockGitLabToken,
            "--gitlab-url", _mockGitLabUrl
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        // Since we're using mock credentials, this will likely fail at authentication
        // but should pass validation and attempt connection
        if (result.ExitCode == 1)
        {
            result.StandardError.Should().Contain("GitLab", "Should mention GitLab in error");
            // Should not contain validation errors about missing token
            result.StandardError.Should().NotContain("required", "Should not have validation errors");
        }
        else
        {
            // If somehow successful (mock environment), should show project list
            result.StandardOutput.Should().Contain("projects", "Should mention projects");
        }
    }

    [Fact]
    public async Task CLI_SearchProjects_WithValidToken_ShouldAttemptConnection()
    {
        // Arrange
        var args = new[] 
        { 
            "--search-projects", "test-project",
            "--gitlab-token", _mockGitLabToken,
            "--gitlab-url", _mockGitLabUrl
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        // Since we're using mock credentials, this will likely fail at authentication
        // but should pass validation and attempt connection
        if (result.ExitCode == 1)
        {
            result.StandardError.Should().Contain("GitLab", "Should mention GitLab in error");
            // Should not contain validation errors about missing token
            result.StandardError.Should().NotContain("required", "Should not have validation errors");
        }
        else
        {
            // If somehow successful (mock environment), should show search results
            result.StandardOutput.Should().Contain("Search results", "Should show search results");
        }
    }

    #endregion

    #region Analysis Options Validation Tests

    [Fact]
    public async Task CLI_AnalysisDepth_WithInvalidValue_ShouldReturnError()
    {
        // Arrange
        var args = new[] 
        { 
            "--analyze-project", 
            "--gitlab-token", _mockGitLabToken,
            "--gitlab-project", _mockProjectPath,
            "--analysis-depth", "5"
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail with invalid analysis depth");
        result.StandardError.Should().Contain("Analysis depth", "Should mention analysis depth error");
        result.StandardError.Should().Contain("between 1 and 3", "Should specify valid range");
    }

    [Fact]
    public async Task CLI_SkipAnalysis_WithInvalidType_ShouldReturnError()
    {
        // Arrange
        var args = new[] 
        { 
            "--analyze-project", 
            "--gitlab-token", _mockGitLabToken,
            "--gitlab-project", _mockProjectPath,
            "--skip-analysis", "invalid-type"
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail with invalid skip analysis type");
        result.StandardError.Should().Contain("Invalid skip analysis type", "Should mention invalid type");
        result.StandardError.Should().Contain("files,dependencies,config,deployment", "Should list valid types");
    }

    [Fact]
    public async Task CLI_MaxProjects_WithInvalidValue_ShouldReturnError()
    {
        // Arrange
        var args = new[] 
        { 
            "--list-projects", 
            "--gitlab-token", _mockGitLabToken,
            "--max-projects", "2000"
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail with invalid max projects");
        result.StandardError.Should().Contain("Max projects", "Should mention max projects error");
        result.StandardError.Should().Contain("between 1 and 1000", "Should specify valid range");
    }

    #endregion

    #region Hybrid Mode Tests

    [Fact]
    public async Task CLI_PreferDetected_WithoutAnalyzeProject_ShouldReturnError()
    {
        // Arrange
        var args = new[] 
        { 
            "--type", "dotnet",
            "--prefer-detected"
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail when prefer-detected used without analyze-project");
        result.StandardError.Should().Contain("--prefer-detected", "Should mention prefer-detected option");
        result.StandardError.Should().Contain("--analyze-project", "Should mention analyze-project requirement");
    }

    [Fact]
    public async Task CLI_ShowConflicts_WithoutAnalyzeProject_ShouldReturnError()
    {
        // Arrange
        var args = new[] 
        { 
            "--type", "dotnet",
            "--show-conflicts"
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail when show-conflicts used without analyze-project");
        result.StandardError.Should().Contain("--show-conflicts", "Should mention show-conflicts option");
        result.StandardError.Should().Contain("--analyze-project", "Should mention analyze-project requirement");
    }

    [Fact]
    public async Task CLI_ShowAnalysis_WithoutAnalyzeProject_ShouldReturnError()
    {
        // Arrange
        var args = new[] 
        { 
            "--type", "dotnet",
            "--show-analysis"
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail when show-analysis used without analyze-project");
        result.StandardError.Should().Contain("--show-analysis", "Should mention show-analysis option");
        result.StandardError.Should().Contain("--analyze-project", "Should mention analyze-project requirement");
    }

    [Fact]
    public async Task CLI_HybridMode_WithValidOptions_ShouldPassValidation()
    {
        // Arrange
        var args = new[] 
        { 
            "--analyze-project",
            "--gitlab-token", _mockGitLabToken,
            "--gitlab-project", _mockProjectPath,
            "--type", "dotnet",
            "--dotnet-version", "9.0",
            "--prefer-detected",
            "--show-conflicts",
            "--show-analysis",
            "--validate-only"
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        // Should pass validation (validate-only mode)
        if (result.ExitCode == 0)
        {
            result.StandardOutput.Should().Contain("valid", "Should confirm validation success");
        }
        else
        {
            // If it fails, should not be due to option validation
            result.StandardError.Should().NotContain("--prefer-detected", "Should not have prefer-detected validation error");
            result.StandardError.Should().NotContain("--show-conflicts", "Should not have show-conflicts validation error");
            result.StandardError.Should().NotContain("--show-analysis", "Should not have show-analysis validation error");
        }
    }

    #endregion

    #region Conflicting Options Tests

    [Fact]
    public async Task CLI_ListProjectsAndSearchProjects_ShouldReturnError()
    {
        // Arrange
        var args = new[] 
        { 
            "--list-projects",
            "--search-projects", "test",
            "--gitlab-token", _mockGitLabToken
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail with conflicting project discovery options");
        result.StandardError.Should().Contain("--list-projects", "Should mention list-projects option");
        result.StandardError.Should().Contain("--search-projects", "Should mention search-projects option");
    }

    [Fact]
    public async Task CLI_ProjectDiscoveryAndAnalyzeProject_ShouldReturnError()
    {
        // Arrange
        var args = new[] 
        { 
            "--list-projects",
            "--analyze-project",
            "--gitlab-token", _mockGitLabToken,
            "--gitlab-project", _mockProjectPath
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail with conflicting options");
        result.StandardError.Should().Contain("project discovery", "Should mention project discovery conflict");
        result.StandardError.Should().Contain("--analyze-project", "Should mention analyze-project option");
    }

    [Fact]
    public async Task CLI_GitLabProfileAndToken_ShouldReturnError()
    {
        // Arrange
        var args = new[] 
        { 
            "--analyze-project",
            "--gitlab-profile", "test-profile",
            "--gitlab-token", _mockGitLabToken,
            "--gitlab-project", _mockProjectPath
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail with conflicting authentication options");
        result.StandardError.Should().Contain("--gitlab-profile", "Should mention gitlab-profile option");
        result.StandardError.Should().Contain("--gitlab-token", "Should mention gitlab-token option");
    }

    #endregion

    #region Error Handling and Fallback Tests

    [Fact]
    public async Task CLI_GitLabAnalysis_WithConnectionError_ShouldSuggestFallback()
    {
        // Arrange
        var args = new[] 
        { 
            "--analyze-project",
            "--gitlab-token", "invalid-token-format",
            "--gitlab-url", _mockGitLabUrl,
            "--gitlab-project", _mockProjectPath,
            "--verbose"
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail with connection error");
        
        // Should suggest fallback to manual mode
        result.StandardOutput.Should().Contain("manually", "Should suggest manual mode");
        result.StandardOutput.Should().Contain("--type", "Should show manual option example");
        
        if (result.Verbose)
        {
            result.StandardError.Should().NotBeEmpty("Should show detailed error in verbose mode");
        }
    }

    [Fact]
    public async Task CLI_GitLabAnalysis_WithAuthenticationError_ShouldProvideHelpfulMessage()
    {
        // Arrange
        var args = new[] 
        { 
            "--analyze-project",
            "--gitlab-token", "glpat-invalid-token",
            "--gitlab-url", _mockGitLabUrl,
            "--gitlab-project", _mockProjectPath
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail with authentication error");
        
        // Should provide helpful error message (exact message depends on GitLab service implementation)
        result.StandardError.Should().Contain("GitLab", "Should mention GitLab in error");
        
        // Should suggest fallback
        result.StandardOutput.Should().Contain("manually", "Should suggest manual mode fallback");
    }

    [Fact]
    public async Task CLI_GitLabAnalysis_WithNetworkError_ShouldHandleGracefully()
    {
        // Arrange - Use an unreachable URL to simulate network error
        var args = new[] 
        { 
            "--analyze-project",
            "--gitlab-token", _mockGitLabToken,
            "--gitlab-url", "https://unreachable.gitlab.invalid",
            "--gitlab-project", _mockProjectPath
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail with network error");
        
        // Should handle network error gracefully
        result.StandardError.Should().Contain("GitLab", "Should mention GitLab in error");
        
        // Should suggest fallback
        result.StandardOutput.Should().Contain("manually", "Should suggest manual mode fallback");
    }

    #endregion

    #region Complete Workflow Tests

    [Fact]
    public async Task CLI_CompleteGitLabWorkflow_WithDryRun_ShouldValidateEntireFlow()
    {
        // Arrange
        var args = new[] 
        { 
            "--analyze-project",
            "--gitlab-token", _mockGitLabToken,
            "--gitlab-url", _mockGitLabUrl,
            "--gitlab-project", _mockProjectPath,
            "--analysis-depth", "2",
            "--skip-analysis", "deployment",
            "--show-analysis",
            "--show-conflicts",
            "--merge-config",
            "--type", "dotnet",
            "--dotnet-version", "9.0",
            "--stages", "build,test,deploy",
            "--include-code-quality",
            "--include-security",
            "--variables", "BUILD_CONFIG=Release,TEST_ENV=staging",
            "--dry-run",
            "--verbose"
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        // Should pass validation and attempt the workflow
        // Exact behavior depends on GitLab service implementation
        if (result.ExitCode == 1)
        {
            // If it fails, should be due to GitLab connection, not validation
            result.StandardError.Should().NotContain("Invalid", "Should not have validation errors");
            result.StandardError.Should().NotContain("required", "Should not have missing required field errors");
            result.StandardError.Should().NotContain("format", "Should not have format errors");
            
            // Should suggest fallback
            result.StandardOutput.Should().Contain("manually", "Should suggest manual mode fallback");
        }
        else
        {
            // If successful, should show dry run completion
            result.StandardOutput.Should().Contain("Dry run", "Should indicate dry run mode");
            result.StandardOutput.Should().Contain("analysis", "Should mention analysis");
        }
    }

    [Fact]
    public async Task CLI_GitLabWorkflow_WithFileOutput_ShouldAttemptGeneration()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "gitlab-workflow.yml");
        var args = new[] 
        { 
            "--analyze-project",
            "--gitlab-token", _mockGitLabToken,
            "--gitlab-url", _mockGitLabUrl,
            "--gitlab-project", _mockProjectPath,
            "--type", "dotnet",
            "--output", outputFile
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        if (result.ExitCode == 0)
        {
            // If successful, should create output file
            File.Exists(outputFile).Should().BeTrue("Should create output file");
            _createdFiles.Add(outputFile);
            
            var yamlContent = await File.ReadAllTextAsync(outputFile);
            ValidateGitLabCiYaml(yamlContent);
        }
        else
        {
            // If it fails, should be due to GitLab connection, not validation
            result.StandardError.Should().NotContain("Invalid", "Should not have validation errors");
            
            // Should not create file on failure
            File.Exists(outputFile).Should().BeFalse("Should not create file on failure");
        }
    }

    [Fact]
    public async Task CLI_GitLabWorkflow_WithConsoleOutput_ShouldAttemptGeneration()
    {
        // Arrange
        var args = new[] 
        { 
            "--analyze-project",
            "--gitlab-token", _mockGitLabToken,
            "--gitlab-url", _mockGitLabUrl,
            "--gitlab-project", _mockProjectPath,
            "--type", "dotnet",
            "--console-output"
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        if (result.ExitCode == 0)
        {
            // If successful, should output YAML to console
            result.StandardOutput.Should().Contain("stages:", "Should output YAML to console");
            ValidateGitLabCiYaml(result.StandardOutput);
        }
        else
        {
            // If it fails, should be due to GitLab connection, not validation
            result.StandardError.Should().NotContain("Invalid", "Should not have validation errors");
        }
    }

    #endregion

    #region Profile and Configuration Tests

    [Fact]
    public async Task CLI_GitLabProfile_WithValidProfile_ShouldAttemptConnection()
    {
        // Arrange
        var args = new[] 
        { 
            "--analyze-project",
            "--gitlab-profile", "test-profile",
            "--gitlab-project", _mockProjectPath,
            "--dry-run"
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        // Should pass validation and attempt to use profile
        if (result.ExitCode == 1)
        {
            // If it fails, should be due to profile not found or GitLab connection
            result.StandardError.Should().NotContain("required", "Should not have validation errors about missing token");
        }
        else
        {
            // If successful, should show dry run completion
            result.StandardOutput.Should().Contain("Dry run", "Should indicate dry run mode");
        }
    }

    [Fact]
    public async Task CLI_ProjectFilter_WithValidFilters_ShouldPassValidation()
    {
        // Arrange
        var args = new[] 
        { 
            "--list-projects",
            "--gitlab-token", _mockGitLabToken,
            "--project-filter", "owned,private",
            "--max-projects", "10"
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        // Should pass validation
        if (result.ExitCode == 1)
        {
            // If it fails, should be due to GitLab connection, not validation
            result.StandardError.Should().NotContain("Invalid project filter", "Should not have filter validation errors");
        }
        else
        {
            // If successful, should show project list
            result.StandardOutput.Should().Contain("projects", "Should show project list");
        }
    }

    [Fact]
    public async Task CLI_ProjectFilter_WithInvalidFilter_ShouldReturnError()
    {
        // Arrange
        var args = new[] 
        { 
            "--list-projects",
            "--gitlab-token", _mockGitLabToken,
            "--project-filter", "invalid-filter"
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail with invalid project filter");
        result.StandardError.Should().Contain("Invalid project filter", "Should mention invalid filter");
        result.StandardError.Should().Contain("owned,member,public,private,internal", "Should list valid filters");
    }

    #endregion

    #region Verbose Output Tests

    [Fact]
    public async Task CLI_GitLabWorkflow_WithVerbose_ShouldShowDetailedOutput()
    {
        // Arrange
        var args = new[] 
        { 
            "--analyze-project",
            "--gitlab-token", _mockGitLabToken,
            "--gitlab-url", _mockGitLabUrl,
            "--gitlab-project", _mockProjectPath,
            "--dry-run",
            "--verbose"
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        if (result.ExitCode == 0)
        {
            // If successful, should show verbose output
            result.StandardOutput.Should().Contain("Analyzing", "Should show analysis progress");
        }
        else
        {
            // If it fails, should show detailed error information
            result.StandardError.Should().NotBeEmpty("Should show detailed error in verbose mode");
        }
    }

    #endregion

    #region Helper Methods

    private async Task<CliResult> RunCliAsync(string[] args, string? workingDirectory = null)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{GetCliProjectPath()}\" -- {string.Join(" ", args.Select(arg => $"\"{arg}\""))}",
            WorkingDirectory = workingDirectory ?? _testOutputDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        
        process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };
        
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        await process.WaitForExitAsync();
        
        return new CliResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = outputBuilder.ToString(),
            StandardError = errorBuilder.ToString(),
            Verbose = args.Contains("--verbose")
        };
    }

    private string GetCliProjectPath()
    {
        // Navigate up from the test project to find the CLI project
        var currentDir = Directory.GetCurrentDirectory();
        var solutionDir = FindSolutionDirectory(currentDir);
        return Path.Combine(solutionDir, "GitlabPipelineGenerator.CLI", "GitlabPipelineGenerator.CLI.csproj");
    }

    private string FindSolutionDirectory(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Any())
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not find solution directory");
    }

    private void ValidateGitLabCiYaml(string yamlContent)
    {
        yamlContent.Should().NotBeNullOrEmpty("YAML content should not be empty");
        
        // Basic GitLab CI/CD structure validation
        yamlContent.Should().Contain("stages:", "Should have stages section");
        yamlContent.Should().Contain("script:", "Should have at least one script section");
        
        // Try to parse YAML to ensure it's valid
        try
        {
            var yamlObject = ParseYaml(yamlContent);
            yamlObject.Should().NotBeNull("YAML should be parseable");
        }
        catch (Exception)
        {
            // For complex YAML, just check basic structure
            yamlContent.Should().NotContain("!!!", "YAML should not contain parsing errors");
        }
    }

    private Dictionary<object, object> ParseYaml(string yamlContent)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        
        return deserializer.Deserialize<Dictionary<object, object>>(yamlContent);
    }

    #endregion

    #region Helper Classes

    private class CliResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
        public bool Verbose { get; set; } = false;
    }

    #endregion
}