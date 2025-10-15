namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Information about detected frameworks in a project
/// </summary>
public class FrameworkInfo
{
    /// <summary>
    /// Primary framework name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Framework version (if detected)
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Additional frameworks detected
    /// </summary>
    public List<string> AdditionalFrameworks { get; set; } = new();

    /// <summary>
    /// Detected features or capabilities
    /// </summary>
    public List<string> DetectedFeatures { get; set; } = new();

    /// <summary>
    /// Framework-specific configuration
    /// </summary>
    public Dictionary<string, string> Configuration { get; set; } = new();

    /// <summary>
    /// Confidence level of framework detection
    /// </summary>
    public AnalysisConfidence Confidence { get; set; } = AnalysisConfidence.Low;
}