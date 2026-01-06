using System.Text.Json;
using FluentAssertions;

namespace Acode.Cli.Tests;

/// <summary>
/// Tests for <see cref="JsonLinesFormatter"/>.
/// </summary>
public class JsonLinesFormatterTests
{
    [Fact]
    public void WriteMessage_WithInfoType_OutputsJsonLine()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new JsonLinesFormatter(output);

        // Act
        formatter.WriteMessage("Test message", MessageType.Info);

        // Assert
        var json = output.ToString();
        json.Should().Contain("\"type\":\"message\"");
        json.Should().Contain("\"message\":\"Test message\"");
        json.Should().Contain("\"level\":\"info\"");
        json.Should().EndWith("\n");
    }

    [Fact]
    public void WriteMessage_WithErrorType_OutputsErrorLevel()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new JsonLinesFormatter(output);

        // Act
        formatter.WriteMessage("Error occurred", MessageType.Error);

        // Assert
        var json = output.ToString();
        json.Should().Contain("\"level\":\"error\"");
        json.Should().Contain("\"message\":\"Error occurred\"");
    }

    [Fact]
    public void WriteHeading_OutputsJsonLine()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new JsonLinesFormatter(output);

        // Act
        formatter.WriteHeading("Main Heading", level: 1);

        // Assert
        var json = output.ToString();
        json.Should().Contain("\"type\":\"heading\"");
        json.Should().Contain("\"text\":\"Main Heading\"");
        json.Should().Contain("\"level\":1");
    }

    [Fact]
    public void WriteKeyValue_OutputsJsonLine()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new JsonLinesFormatter(output);

        // Act
        formatter.WriteKeyValue("Model", "llama3.3");

        // Assert
        var json = output.ToString();
        json.Should().Contain("\"type\":\"key_value\"");
        json.Should().Contain("\"key\":\"Model\"");
        json.Should().Contain("\"value\":\"llama3.3\"");
    }

    [Fact]
    public void WriteList_OutputsJsonLine()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new JsonLinesFormatter(output);
        var items = new[] { "First", "Second", "Third" };

        // Act
        formatter.WriteList(items, ordered: false);

        // Assert
        var json = output.ToString();
        json.Should().Contain("\"type\":\"list\"");
        json.Should().Contain("\"items\":[\"First\",\"Second\",\"Third\"]");
        json.Should().Contain("\"ordered\":false");
    }

    [Fact]
    public void WriteTable_OutputsJsonLine()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new JsonLinesFormatter(output);
        var headers = new[] { "Name", "Version" };
        var rows = new List<string[]>
        {
            new[] { "llama3.3", "70b" },
            new[] { "llama3.2", "3b" },
        };

        // Act
        formatter.WriteTable(headers, rows);

        // Assert
        var json = output.ToString();
        json.Should().Contain("\"type\":\"table\"");
        json.Should().Contain("\"headers\":[\"Name\",\"Version\"]");
        json.Should().Contain("\"rows\":");
    }

    [Fact]
    public void WriteBlankLine_OutputsJsonLine()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new JsonLinesFormatter(output);

        // Act
        formatter.WriteBlankLine();

        // Assert
        var json = output.ToString();
        json.Should().Contain("\"type\":\"blank_line\"");
    }

    [Fact]
    public void WriteSeparator_OutputsJsonLine()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new JsonLinesFormatter(output);

        // Act
        formatter.WriteSeparator();

        // Assert
        var json = output.ToString();
        json.Should().Contain("\"type\":\"separator\"");
    }

    [Fact]
    public void MultipleWrites_OutputsMultipleJsonLines()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new JsonLinesFormatter(output);

        // Act
        formatter.WriteMessage("First", MessageType.Info);
        formatter.WriteMessage("Second", MessageType.Success);

        // Assert
        var lines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(2);
        lines[0].Should().Contain("\"message\":\"First\"");
        lines[1].Should().Contain("\"message\":\"Second\"");
    }

    [Fact]
    public void WriteMessage_OutputsValidJson()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new JsonLinesFormatter(output);

        // Act
        formatter.WriteMessage("Test", MessageType.Info);

        // Assert - should be valid JSON
        var json = output.ToString().TrimEnd('\n');
        var act = () => JsonDocument.Parse(json);
        act.Should().NotThrow("output should be valid JSON");
    }
}
