namespace Acode.Integration.Tests.Ollama;

using System;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Inference;
using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Ollama.Mapping;
using Acode.Infrastructure.Ollama.Models;
using Acode.Infrastructure.Ollama.ToolCall;
using Acode.Infrastructure.Ollama.ToolCall.Exceptions;
using Acode.Infrastructure.Ollama.ToolCall.Models;
using FluentAssertions;
using NSubstitute;

/// <summary>
/// End-to-end integration tests for tool call parsing flow.
/// Tests the complete pipeline: Ollama response → parsing → retry → domain ToolCall.
/// </summary>
public sealed class ToolCallIntegrationTests
{
    private readonly IModelProvider mockProvider;
    private readonly ToolCallParser parser;
    private readonly RetryConfig retryConfig;

    public ToolCallIntegrationTests()
    {
        mockProvider = Substitute.For<IModelProvider>();
        parser = new ToolCallParser();
        retryConfig = new RetryConfig
        {
            MaxRetries = 3,
            EnableAutoRepair = true,
            RetryDelayMs = 10, // Fast for tests
        };
    }

    [Fact]
    public void EndToEnd_ValidToolCall_ParsesSuccessfully()
    {
        // Arrange - Create valid Ollama response with tool call
        var message = new OllamaMessage(
            role: "assistant",
            content: string.Empty,
            toolCalls: new[]
            {
                new Acode.Infrastructure.Ollama.Models.OllamaToolCallResponse(
                    id: "call_001",
                    function: new Acode.Infrastructure.Ollama.Models.OllamaToolCallFunction(
                        name: "read_file",
                        arguments: "{\"path\": \"/home/user/test.txt\", \"encoding\": \"utf-8\"}")),
            });

        var ollamaResponse = new OllamaResponse(
            model: "llama3.2",
            createdAt: "2026-01-13T10:30:00Z",
            message: message,
            done: true,
            doneReason: null,
            totalDuration: 1234567890,
            promptEvalCount: 10,
            evalCount: 20);

        // Act - Map Ollama response to domain ChatResponse
        var chatResponse = OllamaResponseMapper.Map(ollamaResponse, parser);

        // Assert - Tool call parsed correctly
        chatResponse.Should().NotBeNull();
        chatResponse.Message.Should().NotBeNull();
        chatResponse.Message.ToolCalls.Should().NotBeNull();
        chatResponse.Message.ToolCalls.Should().HaveCount(1);

        var toolCall = chatResponse.Message.ToolCalls![0];
        toolCall.Name.Should().Be("read_file");
        toolCall.Arguments.GetProperty("path").GetString().Should().Be("/home/user/test.txt");
        toolCall.Arguments.GetProperty("encoding").GetString().Should().Be("utf-8");

        // Verify FinishReason is set correctly
        chatResponse.FinishReason.Should().Be(FinishReason.ToolCalls);
    }

    [Fact]
    public void EndToEnd_MultipleToolCalls_AllParsed()
    {
        // Arrange - Create Ollama response with multiple tool calls
        var message = new OllamaMessage(
            role: "assistant",
            content: string.Empty,
            toolCalls: new[]
            {
                new Acode.Infrastructure.Ollama.Models.OllamaToolCallResponse(
                    id: "call_001",
                    function: new Acode.Infrastructure.Ollama.Models.OllamaToolCallFunction(
                        name: "read_file",
                        arguments: "{\"path\": \"input.txt\"}")),
                new Acode.Infrastructure.Ollama.Models.OllamaToolCallResponse(
                    id: "call_002",
                    function: new Acode.Infrastructure.Ollama.Models.OllamaToolCallFunction(
                        name: "write_file",
                        arguments: "{\"path\": \"output.txt\", \"content\": \"hello world\"}")),
                new Acode.Infrastructure.Ollama.Models.OllamaToolCallResponse(
                    id: "call_003",
                    function: new Acode.Infrastructure.Ollama.Models.OllamaToolCallFunction(
                        name: "execute_command",
                        arguments: "{\"command\": \"ls\", \"args\": [\"-la\"]}")),
            });

        var ollamaResponse = new OllamaResponse(
            model: "llama3.2",
            createdAt: "2026-01-13T10:30:00Z",
            message: message,
            done: true,
            totalDuration: 1234567890);

        // Act
        var chatResponse = OllamaResponseMapper.Map(ollamaResponse, parser);

        // Assert - All 3 tool calls parsed
        chatResponse.Message.ToolCalls.Should().HaveCount(3);

        chatResponse.Message.ToolCalls![0].Name.Should().Be("read_file");
        chatResponse.Message.ToolCalls[0].Arguments.GetProperty("path").GetString().Should().Be("input.txt");

        chatResponse.Message.ToolCalls[1].Name.Should().Be("write_file");
        chatResponse.Message.ToolCalls[1].Arguments.GetProperty("path").GetString().Should().Be("output.txt");
        chatResponse.Message.ToolCalls[1].Arguments.GetProperty("content").GetString().Should().Be("hello world");

        chatResponse.Message.ToolCalls[2].Name.Should().Be("execute_command");
        chatResponse.Message.ToolCalls[2].Arguments.GetProperty("command").GetString().Should().Be("ls");
        chatResponse.Message.ToolCalls[2].Arguments.GetProperty("args")[0].GetString().Should().Be("-la");

        chatResponse.FinishReason.Should().Be(FinishReason.ToolCalls);
    }

    [Fact]
    public async Task EndToEnd_MalformedToolCall_AutoRepairSucceeds()
    {
        // Arrange - Tool call with trailing comma (repairable)
        var malformedToolCalls = new[]
        {
            new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaToolCall
            {
                Id = "call_123",
                Function = new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaFunction
                {
                    Name = "read_file",
                    Arguments = "{\"path\": \"test.txt\",}", // Trailing comma
                },
            },
        };

        var handler = new ToolCallRetryHandler(retryConfig, parser, mockProvider);
        var originalRequest = CreateChatRequest();

        // Act - Parse with retry (should auto-repair without retry)
        var result = await handler.ParseWithRetryAsync(malformedToolCalls, originalRequest);

        // Assert - Auto-repair succeeded, no retry needed
        result.AllSucceeded.Should().BeTrue();
        result.ToolCalls.Should().HaveCount(1);
        result.ToolCalls[0].Name.Should().Be("read_file");
        result.ToolCalls[0].Arguments.GetProperty("path").GetString().Should().Be("test.txt");
        result.Repairs.Should().HaveCount(1);
        result.Repairs[0].WasRepaired.Should().BeTrue();

        // Verify no retry was needed (provider not called)
        await mockProvider.DidNotReceive().ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EndToEnd_MalformedToolCall_RetriesAndSucceeds()
    {
        // Arrange - Malformed JSON that can't be auto-repaired
        var malformedToolCalls = new[]
        {
            new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaToolCall
            {
                Id = "call_123",
                Function = new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaFunction
                {
                    Name = "read_file",
                    Arguments = "not valid json at all {{{{", // Not repairable
                },
            },
        };

        // Mock provider to return corrected tool call on retry
        var correctedToolCalls = new[]
        {
            new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaToolCall
            {
                Id = "call_123",
                Function = new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaFunction
                {
                    Name = "read_file",
                    Arguments = "{\"path\": \"test.txt\"}", // Corrected
                },
            },
        };

        mockProvider.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateChatResponse(correctedToolCalls));

        var handler = new ToolCallRetryHandler(retryConfig, parser, mockProvider);
        var originalRequest = CreateChatRequest();

        // Act - Parse with retry
        var result = await handler.ParseWithRetryAsync(malformedToolCalls, originalRequest);

        // Assert - Retry succeeded
        result.AllSucceeded.Should().BeTrue();
        result.ToolCalls.Should().HaveCount(1);
        result.ToolCalls[0].Name.Should().Be("read_file");
        result.ToolCalls[0].Arguments.GetProperty("path").GetString().Should().Be("test.txt");

        // Verify retry was performed (provider called once)
        await mockProvider.Received(1).ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EndToEnd_RetryExhausted_ThrowsException()
    {
        // Arrange - Malformed JSON that never succeeds
        var malformedToolCalls = new[]
        {
            new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaToolCall
            {
                Id = "call_123",
                Function = new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaFunction
                {
                    Name = "read_file",
                    Arguments = "persistently invalid {{{{",
                },
            },
        };

        // Mock provider to always return malformed JSON
        mockProvider.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateChatResponse(malformedToolCalls));

        var handler = new ToolCallRetryHandler(retryConfig, parser, mockProvider);
        var originalRequest = CreateChatRequest();

        // Act & Assert - Should exhaust retries and throw
        await handler.Invoking(h => h.ParseWithRetryAsync(malformedToolCalls, originalRequest))
            .Should().ThrowAsync<ToolCallRetryExhaustedException>()
            .WithMessage("*3 retry attempts*");

        // Verify all retries were attempted
        await mockProvider.Received(3).ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EndToEnd_PartialFailure_SuccessfulCallsReturned()
    {
        // Arrange - Mix of valid and malformed tool calls
        var mixedToolCalls = new[]
        {
            new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaToolCall
            {
                Id = "call_1",
                Function = new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaFunction
                {
                    Name = "read_file",
                    Arguments = "{\"path\": \"test.txt\"}", // Valid
                },
            },
            new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaToolCall
            {
                Id = "call_2",
                Function = new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaFunction
                {
                    Name = "write_file",
                    Arguments = "{\"path\": \"out.txt\",}", // Trailing comma (repairable)
                },
            },
            new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaToolCall
            {
                Id = "call_3",
                Function = new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaFunction
                {
                    Name = "execute_command",
                    Arguments = "invalid {{{{", // Not repairable
                },
            },
        };

        // Mock provider to return corrected call_3 on retry
        var correctedToolCalls = new[]
        {
            new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaToolCall
            {
                Id = "call_1",
                Function = new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaFunction
                {
                    Name = "read_file",
                    Arguments = "{\"path\": \"test.txt\"}",
                },
            },
            new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaToolCall
            {
                Id = "call_2",
                Function = new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaFunction
                {
                    Name = "write_file",
                    Arguments = "{\"path\": \"out.txt\"}",
                },
            },
            new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaToolCall
            {
                Id = "call_3",
                Function = new Acode.Infrastructure.Ollama.ToolCall.Models.OllamaFunction
                {
                    Name = "execute_command",
                    Arguments = "{\"command\": \"ls\"}", // Corrected
                },
            },
        };

        mockProvider.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateChatResponse(correctedToolCalls));

        var handler = new ToolCallRetryHandler(retryConfig, parser, mockProvider);
        var originalRequest = CreateChatRequest();

        // Act
        var result = await handler.ParseWithRetryAsync(mixedToolCalls, originalRequest);

        // Assert - All 3 tool calls successfully parsed
        result.AllSucceeded.Should().BeTrue();
        result.ToolCalls.Should().HaveCount(3);

        result.ToolCalls[0].Name.Should().Be("read_file");
        result.ToolCalls[1].Name.Should().Be("write_file");
        result.ToolCalls[2].Name.Should().Be("execute_command");

        // Verify one repair happened (call_2)
        result.Repairs.Should().HaveCount(1);

        // Verify retry performed (for call_3)
        await mockProvider.Received(1).ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void EndToEnd_NoToolCalls_ReturnsRegularMessage()
    {
        // Arrange - Response without tool calls
        var message = new OllamaMessage(
            role: "assistant",
            content: "I'll help you with that task.",
            toolCalls: null);

        var ollamaResponse = new OllamaResponse(
            model: "llama3.2",
            createdAt: "2026-01-13T10:30:00Z",
            message: message,
            done: true,
            totalDuration: 1234567890);

        // Act
        var chatResponse = OllamaResponseMapper.Map(ollamaResponse, parser);

        // Assert - Regular message, no tool calls
        chatResponse.Message.Content.Should().Be("I'll help you with that task.");
        chatResponse.Message.ToolCalls.Should().BeNull();
        chatResponse.FinishReason.Should().Be(FinishReason.Stop);
    }

    [Fact]
    public void EndToEnd_EmptyToolCallsArray_ReturnsRegularMessage()
    {
        // Arrange - Empty tool calls array
        var message = new OllamaMessage(
            role: "assistant",
            content: "No tools needed for this response.",
            toolCalls: Array.Empty<Acode.Infrastructure.Ollama.Models.OllamaToolCallResponse>());

        var ollamaResponse = new OllamaResponse(
            model: "llama3.2",
            createdAt: "2026-01-13T10:30:00Z",
            message: message,
            done: true,
            totalDuration: 1234567890);

        // Act
        var chatResponse = OllamaResponseMapper.Map(ollamaResponse, parser);

        // Assert - Regular message, no tool calls
        chatResponse.Message.Content.Should().Be("No tools needed for this response.");
        chatResponse.Message.ToolCalls.Should().BeNull();
        chatResponse.FinishReason.Should().Be(FinishReason.Stop);
    }

    // ==================== Helper Methods ====================
    private static ChatRequest CreateChatRequest()
    {
        return new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Test prompt") },
            modelParameters: null,
            tools: null,
            stream: false);
    }

    private static ChatResponse CreateChatResponse(Acode.Infrastructure.Ollama.ToolCall.Models.OllamaToolCall[] toolCalls)
    {
        // Parse tool calls to create proper ChatMessage
        var parser = new ToolCallParser();
        var parseResult = parser.Parse(toolCalls);

        var message = parseResult.AllSucceeded
            ? ChatMessage.CreateAssistant(content: null, toolCalls: parseResult.ToolCalls)
            : ChatMessage.CreateAssistant("Test response");

        return new ChatResponse(
            Id: "test-id",
            Message: message,
            FinishReason: parseResult.AllSucceeded ? FinishReason.ToolCalls : FinishReason.Stop,
            Usage: new UsageInfo(0, 0),
            Metadata: new ResponseMetadata("test", "test-model", TimeSpan.Zero),
            Created: DateTimeOffset.UtcNow,
            Model: "test-model");
    }
}
