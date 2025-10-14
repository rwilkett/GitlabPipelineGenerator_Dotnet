using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Service for managing and using pipeline templates
/// </summary>
public class PipelineTemplateService : IPipelineTemplateService
{
    private readonly Dictionary<string, IPipelineTemplate> _templates;

    public PipelineTemplateService()
    {
        _templates = new Dictionary<string, IPipelineTemplate>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all available pipeline templates
    /// </summary>
    /// <returns>Collection of available templates</returns>
    public IEnumerable<IPipelineTemplate> GetAvailableTemplates()
    {
        return _templates.Values;
    }

    /// <summary>
    /// Gets templates that support the specified project type
    /// </summary>
    /// <param name="projectType">Project type to filter by</param>
    /// <returns>Collection of compatible templates</returns>
    public IEnumerable<IPipelineTemplate> GetTemplatesForProjectType(string projectType)
    {
        if (string.IsNullOrWhiteSpace(projectType))
            return Enumerable.Empty<IPipelineTemplate>();

        return _templates.Values.Where(template => 
            template.SupportedProjectTypes.Contains(projectType, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a specific template by name
    /// </summary>
    /// <param name="templateName">Name of the template to retrieve</param>
    /// <returns>Template instance, or null if not found</returns>
    public IPipelineTemplate? GetTemplate(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return null;

        return _templates.TryGetValue(templateName, out var template) ? template : null;
    }

    /// <summary>
    /// Generates a pipeline using the specified template
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

        var template = GetTemplate(templateName);
        if (template == null)
        {
            throw new PipelineGenerationException($"Template '{templateName}' not found. Available templates: {string.Join(", ", _templates.Keys)}");
        }

        try
        {
            return await template.GenerateAsync(options);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidPipelineOptionsException($"Invalid options for template '{templateName}': {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new PipelineGenerationException($"Failed to generate pipeline using template '{templateName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Registers a new template with the service
    /// </summary>
    /// <param name="template">Template to register</param>
    public void RegisterTemplate(IPipelineTemplate template)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));

        if (string.IsNullOrWhiteSpace(template.Name))
            throw new ArgumentException("Template name cannot be null or empty", nameof(template));

        if (_templates.ContainsKey(template.Name))
        {
            throw new InvalidOperationException($"Template with name '{template.Name}' is already registered");
        }

        _templates[template.Name] = template;
    }

    /// <summary>
    /// Unregisters a template from the service
    /// </summary>
    /// <param name="templateName">Name of the template to unregister</param>
    /// <returns>True if the template was removed, false if it wasn't found</returns>
    public bool UnregisterTemplate(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return false;

        return _templates.Remove(templateName);
    }

    /// <summary>
    /// Checks if a template with the specified name is registered
    /// </summary>
    /// <param name="templateName">Name of the template to check</param>
    /// <returns>True if the template is registered, false otherwise</returns>
    public bool IsTemplateRegistered(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return false;

        return _templates.ContainsKey(templateName);
    }

    /// <summary>
    /// Gets the names of all registered templates
    /// </summary>
    /// <returns>Collection of template names</returns>
    public IEnumerable<string> GetTemplateNames()
    {
        return _templates.Keys;
    }

    /// <summary>
    /// Validates that the specified options are compatible with the template
    /// </summary>
    /// <param name="templateName">Name of the template to validate against</param>
    /// <param name="options">Pipeline generation options to validate</param>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> ValidateOptionsForTemplate(string templateName, PipelineOptions options)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return new List<string> { "Template name cannot be null or empty" };

        if (options == null)
            return new List<string> { "Pipeline options cannot be null" };

        var template = GetTemplate(templateName);
        if (template == null)
        {
            return new List<string> { $"Template '{templateName}' not found" };
        }

        return template.ValidateOptions(options);
    }

    /// <summary>
    /// Gets the default options for the specified template and project type
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <param name="projectType">Project type to get defaults for</param>
    /// <returns>Default pipeline options, or null if template not found</returns>
    public PipelineOptions? GetDefaultOptionsForTemplate(string templateName, string projectType)
    {
        if (string.IsNullOrWhiteSpace(templateName) || string.IsNullOrWhiteSpace(projectType))
            return null;

        var template = GetTemplate(templateName);
        return template?.GetDefaultOptions(projectType);
    }
}