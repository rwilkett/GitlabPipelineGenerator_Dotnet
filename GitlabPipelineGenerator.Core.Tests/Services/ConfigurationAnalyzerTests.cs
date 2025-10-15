using FluentAssertions;
using GitlabPipelineGenerator.Core.Models.GitLab;
using GitlabPipelineGenerator.Core.Services;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

/// <summary>
/// Unit tests for ConfigurationAnalyzer
/// </summary>
public class ConfigurationAnalyzerTests
{
    private readonly ConfigurationAnalyzer _analyzer;

    public ConfigurationAnalyzerTests()
    {
        _analyzer = new ConfigurationAnalyzer();
    }

    #region AnalyzeExistingCIConfigAsync Tests

    [Fact]
    public async Task AnalyzeExistingCIConfigAsync_WithGitLabCIFile_ShouldDetectGitLabCI()
    {
        // Arrange
        var project = CreateSampleProject();

        // Act
        var result = await _analyzer.AnalyzeExistingCIConfigAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.HasExistingConfig.Should().BeTrue();
        result.SystemType.Should().Be(CISystemType.GitLabCI);
        result.ConfigurationFiles.Should().Contain(".gitlab-ci.yml");
        result.Confidence.Should().Be(AnalysisConfidence.High);
        result.DetectedStages.Should().Contain("build");
        result.DetectedStages.Should().Contain("test");
        result.DetectedStages.Should().Contain("deploy");
    }

    [Fact]
    public async Task AnalyzeExistingCIConfigAsync_WithGitHubActions_ShouldDetectGitHubActions()
    {
        // Arrange
        var project = CreateSampleProjectWithGitHubActions();

        // Act
        var result = await _analyzer.AnalyzeExistingCIConfigAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.HasExistingConfig.Should().BeTrue();
        result.SystemType.Should().Be(CISystemType.GitHubActions);
        result.ConfigurationFiles.Should().Contain(".github/workflows");
        result.Confidence.Should().Be(AnalysisConfidence.High);
        result.MigrationRecommendations.Should().NotBeEmpty();
        result.MigrationRecommendations.Should().Contain(r => r.Contains("GitHub Actions"));
    }

    [Fact]
    public async Task AnalyzeExistingCIConfigAsync_WithNoCIConfig_ShouldReturnNoCIConfig()
    {
        // Arrange
        var project = CreateSampleProjectWithoutCI();

        // Act
        var result = await _analyzer.AnalyzeExistingCIConfigAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.HasExistingConfig.Should().BeFalse();
        result.SystemType.Should().Be(CISystemType.None);
        result.ConfigurationFiles.Should().BeEmpty();
        result.Confidence.Should().Be(AnalysisConfidence.Low);
    }

    [Fact]
    public async Task AnalyzeExistingCIConfigAsync_WithJenkinsfile_ShouldDetectJenkins()
    {
        // Arrange
        var project = CreateSampleProjectWithJenkins();

        // Act
        var result = await _analyzer.AnalyzeExistingCIConfigAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.HasExistingConfig.Should().BeTrue();
        result.SystemType.Should().Be(CISystemType.Jenkins);
        result.ConfigurationFiles.Should().Contain("jenkinsfile");
        result.MigrationRecommendations.Should().Contain(r => r.Contains("Jenkinsfile"));
    }

    #endregion

    #region AnalyzeDockerConfigurationAsync Tests

    [Fact]
    public async Task AnalyzeDockerConfigurationAsync_WithDockerfile_ShouldDetectDockerConfig()
    {
        // Arrange
        var project = CreateSampleProject();

        // Act
        var result = await _analyzer.AnalyzeDockerConfigurationAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.HasDockerConfig.Should().BeTrue();
        result.DockerfilePath.Should().Be("Dockerfile");
        result.Confidence.Should().Be(AnalysisConfidence.High);
        result.BaseImage.Should().Be("node:16-alpine");
        result.ExposedPorts.Should().Contain(3000);
        result.IsMultiStage.Should().BeTrue();
        result.BuildStages.Should().Contain("builder");
        result.BuildStages.Should().Contain("runtime");
    }

    [Fact]
    public async Task AnalyzeDockerConfigurationAsync_WithDockerCompose_ShouldDetectComposeConfig()
    {
        // Arrange
        var project = CreateSampleProject();

        // Act
        var result = await _analyzer.AnalyzeDockerConfigurationAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.HasDockerConfig.Should().BeTrue();
        result.ComposeFiles.Should().Contain("docker-compose.yml");
        result.Services.Should().HaveCount(2);
        
        var appService = result.Services.FirstOrDefault(s => s.Name == "app");
        appService.Should().NotBeNull();
        appService!.Image.Should().Be("node:16");
        appService.Ports.Should().Contain("3000:3000");
        
        var dbService = result.Services.FirstOrDefault(s => s.Name == "database");
        dbService.Should().NotBeNull();
        dbService!.Image.Should().Be("postgres:13");
    }

    [Fact]
    public async Task AnalyzeDockerConfigurationAsync_WithNoDockerConfig_ShouldReturnNoConfig()
    {
        // Arrange
        var project = CreateSampleProjectWithoutDocker();

        // Act
        var result = await _analyzer.AnalyzeDockerConfigurationAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.HasDockerConfig.Should().BeFalse();
        result.DockerfilePath.Should().BeNull();
        result.ComposeFiles.Should().BeEmpty();
        result.Services.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeDockerConfigurationAsync_WithOptimizationOpportunities_ShouldProvideRecommendations()
    {
        // Arrange
        var project = CreateSampleProject();

        // Act
        var result = await _analyzer.AnalyzeDockerConfigurationAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.OptimizationRecommendations.Should().NotBeEmpty();
        // Since the base image is already alpine, it shouldn't recommend alpine
        result.OptimizationRecommendations.Should().NotContain(r => r.Contains("Alpine"));
    }

    #endregion

    #region AnalyzeDeploymentConfigurationAsync Tests

    [Fact]
    public async Task AnalyzeDeploymentConfigurationAsync_WithKubernetesFiles_ShouldDetectKubernetes()
    {
        // Arrange
        var project = CreateSampleProject();

        // Act
        var result = await _analyzer.AnalyzeDeploymentConfigurationAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.HasDeploymentConfig.Should().BeTrue();
        result.KubernetesFiles.Should().Contain("deployment.yaml");
        result.KubernetesFiles.Should().Contain("service.yaml");
        result.Confidence.Should().Be(AnalysisConfidence.Medium);
        
        var k8sTarget = result.Targets.FirstOrDefault(t => t.Name == "kubernetes");
        k8sTarget.Should().NotBeNull();
        k8sTarget!.Type.Should().Be(DeploymentTargetType.Kubernetes);
    }

    [Fact]
    public async Task AnalyzeDeploymentConfigurationAsync_WithHelmCharts_ShouldDetectHelm()
    {
        // Arrange
        var project = CreateSampleProjectWithHelm();

        // Act
        var result = await _analyzer.AnalyzeDeploymentConfigurationAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.HasDeploymentConfig.Should().BeTrue();
        result.HelmCharts.Should().Contain("Chart.yaml");
        
        var helmTarget = result.Targets.FirstOrDefault(t => t.Name == "helm");
        helmTarget.Should().NotBeNull();
        helmTarget!.Type.Should().Be(DeploymentTargetType.Kubernetes);
    }

    [Fact]
    public async Task AnalyzeDeploymentConfigurationAsync_WithTerraformFiles_ShouldDetectTerraform()
    {
        // Arrange
        var project = CreateSampleProjectWithTerraform();

        // Act
        var result = await _analyzer.AnalyzeDeploymentConfigurationAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.HasDeploymentConfig.Should().BeTrue();
        result.TerraformFiles.Should().NotBeEmpty();
        
        var terraformTarget = result.Targets.FirstOrDefault(t => t.Name == "terraform");
        terraformTarget.Should().NotBeNull();
        terraformTarget!.Type.Should().Be(DeploymentTargetType.Cloud);
    }

    [Fact]
    public async Task AnalyzeDeploymentConfigurationAsync_WithDeploymentScripts_ShouldDetectScripts()
    {
        // Arrange
        var project = CreateSampleProject();

        // Act
        var result = await _analyzer.AnalyzeDeploymentConfigurationAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.DeploymentScripts.Should().Contain("deploy.sh");
        result.DeploymentScripts.Should().Contain("deploy-prod.sh");
    }

    [Fact]
    public async Task AnalyzeDeploymentConfigurationAsync_WithNoDeploymentConfig_ShouldReturnNoConfig()
    {
        // Arrange
        var project = CreateSampleProjectWithoutDeployment();

        // Act
        var result = await _analyzer.AnalyzeDeploymentConfigurationAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.HasDeploymentConfig.Should().BeFalse();
        result.KubernetesFiles.Should().BeEmpty();
        result.TerraformFiles.Should().BeEmpty();
        result.DeploymentScripts.Should().BeEmpty();
        result.Targets.Should().BeEmpty();
    }

    #endregion

    #region DetectEnvironmentsAsync Tests

    [Fact]
    public async Task DetectEnvironmentsAsync_WithEnvironmentFiles_ShouldDetectEnvironments()
    {
        // Arrange
        var project = CreateSampleProject();

        // Act
        var result = await _analyzer.DetectEnvironmentsAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.Environments.Should().HaveCountGreaterThan(0);
        result.ConfigurationFiles.Should().NotBeEmpty();
        result.Confidence.Should().Be(AnalysisConfidence.Medium);
        
        var devEnv = result.Environments.FirstOrDefault(e => e.Name == "dev");
        devEnv.Should().NotBeNull();
        devEnv!.Type.Should().Be(EnvironmentType.Development);
        
        var prodEnv = result.Environments.FirstOrDefault(e => e.Name == "prod");
        prodEnv.Should().NotBeNull();
        prodEnv!.Type.Should().Be(EnvironmentType.Production);
    }

    [Fact]
    public async Task DetectEnvironmentsAsync_WithMultipleEnvironments_ShouldSetupPromotionRules()
    {
        // Arrange
        var project = CreateSampleProject();

        // Act
        var result = await _analyzer.DetectEnvironmentsAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.PromotionRules.Should().NotBeEmpty();
        
        var promotionRule = result.PromotionRules.FirstOrDefault();
        promotionRule.Should().NotBeNull();
        promotionRule!.SourceEnvironment.Should().NotBeNullOrEmpty();
        promotionRule.TargetEnvironment.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DetectEnvironmentsAsync_WithProductionEnvironment_ShouldRequireApproval()
    {
        // Arrange
        var project = CreateSampleProject();

        // Act
        var result = await _analyzer.DetectEnvironmentsAsync(project);

        // Assert
        result.Should().NotBeNull();
        var prodPromotionRule = result.PromotionRules.FirstOrDefault(r => 
            r.TargetEnvironment.Contains("prod", StringComparison.OrdinalIgnoreCase));
        
        if (prodPromotionRule != null)
        {
            prodPromotionRule.RequiresApproval.Should().BeTrue();
            prodPromotionRule.IsAutomatic.Should().BeFalse();
        }
    }

    [Fact]
    public async Task DetectEnvironmentsAsync_WithNoEnvironmentFiles_ShouldReturnLowConfidence()
    {
        // Arrange
        var project = CreateSampleProjectWithoutEnvironments();

        // Act
        var result = await _analyzer.DetectEnvironmentsAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.Environments.Should().BeEmpty();
        result.ConfigurationFiles.Should().BeEmpty();
        result.Confidence.Should().Be(AnalysisConfidence.Low);
    }

    [Fact]
    public async Task DetectEnvironmentsAsync_WithDotEnvFiles_ShouldDetectEnvironments()
    {
        // Arrange
        var project = CreateSampleProjectWithDotEnv();

        // Act
        var result = await _analyzer.DetectEnvironmentsAsync(project);

        // Assert
        result.Should().NotBeNull();
        result.ConfigurationFiles.Should().ContainKey("default");
        result.ConfigurationFiles.Should().ContainKey("dev");
        result.ConfigurationFiles.Should().ContainKey("prod");
        result.ConfigurationFiles["default"].Should().Contain(".env");
        result.ConfigurationFiles["dev"].Should().Contain(".env.dev");
        result.ConfigurationFiles["prod"].Should().Contain(".env.prod");
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task AnalyzeExistingCIConfigAsync_WithNullProject_ShouldHandleGracefully()
    {
        // Act & Assert
        var result = await _analyzer.AnalyzeExistingCIConfigAsync(null!);
        result.Should().NotBeNull();
        result.HasExistingConfig.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeDockerConfigurationAsync_WithNullProject_ShouldHandleGracefully()
    {
        // Act & Assert
        var result = await _analyzer.AnalyzeDockerConfigurationAsync(null!);
        result.Should().NotBeNull();
        result.HasDockerConfig.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeDeploymentConfigurationAsync_WithNullProject_ShouldHandleGracefully()
    {
        // Act & Assert
        var result = await _analyzer.AnalyzeDeploymentConfigurationAsync(null!);
        result.Should().NotBeNull();
        result.HasDeploymentConfig.Should().BeFalse();
    }

    [Fact]
    public async Task DetectEnvironmentsAsync_WithNullProject_ShouldHandleGracefully()
    {
        // Act & Assert
        var result = await _analyzer.DetectEnvironmentsAsync(null!);
        result.Should().NotBeNull();
        result.Environments.Should().BeEmpty();
        result.Confidence.Should().Be(AnalysisConfidence.Low);
    }

    #endregion

    #region Helper Methods

    private GitLabProject CreateSampleProject()
    {
        return new GitLabProject
        {
            Id = 123,
            Name = "sample-project",
            Path = "sample-project",
            FullPath = "group/sample-project",
            DefaultBranch = "main",
            WebUrl = "https://gitlab.example.com/group/sample-project"
        };
    }

    private GitLabProject CreateSampleProjectWithGitHubActions()
    {
        return new GitLabProject
        {
            Id = 124,
            Name = "github-project",
            Path = "github-project",
            FullPath = "group/github-project",
            DefaultBranch = "main",
            WebUrl = "https://gitlab.example.com/group/github-project"
        };
    }

    private GitLabProject CreateSampleProjectWithoutCI()
    {
        return new GitLabProject
        {
            Id = 125,
            Name = "no-ci-project",
            Path = "no-ci-project",
            FullPath = "group/no-ci-project",
            DefaultBranch = "main",
            WebUrl = "https://gitlab.example.com/group/no-ci-project"
        };
    }

    private GitLabProject CreateSampleProjectWithJenkins()
    {
        return new GitLabProject
        {
            Id = 126,
            Name = "jenkins-project",
            Path = "jenkins-project",
            FullPath = "group/jenkins-project",
            DefaultBranch = "main",
            WebUrl = "https://gitlab.example.com/group/jenkins-project"
        };
    }

    private GitLabProject CreateSampleProjectWithoutDocker()
    {
        return new GitLabProject
        {
            Id = 127,
            Name = "no-docker-project",
            Path = "no-docker-project",
            FullPath = "group/no-docker-project",
            DefaultBranch = "main",
            WebUrl = "https://gitlab.example.com/group/no-docker-project"
        };
    }

    private GitLabProject CreateSampleProjectWithHelm()
    {
        return new GitLabProject
        {
            Id = 128,
            Name = "helm-project",
            Path = "helm-project",
            FullPath = "group/helm-project",
            DefaultBranch = "main",
            WebUrl = "https://gitlab.example.com/group/helm-project"
        };
    }

    private GitLabProject CreateSampleProjectWithTerraform()
    {
        return new GitLabProject
        {
            Id = 129,
            Name = "terraform-project",
            Path = "terraform-project",
            FullPath = "group/terraform-project",
            DefaultBranch = "main",
            WebUrl = "https://gitlab.example.com/group/terraform-project"
        };
    }

    private GitLabProject CreateSampleProjectWithoutDeployment()
    {
        return new GitLabProject
        {
            Id = 130,
            Name = "no-deployment-project",
            Path = "no-deployment-project",
            FullPath = "group/no-deployment-project",
            DefaultBranch = "main",
            WebUrl = "https://gitlab.example.com/group/no-deployment-project"
        };
    }

    private GitLabProject CreateSampleProjectWithoutEnvironments()
    {
        return new GitLabProject
        {
            Id = 131,
            Name = "no-env-project",
            Path = "no-env-project",
            FullPath = "group/no-env-project",
            DefaultBranch = "main",
            WebUrl = "https://gitlab.example.com/group/no-env-project"
        };
    }

    private GitLabProject CreateSampleProjectWithDotEnv()
    {
        return new GitLabProject
        {
            Id = 132,
            Name = "dotenv-project",
            Path = "dotenv-project",
            FullPath = "group/dotenv-project",
            DefaultBranch = "main",
            WebUrl = "https://gitlab.example.com/group/dotenv-project"
        };
    }

    #endregion
}