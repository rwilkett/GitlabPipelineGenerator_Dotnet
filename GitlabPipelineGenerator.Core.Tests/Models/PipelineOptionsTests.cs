using FluentAssertions;
using GitlabPipelineGenerator.Core.Models;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Models;

public class PipelineOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var options = new PipelineOptions();

        // Assert
        options.ProjectType.Should().Be(string.Empty);
        options.Stages.Should().NotBeNull().And.HaveCount(3);
        options.Stages.Should().ContainInOrder("build", "test", "deploy");
        options.DotNetVersion.Should().BeNull();
        options.IncludeTests.Should().BeTrue();
        options.IncludeDeployment.Should().BeTrue();
        options.CustomVariables.Should().NotBeNull().And.BeEmpty();
        options.DockerImage.Should().BeNull();
        options.RunnerTags.Should().NotBeNull().And.BeEmpty();
        options.IncludeCodeQuality.Should().BeFalse();
        options.IncludeSecurity.Should().BeFalse();
        options.IncludePerformance.Should().BeFalse();
        options.DeploymentEnvironments.Should().NotBeNull().And.BeEmpty();
        options.Cache.Should().BeNull();
        options.Artifacts.Should().BeNull();
        options.Notifications.Should().BeNull();
        options.CustomJobs.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void RequiredProperties_ShouldBeValidated()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "", // Invalid empty string
            Stages = new List<string>() // Invalid empty list
        };
        var validationContext = new ValidationContext(options);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().HaveCount(2);
        validationResults.Should().Contain(vr => vr.ErrorMessage == "Project type is required");
        validationResults.Should().Contain(vr => vr.ErrorMessage == "At least one stage must be specified");
    }

    [Fact]
    public void ValidOptions_ShouldPassValidation()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build", "test" }
        };
        var validationContext = new ValidationContext(options);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(options, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData("dotnet")]
    [InlineData("nodejs")]
    [InlineData("python")]
    [InlineData("docker")]
    [InlineData("generic")]
    public void Validate_WithValidProjectType_ShouldReturnNoErrors(string projectType)
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = projectType,
            Stages = new List<string> { "build" }
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("java")]
    [InlineData("")]
    public void Validate_WithInvalidProjectType_ShouldReturnError(string projectType)
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = projectType,
            Stages = new List<string> { "build" }
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Contains("Invalid project type"));
    }

    [Fact]
    public void Validate_WithEmptyStages_ShouldReturnError()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string>()
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain("At least one stage must be specified");
    }

    [Fact]
    public void Validate_WithWhitespaceStage_ShouldReturnError()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build", "  ", "test" }
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain("Stage names cannot be empty or whitespace");
    }

    [Theory]
    [InlineData("6.0")]
    [InlineData("7.0")]
    [InlineData("8.0")]
    [InlineData("9.0")]
    public void Validate_WithValidDotNetVersion_ShouldReturnNoErrors(string version)
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build" },
            DotNetVersion = version
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("5.0")]
    [InlineData("10.0")]
    [InlineData("invalid")]
    public void Validate_WithInvalidDotNetVersion_ShouldReturnError(string version)
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build" },
            DotNetVersion = version
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Contains("Invalid .NET version"));
    }

    [Fact]
    public void Validate_WithNullDotNetVersion_ShouldReturnNoErrors()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build" },
            DotNetVersion = null
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyVariableName_ShouldReturnError()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build" },
            CustomVariables = new Dictionary<string, string>
            {
                { "", "value" },
                { "VALID_VAR", "value" }
            }
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain("Variable names cannot be empty or whitespace");
    }

    [Fact]
    public void Validate_WithVariableNameContainingSpaces_ShouldReturnError()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build" },
            CustomVariables = new Dictionary<string, string>
            {
                { "INVALID VAR", "value" }
            }
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Contains("cannot contain spaces"));
    }

    [Fact]
    public void Validate_WithValidCustomVariables_ShouldReturnNoErrors()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build" },
            CustomVariables = new Dictionary<string, string>
            {
                { "BUILD_CONFIGURATION", "Release" },
                { "DOTNET_VERSION", "9.0" },
                { "MY_CUSTOM_VAR", "custom_value" }
            }
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithInvalidDeploymentEnvironment_ShouldReturnErrors()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build", "deploy" },
            DeploymentEnvironments = new List<DeploymentEnvironment>
            {
                new DeploymentEnvironment
                {
                    Name = "", // Invalid empty name
                    Url = "invalid-url" // Invalid URL format
                }
            }
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain("Environment name is required");
        errors.Should().Contain(e => e.Contains("Invalid URL format"));
    }

    [Fact]
    public void Validate_WithInvalidCustomJob_ShouldReturnErrors()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build" },
            CustomJobs = new List<CustomJobOptions>
            {
                new CustomJobOptions
                {
                    Name = "", // Invalid empty name
                    Stage = "build",
                    Script = new List<string>() // Invalid empty script
                }
            }
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain("Job name is required");
        errors.Should().Contain(e => e.Contains("At least one script command is required"));
    }

    [Fact]
    public void Validate_WithComplexValidConfiguration_ShouldReturnNoErrors()
    {
        // Arrange
        var options = new PipelineOptions
        {
            ProjectType = "dotnet",
            Stages = new List<string> { "build", "test", "deploy" },
            DotNetVersion = "9.0",
            IncludeTests = true,
            IncludeDeployment = true,
            CustomVariables = new Dictionary<string, string>
            {
                { "BUILD_CONFIGURATION", "Release" },
                { "DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "true" }
            },
            DockerImage = "mcr.microsoft.com/dotnet/sdk:9.0",
            RunnerTags = new List<string> { "docker", "linux" },
            IncludeCodeQuality = true,
            IncludeSecurity = false,
            DeploymentEnvironments = new List<DeploymentEnvironment>
            {
                new DeploymentEnvironment
                {
                    Name = "staging",
                    Url = "https://staging.myapp.com",
                    IsManual = false,
                    AutoDeployPattern = "develop"
                },
                new DeploymentEnvironment
                {
                    Name = "production",
                    Url = "https://myapp.com",
                    IsManual = true
                }
            },
            CustomJobs = new List<CustomJobOptions>
            {
                new CustomJobOptions
                {
                    Name = "custom-lint",
                    Stage = "test",
                    Script = new List<string> { "dotnet format --verify-no-changes" },
                    AllowFailure = true
                }
            }
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().BeEmpty();
    }
}

public class DeploymentEnvironmentTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var env = new DeploymentEnvironment();

        // Assert
        env.Name.Should().Be(string.Empty);
        env.Url.Should().BeNull();
        env.IsManual.Should().BeFalse();
        env.AutoDeployPattern.Should().BeNull();
        env.Variables.Should().NotBeNull().And.BeEmpty();
        env.KubernetesNamespace.Should().BeNull();
        env.AutoStop.Should().BeFalse();
        env.AutoStopIn.Should().BeNull();
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldReturnError()
    {
        // Arrange
        var env = new DeploymentEnvironment { Name = "" };

        // Act
        var errors = env.Validate();

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain("Environment name is required");
    }

    [Theory]
    [InlineData("invalid-url")]
    [InlineData("ftp://example.com")]
    [InlineData("not-a-url")]
    public void Validate_WithInvalidUrl_ShouldReturnError(string invalidUrl)
    {
        // Arrange
        var env = new DeploymentEnvironment
        {
            Name = "test",
            Url = invalidUrl
        };

        // Act
        var errors = env.Validate();

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Contains("Invalid URL format"));
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://localhost:8080")]
    [InlineData("https://staging.myapp.com/api")]
    public void Validate_WithValidUrl_ShouldReturnNoErrors(string validUrl)
    {
        // Arrange
        var env = new DeploymentEnvironment
        {
            Name = "test",
            Url = validUrl
        };

        // Act
        var errors = env.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithAutoStopEnabledButNoAutoStopIn_ShouldReturnError()
    {
        // Arrange
        var env = new DeploymentEnvironment
        {
            Name = "test",
            AutoStop = true,
            AutoStopIn = null
        };

        // Act
        var errors = env.Validate();

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Contains("AutoStopIn is required when AutoStop is enabled"));
    }

    [Fact]
    public void Validate_WithAutoStopEnabledAndAutoStopIn_ShouldReturnNoErrors()
    {
        // Arrange
        var env = new DeploymentEnvironment
        {
            Name = "test",
            AutoStop = true,
            AutoStopIn = "1 day"
        };

        // Act
        var errors = env.Validate();

        // Assert
        errors.Should().BeEmpty();
    }
}

public class CustomJobOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var job = new CustomJobOptions();

        // Assert
        job.Name.Should().Be(string.Empty);
        job.Stage.Should().Be(string.Empty);
        job.Script.Should().NotBeNull().And.BeEmpty();
        job.BeforeScript.Should().NotBeNull().And.BeEmpty();
        job.AfterScript.Should().NotBeNull().And.BeEmpty();
        job.Variables.Should().NotBeNull().And.BeEmpty();
        job.When.Should().BeNull();
        job.AllowFailure.Should().BeFalse();
        job.Image.Should().BeNull();
        job.Tags.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldReturnError()
    {
        // Arrange
        var job = new CustomJobOptions
        {
            Name = "",
            Stage = "test",
            Script = new List<string> { "echo test" }
        };

        // Act
        var errors = job.Validate();

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain("Job name is required");
    }

    [Fact]
    public void Validate_WithEmptyStage_ShouldReturnError()
    {
        // Arrange
        var job = new CustomJobOptions
        {
            Name = "test-job",
            Stage = "",
            Script = new List<string> { "echo test" }
        };

        // Act
        var errors = job.Validate();

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Contains("Stage is required for job"));
    }

    [Fact]
    public void Validate_WithEmptyScript_ShouldReturnError()
    {
        // Arrange
        var job = new CustomJobOptions
        {
            Name = "test-job",
            Stage = "test",
            Script = new List<string>()
        };

        // Act
        var errors = job.Validate();

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Contains("At least one script command is required"));
    }

    [Fact]
    public void Validate_WithWhitespaceOnlyScript_ShouldReturnError()
    {
        // Arrange
        var job = new CustomJobOptions
        {
            Name = "test-job",
            Stage = "test",
            Script = new List<string> { "  ", "" }
        };

        // Act
        var errors = job.Validate();

        // Assert
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Contains("At least one script command is required"));
    }

    [Fact]
    public void Validate_WithValidConfiguration_ShouldReturnNoErrors()
    {
        // Arrange
        var job = new CustomJobOptions
        {
            Name = "custom-test",
            Stage = "test",
            Script = new List<string> { "dotnet test", "echo 'Tests completed'" },
            BeforeScript = new List<string> { "echo 'Starting tests'" },
            Variables = new Dictionary<string, string>
            {
                { "TEST_CONFIGURATION", "Debug" }
            },
            AllowFailure = true,
            Tags = new List<string> { "docker" }
        };

        // Act
        var errors = job.Validate();

        // Assert
        errors.Should().BeEmpty();
    }
}
