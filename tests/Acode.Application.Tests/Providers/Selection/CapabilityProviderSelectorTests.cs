namespace Acode.Application.Tests.Providers.Selection;

using System;
using System.Collections.Generic;
using System.Text.Json;
using Acode.Application.Inference;
using Acode.Application.Providers;
using Acode.Application.Providers.Selection;
using Acode.Domain.Models.Inference;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for CapabilityProviderSelector implementation (task-004c spec).
/// Gap #11 from task-004c completion checklist.
/// </summary>
public sealed class CapabilityProviderSelectorTests
{
    [Fact]
    public void Should_Select_Provider_Supporting_Streaming()
    {
        // Arrange
        var nonStreamingProvider = CreateProviderMock("provider1", supportsStreaming: false);
        var streamingProvider = CreateProviderMock("provider2", supportsStreaming: true);

        var descriptors = new List<ProviderDescriptor>
        {
            CreateDescriptor("provider1", supportsStreaming: false),
            CreateDescriptor("provider2", supportsStreaming: true)
        };

        var healthStatus = new Dictionary<string, ProviderHealth>
        {
            ["provider1"] = new ProviderHealth(HealthStatus.Healthy),
            ["provider2"] = new ProviderHealth(HealthStatus.Healthy)
        };

        var request = CreateRequest(stream: true);
        var selector = new CapabilityProviderSelector(
            CreateProviderFactory(descriptors, new[] { nonStreamingProvider, streamingProvider }));

        // Act
        var result = selector.SelectProvider(descriptors, request, healthStatus);

        // Assert
        result.Should().Be(streamingProvider);
    }

    [Fact]
    public void Should_Select_Provider_Supporting_Tools()
    {
        // Arrange
        var nonToolProvider = CreateProviderMock("provider1", supportsTools: false);
        var toolProvider = CreateProviderMock("provider2", supportsTools: true);

        var descriptors = new List<ProviderDescriptor>
        {
            CreateDescriptor("provider1", supportsTools: false),
            CreateDescriptor("provider2", supportsTools: true)
        };

        var healthStatus = new Dictionary<string, ProviderHealth>
        {
            ["provider1"] = new ProviderHealth(HealthStatus.Healthy),
            ["provider2"] = new ProviderHealth(HealthStatus.Healthy)
        };

        var toolParams = JsonDocument.Parse("{\"type\":\"object\",\"properties\":{}}").RootElement;
        var request = CreateRequest(tools: new[] { new ToolDefinition("test_tool", "A test tool", toolParams) });
        var selector = new CapabilityProviderSelector(
            CreateProviderFactory(descriptors, new[] { nonToolProvider, toolProvider }));

        // Act
        var result = selector.SelectProvider(descriptors, request, healthStatus);

        // Assert
        result.Should().Be(toolProvider);
    }

    [Fact]
    public void Should_Prefer_Healthy_Over_Degraded()
    {
        // Arrange
        var degradedProvider = CreateProviderMock("provider1");
        var healthyProvider = CreateProviderMock("provider2");

        var descriptors = new List<ProviderDescriptor>
        {
            CreateDescriptor("provider1"),
            CreateDescriptor("provider2")
        };

        var healthStatus = new Dictionary<string, ProviderHealth>
        {
            ["provider1"] = new ProviderHealth(HealthStatus.Degraded),
            ["provider2"] = new ProviderHealth(HealthStatus.Healthy)
        };

        var request = CreateRequest();
        var selector = new CapabilityProviderSelector(
            CreateProviderFactory(descriptors, new[] { degradedProvider, healthyProvider }));

        // Act
        var result = selector.SelectProvider(descriptors, request, healthStatus);

        // Assert
        result.Should().Be(healthyProvider);
    }

    [Fact]
    public void Should_Return_Null_When_No_Provider_Supports_Required_Capability()
    {
        // Arrange
        var provider = CreateProviderMock("provider1", supportsStreaming: false);

        var descriptors = new List<ProviderDescriptor>
        {
            CreateDescriptor("provider1", supportsStreaming: false)
        };

        var healthStatus = new Dictionary<string, ProviderHealth>
        {
            ["provider1"] = new ProviderHealth(HealthStatus.Healthy)
        };

        var request = CreateRequest(stream: true);
        var selector = new CapabilityProviderSelector(
            CreateProviderFactory(descriptors, new[] { provider }));

        // Act
        var result = selector.SelectProvider(descriptors, request, healthStatus);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Should_Select_First_Capable_Provider_When_Multiple_Match()
    {
        // Arrange
        var provider1 = CreateProviderMock("provider1", supportsStreaming: true);
        var provider2 = CreateProviderMock("provider2", supportsStreaming: true);

        var descriptors = new List<ProviderDescriptor>
        {
            CreateDescriptor("provider1", supportsStreaming: true),
            CreateDescriptor("provider2", supportsStreaming: true)
        };

        var healthStatus = new Dictionary<string, ProviderHealth>
        {
            ["provider1"] = new ProviderHealth(HealthStatus.Healthy),
            ["provider2"] = new ProviderHealth(HealthStatus.Healthy)
        };

        var request = CreateRequest(stream: true);
        var selector = new CapabilityProviderSelector(
            CreateProviderFactory(descriptors, new[] { provider1, provider2 }));

        // Act
        var result = selector.SelectProvider(descriptors, request, healthStatus);

        // Assert
        result.Should().Be(provider1);
    }

    [Fact]
    public void Should_Handle_Request_With_No_Special_Requirements()
    {
        // Arrange
        var provider = CreateProviderMock("provider1");

        var descriptors = new List<ProviderDescriptor>
        {
            CreateDescriptor("provider1")
        };

        var healthStatus = new Dictionary<string, ProviderHealth>
        {
            ["provider1"] = new ProviderHealth(HealthStatus.Healthy)
        };

        var request = CreateRequest(); // No streaming, no tools
        var selector = new CapabilityProviderSelector(
            CreateProviderFactory(descriptors, new[] { provider }));

        // Act
        var result = selector.SelectProvider(descriptors, request, healthStatus);

        // Assert - any healthy provider works
        result.Should().Be(provider);
    }

    [Fact]
    public void Should_Return_Null_When_All_Providers_Unhealthy()
    {
        // Arrange
        var provider = CreateProviderMock("provider1");

        var descriptors = new List<ProviderDescriptor>
        {
            CreateDescriptor("provider1")
        };

        var healthStatus = new Dictionary<string, ProviderHealth>
        {
            ["provider1"] = new ProviderHealth(HealthStatus.Unhealthy)
        };

        var request = CreateRequest();
        var selector = new CapabilityProviderSelector(
            CreateProviderFactory(descriptors, new[] { provider }));

        // Act
        var result = selector.SelectProvider(descriptors, request, healthStatus);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Should_Implement_IProviderSelector()
    {
        // Assert
        typeof(CapabilityProviderSelector).Should().Implement<IProviderSelector>();
    }

    private static ProviderDescriptor CreateDescriptor(
        string id,
        bool supportsStreaming = false,
        bool supportsTools = false)
    {
        return new ProviderDescriptor
        {
            Id = id,
            Name = $"Provider {id}",
            Type = ProviderType.Local,
            Capabilities = new ProviderCapabilities(
                supportsStreaming: supportsStreaming,
                supportsTools: supportsTools),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"))
        };
    }

    private static ChatRequest CreateRequest(bool stream = false, ToolDefinition[]? tools = null)
    {
        return new ChatRequest(
            messages: new[]
            {
                new ChatMessage(MessageRole.User, "test", null, null)
            },
            modelParameters: null,
            tools: tools,
            stream: stream);
    }

    private static IModelProvider CreateProviderMock(
        string id,
        bool supportsStreaming = false,
        bool supportsTools = false)
    {
        var provider = Substitute.For<IModelProvider>();
        provider.ProviderName.Returns(id);
        provider.Capabilities.Returns(new ProviderCapabilities(
            supportsStreaming: supportsStreaming,
            supportsTools: supportsTools));
        return provider;
    }

    private static Func<ProviderDescriptor, IModelProvider?> CreateProviderFactory(
        IReadOnlyList<ProviderDescriptor> descriptors,
        IReadOnlyList<IModelProvider> providers)
    {
        var mapping = new Dictionary<string, IModelProvider>();
        for (int i = 0; i < Math.Min(descriptors.Count, providers.Count); i++)
        {
            mapping[descriptors[i].Id] = providers[i];
        }

        return descriptor => mapping.TryGetValue(descriptor.Id, out var provider) ? provider : null;
    }
}
