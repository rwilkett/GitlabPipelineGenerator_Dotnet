using GitlabPipelineGenerator.CLI.Models;
using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.CLI.Services;

/// <summary>
/// Converts command-line options to pipeline options
/// </summary>
public static class OptionsConverter
{
    /// <summary>
    /// Converts CommandLineOptions to PipelineOptions
    /// </summary>
    /// <param name="cliOptions">Command-line options</param>
    /// <returns>Pipeline options</returns>
    public static PipelineOptions ToPipelineOptions(CommandLineOptions cliOptions)
    {
        // Ensure stages are set with defaults if empty
        var stages = cliOptions.Stages?.ToList() ?? new List<string>();
        if (!stages.Any())
        {
            stages = new List<string> { "build", "test", "deploy" };
        }

        var pipelineOptions = new PipelineOptions
        {
            ProjectType = cliOptions.ProjectType.ToLowerInvariant(),
            Stages = stages,
            DotNetVersion = cliOptions.DotNetVersion,
            IncludeTests = cliOptions.IncludeTests,
            IncludeDeployment = cliOptions.IncludeDeployment,
            DockerImage = cliOptions.DockerImage,
            RunnerTags = cliOptions.RunnerTags?.ToList() ?? new List<string>(),
            IncludeCodeQuality = cliOptions.IncludeCodeQuality,
            IncludeSecurity = cliOptions.IncludeSecurity,
            IncludePerformance = cliOptions.IncludePerformance
        };

        // Parse custom variables
        if (cliOptions.Variables != null)
        {
            foreach (var variable in cliOptions.Variables)
            {
                var parts = variable.Split('=', 2);
                if (parts.Length == 2)
                {
                    pipelineOptions.CustomVariables[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }

        // Parse deployment environments
        if (cliOptions.Environments != null)
        {
            foreach (var environment in cliOptions.Environments)
            {
                var parts = environment.Split(':', 2);
                if (parts.Length >= 1)
                {
                    var deploymentEnv = new DeploymentEnvironment
                    {
                        Name = parts[0].Trim()
                    };

                    if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
                    {
                        deploymentEnv.Url = parts[1].Trim();
                    }

                    pipelineOptions.DeploymentEnvironments.Add(deploymentEnv);
                }
            }
        }

        // Configure cache options if specified
        if ((cliOptions.CachePaths?.Any() ?? false) || !string.IsNullOrEmpty(cliOptions.CacheKey))
        {
            pipelineOptions.Cache = new CacheOptions
            {
                Key = cliOptions.CacheKey,
                Paths = cliOptions.CachePaths?.ToList() ?? new List<string>()
            };
        }

        // Configure artifact options if specified
        if ((cliOptions.ArtifactPaths?.Any() ?? false) || !string.IsNullOrEmpty(cliOptions.ArtifactExpireIn))
        {
            pipelineOptions.Artifacts = new ArtifactOptions
            {
                DefaultPaths = cliOptions.ArtifactPaths?.ToList() ?? new List<string>(),
                DefaultExpireIn = cliOptions.ArtifactExpireIn
            };
        }

        return pipelineOptions;
    }
}