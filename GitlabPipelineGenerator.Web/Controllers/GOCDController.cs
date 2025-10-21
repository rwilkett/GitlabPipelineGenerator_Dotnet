using Microsoft.AspNetCore.Mvc;
using GitlabPipelineGenerator.Web.Models;

using GitlabPipelineGenerator.GOCDApiClient.Models;

namespace GitlabPipelineGenerator.Web.Controllers;

public class GOCDController : Controller
{
    public IActionResult Index()
    {
        return View(new GOCDConnectionModel());
    }

    [HttpPost]
    public async Task<IActionResult> Connect(GOCDConnectionModel model)
    {
        if (!ModelState.IsValid)
            return View("Index", model);

        try
        {
            using var client = model.AuthMethod == "token" 
                ? new GitlabPipelineGenerator.GOCDApiClient.GOCDApiClient(model.ServerUrl, model.BearerToken!)
                : new GitlabPipelineGenerator.GOCDApiClient.GOCDApiClient(model.ServerUrl, model.Username!, model.Password!);

            var groups = await client.GetPipelineGroupsAsync();
            
            TempData["ServerUrl"] = model.ServerUrl;
            TempData["AuthMethod"] = model.AuthMethod;
            TempData["BearerToken"] = model.BearerToken;
            TempData["Username"] = model.Username;
            TempData["Password"] = model.Password;

            return View("Groups", groups);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Connection failed: {ex.Message}");
            return View("Index", model);
        }
    }

    public async Task<IActionResult> Group(string groupName)
    {
        try
        {
            using var client = CreateClient();
            var group = await client.GetPipelineGroupAsync(groupName);
            
            if (group == null)
                return NotFound();

            return View(group);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error loading group: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    public async Task<IActionResult> Pipeline(string pipelineName)
    {
        try
        {
            using var client = CreateClient();
            var pipelineConfig = await client.GetPipelineConfigAsync(pipelineName);
            
            if (pipelineConfig == null)
                return NotFound();

            var config = new PipelineConfigModel
            {
                Name = pipelineConfig.Name,
                Template = pipelineConfig.Template,
                EnvironmentVariables = pipelineConfig.EnvironmentVariables.Select(e => new EnvironmentVariableModel
                {
                    Name = e.Name,
                    Value = e.Value,
                    Secure = e.Secure
                }).ToList(),
                Materials = pipelineConfig.Materials.Select(m => new MaterialModel
                {
                    Type = m.Type,
                    Url = m.Attributes.Url ?? "",
                    Branch = m.Attributes.Branch
                }).ToList(),
                Stages = pipelineConfig.Stages.Select(s => new StageModel
                {
                    Name = s.Name,
                    Jobs = s.Jobs.Select(j => new JobModel
                    {
                        Name = j.Name,
                        Tasks = j.Tasks.Select(t => new TaskModel
                        {
                            Type = t.Type,
                            Command = t.Attributes.ContainsKey("command") ? t.Attributes["command"].ToString() ?? "" : ""
                        }).ToList()
                    }).ToList()
                }).ToList()
            };

            return View(config);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error loading pipeline: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    private GitlabPipelineGenerator.GOCDApiClient.GOCDApiClient CreateClient()
    {
        var serverUrl = TempData.Peek("ServerUrl")?.ToString() ?? "";
        var authMethod = TempData.Peek("AuthMethod")?.ToString() ?? "token";
        
        return authMethod == "token"
            ? new GitlabPipelineGenerator.GOCDApiClient.GOCDApiClient(serverUrl, TempData.Peek("BearerToken")?.ToString() ?? "")
            : new GitlabPipelineGenerator.GOCDApiClient.GOCDApiClient(serverUrl, TempData.Peek("Username")?.ToString() ?? "", TempData.Peek("Password")?.ToString() ?? "");
    }
}