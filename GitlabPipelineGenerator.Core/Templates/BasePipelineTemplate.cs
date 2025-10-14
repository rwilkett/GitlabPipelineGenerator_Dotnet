using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Templates;

/// <summary>
/// Base class for pipeline templates providing common functionality
/// </summary>
public abstract class BasePipelineTemplate : IPipelineTemplate
{
    protected readonly IStageBuilder _stageBuilder;
    protected readonly IJobBuilder _jobBuilder;
    protected readonly IVariableBuilder _variableBuilder;

    protected BasePipelineTemplate(
        IStageBuilder stageBuilder,
        IJobBuilder jobBuilder,
        IVariableBuilder variableBuilder)
    {
        _stageBuilder = stageBuilder ?? throw new ArgumentNullException(nameof(stageBuilder));
        _jobBuilder = jobBuilder ?? throw new ArgumentNullException(nameof(jobBuilder));
        _variableBuilder = variableBuilder ?? throw new ArgumentNullException(nameof(variableBuilder));
    }

    /// <summary>
    /// Gets the name of the template
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the description of the template
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Gets the supported project types for this template
    /// </summary>
    public abstract IEnumerable<string> SupportedProjectTypes { get; }

    /// <summary>
    /// Generates a pipeline configuration using this template
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Generated pipeline configuration</returns>
    public virtual async Task<PipelineConfiguration> GenerateAsync(PipelineOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        // Validate options
        var validationErrors = ValidateOptions(options);
        if (validationErrors.Any())
        {
            throw new ArgumentException($"Invalid pipeline options: {string.Join(", ", validationErrors)}");
        }

        // Apply template customizations to options
        var customizedOptions = ApplyTemplateCustomizations(options);

        // Generate pipeline configuration
        var pipeline = new PipelineConfiguration();

        // Build stages
        pipeline.Stages = await _stageBuilder.BuildStagesAsync(customizedOptions);

        // Build global variables
        pipeline.Variables = await _variableBuilder.BuildGlobalVariablesAsync(customizedOptions);

        // Build default configuration
        pipeline.Default = await _variableBuilder.BuildDefaultConfigurationAsync(customizedOptions);

        // Build jobs for each stage
        foreach (var stage in pipeline.Stages)
        {
            var stageJobs = await _jobBuilder.BuildJobsForStageAsync(stage, customizedOptions);
            foreach (var job in stageJobs)
            {
                pipeline.Jobs[job.Key] = job.Value;
            }
        }

        // Add custom jobs
        foreach (var customJob in customizedOptions.CustomJobs)
        {
            var job = await _jobBuilder.BuildCustomJobAsync(customJob, customizedOptions);
            pipeline.Jobs[customJob.Name] = job;
        }

        // Apply template-specific pipeline modifications
        await ApplyTemplatePipelineModificationsAsync(pipeline, customizedOptions);

        return pipeline;
    }

    /// <summary>
    /// Validates that the provided options are compatible with this template
    /// </summary>
    /// <param name="options">Pipeline generation options to validate</param>
    /// <returns>List of validation errors, empty if valid</returns>
    public virtual List<string> ValidateOptions(PipelineOptions options)
    {
        var errors = new List<string>();

        if (options == null)
        {
            errors.Add("Pipeline options cannot be null");
            return errors;
        }

        // Validate project type compatibility
        if (!SupportedProjectTypes.Contains(options.ProjectType, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"Project type '{options.ProjectType}' is not supported by template '{Name}'. Supported types: {string.Join(", ", SupportedProjectTypes)}");
        }

        // Validate basic options
        errors.AddRange(options.Validate());

        // Template-specific validation
        errors.AddRange(ValidateTemplateSpecificOptions(options));

        return errors;
    }

    /// <summary>
    /// Gets the default pipeline options for this template
    /// </summary>
    /// <param name="projectType">Project type to get defaults for</param>
    /// <returns>Default pipeline options</returns>
    public abstract PipelineOptions GetDefaultOptions(string projectType);

    /// <summary>
    /// Applies template-specific customizations to the pipeline options
    /// </summary>
    /// <param name="options">Original pipeline options</param>
    /// <returns>Customized pipeline options</returns>
    protected virtual PipelineOptions ApplyTemplateCustomizations(PipelineOptions options)
    {
        // Default implementation returns options unchanged
        // Override in derived classes for template-specific customizations
        return options;
    }

    /// <summary>
    /// Applies template-specific modifications to the generated pipeline
    /// </summary>
    /// <param name="pipeline">Generated pipeline configuration</param>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Task representing the async operation</returns>
    protected virtual async Task ApplyTemplatePipelineModificationsAsync(PipelineConfiguration pipeline, PipelineOptions options)
    {
        // Default implementation does nothing
        // Override in derived classes for template-specific pipeline modifications
        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates template-specific options
    /// </summary>
    /// <param name="options">Pipeline generation options to validate</param>
    /// <returns>List of template-specific validation errors</returns>
    protected virtual List<string> ValidateTemplateSpecificOptions(PipelineOptions options)
    {
        // Default implementation returns no errors
        // Override in derived classes for template-specific validation
        return new List<string>();
    }
}