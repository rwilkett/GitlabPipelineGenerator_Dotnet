namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Environment configuration information
/// </summary>
public class EnvironmentConfiguration
{
    /// <summary>
    /// Detected environments
    /// </summary>
    public List<Environment> Environments { get; set; } = new();

    /// <summary>
    /// Environment-specific configuration files
    /// </summary>
    public Dictionary<string, List<string>> ConfigurationFiles { get; set; } = new();

    /// <summary>
    /// Global environment variables
    /// </summary>
    public Dictionary<string, string> GlobalVariables { get; set; } = new();

    /// <summary>
    /// Environment-specific variables
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> EnvironmentVariables { get; set; } = new();

    /// <summary>
    /// Secrets required for environments
    /// </summary>
    public Dictionary<string, List<string>> RequiredSecrets { get; set; } = new();

    /// <summary>
    /// Environment promotion rules
    /// </summary>
    public List<PromotionRule> PromotionRules { get; set; } = new();

    /// <summary>
    /// Confidence level of the analysis
    /// </summary>
    public AnalysisConfidence Confidence { get; set; } = AnalysisConfidence.Medium;
}

/// <summary>
/// Environment information
/// </summary>
public class Environment
{
    /// <summary>
    /// Environment name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Environment type
    /// </summary>
    public EnvironmentType Type { get; set; } = EnvironmentType.Unknown;

    /// <summary>
    /// Environment URL
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Deployment conditions
    /// </summary>
    public List<string> DeploymentConditions { get; set; } = new();

    /// <summary>
    /// Manual approval required
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// Environment-specific configuration
    /// </summary>
    public Dictionary<string, string> Configuration { get; set; } = new();

    /// <summary>
    /// Protection rules
    /// </summary>
    public List<string> ProtectionRules { get; set; } = new();
}

/// <summary>
/// Environment promotion rule
/// </summary>
public class PromotionRule
{
    /// <summary>
    /// Source environment
    /// </summary>
    public string SourceEnvironment { get; set; } = string.Empty;

    /// <summary>
    /// Target environment
    /// </summary>
    public string TargetEnvironment { get; set; } = string.Empty;

    /// <summary>
    /// Conditions for promotion
    /// </summary>
    public List<string> Conditions { get; set; } = new();

    /// <summary>
    /// Automatic promotion enabled
    /// </summary>
    public bool IsAutomatic { get; set; }

    /// <summary>
    /// Approval required for promotion
    /// </summary>
    public bool RequiresApproval { get; set; }
}

/// <summary>
/// Types of environments
/// </summary>
public enum EnvironmentType
{
    /// <summary>
    /// Unknown environment type
    /// </summary>
    Unknown,

    /// <summary>
    /// Development environment
    /// </summary>
    Development,

    /// <summary>
    /// Testing environment
    /// </summary>
    Testing,

    /// <summary>
    /// Staging environment
    /// </summary>
    Staging,

    /// <summary>
    /// Production environment
    /// </summary>
    Production,

    /// <summary>
    /// Preview environment
    /// </summary>
    Preview,

    /// <summary>
    /// Integration environment
    /// </summary>
    Integration,

    /// <summary>
    /// User acceptance testing environment
    /// </summary>
    UAT
}