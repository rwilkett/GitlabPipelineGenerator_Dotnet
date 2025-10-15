namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Information about the authenticated GitLab user
/// </summary>
public class GitLabUserInfo
{
    /// <summary>
    /// User ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Avatar URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Web URL to user profile
    /// </summary>
    public string WebUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user is an admin
    /// </summary>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// User state (active, blocked, etc.)
    /// </summary>
    public string State { get; set; } = string.Empty;
}

