// <copyright file="EnvironmentProvider.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Default implementation of <see cref="IEnvironmentProvider"/> that wraps system environment.
/// </summary>
public sealed class EnvironmentProvider : IEnvironmentProvider
{
    /// <inheritdoc/>
    public string? GetVariable(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return Environment.GetEnvironmentVariable(name);
    }
}
