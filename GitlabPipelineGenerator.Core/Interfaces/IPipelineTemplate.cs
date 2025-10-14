using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Interface for pipeline templates that generate predefined pipeline configurations
/// </summary>
public interface IPipelineTemplate
{
    /// <summary>
    /// Gets the name of the template
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the template
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the supported project types for this template
    /// </summary>
    IEnumerable<string> SupportedProjectTypes { get; }

    /// <summary>
    /// Generates a pipeline configuration using this template
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Generated pipeline configuration</returns>
    Task<PipelineConfiguration> GenerateAsync(PipelineOptions options);

    /// <summary>
    /// Validates that the provided options are compatible with this template
    /// </summary>
    /// <param name="options">Pipeline generation options to validate</param>
    /// <returns>List of validation errors, empty if valid</returns>
    List<string> ValidateOptions(PipelineOptions options);

    /// <summary>
    /// Gets the default pipeline options for this template
    /// </summary>
    /// <param name="projectType">Project type to get defaults for</param>
    /// <returns>Default pipeline options</returns>
    PipelineOptions GetDefaultOptions(string projectType);
}