// <copyright file="ProgressInfoTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Acode.Cli.Progress;
using FluentAssertions;

namespace Acode.Cli.Tests.Progress;

/// <summary>
/// Unit tests for <see cref="ProgressInfo"/>.
/// </summary>
public sealed class ProgressInfoTests
{
    /// <summary>
    /// Should store all properties.
    /// </summary>
    [Fact]
    public void Should_Store_All_Properties()
    {
        // Arrange & Act
        var info = new ProgressInfo(
            PercentComplete: 50,
            Message: "Processing",
            CurrentStep: "Step 1",
            TotalSteps: 5,
            CurrentStepNumber: 1
        );

        // Assert
        info.PercentComplete.Should().Be(50);
        info.Message.Should().Be("Processing");
        info.CurrentStep.Should().Be("Step 1");
        info.TotalSteps.Should().Be(5);
        info.CurrentStepNumber.Should().Be(1);
    }

    /// <summary>
    /// Should allow optional properties.
    /// </summary>
    [Fact]
    public void Should_Allow_Minimal_Properties()
    {
        // Arrange & Act
        var info = new ProgressInfo(75, "Almost done");

        // Assert
        info.PercentComplete.Should().Be(75);
        info.Message.Should().Be("Almost done");
        info.CurrentStep.Should().BeNull();
        info.TotalSteps.Should().BeNull();
        info.CurrentStepNumber.Should().BeNull();
    }
}
