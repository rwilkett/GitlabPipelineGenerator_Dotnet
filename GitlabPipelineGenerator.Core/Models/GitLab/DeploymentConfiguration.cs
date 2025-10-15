namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Deployment configuration information
/// </summary>
public class DeploymentConfiguration
{
    /// <summary>
    /// Whether deployment configuration was found
    /// </summary>
    public bool HasDeploymentConfig { get; set; }

    /// <summary>
    /// Deployment targets detected
    /// </summary>
    public List<DeploymentTarget> Targets { get; set; } = new();

    /// <summary>
    /// Deployment strategies detected
    /// </summary>
    public List<DeploymentStrategy> Strategies { get; set; } = new();

    /// <summary>
    /// Infrastructure as Code files detected
    /// </summary>
    public List<string> IaCFiles { get; set; } = new();

    /// <summary>
    /// Kubernetes configuration files
    /// </summary>
    public List<string> KubernetesFiles { get; set; } = new();

    /// <summary>
    /// Helm charts detected
    /// </summary>
    public List<string> HelmCharts { get; set; } = new();

    /// <summary>
    /// Terraform files detected
    /// </summary>
    public List<string> TerraformFiles { get; set; } = new();

    /// <summary>
    /// CloudFormation templates detected
    /// </summary>
    public List<string> CloudFormationTemplates { get; set; } = new();

    /// <summary>
    /// Environment-specific configuration files
    /// </summary>
    public Dictionary<string, List<string>> EnvironmentConfigs { get; set; } = new();

    /// <summary>
    /// Deployment scripts detected
    /// </summary>
    public List<string> DeploymentScripts { get; set; } = new();

    /// <summary>
    /// Required secrets for deployment
    /// </summary>
    public List<string> RequiredSecrets { get; set; } = new();

    /// <summary>
    /// Confidence level of the analysis
    /// </summary>
    public AnalysisConfidence Confidence { get; set; } = AnalysisConfidence.Medium;
}

/// <summary>
/// Deployment target information
/// </summary>
public class DeploymentTarget
{
    /// <summary>
    /// Target name or environment
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Target type
    /// </summary>
    public DeploymentTargetType Type { get; set; } = DeploymentTargetType.Unknown;

    /// <summary>
    /// Target URL or endpoint
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Configuration files for this target
    /// </summary>
    public List<string> ConfigFiles { get; set; } = new();

    /// <summary>
    /// Required environment variables
    /// </summary>
    public List<string> RequiredVariables { get; set; } = new();

    /// <summary>
    /// Deployment conditions
    /// </summary>
    public List<string> Conditions { get; set; } = new();
}

/// <summary>
/// Deployment strategy information
/// </summary>
public class DeploymentStrategy
{
    /// <summary>
    /// Strategy name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Strategy type
    /// </summary>
    public DeploymentStrategyType Type { get; set; } = DeploymentStrategyType.Unknown;

    /// <summary>
    /// Configuration for the strategy
    /// </summary>
    public Dictionary<string, string> Configuration { get; set; } = new();

    /// <summary>
    /// Rollback configuration
    /// </summary>
    public Dictionary<string, string> RollbackConfig { get; set; } = new();
}

/// <summary>
/// Types of deployment targets
/// </summary>
public enum DeploymentTargetType
{
    /// <summary>
    /// Unknown target type
    /// </summary>
    Unknown,

    /// <summary>
    /// Kubernetes cluster
    /// </summary>
    Kubernetes,

    /// <summary>
    /// Docker container
    /// </summary>
    Docker,

    /// <summary>
    /// Cloud platform (AWS, Azure, GCP)
    /// </summary>
    Cloud,

    /// <summary>
    /// Virtual machine or server
    /// </summary>
    Server,

    /// <summary>
    /// Static website hosting
    /// </summary>
    Static,

    /// <summary>
    /// Serverless function
    /// </summary>
    Serverless,

    /// <summary>
    /// Container registry
    /// </summary>
    Registry
}

/// <summary>
/// Types of deployment strategies
/// </summary>
public enum DeploymentStrategyType
{
    /// <summary>
    /// Unknown strategy
    /// </summary>
    Unknown,

    /// <summary>
    /// Rolling deployment
    /// </summary>
    Rolling,

    /// <summary>
    /// Blue-green deployment
    /// </summary>
    BlueGreen,

    /// <summary>
    /// Canary deployment
    /// </summary>
    Canary,

    /// <summary>
    /// Recreate deployment
    /// </summary>
    Recreate,

    /// <summary>
    /// A/B testing deployment
    /// </summary>
    ABTesting
}