namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Configuration settings for GitLab API integration
/// </summary>
public class GitLabApiSettings
{
    /// <summary>
    /// Default GitLab instance URL
    /// </summary>
    public string DefaultInstanceUrl { get; set; } = "https://gitlab.com";

    /// <summary>
    /// GitLab API version to use
    /// </summary>
    public string ApiVersion { get; set; } = "v4";

    /// <summary>
    /// Request timeout duration
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of retry attempts for failed requests
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Whether to respect rate limiting with delays
    /// </summary>
    public bool RateLimitRespectful { get; set; } = true;

    /// <summary>
    /// Analysis-specific settings
    /// </summary>
    public AnalysisSettings Analysis { get; set; } = new();
}

/// <summary>
/// Settings specific to project analysis
/// </summary>
public class AnalysisSettings
{
    /// <summary>
    /// Maximum file size to analyze (in bytes)
    /// </summary>
    public long MaxFileSize { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Maximum number of files to analyze per project
    /// </summary>
    public int MaxFilesAnalyzed { get; set; } = 1000;

    /// <summary>
    /// Supported file extensions for analysis
    /// </summary>
    public List<string> SupportedFileExtensions { get; set; } = new()
    {
        ".cs", ".csproj", ".sln",
        ".js", ".ts", ".json",
        ".py", ".txt",
        ".yml", ".yaml",
        ".xml", ".config"
    };

    /// <summary>
    /// Default patterns to exclude from analysis
    /// </summary>
    public List<string> DefaultExcludePatterns { get; set; } = new()
    {
        "node_modules/**",
        "bin/**",
        "obj/**",
        ".git/**",
        "*.log",
        "*.tmp",
        "packages/**",
        "__pycache__/**"
    };

    /// <summary>
    /// Cache duration for analysis results
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(30);
}