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

        // Register CLI services
        services.AddTransient<OutputFormatter>();

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
}
