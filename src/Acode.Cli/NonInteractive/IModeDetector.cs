// <copyright file="IModeDetector.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Interface for detecting interactive vs non-interactive mode.
/// </summary>
/// <remarks>
/// FR-001 through FR-008: Mode detection requirements.
/// </remarks>
public interface IModeDetector
{
    /// <summary>
    /// Gets a value indicating whether the CLI is running in interactive mode.
    /// </summary>
    bool IsInteractive { get; }

    /// <summary>
    /// Gets a value indicating whether stdin and stdout are TTYs.
    /// </summary>
    bool IsTTY { get; }

    /// <summary>
    /// Gets the detected CI/CD environment, if any.
    /// </summary>
    CIEnvironment? DetectedCIEnvironment { get; }

    /// <summary>
    /// Gets a value indicating whether mode detection has been performed.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Initializes the mode detector by performing detection.
    /// </summary>
    /// <remarks>
    /// FR-006: Mode MUST be determined at startup.
    /// FR-007: Mode MUST NOT change during execution.
    /// FR-008: Mode MUST be logged at startup.
    /// </remarks>
    void Initialize();
}
