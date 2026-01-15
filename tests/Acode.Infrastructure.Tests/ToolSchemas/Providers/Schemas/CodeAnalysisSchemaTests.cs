namespace Acode.Infrastructure.Tests.ToolSchemas.Providers.Schemas;

using Acode.Infrastructure.ToolSchemas.Providers;
using FluentAssertions;

/// <summary>
/// Tests for code analysis tool schemas (semantic_search, find_symbol, get_definition).
/// </summary>
public sealed class CodeAnalysisSchemaTests
{
    private readonly CoreToolsProvider provider = new();

    [Fact]
    public void SemanticSearch_Query_ShouldHaveCorrectConstraints()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["semantic_search"].Parameters;

        var properties = schema.GetProperty("properties");
        var query = properties.GetProperty("query");

        query.GetProperty("type").GetString().Should().Be("string");
        query.GetProperty("minLength").GetInt32().Should().Be(3);
        query.GetProperty("maxLength").GetInt32().Should().Be(500);
    }

    [Fact]
    public void SemanticSearch_MaxResults_ShouldHaveBoundsAndDefault()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["semantic_search"].Parameters;

        var properties = schema.GetProperty("properties");
        var maxResults = properties.GetProperty("max_results");

        maxResults.GetProperty("minimum").GetInt32().Should().Be(1);
        maxResults.GetProperty("maximum").GetInt32().Should().Be(50);
        maxResults.GetProperty("default").GetInt32().Should().Be(10);
    }

    [Fact]
    public void SemanticSearch_IncludeContext_ShouldDefaultToTrue()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["semantic_search"].Parameters;

        var properties = schema.GetProperty("properties");
        var includeContext = properties.GetProperty("include_context");

        includeContext.GetProperty("default").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void SemanticSearch_RequiredFields_ShouldOnlyBeQuery()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["semantic_search"].Parameters;

        var required = schema.GetProperty("required");
        var requiredFields = required.EnumerateArray().Select(e => e.GetString()).ToList();

        requiredFields.Should().BeEquivalentTo(new[] { "query" });
    }

    [Fact]
    public void SemanticSearch_Path_ShouldHaveDefaultCurrentDirectory()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["semantic_search"].Parameters;

        var properties = schema.GetProperty("properties");
        var path = properties.GetProperty("path");

        path.GetProperty("default").GetString().Should().Be(".");
        path.GetProperty("maxLength").GetInt32().Should().Be(4096);
    }

    [Fact]
    public void FindSymbol_Name_ShouldHaveCorrectConstraints()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["find_symbol"].Parameters;

        var properties = schema.GetProperty("properties");
        var name = properties.GetProperty("name");

        name.GetProperty("type").GetString().Should().Be("string");
        name.GetProperty("minLength").GetInt32().Should().Be(1);
        name.GetProperty("maxLength").GetInt32().Should().Be(256);
    }

    [Fact]
    public void FindSymbol_SymbolType_ShouldHaveCorrectEnumValues()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["find_symbol"].Parameters;

        var properties = schema.GetProperty("properties");
        var symbolType = properties.GetProperty("symbol_type");
        var enumValues = symbolType.GetProperty("enum")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();

        enumValues.Should().Contain("class");
        enumValues.Should().Contain("method");
        enumValues.Should().Contain("function");
        enumValues.Should().Contain("interface");
    }

    [Fact]
    public void FindSymbol_ExactMatch_ShouldDefaultToFalse()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["find_symbol"].Parameters;

        var properties = schema.GetProperty("properties");
        var exactMatch = properties.GetProperty("exact_match");

        exactMatch.GetProperty("default").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void FindSymbol_CaseSensitive_ShouldDefaultToFalse()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["find_symbol"].Parameters;

        var properties = schema.GetProperty("properties");
        var caseSensitive = properties.GetProperty("case_sensitive");

        caseSensitive.GetProperty("default").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void FindSymbol_RequiredFields_ShouldOnlyBeName()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["find_symbol"].Parameters;

        var required = schema.GetProperty("required");
        var requiredFields = required.EnumerateArray().Select(e => e.GetString()).ToList();

        requiredFields.Should().BeEquivalentTo(new[] { "name" });
    }

    [Fact]
    public void GetDefinition_RequiredFields_ShouldIncludeFileLineAndColumn()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["get_definition"].Parameters;

        var required = schema.GetProperty("required");
        var requiredFields = required.EnumerateArray().Select(e => e.GetString()).ToList();

        requiredFields.Should().BeEquivalentTo(new[] { "file", "line", "column" });
    }

    [Fact]
    public void GetDefinition_Line_ShouldHaveMinimum1()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["get_definition"].Parameters;

        var properties = schema.GetProperty("properties");
        var line = properties.GetProperty("line");

        line.GetProperty("type").GetString().Should().Be("integer");
        line.GetProperty("minimum").GetInt32().Should().Be(1);
    }

    [Fact]
    public void GetDefinition_Column_ShouldHaveMinimum1()
    {
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["get_definition"].Parameters;

        var properties = schema.GetProperty("properties");
        var column = properties.GetProperty("column");

        column.GetProperty("type").GetString().Should().Be("integer");
        column.GetProperty("minimum").GetInt32().Should().Be(1);
    }
}
