using FluentAssertions;
using GitlabPipelineGenerator.Core.Models;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Models;

public class PipelineConfigurationTests
{
    [Fact]
    public void Constructor_ShouldInitializeEmptyCollections()
    {
        // Act
        var config = new PipelineConfiguration();

        // Assert
        config.Stages.Should().NotBeNull().And.BeEmpty();
        config.Jobs.Should().NotBeNull().And.BeEmpty();
        config.Variables.Should().NotBeNull().And.BeEmpty();
        config.Default.Should().NotBeNull().And.BeEmpty();
        config.Workflow.Should().BeNull();
        config.Include.Should().BeNull();
    }

    [Fact]
    public void Stages_ShouldAllowAddingStages()
    {
        // Arrange
        var config = new PipelineConfiguration();
        var stages = new List<string> { "build", "test", "deploy" };

        // Act
        config.Stages.AddRange(stages);

        // Assert
        config.Stages.Should().HaveCount(3);
        config.Stages.Should().ContainInOrder("build", "test", "deploy");
    }

    [Fact]
    public void Jobs_ShouldAllowAddingJobs()
    {
        // Arrange
        var config = new PipelineConfiguration();
        var job = new Job
        {
            Stage = "build",
            Script = new List<string> { "dotnet build" }
        };

        // Act
        config.Jobs.Add("build-job", job);

        // Assert
        config.Jobs.Should().HaveCount(1);
        config.Jobs.Should().ContainKey("build-job");
        config.Jobs["build-job"].Should().Be(job);
    }

    [Fact]
    public void Variables_ShouldAllowAddingVariables()
    {
        // Arrange
        var config = new PipelineConfiguration();

        // Act
        config.Variables.Add("DOTNET_VERSION", "9.0");
        config.Variables.Add("BUILD_CONFIGURATION", "Release");

        // Assert
        config.Variables.Should().HaveCount(2);
        config.Variables.Should().ContainKeys("DOTNET_VERSION", "BUILD_CONFIGURATION");
        config.Variables["DOTNET_VERSION"].Should().Be("9.0");
        config.Variables["BUILD_CONFIGURATION"].Should().Be("Release");
    }

    [Fact]
    public void Default_ShouldAllowAddingDefaultSettings()
    {
        // Arrange
        var config = new PipelineConfiguration();

        // Act
        config.Default.Add("image", "mcr.microsoft.com/dotnet/sdk:9.0");
        config.Default.Add("before_script", new List<string> { "echo 'Starting job'" });

        // Assert
        config.Default.Should().HaveCount(2);
        config.Default.Should().ContainKeys("image", "before_script");
        config.Default["image"].Should().Be("mcr.microsoft.com/dotnet/sdk:9.0");
    }

    [Fact]
    public void Workflow_ShouldAllowSettingWorkflowRules()
    {
        // Arrange
        var config = new PipelineConfiguration();
        var workflow = new WorkflowRules
        {
            Rules = new List<Rule>
            {
                new Rule { If = "$CI_COMMIT_BRANCH == 'main'", When = "always" }
            }
        };

        // Act
        config.Workflow = workflow;

        // Assert
        config.Workflow.Should().NotBeNull();
        config.Workflow.Rules.Should().HaveCount(1);
        config.Workflow.Rules[0].If.Should().Be("$CI_COMMIT_BRANCH == 'main'");
        config.Workflow.Rules[0].When.Should().Be("always");
    }

    [Fact]
    public void Include_ShouldAllowAddingIncludeRules()
    {
        // Arrange
        var config = new PipelineConfiguration();
        var includeRules = new List<IncludeRule>
        {
            new IncludeRule { Local = ".gitlab/ci/build.yml" },
            new IncludeRule { Template = "Security/SAST.gitlab-ci.yml" }
        };

        // Act
        config.Include = includeRules;

        // Assert
        config.Include.Should().NotBeNull();
        config.Include.Should().HaveCount(2);
        config.Include[0].Local.Should().Be(".gitlab/ci/build.yml");
        config.Include[1].Template.Should().Be("Security/SAST.gitlab-ci.yml");
    }
}

public class WorkflowRulesTests
{
    [Fact]
    public void Constructor_ShouldInitializeEmptyRulesList()
    {
        // Act
        var workflow = new WorkflowRules();

        // Assert
        workflow.Rules.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Rules_ShouldAllowAddingRules()
    {
        // Arrange
        var workflow = new WorkflowRules();
        var rule = new Rule
        {
            If = "$CI_COMMIT_BRANCH == 'main'",
            When = "always",
            AllowFailure = false
        };

        // Act
        workflow.Rules.Add(rule);

        // Assert
        workflow.Rules.Should().HaveCount(1);
        workflow.Rules[0].Should().Be(rule);
    }
}

public class RuleTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithNullValues()
    {
        // Act
        var rule = new Rule();

        // Assert
        rule.If.Should().BeNull();
        rule.When.Should().BeNull();
        rule.AllowFailure.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldAllowSettingValues()
    {
        // Arrange
        var rule = new Rule();

        // Act
        rule.If = "$CI_COMMIT_BRANCH == 'develop'";
        rule.When = "manual";
        rule.AllowFailure = true;

        // Assert
        rule.If.Should().Be("$CI_COMMIT_BRANCH == 'develop'");
        rule.When.Should().Be("manual");
        rule.AllowFailure.Should().BeTrue();
    }
}

public class IncludeRuleTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithNullValues()
    {
        // Act
        var includeRule = new IncludeRule();

        // Assert
        includeRule.Local.Should().BeNull();
        includeRule.Remote.Should().BeNull();
        includeRule.Template.Should().BeNull();
        includeRule.Project.Should().BeNull();
        includeRule.File.Should().BeNull();
        includeRule.Ref.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldAllowSettingValues()
    {
        // Arrange
        var includeRule = new IncludeRule();

        // Act
        includeRule.Local = ".gitlab/ci/build.yml";
        includeRule.Remote = "https://example.com/ci.yml";
        includeRule.Template = "Security/SAST.gitlab-ci.yml";
        includeRule.Project = "group/project";
        includeRule.File = "ci/build.yml";
        includeRule.Ref = "main";

        // Assert
        includeRule.Local.Should().Be(".gitlab/ci/build.yml");
        includeRule.Remote.Should().Be("https://example.com/ci.yml");
        includeRule.Template.Should().Be("Security/SAST.gitlab-ci.yml");
        includeRule.Project.Should().Be("group/project");
        includeRule.File.Should().Be("ci/build.yml");
        includeRule.Ref.Should().Be("main");
    }

    [Theory]
    [InlineData("local", ".gitlab/ci/build.yml")]
    [InlineData("remote", "https://example.com/ci.yml")]
    [InlineData("template", "Security/SAST.gitlab-ci.yml")]
    public void SingleProperty_ShouldBeValidIncludeRule(string propertyType, string value)
    {
        // Arrange
        var includeRule = new IncludeRule();

        // Act & Assert
        switch (propertyType)
        {
            case "local":
                includeRule.Local = value;
                includeRule.Local.Should().Be(value);
                break;
            case "remote":
                includeRule.Remote = value;
                includeRule.Remote.Should().Be(value);
                break;
            case "template":
                includeRule.Template = value;
                includeRule.Template.Should().Be(value);
                break;
        }
    }
}
