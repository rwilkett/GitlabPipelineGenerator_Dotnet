using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using GitlabPipelineGenerator.CLI.Models;
using GitlabPipelineGenerator.CLI.Services;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Services;
using GitlabPipelineGenerator.Core.Builders;
using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Models;
using Microsoft.Extensions.Options;

namespace GitlabPipelineGenerator.CLI;

/// <summary>
/// Main program class for the GitLab Pipeline Generator CLI
/// </summary>
public class Program
{
    private static IServiceProvider? _serviceProvider;
    private static ILogger<Program>? _logger;

    /// <summary>
    /// Main entry point for the CLI application
    /// </summary>
    /// <param name="args">Command-line arguments</param>
    /// <returns>Exit code (0 = success, 1 = error)</returns>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Handle help requests before parsing arguments
            if (HelpService.HandleHelpRequest(args))
            {
                return 0;
            }

            // Configure services
            ConfigureServices();
            _logger = _serviceProvider!.GetRequiredService<ILogger<Program>>();

            // Parse command-line arguments
            var result = await Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsedAsync(async options => await RunAsync(options));

            return result.Tag == ParserResultType.Parsed ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            if (_logger != null)
            {
                _logger.LogCritical(ex, "Fatal error occurred");
            }
            return 1;
        }
        finally
        {
            // Dispose services
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    /// Configures dependency injection services
    /// </summary>
    private static void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Configure configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Configure GitLab settings
        services.Configure<GitLabApiSettings>(configuration.GetSection("GitLab"));

        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Register Core services
        services.AddTransient<IPipelineGenerator, PipelineGenerator>();
        services.AddTransient<IStageBuilder, StageBuilder>();
        services.AddTransient<IJobBuilder, JobBuilder>();
        services.AddTransient<IVariableBuilder, VariableBuilder>();
        services.AddTransient<YamlSerializationService>();
        services.AddTransient<ValidationService>();

        // Register template services if they exist
        services.AddTransient<IPipelineTemplateService, PipelineTemplateService>();
        services.AddTransient<ITemplateCustomizationService, TemplateCustomizationService>();

        // Register GitLab API services
        services.AddTransient<IGitLabAuthenticationService, GitLabAuthenticationService>();
        services.AddTransient<IGitLabProjectService, GitLabProjectService>();
        services.AddTransient<IProjectAnalysisService, ProjectAnalysisService>();
        services.AddTransient<IFilePatternAnalyzer, FilePatternAnalyzer>();
        services.AddTransient<IDependencyAnalyzer, DependencyAnalyzer>();
        services.AddTransient<IConfigurationAnalyzer, ConfigurationAnalyzer>();
        services.AddTransient<IAnalysisToPipelineMappingService, AnalysisToPipelineMappingService>();
        services.AddTransient<IntelligentPipelineGenerator>();

        // Register GitLab error handling and resilience services
        services.AddSingleton<GitLabApiErrorHandler>();
        services.AddSingleton<CircuitBreaker>();
        services.AddTransient<ResilientGitLabService>();
        services.AddTransient<IGitLabFallbackService, GitLabFallbackService>();
        services.AddTransient<DegradedAnalysisService>();

        // Register configuration management services
        services.AddSingleton<ICredentialStorageService, CrossPlatformCredentialStorageService>();
        services.AddTransient<IConfigurationProfileService, ConfigurationProfileService>();
        services.AddTransient<IConfigurationManagementService, ConfigurationManagementService>();

        // Register GitLab connection and validation services
        services.AddTransient<GitLabConnectionValidator>();
        services.AddTransient<IGitLabPermissionValidator, GitLabPermissionValidator>();
        services.AddTransient<IGitLabApiErrorHandler, GitLabApiErrorHandler>();

        // Register enhanced pipeline generator
        services.AddTransient<EnhancedPipelineGenerator>();

        // Register CLI services
        services.AddTransient<OutputFormatter>();
        services.AddTransient<VerboseOutputService>();
        services.AddTransient<UserFriendlyErrorService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Runs the pipeline generation with the provided options
    /// </summary>
    /// <param name="options">Parsed command-line options</param>
    /// <returns>Task representing the async operation</returns>
    private static async Task RunAsync(CommandLineOptions options)
    {
        _logger!.LogInformation("Starting GitLab Pipeline Generator CLI");

        try
        {
            // Configure logging level based on verbose flag
            if (options.Verbose)
            {
                _logger?.LogDebug("Verbose logging enabled");
            }

            // Validate command-line options
            var validationErrors = options.Validate();
            if (validationErrors.Any())
            {
                Console.Error.WriteLine("Validation errors:");
                foreach (var error in validationErrors)
                {
                    Console.Error.WriteLine($"  - {error}");
                }
                
                // Provide helpful suggestions
                var suggestions = GetCliValidationSuggestions(validationErrors);
                if (suggestions.Any())
                {
                    Console.WriteLine();
                    Console.WriteLine("Suggestions:");
                    foreach (var suggestion in suggestions)
                    {
                        Console.WriteLine($"  💡 {suggestion}");
                    }
                }
                
                Console.WriteLine();
                Console.WriteLine("Usage examples:");
                var examples = CommandLineOptions.GetUsageExamples();
                foreach (var example in examples)
                {
                    Console.WriteLine(example);
                }
                
                throw new InvalidOperationException("Invalid command-line options");
            }

            // If validate-only flag is set, just validate and exit
            if (options.ValidateOnly)
            {
                Console.WriteLine("✓ Command-line options are valid");
                _logger?.LogInformation("Options validation completed successfully");
                return;
            }

            // If show-sample flag is set, show sample output and exit
            if (options.ShowSample)
            {
                SampleOutputService.ShowSampleOutput(options.ProjectType);
                _logger?.LogInformation("Sample output displayed for project type: {ProjectType}", options.ProjectType);
                return;
            }

            // Handle GitLab project discovery operations
            if (options.ListProjects || !string.IsNullOrEmpty(options.SearchProjects))
            {
                await HandleProjectDiscoveryAsync(options);
                return;
            }

            // Handle GitLab project analysis workflow
            if (options.AnalyzeProject)
            {
                await HandleProjectAnalysisWorkflowAsync(options);
                return;
            }

            // Convert CLI options to pipeline options
            var pipelineOptions = OptionsConverter.ToPipelineOptions(options);
            _logger?.LogDebug("Converted CLI options to pipeline options");

            // Validate pipeline options using ValidationService
            try
            {
                GitlabPipelineGenerator.Core.Services.ValidationService.ValidateAndThrow(pipelineOptions);
                _logger?.LogDebug("Pipeline options validation passed");
            }
            catch (InvalidPipelineOptionsException ex)
            {
                _logger?.LogError("Pipeline options validation failed: {Errors}", string.Join(", ", ex.ValidationErrors));
                
                Console.Error.WriteLine("Pipeline configuration validation failed:");
                foreach (var error in ex.ValidationErrors)
                {
                    Console.Error.WriteLine($"  - {error}");
                }
                
                // Provide helpful suggestions
                var suggestions = GitlabPipelineGenerator.Core.Services.ValidationService.GetValidationSuggestions(ex.ValidationErrors);
                if (suggestions.Any())
                {
                    Console.WriteLine();
                    Console.WriteLine("Suggestions:");
                    foreach (var suggestion in suggestions)
                    {
                        Console.WriteLine($"  💡 {suggestion}");
                    }
                }
                
                throw;
            }

            // Generate pipeline
            var pipelineGenerator = _serviceProvider!.GetRequiredService<IPipelineGenerator>();
            _logger?.LogInformation("Generating pipeline for project type: {ProjectType}", pipelineOptions.ProjectType);
            
            var pipeline = await pipelineGenerator.GenerateAsync(pipelineOptions);
            _logger?.LogInformation("Pipeline generated successfully with {JobCount} jobs", pipeline.Jobs.Count);

            // Serialize to YAML
            var yamlContent = pipelineGenerator.SerializeToYaml(pipeline);
            _logger?.LogDebug("Pipeline serialized to YAML ({Length} characters)", yamlContent.Length);

            // Handle output
            var outputFormatter = _serviceProvider!.GetRequiredService<OutputFormatter>();
            
            // Validate YAML if verbose mode is enabled
            if (options.Verbose)
            {
                outputFormatter.ValidateYaml(yamlContent, options.Verbose);
            }
            
            if (options.DryRun)
            {
                Console.WriteLine("🔍 Dry run - pipeline generated successfully but not written to file");
                Console.WriteLine($"Generated pipeline with {pipeline.Jobs.Count} jobs across {pipeline.Stages.Count} stages");
                
                if (options.Verbose)
                {
                    outputFormatter.ShowPipelineStats(yamlContent, options.Verbose);
                }
                
                _logger?.LogInformation("Dry run completed successfully");
            }
            else if (options.ConsoleOutput)
            {
                await outputFormatter.WriteToConsoleAsync(yamlContent, options.Verbose);
                
                if (options.Verbose)
                {
                    outputFormatter.ShowPipelineStats(yamlContent, options.Verbose);
                }
                
                _logger?.LogInformation("Pipeline output written to console");
            }
            else
            {
                var outputPath = options.OutputPath ?? ".gitlab-ci.yml";
                await outputFormatter.WriteToFileAsync(yamlContent, outputPath, options.Verbose);
                
                if (options.Verbose)
                {
                    outputFormatter.ShowPipelineStats(yamlContent, options.Verbose);
                }
                
                _logger?.LogInformation("Pipeline written to file: {OutputPath}", outputPath);
            }

            Console.WriteLine("✓ Pipeline generation completed successfully");
        }
        catch (InvalidPipelineOptionsException ex)
        {
            _logger?.LogError(ex, "Invalid pipeline options");
            Console.Error.WriteLine("Pipeline options validation failed:");
            foreach (var error in ex.ValidationErrors)
            {
                Console.Error.WriteLine($"  - {error}");
            }
            throw;
        }
        catch (PipelineGenerationException ex)
        {
            _logger?.LogError(ex, "Pipeline generation failed");
            Console.Error.WriteLine($"Pipeline generation failed: {ex.Message}");
            if (options.Verbose && ex.InnerException != null)
            {
                Console.Error.WriteLine($"Details: {ex.InnerException.Message}");
            }
            throw;
        }
        catch (YamlSerializationException ex)
        {
            _logger?.LogError(ex, "YAML serialization failed");
            Console.Error.WriteLine($"YAML serialization failed: {ex.Message}");
            if (options.Verbose && ex.InnerException != null)
            {
                Console.Error.WriteLine($"Details: {ex.InnerException.Message}");
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error occurred");
            Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
            if (options.Verbose)
            {
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            throw;
        }
    }

    /// <summary>
    /// Gets validation suggestions for CLI-specific validation errors
    /// </summary>
    /// <param name="validationErrors">List of validation errors</param>
    /// <returns>List of helpful suggestions</returns>
    private static List<string> GetCliValidationSuggestions(List<string> validationErrors)
    {
        var suggestions = new List<string>();

        if (validationErrors == null || !validationErrors.Any())
            return suggestions;

        foreach (var error in validationErrors)
        {
            var lowerError = error.ToLowerInvariant();

            if (lowerError.Contains("invalid project type"))
            {
                suggestions.Add("Use one of the supported project types: dotnet, nodejs, python, docker, or generic");
            }
            else if (lowerError.Contains("stage") && lowerError.Contains("empty"))
            {
                suggestions.Add("Provide at least one stage name, e.g., --stages build,test,deploy");
            }
            else if (lowerError.Contains("stage") && lowerError.Contains("spaces"))
            {
                suggestions.Add("Use underscores or hyphens instead of spaces in stage names, e.g., 'code_quality' or 'code-quality'");
            }
            else if (lowerError.Contains("duplicate stage"))
            {
                suggestions.Add("Remove duplicate stage names from your stages list");
            }
            else if (lowerError.Contains("invalid .net version"))
            {
                suggestions.Add("Use a supported .NET version: 6.0, 7.0, 8.0, or 9.0");
            }
            else if (lowerError.Contains("variable") && lowerError.Contains("format"))
            {
                suggestions.Add("Use the format 'KEY=VALUE' for variables, e.g., --variables \"BUILD_CONFIG=Release,NODE_ENV=production\"");
            }
            else if (lowerError.Contains("environment") && lowerError.Contains("format"))
            {
                suggestions.Add("Use the format 'name:url' for environments, e.g., --environments \"staging:https://staging.example.com\"");
            }
            else if (lowerError.Contains("invalid url"))
            {
                suggestions.Add("Ensure environment URLs are valid and include the protocol (http:// or https://)");
            }
            else if (lowerError.Contains("artifact expiration"))
            {
                suggestions.Add("Use format like '1 week', '30 days', '2 hours' for artifact expiration");
            }
            else if (lowerError.Contains("too long"))
            {
                suggestions.Add("Shorten the value to meet the maximum length requirement");
            }
            else if (lowerError.Contains("cannot contain spaces"))
            {
                suggestions.Add("Replace spaces with underscores or hyphens");
            }
            else if (lowerError.Contains("output directory does not exist"))
            {
                suggestions.Add("Create the output directory first or use a different output path");
            }
            else if (lowerError.Contains("cannot specify both"))
            {
                suggestions.Add("Choose only one of the conflicting options");
            }
        }

        // Remove duplicates and return unique suggestions
        return suggestions.Distinct().ToList();
    }

    /// <summary>
    /// Handles GitLab project discovery operations (list/search projects)
    /// </summary>
    /// <param name="options">Command-line options</param>
    /// <returns>Task representing the async operation</returns>
    private static async Task HandleProjectDiscoveryAsync(CommandLineOptions options)
    {
        _logger?.LogInformation("Starting GitLab project discovery");

        var verboseOutput = new VerboseOutputService(options.Verbose);
        var errorService = new UserFriendlyErrorService(options.Verbose);

        try
        {
            // Authenticate with GitLab
            var authService = _serviceProvider!.GetRequiredService<IGitLabAuthenticationService>();
            var connectionOptions = CreateGitLabConnectionOptions(options);
            
            var gitlabClient = await ProgressIndicatorService.ExecuteWithProgressAsync(
                () => authService.AuthenticateAsync(connectionOptions),
                "Authenticating with GitLab",
                "GitLab authentication");

            _logger?.LogInformation("Successfully authenticated with GitLab");

            if (options.Verbose)
            {
                var userInfo = await authService.GetCurrentUserAsync();
                verboseOutput.DisplayAuthenticationDetails(connectionOptions, userInfo);
            }

            // Get project service
            var projectService = _serviceProvider!.GetRequiredService<IGitLabProjectService>();

            if (options.ListProjects)
            {
                await ListProjectsAsync(projectService, options);
            }
            else if (!string.IsNullOrEmpty(options.SearchProjects))
            {
                await SearchProjectsAsync(projectService, options);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GitLab project discovery failed");
            
            errorService.DisplayError(ex, "GitLab project discovery");
            verboseOutput.DisplayErrorDetails(ex, "Project discovery");
            
            throw;
        }
    }

    /// <summary>
    /// Handles GitLab project analysis workflow
    /// </summary>
    /// <param name="options">Command-line options</param>
    /// <returns>Task representing the async operation</returns>
    private static async Task HandleProjectAnalysisWorkflowAsync(CommandLineOptions options)
    {
        _logger?.LogInformation("Starting GitLab project analysis workflow");

        var verboseOutput = new VerboseOutputService(options.Verbose);
        var errorService = new UserFriendlyErrorService(options.Verbose);

        try
        {
            // Authenticate with GitLab
            var authService = _serviceProvider!.GetRequiredService<IGitLabAuthenticationService>();
            var connectionOptions = CreateGitLabConnectionOptions(options);
            
            var gitlabClient = await ProgressIndicatorService.ExecuteWithProgressAsync(
                () => authService.AuthenticateAsync(connectionOptions),
                "Authenticating with GitLab",
                "GitLab authentication");

            var userInfo = await authService.GetCurrentUserAsync();
            _logger?.LogInformation("Successfully authenticated with GitLab");

            verboseOutput.DisplayAuthenticationDetails(connectionOptions, userInfo);

            // Get project information
            var projectService = _serviceProvider!.GetRequiredService<IGitLabProjectService>();
            var project = await ProgressIndicatorService.ExecuteWithProgressAsync(
                () => projectService.GetProjectAsync(options.GitLabProject!),
                "Retrieving project information",
                "Project information retrieved");

            _logger?.LogInformation("Retrieved project: {ProjectName} ({ProjectId})", project.Name, project.Id);

            Console.WriteLine($"📋 Analyzing project: {project.Name}");
            Console.WriteLine($"   Path: {project.FullPath}");
            Console.WriteLine($"   URL: {project.WebUrl}");
            Console.WriteLine();

            // Get and display project permissions
            var permissions = await projectService.GetProjectPermissionsAsync(project.Id);
            verboseOutput.DisplayProjectDetails(project, permissions);

            // Perform project analysis
            var analysisService = _serviceProvider!.GetRequiredService<IProjectAnalysisService>();
            var analysisOptions = CreateAnalysisOptions(options);
            
            verboseOutput.DisplayAnalysisOptions(analysisOptions);

            var analysisResult = await ProgressIndicatorService.ExecuteWithProgressAsync(
                () => analysisService.AnalyzeProjectAsync(project, analysisOptions),
                "Analyzing project structure and dependencies",
                "Project analysis");

            _logger?.LogInformation("Project analysis completed with confidence: {Confidence}", analysisResult.Confidence);

            // Show analysis results if requested
            if (options.ShowAnalysis)
            {
                DisplayAnalysisResults(analysisResult, options.Verbose);
                verboseOutput.DisplayAnalysisResults(analysisResult);
            }

            // Convert CLI options to pipeline options and merge with analysis
            var basePipelineOptions = OptionsConverter.ToPipelineOptions(options);
            var enhancedOptions = MergeAnalysisWithOptions(basePipelineOptions, analysisResult, options);

            // Show conflicts if requested
            if (options.ShowConflicts)
            {
                DisplayConfigurationConflicts(basePipelineOptions, enhancedOptions, analysisResult);
            }

            // Ask for confirmation if analysis preview is enabled
            if (options.ShowAnalysis && !options.DryRun && !options.ValidateOnly)
            {
                Console.WriteLine();
                Console.Write("Continue with pipeline generation? (y/N): ");
                var response = Console.ReadLine();
                if (!string.Equals(response?.Trim(), "y", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(response?.Trim(), "yes", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Pipeline generation cancelled.");
                    return;
                }
            }

            // Generate pipeline using intelligent generator
            var intelligentGenerator = _serviceProvider!.GetRequiredService<IntelligentPipelineGenerator>();
            _logger?.LogInformation("Generating intelligent pipeline");
            
            var pipeline = await ProgressIndicatorService.ExecuteWithProgressAsync(
                () => intelligentGenerator.GenerateAsync(enhancedOptions),
                "Generating intelligent pipeline",
                "Pipeline generation");

            _logger?.LogInformation("Intelligent pipeline generated successfully with {JobCount} jobs", pipeline.Jobs.Count);

            verboseOutput.DisplayPipelineGenerationDetails(pipeline, enhancedOptions);

            // Serialize to YAML
            var yamlContent = intelligentGenerator.SerializeToYaml(pipeline);
            _logger?.LogDebug("Pipeline serialized to YAML ({Length} characters)", yamlContent.Length);

            // Handle output
            var outputFormatter = _serviceProvider!.GetRequiredService<OutputFormatter>();
            
            // Validate YAML if verbose mode is enabled
            if (options.Verbose)
            {
                outputFormatter.ValidateYaml(yamlContent, options.Verbose);
            }
            
            if (options.DryRun)
            {
                Console.WriteLine("🔍 Dry run - intelligent pipeline generated successfully but not written to file");
                Console.WriteLine($"Generated pipeline with {pipeline.Jobs.Count} jobs across {pipeline.Stages.Count} stages");
                Console.WriteLine($"Based on analysis: {analysisResult.DetectedType} project with {analysisResult.Framework.Name}");
                
                if (options.Verbose)
                {
                    outputFormatter.ShowPipelineStats(yamlContent, options.Verbose);
                }
                
                _logger?.LogInformation("Dry run completed successfully");
            }
            else if (options.ConsoleOutput)
            {
                await outputFormatter.WriteToConsoleAsync(yamlContent, options.Verbose);
                
                if (options.Verbose)
                {
                    outputFormatter.ShowPipelineStats(yamlContent, options.Verbose);
                }
                
                _logger?.LogInformation("Pipeline output written to console");
            }
            else
            {
                var outputPath = options.OutputPath ?? ".gitlab-ci.yml";
                await outputFormatter.WriteToFileAsync(yamlContent, outputPath, options.Verbose);
                
                if (options.Verbose)
                {
                    outputFormatter.ShowPipelineStats(yamlContent, options.Verbose);
                }
                
                _logger?.LogInformation("Pipeline written to file: {OutputPath}", outputPath);
            }

            Console.WriteLine("✓ Intelligent pipeline generation completed successfully");
            Console.WriteLine($"  Based on {analysisResult.DetectedType} project analysis");
            if (analysisResult.Warnings.Any())
            {
                Console.WriteLine($"  ⚠️  {analysisResult.Warnings.Count} analysis warnings (use --verbose for details)");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GitLab project analysis workflow failed");
            
            errorService.DisplayError(ex, "GitLab project analysis workflow");
            verboseOutput.DisplayErrorDetails(ex, "Project analysis workflow");

            // Offer fallback to manual mode
            errorService.DisplayInfo("You can still generate a pipeline manually by specifying --type and other options");
            Console.WriteLine("   Example: gitlab-pipeline-generator --type dotnet --dotnet-version 9.0");
            Console.WriteLine();
            
            throw;
        }
    }

    /// <summary>
    /// Creates GitLab connection options from CLI options
    /// </summary>
    /// <param name="options">Command-line options</param>
    /// <returns>GitLab connection options</returns>
    private static GitLabConnectionOptions CreateGitLabConnectionOptions(CommandLineOptions options)
    {
        return new GitLabConnectionOptions
        {
            PersonalAccessToken = options.GitLabToken,
            InstanceUrl = options.GitLabUrl,
            ProfileName = options.GitLabProfile,
            StoreCredentials = false // CLI doesn't store credentials by default
        };
    }

    /// <summary>
    /// Creates analysis options from CLI options
    /// </summary>
    /// <param name="options">Command-line options</param>
    /// <returns>Analysis options</returns>
    private static AnalysisOptions CreateAnalysisOptions(CommandLineOptions options)
    {
        var skipTypes = options.SkipAnalysis?.ToList() ?? new List<string>();
        
        return new AnalysisOptions
        {
            AnalyzeFiles = !skipTypes.Contains("files", StringComparer.OrdinalIgnoreCase),
            AnalyzeDependencies = !skipTypes.Contains("dependencies", StringComparer.OrdinalIgnoreCase),
            AnalyzeExistingCI = !skipTypes.Contains("config", StringComparer.OrdinalIgnoreCase),
            AnalyzeDeployment = !skipTypes.Contains("deployment", StringComparer.OrdinalIgnoreCase),
            MaxFileAnalysisDepth = options.AnalysisDepth,
            ExcludePatterns = options.AnalysisExcludePatterns?.ToList() ?? new List<string>(),
            IncludeSecurityAnalysis = options.IncludeSecurity
        };
    }

    /// <summary>
    /// Lists GitLab projects based on options
    /// </summary>
    /// <param name="projectService">Project service</param>
    /// <param name="options">Command-line options</param>
    /// <returns>Task representing the async operation</returns>
    private static async Task ListProjectsAsync(IGitLabProjectService projectService, CommandLineOptions options)
    {
        var listOptions = CreateProjectListOptions(options);
        
        var projects = await ProgressIndicatorService.ExecuteWithProgressAsync(
            () => projectService.ListProjectsAsync(listOptions),
            "Retrieving project list",
            "Project list retrieved");
        
        Console.WriteLine($"📋 Found {projects.Count()} accessible projects:");
        Console.WriteLine();
        
        foreach (var project in projects)
        {
            Console.WriteLine($"  {project.Id,8} | {project.FullPath,-40} | {project.Visibility,-10} | {project.LastActivityAt:yyyy-MM-dd}");
            if (options.Verbose && !string.IsNullOrEmpty(project.Description))
            {
                Console.WriteLine($"           | {project.Description}");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine("💡 Use --gitlab-project <id-or-path> to analyze a specific project");
    }

    /// <summary>
    /// Searches GitLab projects based on options
    /// </summary>
    /// <param name="projectService">Project service</param>
    /// <param name="options">Command-line options</param>
    /// <returns>Task representing the async operation</returns>
    private static async Task SearchProjectsAsync(IGitLabProjectService projectService, CommandLineOptions options)
    {
        var projects = await ProgressIndicatorService.ExecuteWithProgressAsync(
            () => projectService.SearchProjectsAsync(options.SearchProjects!),
            $"Searching for projects matching '{options.SearchProjects}'",
            "Project search completed");
        
        Console.WriteLine($"🔍 Search results for '{options.SearchProjects}' ({projects.Count()} found):");
        Console.WriteLine();
        
        foreach (var project in projects.Take(options.MaxProjects))
        {
            Console.WriteLine($"  {project.Id,8} | {project.FullPath,-40} | {project.Visibility,-10}");
            if (options.Verbose && !string.IsNullOrEmpty(project.Description))
            {
                Console.WriteLine($"           | {project.Description}");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine("💡 Use --gitlab-project <id-or-path> to analyze a specific project");
    }

    /// <summary>
    /// Creates project list options from CLI options
    /// </summary>
    /// <param name="options">Command-line options</param>
    /// <returns>Project list options</returns>
    private static ProjectListOptions CreateProjectListOptions(CommandLineOptions options)
    {
        var filters = options.ProjectFilter?.ToList() ?? new List<string>();
        
        return new ProjectListOptions
        {
            OwnedOnly = filters.Contains("owned", StringComparer.OrdinalIgnoreCase),
            MemberOnly = filters.Contains("member", StringComparer.OrdinalIgnoreCase) || !filters.Any(),
            Visibility = GetVisibilityFilter(filters),
            MaxResults = options.MaxProjects
        };
    }

    /// <summary>
    /// Gets visibility filter from project filters
    /// </summary>
    /// <param name="filters">Project filters</param>
    /// <returns>Visibility filter or null</returns>
    private static ProjectVisibility? GetVisibilityFilter(List<string> filters)
    {
        if (filters.Contains("public", StringComparer.OrdinalIgnoreCase))
            return ProjectVisibility.Public;
        if (filters.Contains("private", StringComparer.OrdinalIgnoreCase))
            return ProjectVisibility.Private;
        if (filters.Contains("internal", StringComparer.OrdinalIgnoreCase))
            return ProjectVisibility.Internal;
        
        return null;
    }

    /// <summary>
    /// Displays analysis results to the console
    /// </summary>
    /// <param name="result">Analysis result</param>
    /// <param name="verbose">Whether to show verbose output</param>
    private static void DisplayAnalysisResults(ProjectAnalysisResult result, bool verbose)
    {
        Console.WriteLine("📊 Analysis Results:");
        Console.WriteLine($"   Project Type: {result.DetectedType}");
        Console.WriteLine($"   Framework: {result.Framework.Name} {result.Framework.Version}");
        Console.WriteLine($"   Build Tool: {result.BuildConfig.BuildTool}");
        Console.WriteLine($"   Confidence: {result.Confidence}");
        
        if (result.Dependencies.Dependencies.Any())
        {
            Console.WriteLine($"   Dependencies: {result.Dependencies.Dependencies.Count} packages");
        }
        
        if (result.ExistingCI != null)
        {
            Console.WriteLine($"   Existing CI: {result.ExistingCI.SystemType} detected");
        }
        
        if (verbose)
        {
            if (result.Framework.DetectedFeatures.Any())
            {
                Console.WriteLine("   Features:");
                foreach (var feature in result.Framework.DetectedFeatures)
                {
                    Console.WriteLine($"     - {feature}");
                }
            }
            
            if (result.BuildConfig.BuildCommands.Any())
            {
                Console.WriteLine("   Build Commands:");
                foreach (var command in result.BuildConfig.BuildCommands)
                {
                    Console.WriteLine($"     - {command}");
                }
            }
        }
        
        if (result.Warnings.Any())
        {
            Console.WriteLine("   ⚠️  Warnings:");
            foreach (var warning in result.Warnings)
            {
                Console.WriteLine($"     - {warning.Message}");
            }
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// Merges analysis results with CLI options
    /// </summary>
    /// <param name="baseOptions">Base pipeline options from CLI</param>
    /// <param name="analysisResult">Analysis result</param>
    /// <param name="cliOptions">CLI options for merge behavior</param>
    /// <returns>Enhanced pipeline options</returns>
    private static AnalysisBasedPipelineOptions MergeAnalysisWithOptions(
        PipelineOptions baseOptions, 
        ProjectAnalysisResult analysisResult, 
        CommandLineOptions cliOptions)
    {
        var enhancedOptions = new AnalysisBasedPipelineOptions
        {
            AnalysisResult = analysisResult,
            UseAnalysisDefaults = cliOptions.PreferDetected
        };

        // Copy base options
        enhancedOptions.ProjectType = baseOptions.ProjectType;
        enhancedOptions.Stages = baseOptions.Stages;
        enhancedOptions.DotNetVersion = baseOptions.DotNetVersion;
        enhancedOptions.IncludeTests = baseOptions.IncludeTests;
        enhancedOptions.IncludeDeployment = baseOptions.IncludeDeployment;
        enhancedOptions.DockerImage = baseOptions.DockerImage;
        enhancedOptions.RunnerTags = baseOptions.RunnerTags;
        enhancedOptions.IncludeCodeQuality = baseOptions.IncludeCodeQuality;
        enhancedOptions.IncludeSecurity = baseOptions.IncludeSecurity;
        enhancedOptions.IncludePerformance = baseOptions.IncludePerformance;
        enhancedOptions.CustomVariables = baseOptions.CustomVariables;
        enhancedOptions.DeploymentEnvironments = baseOptions.DeploymentEnvironments;
        enhancedOptions.Cache = baseOptions.Cache;
        enhancedOptions.Artifacts = baseOptions.Artifacts;
        enhancedOptions.Notifications = baseOptions.Notifications;

        return enhancedOptions;
    }

    /// <summary>
    /// Displays configuration conflicts between CLI and analysis
    /// </summary>
    /// <param name="cliOptions">CLI-based options</param>
    /// <param name="enhancedOptions">Enhanced options with analysis</param>
    /// <param name="analysisResult">Analysis result</param>
    private static void DisplayConfigurationConflicts(
        PipelineOptions cliOptions, 
        AnalysisBasedPipelineOptions enhancedOptions, 
        ProjectAnalysisResult analysisResult)
    {
        Console.WriteLine("⚖️  Configuration Comparison:");
        
        // Compare project type
        if (!string.IsNullOrEmpty(cliOptions.ProjectType) && 
            !cliOptions.ProjectType.Equals(analysisResult.DetectedType.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"   Project Type: CLI='{cliOptions.ProjectType}' vs Detected='{analysisResult.DetectedType}'");
        }
        
        // Compare .NET version if applicable
        if (!string.IsNullOrEmpty(cliOptions.DotNetVersion) && 
            !string.IsNullOrEmpty(analysisResult.Framework.Version) &&
            !cliOptions.DotNetVersion.Equals(analysisResult.Framework.Version))
        {
            Console.WriteLine($"   .NET Version: CLI='{cliOptions.DotNetVersion}' vs Detected='{analysisResult.Framework.Version}'");
        }
        
        // Compare cache paths
        if (cliOptions.Cache?.Paths?.Any() == true && analysisResult.Dependencies.CacheRecommendation.CachePaths.Any())
        {
            var cliPaths = string.Join(", ", cliOptions.Cache.Paths);
            var detectedPaths = string.Join(", ", analysisResult.Dependencies.CacheRecommendation.CachePaths);
            if (!cliPaths.Equals(detectedPaths))
            {
                Console.WriteLine($"   Cache Paths: CLI='{cliPaths}' vs Detected='{detectedPaths}'");
            }
        }
        
        Console.WriteLine($"   Resolution: {(enhancedOptions.UseAnalysisDefaults ? "Preferring detected settings" : "Preferring CLI settings")}");
        Console.WriteLine();
    }
}
