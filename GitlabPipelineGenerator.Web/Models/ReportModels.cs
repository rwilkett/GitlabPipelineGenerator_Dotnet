namespace GitlabPipelineGenerator.Web.Models;

public class VariablesReportModel
{
    public string GroupName { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public int TopLevelVariableCount { get; set; }
    public int TotalVariableCount { get; set; }
    public List<HierarchicalGroupSummary> Groups { get; set; } = new();
}

public class HierarchicalGroupSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public int VariableCount { get; set; }
    public int TotalVariableCount { get; set; }
    public List<HierarchicalGroupSummary> Subgroups { get; set; } = new();
    public List<ProjectVariablesSummary> Projects { get; set; } = new();
    public bool IsExpanded { get; set; } = false;
}

public class GroupVariablesSummary
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public int VariableCount { get; set; }
}

public class ProjectVariablesSummary
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public int VariableCount { get; set; }
}

public class PipelineRunsReportModel
{
    public string GroupName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<GroupPipelineRuns> Groups { get; set; } = new();
}

public class GroupPipelineRuns
{
    public string GroupName { get; set; } = string.Empty;
    public string GroupPath { get; set; } = string.Empty;
    public List<ProjectPipelineRuns> Projects { get; set; } = new();
}

public class ProjectPipelineRuns
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public int PipelineCount { get; set; }
}