namespace Acode.Application.Tests.Tools.Retry;

using Acode.Application.Tools.Retry;
using FluentAssertions;

/// <summary>
/// Tests for ValidationErrorCode constants.
/// FR-007b: Validation error retry contract - error codes.
/// </summary>
public sealed class ValidationErrorCodeTests
{
    [Fact]
    public void RequiredMissing_ShouldBeVAL001()
    {
        // Assert
        ValidationErrorCode.RequiredMissing.Should().Be("VAL-001");
    }

    [Fact]
    public void TypeMismatch_ShouldBeVAL002()
    {
        // Assert
        ValidationErrorCode.TypeMismatch.Should().Be("VAL-002");
    }

    [Fact]
    public void ConstraintViolation_ShouldBeVAL003()
    {
        // Assert
        ValidationErrorCode.ConstraintViolation.Should().Be("VAL-003");
    }

    [Fact]
    public void InvalidJson_ShouldBeVAL004()
    {
        // Assert
        ValidationErrorCode.InvalidJson.Should().Be("VAL-004");
    }

    [Fact]
    public void UnknownField_ShouldBeVAL005()
    {
        // Assert
        ValidationErrorCode.UnknownField.Should().Be("VAL-005");
    }

    [Fact]
    public void ArrayLengthViolation_ShouldBeVAL006()
    {
        // Assert
        ValidationErrorCode.ArrayLengthViolation.Should().Be("VAL-006");
    }

    [Fact]
    public void PatternMismatch_ShouldBeVAL007()
    {
        // Assert
        ValidationErrorCode.PatternMismatch.Should().Be("VAL-007");
    }

    [Fact]
    public void InvalidEnumValue_ShouldBeVAL008()
    {
        // Assert
        ValidationErrorCode.InvalidEnumValue.Should().Be("VAL-008");
    }

    [Fact]
    public void StringLengthViolation_ShouldBeVAL009()
    {
        // Assert
        ValidationErrorCode.StringLengthViolation.Should().Be("VAL-009");
    }

    [Fact]
    public void FormatViolation_ShouldBeVAL010()
    {
        // Assert
        ValidationErrorCode.FormatViolation.Should().Be("VAL-010");
    }

    [Fact]
    public void AllCodes_ShouldFollowVALFormat()
    {
        // Assert - All codes should match VAL-XXX pattern
        ValidationErrorCode.RequiredMissing.Should().MatchRegex(@"^VAL-\d{3}$");
        ValidationErrorCode.TypeMismatch.Should().MatchRegex(@"^VAL-\d{3}$");
        ValidationErrorCode.ConstraintViolation.Should().MatchRegex(@"^VAL-\d{3}$");
        ValidationErrorCode.InvalidJson.Should().MatchRegex(@"^VAL-\d{3}$");
        ValidationErrorCode.UnknownField.Should().MatchRegex(@"^VAL-\d{3}$");
        ValidationErrorCode.ArrayLengthViolation.Should().MatchRegex(@"^VAL-\d{3}$");
        ValidationErrorCode.PatternMismatch.Should().MatchRegex(@"^VAL-\d{3}$");
        ValidationErrorCode.InvalidEnumValue.Should().MatchRegex(@"^VAL-\d{3}$");
        ValidationErrorCode.StringLengthViolation.Should().MatchRegex(@"^VAL-\d{3}$");
        ValidationErrorCode.FormatViolation.Should().MatchRegex(@"^VAL-\d{3}$");
    }
}
