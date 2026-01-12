namespace Acode.Domain.Tests.Models.Inference;

using System.Linq;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Tests for ToolCallDelta record.
/// FR-004a-91 to FR-004a-100: ToolCallDelta must support streaming tool calls.
/// </summary>
public sealed class ToolCallDeltaTests
{
    [Fact]
    public void Should_Be_Immutable()
    {
        // Arrange
        var delta = new ToolCallDelta
        {
            Index = 0,
            Id = "call_123",
            Name = "read_file",
        };

        // Act
        var modified = delta with { Index = 1 };

        // Assert
        delta.Index.Should().Be(0);
        modified.Index.Should().Be(1);
        delta.Should().NotBeSameAs(modified);
    }

    [Fact]
    public void Should_Have_Index()
    {
        // Arrange & Act
        var delta = new ToolCallDelta { Index = 2 };

        // Assert
        delta.Index.Should().Be(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Should_Accept_Valid_Index_Values(int index)
    {
        // Act
        var delta = new ToolCallDelta { Index = index };

        // Assert
        delta.Index.Should().Be(index);
    }

    [Fact]
    public void Index_Identifies_Which_ToolCall()
    {
        // Arrange - simulating multiple parallel tool calls
        var delta0 = new ToolCallDelta { Index = 0, Name = "read_file" };
        var delta1 = new ToolCallDelta { Index = 1, Name = "write_file" };

        // Assert
        delta0.Index.Should().NotBe(delta1.Index);
    }

    [Fact]
    public void Should_Allow_Only_Index()
    {
        // Arrange - delta with just index (continuation chunk)
        var delta = new ToolCallDelta { Index = 0 };

        // Assert
        delta.Id.Should().BeNull();
        delta.Name.Should().BeNull();
        delta.ArgumentsDelta.Should().BeNull();
    }

    [Fact]
    public void First_Delta_Should_Have_Id_And_Name()
    {
        // Arrange - first chunk introduces the tool call
        var firstDelta = new ToolCallDelta
        {
            Index = 0,
            Id = "call_abc123",
            Name = "write_file",
        };

        // Assert
        firstDelta.Id.Should().NotBeNull();
        firstDelta.Name.Should().NotBeNull();
    }

    [Fact]
    public void Subsequent_Deltas_Only_Need_ArgumentsDelta()
    {
        // Arrange - continuation chunks only have partial arguments
        var continuationDelta = new ToolCallDelta
        {
            Index = 0,
            ArgumentsDelta = "{\"pa",
        };

        // Assert
        continuationDelta.Id.Should().BeNull();
        continuationDelta.Name.Should().BeNull();
        continuationDelta.ArgumentsDelta.Should().Be("{\"pa");
    }

    [Fact]
    public void Should_Support_ArgumentsDelta()
    {
        // Arrange
        var delta = new ToolCallDelta
        {
            Index = 0,
            ArgumentsDelta = "th\": \"test.cs",
        };

        // Assert
        delta.ArgumentsDelta.Should().Be("th\": \"test.cs");
    }

    [Fact]
    public void ArgumentsDelta_Can_Be_Partial_Json()
    {
        // Arrange - simulating streaming JSON in chunks
        var chunk1 = new ToolCallDelta { Index = 0, Id = "call_1", Name = "write_file", ArgumentsDelta = "{\"pa" };
        var chunk2 = new ToolCallDelta { Index = 0, ArgumentsDelta = "th\": \"" };
        var chunk3 = new ToolCallDelta { Index = 0, ArgumentsDelta = "test.cs\"}" };

        // Act - combine to form complete JSON
        var fullArgs = chunk1.ArgumentsDelta + chunk2.ArgumentsDelta + chunk3.ArgumentsDelta;

        // Assert
        fullArgs.Should().Be("{\"path\": \"test.cs\"}");
    }

    [Fact]
    public void ArgumentsDelta_Can_Be_Empty_String()
    {
        // Arrange
        var delta = new ToolCallDelta
        {
            Index = 0,
            ArgumentsDelta = string.Empty,
        };

        // Assert
        delta.ArgumentsDelta.Should().BeEmpty();
    }

    [Fact]
    public void Should_Support_Accumulation_Pattern()
    {
        // Arrange - simulate streaming tool call
        var deltas = new[]
        {
            new ToolCallDelta { Index = 0, Id = "call_xyz", Name = "search", ArgumentsDelta = "{" },
            new ToolCallDelta { Index = 0, ArgumentsDelta = "\"query\"" },
            new ToolCallDelta { Index = 0, ArgumentsDelta = ": \"test\"}" },
        };

        // Act - accumulate
        string? id = null;
        string? name = null;
        var argsBuilder = new System.Text.StringBuilder();

        foreach (var delta in deltas)
        {
            id ??= delta.Id;
            name ??= delta.Name;
            if (delta.ArgumentsDelta != null)
            {
                argsBuilder.Append(delta.ArgumentsDelta);
            }
        }

        // Assert
        id.Should().Be("call_xyz");
        name.Should().Be("search");
        argsBuilder.ToString().Should().Be("{\"query\": \"test\"}");
    }

    [Fact]
    public void Should_Handle_Multiple_Parallel_ToolCalls()
    {
        // Arrange - model calling two tools simultaneously
        var deltas = new[]
        {
            new ToolCallDelta { Index = 0, Id = "call_1", Name = "read_file" },
            new ToolCallDelta { Index = 1, Id = "call_2", Name = "write_file" },
            new ToolCallDelta { Index = 0, ArgumentsDelta = "{\"path\":\"a.cs\"}" },
            new ToolCallDelta { Index = 1, ArgumentsDelta = "{\"path\":\"b.cs\"}" },
        };

        // Act - separate by index
        var tool0Args = string.Join(string.Empty, deltas.Where(d => d.Index == 0).Select(d => d.ArgumentsDelta ?? string.Empty));
        var tool1Args = string.Join(string.Empty, deltas.Where(d => d.Index == 1).Select(d => d.ArgumentsDelta ?? string.Empty));

        // Assert
        tool0Args.Should().Contain("a.cs");
        tool1Args.Should().Contain("b.cs");
    }

    [Fact]
    public void Should_Serialize_To_Json()
    {
        // Arrange
        var delta = new ToolCallDelta
        {
            Index = 0,
            Id = "call_123",
            Name = "read_file",
            ArgumentsDelta = "{\"path\":\"",
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(delta);

        // Assert
        json.Should().Contain("\"index\":0");
        json.Should().Contain("\"id\":\"call_123\"");
    }

    [Fact]
    public void Should_Omit_Null_Properties()
    {
        // Arrange
        var delta = new ToolCallDelta { Index = 0 };
        var options = new System.Text.Json.JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(delta, options);

        // Assert
        json.Should().NotContain("\"id\"");
        json.Should().NotContain("\"name\"");
    }
}
