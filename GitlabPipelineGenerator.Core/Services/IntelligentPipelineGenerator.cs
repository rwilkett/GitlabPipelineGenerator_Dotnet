using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;
using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Intelligent pipeline generator that uses project analysis results to generate optimized pipelines
/// </summary>
public class IntelligentPipelineGenerator : IIntelligentPipelineGenerator
{
    private readonly IPipelineGenerator _basePipelineGenerator;
    private readonly IAnalysisToPipelineMappingService _mappingService;
    private readonly YamlSerializationService _yamlService;

    public IntelligentPipelineGenerator(
        IPipelineGenerator basePipelineGenerator,
        IAnalysisToPipelineMappingService mappingService,
        YamlSerializationService yamlService)
    {
        _basePipelineGenerator = basePipelineGenerator ?? throw new ArgumentNullException(nameof(basePipelineGenerator));
        _mappingService = mappingService ?? throw new ArgumentNullException(nameof(mappingService));
        _yamlService = yamlService ?? throw new ArgumentNullException(nameof(yamlService));
    }

    /// <summary>
    /// Generates a pipeline configuration based on the provided options
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>Generated pipeline configuration</returns>
    public async Task<PipelineConfiguration> GenerateAsync(PipelineOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        // If analysis results are available, use intelligent generation
        if (options.AnalysisResult != null && options.UseAnalysisDefaults)
        {
            return await GenerateIntelligentPipelineAsync(options);
        }

        // Fall back to base pipeline generator
        return await _basePipelineGenerator.GenerateAsync(options);
    }

    /// <summary>
    /// Generates a pipeline using analysis results with manual options
    /// </summary>
    /// <param name="analysisResult">Project analysis result</param>
    /// <param name="manualOptions">Manual pipeline options (optional)</param>
    /// <param name="mergeStrategy">Strategy for merging analysis and manual options</param>
    /// <returns>Generated pipeline configuration</returns>
    public async Task<PipelineConfiguration> GenerateFromAnalysisAsync(
        ProjectAnalysisResult analysisResult,
        PipelineOptions? manualOptions = null,
        ConfigurationMergeStrategy mergeStrategy = ConfigurationMergeStrategy.PreferManual)
    {
        if (analysisResult == null)
            throw new ArgumentNullException(nameof(analysisResult));

        try
        {
            // Create analysis-based options
            var intelligentOptions = AnalysisBasedPipelineOptions.CreateFromAnalysis(
                analysisResult, manualOptions, mergeStrategy);

            return await GenerateIntelligentPipelineAsync(intelligentOptions);
        }
        catch (Exception ex) when (!(ex is ArgumentNullException))
        {
            throw new PipelineGenerationException(
                $"Failed to generate pipeline from analysis: {ex.Message}", 
                manualOptions ?? new PipelineOptions(), 
                "analysis-generation", 
                ex);
        }
    }

    /// <summary>
    /// Generates an intelligent pipeline using analysis-enhanced options
    /// </summary>
    /// <param name="options">Analysis-enhanced pipeline options</param>
    /// <returns>Generated pipeline configuration</returns>
    private async Task<PipelineConfiguration> GenerateIntelligentPipelineAsync(PipelineOptions options)
    {
        // Generate base pipeline
        var pipeline = await _basePipelineGenerator.GenerateAsync(options);

        // Enhance pipeline with analysis-based optimizations
        if (options.AnalysisResult != null)
        {
            pipeline = await EnhancePipelineWithAnalysisAsync(pipeline, options.AnalysisResult, options);
        }

        return pipeline;
    }

    /// <summary>
    /// Enhances a pipeline configuration with analysis-based optimizations
    /// </summary>
    /// <param name="pipeline">Base pipeline configuration</param>
    /// <param name="analysis">Project analysis result</param>
    /// <param name="options">Pipeline options</param>
    /// <returns>Enhanced pipeline configuration</returns>
    private async Task<PipelineConfiguration> EnhancePipelineWithAnalysisAsync(
        PipelineConfiguration pipeline, 
        ProjectAnalysisResult analysis, 
        PipelineOptions options)
    {
        // Apply framework-specific enhancements
        pipeline = await ApplyFrameworkEnhancementsAsync(pipeline, analysis.Framework, options);

        // Apply build configuration enhancements
        pipeline = await ApplyBuildEnhancementsAsync(pipeline, analysis.BuildConfig, options);

        // Apply dependency-based enhancements
        pipeline = await ApplyDependencyEnhancementsAsync(pipeline, analysis.Dependencies, options);

        // Apply Docker enhancements if applicable
        if (analysis.Docker != null)
        {
            pipeline = await ApplyDockerEnhancementsAsync(pipeline, analysis.Docker, options);
        }

        // Apply security enhancements
        pipeline = await ApplySecurityEnhancementsAsync(pipeline, analysis, options);

        // Apply caching enhancements
        pipeline = await ApplyCacheEnhancementsAsync(pipeline, analysis.Dependencies, options);

        // Apply deployment enhancements
        pipeline = await ApplyDeploymentEnhancementsAsync(pipeline, analysis.Deployment, options);

        return pipeline;
    }

    /// <summary>
    /// Applies framework-specific enhancements to the pipeline
    /// </summary>
    private async Task<PipelineConfiguration> ApplyFrameworkEnhancementsAsync(
        PipelineConfiguration pipeline, 
        FrameworkInfo framework, 
        PipelineOptions options)
    {
        await Task.CompletedTask; // Placeholder for async operations

        // Add framework-specific variables
        if (!string.IsNullOrEmpty(framework.Version))
        {
            var versionVar = GetFrameworkVersionVariable(framework.Name);
            if (!string.IsNullOrEmpty(versionVar))
            {
                pipeline.Variables[versionVar] = framework.Version;
            }
        }

        // Add framework-specific configuration
        foreach (var config in framework.Configuration)
        {
            if (!pipeline.Variables.ContainsKey(config.Key))
            {
                pipeline.Variables[config.Key] = config.Value;
            }
        }

        // Enhance jobs with framework-specific optimizations
        foreach (var job in pipeline.Jobs.Values)
        {
            EnhanceJobForFramework(job, framework);
        }

        return pipeline;
    }

    /// <summary>
    /// Applies build configuration enhancements to the pipeline
    /// </summary>
    private async Task<PipelineConfiguration> ApplyBuildEnhancementsAsync(
        PipelineConfiguration pipeline, 
        BuildConfiguration buildConfig, 
        PipelineOptions options)
    {
        await Task.CompletedTask; // Placeholder for async operations

        // Replace generic build commands with detected ones
        foreach (var job in pipeline.Jobs.Values)
        {
            if (job.Stage == "build" && buildConfig.BuildCommands.Any())
            {
                // Replace or enhance build script with detected commands
                job.Script = EnhanceBuildScript(job.Script, buildConfig.BuildCommands);
            }

            if (job.Stage == "test" && buildConfig.TestCommands.Any())
            {
                // Replace or enhance test script with detected commands
                job.Script = EnhanceTestScript(job.Script, buildConfig.TestCommands);
            }
        }

        // Add build tool specific variables
        if (!string.IsNullOrEmpty(buildConfig.BuildTool))
        {
            pipeline.Variables["BUILD_TOOL"] = buildConfig.BuildTool;
            
            if (!string.IsNullOrEmpty(buildConfig.BuildToolVersion))
            {
                pipeline.Variables["BUILD_TOOL_VERSION"] = buildConfig.BuildToolVersion;
            }
        }

        // Add environment variables from build config
        foreach (var envVar in buildConfig.EnvironmentVariables)
        {
            if (!pipeline.Variables.ContainsKey(envVar.Key))
            {
                pipeline.Variables[envVar.Key] = envVar.Value;
            }
        }

        return pipeline;
    }

    /// <summary>
    /// Applies dependency-based enhancements to the pipeline
    /// </summary>
    private async Task<PipelineConfiguration> ApplyDependencyEnhancementsAsync(
        PipelineConfiguration pipeline, 
        DependencyInfo dependencies, 
        PipelineOptions options)
    {
        await Task.CompletedTask; // Placeholder for async operations

        // Add package manager information
        if (!string.IsNullOrEmpty(dependencies.PackageManager))
        {
            pipeline.Variables["PACKAGE_MANAGER"] = dependencies.PackageManager;
        }

        // Add dependency count for optimization decisions
        pipeline.Variables["DEPENDENCY_COUNT"] = dependencies.TotalDependencies.ToString();

        // Enhance jobs with dependency-specific optimizations
        foreach (var job in pipeline.Jobs.Values)
        {
            EnhanceJobForDependencies(job, dependencies);
        }

        return pipeline;
    }

    /// <summary>
    /// Applies Docker-specific enhancements to the pipeline
    /// </summary>
    private async Task<PipelineConfiguration> ApplyDockerEnhancementsAsync(
        PipelineConfiguration pipeline, 
        DockerConfiguration docker, 
        PipelineOptions options)
    {
        await Task.CompletedTask; // Placeholder for async operations

        // Set Docker image if not already set
        if (!string.IsNullOrEmpty(docker.BaseImage))
        {
            foreach (var job in pipeline.Jobs.Values)
            {
                if (job.Image == null || string.IsNullOrEmpty(job.Image.Name))
                {
                    job.Image = new JobImage { Name = docker.BaseImage };
                }
            }
        }

        // Add Docker build arguments as variables
        foreach (var buildArg in docker.BuildArgs)
        {
            if (!pipeline.Variables.ContainsKey(buildArg.Key))
            {
                pipeline.Variables[buildArg.Key] = buildArg.Value;
            }
        }

        // Add Docker-specific jobs if Dockerfile is present
        if (docker.HasDockerConfig)
        {
            await AddDockerBuildJobAsync(pipeline, docker, options);
        }

        return pipeline;
    }

    /// <summary>
    /// Applies security enhancements to the pipeline
    /// </summary>
    private async Task<PipelineConfiguration> ApplySecurityEnhancementsAsync(
        PipelineConfiguration pipeline, 
        ProjectAnalysisResult analysis, 
        PipelineOptions options)
    {
        await Task.CompletedTask; // Placeholder for async operations

        if (analysis.Dependencies.SecurityScanRecommendation?.IsRecommended == true)
        {
            await AddSecurityScanningJobsAsync(pipeline, analysis.Dependencies.SecurityScanRecommendation, options);
        }

        return pipeline;
    }

    /// <summary>
    /// Applies caching enhancements to the pipeline
    /// </summary>
    private async Task<PipelineConfiguration> ApplyCacheEnhancementsAsync(
        PipelineConfiguration pipeline, 
        DependencyInfo dependencies, 
        PipelineOptions options)
    {
        await Task.CompletedTask; // Placeholder for async operations

        if (dependencies.CacheRecommendation?.IsRecommended == true)
        {
            var cacheConfig = dependencies.CacheRecommendation;
            
            // Apply caching to relevant jobs
            foreach (var job in pipeline.Jobs.Values)
            {
                if (ShouldApplyCacheToJob(job, cacheConfig))
                {
                    ApplyCacheToJob(job, cacheConfig);
                }
            }
        }

        return pipeline;
    }

    /// <summary>
    /// Applies deployment enhancements to the pipeline
    /// </summary>
    private async Task<PipelineConfiguration> ApplyDeploymentEnhancementsAsync(
        PipelineConfiguration pipeline, 
        DeploymentInfo deployment, 
        PipelineOptions options)
    {
        await Task.CompletedTask; // Placeholder for async operations

        if (deployment.HasDeploymentConfig)
        {
            // Enhance deployment jobs with detected commands
            foreach (var job in pipeline.Jobs.Values)
            {
                if (job.Stage == "deploy" && deployment.DeploymentCommands.Any())
                {
                    job.Script = EnhanceDeploymentScript(job.Script, deployment.DeploymentCommands);
                }
            }

            // Add deployment-specific variables
            foreach (var secret in deployment.RequiredSecrets)
            {
                // Add comments about required secrets (actual secrets should be configured in GitLab)
                pipeline.Variables[$"# Required secret: {secret}"] = "Configure in GitLab CI/CD settings";
            }
        }

        return pipeline;
    }

    /// <summary>
    /// Gets the appropriate framework version variable name
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
    /// Enhances a job with framework-specific optimizations
    /// </summary>
    private static void EnhanceJobForFramework(Job job, FrameworkInfo framework)
    {
        // Add framework-specific before_script commands
        var beforeScriptCommands = GetFrameworkBeforeScript(framework);
        if (beforeScriptCommands.Any())
        {
            job.BeforeScript = (job.BeforeScript ?? new List<string>()).Concat(beforeScriptCommands).ToList();
        }

        // Add framework-specific job variables
        job.Variables ??= new Dictionary<string, object>();
        foreach (var config in framework.Configuration)
        {
            if (!job.Variables.ContainsKey(config.Key))
            {
                job.Variables[config.Key] = config.Value;
            }
        }
    }

    /// <summary>
    /// Enhances a job with dependency-specific optimizations
    /// </summary>
    private static void EnhanceJobForDependencies(Job job, DependencyInfo dependencies)
    {
        // Add dependency installation commands if needed
        var installCommands = GetDependencyInstallCommands(dependencies);
        if (installCommands.Any())
        {
            job.BeforeScript = (job.BeforeScript ?? new List<string>()).Concat(installCommands).ToList();
        }
    }

    /// <summary>
    /// Enhances build script with detected build commands
    /// </summary>
    private static List<string> EnhanceBuildScript(List<string> currentScript, List<string> detectedCommands)
    {
        // If current script is generic, replace with detected commands
        if (IsGenericBuildScript(currentScript))
        {
            return detectedCommands;
        }

        // Otherwise, prepend detected commands
        return detectedCommands.Concat(currentScript).ToList();
    }

    /// <summary>
    /// Enhances test script with detected test commands
    /// </summary>
    private static List<string> EnhanceTestScript(List<string> currentScript, List<string> detectedCommands)
    {
        // If current script is generic, replace with detected commands
        if (IsGenericTestScript(currentScript))
        {
            return detectedCommands;
        }

        // Otherwise, prepend detected commands
        return detectedCommands.Concat(currentScript).ToList();
    }

    /// <summary>
    /// Enhances deployment script with detected deployment commands
    /// </summary>
    private static List<string> EnhanceDeploymentScript(List<string> currentScript, List<string> detectedCommands)
    {
        // If current script is generic, replace with detected commands
        if (IsGenericDeploymentScript(currentScript))
        {
            return detectedCommands;
        }

        // Otherwise, append detected commands
        return currentScript.Concat(detectedCommands).ToList();
    }

    /// <summary>
    /// Gets framework-specific before_script commands
    /// </summary>
    private static List<string> GetFrameworkBeforeScript(FrameworkInfo framework)
    {
        return framework.Name.ToLowerInvariant() switch
        {
            var name when name.Contains("dotnet") => new List<string> { "dotnet --version" },
            var name when name.Contains("node") => new List<string> { "node --version", "npm --version" },
            var name when name.Contains("python") => new List<string> { "python --version", "pip --version" },
            var name when name.Contains("java") => new List<string> { "java -version" },
            var name when name.Contains("go") => new List<string> { "go version" },
            var name when name.Contains("ruby") => new List<string> { "ruby --version", "gem --version" },
            var name when name.Contains("php") => new List<string> { "php --version", "composer --version" },
            _ => new List<string>()
        };
    }

    /// <summary>
    /// Gets dependency installation commands
    /// </summary>
    private static List<string> GetDependencyInstallCommands(DependencyInfo dependencies)
    {
        return dependencies.PackageManager.ToLowerInvariant() switch
        {
            "npm" => new List<string> { "npm ci" },
            "yarn" => new List<string> { "yarn install --frozen-lockfile" },
            "pip" => new List<string> { "pip install -r requirements.txt" },
            "maven" => new List<string> { "mvn dependency:resolve" },
            "gradle" => new List<string> { "gradle dependencies" },
            "composer" => new List<string> { "composer install --no-dev --optimize-autoloader" },
            "bundler" => new List<string> { "bundle install" },
            _ => new List<string>()
        };
    }

    /// <summary>
    /// Determines if a build script is generic and can be replaced
    /// </summary>
    private static bool IsGenericBuildScript(List<string> script)
    {
        return script.Any(cmd => cmd.Contains("echo") && cmd.Contains("build")) ||
               script.Count <= 2;
    }

    /// <summary>
    /// Determines if a test script is generic and can be replaced
    /// </summary>
    private static bool IsGenericTestScript(List<string> script)
    {
        return script.Any(cmd => cmd.Contains("echo") && cmd.Contains("test")) ||
               script.Count <= 2;
    }

    /// <summary>
    /// Determines if a deployment script is generic and can be replaced
    /// </summary>
    private static bool IsGenericDeploymentScript(List<string> script)
    {
        return script.Any(cmd => cmd.Contains("echo") && cmd.Contains("deploy")) ||
               script.Count <= 2;
    }

    /// <summary>
    /// Determines if cache should be applied to a job
    /// </summary>
    private static bool ShouldApplyCacheToJob(Job job, CacheRecommendation cacheRecommendation)
    {
        // Apply cache to build and test jobs primarily
        return job.Stage == "build" || job.Stage == "test" || 
               job.Script.Any(cmd => cmd.Contains("install") || cmd.Contains("restore"));
    }

    /// <summary>
    /// Applies cache configuration to a job
    /// </summary>
    private static void ApplyCacheToJob(Job job, CacheRecommendation cacheRecommendation)
    {
        if (cacheRecommendation.CachePaths.Any())
        {
            job.Cache = new JobCache
            {
                Key = cacheRecommendation.CacheKey,
                Paths = cacheRecommendation.CachePaths,
                Policy = "pull-push"
            };
        }
    }

    /// <summary>
    /// Adds Docker build job to the pipeline
    /// </summary>
    private async Task AddDockerBuildJobAsync(PipelineConfiguration pipeline, DockerConfiguration docker, PipelineOptions options)
    {
        await Task.CompletedTask; // Placeholder for async operations

        if (!pipeline.Jobs.ContainsKey("docker-build"))
        {
            var dockerJob = new Job
            {
                Stage = "build",
                Image = new JobImage { Name = "docker:latest" },
                Services = new List<JobService> { new JobService { Name = "docker:dind" } },
                Script = new List<string>
                {
                    "docker build -t $CI_REGISTRY_IMAGE:$CI_COMMIT_SHA .",
                    "docker push $CI_REGISTRY_IMAGE:$CI_COMMIT_SHA"
                },
                Variables = new Dictionary<string, object>
                {
                    ["DOCKER_DRIVER"] = "overlay2",
                    ["DOCKER_TLS_CERTDIR"] = "/certs"
                }
            };

            // Add build args if present
            if (docker.BuildArgs.Any())
            {
                var buildArgsString = string.Join(" ", docker.BuildArgs.Select(arg => $"--build-arg {arg.Key}={arg.Value}"));
                dockerJob.Script[0] = $"docker build {buildArgsString} -t $CI_REGISTRY_IMAGE:$CI_COMMIT_SHA .";
            }

            pipeline.Jobs["docker-build"] = dockerJob;
        }
    }

    /// <summary>
    /// Adds security scanning jobs to the pipeline
    /// </summary>
    private async Task AddSecurityScanningJobsAsync(PipelineConfiguration pipeline, SecurityScanConfiguration securityConfig, PipelineOptions options)
    {
        await Task.CompletedTask; // Placeholder for async operations

        foreach (var scanner in securityConfig.RecommendedScanners)
        {
            var jobName = $"security-{scanner.Name.ToLowerInvariant()}";
            
            if (!pipeline.Jobs.ContainsKey(jobName))
            {
                var securityJob = CreateSecurityScanJob(scanner);
                pipeline.Jobs[jobName] = securityJob;
            }
        }
    }

    /// <summary>
    /// Creates a security scan job based on scanner configuration
    /// </summary>
    private static Job CreateSecurityScanJob(SecurityScanner scanner)
    {
        return scanner.Type switch
        {
            SecurityScanType.SAST => new Job
            {
                Stage = "security",
                Image = new JobImage { Name = "registry.gitlab.com/gitlab-org/security-products/analyzers/semgrep:latest" },
                Script = new List<string> { "semgrep --config=auto --json --output=sast-report.json ." },
                Artifacts = new JobArtifacts
                {
                    Reports = new ArtifactReports { Sast = new List<string> { "sast-report.json" } },
                    ExpireIn = "1 week"
                },
                AllowFailure = true
            },
            SecurityScanType.DependencyScanning => new Job
            {
                Stage = "security",
                Image = new JobImage { Name = "registry.gitlab.com/gitlab-org/security-products/analyzers/gemnasium:latest" },
                Script = new List<string> { "gemnasium-dependency-scanning" },
                Artifacts = new JobArtifacts
                {
                    Reports = new ArtifactReports { DependencyScanning = new List<string> { "dependency-scanning-report.json" } },
                    ExpireIn = "1 week"
                },
                AllowFailure = true
            },
            SecurityScanType.SecretDetection => new Job
            {
                Stage = "security",
                Image = new JobImage { Name = "registry.gitlab.com/gitlab-org/security-products/analyzers/secrets:latest" },
                Script = new List<string> { "secrets-analyzer" },
                Artifacts = new JobArtifacts
                {
                    Reports = new ArtifactReports { /* Secret detection reports don't have a specific property */ },
                    ExpireIn = "1 week"
                },
                AllowFailure = true
            },
            _ => new Job
            {
                Stage = "security",
                Script = new List<string> { $"echo 'Running {scanner.Name} security scan'" },
                AllowFailure = true
            }
        };
    }

    /// <summary>
    /// Serializes a pipeline configuration to YAML format
    /// </summary>
    /// <param name="pipeline">Pipeline configuration to serialize</param>
    /// <returns>YAML representation of the pipeline</returns>
    public string SerializeToYaml(PipelineConfiguration pipeline)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));

        return _yamlService.SerializePipeline(pipeline);
    }
}