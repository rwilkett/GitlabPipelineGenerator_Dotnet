using GitlabPipelineGenerator.Core.Models;
using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Service for mapping project analysis results to pipeline configuration
/// </summary>
public interface IAnalysisToPipelineMappingService
{
    /// <summary>
    /// Maps project analysis result to pipeline options
    /// </summary>
    /// <param name="analysisResult">Project analysis result</param>
    /// <returns>Pipeline options based on analysis</returns>
    Task<PipelineOptions> MapAnalysisToPipelineOptionsAsync(ProjectAnalysisResult analysisResult);

    /// <summary>
    /// Gets project type specific pipeline template recommendations
    /// </summary>
    /// <param name="projectType">Detected project type</param>
    /// <param name="framework">Framework information</param>
    /// <returns>Recommended pipeline template names</returns>
    Task<IEnumerable<string>> GetRecommendedTemplatesAsync(ProjectType projectType, FrameworkInfo framework);

    /// <summary>
    /// Maps build configuration to pipeline jobs
    /// </summary>
    /// <param name="buildConfig">Build configuration from analysis</param>
    /// <param name="projectType">Project type</param>
    /// <returns>Dictionary of job configurations</returns>
    Task<Dictionary<string, Job>> MapBuildConfigurationToJobsAsync(BuildConfiguration buildConfig, ProjectType projectType);

    /// <summary>
    /// Maps dependency information to caching configuration
    /// </summary>
    /// <param name="dependencies">Dependency information</param>
    /// <returns>Cache configuration for pipeline</returns>
    Task<Dictionary<string, object>> MapDependenciesToCacheConfigAsync(DependencyInfo dependencies);

    /// <summary>
    /// Maps security scan recommendations to security jobs
    /// </summary>
    /// <param name="securityConfig">Security scan configuration</param>
    /// <returns>Security job configurations</returns>
    Task<Dictionary<string, Job>> MapSecurityConfigToJobsAsync(SecurityScanConfiguration securityConfig);

    /// <summary>
    /// Maps deployment configuration to deployment jobs
    /// </summary>
    /// <param name="deploymentInfo">Deployment information</param>
    /// <param name="environments">Environment configurations</param>
    /// <returns>Deployment job configurations</returns>
    Task<Dictionary<string, Job>> MapDeploymentConfigToJobsAsync(DeploymentInfo deploymentInfo, List<DeploymentEnvironment> environments);

    /// <summary>
    /// Gets framework-specific Docker image recommendations
    /// </summary>
    /// <param name="framework">Framework information</param>
    /// <param name="dockerConfig">Docker configuration (if available)</param>
    /// <returns>Recommended Docker image</returns>
    Task<string?> GetRecommendedDockerImageAsync(FrameworkInfo framework, DockerConfiguration? dockerConfig = null);

    /// <summary>
    /// Maps analysis confidence to pipeline generation strategy
    /// </summary>
    /// <param name="confidence">Analysis confidence level</param>
    /// <returns>Recommended generation strategy</returns>
    Task<PipelineGenerationStrategy> MapConfidenceToStrategyAsync(AnalysisConfidence confidence);
}

/// <summary>
/// Pipeline generation strategies based on analysis confidence
/// </summary>
public enum PipelineGenerationStrategy
{
    /// <summary>
    /// Use conservative defaults with minimal assumptions
    /// </summary>
    Conservative,

    /// <summary>
    /// Use balanced approach with moderate optimizations
    /// </summary>
    Balanced,

    /// <summary>
    /// Use aggressive optimizations based on high-confidence analysis
    /// </summary>
    Aggressive,

    /// <summary>
    /// Use template-based generation with analysis enhancements
    /// </summary>
    TemplateEnhanced
}