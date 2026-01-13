namespace Acode.Application.Tests.Providers.Selection;

using System;
using System.Collections.Generic;
using Acode.Application.Inference;
using Acode.Application.Providers;
using Acode.Application.Providers.Selection;
using Acode.Domain.Models.Inference;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for DefaultProviderSelector implementation (task-004c spec).
/// Gap #10 from task-004c completion checklist.
/// </summary>
public sealed class DefaultProviderSelectorTests
{
    [Fact]
    public void Should_Return_Default_Provider_When_Healthy()
    {
        // Arrange
        var defaultProvider = CreateProviderMock("default-provider");
        var otherProvider = CreateProviderMock("other-provider");

        var descriptors = new List<ProviderDescriptor>
        {
            CreateDescriptor("default-provider"),
            CreateDescriptor("other-provider")
        };

        var healthStatus = new Dictionary<string, ProviderHealth>
        {
            ["default-provider"] = new ProviderHealth(HealthStatus.Healthy),
            ["other-provider"] = new ProviderHealth(HealthStatus.Healthy)
        };

        var request = CreateRequest();
        var selector = new DefaultProviderSelector(
            "default-provider",
            CreateProviderFactory(descriptors, new[] { defaultProvider, otherProvider }));

        // Act
        var result = selector.SelectProvider(descriptors, request, healthStatus);

        // Assert
        result.Should().Be(defaultProvider);
    }

    [Fact]
    public void Should_Fallback_To_First_Healthy_When_Default_Unhealthy()
    {
        // Arrange
        var defaultProvider = CreateProviderMock("default-provider");
        var fallbackProvider = CreateProviderMock("fallback-provider");

        var descriptors = new List<ProviderDescriptor>
        {
            CreateDescriptor("default-provider"),
            CreateDescriptor("fallback-provider")
        };

        var healthStatus = new Dictionary<string, ProviderHealth>
        {
            ["default-provider"] = new ProviderHealth(HealthStatus.Unhealthy),
            ["fallback-provider"] = new ProviderHealth(HealthStatus.Healthy)
        };

        var request = CreateRequest();
        var selector = new DefaultProviderSelector(
            "default-provider",
            CreateProviderFactory(descriptors, new[] { defaultProvider, fallbackProvider }));

        // Act
        var result = selector.SelectProvider(descriptors, request, healthStatus);

        // Assert
        result.Should().Be(fallbackProvider);
    }

    [Fact]
    public void Should_Return_Null_When_No_Healthy_Providers()
    {
        // Arrange
        var descriptor = CreateDescriptor("provider1");
        var descriptors = new List<ProviderDescriptor> { descriptor };

        var healthStatus = new Dictionary<string, ProviderHealth>
        {
            ["provider1"] = new ProviderHealth(HealthStatus.Unhealthy)
        };

        var request = CreateRequest();
        var selector = new DefaultProviderSelector(
            "provider1",
            CreateProviderFactory(descriptors, Array.Empty<IModelProvider>()));

        // Act
        var result = selector.SelectProvider(descriptors, request, healthStatus);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Should_Handle_Missing_Default_Provider()
    {
        // Arrange
        var provider = CreateProviderMock("available-provider");
        var descriptors = new List<ProviderDescriptor>
        {
            CreateDescriptor("available-provider")
        };

        var healthStatus = new Dictionary<string, ProviderHealth>
        {
            ["available-provider"] = new ProviderHealth(HealthStatus.Healthy)
        };

        var request = CreateRequest();
        var selector = new DefaultProviderSelector(
            "non-existent-provider",
            CreateProviderFactory(descriptors, new[] { provider }));

        // Act
        var result = selector.SelectProvider(descriptors, request, healthStatus);

        // Assert - should fallback to first healthy
        result.Should().Be(provider);
    }

    [Fact]
    public void Should_Respect_Degraded_As_Unhealthy()
    {
        // Arrange
        var defaultProvider = CreateProviderMock("default-provider");
        var fallbackProvider = CreateProviderMock("fallback-provider");

        var descriptors = new List<ProviderDescriptor>
        {
            CreateDescriptor("default-provider"),
            CreateDescriptor("fallback-provider")
        };

        var healthStatus = new Dictionary<string, ProviderHealth>
        {
            ["default-provider"] = new ProviderHealth(HealthStatus.Degraded),
            ["fallback-provider"] = new ProviderHealth(HealthStatus.Healthy)
        };

        var request = CreateRequest();
        var selector = new DefaultProviderSelector(
            "default-provider",
            CreateProviderFactory(descriptors, new[] { defaultProvider, fallbackProvider }));

        // Act
        var result = selector.SelectProvider(descriptors, request, healthStatus);

        // Assert - degraded should fallback
        result.Should().Be(fallbackProvider);
    }

    [Fact]
    public void Should_Handle_Empty_Provider_List()
    {
        // Arrange
        var descriptors = new List<ProviderDescriptor>();
        var healthStatus = new Dictionary<string, ProviderHealth>();
        var request = CreateRequest();
        var selector = new DefaultProviderSelector(
            "default-provider",
            CreateProviderFactory(descriptors, Array.Empty<IModelProvider>()));

        // Act
        var result = selector.SelectProvider(descriptors, request, healthStatus);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Should_Implement_IProviderSelector()
    {
        // Assert
        typeof(DefaultProviderSelector).Should().Implement<IProviderSelector>();
    }

    private static ProviderDescriptor CreateDescriptor(string id)
    {
        return new ProviderDescriptor
        {
            Id = id,
            Name = $"Provider {id}",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"))
        };
    }

    private static ChatRequest CreateRequest()
    {
        return new ChatRequest(
            messages: new[]
            {
                new ChatMessage(MessageRole.User, "test", null, null)
            });
    }

    private static IModelProvider CreateProviderMock(string id)
    {
        var provider = Substitute.For<IModelProvider>();
        provider.ProviderName.Returns(id);
        return provider;
    }

    private static Func<ProviderDescriptor, IModelProvider?> CreateProviderFactory(
        IReadOnlyList<ProviderDescriptor> descriptors,
        IReadOnlyList<IModelProvider> providers)
    {
        // Create mapping from descriptor ID to provider
        var mapping = new Dictionary<string, IModelProvider>();
        for (int i = 0; i < Math.Min(descriptors.Count, providers.Count); i++)
        {
            mapping[descriptors[i].Id] = providers[i];
        }

        return descriptor => mapping.TryGetValue(descriptor.Id, out var provider) ? provider : null;
    }
}
