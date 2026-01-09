// tests/Acode.Domain.Tests/Conversation/MessageTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code
#pragma warning disable CA1062 // Null validation not needed for xUnit Theory parameters
namespace Acode.Domain.Tests.Conversation;

using System;
using System.Text.Json;
using Acode.Domain.Conversation;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for Message entity.
/// Verifies message creation, role validation, tool call management, and content limits.
/// </summary>
public sealed class MessageTests
{
    [Fact]
    public void Create_WithValidInputs_CreatesMessage()
    {
        // Arrange
        var runId = RunId.NewId();
        var role = "user";
        var content = "Hello, how can you help?";

        // Act
        var message = Message.Create(runId, role, content, sequenceNumber: 1);

        // Assert
        message.Should().NotBeNull();
        message.Id.Should().NotBe(MessageId.Empty);
        message.RunId.Should().Be(runId);
        message.Role.Should().Be("user"); // Normalized to lowercase
        message.Content.Should().Be(content);
        message.SequenceNumber.Should().Be(1);
        message.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        message.SyncStatus.Should().Be(SyncStatus.Pending);
        message.ToolCalls.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyRunId_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => Message.Create(RunId.Empty, "user", "Hello");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*RunId*");
    }

    [Theory]
    [InlineData("user")]
    [InlineData("assistant")]
    [InlineData("system")]
    [InlineData("tool")]
    [InlineData("USER")] // Test case insensitivity
    [InlineData("ASSISTANT")]
    public void Create_WithValidRoles_AcceptsRole(string role)
    {
        // Arrange
        var runId = RunId.NewId();

        // Act
        var message = Message.Create(runId, role, "Content");

        // Assert
        message.Role.Should().Be(role.ToLowerInvariant()); // Normalized to lowercase
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("function")] // Not a valid role
    public void Create_WithInvalidRole_ThrowsArgumentException(string invalidRole)
    {
        // Arrange
        var runId = RunId.NewId();

        // Act
        var act = () => Message.Create(runId, invalidRole, "Content");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*role*");
    }

    [Fact]
    public void Create_WithNullContent_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => Message.Create(RunId.NewId(), "user", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("content");
    }

    [Fact]
    public void Create_WithContentExceeding100KB_ThrowsArgumentException()
    {
        // Arrange
        var largeContent = new string('a', (100 * 1024) + 1); // 100KB + 1 byte

        // Act
        var act = () => Message.Create(RunId.NewId(), "user", largeContent);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*102400*"); // Matches the byte count in error message
    }

    [Fact]
    public void Create_WithContentAt100KB_Succeeds()
    {
        // Arrange
        var largeContent = new string('a', 100 * 1024); // Exactly 100KB

        // Act
        var message = Message.Create(RunId.NewId(), "user", largeContent);

        // Assert
        message.Content.Should().HaveLength(100 * 1024);
        message.Content.Should().Be(largeContent);
    }

    [Fact]
    public void AddToolCalls_ToAssistantMessage_AddsToolCalls()
    {
        // Arrange
        var message = Message.Create(RunId.NewId(), "assistant", "I'll search for that.");

        var toolCalls = new[]
        {
            new ToolCall("call_001", "grep_search", """{"query": "user authentication"}"""),
            new ToolCall("call_002", "read_file", """{"filePath": "src/auth.ts"}"""),
        };

        // Act
        message.AddToolCalls(toolCalls);

        // Assert
        message.ToolCalls.Should().HaveCount(2);
        message.ToolCalls[0].Id.Should().Be("call_001");
        message.ToolCalls[0].Function.Should().Be("grep_search");
        message.ToolCalls[1].Id.Should().Be("call_002");
        message.ToolCalls[1].Function.Should().Be("read_file");
        message.SyncStatus.Should().Be(SyncStatus.Pending);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("system")]
    [InlineData("tool")]
    public void AddToolCalls_ToNonAssistantMessage_ThrowsInvalidOperationException(string role)
    {
        // Arrange
        var message = Message.Create(RunId.NewId(), role, "Content");
        var toolCalls = new[] { new ToolCall("call_001", "function", "{}") };

        // Act
        var act = () => message.AddToolCalls(toolCalls);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*assistant*");
    }

    [Fact]
    public void GetToolCallsJson_WithToolCalls_ReturnsJson()
    {
        // Arrange
        var message = Message.Create(RunId.NewId(), "assistant", "Searching...");
        var toolCalls = new[]
        {
            new ToolCall("call_001", "grep_search", """{"query": "test"}"""),
            new ToolCall("call_002", "read_file", """{"path": "/file.txt"}"""),
        };
        message.AddToolCalls(toolCalls);

        // Act
        var json = message.GetToolCallsJson();

        // Assert
        json.Should().NotBeNullOrEmpty();

        var deserialized = JsonSerializer.Deserialize<ToolCall[]>(json!);
        deserialized.Should().HaveCount(2);
        deserialized![0].Id.Should().Be("call_001");
        deserialized[0].Function.Should().Be("grep_search");
        deserialized[1].Id.Should().Be("call_002");
        deserialized[1].Function.Should().Be("read_file");
    }

    [Fact]
    public void GetToolCallsJson_WithNoToolCalls_ReturnsNull()
    {
        // Arrange
        var message = Message.Create(RunId.NewId(), "user", "Hello");

        // Act
        var json = message.GetToolCallsJson();

        // Assert
        json.Should().BeNull();
    }

    [Fact]
    public void Reconstitute_CreatesMessageFromPersistedData()
    {
        // Arrange
        var id = MessageId.NewId();
        var runId = RunId.NewId();
        var role = "assistant";
        var content = "Here's the result";
        var toolCalls = new[]
        {
            new ToolCall("call_001", "grep_search", """{"query": "test"}"""),
        };
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var sequenceNumber = 2;
        var syncStatus = SyncStatus.Synced;

        // Act
        var message = Message.Reconstitute(
            id,
            runId,
            role,
            content,
            toolCalls,
            createdAt,
            sequenceNumber,
            syncStatus);

        // Assert
        message.Id.Should().Be(id);
        message.RunId.Should().Be(runId);
        message.Role.Should().Be(role);
        message.Content.Should().Be(content);
        message.ToolCalls.Should().HaveCount(1);
        message.ToolCalls[0].Id.Should().Be("call_001");
        message.CreatedAt.Should().Be(createdAt);
        message.SequenceNumber.Should().Be(sequenceNumber);
        message.SyncStatus.Should().Be(syncStatus);
    }

    [Fact]
    public void Reconstitute_WithNullToolCalls_CreatesMessageWithEmptyToolCalls()
    {
        // Act
        var message = Message.Reconstitute(
            MessageId.NewId(),
            RunId.NewId(),
            "user",
            "Content",
            toolCalls: null,
            DateTimeOffset.UtcNow,
            1,
            SyncStatus.Pending);

        // Assert
        message.ToolCalls.Should().BeEmpty();
    }
}
