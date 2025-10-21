using System.Text.Json.Serialization;

namespace GitlabPipelineGenerator.GOCDApiClient.Models;

public class Pipeline
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    [JsonPropertyName("pause_info")]
    public PauseInfo? PauseInfo { get; set; }

    [JsonPropertyName("stages")]
    public List<Stage> Stages { get; set; } = new();
}

public class PauseInfo
{
    [JsonPropertyName("paused")]
    public bool Paused { get; set; }

    [JsonPropertyName("pause_by")]
    public string? PauseBy { get; set; }

    [JsonPropertyName("pause_reason")]
    public string? PauseReason { get; set; }
}

public class Stage
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("counter")]
    public int Counter { get; set; }

    [JsonPropertyName("approval_type")]
    public string ApprovalType { get; set; } = string.Empty;

    [JsonPropertyName("approved_by")]
    public string? ApprovedBy { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("jobs")]
    public List<Job> Jobs { get; set; } = new();
}

public class Job
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("scheduled_date")]
    public DateTime? ScheduledDate { get; set; }
}