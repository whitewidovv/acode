namespace Acode.Infrastructure.Tests.Ollama.ToolCall;

using Acode.Infrastructure.Ollama.ToolCall;
using Acode.Infrastructure.Ollama.ToolCall.Models;
using FluentAssertions;

/// <summary>
/// Tests for ToolCallParser functionality.
/// </summary>
public sealed class ToolCallParserTests
{
    private readonly ToolCallParser parser = new();

    [Fact]
    public void Parse_NullInput_ReturnsEmptyResult()
    {
        // Arrange
        OllamaToolCall[]? toolCalls = null;

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.Should().NotBeNull();
        result.ToolCalls.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public void Parse_EmptyArray_ReturnsEmptyResult()
    {
        // Arrange
        var toolCalls = Array.Empty<OllamaToolCall>();

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.Should().NotBeNull();
        result.ToolCalls.Should().BeEmpty();
        result.AllSucceeded.Should().BeTrue();
    }

    [Fact]
    public void Parse_ValidToolCall_ReturnsSuccessfulParse()
    {
        // Arrange
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Type = "function",
                Function = new OllamaFunction
                {
                    Name = "read_file",
                    Arguments = "{\"path\": \"README.md\"}",
                },
            },
        };

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.AllSucceeded.Should().BeTrue();
        result.ToolCalls.Should().HaveCount(1);
        result.ToolCalls[0].Id.Should().Be("call_123");
        result.ToolCalls[0].Name.Should().Be("read_file");
        result.ToolCalls[0].Arguments.GetRawText().Should().Be("{\"path\": \"README.md\"}");
    }

    [Fact]
    public void Parse_MultipleValidToolCalls_ReturnsAllParsed()
    {
        // Arrange
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_1",
                Function = new OllamaFunction { Name = "read_file", Arguments = "{\"path\": \"a.txt\"}" },
            },
            new OllamaToolCall
            {
                Id = "call_2",
                Function = new OllamaFunction { Name = "write_file", Arguments = "{\"path\": \"b.txt\", \"content\": \"test\"}" },
            },
        };

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.AllSucceeded.Should().BeTrue();
        result.ToolCalls.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public void Parse_MissingId_GeneratesId()
    {
        // Arrange
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = null, // Missing ID
                Function = new OllamaFunction { Name = "read_file", Arguments = "{\"path\": \"test.txt\"}" },
            },
        };

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.AllSucceeded.Should().BeTrue();
        result.ToolCalls.Should().HaveCount(1);
        result.ToolCalls[0].Id.Should().NotBeNullOrEmpty();
        result.ToolCalls[0].Id.Should().StartWith("gen_");
    }

    [Fact]
    public void Parse_EmptyId_GeneratesId()
    {
        // Arrange
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = string.Empty,
                Function = new OllamaFunction { Name = "read_file", Arguments = "{}" },
            },
        };

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.AllSucceeded.Should().BeTrue();
        result.ToolCalls[0].Id.Should().StartWith("gen_");
    }

    [Fact]
    public void Parse_NullFunction_ReturnsError()
    {
        // Arrange
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = null,
            },
        };

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.ToolCalls.Should().BeEmpty();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].ErrorCode.Should().Be("ACODE-TLP-001");
        result.Errors[0].Message.Should().Contain("function");
    }

    [Fact]
    public void Parse_EmptyFunctionName_ReturnsError()
    {
        // Arrange
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction
                {
                    Name = string.Empty,
                    Arguments = "{}",
                },
            },
        };

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.Errors[0].ErrorCode.Should().Be("ACODE-TLP-002");
    }

    [Fact]
    public void Parse_InvalidFunctionName_ReturnsError()
    {
        // Arrange
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction
                {
                    Name = "invalid-name-with-dashes", // Dashes not allowed
                    Arguments = "{}",
                },
            },
        };

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.Errors[0].ErrorCode.Should().Be("ACODE-TLP-003");
        result.Errors[0].ToolName.Should().Be("invalid-name-with-dashes");
    }

    [Fact]
    public void Parse_MalformedJsonWithTrailingComma_RepairsAndParses()
    {
        // Arrange
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction
                {
                    Name = "read_file",
                    Arguments = "{\"path\": \"test.txt\",}", // Trailing comma
                },
            },
        };

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.AllSucceeded.Should().BeTrue();
        result.ToolCalls.Should().HaveCount(1);
        result.Repairs.Should().HaveCount(1);
        result.Repairs[0].WasRepaired.Should().BeTrue();
    }

    [Fact]
    public void Parse_MalformedJsonWithMissingBrace_RepairsAndParses()
    {
        // Arrange
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction
                {
                    Name = "read_file",
                    Arguments = "{\"path\": \"test.txt\"", // Missing closing brace
                },
            },
        };

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.AllSucceeded.Should().BeTrue();
        result.Repairs.Should().HaveCount(1);
        result.Repairs[0].Repairs.Should().Contain("balanced_braces");
    }

    [Fact]
    public void Parse_IrrepairableJson_ReturnsError()
    {
        // Arrange
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction
                {
                    Name = "read_file",
                    Arguments = "this is not json at all {{{{",
                },
            },
        };

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.Errors[0].ErrorCode.Should().Be("ACODE-TLP-004");
        result.Errors[0].RawArguments.Should().Be("this is not json at all {{{{");
    }

    [Fact]
    public void Parse_MixedValidAndInvalid_ReturnsBoth()
    {
        // Arrange
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_1",
                Function = new OllamaFunction { Name = "read_file", Arguments = "{\"path\": \"test.txt\"}" },
            },
            new OllamaToolCall
            {
                Id = "call_2",
                Function = null, // Invalid
            },
            new OllamaToolCall
            {
                Id = "call_3",
                Function = new OllamaFunction { Name = "write_file", Arguments = "{\"content\": \"hello\"}" },
            },
        };

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.ToolCalls.Should().HaveCount(2);
        result.Errors.Should().HaveCount(1);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public void Parse_EmptyArguments_UsesEmptyObject()
    {
        // Arrange
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction
                {
                    Name = "git_status",
                    Arguments = string.Empty, // Empty defaults to "{}"
                },
            },
        };

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.AllSucceeded.Should().BeTrue();
        result.ToolCalls[0].Arguments.GetRawText().Should().Be("{}");
    }

    [Fact]
    public void Parse_FunctionNameTooLong_ReturnsError()
    {
        // Arrange
        var longName = new string('a', 65); // Max is 64
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction
                {
                    Name = longName,
                    Arguments = "{}",
                },
            },
        };

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.Errors[0].ErrorCode.Should().Be("ACODE-TLP-005");
    }

    [Fact]
    public void Parse_SingleQuoteJson_RepairsAndParses()
    {
        // Arrange
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction
                {
                    Name = "read_file",
                    Arguments = "{'path': 'test.txt'}",
                },
            },
        };

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.AllSucceeded.Should().BeTrue();
        result.Repairs[0].WasRepaired.Should().BeTrue();
        result.Repairs[0].Repairs.Should().Contain("replaced_single_quotes");
    }

    [Fact]
    public void Parse_WithCustomIdGenerator_UsesProvidedGenerator()
    {
        // Arrange
        var counter = 0;
        var customParser = new ToolCallParser(idGenerator: () => $"custom_{++counter}");
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = null,
                Function = new OllamaFunction { Name = "test_tool", Arguments = "{}" },
            },
            new OllamaToolCall
            {
                Id = null,
                Function = new OllamaFunction { Name = "test_tool2", Arguments = "{}" },
            },
        };

        // Act
        var result = customParser.Parse(toolCalls);

        // Assert
        result.ToolCalls[0].Id.Should().Be("custom_1");
        result.ToolCalls[1].Id.Should().Be("custom_2");
    }

    [Fact]
    public void Parse_PreservesOriginalArgumentsWhenValid()
    {
        // Arrange
        var originalArgs = "{\"path\":\"test.txt\",\"encoding\":\"utf-8\"}";
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction
                {
                    Name = "read_file",
                    Arguments = originalArgs,
                },
            },
        };

        // Act
        var result = parser.Parse(toolCalls);

        // Assert
        result.AllSucceeded.Should().BeTrue();
        result.Repairs.Should().BeEmpty();
        result.ToolCalls[0].Arguments.GetRawText().Should().Be(originalArgs);
    }
}
