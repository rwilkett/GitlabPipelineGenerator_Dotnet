namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Represents a GitLab project with essential information
/// </summary>
public class GitLabProject
{
    /// <summary>
    /// Unique project ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Project name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Project path (URL-friendly name)
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Full path including namespace (e.g., "group/project")
    /// </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// Project description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Default branch name
    /// </summary>
    public string DefaultBranch { get; set; } = "main";

    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>
    /// Project visibility level
    /// </summary>
    public ProjectVisibility Visibility { get; set; }

    /// <summary>
    /// Web URL to the project
    /// </summary>
    public string WebUrl { get; set; } = string.Empty;

    /// <summary>
    /// SSH URL for cloning
    /// </summary>
    public string? SshUrl { get; set; }

    /// <summary>
    /// HTTP URL for cloning
    /// </summary>
    public string? HttpUrl { get; set; }

    /// <summary>
    /// Namespace information
    /// </summary>
    public GitLabNamespace? Namespace { get; set; }
}

/// <summary>
/// Represents a GitLab namespace (user or group)
/// </summary>
public class GitLabNamespace
{
    /// <summary>
    /// Namespace ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Namespace name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Namespace path
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Namespace kind (user or group)
    /// </summary>
    public string Kind { get; set; } = string.Empty;
}