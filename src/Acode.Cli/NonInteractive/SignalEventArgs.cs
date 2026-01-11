// <copyright file="SignalEventArgs.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Event arguments for signal events.
/// </summary>
public sealed class SignalEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SignalEventArgs"/> class.
    /// </summary>
    /// <param name="signalType">The type of signal received.</param>
    public SignalEventArgs(string signalType)
    {
        ArgumentNullException.ThrowIfNull(signalType);
        SignalType = signalType;
    }

    /// <summary>
    /// Gets the type of signal received.
    /// </summary>
    public string SignalType { get; }
}
