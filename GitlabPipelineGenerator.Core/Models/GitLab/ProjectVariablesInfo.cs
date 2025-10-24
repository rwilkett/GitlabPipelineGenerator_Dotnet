namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Project-level CI/CD variables information
/// </summary>
public class ProjectVariablesInfo
{
    /// <summary>
    /// List of project variables
    /// </summary>
    public List<ProjectVariableInfo> Variables { get; set; } = new();

    /// <summary>
    /// Total number of variables
    /// </summary>
    public int TotalVariables => Variables.Count;

    /// <summary>
    /// Number of protected variables
    /// </summary>
    public int ProtectedVariables => Variables.Count(v => v.Protected);

    /// <summary>
    /// Number of masked variables
    /// </summary>
    public int MaskedVariables => Variables.Count(v => v.Masked);

    /// <summary>
    /// Environment scopes used
    /// </summary>
    public List<string> EnvironmentScopes => Variables.Select(v => v.EnvironmentScope).Distinct().ToList();

    /// <summary>
    /// Analysis confidence
    /// </summary>
    public AnalysisConfidence Confidence { get; set; } = AnalysisConfidence.High;
}

/// <summary>
/// Individual project variable information
/// </summary>
public class ProjectVariableInfo
{
    /// <summary>
    /// Variable key/name
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Variable type (env_var, file)
    /// </summary>
    public string VariableType { get; set; } = "env_var";

    /// <summary>
    /// Whether the variable is protected
    /// </summary>
    public bool Protected { get; set; }

    /// <summary>
    /// Whether the variable is masked
    /// </summary>
    public bool Masked { get; set; }

    /// <summary>
    /// Whether the variable is raw
    /// </summary>
    public bool Raw { get; set; }

    /// <summary>
    /// Environment scope
    /// </summary>
    public string EnvironmentScope { get; set; } = "*";

    /// <summary>
    /// Variable description
    /// </summary>
    public string? Description { get; set; }
}