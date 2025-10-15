namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Information about project dependencies
/// </summary>
public class DependencyInfo
{
    /// <summary>
    /// Production dependencies
    /// </summary>
    public List<PackageDependency> Dependencies { get; set; } = new();

    /// <summary>
    /// Development dependencies
    /// </summary>
    public List<PackageDependency> DevDependencies { get; set; } = new();

    /// <summary>
    /// Runtime information detected from dependencies
    /// </summary>
    public RuntimeInfo Runtime { get; set; } = new();

    /// <summary>
    /// Cache recommendations based on dependencies
    /// </summary>
    public CacheRecommendation CacheRecommendation { get; set; } = new();

    /// <summary>
    /// Package manager used
    /// </summary>
    public string PackageManager { get; set; } = string.Empty;

    /// <summary>
    /// Package file that was analyzed
    /// </summary>
    public string PackageFile { get; set; } = string.Empty;

    /// <summary>
    /// Total number of dependencies
    /// </summary>
    public int TotalDependencies => Dependencies.Count + DevDependencies.Count;

    /// <summary>
    /// Whether the project has security-sensitive dependencies
    /// </summary>
    public bool HasSecuritySensitiveDependencies { get; set; }

    /// <summary>
    /// Security scan recommendations based on dependencies
    /// </summary>
    public SecurityScanConfiguration SecurityScanRecommendation { get; set; } = new();

    /// <summary>
    /// Confidence level of dependency analysis
    /// </summary>
    public AnalysisConfidence Confidence { get; set; } = AnalysisConfidence.Medium;
}