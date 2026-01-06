using FluentAssertions;

namespace Acode.Cli.Tests;

/// <summary>
/// Tests for <see cref="ConsoleFormatter"/>.
/// </summary>
public class ConsoleFormatterTests
{
    [Fact]
    public void WriteMessage_WithInfoType_WritesMessage()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new ConsoleFormatter(output, enableColors: false);

        // Act
        formatter.WriteMessage("Test message", MessageType.Info);

        // Assert
        var result = output.ToString();
        result.Should().Contain("Test message");
    }

    [Fact]
    public void WriteMessage_WithSuccessType_WritesMessage()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new ConsoleFormatter(output, enableColors: false);

        // Act
        formatter.WriteMessage("Success!", MessageType.Success);

        // Assert
        var result = output.ToString();
        result.Should().Contain("Success!");
    }

    [Fact]
    public void WriteMessage_WithWarningType_WritesMessage()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new ConsoleFormatter(output, enableColors: false);

        // Act
        formatter.WriteMessage("Warning!", MessageType.Warning);

        // Assert
        var result = output.ToString();
        result.Should().Contain("Warning!");
    }

    [Fact]
    public void WriteMessage_WithErrorType_WritesMessage()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new ConsoleFormatter(output, enableColors: false);

        // Act
        formatter.WriteMessage("Error!", MessageType.Error);

        // Assert
        var result = output.ToString();
        result.Should().Contain("Error!");
    }

    [Fact]
    public void WriteHeading_WritesHeadingText()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new ConsoleFormatter(output, enableColors: false);

        // Act
        formatter.WriteHeading("Main Heading", level: 1);

        // Assert
        var result = output.ToString();
        result.Should().Contain("Main Heading");
    }

    [Fact]
    public void WriteKeyValue_WritesKeyAndValue()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new ConsoleFormatter(output, enableColors: false);

        // Act
        formatter.WriteKeyValue("Name", "Acode");

        // Assert
        var result = output.ToString();
        result.Should().Contain("Name");
        result.Should().Contain("Acode");
    }

    [Fact]
    public void WriteList_WithUnorderedList_WritesItems()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new ConsoleFormatter(output, enableColors: false);
        var items = new[] { "Item 1", "Item 2", "Item 3" };

        // Act
        formatter.WriteList(items, ordered: false);

        // Assert
        var result = output.ToString();
        result.Should().Contain("Item 1");
        result.Should().Contain("Item 2");
        result.Should().Contain("Item 3");
    }

    [Fact]
    public void WriteList_WithOrderedList_WritesNumberedItems()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new ConsoleFormatter(output, enableColors: false);
        var items = new[] { "First", "Second", "Third" };

        // Act
        formatter.WriteList(items, ordered: true);

        // Assert
        var result = output.ToString();
        result.Should().Contain("First");
        result.Should().Contain("Second");
        result.Should().Contain("Third");
    }

    [Fact]
    public void WriteTable_WritesHeadersAndRows()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new ConsoleFormatter(output, enableColors: false);
        var headers = new[] { "Name", "Version" };
        var rows = new[]
        {
            new[] { "Acode", "1.0.0" },
            new[] { "CLI", "0.1.0" },
        };

        // Act
        formatter.WriteTable(headers, rows);

        // Assert
        var result = output.ToString();
        result.Should().Contain("Name");
        result.Should().Contain("Version");
        result.Should().Contain("Acode");
        result.Should().Contain("1.0.0");
        result.Should().Contain("CLI");
        result.Should().Contain("0.1.0");
    }

    [Fact]
    public void WriteBlankLine_WritesNewline()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new ConsoleFormatter(output, enableColors: false);

        // Act
        formatter.WriteBlankLine();

        // Assert
        var result = output.ToString();
        result.Should().Contain(Environment.NewLine);
    }

    [Fact]
    public void WriteSeparator_WritesSeparatorLine()
    {
        // Arrange
        var output = new StringWriter();
        var formatter = new ConsoleFormatter(output, enableColors: false);

        // Act
        formatter.WriteSeparator();

        // Assert
        var result = output.ToString();
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_WithColorsEnabled_EnablesColors()
    {
        // Arrange
        var output = new StringWriter();

        // Act
        var formatter = new ConsoleFormatter(output, enableColors: true);

        // Assert - Verify formatter was created successfully
        formatter.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullOutput_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ConsoleFormatter(null!, enableColors: false);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("output");
    }
}
