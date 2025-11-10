using GitlabPipelineGenerator.Web.Models;

namespace GitlabPipelineGenerator.Web.Services;

public interface IComparisonService
{
    Task<ResourceDataModel> LoadResourceDataAsync(GitlabPipelineGenerator.GitLabApiClient.GitLabClient client, string resourceId, string resourceType);
    Task<ResourceDataModel> LoadSourceSubgroupDataAsync(GitlabPipelineGenerator.GitLabApiClient.GitLabClient sourceClient, string sourceGroupId);
    Task<ResourceDataModel> CompareSubgroupsWithTopLevelGroupsAsync(GitlabPipelineGenerator.GitLabApiClient.GitLabClient sourceClient, string sourceGroupId, GitlabPipelineGenerator.GitLabApiClient.GitLabClient targetClient);
    List<VariableDifferenceModel> CompareVariables(Dictionary<string, string> sourceVars, Dictionary<string, string> targetVars);
}

public class ComparisonService : IComparisonService
{
    public async Task<ResourceDataModel> LoadResourceDataAsync(GitlabPipelineGenerator.GitLabApiClient.GitLabClient client, string resourceId, string resourceType)
    {
        if (resourceType == "group")
        {
            var group = await client.GetGroupAsync(resourceId);
            var variables = await client.GetGroupVariablesAsync(resourceId);
            return new ResourceDataModel
            {
                Name = group.Name,
                WebUrl = group.WebUrl,
                Variables = variables.ToDictionary(v => v.Key, v => v.Value ?? "")
            };
        }
        else
        {
            var project = await client.GetProjectAsync(resourceId);
            var variables = await client.GetProjectVariablesAsync(resourceId);
            return new ResourceDataModel
            {
                Name = project.Name,
                WebUrl = project.WebUrl,
                Variables = variables.ToDictionary(v => v.Key, v => v.Value ?? "")
            };
        }
    }

    public async Task<ResourceDataModel> LoadSourceSubgroupDataAsync(GitlabPipelineGenerator.GitLabApiClient.GitLabClient sourceClient, string sourceGroupId)
    {
        var sourceGroup = await sourceClient.GetGroupAsync(sourceGroupId);
        var allSubgroups = new List<GitlabPipelineGenerator.GitLabApiClient.Models.Group>();
        
        // Paginate through all subgroups
        int page = 1;
        List<GitlabPipelineGenerator.GitLabApiClient.Models.Group> pageSubgroups;
        do
        {
            pageSubgroups = await sourceClient.GetSubgroupsAsync(sourceGroupId, 100, page);
            allSubgroups.AddRange(pageSubgroups);
            page++;
        } while (pageSubgroups.Count == 100);
        
        var combinedVariables = new Dictionary<string, string>();
        
        foreach (var subgroup in allSubgroups)
        {
            try
            {
                var variables = await sourceClient.GetGroupVariablesAsync(subgroup.Id.ToString());
                foreach (var variable in variables)
                {
                    combinedVariables[$"{subgroup.Name}:{variable.Key}"] = variable.Value ?? "";
                }
            }
            catch { /* Ignore variable fetch errors */ }
            
            // Always include subgroup even if it has no variables
            if (!combinedVariables.Keys.Any(k => k.StartsWith($"{subgroup.Name}:")))
            {
                combinedVariables[$"{subgroup.Name}:(no variables)"] = "";
            }
        }
        
        return new ResourceDataModel
        {
            Name = $"{sourceGroup.Name} Subgroups",
            WebUrl = sourceGroup.WebUrl,
            Variables = combinedVariables
        };
    }

    public async Task<ResourceDataModel> CompareSubgroupsWithTopLevelGroupsAsync(GitlabPipelineGenerator.GitLabApiClient.GitLabClient sourceClient, string sourceGroupId, GitlabPipelineGenerator.GitLabApiClient.GitLabClient targetClient)
    {
        // Get all source subgroups with pagination
        var allSourceSubgroups = new List<GitlabPipelineGenerator.GitLabApiClient.Models.Group>();
        int sourcePage = 1;
        List<GitlabPipelineGenerator.GitLabApiClient.Models.Group> pageSourceSubgroups;
        do
        {
            pageSourceSubgroups = await sourceClient.GetSubgroupsAsync(sourceGroupId, 100, sourcePage);
            allSourceSubgroups.AddRange(pageSourceSubgroups);
            sourcePage++;
        } while (pageSourceSubgroups.Count == 100);
        
        // Get all target top-level groups with pagination
        var allTargetGroups = new List<GitlabPipelineGenerator.GitLabApiClient.Models.Group>();
        int targetPage = 1;
        List<GitlabPipelineGenerator.GitLabApiClient.Models.Group> pageTargetGroups;
        do
        {
            pageTargetGroups = await targetClient.GetTopLevelGroupsAsync(100);
            allTargetGroups.AddRange(pageTargetGroups);
            targetPage++;
        } while (pageTargetGroups.Count == 100);
        
        var combinedVariables = new Dictionary<string, string>();
        
        // Add all target groups by name, even if they have no variables
        foreach (var targetGroup in allTargetGroups)
        {
            try
            {
                var variables = await targetClient.GetGroupVariablesAsync(targetGroup.Id.ToString());
                if (variables.Any())
                {
                    foreach (var variable in variables)
                    {
                        combinedVariables[$"{targetGroup.Name}:{variable.Key}"] = variable.Value ?? "";
                    }
                }
                else
                {
                    // Add group even if it has no variables
                    combinedVariables[$"{targetGroup.Name}:(no variables)"] = "";
                }
            }
            catch 
            { 
                // Add group even if variable fetch fails
                combinedVariables[$"{targetGroup.Name}:(no variables)"] = "";
            }
        }
        
        return new ResourceDataModel
        {
            Name = "Top-level Groups",
            WebUrl = "",
            Variables = combinedVariables
        };
    }

    public List<VariableDifferenceModel> CompareVariables(Dictionary<string, string> sourceVars, Dictionary<string, string> targetVars)
    {
        var differences = new List<VariableDifferenceModel>();
        
        // Check if we're comparing group:variable format
        var isGroupComparison = sourceVars.Keys.Any(k => k.Contains(":")) || targetVars.Keys.Any(k => k.Contains(":"));
        
        if (isGroupComparison)
        {
            // Group all variables by group name
            var sourceGroups = sourceVars.Keys.Where(k => k.Contains(":")).Select(k => k.Split(':')[0]).Distinct();
            var targetGroups = targetVars.Keys.Where(k => k.Contains(":")).Select(k => k.Split(':')[0]).Distinct();
            var allGroups = sourceGroups.Union(targetGroups).Distinct();
            
            foreach (var groupName in allGroups)
            {
                var sourceGroupVars = sourceVars.Where(kv => kv.Key.StartsWith($"{groupName}:")).ToDictionary(kv => kv.Key, kv => kv.Value);
                var targetGroupVars = targetVars.Where(kv => kv.Key.StartsWith($"{groupName}:")).ToDictionary(kv => kv.Key, kv => kv.Value);
                
                var sourceExists = sourceGroupVars.Any();
                var targetExists = targetGroupVars.Any();
                
                string status;
                if (sourceExists && targetExists)
                {
                    // Compare all variables in the group
                    var allVarsMatch = sourceGroupVars.All(sv => targetGroupVars.ContainsKey(sv.Key) && targetGroupVars[sv.Key] == sv.Value) &&
                                      targetGroupVars.All(tv => sourceGroupVars.ContainsKey(tv.Key));
                    status = allVarsMatch ? "Same" : "Different";
                }
                else if (sourceExists)
                {
                    status = "Source Only";
                }
                else
                {
                    status = "Target Only";
                }
                
                // Add one entry per group with a representative variable key
                var representativeKey = sourceExists ? sourceGroupVars.Keys.First() : targetGroupVars.Keys.First();
                differences.Add(new VariableDifferenceModel
                {
                    Key = representativeKey,
                    SourceExists = sourceExists,
                    TargetExists = targetExists,
                    Status = status
                });
            }
        }
        else
        {
            // Original variable comparison logic
            var allKeys = sourceVars.Keys.Union(targetVars.Keys).Distinct();
            
            foreach (var key in allKeys)
            {
                var sourceExists = sourceVars.ContainsKey(key);
                var targetExists = targetVars.ContainsKey(key);
                
                string status;
                if (sourceExists && targetExists)
                {
                    status = sourceVars[key] == targetVars[key] ? "Same" : "Different";
                }
                else if (sourceExists)
                {
                    status = "Source Only";
                }
                else
                {
                    status = "Target Only";
                }

                differences.Add(new VariableDifferenceModel
                {
                    Key = key,
                    SourceExists = sourceExists,
                    TargetExists = targetExists,
                    Status = status
                });
            }
        }

        return differences.OrderBy(d => d.Key).ToList();
    }
}

public class ResourceDataModel
{
    public string Name { get; set; } = string.Empty;
    public string WebUrl { get; set; } = string.Empty;
    public Dictionary<string, string> Variables { get; set; } = new();
}

public class VariableDifferenceModel
{
    public string Key { get; set; } = string.Empty;
    public bool SourceExists { get; set; }
    public bool TargetExists { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ComparisonResultModel
{
    public string SourceName { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public int SourceVariableCount { get; set; }
    public int TargetVariableCount { get; set; }
    public List<VariableDifferenceModel> VariableDifferences { get; set; } = new();
}