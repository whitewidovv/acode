namespace Acode.Application.Tests.Providers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Inference;
using Acode.Application.Providers;
using Acode.Application.Providers.Exceptions;
using Acode.Application.Providers.Selection;
using Acode.Domain.Models.Inference;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for ProviderRegistry implementation (task-004c spec).
/// Gap #12 from task-004c completion checklist.
/// </summary>
public sealed class ProviderRegistryTests
{
    [Fact]
    public void Should_Register_Provider()
    {
        // Arrange
        var registry = CreateRegistry();
        var descriptor = CreateDescriptor("test-provider");

        // Act
        registry.Register(descriptor);

        // Assert
        registry.IsRegistered("test-provider").Should().BeTrue();
    }

    [Fact]
    public void Should_Throw_When_Registering_Duplicate()
    {
        // Arrange
        var registry = CreateRegistry();
        var descriptor = CreateDescriptor("test-provider");
        registry.Register(descriptor);

        // Act
        Action act = () => registry.Register(descriptor);

        // Assert
        act.Should().Throw<ProviderRegistrationException>()
            .WithMessage("*already registered*")
            .Which.ErrorCode.Should().Be("ACODE-PRV-001");
    }

    [Fact]
    public void Should_Unregister_Provider()
    {
        // Arrange
        var registry = CreateRegistry();
        var descriptor = CreateDescriptor("test-provider");
        registry.Register(descriptor);

        // Act
        registry.Unregister("test-provider");

        // Assert
        registry.IsRegistered("test-provider").Should().BeFalse();
    }

    [Fact]
    public void Should_Be_Idempotent_When_Unregistering_Missing_Provider()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        Action act = () => registry.Unregister("non-existent");

        // Assert - should not throw
        act.Should().NotThrow();
    }

    [Fact]
    public void Should_Get_Provider_By_Id()
    {
        // Arrange
        var provider = CreateProviderMock("test-provider");
        var registry = CreateRegistry(new Dictionary<string, IModelProvider> { ["test-provider"] = provider });
        var descriptor = CreateDescriptor("test-provider");
        registry.Register(descriptor);

        // Act
        var result = registry.GetProvider("test-provider");

        // Assert
        result.Should().Be(provider);
    }

    [Fact]
    public void Should_Throw_When_Getting_Non_Existent_Provider()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        Action act = () => registry.GetProvider("non-existent");

        // Assert
        act.Should().Throw<ProviderNotFoundException>()
            .WithMessage("*non-existent*")
            .Which.ErrorCode.Should().Be("ACODE-PRV-003");
    }

    [Fact]
    public void Should_Get_Default_Provider()
    {
        // Arrange
        var defaultProvider = CreateProviderMock("default");
        var registry = CreateRegistry(
            new Dictionary<string, IModelProvider> { ["default"] = defaultProvider },
            defaultProviderId: "default");
        var descriptor = CreateDescriptor("default");
        registry.Register(descriptor);

        // Act
        var result = registry.GetDefaultProvider();

        // Assert
        result.Should().Be(defaultProvider);
    }

    [Fact]
    public void Should_Throw_When_No_Default_Provider_Configured()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        Action act = () => registry.GetDefaultProvider();

        // Assert
        act.Should().Throw<ProviderNotFoundException>()
            .WithMessage("*default*");
    }

    [Fact]
    public void Should_Select_Provider_For_Request()
    {
        // Arrange
        var provider = CreateProviderMock("test-provider", supportsStreaming: true);
        var registry = CreateRegistry(new Dictionary<string, IModelProvider> { ["test-provider"] = provider });
        var descriptor = CreateDescriptor("test-provider", supportsStreaming: true);
        registry.Register(descriptor);

        var request = new ChatRequest(
            messages: new[] { new ChatMessage(MessageRole.User, "test", null, null) },
            stream: true);

        // Act
        var result = registry.GetProviderFor(request);

        // Assert
        result.Should().Be(provider);
    }

    [Fact]
    public void Should_Throw_When_No_Capable_Provider_For_Request()
    {
        // Arrange
        var provider = CreateProviderMock("test-provider", supportsStreaming: false);
        var registry = CreateRegistry(new Dictionary<string, IModelProvider> { ["test-provider"] = provider });
        var descriptor = CreateDescriptor("test-provider", supportsStreaming: false);
        registry.Register(descriptor);

        var request = new ChatRequest(
            messages: new[] { new ChatMessage(MessageRole.User, "test", null, null) },
            stream: true);

        // Act
        Action act = () => registry.GetProviderFor(request);

        // Assert
        act.Should().Throw<NoCapableProviderException>()
            .Which.ErrorCode.Should().Be("ACODE-PRV-004");
    }

    [Fact]
    public void Should_List_All_Providers()
    {
        // Arrange
        var registry = CreateRegistry();
        var descriptor1 = CreateDescriptor("provider1");
        var descriptor2 = CreateDescriptor("provider2");
        registry.Register(descriptor1);
        registry.Register(descriptor2);

        // Act
        var result = registry.ListProviders();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(d => d.Id == "provider1");
        result.Should().Contain(d => d.Id == "provider2");
    }

    [Fact]
    public void Should_Check_If_Provider_Is_Registered()
    {
        // Arrange
        var registry = CreateRegistry();
        var descriptor = CreateDescriptor("test-provider");
        registry.Register(descriptor);

        // Act & Assert
        registry.IsRegistered("test-provider").Should().BeTrue();
        registry.IsRegistered("non-existent").Should().BeFalse();
    }

    [Fact]
    public void Should_Get_Provider_Health()
    {
        // Arrange
        var provider = CreateProviderMock("test-provider");
        provider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        var registry = CreateRegistry(new Dictionary<string, IModelProvider> { ["test-provider"] = provider });
        var descriptor = CreateDescriptor("test-provider");
        registry.Register(descriptor);

        // Act
        var result = registry.GetProviderHealth("test-provider");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Unknown); // Initial state before first check
    }

    [Fact]
    public async Task Should_Check_All_Provider_Health()
    {
        // Arrange
        var provider1 = CreateProviderMock("provider1");
        var provider2 = CreateProviderMock("provider2");
        provider1.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        provider2.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

        var registry = CreateRegistry(new Dictionary<string, IModelProvider>
        {
            ["provider1"] = provider1,
            ["provider2"] = provider2
        });

        registry.Register(CreateDescriptor("provider1"));
        registry.Register(CreateDescriptor("provider2"));

        // Act
        var result = await registry.CheckAllHealthAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result["provider1"].Status.Should().Be(HealthStatus.Healthy);
        result["provider2"].Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task Should_Dispose_Async()
    {
        // Arrange
        var registry = CreateRegistry();
        var descriptor = CreateDescriptor("test-provider");
        registry.Register(descriptor);

        // Act
        await registry.DisposeAsync();

        // Assert - should not throw
        // Registry should be in disposed state
    }

    private static ProviderRegistry CreateRegistry(
        Dictionary<string, IModelProvider>? providerInstances = null,
        string? defaultProviderId = null)
    {
        var logger = Substitute.For<ILogger<ProviderRegistry>>();
        var selector = new CapabilityProviderSelector(descriptor =>
        {
            if (providerInstances != null && providerInstances.TryGetValue(descriptor.Id, out var provider))
            {
                return provider;
            }

            return null;
        });

        Func<ProviderDescriptor, IModelProvider?>? providerFactory = null;
        if (providerInstances != null)
        {
            providerFactory = descriptor =>
            {
                if (providerInstances.TryGetValue(descriptor.Id, out var provider))
                {
                    return provider;
                }

                return null;
            };
        }

        return new ProviderRegistry(logger, selector, defaultProviderId, providerFactory);
    }

    private static ProviderDescriptor CreateDescriptor(string id, bool supportsStreaming = false, bool supportsTools = false)
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

    private static IModelProvider CreateProviderMock(string id, bool supportsStreaming = false, bool supportsTools = false)
    {
        var provider = Substitute.For<IModelProvider>();
        provider.ProviderName.Returns(id);
        provider.Capabilities.Returns(new ProviderCapabilities(
            supportsStreaming: supportsStreaming,
            supportsTools: supportsTools));
        provider.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        return provider;
    }
}
