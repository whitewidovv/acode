// <copyright file="CIEnvironmentDetectorTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Acode.Cli.NonInteractive;
using FluentAssertions;
using NSubstitute;

namespace Acode.Cli.Tests.NonInteractive;

/// <summary>
/// Unit tests for <see cref="CIEnvironmentDetector"/>.
/// </summary>
public sealed class CIEnvironmentDetectorTests
{
    /// <summary>
    /// FR-009: GitHub Actions MUST be detected (GITHUB_ACTIONS).
    /// </summary>
    [Fact]
    public void Should_Detect_GitHub_Actions()
    {
        // Arrange
        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        environmentProvider.GetVariable("GITHUB_ACTIONS").Returns("true");

        var detector = new CIEnvironmentDetector(environmentProvider);

        // Act
        var ciEnvironment = detector.Detect();

        // Assert
        ciEnvironment.Should().Be(CIEnvironment.GitHubActions);
    }

    /// <summary>
    /// FR-010: GitLab CI MUST be detected (GITLAB_CI).
    /// </summary>
    [Fact]
    public void Should_Detect_GitLab_CI()
    {
        // Arrange
        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        environmentProvider.GetVariable("GITLAB_CI").Returns("true");

        var detector = new CIEnvironmentDetector(environmentProvider);

        // Act
        var ciEnvironment = detector.Detect();

        // Assert
        ciEnvironment.Should().Be(CIEnvironment.GitLabCI);
    }

    /// <summary>
    /// FR-011: Azure DevOps MUST be detected (TF_BUILD).
    /// </summary>
    [Fact]
    public void Should_Detect_Azure_DevOps()
    {
        // Arrange
        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        environmentProvider.GetVariable("TF_BUILD").Returns("True");

        var detector = new CIEnvironmentDetector(environmentProvider);

        // Act
        var ciEnvironment = detector.Detect();

        // Assert
        ciEnvironment.Should().Be(CIEnvironment.AzureDevOps);
    }

    /// <summary>
    /// FR-012: Jenkins MUST be detected (JENKINS_URL).
    /// </summary>
    [Fact]
    public void Should_Detect_Jenkins()
    {
        // Arrange
        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        environmentProvider.GetVariable("JENKINS_URL").Returns("https://jenkins.example.com");

        var detector = new CIEnvironmentDetector(environmentProvider);

        // Act
        var ciEnvironment = detector.Detect();

        // Assert
        ciEnvironment.Should().Be(CIEnvironment.Jenkins);
    }

    /// <summary>
    /// FR-013: CircleCI MUST be detected (CIRCLECI).
    /// </summary>
    [Fact]
    public void Should_Detect_CircleCI()
    {
        // Arrange
        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        environmentProvider.GetVariable("CIRCLECI").Returns("true");

        var detector = new CIEnvironmentDetector(environmentProvider);

        // Act
        var ciEnvironment = detector.Detect();

        // Assert
        ciEnvironment.Should().Be(CIEnvironment.CircleCI);
    }

    /// <summary>
    /// FR-014: Travis CI MUST be detected (TRAVIS).
    /// </summary>
    [Fact]
    public void Should_Detect_Travis_CI()
    {
        // Arrange
        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        environmentProvider.GetVariable("TRAVIS").Returns("true");

        var detector = new CIEnvironmentDetector(environmentProvider);

        // Act
        var ciEnvironment = detector.Detect();

        // Assert
        ciEnvironment.Should().Be(CIEnvironment.TravisCI);
    }

    /// <summary>
    /// FR-015: Bitbucket MUST be detected (BITBUCKET_BUILD_NUMBER).
    /// </summary>
    [Fact]
    public void Should_Detect_Bitbucket()
    {
        // Arrange
        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        environmentProvider.GetVariable("BITBUCKET_BUILD_NUMBER").Returns("12345");

        var detector = new CIEnvironmentDetector(environmentProvider);

        // Act
        var ciEnvironment = detector.Detect();

        // Assert
        ciEnvironment.Should().Be(CIEnvironment.Bitbucket);
    }

    /// <summary>
    /// Generic CI detected via CI=true.
    /// </summary>
    [Fact]
    public void Should_Detect_Generic_CI()
    {
        // Arrange
        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        environmentProvider.GetVariable("CI").Returns("true");

        var detector = new CIEnvironmentDetector(environmentProvider);

        // Act
        var ciEnvironment = detector.Detect();

        // Assert
        ciEnvironment.Should().Be(CIEnvironment.Generic);
    }

    /// <summary>
    /// Returns null when no CI environment detected.
    /// </summary>
    [Fact]
    public void Should_Return_Null_When_No_CI_Environment_Detected()
    {
        // Arrange
        var environmentProvider = Substitute.For<IEnvironmentProvider>();

        var detector = new CIEnvironmentDetector(environmentProvider);

        // Act
        var ciEnvironment = detector.Detect();

        // Assert
        ciEnvironment.Should().BeNull("no CI environment variables set");
    }

    /// <summary>
    /// Specific platform takes precedence over generic CI.
    /// </summary>
    [Fact]
    public void Should_Prioritize_Specific_Platform_Over_Generic_CI()
    {
        // Arrange
        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        environmentProvider.GetVariable("CI").Returns("true");
        environmentProvider.GetVariable("GITHUB_ACTIONS").Returns("true");

        var detector = new CIEnvironmentDetector(environmentProvider);

        // Act
        var ciEnvironment = detector.Detect();

        // Assert
        ciEnvironment
            .Should()
            .Be(
                CIEnvironment.GitHubActions,
                "specific platform detection takes precedence over generic CI"
            );
    }

    /// <summary>
    /// Case-insensitive detection for "true" values.
    /// </summary>
    /// <param name="trueValue">The true value to test.</param>
    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    [InlineData("True")]
    public void Should_Detect_CI_CaseInsensitive(string trueValue)
    {
        // Arrange
        var environmentProvider = Substitute.For<IEnvironmentProvider>();
        environmentProvider.GetVariable("CI").Returns(trueValue);

        var detector = new CIEnvironmentDetector(environmentProvider);

        // Act
        var ciEnvironment = detector.Detect();

        // Assert
        ciEnvironment.Should().Be(CIEnvironment.Generic);
    }

    /// <summary>
    /// Constructor should validate arguments.
    /// </summary>
    [Fact]
    public void Should_Throw_On_Null_EnvironmentProvider()
    {
        // Act
        var act = () => new CIEnvironmentDetector(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("environmentProvider");
    }
}
