using System.Text.Json.Serialization;

namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Represents the result of configuration validation
/// </summary>
public class ConfigurationValidationResult
{
    /// <summary>
    /// Whether the configuration is valid
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
    /// List of validation information messages
    /// </summary>
    public List<string> Information { get; set; } = new();

    /// <summary>
    /// Configuration items that were validated
    /// </summary>
    public List<string> ValidatedItems { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ConfigurationValidationResult Success()
    {
        return new ConfigurationValidationResult { IsValid = true };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static ConfigurationValidationResult Failure(params string[] errors)
    {
        return new ConfigurationValidationResult
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }
}

/// <summary>
/// Represents the result of configuration migration
/// </summary>
public class ConfigurationMigrationResult
{
    /// <summary>
    /// Whether the migration was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Version migrated from
    /// </summary>
    public int FromVersion { get; set; }

    /// <summary>
    /// Version migrated to
    /// </summary>
    public int ToVersion { get; set; }

    /// <summary>
    /// List of migration steps performed
    /// </summary>
    public List<string> MigrationSteps { get; set; } = new();

    /// <summary>
    /// List of migration errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of migration warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Whether a backup was created
    /// </summary>
    public bool BackupCreated { get; set; }

    /// <summary>
    /// Path to the backup file (if created)
    /// </summary>
    public string? BackupPath { get; set; }
}

/// <summary>
/// Represents configuration settings
/// </summary>
public class ConfigurationSettings
{
    /// <summary>
    /// Global default timeout for API calls
    /// </summary>
    [JsonPropertyName("defaultTimeoutSeconds")]
    public int DefaultTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Global default retry attempts
    /// </summary>
    [JsonPropertyName("defaultMaxRetryAttempts")]
    public int DefaultMaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Whether to automatically store credentials
    /// </summary>
    [JsonPropertyName("autoStoreCredentials")]
    public bool AutoStoreCredentials { get; set; } = true;

    /// <summary>
    /// Whether to validate SSL certificates
    /// </summary>
    [JsonPropertyName("validateSslCertificates")]
    public bool ValidateSslCertificates { get; set; } = true;

    /// <summary>
    /// Default analysis options
    /// </summary>
    [JsonPropertyName("defaultAnalysisOptions")]
    public AnalysisOptions DefaultAnalysisOptions { get; set; } = new();

    /// <summary>
    /// Logging configuration
    /// </summary>
    [JsonPropertyName("logging")]
    public LoggingConfiguration Logging { get; set; } = new();

    /// <summary>
    /// Cache configuration
    /// </summary>
    [JsonPropertyName("cache")]
    public CacheSettings Cache { get; set; } = new();

    /// <summary>
    /// Security settings
    /// </summary>
    [JsonPropertyName("security")]
    public SecuritySettings Security { get; set; } = new();

    /// <summary>
    /// Feature flags
    /// </summary>
    [JsonPropertyName("features")]
    public Dictionary<string, bool> Features { get; set; } = new();

    /// <summary>
    /// Custom settings
    /// </summary>
    [JsonPropertyName("custom")]
    public Dictionary<string, object> Custom { get; set; } = new();

    /// <summary>
    /// Configuration version
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Logging configuration settings
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// Minimum log level
    /// </summary>
    [JsonPropertyName("minLevel")]
    public string MinLevel { get; set; } = "Information";

    /// <summary>
    /// Whether to log to console
    /// </summary>
    [JsonPropertyName("logToConsole")]
    public bool LogToConsole { get; set; } = true;

    /// <summary>
    /// Whether to log to file
    /// </summary>
    [JsonPropertyName("logToFile")]
    public bool LogToFile { get; set; } = false;

    /// <summary>
    /// Log file path
    /// </summary>
    [JsonPropertyName("logFilePath")]
    public string? LogFilePath { get; set; }

    /// <summary>
    /// Whether to include sensitive data in logs
    /// </summary>
    [JsonPropertyName("includeSensitiveData")]
    public bool IncludeSensitiveData { get; set; } = false;
}

/// <summary>
/// Cache settings
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// Whether caching is enabled
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cache expiration time in minutes
    /// </summary>
    [JsonPropertyName("expirationMinutes")]
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum cache size in MB
    /// </summary>
    [JsonPropertyName("maxSizeMB")]
    public int MaxSizeMB { get; set; } = 100;

    /// <summary>
    /// Cache directory path
    /// </summary>
    [JsonPropertyName("cachePath")]
    public string? CachePath { get; set; }
}

/// <summary>
/// Security settings
/// </summary>
public class SecuritySettings
{
    /// <summary>
    /// Whether to encrypt stored credentials
    /// </summary>
    [JsonPropertyName("encryptCredentials")]
    public bool EncryptCredentials { get; set; } = true;

    /// <summary>
    /// Whether to require HTTPS for API calls
    /// </summary>
    [JsonPropertyName("requireHttps")]
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// Allowed GitLab instance domains
    /// </summary>
    [JsonPropertyName("allowedDomains")]
    public List<string> AllowedDomains { get; set; } = new();

    /// <summary>
    /// Whether to validate API certificates
    /// </summary>
    [JsonPropertyName("validateCertificates")]
    public bool ValidateCertificates { get; set; } = true;
}

/// <summary>
/// Exportable configuration data
/// </summary>
public class ExportableConfiguration
{
    /// <summary>
    /// Configuration settings
    /// </summary>
    [JsonPropertyName("settings")]
    public ConfigurationSettings Settings { get; set; } = new();

    /// <summary>
    /// Exportable profiles
    /// </summary>
    [JsonPropertyName("profiles")]
    public List<ExportableProfile> Profiles { get; set; } = new();

    /// <summary>
    /// Export metadata
    /// </summary>
    [JsonPropertyName("metadata")]
    public ExportMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Export metadata
/// </summary>
public class ExportMetadata
{
    /// <summary>
    /// Export timestamp
    /// </summary>
    [JsonPropertyName("exportedAt")]
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Export version
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Application version that created the export
    /// </summary>
    [JsonPropertyName("applicationVersion")]
    public string? ApplicationVersion { get; set; }

    /// <summary>
    /// Export description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Configuration import result
/// </summary>
public class ConfigurationImportResult
{
    /// <summary>
    /// Whether the import was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of profiles imported
    /// </summary>
    public int ProfilesImported { get; set; }

    /// <summary>
    /// Number of profiles skipped
    /// </summary>
    public int ProfilesSkipped { get; set; }

    /// <summary>
    /// Whether settings were imported
    /// </summary>
    public bool SettingsImported { get; set; }

    /// <summary>
    /// List of import errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of import warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// List of imported items
    /// </summary>
    public List<string> ImportedItems { get; set; } = new();
}

/// <summary>
/// Schema validation result
/// </summary>
public class SchemaValidationResult
{
    /// <summary>
    /// Whether the schema is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of schema errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Schema version detected
    /// </summary>
    public int? DetectedVersion { get; set; }

    /// <summary>
    /// Expected schema version
    /// </summary>
    public int ExpectedVersion { get; set; } = 1;
}

/// <summary>
/// Configuration health information
/// </summary>
public class ConfigurationHealth
{
    /// <summary>
    /// Overall health status
    /// </summary>
    public HealthStatus Status { get; set; } = HealthStatus.Unknown;

    /// <summary>
    /// Number of profiles configured
    /// </summary>
    public int ProfileCount { get; set; }

    /// <summary>
    /// Number of profiles with stored credentials
    /// </summary>
    public int ProfilesWithCredentials { get; set; }

    /// <summary>
    /// Whether a default profile is set
    /// </summary>
    public bool HasDefaultProfile { get; set; }

    /// <summary>
    /// Configuration version
    /// </summary>
    public int ConfigurationVersion { get; set; }

    /// <summary>
    /// Latest available configuration version
    /// </summary>
    public int LatestVersion { get; set; }

    /// <summary>
    /// Whether credential storage is available
    /// </summary>
    public bool CredentialStorageAvailable { get; set; }

    /// <summary>
    /// Health check timestamp
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Health issues found
    /// </summary>
    public List<string> Issues { get; set; } = new();

    /// <summary>
    /// Health recommendations
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Health status enumeration
/// </summary>
public enum HealthStatus
{
    Unknown,
    Healthy,
    Warning,
    Error
}