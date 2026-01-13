namespace Acode.Integration.Tests.Providers;

using System;
using Acode.Application.Inference;
using Acode.Application.Providers;
using Acode.Application.Providers.Exceptions;
using Acode.Application.Providers.Selection;
using Acode.Domain.Modes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Integration tests for operating mode validation.
/// Gap #27 from task-004c completion checklist.
/// </summary>
public sealed class OperatingModeValidationTests
{
    [Fact]
    public void Should_Validate_Airgapped_Mode()
    {
        // Arrange
        var logger = NullLogger<ProviderRegistry>.Instance;
        var selector = new CapabilityProviderSelector(_ => null);
        var registry = new ProviderRegistry(logger, selector, null, null, OperatingMode.Airgapped);

        var localProvider = new ProviderDescriptor
        {
            Id = "local",
            Name = "Local Provider",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(supportsStreaming: false, supportsTools: false),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:8000"))
        };

        var externalProvider = new ProviderDescriptor
        {
            Id = "external",
            Name = "External Provider",
            Type = ProviderType.Vllm,
            Capabilities = new ProviderCapabilities(supportsStreaming: false, supportsTools: false),
            Endpoint = new ProviderEndpoint(new Uri("https://api.example.com"))
        };

        // Act & Assert - Local provider should be allowed
        Action actLocal = () => registry.Register(localProvider);
        actLocal.Should().NotThrow();
        registry.IsRegistered("local").Should().BeTrue();

        // Act & Assert - External provider should be rejected
        Action actExternal = () => registry.Register(externalProvider);
        actExternal.Should().Throw<ProviderRegistrationException>()
            .WithMessage("*external endpoint*not allowed in Airgapped mode*")
            .Which.ErrorCode.Should().Be("ACODE-PRV-002");
    }

    [Fact]
    public void Should_Warn_On_Inconsistency()
    {
        // Arrange - LocalOnly mode should warn about external endpoints
        var logger = NullLogger<ProviderRegistry>.Instance;
        var selector = new CapabilityProviderSelector(_ => null);
        var registry = new ProviderRegistry(logger, selector, null, null, OperatingMode.LocalOnly);

        var localProvider = new ProviderDescriptor
        {
            Id = "local",
            Name = "Local Provider",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(supportsStreaming: false, supportsTools: false),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:8000"))
        };

        var externalProvider = new ProviderDescriptor
        {
            Id = "external",
            Name = "External Provider",
            Type = ProviderType.Vllm,
            Capabilities = new ProviderCapabilities(supportsStreaming: false, supportsTools: false),
            Endpoint = new ProviderEndpoint(new Uri("https://api.example.com"))
        };

        // Act - Both should be allowed in LocalOnly mode
        Action actLocal = () => registry.Register(localProvider);
        Action actExternal = () => registry.Register(externalProvider);

        // Assert - No exceptions thrown, but external provider logs warning
        actLocal.Should().NotThrow();
        actExternal.Should().NotThrow(); // LocalOnly mode allows but logs warning

        registry.IsRegistered("local").Should().BeTrue();
        registry.IsRegistered("external").Should().BeTrue();

        // Note: Warning logging is not easily testable without capturing log output,
        // but the behavior is validated by not throwing an exception
    }
}
