using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Enhanced pipeline generator that supports templates and customizations
/// </summary>
public class EnhancedPipelineGenerator : IPipelineGenerator
{
    private readonly IPipelineGenerator _basePipelineGenerator;
    private readonly IPipelineTemplateService _templateService;
    private readonly ITemplateCustomizationService _customizationService;
    private readonly YamlSerializationService _yamlService;

    public EnhancedPipelineGenerator(
        IPipelineGenerator basePipelineGenerator,
        IPipelineTemplateService templateService,
        ITemplateCustomizationService customizationService,
        YamlSerializationService yamlService)
    {
        _basePipelineGenerator = basePipelineGenerator ?? throw new ArgumentNullException(nameof(basePipelineGenerator));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _customizationService = customizationService ?? throw new ArgumentNullException(nameof(customizationService));
        _yamlService = yamlService ?? throw new ArgumentNullException(nameof(yamlService));
    }

    /// <summary>
    /// Generates a pipeline configuration based on the provided options
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Generated pipeline configuration</returns>
    public async Task<PipelineConfiguration> GenerateAsync(PipelineOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        // Use the base pipeline generator for standard generation
        return await _basePipelineGenerator.GenerateAsync(options);
    }

    /// <summary>
    /// Generates a pipeline using a specific template
    /// </summary>
    /// <param name="templateName">Name of the template to use</param>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Generated pipeline configuration</returns>
    public async Task<PipelineConfiguration> GenerateFromTemplateAsync(string templateName, PipelineOptions options)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));

        if (options == null)
            throw new ArgumentNullException(nameof(options));

        try
        {
            return await _templateService.GenerateFromTemplateAsync(templateName, options);
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is PipelineGenerationException))
        {
            throw new PipelineGenerationException($"Failed to generate pipeline from template '{templateName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generates a pipeline using a template with customizations
    /// </summary>
    /// <param name="templateName">Name of the template to use</param>
    /// <param name="options">Pipeline generation options</param>
    /// <param name="customization">Template customizations to apply</param>
    /// <returns>Generated pipeline configuration</returns>
    public async Task<PipelineConfiguration> GenerateFromTemplateWithCustomizationsAsync(
        string templateName, 
        PipelineOptions options, 
        TemplateCustomization customization)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));

        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (customization == null)
            throw new ArgumentNullException(nameof(customization));

        try
        {
            // Apply customizations to options first
            var customizedOptions = await _customizationService.ApplyCustomizationsAsync(options, customization);

            // Generate pipeline using template
            var pipeline = await _templateService.GenerateFromTemplateAsync(templateName, customizedOptions);

            // Apply pipeline-level customizations
            var customizedPipeline = await _customizationService.ApplyPipelineCustomizationsAsync(pipeline, customization);

            return customizedPipeline;
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is PipelineGenerationException))
        {
            throw new PipelineGenerationException($"Failed to generate customized pipeline from template '{templateName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets available templates for a specific project type
    /// </summary>
    /// <param name="projectType">Project type to filter by</param>
    /// <returns>Collection of compatible templates</returns>
    public IEnumerable<IPipelineTemplate> GetAvailableTemplates(string projectType)
    {
        return _templateService.GetTemplatesForProjectType(projectType);
    }

    /// <summary>
    /// Gets default options for a specific template
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <param name="projectType">Project type</param>
    /// <returns>Default pipeline options, or null if template not found</returns>
    public PipelineOptions? GetDefaultOptionsForTemplate(string templateName, string projectType)
    {
        return _templateService.GetDefaultOptionsForTemplate(templateName, projectType);
    }

    /// <summary>
    /// Validates options against a specific template
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <param name="options">Pipeline options to validate</param>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> ValidateOptionsForTemplate(string templateName, PipelineOptions options)
    {
        return _templateService.ValidateOptionsForTemplate(templateName, options);
    }

    /// <summary>
    /// Validates customizations against pipeline options
    /// </summary>
    /// <param name="options">Pipeline options</param>
    /// <param name="customization">Customizations to validate</param>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> ValidateCustomizations(PipelineOptions options, TemplateCustomization customization)
    {
        return _customizationService.ValidateCustomizations(options, customization);
    }

    /// <summary>
    /// Gets available parameters for a specific template
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <returns>Dictionary of parameter names and their descriptions</returns>
    public Dictionary<string, string> GetAvailableParameters(string templateName)
    {
        return _customizationService.GetAvailableParameters(templateName);
    }

    /// <summary>
    /// Serializes a pipeline configuration to YAML format
    /// </summary>
    /// <param name="pipeline">Pipeline configuration to serialize</param>
    /// <returns>YAML representation of the pipeline</returns>
    public string SerializeToYaml(PipelineConfiguration pipeline)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));

        return _yamlService.SerializePipeline(pipeline);
    }

    /// <summary>
    /// Generates a pipeline configuration with comprehensive options including templates and customizations
    /// </summary>
    /// <param name="generationRequest">Comprehensive generation request</param>
    /// <returns>Generated pipeline configuration</returns>
    public async Task<PipelineConfiguration> GenerateAsync(PipelineGenerationRequest generationRequest)
    {
        if (generationRequest == null)
            throw new ArgumentNullException(nameof(generationRequest));

        if (generationRequest.Options == null)
            throw new ArgumentException("Pipeline options are required", nameof(generationRequest));

        // If no template is specified, use standard generation
        if (string.IsNullOrWhiteSpace(generationRequest.TemplateName))
        {
            return await GenerateAsync(generationRequest.Options);
        }

        // If template is specified but no customizations, use template generation
        if (generationRequest.Customization == null)
        {
            return await GenerateFromTemplateAsync(generationRequest.TemplateName, generationRequest.Options);
        }

        // Use template with customizations
        return await GenerateFromTemplateWithCustomizationsAsync(
            generationRequest.TemplateName, 
            generationRequest.Options, 
            generationRequest.Customization);
    }
}

/// <summary>
/// Comprehensive request for pipeline generation
/// </summary>
public class PipelineGenerationRequest
{
    /// <summary>
    /// Pipeline generation options
    /// </summary>
    public PipelineOptions Options { get; set; } = new();

    /// <summary>
    /// Optional template name to use
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Optional template customizations
    /// </summary>
    public TemplateCustomization? Customization { get; set; }

    /// <summary>
    /// Whether to validate the request before generation
    /// </summary>
    public bool ValidateBeforeGeneration { get; set; } = true;

    /// <summary>
    /// Additional metadata for the generation request
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}