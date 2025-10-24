using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Tests.Models.GitLab;

public class ProjectVariablesInfoTests
{
    [Fact]
    public void ProjectVariablesInfo_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var variablesInfo = new ProjectVariablesInfo();

        // Assert
        variablesInfo.Variables.Should().NotBeNull();
        variablesInfo.Variables.Should().BeEmpty();
        variablesInfo.TotalVariables.Should().Be(0);
        variablesInfo.ProtectedVariables.Should().Be(0);
        variablesInfo.MaskedVariables.Should().Be(0);
        variablesInfo.EnvironmentScopes.Should().NotBeNull();
        variablesInfo.EnvironmentScopes.Should().BeEmpty();
        variablesInfo.Confidence.Should().Be(AnalysisConfidence.High);
    }

    [Fact]
    public void ProjectVariablesInfo_WithVariables_ShouldCalculateCorrectCounts()
    {
        // Arrange
        var variablesInfo = new ProjectVariablesInfo
        {
            Variables = new List<ProjectVariableInfo>
            {
                new() { Key = "VAR1", Protected = true, Masked = false, EnvironmentScope = "*" },
                new() { Key = "VAR2", Protected = false, Masked = true, EnvironmentScope = "production" },
                new() { Key = "VAR3", Protected = true, Masked = true, EnvironmentScope = "staging" }
            }
        };

        // Act & Assert
        variablesInfo.TotalVariables.Should().Be(3);
        variablesInfo.ProtectedVariables.Should().Be(2);
        variablesInfo.MaskedVariables.Should().Be(2);
        variablesInfo.EnvironmentScopes.Should().HaveCount(3);
        variablesInfo.EnvironmentScopes.Should().Contain("*");
        variablesInfo.EnvironmentScopes.Should().Contain("production");
        variablesInfo.EnvironmentScopes.Should().Contain("staging");
    }
}