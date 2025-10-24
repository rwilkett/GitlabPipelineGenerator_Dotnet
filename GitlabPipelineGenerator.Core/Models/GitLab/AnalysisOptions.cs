namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Configuration options for project analysis
/// </summary>
public class AnalysisOptions
{
    /// <summary>
    /// Whether to analyze project files and structure
    /// </summary>
    public bool AnalyzeFiles { get; set; } = true;

    /// <summary>
    /// Whether to analyze project dependencies
    /// </summary>
    public bool AnalyzeDependencies { get; set; } = true;

    /// <summary>
    /// Whether to analyze existing CI/CD configuration
    /// </summary>
    public bool AnalyzeExistingCI { get; set; } = true;

    /// <summary>
    /// Whether to analyze deployment configuration
    /// </summary>
    public bool AnalyzeDeployment { get; set; } = true;

    /// <summary>
    /// Whether to analyze project-level CI/CD variables
    /// </summary>
    public bool AnalyzeVariables { get; set; } = true;

    /// <summary>
    /// Maximum depth for file analysis (directory levels)
    /// </summary>
    public int MaxFileAnalysisDepth { get; set; } = 3;

    /// <summary>
    /// File patterns to exclude from analysis
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = new()
    {
        "node_modules/**",
        "bin/**",
        "obj/**",
        ".git/**",
        "*.log",
        "*.tmp"
    };

    /// <summary>
    /// Whether to include security analysis recommendations
    /// </summary>
    public bool IncludeSecurityAnalysis { get; set; } = true;

    /// <summary>
    /// Maximum file size to analyze (in bytes)
    /// </summary>
    public long MaxFileSize { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Maximum number of files to analyze
    /// </summary>
    public int MaxFilesAnalyzed { get; set; } = 1000;

    /// <summary>
    /// Supported file extensions for analysis
    /// </summary>
    public List<string> SupportedFileExtensions { get; set; } = new()
    {
        ".cs", ".csproj", ".sln",
        ".js", ".ts", ".json", ".package.json",
        ".py", ".requirements.txt", ".setup.py",
        ".java", ".pom.xml", ".gradle",
        ".yml", ".yaml",
        ".xml", ".config",
        ".dockerfile", ".docker-compose.yml"
    };
}

