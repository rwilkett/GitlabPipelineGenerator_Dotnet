namespace GitlabPipelineGenerator.Core.Models.GitLab;

/// <summary>
/// GitLab project visibility levels
/// </summary>
public enum ProjectVisibility
{
    /// <summary>
    /// Private project - only accessible to project members
    /// </summary>
    Private = 0,

    /// <summary>
    /// Internal project - accessible to authenticated users
    /// </summary>
    Internal = 10,

    /// <summary>
    /// Public project - accessible to everyone
    /// </summary>
    Public = 20
}

/// <summary>
/// Project types detected during analysis
/// </summary>
public enum ProjectType
{
    /// <summary>
    /// Unknown or undetected project type
    /// </summary>
    Unknown,

    /// <summary>
    /// .NET project (C#, F#, VB.NET)
    /// </summary>
    DotNet,

    /// <summary>
    /// Node.js project
    /// </summary>
    NodeJs,

    /// <summary>
    /// Python project
    /// </summary>
    Python,

    /// <summary>
    /// Java project
    /// </summary>
    Java,

    /// <summary>
    /// Go project
    /// </summary>
    Go,

    /// <summary>
    /// Ruby project
    /// </summary>
    Ruby,

    /// <summary>
    /// PHP project
    /// </summary>
    PHP,

    /// <summary>
    /// Rust project
    /// </summary>
    Rust,

    /// <summary>
    /// Static website or frontend project
    /// </summary>
    Static,

    /// <summary>
    /// Docker-based project
    /// </summary>
    Docker,

    /// <summary>
    /// Multi-language or polyglot project
    /// </summary>
    Mixed
}

/// <summary>
/// Confidence levels for project analysis results
/// </summary>
public enum AnalysisConfidence
{
    /// <summary>
    /// Low confidence - analysis may be inaccurate
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium confidence - analysis is likely accurate
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High confidence - analysis is very likely accurate
    /// </summary>
    High = 3
}

/// <summary>
/// Required permissions for project operations
/// </summary>
[Flags]
public enum RequiredPermissions
{
    /// <summary>
    /// No special permissions required
    /// </summary>
    None = 0,

    /// <summary>
    /// Read access to repository
    /// </summary>
    ReadRepository = 1,

    /// <summary>
    /// Read access to project metadata
    /// </summary>
    ReadProject = 2,

    /// <summary>
    /// Read access to CI/CD configuration
    /// </summary>
    ReadCiCd = 4,

    /// <summary>
    /// All read permissions
    /// </summary>
    ReadAll = ReadRepository | ReadProject | ReadCiCd
}

/// <summary>
/// Options for ordering project lists
/// </summary>
public enum ProjectOrderBy
{
    /// <summary>
    /// Order by project ID
    /// </summary>
    Id,

    /// <summary>
    /// Order by project name
    /// </summary>
    Name,

    /// <summary>
    /// Order by creation date
    /// </summary>
    CreatedAt,

    /// <summary>
    /// Order by last activity date
    /// </summary>
    LastActivity,

    /// <summary>
    /// Order by update date
    /// </summary>
    UpdatedAt
}

/// <summary>
/// GitLab access levels for project members
/// </summary>
public enum AccessLevel
{
    /// <summary>
    /// No access
    /// </summary>
    NoAccess = 0,

    /// <summary>
    /// Minimal access - can see project
    /// </summary>
    MinimalAccess = 5,

    /// <summary>
    /// Guest access - can view issues and comments
    /// </summary>
    Guest = 10,

    /// <summary>
    /// Reporter access - can read repository and create issues
    /// </summary>
    Reporter = 20,

    /// <summary>
    /// Developer access - can push to repository and manage issues
    /// </summary>
    Developer = 30,

    /// <summary>
    /// Maintainer access - can manage project settings and members
    /// </summary>
    Maintainer = 40,

    /// <summary>
    /// Owner access - full control over project
    /// </summary>
    Owner = 50
}

/// <summary>
/// Analysis mode used for project analysis
/// </summary>
public enum AnalysisMode
{
    /// <summary>
    /// Full analysis using GitLab API
    /// </summary>
    Full,

    /// <summary>
    /// Partial analysis with some API limitations
    /// </summary>
    Partial,

    /// <summary>
    /// Degraded analysis using cached or minimal data
    /// </summary>
    Degraded,

    /// <summary>
    /// Pattern-based analysis using project name/path patterns
    /// </summary>
    PatternBased,

    /// <summary>
    /// Manual analysis using user-provided configuration
    /// </summary>
    Manual
}

/// <summary>
/// Types of analysis warnings
/// </summary>
public enum WarningType
{
    /// <summary>
    /// General warning
    /// </summary>
    General,

    /// <summary>
    /// Using cached data
    /// </summary>
    CachedData,

    /// <summary>
    /// No data available
    /// </summary>
    NoData,

    /// <summary>
    /// Operating in degraded mode
    /// </summary>
    DegradedMode,

    /// <summary>
    /// Analysis based on patterns
    /// </summary>
    PatternBased,

    /// <summary>
    /// Manual configuration override
    /// </summary>
    ManualOverride,

    /// <summary>
    /// API limitation encountered
    /// </summary>
    ApiLimitation,

    /// <summary>
    /// Partial analysis completed
    /// </summary>
    PartialAnalysis
}

/// <summary>
/// Severity levels for warnings
/// </summary>
public enum WarningSeverity
{
    /// <summary>
    /// Informational message
    /// </summary>
    Info,

    /// <summary>
    /// Warning message
    /// </summary>
    Warning,

    /// <summary>
    /// Error message
    /// </summary>
    Error
}