using FluentAssertions;
using GitlabPipelineGenerator.Core.Models;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Models;

public class JobTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var job = new Job();

        // Assert
        job.Stage.Should().Be(string.Empty);
        job.Script.Should().NotBeNull().And.BeEmpty();
        job.BeforeScript.Should().BeNull();
        job.AfterScript.Should().BeNull();
        job.Variables.Should().BeNull();
        job.Tags.Should().BeNull();
        job.Artifacts.Should().BeNull();
        job.Cache.Should().BeNull();
        job.Dependencies.Should().BeNull();
        job.Needs.Should().BeNull();
        job.When.Should().BeNull();
        job.AllowFailure.Should().BeNull();
        job.Timeout.Should().BeNull();
        job.Retry.Should().BeNull();
        job.Rules.Should().BeNull();
        job.Only.Should().BeNull();
        job.Except.Should().BeNull();
        job.Image.Should().BeNull();
        job.Services.Should().BeNull();
        job.Environment.Should().BeNull();
        job.Coverage.Should().BeNull();
        job.Parallel.Should().BeNull();
        job.ResourceGroup.Should().BeNull();
        job.Release.Should().BeNull();
    }

    [Fact]
    public void RequiredProperties_ShouldBeValidated()
    {
        // Arrange
        var job = new Job();
        var validationContext = new ValidationContext(job);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(job, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().HaveCount(1);
        validationResults.Should().Contain(vr => vr.ErrorMessage == "Stage is required for all jobs");
    }

    [Fact]
    public void ValidJob_ShouldPassValidation()
    {
        // Arrange
        var job = new Job
        {
            Stage = "build",
            Script = new List<string> { "dotnet build" }
        };
        var validationContext = new ValidationContext(job);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(job, validationContext, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void Script_ShouldAllowMultipleCommands()
    {
        // Arrange
        var job = new Job
        {
            Stage = "build",
            Script = new List<string>
            {
                "dotnet restore",
                "dotnet build",
                "dotnet test"
            }
        };

        // Assert
        job.Script.Should().HaveCount(3);
        job.Script.Should().ContainInOrder("dotnet restore", "dotnet build", "dotnet test");
    }

    [Fact]
    public void BeforeScript_ShouldAllowSettingCommands()
    {
        // Arrange
        var job = new Job
        {
            Stage = "build",
            Script = new List<string> { "dotnet build" },
            BeforeScript = new List<string> { "echo 'Starting build'", "dotnet --version" }
        };

        // Assert
        job.BeforeScript.Should().NotBeNull();
        job.BeforeScript.Should().HaveCount(2);
        job.BeforeScript.Should().ContainInOrder("echo 'Starting build'", "dotnet --version");
    }

    [Fact]
    public void AfterScript_ShouldAllowSettingCommands()
    {
        // Arrange
        var job = new Job
        {
            Stage = "build",
            Script = new List<string> { "dotnet build" },
            AfterScript = new List<string> { "echo 'Build completed'" }
        };

        // Assert
        job.AfterScript.Should().NotBeNull();
        job.AfterScript.Should().HaveCount(1);
        job.AfterScript[0].Should().Be("echo 'Build completed'");
    }

    [Fact]
    public void Variables_ShouldAllowSettingJobSpecificVariables()
    {
        // Arrange
        var job = new Job
        {
            Stage = "build",
            Script = new List<string> { "dotnet build" },
            Variables = new Dictionary<string, object>
            {
                { "BUILD_CONFIGURATION", "Release" },
                { "DOTNET_SKIP_FIRST_TIME_EXPERIENCE", true }
            }
        };

        // Assert
        job.Variables.Should().NotBeNull();
        job.Variables.Should().HaveCount(2);
        job.Variables["BUILD_CONFIGURATION"].Should().Be("Release");
        job.Variables["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"].Should().Be(true);
    }

    [Fact]
    public void Tags_ShouldAllowSettingRunnerTags()
    {
        // Arrange
        var job = new Job
        {
            Stage = "build",
            Script = new List<string> { "dotnet build" },
            Tags = new List<string> { "docker", "linux" }
        };

        // Assert
        job.Tags.Should().NotBeNull();
        job.Tags.Should().HaveCount(2);
        job.Tags.Should().ContainInOrder("docker", "linux");
    }

    [Fact]
    public void Artifacts_ShouldAllowSettingArtifactConfiguration()
    {
        // Arrange
        var artifacts = new JobArtifacts
        {
            Paths = new List<string> { "bin/", "obj/" },
            ExpireIn = "1 week"
        };
        var job = new Job
        {
            Stage = "build",
            Script = new List<string> { "dotnet build" },
            Artifacts = artifacts
        };

        // Assert
        job.Artifacts.Should().NotBeNull();
        job.Artifacts.Should().Be(artifacts);
    }

    [Fact]
    public void Dependencies_ShouldAllowSettingJobDependencies()
    {
        // Arrange
        var job = new Job
        {
            Stage = "test",
            Script = new List<string> { "dotnet test" },
            Dependencies = new List<string> { "build-job" }
        };

        // Assert
        job.Dependencies.Should().NotBeNull();
        job.Dependencies.Should().HaveCount(1);
        job.Dependencies[0].Should().Be("build-job");
    }

    [Fact]
    public void When_ShouldAllowSettingExecutionCondition()
    {
        // Arrange
        var job = new Job
        {
            Stage = "deploy",
            Script = new List<string> { "kubectl apply -f deployment.yml" },
            When = "manual"
        };

        // Assert
        job.When.Should().Be("manual");
    }

    [Fact]
    public void AllowFailure_ShouldAllowSettingFailureHandling()
    {
        // Arrange
        var job = new Job
        {
            Stage = "test",
            Script = new List<string> { "dotnet test" },
            AllowFailure = true
        };

        // Assert
        job.AllowFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("1h")]
    [InlineData("30m")]
    [InlineData("3600")]
    public void Timeout_ShouldAllowValidTimeoutFormats(string timeout)
    {
        // Arrange
        var job = new Job
        {
            Stage = "build",
            Script = new List<string> { "dotnet build" },
            Timeout = timeout
        };

        // Assert
        job.Timeout.Should().Be(timeout);
    }

    [Fact]
    public void Rules_ShouldAllowSettingExecutionRules()
    {
        // Arrange
        var rules = new List<Rule>
        {
            new Rule { If = "$CI_COMMIT_BRANCH == 'main'", When = "always" },
            new Rule { If = "$CI_COMMIT_BRANCH == 'develop'", When = "manual" }
        };
        var job = new Job
        {
            Stage = "deploy",
            Script = new List<string> { "deploy.sh" },
            Rules = rules
        };

        // Assert
        job.Rules.Should().NotBeNull();
        job.Rules.Should().HaveCount(2);
        job.Rules[0].If.Should().Be("$CI_COMMIT_BRANCH == 'main'");
        job.Rules[1].When.Should().Be("manual");
    }

    [Fact]
    public void Image_ShouldAllowSettingDockerImage()
    {
        // Arrange
        var image = new JobImage
        {
            Name = "mcr.microsoft.com/dotnet/sdk:9.0",
            PullPolicy = "always"
        };
        var job = new Job
        {
            Stage = "build",
            Script = new List<string> { "dotnet build" },
            Image = image
        };

        // Assert
        job.Image.Should().NotBeNull();
        job.Image.Name.Should().Be("mcr.microsoft.com/dotnet/sdk:9.0");
        job.Image.PullPolicy.Should().Be("always");
    }

    [Fact]
    public void Environment_ShouldAllowSettingDeploymentEnvironment()
    {
        // Arrange
        var environment = new JobEnvironment
        {
            Name = "production",
            Url = "https://myapp.com",
            Action = "start"
        };
        var job = new Job
        {
            Stage = "deploy",
            Script = new List<string> { "deploy.sh" },
            Environment = environment
        };

        // Assert
        job.Environment.Should().NotBeNull();
        job.Environment.Name.Should().Be("production");
        job.Environment.Url.Should().Be("https://myapp.com");
        job.Environment.Action.Should().Be("start");
    }
}

public class JobArtifactsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithNullValues()
    {
        // Act
        var artifacts = new JobArtifacts();

        // Assert
        artifacts.Paths.Should().BeNull();
        artifacts.Exclude.Should().BeNull();
        artifacts.When.Should().BeNull();
        artifacts.ExpireIn.Should().BeNull();
        artifacts.Name.Should().BeNull();
        artifacts.Reports.Should().BeNull();
        artifacts.Public.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldAllowSettingValues()
    {
        // Arrange
        var artifacts = new JobArtifacts();

        // Act
        artifacts.Paths = new List<string> { "bin/", "obj/" };
        artifacts.Exclude = new List<string> { "*.tmp" };
        artifacts.When = "on_success";
        artifacts.ExpireIn = "1 week";
        artifacts.Name = "build-artifacts";
        artifacts.Public = false;

        // Assert
        artifacts.Paths.Should().HaveCount(2);
        artifacts.Exclude.Should().HaveCount(1);
        artifacts.When.Should().Be("on_success");
        artifacts.ExpireIn.Should().Be("1 week");
        artifacts.Name.Should().Be("build-artifacts");
        artifacts.Public.Should().BeFalse();
    }
}

public class JobCacheTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithNullValues()
    {
        // Act
        var cache = new JobCache();

        // Assert
        cache.Key.Should().BeNull();
        cache.Paths.Should().BeNull();
        cache.Policy.Should().BeNull();
        cache.When.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldAllowSettingValues()
    {
        // Arrange
        var cache = new JobCache();

        // Act
        cache.Key = "dotnet-cache";
        cache.Paths = new List<string> { "~/.nuget/packages" };
        cache.Policy = "pull-push";
        cache.When = "on_success";

        // Assert
        cache.Key.Should().Be("dotnet-cache");
        cache.Paths.Should().HaveCount(1);
        cache.Policy.Should().Be("pull-push");
        cache.When.Should().Be("on_success");
    }
}

public class JobRetryTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithNullValues()
    {
        // Act
        var retry = new JobRetry();

        // Assert
        retry.Max.Should().BeNull();
        retry.When.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldAllowSettingValues()
    {
        // Arrange
        var retry = new JobRetry();

        // Act
        retry.Max = 3;
        retry.When = new List<string> { "runner_system_failure", "stuck_or_timeout_failure" };

        // Assert
        retry.Max.Should().Be(3);
        retry.When.Should().HaveCount(2);
        retry.When.Should().Contain("runner_system_failure");
        retry.When.Should().Contain("stuck_or_timeout_failure");
    }
}

public class JobImageTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithNullValues()
    {
        // Act
        var image = new JobImage();

        // Assert
        image.Name.Should().BeNull();
        image.Entrypoint.Should().BeNull();
        image.PullPolicy.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldAllowSettingValues()
    {
        // Arrange
        var image = new JobImage();

        // Act
        image.Name = "mcr.microsoft.com/dotnet/sdk:9.0";
        image.Entrypoint = new List<string> { "/bin/bash", "-c" };
        image.PullPolicy = "always";

        // Assert
        image.Name.Should().Be("mcr.microsoft.com/dotnet/sdk:9.0");
        image.Entrypoint.Should().HaveCount(2);
        image.PullPolicy.Should().Be("always");
    }
}

public class JobEnvironmentTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithNullValues()
    {
        // Act
        var environment = new JobEnvironment();

        // Assert
        environment.Name.Should().BeNull();
        environment.Url.Should().BeNull();
        environment.Action.Should().BeNull();
        environment.AutoStopIn.Should().BeNull();
        environment.Kubernetes.Should().BeNull();
        environment.DeploymentTier.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldAllowSettingValues()
    {
        // Arrange
        var environment = new JobEnvironment();

        // Act
        environment.Name = "staging";
        environment.Url = "https://staging.myapp.com";
        environment.Action = "start";
        environment.AutoStopIn = "1 day";
        environment.DeploymentTier = "staging";

        // Assert
        environment.Name.Should().Be("staging");
        environment.Url.Should().Be("https://staging.myapp.com");
        environment.Action.Should().Be("start");
        environment.AutoStopIn.Should().Be("1 day");
        environment.DeploymentTier.Should().Be("staging");
    }
}
