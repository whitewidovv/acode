using Acode.Domain.PromptPacks;
using FluentAssertions;

namespace Acode.Domain.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="CompositionContext"/>.
/// </summary>
public class CompositionContextTests
{
    [Fact]
    public void Constructor_WithAllParameters_SetsProperties()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            ["workspace_name"] = "MyProject",
            ["language"] = "csharp",
        };

        // Act
        var context = new CompositionContext(
            Role: "coder",
            Language: "csharp",
            Framework: "aspnetcore",
            Variables: variables);

        // Assert
        context.Role.Should().Be("coder");
        context.Language.Should().Be("csharp");
        context.Framework.Should().Be("aspnetcore");
        context.Variables.Should().BeSameAs(variables);
    }

    [Fact]
    public void Constructor_WithNullParameters_AllowsNulls()
    {
        // Act
        var context = new CompositionContext();

        // Assert
        context.Role.Should().BeNull();
        context.Language.Should().BeNull();
        context.Framework.Should().BeNull();
        context.Variables.Should().BeNull();
    }

    [Fact]
    public void VariablesOrEmpty_WithVariables_ReturnsVariables()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            ["key"] = "value",
        };
        var context = new CompositionContext(Variables: variables);

        // Act
        var result = context.VariablesOrEmpty;

        // Assert
        result.Should().BeSameAs(variables);
    }

    [Fact]
    public void VariablesOrEmpty_WithNullVariables_ReturnsEmptyDictionary()
    {
        // Arrange
        var context = new CompositionContext();

        // Act
        var result = context.VariablesOrEmpty;

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void WithVariables_CreatesContextWithVariables()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            ["name"] = "Test",
        };

        // Act
        var context = CompositionContext.WithVariables(variables);

        // Assert
        context.Variables.Should().BeSameAs(variables);
        context.Role.Should().BeNull();
        context.Language.Should().BeNull();
        context.Framework.Should().BeNull();
    }

    [Fact]
    public void ForRole_WithValidRole_CreatesContextWithRole()
    {
        // Act
        var context = CompositionContext.ForRole("planner");

        // Assert
        context.Role.Should().Be("planner");
        context.Language.Should().BeNull();
        context.Framework.Should().BeNull();
        context.Variables.Should().BeNull();
    }

    [Fact]
    public void ForRole_WithVariables_CreatesContextWithRoleAndVariables()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            ["workspace_name"] = "MyProject",
        };

        // Act
        var context = CompositionContext.ForRole("coder", variables);

        // Assert
        context.Role.Should().Be("coder");
        context.Variables.Should().BeSameAs(variables);
    }

    [Fact]
    public void ForRole_WithNullRole_ThrowsArgumentException()
    {
        // Act
        var act = () => CompositionContext.ForRole(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ForRole_WithEmptyRole_ThrowsArgumentException()
    {
        // Act
        var act = () => CompositionContext.ForRole(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ForTechnology_WithLanguage_CreatesContextWithLanguage()
    {
        // Act
        var context = CompositionContext.ForTechnology("typescript");

        // Assert
        context.Language.Should().Be("typescript");
        context.Framework.Should().BeNull();
        context.Role.Should().BeNull();
    }

    [Fact]
    public void ForTechnology_WithLanguageAndFramework_CreatesContextWithBoth()
    {
        // Act
        var context = CompositionContext.ForTechnology("typescript", "react");

        // Assert
        context.Language.Should().Be("typescript");
        context.Framework.Should().Be("react");
    }

    [Fact]
    public void ForTechnology_WithAllParameters_CreatesCompleteContext()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            ["project_name"] = "WebApp",
        };

        // Act
        var context = CompositionContext.ForTechnology("csharp", "aspnetcore", variables);

        // Assert
        context.Language.Should().Be("csharp");
        context.Framework.Should().Be("aspnetcore");
        context.Variables.Should().BeSameAs(variables);
    }

    [Fact]
    public void ForTechnology_WithNullLanguage_ThrowsArgumentException()
    {
        // Act
        var act = () => CompositionContext.ForTechnology(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ForTechnology_WithEmptyLanguage_ThrowsArgumentException()
    {
        // Act
        var act = () => CompositionContext.ForTechnology(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordEquality_WithSameValues_AreEqual()
    {
        // Arrange
        var variables = new Dictionary<string, string> { ["key"] = "value" };
        var context1 = new CompositionContext("coder", "csharp", "aspnetcore", variables);
        var context2 = new CompositionContext("coder", "csharp", "aspnetcore", variables);

        // Act & Assert
        context1.Should().Be(context2);
        (context1 == context2).Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_WithDifferentValues_AreNotEqual()
    {
        // Arrange
        var context1 = new CompositionContext("coder");
        var context2 = new CompositionContext("planner");

        // Act & Assert
        context1.Should().NotBe(context2);
        (context1 != context2).Should().BeTrue();
    }
}
