namespace Acode.Infrastructure.Tests.DependencyInjection;

using Acode.Application.Inference;
using Acode.Infrastructure.DependencyInjection;
using Acode.Infrastructure.Ollama;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddOllamaProvider_WithDefaults_RegistersProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOllamaProvider();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var provider = serviceProvider.GetService<IModelProvider>();
        provider.Should().NotBeNull();
        provider.Should().BeOfType<OllamaProvider>();
        provider!.ProviderName.Should().Be("ollama");
    }

    [Fact]
    public void AddOllamaProvider_WithCustomConfiguration_UsesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new OllamaConfiguration(
            baseUrl: "http://custom-host:8080",
            defaultModel: "custom-model:latest");

        // Act
        services.AddOllamaProvider(config);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var provider = serviceProvider.GetService<IModelProvider>();
        provider.Should().NotBeNull();
        provider!.Capabilities.DefaultModel.Should().Be("custom-model:latest");

        // Verify configuration was registered
        var registeredConfig = serviceProvider.GetService<OllamaConfiguration>();
        registeredConfig.Should().NotBeNull();
        registeredConfig!.BaseUrl.Should().Be("http://custom-host:8080");
        registeredConfig.DefaultModel.Should().Be("custom-model:latest");
    }

    [Fact]
    public void AddOllamaProvider_RegistersHttpClientFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOllamaProvider();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var httpClientFactory = serviceProvider.GetService<System.Net.Http.IHttpClientFactory>();
        httpClientFactory.Should().NotBeNull();

        var httpClient = httpClientFactory!.CreateClient("Ollama");
        httpClient.Should().NotBeNull();
        httpClient.BaseAddress.Should().Be(new System.Uri("http://localhost:11434"));
    }

    [Fact]
    public void AddOllamaProvider_ProviderIsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOllamaProvider();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var provider1 = serviceProvider.GetService<IModelProvider>();
        var provider2 = serviceProvider.GetService<IModelProvider>();

        // Assert
        provider1.Should().BeSameAs(provider2);
    }
}
