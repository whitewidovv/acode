namespace Acode.Infrastructure.Tests.ToolSchemas.Providers.Schemas;

using Acode.Infrastructure.ToolSchemas.Providers;
using FluentAssertions;

/// <summary>
/// Tests for user interaction tool schemas (ask_user, confirm_action).
/// </summary>
public sealed class UserInteractionSchemaTests
{
    private readonly CoreToolsProvider provider = new();

    [Fact]
    public void AskUser_Question_ShouldHaveCorrectConstraints()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["ask_user"].Parameters;

        var properties = schema.GetProperty("properties");
        var question = properties.GetProperty("question");

        question.GetProperty("type").GetString().Should().Be("string");
        question.GetProperty("minLength").GetInt32().Should().Be(5);
        question.GetProperty("maxLength").GetInt32().Should().Be(1000);
    }

    [Fact]
    public void AskUser_Options_ShouldHaveArrayConstraints()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["ask_user"].Parameters;

        var properties = schema.GetProperty("properties");
        var options = properties.GetProperty("options");

        options.GetProperty("type").GetString().Should().Be("array");
        options.GetProperty("minItems").GetInt32().Should().Be(2);
        options.GetProperty("maxItems").GetInt32().Should().Be(10);
    }

    [Fact]
    public void AskUser_TimeoutSeconds_ShouldHaveBoundsAndDefault()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["ask_user"].Parameters;

        var properties = schema.GetProperty("properties");
        var timeout = properties.GetProperty("timeout_seconds");

        timeout.GetProperty("minimum").GetInt32().Should().Be(10);
        timeout.GetProperty("maximum").GetInt32().Should().Be(3600);
        timeout.GetProperty("default").GetInt32().Should().Be(300);
    }

    [Fact]
    public void AskUser_DefaultOption_ShouldHaveMaxLength200()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["ask_user"].Parameters;

        var properties = schema.GetProperty("properties");
        var defaultOption = properties.GetProperty("default_option");

        defaultOption.GetProperty("maxLength").GetInt32().Should().Be(200);
    }

    [Fact]
    public void AskUser_RequiredFields_ShouldOnlyBeQuestion()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["ask_user"].Parameters;

        var required = schema.GetProperty("required");
        var requiredFields = required.EnumerateArray().Select(e => e.GetString()).ToList();

        requiredFields.Should().BeEquivalentTo(new[] { "question" });
    }

    [Fact]
    public void ConfirmAction_Action_ShouldHaveMinLength10()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["confirm_action"].Parameters;

        var properties = schema.GetProperty("properties");
        var action = properties.GetProperty("action");

        action.GetProperty("type").GetString().Should().Be("string");
        action.GetProperty("minLength").GetInt32().Should().Be(10);
        action.GetProperty("maxLength").GetInt32().Should().Be(500);
    }

    [Fact]
    public void ConfirmAction_Destructive_ShouldDefaultToFalse()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["confirm_action"].Parameters;

        var properties = schema.GetProperty("properties");
        var destructive = properties.GetProperty("destructive");

        destructive.GetProperty("default").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void ConfirmAction_DefaultConfirm_ShouldDefaultToFalse()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["confirm_action"].Parameters;

        var properties = schema.GetProperty("properties");
        var defaultConfirm = properties.GetProperty("default_confirm");

        defaultConfirm.GetProperty("default").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void ConfirmAction_TimeoutSeconds_ShouldHaveBoundsAndDefault()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["confirm_action"].Parameters;

        var properties = schema.GetProperty("properties");
        var timeout = properties.GetProperty("timeout_seconds");

        timeout.GetProperty("minimum").GetInt32().Should().Be(10);
        timeout.GetProperty("maximum").GetInt32().Should().Be(600);
        timeout.GetProperty("default").GetInt32().Should().Be(60);
    }

    [Fact]
    public void ConfirmAction_RequiredFields_ShouldOnlyBeAction()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["confirm_action"].Parameters;

        var required = schema.GetProperty("required");
        var requiredFields = required.EnumerateArray().Select(e => e.GetString()).ToList();

        requiredFields.Should().BeEquivalentTo(new[] { "action" });
    }
}
