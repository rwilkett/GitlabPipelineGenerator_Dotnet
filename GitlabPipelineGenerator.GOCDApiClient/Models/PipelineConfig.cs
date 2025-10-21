using System.Text.Json.Serialization;

namespace GitlabPipelineGenerator.GOCDApiClient.Models;

public class PipelineConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("template")]
    public string? Template { get; set; }

    [JsonPropertyName("environment_variables")]
    public List<EnvironmentVariable> EnvironmentVariables { get; set; } = new();

    [JsonPropertyName("materials")]
    public List<Material> Materials { get; set; } = new();

    [JsonPropertyName("stages")]
    public List<ConfigStage> Stages { get; set; } = new();
}

public class Material
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("attributes")]
    public MaterialAttributes Attributes { get; set; } = new();
}

public class MaterialAttributes
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("branch")]
    public string? Branch { get; set; }

    [JsonPropertyName("destination")]
    public string? Destination { get; set; }
}

public class ConfigStage
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("jobs")]
    public List<ConfigJob> Jobs { get; set; } = new();

    [JsonPropertyName("approval")]
    public Approval? Approval { get; set; }
}

public class ConfigJob
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tasks")]
    public List<Task> Tasks { get; set; } = new();

    [JsonPropertyName("environment_variables")]
    public List<EnvironmentVariable> EnvironmentVariables { get; set; } = new();
}