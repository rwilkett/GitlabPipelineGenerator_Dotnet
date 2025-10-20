using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Templates;

/// <summary>
/// Pipeline template for Python projects
/// </summary>
public class PythonProjectTemplate : BasePipelineTemplate
{
    public PythonProjectTemplate(
        IStageBuilder stageBuilder,
        IJobBuilder jobBuilder,
        IVariableBuilder variableBuilder)
        : base(stageBuilder, jobBuilder, variableBuilder)
    {
    }

    public override string Name => "python-standard";
    public override string Description => "Standard Python project pipeline with pip, pytest, and deployment";
    public override IEnumerable<string> SupportedProjectTypes => new[] { "python" };

    public override PipelineOptions GetDefaultOptions(string projectType)
    {
        return new PipelineOptions
        {
            ProjectType = "python",
            Stages = new List<string> { "build", "test", "quality", "deploy" },
            IncludeTests = true,
            IncludeDeployment = true,
            IncludeCodeQuality = true,
            DockerImage = "python:3.11-slim",
            CustomVariables = new Dictionary<string, string>
            {
                ["PIP_CACHE_DIR"] = "$CI_PROJECT_DIR/.cache/pip",
                ["PYTHONPATH"] = "$CI_PROJECT_DIR"
            },
            Cache = new CacheOptions
            {
                Key = "$CI_COMMIT_REF_SLUG-python",
                Paths = new List<string> { ".cache/pip/", "venv/" },
                Policy = "pull-push"
            }
        };
    }

    protected override PipelineOptions ApplyTemplateCustomizations(PipelineOptions options)
    {
        var customized = new PipelineOptions
        {
            ProjectType = options.ProjectType,
            Stages = new List<string>(options.Stages),
            IncludeTests = options.IncludeTests,
            IncludeDeployment = options.IncludeDeployment,
            IncludeCodeQuality = options.IncludeCodeQuality,
            DockerImage = options.DockerImage ?? "python:3.11-slim",
            CustomVariables = new Dictionary<string, string>(options.CustomVariables)
        };

        var pythonVariables = new Dictionary<string, string>
        {
            ["PIP_CACHE_DIR"] = "$CI_PROJECT_DIR/.cache/pip",
            ["PYTHONPATH"] = "$CI_PROJECT_DIR"
        };

        foreach (var variable in pythonVariables)
        {
            if (!customized.CustomVariables.ContainsKey(variable.Key))
            {
                customized.CustomVariables[variable.Key] = variable.Value;
            }
        }

        return customized;
    }

    protected override async Task ApplyTemplatePipelineModificationsAsync(PipelineConfiguration pipeline, PipelineOptions options)
    {
        await Task.CompletedTask;
    }

    protected override List<string> ValidateTemplateSpecificOptions(PipelineOptions options)
    {
        var errors = new List<string>();
        
        if (!options.Stages.Contains("build", StringComparer.OrdinalIgnoreCase))
        {
            errors.Add("Build stage is required for Python projects");
        }
        
        return errors;
    }
}