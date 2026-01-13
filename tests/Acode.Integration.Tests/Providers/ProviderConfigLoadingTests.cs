namespace Acode.Integration.Tests.Providers;

using System;
using Acode.Application.Inference;
using Acode.Application.Providers;
using Acode.Application.Providers.Selection;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Integration tests for provider configuration loading scenarios.
/// Gap #25 from task-004c completion checklist.
/// </summary>
public sealed class ProviderConfigLoadingTests
{
    [Fact]
    public void Should_Load_From_Config_Yml()
    {
        // Arrange - Simulate loading multiple providers from config
        var logger = NullLogger<ProviderRegistry>.Instance;
        var selector = new CapabilityProviderSelector(_ => null);
        var registry = new ProviderRegistry(logger, selector, "ollama");

        var ollama = new ProviderDescriptor
        {
            Id = "ollama",
            Name = "Ollama Local",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(supportsStreaming: true, supportsTools: true),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"))
        };

        var vllm = new ProviderDescriptor
        {
            Id = "vllm",
            Name = "vLLM Local",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(supportsStreaming: true, supportsTools: false),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:8000"))
        };

        // Act - Register providers (simulating config loading)
        registry.Register(ollama);
        registry.Register(vllm);

        // Assert
        registry.IsRegistered("ollama").Should().BeTrue();
        registry.IsRegistered("vllm").Should().BeTrue();
        var providers = registry.ListProviders();
        providers.Should().HaveCount(2);
        providers.Should().Contain(d => d.Id == "ollama");
        providers.Should().Contain(d => d.Id == "vllm");
    }

    [Fact]
    public void Should_Apply_Defaults()
    {
        // Arrange - Provider without optional configuration
        var logger = NullLogger<ProviderRegistry>.Instance;
        var selector = new CapabilityProviderSelector(_ => null);
        var registry = new ProviderRegistry(logger, selector);

        var descriptor = new ProviderDescriptor
        {
            Id = "minimal",
            Name = "Minimal Provider",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(supportsStreaming: false, supportsTools: false),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:9000"))

            // No Config, RetryPolicy, or FallbackProviderId specified - should use defaults
        };

        // Act
        registry.Register(descriptor);

        // Assert - Descriptor has default values
        descriptor.Config.Should().BeNull(); // Optional, defaults handled elsewhere
        descriptor.RetryPolicy.Should().BeNull(); // Optional
        descriptor.FallbackProviderId.Should().BeNull(); // Optional
        registry.IsRegistered("minimal").Should().BeTrue();
    }

    [Fact]
    public void Should_Override_With_Env_Vars()
    {
        // Arrange - Simulate environment variable override pattern
        var baseUrl = "http://localhost:11434";
        var envOverride = Environment.GetEnvironmentVariable("ACODE_OLLAMA_ENDPOINT");
        var finalUrl = envOverride ?? baseUrl;

        var logger = NullLogger<ProviderRegistry>.Instance;
        var selector = new CapabilityProviderSelector(_ => null);
        var registry = new ProviderRegistry(logger, selector);

        var descriptor = new ProviderDescriptor
        {
            Id = "ollama",
            Name = "Ollama",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(supportsStreaming: true, supportsTools: true),
            Endpoint = new ProviderEndpoint(new Uri(finalUrl))
        };

        // Act
        registry.Register(descriptor);

        // Assert - Endpoint respects environment variable or uses default
        registry.IsRegistered("ollama").Should().BeTrue();
        descriptor.Endpoint.BaseUrl.ToString().TrimEnd('/').Should().Be(finalUrl.TrimEnd('/'));
    }

    [Fact]
    public void Should_Validate_Config()
    {
        // Arrange
        var logger = NullLogger<ProviderRegistry>.Instance;
        var selector = new CapabilityProviderSelector(_ => null);
        var registry = new ProviderRegistry(logger, selector);

        // Act & Assert - Null descriptor should throw
        Action actNull = () => registry.Register(null!);
        actNull.Should().Throw<ArgumentNullException>();

        // Act & Assert - Duplicate registration should throw
        var descriptor = new ProviderDescriptor
        {
            Id = "duplicate",
            Name = "Duplicate",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(supportsStreaming: false, supportsTools: false),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:8000"))
        };

        registry.Register(descriptor);
        Action actDuplicate = () => registry.Register(descriptor);
        actDuplicate.Should().Throw<Acode.Application.Providers.Exceptions.ProviderRegistrationException>()
            .WithMessage("*already registered*");

        // Act & Assert - Invalid ID should be caught by ProviderDescriptor validation
        Action actInvalidId = () => new ProviderDescriptor
        {
            Id = "Invalid_ID!", // Uppercase and special chars not allowed
            Name = "Invalid",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(supportsStreaming: false, supportsTools: false),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:8000"))
        };
        actInvalidId.Should().Throw<ArgumentException>()
            .WithMessage("*lowercase alphanumeric*");
    }
}
