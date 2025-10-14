using GitlabPipelineGenerator.Core.Builders;
using GitlabPipelineGenerator.Core.Models;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Builders;

public class StageBuilderTests
{
    private readonly StageBuilder _stageBuilder;

    public StageBuilderTests()
    {
        _stageBuilder = new StageBuilder();
    }

    [Fact]
    public async Task BuildStagesAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _stageBuilder.BuildStagesAsync(null!));
        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public async Task BuildStagesAsync_WithEmptyStages_ReturnsDefaultStages()
    {
        // Arrange
        var options = new PipelineOptions { ProjectType = "dotnet" };

        // Act
        var result = await _stageBuilder.BuildStagesAsync(options);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("build", result);
        Assert.Contains("test", result);
        Assert.Contains("deploy", result);
    }

    [Fact]
    public async Task BuildStagesAsync_WithCustomStages_ReturnsCustomStages()
    {
        // Arrange
        var options = new PipelineOptions 
        { 
            ProjectType = "dotnet",
            Stages = new List<string> { "build", "test", "package" }
        };

        // Act
        var result = await _stageBuilder.BuildStagesAsync(options);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("build", result);
        Assert.Contains("test", result);
        Assert.Contains("package", result);
    }

    [Fact]
    public async Task BuildStagesAsync_WithIncludeCodeQuality_AddsQualityStage()
    {
        // Arrange
        var options = new PipelineOptions 
        { 
            ProjectType = "dotnet",
            IncludeCodeQuality = true
        };

        // Act
        var result = await _stageBuilder.BuildStagesAsync(options);

        // Assert
        Assert.Contains("quality", result);
        
        // Quality should be after test
        var testIndex = result.IndexOf("test");
        var qualityIndex = result.IndexOf("quality");
        Assert.True(qualityIndex > testIndex);
    }

    [Fact]
    public void GetDefaultStages_WithDotNetProjectType_ReturnsDotNetStages()
    {
        // Act
        var result = _stageBuilder.GetDefaultStages("dotnet");

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("build", result);
        Assert.Contains("test", result);
        Assert.Contains("deploy", result);
    }

    [Fact]
    public void ValidateStages_WithValidStages_ReturnsNoErrors()
    {
        // Arrange
        var stages = new List<string> { "build", "test", "deploy" };

        // Act
        var result = _stageBuilder.ValidateStages(stages, "dotnet");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ValidateStages_WithInvalidStage_ReturnsValidationError()
    {
        // Arrange
        var stages = new List<string> { "build", "invalid-stage", "deploy" };

        // Act
        var result = _stageBuilder.ValidateStages(stages, "dotnet");

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, e => e.Contains("Invalid stage 'invalid-stage'"));
    }
}