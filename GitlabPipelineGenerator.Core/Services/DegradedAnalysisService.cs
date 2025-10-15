using GitlabPipelineGenerator.Core.Models.GitLab;
using Microsoft.Extensions.Logging;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Service for performing degraded project analysis when GitLab API is unavailable
/// </summary>
public class DegradedAnalysisService
{
    private readonly ILogger<DegradedAnalysisService> _logger;

    public DegradedAnalysisService(ILogger<DegradedAnalysisService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a basic analysis result using cached data or minimal defaults
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="cachedResult">Previously cached analysis result</param>
    /// <param name="manualOptions">Manual configuration options provided by user</param>
    /// <returns>Degraded analysis result</returns>
    public ProjectAnalysisResult CreateDegradedAnalysis(
        string projectId,
        CachedAnalysisResult? cachedResult,
        ManualAnalysisOptions? manualOptions = null)
    {
        _logger.LogInformation("Creating degraded analysis for project: {ProjectId}", projectId);

        var result = new ProjectAnalysisResult
        {
            ProjectId = projectId,
            AnalysisMode = AnalysisMode.Degraded,
            Confidence = AnalysisConfidence.Low,
            Warnings = new List<AnalysisWarning>()
        };

        // Use cached data if available
        if (cachedResult != null)
        {
            _logger.LogDebug("Using cached analysis data from {CachedAt}", cachedResult.CachedAt);
            
            result.DetectedType = cachedResult.Result.DetectedType;
            result.Framework = cachedResult.Result.Framework;
            result.BuildConfig = cachedResult.Result.BuildConfig;
            result.Dependencies = cachedResult.Result.Dependencies;
            result.Deployment = cachedResult.Result.Deployment;
            result.ExistingCI = cachedResult.Result.ExistingCI;
            
            result.Warnings.Add(new AnalysisWarning
            {
                Type = WarningType.CachedData,
                Message = $"Using cached analysis data from {cachedResult.CachedAt:yyyy-MM-dd HH:mm:ss} UTC",
                Severity = WarningSeverity.Info
            });
        }
        else
        {
            // Create minimal defaults
            _logger.LogDebug("No cached data available, using minimal defaults");
            
            result.DetectedType = ProjectType.Unknown;
            result.Framework = new FrameworkInfo
            {
                Name = "Unknown",
                Version = null,
                DetectedFeatures = new List<string>(),
                Configuration = new Dictionary<string, string>()
            };
            
            result.BuildConfig = new BuildConfiguration
            {
                BuildTool = "Unknown",
                BuildCommands = new List<string>(),
                TestCommands = new List<string>(),
                ArtifactPaths = new List<string>()
            };
            
            result.Dependencies = new DependencyInfo
            {
                Dependencies = new List<PackageDependency>(),
                DevDependencies = new List<PackageDependency>(),
                Runtime = new RuntimeInfo { Name = "Unknown", Version = null },
                CacheRecommendation = new CacheRecommendation
                {
                    IsRecommended = false,
                    Configuration = new CacheConfiguration
                    {
                        CachePaths = new List<string>(),
                        CacheKey = "default"
                    }
                }
            };
            
            result.Deployment = new DeploymentInfo
            {
                HasDeploymentConfig = false,
                DetectedEnvironments = new List<string>(),
                Configuration = new DeploymentConfiguration()
            };

            result.Warnings.Add(new AnalysisWarning
            {
                Type = WarningType.NoData,
                Message = "No cached data available, using minimal defaults",
                Severity = WarningSeverity.Warning
            });
        }

        // Apply manual overrides if provided
        if (manualOptions != null)
        {
            ApplyManualOverrides(result, manualOptions);
        }

        // Add general degraded mode warning
        result.Warnings.Add(new AnalysisWarning
        {
            Type = WarningType.DegradedMode,
            Message = "Analysis performed in degraded mode due to GitLab API unavailability",
            Severity = WarningSeverity.Warning
        });

        _logger.LogInformation("Degraded analysis completed for project: {ProjectId}", projectId);
        return result;
    }

    /// <summary>
    /// Creates a fallback analysis based on common project patterns
    /// </summary>
    /// <param name="projectName">Project name for pattern detection</param>
    /// <param name="projectPath">Project path for pattern detection</param>
    /// <param name="manualOptions">Manual configuration options</param>
    /// <returns>Pattern-based analysis result</returns>
    public ProjectAnalysisResult CreatePatternBasedAnalysis(
        string projectName,
        string projectPath,
        ManualAnalysisOptions? manualOptions = null)
    {
        _logger.LogInformation("Creating pattern-based analysis for project: {ProjectName}", projectName);

        var result = new ProjectAnalysisResult
        {
            ProjectId = projectPath,
            AnalysisMode = AnalysisMode.PatternBased,
            Confidence = AnalysisConfidence.Low,
            Warnings = new List<AnalysisWarning>()
        };

        // Detect project type based on name patterns
        result.DetectedType = DetectProjectTypeFromName(projectName, projectPath);
        
        // Create framework info based on detected type
        result.Framework = CreateFrameworkInfoForType(result.DetectedType);
        
        // Create build configuration based on detected type
        result.BuildConfig = CreateBuildConfigForType(result.DetectedType);
        
        // Create basic dependency info
        result.Dependencies = CreateDependencyInfoForType(result.DetectedType);
        
        // Create deployment info
        result.Deployment = new DeploymentInfo
        {
            HasDeploymentConfig = false,
            DetectedEnvironments = new List<string>(),
            Configuration = new DeploymentConfiguration()
        };

        // Apply manual overrides if provided
        if (manualOptions != null)
        {
            ApplyManualOverrides(result, manualOptions);
        }

        result.Warnings.Add(new AnalysisWarning
        {
            Type = WarningType.PatternBased,
            Message = "Analysis based on project name patterns - may not be accurate",
            Severity = WarningSeverity.Warning
        });

        _logger.LogInformation("Pattern-based analysis completed for project: {ProjectName}", projectName);
        return result;
    }

    private ProjectType DetectProjectTypeFromName(string projectName, string projectPath)
    {
        var lowerName = projectName.ToLowerInvariant();
        var lowerPath = projectPath.ToLowerInvariant();

        // .NET patterns
        if (lowerName.Contains("dotnet") || lowerName.Contains(".net") || 
            lowerName.Contains("csharp") || lowerName.Contains("c#") ||
            lowerPath.Contains("dotnet") || lowerPath.Contains("csharp"))
        {
            return ProjectType.DotNet;
        }

        // Node.js patterns
        if (lowerName.Contains("node") || lowerName.Contains("npm") || 
            lowerName.Contains("javascript") || lowerName.Contains("js") ||
            lowerName.Contains("typescript") || lowerName.Contains("ts") ||
            lowerName.Contains("react") || lowerName.Contains("vue") || lowerName.Contains("angular"))
        {
            return ProjectType.NodeJs;
        }

        // Python patterns
        if (lowerName.Contains("python") || lowerName.Contains("py") ||
            lowerName.Contains("django") || lowerName.Contains("flask") ||
            lowerName.Contains("fastapi") || lowerName.Contains("pandas"))
        {
            return ProjectType.Python;
        }

        // Java patterns
        if (lowerName.Contains("java") || lowerName.Contains("spring") ||
            lowerName.Contains("maven") || lowerName.Contains("gradle"))
        {
            return ProjectType.Java;
        }

        // Go patterns
        if (lowerName.Contains("golang") || lowerName.Contains("go-"))
        {
            return ProjectType.Go;
        }

        // Rust patterns
        if (lowerName.Contains("rust") || lowerName.Contains("cargo"))
        {
            return ProjectType.Rust;
        }

        // PHP patterns
        if (lowerName.Contains("php") || lowerName.Contains("laravel") ||
            lowerName.Contains("symfony") || lowerName.Contains("composer"))
        {
            return ProjectType.PHP;
        }

        return ProjectType.Unknown;
    }

    private FrameworkInfo CreateFrameworkInfoForType(ProjectType projectType)
    {
        return projectType switch
        {
            ProjectType.DotNet => new FrameworkInfo
            {
                Name = ".NET",
                Version = "8.0",
                DetectedFeatures = new List<string> { "Web API", "Entity Framework" },
                Configuration = new Dictionary<string, string>
                {
                    ["TargetFramework"] = "net8.0",
                    ["BuildTool"] = "dotnet"
                }
            },
            ProjectType.NodeJs => new FrameworkInfo
            {
                Name = "Node.js",
                Version = "18",
                DetectedFeatures = new List<string> { "Express", "npm" },
                Configuration = new Dictionary<string, string>
                {
                    ["Runtime"] = "node",
                    ["PackageManager"] = "npm"
                }
            },
            ProjectType.Python => new FrameworkInfo
            {
                Name = "Python",
                Version = "3.11",
                DetectedFeatures = new List<string> { "pip" },
                Configuration = new Dictionary<string, string>
                {
                    ["Runtime"] = "python",
                    ["PackageManager"] = "pip"
                }
            },
            ProjectType.Java => new FrameworkInfo
            {
                Name = "Java",
                Version = "17",
                DetectedFeatures = new List<string> { "Maven" },
                Configuration = new Dictionary<string, string>
                {
                    ["Runtime"] = "java",
                    ["BuildTool"] = "maven"
                }
            },
            _ => new FrameworkInfo
            {
                Name = "Unknown",
                Version = null,
                DetectedFeatures = new List<string>(),
                Configuration = new Dictionary<string, string>()
            }
        };
    }

    private BuildConfiguration CreateBuildConfigForType(ProjectType projectType)
    {
        return projectType switch
        {
            ProjectType.DotNet => new BuildConfiguration
            {
                BuildTool = "dotnet",
                BuildCommands = new List<string> { "dotnet restore", "dotnet build" },
                TestCommands = new List<string> { "dotnet test" },
                ArtifactPaths = new List<string> { "bin/", "obj/" }
            },
            ProjectType.NodeJs => new BuildConfiguration
            {
                BuildTool = "npm",
                BuildCommands = new List<string> { "npm ci", "npm run build" },
                TestCommands = new List<string> { "npm test" },
                ArtifactPaths = new List<string> { "node_modules/", "dist/" }
            },
            ProjectType.Python => new BuildConfiguration
            {
                BuildTool = "pip",
                BuildCommands = new List<string> { "pip install -r requirements.txt" },
                TestCommands = new List<string> { "python -m pytest" },
                ArtifactPaths = new List<string> { "__pycache__/", "*.pyc" }
            },
            ProjectType.Java => new BuildConfiguration
            {
                BuildTool = "maven",
                BuildCommands = new List<string> { "mvn clean compile" },
                TestCommands = new List<string> { "mvn test" },
                ArtifactPaths = new List<string> { "target/" }
            },
            _ => new BuildConfiguration
            {
                BuildTool = "Unknown",
                BuildCommands = new List<string>(),
                TestCommands = new List<string>(),
                ArtifactPaths = new List<string>()
            }
        };
    }

    private DependencyInfo CreateDependencyInfoForType(ProjectType projectType)
    {
        return new DependencyInfo
        {
            Dependencies = new List<PackageDependency>(),
            DevDependencies = new List<PackageDependency>(),
            Runtime = projectType switch
            {
                ProjectType.DotNet => new RuntimeInfo { Name = ".NET", Version = "8.0" },
                ProjectType.NodeJs => new RuntimeInfo { Name = "Node.js", Version = "18" },
                ProjectType.Python => new RuntimeInfo { Name = "Python", Version = "3.11" },
                ProjectType.Java => new RuntimeInfo { Name = "Java", Version = "17" },
                _ => new RuntimeInfo { Name = "Unknown", Version = null }
            },
            CacheRecommendation = new CacheRecommendation
            {
                IsRecommended = true,
                Configuration = new CacheConfiguration
                {
                    CachePaths = projectType switch
                    {
                        ProjectType.DotNet => new List<string> { "~/.nuget/packages" },
                        ProjectType.NodeJs => new List<string> { "node_modules/" },
                        ProjectType.Python => new List<string> { "~/.cache/pip" },
                        ProjectType.Java => new List<string> { "~/.m2/repository" },
                        _ => new List<string>()
                    },
                    CacheKey = "dependencies"
                }
            }
        };
    }

    private void ApplyManualOverrides(ProjectAnalysisResult result, ManualAnalysisOptions options)
    {
        if (options.ProjectType.HasValue)
        {
            result.DetectedType = options.ProjectType.Value;
            result.Confidence = AnalysisConfidence.Medium; // Increase confidence with manual input
        }

        if (!string.IsNullOrEmpty(options.FrameworkName))
        {
            result.Framework.Name = options.FrameworkName;
            if (!string.IsNullOrEmpty(options.FrameworkVersion))
            {
                result.Framework.Version = options.FrameworkVersion;
            }
        }

        if (options.BuildCommands?.Any() == true)
        {
            result.BuildConfig.BuildCommands = options.BuildCommands.ToList();
        }

        if (options.TestCommands?.Any() == true)
        {
            result.BuildConfig.TestCommands = options.TestCommands.ToList();
        }

        if (options.CachePaths?.Any() == true)
        {
            result.Dependencies.CacheRecommendation.Configuration.CachePaths = options.CachePaths.ToList();
            result.Dependencies.CacheRecommendation.IsRecommended = true;
        }

        result.Warnings.Add(new AnalysisWarning
        {
            Type = WarningType.ManualOverride,
            Message = "Manual configuration options applied",
            Severity = WarningSeverity.Info
        });
    }
}

/// <summary>
/// Manual analysis options for degraded mode
/// </summary>
public class ManualAnalysisOptions
{
    /// <summary>
    /// Manually specified project type
    /// </summary>
    public ProjectType? ProjectType { get; set; }

    /// <summary>
    /// Manually specified framework name
    /// </summary>
    public string? FrameworkName { get; set; }

    /// <summary>
    /// Manually specified framework version
    /// </summary>
    public string? FrameworkVersion { get; set; }

    /// <summary>
    /// Manually specified build commands
    /// </summary>
    public IEnumerable<string>? BuildCommands { get; set; }

    /// <summary>
    /// Manually specified test commands
    /// </summary>
    public IEnumerable<string>? TestCommands { get; set; }

    /// <summary>
    /// Manually specified cache paths
    /// </summary>
    public IEnumerable<string>? CachePaths { get; set; }
}