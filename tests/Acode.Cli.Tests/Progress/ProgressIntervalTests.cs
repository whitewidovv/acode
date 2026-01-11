// <copyright file="ProgressIntervalTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Acode.Cli.Progress;
using FluentAssertions;

namespace Acode.Cli.Tests.Progress;

/// <summary>
/// Unit tests for <see cref="ProgressInterval"/>.
/// </summary>
public sealed class ProgressIntervalTests
{
    /// <summary>
    /// FR-049: Default progress interval: 10 seconds.
    /// </summary>
    [Fact]
    public void Should_Have_Default_Of_10_Seconds()
    {
        // Arrange & Act
        var interval = new ProgressInterval();

        // Assert
        interval.Interval.Should().Be(TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Should accept valid intervals.
    /// </summary>
    [Fact]
    public void Should_Accept_Valid_Interval()
    {
        // Arrange & Act
        var interval = new ProgressInterval(TimeSpan.FromSeconds(30));

        // Assert
        interval.Interval.Should().Be(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Should throw on too small interval.
    /// </summary>
    [Fact]
    public void Should_Throw_On_Too_Small_Interval()
    {
        // Act
        var act = () => new ProgressInterval(TimeSpan.FromMilliseconds(100));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Should throw on too large interval.
    /// </summary>
    [Fact]
    public void Should_Throw_On_Too_Large_Interval()
    {
        // Act
        var act = () => new ProgressInterval(TimeSpan.FromMinutes(10));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Should create from seconds.
    /// </summary>
    [Fact]
    public void FromSeconds_Should_Create_Correct_Interval()
    {
        // Act
        var interval = ProgressInterval.FromSeconds(30);

        // Assert
        interval.Interval.Should().Be(TimeSpan.FromSeconds(30));
    }
}
