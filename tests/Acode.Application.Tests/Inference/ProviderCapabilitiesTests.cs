namespace Acode.Application.Tests.Inference;

using System.Text.Json;
using Acode.Application.Inference;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for ProviderCapabilities record following TDD (RED phase).
/// FR-004-73 to FR-004-80.
/// </summary>
public class ProviderCapabilitiesTests
{
    [Fact]
    public void ProviderCapabilities_HasSupportsStreamingProperty()
    {
        // FR-004-73: ProviderCapabilities MUST include SupportsStreaming (bool)
        var streamingProvider = new ProviderCapabilities(supportsStreaming: true);
        var nonStreamingProvider = new ProviderCapabilities(supportsStreaming: false);

        streamingProvider.SupportsStreaming.Should().BeTrue();
        nonStreamingProvider.SupportsStreaming.Should().BeFalse();
    }

    [Fact]
    public void ProviderCapabilities_HasSupportsToolsProperty()
    {
        // FR-004-74: ProviderCapabilities MUST include SupportsTools (bool)
        var toolProvider = new ProviderCapabilities(supportsTools: true);
        var nonToolProvider = new ProviderCapabilities(supportsTools: false);

        toolProvider.SupportsTools.Should().BeTrue();
        nonToolProvider.SupportsTools.Should().BeFalse();
    }

    [Fact]
    public void ProviderCapabilities_HasSupportsSystemMessagesProperty()
    {
        // FR-004-75: ProviderCapabilities MUST include SupportsSystemMessages (bool)
        var systemProvider = new ProviderCapabilities(supportsSystemMessages: true);
        var noSystemProvider = new ProviderCapabilities(supportsSystemMessages: false);

        systemProvider.SupportsSystemMessages.Should().BeTrue();
        noSystemProvider.SupportsSystemMessages.Should().BeFalse();
    }

    [Fact]
    public void ProviderCapabilities_HasSupportsVisionProperty()
    {
        // FR-004-76: ProviderCapabilities MUST include SupportsVision (bool)
        var visionProvider = new ProviderCapabilities(supportsVision: true);
        var textOnlyProvider = new ProviderCapabilities(supportsVision: false);

        visionProvider.SupportsVision.Should().BeTrue();
        textOnlyProvider.SupportsVision.Should().BeFalse();
    }

    [Fact]
    public void ProviderCapabilities_HasMaxContextLengthProperty()
    {
        // FR-004-77, FR-004-78: MaxContextLength is nullable (null = unknown/unlimited)
        var limitedProvider = new ProviderCapabilities(maxContextLength: 8192);
        var unlimitedProvider = new ProviderCapabilities(maxContextLength: null);

        limitedProvider.MaxContextLength.Should().Be(8192);
        unlimitedProvider.MaxContextLength.Should().BeNull();
    }

    [Fact]
    public void ProviderCapabilities_HasSupportedModelsProperty()
    {
        // FR-004-79: ProviderCapabilities MUST include SupportedModels (nullable array)
        var models = new[] { "llama2:7b", "llama2:13b", "codellama" };
        var withModels = new ProviderCapabilities(supportedModels: models);
        var withoutModels = new ProviderCapabilities(supportedModels: null);

        withModels.SupportedModels.Should().BeEquivalentTo(models);
        withoutModels.SupportedModels.Should().BeNull();
    }

    [Fact]
    public void ProviderCapabilities_HasDefaultModelProperty()
    {
        // FR-004-80: ProviderCapabilities MUST include DefaultModel (nullable string)
        var withDefault = new ProviderCapabilities(defaultModel: "llama2:7b");
        var withoutDefault = new ProviderCapabilities(defaultModel: null);

        withDefault.DefaultModel.Should().Be("llama2:7b");
        withoutDefault.DefaultModel.Should().BeNull();
    }

    [Fact]
    public void ProviderCapabilities_AllPropertiesDefaultToFalseOrNull()
    {
        // All capabilities default to false/null (conservative defaults)
        var capabilities = new ProviderCapabilities();

        capabilities.SupportsStreaming.Should().BeFalse();
        capabilities.SupportsTools.Should().BeFalse();
        capabilities.SupportsSystemMessages.Should().BeFalse();
        capabilities.SupportsVision.Should().BeFalse();
        capabilities.MaxContextLength.Should().BeNull();
        capabilities.SupportedModels.Should().BeNull();
        capabilities.DefaultModel.Should().BeNull();
    }

    [Fact]
    public void ProviderCapabilities_SerializesToJson()
    {
        // ProviderCapabilities should serialize to JSON
        var capabilities = new ProviderCapabilities(
            supportsStreaming: true,
            supportsTools: true,
            maxContextLength: 8192,
            defaultModel: "llama2");

        var json = JsonSerializer.Serialize(capabilities);

        json.Should().Contain("\"supportsStreaming\":");
        json.Should().Contain("\"supportsTools\":");
        json.Should().Contain("\"maxContextLength\":");
        json.Should().Contain("\"defaultModel\":");
    }

    [Fact]
    public void ProviderCapabilities_ImplementsValueEquality()
    {
        // Records have value equality
        var caps1 = new ProviderCapabilities(supportsStreaming: true, maxContextLength: 4096);
        var caps2 = new ProviderCapabilities(supportsStreaming: true, maxContextLength: 4096);

        caps1.Should().Be(caps2);
    }

    [Fact]
    public void ProviderCapabilities_FullyPopulated()
    {
        // Test all properties at once
        var capabilities = new ProviderCapabilities(
            supportsStreaming: true,
            supportsTools: true,
            supportsSystemMessages: true,
            supportsVision: false,
            maxContextLength: 16384,
            supportedModels: new[] { "gpt-4", "gpt-3.5-turbo" },
            defaultModel: "gpt-4");

        capabilities.SupportsStreaming.Should().BeTrue();
        capabilities.SupportsTools.Should().BeTrue();
        capabilities.SupportsSystemMessages.Should().BeTrue();
        capabilities.SupportsVision.Should().BeFalse();
        capabilities.MaxContextLength.Should().Be(16384);
        capabilities.SupportedModels.Should().BeEquivalentTo(new[] { "gpt-4", "gpt-3.5-turbo" });
        capabilities.DefaultModel.Should().Be("gpt-4");
    }
}
