using Acode.Application.Configuration;
using FluentAssertions;

namespace Acode.Application.Tests.Configuration;

/// <summary>
/// Tests for ConfigErrorCodes constants.
/// Per FR-002b-48: Error codes must follow ACODE-CFG-NNN format.
/// </summary>
public sealed class ConfigErrorCodesTests
{
    [Fact]
    public void FileNotFound_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 404
        ConfigErrorCodes.FileNotFound.Should().Be("ACODE-CFG-001");
    }

    [Fact]
    public void FileReadError_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 405
        ConfigErrorCodes.FileReadError.Should().Be("ACODE-CFG-002");
    }

    [Fact]
    public void EncodingError_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 406
        ConfigErrorCodes.EncodingError.Should().Be("ACODE-CFG-003");
    }

    [Fact]
    public void YamlSyntaxError_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 407
        ConfigErrorCodes.YamlSyntaxError.Should().Be("ACODE-CFG-004");
    }

    [Fact]
    public void YamlStructureError_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 408
        ConfigErrorCodes.YamlStructureError.Should().Be("ACODE-CFG-005");
    }

    [Fact]
    public void FileTooLarge_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 409
        ConfigErrorCodes.FileTooLarge.Should().Be("ACODE-CFG-006");
    }

    [Fact]
    public void NestingTooDeep_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 410
        ConfigErrorCodes.NestingTooDeep.Should().Be("ACODE-CFG-007");
    }

    [Fact]
    public void TooManyKeys_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 411
        ConfigErrorCodes.TooManyKeys.Should().Be("ACODE-CFG-008");
    }

    [Fact]
    public void CircularReference_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 412
        ConfigErrorCodes.CircularReference.Should().Be("ACODE-CFG-009");
    }

    [Fact]
    public void RequiredFieldMissing_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 413
        ConfigErrorCodes.RequiredFieldMissing.Should().Be("ACODE-CFG-010");
    }

    [Fact]
    public void TypeMismatch_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 414
        ConfigErrorCodes.TypeMismatch.Should().Be("ACODE-CFG-011");
    }

    [Fact]
    public void EnumViolation_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 415
        ConfigErrorCodes.EnumViolation.Should().Be("ACODE-CFG-012");
    }

    [Fact]
    public void PatternViolation_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 416
        ConfigErrorCodes.PatternViolation.Should().Be("ACODE-CFG-013");
    }

    [Fact]
    public void RangeViolation_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 417
        ConfigErrorCodes.RangeViolation.Should().Be("ACODE-CFG-014");
    }

    [Fact]
    public void UnknownField_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 418
        ConfigErrorCodes.UnknownField.Should().Be("ACODE-CFG-015");
    }

    [Fact]
    public void DeprecatedField_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 419
        ConfigErrorCodes.DeprecatedField.Should().Be("ACODE-CFG-016");
    }

    [Fact]
    public void EnvVarMissing_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 420
        ConfigErrorCodes.EnvVarMissing.Should().Be("ACODE-CFG-017");
    }

    [Fact]
    public void EnvVarError_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 421
        ConfigErrorCodes.EnvVarError.Should().Be("ACODE-CFG-018");
    }

    [Fact]
    public void PathTraversal_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 422
        ConfigErrorCodes.PathTraversal.Should().Be("ACODE-CFG-019");
    }

    [Fact]
    public void InvalidGlob_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 423
        ConfigErrorCodes.InvalidGlob.Should().Be("ACODE-CFG-020");
    }

    [Fact]
    public void ModeViolation_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 424
        ConfigErrorCodes.ModeViolation.Should().Be("ACODE-CFG-021");
    }

    [Fact]
    public void ProviderViolation_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 425
        ConfigErrorCodes.ProviderViolation.Should().Be("ACODE-CFG-022");
    }

    [Fact]
    public void SchemaVersionUnsupported_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 426
        ConfigErrorCodes.SchemaVersionUnsupported.Should().Be("ACODE-CFG-023");
    }

    [Fact]
    public void SemanticViolation_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 427
        ConfigErrorCodes.SemanticViolation.Should().Be("ACODE-CFG-024");
    }

    [Fact]
    public void SecurityViolation_ShouldHaveCorrectFormat()
    {
        // FR-002b, line 428
        ConfigErrorCodes.SecurityViolation.Should().Be("ACODE-CFG-025");
    }

    [Fact]
    public void AllErrorCodes_ShouldFollowAcodeCfgNnnFormat()
    {
        // Get all public const string fields from ConfigErrorCodes
        var errorCodeFields = typeof(ConfigErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string) && f.IsLiteral)
            .ToList();

        errorCodeFields.Should().NotBeEmpty("ConfigErrorCodes should have error code constants");

        foreach (var field in errorCodeFields)
        {
            var value = (string?)field.GetValue(null);
            value.Should().MatchRegex(
                @"^ACODE-CFG-\d{3}$",
                $"Error code {field.Name} should follow ACODE-CFG-NNN format");
        }
    }

    [Fact]
    public void AllErrorCodes_ShouldBeUnique()
    {
        // Get all error code values
        var errorCodeValues = typeof(ConfigErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string) && f.IsLiteral)
            .Select(f => (string?)f.GetValue(null))
            .Where(v => v != null)
            .ToList();

        errorCodeValues.Should().OnlyHaveUniqueItems("Each error code should be unique");
    }

    [Fact]
    public void ErrorCodeCount_ShouldBe25()
    {
        // Per spec lines 401-429: 25 error codes total
        var errorCodeFields = typeof(ConfigErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string) && f.IsLiteral)
            .ToList();

        errorCodeFields.Should().HaveCount(25, "Spec defines 25 error codes (ACODE-CFG-001 through ACODE-CFG-025)");
    }
}
