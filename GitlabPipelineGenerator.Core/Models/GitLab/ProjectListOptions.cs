namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Options for listing and filtering GitLab projects
/// </summary>
public class ProjectListOptions
{
    /// <summary>
    /// Only return projects owned by the authenticated user
    /// </summary>
    public bool OwnedOnly { get; set; } = false;

    /// <summary>
    /// Only return projects where the authenticated user is a member
    /// </summary>
    public bool MemberOnly { get; set; } = true;

    /// <summary>
    /// Filter by project visibility level
    /// </summary>
    public ProjectVisibility? Visibility { get; set; }

    /// <summary>
    /// Search term to filter projects by name, description, or path
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Order results by specified field
    /// </summary>
    public ProjectOrderBy OrderBy { get; set; } = ProjectOrderBy.LastActivity;

    /// <summary>
    /// Sort direction (true for ascending, false for descending)
    /// </summary>
    public bool Ascending { get; set; } = false;

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int MaxResults { get; set; } = 50;

    /// <summary>
    /// Page number for pagination (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PerPage { get; set; } = 20;

    /// <summary>
    /// Include archived projects in results
    /// </summary>
    public bool IncludeArchived { get; set; } = false;

    /// <summary>
    /// Minimum access level required for projects
    /// </summary>
    public AccessLevel? MinAccessLevel { get; set; }

    /// <summary>
    /// Filter projects by programming language
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Filter projects created after this date
    /// </summary>
    public DateTime? CreatedAfter { get; set; }

    /// <summary>
    /// Filter projects created before this date
    /// </summary>
    public DateTime? CreatedBefore { get; set; }

    /// <summary>
    /// Filter projects with activity after this date
    /// </summary>
    public DateTime? LastActivityAfter { get; set; }

    /// <summary>
    /// Filter projects with activity before this date
    /// </summary>
    public DateTime? LastActivityBefore { get; set; }
}