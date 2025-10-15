namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Docker configuration information
/// </summary>
public class DockerConfiguration
{
    /// <summary>
    /// Whether Docker configuration was found
    /// </summary>
    public bool HasDockerConfig { get; set; }

    /// <summary>
    /// Dockerfile path
    /// </summary>
    public string? DockerfilePath { get; set; }

    /// <summary>
    /// Docker Compose configuration files
    /// </summary>
    public List<string> ComposeFiles { get; set; } = new();

    /// <summary>
    /// Base image detected in Dockerfile
    /// </summary>
    public string? BaseImage { get; set; }

    /// <summary>
    /// Exposed ports
    /// </summary>
    public List<int> ExposedPorts { get; set; } = new();

    /// <summary>
    /// Environment variables defined in Docker config
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    /// <summary>
    /// Volumes defined
    /// </summary>
    public List<string> Volumes { get; set; } = new();

    /// <summary>
    /// Services defined in Docker Compose
    /// </summary>
    public List<DockerService> Services { get; set; } = new();

    /// <summary>
    /// Build arguments detected
    /// </summary>
    public Dictionary<string, string> BuildArgs { get; set; } = new();

    /// <summary>
    /// Multi-stage build detected
    /// </summary>
    public bool IsMultiStage { get; set; }

    /// <summary>
    /// Build stages detected
    /// </summary>
    public List<string> BuildStages { get; set; } = new();

    /// <summary>
    /// Docker ignore patterns
    /// </summary>
    public List<string> IgnorePatterns { get; set; } = new();

    /// <summary>
    /// Recommended optimizations
    /// </summary>
    public List<string> OptimizationRecommendations { get; set; } = new();

    /// <summary>
    /// Confidence level of the analysis
    /// </summary>
    public AnalysisConfidence Confidence { get; set; } = AnalysisConfidence.Medium;
}

/// <summary>
/// Docker service information
/// </summary>
public class DockerService
{
    /// <summary>
    /// Service name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Docker image used
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// Build configuration
    /// </summary>
    public string? BuildContext { get; set; }

    /// <summary>
    /// Ports mapping
    /// </summary>
    public List<string> Ports { get; set; } = new();

    /// <summary>
    /// Environment variables
    /// </summary>
    public Dictionary<string, string> Environment { get; set; } = new();

    /// <summary>
    /// Volumes
    /// </summary>
    public List<string> Volumes { get; set; } = new();

    /// <summary>
    /// Service dependencies
    /// </summary>
    public List<string> DependsOn { get; set; } = new();

    /// <summary>
    /// Networks
    /// </summary>
    public List<string> Networks { get; set; } = new();
}