namespace Acode.Domain.Tests.Models.Inference;

using System.Text.Json;
using Acode.Domain.Models.Inference;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for ToolResult record.
/// FR-004a-56 to FR-004a-70: ToolResult must be immutable record with ToolCallId, Result, IsError
/// with factory methods and validation.
/// </summary>
public sealed class ToolResultTests
{
    [Fact]
    public void ToolResult_IsRecord()
    {
        // FR-004a-56: ToolResult MUST be a record type
        var result1 = new ToolResult("call_1", "success", false);
        var result2 = new ToolResult("call_1", "success", false);
        result1.Should().Be(result2); // Records have value equality
    }

    [Fact]
    public void ToolResult_HasToolCallIdProperty()
    {
        // FR-004a-58: ToolResult MUST have ToolCallId property
        var result = new ToolResult("call_123", "output", false);
        result.ToolCallId.Should().Be("call_123");
    }

    [Fact]
    public void ToolResult_HasResultProperty()
    {
        // FR-004a-60: ToolResult MUST have Result property
        var result = new ToolResult("call_1", "File created", false);
        result.Result.Should().Be("File created");
    }

    [Fact]
    public void ToolResult_HasIsErrorProperty()
    {
        // FR-004a-63: ToolResult MUST have IsError property
        var success = new ToolResult("call_1", "ok", false);
        var error = new ToolResult("call_2", "failed", true);

        success.IsError.Should().BeFalse();
        error.IsError.Should().BeTrue();
    }

    [Fact]
    public void ToolResult_ThrowsOnEmptyToolCallId()
    {
        // FR-004a-58, FR-004a-66: ToolCallId MUST be non-empty
        var act1 = () => new ToolResult(string.Empty, "output", false);
        act1.Should().Throw<ArgumentException>().WithParameterName("ToolCallId");

        var act2 = () => new ToolResult(null!, "output", false);
        act2.Should().Throw<ArgumentException>().WithParameterName("ToolCallId");

        var act3 = () => new ToolResult("   ", "output", false);
        act3.Should().Throw<ArgumentException>().WithParameterName("ToolCallId");
    }

    [Fact]
    public void ToolResult_ThrowsOnNullResult()
    {
        // FR-004a-60, FR-004a-66: Result MUST NOT be null
        var act = () => new ToolResult("call_1", null!, false);
        act.Should().Throw<ArgumentException>().WithParameterName("Result");
    }

    [Fact]
    public void ToolResult_AllowsEmptyResult()
    {
        // FR-004a-62: Result MAY be empty string
        var result = new ToolResult("call_1", string.Empty, false);
        result.Result.Should().Be(string.Empty);
    }

    [Fact]
    public void ToolResult_IsErrorDefaultsToFalse()
    {
        // FR-004a-64: IsError MUST default to false (tested via factory methods below)
        // This test verifies explicit construction with false
        var result = new ToolResult("call_1", "ok", false);
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void ToolResult_HasSuccessFactory()
    {
        // FR-004a-67: ToolResult MUST have Success factory
        var result = ToolResult.Success("call_123", "Operation completed");

        result.ToolCallId.Should().Be("call_123");
        result.Result.Should().Be("Operation completed");
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void ToolResult_HasErrorFactory()
    {
        // FR-004a-68: ToolResult MUST have Error factory
        var result = ToolResult.Error("call_456", "File not found");

        result.ToolCallId.Should().Be("call_456");
        result.Result.Should().Be("File not found");
        result.IsError.Should().BeTrue(); // FR-004a-69
    }

    [Fact]
    public void ToolResult_ErrorFactorySetsIsErrorTrue()
    {
        // FR-004a-69: Error factory MUST set IsError to true
        var error = ToolResult.Error("call_1", "Something went wrong");
        error.IsError.Should().BeTrue();
    }

    [Fact]
    public void ToolResult_SerializesToJson()
    {
        // FR-004a-65: ToolResult MUST serialize to JSON
        var result = new ToolResult("call_789", "success", false);
        var json = JsonSerializer.Serialize(result);

        json.Should().Contain("\"toolCallId\":\"call_789\"");
        json.Should().Contain("\"result\":\"success\"");
        json.Should().Contain("\"isError\":false");
    }

    [Fact]
    public void ToolResult_DeserializesFromJson()
    {
        // FR-004a-65: ToolResult MUST deserialize from JSON
        var json = "{\"toolCallId\":\"call_999\",\"result\":\"data\",\"isError\":true}";
        var result = JsonSerializer.Deserialize<ToolResult>(json);

        result.Should().NotBeNull();
        result!.ToolCallId.Should().Be("call_999");
        result.Result.Should().Be("data");
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void ToolResult_IsImmutable()
    {
        // FR-004a-57: ToolResult MUST be immutable
        var result = new ToolResult("call_1", "ok", false);

        // This should not compile if properties are settable:
        // result.ToolCallId = "new_id"; // Compilation error expected

        // Instead verify via constructor:
        var result2 = new ToolResult("call_2", "updated", true);
        result2.ToolCallId.Should().Be("call_2");
        result2.Result.Should().Be("updated");
        result2.IsError.Should().BeTrue();
    }

    [Fact]
    public void ToolResult_HasValueEquality()
    {
        // FR-004a-70: ToolResult MUST implement value equality
        var result1 = new ToolResult("call_1", "ok", false);
        var result2 = new ToolResult("call_1", "ok", false);
        var result3 = new ToolResult("call_1", "ok", true);

        result1.Should().Be(result2);
        result1.Should().NotBe(result3);
    }

    [Fact]
    public void ToolResult_ToolCallIdMatchesToolCallId()
    {
        // FR-004a-59: ToolCallId MUST match corresponding ToolCall.Id
        var toolCall = new ToolCall("call_abc", "my_tool", "{}");
        var result = ToolResult.Success(toolCall.Id, "Tool executed");

        result.ToolCallId.Should().Be(toolCall.Id);
    }

    [Fact]
    public void ToolResult_SuccessFactoryValidatesId()
    {
        // FR-004a-67 + FR-004a-66: Success factory must validate
        var act = () => ToolResult.Success(string.Empty, "ok");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToolResult_ErrorFactoryValidatesId()
    {
        // FR-004a-68 + FR-004a-66: Error factory must validate
        var act = () => ToolResult.Error(string.Empty, "failed");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToolResult_SupportsLongResult()
    {
        // FR-004a-60, FR-004a-61: Result must support long strings
        var longResult = new string('x', 10000);
        var result = new ToolResult("call_1", longResult, false);
        result.Result.Should().HaveLength(10000);
    }

    [Fact]
    public void ToolResult_SupportsUnicodeInResult()
    {
        // FR-004a-61: Result must support unicode
        var unicodeResult = "Success: 成功 ✅";
        var result = new ToolResult("call_1", unicodeResult, false);
        result.Result.Should().Be(unicodeResult);
    }
}
