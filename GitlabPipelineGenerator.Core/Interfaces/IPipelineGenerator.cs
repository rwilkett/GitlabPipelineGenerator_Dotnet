using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Interface for generating GitLab CI/CD pipeline configurations
/// </summary>
public interface IPipelineGenerator
{
    /// <summary>
    /// Generates a pipeline configuration based on the provided options
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Generated pipeline configuration</returns>
    Task<PipelineConfiguration> GenerateAsync(PipelineOptions options);

    /// <summary>
    /// Serializes a pipeline configuration to YAML format
    /// </summary>
    /// <param name="pipeline">Pipeline configuration to serialize</param>
    /// <returns>YAML representation of the pipeline</returns>
    string SerializeToYaml(PipelineConfiguration pipeline);
}