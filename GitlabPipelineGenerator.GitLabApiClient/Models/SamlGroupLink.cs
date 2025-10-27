using System.Text.Json.Serialization;

namespace GitlabPipelineGenerator.GitLabApiClient.Models;

public class SamlGroupLink
{
    [JsonPropertyName("saml_group_name")]
    public string SamlGroupName { get; set; } = string.Empty;

    [JsonPropertyName("access_level")]
    public int AccessLevel { get; set; }

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;
}