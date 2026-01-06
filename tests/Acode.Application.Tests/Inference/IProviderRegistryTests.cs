namespace Acode.Application.Tests.Inference;

using System.Threading.Tasks;
using Acode.Application.Inference;
using FluentAssertions;
using NSubstitute;

/// <summary>
/// Tests for IProviderRegistry interface contract following TDD (RED phase).
/// FR-004c-01 to FR-004c-10.
/// </summary>
#pragma warning disable CA2007 // ConfigureAwait not needed in test methods
public class IProviderRegistryTests
{
    [Fact]
    public void IProviderRegistry_HasRegisterMethod()
    {
        // FR-004c-01: IProviderRegistry MUST have Register method accepting IModelProvider
        var registry = Substitute.For<IProviderRegistry>();
        var provider = Substitute.For<IModelProvider>();

        registry.Register(provider);

        registry.Received(1).Register(provider);
    }

    [Fact]
    public void IProviderRegistry_HasGetProviderMethod()
    {
        // FR-004c-02: IProviderRegistry MUST have GetProvider method accepting provider name
        var registry = Substitute.For<IProviderRegistry>();
        var provider = Substitute.For<IModelProvider>();
        provider.ProviderName.Returns("ollama");

        registry.GetProvider("ollama").Returns(provider);

        var result = registry.GetProvider("ollama");

        result.Should().Be(provider);
    }

    [Fact]
    public void IProviderRegistry_GetProviderReturnsNullForUnknown()
    {
        // FR-004c-03: GetProvider MUST return null for unknown provider
        var registry = Substitute.For<IProviderRegistry>();

        registry.GetProvider("unknown").Returns((IModelProvider?)null);

        var result = registry.GetProvider("unknown");

        result.Should().BeNull();
    }

    [Fact]
    public void IProviderRegistry_HasGetAllProvidersMethod()
    {
        // FR-004c-04: IProviderRegistry MUST have GetAllProviders method
        var registry = Substitute.For<IProviderRegistry>();
        var provider1 = Substitute.For<IModelProvider>();
        var provider2 = Substitute.For<IModelProvider>();
        var providers = new[] { provider1, provider2 };

        registry.GetAllProviders().Returns(providers);

        var result = registry.GetAllProviders();

        result.Should().BeEquivalentTo(providers);
    }

    [Fact]
    public void IProviderRegistry_HasUnregisterMethod()
    {
        // FR-004c-05: IProviderRegistry MUST have Unregister method accepting provider name
        var registry = Substitute.For<IProviderRegistry>();

        registry.Unregister("ollama").Returns(true);

        var result = registry.Unregister("ollama");

        result.Should().BeTrue();
        registry.Received(1).Unregister("ollama");
    }

    [Fact]
    public void IProviderRegistry_HasContainsMethod()
    {
        // FR-004c-06: IProviderRegistry MUST have Contains method accepting provider name
        var registry = Substitute.For<IProviderRegistry>();

        registry.Contains("ollama").Returns(true);

        var result = registry.Contains("ollama");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IProviderRegistry_HasGetHealthyProviderAsyncMethod()
    {
        // FR-004c-07: IProviderRegistry MUST have GetHealthyProviderAsync method
        var registry = Substitute.For<IProviderRegistry>();
        var provider = Substitute.For<IModelProvider>();

        registry.GetHealthyProviderAsync().Returns(Task.FromResult<IModelProvider?>(provider));

        var result = await registry.GetHealthyProviderAsync();

        result.Should().Be(provider);
    }

    [Fact]
    public void IProviderRegistry_HasGetDefaultProviderNameMethod()
    {
        // FR-004c-08: IProviderRegistry MUST have GetDefaultProviderName method
        var registry = Substitute.For<IProviderRegistry>();

        registry.GetDefaultProviderName().Returns("ollama");

        var result = registry.GetDefaultProviderName();

        result.Should().Be("ollama");
    }

    [Fact]
    public void IProviderRegistry_HasSetDefaultProviderNameMethod()
    {
        // FR-004c-09: IProviderRegistry MUST have SetDefaultProviderName method
        var registry = Substitute.For<IProviderRegistry>();

        registry.SetDefaultProviderName("vllm");

        registry.Received(1).SetDefaultProviderName("vllm");
    }

    [Fact]
    public void IProviderRegistry_HasCountProperty()
    {
        // FR-004c-10: IProviderRegistry MUST have Count property
        var registry = Substitute.For<IProviderRegistry>();

        registry.Count.Returns(3);

        registry.Count.Should().Be(3);
    }
}
