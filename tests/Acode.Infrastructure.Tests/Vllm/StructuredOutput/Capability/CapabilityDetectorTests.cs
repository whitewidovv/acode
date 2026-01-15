namespace Acode.Infrastructure.Tests.Vllm.StructuredOutput.Capability;

using Acode.Infrastructure.Vllm.StructuredOutput.Capability;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for CapabilityDetector.
/// </summary>
public class CapabilityDetectorTests
{
    [Fact]
    public void DetectCapabilities_WithValidModelId_ReturnsCapabilities()
    {
        // Arrange
        var detector = new CapabilityDetector();
        var modelId = "llama2";

        // Act
        var capabilities = detector.DetectCapabilities(modelId);

        // Assert
        capabilities.Should().NotBeNull();
        capabilities.ModelId.Should().Be(modelId);
        capabilities.SupportsGuidedJson.Should().BeTrue();
        capabilities.MaxSchemaSizeBytes.Should().Be(65536);
    }

    [Fact]
    public void DetectCapabilities_WithEmptyModelId_ReturnsMinimalCapabilities()
    {
        // Arrange
        var detector = new CapabilityDetector();

        // Act
        var capabilities = detector.DetectCapabilities(string.Empty);

        // Assert
        capabilities.Should().NotBeNull();
        capabilities.SupportsGuidedJson.Should().BeFalse();
        capabilities.SupportsGuidedChoice.Should().BeFalse();
        capabilities.SupportsGuidedRegex.Should().BeFalse();
        capabilities.IsStale.Should().BeTrue();
    }

    [Fact]
    public void DetectCapabilities_WithNullModelId_ReturnsMinimalCapabilities()
    {
        // Arrange
        var detector = new CapabilityDetector();

        // Act
        var capabilities = detector.DetectCapabilities(null!);

        // Assert
        capabilities.Should().NotBeNull();
        capabilities.IsStale.Should().BeTrue();
    }

    [Theory]
    [InlineData("llama2", true, true)]
    [InlineData("llama3", true, true)]
    [InlineData("mistral", true, true)]
    [InlineData("neural-2024", true, true)]
    [InlineData("older-model", true, false)]
    public void DetectCapabilities_WithDifferentModels_ReturnsAppropriateFeaturesSupport(
        string modelId, bool expectedGuidedJson, bool expectedGuidedChoice)
    {
        // Arrange
        var detector = new CapabilityDetector();

        // Act
        var capabilities = detector.DetectCapabilities(modelId);

        // Assert
        capabilities.SupportsGuidedJson.Should().Be(expectedGuidedJson);
        capabilities.SupportsGuidedChoice.Should().Be(expectedGuidedChoice);
        capabilities.SupportsGuidedRegex.Should().Be(expectedGuidedChoice);
    }

    [Fact]
    public async Task DetectCapabilitiesAsync_WithValidModelId_ReturnsCapabilitiesAsync()
    {
        // Arrange
        var detector = new CapabilityDetector();
        var modelId = "llama3";

        // Act
        var capabilities = await detector.DetectCapabilitiesAsync(modelId);

        // Assert
        capabilities.Should().NotBeNull();
        capabilities.ModelId.Should().Be(modelId);
        capabilities.SupportsGuidedJson.Should().BeTrue();
    }

    [Fact]
    public void MarkAsStale_WithCapabilities_SetsStaleFlag()
    {
        // Arrange
        var detector = new CapabilityDetector();
        var capabilities = detector.DetectCapabilities("llama2");
        capabilities.IsStale.Should().BeFalse();

        // Act
        detector.MarkAsStale(capabilities);

        // Assert
        capabilities.IsStale.Should().BeTrue();
    }

    [Fact]
    public void MarkAsStale_WithNullCapabilities_DoesNotThrow()
    {
        // Arrange
        var detector = new CapabilityDetector();

        // Act
        var action = () => detector.MarkAsStale(null!);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void RequiresRefresh_WithStaleCapabilities_ReturnsTrue()
    {
        // Arrange
        var detector = new CapabilityDetector();
        var capabilities = detector.DetectCapabilities("llama2");
        detector.MarkAsStale(capabilities);

        // Act
        var result = detector.RequiresRefresh(capabilities);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RequiresRefresh_WithNewCapabilities_ReturnsFalse()
    {
        // Arrange
        var detector = new CapabilityDetector();
        var capabilities = detector.DetectCapabilities("llama2");
        capabilities.IsStale = false;

        // Act
        var result = detector.RequiresRefresh(capabilities, 60);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RequiresRefresh_WithOldCapabilities_ReturnsTrue()
    {
        // Arrange
        var detector = new CapabilityDetector();
        var capabilities = detector.DetectCapabilities("llama2");
        capabilities.LastDetectedUtc = DateTime.UtcNow.AddMinutes(-120); // 2 hours ago
        capabilities.IsStale = false;

        // Act
        var result = detector.RequiresRefresh(capabilities, 60); // 60 minute threshold

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RequiresRefresh_WithNullCapabilities_ReturnsTrue()
    {
        // Arrange
        var detector = new CapabilityDetector();

        // Act
        var result = detector.RequiresRefresh(null!);

        // Assert
        result.Should().BeTrue();
    }
}
