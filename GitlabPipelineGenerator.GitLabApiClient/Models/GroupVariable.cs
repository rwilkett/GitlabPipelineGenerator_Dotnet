using System.Text.Json.Serialization;

namespace GitlabPipelineGenerator.GitLabApiClient.Models;

public class GroupVariable
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("variable_type")]
    public string VariableType { get; set; } = "env_var";

    [JsonPropertyName("protected")]
    public bool Protected { get; set; }

    [JsonPropertyName("masked")]
    public bool Masked { get; set; }

    [JsonPropertyName("raw")]
    public bool Raw { get; set; }

    [JsonPropertyName("environment_scope")]
    public string EnvironmentScope { get; set; } = "*";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("hidden")]
    public bool Hidden { get; set; }
}