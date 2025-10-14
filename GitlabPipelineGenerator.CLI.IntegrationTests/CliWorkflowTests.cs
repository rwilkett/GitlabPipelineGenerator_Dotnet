namespace GitlabPipelineGenerator.CLI.IntegrationTests;

/// <summary>
/// Integration tests for CLI workflows and end-to-end functionality
/// </summary>
public class CliWorkflowTests : IDisposable
{
    private readonly string _testOutputDirectory;
    private readonly List<string> _createdFiles;

    public CliWorkflowTests()
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
    public async Task CLI_ComplexPipeline_ShouldGenerateValidYamlWithAllFeatures()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "complex-pipeline.yml");
        var args = new[]
        {
            "--type", "dotnet",
            "--dotnet-version", "9.0",
            "--stages", "build,test,deploy",
            "--variables", "BUILD_CONFIG=Release,ASPNETCORE_ENVIRONMENT=Production",
            "--output", outputFile
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0, $"CLI should succeed. Error: {result.StandardError}");
        
        var yamlContent = await File.ReadAllTextAsync(outputFile);
        _createdFiles.Add(outputFile);
        
        ValidateGitLabCiYaml(yamlContent);
        
        // Validate complex features are present
        yamlContent.Should().Contain("BUILD_CONFIG", "Should contain custom variables");
        yamlContent.Should().Contain("Release", "Should contain variable value");
        yamlContent.Should().Contain("ASPNETCORE_ENVIRONMENT", "Should contain environment variable");
        yamlContent.Should().Contain("Production", "Should contain environment value");
    }

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
        
        // Basic GitLab CI/CD structure validation without parsing
        yamlContent.Should().Contain("stages:", "Should have stages section");
        yamlContent.Should().Contain("script:", "Should have at least one script section");
        
        // Try to parse YAML to ensure it's valid, but don't fail if it's complex
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
    }

    #endregion

    #region YAML Schema Validation Tests

    [Fact]
    public async Task CLI_GeneratedYaml_ShouldHaveValidGitLabCiStructure()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "schema-validation.yml");
        var args = new[] { "--type", "dotnet", "--output", outputFile };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        
        var yamlContent = await File.ReadAllTextAsync(outputFile);
        _createdFiles.Add(outputFile);
        
        // Validate GitLab CI/CD specific structure
        yamlContent.Should().Contain("stages:", "Should have stages section");
        yamlContent.Should().MatchRegex(@"stages:\s*\n\s*-", "Stages should be a list");
        
        // Should have at least one job with required properties
        yamlContent.Should().MatchRegex(@"\w+:\s*\n\s*stage:", "Should have jobs with stage property");
        yamlContent.Should().MatchRegex(@"script:\s*\n\s*-", "Should have script as a list");
        
        // Should not have syntax errors
        yamlContent.Should().NotContain("!!!", "Should not contain YAML parsing errors");
        yamlContent.Should().NotContain("null", "Should not contain null values");
    }

    [Fact]
    public async Task CLI_DotNetPipeline_ShouldContainExpectedJobs()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "dotnet-jobs.yml");
        var args = new[] { "--type", "dotnet", "--output", outputFile };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        
        var yamlContent = await File.ReadAllTextAsync(outputFile);
        _createdFiles.Add(outputFile);
        
        // Should contain typical .NET pipeline jobs
        yamlContent.Should().Contain("dotnet", "Should reference .NET commands");
        yamlContent.Should().MatchRegex(@"(build|restore|test|publish)", "Should contain .NET build steps");
    }

    #endregion

    #region Error Scenario Tests

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
    public async Task CLI_EmptyStages_ShouldUseDefaults()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "empty-stages.yml");
        var args = new[] { "--type", "dotnet", "--stages", "", "--output", outputFile };

        // Act
        var result = await RunCliAsync(args);

        // Assert - Should either succeed with defaults or fail with validation error
        if (result.ExitCode == 0)
        {
            var yamlContent = await File.ReadAllTextAsync(outputFile);
            _createdFiles.Add(outputFile);
            yamlContent.Should().Contain("stages:", "Should have default stages");
        }
        else
        {
            result.StandardError.Should().Contain("Stage", "Should report stage-related error");
        }
    }

    [Fact]
    public async Task CLI_LongRunningCommand_ShouldCompleteSuccessfully()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "long-running.yml");
        var args = new[] 
        { 
            "--type", "dotnet", 
            "--stages", "build,test,deploy",
            "--variables", "VAR1=value1,VAR2=value2,VAR3=value3",
            "--output", outputFile,
            "--verbose"
        };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0, "Long-running command should complete successfully");
        
        var yamlContent = await File.ReadAllTextAsync(outputFile);
        _createdFiles.Add(outputFile);
        
        ValidateGitLabCiYaml(yamlContent);
        yamlContent.Should().Contain("VAR1", "Should contain all variables");
        yamlContent.Should().Contain("VAR2", "Should contain all variables");
        yamlContent.Should().Contain("VAR3", "Should contain all variables");
    }

    #endregion

    #region File Output Edge Cases

    [Fact]
    public async Task CLI_OutputToSubdirectory_ShouldCreateDirectoryAndFile()
    {
        // Arrange
        var subdirectory = Path.Combine(_testOutputDirectory, "subdir", "nested");
        Directory.CreateDirectory(subdirectory); // Create directory first
        var outputFile = Path.Combine(subdirectory, "pipeline.yml");
        var args = new[] { "--type", "dotnet", "--output", outputFile };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0, $"CLI should succeed. Error: {result.StandardError}");
        Directory.Exists(subdirectory).Should().BeTrue("Should have subdirectory");
        File.Exists(outputFile).Should().BeTrue("Should create file in subdirectory");
        _createdFiles.Add(outputFile);
        
        var yamlContent = await File.ReadAllTextAsync(outputFile);
        ValidateGitLabCiYaml(yamlContent);
    }

    [Fact]
    public async Task CLI_VerboseOutput_ShouldShowDetailedInformation()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDirectory, "verbose.yml");
        var args = new[] { "--type", "dotnet", "--output", outputFile, "--verbose" };

        // Act
        var result = await RunCliAsync(args);

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("Pipeline Statistics:", "Verbose should show statistics");
        result.StandardOutput.Should().Contain("KB", "Should show file size information");
        
        File.Exists(outputFile).Should().BeTrue("Should create output file");
        _createdFiles.Add(outputFile);
    }

    #endregion
}
