using FluentAssertions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

/// <summary>
/// Unit tests for GitLabProjectService - focusing on validation logic and model behavior
/// </summary>
public class GitLabProjectServiceTests
{
    private readonly Mock<IGitLabAuthenticationService> _mockAuthService;
    private readonly Mock<ILogger<GitLabProjectService>> _mockLogger;
    private readonly GitLabProjectService _projectService;

    public GitLabProjectServiceTests()
    {
        _mockAuthService = new Mock<IGitLabAuthenticationService>();
        _mockLogger = new Mock<ILogger<GitLabProjectService>>();
        _projectService = new GitLabProjectService(_mockAuthService.Object, _mockLogger.Object);
    }

    #region GetProjectAsync Validation Tests

    [Fact]
    public async Task GetProjectAsync_WithNullProjectId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _projectService.GetProjectAsync(null!));
        
        exception.Message.Should().Contain("Project ID or path cannot be null or empty");
        exception.ParamName.Should().Be("projectIdOrPath");
    }

    [Fact]
    public async Task GetProjectAsync_WithEmptyProjectId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _projectService.GetProjectAsync(""));
        
        exception.Message.Should().Contain("Project ID or path cannot be null or empty");
        exception.ParamName.Should().Be("projectIdOrPath");
    }

    [Fact]
    public async Task GetProjectAsync_WithWhitespaceProjectId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _projectService.GetProjectAsync("   "));
        
        exception.Message.Should().Contain("Project ID or path cannot be null or empty");
        exception.ParamName.Should().Be("projectIdOrPath");
    }

    [Fact]
    public async Task GetProjectAsync_WithoutStoredCredentials_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthService.Setup(x => x.LoadStoredCredentials()).Returns((GitLabConnectionOptions?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _projectService.GetProjectAsync("123"));
        
        exception.Message.Should().Contain("No authenticated GitLab client available");
    }

    #endregion

    #region ListProjectsAsync Validation Tests

    [Fact]
    public async Task ListProjectsAsync_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _projectService.ListProjectsAsync(null!));
    }

    #endregion

    #region SearchProjectsAsync Validation Tests

    [Fact]
    public async Task SearchProjectsAsync_WithNullSearchTerm_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _projectService.SearchProjectsAsync(null!));
        
        exception.Message.Should().Contain("Search term cannot be null or empty");
        exception.ParamName.Should().Be("searchTerm");
    }

    [Fact]
    public async Task SearchProjectsAsync_WithEmptySearchTerm_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _projectService.SearchProjectsAsync(""));
        
        exception.Message.Should().Contain("Search term cannot be null or empty");
        exception.ParamName.Should().Be("searchTerm");
    }

    [Fact]
    public async Task SearchProjectsAsync_WithWhitespaceSearchTerm_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _projectService.SearchProjectsAsync("   "));
        
        exception.Message.Should().Contain("Search term cannot be null or empty");
        exception.ParamName.Should().Be("searchTerm");
    }

    #endregion

    #region ProjectPermissions Model Tests

    [Fact]
    public void ProjectPermissions_HasPermissions_WithAllRequiredPermissions_ShouldReturnTrue()
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
    public void ProjectPermissions_HasPermissions_WithMissingPermissions_ShouldReturnFalse()
    {
        // Arrange
        var permissions = new ProjectPermissions
        {
            ProjectId = 123,
            AccessLevel = AccessLevel.Guest,
            CanReadProject = true,
            CanReadRepository = false, // Missing this permission
            CanReadCiCd = false,
            CanWriteRepository = false,
            CanManageCiCd = false
        };

        // Act
        var result = permissions.HasPermissions(RequiredPermissions.ReadRepository);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ProjectPermissions_HasPermissions_WithNoPermissions_ShouldReturnFalse()
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
    public void ProjectPermissions_HasPermissions_WithNoneRequired_ShouldReturnTrue()
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
    public void ProjectPermissions_GetMissingPermissions_ShouldReturnCorrectList()
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
    public void ProjectPermissions_GetMissingPermissions_WithAllPermissions_ShouldReturnEmptyList()
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
    public void ProjectPermissions_GetMissingPermissions_WithNoRequiredPermissions_ShouldReturnEmptyList()
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

    #endregion

    #region ProjectListOptions Model Tests

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
    }

    [Fact]
    public void ProjectListOptions_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var options = new ProjectListOptions
        {
            OwnedOnly = true,
            MemberOnly = false,
            Visibility = ProjectVisibility.Public,
            Search = "test-search",
            OrderBy = ProjectOrderBy.Name,
            Ascending = true,
            MaxResults = 100,
            Page = 2,
            PerPage = 50,
            IncludeArchived = true,
            MinAccessLevel = AccessLevel.Developer
        };

        // Assert
        options.OwnedOnly.Should().BeTrue();
        options.MemberOnly.Should().BeFalse();
        options.Visibility.Should().Be(ProjectVisibility.Public);
        options.Search.Should().Be("test-search");
        options.OrderBy.Should().Be(ProjectOrderBy.Name);
        options.Ascending.Should().BeTrue();
        options.MaxResults.Should().Be(100);
        options.Page.Should().Be(2);
        options.PerPage.Should().Be(50);
        options.IncludeArchived.Should().BeTrue();
        options.MinAccessLevel.Should().Be(AccessLevel.Developer);
    }

    #endregion

    #region GitLabProject Model Tests

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

    [Fact]
    public void GitLabProject_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var lastActivity = DateTime.UtcNow;
        var namespace1 = new GitLabNamespace
        {
            Id = 1,
            Name = "test-namespace",
            Path = "test-namespace",
            Kind = "group"
        };

        var project = new GitLabProject
        {
            Id = 123,
            Name = "test-project",
            Path = "test-project",
            FullPath = "test-namespace/test-project",
            Description = "Test project description",
            DefaultBranch = "develop",
            LastActivityAt = lastActivity,
            Visibility = ProjectVisibility.Public,
            WebUrl = "https://gitlab.com/test-namespace/test-project",
            SshUrl = "git@gitlab.com:test-namespace/test-project.git",
            HttpUrl = "https://gitlab.com/test-namespace/test-project.git",
            Namespace = namespace1
        };

        // Assert
        project.Id.Should().Be(123);
        project.Name.Should().Be("test-project");
        project.Path.Should().Be("test-project");
        project.FullPath.Should().Be("test-namespace/test-project");
        project.Description.Should().Be("Test project description");
        project.DefaultBranch.Should().Be("develop");
        project.LastActivityAt.Should().Be(lastActivity);
        project.Visibility.Should().Be(ProjectVisibility.Public);
        project.WebUrl.Should().Be("https://gitlab.com/test-namespace/test-project");
        project.SshUrl.Should().Be("git@gitlab.com:test-namespace/test-project.git");
        project.HttpUrl.Should().Be("https://gitlab.com/test-namespace/test-project.git");
        project.Namespace.Should().Be(namespace1);
    }

    #endregion
}