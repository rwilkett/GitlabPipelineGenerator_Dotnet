using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Exceptions;

/// <summary>
/// Exception thrown when user has insufficient permissions for a GitLab operation
/// </summary>
public class InsufficientPermissionsException : GitLabApiException
{
    /// <summary>
    /// Project ID where permission was insufficient
    /// </summary>
    public int ProjectId { get; }

    /// <summary>
    /// User's current access level
    /// </summary>
    public AccessLevel CurrentAccessLevel { get; }

    /// <summary>
    /// Required permissions that were missing
    /// </summary>
    public RequiredPermissions RequiredPermissions { get; }

    /// <summary>
    /// List of missing permission names
    /// </summary>
    public List<string> MissingPermissions { get; }

    /// <summary>
    /// Suggestions for resolving the permission issue
    /// </summary>
    public List<string> Suggestions { get; }

    /// <summary>
    /// Initializes a new instance of InsufficientPermissionsException
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="currentAccessLevel">Current access level</param>
    /// <param name="requiredPermissions">Required permissions</param>
    /// <param name="missingPermissions">Missing permission names</param>
    /// <param name="suggestions">Suggestions for resolution</param>
    public InsufficientPermissionsException(
        int projectId,
        AccessLevel currentAccessLevel,
        RequiredPermissions requiredPermissions,
        List<string> missingPermissions,
        List<string> suggestions)
        : base(GenerateMessage(projectId, currentAccessLevel, missingPermissions))
    {
        ProjectId = projectId;
        CurrentAccessLevel = currentAccessLevel;
        RequiredPermissions = requiredPermissions;
        MissingPermissions = missingPermissions;
        Suggestions = suggestions;
    }

    /// <summary>
    /// Initializes a new instance of InsufficientPermissionsException with custom message
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="currentAccessLevel">Current access level</param>
    /// <param name="requiredPermissions">Required permissions</param>
    /// <param name="missingPermissions">Missing permission names</param>
    /// <param name="suggestions">Suggestions for resolution</param>
    /// <param name="message">Custom error message</param>
    public InsufficientPermissionsException(
        int projectId,
        AccessLevel currentAccessLevel,
        RequiredPermissions requiredPermissions,
        List<string> missingPermissions,
        List<string> suggestions,
        string message)
        : base(message)
    {
        ProjectId = projectId;
        CurrentAccessLevel = currentAccessLevel;
        RequiredPermissions = requiredPermissions;
        MissingPermissions = missingPermissions;
        Suggestions = suggestions;
    }

    /// <summary>
    /// Gets a formatted error message with suggestions
    /// </summary>
    /// <returns>Detailed error message with suggestions</returns>
    public string GetDetailedMessage()
    {
        var message = Message;
        
        if (Suggestions.Any())
        {
            message += "\n\nSuggestions:";
            foreach (var suggestion in Suggestions)
            {
                message += $"\n• {suggestion}";
            }
        }

        return message;
    }

    /// <summary>
    /// Generates the default error message
    /// </summary>
    private static string GenerateMessage(int projectId, AccessLevel currentAccessLevel, List<string> missingPermissions)
    {
        return $"Insufficient permissions for project '{projectId}'. " +
               $"Current access level: {currentAccessLevel}. " +
               $"Missing permissions: {string.Join(", ", missingPermissions)}.";
    }
}