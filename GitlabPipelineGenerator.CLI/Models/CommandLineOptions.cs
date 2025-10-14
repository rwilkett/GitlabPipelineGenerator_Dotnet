using CommandLine;

namespace GitlabPipelineGenerator.CLI.Models;

/// <summary>
/// Command-line options for the GitLab Pipeline Generator CLI
/// </summary>
[Verb("generate", isDefault: true, HelpText = "Generate a GitLab CI/CD pipeline configuration. Use --help-examples for detailed usage examples.")]
public class CommandLineOptions
{
    [Option('t', "type", Required = true, HelpText = "Project type: dotnet, nodejs, python, docker, generic. Use --help-<type> for project-specific help.")]
    public string ProjectType { get; set; } = string.Empty;

    [Option('o', "output", Required = false, HelpText = "Output file path. Default: .gitlab-ci.yml")]
    public string? OutputPath { get; set; }

    [Option('s', "stages", Required = false, HelpText = "Comma-separated pipeline stages. Default: build,test,deploy", Separator = ',')]
    public IEnumerable<string> Stages { get; set; } = new[] { "build", "test", "deploy" };

    [Option("dotnet-version", Required = false, HelpText = ".NET version for dotnet projects: 6.0, 7.0, 8.0, 9.0")]
    public string? DotNetVersion { get; set; }

    [Option("include-tests", Required = false, Default = true, HelpText = "Include test jobs in the pipeline")]
    public bool IncludeTests { get; set; } = true;

    [Option("include-deployment", Required = false, Default = true, HelpText = "Include deployment jobs in the pipeline")]
    public bool IncludeDeployment { get; set; } = true;

    [Option("docker-image", Required = false, HelpText = "Docker image to use for jobs")]
    public string? DockerImage { get; set; }

    [Option("runner-tags", Required = false, HelpText = "Comma-separated list of runner tags", Separator = ',')]
    public IEnumerable<string> RunnerTags { get; set; } = Enumerable.Empty<string>();

    [Option("include-code-quality", Required = false, Default = false, HelpText = "Include code quality checks")]
    public bool IncludeCodeQuality { get; set; } = false;

    [Option("include-security", Required = false, Default = false, HelpText = "Include security scanning")]
    public bool IncludeSecurity { get; set; } = false;

    [Option("include-performance", Required = false, Default = false, HelpText = "Include performance testing")]
    public bool IncludePerformance { get; set; } = false;

    [Option("variables", Required = false, HelpText = "Custom variables in key=value format. Example: --variables \"BUILD_CONFIG=Release,NODE_ENV=production\"", Separator = ',')]
    public IEnumerable<string> Variables { get; set; } = Enumerable.Empty<string>();

    [Option("environments", Required = false, HelpText = "Deployment environments in name:url format. Example: --environments \"staging:https://staging.example.com\"", Separator = ',')]
    public IEnumerable<string> Environments { get; set; } = Enumerable.Empty<string>();

    [Option("cache-paths", Required = false, HelpText = "Comma-separated list of paths to cache", Separator = ',')]
    public IEnumerable<string> CachePaths { get; set; } = Enumerable.Empty<string>();

    [Option("cache-key", Required = false, HelpText = "Cache key pattern")]
    public string? CacheKey { get; set; }

    [Option("artifact-paths", Required = false, HelpText = "Comma-separated list of artifact paths", Separator = ',')]
    public IEnumerable<string> ArtifactPaths { get; set; } = Enumerable.Empty<string>();

    [Option("artifact-expire", Required = false, Default = "1 week", HelpText = "Artifact expiration time")]
    public string ArtifactExpireIn { get; set; } = "1 week";

    [Option('v', "verbose", Required = false, Default = false, HelpText = "Enable verbose output with detailed logging and statistics")]
    public bool Verbose { get; set; } = false;

    [Option("dry-run", Required = false, Default = false, HelpText = "Generate and validate pipeline without writing to file")]
    public bool DryRun { get; set; } = false;

    [Option("console-output", Required = false, Default = false, HelpText = "Output generated YAML to console instead of file")]
    public bool ConsoleOutput { get; set; } = false;

    [Option("validate-only", Required = false, Default = false, HelpText = "Validate configuration options without generating pipeline")]
    public bool ValidateOnly { get; set; } = false;

    [Option("template", Required = false, HelpText = "Template name to use for pipeline generation")]
    public string? TemplateName { get; set; }

    [Option("list-templates", Required = false, Default = false, HelpText = "List available templates for the specified project type")]
    public bool ListTemplates { get; set; } = false;

    [Option("template-params", Required = false, HelpText = "Template parameters in key=value format", Separator = ',')]
    public IEnumerable<string> TemplateParameters { get; set; } = Enumerable.Empty<string>();

    [Option("disable-stages", Required = false, HelpText = "Comma-separated list of stages to disable", Separator = ',')]
    public IEnumerable<string> DisableStages { get; set; } = Enumerable.Empty<string>();

    [Option("enable-stages", Required = false, HelpText = "Comma-separated list of stages to enable", Separator = ',')]
    public IEnumerable<string> EnableStages { get; set; } = Enumerable.Empty<string>();

    [Option("manual-stages", Required = false, HelpText = "Comma-separated list of stages to make manual", Separator = ',')]
    public IEnumerable<string> ManualStages { get; set; } = Enumerable.Empty<string>();

    [Option("parallel-execution", Required = false, Default = true, HelpText = "Enable parallel job execution where possible")]
    public bool EnableParallelExecution { get; set; } = true;

    [Option("enable-caching", Required = false, Default = true, HelpText = "Enable caching optimizations")]
    public bool EnableCaching { get; set; } = true;

    [Option("enable-notifications", Required = false, Default = false, HelpText = "Enable pipeline notifications")]
    public bool EnableNotifications { get; set; } = false;

    [Option("show-sample", Required = false, Default = false, HelpText = "Show sample pipeline output for the specified project type")]
    public bool ShowSample { get; set; } = false;

    /// <summary>
    /// Validates the command-line options and returns validation errors
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // Validate project type
        var validProjectTypes = new[] { "dotnet", "nodejs", "python", "docker", "generic" };
        if (string.IsNullOrWhiteSpace(ProjectType) || !validProjectTypes.Contains(ProjectType.ToLowerInvariant()))
        {
            errors.Add($"Invalid project type '{ProjectType}'. Valid types are: {string.Join(", ", validProjectTypes)}");
        }

        // Validate stages
        var stagesList = Stages?.ToList() ?? new List<string>();
        if (!stagesList.Any())
        {
            // Set default stages if none provided
            stagesList = new List<string> { "build", "test", "deploy" };
        }

        foreach (var stage in stagesList)
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

        // Validate output path if specified
        if (!string.IsNullOrEmpty(OutputPath))
        {
            try
            {
                var directory = Path.GetDirectoryName(OutputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    errors.Add($"Output directory does not exist: {directory}");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Invalid output path '{OutputPath}': {ex.Message}");
            }
        }

        // Validate variables format
        if (Variables != null)
        {
            foreach (var variable in Variables)
            {
                if (!variable.Contains('='))
                {
                    errors.Add($"Invalid variable format '{variable}'. Expected format: key=value");
                }
                else
                {
                    var parts = variable.Split('=', 2);
                    if (string.IsNullOrWhiteSpace(parts[0]))
                    {
                        errors.Add($"Variable name cannot be empty in '{variable}'");
                    }
                    if (parts[0].Contains(" "))
                    {
                        errors.Add($"Variable name '{parts[0]}' cannot contain spaces");
                    }
                }
            }
        }

        // Validate environments format
        if (Environments != null)
        {
            foreach (var environment in Environments)
            {
                if (!environment.Contains(':'))
                {
                    errors.Add($"Invalid environment format '{environment}'. Expected format: name:url");
                }
                else
                {
                    var parts = environment.Split(':', 2);
                    if (string.IsNullOrWhiteSpace(parts[0]))
                    {
                        errors.Add($"Environment name cannot be empty in '{environment}'");
                    }
                    if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) && !Uri.TryCreate(parts[1], UriKind.Absolute, out _))
                    {
                        errors.Add($"Invalid URL format for environment '{parts[0]}': {parts[1]}");
                    }
                }
            }
        }

        // Validate cache key format if specified
        if (!string.IsNullOrEmpty(CacheKey) && CacheKey.Contains(" "))
        {
            errors.Add("Cache key cannot contain spaces");
        }

        // Validate artifact expiration format
        if (!string.IsNullOrEmpty(ArtifactExpireIn))
        {
            var validUnits = new[] { "sec", "min", "hr", "day", "week", "month", "year" };
            var parts = ArtifactExpireIn.Split(' ');
            if (parts.Length != 2 || !int.TryParse(parts[0], out _) || !validUnits.Any(unit => parts[1].StartsWith(unit)))
            {
                errors.Add($"Invalid artifact expiration format '{ArtifactExpireIn}'. Expected format: '1 week', '30 days', etc.");
            }
        }

        // Validate template parameters format
        if (TemplateParameters != null)
        {
            foreach (var parameter in TemplateParameters)
            {
                if (!parameter.Contains('='))
                {
                    errors.Add($"Invalid template parameter format '{parameter}'. Expected format: key=value");
                }
                else
                {
                    var parts = parameter.Split('=', 2);
                    if (string.IsNullOrWhiteSpace(parts[0]))
                    {
                        errors.Add($"Template parameter name cannot be empty in '{parameter}'");
                    }
                }
            }
        }

        // Validate conflicting options
        if (ConsoleOutput && !string.IsNullOrEmpty(OutputPath))
        {
            errors.Add("Cannot specify both --console-output and --output options");
        }

        if (DryRun && ValidateOnly)
        {
            errors.Add("Cannot specify both --dry-run and --validate-only options");
        }

        if (ListTemplates && (DryRun || ValidateOnly))
        {
            errors.Add("Cannot use --list-templates with --dry-run or --validate-only options");
        }

        return errors;
    }

    /// <summary>
    /// Gets usage examples for the CLI
    /// </summary>
    /// <returns>List of usage examples</returns>
    public static List<string> GetUsageExamples()
    {
        return new List<string>
        {
            "Basic .NET pipeline:",
            "  gitlab-pipeline-generator --type dotnet --dotnet-version 9.0",
            "",
            "Custom stages and output:",
            "  gitlab-pipeline-generator --type dotnet --stages build,test,deploy --output my-pipeline.yml",
            "",
            "With custom variables:",
            "  gitlab-pipeline-generator --type nodejs --variables \"NODE_VERSION=18,BUILD_ENV=production\"",
            "",
            "With deployment environments:",
            "  gitlab-pipeline-generator --type dotnet --environments \"staging:https://staging.example.com,production:https://example.com\"",
            "",
            "Enable additional features:",
            "  gitlab-pipeline-generator --type python --include-code-quality --include-security --include-performance",
            "",
            "Dry run (generate without writing file):",
            "  gitlab-pipeline-generator --type dotnet --dry-run",
            "",
            "Output to console:",
            "  gitlab-pipeline-generator --type generic --console-output",
            "",
            "Validate options only:",
            "  gitlab-pipeline-generator --type dotnet --validate-only",
            "",
            "List available templates:",
            "  gitlab-pipeline-generator --type dotnet --list-templates",
            "",
            "Use a specific template:",
            "  gitlab-pipeline-generator --type dotnet --template dotnet-standard",
            "",
            "Use template with parameters:",
            "  gitlab-pipeline-generator --type dotnet --template dotnet-standard --template-params \"PROJECT_NAME=MyApp,SOLUTION_FILE=MyApp.sln\"",
            "",
            "Customize template stages:",
            "  gitlab-pipeline-generator --type dotnet --template dotnet-standard --disable-stages \"performance\" --manual-stages \"deploy\"",
            "",
            "With caching configuration:",
            "  gitlab-pipeline-generator --type dotnet --cache-paths \"node_modules,~/.nuget/packages\" --cache-key \"$CI_COMMIT_REF_SLUG\"",
            "",
            "Full example with all options:",
            "  gitlab-pipeline-generator \\",
            "    --type dotnet \\",
            "    --dotnet-version 9.0 \\",
            "    --stages build,test,deploy \\",
            "    --output .gitlab-ci.yml \\",
            "    --docker-image mcr.microsoft.com/dotnet/sdk:9.0 \\",
            "    --runner-tags \"docker,linux\" \\",
            "    --include-tests \\",
            "    --include-deployment \\",
            "    --include-code-quality \\",
            "    --variables \"BUILD_CONFIGURATION=Release,ASPNETCORE_ENVIRONMENT=Production\" \\",
            "    --environments \"staging:https://staging.example.com,production:https://example.com\" \\",
            "    --cache-paths \"~/.nuget/packages,obj,bin\" \\",
            "    --cache-key \"nuget-$CI_COMMIT_REF_SLUG\" \\",
            "    --artifact-paths \"publish,test-results\" \\",
            "    --artifact-expire \"2 weeks\" \\",
            "    --verbose"
        };
    }
}