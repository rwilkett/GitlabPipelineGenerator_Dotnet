using System.ComponentModel.DataAnnotations;

namespace GitlabPipelineGenerator.Core.Models;

/// <summary>
/// Represents customization options for pipeline templates
/// </summary>
public class TemplateCustomization
{
    /// <summary>
    /// Name of the template to customize
    /// </summary>
    [Required(ErrorMessage = "Template name is required")]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Stages to enable or disable
    /// </summary>
    public StageCustomization Stages { get; set; } = new();

    /// <summary>
    /// Environment-specific customizations
    /// </summary>
    public Dictionary<string, EnvironmentCustomization> Environments { get; set; } = new();

    /// <summary>
    /// Template parameter substitutions
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>
    /// Feature toggles for optional functionality
    /// </summary>
    public FeatureToggles Features { get; set; } = new();

    /// <summary>
    /// Custom job overrides and additions
    /// </summary>
    public List<JobCustomization> JobCustomizations { get; set; } = new();

    /// <summary>
    /// Workflow customizations
    /// </summary>
    public WorkflowCustomization? Workflow { get; set; }

    /// <summary>
    /// Validates the template customization
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            errors.Add("Template name is required");
        }

        // Validate stages
        errors.AddRange(Stages.Validate());

        // Validate environments
        foreach (var env in Environments)
        {
            var envErrors = env.Value.Validate();
            errors.AddRange(envErrors.Select(e => $"Environment '{env.Key}': {e}"));
        }

        // Validate job customizations
        foreach (var jobCustomization in JobCustomizations)
        {
            var jobErrors = jobCustomization.Validate();
            errors.AddRange(jobErrors);
        }

        // Validate workflow customization
        if (Workflow != null)
        {
            var workflowErrors = Workflow.Validate();
            errors.AddRange(workflowErrors);
        }

        return errors;
    }
}

/// <summary>
/// Represents stage-level customizations
/// </summary>
public class StageCustomization
{
    /// <summary>
    /// Stages to explicitly enable
    /// </summary>
    public List<string> Enable { get; set; } = new();

    /// <summary>
    /// Stages to explicitly disable
    /// </summary>
    public List<string> Disable { get; set; } = new();

    /// <summary>
    /// Custom stage ordering
    /// </summary>
    public List<string>? CustomOrder { get; set; }

    /// <summary>
    /// Stage-specific conditions
    /// </summary>
    public Dictionary<string, StageCondition> Conditions { get; set; } = new();

    /// <summary>
    /// Validates the stage customization
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // Check for conflicts between enable and disable
        var conflicts = Enable.Intersect(Disable, StringComparer.OrdinalIgnoreCase).ToList();
        if (conflicts.Any())
        {
            errors.Add($"Stages cannot be both enabled and disabled: {string.Join(", ", conflicts)}");
        }

        // Validate stage conditions
        foreach (var condition in Conditions)
        {
            var conditionErrors = condition.Value.Validate();
            errors.AddRange(conditionErrors.Select(e => $"Stage '{condition.Key}': {e}"));
        }

        return errors;
    }
}

/// <summary>
/// Represents conditions for stage execution
/// </summary>
public class StageCondition
{
    /// <summary>
    /// Branch patterns for when the stage should run
    /// </summary>
    public List<string> OnBranches { get; set; } = new();

    /// <summary>
    /// Branch patterns for when the stage should not run
    /// </summary>
    public List<string> ExceptBranches { get; set; } = new();

    /// <summary>
    /// Variable conditions
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>
    /// File change patterns that trigger the stage
    /// </summary>
    public List<string> OnChanges { get; set; } = new();

    /// <summary>
    /// Whether the stage should run manually
    /// </summary>
    public bool Manual { get; set; } = false;

    /// <summary>
    /// Whether the stage can fail without failing the pipeline
    /// </summary>
    public bool AllowFailure { get; set; } = false;

    /// <summary>
    /// Validates the stage condition
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // Check for conflicts between onBranches and exceptBranches
        var conflicts = OnBranches.Intersect(ExceptBranches, StringComparer.OrdinalIgnoreCase).ToList();
        if (conflicts.Any())
        {
            errors.Add($"Branches cannot be in both onBranches and exceptBranches: {string.Join(", ", conflicts)}");
        }

        return errors;
    }
}

/// <summary>
/// Represents environment-specific customizations
/// </summary>
public class EnvironmentCustomization
{
    /// <summary>
    /// Environment-specific variables
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>
    /// Deployment strategy for this environment
    /// </summary>
    public string? DeploymentStrategy { get; set; }

    /// <summary>
    /// Whether deployment to this environment is manual
    /// </summary>
    public bool ManualDeployment { get; set; } = false;

    /// <summary>
    /// Auto-deployment branch patterns
    /// </summary>
    public List<string> AutoDeployBranches { get; set; } = new();

    /// <summary>
    /// Environment-specific Docker image
    /// </summary>
    public string? DockerImage { get; set; }

    /// <summary>
    /// Environment-specific runner tags
    /// </summary>
    public List<string> RunnerTags { get; set; } = new();

    /// <summary>
    /// Health check configuration
    /// </summary>
    public HealthCheckConfig? HealthCheck { get; set; }

    /// <summary>
    /// Rollback configuration
    /// </summary>
    public RollbackConfig? Rollback { get; set; }

    /// <summary>
    /// Validates the environment customization
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // Validate deployment strategy
        if (!string.IsNullOrEmpty(DeploymentStrategy))
        {
            var validStrategies = new[] { "rolling", "blue-green", "canary", "recreate" };
            if (!validStrategies.Contains(DeploymentStrategy, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"Invalid deployment strategy '{DeploymentStrategy}'. Valid strategies: {string.Join(", ", validStrategies)}");
            }
        }

        // Validate health check configuration
        if (HealthCheck != null)
        {
            var healthCheckErrors = HealthCheck.Validate();
            errors.AddRange(healthCheckErrors);
        }

        // Validate rollback configuration
        if (Rollback != null)
        {
            var rollbackErrors = Rollback.Validate();
            errors.AddRange(rollbackErrors);
        }

        return errors;
    }
}

/// <summary>
/// Represents health check configuration
/// </summary>
public class HealthCheckConfig
{
    /// <summary>
    /// Health check endpoint URL
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Timeout for health check in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Expected HTTP status codes for success
    /// </summary>
    public List<int> ExpectedStatusCodes { get; set; } = new() { 200 };

    /// <summary>
    /// Validates the health check configuration
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (TimeoutSeconds <= 0)
        {
            errors.Add("Health check timeout must be greater than 0");
        }

        if (RetryAttempts < 0)
        {
            errors.Add("Health check retry attempts cannot be negative");
        }

        if (RetryDelaySeconds < 0)
        {
            errors.Add("Health check retry delay cannot be negative");
        }

        if (!ExpectedStatusCodes.Any())
        {
            errors.Add("At least one expected status code must be specified");
        }

        return errors;
    }
}

/// <summary>
/// Represents rollback configuration
/// </summary>
public class RollbackConfig
{
    /// <summary>
    /// Whether automatic rollback is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Conditions that trigger automatic rollback
    /// </summary>
    public List<string> TriggerConditions { get; set; } = new();

    /// <summary>
    /// Rollback strategy
    /// </summary>
    public string Strategy { get; set; } = "previous-version";

    /// <summary>
    /// Timeout for rollback operation in minutes
    /// </summary>
    public int TimeoutMinutes { get; set; } = 10;

    /// <summary>
    /// Validates the rollback configuration
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (TimeoutMinutes <= 0)
        {
            errors.Add("Rollback timeout must be greater than 0");
        }

        var validStrategies = new[] { "previous-version", "snapshot", "manual" };
        if (!validStrategies.Contains(Strategy, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Invalid rollback strategy '{Strategy}'. Valid strategies: {string.Join(", ", validStrategies)}");
        }

        return errors;
    }
}

/// <summary>
/// Represents feature toggles for optional functionality
/// </summary>
public class FeatureToggles
{
    /// <summary>
    /// Enable parallel job execution where possible
    /// </summary>
    public bool EnableParallelExecution { get; set; } = true;

    /// <summary>
    /// Enable caching optimizations
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Enable artifact compression
    /// </summary>
    public bool EnableArtifactCompression { get; set; } = true;

    /// <summary>
    /// Enable job retry on failure
    /// </summary>
    public bool EnableJobRetry { get; set; } = true;

    /// <summary>
    /// Enable notification on pipeline events
    /// </summary>
    public bool EnableNotifications { get; set; } = false;

    /// <summary>
    /// Enable resource usage monitoring
    /// </summary>
    public bool EnableResourceMonitoring { get; set; } = false;

    /// <summary>
    /// Enable pipeline metrics collection
    /// </summary>
    public bool EnableMetricsCollection { get; set; } = false;
}

/// <summary>
/// Represents job-level customizations
/// </summary>
public class JobCustomization
{
    /// <summary>
    /// Name of the job to customize
    /// </summary>
    [Required(ErrorMessage = "Job name is required")]
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Action to perform (override, extend, disable)
    /// </summary>
    [Required(ErrorMessage = "Action is required")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Custom script commands (for override or extend actions)
    /// </summary>
    public List<string> Script { get; set; } = new();

    /// <summary>
    /// Custom variables for the job
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>
    /// Custom Docker image for the job
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// Custom runner tags for the job
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Custom timeout for the job
    /// </summary>
    public string? Timeout { get; set; }

    /// <summary>
    /// Whether the job can fail without failing the pipeline
    /// </summary>
    public bool? AllowFailure { get; set; }

    /// <summary>
    /// When to run the job
    /// </summary>
    public string? When { get; set; }

    /// <summary>
    /// Validates the job customization
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(JobName))
        {
            errors.Add("Job name is required");
        }

        var validActions = new[] { "override", "extend", "disable" };
        if (!validActions.Contains(Action, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Invalid action '{Action}'. Valid actions: {string.Join(", ", validActions)}");
        }

        if (Action.Equals("override", StringComparison.OrdinalIgnoreCase) && !Script.Any())
        {
            errors.Add("Script is required when action is 'override'");
        }

        return errors;
    }
}

/// <summary>
/// Represents workflow-level customizations
/// </summary>
public class WorkflowCustomization
{
    /// <summary>
    /// Custom workflow rules
    /// </summary>
    public List<WorkflowRule> Rules { get; set; } = new();

    /// <summary>
    /// Auto-cancel redundant pipelines
    /// </summary>
    public bool AutoCancelRedundantPipelines { get; set; } = true;

    /// <summary>
    /// Pipeline timeout in minutes
    /// </summary>
    public int? TimeoutMinutes { get; set; }

    /// <summary>
    /// Validates the workflow customization
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (TimeoutMinutes.HasValue && TimeoutMinutes.Value <= 0)
        {
            errors.Add("Pipeline timeout must be greater than 0");
        }

        foreach (var rule in Rules)
        {
            var ruleErrors = rule.Validate();
            errors.AddRange(ruleErrors);
        }

        return errors;
    }
}

/// <summary>
/// Represents a workflow rule
/// </summary>
public class WorkflowRule
{
    /// <summary>
    /// Condition for when the rule applies
    /// </summary>
    public string? If { get; set; }

    /// <summary>
    /// When to run the pipeline
    /// </summary>
    public string? When { get; set; }

    /// <summary>
    /// Variables to set when the rule matches
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>
    /// Validates the workflow rule
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (!string.IsNullOrEmpty(When))
        {
            var validWhenValues = new[] { "always", "never", "on_success", "on_failure", "manual", "delayed" };
            if (!validWhenValues.Contains(When, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"Invalid 'when' value '{When}'. Valid values: {string.Join(", ", validWhenValues)}");
            }
        }

        return errors;
    }
}