using System.ComponentModel.DataAnnotations;
using YamlDotNet.Serialization;

namespace GitlabPipelineGenerator.Core.Models;

/// <summary>
/// Represents a GitLab CI/CD job
/// </summary>
public class Job
{
    /// <summary>
    /// The stage this job belongs to (required)
    /// </summary>
    [Required(ErrorMessage = "Stage is required for all jobs")]
    [YamlMember(Alias = "stage")]
    public string Stage { get; set; } = string.Empty;

    /// <summary>
    /// List of commands to execute in the job (required)
    /// </summary>
    [Required(ErrorMessage = "Script is required for all jobs")]
    [YamlMember(Alias = "script")]
    public List<string> Script { get; set; } = new();

    /// <summary>
    /// Commands to run before the main script
    /// </summary>
    [YamlMember(Alias = "before_script")]
    public List<string>? BeforeScript { get; set; }

    /// <summary>
    /// Commands to run after the main script
    /// </summary>
    [YamlMember(Alias = "after_script")]
    public List<string>? AfterScript { get; set; }

    /// <summary>
    /// Job-specific variables
    /// </summary>
    [YamlMember(Alias = "variables")]
    public Dictionary<string, object>? Variables { get; set; }

    /// <summary>
    /// Tags to select specific runners
    /// </summary>
    [YamlMember(Alias = "tags")]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Artifacts configuration for the job
    /// </summary>
    [YamlMember(Alias = "artifacts")]
    public JobArtifacts? Artifacts { get; set; }

    /// <summary>
    /// Cache configuration for the job
    /// </summary>
    [YamlMember(Alias = "cache")]
    public JobCache? Cache { get; set; }

    /// <summary>
    /// Dependencies on other jobs
    /// </summary>
    [YamlMember(Alias = "dependencies")]
    public List<string>? Dependencies { get; set; }

    /// <summary>
    /// Needs configuration for job dependencies
    /// </summary>
    [YamlMember(Alias = "needs")]
    public List<JobNeed>? Needs { get; set; }

    /// <summary>
    /// When to run this job (on_success, on_failure, always, manual, delayed)
    /// </summary>
    [YamlMember(Alias = "when")]
    public string? When { get; set; }

    /// <summary>
    /// Allow this job to fail without failing the pipeline
    /// </summary>
    [YamlMember(Alias = "allow_failure")]
    public bool? AllowFailure { get; set; }

    /// <summary>
    /// Timeout for the job in seconds
    /// </summary>
    [YamlMember(Alias = "timeout")]
    public string? Timeout { get; set; }

    /// <summary>
    /// Retry configuration for failed jobs
    /// </summary>
    [YamlMember(Alias = "retry")]
    public JobRetry? Retry { get; set; }

    /// <summary>
    /// Rules for when this job should run
    /// </summary>
    [YamlMember(Alias = "rules")]
    public List<Rule>? Rules { get; set; }

    /// <summary>
    /// Only/except configuration (legacy, prefer rules)
    /// </summary>
    [YamlMember(Alias = "only")]
    public JobCondition? Only { get; set; }

    /// <summary>
    /// Except configuration (legacy, prefer rules)
    /// </summary>
    [YamlMember(Alias = "except")]
    public JobCondition? Except { get; set; }

    /// <summary>
    /// Docker image to use for this job
    /// </summary>
    [YamlMember(Alias = "image")]
    public JobImage? Image { get; set; }

    /// <summary>
    /// Services to run alongside this job
    /// </summary>
    [YamlMember(Alias = "services")]
    public List<JobService>? Services { get; set; }

    /// <summary>
    /// Environment configuration for deployments
    /// </summary>
    [YamlMember(Alias = "environment")]
    public JobEnvironment? Environment { get; set; }

    /// <summary>
    /// Coverage regex pattern
    /// </summary>
    [YamlMember(Alias = "coverage")]
    public string? Coverage { get; set; }

    /// <summary>
    /// Parallel configuration for running multiple instances
    /// </summary>
    [YamlMember(Alias = "parallel")]
    public JobParallel? Parallel { get; set; }

    /// <summary>
    /// Resource group for limiting concurrent jobs
    /// </summary>
    [YamlMember(Alias = "resource_group")]
    public string? ResourceGroup { get; set; }

    /// <summary>
    /// Release configuration for creating releases
    /// </summary>
    [YamlMember(Alias = "release")]
    public JobRelease? Release { get; set; }
}

/// <summary>
/// Represents job artifacts configuration
/// </summary>
public class JobArtifacts
{
    /// <summary>
    /// Paths to include as artifacts
    /// </summary>
    [YamlMember(Alias = "paths")]
    public List<string>? Paths { get; set; }

    /// <summary>
    /// Paths to exclude from artifacts
    /// </summary>
    [YamlMember(Alias = "exclude")]
    public List<string>? Exclude { get; set; }

    /// <summary>
    /// When to collect artifacts (on_success, on_failure, always)
    /// </summary>
    [YamlMember(Alias = "when")]
    public string? When { get; set; }

    /// <summary>
    /// How long to keep artifacts
    /// </summary>
    [YamlMember(Alias = "expire_in")]
    public string? ExpireIn { get; set; }

    /// <summary>
    /// Name for the artifacts archive
    /// </summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Reports configuration for test results, coverage, etc.
    /// </summary>
    [YamlMember(Alias = "reports")]
    public ArtifactReports? Reports { get; set; }

    /// <summary>
    /// Whether artifacts are public
    /// </summary>
    [YamlMember(Alias = "public")]
    public bool? Public { get; set; }
}

/// <summary>
/// Represents artifact reports configuration
/// </summary>
public class ArtifactReports
{
    /// <summary>
    /// JUnit test report files
    /// </summary>
    [YamlMember(Alias = "junit")]
    public List<string>? Junit { get; set; }

    /// <summary>
    /// Coverage report files
    /// </summary>
    [YamlMember(Alias = "coverage_report")]
    public CoverageReport? CoverageReport { get; set; }

    /// <summary>
    /// Cobertura coverage report files
    /// </summary>
    [YamlMember(Alias = "cobertura")]
    public List<string>? Cobertura { get; set; }

    /// <summary>
    /// Code quality report files
    /// </summary>
    [YamlMember(Alias = "codequality")]
    public List<string>? CodeQuality { get; set; }

    /// <summary>
    /// SAST security report files
    /// </summary>
    [YamlMember(Alias = "sast")]
    public List<string>? Sast { get; set; }

    /// <summary>
    /// Dependency scanning report files
    /// </summary>
    [YamlMember(Alias = "dependency_scanning")]
    public List<string>? DependencyScanning { get; set; }

    /// <summary>
    /// Container scanning report files
    /// </summary>
    [YamlMember(Alias = "container_scanning")]
    public List<string>? ContainerScanning { get; set; }

    /// <summary>
    /// Performance report files
    /// </summary>
    [YamlMember(Alias = "performance")]
    public List<string>? Performance { get; set; }
}

/// <summary>
/// Represents coverage report configuration
/// </summary>
public class CoverageReport
{
    /// <summary>
    /// Coverage format (cobertura, jacoco, etc.)
    /// </summary>
    [YamlMember(Alias = "coverage_format")]
    public string? CoverageFormat { get; set; }

    /// <summary>
    /// Path to coverage report file
    /// </summary>
    [YamlMember(Alias = "path")]
    public string? Path { get; set; }
}

/// <summary>
/// Represents job cache configuration
/// </summary>
public class JobCache
{
    /// <summary>
    /// Cache key
    /// </summary>
    [YamlMember(Alias = "key")]
    public string? Key { get; set; }

    /// <summary>
    /// Paths to cache
    /// </summary>
    [YamlMember(Alias = "paths")]
    public List<string>? Paths { get; set; }

    /// <summary>
    /// Cache policy (pull, push, pull-push)
    /// </summary>
    [YamlMember(Alias = "policy")]
    public string? Policy { get; set; }

    /// <summary>
    /// When to cache (on_success, on_failure, always)
    /// </summary>
    [YamlMember(Alias = "when")]
    public string? When { get; set; }
}

/// <summary>
/// Represents a job dependency with needs
/// </summary>
public class JobNeed
{
    /// <summary>
    /// Job name that this job needs
    /// </summary>
    [YamlMember(Alias = "job")]
    public string? JobName { get; set; }

    /// <summary>
    /// Project containing the needed job
    /// </summary>
    [YamlMember(Alias = "project")]
    public string? Project { get; set; }

    /// <summary>
    /// Reference (branch, tag) for the needed job
    /// </summary>
    [YamlMember(Alias = "ref")]
    public string? Ref { get; set; }

    /// <summary>
    /// Artifacts to download from the needed job
    /// </summary>
    [YamlMember(Alias = "artifacts")]
    public bool? Artifacts { get; set; }

    /// <summary>
    /// Whether this need is optional
    /// </summary>
    [YamlMember(Alias = "optional")]
    public bool? Optional { get; set; }
}

/// <summary>
/// Represents job retry configuration
/// </summary>
public class JobRetry
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    [YamlMember(Alias = "max")]
    public int? Max { get; set; }

    /// <summary>
    /// When to retry (always, unknown_failure, script_failure, etc.)
    /// </summary>
    [YamlMember(Alias = "when")]
    public List<string>? When { get; set; }
}

/// <summary>
/// Represents job condition for only/except (legacy)
/// </summary>
public class JobCondition
{
    /// <summary>
    /// Branches to include/exclude
    /// </summary>
    [YamlMember(Alias = "refs")]
    public List<string>? Refs { get; set; }

    /// <summary>
    /// Variables conditions
    /// </summary>
    [YamlMember(Alias = "variables")]
    public List<string>? Variables { get; set; }

    /// <summary>
    /// Changes conditions
    /// </summary>
    [YamlMember(Alias = "changes")]
    public List<string>? Changes { get; set; }

    /// <summary>
    /// Kubernetes conditions
    /// </summary>
    [YamlMember(Alias = "kubernetes")]
    public string? Kubernetes { get; set; }
}

/// <summary>
/// Represents Docker image configuration
/// </summary>
public class JobImage
{
    /// <summary>
    /// Docker image name
    /// </summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Docker image entrypoint
    /// </summary>
    [YamlMember(Alias = "entrypoint")]
    public List<string>? Entrypoint { get; set; }

    /// <summary>
    /// Docker pull policy
    /// </summary>
    [YamlMember(Alias = "pull_policy")]
    public string? PullPolicy { get; set; }
}

/// <summary>
/// Represents a service for the job
/// </summary>
public class JobService
{
    /// <summary>
    /// Service image name
    /// </summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Service alias
    /// </summary>
    [YamlMember(Alias = "alias")]
    public string? Alias { get; set; }

    /// <summary>
    /// Service entrypoint
    /// </summary>
    [YamlMember(Alias = "entrypoint")]
    public List<string>? Entrypoint { get; set; }

    /// <summary>
    /// Service command
    /// </summary>
    [YamlMember(Alias = "command")]
    public List<string>? Command { get; set; }

    /// <summary>
    /// Service variables
    /// </summary>
    [YamlMember(Alias = "variables")]
    public Dictionary<string, object>? Variables { get; set; }
}

/// <summary>
/// Represents job environment configuration
/// </summary>
public class JobEnvironment
{
    /// <summary>
    /// Environment name
    /// </summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Environment URL
    /// </summary>
    [YamlMember(Alias = "url")]
    public string? Url { get; set; }

    /// <summary>
    /// Environment action (start, prepare, stop)
    /// </summary>
    [YamlMember(Alias = "action")]
    public string? Action { get; set; }

    /// <summary>
    /// Auto stop in duration
    /// </summary>
    [YamlMember(Alias = "auto_stop_in")]
    public string? AutoStopIn { get; set; }

    /// <summary>
    /// Kubernetes configuration
    /// </summary>
    [YamlMember(Alias = "kubernetes")]
    public EnvironmentKubernetes? Kubernetes { get; set; }

    /// <summary>
    /// Deployment tier
    /// </summary>
    [YamlMember(Alias = "deployment_tier")]
    public string? DeploymentTier { get; set; }
}

/// <summary>
/// Represents Kubernetes environment configuration
/// </summary>
public class EnvironmentKubernetes
{
    /// <summary>
    /// Kubernetes namespace
    /// </summary>
    [YamlMember(Alias = "namespace")]
    public string? Namespace { get; set; }
}

/// <summary>
/// Represents parallel job configuration
/// </summary>
public class JobParallel
{
    /// <summary>
    /// Matrix configuration for parallel jobs
    /// </summary>
    [YamlMember(Alias = "matrix")]
    public List<Dictionary<string, List<object>>>? Matrix { get; set; }
}

/// <summary>
/// Represents release configuration
/// </summary>
public class JobRelease
{
    /// <summary>
    /// Release tag name
    /// </summary>
    [YamlMember(Alias = "tag_name")]
    public string? TagName { get; set; }

    /// <summary>
    /// Release name
    /// </summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Release description
    /// </summary>
    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// Reference for the release
    /// </summary>
    [YamlMember(Alias = "ref")]
    public string? Ref { get; set; }

    /// <summary>
    /// Milestones for the release
    /// </summary>
    [YamlMember(Alias = "milestones")]
    public List<string>? Milestones { get; set; }

    /// <summary>
    /// Release date
    /// </summary>
    [YamlMember(Alias = "released_at")]
    public string? ReleasedAt { get; set; }

    /// <summary>
    /// Release assets
    /// </summary>
    [YamlMember(Alias = "assets")]
    public ReleaseAssets? Assets { get; set; }
}

/// <summary>
/// Represents release assets
/// </summary>
public class ReleaseAssets
{
    /// <summary>
    /// Asset links
    /// </summary>
    [YamlMember(Alias = "links")]
    public List<ReleaseAssetLink>? Links { get; set; }
}

/// <summary>
/// Represents a release asset link
/// </summary>
public class ReleaseAssetLink
{
    /// <summary>
    /// Asset name
    /// </summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Asset URL
    /// </summary>
    [YamlMember(Alias = "url")]
    public string? Url { get; set; }

    /// <summary>
    /// Asset link type
    /// </summary>
    [YamlMember(Alias = "link_type")]
    public string? LinkType { get; set; }

    /// <summary>
    /// Asset file path
    /// </summary>
    [YamlMember(Alias = "filepath")]
    public string? FilePath { get; set; }
}