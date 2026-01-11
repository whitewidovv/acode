using Acode.Application.Commands;
using FluentAssertions;

namespace Acode.Application.Tests.Commands;

/// <summary>
/// Tests for TimeoutPolicy class.
/// Covers timeout logic per Task 002.c spec lines 1032, FR-002c-96 through FR-002c-102.
/// </summary>
public class TimeoutPolicyTests
{
    [Fact]
    public void DefaultTimeoutSeconds_ShouldBe300()
    {
        // Act
        var defaultTimeout = TimeoutPolicy.DefaultTimeoutSeconds;

        // Assert
        defaultTimeout.Should().Be(300, "per FR-002c-97: default timeout is 300 seconds (5 minutes)");
    }

    [Fact]
    public void NoTimeout_ShouldBeZero()
    {
        // Act
        var noTimeout = TimeoutPolicy.NoTimeout;

        // Assert
        noTimeout.Should().Be(0, "per FR-002c-100: timeout of 0 means no timeout");
    }

    [Fact]
    public void GetTimeout_WithPositiveValue_ReturnsTimeSpan()
    {
        // Arrange
        var timeoutSeconds = 120;

        // Act
        var timeout = TimeoutPolicy.GetTimeout(timeoutSeconds);

        // Assert
        timeout.Should().Be(TimeSpan.FromSeconds(120));
    }

    [Fact]
    public void GetTimeout_WithZero_ReturnsInfiniteTimeSpan()
    {
        // Arrange
        var timeoutSeconds = 0;

        // Act
        var timeout = TimeoutPolicy.GetTimeout(timeoutSeconds);

        // Assert
        timeout.Should().Be(Timeout.InfiniteTimeSpan, "zero means no timeout");
    }

    [Fact]
    public void GetTimeout_WithDefaultValue_Returns300Seconds()
    {
        // Arrange
        var timeoutSeconds = TimeoutPolicy.DefaultTimeoutSeconds;

        // Act
        var timeout = TimeoutPolicy.GetTimeout(timeoutSeconds);

        // Assert
        timeout.Should().Be(TimeSpan.FromSeconds(300));
    }

    [Fact]
    public void IsTimeout_WithPositiveValue_ReturnsTrue()
    {
        // Arrange
        var timeoutSeconds = 60;

        // Act
        var isTimeout = TimeoutPolicy.IsTimeout(timeoutSeconds);

        // Assert
        isTimeout.Should().BeTrue("positive timeout values indicate a timeout is set");
    }

    [Fact]
    public void IsTimeout_WithZero_ReturnsFalse()
    {
        // Arrange
        var timeoutSeconds = 0;

        // Act
        var isTimeout = TimeoutPolicy.IsTimeout(timeoutSeconds);

        // Assert
        isTimeout.Should().BeFalse("zero means no timeout");
    }

    [Fact]
    public void IsTimeout_WithNegativeValue_ReturnsFalse()
    {
        // Arrange
        var timeoutSeconds = -1;

        // Act
        var isTimeout = TimeoutPolicy.IsTimeout(timeoutSeconds);

        // Assert
        isTimeout.Should().BeFalse("negative values are invalid and treated as no timeout");
    }
}
