namespace GitlabPipelineGenerator.Web.Models;

public class GitLabConnectionModel
{
    public string ServerUrl { get; set; } = "https://gitlab.com";
    public string Token { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string DefaultGroupId { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
}

public class GitLabInstanceModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ServerUrl { get; set; } = "https://gitlab.com";
    public string Token { get; set; } = string.Empty;
    public string DefaultGroupId { get; set; } = string.Empty;
}

public class GitLabSettingsModel
{
    public List<GitLabInstanceModel> Instances { get; set; } = new();
    public string? DefaultInstanceId { get; set; }
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

public class SamlGroupLinkModel
{
    public string SamlGroupName { get; set; } = string.Empty;
    public string AccessLevel { get; set; } = string.Empty;
    public string MemberRoleId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
}