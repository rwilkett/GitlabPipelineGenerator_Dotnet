using System.Text.Json.Serialization;

namespace GitlabPipelineGenerator.GOCDApiClient.Models;

public class ApiResponse<T>
{
    [JsonPropertyName("_embedded")]
    public EmbeddedData<T>? Embedded { get; set; }

    [JsonPropertyName("_links")]
    public Dictionary<string, Link>? Links { get; set; }
}

public class EmbeddedData<T>
{
    [JsonPropertyName("pipelines")]
    public List<T>? Pipelines { get; set; }

    [JsonPropertyName("templates")]
    public List<T>? Templates { get; set; }
}

public class Link
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;
}

public class PipelineHistory
{
    [JsonPropertyName("pipelines")]
    public List<Pipeline> Pipelines { get; set; } = new();
}

public class TemplateList
{
    [JsonPropertyName("_embedded")]
    public TemplateEmbedded? Embedded { get; set; }
}

public class TemplateEmbedded
{
    [JsonPropertyName("templates")]
    public List<Template> Templates { get; set; } = new();
}