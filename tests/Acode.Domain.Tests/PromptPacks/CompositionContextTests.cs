using Acode.Domain.PromptPacks;
using FluentAssertions;

namespace Acode.Domain.Tests.PromptPacks;

/// <summary>
/// Tests for CompositionContext domain model.
/// </summary>
public class CompositionContextTests
{
    [Fact]
    public void Should_Create_Empty_Context_With_Defaults()
    {
        // Act
        var context = new CompositionContext();

        // Assert
        context.Role.Should().BeNull();
        context.Language.Should().BeNull();
        context.Framework.Should().BeNull();
        context.Variables.Should().BeEmpty();
        context.ConfigVariables.Should().BeEmpty();
        context.EnvironmentVariables.Should().BeEmpty();
        context.ContextVariables.Should().BeEmpty();
        context.DefaultVariables.Should().BeEmpty();
    }

    [Fact]
    public void Should_Set_Role_Language_Framework()
    {
        // Act
        var context = new CompositionContext
        {
            Role = "coder",
            Language = "csharp",
            Framework = "aspnetcore"
        };

        // Assert
        context.Role.Should().Be("coder");
        context.Language.Should().Be("csharp");
        context.Framework.Should().Be("aspnetcore");
    }

    [Fact]
    public void Should_Set_Variables_Dictionary()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            ["workspace_name"] = "MyProject",
            ["team_name"] = "Backend"
        };

        // Act
        var context = new CompositionContext
        {
            Variables = variables
        };

        // Assert
        context.Variables.Should().HaveCount(2);
        context.Variables["workspace_name"].Should().Be("MyProject");
        context.Variables["team_name"].Should().Be("Backend");
    }

    [Fact]
    public void Should_Set_All_Variable_Sources()
    {
        // Arrange
        var configVars = new Dictionary<string, string> { ["key1"] = "config" };
        var envVars = new Dictionary<string, string> { ["key2"] = "env" };
        var contextVars = new Dictionary<string, string> { ["key3"] = "context" };
        var defaultVars = new Dictionary<string, string> { ["key4"] = "default" };

        // Act
        var context = new CompositionContext
        {
            ConfigVariables = configVars,
            EnvironmentVariables = envVars,
            ContextVariables = contextVars,
            DefaultVariables = defaultVars
        };

        // Assert
        context.ConfigVariables["key1"].Should().Be("config");
        context.EnvironmentVariables["key2"].Should().Be("env");
        context.ContextVariables["key3"].Should().Be("context");
        context.DefaultVariables["key4"].Should().Be("default");
    }

    [Fact]
    public void Should_Be_Immutable_After_Creation()
    {
        // Arrange
        var context = new CompositionContext
        {
            Role = "coder",
            Language = "csharp"
        };

        // Assert - with expressions create new instances, original unchanged
        var newContext = context with { Role = "planner" };
        context.Role.Should().Be("coder");
        newContext.Role.Should().Be("planner");
    }
}
