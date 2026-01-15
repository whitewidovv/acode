namespace Acode.Infrastructure.Tests.ToolSchemas.Retry;

using Acode.Application.ToolSchemas.Retry;
using Acode.Infrastructure.ToolSchemas.Retry;
using FluentAssertions;

/// <summary>
/// Unit tests for ErrorAggregator class.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3809-3856.
/// Tests error deduplication, sorting by severity, and limit enforcement.
/// </remarks>
public sealed class ErrorAggregatorTests
{
    [Fact]
    public void Should_Deduplicate_By_Field_Path_And_Code()
    {
        // Arrange
        var aggregator = new ErrorAggregator(maxErrors: 10);
        var errors = new List<ValidationError>
        {
            new() { ErrorCode = ErrorCode.RequiredFieldMissing, FieldPath = "/path", Message = "First", Severity = ErrorSeverity.Error },
            new() { ErrorCode = ErrorCode.RequiredFieldMissing, FieldPath = "/path", Message = "Duplicate", Severity = ErrorSeverity.Error },
            new() { ErrorCode = ErrorCode.TypeMismatch, FieldPath = "/path", Message = "Different code", Severity = ErrorSeverity.Error },
        };

        // Act
        var result = aggregator.Aggregate(errors);

        // Assert
        result.Should().HaveCount(2, "duplicate should be removed");
        result.Select(e => e.Message).Should().Contain("First");
        result.Select(e => e.Message).Should().NotContain("Duplicate");
    }

    [Fact]
    public void Should_Sort_By_Severity_Then_Field_Path()
    {
        // Arrange
        var aggregator = new ErrorAggregator(maxErrors: 10);
        var errors = new List<ValidationError>
        {
            new() { ErrorCode = ErrorCode.UnknownField, FieldPath = "/zebra", Message = "Info", Severity = ErrorSeverity.Info },
            new() { ErrorCode = ErrorCode.TypeMismatch, FieldPath = "/alpha", Message = "Error", Severity = ErrorSeverity.Error },
            new() { ErrorCode = ErrorCode.PatternMismatch, FieldPath = "/beta", Message = "Warning", Severity = ErrorSeverity.Warning },
            new() { ErrorCode = ErrorCode.RequiredFieldMissing, FieldPath = "/gamma", Message = "Error 2", Severity = ErrorSeverity.Error },
        };

        // Act
        var result = aggregator.Aggregate(errors);

        // Assert - Errors first (descending severity), then alphabetical by path
        result.Should().HaveCount(4);
        result[0].Severity.Should().Be(ErrorSeverity.Error);
        result[0].FieldPath.Should().Be("/alpha"); // Error, comes before /gamma alphabetically
        result[1].Severity.Should().Be(ErrorSeverity.Error);
        result[1].FieldPath.Should().Be("/gamma");
        result[2].Severity.Should().Be(ErrorSeverity.Warning);
        result[3].Severity.Should().Be(ErrorSeverity.Info);
    }

    [Fact]
    public void Should_Respect_Max_Errors_Limit()
    {
        // Arrange
        var aggregator = new ErrorAggregator(maxErrors: 3);
        var errors = Enumerable.Range(1, 10)
            .Select(i => new ValidationError
            {
                ErrorCode = ErrorCode.TypeMismatch,
                FieldPath = $"/field{i}",
                Message = $"Error {i}",
                Severity = ErrorSeverity.Error,
            })
            .ToList();

        // Act
        var result = aggregator.Aggregate(errors);

        // Assert
        result.Should().HaveCount(3, "should limit to maxErrors");
    }

    [Fact]
    public void Should_Handle_Empty_Input()
    {
        // Arrange
        var aggregator = new ErrorAggregator(maxErrors: 10);

        // Act
        var result = aggregator.Aggregate(Array.Empty<ValidationError>());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Should_Handle_Null_Input()
    {
        // Arrange
        var aggregator = new ErrorAggregator(maxErrors: 10);

        // Act
        var result = aggregator.Aggregate(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Should_Preserve_First_Duplicate()
    {
        // Arrange
        var aggregator = new ErrorAggregator(maxErrors: 10);
        var errors = new List<ValidationError>
        {
            new() { ErrorCode = ErrorCode.RequiredFieldMissing, FieldPath = "/path", Message = "First message", Severity = ErrorSeverity.Error },
            new() { ErrorCode = ErrorCode.RequiredFieldMissing, FieldPath = "/path", Message = "Second message", Severity = ErrorSeverity.Error },
        };

        // Act
        var result = aggregator.Aggregate(errors);

        // Assert
        result.Should().HaveCount(1);
        result[0].Message.Should().Be("First message", "first duplicate should be preserved");
    }

    [Fact]
    public void Should_Group_Duplicates_By_Field_Path_And_Error_Code()
    {
        // Arrange
        var aggregator = new ErrorAggregator(maxErrors: 10);
        var errors = new List<ValidationError>
        {
            new() { ErrorCode = ErrorCode.RequiredFieldMissing, FieldPath = "/a", Message = "M1", Severity = ErrorSeverity.Error },
            new() { ErrorCode = ErrorCode.TypeMismatch, FieldPath = "/a", Message = "M2", Severity = ErrorSeverity.Error },
            new() { ErrorCode = ErrorCode.RequiredFieldMissing, FieldPath = "/b", Message = "M3", Severity = ErrorSeverity.Error },
            new() { ErrorCode = ErrorCode.RequiredFieldMissing, FieldPath = "/a", Message = "M4 - Duplicate of M1", Severity = ErrorSeverity.Error },
        };

        // Act
        var result = aggregator.Aggregate(errors);

        // Assert - 3 unique combinations of FieldPath + ErrorCode
        result.Should().HaveCount(3);
    }
}
