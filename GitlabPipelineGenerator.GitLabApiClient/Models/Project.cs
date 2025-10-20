using System.Text.Json.Serialization;

namespace GitlabPipelineGenerator.GitLabApiClient.Models;

public class Project
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("path_with_namespace")]
    public string PathWithNamespace { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("default_branch")]
    public string? DefaultBranch { get; set; }

    [JsonPropertyName("last_activity_at")]
    public DateTime? LastActivityAt { get; set; }

    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = "private";

    [JsonPropertyName("web_url")]
    public string WebUrl { get; set; } = string.Empty;

    [JsonPropertyName("ssh_url_to_repo")]
    public string SshUrlToRepo { get; set; } = string.Empty;

    [JsonPropertyName("http_url_to_repo")]
    public string HttpUrlToRepo { get; set; } = string.Empty;

    [JsonPropertyName("namespace")]
    public Namespace? Namespace { get; set; }

    [JsonPropertyName("permissions")]
    public ProjectPermissions? Permissions { get; set; }

    [JsonPropertyName("issues_enabled")]
    public bool IssuesEnabled { get; set; }

    [JsonPropertyName("merge_requests_enabled")]
    public bool MergeRequestsEnabled { get; set; }

    [JsonPropertyName("wiki_enabled")]
    public bool WikiEnabled { get; set; }

    [JsonPropertyName("snippets_enabled")]
    public bool SnippetsEnabled { get; set; }
}

public class Namespace
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;
}

public class ProjectPermissions
{
    [JsonPropertyName("project_access")]
    public AccessInfo? ProjectAccess { get; set; }
}

public class AccessInfo
{
    [JsonPropertyName("access_level")]
    public int AccessLevel { get; set; }
}