using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;
using GitlabPipelineGenerator.Core.Services;
using Moq;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

public class PipelineGeneratorTests
{
    private readonly Mock<IStageBuilder> _mockStageBuilder;
    private readonly Mock<IJobBuilder> _mockJobBuilder;
    private readonly Mock<IVariableBuilder> _mockVariableBuilder;
    private readonly YamlSerializationService _yamlService;
    private readonly PipelineGenerator _pipelineGenerator;

    public PipelineGeneratorTests()
    {
        _mockStageBuilder = new Mock<IStageBuilder>();
        _mockJobBuilder = new Mock<IJobBuilder>();
        _mockVariableBuilder = new Mock<IVariableBuilder>();
        _yamlService = new YamlSerializationService();
        
        _pipelineGenerator = new PipelineGenerator(
            _mockStageBuilder.Object,
            _mockJobBuilder.Object,
            _mockVariableBuilder.Object,
            _yamlService);
    }

    [Fact]
    public void Constructor_WithNullStageBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PipelineGenerator(
            null!,
            _mockJobBuilder.Object,
            _mockVariableBuilder.Object,
            _yamlService));
    }

    [Fact]
    public async Task GenerateAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _pipelineGenerator.GenerateAsync(null!));
        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public async Task GenerateAsync_WithValidOptions_ReturnsValidPipelineConfiguration()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build", "test" },
            IncludeTests = true
        };

        var expectedStages = new List<string> { "build", "test" };
        var expectedVariables = new Dictionary<string, object> { ["TEST_VAR"] = "value" };
        var expectedDefaultConfig = new Dictionary<string, object> { ["image"] = "dotnet:9.0" };
        var expectedBuildJobs = new Dictionary<string, Job>
        {
            ["build"] = new Job { Stage = "build", Script = new List<string> { "dotnet build" } }
        };
        var expectedTestJobs = new Dictionary<string, Job>
        {
            ["test"] = new Job { Stage = "test", Script = new List<string> { "dotnet test" } }
        };

        _mockStageBuilder.Setup(x => x.BuildStagesAsync(options))
            .ReturnsAsync(expectedStages);
        _mockVariableBuilder.Setup(x => x.BuildGlobalVariablesAsync(options))
            .ReturnsAsync(expectedVariables);
        _mockVariableBuilder.Setup(x => x.BuildDefaultConfigurationAsync(options))
            .ReturnsAsync(expectedDefaultConfig);
        _mockJobBuilder.Setup(x => x.BuildJobsForStageAsync("build", options))
            .ReturnsAsync(expectedBuildJobs);
        _mockJobBuilder.Setup(x => x.BuildJobsForStageAsync("test", options))
            .ReturnsAsync(expectedTestJobs);

        // Act
        var result = await _pipelineGenerator.GenerateAsync(options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedStages, result.Stages);
        Assert.Equal(expectedVariables, result.Variables);
        Assert.Equal(expectedDefaultConfig, result.Default);
        Assert.Equal(2, result.Jobs.Count);
        Assert.True(result.Jobs.ContainsKey("build"));
        Assert.True(result.Jobs.ContainsKey("test"));
    }

    [Fact]
    public void SerializeToYaml_WithNullPipeline_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _pipelineGenerator.SerializeToYaml(null!));
        Assert.Equal("pipeline", ex.ParamName);
    }

    [Fact]
    public void SerializeToYaml_WithValidPipeline_ReturnsYamlString()
    {
        // Arrange
        var pipeline = new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test" },
            Jobs = new Dictionary<string, Job>
            {
                ["build"] = new Job { Stage = "build", Script = new List<string> { "dotnet build" } }
            }
        };

        // Act
        var result = _pipelineGenerator.SerializeToYaml(pipeline);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("stages:", result);
        Assert.Contains("build:", result);
    }
}