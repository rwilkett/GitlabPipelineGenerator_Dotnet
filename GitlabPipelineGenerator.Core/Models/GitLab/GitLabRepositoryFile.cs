namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Represents a file in a GitLab repository
/// </summary>
public class GitLabRepositoryFile
{
    /// <summary>
    /// File name including extension
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Full path of the file in the repository
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// File type (blob, tree, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// File content (if retrieved)
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// File extension (derived from name)
    /// </summary>
    public string Extension => System.IO.Path.GetExtension(Name).ToLowerInvariant();

    /// <summary>
    /// Directory path of the file
    /// </summary>
    public string Directory => System.IO.Path.GetDirectoryName(Path) ?? string.Empty;

    /// <summary>
    /// Whether this is a root-level file
    /// </summary>
    public bool IsRootFile => string.IsNullOrEmpty(Directory) || Directory == ".";
}