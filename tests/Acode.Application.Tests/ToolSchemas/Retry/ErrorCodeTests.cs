namespace Acode.Application.Tests.ToolSchemas.Retry;

using System.Reflection;
using Acode.Application.ToolSchemas.Retry;
using FluentAssertions;

/// <summary>
/// Unit tests for ErrorCode static class.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3244-3331.
/// ErrorCode contains 15 standard error codes in VAL-XXX format.
/// </remarks>
public sealed class ErrorCodeTests
{
    [Fact]
    public void Should_Have_All_15_Error_Codes()
    {
        // Arrange
        var fields = typeof(ErrorCode)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .ToList();

        // Assert
        fields.Should().HaveCount(15, "ErrorCode should have exactly 15 constants");
    }

    [Theory]
    [InlineData(nameof(ErrorCode.RequiredFieldMissing), "VAL-001")]
    [InlineData(nameof(ErrorCode.TypeMismatch), "VAL-002")]
    [InlineData(nameof(ErrorCode.ConstraintViolation), "VAL-003")]
    [InlineData(nameof(ErrorCode.InvalidJsonSyntax), "VAL-004")]
    [InlineData(nameof(ErrorCode.UnknownField), "VAL-005")]
    [InlineData(nameof(ErrorCode.ArrayLengthViolation), "VAL-006")]
    [InlineData(nameof(ErrorCode.PatternMismatch), "VAL-007")]
    [InlineData(nameof(ErrorCode.InvalidEnumValue), "VAL-008")]
    [InlineData(nameof(ErrorCode.StringLengthViolation), "VAL-009")]
    [InlineData(nameof(ErrorCode.FormatViolation), "VAL-010")]
    [InlineData(nameof(ErrorCode.NumberRangeViolation), "VAL-011")]
    [InlineData(nameof(ErrorCode.UniqueConstraintViolation), "VAL-012")]
    [InlineData(nameof(ErrorCode.DependencyViolation), "VAL-013")]
    [InlineData(nameof(ErrorCode.MutualExclusivityViolation), "VAL-014")]
    [InlineData(nameof(ErrorCode.ObjectSchemaViolation), "VAL-015")]
    public void Should_Have_Correct_Value(string fieldName, string expectedValue)
    {
        // Arrange
        var field = typeof(ErrorCode).GetField(fieldName, BindingFlags.Public | BindingFlags.Static);

        // Act
        var actualValue = field?.GetValue(null) as string;

        // Assert
        actualValue.Should().Be(expectedValue, $"{fieldName} should be '{expectedValue}'");
    }

    [Fact]
    public void All_Error_Codes_Should_Follow_VAL_XXX_Format()
    {
        // Arrange
        var fields = typeof(ErrorCode)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .ToList();

        // Act & Assert
        foreach (var field in fields)
        {
            var value = field.GetValue(null) as string;
            value.Should().MatchRegex(
                @"^VAL-\d{3}$",
                $"Error code '{field.Name}' should follow VAL-XXX format");
        }
    }

    [Fact]
    public void All_Error_Codes_Should_Be_Unique()
    {
        // Arrange
        var fields = typeof(ErrorCode)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .ToList();

        var values = fields.Select(f => f.GetValue(null) as string).ToList();

        // Assert
        values.Should().OnlyHaveUniqueItems("All error codes must be unique");
    }

    [Fact]
    public void Error_Codes_Should_Be_Sequential()
    {
        // Arrange
        var fields = typeof(ErrorCode)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .ToList();

        var values = fields
            .Select(f => f.GetValue(null) as string)
            .Where(v => v != null)
            .Select(v => int.Parse(v!.Replace("VAL-", string.Empty, StringComparison.Ordinal)))
            .OrderBy(v => v)
            .ToList();

        // Assert - should be 1 through 15
        values.Should().BeEquivalentTo(Enumerable.Range(1, 15));
    }

    [Fact]
    public void RequiredFieldMissing_Should_Be_VAL_001()
    {
        ErrorCode.RequiredFieldMissing.Should().Be("VAL-001");
    }

    [Fact]
    public void TypeMismatch_Should_Be_VAL_002()
    {
        ErrorCode.TypeMismatch.Should().Be("VAL-002");
    }

    [Fact]
    public void ConstraintViolation_Should_Be_VAL_003()
    {
        ErrorCode.ConstraintViolation.Should().Be("VAL-003");
    }

    [Fact]
    public void InvalidJsonSyntax_Should_Be_VAL_004()
    {
        ErrorCode.InvalidJsonSyntax.Should().Be("VAL-004");
    }

    [Fact]
    public void UnknownField_Should_Be_VAL_005()
    {
        ErrorCode.UnknownField.Should().Be("VAL-005");
    }

    [Fact]
    public void ObjectSchemaViolation_Should_Be_VAL_015()
    {
        ErrorCode.ObjectSchemaViolation.Should().Be("VAL-015");
    }
}
