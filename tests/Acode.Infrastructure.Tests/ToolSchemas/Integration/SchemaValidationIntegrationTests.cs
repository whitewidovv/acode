namespace Acode.Infrastructure.Tests.ToolSchemas.Integration;

using System.Diagnostics;
using Acode.Application.Tools;
using Acode.Infrastructure.ToolSchemas.Providers;
using FluentAssertions;

/// <summary>
/// Integration tests for schema registration and validation.
/// </summary>
public sealed class SchemaValidationIntegrationTests
{
    private readonly CoreToolsProvider provider = new();

    [Fact]
    public void Should_Register_All_17_Core_Tools()
    {
        var tools = this.provider.GetToolDefinitions().ToList();

        tools.Should().HaveCount(17, "CoreToolsProvider should register exactly 17 tools");

        var toolNames = tools.Select(t => t.Name).ToList();

        // File Operations (6)
        toolNames.Should().Contain("read_file");
        toolNames.Should().Contain("write_file");
        toolNames.Should().Contain("list_directory");
        toolNames.Should().Contain("search_files");
        toolNames.Should().Contain("delete_file");
        toolNames.Should().Contain("move_file");

        // Code Execution (2)
        toolNames.Should().Contain("execute_command");
        toolNames.Should().Contain("execute_script");

        // Code Analysis (3)
        toolNames.Should().Contain("semantic_search");
        toolNames.Should().Contain("find_symbol");
        toolNames.Should().Contain("get_definition");

        // Version Control (4)
        toolNames.Should().Contain("git_status");
        toolNames.Should().Contain("git_diff");
        toolNames.Should().Contain("git_log");
        toolNames.Should().Contain("git_commit");

        // User Interaction (2)
        toolNames.Should().Contain("ask_user");
        toolNames.Should().Contain("confirm_action");
    }

    [Fact]
    public void Should_Categorize_Tools_By_Type()
    {
        var tools = this.provider.GetToolDefinitions().ToList();

        // Group by common prefixes/patterns
        var fileOps = tools.Where(t =>
            t.Name is "read_file" or "write_file" or "list_directory" or "search_files" or "delete_file" or "move_file")
            .ToList();
        var codeExec = tools.Where(t => t.Name.StartsWith("execute", StringComparison.Ordinal)).ToList();
        var codeAnalysis = tools.Where(t =>
            t.Name is "semantic_search" or "find_symbol" or "get_definition").ToList();
        var versionControl = tools.Where(t => t.Name.StartsWith("git_", StringComparison.Ordinal)).ToList();
        var userInteraction = tools.Where(t =>
            t.Name is "ask_user" or "confirm_action").ToList();

        fileOps.Should().HaveCount(6, "6 file operation tools expected");
        codeExec.Should().HaveCount(2, "2 code execution tools expected");
        codeAnalysis.Should().HaveCount(3, "3 code analysis tools expected");
        versionControl.Should().HaveCount(4, "4 version control tools expected");
        userInteraction.Should().HaveCount(2, "2 user interaction tools expected");
    }

    [Fact]
    public void All_Tools_Should_Have_Valid_JSON_Schema_Structure()
    {
        var tools = this.provider.GetToolDefinitions().ToList();

        foreach (var tool in tools)
        {
            tool.Parameters.TryGetProperty("type", out var typeProperty).Should().BeTrue(
                $"Tool '{tool.Name}' schema should have 'type' property");
            typeProperty.GetString().Should().Be(
                "object",
                $"Tool '{tool.Name}' schema type should be 'object'");

            tool.Parameters.TryGetProperty("properties", out _).Should().BeTrue(
                $"Tool '{tool.Name}' schema should have 'properties' property");

            tool.Parameters.TryGetProperty("required", out _).Should().BeTrue(
                $"Tool '{tool.Name}' schema should have 'required' property");
        }
    }

    [Fact]
    public void All_Tools_Should_Have_Non_Empty_Descriptions()
    {
        var tools = this.provider.GetToolDefinitions().ToList();

        foreach (var tool in tools)
        {
            tool.Description.Should().NotBeNullOrWhiteSpace(
                $"Tool '{tool.Name}' should have a non-empty description");
            tool.Description.Length.Should().BeGreaterThan(
                10,
                $"Tool '{tool.Name}' description should be meaningful (>10 chars)");
        }
    }

    [Fact]
    public void Provider_Should_Implement_IToolSchemaProvider()
    {
        this.provider.Should().BeAssignableTo<IToolSchemaProvider>();
        this.provider.Name.Should().Be("CoreTools");
        this.provider.Version.Should().Be("1.0.0");
        this.provider.Order.Should().Be(0, "CoreTools should load first (order 0)");
    }

    [Fact]
    public void Schema_Compilation_Should_Complete_Under_500ms()
    {
        var stopwatch = Stopwatch.StartNew();

        // Compile all schemas multiple times to get reliable measurement
        for (int i = 0; i < 10; i++)
        {
            var newProvider = new CoreToolsProvider();
            var tools = newProvider.GetToolDefinitions().ToList();
            tools.Should().HaveCount(17);
        }

        stopwatch.Stop();

        // Average per compilation should be <50ms, total <500ms
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(
            500,
            "Compiling all 17 schemas 10 times should complete in <500ms total");
    }

    [Fact]
    public void All_Tool_Names_Should_Use_Snake_Case()
    {
        var tools = this.provider.GetToolDefinitions().ToList();

        foreach (var tool in tools)
        {
            tool.Name.Should().MatchRegex(
                @"^[a-z][a-z0-9_]*$",
                $"Tool '{tool.Name}' should use snake_case naming");
        }
    }

    [Fact]
    public void All_Tool_Names_Should_Be_Unique()
    {
        var tools = this.provider.GetToolDefinitions().ToList();
        var names = tools.Select(t => t.Name).ToList();

        names.Should().OnlyHaveUniqueItems("All tool names must be unique");
    }

    [Fact]
    public void All_Tool_Descriptions_Should_Be_Between_50_And_200_Characters()
    {
        var tools = this.provider.GetToolDefinitions().ToList();

        foreach (var tool in tools)
        {
            tool.Description.Length.Should().BeGreaterThanOrEqualTo(
                50,
                $"Tool '{tool.Name}' description should be at least 50 chars (actual: {tool.Description.Length})");
            tool.Description.Length.Should().BeLessThanOrEqualTo(
                200,
                $"Tool '{tool.Name}' description should be at most 200 chars (actual: {tool.Description.Length})");
        }
    }
}
