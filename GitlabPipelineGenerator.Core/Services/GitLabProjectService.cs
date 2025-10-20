using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.GitLabApiClient;
using GitlabPipelineGenerator.GitLabApiClient.Models;
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
    private GitlabPipelineGenerator.GitLabApiClient.GitLabClient? _client;

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

                project = await client.GetProjectAsync(projectIdOrPath);

                var result = MapToGitLabProject(project);
                _logger.LogDebug("Successfully retrieved project: {ProjectName} (ID: {ProjectId})", result.Name, result.Id);

                return result;
            }
            catch (GitlabPipelineGenerator.GitLabApiClient.GitLabApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Project not found: {ProjectIdentifier}", projectIdOrPath);
                throw new ProjectNotFoundException(projectIdOrPath, $"Project '{projectIdOrPath}' was not found or you don't have access to it");
            }
            catch (GitlabPipelineGenerator.GitLabApiClient.GitLabApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Access denied to project: {ProjectIdentifier}", projectIdOrPath);
                throw new ProjectNotFoundException(projectIdOrPath, $"Access denied to project '{projectIdOrPath}'. You may not have sufficient permissions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve project: {ProjectIdentifier}", projectIdOrPath);
                throw new Core.Exceptions.GitLabApiException($"Failed to retrieve project '{projectIdOrPath}': {ex.Message}", ex);
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

            // Get projects with basic filtering
            var projects = await client.GetProjectsAsync(owned: options.OwnedOnly, perPage: options.MaxResults > 0 ? options.MaxResults : 100);
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
            throw new Core.Exceptions.GitLabApiException($"Failed to list projects: {ex.Message}", ex);
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
    public async Task<Core.Models.GitLab.ProjectPermissions> GetProjectPermissionsAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var client = await GetAuthenticatedClientAsync();

        try
        {
            _logger.LogDebug("Getting permissions for project: {ProjectId}", projectId);

            // Get project details which include permissions
            var project = await client.GetProjectAsync(projectId.ToString());

            // Get access level from ProjectAccess
            var accessLevel = project.Permissions?.ProjectAccess?.AccessLevel ?? 0;

            var permissions = new Core.Models.GitLab.ProjectPermissions
            {
                ProjectId = projectId,
                AccessLevel = MapFromGitLabAccessLevel(accessLevel),
                CanReadProject = true, // If we can get the project, we can read it
                CanReadRepository = HasRepositoryAccess(accessLevel),
                CanReadCiCd = HasCiCdAccess(accessLevel),
                CanWriteRepository = HasWriteAccess(accessLevel),
                CanManageCiCd = HasManageAccess(accessLevel),
                CanReadIssues = project.IssuesEnabled,
                CanReadMergeRequests = project.MergeRequestsEnabled,
                CanReadWiki = project.WikiEnabled,
                CanReadSnippets = project.SnippetsEnabled,
                CanReadArtifacts = HasArtifactAccess(accessLevel)
            };

            _logger.LogDebug("Retrieved permissions for project {ProjectId}: AccessLevel={AccessLevel}",
                projectId, permissions.AccessLevel);

            return permissions;
        }
        catch (GitlabPipelineGenerator.GitLabApiClient.GitLabApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Project not found when getting permissions: {ProjectId}", projectId);
            throw new ProjectNotFoundException(projectId.ToString(), $"Project with ID '{projectId}' was not found or you don't have access to it");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permissions for project: {ProjectId}", projectId);
            throw new Core.Exceptions.GitLabApiException($"Failed to get permissions for project '{projectId}': {ex.Message}", ex);
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
    private async Task<GitlabPipelineGenerator.GitLabApiClient.GitLabClient> GetAuthenticatedClientAsync()
    {
        if (_client != null)
        {
            return _client;
        }

        // Try to load stored credentials and authenticate
        var storedCredentials = _authService.LoadStoredCredentials();
        if (storedCredentials != null)
        {
            _client = await _authService.AuthenticateAsync(storedCredentials);
            return _client;
        }

        throw new InvalidOperationException("No authenticated GitLab client available. Please authenticate first using the authentication service.");
    }

    /// <summary>
    /// Sets the authenticated client (called by the authentication service)
    /// </summary>
    public void SetAuthenticatedClient(GitlabPipelineGenerator.GitLabApiClient.GitLabClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
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
    private static string MapVisibility(ProjectVisibility visibility)
    {
        return visibility switch
        {
            ProjectVisibility.Private => "private",
            ProjectVisibility.Internal => "internal",
            ProjectVisibility.Public => "public",
            _ => "private"
        };
    }

    /// <summary>
    /// Maps GitLab API visibility to our enum
    /// </summary>
    private static ProjectVisibility MapFromGitLabVisibility(string visibility)
    {
        return visibility switch
        {
            "public" => ProjectVisibility.Public,
            "internal" => ProjectVisibility.Internal,
            "private" => ProjectVisibility.Private,
            _ => ProjectVisibility.Private
        };
    }

    /// <summary>
    /// Maps our access level enum to GitLab API access level
    /// </summary>
    private static int MapAccessLevel(AccessLevel accessLevel)
    {
        return accessLevel switch
        {
            AccessLevel.Guest => 10,
            AccessLevel.Reporter => 20,
            AccessLevel.Developer => 30,
            AccessLevel.Maintainer => 40,
            AccessLevel.Owner => 50,
            _ => 10
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

    /// <inheritdoc />
    public async Task<IEnumerable<GitLabRepositoryFile>> GetRepositoryFilesAsync(int projectId, string path = "", bool recursive = false, int maxDepth = 3, CancellationToken cancellationToken = default)
    {
        var client = await GetAuthenticatedClientAsync();

        try
        {
            _logger.LogDebug("Getting repository files for project {ProjectId}, path: {Path}, recursive: {Recursive}, maxDepth: {MaxDepth}", projectId, path, recursive, maxDepth);

            var allFiles = new List<GitLabRepositoryFile>();
            await GetFilesRecursivelyAsync(client, projectId.ToString(), path, allFiles, recursive, maxDepth);

            _logger.LogDebug("Retrieved {Count} files from repository", allFiles.Count);
            return allFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get repository files for project: {ProjectId}", projectId);
            throw new Core.Exceptions.GitLabApiException($"Failed to get repository files for project '{projectId}': {ex.Message}", ex);
        }
    }

    private async Task GetFilesRecursivelyAsync(GitlabPipelineGenerator.GitLabApiClient.GitLabClient client, string projectId, string path, List<GitLabRepositoryFile> allFiles, bool recursive, int maxDepth = 3, int currentDepth = 0)
    {
        var repositoryTree = await client.GetRepositoryTreeAsync(projectId, path);
        
        foreach (var item in repositoryTree)
        {
            if (item.Type == "blob")
            {
                allFiles.Add(new GitLabRepositoryFile
                {
                    Name = item.Name,
                    Path = item.Path,
                    Type = item.Type,
                    Size = 0
                });
            }
            else if (item.Type == "tree" && recursive && currentDepth < maxDepth)
            {
                await GetFilesRecursivelyAsync(client, projectId, item.Path, allFiles, recursive, maxDepth, currentDepth + 1);
            }
        }
    }

    /// <inheritdoc />
    public async Task<string> GetFileContentAsync(int projectId, string filePath, string? branch = null, CancellationToken cancellationToken = default)
    {
        var client = await GetAuthenticatedClientAsync();

        try
        {
            _logger.LogDebug("Getting file content for project {ProjectId}, file: {FilePath}, branch: {Branch}", projectId, filePath, branch);

            var file = await client.GetFileAsync(projectId.ToString(), filePath, branch);
            var content = file.Content;

            _logger.LogDebug("Retrieved content for file {FilePath} ({Size} characters)", filePath, content.Length);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file content for project {ProjectId}, file: {FilePath}", projectId, filePath);
            throw new Core.Exceptions.GitLabApiException($"Failed to get file content for '{filePath}' in project '{projectId}': {ex.Message}", ex);
        }
    }
}