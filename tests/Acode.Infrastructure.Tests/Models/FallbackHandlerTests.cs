using Acode.Application.Models;
using Acode.Domain.Models.Routing;
using Acode.Infrastructure.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Acode.Infrastructure.Tests.Models;

/// <summary>
/// Tests for <see cref="FallbackHandler"/>.
/// </summary>
public class FallbackHandlerTests
{
    private readonly ILogger<FallbackHandler> _logger;
    private readonly IModelAvailabilityChecker _availabilityChecker;

    public FallbackHandlerTests()
    {
        _logger = Substitute.For<ILogger<FallbackHandler>>();
        _availabilityChecker = Substitute.For<IModelAvailabilityChecker>();
    }

    [Fact]
    public void TryFallback_WithAvailablePrimaryModel_ReturnsPrimaryWithoutFallback()
    {
        // Arrange
        var handler = new FallbackHandler(_logger, _availabilityChecker);
        var config = new RoutingConfig
        {
            DefaultModel = "llama3.2:7b",
            FallbackChain = new List<string> { "llama3.2:70b", "mistral:7b" },
        };

        _availabilityChecker.IsModelAvailable("llama3.2:7b").Returns(true);

        // Act
        var result = handler.TryFallback("llama3.2:7b", config, out var fallbackModel);

        // Assert
        result.Should().BeFalse(); // No fallback needed
        fallbackModel.Should().BeNull();
    }

    [Fact]
    public void TryFallback_WithUnavailablePrimary_TriesFallbackChainSequentially()
    {
        // Arrange
        var handler = new FallbackHandler(_logger, _availabilityChecker);
        var config = new RoutingConfig
        {
            DefaultModel = "llama3.2:7b",
            FallbackChain = new List<string> { "llama3.2:70b", "mistral:7b", "qwen2.5:14b" },
        };

        _availabilityChecker.IsModelAvailable("llama3.2:7b").Returns(false);
        _availabilityChecker.IsModelAvailable("llama3.2:70b").Returns(false);
        _availabilityChecker.IsModelAvailable("mistral:7b").Returns(true);

        // Act
        var result = handler.TryFallback("llama3.2:7b", config, out var fallbackModel);

        // Assert
        result.Should().BeTrue();
        fallbackModel.Should().Be("mistral:7b");

        // Verify sequential checking
        Received.InOrder(() =>
        {
            _availabilityChecker.IsModelAvailable("llama3.2:7b");
            _availabilityChecker.IsModelAvailable("llama3.2:70b");
            _availabilityChecker.IsModelAvailable("mistral:7b");
        });
    }

    [Fact]
    public void TryFallback_WithNullFallbackChain_ReturnsFalse()
    {
        // Arrange
        var handler = new FallbackHandler(_logger, _availabilityChecker);
        var config = new RoutingConfig
        {
            DefaultModel = "llama3.2:7b",
            FallbackChain = null,
        };

        _availabilityChecker.IsModelAvailable("llama3.2:7b").Returns(false);

        // Act
        var result = handler.TryFallback("llama3.2:7b", config, out var fallbackModel);

        // Assert
        result.Should().BeFalse();
        fallbackModel.Should().BeNull();
    }

    [Fact]
    public void TryFallback_WithEmptyFallbackChain_ReturnsFalse()
    {
        // Arrange
        var handler = new FallbackHandler(_logger, _availabilityChecker);
        var config = new RoutingConfig
        {
            DefaultModel = "llama3.2:7b",
            FallbackChain = new List<string>(),
        };

        _availabilityChecker.IsModelAvailable("llama3.2:7b").Returns(false);

        // Act
        var result = handler.TryFallback("llama3.2:7b", config, out var fallbackModel);

        // Assert
        result.Should().BeFalse();
        fallbackModel.Should().BeNull();
    }

    [Fact]
    public void TryFallback_WhenAllModelsUnavailable_ReturnsFalse()
    {
        // Arrange
        var handler = new FallbackHandler(_logger, _availabilityChecker);
        var config = new RoutingConfig
        {
            DefaultModel = "llama3.2:7b",
            FallbackChain = new List<string> { "llama3.2:70b", "mistral:7b" },
        };

        _availabilityChecker.IsModelAvailable(Arg.Any<string>()).Returns(false);

        // Act
        var result = handler.TryFallback("llama3.2:7b", config, out var fallbackModel);

        // Assert
        result.Should().BeFalse();
        fallbackModel.Should().BeNull();
    }

    [Fact]
    public void TryFallback_LogsEachFallbackAttempt()
    {
        // Arrange
        var handler = new FallbackHandler(_logger, _availabilityChecker);
        var config = new RoutingConfig
        {
            DefaultModel = "llama3.2:7b",
            FallbackChain = new List<string> { "llama3.2:70b", "mistral:7b" },
        };

        _availabilityChecker.IsModelAvailable("llama3.2:7b").Returns(false);
        _availabilityChecker.IsModelAvailable("llama3.2:70b").Returns(false);
        _availabilityChecker.IsModelAvailable("mistral:7b").Returns(true);

        // Act
        var result = handler.TryFallback("llama3.2:7b", config, out var fallbackModel);

        // Assert - Should log attempts for llama3.2:70b and mistral:7b
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("llama3.2:70b") && o.ToString()!.Contains("fallback")),
            null,
            Arg.Any<Func<object, Exception?, string>>());

        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("mistral:7b")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void TryFallback_WhenAllUnavailable_LogsFailureWithSuggestions()
    {
        // Arrange
        var handler = new FallbackHandler(_logger, _availabilityChecker);
        var config = new RoutingConfig
        {
            DefaultModel = "llama3.2:7b",
            FallbackChain = new List<string> { "llama3.2:70b" },
        };

        _availabilityChecker.IsModelAvailable(Arg.Any<string>()).Returns(false);
        _availabilityChecker.ListAvailableModels().Returns(new List<string> { "qwen2.5:14b", "mistral:7b" });

        // Act
        var result = handler.TryFallback("llama3.2:7b", config, out var fallbackModel);

        // Assert
        result.Should().BeFalse();

        // Should log failure with available models
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("qwen2.5:14b") || o.ToString()!.Contains("mistral:7b")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new FallbackHandler(null!, _availabilityChecker);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullAvailabilityChecker_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new FallbackHandler(_logger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("availabilityChecker");
    }

    [Fact]
    public void TryFallback_WithNullPrimaryModel_ThrowsArgumentException()
    {
        // Arrange
        var handler = new FallbackHandler(_logger, _availabilityChecker);
        var config = new RoutingConfig { DefaultModel = "llama3.2:7b" };

        // Act
        var act = () => handler.TryFallback(null!, config, out var _);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryFallback_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = new FallbackHandler(_logger, _availabilityChecker);

        // Act
        var act = () => handler.TryFallback("llama3.2:7b", null!, out var _);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("config");
    }
}
