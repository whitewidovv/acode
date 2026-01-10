namespace Acode.Application.Tests.Fallback;

using Acode.Application.Fallback;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="CircuitStateInfo"/>.
/// </summary>
public sealed class CircuitStateInfoTests
{
    /// <summary>
    /// Test that closed circuit allows requests.
    /// </summary>
    [Fact]
    public void IsAllowingRequests_Should_Return_True_For_Closed_Circuit()
    {
        // Arrange
        var info = new CircuitStateInfo
        {
            ModelId = "llama3.2:7b",
            State = CircuitState.Closed,
            FailureCount = 0,
        };

        // Act & Assert
        info.IsAllowingRequests.Should().BeTrue();
    }

    /// <summary>
    /// Test that open circuit blocks requests.
    /// </summary>
    [Fact]
    public void IsAllowingRequests_Should_Return_False_For_Open_Circuit()
    {
        // Arrange
        var info = new CircuitStateInfo
        {
            ModelId = "llama3.2:7b",
            State = CircuitState.Open,
            FailureCount = 5,
            LastFailureTime = DateTimeOffset.UtcNow,
            NextRetryTime = DateTimeOffset.UtcNow.AddMinutes(1),
        };

        // Act & Assert
        info.IsAllowingRequests.Should().BeFalse();
    }

    /// <summary>
    /// Test that half-open circuit allows requests.
    /// </summary>
    [Fact]
    public void IsAllowingRequests_Should_Return_True_For_HalfOpen_Circuit()
    {
        // Arrange
        var info = new CircuitStateInfo
        {
            ModelId = "llama3.2:7b",
            State = CircuitState.HalfOpen,
            FailureCount = 5,
        };

        // Act & Assert
        info.IsAllowingRequests.Should().BeTrue();
    }

    /// <summary>
    /// Test that all properties are set correctly.
    /// </summary>
    [Fact]
    public void Should_Have_All_Required_Properties()
    {
        // Arrange
        var modelId = "mistral:7b";
        var state = CircuitState.Open;
        var failureCount = 3;
        var lastFailureTime = DateTimeOffset.UtcNow;
        var nextRetryTime = DateTimeOffset.UtcNow.AddSeconds(60);

        // Act
        var info = new CircuitStateInfo
        {
            ModelId = modelId,
            State = state,
            FailureCount = failureCount,
            LastFailureTime = lastFailureTime,
            NextRetryTime = nextRetryTime,
        };

        // Assert
        info.ModelId.Should().Be(modelId);
        info.State.Should().Be(state);
        info.FailureCount.Should().Be(failureCount);
        info.LastFailureTime.Should().Be(lastFailureTime);
        info.NextRetryTime.Should().Be(nextRetryTime);
    }
}
