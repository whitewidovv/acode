using Acode.Infrastructure.Vllm.Client.Serialization;
using Acode.Infrastructure.Vllm.Exceptions;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Client.Serialization;

public class VllmResponseParserTests
{
    [Fact]
    public void Parse_Should_Extract_Choices_Array()
    {
        // Arrange (FR-033, AC-033): MUST extract choices array
        var json = @"{
            ""id"": ""test"",
            ""model"": ""llama"",
            ""choices"": [
                {
                    ""message"": {
                        ""role"": ""assistant"",
                        ""content"": ""Hello""
                    }
                }
            ]
        }";

        // Act
        var response = VllmResponseParser.Parse(json);

        // Assert
        response.Choices.Should().NotBeNull();
        response.Choices.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_Should_Extract_Message_From_First_Choice()
    {
        // Arrange (FR-034, AC-034): MUST extract message from first choice
        var json = @"{
            ""id"": ""test-id"",
            ""model"": ""test-model"",
            ""choices"": [
                {
                    ""message"": {
                        ""role"": ""assistant"",
                        ""content"": ""Test response""
                    }
                }
            ]
        }";

        // Act
        var response = VllmResponseParser.Parse(json);

        // Assert
        response.Choices[0].Message.Should().NotBeNull();
        response.Choices[0].Message!.Role.Should().Be("assistant");
        response.Choices[0].Message.Content.Should().Be("Test response");
    }

    [Fact]
    public void Parse_Should_Extract_Content_From_Message()
    {
        // Arrange (FR-035, AC-035): MUST extract content from message
        var json = @"{
            ""id"": ""test"",
            ""model"": ""test"",
            ""choices"": [
                {
                    ""message"": {
                        ""role"": ""assistant"",
                        ""content"": ""Response content here""
                    }
                }
            ]
        }";

        // Act
        var response = VllmResponseParser.Parse(json);

        // Assert
        response.Choices[0].Message!.Content.Should().Be("Response content here");
    }

    [Fact]
    public void Parse_Should_Extract_Finish_Reason_From_Choice()
    {
        // Arrange (FR-037, AC-037): MUST extract finish_reason from choice
        var json = @"{
            ""id"": ""test"",
            ""model"": ""test"",
            ""choices"": [
                {
                    ""finish_reason"": ""stop"",
                    ""message"": {
                        ""role"": ""assistant"",
                        ""content"": ""Done""
                    }
                }
            ]
        }";

        // Act
        var response = VllmResponseParser.Parse(json);

        // Assert
        response.Choices[0].FinishReason.Should().Be("stop");
    }

    [Fact]
    public void Parse_Should_Extract_Usage_From_Response()
    {
        // Arrange (FR-038, AC-038): MUST extract usage from response (optional)
        var json = @"{
            ""id"": ""test"",
            ""model"": ""test"",
            ""choices"": [
                {
                    ""message"": {
                        ""role"": ""assistant"",
                        ""content"": ""Response""
                    }
                }
            ],
            ""usage"": {
                ""prompt_tokens"": 10,
                ""completion_tokens"": 20,
                ""total_tokens"": 30
            }
        }";

        // Act
        var response = VllmResponseParser.Parse(json);

        // Assert
        response.Usage.Should().NotBeNull();
        response.Usage!.PromptTokens.Should().Be(10);
        response.Usage.CompletionTokens.Should().Be(20);
        response.Usage.TotalTokens.Should().Be(30);
    }

    [Fact]
    public void Parse_Should_Handle_Missing_Optional_Fields()
    {
        // Arrange (FR-039, AC-039): MUST handle missing optional fields
        var json = @"{
            ""id"": ""test"",
            ""model"": ""test"",
            ""choices"": [
                {
                    ""message"": {
                        ""role"": ""assistant"",
                        ""content"": ""Response""
                    }
                }
            ]
        }";

        // Act
        var response = VllmResponseParser.Parse(json);

        // Assert
        response.Usage.Should().BeNull();  // Optional field not present
        response.Choices[0].FinishReason.Should().BeNull();  // Optional field not present
    }

    [Fact]
    public void Parse_Should_Validate_Required_Fields_Present()
    {
        // Arrange (FR-040, AC-040): MUST validate required fields present
        var json = @"{
            ""id"": ""test"",
            ""model"": ""test"",
            ""choices"": []
        }";

        // Act & Assert
        var exception = Assert.Throws<VllmParseException>(() =>
        {
            VllmResponseParser.Parse(json);
        });

        exception.Message.Should().Contain("choices");
    }

    [Fact]
    public void Parse_Should_Throw_When_Message_Missing()
    {
        // Arrange (FR-034, AC-034): Message is required
        var json = @"{
            ""id"": ""test"",
            ""model"": ""test"",
            ""choices"": [
                {}
            ]
        }";

        // Act & Assert
        var exception = Assert.Throws<VllmParseException>(() =>
        {
            VllmResponseParser.Parse(json);
        });

        exception.Message.Should().Contain("message");
    }

    [Fact]
    public void Parse_Should_Throw_On_Invalid_Json()
    {
        // Arrange (FR-032, AC-032): Handle invalid JSON
        var json = @"{ invalid json }";

        // Act & Assert
        var exception = Assert.Throws<VllmParseException>(() =>
        {
            VllmResponseParser.Parse(json);
        });

        exception.Message.Should().Contain("Failed to parse");
    }
}
