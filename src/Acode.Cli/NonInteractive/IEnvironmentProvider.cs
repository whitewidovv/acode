// <copyright file="IEnvironmentProvider.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Abstraction for environment variable access to enable testing.
/// </summary>
public interface IEnvironmentProvider
{
    /// <summary>
    /// Gets the value of an environment variable.
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>The value of the environment variable, or null if not set.</returns>
    string? GetVariable(string name);
}
