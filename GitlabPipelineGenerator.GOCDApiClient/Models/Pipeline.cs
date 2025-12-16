using System.Text.Json;
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
    [JsonConverter(typeof(UnixTimestampConverter))]
    public DateTime? ScheduledDate { get; set; }
}

public class UnixTimestampConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out long unixTime))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(unixTime).DateTime;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(((DateTimeOffset)value.Value).ToUnixTimeMilliseconds());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public class PipelineRun
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("stages")]
    public List<StageRun> Stages { get; set; } = new();
}

public class StageRun
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("jobs")]
    public List<JobRun> Jobs { get; set; } = new();
}

public class JobRun
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("scheduled_date")]
    [JsonConverter(typeof(UnixTimestampConverter))]
    public DateTime? ScheduledDate { get; set; }

    /* [JsonPropertyName("completed_date")]
    public DateTime? CompletedDate { get; set; } */
}