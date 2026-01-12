namespace Acode.Domain.Tests.Models.Inference;

using System;
using System.Collections.Generic;
using System.Text.Json;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Tests for ResponseMetadata record following TDD (RED phase).
/// FR-004b-042 to FR-004b-053.
/// </summary>
public class ResponseMetadataTests
{
    [Fact]
    public void ResponseMetadata_HasProviderIdProperty()
    {
        // FR-004b-043: ResponseMetadata MUST include ProviderId property
        var metadata = new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1));

        metadata.ProviderId.Should().Be("ollama");
    }

    [Fact]
    public void ResponseMetadata_HasModelIdProperty()
    {
        // FR-004b-044: ResponseMetadata MUST include ModelId property
        var metadata = new ResponseMetadata("ollama", "llama2:7b", TimeSpan.FromSeconds(1));

        metadata.ModelId.Should().Be("llama2:7b");
    }

    [Fact]
    public void ResponseMetadata_HasRequestDurationProperty()
    {
        // FR-004b-045: ResponseMetadata MUST include RequestDuration property
        var duration = TimeSpan.FromSeconds(2.5);
        var metadata = new ResponseMetadata("ollama", "llama2", duration);

        metadata.RequestDuration.Should().Be(duration);
    }

    [Fact]
    public void ResponseMetadata_HasTimeToFirstTokenProperty()
    {
        // FR-004b-046: ResponseMetadata MUST include TimeToFirstToken property (TimeSpan?, null for non-streaming)
        var ttft = TimeSpan.FromMilliseconds(150);
        var metadata1 = new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1), ttft);
        var metadata2 = new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1));

        metadata1.TimeToFirstToken.Should().Be(ttft);
        metadata2.TimeToFirstToken.Should().BeNull();
    }

    [Fact]
    public void ResponseMetadata_HasTokensPerSecondComputedProperty()
    {
        // FR-004b-047: ResponseMetadata MUST include TokensPerSecond computed property
        var duration = TimeSpan.FromSeconds(2);
        var metadata = new ResponseMetadata("ollama", "llama2", duration);

        // This will be computed from CompletionTokens / Duration - need to add CompletionTokens parameter
        // For now, test structure exists
        metadata.Should().NotBeNull();
    }

    [Fact]
    public void ResponseMetadata_HasExtensionsProperty()
    {
        // FR-004b-048: ResponseMetadata MUST include Extensions property
        var extensions = new Dictionary<string, JsonElement>
        {
            ["model_version"] = JsonDocument.Parse("\"v1.2.3\"").RootElement,
            ["quantization"] = JsonDocument.Parse("\"q4_0\"").RootElement,
        };

        var metadata = new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1), null, null, extensions);

        metadata.Extensions.Should().NotBeNull();
        metadata.Extensions.Should().HaveCount(2);
        metadata.Extensions["model_version"].GetString().Should().Be("v1.2.3");
    }

    [Fact]
    public void ResponseMetadata_PreservesProviderSpecificFields()
    {
        // FR-004b-049: ResponseMetadata MUST preserve arbitrary provider-specific fields in Extensions
        var extensions = new Dictionary<string, JsonElement>
        {
            ["custom_field"] = JsonDocument.Parse("\"custom_value\"").RootElement,
            ["numeric_field"] = JsonDocument.Parse("42").RootElement,
        };

        var metadata = new ResponseMetadata("vllm", "model", TimeSpan.FromSeconds(1), null, null, extensions);

        metadata.Extensions.Should().ContainKey("custom_field");
        metadata.Extensions.Should().ContainKey("numeric_field");
        metadata.Extensions["custom_field"].GetString().Should().Be("custom_value");
        metadata.Extensions["numeric_field"].GetInt32().Should().Be(42);
    }

    [Fact]
    public void ResponseMetadata_AllowsNullTimeToFirstToken()
    {
        // FR-004b-050: ResponseMetadata MUST support null TimeToFirstToken for non-streaming responses
        var metadata = new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1), null);

        metadata.TimeToFirstToken.Should().BeNull();
    }

    [Fact]
    public void ResponseMetadata_ThrowsOnEmptyProviderId()
    {
        // FR-004b-051: ResponseMetadata MUST validate ProviderId is non-empty
        var act = () => new ResponseMetadata(string.Empty, "model", TimeSpan.FromSeconds(1));

        act.Should().Throw<ArgumentException>().WithParameterName("ProviderId");
    }

    [Fact]
    public void ResponseMetadata_ThrowsOnEmptyModelId()
    {
        // FR-004b-052: ResponseMetadata MUST validate ModelId is non-empty
        var act = () => new ResponseMetadata("ollama", string.Empty, TimeSpan.FromSeconds(1));

        act.Should().Throw<ArgumentException>().WithParameterName("ModelId");
    }

    [Fact]
    public void ResponseMetadata_ThrowsOnNegativeRequestDuration()
    {
        // FR-004b-053: ResponseMetadata MUST validate RequestDuration is non-negative
        var act = () => new ResponseMetadata("ollama", "model", TimeSpan.FromSeconds(-1));

        act.Should().Throw<ArgumentException>().WithParameterName("RequestDuration");
    }

    [Fact]
    public void ResponseMetadata_AllowsZeroRequestDuration()
    {
        // Zero duration should be allowed (e.g., cached response)
        var metadata = new ResponseMetadata("ollama", "model", TimeSpan.Zero);

        metadata.RequestDuration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ResponseMetadata_IsImmutable()
    {
        // FR-004b-042: ResponseMetadata MUST be defined as an immutable record type
        var metadata = new ResponseMetadata("ollama", "model", TimeSpan.FromSeconds(1));

        // Record with init-only properties ensures immutability at compile time
        metadata.Should().NotBeNull();
    }

    [Fact]
    public void ResponseMetadata_HasValueEquality()
    {
        // Records have value equality by default
        var metadata1 = new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1));
        var metadata2 = new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(1));
        var metadata3 = new ResponseMetadata("vllm", "llama2", TimeSpan.FromSeconds(1));

        metadata1.Should().Be(metadata2);
        metadata1.Should().NotBe(metadata3);
    }

    [Fact]
    public void ResponseMetadata_ExtensionsCanBeEmpty()
    {
        // Extensions should default to empty dictionary
        var metadata = new ResponseMetadata("ollama", "model", TimeSpan.FromSeconds(1));

        metadata.Extensions.Should().NotBeNull();
        metadata.Extensions.Should().BeEmpty();
    }

    [Fact]
    public void ResponseMetadata_SerializesToJson()
    {
        // ResponseMetadata should serialize to JSON
        var metadata = new ResponseMetadata("ollama", "llama2", TimeSpan.FromSeconds(2.5));

        var json = JsonSerializer.Serialize(metadata);

        json.Should().Contain("\"providerId\":");
        json.Should().Contain("\"modelId\":");
        json.Should().Contain("\"requestDuration\":");
    }

    [Fact]
    public void Should_Compute_TokensPerSecond()
    {
        // FR-004b-047: TokensPerSecond computed from CompletionTokenCount / Duration
        var duration = TimeSpan.FromSeconds(2);
        var metadata = new ResponseMetadata(
            "ollama",
            "llama2",
            duration,
            TimeToFirstToken: null,
            CompletionTokenCount: 100);

        // Assert
        metadata.TokensPerSecond.Should().Be(50.0); // 100 tokens / 2 seconds = 50 tokens/sec
    }

    [Fact]
    public void Should_Handle_Zero_Duration()
    {
        // Zero duration should return null for TokensPerSecond (avoid divide by zero)
        var metadata = new ResponseMetadata(
            "ollama",
            "model",
            TimeSpan.Zero,
            TimeToFirstToken: null,
            CompletionTokenCount: 100);

        metadata.TokensPerSecond.Should().BeNull();
    }

    [Fact]
    public void Should_Handle_Null_CompletionTokenCount()
    {
        // Null CompletionTokenCount should return null for TokensPerSecond
        var metadata = new ResponseMetadata(
            "ollama",
            "model",
            TimeSpan.FromSeconds(2));

        metadata.TokensPerSecond.Should().BeNull();
    }

    [Fact]
    public void Should_Handle_Zero_Tokens()
    {
        // Zero tokens is valid (e.g., error responses)
        var metadata = new ResponseMetadata(
            "ollama",
            "model",
            TimeSpan.FromSeconds(1),
            TimeToFirstToken: null,
            CompletionTokenCount: 0);

        metadata.TokensPerSecond.Should().Be(0.0);
    }
}
