

namespace GitlabPipelineGenerator.CLI.IntegrationTests;

/// <summary>
/// Integration tests for CLI functionality covering end-to-end workflows,
/// file output scenarios, error handling, and YAML validation
/// </summary>
public class CliIntegrationTests : IDisposable
{
    private readonly string _testOutputDirectory;
    private readonly string _cliExecutablePath;
    private readonly List<string> _createdFiles;

    public CliIntegrationTests()
    {
        _testOutputDirectory = Path.Combine(Path.GetTempPath(), "GitlabPipelineGenerator.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputDirectory);
        
        // Find the CLI executable
        _cliExecutablePath = FindCliExecutable();
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

    #region Basic CLI Workflow Tests

    [Fact]
    public async Task CLI_BasicDotNetPipeline_ShouldGenerateValidYaml()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "basic-dotnet.yml");
        var args = new[] { "--type", "dotnet", "--output", outputFile };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0, "CLI should succeed with valid arguments");
        result.StandardError.Should().BeEmpty("No errors should be reported");
        
        File.Exists(outputFile).Should().BeTrue("Output file should be created");
        _createdFiles.Add(outputFile);

        var yamlContent = await File.ReadAllTextAsync(outputFile);
        yamlContent.Should().NotBeEmpty("Generated YAML should not be empty");
        
        // Validate YAML structure
        ValidateGitLabCiYaml(yamlContent);
        
        // Check for expected .NET pipeline elements
        yamlContent.Should().Contain("stages:", "Pipeline should define stages");
        yamlContent.Should().Contain("build", "Pipeline should include build stage");
        yamlContent.Should().Contain("test", "Pipeline should include test stage");
        yamlContent.Should().Contain("deploy", "Pipeline should include deploy stage");
    }

    [Fact]
    public async Task CLI_WithCustomStages_ShouldGenerateCorrectStages()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "custom-stages.yml");
        var args = new[] { "--type", "dotnet", "--stages", "build,quality,deploy", "--output", outputFile };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        
        var yamlContent = await File.ReadAllTextAsync(outputFile);
        _createdFiles.Add(outputFile);
        
        yamlContent.Should().Contain("build", "Should contain build stage");
        yamlContent.Should().Contain("quality", "Should contain quality stage");
        yamlContent.Should().Contain("deploy", "Should contain deploy stage");
        yamlContent.Should().NotContain("test", "Should not contain test stage");
    }

    [Fact]
    public async Task CLI_WithDotNetVersion_ShouldUseSpecifiedVersion()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "dotnet-version.yml");
        var args = new[] { "--type", "dotnet", "--dotnet-version", "8.0", "--output", outputFile };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        
        var yamlContent = await File.ReadAllTextAsync(outputFile);
        _createdFiles.Add(outputFile);
        
        yamlContent.Should().Contain("8.0", "Should reference specified .NET version");
    }

    [Fact]
    public async Task CLI_WithCustomVariables_ShouldIncludeVariables()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "custom-variables.yml");
        var args = new[] 
        { 
            "--type", "dotnet", 
            "--variables", "BUILD_CONFIG=Release,TEST_ENV=staging",
            "--output", outputFile 
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        
        var yamlContent = await File.ReadAllTextAsync(outputFile);
        _createdFiles.Add(outputFile);
        
        yamlContent.Should().Contain("BUILD_CONFIG", "Should contain custom variable");
        yamlContent.Should().Contain("Release", "Should contain custom variable value");
        yamlContent.Should().Contain("TEST_ENV", "Should contain second custom variable");
        yamlContent.Should().Contain("staging", "Should contain second custom variable value");
    }

    #endregion

    #region Console Output Tests

    [Fact]
    public async Task CLI_ConsoleOutput_ShouldWriteToStandardOutput()
    {
        // Arrange
        var args = new[] { "--type", "dotnet", "--console-output" };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().NotBeEmpty("Should write YAML to standard output");
        result.StandardOutput.Should().Contain("stages:", "Output should contain valid YAML");
        
        ValidateGitLabCiYaml(result.StandardOutput);
    }

    [Fact]
    public async Task CLI_VerboseConsoleOutput_ShouldIncludeMetadata()
    {
        // Arrange
        var args = new[] { "--type", "dotnet", "--console-output", "--verbose" };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("Generated GitLab CI/CD Pipeline Configuration", 
            "Verbose output should include header");
        result.StandardOutput.Should().Contain("Generated on:", 
            "Verbose output should include timestamp");
    }

    #endregion

    #region Dry Run Tests

    [Fact]
    public async Task CLI_DryRun_ShouldNotCreateFile()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "dry-run.yml");
        var args = new[] { "--type", "dotnet", "--output", outputFile, "--dry-run" };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("Dry run", "Should indicate dry run mode");
        result.StandardOutput.Should().Contain("pipeline generated successfully", "Should confirm generation");
        
        File.Exists(outputFile).Should().BeFalse("File should not be created in dry run mode");
    }

    [Fact]
    public async Task CLI_DryRunVerbose_ShouldShowPipelineStats()
    {
        // Arrange
        var args = new[] { "--type", "dotnet", "--dry-run", "--verbose" };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("Pipeline Statistics:", "Should show pipeline statistics");
        result.StandardOutput.Should().Contain("jobs", "Should show job count");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task CLI_ValidateOnly_ShouldValidateWithoutGenerating()
    {
        // Arrange
        var args = new[] { "--type", "dotnet", "--validate-only" };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("Command-line options are valid", 
            "Should confirm validation success");
    }

    [Fact]
    public async Task CLI_ValidateOnlyWithInvalidOptions_ShouldReportErrors()
    {
        // Arrange
        var args = new[] { "--type", "invalid-type", "--validate-only" };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail with invalid options");
        result.StandardError.Should().Contain("Invalid project type", "Should report validation error");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CLI_InvalidProjectType_ShouldReturnErrorWithSuggestions()
    {
        // Arrange
        var args = new[] { "--type", "invalid-type" };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail with invalid project type");
        result.StandardError.Should().Contain("Invalid project type", "Should report the error");
        result.StandardError.Should().Contain("Valid types are:", "Should provide valid options");
        result.StandardOutput.Should().Contain("Usage examples:", "Should provide usage examples");
    }

    [Fact]
    public async Task CLI_InvalidDotNetVersion_ShouldReturnError()
    {
        // Arrange
        var args = new[] { "--type", "dotnet", "--dotnet-version", "5.0" };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1);
        result.StandardError.Should().Contain("Invalid .NET version", "Should report version error");
    }

    [Fact]
    public async Task CLI_InvalidVariableFormat_ShouldReturnError()
    {
        // Arrange
        var args = new[] { "--type", "dotnet", "--variables", "INVALID_FORMAT" };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1);
        result.StandardError.Should().Contain("Invalid variable format", "Should report format error");
        result.StandardError.Should().Contain("Expected format: key=value", "Should provide correct format");
    }

    [Fact]
    public async Task CLI_ConflictingOptions_ShouldReturnError()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "conflict.yml");
        var args = new[] { "--type", "dotnet", "--console-output", "--output", outputFile };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1);
        result.StandardError.Should().Contain("Cannot specify both --console-output and --output", 
            "Should report conflicting options");
    }

    [Fact]
    public async Task CLI_InvalidOutputDirectory_ShouldReturnError()
    {
        // Arrange
        var invalidPath = Path.Combine("Z:\\NonExistentDrive", "invalid", "path.yml");
        var args = new[] { "--type", "dotnet", "--output", invalidPath };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1);
        result.StandardError.Should().Contain("Output directory does not exist", 
            "Should report directory error");
    }

    #endregion

    #region File Output Tests

    [Fact]
    public async Task CLI_DefaultOutputPath_ShouldCreateGitlabCiFile()
    {
        // Arrange
        var workingDirectory = _testOutputDirectory;
        var expectedFile = Path.Combine(workingDirectory, ".gitlab-ci.yml");
        var args = new[] { "--type", "dotnet" };

        // Act
        var result = await RunCliAsync(args, workingDirectory);

        // Assert
        result.ExitCode.Should().Be(0);
        File.Exists(expectedFile).Should().BeTrue("Should create default .gitlab-ci.yml file");
        _createdFiles.Add(expectedFile);
        
        var yamlContent = await File.ReadAllTextAsync(expectedFile);
        ValidateGitLabCiYaml(yamlContent);
    }

    [Fact]
    public async Task CLI_CustomOutputPath_ShouldCreateFileAtSpecifiedPath()
    {
        // Arrange
        var customPath = Path.Combine(_testOutputDirectory, "custom", "pipeline.yml");
        var args = new[] { "--type", "dotnet", "--output", customPath };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        File.Exists(customPath).Should().BeTrue("Should create file at custom path");
        _createdFiles.Add(customPath);
        
        var yamlContent = await File.ReadAllTextAsync(customPath);
        ValidateGitLabCiYaml(yamlContent);
    }

    [Fact]
    public async Task CLI_OverwriteExistingFile_ShouldSucceedWithWarning()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "overwrite.yml");
        await File.WriteAllTextAsync(outputFile, "existing content");
        _createdFiles.Add(outputFile);
        
        var args = new[] { "--type", "dotnet", "--output", outputFile, "--verbose" };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("already exists and will be overwritten", 
            "Should warn about overwriting");
        
        var yamlContent = await File.ReadAllTextAsync(outputFile);
        yamlContent.Should().NotBe("existing content", "File should be overwritten");
        ValidateGitLabCiYaml(yamlContent);
    }

    #endregion

    #region YAML Validation Tests

    [Fact]
    public async Task CLI_GeneratedYaml_ShouldBeValidGitLabCiFormat()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "yaml-validation.yml");
        var args = new[] { "--type", "dotnet", "--output", outputFile };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        
        var yamlContent = await File.ReadAllTextAsync(outputFile);
        _createdFiles.Add(outputFile);
        
        // Validate YAML can be parsed
        ValidateGitLabCiYaml(yamlContent);
        
        // Validate GitLab CI/CD specific structure
        var yamlObject = ParseYaml(yamlContent);
        
        yamlObject.Should().ContainKey("stages", "Should have stages section");
        
        // Check that jobs exist and have required properties
        var jobs = yamlObject.Where(kvp => kvp.Key != "stages" && kvp.Key != "variables" && kvp.Key != "default")
                            .ToList();
        jobs.Should().NotBeEmpty("Should have at least one job");
        
        foreach (var job in jobs)
        {
            var jobData = job.Value as Dictionary<object, object>;
            jobData.Should().NotBeNull($"Job {job.Key} should have configuration");
            jobData.Should().ContainKey("stage", $"Job {job.Key} should have a stage");
            jobData.Should().ContainKey("script", $"Job {job.Key} should have a script");
        }
    }

    [Fact]
    public async Task CLI_ComplexPipeline_ShouldGenerateValidYamlWithAllFeatures()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "complex-pipeline.yml");
        var args = new[]
        {
            "--type", "dotnet",
            "--dotnet-version", "9.0",
            "--stages", "build,test,quality,deploy",
            "--include-code-quality",
            "--include-security",
            "--variables", "BUILD_CONFIG=Release,ASPNETCORE_ENVIRONMENT=Production",
            "--environments", "staging:https://staging.example.com,production:https://prod.example.com",
            "--cache-paths", "~/.nuget/packages,obj,bin",
            "--cache-key", "nuget-$CI_COMMIT_REF_SLUG",
            "--artifact-paths", "publish,test-results",
            "--artifact-expire", "2 weeks",
            "--runner-tags", "docker,linux",
            "--output", outputFile
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        
        var yamlContent = await File.ReadAllTextAsync(outputFile);
        _createdFiles.Add(outputFile);
        
        ValidateGitLabCiYaml(yamlContent);
        
        // Validate complex features are present
        yamlContent.Should().Contain("BUILD_CONFIG", "Should contain custom variables");
        yamlContent.Should().Contain("cache:", "Should contain cache configuration");
        yamlContent.Should().Contain("artifacts:", "Should contain artifacts configuration");
        yamlContent.Should().Contain("tags:", "Should contain runner tags");
        yamlContent.Should().Contain("quality", "Should contain quality stage");
        
        // Validate environments are configured
        yamlContent.Should().Contain("staging", "Should contain staging environment");
        yamlContent.Should().Contain("production", "Should contain production environment");
    }

    #endregion

    #region Help and Usage Tests

    [Fact]
    public async Task CLI_HelpFlag_ShouldShowUsageInformation()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("Generate a GitLab CI/CD pipeline configuration", 
            "Should show help text");
        result.StandardOutput.Should().Contain("--type", "Should show type option");
        result.StandardOutput.Should().Contain("--output", "Should show output option");
    }

    [Fact]
    public async Task CLI_NoArguments_ShouldShowUsageInformation()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(1, "Should fail when no arguments provided");
        result.StandardError.Should().Contain("Required option", "Should indicate missing required options");
    }

    #endregion

    #region Performance and Edge Case Tests

    [Fact]
    public async Task CLI_LargeNumberOfVariables_ShouldHandleGracefully()
    {
        // Arrange
        var variables = string.Join(",", Enumerable.Range(1, 50).Select(i => $"VAR{i}=value{i}"));
        var outputFile = Path.Combine(_testOutputDirectory, "many-variables.yml");
        var args = new[] { "--type", "dotnet", "--variables", variables, "--output", outputFile };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        
        var yamlContent = await File.ReadAllTextAsync(outputFile);
        _createdFiles.Add(outputFile);
        
        ValidateGitLabCiYaml(yamlContent);
        yamlContent.Should().Contain("VAR1", "Should contain first variable");
        yamlContent.Should().Contain("VAR50", "Should contain last variable");
    }

    [Fact]
    public async Task CLI_SpecialCharactersInVariables_ShouldHandleCorrectly()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "special-chars.yml");
        var args = new[]
        {
            "--type", "dotnet",
            "--variables", "SPECIAL_VAR=value with spaces and symbols: @#$%",
            "--output", outputFile
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        
        var yamlContent = await File.ReadAllTextAsync(outputFile);
        _createdFiles.Add(outputFile);
        
        ValidateGitLabCiYaml(yamlContent);
        yamlContent.Should().Contain("SPECIAL_VAR", "Should contain variable with special characters");
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
            StandardError = errorBuilder.ToString()
        };
    }

    private string FindCliExecutable()
    {
        // For integration tests, we'll use dotnet run to execute the CLI project
        return GetCliProjectPath();
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
        
        // Parse YAML to ensure it's valid
        var yamlObject = ParseYaml(yamlContent);
        yamlObject.Should().NotBeNull("YAML should be parseable");
        
        // Basic GitLab CI/CD structure validation
        yamlContent.Should().MatchRegex(@"stages:\s*\n", "Should have stages section");
        yamlContent.Should().MatchRegex(@"script:\s*\n", "Should have at least one script section");
    }

    private Dictionary<object, object> ParseYaml(string yamlContent)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
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
    }

    #endregion
}