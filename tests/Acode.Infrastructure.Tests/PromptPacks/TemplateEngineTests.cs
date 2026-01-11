using Acode.Domain.PromptPacks;
using Acode.Domain.PromptPacks.Exceptions;
using Acode.Infrastructure.PromptPacks;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.PromptPacks;

/// <summary>
/// Tests for TemplateEngine implementation.
/// Tests from Task 008 spec lines 1079-1250.
/// </summary>
public class TemplateEngineTests
{
    [Fact]
    public void Should_Substitute_Single_Variable()
    {
        // Arrange
        var content = "Working on {{workspace_name}} project";
        var context = new CompositionContext
        {
            Variables = new Dictionary<string, string>
            {
                ["workspace_name"] = "AgenticCoder"
            }
        };
        var engine = new TemplateEngine();

        // Act
        var result = engine.Substitute(content, context);

        // Assert
        result.Should().Be("Working on AgenticCoder project");
    }

    [Fact]
    public void Should_Substitute_Multiple_Variables()
    {
        // Arrange
        var content = "Project {{workspace_name}} uses {{language}} with {{framework}}";
        var context = new CompositionContext
        {
            Variables = new Dictionary<string, string>
            {
                ["workspace_name"] = "MyApp",
                ["language"] = "csharp",
                ["framework"] = "aspnetcore"
            }
        };
        var engine = new TemplateEngine();

        // Act
        var result = engine.Substitute(content, context);

        // Assert
        result.Should().Be("Project MyApp uses csharp with aspnetcore");
    }

    [Fact]
    public void Should_Replace_Missing_Variable_With_Empty_String()
    {
        // Arrange
        var content = "Language: {{language}}, Framework: {{framework}}";
        var context = new CompositionContext
        {
            Variables = new Dictionary<string, string>
            {
                ["language"] = "typescript"
            }
        };
        var engine = new TemplateEngine();

        // Act
        var result = engine.Substitute(content, context);

        // Assert
        result.Should().Be("Language: typescript, Framework: ");
    }

    [Fact]
    public void Should_Escape_Special_Characters_In_Variable_Values()
    {
        // Arrange
        var content = "Description: {{description}}";
        var context = new CompositionContext
        {
            Variables = new Dictionary<string, string>
            {
                ["description"] = "Use <script>alert('xss')</script> carefully"
            }
        };
        var engine = new TemplateEngine();

        // Act
        var result = engine.Substitute(content, context);

        // Assert
        result.Should().Be("Description: Use &lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt; carefully");
    }

    [Fact]
    public void Should_Reject_Variable_Value_Exceeding_Maximum_Length()
    {
        // Arrange
        var content = "Value: {{long_value}}";
        var longValue = new string('x', 1025); // Exceeds 1024 limit
        var context = new CompositionContext
        {
            Variables = new Dictionary<string, string>
            {
                ["long_value"] = longValue
            }
        };
        var engine = new TemplateEngine();

        // Act & Assert
        var act = () => engine.Substitute(content, context);
        act.Should().Throw<TemplateVariableException>()
           .WithMessage("*exceeds maximum length*");
    }

    [Fact]
    public void Should_Handle_Variable_Resolution_Priority()
    {
        // Arrange
        var content = "Value: {{custom_var}}";
        var context = new CompositionContext
        {
            // Priority: config > environment > context > defaults
            ConfigVariables = new Dictionary<string, string> { ["custom_var"] = "from_config" },
            EnvironmentVariables = new Dictionary<string, string> { ["custom_var"] = "from_env" },
            ContextVariables = new Dictionary<string, string> { ["custom_var"] = "from_context" },
            DefaultVariables = new Dictionary<string, string> { ["custom_var"] = "from_default" }
        };
        var engine = new TemplateEngine();

        // Act
        var result = engine.Substitute(content, context);

        // Assert
        result.Should().Be("Value: from_config");
    }

    [Fact]
    public void Should_Detect_Recursive_Variable_Expansion()
    {
        // Arrange
        var content = "{{var_a}}";
        var context = new CompositionContext
        {
            Variables = new Dictionary<string, string>
            {
                ["var_a"] = "{{var_b}}",
                ["var_b"] = "{{var_c}}",
                ["var_c"] = "{{var_d}}",
                ["var_d"] = "{{var_a}}" // Circular reference
            }
        };
        var engine = new TemplateEngine(maxExpansionDepth: 3);

        // Act & Assert
        var act = () => engine.Substitute(content, context);
        act.Should().Throw<TemplateVariableException>()
           .WithMessage("*expansion depth limit*");
    }

    [Fact]
    public void Should_Substitute_Variables_In_Multi_Line_Template()
    {
        // Arrange
        var content = @"
# Project: {{workspace_name}}

Language: {{language}}
Framework: {{framework}}
Team: {{team_name}}";

        var context = new CompositionContext
        {
            Variables = new Dictionary<string, string>
            {
                ["workspace_name"] = "PaymentService",
                ["language"] = "go",
                ["framework"] = "gin",
                ["team_name"] = "Backend Team"
            }
        };
        var engine = new TemplateEngine();

        // Act
        var result = engine.Substitute(content, context);

        // Assert
        result.Should().Contain("Project: PaymentService");
        result.Should().Contain("Language: go");
        result.Should().Contain("Framework: gin");
        result.Should().Contain("Team: Backend Team");
    }

    [Fact]
    public void Should_Handle_Null_Context()
    {
        // Arrange
        var content = "Test {{var}}";
        var engine = new TemplateEngine();

        // Act & Assert
        var act = () => engine.Substitute(content, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Should_Return_Original_When_No_Variables()
    {
        // Arrange
        var content = "No variables here";
        var context = new CompositionContext();
        var engine = new TemplateEngine();

        // Act
        var result = engine.Substitute(content, context);

        // Assert
        result.Should().Be("No variables here");
    }

    [Fact]
    public void Should_Handle_Empty_Content()
    {
        // Arrange
        var engine = new TemplateEngine();
        var context = new CompositionContext();

        // Act
        var result = engine.Substitute(string.Empty, context);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Should_Handle_Null_Content()
    {
        // Arrange
        var engine = new TemplateEngine();
        var context = new CompositionContext();

        // Act
        var result = engine.Substitute(null!, context);

        // Assert
        result.Should().BeNull();
    }
}
