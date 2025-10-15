using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Interface for analyzing file patterns to detect project characteristics
/// </summary>
public interface IFilePatternAnalyzer
{
    /// <summary>
    /// Detects the primary project type based on file patterns and extensions
    /// </summary>
    /// <param name="files">Collection of repository files to analyze</param>
    /// <returns>Detected project type</returns>
    Task<ProjectType> DetectProjectTypeAsync(IEnumerable<GitLabRepositoryFile> files);

    /// <summary>
    /// Detects frameworks used in the project
    /// </summary>
    /// <param name="files">Collection of repository files to analyze</param>
    /// <returns>Framework information</returns>
    Task<FrameworkInfo> DetectFrameworksAsync(IEnumerable<GitLabRepositoryFile> files);

    /// <summary>
    /// Detects build tools used in the project
    /// </summary>
    /// <param name="files">Collection of repository files to analyze</param>
    /// <returns>Build tool information</returns>
    Task<BuildToolInfo> DetectBuildToolsAsync(IEnumerable<GitLabRepositoryFile> files);

    /// <summary>
    /// Detects test frameworks used in the project
    /// </summary>
    /// <param name="files">Collection of repository files to analyze</param>
    /// <returns>Test framework information</returns>
    Task<TestFrameworkInfo> DetectTestFrameworksAsync(IEnumerable<GitLabRepositoryFile> files);
}