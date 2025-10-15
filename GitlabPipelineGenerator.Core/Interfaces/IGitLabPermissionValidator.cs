using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Services;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Interface for validating GitLab project permissions with detailed error reporting
/// </summary>
public interface IGitLabPermissionValidator
{
    /// <summary>
    /// Validates project permissions for analysis operations with detailed error messages
    /// </summary>
    /// <param name="projectId">Project ID to validate</param>
    /// <param name="requiredPermissions">Required permissions for the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with detailed information</returns>
    Task<PermissionValidationResult> ValidateProjectAnalysisPermissionsAsync(
        int projectId, 
        RequiredPermissions requiredPermissions, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates permissions for multiple projects and returns detailed results
    /// </summary>
    /// <param name="projectIds">Project IDs to validate</param>
    /// <param name="requiredPermissions">Required permissions for the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of validation results by project ID</returns>
    Task<Dictionary<int, PermissionValidationResult>> ValidateMultipleProjectsAsync(
        IEnumerable<int> projectIds,
        RequiredPermissions requiredPermissions,
        CancellationToken cancellationToken = default);
}