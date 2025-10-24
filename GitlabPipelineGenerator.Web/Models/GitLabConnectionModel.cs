namespace GitlabPipelineGenerator.Web.Models;

public class GitLabConnectionModel
{
    public string ServerUrl { get; set; } = "https://gitlab.com";
    public string Token { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string DefaultGroupId { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
}

public class ProjectListModel
{
    public string Token { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public List<ProjectItem> Projects { get; set; } = new();
}

public class ProjectItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string WebUrl { get; set; } = string.Empty;
}

public class SubgroupItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string WebUrl { get; set; } = string.Empty;
    public ProjectVariablesModel? Variables { get; set; }
}

public class AnalysisResultModel
{
    public string ProjectName { get; set; } = string.Empty;
    public string DetectedType { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public string BuildTool { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public List<string> BuildCommands { get; set; } = new();
    public List<string> TestCommands { get; set; } = new();
    public string YamlContent { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public ProjectVariablesModel? Variables { get; set; }
}

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
    public string VariableType { get; set; } = string.Empty;
    public bool Protected { get; set; }
    public bool Masked { get; set; }
    public string EnvironmentScope { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class VariablesReportModel
{
    public string GroupName { get; set; } = string.Empty;
    public List<GroupVariablesSummary> Groups { get; set; } = new();
    public List<ProjectVariablesSummary> Projects { get; set; } = new();
}

public class GroupVariablesSummary
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public int VariableCount { get; set; }
}

public class ProjectVariablesSummary
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public int VariableCount { get; set; }
}

public class PipelineRunsReportModel
{
    public string GroupName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<GroupPipelineRuns> Groups { get; set; } = new();
}

public class GroupPipelineRuns
{
    public string GroupName { get; set; } = string.Empty;
    public string GroupPath { get; set; } = string.Empty;
    public List<ProjectPipelineRuns> Projects { get; set; } = new();
}

public class ProjectPipelineRuns
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public int PipelineCount { get; set; }
}