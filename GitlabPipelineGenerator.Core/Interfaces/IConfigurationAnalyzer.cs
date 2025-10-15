using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Interface for analyzing configuration files and existing CI/CD setups
/// </summary>
public interface IConfigurationAnalyzer
{
    /// <summary>
    /// Analyzes existing CI/CD configuration files
    /// </summary>
    /// <param name="project">GitLab project to analyze</param>
    /// <returns>Existing CI/CD configuration information</returns>
    Task<ExistingCIConfig> AnalyzeExistingCIConfigAsync(GitLabProject project);

    /// <summary>
    /// Analyzes Docker configuration files
    /// </summary>
    /// <param name="project">GitLab project to analyze</param>
    /// <returns>Docker configuration information</returns>
    Task<DockerConfiguration> AnalyzeDockerConfigurationAsync(GitLabProject project);

    /// <summary>
    /// Analyzes deployment configuration files
    /// </summary>
    /// <param name="project">GitLab project to analyze</param>
    /// <returns>Deployment configuration information</returns>
    Task<DeploymentConfiguration> AnalyzeDeploymentConfigurationAsync(GitLabProject project);

    /// <summary>
    /// Detects environment configurations
    /// </summary>
    /// <param name="project">GitLab project to analyze</param>
    /// <returns>Environment configuration information</returns>
    Task<EnvironmentConfiguration> DetectEnvironmentsAsync(GitLabProject project);
}