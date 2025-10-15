using FluentAssertions;
using GitlabPipelineGenerator.Core.Models.GitLab;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Models.GitLab;

/// <summary>
/// Unit tests for ProjectPermissions model
/// </summary>
public class ProjectPermissionsTests
{
    #region HasPermissions Tests

    [Fact]
    public void HasPermissions_WithAllRequiredPermissions_ShouldReturnTrue()
    {
        // Arrange
        var permissions = new ProjectPermissions
        {
            ProjectId = 123,
            AccessLevel = AccessLevel.Developer,
            CanReadProject = true,
            CanReadRepository = true,
            CanReadCiCd = true,
            CanWriteRepository = true,
            CanManageCiCd = false
        };

        // Act
        var result = permissions.HasPermissions(RequiredPermissions.ReadProject | RequiredPermissions.ReadRepository | RequiredPermissions.ReadCiCd);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasPermissions_WithMissingReadProjectPermission_ShouldReturnFalse()
    {
        // Arrange
        var permissions = new ProjectPermissions
        {
            ProjectId = 123,
            AccessLevel = AccessLevel.Guest,
            CanReadProject = false, // Missing this permission
            CanReadRepository = true,
            CanReadCiCd = true,
            CanWriteRepository = false,
            CanManageCiCd = false
        };

        // Act
        var result = permissions.HasPermissions(RequiredPermissions.ReadProject);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasPermissions_WithMissingReadRepositoryPermission_ShouldReturnFalse()
    {
        // Arrange
        var permissions = new ProjectPermissions
        {
            ProjectId = 123,
            AccessLevel = AccessLevel.Guest,
            CanReadProject = true,
            CanReadRepository = false, // Missing this permission
            CanReadCiCd = true,
            CanWriteRepository = false,
            CanManageCiCd = false
        };

        // Act
        var result = permissions.HasPermissions(RequiredPermissions.ReadRepository);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasPermissions_WithMissingReadCiCdPermission_ShouldReturnFalse()
    {
        // Arrange
        var permissions = new ProjectPermissions
        {
            ProjectId = 123,
            AccessLevel = AccessLevel.Guest,
            CanReadProject = true,
            CanReadRepository = true,
            CanReadCiCd = false, // Missing this permission
            CanWriteRepository = false,
            CanManageCiCd = false
        };

        // Act
        var result = permissions.HasPermissions(RequiredPermissions.ReadCiCd);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasPermissions_WithNoPermissions_ShouldReturnFalse()
    {
        // Arrange
        var permissions = new ProjectPermissions
        {
            ProjectId = 123,
            AccessLevel = AccessLevel.NoAccess,
            CanReadProject = false,
            CanReadRepository = false,
            CanReadCiCd = false,
            CanWriteRepository = false,
            CanManageCiCd = false
        };

        // Act
        var result = permissions.HasPermissions(RequiredPermissions.ReadProject);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasPermissions_WithNoneRequired_ShouldReturnTrue()
    {
        // Arrange
        var permissions = new ProjectPermissions
        {
            ProjectId = 123,
            AccessLevel = AccessLevel.Guest,
            CanReadProject = false,
            CanReadRepository = false,
            CanReadCiCd = false,
            CanWriteRepository = false,
            CanManageCiCd = false
        };

        // Act
        var result = permissions.HasPermissions(RequiredPermissions.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasPermissions_WithMultipleRequiredPermissions_AllPresent_ShouldReturnTrue()
    {
        // Arrange
        var permissions = new ProjectPermissions
        {
            ProjectId = 123,
            AccessLevel = AccessLevel.Developer,
            CanReadProject = true,
            CanReadRepository = true,
            CanReadCiCd = true,
            CanWriteRepository = true,
            CanManageCiCd = false
        };

        // Act
        var result = permissions.HasPermissions(RequiredPermissions.ReadProject | RequiredPermissions.ReadRepository | RequiredPermissions.ReadCiCd);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasPermissions_WithMultipleRequiredPermissions_SomeMissing_ShouldReturnFalse()
    {
        // Arrange
        var permissions = new ProjectPermissions
        {
            ProjectId = 123,
            AccessLevel = AccessLevel.Guest,
            CanReadProject = true,
            CanReadRepository = false, // Missing
            CanReadCiCd = false, // Missing
            CanWriteRepository = false,
            CanManageCiCd = false
        };

        // Act
        var result = permissions.HasPermissions(RequiredPermissions.ReadProject | RequiredPermissions.ReadRepository | RequiredPermissions.ReadCiCd);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetMissingPermissions Tests

    [Fact]
    public void GetMissingPermissions_WithAllPermissions_ShouldReturnEmptyList()
    {
        // Arrange
        var permissions = new ProjectPermissions
        {
            ProjectId = 123,
            AccessLevel = AccessLevel.Developer,
            CanReadProject = true,
            CanReadRepository = true,
            CanReadCiCd = true,
            CanWriteRepository = true,
            CanManageCiCd = false
        };

        // Act
        var missing = permissions.GetMissingPermissions(RequiredPermissions.ReadProject | RequiredPermissions.ReadRepository | RequiredPermissions.ReadCiCd);

        // Assert
        missing.Should().BeEmpty();
    }

    [Fact]
    public void GetMissingPermissions_WithSomeMissing_ShouldReturnCorrectList()
    {
        // Arrange
        var permissions = new ProjectPermissions
        {
            ProjectId = 123,
            AccessLevel = AccessLevel.Guest,
            CanReadProject = true,
            CanReadRepository = false,
            CanReadCiCd = false,
            CanWriteRepository = false,
            CanManageCiCd = false
        };

        // Act
        var missing = permissions.GetMissingPermissions(RequiredPermissions.ReadProject | RequiredPermissions.ReadRepository | RequiredPermissions.ReadCiCd);

        // Assert
        missing.Should().HaveCount(2);
        missing.Should().Contain("Read Repository");
        missing.Should().Contain("Read CI/CD");
        missing.Should().NotContain("Read Project");
    }

    [Fact]
    public void GetMissingPermissions_WithAllMissing_ShouldReturnAllRequired()
    {
        // Arrange
        var permissions = new ProjectPermissions
        {
            ProjectId = 123,
            AccessLevel = AccessLevel.NoAccess,
            CanReadProject = false,
            CanReadRepository = false,
            CanReadCiCd = false,
            CanWriteRepository = false,
            CanManageCiCd = false
        };

        // Act
        var missing = permissions.GetMissingPermissions(RequiredPermissions.ReadProject | RequiredPermissions.ReadRepository | RequiredPermissions.ReadCiCd);

        // Assert
        missing.Should().HaveCount(3);
        missing.Should().Contain("Read Project");
        missing.Should().Contain("Read Repository");
        missing.Should().Contain("Read CI/CD");
    }

    [Fact]
    public void GetMissingPermissions_WithNoRequiredPermissions_ShouldReturnEmptyList()
    {
        // Arrange
        var permissions = new ProjectPermissions
        {
            ProjectId = 123,
            AccessLevel = AccessLevel.Guest,
            CanReadProject = false,
            CanReadRepository = false,
            CanReadCiCd = false,
            CanWriteRepository = false,
            CanManageCiCd = false
        };

        // Act
        var missing = permissions.GetMissingPermissions(RequiredPermissions.None);

        // Assert
        missing.Should().BeEmpty();
    }

    [Fact]
    public void GetMissingPermissions_WithOnlyReadProjectRequired_Missing_ShouldReturnCorrectList()
    {
        // Arrange
        var permissions = new ProjectPermissions
        {
            ProjectId = 123,
            AccessLevel = AccessLevel.NoAccess,
            CanReadProject = false,
            CanReadRepository = true,
            CanReadCiCd = true,
            CanWriteRepository = false,
            CanManageCiCd = false
        };

        // Act
        var missing = permissions.GetMissingPermissions(RequiredPermissions.ReadProject);

        // Assert
        missing.Should().HaveCount(1);
        missing.Should().Contain("Read Project");
    }

    [Fact]
    public void GetMissingPermissions_WithOnlyReadRepositoryRequired_Missing_ShouldReturnCorrectList()
    {
        // Arrange
        var permissions = new ProjectPermissions
        {
            ProjectId = 123,
            AccessLevel = AccessLevel.Guest,
            CanReadProject = true,
            CanReadRepository = false,
            CanReadCiCd = true,
            CanWriteRepository = false,
            CanManageCiCd = false
        };

        // Act
        var missing = permissions.GetMissingPermissions(RequiredPermissions.ReadRepository);

        // Assert
        missing.Should().HaveCount(1);
        missing.Should().Contain("Read Repository");
    }

    [Fact]
    public void GetMissingPermissions_WithOnlyReadCiCdRequired_Missing_ShouldReturnCorrectList()
    {
        // Arrange
        var permissions = new ProjectPermissions
        {
            ProjectId = 123,
            AccessLevel = AccessLevel.Guest,
            CanReadProject = true,
            CanReadRepository = true,
            CanReadCiCd = false,
            CanWriteRepository = false,
            CanManageCiCd = false
        };

        // Act
        var missing = permissions.GetMissingPermissions(RequiredPermissions.ReadCiCd);

        // Assert
        missing.Should().HaveCount(1);
        missing.Should().Contain("Read CI/CD");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void ProjectPermissions_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var permissions = new ProjectPermissions();

        // Assert
        permissions.ProjectId.Should().Be(0);
        permissions.AccessLevel.Should().Be(AccessLevel.NoAccess);
        permissions.CanReadProject.Should().BeFalse();
        permissions.CanReadRepository.Should().BeFalse();
        permissions.CanReadCiCd.Should().BeFalse();
        permissions.CanWriteRepository.Should().BeFalse();
        permissions.CanManageCiCd.Should().BeFalse();
        permissions.CanReadIssues.Should().BeFalse();
        permissions.CanReadMergeRequests.Should().BeFalse();
        permissions.CanReadWiki.Should().BeFalse();
        permissions.CanReadSnippets.Should().BeFalse();
        permissions.CanReadArtifacts.Should().BeFalse();
    }

    [Fact]
    public void ProjectPermissions_SetProperties_ShouldRetainValues()
    {
        // Arrange & Act
        var permissions = new ProjectPermissions
        {
            ProjectId = 456,
            AccessLevel = AccessLevel.Maintainer,
            CanReadProject = true,
            CanReadRepository = true,
            CanReadCiCd = true,
            CanWriteRepository = true,
            CanManageCiCd = true,
            CanReadIssues = true,
            CanReadMergeRequests = true,
            CanReadWiki = true,
            CanReadSnippets = true,
            CanReadArtifacts = true
        };

        // Assert
        permissions.ProjectId.Should().Be(456);
        permissions.AccessLevel.Should().Be(AccessLevel.Maintainer);
        permissions.CanReadProject.Should().BeTrue();
        permissions.CanReadRepository.Should().BeTrue();
        permissions.CanReadCiCd.Should().BeTrue();
        permissions.CanWriteRepository.Should().BeTrue();
        permissions.CanManageCiCd.Should().BeTrue();
        permissions.CanReadIssues.Should().BeTrue();
        permissions.CanReadMergeRequests.Should().BeTrue();
        permissions.CanReadWiki.Should().BeTrue();
        permissions.CanReadSnippets.Should().BeTrue();
        permissions.CanReadArtifacts.Should().BeTrue();
    }

    #endregion
}