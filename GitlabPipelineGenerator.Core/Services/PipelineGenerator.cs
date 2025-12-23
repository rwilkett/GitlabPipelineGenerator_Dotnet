using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;
using GitlabPipelineGenerator.Core.Builders;
using GitlabPipelineGenerator.Core.Exceptions;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Implementation of IPipelineGenerator for generating GitLab CI/CD pipeline configurations
/// </summary>
public class PipelineGenerator : IPipelineGenerator
{
    private readonly IStageBuilder _stageBuilder;
    private readonly IJobBuilder _jobBuilder;
    private readonly IVariableBuilder _variableBuilder;
    private readonly YamlSerializationService _yamlService;

    public PipelineGenerator(
        IStageBuilder stageBuilder,
        IJobBuilder jobBuilder,
        IVariableBuilder variableBuilder,
        YamlSerializationService yamlService)
    {
        _stageBuilder = stageBuilder ?? throw new ArgumentNullException(nameof(stageBuilder));
        _jobBuilder = jobBuilder ?? throw new ArgumentNullException(nameof(jobBuilder));
        _variableBuilder = variableBuilder ?? throw new ArgumentNullException(nameof(variableBuilder));
        _yamlService = yamlService ?? throw new ArgumentNullException(nameof(yamlService));
    }

    /// <summary>
    /// Generates a pipeline configuration based on the provided options
    /// </summary>
    /// <param name="options">Pipeline generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated pipeline configuration</returns>
    /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
    /// <exception cref="PipelineGenerationException">Thrown when pipeline generation fails</exception>
    public async Task<PipelineConfiguration> GenerateAsync(PipelineOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Validate options using ValidationService
        ValidationService.ValidateAndThrow(options);

        try
        {
            var pipeline = new PipelineConfiguration();

            // Set stages
            pipeline.Stages = await _stageBuilder.BuildStagesAsync(options).ConfigureAwait(false);

            // Generate global variables
            pipeline.Variables = await _variableBuilder.BuildGlobalVariablesAsync(options).ConfigureAwait(false);

            // Generate jobs for each stage
            var jobs = new Dictionary<string, Job>();
            
            foreach (var stage in pipeline.Stages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var stageJobs = await _jobBuilder.BuildJobsForStageAsync(stage, options).ConfigureAwait(false);
                foreach (var job in stageJobs)
                {
                    jobs[job.Key] = job.Value;
                }
            }

            // Add custom jobs
            foreach (var customJob in options.CustomJobs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var job = await _jobBuilder.BuildCustomJobAsync(customJob, options).ConfigureAwait(false);
                jobs[customJob.Name] = job;
            }

            pipeline.Jobs = jobs;

            // Set default configuration
            pipeline.Default = await _variableBuilder.BuildDefaultConfigurationAsync(options).ConfigureAwait(false);

            // Set workflow rules if needed
            if (ShouldAddWorkflowRules(options))
            {
                pipeline.Workflow = BuildWorkflowRules(options);
            }

            return pipeline;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not (ArgumentNullException or InvalidPipelineOptionsException))
        {
            throw new PipelineGenerationException($"Failed to generate pipeline: {ex.Message}", options, "generation", ex);
        }
    }

    /// <summary>
    /// Serializes a pipeline configuration to YAML format
    /// </summary>
    /// <param name="pipeline">Pipeline configuration to serialize</param>
    /// <returns>YAML representation of the pipeline</returns>
    /// <exception cref="ArgumentNullException">Thrown when pipeline is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when YAML serialization fails</exception>
    public string SerializeToYaml(PipelineConfiguration pipeline)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));

        return _yamlService.SerializePipeline(pipeline);
    }

    /// <summary>
    /// Determines if workflow rules should be added to the pipeline
    /// </summary>
    /// <param name="options">Pipeline options</param>
    /// <returns>True if workflow rules should be added</returns>
    private static bool ShouldAddWorkflowRules(PipelineOptions options)
    {
        // Add workflow rules for deployment pipelines or when specific conditions are needed
        return options.IncludeDeployment || 
               options.DeploymentEnvironments.Any() ||
               options.ProjectType.Equals("production", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Builds workflow rules for the pipeline
    /// </summary>
    /// <param name="options">Pipeline options</param>
    /// <returns>Workflow rules configuration</returns>
    private static WorkflowRules BuildWorkflowRules(PipelineOptions options)
    {

        var rules = new List<Rule>();

        // Add rule to prevent duplicate pipelines
        rules.Add(new Rule
        {
            If = "$CI_PIPELINE_SOURCE == \"merge_request_event\"",
            When = "never"
        });

        // Add rule to run on main branch
        rules.Add(new Rule
        {
            If = "$CI_COMMIT_BRANCH == $CI_DEFAULT_BRANCH",
            When = "always"
        });

        // Add rule to run on tags for deployment
        if (options.IncludeDeployment)
        {
            rules.Add(new Rule
            {
                If = "$CI_COMMIT_TAG",
                When = "always"
            });
        }

        // Default rule
        rules.Add(new Rule
        {
            When = "manual",
            AllowFailure = true
        });

        return new WorkflowRules { Rules = rules };
    }
}