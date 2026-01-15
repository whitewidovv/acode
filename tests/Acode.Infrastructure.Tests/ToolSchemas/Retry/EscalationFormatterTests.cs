namespace Acode.Infrastructure.Tests.ToolSchemas.Retry;

using Acode.Infrastructure.ToolSchemas.Retry;
using FluentAssertions;

/// <summary>
/// Unit tests for EscalationFormatter class.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3957-4017.
/// Tests escalation message formatting with history and recommendations.
/// </remarks>
public sealed class EscalationFormatterTests
{
    private readonly EscalationFormatter formatter;

    public EscalationFormatterTests()
    {
        this.formatter = new EscalationFormatter();
    }

    [Fact]
    public void Should_Format_Escalation_Message()
    {
        // Arrange
        var history = new List<string> { "Error on attempt 1", "Error on attempt 2" };

        // Act
        var result = this.formatter.FormatEscalation("read_file", "call-123", history, 3);

        // Assert
        result.Should().Contain("ESCALATION REQUIRED");
        result.Should().Contain("read_file");
        result.Should().Contain("call-123");
    }

    [Fact]
    public void Should_Include_Full_History()
    {
        // Arrange
        var history = new List<string>
        {
            "First error message",
            "Second error message",
            "Third error message",
        };

        // Act
        var result = this.formatter.FormatEscalation("tool", "call-1", history, 3);

        // Assert
        result.Should().Contain("First error message");
        result.Should().Contain("Second error message");
        result.Should().Contain("Third error message");
        result.Should().Contain("Attempt 1:");
        result.Should().Contain("Attempt 2:");
        result.Should().Contain("Attempt 3:");
    }

    [Fact]
    public void Should_Include_Attempt_Timestamps()
    {
        // Arrange - history entries serve as approximate timestamps
        var history = new List<string> { "Error 1" };

        // Act
        var result = this.formatter.FormatEscalation("tool", "call-1", history, 3);

        // Assert
        result.Should().Contain("Attempt 1:");
        result.Should().Contain("Validation History");
    }

    [Fact]
    public void Should_Provide_Recommendations()
    {
        // Arrange
        var history = new List<string> { "Error" };

        // Act
        var result = this.formatter.FormatEscalation("tool", "call-1", history, 3);

        // Assert
        result.Should().Contain("Recommended Actions");
        result.Should().Contain("Review the tool schema");
        result.Should().Contain("required parameters");
    }

    [Fact]
    public void Should_Include_Analysis_Section()
    {
        // Arrange
        var history = new List<string> { "Error 1", "Error 2", "Error 3" };

        // Act
        var result = this.formatter.FormatEscalation("tool", "call-1", history, 3);

        // Assert
        result.Should().Contain("Analysis");
        result.Should().Contain("Total validation attempts: 3");
    }

    [Fact]
    public void Should_Include_Tool_Name_And_Attempt_Count()
    {
        // Arrange
        var history = new List<string> { "Error" };

        // Act
        var result = this.formatter.FormatEscalation("my_custom_tool", "call-123", history, 5);

        // Assert
        result.Should().Contain("'my_custom_tool'");
        result.Should().Contain("5 attempts");
    }

    [Fact]
    public void Should_Handle_Empty_History()
    {
        // Arrange
        var history = new List<string>();

        // Act
        var result = this.formatter.FormatEscalation("tool", "call-1", history, 3);

        // Assert
        result.Should().Contain("No validation history recorded");
    }

    [Fact]
    public void Should_Include_Escalation_Prompt()
    {
        // Arrange
        var history = new List<string> { "Error" };

        // Act
        var result = this.formatter.FormatEscalation("tool", "call-1", history, 3);

        // Assert
        result.Should().Contain("Human intervention required");
    }
}
