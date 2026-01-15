namespace Acode.Infrastructure.Tests.ToolSchemas.Providers.Schemas;

using Acode.Infrastructure.ToolSchemas.Providers;
using FluentAssertions;

/// <summary>
/// Tests for file operations tool schemas (read_file, write_file, list_directory,
/// search_files, delete_file, move_file).
/// </summary>
public sealed class FileOperationsSchemaTests
{
    private readonly CoreToolsProvider provider = new();

    [Fact]
    public void WriteFile_CreateDirectories_DefaultValue_ShouldBeFalse()
    {
        // Arrange - Per spec line 499, create_directories default must be false
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["write_file"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var createDirectories = properties.GetProperty("create_directories");
        var defaultValue = createDirectories.GetProperty("default");

        // Assert - CRITICAL: Spec says default MUST be false, not true
        defaultValue.GetBoolean().Should().BeFalse(
            "create_directories must default to false per spec line 499 to prevent unexpected directory creation");
    }

    [Fact]
    public void WriteFile_Overwrite_DefaultValue_ShouldBeTrue()
    {
        // Arrange - Per spec line 501, overwrite default must be true
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["write_file"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var overwrite = properties.GetProperty("overwrite");
        var defaultValue = overwrite.GetProperty("default");

        // Assert
        defaultValue.GetBoolean().Should().BeTrue(
            "overwrite must default to true per spec line 501");
    }

    [Fact]
    public void WriteFile_Path_ShouldHaveCorrectConstraints()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["write_file"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var path = properties.GetProperty("path");

        // Assert
        path.GetProperty("type").GetString().Should().Be("string");
        path.GetProperty("minLength").GetInt32().Should().Be(1);
        path.GetProperty("maxLength").GetInt32().Should().Be(4096);
    }

    [Fact]
    public void WriteFile_Content_ShouldHaveMaxLength1MB()
    {
        // Arrange - Per spec, content max is 1MB (1,048,576 bytes)
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["write_file"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var content = properties.GetProperty("content");

        // Assert
        content.GetProperty("maxLength").GetInt32().Should().Be(1048576);
    }

    [Fact]
    public void WriteFile_RequiredFields_ShouldBePathAndContent()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["write_file"].Parameters;

        // Act
        var required = schema.GetProperty("required");
        var requiredFields = required.EnumerateArray().Select(e => e.GetString()).ToList();

        // Assert
        requiredFields.Should().BeEquivalentTo(new[] { "path", "content" });
    }

    [Fact]
    public void WriteFile_Encoding_ShouldHaveCorrectEnumValues()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["write_file"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var encoding = properties.GetProperty("encoding");
        var enumValues = encoding.GetProperty("enum")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();

        // Assert
        enumValues.Should().BeEquivalentTo(new[] { "utf-8", "ascii", "utf-16" });
        encoding.GetProperty("default").GetString().Should().Be("utf-8");
    }

    [Fact]
    public void ReadFile_Path_ShouldHaveCorrectConstraints()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["read_file"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var path = properties.GetProperty("path");

        // Assert
        path.GetProperty("type").GetString().Should().Be("string");
        path.GetProperty("minLength").GetInt32().Should().Be(1);
        path.GetProperty("maxLength").GetInt32().Should().Be(4096);
    }

    [Fact]
    public void ReadFile_StartLine_ShouldHaveMinimum1()
    {
        // Arrange - Lines are 1-indexed
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["read_file"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var startLine = properties.GetProperty("start_line");

        // Assert
        startLine.GetProperty("type").GetString().Should().Be("integer");
        startLine.GetProperty("minimum").GetInt32().Should().Be(1);
    }

    [Fact]
    public void ReadFile_EndLine_ShouldHaveMinimum1()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["read_file"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var endLine = properties.GetProperty("end_line");

        // Assert
        endLine.GetProperty("type").GetString().Should().Be("integer");
        endLine.GetProperty("minimum").GetInt32().Should().Be(1);
    }

    [Fact]
    public void ReadFile_Encoding_ShouldDefaultToUtf8()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["read_file"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var encoding = properties.GetProperty("encoding");

        // Assert
        encoding.GetProperty("default").GetString().Should().Be("utf-8");
    }

    [Fact]
    public void ReadFile_RequiredFields_ShouldOnlyBePath()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["read_file"].Parameters;

        // Act
        var required = schema.GetProperty("required");
        var requiredFields = required.EnumerateArray().Select(e => e.GetString()).ToList();

        // Assert
        requiredFields.Should().BeEquivalentTo(new[] { "path" });
    }

    [Fact]
    public void ListDirectory_Path_ShouldHaveMaxLength4096()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["list_directory"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var path = properties.GetProperty("path");

        // Assert
        path.GetProperty("maxLength").GetInt32().Should().Be(4096);
    }

    [Fact]
    public void ListDirectory_Recursive_ShouldDefaultToFalse()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["list_directory"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var recursive = properties.GetProperty("recursive");

        // Assert
        recursive.GetProperty("default").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void ListDirectory_MaxDepth_ShouldHaveBounds1To100()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["list_directory"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var maxDepth = properties.GetProperty("max_depth");

        // Assert
        maxDepth.GetProperty("minimum").GetInt32().Should().Be(1);
        maxDepth.GetProperty("maximum").GetInt32().Should().Be(100);
    }

    [Fact]
    public void ListDirectory_IncludeHidden_ShouldDefaultToFalse()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["list_directory"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var includeHidden = properties.GetProperty("include_hidden");

        // Assert
        includeHidden.GetProperty("default").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void SearchFiles_Query_ShouldHaveConstraints()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["search_files"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var query = properties.GetProperty("query");

        // Assert
        query.GetProperty("minLength").GetInt32().Should().Be(1);
        query.GetProperty("maxLength").GetInt32().Should().Be(1000);
    }

    [Fact]
    public void SearchFiles_MaxResults_ShouldHaveBoundsAndDefault()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["search_files"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var maxResults = properties.GetProperty("max_results");

        // Assert
        maxResults.GetProperty("minimum").GetInt32().Should().Be(1);
        maxResults.GetProperty("maximum").GetInt32().Should().Be(1000);
        maxResults.GetProperty("default").GetInt32().Should().Be(100);
    }

    [Fact]
    public void SearchFiles_CaseSensitive_ShouldDefaultToFalse()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["search_files"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var caseSensitive = properties.GetProperty("case_sensitive");

        // Assert
        caseSensitive.GetProperty("default").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void SearchFiles_Regex_ShouldDefaultToFalse()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["search_files"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var regex = properties.GetProperty("regex");

        // Assert
        regex.GetProperty("default").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void DeleteFile_Path_ShouldHaveMaxLength4096()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["delete_file"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var path = properties.GetProperty("path");

        // Assert
        path.GetProperty("maxLength").GetInt32().Should().Be(4096);
    }

    [Fact]
    public void DeleteFile_Confirm_ShouldDefaultToFalse()
    {
        // Arrange - confirm parameter required for safety
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["delete_file"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var confirm = properties.GetProperty("confirm");

        // Assert
        confirm.GetProperty("default").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void DeleteFile_RequiredFields_ShouldBePathAndConfirm()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["delete_file"].Parameters;

        // Act
        var required = schema.GetProperty("required");
        var requiredFields = required.EnumerateArray().Select(e => e.GetString()).ToList();

        // Assert
        requiredFields.Should().BeEquivalentTo(new[] { "path", "confirm" });
    }

    [Fact]
    public void MoveFile_SourceAndDestination_ShouldHaveMaxLength4096()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["move_file"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var source = properties.GetProperty("source");
        var destination = properties.GetProperty("destination");

        // Assert
        source.GetProperty("maxLength").GetInt32().Should().Be(4096);
        destination.GetProperty("maxLength").GetInt32().Should().Be(4096);
    }

    [Fact]
    public void MoveFile_Overwrite_ShouldDefaultToFalse()
    {
        // Arrange - Per spec, move_file overwrite defaults to false (unlike write_file)
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["move_file"].Parameters;

        // Act
        var properties = schema.GetProperty("properties");
        var overwrite = properties.GetProperty("overwrite");

        // Assert
        overwrite.GetProperty("default").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void MoveFile_RequiredFields_ShouldBeSourceAndDestination()
    {
        // Arrange
        var tools = this.provider.GetToolDefinitions().ToDictionary(t => t.Name);
        var schema = tools["move_file"].Parameters;

        // Act
        var required = schema.GetProperty("required");
        var requiredFields = required.EnumerateArray().Select(e => e.GetString()).ToList();

        // Assert
        requiredFields.Should().BeEquivalentTo(new[] { "source", "destination" });
    }
}
