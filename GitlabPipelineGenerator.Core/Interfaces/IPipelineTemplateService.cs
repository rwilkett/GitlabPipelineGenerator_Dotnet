using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Service interface for managing and using pipeline templates
/// </summary>
public interface IPipelineTemplateService
{
    /// <summary>
    /// Gets all available pipeline templates
    /// </summary>
    /// <returns>Collection of available templates</returns>
    IEnumerable<IPipelineTemplate> GetAvailableTemplates();

    /// <summary>
    /// Gets templates that support the specified project type
    /// </summary>
    /// <param name="projectType">Project type to filter by</param>
    /// <returns>Collection of compatible templates</returns>
    IEnumerable<IPipelineTemplate> GetTemplatesForProjectType(string projectType);

    /// <summary>
    /// Gets a specific template by name
    /// </summary>
    /// <param name="templateName">Name of the template to retrieve</param>
    /// <returns>Template instance, or null if not found</returns>
    IPipelineTemplate? GetTemplate(string templateName);

    /// <summary>
    /// Generates a pipeline using the specified template
    /// </summary>
    /// <param name="templateName">Name of the template to use</param>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Generated pipeline configuration</returns>
    Task<PipelineConfiguration> GenerateFromTemplateAsync(string templateName, PipelineOptions options);

    /// <summary>
    /// Registers a new template with the service
    /// </summary>
    /// <param name="template">Template to register</param>
    void RegisterTemplate(IPipelineTemplate template);

    /// <summary>
    /// Gets default options for a specific template and project type
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <param name="projectType">Project type</param>
    /// <returns>Default pipeline options, or null if template not found</returns>
    PipelineOptions? GetDefaultOptionsForTemplate(string templateName, string projectType);

    /// <summary>
    /// Validates options against a specific template
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <param name="options">Pipeline options to validate</param>
    /// <returns>List of validation errors, empty if valid</returns>
    List<string> ValidateOptionsForTemplate(string templateName, PipelineOptions options);
}