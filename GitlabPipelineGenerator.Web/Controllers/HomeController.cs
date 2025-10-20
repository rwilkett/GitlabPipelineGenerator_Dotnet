using Microsoft.AspNetCore.Mvc;
using GitlabPipelineGenerator.Web.Models;
using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Services;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.GitLabApiClient;

namespace GitlabPipelineGenerator.Web.Controllers;

public class HomeController : Controller
{
    private readonly IGitLabAuthenticationService _authService;
    private readonly IGitLabProjectService _projectService;
    private readonly IProjectAnalysisService _analysisService;

    public HomeController(
        IGitLabAuthenticationService authService,
        IGitLabProjectService projectService,
        IProjectAnalysisService analysisService)
    {
        _authService = authService;
        _projectService = projectService;
        _analysisService = analysisService;
    }

    public IActionResult Index()
    {
        return View(new GitLabConnectionModel());
    }

    [HttpPost]
    public async Task<IActionResult> Connect(GitLabConnectionModel model)
    {
        try
        {
            var connectionOptions = new GitLabConnectionOptions
            {
                PersonalAccessToken = model.Token,
                InstanceUrl = "https://gitlab.com"
            };

            var client = await _authService.AuthenticateAsync(connectionOptions);
            _projectService.SetAuthenticatedClient(client);
            _analysisService.SetAuthenticatedClient(client);

            // Get group info to validate it exists
            var group = await client.GetGroupAsync(model.GroupId);
            
            // Get projects directly from the group
            var groupProjects = await client.GetGroupProjectsAsync(model.GroupId, perPage: 100);

            var projectListModel = new ProjectListModel
            {
                Token = model.Token,
                GroupId = model.GroupId,
                GroupName = group.Name,
                Projects = groupProjects.Select(p => new ProjectItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    FullPath = p.PathWithNamespace,
                    Description = p.Description ?? "",
                    WebUrl = p.WebUrl
                }).ToList()
            };

            return View("Projects", projectListModel);
        }
        catch (Exception ex)
        {
            model.IsConnected = false;
            ViewBag.Error = $"Connection failed: {ex.Message}";
            return View("Index", model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Analyze(string token, string projectPath)
    {
        try
        {
            var connectionOptions = new GitLabConnectionOptions
            {
                PersonalAccessToken = token,
                InstanceUrl = "https://gitlab.com"
            };

            var client = await _authService.AuthenticateAsync(connectionOptions);
            _projectService.SetAuthenticatedClient(client);
            _analysisService.SetAuthenticatedClient(client);

            var project = await _projectService.GetProjectAsync(projectPath);
            var analysisOptions = new AnalysisOptions
            {
                MaxFileAnalysisDepth = 3,
                AnalyzeFiles = true,
                AnalyzeDependencies = true
            };

            var analysisResult = await _analysisService.AnalyzeProjectAsync(project, analysisOptions);

            var yamlContent = GenerateBasicYaml(analysisResult);

            var resultModel = new AnalysisResultModel
            {
                ProjectName = project.Name,
                DetectedType = analysisResult.DetectedType.ToString(),
                Framework = analysisResult.Framework.Name,
                BuildTool = analysisResult.BuildConfig.BuildTool,
                Version = analysisResult.BuildConfig.Settings.GetValueOrDefault("DotNetVersion", ""),
                BuildCommands = analysisResult.BuildConfig.BuildCommands,
                TestCommands = analysisResult.BuildConfig.TestCommands,
                YamlContent = yamlContent,
                Success = true
            };

            return View("Analysis", resultModel);
        }
        catch (Exception ex)
        {
            var errorModel = new AnalysisResultModel
            {
                Success = false,
                ErrorMessage = ex.Message
            };
            return View("Analysis", errorModel);
        }
    }

    private string GenerateBasicYaml(ProjectAnalysisResult analysis)
    {
        var dockerImage = analysis.BuildConfig.Settings.GetValueOrDefault("DockerImage", "mcr.microsoft.com/dotnet/sdk:8.0");
        
        return $@"stages:
  - build
  - test

variables:
  DOTNET_VERSION: ""{analysis.BuildConfig.Settings.GetValueOrDefault("DotNetVersion", "8.0")}""

build:
  stage: build
  image: {dockerImage}
  script:
{string.Join("\n", analysis.BuildConfig.BuildCommands.Select(cmd => $"    - {cmd}"))}
  artifacts:
    paths:
{string.Join("\n", analysis.BuildConfig.ArtifactPaths.Select(path => $"      - {path}"))}

test:
  stage: test
  image: {dockerImage}
  script:
{string.Join("\n", analysis.BuildConfig.TestCommands.Select(cmd => $"    - {cmd}"))}
  dependencies:
    - build";
    }
}