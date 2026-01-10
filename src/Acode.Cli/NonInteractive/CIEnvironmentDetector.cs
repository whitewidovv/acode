// <copyright file="CIEnvironmentDetector.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Detects the CI/CD environment based on environment variables.
/// </summary>
/// <remarks>
/// FR-009 through FR-016: CI/CD environment detection.
/// </remarks>
public sealed class CIEnvironmentDetector
{
    private readonly IEnvironmentProvider _environmentProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CIEnvironmentDetector"/> class.
    /// </summary>
    /// <param name="environmentProvider">The environment provider for accessing environment variables.</param>
    public CIEnvironmentDetector(IEnvironmentProvider environmentProvider)
    {
        ArgumentNullException.ThrowIfNull(environmentProvider);
        _environmentProvider = environmentProvider;
    }

    /// <summary>
    /// Detects the CI/CD environment from environment variables.
    /// </summary>
    /// <returns>The detected CI environment, or null if not running in CI.</returns>
    public CIEnvironment? Detect()
    {
        // FR-009: GitHub Actions MUST be detected (GITHUB_ACTIONS)
        if (IsEnvTrue("GITHUB_ACTIONS"))
        {
            return CIEnvironment.GitHubActions;
        }

        // FR-010: GitLab CI MUST be detected (GITLAB_CI)
        if (IsEnvTrue("GITLAB_CI"))
        {
            return CIEnvironment.GitLabCI;
        }

        // FR-011: Azure DevOps MUST be detected (TF_BUILD)
        // Note: Azure DevOps uses "True" with capital T
        if (
            string.Equals(
                _environmentProvider.GetVariable("TF_BUILD"),
                "True",
                StringComparison.Ordinal
            )
        )
        {
            return CIEnvironment.AzureDevOps;
        }

        // FR-012: Jenkins MUST be detected (JENKINS_URL)
        if (!string.IsNullOrEmpty(_environmentProvider.GetVariable("JENKINS_URL")))
        {
            return CIEnvironment.Jenkins;
        }

        // FR-013: CircleCI MUST be detected (CIRCLECI)
        if (IsEnvTrue("CIRCLECI"))
        {
            return CIEnvironment.CircleCI;
        }

        // FR-014: Travis CI MUST be detected (TRAVIS)
        if (IsEnvTrue("TRAVIS"))
        {
            return CIEnvironment.TravisCI;
        }

        // FR-015: Bitbucket MUST be detected (BITBUCKET_BUILD_NUMBER)
        if (!string.IsNullOrEmpty(_environmentProvider.GetVariable("BITBUCKET_BUILD_NUMBER")))
        {
            return CIEnvironment.Bitbucket;
        }

        // FR-005: CI=true MUST trigger non-interactive mode
        // Generic CI detection as fallback
        if (IsEnvTrue("CI") || IsEnvTrue("CONTINUOUS_INTEGRATION"))
        {
            return CIEnvironment.Generic;
        }

        return null;
    }

    private bool IsEnvTrue(string name)
    {
        var value = _environmentProvider.GetVariable(name);
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
