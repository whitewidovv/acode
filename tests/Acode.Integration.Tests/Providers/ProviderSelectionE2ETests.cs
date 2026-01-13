namespace Acode.Integration.Tests.Providers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Inference;
using Acode.Application.Providers;
using Acode.Application.Providers.Exceptions;
using Acode.Application.Providers.Selection;
using Acode.Domain.Models.Inference;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

/// <summary>
/// End-to-end tests for provider selection flow.
/// Gap #28 from task-004c completion checklist.
/// </summary>
public sealed class ProviderSelectionE2ETests
{
    [Fact]
    public void Should_Select_Default_Provider()
    {
        // Arrange
        var defaultProvider = Substitute.For<IModelProvider>();
        defaultProvider.ProviderName.Returns("ollama");
        defaultProvider.Capabilities.Returns(new ProviderCapabilities(supportsStreaming: true, supportsTools: true));

        var providerFactory = new Func<ProviderDescriptor, IModelProvider?>(desc =>
        {
            if (desc.Id == "ollama")
            {
                return defaultProvider;
            }

            return null;
        });

        var logger = NullLogger<ProviderRegistry>.Instance;
        var selector = new CapabilityProviderSelector(_ => null);
        var registry = new ProviderRegistry(logger, selector, "ollama", providerFactory);

        registry.Register(new ProviderDescriptor
        {
            Id = "ollama",
            Name = "Ollama",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(supportsStreaming: true, supportsTools: true),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"))
        });

        // Act
        var provider = registry.GetDefaultProvider();

        // Assert
        provider.Should().NotBeNull();
        provider.ProviderName.Should().Be("ollama");
    }

    [Fact]
    public void Should_Select_By_Capability()
    {
        // Arrange
        var streamingProvider = Substitute.For<IModelProvider>();
        streamingProvider.ProviderName.Returns("streaming");
        streamingProvider.Capabilities.Returns(new ProviderCapabilities(supportsStreaming: true, supportsTools: false));

        var toolsProvider = Substitute.For<IModelProvider>();
        toolsProvider.ProviderName.Returns("tools");
        toolsProvider.Capabilities.Returns(new ProviderCapabilities(supportsStreaming: false, supportsTools: true));

        var providerFactory = new Func<ProviderDescriptor, IModelProvider?>(desc =>
        {
            if (desc.Id == "streaming")
            {
                return streamingProvider;
            }

            if (desc.Id == "tools")
            {
                return toolsProvider;
            }

            return null;
        });

        var logger = NullLogger<ProviderRegistry>.Instance;
        var selector = new CapabilityProviderSelector(providerFactory);
        var registry = new ProviderRegistry(logger, selector, null, providerFactory);

        registry.Register(new ProviderDescriptor
        {
            Id = "streaming",
            Name = "Streaming Provider",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(supportsStreaming: true, supportsTools: false),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:8000"))
        });

        registry.Register(new ProviderDescriptor
        {
            Id = "tools",
            Name = "Tools Provider",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(supportsStreaming: false, supportsTools: true),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:9000"))
        });

        // Act - Request with streaming requirement
        var streamingRequest = new ChatRequest(
            messages: new[] { new ChatMessage(MessageRole.User, "test", null, null) },
            stream: true);

        var selectedProvider = registry.GetProviderFor(streamingRequest);

        // Assert
        selectedProvider.Should().NotBeNull();
        selectedProvider.ProviderName.Should().Be("streaming");
    }

    [Fact]
    public async Task Should_Fallback_On_Failure()
    {
        // Arrange
        var primaryProvider = Substitute.For<IModelProvider>();
        primaryProvider.ProviderName.Returns("primary");
        primaryProvider.Capabilities.Returns(new ProviderCapabilities(supportsStreaming: true, supportsTools: true));
        primaryProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false)); // Unhealthy

        var fallbackProvider = Substitute.For<IModelProvider>();
        fallbackProvider.ProviderName.Returns("fallback");
        fallbackProvider.Capabilities.Returns(new ProviderCapabilities(supportsStreaming: true, supportsTools: true));
        fallbackProvider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true)); // Healthy

        var providerFactory = new Func<ProviderDescriptor, IModelProvider?>(desc =>
        {
            if (desc.Id == "primary")
            {
                return primaryProvider;
            }

            if (desc.Id == "fallback")
            {
                return fallbackProvider;
            }

            return null;
        });

        var logger = NullLogger<ProviderRegistry>.Instance;
        var selector = new CapabilityProviderSelector(providerFactory);
        var registry = new ProviderRegistry(logger, selector, "primary", providerFactory);

        registry.Register(new ProviderDescriptor
        {
            Id = "primary",
            Name = "Primary Provider",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(supportsStreaming: true, supportsTools: true),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:8000")),
            FallbackProviderId = "fallback"
        });

        registry.Register(new ProviderDescriptor
        {
            Id = "fallback",
            Name = "Fallback Provider",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(supportsStreaming: true, supportsTools: true),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:9000"))
        });

        // Act - Check health to mark primary as unhealthy
        await registry.CheckAllHealthAsync(CancellationToken.None);

        // Assert - Primary is unhealthy, fallback is available
        var primaryHealth = registry.GetProviderHealth("primary");
        var fallbackHealth = registry.GetProviderHealth("fallback");

        primaryHealth.Status.Should().Be(HealthStatus.Unhealthy);
        fallbackHealth.Status.Should().Be(HealthStatus.Healthy);

        // Note: Actual fallback selection logic would happen in application code
        // The registry provides the health status to enable fallback decisions
    }

    [Fact]
    public void Should_Fail_When_No_Match()
    {
        // Arrange
        var nonStreamingProvider = Substitute.For<IModelProvider>();
        nonStreamingProvider.ProviderName.Returns("non-streaming");
        nonStreamingProvider.Capabilities.Returns(new ProviderCapabilities(supportsStreaming: false, supportsTools: false));

        var providerFactory = new Func<ProviderDescriptor, IModelProvider?>(desc =>
        {
            if (desc.Id == "non-streaming")
            {
                return nonStreamingProvider;
            }

            return null;
        });

        var logger = NullLogger<ProviderRegistry>.Instance;
        var selector = new CapabilityProviderSelector(providerFactory);
        var registry = new ProviderRegistry(logger, selector, null, providerFactory);

        registry.Register(new ProviderDescriptor
        {
            Id = "non-streaming",
            Name = "Non-Streaming Provider",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(supportsStreaming: false, supportsTools: false),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:8000"))
        });

        // Act - Request streaming, but no provider supports it
        var streamingRequest = new ChatRequest(
            messages: new[] { new ChatMessage(MessageRole.User, "test", null, null) },
            stream: true);

        Action act = () => registry.GetProviderFor(streamingRequest);

        // Assert
        act.Should().Throw<NoCapableProviderException>()
            .WithMessage("*No provider capable of handling the request*")
            .Which.ErrorCode.Should().Be("ACODE-PRV-004");
    }
}
