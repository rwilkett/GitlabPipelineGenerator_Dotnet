using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Service interface for applying template customizations
/// </summary>
public interface ITemplateCustomizationService
{
    /// <summary>
    /// Applies customizations to pipeline options
    /// </summary>
    /// <param name="options">Original pipeline options</param>
    /// <param name="customization">Customizations to apply</param>
    /// <returns>Customized pipeline options</returns>
    Task<PipelineOptions> ApplyCustomizationsAsync(PipelineOptions options, TemplateCustomization customization);

    /// <summary>
    /// Applies customizations to a generated pipeline configuration
    /// </summary>
    /// <param name="pipeline">Original pipeline configuration</param>
    /// <param name="customization">Customizations to apply</param>
    /// <returns>Customized pipeline configuration</returns>
    Task<PipelineConfiguration> ApplyPipelineCustomizationsAsync(PipelineConfiguration pipeline, TemplateCustomization customization);

    /// <summary>
    /// Validates that customizations are compatible with the pipeline options
    /// </summary>
    /// <param name="options">Pipeline options</param>
    /// <param name="customization">Customizations to validate</param>
    /// <returns>List of validation errors, empty if valid</returns>
    List<string> ValidateCustomizations(PipelineOptions options, TemplateCustomization customization);

    /// <summary>
    /// Substitutes template parameters in a string value
    /// </summary>
    /// <param name="value">String value containing parameter placeholders</param>
    /// <param name="parameters">Parameter values for substitution</param>
    /// <returns>String with parameters substituted</returns>
    string SubstituteParameters(string value, Dictionary<string, string> parameters);

    /// <summary>
    /// Gets available parameters for a specific template
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <returns>Dictionary of parameter names and their descriptions</returns>
    Dictionary<string, string> GetAvailableParameters(string templateName);
}