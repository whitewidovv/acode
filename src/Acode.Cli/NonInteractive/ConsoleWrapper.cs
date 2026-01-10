// <copyright file="ConsoleWrapper.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Default implementation of <see cref="IConsoleWrapper"/> that wraps the system Console.
/// </summary>
public sealed class ConsoleWrapper : IConsoleWrapper
{
    /// <inheritdoc/>
    public bool IsInputRedirected => Console.IsInputRedirected;

    /// <inheritdoc/>
    public bool IsOutputRedirected => Console.IsOutputRedirected;

    /// <inheritdoc/>
    public bool IsErrorRedirected => Console.IsErrorRedirected;
}
