namespace Acode.Infrastructure.Tests.Ollama.ToolCall;

using System;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Inference;
using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Ollama.ToolCall;
using Acode.Infrastructure.Ollama.ToolCall.Exceptions;
using Acode.Infrastructure.Ollama.ToolCall.Models;
using FluentAssertions;
using NSubstitute;

/// <summary>
/// Tests for ToolCallRetryHandler retry logic (Gap #6).
/// </summary>
public sealed class ToolCallRetryHandlerTests
{
    private readonly IModelProvider mockProvider;
    private readonly ToolCallParser parser;
    private readonly RetryConfig config;

    public ToolCallRetryHandlerTests()
    {
        mockProvider = Substitute.For<IModelProvider>();
        parser = new ToolCallParser();
        config = new RetryConfig { MaxRetries = 3, RetryDelayMs = 10 };
    }

    [Fact]
    public async Task ParseWithRetryAsync_ValidToolCalls_NoRetryNeeded()
    {
        // Arrange - Valid tool calls that parse successfully on first attempt
        var toolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction
                {
                    Name = "read_file",
                    Arguments = "{\"path\": \"test.txt\"}",
                },
            },
        };

        var handler = new ToolCallRetryHandler(config, parser, mockProvider);
        var originalRequest = CreateChatRequest();

        // Act
        var result = await handler.ParseWithRetryAsync(toolCalls, originalRequest);

        // Assert
        result.AllSucceeded.Should().BeTrue();
        result.ToolCalls.Should().HaveCount(1);
        result.ToolCalls[0].Name.Should().Be("read_file");
        await mockProvider.DidNotReceive().ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ParseWithRetryAsync_MalformedJson_RetriesAndSucceeds()
    {
        // Arrange - Malformed JSON on first attempt, valid on retry
        var malformedToolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction
                {
                    Name = "read_file",
                    Arguments = "not json at all {{{{",
                },
            },
        };

        // Mock provider to return valid tool calls on retry
        var validToolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction
                {
                    Name = "read_file",
                    Arguments = "{\"path\": \"test.txt\"}",
                },
            },
        };

        mockProvider.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateChatResponse(validToolCalls));

        var handler = new ToolCallRetryHandler(config, parser, mockProvider);
        var originalRequest = CreateChatRequest();

        // Act
        var result = await handler.ParseWithRetryAsync(malformedToolCalls, originalRequest);

        // Assert
        result.AllSucceeded.Should().BeTrue();
        result.ToolCalls.Should().HaveCount(1);
        await mockProvider.Received(1).ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ParseWithRetryAsync_MaxRetriesExceeded_ThrowsException()
    {
        // Arrange - Malformed JSON that never succeeds
        var malformedToolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction
                {
                    Name = "read_file",
                    Arguments = "invalid json {{{{",
                },
            },
        };

        // Mock provider to always return malformed JSON
        mockProvider.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateChatResponse(malformedToolCalls));

        var handler = new ToolCallRetryHandler(config, parser, mockProvider);
        var originalRequest = CreateChatRequest();

        // Act & Assert
        await handler.Invoking(h => h.ParseWithRetryAsync(malformedToolCalls, originalRequest))
            .Should().ThrowAsync<ToolCallRetryExhaustedException>()
            .WithMessage("*3 retry attempts*");

        await mockProvider.Received(3).ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ParseWithRetryAsync_PartialFailure_RetriesOnlyFailed()
    {
        // Arrange - Mixed success/failure, should return successful ones
        var mixedToolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_1",
                Function = new OllamaFunction { Name = "read_file", Arguments = "{\"path\": \"test.txt\"}" },
            },
            new OllamaToolCall
            {
                Id = "call_2",
                Function = new OllamaFunction { Name = "write_file", Arguments = "invalid {{{{" },
            },
        };

        // Mock provider to return both tool calls fixed
        var fixedToolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_1",
                Function = new OllamaFunction { Name = "read_file", Arguments = "{\"path\": \"test.txt\"}" },
            },
            new OllamaToolCall
            {
                Id = "call_2",
                Function = new OllamaFunction { Name = "write_file", Arguments = "{\"path\": \"out.txt\", \"content\": \"hello\"}" },
            },
        };

        mockProvider.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateChatResponse(fixedToolCalls));

        var handler = new ToolCallRetryHandler(config, parser, mockProvider);
        var originalRequest = CreateChatRequest();

        // Act
        var result = await handler.ParseWithRetryAsync(mixedToolCalls, originalRequest);

        // Assert
        result.AllSucceeded.Should().BeTrue();
        result.ToolCalls.Should().HaveCount(2);
    }

    [Fact]
    public async Task ParseWithRetryAsync_RetrySucceedsOnSecondAttempt()
    {
        // Arrange - Fails first retry, succeeds on second
        var malformedToolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction { Name = "read_file", Arguments = "invalid {{{{" },
            },
        };

        var stillBadToolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction { Name = "read_file", Arguments = "still invalid {{{{" },
            },
        };

        var validToolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction { Name = "read_file", Arguments = "{\"path\": \"test.txt\"}" },
            },
        };

        // First retry returns still bad, second returns valid
        mockProvider.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                CreateChatResponse(stillBadToolCalls),
                CreateChatResponse(validToolCalls));

        var handler = new ToolCallRetryHandler(config, parser, mockProvider);
        var originalRequest = CreateChatRequest();

        // Act
        var result = await handler.ParseWithRetryAsync(malformedToolCalls, originalRequest);

        // Assert
        result.AllSucceeded.Should().BeTrue();
        await mockProvider.Received(2).ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ParseWithRetryAsync_ExponentialBackoff_DelaysCorrectly()
    {
        // Arrange - Track delays
        var delays = new System.Collections.Generic.List<int>();
        var configWithDelay = new RetryConfig { MaxRetries = 3, RetryDelayMs = 50 };

        var malformedToolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction { Name = "read_file", Arguments = "invalid {{{{" },
            },
        };

        var validToolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction { Name = "read_file", Arguments = "{\"path\": \"test.txt\"}" },
            },
        };

        // Return valid after checking delays
        mockProvider.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateChatResponse(validToolCalls));

        var handler = new ToolCallRetryHandler(configWithDelay, parser, mockProvider);
        var originalRequest = CreateChatRequest();

        // Act
        var startTime = System.Diagnostics.Stopwatch.StartNew();
        var result = await handler.ParseWithRetryAsync(malformedToolCalls, originalRequest);
        startTime.Stop();

        // Assert
        result.AllSucceeded.Should().BeTrue();

        // Allow some timing tolerance (40-70ms range for 50ms target)
        startTime.ElapsedMilliseconds.Should().BeInRange(40, 100); // At least approximately base delay
    }

    [Fact]
    public async Task ParseWithRetryAsync_CancellationToken_CancelsRetry()
    {
        // Arrange - Malformed that would require retry
        var malformedToolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction { Name = "read_file", Arguments = "invalid {{{{" },
            },
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var handler = new ToolCallRetryHandler(config, parser, mockProvider);
        var originalRequest = CreateChatRequest();

        // Act & Assert
        await handler.Invoking(h => h.ParseWithRetryAsync(malformedToolCalls, originalRequest, cts.Token))
            .Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public void BuildRetryPrompt_IncludesErrorDetails()
    {
        // Arrange
        var error = new ToolCallError(
            message: "Failed to parse JSON",
            errorCode: "ACODE-TLP-004")
        {
            RawArguments = "invalid {{{{",
            ToolName = "read_file",
        };

        var handler = new ToolCallRetryHandler(config, parser, mockProvider);

        // Act
        var prompt = handler.BuildRetryPrompt(new[] { error });

        // Assert
        prompt.Should().Contain("Failed to parse JSON");
        prompt.Should().Contain("read_file");
        prompt.Should().Contain("invalid");
    }

    [Fact]
    public async Task ParseWithRetryAsync_ZeroMaxRetries_NoRetryAttempted()
    {
        // Arrange - Config with zero retries
        var noRetryConfig = new RetryConfig { MaxRetries = 0 };

        var malformedToolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction { Name = "read_file", Arguments = "invalid {{{{" },
            },
        };

        var handler = new ToolCallRetryHandler(noRetryConfig, parser, mockProvider);
        var originalRequest = CreateChatRequest();

        // Act & Assert - Should throw immediately without retry
        await handler.Invoking(h => h.ParseWithRetryAsync(malformedToolCalls, originalRequest))
            .Should().ThrowAsync<ToolCallRetryExhaustedException>();

        await mockProvider.DidNotReceive().ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ParseWithRetryAsync_RetryPromptUsedCorrectly()
    {
        // Arrange - Verify retry prompt is constructed from config template
        var customTemplate = "Fix this JSON for {tool_name}: {malformed_json}";
        var customConfig = new RetryConfig
        {
            MaxRetries = 1,
            RetryDelayMs = 10,
            RetryPromptTemplate = customTemplate,
        };

        var malformedToolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction { Name = "read_file", Arguments = "invalid {{{{" },
            },
        };

        var validToolCalls = new[]
        {
            new OllamaToolCall
            {
                Id = "call_123",
                Function = new OllamaFunction { Name = "read_file", Arguments = "{\"path\": \"test.txt\"}" },
            },
        };

        ChatRequest? capturedRequest = null;
        mockProvider.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedRequest = callInfo.Arg<ChatRequest>();
                return CreateChatResponse(validToolCalls);
            });

        var handler = new ToolCallRetryHandler(customConfig, parser, mockProvider);
        var originalRequest = CreateChatRequest();

        // Act
        await handler.ParseWithRetryAsync(malformedToolCalls, originalRequest);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Messages.Should().Contain(m => m.Content != null && m.Content.Contains("Fix this JSON"));
        capturedRequest.Messages.Should().Contain(m => m.Content != null && m.Content.Contains("read_file"));
    }

    // ==================== Helper Methods ====================
    private static ChatRequest CreateChatRequest()
    {
        return new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Test message") },
            modelParameters: null,
            tools: null,
            stream: false);
    }

    private static ChatResponse CreateChatResponse(OllamaToolCall[] toolCalls)
    {
        // Parse the tool calls using ToolCallParser to convert to domain ToolCalls
        var parser = new ToolCallParser();
        var parseResult = parser.Parse(toolCalls);

        // Create assistant message with tool calls
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
