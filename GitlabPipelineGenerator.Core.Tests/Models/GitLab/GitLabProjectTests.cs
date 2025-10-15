using FluentAssertions;
using GitlabPipelineGenerator.Core.Models.GitLab;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Models.GitLab;

/// <summary>
/// Unit tests for GitLabProject model
/// </summary>
public class GitLabProjectTests
{
    #region Default Values Tests

    [Fact]
    public void GitLabProject_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var project = new GitLabProject();

        // Assert
        project.Id.Should().Be(0);
        project.Name.Should().Be(string.Empty);
        project.Path.Should().Be(string.Empty);
        project.FullPath.Should().Be(string.Empty);
        project.Description.Should().BeNull();
        project.DefaultBranch.Should().Be("main");
        project.LastActivityAt.Should().Be(default(DateTime));
        project.Visibility.Should().Be(ProjectVisibility.Private);
        project.WebUrl.Should().Be(string.Empty);
        project.SshUrl.Should().BeNull();
        project.HttpUrl.Should().BeNull();
        project.Namespace.Should().BeNull();
    }

    #endregion

    #region Property Setting Tests

    [Fact]
    public void GitLabProject_SetBasicProperties_ShouldRetainValues()
    {
        // Arrange & Act
        var project = new GitLabProject
        {
            Id = 123,
            Name = "test-project",
            Path = "test-project",
            FullPath = "test-namespace/test-project",
            Description = "Test project description",
            DefaultBranch = "develop"
        };

        // Assert
        project.Id.Should().Be(123);
        project.Name.Should().Be("test-project");
        project.Path.Should().Be("test-project");
        project.FullPath.Should().Be("test-namespace/test-project");
        project.Description.Should().Be("Test project description");
        project.DefaultBranch.Should().Be("develop");
    }

    [Fact]
    public void GitLabProject_SetDateTimeAndEnumProperties_ShouldRetainValues()
    {
        // Arrange
        var lastActivity = DateTime.UtcNow.AddDays(-1);

        // Act
        var project = new GitLabProject
        {
            LastActivityAt = lastActivity,
            Visibility = ProjectVisibility.Public
        };

        // Assert
        project.LastActivityAt.Should().Be(lastActivity);
        project.Visibility.Should().Be(ProjectVisibility.Public);
    }

    [Fact]
    public void GitLabProject_SetUrlProperties_ShouldRetainValues()
    {
        // Arrange & Act
        var project = new GitLabProject
        {
            WebUrl = "https://gitlab.com/test-namespace/test-project",
            SshUrl = "git@gitlab.com:test-namespace/test-project.git",
            HttpUrl = "https://gitlab.com/test-namespace/test-project.git"
        };

        // Assert
        project.WebUrl.Should().Be("https://gitlab.com/test-namespace/test-project");
        project.SshUrl.Should().Be("git@gitlab.com:test-namespace/test-project.git");
        project.HttpUrl.Should().Be("https://gitlab.com/test-namespace/test-project.git");
    }

    [Fact]
    public void GitLabProject_SetNamespaceProperty_ShouldRetainValue()
    {
        // Arrange
        var namespace1 = new GitLabNamespace
        {
            Id = 1,
            Name = "test-namespace",
            Path = "test-namespace",
            Kind = "group"
        };

        // Act
        var project = new GitLabProject
        {
            Namespace = namespace1
        };

        // Assert
        project.Namespace.Should().Be(namespace1);
        project.Namespace!.Id.Should().Be(1);
        project.Namespace.Name.Should().Be("test-namespace");
        project.Namespace.Path.Should().Be("test-namespace");
        project.Namespace.Kind.Should().Be("group");
    }

    #endregion

    #region Comprehensive Configuration Tests

    [Fact]
    public void GitLabProject_FullConfiguration_ShouldRetainAllValues()
    {
        // Arrange
        var lastActivity = DateTime.UtcNow.AddHours(-2);
        var namespace1 = new GitLabNamespace
        {
            Id = 42,
            Name = "my-organization",
            Path = "my-org",
            Kind = "group"
        };

        // Act
        var project = new GitLabProject
        {
            Id = 456,
            Name = "awesome-project",
            Path = "awesome-project",
            FullPath = "my-org/awesome-project",
            Description = "An awesome project for testing",
            DefaultBranch = "master",
            LastActivityAt = lastActivity,
            Visibility = ProjectVisibility.Internal,
            WebUrl = "https://gitlab.example.com/my-org/awesome-project",
            SshUrl = "git@gitlab.example.com:my-org/awesome-project.git",
            HttpUrl = "https://gitlab.example.com/my-org/awesome-project.git",
            Namespace = namespace1
        };

        // Assert
        project.Id.Should().Be(456);
        project.Name.Should().Be("awesome-project");
        project.Path.Should().Be("awesome-project");
        project.FullPath.Should().Be("my-org/awesome-project");
        project.Description.Should().Be("An awesome project for testing");
        project.DefaultBranch.Should().Be("master");
        project.LastActivityAt.Should().Be(lastActivity);
        project.Visibility.Should().Be(ProjectVisibility.Internal);
        project.WebUrl.Should().Be("https://gitlab.example.com/my-org/awesome-project");
        project.SshUrl.Should().Be("git@gitlab.example.com:my-org/awesome-project.git");
        project.HttpUrl.Should().Be("https://gitlab.example.com/my-org/awesome-project.git");
        project.Namespace.Should().Be(namespace1);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void GitLabProject_SetNullablePropertiesToNull_ShouldRetainNullValues()
    {
        // Arrange & Act
        var project = new GitLabProject
        {
            Description = null,
            SshUrl = null,
            HttpUrl = null,
            Namespace = null
        };

        // Assert
        project.Description.Should().BeNull();
        project.SshUrl.Should().BeNull();
        project.HttpUrl.Should().BeNull();
        project.Namespace.Should().BeNull();
    }

    [Fact]
    public void GitLabProject_SetEmptyStringProperties_ShouldRetainEmptyValues()
    {
        // Arrange & Act
        var project = new GitLabProject
        {
            Name = "",
            Path = "",
            FullPath = "",
            DefaultBranch = "",
            WebUrl = ""
        };

        // Assert
        project.Name.Should().Be("");
        project.Path.Should().Be("");
        project.FullPath.Should().Be("");
        project.DefaultBranch.Should().Be("");
        project.WebUrl.Should().Be("");
    }

    [Fact]
    public void GitLabProject_SetMinimumDateTime_ShouldRetainValue()
    {
        // Arrange & Act
        var project = new GitLabProject
        {
            LastActivityAt = DateTime.MinValue
        };

        // Assert
        project.LastActivityAt.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void GitLabProject_SetMaximumDateTime_ShouldRetainValue()
    {
        // Arrange & Act
        var project = new GitLabProject
        {
            LastActivityAt = DateTime.MaxValue
        };

        // Assert
        project.LastActivityAt.Should().Be(DateTime.MaxValue);
    }

    #endregion

    #region Visibility Enum Tests

    [Fact]
    public void GitLabProject_AllVisibilityValues_ShouldBeSupported()
    {
        // Test Private
        var project1 = new GitLabProject { Visibility = ProjectVisibility.Private };
        project1.Visibility.Should().Be(ProjectVisibility.Private);

        // Test Internal
        var project2 = new GitLabProject { Visibility = ProjectVisibility.Internal };
        project2.Visibility.Should().Be(ProjectVisibility.Internal);

        // Test Public
        var project3 = new GitLabProject { Visibility = ProjectVisibility.Public };
        project3.Visibility.Should().Be(ProjectVisibility.Public);
    }

    #endregion
}

/// <summary>
/// Unit tests for GitLabNamespace model
/// </summary>
public class GitLabNamespaceTests
{
    #region Default Values Tests

    [Fact]
    public void GitLabNamespace_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var namespace1 = new GitLabNamespace();

        // Assert
        namespace1.Id.Should().Be(0);
        namespace1.Name.Should().Be(string.Empty);
        namespace1.Path.Should().Be(string.Empty);
        namespace1.Kind.Should().Be(string.Empty);
    }

    #endregion

    #region Property Setting Tests

    [Fact]
    public void GitLabNamespace_SetProperties_ShouldRetainValues()
    {
        // Arrange & Act
        var namespace1 = new GitLabNamespace
        {
            Id = 789,
            Name = "test-namespace",
            Path = "test-namespace",
            Kind = "user"
        };

        // Assert
        namespace1.Id.Should().Be(789);
        namespace1.Name.Should().Be("test-namespace");
        namespace1.Path.Should().Be("test-namespace");
        namespace1.Kind.Should().Be("user");
    }

    [Fact]
    public void GitLabNamespace_SetEmptyStringProperties_ShouldRetainEmptyValues()
    {
        // Arrange & Act
        var namespace1 = new GitLabNamespace
        {
            Name = "",
            Path = "",
            Kind = ""
        };

        // Assert
        namespace1.Name.Should().Be("");
        namespace1.Path.Should().Be("");
        namespace1.Kind.Should().Be("");
    }

    #endregion

    #region Kind Values Tests

    [Fact]
    public void GitLabNamespace_CommonKindValues_ShouldBeSupported()
    {
        // Test user kind
        var userNamespace = new GitLabNamespace { Kind = "user" };
        userNamespace.Kind.Should().Be("user");

        // Test group kind
        var groupNamespace = new GitLabNamespace { Kind = "group" };
        groupNamespace.Kind.Should().Be("group");
    }

    #endregion
}