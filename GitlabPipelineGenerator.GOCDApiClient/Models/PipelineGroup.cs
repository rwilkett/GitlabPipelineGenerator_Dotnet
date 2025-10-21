using System.Text.Json.Serialization;

namespace GitlabPipelineGenerator.GOCDApiClient.Models;

public class PipelineGroup
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("pipelines")]
    public List<PipelineInfo> Pipelines { get; set; } = new();

    [JsonPropertyName("authorization")]
    public GroupAuthorization? Authorization { get; set; }
}

public class PipelineInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    [JsonPropertyName("pause_info")]
    public PauseInfo? PauseInfo { get; set; }
}

public class GroupAuthorization
{
    [JsonPropertyName("view")]
    public ViewPermission? View { get; set; }

    [JsonPropertyName("admins")]
    public AdminPermission? Admins { get; set; }

    [JsonPropertyName("operate")]
    public OperatePermission? Operate { get; set; }
}

public class ViewPermission
{
    [JsonPropertyName("users")]
    public List<string> Users { get; set; } = new();

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();
}

public class AdminPermission
{
    [JsonPropertyName("users")]
    public List<string> Users { get; set; } = new();

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();
}

public class OperatePermission
{
    [JsonPropertyName("users")]
    public List<string> Users { get; set; } = new();

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();
}