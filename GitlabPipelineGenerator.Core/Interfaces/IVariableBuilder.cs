using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Interface for building pipeline variables and configurations
/// </summary>
public interface IVariableBuilder
{
    /// <summary>
    /// Builds global variables for the pipeline
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Dictionary of global variables</returns>
    Task<Dictionary<string, object>> BuildGlobalVariablesAsync(PipelineOptions options);

    /// <summary>
    /// Builds default configuration that applies to all jobs
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Dictionary of default configuration</returns>
    Task<Dictionary<string, object>> BuildDefaultConfigurationAsync(PipelineOptions options);

    /// <summary>
    /// Builds job-specific variables
    /// </summary>
    /// <param name="jobType">Type of job (build, test, deploy, etc.)</param>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Dictionary of job-specific variables</returns>
    Task<Dictionary<string, object>> BuildJobVariablesAsync(string jobType, PipelineOptions options);

    /// <summary>
    /// Gets default variables for a specific project type
    /// </summary>
    /// <param name="projectType">Type of project</param>
    /// <returns>Dictionary of default variables</returns>
    Dictionary<string, object> GetDefaultVariables(string projectType);

    /// <summary>
    /// Merges custom variables with default variables
    /// </summary>
    /// <param name="defaultVariables">Default variables</param>
    /// <param name="customVariables">Custom variables to merge</param>
    /// <returns>Merged variables dictionary</returns>
    Dictionary<string, object> MergeVariables(Dictionary<string, object> defaultVariables, Dictionary<string, string> customVariables);
}