using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Services;
using GitlabPipelineGenerator.Core.Models.GitLab;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

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

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
