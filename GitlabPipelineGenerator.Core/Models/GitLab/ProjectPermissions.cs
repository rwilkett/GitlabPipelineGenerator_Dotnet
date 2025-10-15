namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Represents user permissions for a GitLab project
/// </summary>
public class ProjectPermissions
{
    /// <summary>
    /// Project ID these permissions apply to
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// User's access level in the project
    /// </summary>
    public AccessLevel AccessLevel { get; set; }

    /// <summary>
    /// Can read project information
    /// </summary>
    public bool CanReadProject { get; set; }

    /// <summary>
    /// Can read repository content
    /// </summary>
    public bool CanReadRepository { get; set; }

    /// <summary>
    /// Can read CI/CD configuration and pipelines
    /// </summary>
    public bool CanReadCiCd { get; set; }

    /// <summary>
    /// Can write to repository
    /// </summary>
    public bool CanWriteRepository { get; set; }

    /// <summary>
    /// Can manage CI/CD settings
    /// </summary>
    public bool CanManageCiCd { get; set; }

    /// <summary>
    /// Can read project issues
    /// </summary>
    public bool CanReadIssues { get; set; }

    /// <summary>
    /// Can read merge requests
    /// </summary>
    public bool CanReadMergeRequests { get; set; }

    /// <summary>
    /// Can read project wiki
    /// </summary>
    public bool CanReadWiki { get; set; }

    /// <summary>
    /// Can read project snippets
    /// </summary>
    public bool CanReadSnippets { get; set; }

    /// <summary>
    /// Can download project artifacts
    /// </summary>
    public bool CanReadArtifacts { get; set; }

    /// <summary>
    /// Checks if user has all the specified required permissions
    /// </summary>
    /// <param name="requiredPermissions">Required permissions to check</param>
    /// <returns>True if user has all required permissions</returns>
    public bool HasPermissions(RequiredPermissions requiredPermissions)
    {
        if (requiredPermissions.HasFlag(RequiredPermissions.ReadProject) && !CanReadProject)
            return false;

        if (requiredPermissions.HasFlag(RequiredPermissions.ReadRepository) && !CanReadRepository)
            return false;

        if (requiredPermissions.HasFlag(RequiredPermissions.ReadCiCd) && !CanReadCiCd)
            return false;

        return true;
    }

    /// <summary>
    /// Gets a list of missing permissions from the required set
    /// </summary>
    /// <param name="requiredPermissions">Required permissions to check</param>
    /// <returns>List of missing permission names</returns>
    public List<string> GetMissingPermissions(RequiredPermissions requiredPermissions)
    {
        var missing = new List<string>();

        if (requiredPermissions.HasFlag(RequiredPermissions.ReadProject) && !CanReadProject)
            missing.Add("Read Project");

        if (requiredPermissions.HasFlag(RequiredPermissions.ReadRepository) && !CanReadRepository)
            missing.Add("Read Repository");

        if (requiredPermissions.HasFlag(RequiredPermissions.ReadCiCd) && !CanReadCiCd)
            missing.Add("Read CI/CD");

        return missing;
    }
}