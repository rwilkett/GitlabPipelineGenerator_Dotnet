using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Builders;

/// <summary>
/// Implementation of IStageBuilder for building pipeline stages
/// </summary>
public class StageBuilder : IStageBuilder
{
    private static readonly Dictionary<string, List<string>> DefaultStagesByProjectType = new()
    {
        ["dotnet"] = new() { "build", "test", "deploy" },
        ["nodejs"] = new() { "build", "test", "deploy" },
        ["python"] = new() { "build", "test", "deploy" },
        ["docker"] = new() { "build", "test", "deploy" },
        ["generic"] = new() { "build", "test", "deploy" }
    };

    private static readonly List<string> ValidStages = new()
    {
        "build", "test", "deploy", "review", "staging", "production", 
        "cleanup", "security", "quality", "performance", "package", "release"
    };

    /// <summary>
    /// Builds the list of stages for a pipeline based on the provided options
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <returns>List of stage names</returns>
    public async Task<List<string>> BuildStagesAsync(PipelineOptions options)
    {
        await Task.CompletedTask; // Placeholder for async operations

        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var stages = new List<string>();

        // Start with user-specified stages or defaults
        if (options.Stages.Any())
        {
            stages.AddRange(options.Stages);
        }
        else
        {
            stages.AddRange(GetDefaultStages(options.ProjectType));
        }

        // Add additional stages based on options
        if (options.IncludeCodeQuality && !stages.Contains("quality"))
        {
            // Insert quality stage after test stage
            var testIndex = stages.IndexOf("test");
            if (testIndex >= 0)
            {
                stages.Insert(testIndex + 1, "quality");
            }
            else
            {
                stages.Add("quality");
            }
        }

        if (options.IncludeSecurity && !stages.Contains("security"))
        {
            // Insert security stage after test stage (or quality if it exists)
            var insertIndex = stages.IndexOf("quality");
            if (insertIndex < 0)
                insertIndex = stages.IndexOf("test");
            
            if (insertIndex >= 0)
            {
                stages.Insert(insertIndex + 1, "security");
            }
            else
            {
                stages.Add("security");
            }
        }

        if (options.IncludePerformance && !stages.Contains("performance"))
        {
            // Insert performance stage before deploy
            var deployIndex = stages.IndexOf("deploy");
            if (deployIndex >= 0)
            {
                stages.Insert(deployIndex, "performance");
            }
            else
            {
                stages.Add("performance");
            }
        }

        // Add deployment environment stages
        foreach (var env in options.DeploymentEnvironments)
        {
            var envStage = env.Name.ToLowerInvariant();
            if (!stages.Contains(envStage))
            {
                stages.Add(envStage);
            }
        }

        // Remove duplicates while preserving order
        var uniqueStages = new List<string>();
        foreach (var stage in stages)
        {
            if (!uniqueStages.Contains(stage))
            {
                uniqueStages.Add(stage);
            }
        }

        // Validate stages
        var validationErrors = ValidateStages(uniqueStages, options.ProjectType);
        if (validationErrors.Any())
        {
            throw new InvalidOperationException($"Invalid stages: {string.Join(", ", validationErrors)}");
        }

        return uniqueStages;
    }

    /// <summary>
    /// Gets the default stages for a specific project type
    /// </summary>
    /// <param name="projectType">Type of project</param>
    /// <returns>List of default stage names</returns>
    public List<string> GetDefaultStages(string projectType)
    {
        if (string.IsNullOrWhiteSpace(projectType))
            return DefaultStagesByProjectType["generic"];

        var normalizedProjectType = projectType.ToLowerInvariant();
        return DefaultStagesByProjectType.TryGetValue(normalizedProjectType, out var stages) 
            ? new List<string>(stages) 
            : DefaultStagesByProjectType["generic"];
    }

    /// <summary>
    /// Validates that the provided stages are valid for the project type
    /// </summary>
    /// <param name="stages">Stages to validate</param>
    /// <param name="projectType">Project type</param>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> ValidateStages(List<string> stages, string projectType)
    {
        var errors = new List<string>();

        if (stages == null || !stages.Any())
        {
            errors.Add("At least one stage must be specified");
            return errors;
        }

        foreach (var stage in stages)
        {
            if (string.IsNullOrWhiteSpace(stage))
            {
                errors.Add("Stage names cannot be empty or whitespace");
                continue;
            }

            var normalizedStage = stage.ToLowerInvariant();
            if (!ValidStages.Contains(normalizedStage) && !IsCustomEnvironmentStage(normalizedStage))
            {
                errors.Add($"Invalid stage '{stage}'. Valid stages are: {string.Join(", ", ValidStages)}");
            }
        }

        // Validate stage order for common patterns
        ValidateStageOrder(stages, errors);

        return errors;
    }

    /// <summary>
    /// Checks if a stage name represents a custom environment stage
    /// </summary>
    /// <param name="stage">Stage name to check</param>
    /// <returns>True if it's a custom environment stage</returns>
    private static bool IsCustomEnvironmentStage(string stage)
    {
        // Allow custom environment names that are reasonable
        var commonEnvironmentPatterns = new[] { "dev", "development", "staging", "prod", "production", "qa", "uat" };
        return commonEnvironmentPatterns.Any(pattern => stage.Contains(pattern));
    }

    /// <summary>
    /// Validates the logical order of stages
    /// </summary>
    /// <param name="stages">List of stages to validate</param>
    /// <param name="errors">List to add validation errors to</param>
    private static void ValidateStageOrder(List<string> stages, List<string> errors)
    {
        var stageOrder = new Dictionary<string, int>
        {
            ["build"] = 1,
            ["test"] = 2,
            ["quality"] = 3,
            ["security"] = 4,
            ["performance"] = 5,
            ["package"] = 6,
            ["review"] = 7,
            ["staging"] = 8,
            ["production"] = 9,
            ["deploy"] = 10,
            ["cleanup"] = 11
        };

        var previousOrder = 0;
        foreach (var stage in stages)
        {
            var normalizedStage = stage.ToLowerInvariant();
            if (stageOrder.TryGetValue(normalizedStage, out var currentOrder))
            {
                if (currentOrder < previousOrder)
                {
                    errors.Add($"Stage '{stage}' appears out of logical order. Consider reordering stages.");
                }
                previousOrder = Math.Max(previousOrder, currentOrder);
            }
        }
    }
}