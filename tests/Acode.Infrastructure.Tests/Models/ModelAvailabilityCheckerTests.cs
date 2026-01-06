using Acode.Application.Inference;
using Acode.Infrastructure.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Acode.Infrastructure.Tests.Models;

/// <summary>
/// Tests for <see cref="ModelAvailabilityChecker"/>.
/// </summary>
public class ModelAvailabilityCheckerTests
{
    private readonly ILogger<ModelAvailabilityChecker> _logger;
    private readonly IProviderRegistry _providerRegistry;

    public ModelAvailabilityCheckerTests()
    {
        _logger = Substitute.For<ILogger<ModelAvailabilityChecker>>();
        _providerRegistry = Substitute.For<IProviderRegistry>();
    }

    [Fact]
    public void IsModelAvailable_WithAvailableModel_ReturnsTrue()
    {
        // Arrange
        var provider = Substitute.For<IModelProvider>();
        provider.GetSupportedModels().Returns(new[] { "llama3.2:7b", "llama3.2:70b" });

        _providerRegistry.GetAllProviders().Returns(new[] { provider });

        var checker = new ModelAvailabilityChecker(_logger, _providerRegistry);

        // Act
        var result = checker.IsModelAvailable("llama3.2:7b");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsModelAvailable_WithUnavailableModel_ReturnsFalse()
    {
        // Arrange
        var provider = Substitute.For<IModelProvider>();
        provider.GetSupportedModels().Returns(new[] { "llama3.2:7b" });

        _providerRegistry.GetAllProviders().Returns(new[] { provider });

        var checker = new ModelAvailabilityChecker(_logger, _providerRegistry);

        // Act
        var result = checker.IsModelAvailable("mistral:7b");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsModelAvailable_WithMultipleProviders_ChecksAll()
    {
        // Arrange
        var provider1 = Substitute.For<IModelProvider>();
        provider1.GetSupportedModels().Returns(new[] { "llama3.2:7b" });

        var provider2 = Substitute.For<IModelProvider>();
        provider2.GetSupportedModels().Returns(new[] { "mistral:7b", "qwen2.5:14b" });

        _providerRegistry.GetAllProviders().Returns(new[] { provider1, provider2 });

        var checker = new ModelAvailabilityChecker(_logger, _providerRegistry);

        // Act & Assert
        checker.IsModelAvailable("llama3.2:7b").Should().BeTrue();
        checker.IsModelAvailable("mistral:7b").Should().BeTrue();
        checker.IsModelAvailable("qwen2.5:14b").Should().BeTrue();
        checker.IsModelAvailable("gpt-4").Should().BeFalse();
    }

    [Fact]
    public void IsModelAvailable_WithNoProviders_ReturnsFalse()
    {
        // Arrange
        _providerRegistry.GetAllProviders().Returns(Array.Empty<IModelProvider>());

        var checker = new ModelAvailabilityChecker(_logger, _providerRegistry);

        // Act
        var result = checker.IsModelAvailable("llama3.2:7b");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsModelAvailable_CachesResults()
    {
        // Arrange
        var provider = Substitute.For<IModelProvider>();
        provider.GetSupportedModels().Returns(new[] { "llama3.2:7b" });

        _providerRegistry.GetAllProviders().Returns(new[] { provider });

        var checker = new ModelAvailabilityChecker(_logger, _providerRegistry);

        // Act - Call twice
        var result1 = checker.IsModelAvailable("llama3.2:7b");
        var result2 = checker.IsModelAvailable("llama3.2:7b");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();

        // Should only query providers once due to caching
        _providerRegistry.Received(1).GetAllProviders();
    }

    [Fact]
    public void IsModelAvailable_CacheExpires_QueriesProvidersAgain()
    {
        // Arrange
        var provider = Substitute.For<IModelProvider>();
        provider.GetSupportedModels().Returns(new[] { "llama3.2:7b" });

        _providerRegistry.GetAllProviders().Returns(new[] { provider });

        var checker = new ModelAvailabilityChecker(_logger, _providerRegistry, cacheTtlSeconds: 0);

        // Act - Small delay to ensure cache expires
        checker.IsModelAvailable("llama3.2:7b");
        System.Threading.Thread.Sleep(10);
        checker.IsModelAvailable("llama3.2:7b");

        // Assert - Should query providers twice since cache expired
        _providerRegistry.Received(2).GetAllProviders();
    }

    [Fact]
    public void ListAvailableModels_ReturnsDeduplicatedList()
    {
        // Arrange
        var provider1 = Substitute.For<IModelProvider>();
        provider1.GetSupportedModels().Returns(new[] { "llama3.2:7b", "mistral:7b" });

        var provider2 = Substitute.For<IModelProvider>();
        provider2.GetSupportedModels().Returns(new[] { "llama3.2:7b", "qwen2.5:14b" });

        _providerRegistry.GetAllProviders().Returns(new[] { provider1, provider2 });

        var checker = new ModelAvailabilityChecker(_logger, _providerRegistry);

        // Act
        var models = checker.ListAvailableModels();

        // Assert
        models.Should().HaveCount(3);
        models.Should().Contain("llama3.2:7b");
        models.Should().Contain("mistral:7b");
        models.Should().Contain("qwen2.5:14b");
    }

    [Fact]
    public void ListAvailableModels_WithNoProviders_ReturnsEmptyList()
    {
        // Arrange
        _providerRegistry.GetAllProviders().Returns(Array.Empty<IModelProvider>());

        var checker = new ModelAvailabilityChecker(_logger, _providerRegistry);

        // Act
        var models = checker.ListAvailableModels();

        // Assert
        models.Should().BeEmpty();
    }

    [Fact]
    public void ListAvailableModels_CachesResults()
    {
        // Arrange
        var provider = Substitute.For<IModelProvider>();
        provider.GetSupportedModels().Returns(new[] { "llama3.2:7b" });

        _providerRegistry.GetAllProviders().Returns(new[] { provider });

        var checker = new ModelAvailabilityChecker(_logger, _providerRegistry);

        // Act - Call twice
        var models1 = checker.ListAvailableModels();
        var models2 = checker.ListAvailableModels();

        // Assert
        models1.Should().Equal(models2);

        // Should only query providers once due to caching
        _providerRegistry.Received(1).GetAllProviders();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ModelAvailabilityChecker(null!, _providerRegistry);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullProviderRegistry_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ModelAvailabilityChecker(_logger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("providerRegistry");
    }

    [Fact]
    public void IsModelAvailable_WithNullModelId_ThrowsArgumentException()
    {
        // Arrange
        var checker = new ModelAvailabilityChecker(_logger, _providerRegistry);

        // Act
        var act = () => checker.IsModelAvailable(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsModelAvailable_WithEmptyModelId_ThrowsArgumentException()
    {
        // Arrange
        var checker = new ModelAvailabilityChecker(_logger, _providerRegistry);

        // Act
        var act = () => checker.IsModelAvailable(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
