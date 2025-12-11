using GitlabPipelineGenerator.GitLabApiClient;
using GitlabPipelineGenerator.GitLabApiClient.Models;
using Xunit;

namespace GitlabPipelineGenerator.Core.Tests.Services;

/// <summary>
/// Tests for project variable copying functionality
/// </summary>
public class ProjectVariableCopyTests
{
    [Fact]
    public void CreateProjectVariableAsync_ShouldHaveCorrectSignature()
    {
        // Arrange
        var client = new GitLabClient("https://gitlab.example.com", "test-token");
        
        // Act & Assert - Verify method exists with correct signature
        var method = typeof(GitLabClient).GetMethod("CreateProjectVariableAsync");
        
        Assert.NotNull(method);
        Assert.Equal(8, method!.GetParameters().Length);
        Assert.Equal("projectIdOrPath", method.GetParameters()[0].Name);
        Assert.Equal("key", method.GetParameters()[1].Name);
        Assert.Equal("value", method.GetParameters()[2].Name);
        Assert.Equal("variableType", method.GetParameters()[3].Name);
        Assert.Equal("protected", method.GetParameters()[4].Name);
        Assert.Equal("masked", method.GetParameters()[5].Name);
        Assert.Equal("environmentScope", method.GetParameters()[6].Name);
        Assert.Equal("description", method.GetParameters()[7].Name);
        
        client.Dispose();
    }
    
    [Fact]
    public void ProjectVariable_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var variable = new ProjectVariable
        {
            Key = "TEST_KEY",
            Value = "test_value",
            VariableType = "env_var",
            Protected = true,
            Masked = false,
            EnvironmentScope = "*",
            Description = "Test variable"
        };
        
        // Assert
        Assert.Equal("TEST_KEY", variable.Key);
        Assert.Equal("test_value", variable.Value);
        Assert.Equal("env_var", variable.VariableType);
        Assert.True(variable.Protected);
        Assert.False(variable.Masked);
        Assert.Equal("*", variable.EnvironmentScope);
        Assert.Equal("Test variable", variable.Description);
    }
}