using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models;

namespace GitlabPipelineGenerator.Core.Templates;

/// <summary>
/// Pipeline template for JavaScript/Node.js projects
/// </summary>
public class JavaScriptProjectTemplate : BasePipelineTemplate
{
    public JavaScriptProjectTemplate(
        IStageBuilder stageBuilder,
        IJobBuilder jobBuilder,
        IVariableBuilder variableBuilder)
        : base(stageBuilder, jobBuilder, variableBuilder)
    {
    }

    public override string Name => "nodejs-standard";
    public override string Description => "Standard Node.js project pipeline with npm/yarn, testing, and deployment";
    public override IEnumerable<string> SupportedProjectTypes => new[] { "nodejs", "javascript" };

    public override PipelineOptions GetDefaultOptions(string projectType)
    {
        return new PipelineOptions
        {
            ProjectType = "nodejs",
            Stages = new List<string> { "build", "test", "quality", "deploy" },
            IncludeTests = true,
            IncludeDeployment = true,
            IncludeCodeQuality = true,
            DockerImage = "node:18-alpine",
            CustomVariables = new Dictionary<string, string>
            {
                ["NPM_CONFIG_CACHE"] = "$CI_PROJECT_DIR/.npm",
                ["CYPRESS_CACHE_FOLDER"] = "$CI_PROJECT_DIR/cache/Cypress"
            },
            Cache = new CacheOptions
            {
                Key = "$CI_COMMIT_REF_SLUG-nodejs",
                Paths = new List<string> { "node_modules/", ".npm/", "cache/Cypress/" },
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
            DockerImage = options.DockerImage ?? "node:18-alpine",
            CustomVariables = new Dictionary<string, string>(options.CustomVariables)
        };

        var nodeVariables = new Dictionary<string, string>
        {
            ["NPM_CONFIG_CACHE"] = "$CI_PROJECT_DIR/.npm",
            ["CYPRESS_CACHE_FOLDER"] = "$CI_PROJECT_DIR/cache/Cypress"
        };

        foreach (var variable in nodeVariables)
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
            errors.Add("Build stage is required for Node.js projects");
        }
        
        return errors;
    }
}