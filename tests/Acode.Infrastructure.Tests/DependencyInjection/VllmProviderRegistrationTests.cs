using Acode.Application.Inference;
using Acode.Infrastructure.DependencyInjection;
using Acode.Infrastructure.Vllm;
using Acode.Infrastructure.Vllm.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Acode.Infrastructure.Tests.DependencyInjection;

public class VllmProviderRegistrationTests
{
    [Fact]
    public void AddVllmProvider_Should_RegisterProvider_When_NoConfigurationProvided()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVllmProvider();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetService<IModelProvider>();

        provider.Should().NotBeNull();
        provider.Should().BeOfType<VllmProvider>();
        provider!.ProviderName.Should().Be("vllm");
    }

    [Fact]
    public void AddVllmProvider_Should_UseDefaultConfiguration_When_NoConfigurationProvided()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVllmProvider();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var config = serviceProvider.GetService<VllmClientConfiguration>();

        config.Should().NotBeNull();
        config!.Endpoint.Should().Be("http://localhost:8000");
    }

    [Fact]
    public void AddVllmProvider_Should_UseProvidedConfiguration_When_ConfigurationProvided()
    {
        // Arrange
        var services = new ServiceCollection();
        var customConfig = new VllmClientConfiguration
        {
            Endpoint = "http://custom-endpoint:9000",
            ApiKey = "test-key",
            MaxConnections = 20,
        };

        // Act
        services.AddVllmProvider(customConfig);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var config = serviceProvider.GetService<VllmClientConfiguration>();

        config.Should().NotBeNull();
        config!.Endpoint.Should().Be("http://custom-endpoint:9000");
        config.ApiKey.Should().Be("test-key");
        config.MaxConnections.Should().Be(20);
    }

    [Fact]
    public void AddVllmProvider_Should_RegisterSingletonProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVllmProvider();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var provider1 = serviceProvider.GetService<IModelProvider>();
        var provider2 = serviceProvider.GetService<IModelProvider>();

        provider1.Should().BeSameAs(provider2);
    }

    [Fact]
    public void AddVllmProvider_Should_ThrowArgumentNullException_When_ServicesNull()
    {
        // Arrange
        ServiceCollection? services = null;

        // Act & Assert
#pragma warning disable CS8604 // Possible null reference argument - intentional for test
        var act = () => services.AddVllmProvider();
#pragma warning restore CS8604

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }
}
