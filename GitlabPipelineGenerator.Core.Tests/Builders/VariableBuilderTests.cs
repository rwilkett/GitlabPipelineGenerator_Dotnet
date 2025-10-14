using GitlabPipelineGenerator.Core.Builders;
using GitlabPipelineGenerator.Core.Models;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Builders;

public class VariableBuilderTests
{
    private readonly VariableBuilder _variableBuilder;

    public VariableBuilderTests()
    {
        _variableBuilder = new VariableBuilder();
    }

    [Fact]
    public async Task BuildGlobalVariablesAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _variableBuilder.BuildGlobalVariablesAsync(null!));
        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public async Task BuildGlobalVariablesAsync_WithDotNetProject_ReturnsDefaultDotNetVariables()
    {
        // Arrange
        var options = new PipelineOptions { ProjectType = "dotnet" };

        // Act
        var result = await _variableBuilder.BuildGlobalVariablesAsync(options);

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.ContainsKey("DOTNET_CLI_TELEMETRY_OPTOUT"));
        Assert.Equal("true", result["DOTNET_CLI_TELEMETRY_OPTOUT"]);
        Assert.True(result.ContainsKey("DOTNET_SKIP_FIRST_TIME_EXPERIENCE"));
        Assert.Equal("true", result["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"]);
        Assert.True(result.ContainsKey("NUGET_PACKAGES"));
        Assert.Equal("$CI_PROJECT_DIR/.nuget/packages", result["NUGET_PACKAGES"]);
    }

    [Fact]
    public async Task BuildGlobalVariablesAsync_WithDotNetVersion_IncludesDotNetVersionVariable()
    {
        // Arrange
        var options = new PipelineOptions 
        { 
            ProjectType = "dotnet",
            DotNetVersion = "9.0"
        };

        // Act
        var result = await _variableBuilder.BuildGlobalVariablesAsync(options);

        // Assert
        Assert.True(result.ContainsKey("DOTNET_VERSION"));
        Assert.Equal("9.0", result["DOTNET_VERSION"]);
    }

    [Fact]
    public async Task BuildGlobalVariablesAsync_WithCustomVariables_MergesCustomVariables()
    {
        // Arrange
        var options = new PipelineOptions 
        { 
            ProjectType = "dotnet",
            CustomVariables = new Dictionary<string, string>
            {
                ["CUSTOM_VAR"] = "custom_value",
                ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "false" // Override default
            }
        };

        // Act
        var result = await _variableBuilder.BuildGlobalVariablesAsync(options);

        // Assert
        Assert.True(result.ContainsKey("CUSTOM_VAR"));
        Assert.Equal("custom_value", result["CUSTOM_VAR"]);
        Assert.True(result.ContainsKey("DOTNET_CLI_TELEMETRY_OPTOUT"));
        Assert.Equal("false", result["DOTNET_CLI_TELEMETRY_OPTOUT"]); // Custom value should override default
    }

    [Fact]
    public async Task BuildJobVariablesAsync_WithBuildJobType_ReturnsBuildVariables()
    {
        // Arrange
        var options = new PipelineOptions { ProjectType = "dotnet" };

        // Act
        var result = await _variableBuilder.BuildJobVariablesAsync("build", options);

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.ContainsKey("BUILD_CONFIGURATION"));
        Assert.Equal("Release", result["BUILD_CONFIGURATION"]);
        Assert.True(result.ContainsKey("DOTNET_CONFIGURATION"));
        Assert.Equal("Release", result["DOTNET_CONFIGURATION"]);
        Assert.True(result.ContainsKey("DOTNET_VERBOSITY"));
        Assert.Equal("minimal", result["DOTNET_VERBOSITY"]);
    }

    [Fact]
    public void GetDefaultVariables_WithDotNetProjectType_ReturnsDotNetVariables()
    {
        // Act
        var result = _variableBuilder.GetDefaultVariables("dotnet");

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.ContainsKey("DOTNET_CLI_TELEMETRY_OPTOUT"));
        Assert.True(result.ContainsKey("DOTNET_SKIP_FIRST_TIME_EXPERIENCE"));
        Assert.True(result.ContainsKey("NUGET_PACKAGES"));
    }

    [Fact]
    public void MergeVariables_WithBothVariables_MergesCorrectly()
    {
        // Arrange
        var defaultVariables = new Dictionary<string, object> 
        { 
            ["DEFAULT"] = "default_value",
            ["SHARED"] = "default_shared"
        };
        var customVariables = new Dictionary<string, string> 
        { 
            ["CUSTOM"] = "custom_value",
            ["SHARED"] = "custom_shared" // Should override default
        };

        // Act
        var result = _variableBuilder.MergeVariables(defaultVariables, customVariables);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("default_value", result["DEFAULT"]);
        Assert.Equal("custom_value", result["CUSTOM"]);
        Assert.Equal("custom_shared", result["SHARED"]); // Custom should override default
    }
}