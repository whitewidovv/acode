using Acode.Infrastructure.Vllm.Client;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Vllm.Client;

public class VllmClientConfigurationTests
{
    [Fact]
    public void Constructor_Should_SetDefaultValues()
    {
        // Arrange & Act
        var config = new VllmClientConfiguration();

        // Assert
        config.Endpoint.Should().Be("http://localhost:8000");
        config.MaxConnections.Should().Be(10);
        config.IdleTimeoutSeconds.Should().Be(120);
        config.ConnectionLifetimeSeconds.Should().Be(300);
        config.ConnectTimeoutSeconds.Should().Be(5);
        config.RequestTimeoutSeconds.Should().Be(300);
        config.StreamingReadTimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public void Constructor_Should_AcceptCustomEndpoint()
    {
        // Arrange & Act
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://gpu-server:8000"
        };

        // Assert
        config.Endpoint.Should().Be("http://gpu-server:8000");
    }

    [Fact]
    public void Validate_Should_ThrowOnInvalidEndpoint()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "not-a-url"
        };

        // Act
        var act = () => config.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*valid URI*");
    }

    [Fact]
    public void Validate_Should_ThrowOnNegativeMaxConnections()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            MaxConnections = -1
        };

        // Act
        var act = () => config.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*MaxConnections*greater than 0*");
    }

    [Fact]
    public void Validate_Should_ThrowOnNegativeTimeouts()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            ConnectTimeoutSeconds = -1
        };

        // Act
        var act = () => config.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*timeout*greater than 0*");
    }

    [Fact]
    public void Validate_Should_PassWithValidConfiguration()
    {
        // Arrange
        var config = new VllmClientConfiguration
        {
            Endpoint = "http://localhost:8000",
            MaxConnections = 20,
            IdleTimeoutSeconds = 60,
            ConnectionLifetimeSeconds = 600,
            ConnectTimeoutSeconds = 10,
            RequestTimeoutSeconds = 600,
            StreamingReadTimeoutSeconds = 120
        };

        // Act
        var act = () => config.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ApiKey_Should_BeOptional()
    {
        // Arrange & Act
        var config = new VllmClientConfiguration();

        // Assert
        config.ApiKey.Should().BeNull();
    }

    [Fact]
    public void ApiKey_Should_AcceptValue()
    {
        // Arrange & Act
        var config = new VllmClientConfiguration
        {
            ApiKey = "test-key-123"
        };

        // Assert
        config.ApiKey.Should().Be("test-key-123");
    }
}
