using Acode.Application.Inference;
using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Vllm;
using Acode.Infrastructure.Vllm.Client;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm;

public class VllmProviderTests
{
    [Fact]
    public void Constructor_Should_SetProviderName()
    {
        // Arrange
        var config = new VllmClientConfiguration();

        // Act
        var provider = new VllmProvider(config);

        // Assert
        provider.ProviderName.Should().Be("vllm");
    }

    [Fact]
    public void Capabilities_Should_DeclareStreamingAndToolsSupport()
    {
        // Arrange
        var config = new VllmClientConfiguration();
        var provider = new VllmProvider(config);

        // Act
        var capabilities = provider.Capabilities;

        // Assert
        capabilities.SupportsStreaming.Should().BeTrue();
        capabilities.SupportsTools.Should().BeTrue();
        capabilities.SupportsSystemMessages.Should().BeTrue();
    }

    [Fact]
    public void GetSupportedModels_Should_ReturnCommonVllmModels()
    {
        // Arrange
        var config = new VllmClientConfiguration();
        var provider = new VllmProvider(config);

        // Act
        var models = provider.GetSupportedModels();

        // Assert
        models.Should().NotBeEmpty();
        models.Should().Contain(m => m.Contains("llama", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task IsHealthyAsync_Should_ReturnFalse_When_ServerNotRunning()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:9999"
        };
        var provider = new VllmProvider(config);

        // Act
#pragma warning disable CA2007
        var isHealthy = await provider.IsHealthyAsync(CancellationToken.None);
#pragma warning restore CA2007

        // Assert
        isHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task ChatAsync_Should_ThrowConnectionException_When_ServerUnreachable()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:9999"
        };
        var provider = new VllmProvider(config);

        var request = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Hello") },
            modelParameters: new ModelParameters("test-model"));

        // Act & Assert
#pragma warning disable CA2007
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await provider.ChatAsync(request, CancellationToken.None));
#pragma warning restore CA2007
    }

    [Fact]
    public async Task StreamChatAsync_Should_ThrowConnectionException_When_ServerUnreachable()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:9999"
        };
        var provider = new VllmProvider(config);

        var request = new ChatRequest(
            messages: new[] { ChatMessage.CreateUser("Hello") },
            modelParameters: new ModelParameters("test-model"));

        // Act & Assert
#pragma warning disable CA2007
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await foreach (var delta in provider.StreamChatAsync(request, CancellationToken.None))
            {
                delta.Should().NotBeNull();
            }
        });
#pragma warning restore CA2007
    }

    [Fact]
    public void Dispose_Should_CleanupResources()
    {
        // Arrange
        var config = new VllmClientConfiguration();
        var provider = new VllmProvider(config);

        // Act
        provider.Dispose();

        // Assert - no exception, safe to dispose twice
        provider.Dispose();
    }
}
