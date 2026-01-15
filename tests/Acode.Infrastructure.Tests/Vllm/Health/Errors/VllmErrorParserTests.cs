using Acode.Infrastructure.Vllm.Health.Errors;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Health.Errors;

public class VllmErrorParserTests
{
    [Fact]
    public void Should_Parse_Complete_Error_Response()
    {
        // Arrange
        var json = @"{
            ""error"": {
                ""message"": ""Model 'nonexistent' not found"",
                ""type"": ""invalid_request_error"",
                ""code"": ""model_not_found"",
                ""param"": ""model""
            }
        }";
        var parser = new VllmErrorParser();

        // Act
        var error = parser.Parse(json);

        // Assert
        error.Message.Should().Be("Model 'nonexistent' not found");
        error.Type.Should().Be("invalid_request_error");
        error.Code.Should().Be("model_not_found");
        error.Param.Should().Be("model");
    }

    [Fact]
    public void Should_Extract_All_Fields()
    {
        // Arrange
        var json = @"{
            ""error"": {
                ""message"": ""Test message"",
                ""type"": ""test_type"",
                ""code"": ""test_code"",
                ""param"": ""test_param""
            }
        }";
        var parser = new VllmErrorParser();

        // Act
        var error = parser.Parse(json);

        // Assert
        error.Should().NotBeNull();
        error.Message.Should().Be("Test message");
        error.Type.Should().Be("test_type");
        error.Code.Should().Be("test_code");
        error.Param.Should().Be("test_param");
    }

    [Fact]
    public void Should_Handle_Missing_Type()
    {
        // Arrange
        var json = @"{
            ""error"": {
                ""message"": ""Test message"",
                ""code"": ""test_code""
            }
        }";
        var parser = new VllmErrorParser();

        // Act
        var error = parser.Parse(json);

        // Assert
        error.Message.Should().Be("Test message");
        error.Type.Should().BeNull();
        error.Code.Should().Be("test_code");
    }

    [Fact]
    public void Should_Handle_Missing_Code()
    {
        // Arrange
        var json = @"{
            ""error"": {
                ""message"": ""Test message"",
                ""type"": ""test_type""
            }
        }";
        var parser = new VllmErrorParser();

        // Act
        var error = parser.Parse(json);

        // Assert
        error.Message.Should().Be("Test message");
        error.Type.Should().Be("test_type");
        error.Code.Should().BeNull();
    }

    [Fact]
    public void Should_Handle_Missing_Param()
    {
        // Arrange
        var json = @"{
            ""error"": {
                ""message"": ""Test message""
            }
        }";
        var parser = new VllmErrorParser();

        // Act
        var error = parser.Parse(json);

        // Assert
        error.Message.Should().Be("Test message");
        error.Param.Should().BeNull();
    }

    [Fact]
    public void Should_Handle_Missing_Error_Object()
    {
        // Arrange
        var json = @"{ ""data"": null }";
        var parser = new VllmErrorParser();

        // Act
        var error = parser.Parse(json);

        // Assert
        error.Message.Should().Contain("error object");
    }

    [Fact]
    public void Should_Handle_Malformed_JSON()
    {
        // Arrange
        var json = @"{ invalid json }";
        var parser = new VllmErrorParser();

        // Act
        var error = parser.Parse(json);

        // Assert
        error.Message.Should().Contain("Malformed");
    }

    [Fact]
    public void Should_Handle_Empty_String()
    {
        // Arrange
        var parser = new VllmErrorParser();

        // Act
        var error = parser.Parse(string.Empty);

        // Assert
        error.Message.Should().Contain("Empty");
    }

    [Fact]
    public void Should_Handle_Null_String()
    {
        // Arrange
        var parser = new VllmErrorParser();

        // Act
        var error = parser.Parse(null!);

        // Assert
        error.Message.Should().Contain("Empty");
    }
}
