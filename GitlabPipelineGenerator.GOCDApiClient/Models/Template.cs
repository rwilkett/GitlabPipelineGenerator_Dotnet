using System.Text.Json.Serialization;

namespace GitlabPipelineGenerator.GOCDApiClient.Models;

public class Template
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("stages")]
    public List<TemplateStage> Stages { get; set; } = new();

    [JsonPropertyName("parameters")]
    public List<Parameter> Parameters { get; set; } = new();
}

public class TemplateStage
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("fetch_materials")]
    public bool FetchMaterials { get; set; }

    [JsonPropertyName("clean_working_directory")]
    public bool CleanWorkingDirectory { get; set; }

    [JsonPropertyName("never_cleanup_artifacts")]
    public bool NeverCleanupArtifacts { get; set; }

    [JsonPropertyName("approval")]
    public Approval? Approval { get; set; }

    [JsonPropertyName("jobs")]
    public List<TemplateJob> Jobs { get; set; } = new();
}

public class TemplateJob
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("run_instance_count")]
    public string? RunInstanceCount { get; set; }

    [JsonPropertyName("timeout")]
    public int? Timeout { get; set; }

    [JsonPropertyName("environment_variables")]
    public List<EnvironmentVariable> EnvironmentVariables { get; set; } = new();

    [JsonPropertyName("resources")]
    public List<string> Resources { get; set; } = new();

    [JsonPropertyName("tasks")]
    public List<Task> Tasks { get; set; } = new();

    [JsonPropertyName("tabs")]
    public List<Tab> Tabs { get; set; } = new();

    [JsonPropertyName("artifacts")]
    public List<Artifact> Artifacts { get; set; } = new();
}

public class Parameter
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("default_value")]
    public string? DefaultValue { get; set; }
}

public class Approval
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("authorization")]
    public Authorization? Authorization { get; set; }
}

public class Authorization
{
    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();

    [JsonPropertyName("users")]
    public List<string> Users { get; set; } = new();
}

public class EnvironmentVariable
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("secure")]
    public bool Secure { get; set; }
}

public class Task
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("attributes")]
    public Dictionary<string, object> Attributes { get; set; } = new();
}

public class Tab
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
}

public class Artifact
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("destination")]
    public string? Destination { get; set; }
}