// <copyright file="CIEnvironment.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Represents detected CI/CD environments.
/// </summary>
/// <remarks>
/// FR-009 through FR-016: CI/CD platform detection.
/// </remarks>
public enum CIEnvironment
{
    /// <summary>
    /// GitHub Actions CI/CD environment.
    /// Detected via GITHUB_ACTIONS=true.
    /// </summary>
    GitHubActions,

    /// <summary>
    /// GitLab CI/CD environment.
    /// Detected via GITLAB_CI=true.
    /// </summary>
    GitLabCI,

    /// <summary>
    /// Azure DevOps Pipelines environment.
    /// Detected via TF_BUILD=True.
    /// </summary>
    AzureDevOps,

    /// <summary>
    /// Jenkins CI/CD environment.
    /// Detected via JENKINS_URL being set.
    /// </summary>
    Jenkins,

    /// <summary>
    /// CircleCI environment.
    /// Detected via CIRCLECI=true.
    /// </summary>
    CircleCI,

    /// <summary>
    /// Travis CI environment.
    /// Detected via TRAVIS=true.
    /// </summary>
    TravisCI,

    /// <summary>
    /// Bitbucket Pipelines environment.
    /// Detected via BITBUCKET_BUILD_NUMBER being set.
    /// </summary>
    Bitbucket,

    /// <summary>
    /// Generic CI environment detected via CI=true.
    /// </summary>
    Generic,
}
