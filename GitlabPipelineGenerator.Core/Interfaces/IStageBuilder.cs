using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Interface for building pipeline stages
/// </summary>
public interface IStageBuilder
{
    /// <summary>
    /// Builds the list of stages for a pipeline based on the provided options
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>List of stage names</returns>
    Task<List<string>> BuildStagesAsync(PipelineOptions options);

    /// <summary>
    /// Gets the default stages for a specific project type
    /// </summary>
    /// <param name="projectType">Type of project</param>
    /// <returns>List of default stage names</returns>
    List<string> GetDefaultStages(string projectType);

    /// <summary>
    /// Validates that the provided stages are valid for the project type
    /// </summary>
    /// <param name="stages">Stages to validate</param>
    /// <param name="projectType">Project type</param>
    /// <returns>List of validation errors, empty if valid</returns>
    List<string> ValidateStages(List<string> stages, string projectType);
}