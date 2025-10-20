using System.Text.Json.Serialization;

namespace GitlabPipelineGenerator.GitLabApiClient.Models;

public class RepositoryFile
{
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("encoding")]
    public string Encoding { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("content_sha256")]
    public string? ContentSha256 { get; set; }

    [JsonPropertyName("ref")]
    public string Ref { get; set; } = string.Empty;

    [JsonPropertyName("blob_id")]
    public string BlobId { get; set; } = string.Empty;

    [JsonPropertyName("commit_id")]
    public string CommitId { get; set; } = string.Empty;

    [JsonPropertyName("last_commit_id")]
    public string LastCommitId { get; set; } = string.Empty;
}

public class RepositoryTree
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = string.Empty;
}