namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Information about existing CI/CD configuration
/// </summary>
public class ExistingCIConfig
{
    /// <summary>
    /// Whether existing CI/CD configuration was found
    /// </summary>
    public bool HasExistingConfig { get; set; }

    /// <summary>
    /// Type of CI/CD system detected
    /// </summary>
    public CISystemType SystemType { get; set; } = CISystemType.None;

    /// <summary>
    /// Configuration files found
    /// </summary>
    public List<string> ConfigurationFiles { get; set; } = new();

    /// <summary>
    /// Detected stages in existing configuration
    /// </summary>
    public List<string> DetectedStages { get; set; } = new();

    /// <summary>
    /// Detected jobs in existing configuration
    /// </summary>
    public List<CIJob> DetectedJobs { get; set; } = new();

    /// <summary>
    /// Environment variables detected
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    /// <summary>
    /// Secrets or variables that need to be configured
    /// </summary>
    public List<string> RequiredSecrets { get; set; } = new();

    /// <summary>
    /// Docker images used in existing configuration
    /// </summary>
    public List<string> DockerImages { get; set; } = new();

    /// <summary>
    /// Services defined in existing configuration
    /// </summary>
    public List<string> Services { get; set; } = new();

    /// <summary>
    /// Cache configuration detected
    /// </summary>
    public List<string> CacheConfiguration { get; set; } = new();

    /// <summary>
    /// Artifacts configuration detected
    /// </summary>
    public List<string> ArtifactsConfiguration { get; set; } = new();

    /// <summary>
    /// Confidence level of the analysis
    /// </summary>
    public AnalysisConfidence Confidence { get; set; } = AnalysisConfidence.Medium;

    /// <summary>
    /// Migration recommendations
    /// </summary>
    public List<string> MigrationRecommendations { get; set; } = new();
}

/// <summary>
/// CI/CD system types
/// </summary>
public enum CISystemType
{
    /// <summary>
    /// No CI/CD system detected
    /// </summary>
    None,

    /// <summary>
    /// GitLab CI/CD
    /// </summary>
    GitLabCI,

    /// <summary>
    /// GitHub Actions
    /// </summary>
    GitHubActions,

    /// <summary>
    /// Jenkins
    /// </summary>
    Jenkins,

    /// <summary>
    /// Azure DevOps
    /// </summary>
    AzureDevOps,

    /// <summary>
    /// CircleCI
    /// </summary>
    CircleCI,

    /// <summary>
    /// Travis CI
    /// </summary>
    TravisCI,

    /// <summary>
    /// Other CI/CD system
    /// </summary>
    Other
}

/// <summary>
/// Represents a CI/CD job
/// </summary>
public class CIJob
{
    /// <summary>
    /// Job name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Stage the job belongs to
    /// </summary>
    public string Stage { get; set; } = string.Empty;

    /// <summary>
    /// Docker image used by the job
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// Scripts or commands in the job
    /// </summary>
    public List<string> Scripts { get; set; } = new();

    /// <summary>
    /// Job dependencies
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// Job artifacts
    /// </summary>
    public List<string> Artifacts { get; set; } = new();

    /// <summary>
    /// Job type (build, test, deploy, etc.)
    /// </summary>
    public JobType Type { get; set; } = JobType.Unknown;
}

/// <summary>
/// Types of CI/CD jobs
/// </summary>
public enum JobType
{
    /// <summary>
    /// Unknown job type
    /// </summary>
    Unknown,

    /// <summary>
    /// Build job
    /// </summary>
    Build,

    /// <summary>
    /// Test job
    /// </summary>
    Test,

    /// <summary>
    /// Deploy job
    /// </summary>
    Deploy,

    /// <summary>
    /// Security scan job
    /// </summary>
    Security,

    /// <summary>
    /// Quality analysis job
    /// </summary>
    Quality,

    /// <summary>
    /// Package/publish job
    /// </summary>
    Package
}