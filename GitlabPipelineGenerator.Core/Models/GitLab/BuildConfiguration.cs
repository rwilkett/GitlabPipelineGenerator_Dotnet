namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Build configuration information
/// </summary>
public class BuildConfiguration
{
    /// <summary>
    /// Primary build tool
    /// </summary>
    public string BuildTool { get; set; } = string.Empty;

    /// <summary>
    /// Build tool version
    /// </summary>
    public string? BuildToolVersion { get; set; }

    /// <summary>
    /// Build commands
    /// </summary>
    public List<string> BuildCommands { get; set; } = new();

    /// <summary>
    /// Test commands
    /// </summary>
    public List<string> TestCommands { get; set; } = new();

    /// <summary>
    /// Lint commands
    /// </summary>
    public List<string> LintCommands { get; set; } = new();

    /// <summary>
    /// Package/publish commands
    /// </summary>
    public List<string> PackageCommands { get; set; } = new();

    /// <summary>
    /// Artifact paths
    /// </summary>
    public List<string> ArtifactPaths { get; set; } = new();

    /// <summary>
    /// Build output directories
    /// </summary>
    public List<string> OutputDirectories { get; set; } = new();

    /// <summary>
    /// Docker configuration for build
    /// </summary>
    public DockerConfiguration? Docker { get; set; }

    /// <summary>
    /// Build environment variables
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    /// <summary>
    /// Build prerequisites
    /// </summary>
    public List<string> Prerequisites { get; set; } = new();

    /// <summary>
    /// Build stages
    /// </summary>
    public List<BuildStage> Stages { get; set; } = new();

    /// <summary>
    /// Parallel build support
    /// </summary>
    public bool SupportsParallelBuild { get; set; }

    /// <summary>
    /// Estimated build time
    /// </summary>
    public TimeSpan EstimatedBuildTime { get; set; }

    /// <summary>
    /// Build configuration confidence
    /// </summary>
    public AnalysisConfidence Confidence { get; set; } = AnalysisConfidence.Medium;
}

/// <summary>
/// Build stage information
/// </summary>
public class BuildStage
{
    /// <summary>
    /// Stage name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Stage commands
    /// </summary>
    public List<string> Commands { get; set; } = new();

    /// <summary>
    /// Stage dependencies
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// Stage artifacts
    /// </summary>
    public List<string> Artifacts { get; set; } = new();

    /// <summary>
    /// Whether stage can run in parallel
    /// </summary>
    public bool CanRunInParallel { get; set; }

    /// <summary>
    /// Stage priority
    /// </summary>
    public int Priority { get; set; }
}