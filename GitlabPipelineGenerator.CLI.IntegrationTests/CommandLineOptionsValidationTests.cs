using FluentAssertions;
using GitlabPipelineGenerator.CLI.Models;

namespace GitlabPipelineGenerator.CLI.IntegrationTests;

/// <summary>
/// Unit tests for CommandLineOptions validation logic specifically for GitLab integration features.
/// These tests validate the CLI argument parsing and validation without requiring the full application to compile.
/// </summary>
public class CommandLineOptionsValidationTests
{
    #region GitLab Authentication Validation

    [Fact]
    public void Validate_AnalyzeProject_WithoutToken_ShouldReturnError()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            AnalyzeProject = true,
            GitLabProject = "test-group/test-project"
            // GitLabToken is missing
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors when token is missing");
        errors.Should().Contain(error => 
            error.Contains("GitLab token") && error.Contains("required"), 
            "Should require GitLab token for project analysis");
    }

    [Fact]
    public void Validate_AnalyzeProject_WithoutProject_ShouldReturnError()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            AnalyzeProject = true,
            GitLabToken = "glpat-test-token"
            // GitLabProject is missing
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors when project is missing");
        errors.Should().Contain(error => 
            error.Contains("GitLab project") && error.Contains("required"), 
            "Should require GitLab project for analysis");
    }

    [Fact]
    public void Validate_AnalyzeProject_WithValidCredentials_ShouldPassValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            AnalyzeProject = true,
            GitLabToken = "glpat-test-token",
            GitLabProject = "test-group/test-project"
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().BeEmpty("Should pass validation with valid credentials");
    }

    #endregion

    #region GitLab URL Validation

    [Fact]
    public void Validate_GitLabUrl_WithInvalidFormat_ShouldReturnError()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            AnalyzeProject = true,
            GitLabToken = "glpat-test-token",
            GitLabProject = "test-group/test-project",
            GitLabUrl = "not-a-valid-url"
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors for invalid URL");
        errors.Should().Contain(error => 
            error.Contains("Invalid GitLab URL"), 
            "Should validate GitLab URL format");
    }

    [Fact]
    public void Validate_GitLabUrl_WithInvalidProtocol_ShouldReturnError()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            AnalyzeProject = true,
            GitLabToken = "glpat-test-token",
            GitLabProject = "test-group/test-project",
            GitLabUrl = "ftp://gitlab.example.com"
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors for invalid protocol");
        errors.Should().Contain(error => 
            error.Contains("HTTP or HTTPS protocol"), 
            "Should require HTTP or HTTPS protocol");
    }

    [Fact]
    public void Validate_GitLabUrl_WithHttps_ShouldPassValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            AnalyzeProject = true,
            GitLabToken = "glpat-test-token",
            GitLabProject = "test-group/test-project",
            GitLabUrl = "https://gitlab.example.com"
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().BeEmpty("Should pass validation with HTTPS URL");
    }

    [Fact]
    public void Validate_GitLabUrl_WithHttp_ShouldPassValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            AnalyzeProject = true,
            GitLabToken = "glpat-test-token",
            GitLabProject = "test-group/test-project",
            GitLabUrl = "http://gitlab.internal.com"
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().BeEmpty("Should pass validation with HTTP URL");
    }

    #endregion

    #region Project Discovery Validation

    [Fact]
    public void Validate_ListProjects_WithoutToken_ShouldReturnError()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            ListProjects = true
            // GitLabToken is missing
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors when token is missing");
        errors.Should().Contain(error => 
            error.Contains("GitLab token") && error.Contains("required"), 
            "Should require GitLab token for project listing");
    }

    [Fact]
    public void Validate_SearchProjects_WithoutToken_ShouldReturnError()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            SearchProjects = "test-project"
            // GitLabToken is missing
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors when token is missing");
        errors.Should().Contain(error => 
            error.Contains("GitLab token") && error.Contains("required"), 
            "Should require GitLab token for project search");
    }

    #endregion

    #region Analysis Options Validation

    [Fact]
    public void Validate_AnalysisDepth_WithInvalidValue_ShouldReturnError()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            AnalyzeProject = true,
            GitLabToken = "glpat-test-token",
            GitLabProject = "test-group/test-project",
            AnalysisDepth = 5 // Invalid: should be 1-3
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors for invalid analysis depth");
        errors.Should().Contain(error => 
            error.Contains("Analysis depth") && error.Contains("between 1 and 3"), 
            "Should validate analysis depth range");
    }

    [Fact]
    public void Validate_SkipAnalysis_WithInvalidType_ShouldReturnError()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            AnalyzeProject = true,
            GitLabToken = "glpat-test-token",
            GitLabProject = "test-group/test-project",
            SkipAnalysis = new[] { "invalid-type" }
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors for invalid skip analysis type");
        errors.Should().Contain(error => 
            error.Contains("Invalid skip analysis type"), 
            "Should validate skip analysis types");
    }

    [Fact]
    public void Validate_MaxProjects_WithInvalidValue_ShouldReturnError()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            ListProjects = true,
            GitLabToken = "glpat-test-token",
            MaxProjects = 2000 // Invalid: should be 1-1000
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors for invalid max projects");
        errors.Should().Contain(error => 
            error.Contains("Max projects") && error.Contains("between 1 and 1000"), 
            "Should validate max projects range");
    }

    #endregion

    #region Hybrid Mode Validation

    [Fact]
    public void Validate_PreferDetected_WithoutAnalyzeProject_ShouldReturnError()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            PreferDetected = true
            // AnalyzeProject is false
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors when prefer-detected used without analyze-project");
        errors.Should().Contain(error => 
            error.Contains("--prefer-detected") && error.Contains("--analyze-project"), 
            "Should require analyze-project for prefer-detected");
    }

    [Fact]
    public void Validate_ShowConflicts_WithoutAnalyzeProject_ShouldReturnError()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            ShowConflicts = true
            // AnalyzeProject is false
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors when show-conflicts used without analyze-project");
        errors.Should().Contain(error => 
            error.Contains("--show-conflicts") && error.Contains("--analyze-project"), 
            "Should require analyze-project for show-conflicts");
    }

    [Fact]
    public void Validate_ShowAnalysis_WithoutAnalyzeProject_ShouldReturnError()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            ShowAnalysis = true
            // AnalyzeProject is false
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors when show-analysis used without analyze-project");
        errors.Should().Contain(error => 
            error.Contains("--show-analysis") && error.Contains("--analyze-project"), 
            "Should require analyze-project for show-analysis");
    }

    #endregion

    #region Conflicting Options Validation

    [Fact]
    public void Validate_ListProjectsAndSearchProjects_ShouldReturnError()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            ListProjects = true,
            SearchProjects = "test-project",
            GitLabToken = "glpat-test-token"
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors for conflicting options");
        errors.Should().Contain(error => 
            error.Contains("--list-projects") && error.Contains("--search-projects"), 
            "Should not allow both list and search projects");
    }

    [Fact]
    public void Validate_ProjectDiscoveryAndAnalyzeProject_ShouldReturnError()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            ListProjects = true,
            AnalyzeProject = true,
            GitLabToken = "glpat-test-token",
            GitLabProject = "test-group/test-project"
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors for conflicting options");
        errors.Should().Contain(error => 
            error.Contains("project discovery") && error.Contains("--analyze-project"), 
            "Should not allow project discovery with analyze-project");
    }

    [Fact]
    public void Validate_GitLabProfileAndToken_ShouldReturnError()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            AnalyzeProject = true,
            GitLabProfile = "test-profile",
            GitLabToken = "glpat-test-token",
            GitLabProject = "test-group/test-project"
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors for conflicting authentication options");
        errors.Should().Contain(error => 
            error.Contains("--gitlab-profile") && error.Contains("--gitlab-token"), 
            "Should not allow both profile and token");
    }

    #endregion

    #region Project Filter Validation

    [Fact]
    public void Validate_ProjectFilter_WithInvalidFilter_ShouldReturnError()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            ListProjects = true,
            GitLabToken = "glpat-test-token",
            ProjectFilter = new[] { "invalid-filter" }
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors for invalid project filter");
        errors.Should().Contain(error => 
            error.Contains("Invalid project filter"), 
            "Should validate project filter values");
    }

    [Fact]
    public void Validate_ProjectFilter_WithValidFilters_ShouldPassValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            ListProjects = true,
            GitLabToken = "glpat-test-token",
            ProjectFilter = new[] { "owned", "private", "member" }
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().BeEmpty("Should pass validation with valid project filters");
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void Validate_CompleteGitLabWorkflow_WithAllValidOptions_ShouldPassValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            DotNetVersion = "9.0",
            AnalyzeProject = true,
            GitLabToken = "glpat-test-token-12345",
            GitLabUrl = "https://gitlab.example.com",
            GitLabProject = "test-group/test-project",
            AnalysisDepth = 2,
            SkipAnalysis = new[] { "deployment" },
            ShowAnalysis = true,
            ShowConflicts = true,
            MergeConfig = true,
            Stages = new[] { "build", "test", "deploy" },
            IncludeCodeQuality = true,
            IncludeSecurity = true,
            Variables = new[] { "BUILD_CONFIG=Release", "TEST_ENV=staging" },
            Verbose = true
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().BeEmpty("Should pass validation with all valid GitLab workflow options");
    }

    [Fact]
    public void Validate_MultipleValidationErrors_ShouldReportAll()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "invalid-type",
            AnalyzeProject = true,
            // Missing GitLabToken and GitLabProject
            AnalysisDepth = 5, // Invalid depth
            SkipAnalysis = new[] { "invalid-type" }, // Invalid skip type
            MaxProjects = 2000, // Invalid max projects
            PreferDetected = true, // Invalid without proper setup
            ShowConflicts = true, // Invalid without proper setup
            GitLabUrl = "invalid-url" // Invalid URL
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have multiple validation errors");
        errors.Count.Should().BeGreaterThan(3, "Should report multiple validation errors");
        
        // Check for specific error types
        errors.Should().Contain(error => error.Contains("Invalid project type"));
        errors.Should().Contain(error => error.Contains("GitLab token") && error.Contains("required"));
        errors.Should().Contain(error => error.Contains("GitLab project") && error.Contains("required"));
        errors.Should().Contain(error => error.Contains("Analysis depth"));
        errors.Should().Contain(error => error.Contains("Invalid GitLab URL"));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_EmptyGitLabToken_ShouldFailValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            AnalyzeProject = true,
            GitLabToken = "", // Empty token
            GitLabProject = "test-group/test-project"
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors for empty token");
        errors.Should().Contain(error => 
            error.Contains("GitLab token") && error.Contains("required"), 
            "Should treat empty token as missing");
    }

    [Fact]
    public void Validate_EmptyGitLabProject_ShouldFailValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            AnalyzeProject = true,
            GitLabToken = "glpat-test-token",
            GitLabProject = "" // Empty project
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().NotBeEmpty("Should have validation errors for empty project");
        errors.Should().Contain(error => 
            error.Contains("GitLab project") && error.Contains("required"), 
            "Should treat empty project as missing");
    }

    [Fact]
    public void Validate_GitLabProfile_WithValidProfile_ShouldPassValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            AnalyzeProject = true,
            GitLabProfile = "company-profile",
            GitLabProject = "test-group/test-project",
            ShowAnalysis = true
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().BeEmpty("Should pass validation with valid GitLab profile");
    }

    #endregion
}