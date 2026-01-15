namespace Acode.Domain.Providers.Ollama;

/// <summary>
/// Represents how Ollama service lifecycle is managed.
/// </summary>
/// <remarks>
/// Three modes define who controls the Ollama process lifecycle:
/// - Managed: Acode fully controls startup, health checks, restart, and shutdown
/// - Monitored: External service manager (e.g., systemd) controls; Acode monitors only
/// - External: Assumes Ollama is always running; minimal overhead checks only
///
/// Task 005d Functional Requirements: FR-006 to FR-009.
/// </remarks>
public enum OllamaLifecycleMode
{
    /// <summary>
    /// Acode fully controls the Ollama process lifecycle (default, recommended for most users).
    /// Acode will:
    /// - Automatically start Ollama when needed
    /// - Perform periodic health checks
    /// - Automatically restart if crash is detected
    /// - Gracefully shut down on application exit (if configured)
    /// Recommended for: Development, single-purpose machines, users wanting "just works" experience.
    /// </summary>
    Managed = 0,

    /// <summary>
    /// External service manager (e.g., systemd, Docker, Kubernetes) controls the process.
    /// Acode will:
    /// - Monitor health status via periodic checks
    /// - NOT attempt to start Ollama
    /// - NOT attempt to restart on failure
    /// - Alert user if service becomes unavailable
    /// Recommended for: Production environments, multi-service setups, when you want external orchestration.
    /// </summary>
    Monitored = 1,

    /// <summary>
    /// Assumes Ollama is already running and always available.
    /// Acode will:
    /// - Skip health checks to reduce overhead
    /// - NOT attempt to start or restart
    /// - Not verify service state at startup
    /// Use only if: You guarantee Ollama is always running and responsive.
    /// Recommended for: High-performance scenarios, embedded/airgapped systems.
    /// </summary>
    External = 2
}
