using GitlabPipelineGenerator.Core.Models;
using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Interfaces;

/// <summary>
/// Interface for intelligent pipeline generator that uses project analysis results to generate optimized pipelines
/// </summary>
public interface IIntelligentPipelineGenerator : IPipelineGenerator
{
    /// <summary>
    /// Generates a pipeline using analysis results with manual options
    /// </summary>
    /// <param name="analysisResult">Project analysis result</param>
    /// <param name="manualOptions">Manual pipeline options (optional)</param>
    /// <param name="mergeStrategy">Strategy for merging analysis and manual options</param>
    /// <returns>Generated pipeline configuration</returns>
    Task<PipelineConfiguration> GenerateFromAnalysisAsync(
        ProjectAnalysisResult analysisResult,
        PipelineOptions? manualOptions = null,
        ConfigurationMergeStrategy mergeStrategy = ConfigurationMergeStrategy.PreferManual);

    /// <summary>
    /// Serializes a pipeline configuration to YAML format
    /// </summary>
    /// <param name="pipeline">Pipeline configuration to serialize</param>
    /// <returns>YAML representation of the pipeline</returns>
    string SerializeToYaml(PipelineConfiguration pipeline);
}