namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput.Fallback;

using Acode.Infrastructure.Vllm.StructuredOutput.Fallback;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for OutputValidator.
/// </summary>
public class OutputValidatorTests
{
    [Fact]
    public void Validate_WithValidJsonAndSchema_ReturnsValid()
    {
        // Arrange
        var validator = new OutputValidator();
        var output = @"{""name"":""John"",""age"":30}";
        var schema = @"{""type"":""object"",""properties"":{""name"":{""type"":""string""},""age"":{""type"":""integer""}}}";

        // Act
        var result = validator.Validate(output, schema);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithInvalidJsonOutput_ReturnsInvalid()
    {
        // Arrange
        var validator = new OutputValidator();
        var output = @"{invalid json}";
        var schema = @"{""type"":""object""}";

        // Act
        var result = validator.Validate(output, schema);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Should().Contain("Invalid JSON");
    }

    [Fact]
    public void Validate_WithEmptyOutput_ReturnsInvalid()
    {
        // Arrange
        var validator = new OutputValidator();
        var output = string.Empty;
        var schema = @"{""type"":""object""}";

        // Act
        var result = validator.Validate(output, schema);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Output and schema must not be empty");
    }

    [Fact]
    public void Validate_WithEmptySchema_ReturnsInvalid()
    {
        // Arrange
        var validator = new OutputValidator();
        var output = @"{""name"":""John""}";
        var schema = string.Empty;

        // Act
        var result = validator.Validate(output, schema);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TryExtractValidJson_WithValidJson_ReturnsJson()
    {
        // Arrange
        var validator = new OutputValidator();
        var output = @"{""name"":""John"",""age"":30}";

        // Act
        var extracted = validator.TryExtractValidJson(output);

        // Assert
        extracted.Should().Be(output);
    }

    [Fact]
    public void TryExtractValidJson_WithJsonInText_ExtractsJson()
    {
        // Arrange
        var validator = new OutputValidator();
        var output = @"The result is: {""name"":""John"",""age"":30} as shown.";

        // Act
        var extracted = validator.TryExtractValidJson(output);

        // Assert
        extracted.Should().NotBeNullOrEmpty();
        extracted.Should().Contain(@"""name""");
    }

    [Fact]
    public void TryExtractValidJson_WithArrayJson_ExtractsArray()
    {
        // Arrange
        var validator = new OutputValidator();
        var output = @"Here is the list: [""a"",""b"",""c""] end.";

        // Act
        var extracted = validator.TryExtractValidJson(output);

        // Assert
        extracted.Should().NotBeNullOrEmpty();
        extracted.Should().Contain(@"""a""");
    }

    [Fact]
    public void TryExtractValidJson_WithNoJson_ReturnsNull()
    {
        // Arrange
        var validator = new OutputValidator();
        var output = "This is just plain text with no JSON";

        // Act
        var extracted = validator.TryExtractValidJson(output);

        // Assert
        extracted.Should().BeNull();
    }

    [Fact]
    public void TryExtractValidJson_WithMalformedJson_ReturnsNull()
    {
        // Arrange
        var validator = new OutputValidator();
        var output = @"Here is {""name"":""John"" invalid} end.";

        // Act
        var extracted = validator.TryExtractValidJson(output);

        // Assert
        extracted.Should().BeNull();
    }

    [Fact]
    public void TryExtractValidJson_WithEmptyString_ReturnsNull()
    {
        // Arrange
        var validator = new OutputValidator();

        // Act
        var extracted = validator.TryExtractValidJson(string.Empty);

        // Assert
        extracted.Should().BeNull();
    }

    [Fact]
    public void TryExtractValidJson_WithNestedObjects_ExtractsCorrectly()
    {
        // Arrange
        var validator = new OutputValidator();
        var output = @"Result: {""user"":{""name"":""John"",""age"":30}} done.";

        // Act
        var extracted = validator.TryExtractValidJson(output);

        // Assert
        extracted.Should().NotBeNullOrEmpty();
        extracted.Should().Contain("\"user\"");
    }
}
