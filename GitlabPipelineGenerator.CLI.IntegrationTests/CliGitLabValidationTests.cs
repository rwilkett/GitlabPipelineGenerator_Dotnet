using FluentAssertions;
using GitlabPipelineGenerator.CLI.Models;

namespace GitlabPipelineGenerator.CLI.IntegrationTests;

/// <summary>
/// Integration tests for CLI GitLab option validation and error handling.
/// These tests focus on validating CLI argument parsing and validation logic
/// without requiring the full GitLab API integration to be functional.
/// </summary>
public class CliGitLabValidationTests
{
    #region GitLab Option Validation Tests

    [Fact]
    public void CommandLineOptions_GitLabAnalysis_WithoutToken_ShouldFailValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("GitLab token") && error.Contains("required"), 
            "Should require GitLab token for analysis");
    }

    [Fact]
    public void CommandLineOptions_GitLabAnalysis_WithoutProject_ShouldFailValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("GitLab project") && error.Contains("required"), 
            "Should require GitLab project for analysis");
    }

    [Fact]
    public void CommandLineOptions_GitLabAnalysis_WithInvalidUrl_ShouldFailValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            AnalyzeProject = true,
            GitLabToken = "glpat-test-token",
            GitLabProject = "test-group/test-project",
            GitLabUrl = "invalid-url-format"
        };

        // Act
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("Invalid GitLab URL"), 
            "Should validate GitLab URL format");
    }

    [Fact]
    public void CommandLineOptions_GitLabAnalysis_WithValidOptions_ShouldPassValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().BeEmpty("Should pass validation with valid options");
    }

    #endregion

    #region Project Discovery Validation Tests

    [Fact]
    public void CommandLineOptions_ListProjects_WithoutToken_ShouldFailValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            ListProjects = true
            // GitLabToken is missing
        };

        // Act
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("GitLab token") && error.Contains("required"), 
            "Should require GitLab token for project discovery");
    }

    [Fact]
    public void CommandLineOptions_SearchProjects_WithoutToken_ShouldFailValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            SearchProjects = "test-project"
            // GitLabToken is missing
        };

        // Act
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("GitLab token") && error.Contains("required"), 
            "Should require GitLab token for project search");
    }

    [Fact]
    public void CommandLineOptions_ProjectDiscovery_WithValidToken_ShouldPassValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            ListProjects = true,
            GitLabToken = "glpat-test-token",
            GitLabUrl = "https://gitlab.example.com"
        };

        // Act
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().BeEmpty("Should pass validation with valid token");
    }

    #endregion

    #region Analysis Options Validation Tests

    [Fact]
    public void CommandLineOptions_AnalysisDepth_WithInvalidValue_ShouldFailValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("Analysis depth") && error.Contains("between 1 and 3"), 
            "Should validate analysis depth range");
    }

    [Fact]
    public void CommandLineOptions_SkipAnalysis_WithInvalidType_ShouldFailValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("Invalid skip analysis type"), 
            "Should validate skip analysis types");
    }

    [Fact]
    public void CommandLineOptions_SkipAnalysis_WithValidTypes_ShouldPassValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            AnalyzeProject = true,
            GitLabToken = "glpat-test-token",
            GitLabProject = "test-group/test-project",
            SkipAnalysis = new[] { "files", "dependencies", "config" }
        };

        // Act
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().BeEmpty("Should pass validation with valid skip analysis types");
    }

    [Fact]
    public void CommandLineOptions_MaxProjects_WithInvalidValue_ShouldFailValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("Max projects") && error.Contains("between 1 and 1000"), 
            "Should validate max projects range");
    }

    #endregion

    #region Hybrid Mode Validation Tests

    [Fact]
    public void CommandLineOptions_PreferDetected_WithoutAnalyzeProject_ShouldFailValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            PreferDetected = true
            // AnalyzeProject is false
        };

        // Act
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("--prefer-detected") && error.Contains("--analyze-project"), 
            "Should require analyze-project for prefer-detected");
    }

    [Fact]
    public void CommandLineOptions_ShowConflicts_WithoutAnalyzeProject_ShouldFailValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            ShowConflicts = true
            // AnalyzeProject is false
        };

        // Act
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("--show-conflicts") && error.Contains("--analyze-project"), 
            "Should require analyze-project for show-conflicts");
    }

    [Fact]
    public void CommandLineOptions_ShowAnalysis_WithoutAnalyzeProject_ShouldFailValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            ShowAnalysis = true
            // AnalyzeProject is false
        };

        // Act
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("--show-analysis") && error.Contains("--analyze-project"), 
            "Should require analyze-project for show-analysis");
    }

    [Fact]
    public void CommandLineOptions_HybridMode_WithValidOptions_ShouldPassValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            AnalyzeProject = true,
            GitLabToken = "glpat-test-token",
            GitLabProject = "test-group/test-project",
            PreferDetected = true,
            ShowConflicts = true,
            ShowAnalysis = true,
            MergeConfig = true
        };

        // Act
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().BeEmpty("Should pass validation with valid hybrid mode options");
    }

    #endregion

    #region Conflicting Options Validation Tests

    [Fact]
    public void CommandLineOptions_ListProjectsAndSearchProjects_ShouldFailValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("--list-projects") && error.Contains("--search-projects"), 
            "Should not allow both list and search projects");
    }

    [Fact]
    public void CommandLineOptions_ProjectDiscoveryAndAnalyzeProject_ShouldFailValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("project discovery") && error.Contains("--analyze-project"), 
            "Should not allow project discovery with analyze-project");
    }

    [Fact]
    public void CommandLineOptions_GitLabProfileAndToken_ShouldFailValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("--gitlab-profile") && error.Contains("--gitlab-token"), 
            "Should not allow both profile and token");
    }

    #endregion

    #region Project Filter Validation Tests

    [Fact]
    public void CommandLineOptions_ProjectFilter_WithInvalidFilter_ShouldFailValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("Invalid project filter"), 
            "Should validate project filter values");
    }

    [Fact]
    public void CommandLineOptions_ProjectFilter_WithValidFilters_ShouldPassValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().BeEmpty("Should pass validation with valid project filters");
    }

    #endregion

    #region GitLab URL Validation Tests

    [Fact]
    public void CommandLineOptions_GitLabUrl_WithHttpsUrl_ShouldPassValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().BeEmpty("Should pass validation with HTTPS URL");
    }

    [Fact]
    public void CommandLineOptions_GitLabUrl_WithHttpUrl_ShouldPassValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().BeEmpty("Should pass validation with HTTP URL");
    }

    [Fact]
    public void CommandLineOptions_GitLabUrl_WithInvalidProtocol_ShouldFailValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("HTTP or HTTPS protocol"), 
            "Should require HTTP or HTTPS protocol");
    }

    #endregion

    #region Complex Validation Scenarios

    [Fact]
    public void CommandLineOptions_CompleteGitLabWorkflow_WithAllValidOptions_ShouldPassValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().BeEmpty("Should pass validation with all valid GitLab workflow options");
    }

    [Fact]
    public void CommandLineOptions_ProjectDiscovery_WithAllValidOptions_ShouldPassValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            ListProjects = true,
            GitLabToken = "glpat-test-token-12345",
            GitLabUrl = "https://gitlab.example.com",
            ProjectFilter = new[] { "owned", "private" },
            MaxProjects = 25,
            Verbose = true
        };

        // Act
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().BeEmpty("Should pass validation with all valid project discovery options");
    }

    [Fact]
    public void CommandLineOptions_SearchProjects_WithAllValidOptions_ShouldPassValidation()
    {
        // Arrange
        var options = new CommandLineOptions
        {
            ProjectType = "dotnet",
            SearchProjects = "my-project",
            GitLabToken = "glpat-test-token-12345",
            GitLabUrl = "https://gitlab.example.com",
            ProjectFilter = new[] { "member", "public" },
            MaxProjects = 50,
            Verbose = true
        };

        // Act
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().BeEmpty("Should pass validation with all valid search options");
    }

    [Fact]
    public void CommandLineOptions_GitLabProfile_WithValidProfile_ShouldPassValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().BeEmpty("Should pass validation with valid GitLab profile");
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public void CommandLineOptions_EmptyGitLabToken_ShouldFailValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("GitLab token") && error.Contains("required"), 
            "Should treat empty token as missing");
    }

    [Fact]
    public void CommandLineOptions_EmptyGitLabProject_ShouldFailValidation()
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
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have validation errors");
        validationErrors.Should().Contain(error => 
            error.Contains("GitLab project") && error.Contains("required"), 
            "Should treat empty project as missing");
    }

    [Fact]
    public void CommandLineOptions_MultipleValidationErrors_ShouldReportAll()
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
            PreferDetected = true, // Invalid without analyze-project token
            ShowConflicts = true, // Invalid without analyze-project token
            GitLabUrl = "invalid-url" // Invalid URL
        };

        // Act
        var validationErrors = options.Validate();

        // Assert
        validationErrors.Should().NotBeEmpty("Should have multiple validation errors");
        validationErrors.Count.Should().BeGreaterThan(3, "Should report multiple validation errors");
        
        // Check for specific error types
        validationErrors.Should().Contain(error => error.Contains("Invalid project type"));
        validationErrors.Should().Contain(error => error.Contains("GitLab token") && error.Contains("required"));
        validationErrors.Should().Contain(error => error.Contains("GitLab project") && error.Contains("required"));
        validationErrors.Should().Contain(error => error.Contains("Analysis depth"));
        validationErrors.Should().Contain(error => error.Contains("Invalid GitLab URL"));
    }

    #endregion
}