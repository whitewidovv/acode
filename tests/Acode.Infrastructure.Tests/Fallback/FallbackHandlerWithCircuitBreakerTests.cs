namespace Acode.Infrastructure.Tests.Fallback;

using Acode.Application.Fallback;
using Acode.Application.Inference;
using Acode.Application.Routing;
using Acode.Domain.Modes;
using Acode.Infrastructure.Fallback;
using Acode.Infrastructure.Routing;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="FallbackHandlerWithCircuitBreaker"/>.
/// Tests AC-008 to AC-013: FallbackHandler implementation.
/// </summary>
public sealed class FallbackHandlerWithCircuitBreakerTests
{
    private readonly ModelRegistry _mockRegistry;
    private readonly ILogger<FallbackHandlerWithCircuitBreaker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FallbackHandlerWithCircuitBreakerTests"/> class.
    /// </summary>
    public FallbackHandlerWithCircuitBreakerTests()
    {
        var mockProviders = Array.Empty<IModelProvider>();
        var registryLogger = NullLogger<ModelRegistry>.Instance;
        _mockRegistry = new ModelRegistry(mockProviders, registryLogger);
        _logger = NullLogger<FallbackHandlerWithCircuitBreaker>.Instance;
    }

    /// <summary>
    /// Test that null context throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void GetFallback_Should_Throw_For_Null_Context()
    {
        // Arrange
        var config = new FallbackConfiguration(new[] { "llama3.2:7b" });
        var handler = new FallbackHandlerWithCircuitBreaker(_mockRegistry, config, _logger);

        // Act
        var action = () => handler.GetFallback(AgentRole.Coder, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Test that empty fallback chain returns failure.
    /// </summary>
    [Fact]
    public void GetFallback_Should_Fail_When_No_Chain_Configured()
    {
        // Arrange
        var config = new FallbackConfiguration();
        var handler = new FallbackHandlerWithCircuitBreaker(_mockRegistry, config, _logger);
        var context = CreateContext("primary:model");

        // Act
        var result = handler.GetFallback(AgentRole.Coder, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Reason.Should().Contain("No fallback chain configured");
    }

    /// <summary>
    /// Test that circuit breaker opens after threshold.
    /// </summary>
    [Fact]
    public void NotifyFailure_Should_Open_Circuit_After_Threshold()
    {
        // Arrange
        var config = new FallbackConfiguration(new[] { "llama3.2:7b" }, failureThreshold: 3);
        var handler = new FallbackHandlerWithCircuitBreaker(_mockRegistry, config, _logger);
        var exception = new InvalidOperationException("Test error");

        // Act
        handler.NotifyFailure("llama3.2:7b", exception);
        handler.NotifyFailure("llama3.2:7b", exception);
        handler.NotifyFailure("llama3.2:7b", exception);

        // Assert
        handler.IsCircuitOpen("llama3.2:7b").Should().BeTrue();
    }

    /// <summary>
    /// Test that success closes circuit.
    /// </summary>
    [Fact]
    public void NotifySuccess_Should_Close_Circuit()
    {
        // Arrange
        var config = new FallbackConfiguration(new[] { "llama3.2:7b" }, failureThreshold: 1);
        var handler = new FallbackHandlerWithCircuitBreaker(_mockRegistry, config, _logger);
        handler.NotifyFailure("llama3.2:7b", new InvalidOperationException("Test"));

        // Act
        handler.NotifySuccess("llama3.2:7b");

        // Assert
        handler.IsCircuitOpen("llama3.2:7b").Should().BeFalse();
    }

    /// <summary>
    /// Test that reset closes specific circuit.
    /// </summary>
    [Fact]
    public void ResetCircuit_Should_Close_Specific_Circuit()
    {
        // Arrange
        var config = new FallbackConfiguration(new[] { "llama3.2:7b" }, failureThreshold: 1);
        var handler = new FallbackHandlerWithCircuitBreaker(_mockRegistry, config, _logger);
        handler.NotifyFailure("llama3.2:7b", new InvalidOperationException("Test"));

        // Act
        handler.ResetCircuit("llama3.2:7b");

        // Assert
        handler.IsCircuitOpen("llama3.2:7b").Should().BeFalse();
    }

    /// <summary>
    /// Test that reset all closes all circuits.
    /// </summary>
    [Fact]
    public void ResetAllCircuits_Should_Close_All_Circuits()
    {
        // Arrange
        var config = new FallbackConfiguration(
            new[] { "llama3.2:7b", "mistral:7b" },
            failureThreshold: 1
        );
        var handler = new FallbackHandlerWithCircuitBreaker(_mockRegistry, config, _logger);
        handler.NotifyFailure("llama3.2:7b", new InvalidOperationException("Test"));
        handler.NotifyFailure("mistral:7b", new InvalidOperationException("Test"));

        // Act
        handler.ResetAllCircuits();

        // Assert
        handler.IsCircuitOpen("llama3.2:7b").Should().BeFalse();
        handler.IsCircuitOpen("mistral:7b").Should().BeFalse();
    }

    /// <summary>
    /// Test that GetCircuitState returns correct state.
    /// </summary>
    [Fact]
    public void GetCircuitState_Should_Return_Correct_State()
    {
        // Arrange
        var config = new FallbackConfiguration(new[] { "llama3.2:7b" }, failureThreshold: 5);
        var handler = new FallbackHandlerWithCircuitBreaker(_mockRegistry, config, _logger);
        handler.NotifyFailure("llama3.2:7b", new InvalidOperationException("Test"));
        handler.NotifyFailure("llama3.2:7b", new InvalidOperationException("Test"));

        // Act
        var state = handler.GetCircuitState("llama3.2:7b");

        // Assert
        state.ModelId.Should().Be("llama3.2:7b");
        state.State.Should().Be(CircuitState.Closed);
        state.FailureCount.Should().Be(2);
    }

    /// <summary>
    /// Test that GetAllCircuitStates returns all states.
    /// </summary>
    [Fact]
    public void GetAllCircuitStates_Should_Return_All_States()
    {
        // Arrange
        var config = new FallbackConfiguration(
            new[] { "llama3.2:7b", "mistral:7b" },
            failureThreshold: 5
        );
        var handler = new FallbackHandlerWithCircuitBreaker(_mockRegistry, config, _logger);
        handler.NotifyFailure("llama3.2:7b", new InvalidOperationException("Test"));
        handler.NotifyFailure("mistral:7b", new InvalidOperationException("Test"));

        // Act
        var states = handler.GetAllCircuitStates();

        // Assert
        states.Should().HaveCount(2);
        states.Should().ContainKey("llama3.2:7b");
        states.Should().ContainKey("mistral:7b");
    }

    /// <summary>
    /// Test that null exception throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void NotifyFailure_Should_Throw_For_Null_Exception()
    {
        // Arrange
        var config = new FallbackConfiguration(new[] { "llama3.2:7b" });
        var handler = new FallbackHandlerWithCircuitBreaker(_mockRegistry, config, _logger);

        // Act
        var action = () => handler.NotifyFailure("llama3.2:7b", null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Test that empty model ID is handled gracefully.
    /// </summary>
    [Fact]
    public void NotifyFailure_Should_Handle_Empty_ModelId()
    {
        // Arrange
        var config = new FallbackConfiguration(new[] { "llama3.2:7b" });
        var handler = new FallbackHandlerWithCircuitBreaker(_mockRegistry, config, _logger);

        // Act - should not throw
        handler.NotifyFailure(string.Empty, new InvalidOperationException("Test"));
        handler.NotifySuccess(string.Empty);

        // Assert - no exception
        handler.IsCircuitOpen(string.Empty).Should().BeFalse();
    }

    /// <summary>
    /// Test that GetCircuitState returns default for unknown model.
    /// </summary>
    [Fact]
    public void GetCircuitState_Should_Return_Default_For_Empty_ModelId()
    {
        // Arrange
        var config = new FallbackConfiguration(new[] { "llama3.2:7b" });
        var handler = new FallbackHandlerWithCircuitBreaker(_mockRegistry, config, _logger);

        // Act
        var state = handler.GetCircuitState(string.Empty);

        // Assert
        state.State.Should().Be(CircuitState.Closed);
        state.FailureCount.Should().Be(0);
    }

    private static FallbackContext CreateContext(string originalModel)
    {
        return new FallbackContext
        {
            OriginalModel = originalModel,
            Trigger = EscalationTrigger.Unavailable,
            OperatingMode = OperatingMode.LocalOnly,
        };
    }
}
