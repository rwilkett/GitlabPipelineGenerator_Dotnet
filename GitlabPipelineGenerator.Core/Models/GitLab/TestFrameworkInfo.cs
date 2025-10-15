namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Information about detected test frameworks in a project
/// </summary>
public class TestFrameworkInfo
{
    /// <summary>
    /// Primary test framework name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Test framework version (if detected)
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Additional test frameworks detected
    /// </summary>
    public List<string> AdditionalFrameworks { get; set; } = new();

    /// <summary>
    /// Detected test commands
    /// </summary>
    public List<string> TestCommands { get; set; } = new();

    /// <summary>
    /// Test configuration files found
    /// </summary>
    public List<string> ConfigurationFiles { get; set; } = new();

    /// <summary>
    /// Test directories detected
    /// </summary>
    public List<string> TestDirectories { get; set; } = new();

    /// <summary>
    /// Test framework specific settings
    /// </summary>
    public Dictionary<string, string> Settings { get; set; } = new();

    /// <summary>
    /// Confidence level of test framework detection
    /// </summary>
    public AnalysisConfidence Confidence { get; set; } = AnalysisConfidence.Low;
}