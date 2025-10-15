using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;
using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Service for mapping project analysis results to pipeline configuration
/// </summary>
public class AnalysisToPipelineMappingService : IAnalysisToPipelineMappingService
{
    private readonly Dictionary<ProjectType, string[]> _projectTypeTemplates;
    private readonly Dictionary<string, string> _frameworkDockerImages;

    public AnalysisToPipelineMappingService()
    {
        _projectTypeTemplates = InitializeProjectTypeTemplates();
        _frameworkDockerImages = InitializeFrameworkDockerImages();
    }

    /// <summary>
    /// Maps project analysis result to pipeline options
    /// </summary>
    /// <param name="analysisResult">Project analysis result</param>
    /// <returns>Pipeline options based on analysis</returns>
    public async Task<PipelineOptions> MapAnalysisToPipelineOptionsAsync(ProjectAnalysisResult analysisResult)
    {
        await Task.CompletedTask; // Placeholder for async operations

        var options = new PipelineOptions
        {
            ProjectType = MapProjectType(analysisResult.DetectedType),
            Stages = DetermineStages(analysisResult),
            IncludeTests = ShouldIncludeTests(analysisResult),
            IncludeDeployment = ShouldIncludeDeployment(analysisResult),
            IncludeCodeQuality = ShouldIncludeCodeQuality(analysisResult),
            IncludeSecurity = ShouldIncludeSecurity(analysisResult),
            IncludePerformance = ShouldIncludePerformance(analysisResult),
            CustomVariables = ExtractCustomVariables(analysisResult),
            DockerImage = await GetRecommendedDockerImageAsync(analysisResult.Framework, analysisResult.Docker),
            Cache = MapCacheOptions(analysisResult.Dependencies),
            Artifacts = MapArtifactOptions(analysisResult.BuildConfig),
            DeploymentEnvironments = MapDeploymentEnvironments(analysisResult.Deployment)
        };

        // Set framework-specific options
        ApplyFrameworkSpecificOptions(options, analysisResult.Framework);

        return options;
    }

    /// <summary>
    /// Gets project type specific pipeline template recommendations
    /// </summary>
    /// <param name="projectType">Detected project type</param>
    /// <param name="framework">Framework information</param>
    /// <returns>Recommended pipeline template names</returns>
    public async Task<IEnumerable<string>> GetRecommendedTemplatesAsync(ProjectType projectType, FrameworkInfo framework)
    {
        await Task.CompletedTask; // Placeholder for async operations

        var templates = new List<string>();

        // Add project type specific templates
        if (_projectTypeTemplates.TryGetValue(projectType, out var projectTemplates))
        {
            templates.AddRange(projectTemplates);
        }

        // Add framework specific templates
        var frameworkTemplates = GetFrameworkSpecificTemplates(framework);
        templates.AddRange(frameworkTemplates);

        // Add confidence-based templates
        var confidenceTemplates = GetConfidenceBasedTemplates(framework.Confidence);
        templates.AddRange(confidenceTemplates);

        return templates.Distinct();
    }

    /// <summary>
    /// Maps build configuration to pipeline jobs
    /// </summary>
    /// <param name="buildConfig">Build configuration from analysis</param>
    /// <param name="projectType">Project type</param>
    /// <returns>Dictionary of job configurations</returns>
    public async Task<Dictionary<string, Job>> MapBuildConfigurationToJobsAsync(BuildConfiguration buildConfig, ProjectType projectType)
    {
        await Task.CompletedTask; // Placeholder for async operations

        var jobs = new Dictionary<string, Job>();

        // Create build job
        if (buildConfig.BuildCommands.Any())
        {
            jobs["build"] = CreateBuildJob(buildConfig, projectType);
        }

        // Create test job
        if (buildConfig.TestCommands.Any())
        {
            jobs["test"] = CreateTestJob(buildConfig, projectType);
        }

        // Create lint job
        if (buildConfig.LintCommands.Any())
        {
            jobs["lint"] = CreateLintJob(buildConfig, projectType);
        }

        // Create package job
        if (buildConfig.PackageCommands.Any())
        {
            jobs["package"] = CreatePackageJob(buildConfig, projectType);
        }

        return jobs;
    }

    /// <summary>
    /// Maps dependency information to caching configuration
    /// </summary>
    /// <param name="dependencies">Dependency information</param>
    /// <returns>Cache configuration for pipeline</returns>
    public async Task<Dictionary<string, object>> MapDependenciesToCacheConfigAsync(DependencyInfo dependencies)
    {
        await Task.CompletedTask; // Placeholder for async operations

        var cacheConfig = new Dictionary<string, object>();

        if (dependencies.CacheRecommendation?.IsRecommended == true)
        {
            var recommendation = dependencies.CacheRecommendation;

            cacheConfig["key"] = recommendation.CacheKey;
            cacheConfig["paths"] = recommendation.CachePaths;
            cacheConfig["policy"] = "pull-push";
            cacheConfig["when"] = "on_success";

            // Add package manager specific optimizations
            ApplyPackageManagerCacheOptimizations(cacheConfig, dependencies.PackageManager);
        }

        return cacheConfig;
    }

    /// <summary>
    /// Maps security scan recommendations to security jobs
    /// </summary>
    /// <param name="securityConfig">Security scan configuration</param>
    /// <returns>Security job configurations</returns>
    public async Task<Dictionary<string, Job>> MapSecurityConfigToJobsAsync(SecurityScanConfiguration securityConfig)
    {
        await Task.CompletedTask; // Placeholder for async operations

        var jobs = new Dictionary<string, Job>();

        if (!securityConfig.IsRecommended)
        {
            return jobs;
        }

        foreach (var scanner in securityConfig.RecommendedScanners)
        {
            var jobName = $"security-{scanner.Name.ToLowerInvariant().Replace(" ", "-")}";
            jobs[jobName] = CreateSecurityScanJob(scanner, securityConfig);
        }

        return jobs;
    }

    /// <summary>
    /// Maps deployment configuration to deployment jobs
    /// </summary>
    /// <param name="deploymentInfo">Deployment information</param>
    /// <param name="environments">Environment configurations</param>
    /// <returns>Deployment job configurations</returns>
    public async Task<Dictionary<string, Job>> MapDeploymentConfigToJobsAsync(DeploymentInfo deploymentInfo, List<DeploymentEnvironment> environments)
    {
        await Task.CompletedTask; // Placeholder for async operations

        var jobs = new Dictionary<string, Job>();

        if (!deploymentInfo.HasDeploymentConfig)
        {
            return jobs;
        }

        foreach (var environment in environments)
        {
            var jobName = $"deploy-{environment.Name.ToLowerInvariant()}";
            jobs[jobName] = CreateDeploymentJob(environment, deploymentInfo);
        }

        return jobs;
    }

    /// <summary>
    /// Gets framework-specific Docker image recommendations
    /// </summary>
    /// <param name="framework">Framework information</param>
    /// <param name="dockerConfig">Docker configuration (if available)</param>
    /// <returns>Recommended Docker image</returns>
    public async Task<string?> GetRecommendedDockerImageAsync(FrameworkInfo framework, DockerConfiguration? dockerConfig = null)
    {
        await Task.CompletedTask; // Placeholder for async operations

        // Use detected Docker image if available
        if (dockerConfig != null && !string.IsNullOrEmpty(dockerConfig.BaseImage))
        {
            return dockerConfig.BaseImage;
        }

        // Use framework-specific image
        var frameworkKey = framework.Name.ToLowerInvariant();
        if (_frameworkDockerImages.TryGetValue(frameworkKey, out var image))
        {
            // Replace version placeholder if framework version is available
            if (!string.IsNullOrEmpty(framework.Version))
            {
                image = image.Replace("{version}", framework.Version);
            }
            return image;
        }

        // Fallback based on project type patterns
        return GetFallbackDockerImage(framework);
    }

    /// <summary>
    /// Maps analysis confidence to pipeline generation strategy
    /// </summary>
    /// <param name="confidence">Analysis confidence level</param>
    /// <returns>Recommended generation strategy</returns>
    public async Task<PipelineGenerationStrategy> MapConfidenceToStrategyAsync(AnalysisConfidence confidence)
    {
        await Task.CompletedTask; // Placeholder for async operations

        return confidence switch
        {
            AnalysisConfidence.High => PipelineGenerationStrategy.Aggressive,
            AnalysisConfidence.Medium => PipelineGenerationStrategy.Balanced,
            AnalysisConfidence.Low => PipelineGenerationStrategy.Conservative,
            _ => PipelineGenerationStrategy.Conservative
        };
    }

    #region Private Helper Methods

    /// <summary>
    /// Initializes project type to template mappings
    /// </summary>
    private static Dictionary<ProjectType, string[]> InitializeProjectTypeTemplates()
    {
        return new Dictionary<ProjectType, string[]>
        {
            [ProjectType.DotNet] = new[] { "dotnet-core", "dotnet-framework", "aspnet-core" },
            [ProjectType.NodeJs] = new[] { "nodejs", "react", "vue", "angular", "express" },
            [ProjectType.Python] = new[] { "python", "django", "flask", "fastapi" },
            [ProjectType.Java] = new[] { "java", "spring-boot", "maven", "gradle" },
            [ProjectType.Go] = new[] { "golang", "go-modules" },
            [ProjectType.Ruby] = new[] { "ruby", "rails", "sinatra" },
            [ProjectType.PHP] = new[] { "php", "laravel", "symfony", "composer" },
            [ProjectType.Docker] = new[] { "docker", "docker-compose", "kubernetes" }
        };
    }

    /// <summary>
    /// Initializes framework to Docker image mappings
    /// </summary>
    private static Dictionary<string, string> InitializeFrameworkDockerImages()
    {
        return new Dictionary<string, string>
        {
            ["dotnet"] = "mcr.microsoft.com/dotnet/sdk:{version}",
            [".net"] = "mcr.microsoft.com/dotnet/sdk:{version}",
            ["node"] = "node:{version}-alpine",
            ["nodejs"] = "node:{version}-alpine",
            ["python"] = "python:{version}-slim",
            ["java"] = "openjdk:{version}-jdk-slim",
            ["go"] = "golang:{version}-alpine",
            ["golang"] = "golang:{version}-alpine",
            ["ruby"] = "ruby:{version}-slim",
            ["php"] = "php:{version}-cli",
            ["rust"] = "rust:{version}-slim"
        };
    }

    /// <summary>
    /// Maps project type from analysis to pipeline project type
    /// </summary>
    private static string MapProjectType(ProjectType projectType)
    {
        return projectType switch
        {
            ProjectType.DotNet => "dotnet",
            ProjectType.NodeJs => "nodejs",
            ProjectType.Python => "python",
            ProjectType.Java => "java",
            ProjectType.Go => "go",
            ProjectType.Ruby => "ruby",
            ProjectType.PHP => "php",
            ProjectType.Docker => "docker",
            _ => "generic"
        };
    }

    /// <summary>
    /// Determines pipeline stages based on analysis
    /// </summary>
    private static List<string> DetermineStages(ProjectAnalysisResult analysis)
    {
        var stages = new List<string> { "build" };

        if (analysis.BuildConfig.TestCommands.Any() || 
            analysis.Framework.DetectedFeatures.Any(f => f.Contains("test", StringComparison.OrdinalIgnoreCase)))
        {
            stages.Add("test");
        }

        if (analysis.Dependencies.SecurityScanRecommendation?.IsRecommended == true)
        {
            stages.Add("security");
        }

        if (analysis.Deployment.HasDeploymentConfig)
        {
            stages.Add("deploy");
        }

        return stages;
    }

    /// <summary>
    /// Determines if tests should be included
    /// </summary>
    private static bool ShouldIncludeTests(ProjectAnalysisResult analysis)
    {
        return analysis.BuildConfig.TestCommands.Any() ||
               analysis.Framework.DetectedFeatures.Any(f => f.Contains("test", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines if deployment should be included
    /// </summary>
    private static bool ShouldIncludeDeployment(ProjectAnalysisResult analysis)
    {
        return analysis.Deployment.HasDeploymentConfig ||
               analysis.Docker != null ||
               analysis.Environment?.Environments.Any() == true;
    }

    /// <summary>
    /// Determines if code quality should be included
    /// </summary>
    private static bool ShouldIncludeCodeQuality(ProjectAnalysisResult analysis)
    {
        return analysis.DetectedType == ProjectType.DotNet ||
               analysis.DetectedType == ProjectType.NodeJs ||
               analysis.DetectedType == ProjectType.Python ||
               analysis.DetectedType == ProjectType.Java;
    }

    /// <summary>
    /// Determines if security scanning should be included
    /// </summary>
    private static bool ShouldIncludeSecurity(ProjectAnalysisResult analysis)
    {
        return analysis.Dependencies.SecurityScanRecommendation?.IsRecommended == true ||
               analysis.Dependencies.HasSecuritySensitiveDependencies;
    }

    /// <summary>
    /// Determines if performance testing should be included
    /// </summary>
    private static bool ShouldIncludePerformance(ProjectAnalysisResult analysis)
    {
        return analysis.Framework.DetectedFeatures.Any(f => 
            f.Contains("web", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("api", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("service", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Extracts custom variables from analysis
    /// </summary>
    private static Dictionary<string, string> ExtractCustomVariables(ProjectAnalysisResult analysis)
    {
        var variables = new Dictionary<string, string>();

        // Add framework version
        if (!string.IsNullOrEmpty(analysis.Framework.Version))
        {
            var versionKey = GetFrameworkVersionVariable(analysis.Framework.Name);
            if (!string.IsNullOrEmpty(versionKey))
            {
                variables[versionKey] = analysis.Framework.Version;
            }
        }

        // Add build tool information
        if (!string.IsNullOrEmpty(analysis.BuildConfig.BuildTool))
        {
            variables["BUILD_TOOL"] = analysis.BuildConfig.BuildTool;
            
            if (!string.IsNullOrEmpty(analysis.BuildConfig.BuildToolVersion))
            {
                variables["BUILD_TOOL_VERSION"] = analysis.BuildConfig.BuildToolVersion;
            }
        }

        // Add package manager information
        if (!string.IsNullOrEmpty(analysis.Dependencies.PackageManager))
        {
            variables["PACKAGE_MANAGER"] = analysis.Dependencies.PackageManager;
        }

        // Add dependency count for optimization decisions
        variables["DEPENDENCY_COUNT"] = analysis.Dependencies.TotalDependencies.ToString();

        return variables;
    }

    /// <summary>
    /// Gets framework version variable name
    /// </summary>
    private static string GetFrameworkVersionVariable(string frameworkName)
    {
        return frameworkName.ToLowerInvariant() switch
        {
            var name when name.Contains("dotnet") => "DOTNET_VERSION",
            var name when name.Contains("node") => "NODE_VERSION",
            var name when name.Contains("python") => "PYTHON_VERSION",
            var name when name.Contains("java") => "JAVA_VERSION",
            var name when name.Contains("go") => "GO_VERSION",
            var name when name.Contains("ruby") => "RUBY_VERSION",
            var name when name.Contains("php") => "PHP_VERSION",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Applies framework-specific options
    /// </summary>
    private static void ApplyFrameworkSpecificOptions(PipelineOptions options, FrameworkInfo framework)
    {
        switch (framework.Name.ToLowerInvariant())
        {
            case var name when name.Contains("dotnet"):
                if (!string.IsNullOrEmpty(framework.Version))
                {
                    options.DotNetVersion = framework.Version;
                }
                break;
        }
    }

    /// <summary>
    /// Maps cache options from dependencies
    /// </summary>
    private static CacheOptions? MapCacheOptions(DependencyInfo dependencies)
    {
        if (dependencies.CacheRecommendation?.IsRecommended != true)
        {
            return null;
        }

        var recommendation = dependencies.CacheRecommendation;
        return new CacheOptions
        {
            Key = recommendation.CacheKey,
            Paths = recommendation.CachePaths.ToList(),
            Policy = "pull-push",
            When = "on_success"
        };
    }

    /// <summary>
    /// Maps artifact options from build configuration
    /// </summary>
    private static ArtifactOptions? MapArtifactOptions(BuildConfiguration buildConfig)
    {
        if (!buildConfig.ArtifactPaths.Any())
        {
            return null;
        }

        return new ArtifactOptions
        {
            DefaultPaths = buildConfig.ArtifactPaths.ToList(),
            DefaultExpireIn = "1 week",
            IncludeTestReports = buildConfig.TestCommands.Any(),
            IncludeCoverageReports = buildConfig.TestCommands.Any()
        };
    }

    /// <summary>
    /// Maps deployment environments from deployment info
    /// </summary>
    private static List<DeploymentEnvironment> MapDeploymentEnvironments(DeploymentInfo deploymentInfo)
    {
        var environments = new List<DeploymentEnvironment>();

        foreach (var envName in deploymentInfo.DetectedEnvironments)
        {
            environments.Add(new DeploymentEnvironment
            {
                Name = envName,
                IsManual = envName.Equals("production", StringComparison.OrdinalIgnoreCase),
                AutoDeployPattern = envName.Equals("development", StringComparison.OrdinalIgnoreCase) ? "main" : null
            });
        }

        return environments;
    }

    /// <summary>
    /// Gets framework-specific templates
    /// </summary>
    private static IEnumerable<string> GetFrameworkSpecificTemplates(FrameworkInfo framework)
    {
        var templates = new List<string>();

        var frameworkName = framework.Name.ToLowerInvariant();
        
        if (frameworkName.Contains("react"))
            templates.Add("react");
        else if (frameworkName.Contains("vue"))
            templates.Add("vue");
        else if (frameworkName.Contains("angular"))
            templates.Add("angular");
        else if (frameworkName.Contains("spring"))
            templates.Add("spring-boot");
        else if (frameworkName.Contains("django"))
            templates.Add("django");
        else if (frameworkName.Contains("flask"))
            templates.Add("flask");
        else if (frameworkName.Contains("rails"))
            templates.Add("rails");

        return templates;
    }

    /// <summary>
    /// Gets confidence-based templates
    /// </summary>
    private static IEnumerable<string> GetConfidenceBasedTemplates(AnalysisConfidence confidence)
    {
        return confidence switch
        {
            AnalysisConfidence.High => new[] { "optimized", "advanced" },
            AnalysisConfidence.Medium => new[] { "standard", "balanced" },
            AnalysisConfidence.Low => new[] { "basic", "minimal" },
            _ => new[] { "generic" }
        };
    }

    /// <summary>
    /// Creates a build job from build configuration
    /// </summary>
    private static Job CreateBuildJob(BuildConfiguration buildConfig, ProjectType projectType)
    {
        var job = new Job
        {
            Stage = "build",
            Script = buildConfig.BuildCommands.ToList()
        };

        // Add build-specific artifacts
        if (buildConfig.ArtifactPaths.Any())
        {
            job.Artifacts = new JobArtifacts
            {
                Paths = buildConfig.ArtifactPaths.ToList(),
                ExpireIn = "1 hour"
            };
        }

        // Add environment variables
        if (buildConfig.EnvironmentVariables.Any())
        {
            job.Variables = buildConfig.EnvironmentVariables.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
        }

        return job;
    }

    /// <summary>
    /// Creates a test job from build configuration
    /// </summary>
    private static Job CreateTestJob(BuildConfiguration buildConfig, ProjectType projectType)
    {
        var job = new Job
        {
            Stage = "test",
            Script = buildConfig.TestCommands.ToList()
        };

        // Add test artifacts
        job.Artifacts = new JobArtifacts
        {
            Reports = new ArtifactReports
            {
                Junit = new List<string> { "test-results.xml" },
                Cobertura = new List<string> { "coverage.xml" }
            },
            ExpireIn = "1 week"
        };

        return job;
    }

    /// <summary>
    /// Creates a lint job from build configuration
    /// </summary>
    private static Job CreateLintJob(BuildConfiguration buildConfig, ProjectType projectType)
    {
        return new Job
        {
            Stage = "test",
            Script = buildConfig.LintCommands.ToList(),
            AllowFailure = true
        };
    }

    /// <summary>
    /// Creates a package job from build configuration
    /// </summary>
    private static Job CreatePackageJob(BuildConfiguration buildConfig, ProjectType projectType)
    {
        var job = new Job
        {
            Stage = "build",
            Script = buildConfig.PackageCommands.ToList()
        };

        // Add package artifacts
        if (buildConfig.OutputDirectories.Any())
        {
            job.Artifacts = new JobArtifacts
            {
                Paths = buildConfig.OutputDirectories.ToList(),
                ExpireIn = "1 day"
            };
        }

        return job;
    }

    /// <summary>
    /// Applies package manager specific cache optimizations
    /// </summary>
    private static void ApplyPackageManagerCacheOptimizations(Dictionary<string, object> cacheConfig, string packageManager)
    {
        switch (packageManager.ToLowerInvariant())
        {
            case "npm":
                cacheConfig["key"] = "$CI_COMMIT_REF_SLUG-npm";
                if (!cacheConfig.ContainsKey("paths"))
                {
                    cacheConfig["paths"] = new List<string> { "node_modules/", ".npm/" };
                }
                break;

            case "yarn":
                cacheConfig["key"] = "$CI_COMMIT_REF_SLUG-yarn";
                if (!cacheConfig.ContainsKey("paths"))
                {
                    cacheConfig["paths"] = new List<string> { "node_modules/", ".yarn-cache/" };
                }
                break;

            case "pip":
                cacheConfig["key"] = "$CI_COMMIT_REF_SLUG-pip";
                if (!cacheConfig.ContainsKey("paths"))
                {
                    cacheConfig["paths"] = new List<string> { ".cache/pip/" };
                }
                break;

            case "nuget":
                cacheConfig["key"] = "$CI_COMMIT_REF_SLUG-nuget";
                if (!cacheConfig.ContainsKey("paths"))
                {
                    cacheConfig["paths"] = new List<string> { "~/.nuget/packages/" };
                }
                break;
        }
    }

    /// <summary>
    /// Creates a security scan job
    /// </summary>
    private static Job CreateSecurityScanJob(SecurityScanner scanner, SecurityScanConfiguration securityConfig)
    {
        var job = new Job
        {
            Stage = "security",
            AllowFailure = true
        };

        switch (scanner.Type)
        {
            case SecurityScanType.SAST:
                job.Image = new JobImage { Name = "registry.gitlab.com/gitlab-org/security-products/analyzers/semgrep:latest" };
                job.Script = new List<string> { "semgrep --config=auto --json --output=sast-report.json ." };
                job.Artifacts = new JobArtifacts
                {
                    Reports = new ArtifactReports { Sast = new List<string> { "sast-report.json" } },
                    ExpireIn = "1 week"
                };
                break;

            case SecurityScanType.DependencyScanning:
                job.Image = new JobImage { Name = "registry.gitlab.com/gitlab-org/security-products/analyzers/gemnasium:latest" };
                job.Script = new List<string> { "gemnasium-dependency-scanning" };
                job.Artifacts = new JobArtifacts
                {
                    Reports = new ArtifactReports { DependencyScanning = new List<string> { "dependency-scanning-report.json" } },
                    ExpireIn = "1 week"
                };
                break;

            case SecurityScanType.SecretDetection:
                job.Image = new JobImage { Name = "registry.gitlab.com/gitlab-org/security-products/analyzers/secrets:latest" };
                job.Script = new List<string> { "secrets-analyzer" };
                job.Artifacts = new JobArtifacts
                {
                    ExpireIn = "1 week"
                };
                break;

            default:
                job.Script = new List<string> { $"echo 'Running {scanner.Name} security scan'" };
                break;
        }

        return job;
    }

    /// <summary>
    /// Creates a deployment job
    /// </summary>
    private static Job CreateDeploymentJob(DeploymentEnvironment environment, DeploymentInfo deploymentInfo)
    {
        var job = new Job
        {
            Stage = "deploy",
            Script = deploymentInfo.DeploymentCommands.Any() 
                ? deploymentInfo.DeploymentCommands.ToList()
                : new List<string> { $"echo 'Deploying to {environment.Name}'" },
            Environment = new JobEnvironment
            {
                Name = environment.Name,
                Url = environment.Url
            }
        };

        // Set manual deployment for production
        if (environment.IsManual)
        {
            job.When = "manual";
        }

        // Add environment-specific variables
        if (environment.Variables.Any())
        {
            job.Variables = environment.Variables.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
        }

        // Add deployment rules
        if (!string.IsNullOrEmpty(environment.AutoDeployPattern))
        {
            job.Rules = new List<Rule>
            {
                new Rule
                {
                    If = $"$CI_COMMIT_BRANCH == \"{environment.AutoDeployPattern}\"",
                    When = "always"
                }
            };
        }

        return job;
    }

    /// <summary>
    /// Gets fallback Docker image based on framework patterns
    /// </summary>
    private static string? GetFallbackDockerImage(FrameworkInfo framework)
    {
        var frameworkName = framework.Name.ToLowerInvariant();

        if (frameworkName.Contains("net") || frameworkName.Contains("c#"))
            return "mcr.microsoft.com/dotnet/sdk:8.0";
        
        if (frameworkName.Contains("node") || frameworkName.Contains("javascript") || frameworkName.Contains("typescript"))
            return "node:18-alpine";
        
        if (frameworkName.Contains("python"))
            return "python:3.11-slim";
        
        if (frameworkName.Contains("java"))
            return "openjdk:17-jdk-slim";
        
        if (frameworkName.Contains("go"))
            return "golang:1.21-alpine";
        
        if (frameworkName.Contains("ruby"))
            return "ruby:3.2-slim";
        
        if (frameworkName.Contains("php"))
            return "php:8.2-cli";

        return null;
    }

    #endregion
}