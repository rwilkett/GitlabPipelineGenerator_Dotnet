namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Information about runtime requirements
/// </summary>
public class RuntimeInfo
{
    /// <summary>
    /// Runtime name (e.g., "node", "dotnet", "python")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Runtime version or version constraint
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Additional runtime requirements
    /// </summary>
    public List<string> AdditionalRequirements { get; set; } = new();

    /// <summary>
    /// Environment variables required
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    /// <summary>
    /// System dependencies required
    /// </summary>
    public List<string> SystemDependencies { get; set; } = new();

    /// <summary>
    /// Docker base image recommendations
    /// </summary>
    public List<string> RecommendedBaseImages { get; set; } = new();

    /// <summary>
    /// Runtime-specific configuration
    /// </summary>
    public Dictionary<string, string> Configuration { get; set; } = new();
}