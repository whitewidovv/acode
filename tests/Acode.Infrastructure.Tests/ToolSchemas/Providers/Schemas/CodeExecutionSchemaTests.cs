namespace Acode.Infrastructure.Tests.ToolSchemas.Providers.Schemas;

using Acode.Infrastructure.ToolSchemas.Providers;
using FluentAssertions;

/// <summary>
/// Tests for code execution tool schemas (execute_command, execute_script).
/// </summary>
public sealed class CodeExecutionSchemaTests
{
    private readonly CoreToolsProvider provider = new();

    [Fact]
    public void ExecuteCommand_Command_ShouldHaveCorrectConstraints()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["execute_command"].Parameters;

        var properties = schema.GetProperty("properties");
        var command = properties.GetProperty("command");

        command.GetProperty("type").GetString().Should().Be("string");
        command.GetProperty("minLength").GetInt32().Should().Be(1);
        command.GetProperty("maxLength").GetInt32().Should().Be(8192);
    }

    [Fact]
    public void ExecuteCommand_TimeoutSeconds_ShouldHaveCorrectBoundsAndDefault()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["execute_command"].Parameters;

        var properties = schema.GetProperty("properties");
        var timeout = properties.GetProperty("timeout_seconds");

        timeout.GetProperty("minimum").GetInt32().Should().Be(1);
        timeout.GetProperty("maximum").GetInt32().Should().Be(3600);
        timeout.GetProperty("default").GetInt32().Should().Be(120);
    }

    [Fact]
    public void ExecuteCommand_Shell_ShouldHaveCorrectEnumValues()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["execute_command"].Parameters;

        var properties = schema.GetProperty("properties");
        var shell = properties.GetProperty("shell");
        var enumValues = shell.GetProperty("enum")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();

        enumValues.Should().BeEquivalentTo(new[] { "bash", "powershell", "cmd", "sh" });
    }

    [Fact]
    public void ExecuteCommand_CaptureOutput_ShouldDefaultToTrue()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["execute_command"].Parameters;

        var properties = schema.GetProperty("properties");
        var captureOutput = properties.GetProperty("capture_output");

        captureOutput.GetProperty("default").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void ExecuteCommand_RequiredFields_ShouldOnlyBeCommand()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["execute_command"].Parameters;

        var required = schema.GetProperty("required");
        var requiredFields = required.EnumerateArray().Select(e => e.GetString()).ToList();

        requiredFields.Should().BeEquivalentTo(new[] { "command" });
    }

    [Fact]
    public void ExecuteCommand_WorkingDirectory_ShouldHaveMaxLength4096()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["execute_command"].Parameters;

        var properties = schema.GetProperty("properties");
        var workingDir = properties.GetProperty("working_directory");

        workingDir.GetProperty("maxLength").GetInt32().Should().Be(4096);
    }

    [Fact]
    public void ExecuteScript_Script_ShouldHaveCorrectConstraints()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["execute_script"].Parameters;

        var properties = schema.GetProperty("properties");
        var script = properties.GetProperty("script");

        script.GetProperty("type").GetString().Should().Be("string");
        script.GetProperty("minLength").GetInt32().Should().Be(1);
        script.GetProperty("maxLength").GetInt32().Should().Be(65536);
    }

    [Fact]
    public void ExecuteScript_Language_ShouldHaveCorrectEnumValues()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["execute_script"].Parameters;

        var properties = schema.GetProperty("properties");
        var language = properties.GetProperty("language");
        var enumValues = language.GetProperty("enum")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();

        enumValues.Should().BeEquivalentTo(new[] { "python", "bash", "powershell", "node" });
    }

    [Fact]
    public void ExecuteScript_TimeoutSeconds_ShouldHaveCorrectBoundsAndDefault()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["execute_script"].Parameters;

        var properties = schema.GetProperty("properties");
        var timeout = properties.GetProperty("timeout_seconds");

        timeout.GetProperty("minimum").GetInt32().Should().Be(1);
        timeout.GetProperty("maximum").GetInt32().Should().Be(3600);
        timeout.GetProperty("default").GetInt32().Should().Be(120);
    }

    [Fact]
    public void ExecuteScript_CaptureOutput_ShouldDefaultToTrue()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["execute_script"].Parameters;

        var properties = schema.GetProperty("properties");
        var captureOutput = properties.GetProperty("capture_output");

        captureOutput.GetProperty("default").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void ExecuteScript_RequiredFields_ShouldBeScriptAndLanguage()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["execute_script"].Parameters;

        var required = schema.GetProperty("required");
        var requiredFields = required.EnumerateArray().Select(e => e.GetString()).ToList();

        requiredFields.Should().BeEquivalentTo(new[] { "script", "language" });
    }

    [Fact]
    public void ExecuteScript_WorkingDirectory_ShouldHaveMaxLength4096()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["execute_script"].Parameters;

        var properties = schema.GetProperty("properties");
        var workingDir = properties.GetProperty("working_directory");

        workingDir.GetProperty("maxLength").GetInt32().Should().Be(4096);
    }
}
