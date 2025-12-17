using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using GitlabPipelineGenerator.CLI.Services;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Services;
using GitlabPipelineGenerator.Core.Builders;
using GitlabPipelineGenerator.Core.Templates;
using GitlabPipelineGenerator.Core.Configuration;

namespace GitlabPipelineGenerator.CLI.Extensions;

/// <summary>
/// Extension methods for service collection registration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all GitLab Pipeline Generator core services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddGitLabPipelineGenerator(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // Register configuration options
        if (configuration != null)
        {
            services.Configure<PipelineGeneratorOptions>(configuration.GetSection(PipelineGeneratorOptions.SectionName));
        }
        else
        {
            services.Configure<PipelineGeneratorOptions>(_ => { });
        }

        // Register core pipeline services
        services.AddTransient<IPipelineGenerator, PipelineGenerator>();
        services.AddTransient<IStageBuilder, StageBuilder>();
        services.AddTransient<IJobBuilder, JobBuilder>();
        services.AddTransient<IVariableBuilder, VariableBuilder>();
        services.AddTransient<YamlSerializationService>();
        services.AddTransient<ValidationService>();

        // Register template services
        services.AddTransient<ITemplateCustomizationService, TemplateCustomizationService>();
        services.AddTransient<DotNetProjectTemplate>();
        services.AddTransient<PythonProjectTemplate>();
        services.AddTransient<JavaScriptProjectTemplate>();

        // Register template service with factory
        services.AddSingleton<IPipelineTemplateService>(provider =>
        {
            var templateService = new PipelineTemplateService();
            templateService.RegisterTemplate(provider.GetRequiredService<DotNetProjectTemplate>());
            templateService.RegisterTemplate(provider.GetRequiredService<PythonProjectTemplate>());
            templateService.RegisterTemplate(provider.GetRequiredService<JavaScriptProjectTemplate>());
            return templateService;
        });

        // Register GitLab API services
        services.AddTransient<IGitLabAuthenticationService, GitLabAuthenticationService>();
        services.AddTransient<IGitLabProjectService, GitLabProjectService>();
        services.AddTransient<IProjectAnalysisService, ProjectAnalysisService>();
        services.AddTransient<IFilePatternAnalyzer, FilePatternAnalyzer>();
        services.AddTransient<IDependencyAnalyzer, DependencyAnalyzer>();
        services.AddTransient<IConfigurationAnalyzer, ConfigurationAnalyzer>();
        services.AddTransient<IAnalysisToPipelineMappingService, AnalysisToPipelineMappingService>();
        services.AddTransient<IntelligentPipelineGenerator>();

        // Register resilience services
        services.AddSingleton<GitLabApiErrorHandler>();
        services.AddSingleton<CircuitBreaker>();
        services.AddTransient<ResilientGitLabService>();
        services.AddTransient<IGitLabFallbackService, GitLabFallbackService>();
        services.AddTransient<DegradedAnalysisService>();

        // Register configuration services
        services.AddSingleton<ICredentialStorageService, CrossPlatformCredentialStorageService>();
        services.AddTransient<IConfigurationProfileService, ConfigurationProfileService>();
        services.AddTransient<IConfigurationManagementService, ConfigurationManagementService>();

        // Register validation services
        services.AddTransient<GitLabConnectionValidator>();
        services.AddTransient<IGitLabPermissionValidator, GitLabPermissionValidator>();
        services.AddTransient<IGitLabApiErrorHandler, GitLabApiErrorHandler>();

        // Register enhanced services
        services.AddTransient<EnhancedPipelineGenerator>();

        return services;
    }

    /// <summary>
    /// Registers CLI-specific services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddCliServices(this IServiceCollection services)
    {
        services.AddTransient<OutputFormatter>();
        services.AddTransient<VerboseOutputService>();
        services.AddTransient<UserFriendlyErrorService>();
        services.AddTransient<ProgressIndicatorService>();

        return services;
    }
}