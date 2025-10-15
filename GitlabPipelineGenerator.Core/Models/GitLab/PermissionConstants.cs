namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// Constants and utilities for GitLab permissions
/// </summary>
public static class PermissionConstants
{
    /// <summary>
    /// Standard permission sets for common operations
    /// </summary>
    public static class StandardPermissions
    {
        /// <summary>
        /// Permissions required for basic project analysis
        /// </summary>
        public static readonly RequiredPermissions BasicAnalysis = 
            RequiredPermissions.ReadProject | RequiredPermissions.ReadRepository;

        /// <summary>
        /// Permissions required for comprehensive project analysis including CI/CD
        /// </summary>
        public static readonly RequiredPermissions FullAnalysis = 
            RequiredPermissions.ReadAll;

        /// <summary>
        /// Permissions required for reading project metadata only
        /// </summary>
        public static readonly RequiredPermissions ProjectMetadata = 
            RequiredPermissions.ReadProject;

        /// <summary>
        /// Permissions required for repository content analysis
        /// </summary>
        public static readonly RequiredPermissions RepositoryAnalysis = 
            RequiredPermissions.ReadRepository;

        /// <summary>
        /// Permissions required for CI/CD configuration analysis
        /// </summary>
        public static readonly RequiredPermissions CiCdAnalysis = 
            RequiredPermissions.ReadCiCd;
    }

    /// <summary>
    /// Human-readable descriptions for permission types
    /// </summary>
    public static class PermissionDescriptions
    {
        public static readonly Dictionary<RequiredPermissions, string> Descriptions = new()
        {
            { RequiredPermissions.ReadProject, "Read basic project information and metadata" },
            { RequiredPermissions.ReadRepository, "Read repository files and structure for analysis" },
            { RequiredPermissions.ReadCiCd, "Read CI/CD configuration and pipeline settings" },
            { RequiredPermissions.ReadAll, "Read all project data including repository and CI/CD" }
        };

        /// <summary>
        /// Gets a human-readable description for the specified permissions
        /// </summary>
        /// <param name="permissions">Permissions to describe</param>
        /// <returns>Human-readable description</returns>
        public static string GetDescription(RequiredPermissions permissions)
        {
            if (Descriptions.TryGetValue(permissions, out var description))
            {
                return description;
            }

            // Build description for combined permissions
            var parts = new List<string>();
            
            if (permissions.HasFlag(RequiredPermissions.ReadProject))
                parts.Add("read project information");
            
            if (permissions.HasFlag(RequiredPermissions.ReadRepository))
                parts.Add("read repository content");
            
            if (permissions.HasFlag(RequiredPermissions.ReadCiCd))
                parts.Add("read CI/CD configuration");

            return parts.Any() ? string.Join(", ", parts) : "no specific permissions";
        }
    }

    /// <summary>
    /// Access level requirements for different operations
    /// </summary>
    public static class AccessLevelRequirements
    {
        /// <summary>
        /// Minimum access level required for each permission type
        /// </summary>
        public static readonly Dictionary<RequiredPermissions, AccessLevel> MinimumLevels = new()
        {
            { RequiredPermissions.ReadProject, AccessLevel.Guest },
            { RequiredPermissions.ReadRepository, AccessLevel.Reporter },
            { RequiredPermissions.ReadCiCd, AccessLevel.Reporter },
            { RequiredPermissions.ReadAll, AccessLevel.Reporter }
        };

        /// <summary>
        /// Gets the minimum access level required for the specified permissions
        /// </summary>
        /// <param name="permissions">Required permissions</param>
        /// <returns>Minimum access level needed</returns>
        public static AccessLevel GetMinimumAccessLevel(RequiredPermissions permissions)
        {
            var maxLevel = AccessLevel.NoAccess;

            foreach (var kvp in MinimumLevels)
            {
                if (permissions.HasFlag(kvp.Key) && kvp.Value > maxLevel)
                {
                    maxLevel = kvp.Value;
                }
            }

            return maxLevel;
        }
    }

    /// <summary>
    /// Common error messages for permission failures
    /// </summary>
    public static class ErrorMessages
    {
        public const string ProjectNotFound = "Project was not found or you don't have access to it";
        public const string InsufficientAccess = "You don't have sufficient permissions to perform this operation";
        public const string RepositoryAccessRequired = "Repository read access is required for project analysis";
        public const string CiCdAccessRequired = "CI/CD read access is required to analyze pipeline configurations";
        public const string ContactOwner = "Contact the project owner or maintainer to request appropriate access";
        public const string VerifyProjectId = "Please verify the project ID is correct and the project exists";
    }

    /// <summary>
    /// Common suggestions for resolving permission issues
    /// </summary>
    public static class Suggestions
    {
        public static readonly List<string> RequestAccess = new()
        {
            "Request access to the project from the project owner or maintainer",
            "Ensure you have at least Reporter access for full project analysis",
            "Contact your GitLab administrator if you believe you should have access"
        };

        public static readonly List<string> VerifyProject = new()
        {
            "Verify the project ID or path is correct",
            "Ensure the project exists and is not archived",
            "Check if the project visibility allows your access level"
        };

        public static readonly List<string> AlternativeOptions = new()
        {
            "You can still use manual pipeline generation mode without project analysis",
            "Consider using a different project where you have appropriate permissions",
            "Ask the project owner to generate the pipeline configuration for you"
        };

        /// <summary>
        /// Gets appropriate suggestions based on the access level and missing permissions
        /// </summary>
        /// <param name="currentLevel">Current access level</param>
        /// <param name="missingPermissions">Missing permissions</param>
        /// <returns>List of relevant suggestions</returns>
        public static List<string> GetSuggestions(AccessLevel currentLevel, List<string> missingPermissions)
        {
            var suggestions = new List<string>();

            if (currentLevel == AccessLevel.NoAccess)
            {
                suggestions.AddRange(RequestAccess);
                suggestions.AddRange(VerifyProject);
            }
            else if (currentLevel < AccessLevel.Reporter)
            {
                suggestions.Add("Request Reporter access or higher from a project maintainer");
                suggestions.Add("Reporter access is required to read repository content and CI/CD configurations");
            }

            if (missingPermissions.Contains("Read Repository"))
            {
                suggestions.Add("Repository read access is required for project analysis");
            }

            if (missingPermissions.Contains("Read CI/CD"))
            {
                suggestions.Add("CI/CD read access is required to analyze existing pipeline configurations");
            }

            suggestions.AddRange(AlternativeOptions);

            return suggestions.Distinct().ToList();
        }
    }
}