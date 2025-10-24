namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Complete project analysis results
/// </summary>
public class ProjectAnalysisResult
{
    /// <summary>
    /// Detected project type
    /// </summary>
    public ProjectType DetectedType { get; set; } = ProjectType.Unknown;

    /// <summary>
    /// Framework information
    /// </summary>
    public FrameworkInfo Framework { get; set; } = new();

    /// <summary>
    /// Build configuration
    /// </summary>
    public BuildConfiguration BuildConfig { get; set; } = new();

    /// <summary>
    /// Dependency information
    /// </summary>
    public DependencyInfo Dependencies { get; set; } = new();

    /// <summary>
    /// Deployment information
    /// </summary>
    public DeploymentInfo Deployment { get; set; } = new();

    /// <summary>
    /// Existing CI/CD configuration
    /// </summary>
    public ExistingCIConfig? ExistingCI { get; set; }

    /// <summary>
    /// Docker configuration
    /// </summary>
    public DockerConfiguration? Docker { get; set; }

    /// <summary>
    /// Environment configuration
    /// </summary>
    public EnvironmentConfiguration? Environment { get; set; }

    /// <summary>
    /// Project-level CI/CD variables
    /// </summary>
    public ProjectVariablesInfo? Variables { get; set; }

    /// <summary>
    /// Overall analysis confidence
    /// </summary>
    public AnalysisConfidence Confidence { get; set; } = AnalysisConfidence.Low;

    /// <summary>
    /// Project ID that was analyzed
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Analysis mode used
    /// </summary>
    public AnalysisMode AnalysisMode { get; set; } = AnalysisMode.Full;

    /// <summary>
    /// Analysis warnings and issues
    /// </summary>
    public List<AnalysisWarning> Warnings { get; set; } = new();

    /// <summary>
    /// Analysis recommendations
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Analysis metadata
    /// </summary>
    public AnalysisMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Time taken for analysis
    /// </summary>
    public TimeSpan AnalysisTime { get; set; }

    /// <summary>
    /// Number of files analyzed
    /// </summary>
    public int FilesAnalyzed { get; set; }
}

/// <summary>
/// Analysis warning information
/// </summary>
public class AnalysisWarning
{
    /// <summary>
    /// Warning type
    /// </summary>
    public WarningType Type { get; set; } = WarningType.General;

    /// <summary>
    /// Warning severity
    /// </summary>
    public WarningSeverity Severity { get; set; } = WarningSeverity.Info;

    /// <summary>
    /// Warning message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Component that generated the warning
    /// </summary>
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// File or context related to the warning
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Suggested resolution
    /// </summary>
    public string? Resolution { get; set; }
}

/// <summary>
/// Analysis metadata
/// </summary>
public class AnalysisMetadata
{
    /// <summary>
    /// Analysis timestamp
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Analysis version
    /// </summary>
    public string AnalysisVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Project branch analyzed
    /// </summary>
    public string Branch { get; set; } = string.Empty;

    /// <summary>
    /// Project commit SHA
    /// </summary>
    public string? CommitSha { get; set; }

    /// <summary>
    /// Analysis options used
    /// </summary>
    public AnalysisOptions Options { get; set; } = new();

    /// <summary>
    /// Components that participated in analysis
    /// </summary>
    public List<string> AnalyzedComponents { get; set; } = new();
}

