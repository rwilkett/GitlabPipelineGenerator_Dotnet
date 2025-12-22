using System.Text.Json.Serialization;

namespace GitlabPipelineGenerator.OctopusApiClient.Models;

public class Environment
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Description")]
    public string Description { get; set; } = string.Empty;
}