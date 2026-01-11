namespace Acode.Infrastructure.Tests.Fallback;

using Acode.Application.Fallback;
using Acode.Infrastructure.Fallback;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="CircuitBreaker"/>.
/// Tests AC-031 to AC-038: Circuit breaker pattern.
/// </summary>
public sealed class CircuitBreakerTests
{
    /// <summary>
    /// Test that circuit starts in closed state.
    /// </summary>
    [Fact]
    public void Should_Start_In_Closed_State()
    {
        // Arrange & Act
        var breaker = new CircuitBreaker(5, TimeSpan.FromSeconds(60));

        // Assert
        breaker.State.Should().Be(CircuitState.Closed);
        breaker.FailureCount.Should().Be(0);
    }

    /// <summary>
    /// Test that recording failure increments count.
    /// </summary>
    [Fact]
    public void RecordFailure_Should_Increment_Failure_Count()
    {
        // Arrange
        var breaker = new CircuitBreaker(5, TimeSpan.FromSeconds(60));

        // Act
        breaker.RecordFailure();

        // Assert
        breaker.FailureCount.Should().Be(1);
        breaker.State.Should().Be(CircuitState.Closed);
    }

    /// <summary>
    /// Test that circuit opens after threshold failures.
    /// </summary>
    [Fact]
    public void RecordFailure_Should_Open_Circuit_After_Threshold()
    {
        // Arrange
        var threshold = 5;
        var breaker = new CircuitBreaker(threshold, TimeSpan.FromSeconds(60));

        // Act
        for (int i = 0; i < threshold; i++)
        {
            breaker.RecordFailure();
        }

        // Assert
        breaker.State.Should().Be(CircuitState.Open);
        breaker.FailureCount.Should().Be(threshold);
    }

    /// <summary>
    /// Test that success resets failure count and closes circuit.
    /// </summary>
    [Fact]
    public void RecordSuccess_Should_Close_Circuit_And_Reset_Count()
    {
        // Arrange
        var breaker = new CircuitBreaker(5, TimeSpan.FromSeconds(60));
        breaker.RecordFailure();
        breaker.RecordFailure();

        // Act
        breaker.RecordSuccess();

        // Assert
        breaker.State.Should().Be(CircuitState.Closed);
        breaker.FailureCount.Should().Be(0);
    }

    /// <summary>
    /// Test that closed circuit allows requests.
    /// </summary>
    [Fact]
    public void ShouldAllow_Should_Return_True_For_Closed_Circuit()
    {
        // Arrange
        var breaker = new CircuitBreaker(5, TimeSpan.FromSeconds(60));

        // Act & Assert
        breaker.ShouldAllow().Should().BeTrue();
    }

    /// <summary>
    /// Test that open circuit blocks requests.
    /// </summary>
    [Fact]
    public void ShouldAllow_Should_Return_False_For_Open_Circuit()
    {
        // Arrange
        var breaker = new CircuitBreaker(1, TimeSpan.FromMinutes(1));
        breaker.RecordFailure(); // Opens circuit

        // Act & Assert
        breaker.ShouldAllow().Should().BeFalse();
    }

    /// <summary>
    /// Test that manual reset closes circuit.
    /// </summary>
    [Fact]
    public void Reset_Should_Close_Circuit()
    {
        // Arrange
        var breaker = new CircuitBreaker(1, TimeSpan.FromMinutes(1));
        breaker.RecordFailure(); // Opens circuit

        // Act
        breaker.Reset();

        // Assert
        breaker.State.Should().Be(CircuitState.Closed);
        breaker.FailureCount.Should().Be(0);
    }

    /// <summary>
    /// Test that GetStateInfo returns correct state.
    /// </summary>
    [Fact]
    public void GetStateInfo_Should_Return_Correct_State()
    {
        // Arrange
        var breaker = new CircuitBreaker(5, TimeSpan.FromSeconds(60));
        breaker.RecordFailure();
        breaker.RecordFailure();

        // Act
        var info = breaker.GetStateInfo("llama3.2:7b");

        // Assert
        info.ModelId.Should().Be("llama3.2:7b");
        info.State.Should().Be(CircuitState.Closed);
        info.FailureCount.Should().Be(2);
        info.LastFailureTime.Should().NotBeNull();
    }

    /// <summary>
    /// Test that invalid threshold throws exception.
    /// </summary>
    /// <param name="threshold">The invalid threshold.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public void Constructor_Should_Throw_For_Invalid_Threshold(int threshold)
    {
        // Act
        var action = () => new CircuitBreaker(threshold, TimeSpan.FromSeconds(60));

        // Assert
        action
            .Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Threshold must be between 1 and 20*");
    }

    /// <summary>
    /// Test that invalid cooling period throws exception.
    /// </summary>
    [Fact]
    public void Constructor_Should_Throw_For_Invalid_CoolingPeriod()
    {
        // Act - too short
        var actionTooShort = () => new CircuitBreaker(5, TimeSpan.FromSeconds(4));

        // Assert
        actionTooShort
            .Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Cooling period must be between 5 seconds and 10 minutes*");

        // Act - too long
        var actionTooLong = () => new CircuitBreaker(5, TimeSpan.FromMinutes(11));

        // Assert
        actionTooLong.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Test that GetNextRetryTime returns null for closed circuit.
    /// </summary>
    [Fact]
    public void GetNextRetryTime_Should_Return_Null_For_Closed_Circuit()
    {
        // Arrange
        var breaker = new CircuitBreaker(5, TimeSpan.FromSeconds(60));

        // Act
        var nextRetry = breaker.GetNextRetryTime();

        // Assert
        nextRetry.Should().BeNull();
    }

    /// <summary>
    /// Test that GetNextRetryTime returns time for open circuit.
    /// </summary>
    [Fact]
    public void GetNextRetryTime_Should_Return_Time_For_Open_Circuit()
    {
        // Arrange
        var breaker = new CircuitBreaker(1, TimeSpan.FromSeconds(60));
        breaker.RecordFailure();

        // Act
        var nextRetry = breaker.GetNextRetryTime();

        // Assert
        nextRetry.Should().NotBeNull();
        nextRetry.Should().BeAfter(DateTimeOffset.UtcNow);
    }
}
