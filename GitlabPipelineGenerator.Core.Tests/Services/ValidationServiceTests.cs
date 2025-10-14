using FluentAssertions;
using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Models;
using GitlabPipelineGenerator.Core.Services;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

public class ValidationServiceTests
{
    [Fact]
    public void ValidateAndThrow_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => ValidationService.ValidateAndThrow(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateAndThrow_WithValidOptions_ShouldNotThrow()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build", "test" }
        };

        // Act & Assert
        var action = () => ValidationService.ValidateAndThrow(options);
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateAndThrow_WithInvalidOptions_ShouldThrowInvalidPipelineOptionsException()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "invalid",
            Stages = new List<string>()
        };

        // Act & Assert
        var action = () => ValidationService.ValidateAndThrow(options);
        action.Should().Throw<InvalidPipelineOptionsException>()
            .Which.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateOptions_WithNullOptions_ShouldReturnInvalidResult()
    {
        // Act
        var result = ValidationService.ValidateOptions(null!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Be("Pipeline options cannot be null");
    }

    [Fact]
    public void ValidateOptions_WithValidOptions_ShouldReturnValidResult()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build", "test" }
        };

        // Act
        var result = ValidationService.ValidateOptions(options);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateOptions_WithInvalidOptions_ShouldReturnInvalidResult()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "invalid",
            Stages = new List<string>()
        };

        // Act
        var result = ValidationService.ValidateOptions(options);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("dotnet", true)]
    [InlineData("nodejs", true)]
    [InlineData("python", true)]
    [InlineData("docker", true)]
    [InlineData("generic", true)]
    [InlineData("DOTNET", true)] // Case insensitive
    [InlineData("invalid", false)]
    [InlineData("java", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidProjectType_ShouldValidateCorrectly(string? projectType, bool expected)
    {
        // Act
        var result = ValidationService.IsValidProjectType(projectType);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("6.0", true)]
    [InlineData("7.0", true)]
    [InlineData("8.0", true)]
    [InlineData("9.0", true)]
    [InlineData("5.0", false)]
    [InlineData("10.0", false)]
    [InlineData("invalid", false)]
    [InlineData("", true)] // Empty is valid (optional)
    [InlineData(null, true)] // Null is valid (optional)
    public void IsValidDotNetVersion_ShouldValidateCorrectly(string? version, bool expected)
    {
        // Act
        var result = ValidationService.IsValidDotNetVersion(version);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("build", true)]
    [InlineData("test", true)]
    [InlineData("deploy", true)]
    [InlineData("custom-stage", true)]
    [InlineData("stage_with_underscore", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("  ", false)]
    [InlineData("stage:with:colon", false)]
    [InlineData("stage[with]brackets", false)]
    [InlineData("stage{with}braces", false)]
    public void IsValidStageName_ShouldValidateCorrectly(string? stageName, bool expected)
    {
        // Act
        var result = ValidationService.IsValidStageName(stageName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("VALID_VAR", true)]
    [InlineData("valid_var", true)]
    [InlineData("ValidVar", true)]
    [InlineData("VAR123", true)]
    [InlineData("_PRIVATE_VAR", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("  ", false)]
    [InlineData("INVALID VAR", false)]
    [InlineData("123INVALID", false)]
    [InlineData("INVALID-VAR", false)]
    [InlineData("INVALID.VAR", false)]
    public void IsValidVariableName_ShouldValidateCorrectly(string? variableName, bool expected)
    {
        // Act
        var result = ValidationService.IsValidVariableName(variableName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://localhost:8080", true)]
    [InlineData("https://staging.myapp.com/api", true)]
    [InlineData("", true)] // Empty is valid (optional)
    [InlineData(null, true)] // Null is valid (optional)
    [InlineData("invalid-url", false)]
    [InlineData("ftp://example.com", false)]
    [InlineData("not-a-url", false)]
    public void IsValidUrl_ShouldValidateCorrectly(string? url, bool expected)
    {
        // Act
        var result = ValidationService.IsValidUrl(url);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("path/to/file.txt", true)]
    [InlineData("./relative/path.yml", true)]
    [InlineData("/absolute/path.yml", true)]
    [InlineData("C:\\Windows\\path.txt", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("  ", false)]
    public void IsValidFilePath_ShouldValidateCorrectly(string? filePath, bool expected)
    {
        // Act
        var result = ValidationService.IsValidFilePath(filePath);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("nginx:latest", true)]
    [InlineData("mcr.microsoft.com/dotnet/sdk:9.0", true)]
    [InlineData("registry.example.com/namespace/image:tag", true)]
    [InlineData("simple-image", true)]
    [InlineData("", true)] // Empty is valid (optional)
    [InlineData(null, true)] // Null is valid (optional)
    [InlineData("invalid image name", false)]
    [InlineData("image:tag:extra", false)]
    public void IsValidDockerImageName_ShouldValidateCorrectly(string? imageName, bool expected)
    {
        // Act
        var result = ValidationService.IsValidDockerImageName(imageName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("cache-key", true)]
    [InlineData("cache_key", true)]
    [InlineData("cache-key-123", true)]
    [InlineData("", true)] // Empty is valid (optional)
    [InlineData(null, true)] // Null is valid (optional)
    [InlineData("cache key", false)]
    [InlineData("cache\tkey", false)]
    [InlineData("cache\nkey", false)]
    public void IsValidCacheKey_ShouldValidateCorrectly(string? cacheKey, bool expected)
    {
        // Act
        var result = ValidationService.IsValidCacheKey(cacheKey);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("1 week", true)]
    [InlineData("30 days", true)]
    [InlineData("2 hours", true)]
    [InlineData("1 month", true)]
    [InlineData("365 days", true)]
    [InlineData("", true)] // Empty is valid (optional)
    [InlineData(null, true)] // Null is valid (optional)
    [InlineData("invalid", false)]
    [InlineData("1", false)]
    [InlineData("week", false)]
    [InlineData("0 days", false)]
    [InlineData("-1 week", false)]
    public void IsValidArtifactExpiration_ShouldValidateCorrectly(string? expiration, bool expected)
    {
        // Act
        var result = ValidationService.IsValidArtifactExpiration(expiration);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetValidationSuggestions_WithProjectTypeError_ShouldReturnProjectTypeSuggestion()
    {
        // Arrange
        var errors = new[] { "Invalid project type 'java'" };

        // Act
        var suggestions = ValidationService.GetValidationSuggestions(errors);

        // Assert
        suggestions.Should().Contain("Use one of the supported project types: dotnet, nodejs, python, docker, generic");
    }

    [Fact]
    public void GetValidationSuggestions_WithDotNetVersionError_ShouldReturnVersionSuggestion()
    {
        // Arrange
        var errors = new[] { "Invalid .NET version '5.0'" };

        // Act
        var suggestions = ValidationService.GetValidationSuggestions(errors);

        // Assert
        suggestions.Should().Contain("Use a supported .NET version: 6.0, 7.0, 8.0, or 9.0");
    }

    [Fact]
    public void GetValidationSuggestions_WithStageError_ShouldReturnStageSuggestion()
    {
        // Arrange
        var errors = new[] { "Invalid stage name" };

        // Act
        var suggestions = ValidationService.GetValidationSuggestions(errors);

        // Assert
        suggestions.Should().Contain("Ensure stage names are not empty and don't contain special characters like :, [, ], {, }");
    }

    [Fact]
    public void GetValidationSuggestions_WithVariableError_ShouldReturnVariableSuggestion()
    {
        // Arrange
        var errors = new[] { "Invalid variable name" };

        // Act
        var suggestions = ValidationService.GetValidationSuggestions(errors);

        // Assert
        suggestions.Should().Contain("Variable names should contain only letters, numbers, and underscores, and cannot start with a number");
    }

    [Fact]
    public void GetValidationSuggestions_WithUrlError_ShouldReturnUrlSuggestion()
    {
        // Arrange
        var errors = new[] { "Invalid URL format" };

        // Act
        var suggestions = ValidationService.GetValidationSuggestions(errors);

        // Assert
        suggestions.Should().Contain("Ensure URLs are properly formatted with http:// or https:// protocol");
    }

    [Fact]
    public void GetValidationSuggestions_WithDockerImageError_ShouldReturnDockerSuggestion()
    {
        // Arrange
        var errors = new[] { "Invalid docker image name" };

        // Act
        var suggestions = ValidationService.GetValidationSuggestions(errors);

        // Assert
        suggestions.Should().Contain("Docker image names should follow the format: [registry/]namespace/repository[:tag]");
    }

    [Fact]
    public void GetValidationSuggestions_WithCacheKeyError_ShouldReturnCacheSuggestion()
    {
        // Arrange
        var errors = new[] { "Invalid cache key" };

        // Act
        var suggestions = ValidationService.GetValidationSuggestions(errors);

        // Assert
        suggestions.Should().Contain("Cache keys should not contain spaces or special characters");
    }

    [Fact]
    public void GetValidationSuggestions_WithExpirationError_ShouldReturnExpirationSuggestion()
    {
        // Arrange
        var errors = new[] { "Invalid expiration format" };

        // Act
        var suggestions = ValidationService.GetValidationSuggestions(errors);

        // Assert
        suggestions.Should().Contain("Use format like '1 week', '30 days', '2 hours' for expiration times");
    }

    [Fact]
    public void GetValidationSuggestions_WithUnknownError_ShouldReturnGenericSuggestion()
    {
        // Arrange
        var errors = new[] { "Some unknown error" };

        // Act
        var suggestions = ValidationService.GetValidationSuggestions(errors);

        // Assert
        suggestions.Should().Contain("Please check the documentation for valid configuration options");
    }

    [Fact]
    public void GetValidationSuggestions_WithMultipleErrors_ShouldReturnMultipleSuggestions()
    {
        // Arrange
        var errors = new[] 
        { 
            "Invalid project type 'java'", 
            "Invalid .NET version '5.0'",
            "Invalid URL format"
        };

        // Act
        var suggestions = ValidationService.GetValidationSuggestions(errors);

        // Assert
        suggestions.Should().HaveCount(3);
        suggestions.Should().Contain("Use one of the supported project types: dotnet, nodejs, python, docker, generic");
        suggestions.Should().Contain("Use a supported .NET version: 6.0, 7.0, 8.0, or 9.0");
        suggestions.Should().Contain("Ensure URLs are properly formatted with http:// or https:// protocol");
    }

    [Fact]
    public void GetValidationSuggestions_WithDuplicateErrorTypes_ShouldReturnDistinctSuggestions()
    {
        // Arrange
        var errors = new[] 
        { 
            "Invalid project type 'java'", 
            "Invalid project type 'ruby'"
        };

        // Act
        var suggestions = ValidationService.GetValidationSuggestions(errors);

        // Assert
        suggestions.Should().HaveCount(1);
        suggestions.Should().Contain("Use one of the supported project types: dotnet, nodejs, python, docker, generic");
    }
}

public class PipelineValidationResultTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var result = new PipelineValidationResult(false, errors);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().ContainInOrder("Error 1", "Error 2");
    }

    [Fact]
    public void Constructor_WithValidResult_ShouldHaveNoErrors()
    {
        // Act
        var result = new PipelineValidationResult(true, new List<string>());

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Errors_ShouldBeReadOnly()
    {
        // Arrange
        var errors = new List<string> { "Error 1" };
        var result = new PipelineValidationResult(false, errors);

        // Act & Assert
        result.Errors.Should().BeAssignableTo<IReadOnlyList<string>>();
        
        // Verify we can't modify the original list and affect the result
        errors.Add("Error 2");
        result.Errors.Should().HaveCount(1);
    }
}
