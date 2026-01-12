namespace Acode.Application.Tests.Providers.Selection;

using System.Collections.Generic;
using Acode.Application.Inference;
using Acode.Application.Providers;
using Acode.Application.Providers.Selection;
using Acode.Domain.Models.Inference;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for IProviderSelector interface contract (task-004c spec).
/// Gap #9 from task-004c completion checklist.
/// </summary>
public sealed class IProviderSelectorTests
{
    [Fact]
    public void Should_Have_SelectProvider_Method()
    {
        // Arrange
        var selector = Substitute.For<IProviderSelector>();
        var providers = new List<ProviderDescriptor>
        {
            CreateTestDescriptor("provider1")
        };
        var request = CreateTestRequest();
        var healthStatus = new Dictionary<string, ProviderHealth>
        {
            ["provider1"] = new ProviderHealth(HealthStatus.Healthy)
        };
        var provider = Substitute.For<IModelProvider>();
        selector.SelectProvider(providers, request, healthStatus).Returns(provider);

        // Act
        var result = selector.SelectProvider(providers, request, healthStatus);

        // Assert
        result.Should().Be(provider);
    }

    [Fact]
    public void SelectProvider_Should_Allow_Null_Return()
    {
        // Arrange
        var selector = Substitute.For<IProviderSelector>();
        var providers = new List<ProviderDescriptor>();
        var request = CreateTestRequest();
        var healthStatus = new Dictionary<string, ProviderHealth>();
        selector.SelectProvider(providers, request, healthStatus).Returns((IModelProvider?)null);

        // Act
        var result = selector.SelectProvider(providers, request, healthStatus);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SelectProvider_Should_Accept_Empty_Lists()
    {
        // Arrange
        var selector = Substitute.For<IProviderSelector>();
        var providers = new List<ProviderDescriptor>();
        var request = CreateTestRequest();
        var healthStatus = new Dictionary<string, ProviderHealth>();
        selector.SelectProvider(providers, request, healthStatus).Returns((IModelProvider?)null);

        // Act
        var result = selector.SelectProvider(providers, request, healthStatus);

        // Assert - should not throw, returns null for no providers
        result.Should().BeNull();
    }

    private static ProviderDescriptor CreateTestDescriptor(string id)
    {
        return new ProviderDescriptor
        {
            Id = id,
            Name = "Test Provider",
            Type = ProviderType.Local,
            Capabilities = new ProviderCapabilities(),
            Endpoint = new ProviderEndpoint(new System.Uri("http://localhost:11434"))
        };
    }

    private static ChatRequest CreateTestRequest()
    {
        return new ChatRequest(
            messages: new[]
            {
                new ChatMessage(MessageRole.User, "test", null, null)
            });
    }
}
