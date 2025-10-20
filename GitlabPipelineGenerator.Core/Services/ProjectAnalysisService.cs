using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using System.Diagnostics;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Orchestrates comprehensive project analysis
/// </summary>
public class ProjectAnalysisService : IProjectAnalysisService
{
    private readonly IFilePatternAnalyzer _filePatternAnalyzer;
    private readonly IDependencyAnalyzer _dependencyAnalyzer;
    private readonly IConfigurationAnalyzer _configurationAnalyzer;
    private readonly IGitLabProjectService _gitLabProjectService;

    public ProjectAnalysisService(
        IFilePatternAnalyzer filePatternAnalyzer,
        IDependencyAnalyzer dependencyAnalyzer,
        IConfigurationAnalyzer configurationAnalyzer,
        IGitLabProjectService gitLabProjectService)
    {
        _filePatternAnalyzer = filePatternAnalyzer ?? throw new ArgumentNullException(nameof(filePatternAnalyzer));
        _dependencyAnalyzer = dependencyAnalyzer ?? throw new ArgumentNullException(nameof(dependencyAnalyzer));
        _configurationAnalyzer = configurationAnalyzer ?? throw new ArgumentNullException(nameof(configurationAnalyzer));
        _gitLabProjectService = gitLabProjectService ?? throw new ArgumentNullException(nameof(gitLabProjectService));
    }

    public async Task<ProjectAnalysisResult> AnalyzeProjectAsync(GitLabProject project, AnalysisOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ProjectAnalysisResult();
        var warnings = new List<AnalysisWarning>();
        var recommendations = new List<string>();

        try
        {
            // Get project files for analysis
            var files = await GetProjectFilesAsync(project, options);
            result.FilesAnalyzed = files.Count;

            // Analyze file patterns and project type
            if (options.AnalyzeFiles)
            {
                result.DetectedType = await _filePatternAnalyzer.DetectProjectTypeAsync(files);
                result.Framework = await _filePatternAnalyzer.DetectFrameworksAsync(files);
                
                var buildToolInfo = await _filePatternAnalyzer.DetectBuildToolsAsync(files);
                var testFrameworkInfo = await _filePatternAnalyzer.DetectTestFrameworksAsync(files);

                result.BuildConfig = await BuildConfigurationFromAnalysisAsync(buildToolInfo, testFrameworkInfo);
                result.Metadata.AnalyzedComponents.Add("FilePatternAnalyzer");
            }

            // Analyze dependencies
            if (options.AnalyzeDependencies)
            {
                result.Dependencies = await AnalyzeProjectDependenciesAsync(project, files);
                result.Metadata.AnalyzedComponents.Add("DependencyAnalyzer");
            }

            // Analyze existing CI/CD configuration
            if (options.AnalyzeExistingCI)
            {
                result.ExistingCI = await _configurationAnalyzer.AnalyzeExistingCIConfigAsync(project);
                result.Metadata.AnalyzedComponents.Add("ConfigurationAnalyzer-CI");
            }

            // Analyze Docker configuration
            result.Docker = await _configurationAnalyzer.AnalyzeDockerConfigurationAsync(project);
            result.Metadata.AnalyzedComponents.Add("ConfigurationAnalyzer-Docker");

            // Analyze deployment configuration
            if (options.AnalyzeDeployment)
            {
                var deploymentConfig = await _configurationAnalyzer.AnalyzeDeploymentConfigurationAsync(project);
                var environmentConfig = await _configurationAnalyzer.DetectEnvironmentsAsync(project);
                
                result.Deployment = new DeploymentInfo
                {
                    HasDeploymentConfig = deploymentConfig.HasDeploymentConfig,
                    Configuration = deploymentConfig,
                    Environment = environmentConfig,
                    Confidence = CombineConfidence(deploymentConfig.Confidence, environmentConfig.Confidence)
                };
                
                result.Environment = environmentConfig;
                result.Metadata.AnalyzedComponents.Add("ConfigurationAnalyzer-Deployment");
            }

            // Generate cache and security recommendations
            if (result.Dependencies.TotalDependencies > 0)
            {
                await _dependencyAnalyzer.RecommendCacheConfigurationAsync(result.Dependencies);
                var securityConfig = await _dependencyAnalyzer.RecommendSecurityScanningAsync(result.Dependencies);
                
                if (securityConfig.IsRecommended)
                {
                    recommendations.Add($"Security scanning recommended: {securityConfig.Reason}");
                }
            }

            // Calculate overall confidence
            result.Confidence = CalculateOverallConfidence(result);

            // Generate warnings and recommendations
            GenerateAnalysisWarnings(result, warnings);
            GenerateRecommendations(result, recommendations);

            result.Warnings = warnings;
            result.Recommendations = recommendations;

            // Set metadata
            result.Metadata.AnalyzedAt = DateTime.UtcNow;
            result.Metadata.Branch = project.DefaultBranch;
            result.Metadata.Options = options;

            stopwatch.Stop();
            result.AnalysisTime = stopwatch.Elapsed;

            return result;
        }
        catch (Exception ex)
        {
            warnings.Add(new AnalysisWarning
            {
                Severity = WarningSeverity.Error,
                Message = $"Analysis failed: {ex.Message}",
                Component = "ProjectAnalysisService"
            });

            result.Warnings = warnings;
            result.Confidence = AnalysisConfidence.Low;
            stopwatch.Stop();
            result.AnalysisTime = stopwatch.Elapsed;

            return result;
        }
    }

    public async Task<ProjectType> DetectProjectTypeAsync(GitLabProject project)
    {
        var files = await GetProjectFilesAsync(project, new AnalysisOptions { AnalyzeFiles = true });
        return await _filePatternAnalyzer.DetectProjectTypeAsync(files);
    }

    public async Task<BuildConfiguration> AnalyzeBuildConfigurationAsync(GitLabProject project)
    {
        var files = await GetProjectFilesAsync(project, new AnalysisOptions { AnalyzeFiles = true });
        var buildToolInfo = await _filePatternAnalyzer.DetectBuildToolsAsync(files);
        var testFrameworkInfo = await _filePatternAnalyzer.DetectTestFrameworksAsync(files);
        
        return await BuildConfigurationFromAnalysisAsync(buildToolInfo, testFrameworkInfo);
    }

    public async Task<DependencyInfo> AnalyzeDependenciesAsync(GitLabProject project)
    {
        var files = await GetProjectFilesAsync(project, new AnalysisOptions { AnalyzeDependencies = true });
        return await AnalyzeProjectDependenciesAsync(project, files);
    }

    public async Task<DeploymentInfo> AnalyzeDeploymentConfigurationAsync(GitLabProject project)
    {
        var deploymentConfig = await _configurationAnalyzer.AnalyzeDeploymentConfigurationAsync(project);
        var environmentConfig = await _configurationAnalyzer.DetectEnvironmentsAsync(project);
        
        return new DeploymentInfo
        {
            HasDeploymentConfig = deploymentConfig.HasDeploymentConfig,
            Configuration = deploymentConfig,
            Environment = environmentConfig,
            Confidence = CombineConfidence(deploymentConfig.Confidence, environmentConfig.Confidence)
        };
    }

    private async Task<List<GitLabRepositoryFile>> GetProjectFilesAsync(GitLabProject project, AnalysisOptions options)
    {
        var files = (await _gitLabProjectService.GetRepositoryFilesAsync(project.Id, "", recursive: true, maxDepth: options.MaxFileAnalysisDepth)).ToList();

        // Filter files based on analysis options
        if (!options.AnalyzeFiles)
        {
            files = files.Where(f => f.Type != "blob" || IsPackageFile(f.Name)).ToList();
        }

        // Apply exclusion patterns
        if (options.ExcludePatterns.Any())
        {
            files = files.Where(f => !options.ExcludePatterns.Any(pattern => 
                f.Path.Contains(pattern, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        return files;
    }

    private async Task<DependencyInfo> AnalyzeProjectDependenciesAsync(GitLabProject project, List<GitLabRepositoryFile> files)
    {
        var packageFiles = files.Where(f => IsPackageFile(f.Name)).ToList();
        
        if (!packageFiles.Any())
        {
            return new DependencyInfo { Confidence = AnalysisConfidence.Low };
        }

        // Analyze package files based on metadata only (no content download)
        return await _dependencyAnalyzer.AnalyzePackageFilesAsync(packageFiles.Select(f => f.Name).ToList());
    }

    private async Task<BuildConfiguration> BuildConfigurationFromAnalysisAsync(
        BuildToolInfo buildToolInfo, 
        TestFrameworkInfo testFrameworkInfo)
    {
        var buildConfig = new BuildConfiguration
        {
            BuildTool = buildToolInfo.Name,
            BuildCommands = buildToolInfo.BuildCommands.ToList(),
            TestCommands = buildToolInfo.TestCommands.Concat(testFrameworkInfo.TestCommands).Distinct().ToList(),
            ArtifactPaths = new List<string>(),
            Confidence = CombineConfidence(buildToolInfo.Confidence, testFrameworkInfo.Confidence)
        };

        // Add version-specific configuration for .NET
        if (buildToolInfo.Name.ToLowerInvariant() == "dotnet" && !string.IsNullOrEmpty(buildToolInfo.Version))
        {
            buildConfig.Settings["DotNetVersion"] = buildToolInfo.Version;
            buildConfig.Settings["DockerImage"] = GetDotNetDockerImage(buildToolInfo.Version);
        }

        // Add common artifact paths based on build tool
        switch (buildToolInfo.Name.ToLowerInvariant())
        {
            case "dotnet":
                buildConfig.ArtifactPaths.AddRange(new[] { "bin/", "obj/", "*.nupkg" });
                buildConfig.OutputDirectories.Add("bin/Release/");
                break;
            case "npm":
                buildConfig.ArtifactPaths.AddRange(new[] { "dist/", "build/", "node_modules/" });
                buildConfig.OutputDirectories.Add("dist/");
                break;
            case "maven":
                buildConfig.ArtifactPaths.AddRange(new[] { "target/", "*.jar" });
                buildConfig.OutputDirectories.Add("target/");
                break;
            case "gradle":
                buildConfig.ArtifactPaths.AddRange(new[] { "build/", "*.jar" });
                buildConfig.OutputDirectories.Add("build/libs/");
                break;
        }

        return buildConfig;
    }

    private string GetDotNetDockerImage(string version)
    {
        return version switch
        {
            "9.0" => "mcr.microsoft.com/dotnet/sdk:9.0",
            "8.0" => "mcr.microsoft.com/dotnet/sdk:8.0",
            "7.0" => "mcr.microsoft.com/dotnet/sdk:7.0",
            "6.0" => "mcr.microsoft.com/dotnet/sdk:6.0",
            "5.0" => "mcr.microsoft.com/dotnet/sdk:5.0",
            "3.1" => "mcr.microsoft.com/dotnet/sdk:3.1",
            "2.1" => "mcr.microsoft.com/dotnet/sdk:2.1",
            "framework" => "mcr.microsoft.com/dotnet/framework/sdk:4.8",
            _ => "mcr.microsoft.com/dotnet/sdk:8.0"
        };
    }

    private AnalysisConfidence CalculateOverallConfidence(ProjectAnalysisResult result)
    {
        var confidenceScores = new List<int>();

        if (result.DetectedType != ProjectType.Unknown)
            confidenceScores.Add((int)result.Framework.Confidence);

        if (result.Dependencies.TotalDependencies > 0)
            confidenceScores.Add((int)result.Dependencies.Confidence);

        if (result.BuildConfig.BuildCommands.Any())
            confidenceScores.Add((int)result.BuildConfig.Confidence);

        if (result.ExistingCI?.HasExistingConfig == true)
            confidenceScores.Add((int)result.ExistingCI.Confidence);

        if (!confidenceScores.Any())
            return AnalysisConfidence.Low;

        var averageScore = confidenceScores.Average();
        return averageScore switch
        {
            >= 2.5 => AnalysisConfidence.High,
            >= 1.5 => AnalysisConfidence.Medium,
            _ => AnalysisConfidence.Low
        };
    }

    private void GenerateAnalysisWarnings(ProjectAnalysisResult result, List<AnalysisWarning> warnings)
    {
        if (result.DetectedType == ProjectType.Unknown)
        {
            warnings.Add(new AnalysisWarning
            {
                Severity = WarningSeverity.Warning,
                Message = "Could not determine project type",
                Component = "FilePatternAnalyzer",
                Resolution = "Ensure project contains recognizable configuration files"
            });
        }

        if (result.Dependencies.TotalDependencies == 0)
        {
            warnings.Add(new AnalysisWarning
            {
                Severity = WarningSeverity.Info,
                Message = "No dependencies detected",
                Component = "DependencyAnalyzer",
                Resolution = "Verify package files are present and accessible"
            });
        }

        if (result.Dependencies.HasSecuritySensitiveDependencies)
        {
            warnings.Add(new AnalysisWarning
            {
                Severity = WarningSeverity.Warning,
                Message = "Security-sensitive dependencies detected",
                Component = "DependencyAnalyzer",
                Resolution = "Consider enabling security scanning in CI/CD pipeline"
            });
        }

        if (result.ExistingCI?.HasExistingConfig == true && result.ExistingCI.SystemType != CISystemType.GitLabCI)
        {
            warnings.Add(new AnalysisWarning
            {
                Severity = WarningSeverity.Info,
                Message = $"Existing {result.ExistingCI.SystemType} configuration detected",
                Component = "ConfigurationAnalyzer",
                Resolution = "Consider migrating to GitLab CI/CD for better integration"
            });
        }
    }

    private void GenerateRecommendations(ProjectAnalysisResult result, List<string> recommendations)
    {
        if (result.DetectedType != ProjectType.Unknown)
        {
            recommendations.Add($"Detected {result.DetectedType} project - pipeline will be optimized accordingly");
        }

        if (result.Framework.Name != "Unknown")
        {
            recommendations.Add($"Framework {result.Framework.Name} detected - including framework-specific optimizations");
        }

        if (result.Dependencies.CacheRecommendation.IsRecommended)
        {
            recommendations.Add($"Caching recommended: {result.Dependencies.CacheRecommendation.Reason}");
        }

        if (result.Docker?.HasDockerConfig == true)
        {
            recommendations.Add("Docker configuration detected - enabling container-based builds");
        }

        if (result.Deployment?.HasDeploymentConfig == true)
        {
            recommendations.Add("Deployment configuration detected - including deployment stages");
        }

        if (result.Environment?.Environments.Any() == true)
        {
            var envCount = result.Environment.Environments.Count;
            recommendations.Add($"{envCount} environment(s) detected - configuring environment-specific deployments");
        }
    }

    private AnalysisConfidence CombineConfidence(AnalysisConfidence confidence1, AnalysisConfidence confidence2)
    {
        var average = ((int)confidence1 + (int)confidence2) / 2.0;
        return average switch
        {
            >= 2.5 => AnalysisConfidence.High,
            >= 1.5 => AnalysisConfidence.Medium,
            _ => AnalysisConfidence.Low
        };
    }

    private bool IsPackageFile(string fileName)
    {
        var packageFiles = new[]
        {
            "package.json", "package-lock.json", "yarn.lock",
            "requirements.txt", "Pipfile", "pyproject.toml",
            "pom.xml", "build.gradle", "build.gradle.kts",
            "Gemfile", "Gemfile.lock",
            "composer.json", "composer.lock",
            "go.mod", "go.sum"
        };

        return packageFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase) ||
               fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public void SetAuthenticatedClient(GitLabApiClient.GitLabClient client)
    {
        _gitLabProjectService.SetAuthenticatedClient(client);
    }
}