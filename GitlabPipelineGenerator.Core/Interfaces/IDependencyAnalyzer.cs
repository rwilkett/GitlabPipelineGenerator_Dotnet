using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Interface for analyzing project dependencies and package files
/// </summary>
public interface IDependencyAnalyzer
{
    /// <summary>
    /// Analyzes a package file to extract dependency information
    /// </summary>
    /// <param name="fileName">Name of the package file</param>
    /// <param name="content">Content of the package file</param>
    /// <returns>Dependency information extracted from the file</returns>
    Task<DependencyInfo> AnalyzePackageFileAsync(string fileName, string content);

    /// <summary>
    /// Analyzes package files based on file names only (metadata-based analysis)
    /// </summary>
    /// <param name="packageFileNames">List of package file names</param>
    /// <returns>Dependency information based on file presence</returns>
    Task<DependencyInfo> AnalyzePackageFilesAsync(List<string> packageFileNames);

    /// <summary>
    /// Recommends cache configuration based on detected dependencies
    /// </summary>
    /// <param name="dependencies">Dependency information</param>
    /// <returns>Cache configuration recommendations</returns>
    Task<CacheConfiguration> RecommendCacheConfigurationAsync(DependencyInfo dependencies);

    /// <summary>
    /// Recommends security scanning configuration based on dependencies
    /// </summary>
    /// <param name="dependencies">Dependency information</param>
    /// <returns>Security scanning recommendations</returns>
    Task<SecurityScanConfiguration> RecommendSecurityScanningAsync(DependencyInfo dependencies);

    /// <summary>
    /// Detects runtime requirements based on dependencies
    /// </summary>
    /// <param name="dependencies">Dependency information</param>
    /// <returns>Runtime information</returns>
    Task<RuntimeInfo> DetectRuntimeRequirementsAsync(DependencyInfo dependencies);
}