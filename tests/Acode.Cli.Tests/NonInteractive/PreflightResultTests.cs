// <copyright file="PreflightResultTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Acode.Cli.NonInteractive;
using FluentAssertions;

namespace Acode.Cli.Tests.NonInteractive;

/// <summary>
/// Unit tests for <see cref="PreflightResult"/>.
/// </summary>
public sealed class PreflightResultTests
{
    /// <summary>
    /// Should return false for AllPassed when no checks run.
    /// </summary>
    [Fact]
    public void AllPassed_Should_Return_False_When_Empty()
    {
        // Arrange
        var result = new PreflightResult();

        // Act & Assert
        result.AllPassed.Should().BeFalse();
    }

    /// <summary>
    /// Should return true for AllPassed when all checks pass.
    /// </summary>
    [Fact]
    public void AllPassed_Should_Return_True_When_All_Pass()
    {
        // Arrange
        var result = new PreflightResult();
        result.AddResult(new PreflightCheckResult("check1", true, "OK"));
        result.AddResult(new PreflightCheckResult("check2", true, "OK"));

        // Act & Assert
        result.AllPassed.Should().BeTrue();
    }

    /// <summary>
    /// Should return failure summary.
    /// </summary>
    [Fact]
    public void GetFailureSummary_Should_List_Failures()
    {
        // Arrange
        var result = new PreflightResult();
        result.AddResult(new PreflightCheckResult("check1", false, "Failed 1"));
        result.AddResult(new PreflightCheckResult("check2", false, "Failed 2"));

        // Act
        var summary = result.GetFailureSummary();

        // Assert
        summary.Should().Contain("check1");
        summary.Should().Contain("Failed 1");
        summary.Should().Contain("check2");
        summary.Should().Contain("Failed 2");
        summary.Should().Contain("2 issue(s)");
    }

    /// <summary>
    /// Should throw on null result.
    /// </summary>
    [Fact]
    public void AddResult_Should_Throw_On_Null()
    {
        // Arrange
        var result = new PreflightResult();

        // Act
        var act = () => result.AddResult(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("result");
    }
}
