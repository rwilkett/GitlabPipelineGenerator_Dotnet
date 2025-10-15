using System.ComponentModel.DataAnnotations;
using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Models;

/// <summary>
/// Represents configuration options for pipeline generation
/// </summary>
public class PipelineOptions
{
    /// <summary>
    /// Type of project (e.g., "dotnet", "nodejs", "python", "docker")
    /// </summary>
    [Required(ErrorMessage = "Project type is required")]
    public string ProjectType { get; set; } = string.Empty;

    /// <summary>
    /// List of stages to include in the pipeline
    /// </summary>
    [Required(ErrorMessage = "At least one stage is required")]
    [MinLength(1, ErrorMessage = "At least one stage must be specified")]
    public List<string> Stages { get; set; } = new() { "build", "test", "deploy" };

    /// <summary>
    /// .NET version to use (e.g., "8.0", "9.0")
    /// </summary>
    public string? DotNetVersion { get; set; }

    /// <summary>
    /// Whether to include test jobs in the pipeline
    /// </summary>
    public bool IncludeTests { get; set; } = true;

    /// <summary>
    /// Whether to include deployment jobs in the pipeline
    /// </summary>
    public bool IncludeDeployment { get; set; } = true;

    /// <summary>
    /// Custom variables to include in the pipeline
    /// </summary>
    public Dictionary<string, string> CustomVariables { get; set; } = new();

    /// <summary>
    /// Docker image to use for jobs (if not specified, uses default for project type)
    /// </summary>
    public string? DockerImage { get; set; }

    /// <summary>
    /// Tags to use for runner selection
    /// </summary>
    public List<string> RunnerTags { get; set; } = new();

    /// <summary>
    /// Whether to include code quality checks
    /// </summary>
    public bool IncludeCodeQuality { get; set; } = false;

    /// <summary>
    /// Whether to include security scanning
    /// </summary>
    public bool IncludeSecurity { get; set; } = false;

    /// <summary>
    /// Whether to include performance testing
    /// </summary>
    public bool IncludePerformance { get; set; } = false;

    /// <summary>
    /// Deployment environments configuration
    /// </summary>
    public List<DeploymentEnvironment> DeploymentEnvironments { get; set; } = new();

    /// <summary>
    /// Cache configuration options
    /// </summary>
    public CacheOptions? Cache { get; set; }

    /// <summary>
    /// Artifact configuration options
    /// </summary>
    public ArtifactOptions? Artifacts { get; set; }

    /// <summary>
    /// Notification settings
    /// </summary>
    public NotificationOptions? Notifications { get; set; }

    /// <summary>
    /// Custom job configurations
    /// </summary>
    public List<CustomJobOptions> CustomJobs { get; set; } = new();

    /// <summary>
    /// Project analysis result for intelligent pipeline generation
    /// </summary>
    public ProjectAnalysisResult? AnalysisResult { get; set; }

    /// <summary>
    /// Whether to use analysis results for intelligent defaults
    /// </summary>
    public bool UseAnalysisDefaults { get; set; } = true;

    /// <summary>
    /// Configuration merge strategy when both analysis and manual options are provided
    /// </summary>
    public ConfigurationMergeStrategy MergeStrategy { get; set; } = ConfigurationMergeStrategy.PreferManual;

    /// <summary>
    /// Validates the pipeline options and returns validation errors
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // Validate project type
        var validProjectTypes = new[] { "dotnet", "nodejs", "python", "docker", "generic" };
        if (!validProjectTypes.Contains(ProjectType.ToLowerInvariant()))
        {
            errors.Add($"Invalid project type '{ProjectType}'. Valid types are: {string.Join(", ", validProjectTypes)}");
        }

        // Validate stages
        if (!Stages.Any())
        {
            errors.Add("At least one stage must be specified");
        }

        var validStages = new[] { "build", "test", "deploy", "review", "staging", "production", "cleanup" };
        foreach (var stage in Stages)
        {
            if (string.IsNullOrWhiteSpace(stage))
            {
                errors.Add("Stage names cannot be empty or whitespace");
            }
        }

        // Validate .NET version if specified
        if (!string.IsNullOrEmpty(DotNetVersion))
        {
            var validVersions = new[] { "6.0", "7.0", "8.0", "9.0" };
            if (!validVersions.Contains(DotNetVersion))
            {
                errors.Add($"Invalid .NET version '{DotNetVersion}'. Valid versions are: {string.Join(", ", validVersions)}");
            }
        }

        // Validate custom variables
        foreach (var variable in CustomVariables)
        {
            if (string.IsNullOrWhiteSpace(variable.Key))
            {
                errors.Add("Variable names cannot be empty or whitespace");
            }
            if (variable.Key.Contains(" "))
            {
                errors.Add($"Variable name '{variable.Key}' cannot contain spaces");
            }
        }

        // Validate deployment environments
        foreach (var env in DeploymentEnvironments)
        {
            var envErrors = env.Validate();
            errors.AddRange(envErrors);
        }

        // Validate custom jobs
        foreach (var job in CustomJobs)
        {
            var jobErrors = job.Validate();
            errors.AddRange(jobErrors);
        }

        return errors;
    }
}

/// <summary>
/// Represents a deployment environment configuration
/// </summary>
public class DeploymentEnvironment
{
    /// <summary>
    /// Environment name (e.g., "staging", "production")
    /// </summary>
    [Required(ErrorMessage = "Environment name is required")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Environment URL
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Whether deployment to this environment is manual
    /// </summary>
    public bool IsManual { get; set; } = false;

    /// <summary>
    /// Branch or tag pattern for automatic deployment
    /// </summary>
    public string? AutoDeployPattern { get; set; }

    /// <summary>
    /// Environment-specific variables
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>
    /// Kubernetes namespace for this environment
    /// </summary>
    public string? KubernetesNamespace { get; set; }

    /// <summary>
    /// Whether this environment should auto-stop
    /// </summary>
    public bool AutoStop { get; set; } = false;

    /// <summary>
    /// Auto-stop duration (e.g., "1 day", "2 weeks")
    /// </summary>
    public string? AutoStopIn { get; set; }

    /// <summary>
    /// Validates the deployment environment configuration
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Environment name is required");
        }

        if (!string.IsNullOrEmpty(Url))
        {
            if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri))
            {
                errors.Add($"Invalid URL format for environment '{Name}': {Url}");
            }
            else if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                errors.Add($"Invalid URL format for environment '{Name}': {Url}");
            }
        }

        if (AutoStop && string.IsNullOrEmpty(AutoStopIn))
        {
            errors.Add($"AutoStopIn is required when AutoStop is enabled for environment '{Name}'");
        }

        return errors;
    }
}

/// <summary>
/// Represents cache configuration options
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Cache key pattern
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Paths to cache
    /// </summary>
    public List<string> Paths { get; set; } = new();

    /// <summary>
    /// Cache policy (pull, push, pull-push)
    /// </summary>
    public string Policy { get; set; } = "pull-push";

    /// <summary>
    /// When to cache (on_success, on_failure, always)
    /// </summary>
    public string When { get; set; } = "on_success";
}

/// <summary>
/// Represents artifact configuration options
/// </summary>
public class ArtifactOptions
{
    /// <summary>
    /// Default paths to include as artifacts
    /// </summary>
    public List<string> DefaultPaths { get; set; } = new();

    /// <summary>
    /// Default expiration time for artifacts
    /// </summary>
    public string DefaultExpireIn { get; set; } = "1 week";

    /// <summary>
    /// Whether to include test reports by default
    /// </summary>
    public bool IncludeTestReports { get; set; } = true;

    /// <summary>
    /// Whether to include coverage reports by default
    /// </summary>
    public bool IncludeCoverageReports { get; set; } = true;
}

/// <summary>
/// Represents notification configuration options
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// Email addresses to notify on pipeline failure
    /// </summary>
    public List<string> EmailOnFailure { get; set; } = new();

    /// <summary>
    /// Email addresses to notify on pipeline success
    /// </summary>
    public List<string> EmailOnSuccess { get; set; } = new();

    /// <summary>
    /// Slack webhook URL for notifications
    /// </summary>
    public string? SlackWebhook { get; set; }

    /// <summary>
    /// Teams webhook URL for notifications
    /// </summary>
    public string? TeamsWebhook { get; set; }
}

/// <summary>
/// Represents custom job configuration options
/// </summary>
public class CustomJobOptions
{
    /// <summary>
    /// Job name
    /// </summary>
    [Required(ErrorMessage = "Job name is required")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Stage for the job
    /// </summary>
    [Required(ErrorMessage = "Job stage is required")]
    public string Stage { get; set; } = string.Empty;

    /// <summary>
    /// Script commands for the job
    /// </summary>
    [Required(ErrorMessage = "Job script is required")]
    [MinLength(1, ErrorMessage = "At least one script command is required")]
    public List<string> Script { get; set; } = new();

    /// <summary>
    /// Before script commands
    /// </summary>
    public List<string> BeforeScript { get; set; } = new();

    /// <summary>
    /// After script commands
    /// </summary>
    public List<string> AfterScript { get; set; } = new();

    /// <summary>
    /// Job-specific variables
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>
    /// When to run the job
    /// </summary>
    public string? When { get; set; }

    /// <summary>
    /// Whether the job can fail without failing the pipeline
    /// </summary>
    public bool AllowFailure { get; set; } = false;

    /// <summary>
    /// Docker image for the job
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// Tags for runner selection
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Validates the custom job configuration
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Job name is required");
        }

        if (string.IsNullOrWhiteSpace(Stage))
        {
            errors.Add($"Stage is required for job '{Name}'");
        }

        if (!Script.Any() || Script.All(string.IsNullOrWhiteSpace))
        {
            errors.Add($"At least one script command is required for job '{Name}'");
        }

        return errors;
    }
}

/// <summary>
/// Configuration merge strategy for combining analysis results with manual options
/// </summary>
public enum ConfigurationMergeStrategy
{
    /// <summary>
    /// Manual options override analysis results
    /// </summary>
    PreferManual,

    /// <summary>
    /// Analysis results override manual options
    /// </summary>
    PreferAnalysis,

    /// <summary>
    /// Merge both with intelligent conflict resolution
    /// </summary>
    IntelligentMerge,

    /// <summary>
    /// Use only analysis results, ignore manual options
    /// </summary>
    AnalysisOnly,

    /// <summary>
    /// Use only manual options, ignore analysis results
    /// </summary>
    ManualOnly
}

/// <summary>
/// Pipeline options with intelligent defaults based on project analysis
/// </summary>
public class AnalysisBasedPipelineOptions : PipelineOptions
{
    /// <summary>
    /// Creates pipeline options with intelligent defaults from analysis results
    /// </summary>
    /// <param name="analysisResult">Project analysis result</param>
    /// <param name="baseOptions">Base manual options to merge with</param>
    /// <param name="mergeStrategy">Strategy for merging analysis and manual options</param>
    /// <returns>Pipeline options with intelligent defaults</returns>
    public static AnalysisBasedPipelineOptions CreateFromAnalysis(
        ProjectAnalysisResult analysisResult,
        PipelineOptions? baseOptions = null,
        ConfigurationMergeStrategy mergeStrategy = ConfigurationMergeStrategy.PreferManual)
    {
        var options = new AnalysisBasedPipelineOptions
        {
            AnalysisResult = analysisResult,
            MergeStrategy = mergeStrategy,
            UseAnalysisDefaults = true
        };

        // Apply intelligent defaults from analysis
        options.ApplyAnalysisDefaults(analysisResult);

        // Merge with base options if provided
        if (baseOptions != null)
        {
            options.MergeWithBaseOptions(baseOptions, mergeStrategy);
        }

        return options;
    }

    /// <summary>
    /// Applies intelligent defaults based on analysis results
    /// </summary>
    /// <param name="analysis">Project analysis result</param>
    private void ApplyAnalysisDefaults(ProjectAnalysisResult analysis)
    {
        // Set project type based on analysis
        ProjectType = MapProjectTypeFromAnalysis(analysis.DetectedType);

        // Configure stages based on detected project properties
        Stages = DetermineStagesFromAnalysis(analysis);

        // Set framework-specific options
        ApplyFrameworkDefaults(analysis.Framework);

        // Configure build and test settings
        ApplyBuildDefaults(analysis.BuildConfig);

        // Configure deployment settings
        ApplyDeploymentDefaults(analysis.Deployment);

        // Configure caching based on dependencies
        ApplyCacheDefaults(analysis.Dependencies);

        // Configure security and quality based on analysis
        ApplySecurityDefaults(analysis);

        // Configure Docker settings if detected
        ApplyDockerDefaults(analysis.Docker);

        // Set custom variables from analysis
        ApplyAnalysisVariables(analysis);
    }

    /// <summary>
    /// Merges analysis-based options with manual base options
    /// </summary>
    /// <param name="baseOptions">Manual base options</param>
    /// <param name="strategy">Merge strategy</param>
    private void MergeWithBaseOptions(PipelineOptions baseOptions, ConfigurationMergeStrategy strategy)
    {
        switch (strategy)
        {
            case ConfigurationMergeStrategy.PreferManual:
                MergePreferringManual(baseOptions);
                break;
            case ConfigurationMergeStrategy.PreferAnalysis:
                MergePreferringAnalysis(baseOptions);
                break;
            case ConfigurationMergeStrategy.IntelligentMerge:
                MergeIntelligently(baseOptions);
                break;
            case ConfigurationMergeStrategy.ManualOnly:
                CopyFromBaseOptions(baseOptions);
                break;
            case ConfigurationMergeStrategy.AnalysisOnly:
                // Keep analysis defaults, don't merge
                break;
        }
    }

    /// <summary>
    /// Maps analysis project type to pipeline project type
    /// </summary>
    private static string MapProjectTypeFromAnalysis(GitLab.ProjectType analysisType)
    {
        return analysisType switch
        {
            GitLab.ProjectType.DotNet => "dotnet",
            GitLab.ProjectType.NodeJs => "nodejs",
            GitLab.ProjectType.Python => "python",
            GitLab.ProjectType.Docker => "docker",
            GitLab.ProjectType.Java => "java",
            GitLab.ProjectType.Go => "go",
            GitLab.ProjectType.Ruby => "ruby",
            GitLab.ProjectType.PHP => "php",
            _ => "generic"
        };
    }

    /// <summary>
    /// Determines pipeline stages based on analysis results
    /// </summary>
    private static List<string> DetermineStagesFromAnalysis(ProjectAnalysisResult analysis)
    {
        var stages = new List<string> { "build" };

        // Add test stage if test frameworks detected
        if (analysis.BuildConfig.TestCommands.Any() || analysis.Framework.DetectedFeatures.Any(f => f.Contains("test")))
        {
            stages.Add("test");
        }

        // Add security stage if security scanning is recommended
        if (analysis.Dependencies.SecurityScanRecommendation?.IsRecommended == true)
        {
            stages.Add("security");
        }

        // Add deploy stage if deployment configuration detected
        if (analysis.Deployment.HasDeploymentConfig || analysis.Docker != null)
        {
            stages.Add("deploy");
        }

        return stages;
    }

    /// <summary>
    /// Applies framework-specific defaults
    /// </summary>
    private void ApplyFrameworkDefaults(FrameworkInfo framework)
    {
        // Set .NET version if detected
        if (framework.Name.Contains(".NET", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(framework.Version))
        {
            DotNetVersion = framework.Version;
        }

        // Set Docker image based on framework
        if (string.IsNullOrEmpty(DockerImage))
        {
            DockerImage = GetDefaultDockerImageForFramework(framework);
        }
    }

    /// <summary>
    /// Applies build configuration defaults
    /// </summary>
    private void ApplyBuildDefaults(BuildConfiguration buildConfig)
    {
        // Enable tests if test commands detected
        if (buildConfig.TestCommands.Any())
        {
            IncludeTests = true;
        }

        // Configure artifacts based on detected artifact paths
        if (buildConfig.ArtifactPaths.Any())
        {
            Artifacts ??= new ArtifactOptions();
            Artifacts.DefaultPaths.AddRange(buildConfig.ArtifactPaths);
        }
    }

    /// <summary>
    /// Applies deployment configuration defaults
    /// </summary>
    private void ApplyDeploymentDefaults(DeploymentInfo deployment)
    {
        if (deployment.HasDeploymentConfig)
        {
            IncludeDeployment = true;

            // Add deployment environments based on detected configuration
            foreach (var env in deployment.DetectedEnvironments)
            {
                if (!DeploymentEnvironments.Any(e => e.Name.Equals(env, StringComparison.OrdinalIgnoreCase)))
                {
                    DeploymentEnvironments.Add(new DeploymentEnvironment
                    {
                        Name = env,
                        IsManual = env.Equals("production", StringComparison.OrdinalIgnoreCase)
                    });
                }
            }
        }
    }

    /// <summary>
    /// Applies cache configuration based on dependencies
    /// </summary>
    private void ApplyCacheDefaults(DependencyInfo dependencies)
    {
        if (dependencies.CacheRecommendation?.IsRecommended == true)
        {
            Cache ??= new CacheOptions();
            
            if (dependencies.CacheRecommendation.CachePaths.Any())
            {
                Cache.Paths.AddRange(dependencies.CacheRecommendation.CachePaths);
            }

            if (!string.IsNullOrEmpty(dependencies.CacheRecommendation.CacheKey))
            {
                Cache.Key = dependencies.CacheRecommendation.CacheKey;
            }
        }
    }

    /// <summary>
    /// Applies security and quality defaults
    /// </summary>
    private void ApplySecurityDefaults(ProjectAnalysisResult analysis)
    {
        // Enable security scanning if recommended
        if (analysis.Dependencies.SecurityScanRecommendation?.IsRecommended == true)
        {
            IncludeSecurity = true;
        }

        // Enable code quality for certain project types
        if (analysis.DetectedType == GitLab.ProjectType.DotNet || 
            analysis.DetectedType == GitLab.ProjectType.NodeJs || 
            analysis.DetectedType == GitLab.ProjectType.Python)
        {
            IncludeCodeQuality = true;
        }
    }

    /// <summary>
    /// Applies Docker configuration defaults
    /// </summary>
    private void ApplyDockerDefaults(DockerConfiguration? docker)
    {
        if (docker != null)
        {
            // Use detected Docker image if available
            if (!string.IsNullOrEmpty(docker.BaseImage) && string.IsNullOrEmpty(DockerImage))
            {
                DockerImage = docker.BaseImage;
            }

            // Add Docker-related variables
            if (docker.BuildArgs.Any())
            {
                foreach (var arg in docker.BuildArgs)
                {
                    if (!CustomVariables.ContainsKey(arg.Key))
                    {
                        CustomVariables[arg.Key] = arg.Value;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Applies analysis-derived variables
    /// </summary>
    private void ApplyAnalysisVariables(ProjectAnalysisResult analysis)
    {
        // Add framework version variables
        if (!string.IsNullOrEmpty(analysis.Framework.Version))
        {
            var versionKey = $"{analysis.Framework.Name.ToUpperInvariant()}_VERSION";
            if (!CustomVariables.ContainsKey(versionKey))
            {
                CustomVariables[versionKey] = analysis.Framework.Version;
            }
        }

        // Add build tool variables
        if (!string.IsNullOrEmpty(analysis.BuildConfig.BuildTool))
        {
            var buildToolKey = "BUILD_TOOL";
            if (!CustomVariables.ContainsKey(buildToolKey))
            {
                CustomVariables[buildToolKey] = analysis.BuildConfig.BuildTool;
            }
        }
    }

    /// <summary>
    /// Gets default Docker image for framework
    /// </summary>
    private static string? GetDefaultDockerImageForFramework(FrameworkInfo framework)
    {
        return framework.Name.ToLowerInvariant() switch
        {
            var name when name.Contains("dotnet") => $"mcr.microsoft.com/dotnet/sdk:{framework.Version ?? "8.0"}",
            var name when name.Contains("node") => $"node:{framework.Version ?? "18"}-alpine",
            var name when name.Contains("python") => $"python:{framework.Version ?? "3.11"}-slim",
            var name when name.Contains("java") => $"openjdk:{framework.Version ?? "17"}-jdk-slim",
            var name when name.Contains("go") => $"golang:{framework.Version ?? "1.21"}-alpine",
            var name when name.Contains("ruby") => $"ruby:{framework.Version ?? "3.2"}-slim",
            var name when name.Contains("php") => $"php:{framework.Version ?? "8.2"}-cli",
            _ => null
        };
    }

    /// <summary>
    /// Merges options preferring manual settings
    /// </summary>
    private void MergePreferringManual(PipelineOptions baseOptions)
    {
        // Override analysis defaults with manual options where provided
        if (!string.IsNullOrEmpty(baseOptions.ProjectType) && baseOptions.ProjectType != "generic")
            ProjectType = baseOptions.ProjectType;

        if (baseOptions.Stages.Any())
            Stages = baseOptions.Stages;

        if (!string.IsNullOrEmpty(baseOptions.DotNetVersion))
            DotNetVersion = baseOptions.DotNetVersion;

        if (!string.IsNullOrEmpty(baseOptions.DockerImage))
            DockerImage = baseOptions.DockerImage;

        // Merge collections
        RunnerTags.AddRange(baseOptions.RunnerTags.Except(RunnerTags));
        DeploymentEnvironments.AddRange(baseOptions.DeploymentEnvironments.Where(e => 
            !DeploymentEnvironments.Any(existing => existing.Name.Equals(e.Name, StringComparison.OrdinalIgnoreCase))));

        // Override boolean flags
        IncludeTests = baseOptions.IncludeTests;
        IncludeDeployment = baseOptions.IncludeDeployment;
        IncludeCodeQuality = baseOptions.IncludeCodeQuality;
        IncludeSecurity = baseOptions.IncludeSecurity;
        IncludePerformance = baseOptions.IncludePerformance;

        // Merge custom variables (manual takes precedence)
        foreach (var variable in baseOptions.CustomVariables)
        {
            CustomVariables[variable.Key] = variable.Value;
        }

        // Override configuration objects if provided
        if (baseOptions.Cache != null)
            Cache = baseOptions.Cache;

        if (baseOptions.Artifacts != null)
            Artifacts = baseOptions.Artifacts;

        if (baseOptions.Notifications != null)
            Notifications = baseOptions.Notifications;

        CustomJobs.AddRange(baseOptions.CustomJobs);
    }

    /// <summary>
    /// Merges options preferring analysis results
    /// </summary>
    private void MergePreferringAnalysis(PipelineOptions baseOptions)
    {
        // Keep analysis defaults, only fill gaps with manual options
        if (string.IsNullOrEmpty(ProjectType) || ProjectType == "generic")
            ProjectType = baseOptions.ProjectType;

        if (!Stages.Any())
            Stages = baseOptions.Stages;

        if (string.IsNullOrEmpty(DotNetVersion))
            DotNetVersion = baseOptions.DotNetVersion;

        if (string.IsNullOrEmpty(DockerImage))
            DockerImage = baseOptions.DockerImage;

        // Add manual runner tags to analysis-based ones
        RunnerTags.AddRange(baseOptions.RunnerTags.Except(RunnerTags));

        // Add manual environments that weren't detected
        DeploymentEnvironments.AddRange(baseOptions.DeploymentEnvironments.Where(e => 
            !DeploymentEnvironments.Any(existing => existing.Name.Equals(e.Name, StringComparison.OrdinalIgnoreCase))));

        // Add manual variables that weren't set by analysis
        foreach (var variable in baseOptions.CustomVariables)
        {
            if (!CustomVariables.ContainsKey(variable.Key))
            {
                CustomVariables[variable.Key] = variable.Value;
            }
        }

        // Use manual configuration objects if analysis didn't set them
        Cache ??= baseOptions.Cache;
        Artifacts ??= baseOptions.Artifacts;
        Notifications ??= baseOptions.Notifications;

        CustomJobs.AddRange(baseOptions.CustomJobs);
    }

    /// <summary>
    /// Merges options using intelligent conflict resolution
    /// </summary>
    private void MergeIntelligently(PipelineOptions baseOptions)
    {
        // Use confidence-based merging - higher confidence analysis results take precedence
        var analysisConfidence = AnalysisResult?.Confidence ?? AnalysisConfidence.Low;

        if (analysisConfidence >= AnalysisConfidence.High)
        {
            MergePreferringAnalysis(baseOptions);
        }
        else if (analysisConfidence >= AnalysisConfidence.Medium)
        {
            // Merge selectively based on specific confidence areas
            MergeSelectivelyByConfidence(baseOptions);
        }
        else
        {
            MergePreferringManual(baseOptions);
        }
    }

    /// <summary>
    /// Merges selectively based on confidence in specific areas
    /// </summary>
    private void MergeSelectivelyByConfidence(PipelineOptions baseOptions)
    {
        // For medium confidence, be more selective about what to override
        
        // Project type: prefer analysis if detected with medium+ confidence
        if (AnalysisResult?.DetectedType != GitLab.ProjectType.Unknown && 
            !string.IsNullOrEmpty(baseOptions.ProjectType) && 
            baseOptions.ProjectType != "generic")
        {
            // Keep manual project type if analysis is uncertain
            ProjectType = baseOptions.ProjectType;
        }

        // Stages: merge both
        var combinedStages = Stages.Union(baseOptions.Stages).Distinct().ToList();
        Stages = combinedStages;

        // Framework versions: prefer manual if provided
        if (!string.IsNullOrEmpty(baseOptions.DotNetVersion))
            DotNetVersion = baseOptions.DotNetVersion;

        // Docker: prefer manual if provided
        if (!string.IsNullOrEmpty(baseOptions.DockerImage))
            DockerImage = baseOptions.DockerImage;

        // Boolean flags: use OR logic (enable if either suggests it)
        IncludeTests = IncludeTests || baseOptions.IncludeTests;
        IncludeDeployment = IncludeDeployment || baseOptions.IncludeDeployment;
        IncludeCodeQuality = IncludeCodeQuality || baseOptions.IncludeCodeQuality;
        IncludeSecurity = IncludeSecurity || baseOptions.IncludeSecurity;
        IncludePerformance = IncludePerformance || baseOptions.IncludePerformance;

        // Collections: merge all
        RunnerTags.AddRange(baseOptions.RunnerTags.Except(RunnerTags));
        DeploymentEnvironments.AddRange(baseOptions.DeploymentEnvironments.Where(e => 
            !DeploymentEnvironments.Any(existing => existing.Name.Equals(e.Name, StringComparison.OrdinalIgnoreCase))));

        // Variables: merge with manual taking precedence for conflicts
        foreach (var variable in baseOptions.CustomVariables)
        {
            CustomVariables[variable.Key] = variable.Value;
        }

        // Configuration objects: prefer manual if provided
        Cache = baseOptions.Cache ?? Cache;
        Artifacts = baseOptions.Artifacts ?? Artifacts;
        Notifications = baseOptions.Notifications ?? Notifications;

        CustomJobs.AddRange(baseOptions.CustomJobs);
    }

    /// <summary>
    /// Copies all options from base options (manual only mode)
    /// </summary>
    private void CopyFromBaseOptions(PipelineOptions baseOptions)
    {
        ProjectType = baseOptions.ProjectType;
        Stages = new List<string>(baseOptions.Stages);
        DotNetVersion = baseOptions.DotNetVersion;
        IncludeTests = baseOptions.IncludeTests;
        IncludeDeployment = baseOptions.IncludeDeployment;
        CustomVariables = new Dictionary<string, string>(baseOptions.CustomVariables);
        DockerImage = baseOptions.DockerImage;
        RunnerTags = new List<string>(baseOptions.RunnerTags);
        IncludeCodeQuality = baseOptions.IncludeCodeQuality;
        IncludeSecurity = baseOptions.IncludeSecurity;
        IncludePerformance = baseOptions.IncludePerformance;
        DeploymentEnvironments = new List<DeploymentEnvironment>(baseOptions.DeploymentEnvironments);
        Cache = baseOptions.Cache;
        Artifacts = baseOptions.Artifacts;
        Notifications = baseOptions.Notifications;
        CustomJobs = new List<CustomJobOptions>(baseOptions.CustomJobs);
        
        // Clear analysis-specific properties
        AnalysisResult = null;
        UseAnalysisDefaults = false;
    }
}