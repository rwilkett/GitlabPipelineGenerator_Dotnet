using FluentAssertions;
using GitlabPipelineGenerator.Core.Models.GitLab;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Models.GitLab;

/// <summary>
/// Unit tests for ProjectListOptions model
/// </summary>
public class ProjectListOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void ProjectListOptions_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var options = new ProjectListOptions();

        // Assert
        options.OwnedOnly.Should().BeFalse();
        options.MemberOnly.Should().BeTrue();
        options.Visibility.Should().BeNull();
        options.Search.Should().BeNull();
        options.OrderBy.Should().Be(ProjectOrderBy.LastActivity);
        options.Ascending.Should().BeFalse();
        options.MaxResults.Should().Be(50);
        options.Page.Should().Be(1);
        options.PerPage.Should().Be(20);
        options.IncludeArchived.Should().BeFalse();
        options.MinAccessLevel.Should().BeNull();
        options.Language.Should().BeNull();
        options.CreatedAfter.Should().BeNull();
        options.CreatedBefore.Should().BeNull();
        options.LastActivityAfter.Should().BeNull();
        options.LastActivityBefore.Should().BeNull();
    }

    #endregion

    #region Property Setting Tests

    [Fact]
    public void ProjectListOptions_SetBooleanProperties_ShouldRetainValues()
    {
        // Arrange & Act
        var options = new ProjectListOptions
        {
            OwnedOnly = true,
            MemberOnly = false,
            Ascending = true,
            IncludeArchived = true
        };

        // Assert
        options.OwnedOnly.Should().BeTrue();
        options.MemberOnly.Should().BeFalse();
        options.Ascending.Should().BeTrue();
        options.IncludeArchived.Should().BeTrue();
    }

    [Fact]
    public void ProjectListOptions_SetEnumProperties_ShouldRetainValues()
    {
        // Arrange & Act
        var options = new ProjectListOptions
        {
            Visibility = ProjectVisibility.Public,
            OrderBy = ProjectOrderBy.Name,
            MinAccessLevel = AccessLevel.Developer
        };

        // Assert
        options.Visibility.Should().Be(ProjectVisibility.Public);
        options.OrderBy.Should().Be(ProjectOrderBy.Name);
        options.MinAccessLevel.Should().Be(AccessLevel.Developer);
    }

    [Fact]
    public void ProjectListOptions_SetStringProperties_ShouldRetainValues()
    {
        // Arrange & Act
        var options = new ProjectListOptions
        {
            Search = "test-search-term",
            Language = "C#"
        };

        // Assert
        options.Search.Should().Be("test-search-term");
        options.Language.Should().Be("C#");
    }

    [Fact]
    public void ProjectListOptions_SetNumericProperties_ShouldRetainValues()
    {
        // Arrange & Act
        var options = new ProjectListOptions
        {
            MaxResults = 100,
            Page = 3,
            PerPage = 25
        };

        // Assert
        options.MaxResults.Should().Be(100);
        options.Page.Should().Be(3);
        options.PerPage.Should().Be(25);
    }

    [Fact]
    public void ProjectListOptions_SetDateTimeProperties_ShouldRetainValues()
    {
        // Arrange
        var createdAfter = DateTime.UtcNow.AddDays(-30);
        var createdBefore = DateTime.UtcNow.AddDays(-1);
        var activityAfter = DateTime.UtcNow.AddDays(-7);
        var activityBefore = DateTime.UtcNow;

        // Act
        var options = new ProjectListOptions
        {
            CreatedAfter = createdAfter,
            CreatedBefore = createdBefore,
            LastActivityAfter = activityAfter,
            LastActivityBefore = activityBefore
        };

        // Assert
        options.CreatedAfter.Should().Be(createdAfter);
        options.CreatedBefore.Should().Be(createdBefore);
        options.LastActivityAfter.Should().Be(activityAfter);
        options.LastActivityBefore.Should().Be(activityBefore);
    }

    #endregion

    #region Comprehensive Configuration Tests

    [Fact]
    public void ProjectListOptions_FullConfiguration_ShouldRetainAllValues()
    {
        // Arrange
        var createdAfter = DateTime.UtcNow.AddDays(-30);
        var createdBefore = DateTime.UtcNow.AddDays(-1);
        var activityAfter = DateTime.UtcNow.AddDays(-7);
        var activityBefore = DateTime.UtcNow;

        // Act
        var options = new ProjectListOptions
        {
            OwnedOnly = true,
            MemberOnly = false,
            Visibility = ProjectVisibility.Internal,
            Search = "my-project",
            OrderBy = ProjectOrderBy.CreatedAt,
            Ascending = true,
            MaxResults = 75,
            Page = 2,
            PerPage = 30,
            IncludeArchived = true,
            MinAccessLevel = AccessLevel.Maintainer,
            Language = "TypeScript",
            CreatedAfter = createdAfter,
            CreatedBefore = createdBefore,
            LastActivityAfter = activityAfter,
            LastActivityBefore = activityBefore
        };

        // Assert
        options.OwnedOnly.Should().BeTrue();
        options.MemberOnly.Should().BeFalse();
        options.Visibility.Should().Be(ProjectVisibility.Internal);
        options.Search.Should().Be("my-project");
        options.OrderBy.Should().Be(ProjectOrderBy.CreatedAt);
        options.Ascending.Should().BeTrue();
        options.MaxResults.Should().Be(75);
        options.Page.Should().Be(2);
        options.PerPage.Should().Be(30);
        options.IncludeArchived.Should().BeTrue();
        options.MinAccessLevel.Should().Be(AccessLevel.Maintainer);
        options.Language.Should().Be("TypeScript");
        options.CreatedAfter.Should().Be(createdAfter);
        options.CreatedBefore.Should().Be(createdBefore);
        options.LastActivityAfter.Should().Be(activityAfter);
        options.LastActivityBefore.Should().Be(activityBefore);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void ProjectListOptions_SetNullablePropertiesToNull_ShouldRetainNullValues()
    {
        // Arrange & Act
        var options = new ProjectListOptions
        {
            Visibility = null,
            Search = null,
            MinAccessLevel = null,
            Language = null,
            CreatedAfter = null,
            CreatedBefore = null,
            LastActivityAfter = null,
            LastActivityBefore = null
        };

        // Assert
        options.Visibility.Should().BeNull();
        options.Search.Should().BeNull();
        options.MinAccessLevel.Should().BeNull();
        options.Language.Should().BeNull();
        options.CreatedAfter.Should().BeNull();
        options.CreatedBefore.Should().BeNull();
        options.LastActivityAfter.Should().BeNull();
        options.LastActivityBefore.Should().BeNull();
    }

    [Fact]
    public void ProjectListOptions_SetEmptyStringProperties_ShouldRetainEmptyValues()
    {
        // Arrange & Act
        var options = new ProjectListOptions
        {
            Search = "",
            Language = ""
        };

        // Assert
        options.Search.Should().Be("");
        options.Language.Should().Be("");
    }

    [Fact]
    public void ProjectListOptions_SetZeroNumericProperties_ShouldRetainZeroValues()
    {
        // Arrange & Act
        var options = new ProjectListOptions
        {
            MaxResults = 0,
            Page = 0,
            PerPage = 0
        };

        // Assert
        options.MaxResults.Should().Be(0);
        options.Page.Should().Be(0);
        options.PerPage.Should().Be(0);
    }

    #endregion

    #region Enum Values Tests

    [Fact]
    public void ProjectListOptions_AllProjectVisibilityValues_ShouldBeSupported()
    {
        // Test Private
        var options1 = new ProjectListOptions { Visibility = ProjectVisibility.Private };
        options1.Visibility.Should().Be(ProjectVisibility.Private);

        // Test Internal
        var options2 = new ProjectListOptions { Visibility = ProjectVisibility.Internal };
        options2.Visibility.Should().Be(ProjectVisibility.Internal);

        // Test Public
        var options3 = new ProjectListOptions { Visibility = ProjectVisibility.Public };
        options3.Visibility.Should().Be(ProjectVisibility.Public);
    }

    [Fact]
    public void ProjectListOptions_AllProjectOrderByValues_ShouldBeSupported()
    {
        // Test Id
        var options1 = new ProjectListOptions { OrderBy = ProjectOrderBy.Id };
        options1.OrderBy.Should().Be(ProjectOrderBy.Id);

        // Test Name
        var options2 = new ProjectListOptions { OrderBy = ProjectOrderBy.Name };
        options2.OrderBy.Should().Be(ProjectOrderBy.Name);

        // Test CreatedAt
        var options3 = new ProjectListOptions { OrderBy = ProjectOrderBy.CreatedAt };
        options3.OrderBy.Should().Be(ProjectOrderBy.CreatedAt);

        // Test LastActivity
        var options4 = new ProjectListOptions { OrderBy = ProjectOrderBy.LastActivity };
        options4.OrderBy.Should().Be(ProjectOrderBy.LastActivity);

        // Test UpdatedAt
        var options5 = new ProjectListOptions { OrderBy = ProjectOrderBy.UpdatedAt };
        options5.OrderBy.Should().Be(ProjectOrderBy.UpdatedAt);
    }

    [Fact]
    public void ProjectListOptions_AllAccessLevelValues_ShouldBeSupported()
    {
        // Test NoAccess
        var options1 = new ProjectListOptions { MinAccessLevel = AccessLevel.NoAccess };
        options1.MinAccessLevel.Should().Be(AccessLevel.NoAccess);

        // Test MinimalAccess
        var options2 = new ProjectListOptions { MinAccessLevel = AccessLevel.MinimalAccess };
        options2.MinAccessLevel.Should().Be(AccessLevel.MinimalAccess);

        // Test Guest
        var options3 = new ProjectListOptions { MinAccessLevel = AccessLevel.Guest };
        options3.MinAccessLevel.Should().Be(AccessLevel.Guest);

        // Test Reporter
        var options4 = new ProjectListOptions { MinAccessLevel = AccessLevel.Reporter };
        options4.MinAccessLevel.Should().Be(AccessLevel.Reporter);

        // Test Developer
        var options5 = new ProjectListOptions { MinAccessLevel = AccessLevel.Developer };
        options5.MinAccessLevel.Should().Be(AccessLevel.Developer);

        // Test Maintainer
        var options6 = new ProjectListOptions { MinAccessLevel = AccessLevel.Maintainer };
        options6.MinAccessLevel.Should().Be(AccessLevel.Maintainer);

        // Test Owner
        var options7 = new ProjectListOptions { MinAccessLevel = AccessLevel.Owner };
        options7.MinAccessLevel.Should().Be(AccessLevel.Owner);
    }

    #endregion
}