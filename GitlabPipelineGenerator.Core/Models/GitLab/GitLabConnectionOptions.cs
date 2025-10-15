namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Configuration options for connecting to GitLab API
/// </summary>
public class GitLabConnectionOptions
{
    /// <summary>
    /// Personal access token for GitLab API authentication
    /// </summary>
    public string? PersonalAccessToken { get; set; }

    /// <summary>
    /// GitLab instance URL (defaults to gitlab.com)
    /// </summary>
    public string InstanceUrl { get; set; } = "https://gitlab.com";

    /// <summary>
    /// Profile name for storing multiple GitLab configurations
    /// </summary>
    public string? ProfileName { get; set; }

    /// <summary>
    /// Whether to store credentials securely in OS credential store
    /// </summary>
    public bool StoreCredentials { get; set; } = false;

    /// <summary>
    /// API timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retry attempts for API calls
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
}