using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Interface for orchestrating comprehensive project analysis
/// </summary>
public interface IProjectAnalysisService
{
    /// <summary>
    /// Performs comprehensive analysis of a GitLab project
    /// </summary>
    /// <param name="project">GitLab project to analyze</param>
    /// <param name="options">Analysis options</param>
    /// <returns>Complete project analysis results</returns>
    Task<ProjectAnalysisResult> AnalyzeProjectAsync(GitLabProject project, AnalysisOptions options);

    /// <summary>
    /// Detects the primary project type
    /// </summary>
    /// <param name="project">GitLab project to analyze</param>
    /// <returns>Detected project type</returns>
    Task<ProjectType> DetectProjectTypeAsync(GitLabProject project);

    /// <summary>
    /// Analyzes build configuration requirements
    /// </summary>
    /// <param name="project">GitLab project to analyze</param>
    /// <returns>Build configuration information</returns>
    Task<BuildConfiguration> AnalyzeBuildConfigurationAsync(GitLabProject project);

    /// <summary>
    /// Analyzes project dependencies
    /// </summary>
    /// <param name="project">GitLab project to analyze</param>
    /// <returns>Dependency information</returns>
    Task<DependencyInfo> AnalyzeDependenciesAsync(GitLabProject project);

    /// <summary>
    /// Analyzes deployment configuration
    /// </summary>
    /// <param name="project">GitLab project to analyze</param>
    /// <returns>Deployment information</returns>
    Task<DeploymentInfo> AnalyzeDeploymentConfigurationAsync(GitLabProject project);
}