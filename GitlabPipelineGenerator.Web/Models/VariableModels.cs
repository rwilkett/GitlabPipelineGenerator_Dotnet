namespace GitlabPipelineGenerator.Web.Models;

public class ProjectVariablesModel
{
    public int TotalVariables { get; set; }
    public int ProtectedVariables { get; set; }
    public int MaskedVariables { get; set; }
    public List<string> EnvironmentScopes { get; set; } = new();
    public List<ProjectVariableModel> Variables { get; set; } = new();
}

public class ProjectVariableModel
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string VariableType { get; set; } = string.Empty;
    public bool Protected { get; set; }
    public bool Masked { get; set; }
    public string EnvironmentScope { get; set; } = string.Empty;
    public string? Description { get; set; }
}