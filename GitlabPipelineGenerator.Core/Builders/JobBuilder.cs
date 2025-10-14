using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Builders;

/// <summary>
/// Implementation of IJobBuilder for building pipeline jobs
/// </summary>
public class JobBuilder : IJobBuilder
{
    private readonly IVariableBuilder _variableBuilder;

    private static readonly Dictionary<string, string> DefaultImages = new()
    {
        ["dotnet"] = "mcr.microsoft.com/dotnet/sdk:9.0",
        ["nodejs"] = "node:18",
        ["python"] = "python:3.11",
        ["docker"] = "docker:latest",
        ["generic"] = "ubuntu:latest"
    };

    public JobBuilder(IVariableBuilder variableBuilder)
    {
        _variableBuilder = variableBuilder ?? throw new ArgumentNullException(nameof(variableBuilder));
    }

    /// <summary>
    /// Builds jobs for a specific stage based on the pipeline options
    /// </summary>
    /// <param name="stage">Stage name</param>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Dictionary of jobs keyed by job name</returns>
    public async Task<Dictionary<string, Job>> BuildJobsForStageAsync(string stage, PipelineOptions options)
    {
        if (string.IsNullOrWhiteSpace(stage))
            throw new ArgumentException("Stage name cannot be null or empty", nameof(stage));
        
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var jobs = new Dictionary<string, Job>();
        var normalizedStage = stage.ToLowerInvariant();

        switch (normalizedStage)
        {
            case "build":
                var buildJob = await CreateBuildJobAsync(options);
                jobs["build"] = buildJob;
                break;

            case "test":
                if (options.IncludeTests)
                {
                    var testJobs = await CreateTestJobsAsync(options);
                    foreach (var testJob in testJobs)
                    {
                        jobs[testJob.Key] = testJob.Value;
                    }
                }
                break;

            case "quality":
                if (options.IncludeCodeQuality)
                {
                    jobs["code_quality"] = await CreateCodeQualityJobAsync(options);
                }
                break;

            case "security":
                if (options.IncludeSecurity)
                {
                    jobs["security_scan"] = await CreateSecurityScanJobAsync(options);
                }
                break;

            case "performance":
                if (options.IncludePerformance)
                {
                    jobs["performance_test"] = await CreatePerformanceTestJobAsync(options);
                }
                break;

            case "deploy":
                if (options.IncludeDeployment)
                {
                    var deployJobs = await CreateDeploymentJobsAsync(options);
                    foreach (var deployJob in deployJobs)
                    {
                        jobs[deployJob.Key] = deployJob.Value;
                    }
                }
                break;

            default:
                // Check if it's a custom environment stage
                var envJob = await CreateEnvironmentDeploymentJobAsync(stage, options);
                if (envJob != null)
                {
                    jobs[$"deploy_{stage}"] = envJob;
                }
                break;
        }

        return jobs;
    }

    /// <summary>
    /// Builds a custom job based on custom job options
    /// </summary>
    /// <param name="customJob">Custom job configuration</param>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Configured job</returns>
    public async Task<Job> BuildCustomJobAsync(CustomJobOptions customJob, PipelineOptions options)
    {
        if (customJob == null)
            throw new ArgumentNullException(nameof(customJob));
        
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var job = new Job
        {
            Stage = customJob.Stage,
            Script = new List<string>(customJob.Script),
            BeforeScript = customJob.BeforeScript.Any() ? new List<string>(customJob.BeforeScript) : null,
            AfterScript = customJob.AfterScript.Any() ? new List<string>(customJob.AfterScript) : null,
            Variables = customJob.Variables.ToDictionary(kv => kv.Key, kv => (object)kv.Value),
            When = customJob.When,
            AllowFailure = customJob.AllowFailure,
            Tags = customJob.Tags.Any() ? new List<string>(customJob.Tags) : null
        };

        // Set image
        if (!string.IsNullOrEmpty(customJob.Image))
        {
            job.Image = new JobImage { Name = customJob.Image };
        }
        else if (!string.IsNullOrEmpty(options.DockerImage))
        {
            job.Image = new JobImage { Name = options.DockerImage };
        }
        else
        {
            job.Image = new JobImage { Name = GetDefaultImage(options.ProjectType) };
        }

        // Add job-specific variables
        var jobVariables = await _variableBuilder.BuildJobVariablesAsync("custom", options);
        foreach (var variable in jobVariables)
        {
            job.Variables[variable.Key] = variable.Value;
        }

        return job;
    }

    /// <summary>
    /// Creates a build job for the specified project type
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Build job configuration</returns>
    public async Task<Job> CreateBuildJobAsync(PipelineOptions options)
    {
        var job = new Job
        {
            Stage = "build",
            Image = new JobImage { Name = options.DockerImage ?? GetDefaultImage(options.ProjectType) },
            Tags = options.RunnerTags.Any() ? new List<string>(options.RunnerTags) : null,
            Variables = await _variableBuilder.BuildJobVariablesAsync("build", options)
        };

        // Set scripts based on project type
        switch (options.ProjectType.ToLowerInvariant())
        {
            case "dotnet":
                job.Script = CreateDotNetBuildScript(options);
                job.Artifacts = CreateDotNetBuildArtifacts(options);
                break;

            case "nodejs":
                job.Script = CreateNodeJsBuildScript(options);
                job.Artifacts = CreateNodeJsBuildArtifacts(options);
                break;

            case "python":
                job.Script = CreatePythonBuildScript(options);
                job.Artifacts = CreatePythonBuildArtifacts(options);
                break;

            case "docker":
                job.Script = CreateDockerBuildScript(options);
                break;

            default:
                job.Script = CreateGenericBuildScript(options);
                break;
        }

        // Add cache configuration if specified
        if (options.Cache != null)
        {
            job.Cache = new JobCache
            {
                Key = options.Cache.Key ?? "$CI_COMMIT_REF_SLUG",
                Paths = options.Cache.Paths.Any() ? new List<string>(options.Cache.Paths) : GetDefaultCachePaths(options.ProjectType),
                Policy = options.Cache.Policy,
                When = options.Cache.When
            };
        }

        return job;
    }

    /// <summary>
    /// Creates test jobs for the specified project type
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Dictionary of test jobs</returns>
    public async Task<Dictionary<string, Job>> CreateTestJobsAsync(PipelineOptions options)
    {
        var jobs = new Dictionary<string, Job>();

        var testJob = new Job
        {
            Stage = "test",
            Image = new JobImage { Name = options.DockerImage ?? GetDefaultImage(options.ProjectType) },
            Tags = options.RunnerTags.Any() ? new List<string>(options.RunnerTags) : null,
            Variables = await _variableBuilder.BuildJobVariablesAsync("test", options),
            Dependencies = new List<string> { "build" }
        };

        // Set scripts based on project type
        switch (options.ProjectType.ToLowerInvariant())
        {
            case "dotnet":
                testJob.Script = CreateDotNetTestScript(options);
                testJob.Artifacts = CreateDotNetTestArtifacts(options);
                break;

            case "nodejs":
                testJob.Script = CreateNodeJsTestScript(options);
                testJob.Artifacts = CreateNodeJsTestArtifacts(options);
                break;

            case "python":
                testJob.Script = CreatePythonTestScript(options);
                testJob.Artifacts = CreatePythonTestArtifacts(options);
                break;

            default:
                testJob.Script = CreateGenericTestScript(options);
                break;
        }

        jobs["test"] = testJob;
        return jobs;
    }

    /// <summary>
    /// Creates deployment jobs for the specified environments
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Dictionary of deployment jobs</returns>
    public async Task<Dictionary<string, Job>> CreateDeploymentJobsAsync(PipelineOptions options)
    {
        var jobs = new Dictionary<string, Job>();

        if (options.DeploymentEnvironments.Any())
        {
            foreach (var env in options.DeploymentEnvironments)
            {
                var deployJob = await CreateEnvironmentDeploymentJobAsync(env.Name, options);
                if (deployJob != null)
                {
                    jobs[$"deploy_{env.Name.ToLowerInvariant()}"] = deployJob;
                }
            }
        }
        else
        {
            // Create a generic deployment job
            var deployJob = new Job
            {
                Stage = "deploy",
                Image = new JobImage { Name = options.DockerImage ?? GetDefaultImage(options.ProjectType) },
                Tags = options.RunnerTags.Any() ? new List<string>(options.RunnerTags) : null,
                Variables = await _variableBuilder.BuildJobVariablesAsync("deploy", options),
                Dependencies = new List<string> { "build" },
                Script = CreateGenericDeployScript(options),
                When = "manual"
            };

            jobs["deploy"] = deployJob;
        }

        return jobs;
    }

    #region Private Helper Methods

    private static string GetDefaultImage(string projectType)
    {
        return DefaultImages.TryGetValue(projectType.ToLowerInvariant(), out var image) 
            ? image 
            : DefaultImages["generic"];
    }

    private static List<string> GetDefaultCachePaths(string projectType)
    {
        return projectType.ToLowerInvariant() switch
        {
            "dotnet" => new List<string> { "~/.nuget/packages/" },
            "nodejs" => new List<string> { "node_modules/" },
            "python" => new List<string> { ".pip-cache/" },
            _ => new List<string>()
        };
    }

    private static List<string> CreateDotNetBuildScript(PipelineOptions options)
    {
        var script = new List<string>
        {
            "dotnet restore",
            "dotnet build --configuration Release --no-restore"
        };

        if (!string.IsNullOrEmpty(options.DotNetVersion))
        {
            script.Insert(0, $"dotnet --version");
        }

        return script;
    }

    private static JobArtifacts CreateDotNetBuildArtifacts(PipelineOptions options)
    {
        return new JobArtifacts
        {
            Paths = new List<string> { "bin/", "obj/" },
            ExpireIn = options.Artifacts?.DefaultExpireIn ?? "1 week",
            When = "on_success"
        };
    }

    private static List<string> CreateNodeJsBuildScript(PipelineOptions options)
    {
        return new List<string>
        {
            "npm ci",
            "npm run build"
        };
    }

    private static JobArtifacts CreateNodeJsBuildArtifacts(PipelineOptions options)
    {
        return new JobArtifacts
        {
            Paths = new List<string> { "dist/", "build/" },
            ExpireIn = options.Artifacts?.DefaultExpireIn ?? "1 week",
            When = "on_success"
        };
    }

    private static List<string> CreatePythonBuildScript(PipelineOptions options)
    {
        return new List<string>
        {
            "pip install -r requirements.txt",
            "python setup.py build"
        };
    }

    private static JobArtifacts CreatePythonBuildArtifacts(PipelineOptions options)
    {
        return new JobArtifacts
        {
            Paths = new List<string> { "build/", "dist/" },
            ExpireIn = options.Artifacts?.DefaultExpireIn ?? "1 week",
            When = "on_success"
        };
    }

    private static List<string> CreateDockerBuildScript(PipelineOptions options)
    {
        return new List<string>
        {
            "docker build -t $CI_REGISTRY_IMAGE:$CI_COMMIT_SHA .",
            "docker push $CI_REGISTRY_IMAGE:$CI_COMMIT_SHA"
        };
    }

    private static List<string> CreateGenericBuildScript(PipelineOptions options)
    {
        return new List<string>
        {
            "echo 'Starting build process'",
            "# Add your build commands here"
        };
    }

    private static List<string> CreateDotNetTestScript(PipelineOptions options)
    {
        return new List<string>
        {
            "dotnet test --configuration Release --no-build --collect:\"XPlat Code Coverage\" --logger trx --results-directory ./TestResults/"
        };
    }

    private static JobArtifacts CreateDotNetTestArtifacts(PipelineOptions options)
    {
        return new JobArtifacts
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
    }

    private static List<string> CreateNodeJsTestScript(PipelineOptions options)
    {
        return new List<string>
        {
            "npm test"
        };
    }

    private static JobArtifacts CreateNodeJsTestArtifacts(PipelineOptions options)
    {
        return new JobArtifacts
        {
            Reports = new ArtifactReports
            {
                Junit = new List<string> { "test-results.xml" }
            },
            ExpireIn = options.Artifacts?.DefaultExpireIn ?? "1 week",
            When = "always"
        };
    }

    private static List<string> CreatePythonTestScript(PipelineOptions options)
    {
        return new List<string>
        {
            "python -m pytest --junitxml=test-results.xml --cov=. --cov-report=xml"
        };
    }

    private static JobArtifacts CreatePythonTestArtifacts(PipelineOptions options)
    {
        return new JobArtifacts
        {
            Reports = new ArtifactReports
            {
                Junit = new List<string> { "test-results.xml" },
                Cobertura = new List<string> { "coverage.xml" }
            },
            ExpireIn = options.Artifacts?.DefaultExpireIn ?? "1 week",
            When = "always"
        };
    }

    private static List<string> CreateGenericTestScript(PipelineOptions options)
    {
        return new List<string>
        {
            "echo 'Running tests'",
            "# Add your test commands here"
        };
    }

    private static List<string> CreateGenericDeployScript(PipelineOptions options)
    {
        return new List<string>
        {
            "echo 'Starting deployment'",
            "# Add your deployment commands here"
        };
    }

    private async Task<Job?> CreateEnvironmentDeploymentJobAsync(string environmentName, PipelineOptions options)
    {
        var env = options.DeploymentEnvironments.FirstOrDefault(e => 
            e.Name.Equals(environmentName, StringComparison.OrdinalIgnoreCase));

        if (env == null && !options.IncludeDeployment)
            return null;

        var job = new Job
        {
            Stage = environmentName.ToLowerInvariant(),
            Image = new JobImage { Name = options.DockerImage ?? GetDefaultImage(options.ProjectType) },
            Tags = options.RunnerTags.Any() ? new List<string>(options.RunnerTags) : null,
            Variables = await _variableBuilder.BuildJobVariablesAsync("deploy", options),
            Dependencies = new List<string> { "build" },
            Script = CreateEnvironmentDeployScript(environmentName, options),
            Environment = new JobEnvironment
            {
                Name = environmentName,
                Url = env?.Url
            }
        };

        // Set deployment rules
        if (env != null)
        {
            if (env.IsManual)
            {
                job.When = "manual";
            }
            else if (!string.IsNullOrEmpty(env.AutoDeployPattern))
            {
                job.Rules = new List<Rule>
                {
                    new Rule
                    {
                        If = $"$CI_COMMIT_BRANCH == \"{env.AutoDeployPattern}\"",
                        When = "on_success"
                    },
                    new Rule
                    {
                        When = "manual",
                        AllowFailure = true
                    }
                };
            }

            // Add environment-specific variables
            foreach (var variable in env.Variables)
            {
                job.Variables[variable.Key] = variable.Value;
            }
        }
        else
        {
            job.When = "manual";
        }

        return job;
    }

    private static List<string> CreateEnvironmentDeployScript(string environmentName, PipelineOptions options)
    {
        return new List<string>
        {
            $"echo 'Deploying to {environmentName} environment'",
            "# Add environment-specific deployment commands here"
        };
    }

    private async Task<Job> CreateCodeQualityJobAsync(PipelineOptions options)
    {
        return new Job
        {
            Stage = "quality",
            Image = new JobImage { Name = "sonarsource/sonar-scanner-cli:latest" },
            Tags = options.RunnerTags.Any() ? new List<string>(options.RunnerTags) : null,
            Variables = await _variableBuilder.BuildJobVariablesAsync("quality", options),
            Script = new List<string>
            {
                "sonar-scanner -Dsonar.projectKey=$CI_PROJECT_NAME -Dsonar.sources=. -Dsonar.host.url=$SONAR_HOST_URL -Dsonar.login=$SONAR_TOKEN"
            },
            AllowFailure = true
        };
    }

    private async Task<Job> CreateSecurityScanJobAsync(PipelineOptions options)
    {
        return new Job
        {
            Stage = "security",
            Image = new JobImage { Name = "owasp/zap2docker-stable:latest" },
            Tags = options.RunnerTags.Any() ? new List<string>(options.RunnerTags) : null,
            Variables = await _variableBuilder.BuildJobVariablesAsync("security", options),
            Script = new List<string>
            {
                "echo 'Running security scan'",
                "# Add security scanning commands here"
            },
            AllowFailure = true
        };
    }

    private async Task<Job> CreatePerformanceTestJobAsync(PipelineOptions options)
    {
        return new Job
        {
            Stage = "performance",
            Image = new JobImage { Name = "sitespeedio/sitespeed.io:latest" },
            Tags = options.RunnerTags.Any() ? new List<string>(options.RunnerTags) : null,
            Variables = await _variableBuilder.BuildJobVariablesAsync("performance", options),
            Script = new List<string>
            {
                "echo 'Running performance tests'",
                "# Add performance testing commands here"
            },
            AllowFailure = true
        };
    }

    #endregion
}