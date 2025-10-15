namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Security scanning configuration recommendations
/// </summary>
public class SecurityScanConfiguration
{
    /// <summary>
    /// Whether security scanning is recommended
    /// </summary>
    public bool IsRecommended { get; set; }

    /// <summary>
    /// Recommended security scanners
    /// </summary>
    public List<SecurityScanner> RecommendedScanners { get; set; } = new();

    /// <summary>
    /// Security scan configuration
    /// </summary>
    public Dictionary<string, string> Configuration { get; set; } = new();

    /// <summary>
    /// Reason for the recommendation
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Security risk level
    /// </summary>
    public SecurityRiskLevel RiskLevel { get; set; } = SecurityRiskLevel.Low;

    /// <summary>
    /// Specific vulnerabilities to scan for
    /// </summary>
    public List<string> TargetVulnerabilities { get; set; } = new();
}

/// <summary>
/// Security scanner information
/// </summary>
public class SecurityScanner
{
    /// <summary>
    /// Scanner name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Scanner type
    /// </summary>
    public SecurityScanType Type { get; set; }

    /// <summary>
    /// Scanner configuration
    /// </summary>
    public Dictionary<string, string> Configuration { get; set; } = new();

    /// <summary>
    /// Whether this scanner is enabled by default
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Scanner priority
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
/// Types of security scans
/// </summary>
public enum SecurityScanType
{
    /// <summary>
    /// Static Application Security Testing
    /// </summary>
    SAST,

    /// <summary>
    /// Dynamic Application Security Testing
    /// </summary>
    DAST,

    /// <summary>
    /// Dependency scanning
    /// </summary>
    DependencyScanning,

    /// <summary>
    /// Container scanning
    /// </summary>
    ContainerScanning,

    /// <summary>
    /// License scanning
    /// </summary>
    LicenseScanning,

    /// <summary>
    /// Secret detection
    /// </summary>
    SecretDetection
}

/// <summary>
/// Security risk levels
/// </summary>
public enum SecurityRiskLevel
{
    /// <summary>
    /// Low risk
    /// </summary>
    Low,

    /// <summary>
    /// Medium risk
    /// </summary>
    Medium,

    /// <summary>
    /// High risk
    /// </summary>
    High,

    /// <summary>
    /// Critical risk
    /// </summary>
    Critical
}