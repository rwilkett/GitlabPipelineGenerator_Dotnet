using FluentAssertions;
using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Models;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Exceptions;

public class InvalidPipelineOptionsExceptionTests
{
    [Fact]
    public void DefaultConstructor_ShouldInitializeWithEmptyErrors()
    {
        // Act
        var exception = new InvalidPipelineOptionsException();

        // Assert
        exception.ValidationErrors.Should().NotBeNull().And.BeEmpty();
        exception.PipelineOptions.Should().BeNull();
        exception.Message.Should().Contain("Invalid pipeline options");
    }

    [Fact]
    public void MessageConstructor_ShouldSetMessage()
    {
        // Arrange
        var message = "Custom error message";

        // Act
        var exception = new InvalidPipelineOptionsException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ValidationErrors.Should().NotBeNull().And.BeEmpty();
        exception.PipelineOptions.Should().BeNull();
    }

    [Fact]
    public void ValidationErrorsConstructor_ShouldSetErrorsAndMessage()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var exception = new InvalidPipelineOptionsException(errors);

        // Assert
        exception.ValidationErrors.Should().HaveCount(2);
        exception.ValidationErrors.Should().ContainInOrder("Error 1", "Error 2");
        exception.Message.Should().Contain("Error 1").And.Contain("Error 2");
        exception.PipelineOptions.Should().BeNull();
    }

    [Fact]
    public void ValidationErrorsAndOptionsConstructor_ShouldSetAllProperties()
    {
        // Arrange
        var errors = new[] { "Invalid project type" };
        var options = new PipelineOptions { ProjectType = "invalid" };

        // Act
        var exception = new InvalidPipelineOptionsException(errors, options);

        // Assert
        exception.ValidationErrors.Should().HaveCount(1);
        exception.ValidationErrors[0].Should().Be("Invalid project type");
        exception.PipelineOptions.Should().Be(options);
        exception.Message.Should().Contain("Invalid project type");
    }

    [Fact]
    public void MessageAndInnerExceptionConstructor_ShouldSetMessageAndInnerException()
    {
        // Arrange
        var message = "Custom error message";
        var innerException = new ArgumentException("Inner exception");

        // Act
        var exception = new InvalidPipelineOptionsException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
        exception.ValidationErrors.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void FullConstructor_ShouldSetAllProperties()
    {
        // Arrange
        var message = "Custom error message";
        var errors = new[] { "Error 1" };
        var options = new PipelineOptions { ProjectType = "dotnet" };
        var innerException = new ArgumentException("Inner exception");

        // Act
        var exception = new InvalidPipelineOptionsException(message, errors, options, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.ValidationErrors.Should().HaveCount(1);
        exception.ValidationErrors[0].Should().Be("Error 1");
        exception.PipelineOptions.Should().Be(options);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void ValidationErrors_ShouldBeReadOnly()
    {
        // Arrange
        var errors = new List<string> { "Error 1" };
        var exception = new InvalidPipelineOptionsException(errors);

        // Act & Assert
        exception.ValidationErrors.Should().BeAssignableTo<IReadOnlyList<string>>();
        
        // Verify we can't modify the original list and affect the exception
        errors.Add("Error 2");
        exception.ValidationErrors.Should().HaveCount(1);
    }
}

public class PipelineGenerationExceptionTests
{
    [Fact]
    public void DefaultConstructor_ShouldInitializeWithNullProperties()
    {
        // Act
        var exception = new PipelineGenerationException();

        // Assert
        exception.PipelineOptions.Should().BeNull();
        exception.GenerationStage.Should().BeNull();
        exception.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MessageConstructor_ShouldSetMessage()
    {
        // Arrange
        var message = "Pipeline generation failed";

        // Act
        var exception = new PipelineGenerationException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.PipelineOptions.Should().BeNull();
        exception.GenerationStage.Should().BeNull();
    }

    [Fact]
    public void MessageAndInnerExceptionConstructor_ShouldSetMessageAndInnerException()
    {
        // Arrange
        var message = "Pipeline generation failed";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new PipelineGenerationException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
        exception.PipelineOptions.Should().BeNull();
        exception.GenerationStage.Should().BeNull();
    }

    [Fact]
    public void DetailedConstructor_ShouldSetAllProperties()
    {
        // Arrange
        var message = "Pipeline generation failed";
        var options = new PipelineOptions { ProjectType = "dotnet" };
        var stage = "job-creation";

        // Act
        var exception = new PipelineGenerationException(message, options, stage);

        // Assert
        exception.Message.Should().Be(message);
        exception.PipelineOptions.Should().Be(options);
        exception.GenerationStage.Should().Be(stage);
    }

    [Fact]
    public void FullConstructor_ShouldSetAllProperties()
    {
        // Arrange
        var message = "Pipeline generation failed";
        var options = new PipelineOptions { ProjectType = "dotnet" };
        var stage = "yaml-serialization";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new PipelineGenerationException(message, options, stage, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.PipelineOptions.Should().Be(options);
        exception.GenerationStage.Should().Be(stage);
        exception.InnerException.Should().Be(innerException);
    }

    [Theory]
    [InlineData("validation")]
    [InlineData("job-creation")]
    [InlineData("yaml-serialization")]
    [InlineData("template-processing")]
    public void GenerationStage_ShouldAcceptValidStageNames(string stage)
    {
        // Act
        var exception = new PipelineGenerationException("Failed", null, stage);

        // Assert
        exception.GenerationStage.Should().Be(stage);
    }
}

public class YamlSerializationExceptionTests
{
    [Fact]
    public void DefaultConstructor_ShouldInitializeWithNullProperties()
    {
        // Act
        var exception = new YamlSerializationException();

        // Assert
        exception.YamlContent.Should().BeNull();
        exception.Operation.Should().BeNull();
        exception.SourceObject.Should().BeNull();
        exception.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MessageConstructor_ShouldSetMessage()
    {
        // Arrange
        var message = "YAML serialization failed";

        // Act
        var exception = new YamlSerializationException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.YamlContent.Should().BeNull();
        exception.Operation.Should().BeNull();
        exception.SourceObject.Should().BeNull();
    }

    [Fact]
    public void MessageAndInnerExceptionConstructor_ShouldSetMessageAndInnerException()
    {
        // Arrange
        var message = "YAML serialization failed";
        var innerException = new FormatException("Invalid YAML format");

        // Act
        var exception = new YamlSerializationException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
        exception.YamlContent.Should().BeNull();
        exception.Operation.Should().BeNull();
        exception.SourceObject.Should().BeNull();
    }

    [Fact]
    public void OperationAndYamlContentConstructor_ShouldSetProperties()
    {
        // Arrange
        var message = "YAML deserialization failed";
        var operation = "deserialize";
        var yamlContent = "invalid: yaml: content:";

        // Act
        var exception = new YamlSerializationException(message, operation, yamlContent);

        // Assert
        exception.Message.Should().Be(message);
        exception.Operation.Should().Be(operation);
        exception.YamlContent.Should().Be(yamlContent);
        exception.SourceObject.Should().BeNull();
    }

    [Fact]
    public void SerializationConstructor_ShouldSetPropertiesForSerialization()
    {
        // Arrange
        var message = "YAML serialization failed";
        var operation = "serialize";
        var sourceObject = new PipelineConfiguration();
        var innerException = new InvalidOperationException("Serialization error");

        // Act
        var exception = new YamlSerializationException(message, operation, sourceObject, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.Operation.Should().Be(operation);
        exception.SourceObject.Should().Be(sourceObject);
        exception.InnerException.Should().Be(innerException);
        exception.YamlContent.Should().BeNull();
    }

    [Fact]
    public void DeserializationConstructor_ShouldSetPropertiesForDeserialization()
    {
        // Arrange
        var message = "YAML deserialization failed";
        var operation = "deserialize";
        var yamlContent = "stages:\n  - build\n  - test";
        var innerException = new FormatException("Invalid YAML");

        // Act
        var exception = new YamlSerializationException(message, operation, yamlContent, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.Operation.Should().Be(operation);
        exception.YamlContent.Should().Be(yamlContent);
        exception.InnerException.Should().Be(innerException);
        exception.SourceObject.Should().BeNull();
    }

    [Theory]
    [InlineData("serialize")]
    [InlineData("deserialize")]
    [InlineData("validate")]
    [InlineData("format")]
    public void Operation_ShouldAcceptValidOperationNames(string operation)
    {
        // Act
        var exception = new YamlSerializationException("Failed", operation, "yaml content");

        // Assert
        exception.Operation.Should().Be(operation);
    }

    [Fact]
    public void YamlContent_ShouldHandleMultilineContent()
    {
        // Arrange
        var yamlContent = @"stages:
  - build
  - test
  - deploy
variables:
  DOTNET_VERSION: '9.0'
jobs:
  build-job:
    stage: build
    script:
      - dotnet build";

        // Act
        var exception = new YamlSerializationException("Failed", "deserialize", yamlContent);

        // Assert
        exception.YamlContent.Should().Be(yamlContent);
    }

    [Fact]
    public void SourceObject_ShouldHandleComplexObjects()
    {
        // Arrange
        var sourceObject = new PipelineConfiguration
        {
            Stages = new List<string> { "build", "test" },
            Variables = new Dictionary<string, object>
            {
                { "DOTNET_VERSION", "9.0" }
            }
        };

        // Act
        var exception = new YamlSerializationException("Failed", "serialize", sourceObject, new Exception());

        // Assert
        exception.SourceObject.Should().Be(sourceObject);
        var config = exception.SourceObject as PipelineConfiguration;
        config.Should().NotBeNull();
        config!.Stages.Should().HaveCount(2);
        config.Variables.Should().ContainKey("DOTNET_VERSION");
    }
}
