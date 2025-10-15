namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Cache configuration recommendations
/// </summary>
public class CacheConfiguration
{
    /// <summary>
    /// Cache paths to include
    /// </summary>
    public List<string> CachePaths { get; set; } = new();

    /// <summary>
    /// Cache key pattern
    /// </summary>
    public string CacheKey { get; set; } = string.Empty;

    /// <summary>
    /// Cache policy (per-branch, per-job, etc.)
    /// </summary>
    public CachePolicy Policy { get; set; } = CachePolicy.PerBranch;

    /// <summary>
    /// Whether to use fallback keys
    /// </summary>
    public bool UseFallbackKeys { get; set; } = true;

    /// <summary>
    /// Fallback cache keys
    /// </summary>
    public List<string> FallbackKeys { get; set; } = new();

    /// <summary>
    /// Cache-specific configuration
    /// </summary>
    public Dictionary<string, string> Configuration { get; set; } = new();

    /// <summary>
    /// Estimated cache effectiveness
    /// </summary>
    public CacheEffectiveness Effectiveness { get; set; } = CacheEffectiveness.Medium;
}

/// <summary>
/// Cache recommendation based on dependencies
/// </summary>
public class CacheRecommendation
{
    /// <summary>
    /// Whether caching is recommended
    /// </summary>
    public bool IsRecommended { get; set; }

    /// <summary>
    /// Cache configuration
    /// </summary>
    public CacheConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// Reason for the recommendation
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Estimated build time savings
    /// </summary>
    public TimeSpan EstimatedTimeSavings { get; set; }

    /// <summary>
    /// Convenience property for cache paths
    /// </summary>
    public List<string> CachePaths => Configuration.CachePaths;

    /// <summary>
    /// Convenience property for cache key
    /// </summary>
    public string CacheKey => Configuration.CacheKey;
}

/// <summary>
/// Cache policies
/// </summary>
public enum CachePolicy
{
    /// <summary>
    /// Cache per branch
    /// </summary>
    PerBranch,

    /// <summary>
    /// Cache per job
    /// </summary>
    PerJob,

    /// <summary>
    /// Global cache
    /// </summary>
    Global,

    /// <summary>
    /// No caching
    /// </summary>
    None
}

/// <summary>
/// Cache effectiveness levels
/// </summary>
public enum CacheEffectiveness
{
    /// <summary>
    /// Low effectiveness
    /// </summary>
    Low,

    /// <summary>
    /// Medium effectiveness
    /// </summary>
    Medium,

    /// <summary>
    /// High effectiveness
    /// </summary>
    High
}