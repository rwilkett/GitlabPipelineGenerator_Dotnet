using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using Microsoft.Extensions.Logging;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Service for validating GitLab project permissions with detailed error reporting
/// </summary>
public class GitLabPermissionValidator : IGitLabPermissionValidator
{
    private readonly IGitLabProjectService _projectService;
    private readonly ILogger<GitLabPermissionValidator> _logger;

    public GitLabPermissionValidator(
        IGitLabProjectService projectService,
        ILogger<GitLabPermissionValidator> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    /// <summary>
    /// Validates project permissions for analysis operations with detailed error messages
    /// </summary>
    /// <param name="projectId">Project ID to validate</param>
    /// <param name="requiredPermissions">Required permissions for the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with detailed information</returns>
    public async Task<PermissionValidationResult> ValidateProjectAnalysisPermissionsAsync(
        int projectId, 
        RequiredPermissions requiredPermissions, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating permissions for project {ProjectId} with requirements: {RequiredPermissions}", 
                projectId, requiredPermissions);

            var permissions = await _projectService.GetProjectPermissionsAsync(projectId, cancellationToken);
            var hasPermissions = permissions.HasPermissions(requiredPermissions);

            if (hasPermissions)
            {
                _logger.LogDebug("Permission validation successful for project {ProjectId}", projectId);
                return PermissionValidationResult.Success();
            }

            var missingPermissions = permissions.GetMissingPermissions(requiredPermissions);
            var errorMessage = GeneratePermissionErrorMessage(projectId, permissions.AccessLevel, missingPermissions, requiredPermissions);
            var suggestions = GeneratePermissionSuggestions(permissions.AccessLevel, missingPermissions);

            _logger.LogWarning("Permission validation failed for project {ProjectId}. Missing: {MissingPermissions}", 
                projectId, string.Join(", ", missingPermissions));

            return PermissionValidationResult.Failure(errorMessage, missingPermissions, suggestions);
        }
        catch (ProjectNotFoundException ex)
        {
            _logger.LogWarning(ex, "Project not found during permission validation: {ProjectId}", projectId);
            return PermissionValidationResult.Failure(
                $"Project with ID '{projectId}' was not found or is not accessible. Please verify the project ID and ensure you have access to it.",
                new List<string> { "Project Access" },
                new List<string> 
                { 
                    "Verify the project ID is correct",
                    "Ensure you have at least Guest access to the project",
                    "Contact the project owner to request access"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during permission validation for project {ProjectId}", projectId);
            return PermissionValidationResult.Failure(
                $"Unable to validate permissions for project '{projectId}': {ex.Message}",
                new List<string> { "Permission Check" },
                new List<string> 
                { 
                    "Check your network connection",
                    "Verify your GitLab authentication token is valid",
                    "Try again in a few moments"
                });
        }
    }

    /// <summary>
    /// Validates permissions for multiple projects and returns detailed results
    /// </summary>
    /// <param name="projectIds">Project IDs to validate</param>
    /// <param name="requiredPermissions">Required permissions for the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of validation results by project ID</returns>
    public async Task<Dictionary<int, PermissionValidationResult>> ValidateMultipleProjectsAsync(
        IEnumerable<int> projectIds,
        RequiredPermissions requiredPermissions,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<int, PermissionValidationResult>();
        var projectIdList = projectIds.ToList();

        _logger.LogDebug("Validating permissions for {Count} projects", projectIdList.Count);

        var tasks = projectIdList.Select(async projectId =>
        {
            var result = await ValidateProjectAnalysisPermissionsAsync(projectId, requiredPermissions, cancellationToken);
            return new { ProjectId = projectId, Result = result };
        });

        var completedTasks = await Task.WhenAll(tasks);

        foreach (var task in completedTasks)
        {
            results[task.ProjectId] = task.Result;
        }

        var successCount = results.Values.Count(r => r.IsValid);
        _logger.LogDebug("Permission validation completed: {SuccessCount}/{TotalCount} projects have sufficient permissions", 
            successCount, projectIdList.Count);

        return results;
    }

    /// <summary>
    /// Gets the minimum required access level for specific permissions
    /// </summary>
    /// <param name="requiredPermissions">Required permissions</param>
    /// <returns>Minimum access level needed</returns>
    public static AccessLevel GetMinimumRequiredAccessLevel(RequiredPermissions requiredPermissions)
    {
        if (requiredPermissions.HasFlag(RequiredPermissions.ReadCiCd) ||
            requiredPermissions.HasFlag(RequiredPermissions.ReadRepository))
        {
            return AccessLevel.Reporter;
        }

        if (requiredPermissions.HasFlag(RequiredPermissions.ReadProject))
        {
            return AccessLevel.Guest;
        }

        return AccessLevel.Guest;
    }

    #region Private Methods

    /// <summary>
    /// Generates a user-friendly error message for permission failures
    /// </summary>
    private static string GeneratePermissionErrorMessage(
        int projectId, 
        AccessLevel currentAccessLevel, 
        List<string> missingPermissions, 
        RequiredPermissions requiredPermissions)
    {
        var minRequiredLevel = GetMinimumRequiredAccessLevel(requiredPermissions);
        
        var message = $"Insufficient permissions for project '{projectId}'. ";
        message += $"Your current access level is '{currentAccessLevel}', but '{minRequiredLevel}' or higher is required. ";
        message += $"Missing permissions: {string.Join(", ", missingPermissions)}.";

        return message;
    }

    /// <summary>
    /// Generates suggestions for resolving permission issues
    /// </summary>
    private static List<string> GeneratePermissionSuggestions(AccessLevel currentAccessLevel, List<string> missingPermissions)
    {
        var suggestions = new List<string>();

        if (currentAccessLevel == AccessLevel.NoAccess)
        {
            suggestions.Add("Request access to the project from the project owner or maintainer");
            suggestions.Add("Verify that the project exists and you have the correct project ID");
        }
        else if (currentAccessLevel < AccessLevel.Reporter)
        {
            suggestions.Add("Request Reporter access or higher from a project maintainer");
            suggestions.Add("Reporter access is required to read repository content and CI/CD configurations");
        }

        if (missingPermissions.Contains("Read Repository"))
        {
            suggestions.Add("Repository read access is required for project analysis");
            suggestions.Add("Ensure the project repository is not disabled or restricted");
        }

        if (missingPermissions.Contains("Read CI/CD"))
        {
            suggestions.Add("CI/CD read access is required to analyze existing pipeline configurations");
            suggestions.Add("Check if CI/CD features are enabled for this project");
        }

        if (missingPermissions.Contains("Read Project"))
        {
            suggestions.Add("Basic project read access is required");
            suggestions.Add("Contact the project owner to verify your access permissions");
        }

        // Add general suggestions
        suggestions.Add("You can still use manual pipeline generation mode without project analysis");
        suggestions.Add("Contact your GitLab administrator if you believe you should have access");

        return suggestions;
    }

    #endregion
}

/// <summary>
/// Result of permission validation with detailed information
/// </summary>
public class PermissionValidationResult
{
    /// <summary>
    /// Whether the validation passed
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// List of missing permissions
    /// </summary>
    public List<string> MissingPermissions { get; private set; } = new();

    /// <summary>
    /// Suggestions for resolving permission issues
    /// </summary>
    public List<string> Suggestions { get; private set; } = new();

    private PermissionValidationResult() { }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static PermissionValidationResult Success()
    {
        return new PermissionValidationResult
        {
            IsValid = true
        };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="missingPermissions">Missing permissions</param>
    /// <param name="suggestions">Suggestions for resolution</param>
    public static PermissionValidationResult Failure(
        string errorMessage, 
        List<string> missingPermissions, 
        List<string> suggestions)
    {
        return new PermissionValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            MissingPermissions = missingPermissions,
            Suggestions = suggestions
        };
    }
}