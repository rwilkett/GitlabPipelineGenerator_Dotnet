namespace GitlabPipelineGenerator.Web.Models;

public class GitLabConnectionModel
{
    public string ServerUrl { get; set; } = "https://gitlab.com";
    public string Token { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
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
}