using System.Text.Json.Serialization;

namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Represents a GitLab connection configuration profile
/// </summary>
public class ConfigurationProfile
{
    /// <summary>
    /// Name of the configuration profile
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the profile
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Description of the profile
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

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
    /// Default analysis options for this profile
    /// </summary>
    [JsonPropertyName("defaultAnalysisOptions")]
    public AnalysisOptions? DefaultAnalysisOptions { get; set; }

    /// <summary>
    /// Whether this profile is the default profile
    /// </summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether credentials are stored for this profile
    /// </summary>
    [JsonPropertyName("hasStoredCredentials")]
    public bool HasStoredCredentials { get; set; }

    /// <summary>
    /// Timestamp when the profile was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the profile was last modified
    /// </summary>
    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Version of the profile format for migration support
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Additional metadata for the profile
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Converts the profile to GitLab connection options
    /// </summary>
    /// <param name="personalAccessToken">Personal access token to include</param>
    /// <returns>GitLab connection options</returns>
    public GitLabConnectionOptions ToConnectionOptions(string? personalAccessToken = null)
    {
        return new GitLabConnectionOptions
        {
            PersonalAccessToken = personalAccessToken,
            InstanceUrl = InstanceUrl,
            TimeoutSeconds = TimeoutSeconds,
            MaxRetryAttempts = MaxRetryAttempts,
            ProfileName = Name,
            StoreCredentials = HasStoredCredentials
        };
    }

    /// <summary>
    /// Creates a configuration profile from GitLab connection options
    /// </summary>
    /// <param name="options">GitLab connection options</param>
    /// <param name="name">Profile name</param>
    /// <returns>Configuration profile</returns>
    public static ConfigurationProfile FromConnectionOptions(GitLabConnectionOptions options, string name)
    {
        return new ConfigurationProfile
        {
            Name = name,
            InstanceUrl = options.InstanceUrl ?? "https://gitlab.com",
            TimeoutSeconds = options.TimeoutSeconds,
            MaxRetryAttempts = options.MaxRetryAttempts,
            HasStoredCredentials = options.StoreCredentials,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            Version = 1
        };
    }

    /// <summary>
    /// Updates the last modified timestamp
    /// </summary>
    public void Touch()
    {
        LastModified = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents the result of profile validation
/// </summary>
public class ProfileValidationResult
{
    /// <summary>
    /// Whether the profile is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    /// <returns>Valid profile validation result</returns>
    public static ProfileValidationResult Success()
    {
        return new ProfileValidationResult { IsValid = true };
    }

    /// <summary>
    /// Creates a failed validation result with errors
    /// </summary>
    /// <param name="errors">Validation errors</param>
    /// <returns>Invalid profile validation result</returns>
    public static ProfileValidationResult Failure(params string[] errors)
    {
        return new ProfileValidationResult
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }

    /// <summary>
    /// Adds a validation error
    /// </summary>
    /// <param name="error">Error message</param>
    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    /// <summary>
    /// Adds a validation warning
    /// </summary>
    /// <param name="warning">Warning message</param>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}

/// <summary>
/// Represents an exportable configuration profile (without sensitive data)
/// </summary>
public class ExportableProfile
{
    /// <summary>
    /// Display name for the profile
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Description of the profile
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

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
    /// Default analysis options for this profile
    /// </summary>
    [JsonPropertyName("defaultAnalysisOptions")]
    public AnalysisOptions? DefaultAnalysisOptions { get; set; }

    /// <summary>
    /// Version of the profile format
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Additional metadata for the profile
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Creates an exportable profile from a configuration profile
    /// </summary>
    /// <param name="profile">Configuration profile</param>
    /// <returns>Exportable profile</returns>
    public static ExportableProfile FromConfigurationProfile(ConfigurationProfile profile)
    {
        return new ExportableProfile
        {
            DisplayName = profile.DisplayName,
            Description = profile.Description,
            InstanceUrl = profile.InstanceUrl,
            TimeoutSeconds = profile.TimeoutSeconds,
            MaxRetryAttempts = profile.MaxRetryAttempts,
            DefaultAnalysisOptions = profile.DefaultAnalysisOptions,
            Version = profile.Version,
            Metadata = new Dictionary<string, object>(profile.Metadata)
        };
    }

    /// <summary>
    /// Converts the exportable profile to a configuration profile
    /// </summary>
    /// <param name="name">Profile name</param>
    /// <returns>Configuration profile</returns>
    public ConfigurationProfile ToConfigurationProfile(string name)
    {
        return new ConfigurationProfile
        {
            Name = name,
            DisplayName = DisplayName,
            Description = Description,
            InstanceUrl = InstanceUrl,
            TimeoutSeconds = TimeoutSeconds,
            MaxRetryAttempts = MaxRetryAttempts,
            DefaultAnalysisOptions = DefaultAnalysisOptions,
            Version = Version,
            Metadata = new Dictionary<string, object>(Metadata),
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };
    }
}