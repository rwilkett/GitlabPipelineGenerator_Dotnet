using System.Text.Json.Serialization;

namespace GitlabPipelineGenerator.OctopusApiClient.Models;

public class Team
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
}