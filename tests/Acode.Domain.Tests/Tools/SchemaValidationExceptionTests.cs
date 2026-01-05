namespace Acode.Domain.Tests.Tools;

using Acode.Domain.Tools;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for SchemaValidationException.
/// FR-007: Tool Schema Registry requirements.
/// </summary>
public sealed class SchemaValidationExceptionTests
{
    [Fact]
    public void Constructor_WithToolNameAndErrors_ShouldSetProperties()
    {
        // Arrange
        var errors = new[]
        {
            new SchemaValidationError("/path", "VAL-001", "Required field missing"),
        };

        // Act
        var exception = new SchemaValidationException("read_file", errors);

        // Assert
        exception.ToolName.Should().Be("read_file");
        exception.Errors.Should().HaveCount(1);
    }

    [Fact]
    public void Constructor_WithNullToolName_ShouldThrowArgumentException()
    {
        // Arrange
        var errors = new[] { new SchemaValidationError("/path", "VAL-001", "Error") };

        // Act
        var act = () => new SchemaValidationException(null!, errors);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyToolName_ShouldThrowArgumentException()
    {
        // Arrange
        var errors = new[] { new SchemaValidationError("/path", "VAL-001", "Error") };

        // Act
        var act = () => new SchemaValidationException(string.Empty, errors);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNullErrors_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new SchemaValidationException("read_file", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithEmptyErrors_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new SchemaValidationException("read_file", Array.Empty<SchemaValidationError>());

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var errors = new[] { new SchemaValidationError("/path", "VAL-001", "Error") };
        var inner = new InvalidOperationException("Inner error");

        // Act
        var exception = new SchemaValidationException("read_file", errors, inner);

        // Assert
        exception.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void Message_ShouldIncludeToolName()
    {
        // Arrange
        var errors = new[] { new SchemaValidationError("/path", "VAL-001", "Required field missing") };

        // Act
        var exception = new SchemaValidationException("read_file", errors);

        // Assert
        exception.Message.Should().Contain("read_file");
    }

    [Fact]
    public void Message_ShouldIncludeErrorCount()
    {
        // Arrange
        var errors = new[]
        {
            new SchemaValidationError("/path", "VAL-001", "Error 1"),
            new SchemaValidationError("/content", "VAL-002", "Error 2"),
        };

        // Act
        var exception = new SchemaValidationException("write_file", errors);

        // Assert
        exception.Message.Should().Contain("2");
    }

    [Fact]
    public void Errors_ShouldBeReadOnly()
    {
        // Arrange
        var errors = new[] { new SchemaValidationError("/path", "VAL-001", "Error") };
        var exception = new SchemaValidationException("read_file", errors);

        // Assert
        exception.Errors.Should().BeAssignableTo<IReadOnlyCollection<SchemaValidationError>>();
    }

    [Fact]
    public void ErrorCode_ShouldReturnFirstErrorCode()
    {
        // Arrange
        var errors = new[]
        {
            new SchemaValidationError("/path", "VAL-001", "First error"),
            new SchemaValidationError("/content", "VAL-002", "Second error"),
        };

        // Act
        var exception = new SchemaValidationException("write_file", errors);

        // Assert
        exception.ErrorCode.Should().Be("VAL-001");
    }

    [Fact]
    public void HasMultipleErrors_WithSingleError_ShouldReturnFalse()
    {
        // Arrange
        var errors = new[] { new SchemaValidationError("/path", "VAL-001", "Error") };

        // Act
        var exception = new SchemaValidationException("read_file", errors);

        // Assert
        exception.HasMultipleErrors.Should().BeFalse();
    }

    [Fact]
    public void HasMultipleErrors_WithMultipleErrors_ShouldReturnTrue()
    {
        // Arrange
        var errors = new[]
        {
            new SchemaValidationError("/path", "VAL-001", "Error 1"),
            new SchemaValidationError("/content", "VAL-002", "Error 2"),
        };

        // Act
        var exception = new SchemaValidationException("write_file", errors);

        // Assert
        exception.HasMultipleErrors.Should().BeTrue();
    }

    [Fact]
    public void GetFormattedErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var errors = new[]
        {
            new SchemaValidationError("/path", "VAL-001", "Required field missing"),
            new SchemaValidationError("/content", "VAL-002", "Type mismatch"),
        };

        // Act
        var exception = new SchemaValidationException("write_file", errors);
        var formatted = exception.GetFormattedErrors();

        // Assert
        formatted.Should().Contain("/path");
        formatted.Should().Contain("/content");
        formatted.Should().Contain("VAL-001");
        formatted.Should().Contain("VAL-002");
    }

    [Fact]
    public void SchemaValidationException_ShouldInheritFromException()
    {
        // Arrange
        var errors = new[] { new SchemaValidationError("/path", "VAL-001", "Error") };

        // Act
        var exception = new SchemaValidationException("read_file", errors);

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }
}
