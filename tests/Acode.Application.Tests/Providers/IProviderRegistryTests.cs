namespace Acode.Application.Tests.Providers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Inference;
using Acode.Application.Providers;
using Acode.Domain.Models.Inference;
using FluentAssertions;
using NSubstitute;
using Xunit;

using IProviderRegistry = Acode.Application.Providers.IProviderRegistry;

/// <summary>
/// Tests for IProviderRegistry interface contract (task-004c spec).
/// Gap #8 from task-004c completion checklist.
/// </summary>
public sealed class IProviderRegistryTests
{
    [Fact]
    public void Should_Have_Register_Method_With_ProviderDescriptor()
    {
        // Arrange
        var registry = Substitute.For<IProviderRegistry>();
        var descriptor = CreateTestDescriptor();

        // Act
        registry.Register(descriptor);

        // Assert
        registry.Received(1).Register(descriptor);
    }

    [Fact]
    public void Should_Have_Unregister_Method()
    {
        // Arrange
        var registry = Substitute.For<IProviderRegistry>();

        // Act
        registry.Unregister("test-provider");

        // Assert
        registry.Received(1).Unregister("test-provider");
    }

    [Fact]
    public void Should_Have_GetProvider_Method()
    {
        // Arrange
        var registry = Substitute.For<IProviderRegistry>();
        var provider = Substitute.For<IModelProvider>();
        registry.GetProvider("test-provider").Returns(provider);

        // Act
        var result = registry.GetProvider("test-provider");

        // Assert
        result.Should().Be(provider);
    }

    [Fact]
    public void Should_Have_GetDefaultProvider_Method()
    {
        // Arrange
        var registry = Substitute.For<IProviderRegistry>();
        var provider = Substitute.For<IModelProvider>();
        registry.GetDefaultProvider().Returns(provider);

        // Act
        var result = registry.GetDefaultProvider();

        // Assert
        result.Should().Be(provider);
    }

    [Fact]
    public void Should_Have_GetProviderFor_Method()
    {
        // Arrange
        var registry = Substitute.For<IProviderRegistry>();
        var request = new ChatRequest(
            messages: new[]
            {
                new ChatMessage(MessageRole.User, "test", null, null)
            });
        var provider = Substitute.For<IModelProvider>();
        registry.GetProviderFor(request).Returns(provider);

        // Act
        var result = registry.GetProviderFor(request);

        // Assert
        result.Should().Be(provider);
    }

    [Fact]
    public void Should_Have_ListProviders_Method()
    {
        // Arrange
        var registry = Substitute.For<IProviderRegistry>();
        var descriptors = new List<ProviderDescriptor>
        {
            CreateTestDescriptor("provider1"),
            CreateTestDescriptor("provider2")
        };
        registry.ListProviders().Returns(descriptors);

        // Act
        var result = registry.ListProviders();

        // Assert
        result.Should().BeEquivalentTo(descriptors);
    }

    [Fact]
    public void Should_Have_IsRegistered_Method()
    {
        // Arrange
        var registry = Substitute.For<IProviderRegistry>();
        registry.IsRegistered("test-provider").Returns(true);

        // Act
        var result = registry.IsRegistered("test-provider");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Should_Have_GetProviderHealth_Method()
    {
        // Arrange
        var registry = Substitute.For<IProviderRegistry>();
        var health = new ProviderHealth(HealthStatus.Healthy);
        registry.GetProviderHealth("test-provider").Returns(health);

        // Act
        var result = registry.GetProviderHealth("test-provider");

        // Assert
        result.Should().Be(health);
    }

    [Fact]
    public async Task Should_Have_CheckAllHealthAsync_Method()
    {
        // Arrange
        var registry = Substitute.For<IProviderRegistry>();
        var healthDict = new Dictionary<string, ProviderHealth>
        {
            ["provider1"] = new ProviderHealth(HealthStatus.Healthy),
            ["provider2"] = new ProviderHealth(HealthStatus.Degraded)
        };
        registry.CheckAllHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<string, ProviderHealth>>(healthDict));

        // Act
        var result = await registry.CheckAllHealthAsync(CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(healthDict);
    }

    [Fact]
    public void Should_Implement_IAsyncDisposable()
    {
        // Assert
        typeof(IProviderRegistry).Should().Implement<IAsyncDisposable>();
    }

    private static ProviderDescriptor CreateTestDescriptor(string id = "test-provider")
    {
        return new ProviderDescriptor
        {
            Id = id,
            Name = "Test Provider",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"))
        };
    }
}
