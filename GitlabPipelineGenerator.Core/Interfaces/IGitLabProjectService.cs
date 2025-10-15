using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Service for GitLab project discovery and management operations
/// </summary>
public interface IGitLabProjectService
{
    /// <summary>
    /// Retrieves a specific project by ID or path
    /// </summary>
    /// <param name="projectIdOrPath">Project ID (numeric) or full path (namespace/project)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>GitLab project information</returns>
    /// <exception cref="ProjectNotFoundException">When project is not found or not accessible</exception>
    /// <exception cref="GitLabApiException">When API operation fails</exception>
    Task<GitLabProject> GetProjectAsync(string projectIdOrPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists accessible projects with optional filtering and pagination
    /// </summary>
    /// <param name="options">Project listing options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of accessible projects</returns>
    /// <exception cref="GitLabApiException">When API operation fails</exception>
    Task<IEnumerable<GitLabProject>> ListProjectsAsync(ProjectListOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches projects by name, description, or path
    /// </summary>
    /// <param name="searchTerm">Search term to match against project properties</param>
    /// <param name="options">Additional search options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching projects with relevance scoring</returns>
    /// <exception cref="GitLabApiException">When API operation fails</exception>
    Task<IEnumerable<GitLabProject>> SearchProjectsAsync(string searchTerm, ProjectListOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current user's permissions for a specific project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Project permissions information</returns>
    /// <exception cref="ProjectNotFoundException">When project is not found or not accessible</exception>
    /// <exception cref name="GitLabApiException">When API operation fails</exception>
    Task<ProjectPermissions> GetProjectPermissionsAsync(int projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if current user has sufficient permissions for project analysis
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="requiredPermissions">Required permissions for the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has sufficient permissions, false otherwise</returns>
    /// <exception cref="ProjectNotFoundException">When project is not found or not accessible</exception>
    /// <exception cref="GitLabApiException">When API operation fails</exception>
    Task<bool> HasSufficientPermissionsAsync(int projectId, RequiredPermissions requiredPermissions, CancellationToken cancellationToken = default);
}