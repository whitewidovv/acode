namespace Acode.Infrastructure.Tests.ToolSchemas.Providers.Schemas;

using Acode.Infrastructure.ToolSchemas.Providers;
using FluentAssertions;

/// <summary>
/// Tests for version control tool schemas (git_status, git_diff, git_log, git_commit).
/// </summary>
public sealed class VersionControlSchemaTests
{
    private readonly CoreToolsProvider provider = new();

    [Fact]
    public void GitStatus_ShouldHaveNoRequiredFields()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["git_status"].Parameters;

        var required = schema.GetProperty("required");
        var requiredFields = required.EnumerateArray().Select(e => e.GetString()).ToList();

        requiredFields.Should().BeEmpty();
    }

    [Fact]
    public void GitStatus_Path_ShouldHaveDefaultCurrentDirectory()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["git_status"].Parameters;

        var properties = schema.GetProperty("properties");
        var path = properties.GetProperty("path");

        path.GetProperty("default").GetString().Should().Be(".");
    }

    [Fact]
    public void GitDiff_ShouldHaveNoRequiredFields()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["git_diff"].Parameters;

        var required = schema.GetProperty("required");
        var requiredFields = required.EnumerateArray().Select(e => e.GetString()).ToList();

        requiredFields.Should().BeEmpty();
    }

    [Fact]
    public void GitDiff_Staged_ShouldDefaultToFalse()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["git_diff"].Parameters;

        var properties = schema.GetProperty("properties");
        var staged = properties.GetProperty("staged");

        staged.GetProperty("default").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void GitLog_Count_ShouldHaveBoundsAndDefault()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["git_log"].Parameters;

        var properties = schema.GetProperty("properties");
        var count = properties.GetProperty("count");

        count.GetProperty("minimum").GetInt32().Should().Be(1);
        count.GetProperty("maximum").GetInt32().Should().Be(100);
        count.GetProperty("default").GetInt32().Should().Be(10);
    }

    [Fact]
    public void GitLog_Oneline_ShouldDefaultToFalse()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["git_log"].Parameters;

        var properties = schema.GetProperty("properties");
        var oneline = properties.GetProperty("oneline");

        oneline.GetProperty("default").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void GitLog_ShouldHaveNoRequiredFields()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["git_log"].Parameters;

        var required = schema.GetProperty("required");
        var requiredFields = required.EnumerateArray().Select(e => e.GetString()).ToList();

        requiredFields.Should().BeEmpty();
    }

    [Fact]
    public void GitLog_Author_ShouldHaveMaxLength256()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["git_log"].Parameters;

        var properties = schema.GetProperty("properties");
        var author = properties.GetProperty("author");

        author.GetProperty("maxLength").GetInt32().Should().Be(256);
    }

    [Fact]
    public void GitCommit_Message_ShouldHaveCorrectConstraints()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["git_commit"].Parameters;

        var properties = schema.GetProperty("properties");
        var message = properties.GetProperty("message");

        message.GetProperty("type").GetString().Should().Be("string");
        message.GetProperty("minLength").GetInt32().Should().Be(1);
        message.GetProperty("maxLength").GetInt32().Should().Be(500);
    }

    [Fact]
    public void GitCommit_All_ShouldDefaultToFalse()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["git_commit"].Parameters;

        var properties = schema.GetProperty("properties");
        var all = properties.GetProperty("all");

        all.GetProperty("default").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void GitCommit_Amend_ShouldDefaultToFalse()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["git_commit"].Parameters;

        var properties = schema.GetProperty("properties");
        var amend = properties.GetProperty("amend");

        amend.GetProperty("default").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void GitCommit_RequiredFields_ShouldOnlyBeMessage()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["git_commit"].Parameters;

        var required = schema.GetProperty("required");
        var requiredFields = required.EnumerateArray().Select(e => e.GetString()).ToList();

        requiredFields.Should().BeEquivalentTo(new[] { "message" });
    }

    [Fact]
    public void GitCommit_Path_ShouldHaveDefaultCurrentDirectory()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["git_commit"].Parameters;

        var properties = schema.GetProperty("properties");
        var path = properties.GetProperty("path");

        path.GetProperty("default").GetString().Should().Be(".");
    }
}
