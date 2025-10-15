namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Deployment information
/// </summary>
public class DeploymentInfo
{
    /// <summary>
    /// Whether deployment configuration was detected
    /// </summary>
    public bool HasDeploymentConfig { get; set; }

    /// <summary>
    /// Deployment configuration
    /// </summary>
    public DeploymentConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// Environment configuration
    /// </summary>
    public EnvironmentConfiguration Environment { get; set; } = new();

    /// <summary>
    /// Recommended deployment strategies
    /// </summary>
    public List<DeploymentStrategy> RecommendedStrategies { get; set; } = new();

    /// <summary>
    /// Deployment prerequisites
    /// </summary>
    public List<string> Prerequisites { get; set; } = new();

    /// <summary>
    /// Required secrets for deployment
    /// </summary>
    public List<string> RequiredSecrets { get; set; } = new();

    /// <summary>
    /// Deployment commands
    /// </summary>
    public List<string> DeploymentCommands { get; set; } = new();

    /// <summary>
    /// Rollback commands
    /// </summary>
    public List<string> RollbackCommands { get; set; } = new();

    /// <summary>
    /// Health check configuration
    /// </summary>
    public HealthCheckConfiguration? HealthCheck { get; set; }

    /// <summary>
    /// Monitoring configuration
    /// </summary>
    public MonitoringConfiguration? Monitoring { get; set; }

    /// <summary>
    /// Detected environment names
    /// </summary>
    public List<string> DetectedEnvironments { get; set; } = new();

    /// <summary>
    /// Deployment confidence
    /// </summary>
    public AnalysisConfidence Confidence { get; set; } = AnalysisConfidence.Medium;
}

/// <summary>
/// Health check configuration
/// </summary>
public class HealthCheckConfiguration
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Health check timeout
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Health check interval
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Number of retries
    /// </summary>
    public int Retries { get; set; } = 3;

    /// <summary>
    /// Expected status codes
    /// </summary>
    public List<int> ExpectedStatusCodes { get; set; } = new() { 200 };
}

/// <summary>
/// Monitoring configuration
/// </summary>
public class MonitoringConfiguration
{
    /// <summary>
    /// Metrics endpoints
    /// </summary>
    public List<string> MetricsEndpoints { get; set; } = new();

    /// <summary>
    /// Log aggregation configuration
    /// </summary>
    public Dictionary<string, string> LogConfiguration { get; set; } = new();

    /// <summary>
    /// Alert configuration
    /// </summary>
    public Dictionary<string, string> AlertConfiguration { get; set; } = new();

    /// <summary>
    /// Tracing configuration
    /// </summary>
    public Dictionary<string, string> TracingConfiguration { get; set; } = new();
}