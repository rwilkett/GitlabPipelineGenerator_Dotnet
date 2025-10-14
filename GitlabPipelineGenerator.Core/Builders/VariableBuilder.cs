using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Builders;

/// <summary>
/// Implementation of IVariableBuilder for building pipeline variables and configurations
/// </summary>
public class VariableBuilder : IVariableBuilder
{
    private static readonly Dictionary<string, Dictionary<string, object>> DefaultVariablesByProjectType = new()
    {
        ["dotnet"] = new Dictionary<string, object>
        {
            ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "true",
            ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "true",
            ["NUGET_PACKAGES"] = "$CI_PROJECT_DIR/.nuget/packages",
            ["DOTNET_RESTORE_DISABLE_PARALLEL"] = "true"
        },
        ["nodejs"] = new Dictionary<string, object>
        {
            ["NODE_ENV"] = "production",
            ["NPM_CONFIG_CACHE"] = "$CI_PROJECT_DIR/.npm",
            ["CYPRESS_CACHE_FOLDER"] = "$CI_PROJECT_DIR/cache/Cypress"
        },
        ["python"] = new Dictionary<string, object>
        {
            ["PIP_CACHE_DIR"] = "$CI_PROJECT_DIR/.pip-cache",
            ["PYTHONPATH"] = "$CI_PROJECT_DIR",
            ["PYTHONDONTWRITEBYTECODE"] = "1"
        },
        ["docker"] = new Dictionary<string, object>
        {
            ["DOCKER_DRIVER"] = "overlay2",
            ["DOCKER_TLS_CERTDIR"] = "/certs"
        },
        ["generic"] = new Dictionary<string, object>()
    };

    /// <summary>
    /// Builds global variables for the pipeline
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Dictionary of global variables</returns>
    public async Task<Dictionary<string, object>> BuildGlobalVariablesAsync(PipelineOptions options)
    {
        await Task.CompletedTask; // Placeholder for async operations

        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var variables = new Dictionary<string, object>();

        // Start with default variables for the project type
        var defaultVariables = GetDefaultVariables(options.ProjectType);
        foreach (var variable in defaultVariables)
        {
            variables[variable.Key] = variable.Value;
        }

        // Add project-specific variables
        if (!string.IsNullOrEmpty(options.DotNetVersion) && options.ProjectType.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            variables["DOTNET_VERSION"] = options.DotNetVersion;
        }

        // Add Docker registry variables if using Docker
        if (options.ProjectType.Equals("docker", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(options.DockerImage))
        {
            variables["DOCKER_REGISTRY"] = "$CI_REGISTRY";
            variables["DOCKER_IMAGE_NAME"] = "$CI_REGISTRY_IMAGE";
            variables["DOCKER_IMAGE_TAG"] = "$CI_COMMIT_SHA";
        }

        // Add deployment-related variables
        if (options.IncludeDeployment || options.DeploymentEnvironments.Any())
        {
            variables["DEPLOY_ENABLED"] = "true";
            
            if (options.DeploymentEnvironments.Any())
            {
                var environments = string.Join(",", options.DeploymentEnvironments.Select(e => e.Name));
                variables["DEPLOYMENT_ENVIRONMENTS"] = environments;
            }
        }

        // Add feature flags
        if (options.IncludeTests)
        {
            variables["RUN_TESTS"] = "true";
        }

        if (options.IncludeCodeQuality)
        {
            variables["RUN_CODE_QUALITY"] = "true";
        }

        if (options.IncludeSecurity)
        {
            variables["RUN_SECURITY_SCAN"] = "true";
        }

        if (options.IncludePerformance)
        {
            variables["RUN_PERFORMANCE_TESTS"] = "true";
        }

        // Merge custom variables (custom variables override defaults)
        var mergedVariables = MergeVariables(variables, options.CustomVariables);

        return mergedVariables;
    }

    /// <summary>
    /// Builds default configuration that applies to all jobs
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Dictionary of default configuration</returns>
    public async Task<Dictionary<string, object>> BuildDefaultConfigurationAsync(PipelineOptions options)
    {
        await Task.CompletedTask; // Placeholder for async operations

        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var defaultConfig = new Dictionary<string, object>();

        // Set default image if specified
        if (!string.IsNullOrEmpty(options.DockerImage))
        {
            defaultConfig["image"] = options.DockerImage;
        }

        // Set default tags if specified
        if (options.RunnerTags.Any())
        {
            defaultConfig["tags"] = options.RunnerTags;
        }

        // Set default before_script for common setup
        var beforeScript = new List<string>();
        
        switch (options.ProjectType.ToLowerInvariant())
        {
            case "dotnet":
                beforeScript.AddRange(new[]
                {
                    "echo 'Setting up .NET environment'",
                    "dotnet --info"
                });
                break;

            case "nodejs":
                beforeScript.AddRange(new[]
                {
                    "echo 'Setting up Node.js environment'",
                    "node --version",
                    "npm --version"
                });
                break;

            case "python":
                beforeScript.AddRange(new[]
                {
                    "echo 'Setting up Python environment'",
                    "python --version",
                    "pip --version"
                });
                break;
        }

        if (beforeScript.Any())
        {
            defaultConfig["before_script"] = beforeScript;
        }

        // Set default cache configuration
        if (options.Cache != null)
        {
            var cacheConfig = new Dictionary<string, object>
            {
                ["key"] = options.Cache.Key ?? "$CI_COMMIT_REF_SLUG",
                ["policy"] = options.Cache.Policy,
                ["when"] = options.Cache.When
            };

            if (options.Cache.Paths.Any())
            {
                cacheConfig["paths"] = options.Cache.Paths;
            }

            defaultConfig["cache"] = cacheConfig;
        }

        // Set default retry configuration
        defaultConfig["retry"] = new Dictionary<string, object>
        {
            ["max"] = 2,
            ["when"] = new[] { "runner_system_failure", "stuck_or_timeout_failure" }
        };

        // Set default timeout
        defaultConfig["timeout"] = "1h";

        return defaultConfig;
    }

    /// <summary>
    /// Builds job-specific variables
    /// </summary>
    /// <param name="jobType">Type of job (build, test, deploy, etc.)</param>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Dictionary of job-specific variables</returns>
    public async Task<Dictionary<string, object>> BuildJobVariablesAsync(string jobType, PipelineOptions options)
    {
        await Task.CompletedTask; // Placeholder for async operations

        if (string.IsNullOrWhiteSpace(jobType))
            throw new ArgumentException("Job type cannot be null or empty", nameof(jobType));
        
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var variables = new Dictionary<string, object>();
        var normalizedJobType = jobType.ToLowerInvariant();

        switch (normalizedJobType)
        {
            case "build":
                variables = BuildBuildJobVariables(options);
                break;

            case "test":
                variables = BuildTestJobVariables(options);
                break;

            case "deploy":
                variables = BuildDeployJobVariables(options);
                break;

            case "quality":
                variables = BuildQualityJobVariables(options);
                break;

            case "security":
                variables = BuildSecurityJobVariables(options);
                break;

            case "performance":
                variables = BuildPerformanceJobVariables(options);
                break;

            case "custom":
                variables = BuildCustomJobVariables(options);
                break;
        }

        return variables;
    }

    /// <summary>
    /// Gets default variables for a specific project type
    /// </summary>
    /// <param name="projectType">Type of project</param>
    /// <returns>Dictionary of default variables</returns>
    public Dictionary<string, object> GetDefaultVariables(string projectType)
    {
        if (string.IsNullOrWhiteSpace(projectType))
            return new Dictionary<string, object>(DefaultVariablesByProjectType["generic"]);

        var normalizedProjectType = projectType.ToLowerInvariant();
        return DefaultVariablesByProjectType.TryGetValue(normalizedProjectType, out var variables)
            ? new Dictionary<string, object>(variables)
            : new Dictionary<string, object>(DefaultVariablesByProjectType["generic"]);
    }

    /// <summary>
    /// Merges custom variables with default variables
    /// </summary>
    /// <param name="defaultVariables">Default variables</param>
    /// <param name="customVariables">Custom variables to merge</param>
    /// <returns>Merged variables dictionary</returns>
    public Dictionary<string, object> MergeVariables(Dictionary<string, object> defaultVariables, Dictionary<string, string> customVariables)
    {
        if (defaultVariables == null)
            throw new ArgumentNullException(nameof(defaultVariables));
        
        if (customVariables == null)
            return new Dictionary<string, object>(defaultVariables);

        var merged = new Dictionary<string, object>(defaultVariables);

        foreach (var customVariable in customVariables)
        {
            merged[customVariable.Key] = customVariable.Value;
        }

        return merged;
    }

    #region Private Helper Methods

    private static Dictionary<string, object> BuildBuildJobVariables(PipelineOptions options)
    {
        var variables = new Dictionary<string, object>
        {
            ["BUILD_CONFIGURATION"] = "Release"
        };

        if (options.ProjectType.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            variables["DOTNET_CONFIGURATION"] = "Release";
            variables["DOTNET_VERBOSITY"] = "minimal";
        }

        if (options.ProjectType.Equals("nodejs", StringComparison.OrdinalIgnoreCase))
        {
            variables["NODE_ENV"] = "production";
        }

        return variables;
    }

    private static Dictionary<string, object> BuildTestJobVariables(PipelineOptions options)
    {
        var variables = new Dictionary<string, object>
        {
            ["TEST_CONFIGURATION"] = "Release"
        };

        if (options.ProjectType.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            variables["DOTNET_CONFIGURATION"] = "Release";
            variables["COLLECT_COVERAGE"] = "true";
            variables["LOGGER"] = "trx";
        }

        if (options.ProjectType.Equals("nodejs", StringComparison.OrdinalIgnoreCase))
        {
            variables["NODE_ENV"] = "test";
            variables["CI"] = "true";
        }

        if (options.ProjectType.Equals("python", StringComparison.OrdinalIgnoreCase))
        {
            variables["PYTEST_ADDOPTS"] = "--strict-markers --disable-warnings";
        }

        return variables;
    }

    private static Dictionary<string, object> BuildDeployJobVariables(PipelineOptions options)
    {
        var variables = new Dictionary<string, object>
        {
            ["DEPLOY_STRATEGY"] = "rolling",
            ["HEALTH_CHECK_ENABLED"] = "true"
        };

        if (options.ProjectType.Equals("docker", StringComparison.OrdinalIgnoreCase))
        {
            variables["DOCKER_REGISTRY"] = "$CI_REGISTRY";
            variables["DOCKER_IMAGE"] = "$CI_REGISTRY_IMAGE:$CI_COMMIT_SHA";
        }

        return variables;
    }

    private static Dictionary<string, object> BuildQualityJobVariables(PipelineOptions options)
    {
        var variables = new Dictionary<string, object>
        {
            ["SONAR_USER_HOME"] = "${CI_PROJECT_DIR}/.sonar",
            ["GIT_DEPTH"] = "0"
        };

        return variables;
    }

    private static Dictionary<string, object> BuildSecurityJobVariables(PipelineOptions options)
    {
        var variables = new Dictionary<string, object>
        {
            ["SECURITY_SCAN_ENABLED"] = "true"
        };

        return variables;
    }

    private static Dictionary<string, object> BuildPerformanceJobVariables(PipelineOptions options)
    {
        var variables = new Dictionary<string, object>
        {
            ["PERFORMANCE_TEST_ENABLED"] = "true",
            ["LOAD_TEST_DURATION"] = "300s"
        };

        return variables;
    }

    private static Dictionary<string, object> BuildCustomJobVariables(PipelineOptions options)
    {
        var variables = new Dictionary<string, object>
        {
            ["CUSTOM_JOB"] = "true"
        };

        return variables;
    }

    #endregion
}