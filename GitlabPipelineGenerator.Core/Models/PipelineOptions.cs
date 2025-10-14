using System.ComponentModel.DataAnnotations;

namespace GitlabPipelineGenerator.Core.Models;

/// <summary>
/// Represents configuration options for pipeline generation
/// </summary>
public class PipelineOptions
{
    /// <summary>
    /// Type of project (e.g., "dotnet", "nodejs", "python", "docker")
    /// </summary>
    [Required(ErrorMessage = "Project type is required")]
    public string ProjectType { get; set; } = string.Empty;

    /// <summary>
    /// List of stages to include in the pipeline
    /// </summary>
    [Required(ErrorMessage = "At least one stage is required")]
    [MinLength(1, ErrorMessage = "At least one stage must be specified")]
    public List<string> Stages { get; set; } = new() { "build", "test", "deploy" };

    /// <summary>
    /// .NET version to use (e.g., "8.0", "9.0")
    /// </summary>
    public string? DotNetVersion { get; set; }

    /// <summary>
    /// Whether to include test jobs in the pipeline
    /// </summary>
    public bool IncludeTests { get; set; } = true;

    /// <summary>
    /// Whether to include deployment jobs in the pipeline
    /// </summary>
    public bool IncludeDeployment { get; set; } = true;

    /// <summary>
    /// Custom variables to include in the pipeline
    /// </summary>
    public Dictionary<string, string> CustomVariables { get; set; } = new();

    /// <summary>
    /// Docker image to use for jobs (if not specified, uses default for project type)
    /// </summary>
    public string? DockerImage { get; set; }

    /// <summary>
    /// Tags to use for runner selection
    /// </summary>
    public List<string> RunnerTags { get; set; } = new();

    /// <summary>
    /// Whether to include code quality checks
    /// </summary>
    public bool IncludeCodeQuality { get; set; } = false;

    /// <summary>
    /// Whether to include security scanning
    /// </summary>
    public bool IncludeSecurity { get; set; } = false;

    /// <summary>
    /// Whether to include performance testing
    /// </summary>
    public bool IncludePerformance { get; set; } = false;

    /// <summary>
    /// Deployment environments configuration
    /// </summary>
    public List<DeploymentEnvironment> DeploymentEnvironments { get; set; } = new();

    /// <summary>
    /// Cache configuration options
    /// </summary>
    public CacheOptions? Cache { get; set; }

    /// <summary>
    /// Artifact configuration options
    /// </summary>
    public ArtifactOptions? Artifacts { get; set; }

    /// <summary>
    /// Notification settings
    /// </summary>
    public NotificationOptions? Notifications { get; set; }

    /// <summary>
    /// Custom job configurations
    /// </summary>
    public List<CustomJobOptions> CustomJobs { get; set; } = new();

    /// <summary>
    /// Validates the pipeline options and returns validation errors
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // Validate project type
        var validProjectTypes = new[] { "dotnet", "nodejs", "python", "docker", "generic" };
        if (!validProjectTypes.Contains(ProjectType.ToLowerInvariant()))
        {
            errors.Add($"Invalid project type '{ProjectType}'. Valid types are: {string.Join(", ", validProjectTypes)}");
        }

        // Validate stages
        if (!Stages.Any())
        {
            errors.Add("At least one stage must be specified");
        }

        var validStages = new[] { "build", "test", "deploy", "review", "staging", "production", "cleanup" };
        foreach (var stage in Stages)
        {
            if (string.IsNullOrWhiteSpace(stage))
            {
                errors.Add("Stage names cannot be empty or whitespace");
            }
        }

        // Validate .NET version if specified
        if (!string.IsNullOrEmpty(DotNetVersion))
        {
            var validVersions = new[] { "6.0", "7.0", "8.0", "9.0" };
            if (!validVersions.Contains(DotNetVersion))
            {
                errors.Add($"Invalid .NET version '{DotNetVersion}'. Valid versions are: {string.Join(", ", validVersions)}");
            }
        }

        // Validate custom variables
        foreach (var variable in CustomVariables)
        {
            if (string.IsNullOrWhiteSpace(variable.Key))
            {
                errors.Add("Variable names cannot be empty or whitespace");
            }
            if (variable.Key.Contains(" "))
            {
                errors.Add($"Variable name '{variable.Key}' cannot contain spaces");
            }
        }

        // Validate deployment environments
        foreach (var env in DeploymentEnvironments)
        {
            var envErrors = env.Validate();
            errors.AddRange(envErrors);
        }

        // Validate custom jobs
        foreach (var job in CustomJobs)
        {
            var jobErrors = job.Validate();
            errors.AddRange(jobErrors);
        }

        return errors;
    }
}

/// <summary>
/// Represents a deployment environment configuration
/// </summary>
public class DeploymentEnvironment
{
    /// <summary>
    /// Environment name (e.g., "staging", "production")
    /// </summary>
    [Required(ErrorMessage = "Environment name is required")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Environment URL
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Whether deployment to this environment is manual
    /// </summary>
    public bool IsManual { get; set; } = false;

    /// <summary>
    /// Branch or tag pattern for automatic deployment
    /// </summary>
    public string? AutoDeployPattern { get; set; }

    /// <summary>
    /// Environment-specific variables
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>
    /// Kubernetes namespace for this environment
    /// </summary>
    public string? KubernetesNamespace { get; set; }

    /// <summary>
    /// Whether this environment should auto-stop
    /// </summary>
    public bool AutoStop { get; set; } = false;

    /// <summary>
    /// Auto-stop duration (e.g., "1 day", "2 weeks")
    /// </summary>
    public string? AutoStopIn { get; set; }

    /// <summary>
    /// Validates the deployment environment configuration
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Environment name is required");
        }

        if (!string.IsNullOrEmpty(Url))
        {
            if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri))
            {
                errors.Add($"Invalid URL format for environment '{Name}': {Url}");
            }
            else if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                errors.Add($"Invalid URL format for environment '{Name}': {Url}");
            }
        }

        if (AutoStop && string.IsNullOrEmpty(AutoStopIn))
        {
            errors.Add($"AutoStopIn is required when AutoStop is enabled for environment '{Name}'");
        }

        return errors;
    }
}

/// <summary>
/// Represents cache configuration options
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Cache key pattern
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Paths to cache
    /// </summary>
    public List<string> Paths { get; set; } = new();

    /// <summary>
    /// Cache policy (pull, push, pull-push)
    /// </summary>
    public string Policy { get; set; } = "pull-push";

    /// <summary>
    /// When to cache (on_success, on_failure, always)
    /// </summary>
    public string When { get; set; } = "on_success";
}

/// <summary>
/// Represents artifact configuration options
/// </summary>
public class ArtifactOptions
{
    /// <summary>
    /// Default paths to include as artifacts
    /// </summary>
    public List<string> DefaultPaths { get; set; } = new();

    /// <summary>
    /// Default expiration time for artifacts
    /// </summary>
    public string DefaultExpireIn { get; set; } = "1 week";

    /// <summary>
    /// Whether to include test reports by default
    /// </summary>
    public bool IncludeTestReports { get; set; } = true;

    /// <summary>
    /// Whether to include coverage reports by default
    /// </summary>
    public bool IncludeCoverageReports { get; set; } = true;
}

/// <summary>
/// Represents notification configuration options
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// Email addresses to notify on pipeline failure
    /// </summary>
    public List<string> EmailOnFailure { get; set; } = new();

    /// <summary>
    /// Email addresses to notify on pipeline success
    /// </summary>
    public List<string> EmailOnSuccess { get; set; } = new();

    /// <summary>
    /// Slack webhook URL for notifications
    /// </summary>
    public string? SlackWebhook { get; set; }

    /// <summary>
    /// Teams webhook URL for notifications
    /// </summary>
    public string? TeamsWebhook { get; set; }
}

/// <summary>
/// Represents custom job configuration options
/// </summary>
public class CustomJobOptions
{
    /// <summary>
    /// Job name
    /// </summary>
    [Required(ErrorMessage = "Job name is required")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Stage for the job
    /// </summary>
    [Required(ErrorMessage = "Job stage is required")]
    public string Stage { get; set; } = string.Empty;

    /// <summary>
    /// Script commands for the job
    /// </summary>
    [Required(ErrorMessage = "Job script is required")]
    [MinLength(1, ErrorMessage = "At least one script command is required")]
    public List<string> Script { get; set; } = new();

    /// <summary>
    /// Before script commands
    /// </summary>
    public List<string> BeforeScript { get; set; } = new();

    /// <summary>
    /// After script commands
    /// </summary>
    public List<string> AfterScript { get; set; } = new();

    /// <summary>
    /// Job-specific variables
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>
    /// When to run the job
    /// </summary>
    public string? When { get; set; }

    /// <summary>
    /// Whether the job can fail without failing the pipeline
    /// </summary>
    public bool AllowFailure { get; set; } = false;

    /// <summary>
    /// Docker image for the job
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// Tags for runner selection
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Validates the custom job configuration
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Job name is required");
        }

        if (string.IsNullOrWhiteSpace(Stage))
        {
            errors.Add($"Stage is required for job '{Name}'");
        }

        if (!Script.Any() || Script.All(string.IsNullOrWhiteSpace))
        {
            errors.Add($"At least one script command is required for job '{Name}'");
        }

        return errors;
    }
}