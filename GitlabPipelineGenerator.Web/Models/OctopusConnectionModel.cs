using System.ComponentModel.DataAnnotations;

namespace GitlabPipelineGenerator.Web.Models;

public class OctopusConnectionModel
{
    [Required]
    [Display(Name = "Octopus Server URL")]
    public string ServerUrl { get; set; } = string.Empty;

    [Required]
    [Display(Name = "API Key")]
    [DataType(DataType.Password)]
    public string ApiKey { get; set; } = string.Empty;
}