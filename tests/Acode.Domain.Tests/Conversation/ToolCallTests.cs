// tests/Acode.Domain.Tests/Conversation/ToolCallTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code
namespace Acode.Domain.Tests.Conversation;

using System;
using System.Text.Json;
using Acode.Domain.Conversation;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for ToolCall value object.
/// Verifies tool call creation, status transitions, and JSON argument parsing.
/// </summary>
public sealed class ToolCallTests
{
    [Fact]
    public void Constructor_WithValidInputs_CreatesToolCall()
    {
        // Arrange & Act
        var toolCall = new ToolCall("call_001", "grep_search", """{"query": "user auth"}""");

        // Assert
        toolCall.Should().NotBeNull();
        toolCall.Id.Should().Be("call_001");
        toolCall.Function.Should().Be("grep_search");
        toolCall.Arguments.Should().Be("""{"query": "user auth"}""");
        toolCall.Result.Should().BeNull();
        toolCall.Status.Should().Be(ToolCallStatus.Pending);
    }

    [Fact]
    public void Constructor_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ToolCall(null!, "function", "{}");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("id");
    }

    [Fact]
    public void Constructor_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ToolCall("call_001", null!, "{}");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("function");
    }

    [Fact]
    public void Constructor_WithNullArguments_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ToolCall("call_001", "function", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("arguments");
    }

    [Fact]
    public void WithResult_ReturnsNewInstanceWithResult()
    {
        // Arrange
        var original = new ToolCall("call_001", "grep_search", """{"query": "test"}""");

        // Act
        var updated = original.WithResult("Found 3 matches");

        // Assert
        updated.Should().NotBeSameAs(original); // Record creates new instance
        updated.Id.Should().Be(original.Id);
        updated.Function.Should().Be(original.Function);
        updated.Arguments.Should().Be(original.Arguments);
        updated.Result.Should().Be("Found 3 matches");
        updated.Status.Should().Be(ToolCallStatus.Completed);
    }

    [Fact]
    public void WithError_ReturnsNewInstanceWithError()
    {
        // Arrange
        var original = new ToolCall("call_001", "read_file", """{"path": "/invalid"}""");

        // Act
        var updated = original.WithError("File not found");

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.Result.Should().Be("File not found");
        updated.Status.Should().Be(ToolCallStatus.Failed);
    }

    [Fact]
    public void ParseArguments_WithValidJson_DeserializesCorrectly()
    {
        // Arrange
        var json = """{"query": "authentication", "isRegexp": false}""";
        var toolCall = new ToolCall("call_001", "grep_search", json);

        // Act
        var parsed = toolCall.ParseArguments<GrepSearchArgs>();

        // Assert
        parsed.Should().NotBeNull();
        parsed!.Query.Should().Be("authentication");
        parsed.IsRegexp.Should().BeFalse();
    }

    [Fact]
    public void ParseArguments_WithInvalidJson_ReturnsNull()
    {
        // Arrange
        var toolCall = new ToolCall("call_001", "function", "not json");

        // Act
        var act = () => toolCall.ParseArguments<GrepSearchArgs>();

        // Assert - JsonException will be thrown
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void EqualityComparison_WithSameValues_AreEqual()
    {
        // Arrange
        var call1 = new ToolCall("call_001", "function", """{"arg": "value"}""");
        var call2 = new ToolCall("call_001", "function", """{"arg": "value"}""");

        // Act & Assert
        call1.Should().Be(call2); // Record equality
        call1.GetHashCode().Should().Be(call2.GetHashCode());
    }

    [Fact]
    public void EqualityComparison_WithDifferentIds_AreNotEqual()
    {
        // Arrange
        var call1 = new ToolCall("call_001", "function", "{}");
        var call2 = new ToolCall("call_002", "function", "{}");

        // Act & Assert
        call1.Should().NotBe(call2);
    }

    [Fact]
    public void SerializeDeserialize_PreservesToolCallData()
    {
        // Arrange
        var original = new ToolCall("call_001", "grep_search", """{"query": "test"}""")
            .WithResult("Found 5 matches");

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<ToolCall>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(original.Id);
        deserialized.Function.Should().Be(original.Function);
        deserialized.Arguments.Should().Be(original.Arguments);
        deserialized.Result.Should().Be(original.Result);
        deserialized.Status.Should().Be(original.Status);
    }

    // Helper class for testing argument parsing
    private sealed class GrepSearchArgs
    {
        public string Query { get; set; } = string.Empty;

        public bool IsRegexp { get; set; }
    }
}
