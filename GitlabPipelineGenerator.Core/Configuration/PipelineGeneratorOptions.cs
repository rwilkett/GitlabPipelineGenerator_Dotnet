using System.ComponentModel.DataAnnotations;

namespace GitlabPipelineGenerator.Core.Configuration;

/// <summary>
/// Configuration options for the pipeline generator
/// </summary>
public sealed class PipelineGeneratorOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "PipelineGenerator";

    /// <summary>
    /// Default timeout for pipeline generation operations
    /// </summary>
    [Range(1, 3600)]
    public int DefaultTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Maximum number of concurrent job generations
    /// </summary>
    [Range(1, 50)]
    public int MaxConcurrentJobs { get; set; } = 10;

    /// <summary>
    /// Enable detailed logging for pipeline generation
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Default cache configuration
    /// </summary>
    public CacheOptions Cache { get; set; } = new();

    /// <summary>
    /// Default artifact configuration
    /// </summary>
    public ArtifactOptions Artifacts { get; set; } = new();

    /// <summary>
    /// Template configuration
    /// </summary>
    public TemplateOptions Templates { get; set; } = new();
}

/// <summary>
/// Cache configuration options
/// </summary>
public sealed class CacheOptions
{
    /// <summary>
    /// Default cache key pattern
    /// </summary>
    public string DefaultKeyPattern { get; set; } = "$CI_COMMIT_REF_SLUG";

    /// <summary>
    /// Default cache policy
    /// </summary>
    public string DefaultPolicy { get; set; } = "pull-push";

    /// <summary>
    /// Enable cache by default
    /// </summary>
    public bool EnableByDefault { get; set; } = true;
}

/// <summary>
/// Artifact configuration options
/// </summary>
public sealed class ArtifactOptions
{
    /// <summary>
    /// Default artifact expiration
    /// </summary>
    public string DefaultExpiration { get; set; } = "1 week";

    /// <summary>
    /// Default artifact paths
    /// </summary>
    public List<string> DefaultPaths { get; set; } = new() { "artifacts/" };

    /// <summary>
    /// Enable artifacts by default
    /// </summary>
    public bool EnableByDefault { get; set; } = true;
}

/// <summary>
/// Template configuration options
/// </summary>
public sealed class TemplateOptions
{
    /// <summary>
    /// Template directory path
    /// </summary>
    public string? TemplateDirectory { get; set; }

    /// <summary>
    /// Enable custom templates
    /// </summary>
    public bool EnableCustomTemplates { get; set; } = true;

    /// <summary>
    /// Template validation mode
    /// </summary>
    public TemplateValidationMode ValidationMode { get; set; } = TemplateValidationMode.Strict;
}

/// <summary>
/// Template validation modes
/// </summary>
public enum TemplateValidationMode
{
    /// <summary>
    /// No validation
    /// </summary>
    None,

    /// <summary>
    /// Basic validation
    /// </summary>
    Basic,

    /// <summary>
    /// Strict validation
    /// </summary>
    Strict
}