namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Represents a package dependency
/// </summary>
public class PackageDependency
{
    /// <summary>
    /// Package name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Package version or version constraint
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Dependency type (production, development, etc.)
    /// </summary>
    public DependencyType Type { get; set; } = DependencyType.Production;

    /// <summary>
    /// Package scope or namespace
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Whether this is a security-sensitive dependency
    /// </summary>
    public bool IsSecuritySensitive { get; set; }

    /// <summary>
    /// Package category (framework, utility, testing, etc.)
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Additional metadata about the dependency
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Types of dependencies
/// </summary>
public enum DependencyType
{
    /// <summary>
    /// Production dependency
    /// </summary>
    Production,

    /// <summary>
    /// Development dependency
    /// </summary>
    Development,

    /// <summary>
    /// Test dependency
    /// </summary>
    Test,

    /// <summary>
    /// Build-time dependency
    /// </summary>
    Build,

    /// <summary>
    /// Optional dependency
    /// </summary>
    Optional
}