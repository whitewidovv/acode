namespace Acode.Application.Tests.Providers;

using System;
using System.Collections.Generic;
using Acode.Application.Providers;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for ProviderEndpoint record.
/// Gap #18 from task-004c completion checklist.
/// </summary>
public sealed class ProviderEndpointTests
{
    [Fact]
    public void Should_Require_BaseUrl()
    {
        // Arrange & Act
        Action act = () => new ProviderEndpoint(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*baseUrl*");
    }

    [Fact]
    public void Should_Validate_Url_Scheme()
    {
        // Arrange - Invalid schemes (not HTTP/HTTPS)
        Action actFtp = () => new ProviderEndpoint(new Uri("ftp://localhost:21"));
        Action actFile = () => new ProviderEndpoint(new Uri("file:///C:/test"));

        // Assert
        actFtp.Should().Throw<ArgumentException>()
            .WithMessage("*HTTP or HTTPS*");

        actFile.Should().Throw<ArgumentException>()
            .WithMessage("*HTTP or HTTPS*");
    }

    [Fact]
    public void Should_Accept_Valid_Http_Url()
    {
        // Arrange & Act
        var endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"));

        // Assert
        endpoint.BaseUrl.Should().Be(new Uri("http://localhost:11434"));
        endpoint.BaseUrl.Scheme.Should().Be("http");
    }

    [Fact]
    public void Should_Accept_Valid_Https_Url()
    {
        // Arrange & Act
        var endpoint = new ProviderEndpoint(new Uri("https://api.example.com"));

        // Assert
        endpoint.BaseUrl.Should().Be(new Uri("https://api.example.com"));
        endpoint.BaseUrl.Scheme.Should().Be("https");
    }

    [Fact]
    public void Should_Provide_Default_ConnectTimeout()
    {
        // Arrange & Act
        var endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"));

        // Assert
        endpoint.ConnectTimeout.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Should_Provide_Default_RequestTimeout()
    {
        // Arrange & Act
        var endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"));

        // Assert
        endpoint.RequestTimeout.Should().Be(TimeSpan.FromSeconds(300));
    }

    [Fact]
    public void Should_Provide_Default_MaxRetries()
    {
        // Arrange & Act
        var endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"));

        // Assert
        endpoint.MaxRetries.Should().Be(3);
    }

    [Fact]
    public void Should_Allow_Custom_ConnectTimeout()
    {
        // Arrange & Act
        var endpoint = new ProviderEndpoint(
            new Uri("http://localhost:11434"),
            connectTimeout: TimeSpan.FromSeconds(10));

        // Assert
        endpoint.ConnectTimeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Should_Allow_Custom_RequestTimeout()
    {
        // Arrange & Act
        var endpoint = new ProviderEndpoint(
            new Uri("http://localhost:11434"),
            requestTimeout: TimeSpan.FromMinutes(10));

        // Assert
        endpoint.RequestTimeout.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void Should_Allow_Custom_MaxRetries()
    {
        // Arrange & Act
        var endpoint = new ProviderEndpoint(
            new Uri("http://localhost:11434"),
            maxRetries: 5);

        // Assert
        endpoint.MaxRetries.Should().Be(5);
    }

    [Fact]
    public void Should_Validate_ConnectTimeout_Positive()
    {
        // Arrange & Act
        Action actZero = () => new ProviderEndpoint(
            new Uri("http://localhost:11434"),
            connectTimeout: TimeSpan.Zero);

        Action actNegative = () => new ProviderEndpoint(
            new Uri("http://localhost:11434"),
            connectTimeout: TimeSpan.FromSeconds(-1));

        // Assert
        actZero.Should().Throw<ArgumentException>()
            .WithMessage("*ConnectTimeout must be positive*");

        actNegative.Should().Throw<ArgumentException>()
            .WithMessage("*ConnectTimeout must be positive*");
    }

    [Fact]
    public void Should_Validate_RequestTimeout_Positive()
    {
        // Arrange & Act
        Action actZero = () => new ProviderEndpoint(
            new Uri("http://localhost:11434"),
            requestTimeout: TimeSpan.Zero);

        Action actNegative = () => new ProviderEndpoint(
            new Uri("http://localhost:11434"),
            requestTimeout: TimeSpan.FromSeconds(-1));

        // Assert
        actZero.Should().Throw<ArgumentException>()
            .WithMessage("*RequestTimeout must be positive*");

        actNegative.Should().Throw<ArgumentException>()
            .WithMessage("*RequestTimeout must be positive*");
    }

    [Fact]
    public void Should_Validate_MaxRetries_NonNegative()
    {
        // Arrange & Act
        Action act = () => new ProviderEndpoint(
            new Uri("http://localhost:11434"),
            maxRetries: -1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*MaxRetries must be >= 0*");
    }

    [Fact]
    public void Should_Allow_Zero_MaxRetries()
    {
        // Arrange & Act
        var endpoint = new ProviderEndpoint(
            new Uri("http://localhost:11434"),
            maxRetries: 0);

        // Assert
        endpoint.MaxRetries.Should().Be(0);
    }

    [Fact]
    public void Should_Support_Optional_Headers()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token123",
            ["X-Custom-Header"] = "value"
        };

        // Act
        var endpoint = new ProviderEndpoint(
            new Uri("http://localhost:11434"),
            headers: headers);

        // Assert
        endpoint.Headers.Should().NotBeNull();
        endpoint.Headers.Should().ContainKey("Authorization");
        endpoint.Headers.Should().ContainKey("X-Custom-Header");
        endpoint.Headers!["Authorization"].Should().Be("Bearer token123");
        endpoint.Headers["X-Custom-Header"].Should().Be("value");
    }

    [Fact]
    public void Should_Allow_Null_Headers()
    {
        // Arrange & Act
        var endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"));

        // Assert
        endpoint.Headers.Should().BeNull();
    }

    [Fact]
    public void Should_Be_Immutable()
    {
        // Arrange
        var original = new ProviderEndpoint(new Uri("http://localhost:11434"));

        // Act - use 'with' to create modified copy
        var modified = original with { MaxRetries = 10 };

        // Assert - original unchanged
        original.MaxRetries.Should().Be(3);
        modified.MaxRetries.Should().Be(10);
        original.Should().NotBeSameAs(modified);
    }

    [Fact]
    public void Should_Support_All_Properties()
    {
        // Arrange
        var baseUrl = new Uri("https://api.example.com/v1");
        var connectTimeout = TimeSpan.FromSeconds(10);
        var requestTimeout = TimeSpan.FromMinutes(5);
        var maxRetries = 5;
        var headers = new Dictionary<string, string>
        {
            ["X-API-Key"] = "secret123"
        };

        // Act
        var endpoint = new ProviderEndpoint(
            baseUrl,
            connectTimeout,
            requestTimeout,
            maxRetries,
            headers);

        // Assert
        endpoint.BaseUrl.Should().BeSameAs(baseUrl);
        endpoint.ConnectTimeout.Should().Be(connectTimeout);
        endpoint.RequestTimeout.Should().Be(requestTimeout);
        endpoint.MaxRetries.Should().Be(maxRetries);
        endpoint.Headers.Should().BeSameAs(headers);
    }
}
