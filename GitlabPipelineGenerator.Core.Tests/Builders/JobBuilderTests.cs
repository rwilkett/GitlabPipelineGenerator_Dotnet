using GitlabPipelineGenerator.Core.Builders;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;
using Moq;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Builders;

public class JobBuilderTests
{
    private readonly Mock<IVariableBuilder> _mockVariableBuilder;
    private readonly JobBuilder _jobBuilder;

    public JobBuilderTests()
    {
        _mockVariableBuilder = new Mock<IVariableBuilder>();
        _jobBuilder = new JobBuilder(_mockVariableBuilder.Object);
    }

    [Fact]
    public void Constructor_WithNullVariableBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new JobBuilder(null!));
    }

    [Fact]
    public async Task BuildJobsForStageAsync_WithNullStage_ThrowsArgumentException()
    {
        // Arrange
        var options = new PipelineOptions { ProjectType = "dotnet" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _jobBuilder.BuildJobsForStageAsync(null!, options));
        Assert.Equal("stage", ex.ParamName);
    }

    [Fact]
    public async Task BuildJobsForStageAsync_WithBuildStage_ReturnsBuildJob()
    {
        // Arrange
        var options = new PipelineOptions { ProjectType = "dotnet" };
        var expectedVariables = new Dictionary<string, object> { ["BUILD_CONFIGURATION"] = "Release" };
        
        _mockVariableBuilder.Setup(x => x.BuildJobVariablesAsync("build", options))
            .ReturnsAsync(expectedVariables);

        // Act
        var result = await _jobBuilder.BuildJobsForStageAsync("build", options);

        // Assert
        Assert.Single(result);
        Assert.True(result.ContainsKey("build"));
        
        var buildJob = result["build"];
        Assert.Equal("build", buildJob.Stage);
        Assert.NotEmpty(buildJob.Script);
        Assert.Contains("dotnet restore", buildJob.Script);
        Assert.Contains("dotnet build --configuration Release --no-restore", buildJob.Script);
        Assert.Equal("mcr.microsoft.com/dotnet/sdk:9.0", buildJob.Image?.Name);
        Assert.Equal(expectedVariables, buildJob.Variables);
        Assert.NotNull(buildJob.Artifacts);
        Assert.Contains("bin/", buildJob.Artifacts.Paths);
        Assert.Contains("obj/", buildJob.Artifacts.Paths);
    }

    [Fact]
    public async Task BuildJobsForStageAsync_WithTestStageAndIncludeTests_ReturnsTestJob()
    {
        // Arrange
        var options = new PipelineOptions { ProjectType = "dotnet", IncludeTests = true };
        var expectedVariables = new Dictionary<string, object> { ["TEST_CONFIGURATION"] = "Release" };
        
        _mockVariableBuilder.Setup(x => x.BuildJobVariablesAsync("test", options))
            .ReturnsAsync(expectedVariables);

        // Act
        var result = await _jobBuilder.BuildJobsForStageAsync("test", options);

        // Assert
        Assert.Single(result);
        Assert.True(result.ContainsKey("test"));
        
        var testJob = result["test"];
        Assert.Equal("test", testJob.Stage);
        Assert.NotEmpty(testJob.Script);
        Assert.Contains("dotnet test --configuration Release --no-build --collect:\"XPlat Code Coverage\" --logger trx --results-directory ./TestResults/", testJob.Script);
        Assert.Contains("build", testJob.Dependencies);
        Assert.NotNull(testJob.Artifacts);
        Assert.NotNull(testJob.Artifacts.Reports);
        Assert.Contains("TestResults/*.trx", testJob.Artifacts.Reports.Junit);
    }

    [Fact]
    public async Task CreateBuildJobAsync_WithDotNetProject_ReturnsConfiguredDotNetBuildJob()
    {
        // Arrange
        var options = new PipelineOptions 
        { 
            ProjectType = "dotnet",
            DotNetVersion = "9.0",
            RunnerTags = new List<string> { "docker" }
        };
        var expectedVariables = new Dictionary<string, object> { ["BUILD_CONFIGURATION"] = "Release" };
        
        _mockVariableBuilder.Setup(x => x.BuildJobVariablesAsync("build", options))
            .ReturnsAsync(expectedVariables);

        // Act
        var result = await _jobBuilder.CreateBuildJobAsync(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("build", result.Stage);
        Assert.Equal("mcr.microsoft.com/dotnet/sdk:9.0", result.Image?.Name);
        Assert.Contains("docker", result.Tags);
        Assert.Contains("dotnet restore", result.Script);
        Assert.Contains("dotnet build --configuration Release --no-restore", result.Script);
        Assert.NotNull(result.Artifacts);
        Assert.Contains("bin/", result.Artifacts.Paths);
        Assert.Contains("obj/", result.Artifacts.Paths);
        Assert.Equal(expectedVariables, result.Variables);
    }
}