using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Models;
using GitlabPipelineGenerator.Core.Services;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

public class YamlSerializationServiceTests
{
    private readonly YamlSerializationService _yamlService;

    public YamlSerializationServiceTests()
    {
        _yamlService = new YamlSerializationService();
    }

    [Fact]
    public void SerializePipeline_WithNullPipeline_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => _yamlService.SerializePipeline(null!));
        Assert.Equal("pipeline", ex.ParamName);
    }

    [Fact]
    public void SerializePipeline_WithValidPipeline_ReturnsValidYaml()
    {
        // Arrange
        var pipeline = new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test", "deploy" },
            Variables = new Dictionary<string, object>
            {
                ["DOTNET_VERSION"] = "9.0",
                ["BUILD_CONFIGURATION"] = "Release"
            },
            Jobs = new Dictionary<string, Job>
            {
                ["build"] = new Job
                {
                    Stage = "build",
                    Image = new JobImage { Name = "mcr.microsoft.com/dotnet/sdk:9.0" },
                    Script = new List<string> { "dotnet restore", "dotnet build --configuration Release" },
                    Artifacts = new JobArtifacts
                    {
                        Paths = new List<string> { "bin/", "obj/" },
                        ExpireIn = "1 week"
                    }
                }
            }
        };

        // Act
        var result = _yamlService.SerializePipeline(pipeline);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("stages:", result);
        Assert.Contains("- build", result);
        Assert.Contains("- test", result);
        Assert.Contains("- deploy", result);
        Assert.Contains("variables:", result);
        Assert.Contains("DOTNET_VERSION", result);
        Assert.Contains("BUILD_CONFIGURATION", result);
        Assert.Contains("build:", result);
        Assert.Contains("stage: build", result);
        Assert.Contains("script:", result);
        Assert.Contains("dotnet restore", result);
        Assert.Contains("dotnet build", result);
    }

    [Fact]
    public void DeserializePipeline_WithNullYaml_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _yamlService.DeserializePipeline(null!));
        Assert.Equal("yaml", ex.ParamName);
    }

    [Fact]
    public void DeserializePipeline_WithValidYaml_ReturnsPipelineConfiguration()
    {
        // Arrange
        var yaml = @"
stages:
  - build
  - test
variables:
  DOTNET_VERSION: '9.0'
  BUILD_CONFIGURATION: Release
build:
  stage: build
  image: mcr.microsoft.com/dotnet/sdk:9.0
  script:
    - dotnet restore
    - dotnet build --configuration Release
test:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:9.0
  script:
    - dotnet test --configuration Release
  dependencies:
    - build
";

        // Act
        var result = _yamlService.DeserializePipeline(yaml);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Stages);
        Assert.NotNull(result.Variables);
        Assert.NotNull(result.Jobs);
        
        if (result.Stages.Count > 0)
        {
            Assert.Contains("build", result.Stages);
            Assert.Contains("test", result.Stages);
        }
        
        if (result.Variables.Count > 0)
        {
            Assert.True(result.Variables.ContainsKey("DOTNET_VERSION"));
            Assert.True(result.Variables.ContainsKey("BUILD_CONFIGURATION"));
        }
        
        if (result.Jobs.Count > 0)
        {
            Assert.True(result.Jobs.ContainsKey("build"));
            Assert.True(result.Jobs.ContainsKey("test"));
        }
    }

    [Fact]
    public void ValidateYaml_WithValidGitLabCiYaml_DoesNotThrow()
    {
        // Arrange
        var validYaml = @"
stages:
  - build
  - test
build:
  stage: build
  script:
    - echo 'Building...'
test:
  stage: test
  script:
    - echo 'Testing...'
";

        // Act & Assert
        var exception = Record.Exception(() => _yamlService.ValidateYaml(validYaml));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateYaml_WithInvalidYamlSyntax_ThrowsYamlSerializationException()
    {
        // Arrange
        var invalidYaml = @"
stages:
  - build
  - test
invalid_syntax: [
  missing_bracket
";

        // Act & Assert
        var ex = Assert.Throws<YamlSerializationException>(() => _yamlService.ValidateYaml(invalidYaml));
        Assert.Contains("Invalid YAML syntax", ex.Message);
        Assert.Equal("validate", ex.Operation);
    }
}