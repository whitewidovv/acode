using Acode.Infrastructure.PromptPacks;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="TemplateEngine"/>.
/// </summary>
public class TemplateEngineTests
{
    [Fact]
    public void Substitute_SingleVariable_ReplacesCorrectly()
    {
        // Arrange
        var engine = new TemplateEngine();
        var templateText = "Hello {{name}}!";
        var variables = new Dictionary<string, string>
        {
            ["name"] = "World",
        };

        // Act
        var result = engine.Substitute(templateText, variables);

        // Assert
        result.Should().Be("Hello World!");
    }

    [Fact]
    public void Substitute_MultipleVariables_ReplacesAll()
    {
        // Arrange
        var engine = new TemplateEngine();
        var templateText = "{{greeting}} {{name}}, today is {{day}}!";
        var variables = new Dictionary<string, string>
        {
            ["greeting"] = "Hello",
            ["name"] = "Alice",
            ["day"] = "Monday",
        };

        // Act
        var result = engine.Substitute(templateText, variables);

        // Assert
        result.Should().Be("Hello Alice, today is Monday!");
    }

    [Fact]
    public void Substitute_MissingVariable_BecomesEmptyString()
    {
        // Arrange
        var engine = new TemplateEngine();
        var templateText = "Hello {{name}}, your role is {{role}}.";
        var variables = new Dictionary<string, string>
        {
            ["name"] = "Bob",
        };

        // Act
        var result = engine.Substitute(templateText, variables);

        // Assert
        result.Should().Be("Hello Bob, your role is .");
    }

    [Fact]
    public void Substitute_VariableValueTooLong_ThrowsArgumentException()
    {
        // Arrange
        var engine = new TemplateEngine();
        var templateText = "Content: {{data}}";
        var longValue = new string('a', 1025); // Exceeds 1024 char limit
        var variables = new Dictionary<string, string>
        {
            ["data"] = longValue,
        };

        // Act
        var act = () => engine.Substitute(templateText, variables);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*exceeds maximum length*");
    }

    [Fact]
    public void Substitute_NoVariables_ReturnsOriginalText()
    {
        // Arrange
        var engine = new TemplateEngine();
        var templateText = "This is plain text without any variables.";
        var variables = new Dictionary<string, string>();

        // Act
        var result = engine.Substitute(templateText, variables);

        // Assert
        result.Should().Be(templateText);
    }

    [Fact]
    public void Substitute_SameVariableMultipleTimes_ReplacesAllOccurrences()
    {
        // Arrange
        var engine = new TemplateEngine();
        var templateText = "{{name}} said {{name}} likes {{name}}.";
        var variables = new Dictionary<string, string>
        {
            ["name"] = "Charlie",
        };

        // Act
        var result = engine.Substitute(templateText, variables);

        // Assert
        result.Should().Be("Charlie said Charlie likes Charlie.");
    }

    [Fact]
    public void Substitute_EmptyTemplateText_ReturnsEmpty()
    {
        // Arrange
        var engine = new TemplateEngine();
        var variables = new Dictionary<string, string>
        {
            ["name"] = "Test",
        };

        // Act
        var result = engine.Substitute(string.Empty, variables);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Substitute_RecursiveExpansion_DetectsAndThrows()
    {
        // Arrange
        var engine = new TemplateEngine();
        var templateText = "Value: {{var1}}";
        var variables = new Dictionary<string, string>
        {
            ["var1"] = "{{var2}}",
            ["var2"] = "{{var3}}",
            ["var3"] = "{{var4}}",
            ["var4"] = "final",
        };

        // Act
        var act = () => engine.Substitute(templateText, variables);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*recursive*expansion*");
    }

    [Fact]
    public void Substitute_VariableWithUnderscores_ReplacesCorrectly()
    {
        // Arrange
        var engine = new TemplateEngine();
        var templateText = "Workspace: {{workspace_name}}, Lang: {{current_language}}";
        var variables = new Dictionary<string, string>
        {
            ["workspace_name"] = "MyProject",
            ["current_language"] = "csharp",
        };

        // Act
        var result = engine.Substitute(templateText, variables);

        // Assert
        result.Should().Be("Workspace: MyProject, Lang: csharp");
    }

    [Fact]
    public void ValidateTemplate_ValidTemplate_ReturnsSuccess()
    {
        // Arrange
        var engine = new TemplateEngine();
        var templateText = "Hello {{name}}, welcome to {{workspace_name}}!";

        // Act
        var result = engine.ValidateTemplate(templateText);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateTemplate_UnclosedBraces_ReturnsError()
    {
        // Arrange
        var engine = new TemplateEngine();
        var templateText = "Hello {{name, missing closing braces!";

        // Act
        var result = engine.ValidateTemplate(templateText);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Message.Should().Contain("unclosed");
    }

    [Fact]
    public void ValidateTemplate_InvalidVariableName_ReturnsError()
    {
        // Arrange
        var engine = new TemplateEngine();
        var templateText = "Hello {{name with spaces}}!";

        // Act
        var result = engine.ValidateTemplate(templateText);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Message.Should().Contain("invalid variable name");
    }

    [Fact]
    public void ValidateTemplate_EmptyVariableName_ReturnsError()
    {
        // Arrange
        var engine = new TemplateEngine();
        var templateText = "Value: {{}}";

        // Act
        var result = engine.ValidateTemplate(templateText);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}
