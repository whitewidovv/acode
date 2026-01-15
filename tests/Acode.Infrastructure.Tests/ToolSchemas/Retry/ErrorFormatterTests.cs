namespace Acode.Infrastructure.Tests.ToolSchemas.Retry;

using System.Diagnostics;
using Acode.Application.ToolSchemas.Retry;
using Acode.Infrastructure.ToolSchemas.Retry;
using FluentAssertions;

/// <summary>
/// Unit tests for ErrorFormatter class.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3511-3676.
/// Tests error formatting, truncation, hints, and performance requirements.
/// </remarks>
public sealed class ErrorFormatterTests
{
    private readonly ErrorFormatter formatter;

    public ErrorFormatterTests()
    {
        var config = new RetryConfiguration
        {
            MaxMessageLength = 2000,
            MaxErrorsShown = 10,
            MaxValuePreview = 100,
            IncludeHints = true,
            IncludeActualValues = true,
            RedactSecrets = true,
            RelativizePaths = true,
        };
        this.formatter = new ErrorFormatter(config);
    }

    [Fact]
    public void Should_Format_Single_Error()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new()
            {
                ErrorCode = ErrorCode.RequiredFieldMissing,
                FieldPath = "/path",
                Message = "Field 'path' is required",
                Severity = ErrorSeverity.Error,
                ExpectedValue = "string",
            },
        };

        // Act
        var result = this.formatter.FormatErrors("read_file", errors, 1, 3);

        // Assert
        result.Should().Contain("Validation failed for tool 'read_file'");
        result.Should().Contain("attempt 1/3");
        result.Should().Contain("[VAL-001]");
        result.Should().Contain("/path");
        result.Should().NotContain("Errors:"); // Single error doesn't have "Errors:" header
    }

    [Fact]
    public void Should_Format_Multiple_Errors()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new() { ErrorCode = ErrorCode.RequiredFieldMissing, FieldPath = "/path", Message = "Missing", Severity = ErrorSeverity.Error },
            new() { ErrorCode = ErrorCode.TypeMismatch, FieldPath = "/timeout", Message = "Wrong type", Severity = ErrorSeverity.Error },
        };

        // Act
        var result = this.formatter.FormatErrors("read_file", errors, 1, 3);

        // Assert
        result.Should().Contain("Errors:"); // Multiple errors have "Errors:" header
        result.Should().Contain("[VAL-001]");
        result.Should().Contain("[VAL-002]");
    }

    [Fact]
    public void Should_Include_Tool_Name()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new() { ErrorCode = ErrorCode.RequiredFieldMissing, FieldPath = "/path", Message = "Missing", Severity = ErrorSeverity.Error },
        };

        // Act
        var result = this.formatter.FormatErrors("my_custom_tool", errors, 1, 3);

        // Assert
        result.Should().Contain("'my_custom_tool'");
    }

    [Fact]
    public void Should_Include_Attempt_Number()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new() { ErrorCode = ErrorCode.RequiredFieldMissing, FieldPath = "/path", Message = "Missing", Severity = ErrorSeverity.Error },
        };

        // Act
        var result = this.formatter.FormatErrors("tool", errors, 2, 5);

        // Assert
        result.Should().Contain("attempt 2/5");
    }

    [Fact]
    public void Should_Truncate_Long_Values()
    {
        // Arrange
        var longValue = new string('x', 200);
        var errors = new List<ValidationError>
        {
            new()
            {
                ErrorCode = ErrorCode.TypeMismatch,
                FieldPath = "/data",
                Message = "Type mismatch",
                Severity = ErrorSeverity.Error,
                ActualValue = longValue,
            },
        };

        // Act
        var result = this.formatter.FormatErrors("tool", errors, 1, 3);

        // Assert
        result.Should().Contain("...");
        result.Length.Should().BeLessThan(longValue.Length + 500);
    }

    [Fact]
    public void Should_Respect_Max_Length()
    {
        // Arrange
        var config = new RetryConfiguration { MaxMessageLength = 100 };
        var shortFormatter = new ErrorFormatter(config);
        var errors = Enumerable.Range(1, 20)
            .Select(i => new ValidationError
            {
                ErrorCode = ErrorCode.RequiredFieldMissing,
                FieldPath = $"/field{i}",
                Message = "This is a long error message that will contribute to exceeding max length",
                Severity = ErrorSeverity.Error,
            })
            .ToList();

        // Act
        var result = shortFormatter.FormatErrors("tool", errors, 1, 3);

        // Assert
        result.Length.Should().BeLessOrEqualTo(100);
        result.Should().EndWith("...");
    }

    [Fact]
    public void Should_Include_Correction_Hints()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new()
            {
                ErrorCode = ErrorCode.RequiredFieldMissing,
                FieldPath = "/path",
                Message = "Missing",
                Severity = ErrorSeverity.Error,
            },
        };

        // Act
        var result = this.formatter.FormatErrors("tool", errors, 1, 3);

        // Assert
        result.Should().Contain("Hints:");
        result.Should().Contain("Add the required field");
    }

    [Fact]
    public void Should_Sort_Errors_By_Field_Path()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new() { ErrorCode = ErrorCode.TypeMismatch, FieldPath = "/zebra", Message = "Error", Severity = ErrorSeverity.Error },
            new() { ErrorCode = ErrorCode.TypeMismatch, FieldPath = "/alpha", Message = "Error", Severity = ErrorSeverity.Error },
            new() { ErrorCode = ErrorCode.TypeMismatch, FieldPath = "/middle", Message = "Error", Severity = ErrorSeverity.Error },
        };

        // Act
        var result = this.formatter.FormatErrors("tool", errors, 1, 3);

        // Assert - errors should appear in alphabetical order by field path
        var alphaIndex = result.IndexOf("/alpha", StringComparison.Ordinal);
        var middleIndex = result.IndexOf("/middle", StringComparison.Ordinal);
        var zebraIndex = result.IndexOf("/zebra", StringComparison.Ordinal);

        alphaIndex.Should().BeLessThan(middleIndex);
        middleIndex.Should().BeLessThan(zebraIndex);
    }

    [Fact]
    public void Should_Complete_In_Under_1ms()
    {
        // Arrange
        var errors = Enumerable.Range(1, 10)
            .Select(i => new ValidationError
            {
                ErrorCode = ErrorCode.TypeMismatch,
                FieldPath = $"/field{i}",
                Message = "Error message",
                Severity = ErrorSeverity.Error,
                ActualValue = "test value",
            })
            .ToList();

        var stopwatch = Stopwatch.StartNew();
        const int iterations = 1000;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            _ = this.formatter.FormatErrors("tool", errors, (i % 3) + 1, 3);
        }

        stopwatch.Stop();

        // Assert
        var averageMs = (double)stopwatch.ElapsedMilliseconds / iterations;
        averageMs.Should().BeLessThan(1.0, $"Average should be <1ms (actual: {averageMs:F3}ms)");
    }

    [Fact]
    public void Should_Handle_Empty_Errors()
    {
        // Act
        var result = this.formatter.FormatErrors("tool", Array.Empty<ValidationError>(), 1, 3);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Should_Show_Additional_Error_Count()
    {
        // Arrange
        var config = new RetryConfiguration { MaxErrorsShown = 2 };
        var limitedFormatter = new ErrorFormatter(config);
        var errors = Enumerable.Range(1, 5)
            .Select(i => new ValidationError
            {
                ErrorCode = $"VAL-00{i}",
                FieldPath = $"/field{i}",
                Message = $"Error {i}",
                Severity = ErrorSeverity.Error,
            })
            .ToList();

        // Act
        var result = limitedFormatter.FormatErrors("tool", errors, 1, 3);

        // Assert
        result.Should().Contain("...and 3 more error(s)");
    }

    [Fact]
    public void Should_Include_Severity_Icons_For_Multiple_Errors()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new() { ErrorCode = ErrorCode.RequiredFieldMissing, FieldPath = "/a", Message = "Error", Severity = ErrorSeverity.Error },
            new() { ErrorCode = ErrorCode.PatternMismatch, FieldPath = "/b", Message = "Warning", Severity = ErrorSeverity.Warning },
            new() { ErrorCode = ErrorCode.UnknownField, FieldPath = "/c", Message = "Info", Severity = ErrorSeverity.Info },
        };

        // Act
        var result = this.formatter.FormatErrors("tool", errors, 1, 3);

        // Assert
        result.Should().Contain("❌"); // Error icon
        result.Should().Contain("⚠️"); // Warning icon
        result.Should().Contain("ℹ️"); // Info icon
    }
}
