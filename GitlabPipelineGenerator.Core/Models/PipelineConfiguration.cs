using YamlDotNet.Serialization;

namespace GitlabPipelineGenerator.Core.Models;

/// <summary>
/// Represents a complete GitLab CI/CD pipeline configuration
/// </summary>
public class PipelineConfiguration
{
    /// <summary>
    /// List of stages in the pipeline (e.g., build, test, deploy)
    /// </summary>
    [YamlMember(Alias = "stages")]
    public List<string> Stages { get; set; } = new();

    /// <summary>
    /// Dictionary of jobs in the pipeline, keyed by job name
    /// </summary>
    [YamlMember(Alias = "jobs")]
    public Dictionary<string, Job> Jobs { get; set; } = new();

    /// <summary>
    /// Global variables available to all jobs
    /// </summary>
    [YamlMember(Alias = "variables")]
    public Dictionary<string, object> Variables { get; set; } = new();

    /// <summary>
    /// Default settings applied to all jobs
    /// </summary>
    [YamlMember(Alias = "default")]
    public Dictionary<string, object> Default { get; set; } = new();

    /// <summary>
    /// Workflow rules for when the pipeline should run
    /// </summary>
    [YamlMember(Alias = "workflow")]
    public WorkflowRules? Workflow { get; set; }

    /// <summary>
    /// Include external YAML files
    /// </summary>
    [YamlMember(Alias = "include")]
    public List<IncludeRule>? Include { get; set; }
}

/// <summary>
/// Represents workflow rules for pipeline execution
/// </summary>
public class WorkflowRules
{
    /// <summary>
    /// Rules that determine when the pipeline runs
    /// </summary>
    [YamlMember(Alias = "rules")]
    public List<Rule> Rules { get; set; } = new();
}

/// <summary>
/// Represents a rule for workflow or job execution
/// </summary>
public class Rule
{
    /// <summary>
    /// Condition for when the rule applies
    /// </summary>
    [YamlMember(Alias = "if")]
    public string? If { get; set; }

    /// <summary>
    /// When to run the job/pipeline (always, on_success, on_failure, manual, delayed)
    /// </summary>
    [YamlMember(Alias = "when")]
    public string? When { get; set; }

    /// <summary>
    /// Allow failure for this rule
    /// </summary>
    [YamlMember(Alias = "allow_failure")]
    public bool? AllowFailure { get; set; }
}

/// <summary>
/// Represents an include rule for external YAML files
/// </summary>
public class IncludeRule
{
    /// <summary>
    /// Local file path to include
    /// </summary>
    [YamlMember(Alias = "local")]
    public string? Local { get; set; }

    /// <summary>
    /// Remote file URL to include
    /// </summary>
    [YamlMember(Alias = "remote")]
    public string? Remote { get; set; }

    /// <summary>
    /// Template to include
    /// </summary>
    [YamlMember(Alias = "template")]
    public string? Template { get; set; }

    /// <summary>
    /// Project to include from
    /// </summary>
    [YamlMember(Alias = "project")]
    public string? Project { get; set; }

    /// <summary>
    /// File path in the project
    /// </summary>
    [YamlMember(Alias = "file")]
    public string? File { get; set; }

    /// <summary>
    /// Reference (branch, tag, commit) to use
    /// </summary>
    [YamlMember(Alias = "ref")]
    public string? Ref { get; set; }
}