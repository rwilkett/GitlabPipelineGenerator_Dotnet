using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Services;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Builders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Configure GitLab settings
builder.Services.Configure<GitLabApiSettings>(options =>
{
    options.DefaultInstanceUrl = "https://gitlab.com";
    options.ApiVersion = "v4";
    options.RequestTimeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<GitLabApiSettings>(provider => 
    provider.GetRequiredService<IOptions<GitLabApiSettings>>().Value);

// Register Core services
builder.Services.AddTransient<IGitLabAuthenticationService, GitLabAuthenticationService>();
builder.Services.AddTransient<IGitLabProjectService, GitLabProjectService>();
builder.Services.AddTransient<IProjectAnalysisService, ProjectAnalysisService>();
builder.Services.AddTransient<IFilePatternAnalyzer, FilePatternAnalyzer>();
builder.Services.AddTransient<IDependencyAnalyzer, DependencyAnalyzer>();
builder.Services.AddTransient<IConfigurationAnalyzer, ConfigurationAnalyzer>();
builder.Services.AddTransient<IGitLabApiErrorHandler, GitLabApiErrorHandler>();
builder.Services.AddTransient<ICredentialStorageService, CrossPlatformCredentialStorageService>();
builder.Services.AddTransient<IAnalysisToPipelineMappingService, AnalysisToPipelineMappingService>();
builder.Services.AddTransient<IPipelineGenerator, PipelineGenerator>();
builder.Services.AddTransient<YamlSerializationService>();
builder.Services.AddTransient<IIntelligentPipelineGenerator, IntelligentPipelineGenerator>();
builder.Services.AddTransient<IStageBuilder, StageBuilder>();
builder.Services.AddTransient<IJobBuilder, JobBuilder>();
builder.Services.AddTransient<IVariableBuilder, VariableBuilder>();

// Register pipeline templates
builder.Services.AddTransient<GitlabPipelineGenerator.Core.Templates.DotNetProjectTemplate>();
builder.Services.AddTransient<GitlabPipelineGenerator.Core.Templates.PythonProjectTemplate>();
builder.Services.AddTransient<GitlabPipelineGenerator.Core.Templates.JavaScriptProjectTemplate>();

// Configure template service with all templates
builder.Services.AddSingleton<IPipelineTemplateService>(provider =>
{
    var templateService = new PipelineTemplateService();
    templateService.RegisterTemplate(provider.GetRequiredService<GitlabPipelineGenerator.Core.Templates.DotNetProjectTemplate>());
    templateService.RegisterTemplate(provider.GetRequiredService<GitlabPipelineGenerator.Core.Templates.PythonProjectTemplate>());
    templateService.RegisterTemplate(provider.GetRequiredService<GitlabPipelineGenerator.Core.Templates.JavaScriptProjectTemplate>());
    return templateService;
});

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
