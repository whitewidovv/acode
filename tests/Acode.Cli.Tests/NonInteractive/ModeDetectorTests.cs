// <copyright file="ModeDetectorTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Acode.Cli.NonInteractive;
using FluentAssertions;
using NSubstitute;

namespace Acode.Cli.Tests.NonInteractive;

/// <summary>
/// Unit tests for <see cref="ModeDetector"/>.
/// </summary>
public sealed class ModeDetectorTests
{
    /// <summary>
    /// FR-001: MUST detect non-interactive when stdin is not TTY.
    /// </summary>
    [Fact]
    public void Should_Detect_NonTTY_Stdin()
    {
        // Arrange
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        consoleWrapper.IsInputRedirected.Returns(true); // stdin is pipe/file
        consoleWrapper.IsOutputRedirected.Returns(false);

        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        environmentProvider.GetVariable("CI").Returns((string?)null);

        var detector = new ModeDetector(consoleWrapper, environmentProvider);

        // Act
        detector.Initialize();
        var isInteractive = detector.IsInteractive;

        // Assert
        isInteractive.Should().BeFalse("stdin is redirected (not a TTY)");
        detector.IsTTY.Should().BeFalse();
    }

    /// <summary>
    /// FR-002: MUST detect non-interactive when stdout is not TTY.
    /// </summary>
    [Fact]
    public void Should_Detect_NonTTY_Stdout()
    {
        // Arrange
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        consoleWrapper.IsInputRedirected.Returns(false);
        consoleWrapper.IsOutputRedirected.Returns(true); // stdout is pipe/file

        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        environmentProvider.GetVariable("CI").Returns((string?)null);

        var detector = new ModeDetector(consoleWrapper, environmentProvider);

        // Act
        detector.Initialize();
        var isInteractive = detector.IsInteractive;

        // Assert
        isInteractive.Should().BeFalse("stdout is redirected (not a TTY)");
        detector.IsTTY.Should().BeFalse();
    }

    /// <summary>
    /// FR-005: CI=true MUST trigger non-interactive mode.
    /// </summary>
    [Fact]
    public void Should_Detect_CI_Variable()
    {
        // Arrange
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        consoleWrapper.IsInputRedirected.Returns(false);
        consoleWrapper.IsOutputRedirected.Returns(false);

        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        environmentProvider.GetVariable("CI").Returns("true");

        var detector = new ModeDetector(consoleWrapper, environmentProvider);

        // Act
        detector.Initialize();
        var isInteractive = detector.IsInteractive;

        // Assert
        isInteractive.Should().BeFalse("CI=true forces non-interactive mode");
        detector.DetectedCIEnvironment.Should().Be(CIEnvironment.Generic);
    }

    /// <summary>
    /// FR-003: --non-interactive MUST force non-interactive mode.
    /// </summary>
    [Fact]
    public void Should_Honor_NonInteractive_Flag()
    {
        // Arrange
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        consoleWrapper.IsInputRedirected.Returns(false);
        consoleWrapper.IsOutputRedirected.Returns(false);

        var environmentProvider = Substitute.For<IEnvironmentProvider>();

        var options = new NonInteractiveOptions { NonInteractive = true };
        var detector = new ModeDetector(consoleWrapper, environmentProvider, options);

        // Act
        detector.Initialize();
        var isInteractive = detector.IsInteractive;

        // Assert
        isInteractive.Should().BeFalse("--non-interactive flag forces mode");
    }

    /// <summary>
    /// --yes implies non-interactive mode.
    /// </summary>
    [Fact]
    public void Should_Honor_Yes_Flag()
    {
        // Arrange
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        consoleWrapper.IsInputRedirected.Returns(false);
        consoleWrapper.IsOutputRedirected.Returns(false);

        var environmentProvider = Substitute.For<IEnvironmentProvider>();

        var options = new NonInteractiveOptions { Yes = true };
        var detector = new ModeDetector(consoleWrapper, environmentProvider, options);

        // Act
        detector.Initialize();
        var isInteractive = detector.IsInteractive;

        // Assert
        isInteractive.Should().BeFalse("--yes flag implies non-interactive mode");
    }

    /// <summary>
    /// FR-004: ACODE_NON_INTERACTIVE=1 MUST force mode.
    /// </summary>
    [Fact]
    public void Should_Honor_EnvironmentVariable()
    {
        // Arrange
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        consoleWrapper.IsInputRedirected.Returns(false);
        consoleWrapper.IsOutputRedirected.Returns(false);

        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        environmentProvider.GetVariable("ACODE_NON_INTERACTIVE").Returns("1");

        var detector = new ModeDetector(consoleWrapper, environmentProvider);

        // Act
        detector.Initialize();
        var isInteractive = detector.IsInteractive;

        // Assert
        isInteractive.Should().BeFalse("ACODE_NON_INTERACTIVE=1 forces non-interactive mode");
    }

    /// <summary>
    /// Default to interactive when no indicators present.
    /// </summary>
    [Fact]
    public void Should_Default_To_Interactive_When_All_Indicators_Absent()
    {
        // Arrange
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        consoleWrapper.IsInputRedirected.Returns(false);
        consoleWrapper.IsOutputRedirected.Returns(false);

        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        environmentProvider.GetVariable("CI").Returns((string?)null);

        var detector = new ModeDetector(consoleWrapper, environmentProvider);

        // Act
        detector.Initialize();
        var isInteractive = detector.IsInteractive;

        // Assert
        isInteractive.Should().BeTrue("no indicators of non-interactive mode");
        detector.IsTTY.Should().BeTrue();
    }

    /// <summary>
    /// FR-006: Mode MUST be determined at startup.
    /// FR-007: Mode MUST NOT change during execution.
    /// </summary>
    [Fact]
    public void Should_Throw_If_Not_Initialized()
    {
        // Arrange
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        var detector = new ModeDetector(consoleWrapper, environmentProvider);

        // Act
        var act = () => _ = detector.IsInteractive;

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*not been initialized*");
    }

    /// <summary>
    /// Initialize should be idempotent.
    /// </summary>
    [Fact]
    public void Should_Allow_Multiple_Initialize_Calls()
    {
        // Arrange
        var consoleWrapper = Substitute.For<IConsoleWrapper>();
        consoleWrapper.IsInputRedirected.Returns(false);
        consoleWrapper.IsOutputRedirected.Returns(false);

        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        var detector = new ModeDetector(consoleWrapper, environmentProvider);

        // Act
        detector.Initialize();
        detector.Initialize();

        // Assert
        detector.IsInitialized.Should().BeTrue();
    }

    /// <summary>
    /// Constructor should validate arguments.
    /// </summary>
    [Fact]
    public void Should_Throw_On_Null_ConsoleWrapper()
    {
        // Arrange
        var environmentProvider = Substitute.For<IEnvironmentProvider>();

        // Act
        var act = () => new ModeDetector(null!, environmentProvider);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("consoleWrapper");
    }

    /// <summary>
    /// Constructor should validate arguments.
    /// </summary>
    [Fact]
    public void Should_Throw_On_Null_EnvironmentProvider()
    {
        // Arrange
        var consoleWrapper = Substitute.For<IConsoleWrapper>();

        // Act
        var act = () => new ModeDetector(consoleWrapper, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("environmentProvider");
    }
}
