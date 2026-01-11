// <copyright file="PreflightCheckerTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Acode.Cli.NonInteractive;
using FluentAssertions;
using NSubstitute;

namespace Acode.Cli.Tests.NonInteractive;

/// <summary>
/// Unit tests for <see cref="PreflightChecker"/>.
/// </summary>
public sealed class PreflightCheckerTests
{
    /// <summary>
    /// FR-071: MUST verify required config before start.
    /// </summary>
    [Fact]
    public async Task Should_Run_All_Registered_Checks()
    {
        // Arrange
        var checker = new PreflightChecker();

        var check1 = Substitute.For<IPreflightCheck>();
        check1.Name.Returns("config-check");
        check1
            .RunAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new PreflightCheckResult("config-check", true, "Config valid"))
            );

        var check2 = Substitute.For<IPreflightCheck>();
        check2.Name.Returns("model-check");
        check2
            .RunAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new PreflightCheckResult("model-check", true, "Model available"))
            );

        checker.AddCheck(check1);
        checker.AddCheck(check2);

        // Act
        var result = await checker.RunAllChecksAsync();

        // Assert
        result.AllPassed.Should().BeTrue();
        result.AllResults.Should().HaveCount(2);
        await check1.Received(1).RunAsync(Arg.Any<CancellationToken>());
        await check2.Received(1).RunAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// FR-074: Pre-flight failures MUST exit code 13.
    /// FR-075: Pre-flight MUST list all failures at once.
    /// </summary>
    [Fact]
    public async Task Should_Collect_All_Failures()
    {
        // Arrange
        var checker = new PreflightChecker();

        var check1 = Substitute.For<IPreflightCheck>();
        check1.Name.Returns("config-check");
        check1
            .RunAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new PreflightCheckResult("config-check", false, "Config invalid"))
            );

        var check2 = Substitute.For<IPreflightCheck>();
        check2.Name.Returns("model-check");
        check2
            .RunAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new PreflightCheckResult("model-check", false, "Model unavailable"))
            );

        var check3 = Substitute.For<IPreflightCheck>();
        check3.Name.Returns("permission-check");
        check3
            .RunAsync(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new PreflightCheckResult("permission-check", true, "Permissions OK")
                )
            );

        checker.AddCheck(check1);
        checker.AddCheck(check2);
        checker.AddCheck(check3);

        // Act
        var result = await checker.RunAllChecksAsync();

        // Assert
        result.AllPassed.Should().BeFalse();
        result.Failures.Should().HaveCount(2);
        result.AllResults.Should().HaveCount(3);
    }

    /// <summary>
    /// FR-076: --skip-preflight MUST bypass checks.
    /// </summary>
    [Fact]
    public async Task Should_Return_Empty_Result_When_No_Checks()
    {
        // Arrange
        var checker = new PreflightChecker();

        // Act
        var result = await checker.RunAllChecksAsync();

        // Assert
        result.AllResults.Should().BeEmpty();
        result.AllPassed.Should().BeFalse("AllPassed returns false when no checks run");
    }

    /// <summary>
    /// Should handle check exceptions gracefully.
    /// </summary>
    [Fact]
    public async Task Should_Handle_Check_Exceptions()
    {
        // Arrange
        var checker = new PreflightChecker();

        var failingCheck = Substitute.For<IPreflightCheck>();
        failingCheck.Name.Returns("failing-check");
        failingCheck
            .RunAsync(Arg.Any<CancellationToken>())
            .Returns<Task<PreflightCheckResult>>(_ =>
                throw new InvalidOperationException("Check failed unexpectedly")
            );

        checker.AddCheck(failingCheck);

        // Act
        var result = await checker.RunAllChecksAsync();

        // Assert
        result.AllPassed.Should().BeFalse();
        result.Failures.Should().HaveCount(1);
        result.Failures[0].Message.Should().Contain("Check threw exception");
    }

    /// <summary>
    /// Should respect cancellation token.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task Should_Respect_Cancellation_Token()
    {
        // Arrange
        var checker = new PreflightChecker();

        var slowCheck = Substitute.For<IPreflightCheck>();
        slowCheck.Name.Returns("slow-check");
        slowCheck
            .RunAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(5000, callInfo.Arg<CancellationToken>());
                return new PreflightCheckResult("slow-check", true, "Done");
            });

        checker.AddCheck(slowCheck);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act
        var act = async () => await checker.RunAllChecksAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Should throw on null check.
    /// </summary>
    [Fact]
    public void Should_Throw_On_Null_Check()
    {
        // Arrange
        var checker = new PreflightChecker();

        // Act
        var act = () => checker.AddCheck(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("check");
    }
}
