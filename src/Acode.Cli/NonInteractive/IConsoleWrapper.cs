// <copyright file="IConsoleWrapper.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Abstraction for console operations to enable testing.
/// </summary>
public interface IConsoleWrapper
{
    /// <summary>
    /// Gets a value indicating whether standard input is redirected from a file or pipe.
    /// </summary>
    bool IsInputRedirected { get; }

    /// <summary>
    /// Gets a value indicating whether standard output is redirected to a file or pipe.
    /// </summary>
    bool IsOutputRedirected { get; }

    /// <summary>
    /// Gets a value indicating whether standard error is redirected to a file or pipe.
    /// </summary>
    bool IsErrorRedirected { get; }
}
