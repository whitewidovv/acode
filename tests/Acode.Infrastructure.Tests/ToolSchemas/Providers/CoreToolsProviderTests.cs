namespace Acode.Infrastructure.Tests.ToolSchemas.Providers;

using Acode.Application.Tools;
using Acode.Infrastructure.ToolSchemas.Providers;
using FluentAssertions;

/// <summary>
/// Tests for the CoreToolsProvider which provides all 17 core tool schemas.
/// </summary>
public sealed class CoreToolsProviderTests
{
    private readonly CoreToolsProvider provider = new();

    [Fact]
    public void Name_ShouldBeCoreTools()
    {
        // Act & Assert
        provider.Name.Should().Be("CoreTools");
    }

    [Fact]
    public void Version_ShouldBe1_0_0()
    {
        // Act & Assert
        provider.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void Order_ShouldBeZero_CoreToolsLoadFirst()
    {
        // Act & Assert
        provider.Order.Should().Be(0);
    }

    [Fact]
    public void GetToolDefinitions_ShouldReturn17Tools()
    {
        // Act
        var tools = provider.GetToolDefinitions().ToList();

        // Assert
        tools.Should().HaveCount(17);
    }

    [Fact]
    public void GetToolDefinitions_ShouldReturnAllToolsWithUniqueNames()
    {
        // Act
        var tools = provider.GetToolDefinitions().ToList();
        var names = tools.Select(t => t.Name).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GetToolDefinitions_ShouldIncludeAllFileOperationTools()
    {
        // Act
        var toolNames = provider.GetToolDefinitions().Select(t => t.Name).ToList();

        // Assert - File Operations (6 tools)
        toolNames.Should().Contain("read_file");
        toolNames.Should().Contain("write_file");
        toolNames.Should().Contain("list_directory");
        toolNames.Should().Contain("search_files");
        toolNames.Should().Contain("delete_file");
        toolNames.Should().Contain("move_file");
    }

    [Fact]
    public void GetToolDefinitions_ShouldIncludeAllCodeExecutionTools()
    {
        // Act
        var toolNames = provider.GetToolDefinitions().Select(t => t.Name).ToList();

        // Assert - Code Execution (2 tools)
        toolNames.Should().Contain("execute_command");
        toolNames.Should().Contain("execute_script");
    }

    [Fact]
    public void GetToolDefinitions_ShouldIncludeAllCodeAnalysisTools()
    {
        // Act
        var toolNames = provider.GetToolDefinitions().Select(t => t.Name).ToList();

        // Assert - Code Analysis (3 tools)
        toolNames.Should().Contain("semantic_search");
        toolNames.Should().Contain("find_symbol");
        toolNames.Should().Contain("get_definition");
    }

    [Fact]
    public void GetToolDefinitions_ShouldIncludeAllVersionControlTools()
    {
        // Act
        var toolNames = provider.GetToolDefinitions().Select(t => t.Name).ToList();

        // Assert - Version Control (4 tools)
        toolNames.Should().Contain("git_status");
        toolNames.Should().Contain("git_diff");
        toolNames.Should().Contain("git_log");
        toolNames.Should().Contain("git_commit");
    }

    [Fact]
    public void GetToolDefinitions_ShouldIncludeAllUserInteractionTools()
    {
        // Act
        var toolNames = provider.GetToolDefinitions().Select(t => t.Name).ToList();

        // Assert - User Interaction (2 tools)
        toolNames.Should().Contain("ask_user");
        toolNames.Should().Contain("confirm_action");
    }

    [Fact]
    public void GetToolDefinitions_AllToolsShouldHaveNonEmptyDescriptions()
    {
        // Act
        var tools = provider.GetToolDefinitions().ToList();

        // Assert
        foreach (var tool in tools)
        {
            tool.Description.Should().NotBeNullOrWhiteSpace($"Tool '{tool.Name}' should have a description");
        }
    }

    [Fact]
    public void GetToolDefinitions_AllToolsShouldHaveValidParametersSchema()
    {
        // Act
        var tools = provider.GetToolDefinitions().ToList();

        // Assert
        foreach (var tool in tools)
        {
            tool.Parameters.ValueKind.Should().Be(
                System.Text.Json.JsonValueKind.Object,
                $"Tool '{tool.Name}' parameters should be a JSON object");

            // Should have 'type', 'properties', and 'required' at minimum
            tool.Parameters.TryGetProperty("type", out var typeProperty).Should().BeTrue(
                $"Tool '{tool.Name}' schema should have 'type' property");
            typeProperty.GetString().Should().Be("object");
        }
    }

    [Fact]
    public void Provider_ShouldImplementIToolSchemaProvider()
    {
        // Assert
        provider.Should().BeAssignableTo<IToolSchemaProvider>();
    }

    [Theory]
    [InlineData("read_file", "path")]
    [InlineData("write_file", "path", "content")]
    [InlineData("delete_file", "path", "confirm")]
    [InlineData("move_file", "source", "destination")]
    [InlineData("execute_command", "command")]
    [InlineData("git_commit", "message")]
    [InlineData("ask_user", "question")]
    [InlineData("confirm_action", "action")]
    public void Tool_ShouldHaveRequiredFields(string toolName, params string[] expectedRequired)
    {
        // Arrange
        var tools = provider.GetToolDefinitions().ToDictionary(t => t.Name);

        // Act
        var tool = tools[toolName];
        var required = tool.Parameters.GetProperty("required");
        var requiredFields = required.EnumerateArray().Select(e => e.GetString()).ToList();

        // Assert
        requiredFields.Should().BeEquivalentTo(expectedRequired);
    }
}
