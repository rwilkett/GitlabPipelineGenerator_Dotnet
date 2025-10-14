using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Interface for building pipeline jobs
/// </summary>
public interface IJobBuilder
{
    /// <summary>
    /// Builds jobs for a specific stage based on the pipeline options
    /// </summary>
    /// <param name="stage">Stage name</param>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Dictionary of jobs keyed by job name</returns>
    Task<Dictionary<string, Job>> BuildJobsForStageAsync(string stage, PipelineOptions options);

    /// <summary>
    /// Builds a custom job based on custom job options
    /// </summary>
    /// <param name="customJob">Custom job configuration</param>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Configured job</returns>
    Task<Job> BuildCustomJobAsync(CustomJobOptions customJob, PipelineOptions options);

    /// <summary>
    /// Creates a build job for the specified project type
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Build job configuration</returns>
    Task<Job> CreateBuildJobAsync(PipelineOptions options);

    /// <summary>
    /// Creates test jobs for the specified project type
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Dictionary of test jobs</returns>
    Task<Dictionary<string, Job>> CreateTestJobsAsync(PipelineOptions options);

    /// <summary>
    /// Creates deployment jobs for the specified environments
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Dictionary of deployment jobs</returns>
    Task<Dictionary<string, Job>> CreateDeploymentJobsAsync(PipelineOptions options);
}