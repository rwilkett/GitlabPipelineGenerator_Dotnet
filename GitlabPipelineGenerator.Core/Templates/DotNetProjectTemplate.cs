using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Templates;

/// <summary>
/// Pipeline template for .NET projects with standard build, test, and deploy stages
/// </summary>
public class DotNetProjectTemplate : BasePipelineTemplate
{
    public DotNetProjectTemplate(
        IStageBuilder stageBuilder,
        IJobBuilder jobBuilder,
        IVariableBuilder variableBuilder)
        : base(stageBuilder, jobBuilder, variableBuilder)
    {
    }

    /// <summary>
    /// Gets the name of the template
    /// </summary>
    public override string Name => "dotnet-standard";

    /// <summary>
    /// Gets the description of the template
    /// </summary>
    public override string Description => "Standard .NET project pipeline with build, test, and deployment stages supporting multiple .NET versions";

    /// <summary>
    /// Gets the supported project types for this template
    /// </summary>
    public override IEnumerable<string> SupportedProjectTypes => new[] { "dotnet" };

    /// <summary>
    /// Gets the default pipeline options for this template
    /// </summary>
    /// <param name="projectType">Project type to get defaults for</param>
    /// <returns>Default pipeline options</returns>
    public override PipelineOptions GetDefaultOptions(string projectType)
    {
        return new PipelineOptions
        {
            ProjectType = "dotnet",
            DotNetVersion = "9.0",
            Stages = new List<string> { "build", "test", "deploy" },
            IncludeTests = true,
            IncludeDeployment = true,
            IncludeCodeQuality = false,
            IncludeSecurity = false,
            IncludePerformance = false,
            DockerImage = "mcr.microsoft.com/dotnet/sdk:9.0",
            CustomVariables = new Dictionary<string, string>
            {
                ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "true",
                ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "true",
                ["NUGET_PACKAGES"] = "$CI_PROJECT_DIR/.nuget/packages"
            },
            Cache = new CacheOptions
            {
                Key = "$CI_COMMIT_REF_SLUG-dotnet",
                Paths = new List<string> { ".nuget/packages/" },
                Policy = "pull-push",
                When = "on_success"
            },
            Artifacts = new ArtifactOptions
            {
                DefaultPaths = new List<string> { "bin/", "obj/", "TestResults/" },
                DefaultExpireIn = "1 week",
                IncludeTestReports = true,
                IncludeCoverageReports = true
            }
        };
    }

    /// <summary>
    /// Applies template-specific customizations to the pipeline options
    /// </summary>
    /// <param name="options">Original pipeline options</param>
    /// <returns>Customized pipeline options</returns>
    protected override PipelineOptions ApplyTemplateCustomizations(PipelineOptions options)
    {
        var customized = new PipelineOptions
        {
            ProjectType = options.ProjectType,
            DotNetVersion = options.DotNetVersion ?? "9.0",
            Stages = new List<string>(options.Stages),
            IncludeTests = options.IncludeTests,
            IncludeDeployment = options.IncludeDeployment,
            IncludeCodeQuality = options.IncludeCodeQuality,
            IncludeSecurity = options.IncludeSecurity,
            IncludePerformance = options.IncludePerformance,
            DockerImage = options.DockerImage ?? GetDefaultDockerImage(options.DotNetVersion ?? "9.0"),
            RunnerTags = new List<string>(options.RunnerTags),
            CustomVariables = new Dictionary<string, string>(options.CustomVariables),
            DeploymentEnvironments = new List<DeploymentEnvironment>(options.DeploymentEnvironments),
            Cache = options.Cache ?? GetDefaultCacheOptions(),
            Artifacts = options.Artifacts ?? GetDefaultArtifactOptions(),
            Notifications = options.Notifications,
            CustomJobs = new List<CustomJobOptions>(options.CustomJobs)
        };

        // Add .NET-specific variables if not already present
        var dotnetVariables = new Dictionary<string, string>
        {
            ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "true",
            ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "true",
            ["NUGET_PACKAGES"] = "$CI_PROJECT_DIR/.nuget/packages",
            ["DOTNET_RESTORE_DISABLE_PARALLEL"] = "true"
        };

        foreach (var variable in dotnetVariables)
        {
            if (!customized.CustomVariables.ContainsKey(variable.Key))
            {
                customized.CustomVariables[variable.Key] = variable.Value;
            }
        }

        // Set .NET version-specific Docker image if not specified
        if (string.IsNullOrEmpty(customized.DockerImage))
        {
            customized.DockerImage = GetDefaultDockerImage(customized.DotNetVersion ?? "9.0");
        }

        // Ensure build stage is always first
        if (!customized.Stages.Contains("build"))
        {
            customized.Stages.Insert(0, "build");
        }
        else if (customized.Stages.IndexOf("build") != 0)
        {
            customized.Stages.Remove("build");
            customized.Stages.Insert(0, "build");
        }

        // Ensure test stage comes after build if tests are enabled
        if (customized.IncludeTests && !customized.Stages.Contains("test"))
        {
            var buildIndex = customized.Stages.IndexOf("build");
            customized.Stages.Insert(buildIndex + 1, "test");
        }

        return customized;
    }

    /// <summary>
    /// Applies template-specific modifications to the generated pipeline
    /// </summary>
    /// <param name="pipeline">Generated pipeline configuration</param>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Task representing the async operation</returns>
    protected override async Task ApplyTemplatePipelineModificationsAsync(PipelineConfiguration pipeline, PipelineOptions options)
    {
        // Add .NET-specific workflow rules
        pipeline.Workflow = new WorkflowRules
        {
            Rules = new List<Rule>
            {
                new Rule
                {
                    If = "$CI_COMMIT_BRANCH && $CI_OPEN_MERGE_REQUESTS",
                    When = "never"
                },
                new Rule
                {
                    If = "$CI_COMMIT_BRANCH",
                    When = "always"
                }
            }
        };

        // Add .NET-specific includes for common templates
        pipeline.Include = new List<IncludeRule>
        {
            new IncludeRule
            {
                Template = "Security/SAST.gitlab-ci.yml"
            }
        };

        // Modify build job to include .NET-specific optimizations
        if (pipeline.Jobs.TryGetValue("build", out var buildJob))
        {
            // Add NuGet package caching
            buildJob.Cache = new JobCache
            {
                Key = "$CI_COMMIT_REF_SLUG-nuget",
                Paths = new List<string> { ".nuget/packages/" },
                Policy = "pull-push"
            };

            // Add build artifacts with proper .NET paths
            buildJob.Artifacts = new JobArtifacts
            {
                Paths = new List<string> { "bin/", "obj/" },
                ExpireIn = options.Artifacts?.DefaultExpireIn ?? "1 week",
                When = "on_success"
            };

            // Ensure proper .NET build script
            buildJob.Script = new List<string>
            {
                "dotnet restore --verbosity minimal",
                "dotnet build --configuration Release --no-restore --verbosity minimal",
                "dotnet publish --configuration Release --no-build --output ./publish"
            };
        }

        // Modify test job to include .NET-specific test reporting
        if (pipeline.Jobs.TryGetValue("test", out var testJob))
        {
            testJob.Artifacts = new JobArtifacts
            {
                Reports = new ArtifactReports
                {
                    Junit = new List<string> { "TestResults/*.trx" },
                    Cobertura = new List<string> { "TestResults/*/coverage.cobertura.xml" }
                },
                Paths = new List<string> { "TestResults/" },
                ExpireIn = options.Artifacts?.DefaultExpireIn ?? "1 week",
                When = "always"
            };

            // Add coverage collection
            testJob.Script = new List<string>
            {
                "dotnet test --configuration Release --no-build --collect:\"XPlat Code Coverage\" --logger trx --results-directory ./TestResults/",
                "dotnet tool install -g dotnet-reportgenerator-globaltool || true",
                "reportgenerator -reports:\"TestResults/*/coverage.cobertura.xml\" -targetdir:\"TestResults/CoverageReport\" -reporttypes:Html || true"
            };
        }

        // Add deployment job customizations for .NET applications
        foreach (var job in pipeline.Jobs.Values.Where(j => j.Stage.Contains("deploy") || j.Stage == "deploy"))
        {
            // Add .NET deployment-specific variables
            job.Variables ??= new Dictionary<string, object>();
            job.Variables["ASPNETCORE_ENVIRONMENT"] = "Production";
            job.Variables["DOTNET_ENVIRONMENT"] = "Production";

            // Add health check after deployment
            if (job.Script != null)
            {
                job.Script.Add("echo 'Deployment completed successfully'");
                job.Script.Add("# Add health check commands here");
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Validates template-specific options
    /// </summary>
    /// <param name="options">Pipeline generation options to validate</param>
    /// <returns>List of template-specific validation errors</returns>
    protected override List<string> ValidateTemplateSpecificOptions(PipelineOptions options)
    {
        var errors = new List<string>();

        // Validate .NET version
        if (!string.IsNullOrEmpty(options.DotNetVersion))
        {
            var validVersions = new[] { "6.0", "7.0", "8.0", "9.0" };
            if (!validVersions.Contains(options.DotNetVersion))
            {
                errors.Add($"Invalid .NET version '{options.DotNetVersion}'. Valid versions are: {string.Join(", ", validVersions)}");
            }
        }

        // Validate that build stage is included
        if (!options.Stages.Contains("build", StringComparer.OrdinalIgnoreCase))
        {
            errors.Add("Build stage is required for .NET projects");
        }

        // Validate Docker image for .NET compatibility
        if (!string.IsNullOrEmpty(options.DockerImage) && 
            !options.DockerImage.Contains("dotnet") && 
            !options.DockerImage.Contains("mcr.microsoft.com"))
        {
            // This is a warning, not an error
            // errors.Add($"Docker image '{options.DockerImage}' may not be compatible with .NET projects. Consider using an official .NET image.");
        }

        return errors;
    }

    #region Private Helper Methods

    private static string GetDefaultDockerImage(string dotnetVersion)
    {
        return $"mcr.microsoft.com/dotnet/sdk:{dotnetVersion}";
    }

    private static CacheOptions GetDefaultCacheOptions()
    {
        return new CacheOptions
        {
            Key = "$CI_COMMIT_REF_SLUG-dotnet",
            Paths = new List<string> { ".nuget/packages/" },
            Policy = "pull-push",
            When = "on_success"
        };
    }

    private static ArtifactOptions GetDefaultArtifactOptions()
    {
        return new ArtifactOptions
        {
            DefaultPaths = new List<string> { "bin/", "obj/", "TestResults/", "publish/" },
            DefaultExpireIn = "1 week",
            IncludeTestReports = true,
            IncludeCoverageReports = true
        };
    }

    #endregion
}