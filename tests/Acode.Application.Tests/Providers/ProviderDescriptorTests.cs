namespace Acode.Application.Tests.Providers;

using System;
using System.Collections.Generic;
using Acode.Application.Inference;
using Acode.Application.Providers;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for ProviderDescriptor record.
/// Gap #16 from task-004c completion checklist.
/// </summary>
public sealed class ProviderDescriptorTests
{
    [Fact]
    public void Should_Require_Id()
    {
        // Arrange & Act
        Action act = () => new ProviderDescriptor
        {
            Id = null!,
            Name = "Test Provider",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"))
        };

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*Id*");
    }

    [Fact]
    public void Should_Validate_Id_Not_Empty()
    {
        // Arrange & Act
        Action act = () => new ProviderDescriptor
        {
            Id = string.Empty,
            Name = "Test Provider",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"))
        };

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Id*");
    }

    [Fact]
    public void Should_Validate_Id_Format()
    {
        // Arrange - Id must be lowercase alphanumeric + hyphens
        Action actWithInvalidChars = () => new ProviderDescriptor
        {
            Id = "Invalid_Provider",
            Name = "Test Provider",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"))
        };

        Action actWithUppercase = () => new ProviderDescriptor
        {
            Id = "InvalidProvider",
            Name = "Test Provider",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"))
        };

        // Assert
        actWithInvalidChars.Should().Throw<ArgumentException>()
            .WithMessage("*lowercase alphanumeric*");

        actWithUppercase.Should().Throw<ArgumentException>()
            .WithMessage("*lowercase alphanumeric*");
    }

    [Fact]
    public void Should_Accept_Valid_Id_Format()
    {
        // Arrange & Act
        var descriptor = new ProviderDescriptor
        {
            Id = "ollama-local",
            Name = "Ollama Local",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"))
        };

        // Assert
        descriptor.Id.Should().Be("ollama-local");
    }

    [Fact]
    public void Should_Be_Immutable()
    {
        // Arrange
        var descriptor = new ProviderDescriptor
        {
            Id = "test-provider",
            Name = "Test Provider",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"))
        };

        // Act - use 'with' to create a modified copy
        var modified = descriptor with { Id = "modified-provider" };

        // Assert - original should be unchanged (immutable)
        descriptor.Id.Should().Be("test-provider");
        modified.Id.Should().Be("modified-provider");
        descriptor.Should().NotBeSameAs(modified);
    }

    [Fact]
    public void Should_Support_All_Properties()
    {
        // Arrange
        var capabilities = new ProviderCapabilities(
            supportsStreaming: true,
            supportsTools: true,
            supportsSystemMessages: true,
            maxContextLength: 128000);

        var endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"));

        var config = new ProviderConfig
        {
            DefaultModel = "llama3",
            EnableHealthChecks = true
        };

        var retryPolicy = new RetryPolicy
        {
            MaxAttempts = 3
        };

        var modelMappings = new Dictionary<string, string>
        {
            ["gpt-4"] = "llama3"
        };

        // Act
        var descriptor = new ProviderDescriptor
        {
            Id = "ollama",
            Name = "Ollama Local",
            Type = ProviderType.Ollama,
            Capabilities = capabilities,
            Endpoint = endpoint,
            Config = config,
            RetryPolicy = retryPolicy,
            FallbackProviderId = "vllm",
            ModelMappings = modelMappings
        };

        // Assert
        descriptor.Id.Should().Be("ollama");
        descriptor.Name.Should().Be("Ollama Local");
        descriptor.Type.Should().Be(ProviderType.Ollama);
        descriptor.Capabilities.Should().BeSameAs(capabilities);
        descriptor.Endpoint.Should().BeSameAs(endpoint);
        descriptor.Config.Should().BeSameAs(config);
        descriptor.RetryPolicy.Should().BeSameAs(retryPolicy);
        descriptor.FallbackProviderId.Should().Be("vllm");
        descriptor.ModelMappings.Should().BeSameAs(modelMappings);
    }

    [Fact]
    public void Should_Allow_Null_Optional_Properties()
    {
        // Arrange & Act
        var descriptor = new ProviderDescriptor
        {
            Id = "minimal",
            Name = "Minimal Provider",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:11434")),
            Config = null,
            RetryPolicy = null,
            FallbackProviderId = null,
            ModelMappings = null
        };

        // Assert
        descriptor.Config.Should().BeNull();
        descriptor.RetryPolicy.Should().BeNull();
        descriptor.FallbackProviderId.Should().BeNull();
        descriptor.ModelMappings.Should().BeNull();
    }

    [Fact]
    public void Should_Require_Name()
    {
        // Arrange & Act
        Action act = () => new ProviderDescriptor
        {
            Id = "test",
            Name = null!,
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(),
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"))
        };

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*Name*");
    }

    [Fact]
    public void Should_Require_Capabilities()
    {
        // Arrange & Act
        Action act = () => new ProviderDescriptor
        {
            Id = "test",
            Name = "Test",
            Type = ProviderType.Ollama,
            Capabilities = null!,
            Endpoint = new ProviderEndpoint(new Uri("http://localhost:11434"))
        };

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*Capabilities*");
    }

    [Fact]
    public void Should_Require_Endpoint()
    {
        // Arrange & Act
        Action act = () => new ProviderDescriptor
        {
            Id = "test",
            Name = "Test",
            Type = ProviderType.Ollama,
            Capabilities = new ProviderCapabilities(),
            Endpoint = null!
        };

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*Endpoint*");
    }
}
