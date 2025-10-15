namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Information about detected build tools in a project
/// </summary>
public class BuildToolInfo
{
    /// <summary>
    /// Primary build tool name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Build tool version (if detected)
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Additional build tools detected
    /// </summary>
    public List<string> AdditionalTools { get; set; } = new();

    /// <summary>
    /// Detected build commands
    /// </summary>
    public List<string> BuildCommands { get; set; } = new();

    /// <summary>
    /// Detected test commands
    /// </summary>
    public List<string> TestCommands { get; set; } = new();

    /// <summary>
    /// Build configuration files found
    /// </summary>
    public List<string> ConfigurationFiles { get; set; } = new();

    /// <summary>
    /// Build tool specific settings
    /// </summary>
    public Dictionary<string, string> Settings { get; set; } = new();

    /// <summary>
    /// Confidence level of build tool detection
    /// </summary>
    public AnalysisConfidence Confidence { get; set; } = AnalysisConfidence.Low;
}