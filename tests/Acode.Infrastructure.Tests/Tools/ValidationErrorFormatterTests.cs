namespace Acode.Infrastructure.Tests.Tools;

using Acode.Application.Tools.Retry;
using Acode.Domain.Tools;
using Acode.Infrastructure.Tools;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for ValidationErrorFormatter implementation.
/// FR-007b: Validation error retry contract.
/// </summary>
public sealed class ValidationErrorFormatterTests
{
    private readonly RetryConfiguration config;
    private readonly ValidationErrorFormatter sut;

    public ValidationErrorFormatterTests()
    {
        this.config = RetryConfiguration.Default;
        this.sut = new ValidationErrorFormatter(this.config);
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ValidationErrorFormatter(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FormatErrors_WithSingleError_FormatsCorrectly()
    {
        // Arrange
        var errors = new List<SchemaValidationError>
        {
            new("path", "VAL-001", "Required property 'path' is missing.", ErrorSeverity.Error)
        };

        // Act
        var result = this.sut.FormatErrors("read_file", errors, 1, 3);

        // Assert
        result.Should().Contain("read_file");
        result.Should().Contain("path");
        result.Should().Contain("missing");
        result.Should().Contain("Attempt 1 of 3");
    }

    [Fact]
    public void FormatErrors_WithMultipleErrors_FormatsAllErrors()
    {
        // Arrange
        var errors = new List<SchemaValidationError>
        {
            new("path", "VAL-001", "Required property 'path' is missing.", ErrorSeverity.Error),
            new("encoding", "VAL-002", "Property 'encoding' expected string.", ErrorSeverity.Error)
        };

        // Act
        var result = this.sut.FormatErrors("read_file", errors, 2, 3);

        // Assert
        result.Should().Contain("path");
        result.Should().Contain("encoding");
        result.Should().Contain("Attempt 2 of 3");
    }

    [Fact]
    public void FormatErrors_ExceedsMaxErrors_TruncatesAndIndicates()
    {
        // Arrange
        var config = new RetryConfiguration { MaxErrorsShown = 2 };
        var sut = new ValidationErrorFormatter(config);
        var errors = new List<SchemaValidationError>
        {
            new("path1", "VAL-001", "Error 1", ErrorSeverity.Error),
            new("path2", "VAL-001", "Error 2", ErrorSeverity.Error),
            new("path3", "VAL-001", "Error 3", ErrorSeverity.Error)
        };

        // Act
        var result = sut.FormatErrors("tool", errors, 1, 3);

        // Assert
        result.Should().Contain("path1");
        result.Should().Contain("path2");
        result.Should().Contain("1 additional error");
    }

    [Fact]
    public void FormatErrors_IncludesErrorCode()
    {
        // Arrange
        var errors = new List<SchemaValidationError>
        {
            new("path", "VAL-001", "Error message", ErrorSeverity.Error)
        };

        // Act
        var result = this.sut.FormatErrors("tool", errors, 1, 3);

        // Assert
        result.Should().Contain("VAL-001");
    }

    [Fact]
    public void FormatErrors_IncludesSeverity()
    {
        // Arrange
        var errors = new List<SchemaValidationError>
        {
            new("path", "VAL-001", "Error message", ErrorSeverity.Warning)
        };

        // Act
        var result = this.sut.FormatErrors("tool", errors, 1, 3);

        // Assert
        result.Should().ContainAny("Warning", "warning");
    }

    [Fact]
    public void FormatErrors_ExceedsMaxLength_Truncates()
    {
        // Arrange
        var config = new RetryConfiguration { MaxMessageLength = 100, MaxErrorsShown = 1 };
        var sut = new ValidationErrorFormatter(config);
        var errors = new List<SchemaValidationError>
        {
            new(
                "very_long_path_name_that_exceeds_normal_length",
                "VAL-001",
                "This is a very long error message that should be truncated to fit within the maximum message length constraint.",
                ErrorSeverity.Error)
        };

        // Act
        var result = sut.FormatErrors("tool", errors, 1, 3);

        // Assert
        result.Length.Should().BeLessOrEqualTo(config.MaxMessageLength + 50); // Allow some buffer for truncation marker
    }

    [Fact]
    public void FormatEscalation_FormatsCorrectly()
    {
        // Arrange
        var history = new List<ValidationAttempt>
        {
            new(1, new[] { new SchemaValidationError("path", "VAL-001", "Error 1", ErrorSeverity.Error) }, DateTimeOffset.UtcNow.AddMinutes(-2)),
            new(2, new[] { new SchemaValidationError("path", "VAL-002", "Error 2", ErrorSeverity.Error) }, DateTimeOffset.UtcNow.AddMinutes(-1)),
            new(3, new[] { new SchemaValidationError("path", "VAL-003", "Error 3", ErrorSeverity.Error) }, DateTimeOffset.UtcNow)
        };

        // Act
        var result = this.sut.FormatEscalation("read_file", history);

        // Assert
        result.Should().Contain("read_file");
        result.Should().Contain("3");
        result.Should().ContainAny("escalat", "failed", "max", "exceeded");
    }

    [Fact]
    public void FormatEscalation_IncludesAllAttempts()
    {
        // Arrange
        var history = new List<ValidationAttempt>
        {
            new(1, new[] { new SchemaValidationError("path1", "VAL-001", "Error 1", ErrorSeverity.Error) }, DateTimeOffset.UtcNow),
            new(2, new[] { new SchemaValidationError("path2", "VAL-002", "Error 2", ErrorSeverity.Error) }, DateTimeOffset.UtcNow)
        };

        // Act
        var result = this.sut.FormatEscalation("tool", history);

        // Assert
        result.Should().Contain("Attempt 1");
        result.Should().Contain("Attempt 2");
    }

    [Fact]
    public void FormatErrors_EmptyErrors_HandlesGracefully()
    {
        // Arrange
        var errors = new List<SchemaValidationError>();

        // Act
        var result = this.sut.FormatErrors("tool", errors, 1, 3);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Formatter_ImplementsIValidationErrorFormatter()
    {
        // Assert
        this.sut.Should().BeAssignableTo<IValidationErrorFormatter>();
    }
}
