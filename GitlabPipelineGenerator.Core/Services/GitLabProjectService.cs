using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitLabApiClient;
using GitLabApiClient.Models.Projects.Requests;
using GitLabApiClient.Models.Projects.Responses;
using Microsoft.Extensions.Logging;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Service for GitLab project discovery and management operations
/// </summary>
public class GitLabProjectService : IGitLabProjectService
{
    private readonly IGitLabAuthenticationService _authService;
    private readonly ILogger<GitLabProjectService> _logger;
    private readonly ResilientGitLabService _resilientService;
    private GitLabClient? _client;

    public GitLabProjectService(
        IGitLabAuthenticationService authService,
        ILogger<GitLabProjectService> logger,
        IGitLabApiErrorHandler errorHandler)
    {
        _authService = authService;
        _logger = logger;
        _resilientService = new ResilientGitLabService(errorHandler);
    }

    /// <inheritdoc />
    public async Task<GitLabProject> GetProjectAsync(string projectIdOrPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectIdOrPath))
        {
            throw new ArgumentException("Project ID or path cannot be null or empty", nameof(projectIdOrPath));
        }

        return await _resilientService.ExecuteAsync(async ct =>
        {
            var client = await GetAuthenticatedClientAsync();

            try
            {
                _logger.LogDebug("Retrieving project: {ProjectIdentifier}", projectIdOrPath);

                Project project;
                
                // Try to parse as numeric ID first
                if (int.TryParse(projectIdOrPath, out var projectId))
                {
                    project = await client.Projects.GetAsync(projectId.ToString());
                }
                else
                {
                    // Treat as project path (namespace/project)
                    var encodedPath = Uri.EscapeDataString(projectIdOrPath);
                    project = await client.Projects.GetAsync(encodedPath);
                }

                var result = MapToGitLabProject(project);
                _logger.LogDebug("Successfully retrieved project: {ProjectName} (ID: {ProjectId})", result.Name, result.Id);
                
                return result;
            }
            catch (GitLabApiClient.GitLabException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Project not found: {ProjectIdentifier}", projectIdOrPath);
                throw new ProjectNotFoundException(projectIdOrPath, $"Project '{projectIdOrPath}' was not found or you don't have access to it");
            }
            catch (GitLabApiClient.GitLabException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Access denied to project: {ProjectIdentifier}", projectIdOrPath);
                throw new ProjectNotFoundException(projectIdOrPath, $"Access denied to project '{projectIdOrPath}'. You may not have sufficient permissions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve project: {ProjectIdentifier}", projectIdOrPath);
                throw new GitLabApiException($"Failed to retrieve project '{projectIdOrPath}': {ex.Message}", ex);
            }
        }, RetryPolicy.Default, TimeSpan.FromSeconds(30), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GitLabProject>> ListProjectsAsync(ProjectListOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var client = await GetAuthenticatedClientAsync();

        try
        {
            _logger.LogDebug("Listing projects with options: Owned={Owned}, Member={Member}, Visibility={Visibility}", 
                options.OwnedOnly, options.MemberOnly, options.Visibility);

            // Simplified implementation - get all projects and filter in memory
            var projects = await client.Projects.GetAsync();
            var results = projects.Select(MapToGitLabProject).ToList();

            // Apply MaxResults limit if specified
            if (options.MaxResults > 0 && results.Count > options.MaxResults)
            {
                results = results.Take(options.MaxResults).ToList();
            }

            _logger.LogDebug("Retrieved {Count} projects", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list projects");
            throw new GitLabApiException($"Failed to list projects: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GitLabProject>> SearchProjectsAsync(string searchTerm, ProjectListOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
        }

        // Use provided options or create default ones
        var searchOptions = options ?? new ProjectListOptions();
        searchOptions.Search = searchTerm;

        _logger.LogDebug("Searching projects with term: {SearchTerm}", searchTerm);

        var results = await ListProjectsAsync(searchOptions, cancellationToken);
        
        // Apply relevance scoring - projects with search term in name get higher priority
        var scoredResults = results.Select(project => new
        {
            Project = project,
            Score = CalculateRelevanceScore(project, searchTerm)
        })
        .OrderByDescending(x => x.Score)
        .Select(x => x.Project);

        _logger.LogDebug("Found {Count} projects matching search term: {SearchTerm}", results.Count(), searchTerm);
        return scoredResults;
    }

    /// <inheritdoc />
    public async Task<ProjectPermissions> GetProjectPermissionsAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var client = await GetAuthenticatedClientAsync();

        try
        {
            _logger.LogDebug("Getting permissions for project: {ProjectId}", projectId);

            // Get project details which include permissions
            var project = await client.Projects.GetAsync(projectId.ToString());
            
            var permissions = new ProjectPermissions
            {
                ProjectId = projectId,
                AccessLevel = MapFromGitLabAccessLevel(project.Permissions?.ProjectAccess?.AccessLevel ?? 0),
                CanReadProject = true, // If we can get the project, we can read it
                CanReadRepository = HasRepositoryAccess(project.Permissions?.ProjectAccess?.AccessLevel ?? 0),
                CanReadCiCd = HasCiCdAccess(project.Permissions?.ProjectAccess?.AccessLevel ?? 0),
                CanWriteRepository = HasWriteAccess(project.Permissions?.ProjectAccess?.AccessLevel ?? 0),
                CanManageCiCd = HasManageAccess(project.Permissions?.ProjectAccess?.AccessLevel ?? 0),
                CanReadIssues = project.IssuesEnabled,
                CanReadMergeRequests = project.MergeRequestsEnabled,
                CanReadWiki = project.WikiEnabled,
                CanReadSnippets = project.SnippetsEnabled,
                CanReadArtifacts = HasArtifactAccess(project.Permissions?.ProjectAccess?.AccessLevel ?? 0)
            };

            _logger.LogDebug("Retrieved permissions for project {ProjectId}: AccessLevel={AccessLevel}", 
                projectId, permissions.AccessLevel);

            return permissions;
        }
        catch (GitLabApiClient.GitLabException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Project not found when getting permissions: {ProjectId}", projectId);
            throw new ProjectNotFoundException(projectId.ToString(), $"Project with ID '{projectId}' was not found or you don't have access to it");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permissions for project: {ProjectId}", projectId);
            throw new GitLabApiException($"Failed to get permissions for project '{projectId}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasSufficientPermissionsAsync(int projectId, RequiredPermissions requiredPermissions, CancellationToken cancellationToken = default)
    {
        try
        {
            var permissions = await GetProjectPermissionsAsync(projectId, cancellationToken);
            var hasPermissions = permissions.HasPermissions(requiredPermissions);

            if (!hasPermissions)
            {
                var missing = permissions.GetMissingPermissions(requiredPermissions);
                _logger.LogWarning("Insufficient permissions for project {ProjectId}. Missing: {MissingPermissions}", 
                    projectId, string.Join(", ", missing));
            }

            return hasPermissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check permissions for project: {ProjectId}", projectId);
            return false;
        }
    }

    #region Private Methods

    /// <summary>
    /// Gets an authenticated GitLab client
    /// </summary>
    private async Task<GitLabClient> GetAuthenticatedClientAsync()
    {
        if (_client != null)
        {
            return _client;
        }

        // Try to load stored credentials
        var storedOptions = _authService.LoadStoredCredentials();
        if (storedOptions != null)
        {
            _client = await _authService.AuthenticateAsync(storedOptions);
            return _client;
        }

        throw new InvalidOperationException("No authenticated GitLab client available. Please authenticate first using the authentication service.");
    }

    /// <summary>
    /// Maps GitLab API project to our domain model
    /// </summary>
    private static GitLabProject MapToGitLabProject(Project project)
    {
        return new GitLabProject
        {
            Id = project.Id,
            Name = project.Name,
            Path = project.Path,
            FullPath = project.PathWithNamespace,
            Description = project.Description,
            DefaultBranch = project.DefaultBranch ?? "main",
            LastActivityAt = DateTime.TryParse(project.LastActivityAt?.ToString(), out var lastActivity) ? lastActivity : DateTime.UtcNow,
            Visibility = MapFromGitLabVisibility(project.Visibility),
            WebUrl = project.WebUrl,
            SshUrl = project.SshUrlToRepo,
            HttpUrl = project.HttpUrlToRepo,
            Namespace = project.Namespace != null ? new GitLabNamespace
            {
                Id = project.Namespace.Id,
                Name = project.Namespace.Name,
                Path = project.Namespace.Path,
                Kind = project.Namespace.Kind
            } : null
        };
    }

    /// <summary>
    /// Maps our visibility enum to GitLab API visibility
    /// </summary>
    private static ProjectVisibilityLevel MapVisibility(ProjectVisibility visibility)
    {
        return visibility switch
        {
            ProjectVisibility.Private => ProjectVisibilityLevel.Private,
            ProjectVisibility.Internal => ProjectVisibilityLevel.Internal,
            ProjectVisibility.Public => ProjectVisibilityLevel.Public,
            _ => ProjectVisibilityLevel.Private
        };
    }

    /// <summary>
    /// Maps GitLab API visibility to our enum
    /// </summary>
    private static ProjectVisibility MapFromGitLabVisibility(ProjectVisibilityLevel visibility)
    {
        return visibility switch
        {
            ProjectVisibilityLevel.Public => ProjectVisibility.Public,
            ProjectVisibilityLevel.Internal => ProjectVisibility.Internal,
            ProjectVisibilityLevel.Private => ProjectVisibility.Private,
            _ => ProjectVisibility.Private
        };
    }

    /// <summary>
    /// Maps our access level enum to GitLab API access level
    /// </summary>
    private static GitLabApiClient.Models.AccessLevel MapAccessLevel(AccessLevel accessLevel)
    {
        return accessLevel switch
        {
            AccessLevel.Guest => GitLabApiClient.Models.AccessLevel.Guest,
            AccessLevel.Reporter => GitLabApiClient.Models.AccessLevel.Reporter,
            AccessLevel.Developer => GitLabApiClient.Models.AccessLevel.Developer,
            AccessLevel.Maintainer => GitLabApiClient.Models.AccessLevel.Maintainer,
            AccessLevel.Owner => GitLabApiClient.Models.AccessLevel.Owner,
            _ => GitLabApiClient.Models.AccessLevel.Guest
        };
    }

    /// <summary>
    /// Maps GitLab API access level to our enum
    /// </summary>
    private static AccessLevel MapFromGitLabAccessLevel(int accessLevel)
    {
        return accessLevel switch
        {
            >= 50 => AccessLevel.Owner,
            >= 40 => AccessLevel.Maintainer,
            >= 30 => AccessLevel.Developer,
            >= 20 => AccessLevel.Reporter,
            >= 10 => AccessLevel.Guest,
            >= 5 => AccessLevel.MinimalAccess,
            _ => AccessLevel.NoAccess
        };
    }

    /// <summary>
    /// Maps our order by enum to GitLab API order by
    /// </summary>
    private static string MapOrderBy(ProjectOrderBy orderBy)
    {
        return orderBy switch
        {
            ProjectOrderBy.Id => "id",
            ProjectOrderBy.Name => "name",
            ProjectOrderBy.CreatedAt => "created_at",
            ProjectOrderBy.LastActivity => "last_activity_at",
            ProjectOrderBy.UpdatedAt => "updated_at",
            _ => "last_activity_at"
        };
    }

    /// <summary>
    /// Calculates relevance score for search results
    /// </summary>
    private static int CalculateRelevanceScore(GitLabProject project, string searchTerm)
    {
        var score = 0;
        var lowerSearchTerm = searchTerm.ToLowerInvariant();

        // Exact name match gets highest score
        if (project.Name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
        {
            score += 100;
        }
        // Name starts with search term
        else if (project.Name.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase))
        {
            score += 80;
        }
        // Name contains search term
        else if (project.Name.Contains(lowerSearchTerm, StringComparison.OrdinalIgnoreCase))
        {
            score += 60;
        }

        // Path matches
        if (project.Path.Contains(lowerSearchTerm, StringComparison.OrdinalIgnoreCase))
        {
            score += 40;
        }

        // Full path matches
        if (project.FullPath.Contains(lowerSearchTerm, StringComparison.OrdinalIgnoreCase))
        {
            score += 30;
        }

        // Description matches
        if (!string.IsNullOrEmpty(project.Description) && 
            project.Description.Contains(lowerSearchTerm, StringComparison.OrdinalIgnoreCase))
        {
            score += 20;
        }

        // Recent activity bonus
        if (project.LastActivityAt > DateTime.UtcNow.AddDays(-30))
        {
            score += 10;
        }

        return score;
    }

    /// <summary>
    /// Checks if access level allows repository access
    /// </summary>
    private static bool HasRepositoryAccess(int accessLevel) => accessLevel >= 20; // Reporter and above

    /// <summary>
    /// Checks if access level allows CI/CD access
    /// </summary>
    private static bool HasCiCdAccess(int accessLevel) => accessLevel >= 20; // Reporter and above

    /// <summary>
    /// Checks if access level allows write access
    /// </summary>
    private static bool HasWriteAccess(int accessLevel) => accessLevel >= 30; // Developer and above

    /// <summary>
    /// Checks if access level allows management access
    /// </summary>
    private static bool HasManageAccess(int accessLevel) => accessLevel >= 40; // Maintainer and above

    /// <summary>
    /// Checks if access level allows artifact access
    /// </summary>
    private static bool HasArtifactAccess(int accessLevel) => accessLevel >= 20; // Reporter and above

    #endregion
}