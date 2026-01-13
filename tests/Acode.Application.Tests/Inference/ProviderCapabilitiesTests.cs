namespace Acode.Application.Tests.Inference;

using System.Text.Json;
using Acode.Application.Inference;
using FluentAssertions;

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
        json.Should().Contain("\"supportsToolCalls\":"); // Actual JSON property name
        json.Should().Contain("\"maxContextTokens\":"); // Actual JSON property name
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

    [Fact]
    public void Should_Check_Supports()
    {
        // FR-036: ProviderCapabilities MUST provide Supports(CapabilityRequirement) method
        // Arrange
        var capabilities = new ProviderCapabilities(
            supportsStreaming: true,
            supportsTools: true,
            supportsJsonMode: false,
            maxContextLength: 8192,
            maxOutputTokens: 2048,
            supportedModels: new[] { "llama2", "codellama" });

        // Act & Assert - Matching streaming requirement
        var req1 = new CapabilityRequirement { RequiresStreaming = true };
        capabilities.Supports(req1).Should().BeTrue();

        // Act & Assert - Matching tool calls requirement
        var req2 = new CapabilityRequirement { RequiresToolCalls = true };
        capabilities.Supports(req2).Should().BeTrue();

        // Act & Assert - Non-matching JSON mode requirement
        var req3 = new CapabilityRequirement { RequiresJsonMode = true };
        capabilities.Supports(req3).Should().BeFalse();

        // Act & Assert - Context size requirements (OK - 8192 >= 4096)
        var req4 = new CapabilityRequirement { MinContextTokens = 4096 };
        capabilities.Supports(req4).Should().BeTrue(); // maxContextLength is 8192, meets minimum

        // Act & Assert - Model requirements
        var req5 = new CapabilityRequirement { RequiredModel = "llama2" };
        var capsWithModels = new ProviderCapabilities(supportedModels: new[] { "llama2", "mistral" });
        capsWithModels.Supports(req5).Should().BeTrue();
    }

    [Fact]
    public void Should_Merge_Capabilities()
    {
        // Arrange - Testing Requirements line 830
        var cap1 = new ProviderCapabilities(
            supportsStreaming: true,
            supportsTools: false,
            supportsJsonMode: false,
            maxContextLength: 8192,
            maxOutputTokens: 2048,
            supportedModels: new[] { "model-a", "model-b" },
            defaultModel: "model-a");

        var cap2 = new ProviderCapabilities(
            supportsStreaming: false,
            supportsTools: true,
            supportsJsonMode: true,
            maxContextLength: 16384,
            maxOutputTokens: 4096,
            supportedModels: new[] { "model-c", "model-d" });

        // Act
        var merged = cap1.Merge(cap2);

        // Assert - Boolean capabilities use OR logic
        merged.SupportsStreaming.Should().BeTrue("streaming should be true if either has it");
        merged.SupportsToolCalls.Should().BeTrue("tool calls should be enabled if either has it");

        // Assert - Numeric limits use MAX
        merged.MaxContextTokens.Should().Be(16384, "should take maximum context size");
        merged.MaxOutputTokens.Should().Be(4096, "should take maximum output size");

        // Assert - Models should be union of all models from both providers
        merged.SupportedModels.Should().BeEquivalentTo(new[] { "model-a", "model-b", "model-c", "model-d" });

        // Assert - Default model from first if both have it
        merged.DefaultModel.Should().Be("model-a");
    }

    [Fact]
    public void Should_Merge_Capabilities_WithNullModels()
    {
        // Arrange
        var cap1 = new ProviderCapabilities(
            supportsStreaming: true,
            supportedModels: new[] { "model-a" });

        var cap2 = new ProviderCapabilities(
            supportsStreaming: false,
            supportedModels: null);

        // Act
        var merged = cap1.Merge(cap2);

        // Assert - should keep first provider's models when second has none
        merged.SupportedModels.Should().BeEquivalentTo(new[] { "model-a" });
    }

    [Fact]
    public void Should_Merge_Capabilities_With_Null_Other()
    {
        // Arrange
        var caps = new ProviderCapabilities(supportsStreaming: true);

        // Act & Assert
        Action act = () => caps.Merge(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
