using GitlabPipelineGenerator.Core.Models;
using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Tests.Utilities;

/// <summary>
/// Builder pattern for creating test data objects
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Creates a pipeline options builder
    /// </summary>
    /// <returns>Pipeline options builder</returns>
    public static PipelineOptionsBuilder PipelineOptions() => new();

    /// <summary>
    /// Creates a GitLab project builder
    /// </summary>
    /// <returns>GitLab project builder</returns>
    public static GitLabProjectBuilder GitLabProject() => new();

    /// <summary>
    /// Creates a project analysis result builder
    /// </summary>
    /// <returns>Project analysis result builder</returns>
    public static ProjectAnalysisResultBuilder ProjectAnalysisResult() => new();
}

/// <summary>
/// Builder for PipelineOptions test data
/// </summary>
public sealed class PipelineOptionsBuilder
{
    private readonly PipelineOptions _options = new();

    public PipelineOptionsBuilder WithProjectType(string projectType)
    {
        _options.ProjectType = projectType;
        return this;
    }

    public PipelineOptionsBuilder WithStages(params string[] stages)
    {
        _options.Stages = stages.ToList();
        return this;
    }

    public PipelineOptionsBuilder WithDotNetVersion(string version)
    {
        _options.DotNetVersion = version;
        return this;
    }

    public PipelineOptionsBuilder WithTests(bool includeTests = true)
    {
        _options.IncludeTests = includeTests;
        return this;
    }

    public PipelineOptionsBuilder WithDeployment(bool includeDeployment = true)
    {
        _options.IncludeDeployment = includeDeployment;
        return this;
    }

    public PipelineOptionsBuilder WithCodeQuality(bool includeCodeQuality = true)
    {
        _options.IncludeCodeQuality = includeCodeQuality;
        return this;
    }

    public PipelineOptionsBuilder WithSecurity(bool includeSecurity = true)
    {
        _options.IncludeSecurity = includeSecurity;
        return this;
    }

    public PipelineOptionsBuilder WithVariable(string key, string value)
    {
        _options.CustomVariables ??= new Dictionary<string, string>();
        _options.CustomVariables[key] = value;
        return this;
    }

    public PipelineOptions Build() => _options;
}

/// <summary>
/// Builder for GitLabProject test data
/// </summary>
public sealed class GitLabProjectBuilder
{
    private readonly GitLabProject _project = new();

    public GitLabProjectBuilder WithId(int id)
    {
        _project.Id = id;
        return this;
    }

    public GitLabProjectBuilder WithName(string name)
    {
        _project.Name = name;
        return this;
    }

    public GitLabProjectBuilder WithPath(string path)
    {
        _project.Path = path;
        return this;
    }

    public GitLabProjectBuilder WithFullPath(string fullPath)
    {
        _project.FullPath = fullPath;
        return this;
    }

    public GitLabProjectBuilder WithWebUrl(string webUrl)
    {
        _project.WebUrl = webUrl;
        return this;
    }

    public GitLabProjectBuilder WithVisibility(ProjectVisibility visibility)
    {
        _project.Visibility = visibility;
        return this;
    }

    public GitLabProjectBuilder WithDescription(string description)
    {
        _project.Description = description;
        return this;
    }

    public GitLabProject Build() => _project;
}

/// <summary>
/// Builder for ProjectAnalysisResult test data
/// </summary>
public sealed class ProjectAnalysisResultBuilder
{
    private readonly ProjectAnalysisResult _result = new();

    public ProjectAnalysisResultBuilder WithDetectedType(ProjectType projectType)
    {
        _result.DetectedType = projectType;
        return this;
    }

    public ProjectAnalysisResultBuilder WithFramework(string name, string version)
    {
        _result.Framework = new FrameworkInfo
        {
            Name = name,
            Version = version,
            DetectedFeatures = new List<string>()
        };
        return this;
    }

    public ProjectAnalysisResultBuilder WithConfidence(double confidence)
    {
        _result.Confidence = confidence;
        return this;
    }

    public ProjectAnalysisResultBuilder WithBuildTool(string buildTool)
    {
        _result.BuildConfig = new BuildConfiguration
        {
            BuildTool = buildTool,
            BuildCommands = new List<string>(),
            TestCommands = new List<string>()
        };
        return this;
    }

    public ProjectAnalysisResultBuilder WithDependency(string name, string version)
    {
        _result.Dependencies ??= new DependencyInfo();
        _result.Dependencies.Dependencies ??= new List<PackageDependency>();
        _result.Dependencies.Dependencies.Add(new PackageDependency
        {
            Name = name,
            Version = version,
            Type = "package"
        });
        return this;
    }

    public ProjectAnalysisResult Build() => _result;
}