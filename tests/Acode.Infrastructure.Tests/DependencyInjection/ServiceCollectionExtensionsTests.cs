namespace Acode.Infrastructure.Tests.DependencyInjection;

using Acode.Application.Inference;
using Acode.Application.Tools;
using Acode.Application.Tools.Retry;
using Acode.Infrastructure.DependencyInjection;
using Acode.Infrastructure.Ollama;
using Acode.Infrastructure.Tools;
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

    [Fact]
    public void AddToolSchemaRegistry_RegistersIToolSchemaRegistry()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddToolSchemaRegistry();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var registry = serviceProvider.GetService<IToolSchemaRegistry>();
        registry.Should().NotBeNull();
        registry.Should().BeOfType<ToolSchemaRegistry>();
    }

    [Fact]
    public void AddToolSchemaRegistry_RegistryIsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToolSchemaRegistry();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var registry1 = serviceProvider.GetService<IToolSchemaRegistry>();
        var registry2 = serviceProvider.GetService<IToolSchemaRegistry>();

        // Assert
        registry1.Should().BeSameAs(registry2);
    }

    [Fact]
    public void AddToolSchemaRegistry_RegistryStartsEmpty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToolSchemaRegistry();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var registry = serviceProvider.GetRequiredService<IToolSchemaRegistry>();

        // Assert
        registry.Count.Should().Be(0);
        registry.GetAllTools().Should().BeEmpty();
    }

    [Fact]
    public void AddToolSchemaRegistry_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddToolSchemaRegistry();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddToolValidationRetry_RegistersIValidationErrorFormatter()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddToolValidationRetry();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var formatter = serviceProvider.GetService<IValidationErrorFormatter>();
        formatter.Should().NotBeNull();
        formatter.Should().BeOfType<ValidationErrorFormatter>();
    }

    [Fact]
    public void AddToolValidationRetry_RegistersIRetryTracker()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddToolValidationRetry();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var tracker = serviceProvider.GetService<IRetryTracker>();
        tracker.Should().NotBeNull();
        tracker.Should().BeOfType<RetryTracker>();
    }

    [Fact]
    public void AddToolValidationRetry_RetryTrackerIsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddToolValidationRetry();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var tracker1 = serviceProvider.GetService<IRetryTracker>();
        var tracker2 = serviceProvider.GetService<IRetryTracker>();

        // Assert
        tracker1.Should().BeSameAs(tracker2);
    }

    [Fact]
    public void AddToolValidationRetry_WithCustomConfig_UsesConfig()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new RetryConfiguration { MaxRetries = 5 };

        // Act
        services.AddToolValidationRetry(config);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var registeredConfig = serviceProvider.GetService<RetryConfiguration>();
        registeredConfig.Should().NotBeNull();
        registeredConfig!.MaxRetries.Should().Be(5);
    }

    [Fact]
    public void AddToolValidationRetry_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddToolValidationRetry();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
