// <copyright file="ModeDetector.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Detects whether the CLI is running in interactive or non-interactive mode.
/// </summary>
/// <remarks>
/// FR-001 through FR-008: Mode detection implementation.
/// </remarks>
public sealed class ModeDetector : IModeDetector
{
    private readonly IConsoleWrapper _consoleWrapper;
    private readonly IEnvironmentProvider _environmentProvider;
    private readonly CIEnvironmentDetector _ciDetector;
    private readonly NonInteractiveOptions? _options;
    private readonly ILogger<ModeDetector>? _logger;

    private bool _isInitialized;
    private bool _isInteractive;
    private bool _isTTY;
    private CIEnvironment? _detectedCIEnvironment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModeDetector"/> class.
    /// </summary>
    /// <param name="consoleWrapper">The console wrapper for TTY detection.</param>
    /// <param name="environmentProvider">The environment provider for environment variable access.</param>
    /// <param name="options">Optional non-interactive options from command line.</param>
    /// <param name="logger">Optional logger for logging mode detection.</param>
    public ModeDetector(
        IConsoleWrapper consoleWrapper,
        IEnvironmentProvider environmentProvider,
        NonInteractiveOptions? options = null,
        ILogger<ModeDetector>? logger = null
    )
    {
        ArgumentNullException.ThrowIfNull(consoleWrapper);
        ArgumentNullException.ThrowIfNull(environmentProvider);

        _consoleWrapper = consoleWrapper;
        _environmentProvider = environmentProvider;
        _ciDetector = new CIEnvironmentDetector(environmentProvider);
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool IsInteractive
    {
        get
        {
            ThrowIfNotInitialized();
            return _isInteractive;
        }
    }

    /// <inheritdoc/>
    public bool IsTTY
    {
        get
        {
            ThrowIfNotInitialized();
            return _isTTY;
        }
    }

    /// <inheritdoc/>
    public CIEnvironment? DetectedCIEnvironment
    {
        get
        {
            ThrowIfNotInitialized();
            return _detectedCIEnvironment;
        }
    }

    /// <inheritdoc/>
    public bool IsInitialized => _isInitialized;

    /// <inheritdoc/>
    public void Initialize()
    {
        // FR-007: Mode MUST NOT change during execution
        if (_isInitialized)
        {
            return;
        }

        // Detect CI environment first
        _detectedCIEnvironment = _ciDetector.Detect();

        // FR-001: MUST detect non-interactive when stdin is not TTY
        // FR-002: MUST detect non-interactive when stdout is not TTY
        _isTTY = !_consoleWrapper.IsInputRedirected && !_consoleWrapper.IsOutputRedirected;

        // Determine interactive mode based on precedence:
        // 1. Explicit flags take precedence
        // 2. Environment variables
        // 3. TTY detection
        _isInteractive = DetermineInteractiveMode();
        _isInitialized = true;

        // FR-008: Mode MUST be logged at startup
        LogModeDetection();
    }

    private bool DetermineInteractiveMode()
    {
        // FR-003: --non-interactive MUST force non-interactive mode
        if (_options?.NonInteractive == true)
        {
            return false;
        }

        // --yes implies non-interactive
        if (_options?.Yes == true)
        {
            return false;
        }

        // FR-004: ACODE_NON_INTERACTIVE=1 MUST force mode
        var nonInteractiveEnv = _environmentProvider.GetVariable("ACODE_NON_INTERACTIVE");
        if (string.Equals(nonInteractiveEnv, "1", StringComparison.Ordinal))
        {
            return false;
        }

        // FR-005: CI=true MUST trigger non-interactive mode
        if (_detectedCIEnvironment.HasValue)
        {
            return false;
        }

        // FR-001/FR-002: TTY detection
        if (!_isTTY)
        {
            return false;
        }

        // Default to interactive if no indicators of non-interactive mode
        return true;
    }

    private void LogModeDetection()
    {
        if (_logger == null)
        {
            return;
        }

        var modeText = _isInteractive ? "interactive" : "non-interactive";
        var ttyText = _isTTY ? "TTY" : "non-TTY";

        if (_detectedCIEnvironment.HasValue)
        {
            _logger.LogInformation(
                "Mode detected: {Mode} ({Tty}), CI environment: {CIEnvironment}",
                modeText,
                ttyText,
                _detectedCIEnvironment.Value
            );
        }
        else
        {
            _logger.LogInformation("Mode detected: {Mode} ({Tty})", modeText, ttyText);
        }
    }

    private void ThrowIfNotInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                "ModeDetector has not been initialized. Call Initialize() first."
            );
        }
    }
}
