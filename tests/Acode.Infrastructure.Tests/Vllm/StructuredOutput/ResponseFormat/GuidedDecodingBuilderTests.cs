namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput.ResponseFormat;

using System.Text.Json;
using Acode.Infrastructure.Vllm.StructuredOutput.ResponseFormat;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for GuidedDecodingBuilder.
/// </summary>
public class GuidedDecodingBuilderTests
{
    [Fact]
    public void BuildGuidedJson_WithSchema_ReturnsCorrectParameter()
    {
        // Arrange
        var builder = new GuidedDecodingBuilder();
        var schema = JsonDocument.Parse(@"{""type"":""object"",""properties"":{""name"":{""type"":""string""}}}").RootElement;

        // Act
        var parameter = builder.BuildGuidedJson(schema);

        // Assert
        parameter.Should().NotBeNull();
        parameter.Type.Should().Be("json_schema");
        parameter.Schema.Should().Contain("\"type\":\"object\"");
    }

    [Fact]
    public void BuildGuidedJson_WithUndefinedSchema_Throws()
    {
        // Arrange
        var builder = new GuidedDecodingBuilder();
        var schema = default(JsonElement);

        // Act
        var action = () => builder.BuildGuidedJson(schema);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BuildGuidedChoice_WithChoices_ReturnsCorrectParameter()
    {
        // Arrange
        var builder = new GuidedDecodingBuilder();
        var choices = new[] { "red", "green", "blue" };

        // Act
        var parameter = builder.BuildGuidedChoice(choices);

        // Assert
        parameter.Should().NotBeNull();
        parameter.Type.Should().Be("choice");
        parameter.Choices.Should().Equal(choices);
    }

    [Fact]
    public void BuildGuidedChoice_WithEmptyChoices_Throws()
    {
        // Arrange
        var builder = new GuidedDecodingBuilder();

        // Act
        var action = () => builder.BuildGuidedChoice(Array.Empty<string>());

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BuildGuidedChoice_WithNullChoices_Throws()
    {
        // Arrange
        var builder = new GuidedDecodingBuilder();

        // Act
        var action = () => builder.BuildGuidedChoice(null!);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BuildGuidedRegex_WithPattern_ReturnsCorrectParameter()
    {
        // Arrange
        var builder = new GuidedDecodingBuilder();
        var pattern = @"^\d{3}-\d{2}-\d{4}$";

        // Act
        var parameter = builder.BuildGuidedRegex(pattern);

        // Assert
        parameter.Should().NotBeNull();
        parameter.Type.Should().Be("regex");
        parameter.Pattern.Should().Be(pattern);
    }

    [Fact]
    public void BuildGuidedRegex_WithEmptyPattern_Throws()
    {
        // Arrange
        var builder = new GuidedDecodingBuilder();

        // Act
        var action = () => builder.BuildGuidedRegex(string.Empty);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BuildGuidedRegex_WithNullPattern_Throws()
    {
        // Arrange
        var builder = new GuidedDecodingBuilder();

        // Act
        var action = () => builder.BuildGuidedRegex(null!);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SelectGuidedParameter_WithEnumSchema_ReturnsGuidedChoice()
    {
        // Arrange
        var builder = new GuidedDecodingBuilder();
        var schema = JsonDocument.Parse(@"{""type"":""string"",""enum"":[""active"",""inactive"",""pending""]}").RootElement;

        // Act
        var parameter = builder.SelectGuidedParameter(schema);

        // Assert
        parameter.Should().BeOfType<GuidedChoiceParameter>();
        var choiceParam = (GuidedChoiceParameter)parameter;
        choiceParam.Type.Should().Be("choice");
        choiceParam.Choices.Should().Equal("active", "inactive", "pending");
    }

    [Fact]
    public void SelectGuidedParameter_WithPatternSchema_ReturnsGuidedRegex()
    {
        // Arrange
        var builder = new GuidedDecodingBuilder();
        var schema = JsonDocument.Parse(@"{""type"":""string"",""pattern"":""^[A-Z0-9]+$""}").RootElement;

        // Act
        var parameter = builder.SelectGuidedParameter(schema);

        // Assert
        parameter.Should().BeOfType<GuidedRegexParameter>();
        var regexParam = (GuidedRegexParameter)parameter;
        regexParam.Type.Should().Be("regex");
        regexParam.Pattern.Should().Be("^[A-Z0-9]+$");
    }

    [Fact]
    public void SelectGuidedParameter_WithObjectSchema_ReturnsGuidedJson()
    {
        // Arrange
        var builder = new GuidedDecodingBuilder();
        var schema = JsonDocument.Parse(@"{""type"":""object"",""properties"":{""name"":{""type"":""string""}}}").RootElement;

        // Act
        var parameter = builder.SelectGuidedParameter(schema);

        // Assert
        parameter.Should().BeOfType<GuidedJsonParameter>();
        var jsonParam = (GuidedJsonParameter)parameter;
        jsonParam.Type.Should().Be("json_schema");
        jsonParam.Schema.Should().Contain("properties");
    }

    [Fact]
    public void SelectGuidedParameter_WithNonObjectSchema_ReturnsGuidedJson()
    {
        // Arrange
        var builder = new GuidedDecodingBuilder();
        var schema = JsonDocument.Parse(@"""string""").RootElement;

        // Act
        var parameter = builder.SelectGuidedParameter(schema);

        // Assert
        parameter.Should().BeOfType<GuidedJsonParameter>();
    }
}
