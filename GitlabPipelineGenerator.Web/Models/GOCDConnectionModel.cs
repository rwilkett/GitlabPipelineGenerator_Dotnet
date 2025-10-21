using System.ComponentModel.DataAnnotations;

namespace GitlabPipelineGenerator.Web.Models;

public class GOCDConnectionModel
{
    [Required]
    [Display(Name = "GOCD Server URL")]
    public string ServerUrl { get; set; } = string.Empty;

    [Display(Name = "Authentication Method")]
    public string AuthMethod { get; set; } = "token";

    [Display(Name = "Bearer Token")]
    public string? BearerToken { get; set; }

    [Display(Name = "Username")]
    public string? Username { get; set; }

    [Display(Name = "Password")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }
}

public class PipelineConfigModel
{
    public string Name { get; set; } = string.Empty;
    public string? Template { get; set; }
    public List<EnvironmentVariableModel> EnvironmentVariables { get; set; } = new();
    public List<MaterialModel> Materials { get; set; } = new();
    public List<StageModel> Stages { get; set; } = new();
}

public class EnvironmentVariableModel
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Secure { get; set; }
}

public class MaterialModel
{
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Branch { get; set; }
}

public class StageModel
{
    public string Name { get; set; } = string.Empty;
    public List<JobModel> Jobs { get; set; } = new();
}

public class JobModel
{
    public string Name { get; set; } = string.Empty;
    public List<TaskModel> Tasks { get; set; } = new();
}

public class TaskModel
{
    public string Type { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
}