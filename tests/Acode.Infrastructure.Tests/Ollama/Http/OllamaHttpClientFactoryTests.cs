using Acode.Infrastructure.Ollama;
using Acode.Infrastructure.Ollama.Http;
using FluentAssertions;
using NSubstitute;

namespace Acode.Infrastructure.Tests.Ollama.Http;

/// <summary>
/// Tests for OllamaHttpClientFactory.
/// Gap #3: Factory class for creating configured OllamaHttpClient instances.
/// </summary>
#pragma warning disable CA2007 // ConfigureAwait not needed in test methods
public sealed class OllamaHttpClientFactoryTests
{
    [Fact]
    public void Constructor_Should_AcceptDependencies()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var configuration = new OllamaConfiguration(baseUrl: "http://localhost:11434");

        // Act
        var act = () => new OllamaHttpClientFactory(httpClientFactory, configuration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_Should_ThrowOnNullHttpClientFactory()
    {
        // Arrange
        var configuration = new OllamaConfiguration(baseUrl: "http://localhost:11434");

        // Act
        var act = () => new OllamaHttpClientFactory(null!, configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClientFactory");
    }

    [Fact]
    public void Constructor_Should_ThrowOnNullConfiguration()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();

        // Act
        var act = () => new OllamaHttpClientFactory(httpClientFactory, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    [Fact]
    public void CreateClient_Should_ReturnConfiguredOllamaHttpClient()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var configuration = new OllamaConfiguration(
            baseUrl: "http://localhost:11434",
            requestTimeoutSeconds: 90);

        var httpClient = new HttpClient { BaseAddress = new Uri(configuration.BaseUrl) };
        httpClientFactory.CreateClient("Ollama").Returns(httpClient);

        var factory = new OllamaHttpClientFactory(httpClientFactory, configuration);

        // Act
        var ollamaClient = factory.CreateClient();

        // Assert
        ollamaClient.Should().NotBeNull();
        ollamaClient.BaseAddress.Should().Be(configuration.BaseUrl);
        ollamaClient.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateClient_Should_UseHttpClientFactory()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var configuration = new OllamaConfiguration(baseUrl: "http://localhost:11434");

        var httpClient = new HttpClient { BaseAddress = new Uri(configuration.BaseUrl) };
        httpClientFactory.CreateClient("Ollama").Returns(httpClient);

        var factory = new OllamaHttpClientFactory(httpClientFactory, configuration);

        // Act
        var ollamaClient = factory.CreateClient();

        // Assert
        httpClientFactory.Received(1).CreateClient("Ollama");
    }

    [Fact]
    public void CreateClient_Should_CreateNewInstanceEachTime()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var configuration = new OllamaConfiguration(baseUrl: "http://localhost:11434");

        var httpClient1 = new HttpClient { BaseAddress = new Uri(configuration.BaseUrl) };
        var httpClient2 = new HttpClient { BaseAddress = new Uri(configuration.BaseUrl) };
        httpClientFactory.CreateClient("Ollama").Returns(httpClient1, httpClient2);

        var factory = new OllamaHttpClientFactory(httpClientFactory, configuration);

        // Act
        var client1 = factory.CreateClient();
        var client2 = factory.CreateClient();

        // Assert
        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client1.CorrelationId.Should().NotBe(client2.CorrelationId); // Each instance should have unique correlation ID
        httpClientFactory.Received(2).CreateClient("Ollama");
    }

    [Fact]
    public void CreateClient_Should_ApplyConfigurationTimeout()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var configuration = new OllamaConfiguration(
            baseUrl: "http://localhost:11434",
            requestTimeoutSeconds: 45);

        var httpClient = new HttpClient { BaseAddress = new Uri(configuration.BaseUrl) };
        httpClientFactory.CreateClient("Ollama").Returns(httpClient);

        var factory = new OllamaHttpClientFactory(httpClientFactory, configuration);

        // Act
        var ollamaClient = factory.CreateClient();

        // Assert
        ollamaClient.Should().NotBeNull();
        configuration.RequestTimeout.Should().Be(TimeSpan.FromSeconds(45));
    }

    [Fact]
    public void CreateClient_Should_ApplyConfigurationBaseUrl()
    {
        // Arrange
        var customUrl = "http://custom-ollama:8080";
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var configuration = new OllamaConfiguration(baseUrl: customUrl);

        var httpClient = new HttpClient { BaseAddress = new Uri(customUrl) };
        httpClientFactory.CreateClient("Ollama").Returns(httpClient);

        var factory = new OllamaHttpClientFactory(httpClientFactory, configuration);

        // Act
        var ollamaClient = factory.CreateClient();

        // Assert
        ollamaClient.Should().NotBeNull();
        ollamaClient.BaseAddress.Should().Be(customUrl);
    }
}
