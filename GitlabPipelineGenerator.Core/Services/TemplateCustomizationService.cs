using System.Text.RegularExpressions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Service for applying template customizations to pipeline configurations
/// </summary>
public class TemplateCustomizationService : ITemplateCustomizationService
{
    private static readonly Regex ParameterRegex = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    private static readonly Dictionary<string, Dictionary<string, string>> TemplateParameters = new()
    {
        ["dotnet-standard"] = new Dictionary<string, string>
        {
            ["PROJECT_NAME"] = "Name of the project",
            ["SOLUTION_FILE"] = "Path to the solution file",
            ["TEST_PROJECT"] = "Path to the test project",
            ["PUBLISH_PATH"] = "Path for publishing artifacts",
            ["DOCKER_REGISTRY"] = "Docker registry URL",
            ["DEPLOYMENT_ENVIRONMENT"] = "Target deployment environment",
            ["HEALTH_CHECK_URL"] = "URL for health checks",
            ["DATABASE_CONNECTION"] = "Database connection string variable name"
        }
    };

    /// <summary>
    /// Applies customizations to pipeline options
    /// </summary>
    /// <param name="options">Original pipeline options</param>
    /// <param name="customization">Customizations to apply</param>
    /// <returns>Customized pipeline options</returns>
    public async Task<PipelineOptions> ApplyCustomizationsAsync(PipelineOptions options, TemplateCustomization customization)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        
        if (customization == null)
            throw new ArgumentNullException(nameof(customization));

        // Validate customizations
        var validationErrors = ValidateCustomizations(options, customization);
        if (validationErrors.Any())
        {
            throw new ArgumentException($"Invalid customizations: {string.Join(", ", validationErrors)}");
        }

        // Create a copy of the options to avoid modifying the original
        var customizedOptions = ClonePipelineOptions(options);

        // Apply stage customizations
        await ApplyStageCustomizationsAsync(customizedOptions, customization.Stages);

        // Apply environment customizations
        await ApplyEnvironmentCustomizationsAsync(customizedOptions, customization.Environments);

        // Apply feature toggles
        ApplyFeatureToggles(customizedOptions, customization.Features);

        // Apply parameter substitutions
        ApplyParameterSubstitutions(customizedOptions, customization.Parameters);

        return customizedOptions;
    }

    /// <summary>
    /// Applies customizations to a generated pipeline configuration
    /// </summary>
    /// <param name="pipeline">Original pipeline configuration</param>
    /// <param name="customization">Customizations to apply</param>
    /// <returns>Customized pipeline configuration</returns>
    public async Task<PipelineConfiguration> ApplyPipelineCustomizationsAsync(PipelineConfiguration pipeline, TemplateCustomization customization)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));
        
        if (customization == null)
            throw new ArgumentNullException(nameof(customization));

        // Apply job customizations
        await ApplyJobCustomizationsAsync(pipeline, customization.JobCustomizations);

        // Apply workflow customizations
        if (customization.Workflow != null)
        {
            ApplyWorkflowCustomizations(pipeline, customization.Workflow);
        }

        // Apply parameter substitutions to the entire pipeline
        ApplyParameterSubstitutionsToPipeline(pipeline, customization.Parameters);

        return pipeline;
    }

    /// <summary>
    /// Validates that customizations are compatible with the pipeline options
    /// </summary>
    /// <param name="options">Pipeline options</param>
    /// <param name="customization">Customizations to validate</param>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> ValidateCustomizations(PipelineOptions options, TemplateCustomization customization)
    {
        var errors = new List<string>();

        if (options == null)
        {
            errors.Add("Pipeline options cannot be null");
            return errors;
        }

        if (customization == null)
        {
            errors.Add("Template customization cannot be null");
            return errors;
        }

        // Validate the customization itself
        errors.AddRange(customization.Validate());

        // Validate stage customizations against available stages
        var availableStages = options.Stages.ToList();
        foreach (var enabledStage in customization.Stages.Enable)
        {
            if (!availableStages.Contains(enabledStage, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"Cannot enable stage '{enabledStage}' - not available in pipeline options");
            }
        }

        foreach (var disabledStage in customization.Stages.Disable)
        {
            if (!availableStages.Contains(disabledStage, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"Cannot disable stage '{disabledStage}' - not available in pipeline options");
            }
        }

        // Validate environment customizations
        foreach (var envCustomization in customization.Environments)
        {
            var existingEnv = options.DeploymentEnvironments.FirstOrDefault(e => 
                e.Name.Equals(envCustomization.Key, StringComparison.OrdinalIgnoreCase));
            
            if (existingEnv == null)
            {
                errors.Add($"Environment '{envCustomization.Key}' not found in pipeline options");
            }
        }

        return errors;
    }

    /// <summary>
    /// Substitutes template parameters in a string value
    /// </summary>
    /// <param name="value">String value containing parameter placeholders</param>
    /// <param name="parameters">Parameter values for substitution</param>
    /// <returns>String with parameters substituted</returns>
    public string SubstituteParameters(string value, Dictionary<string, string> parameters)
    {
        if (string.IsNullOrEmpty(value) || !parameters.Any())
            return value;

        return ParameterRegex.Replace(value, match =>
        {
            var parameterName = match.Groups[1].Value;
            return parameters.TryGetValue(parameterName, out var parameterValue) 
                ? parameterValue 
                : match.Value; // Keep original if parameter not found
        });
    }

    /// <summary>
    /// Gets available parameters for a specific template
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <returns>Dictionary of parameter names and their descriptions</returns>
    public Dictionary<string, string> GetAvailableParameters(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return new Dictionary<string, string>();

        return TemplateParameters.TryGetValue(templateName, out var parameters) 
            ? new Dictionary<string, string>(parameters) 
            : new Dictionary<string, string>();
    }

    #region Private Helper Methods

    private static PipelineOptions ClonePipelineOptions(PipelineOptions original)
    {
        return new PipelineOptions
        {
            ProjectType = original.ProjectType,
            DotNetVersion = original.DotNetVersion,
            Stages = new List<string>(original.Stages),
            IncludeTests = original.IncludeTests,
            IncludeDeployment = original.IncludeDeployment,
            IncludeCodeQuality = original.IncludeCodeQuality,
            IncludeSecurity = original.IncludeSecurity,
            IncludePerformance = original.IncludePerformance,
            DockerImage = original.DockerImage,
            RunnerTags = new List<string>(original.RunnerTags),
            CustomVariables = new Dictionary<string, string>(original.CustomVariables),
            DeploymentEnvironments = original.DeploymentEnvironments.Select(e => new DeploymentEnvironment
            {
                Name = e.Name,
                Url = e.Url,
                IsManual = e.IsManual,
                AutoDeployPattern = e.AutoDeployPattern,
                Variables = new Dictionary<string, string>(e.Variables),
                KubernetesNamespace = e.KubernetesNamespace,
                AutoStop = e.AutoStop,
                AutoStopIn = e.AutoStopIn
            }).ToList(),
            Cache = original.Cache == null ? null : new CacheOptions
            {
                Key = original.Cache.Key,
                Paths = new List<string>(original.Cache.Paths),
                Policy = original.Cache.Policy,
                When = original.Cache.When
            },
            Artifacts = original.Artifacts == null ? null : new ArtifactOptions
            {
                DefaultPaths = new List<string>(original.Artifacts.DefaultPaths),
                DefaultExpireIn = original.Artifacts.DefaultExpireIn,
                IncludeTestReports = original.Artifacts.IncludeTestReports,
                IncludeCoverageReports = original.Artifacts.IncludeCoverageReports
            },
            Notifications = original.Notifications == null ? null : new NotificationOptions
            {
                EmailOnFailure = new List<string>(original.Notifications.EmailOnFailure),
                EmailOnSuccess = new List<string>(original.Notifications.EmailOnSuccess),
                SlackWebhook = original.Notifications.SlackWebhook,
                TeamsWebhook = original.Notifications.TeamsWebhook
            },
            CustomJobs = original.CustomJobs.Select(j => new CustomJobOptions
            {
                Name = j.Name,
                Stage = j.Stage,
                Script = new List<string>(j.Script),
                BeforeScript = new List<string>(j.BeforeScript),
                AfterScript = new List<string>(j.AfterScript),
                Variables = new Dictionary<string, string>(j.Variables),
                When = j.When,
                AllowFailure = j.AllowFailure,
                Image = j.Image,
                Tags = new List<string>(j.Tags)
            }).ToList()
        };
    }

    private async Task ApplyStageCustomizationsAsync(PipelineOptions options, StageCustomization stageCustomization)
    {
        // Remove disabled stages
        foreach (var disabledStage in stageCustomization.Disable)
        {
            options.Stages.RemoveAll(s => s.Equals(disabledStage, StringComparison.OrdinalIgnoreCase));
        }

        // Add enabled stages
        foreach (var enabledStage in stageCustomization.Enable)
        {
            if (!options.Stages.Contains(enabledStage, StringComparer.OrdinalIgnoreCase))
            {
                options.Stages.Add(enabledStage);
            }
        }

        // Apply custom stage ordering if specified
        if (stageCustomization.CustomOrder != null && stageCustomization.CustomOrder.Any())
        {
            var reorderedStages = new List<string>();
            
            // Add stages in custom order
            foreach (var stage in stageCustomization.CustomOrder)
            {
                if (options.Stages.Contains(stage, StringComparer.OrdinalIgnoreCase))
                {
                    reorderedStages.Add(stage);
                }
            }

            // Add any remaining stages that weren't in the custom order
            foreach (var stage in options.Stages)
            {
                if (!reorderedStages.Contains(stage, StringComparer.OrdinalIgnoreCase))
                {
                    reorderedStages.Add(stage);
                }
            }

            options.Stages = reorderedStages;
        }

        await Task.CompletedTask;
    }

    private async Task ApplyEnvironmentCustomizationsAsync(PipelineOptions options, Dictionary<string, EnvironmentCustomization> environmentCustomizations)
    {
        foreach (var envCustomization in environmentCustomizations)
        {
            var environment = options.DeploymentEnvironments.FirstOrDefault(e => 
                e.Name.Equals(envCustomization.Key, StringComparison.OrdinalIgnoreCase));

            if (environment != null)
            {
                // Apply environment-specific customizations
                foreach (var variable in envCustomization.Value.Variables)
                {
                    environment.Variables[variable.Key] = variable.Value;
                }

                if (envCustomization.Value.ManualDeployment)
                {
                    environment.IsManual = true;
                }

                if (envCustomization.Value.AutoDeployBranches.Any())
                {
                    environment.AutoDeployPattern = string.Join("|", envCustomization.Value.AutoDeployBranches);
                }

                if (!string.IsNullOrEmpty(envCustomization.Value.DockerImage))
                {
                    // Store environment-specific Docker image in variables
                    environment.Variables["DOCKER_IMAGE"] = envCustomization.Value.DockerImage;
                }
            }
        }

        await Task.CompletedTask;
    }

    private void ApplyFeatureToggles(PipelineOptions options, FeatureToggles features)
    {
        // Apply caching settings
        if (!features.EnableCaching && options.Cache != null)
        {
            options.Cache = null;
        }

        // Apply notification settings
        if (features.EnableNotifications && options.Notifications == null)
        {
            options.Notifications = new NotificationOptions();
        }
        else if (!features.EnableNotifications)
        {
            options.Notifications = null;
        }

        // Apply artifact settings
        if (!features.EnableArtifactCompression && options.Artifacts != null)
        {
            // Add variable to disable compression
            options.CustomVariables["ARTIFACTS_COMPRESSION"] = "false";
        }

        // Apply retry settings
        if (!features.EnableJobRetry)
        {
            options.CustomVariables["JOB_RETRY_DISABLED"] = "true";
        }

        // Apply monitoring settings
        if (features.EnableResourceMonitoring)
        {
            options.CustomVariables["ENABLE_RESOURCE_MONITORING"] = "true";
        }

        if (features.EnableMetricsCollection)
        {
            options.CustomVariables["ENABLE_METRICS_COLLECTION"] = "true";
        }
    }

    private void ApplyParameterSubstitutions(PipelineOptions options, Dictionary<string, string> parameters)
    {
        if (!parameters.Any())
            return;

        // Substitute parameters in custom variables
        var substitutedVariables = new Dictionary<string, string>();
        foreach (var variable in options.CustomVariables)
        {
            substitutedVariables[variable.Key] = SubstituteParameters(variable.Value, parameters);
        }
        options.CustomVariables = substitutedVariables;

        // Substitute parameters in Docker image
        if (!string.IsNullOrEmpty(options.DockerImage))
        {
            options.DockerImage = SubstituteParameters(options.DockerImage, parameters);
        }

        // Substitute parameters in deployment environments
        foreach (var environment in options.DeploymentEnvironments)
        {
            if (!string.IsNullOrEmpty(environment.Url))
            {
                environment.Url = SubstituteParameters(environment.Url, parameters);
            }

            if (!string.IsNullOrEmpty(environment.KubernetesNamespace))
            {
                environment.KubernetesNamespace = SubstituteParameters(environment.KubernetesNamespace, parameters);
            }

            var substitutedEnvVariables = new Dictionary<string, string>();
            foreach (var variable in environment.Variables)
            {
                substitutedEnvVariables[variable.Key] = SubstituteParameters(variable.Value, parameters);
            }
            environment.Variables = substitutedEnvVariables;
        }
    }

    private async Task ApplyJobCustomizationsAsync(PipelineConfiguration pipeline, List<JobCustomization> jobCustomizations)
    {
        foreach (var jobCustomization in jobCustomizations)
        {
            switch (jobCustomization.Action.ToLowerInvariant())
            {
                case "disable":
                    pipeline.Jobs.Remove(jobCustomization.JobName);
                    break;

                case "override":
                    if (pipeline.Jobs.TryGetValue(jobCustomization.JobName, out var existingJob))
                    {
                        ApplyJobOverride(existingJob, jobCustomization);
                    }
                    break;

                case "extend":
                    if (pipeline.Jobs.TryGetValue(jobCustomization.JobName, out var jobToExtend))
                    {
                        ApplyJobExtension(jobToExtend, jobCustomization);
                    }
                    break;
            }
        }

        await Task.CompletedTask;
    }

    private void ApplyJobOverride(Job job, JobCustomization customization)
    {
        if (customization.Script.Any())
        {
            job.Script = new List<string>(customization.Script);
        }

        if (customization.Variables.Any())
        {
            job.Variables = customization.Variables.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
        }

        if (!string.IsNullOrEmpty(customization.Image))
        {
            job.Image = new JobImage { Name = customization.Image };
        }

        if (customization.Tags.Any())
        {
            job.Tags = new List<string>(customization.Tags);
        }

        if (!string.IsNullOrEmpty(customization.Timeout))
        {
            job.Timeout = customization.Timeout;
        }

        if (customization.AllowFailure.HasValue)
        {
            job.AllowFailure = customization.AllowFailure.Value;
        }

        if (!string.IsNullOrEmpty(customization.When))
        {
            job.When = customization.When;
        }
    }

    private void ApplyJobExtension(Job job, JobCustomization customization)
    {
        if (customization.Script.Any())
        {
            job.Script.AddRange(customization.Script);
        }

        if (customization.Variables.Any())
        {
            job.Variables ??= new Dictionary<string, object>();
            foreach (var variable in customization.Variables)
            {
                job.Variables[variable.Key] = variable.Value;
            }
        }

        if (customization.Tags.Any())
        {
            job.Tags ??= new List<string>();
            job.Tags.AddRange(customization.Tags);
        }
    }

    private void ApplyWorkflowCustomizations(PipelineConfiguration pipeline, WorkflowCustomization workflowCustomization)
    {
        pipeline.Workflow ??= new WorkflowRules();

        // Add custom workflow rules
        foreach (var customRule in workflowCustomization.Rules)
        {
            var rule = new Rule
            {
                If = customRule.If,
                When = customRule.When
            };

            pipeline.Workflow.Rules.Add(rule);

            // Add rule variables to global variables
            foreach (var variable in customRule.Variables)
            {
                pipeline.Variables[variable.Key] = variable.Value;
            }
        }

        // Apply auto-cancel setting
        if (workflowCustomization.AutoCancelRedundantPipelines)
        {
            pipeline.Variables["AUTO_CANCEL_REDUNDANT_PIPELINES"] = "true";
        }

        // Apply pipeline timeout
        if (workflowCustomization.TimeoutMinutes.HasValue)
        {
            pipeline.Variables["PIPELINE_TIMEOUT"] = $"{workflowCustomization.TimeoutMinutes}m";
        }
    }

    private void ApplyParameterSubstitutionsToPipeline(PipelineConfiguration pipeline, Dictionary<string, string> parameters)
    {
        if (!parameters.Any())
            return;

        // Substitute parameters in global variables
        var substitutedVariables = new Dictionary<string, object>();
        foreach (var variable in pipeline.Variables)
        {
            if (variable.Value is string stringValue)
            {
                substitutedVariables[variable.Key] = SubstituteParameters(stringValue, parameters);
            }
            else
            {
                substitutedVariables[variable.Key] = variable.Value;
            }
        }
        pipeline.Variables = substitutedVariables;

        // Substitute parameters in job scripts and variables
        foreach (var job in pipeline.Jobs.Values)
        {
            // Substitute in scripts
            for (int i = 0; i < job.Script.Count; i++)
            {
                job.Script[i] = SubstituteParameters(job.Script[i], parameters);
            }

            // Substitute in job variables
            if (job.Variables != null)
            {
                var substitutedJobVariables = new Dictionary<string, object>();
                foreach (var variable in job.Variables)
                {
                    if (variable.Value is string stringValue)
                    {
                        substitutedJobVariables[variable.Key] = SubstituteParameters(stringValue, parameters);
                    }
                    else
                    {
                        substitutedJobVariables[variable.Key] = variable.Value;
                    }
                }
                job.Variables = substitutedJobVariables;
            }

            // Substitute in Docker image
            if (job.Image?.Name != null)
            {
                job.Image.Name = SubstituteParameters(job.Image.Name, parameters);
            }

            // Substitute in environment URL
            if (job.Environment?.Url != null)
            {
                job.Environment.Url = SubstituteParameters(job.Environment.Url, parameters);
            }
        }
    }

    #endregion
}