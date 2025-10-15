using CommandLine;
using System;

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

    // GitLab API Integration Options
    [Option("gitlab-token", Required = false, HelpText = "GitLab personal access token for API authentication")]
    public string? GitLabToken { get; set; }

    [Option("gitlab-url", Required = false, Default = "https://gitlab.com", HelpText = "GitLab instance URL (default: https://gitlab.com)")]
    public string GitLabUrl { get; set; } = "https://gitlab.com";

    [Option("gitlab-project", Required = false, HelpText = "GitLab project ID or path (e.g., 'group/project' or '12345')")]
    public string? GitLabProject { get; set; }

    [Option("gitlab-profile", Required = false, HelpText = "GitLab connection profile name to use")]
    public string? GitLabProfile { get; set; }

    // Project Analysis Options
    [Option("analyze-project", Required = false, Default = false, HelpText = "Enable automatic project analysis using GitLab API")]
    public bool AnalyzeProject { get; set; } = false;

    [Option("analysis-depth", Required = false, Default = 2, HelpText = "Analysis depth level (1-3): 1=basic, 2=standard, 3=comprehensive")]
    public int AnalysisDepth { get; set; } = 2;

    [Option("skip-analysis", Required = false, HelpText = "Skip specific analysis types: files,dependencies,config,deployment", Separator = ',')]
    public IEnumerable<string> SkipAnalysis { get; set; } = Enumerable.Empty<string>();

    [Option("analysis-exclude", Required = false, HelpText = "File patterns to exclude from analysis (glob patterns)", Separator = ',')]
    public IEnumerable<string> AnalysisExcludePatterns { get; set; } = Enumerable.Empty<string>();

    [Option("show-analysis", Required = false, Default = false, HelpText = "Display analysis results before pipeline generation")]
    public bool ShowAnalysis { get; set; } = false;

    // Hybrid Mode Options
    [Option("prefer-detected", Required = false, Default = false, HelpText = "Prefer detected settings over CLI options when conflicts occur")]
    public bool PreferDetected { get; set; } = false;

    [Option("merge-config", Required = false, Default = true, HelpText = "Merge detected and manual configurations (default behavior)")]
    public bool MergeConfig { get; set; } = true;

    [Option("show-conflicts", Required = false, Default = false, HelpText = "Show conflicts between detected and manual settings")]
    public bool ShowConflicts { get; set; } = false;

    // Project Discovery Options
    [Option("list-projects", Required = false, Default = false, HelpText = "List accessible GitLab projects")]
    public bool ListProjects { get; set; } = false;

    [Option("search-projects", Required = false, HelpText = "Search GitLab projects by name or description")]
    public string? SearchProjects { get; set; }

    [Option("project-filter", Required = false, HelpText = "Filter projects: owned,member,public,private,internal", Separator = ',')]
    public IEnumerable<string> ProjectFilter { get; set; } = Enumerable.Empty<string>();

    [Option("max-projects", Required = false, Default = 50, HelpText = "Maximum number of projects to list or search")]
    public int MaxProjects { get; set; } = 50;

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

        // Validate GitLab URL format
        if (!string.IsNullOrEmpty(GitLabUrl))
        {
            if (!Uri.TryCreate(GitLabUrl, UriKind.Absolute, out var gitlabUri))
            {
                errors.Add($"Invalid GitLab URL format: {GitLabUrl}");
            }
            else if (!gitlabUri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && 
                     !gitlabUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"GitLab URL must use HTTP or HTTPS protocol: {GitLabUrl}");
            }
        }

        // Validate analysis depth
        if (AnalysisDepth < 1 || AnalysisDepth > 3)
        {
            errors.Add("Analysis depth must be between 1 and 3");
        }

        // Validate skip analysis options
        if (SkipAnalysis != null)
        {
            var validSkipTypes = new[] { "files", "dependencies", "config", "deployment" };
            foreach (var skipType in SkipAnalysis)
            {
                if (!validSkipTypes.Contains(skipType.ToLowerInvariant()))
                {
                    errors.Add($"Invalid skip analysis type '{skipType}'. Valid types are: {string.Join(", ", validSkipTypes)}");
                }
            }
        }

        // Validate project filter options
        if (ProjectFilter != null)
        {
            var validFilters = new[] { "owned", "member", "public", "private", "internal" };
            foreach (var filter in ProjectFilter)
            {
                if (!validFilters.Contains(filter.ToLowerInvariant()))
                {
                    errors.Add($"Invalid project filter '{filter}'. Valid filters are: {string.Join(", ", validFilters)}");
                }
            }
        }

        // Validate max projects
        if (MaxProjects < 1 || MaxProjects > 1000)
        {
            errors.Add("Max projects must be between 1 and 1000");
        }

        // Validate GitLab-specific requirements
        if (AnalyzeProject && string.IsNullOrEmpty(GitLabToken) && string.IsNullOrEmpty(GitLabProfile))
        {
            errors.Add("GitLab token or profile is required when using --analyze-project");
        }

        if (AnalyzeProject && string.IsNullOrEmpty(GitLabProject))
        {
            errors.Add("GitLab project ID or path is required when using --analyze-project");
        }

        if ((ListProjects || !string.IsNullOrEmpty(SearchProjects)) && 
            string.IsNullOrEmpty(GitLabToken) && string.IsNullOrEmpty(GitLabProfile))
        {
            errors.Add("GitLab token or profile is required for project discovery operations");
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

        if (ListProjects && !string.IsNullOrEmpty(SearchProjects))
        {
            errors.Add("Cannot specify both --list-projects and --search-projects options");
        }

        if ((ListProjects || !string.IsNullOrEmpty(SearchProjects)) && AnalyzeProject)
        {
            errors.Add("Cannot use project discovery options with --analyze-project");
        }

        if (!string.IsNullOrEmpty(GitLabProfile) && !string.IsNullOrEmpty(GitLabToken))
        {
            errors.Add("Cannot specify both --gitlab-profile and --gitlab-token options");
        }

        if (PreferDetected && !AnalyzeProject)
        {
            errors.Add("--prefer-detected can only be used with --analyze-project");
        }

        if (ShowConflicts && !AnalyzeProject)
        {
            errors.Add("--show-conflicts can only be used with --analyze-project");
        }

        if (ShowAnalysis && !AnalyzeProject)
        {
            errors.Add("--show-analysis can only be used with --analyze-project");
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
            "GitLab project analysis:",
            "  gitlab-pipeline-generator --analyze-project --gitlab-token <token> --gitlab-project group/project",
            "",
            "List GitLab projects:",
            "  gitlab-pipeline-generator --list-projects --gitlab-token <token>",
            "",
            "Search GitLab projects:",
            "  gitlab-pipeline-generator --search-projects \"my-app\" --gitlab-token <token>",
            "",
            "Analyze with custom GitLab instance:",
            "  gitlab-pipeline-generator --analyze-project --gitlab-url https://gitlab.company.com --gitlab-token <token> --gitlab-project 123",
            "",
            "Hybrid mode (analysis + manual overrides):",
            "  gitlab-pipeline-generator --analyze-project --gitlab-token <token> --gitlab-project group/project --type dotnet --dotnet-version 9.0",
            "",
            "Analysis with custom depth and exclusions:",
            "  gitlab-pipeline-generator --analyze-project --gitlab-token <token> --gitlab-project group/project --analysis-depth 3 --analysis-exclude \"*.min.js,node_modules/**\"",
            "",
            "Show analysis results before generation:",
            "  gitlab-pipeline-generator --analyze-project --gitlab-token <token> --gitlab-project group/project --show-analysis --show-conflicts",
            "",
            "Use GitLab profile:",
            "  gitlab-pipeline-generator --analyze-project --gitlab-profile company --gitlab-project group/project",
            "",
            "Filter projects by type:",
            "  gitlab-pipeline-generator --list-projects --gitlab-token <token> --project-filter owned,private --max-projects 20",
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
            "    --verbose",
            "",
            "Full GitLab analysis example:",
            "  gitlab-pipeline-generator \\",
            "    --analyze-project \\",
            "    --gitlab-token <token> \\",
            "    --gitlab-project group/my-project \\",
            "    --analysis-depth 3 \\",
            "    --show-analysis \\",
            "    --show-conflicts \\",
            "    --merge-config \\",
            "    --include-code-quality \\",
            "    --include-security \\",
            "    --verbose"
        };
    }
}