using System.Text.Json.Serialization;

namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Represents credential data for secure storage
/// </summary>
public class CredentialData
{
    /// <summary>
    /// Personal access token for GitLab API
    /// </summary>
    [JsonPropertyName("personalAccessToken")]
    public string PersonalAccessToken { get; set; } = string.Empty;

    /// <summary>
    /// GitLab instance URL
    /// </summary>
    [JsonPropertyName("instanceUrl")]
    public string InstanceUrl { get; set; } = "https://gitlab.com";

    /// <summary>
    /// API timeout in seconds
    /// </summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum retry attempts for API calls
    /// </summary>
    [JsonPropertyName("maxRetryAttempts")]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Profile name for this credential set
    /// </summary>
    [JsonPropertyName("profileName")]
    public string? ProfileName { get; set; }

    /// <summary>
    /// Timestamp when the credential was stored
    /// </summary>
    [JsonPropertyName("storedAt")]
    public DateTime StoredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Version of the credential format for migration support
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Converts credential data to GitLab connection options
    /// </summary>
    /// <returns>GitLab connection options</returns>
    public GitLabConnectionOptions ToConnectionOptions()
    {
        return new GitLabConnectionOptions
        {
            PersonalAccessToken = PersonalAccessToken,
            InstanceUrl = InstanceUrl,
            TimeoutSeconds = TimeoutSeconds,
            MaxRetryAttempts = MaxRetryAttempts,
            ProfileName = ProfileName
        };
    }

    /// <summary>
    /// Creates credential data from GitLab connection options
    /// </summary>
    /// <param name="options">GitLab connection options</param>
    /// <returns>Credential data</returns>
    public static CredentialData FromConnectionOptions(GitLabConnectionOptions options)
    {
        return new CredentialData
        {
            PersonalAccessToken = options.PersonalAccessToken ?? string.Empty,
            InstanceUrl = options.InstanceUrl ?? "https://gitlab.com",
            TimeoutSeconds = options.TimeoutSeconds,
            MaxRetryAttempts = options.MaxRetryAttempts,
            ProfileName = options.ProfileName,
            StoredAt = DateTime.UtcNow,
            Version = 1
        };
    }
}