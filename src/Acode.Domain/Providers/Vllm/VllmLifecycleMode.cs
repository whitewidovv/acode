namespace Acode.Domain.Providers.Vllm;

/// <summary>
/// Defines the lifecycle management mode for vLLM services.
/// Determines whether Acode controls, monitors, or assumes external management.
/// </summary>
public enum VllmLifecycleMode
{
    /// <summary>
    /// Managed mode: Acode fully controls vLLM lifecycle.
    /// Acode starts vLLM process if not running.
    /// Acode stops vLLM on application exit (if StopOnExit configured).
    /// Acode monitors health and auto-restarts on crashes.
    /// Default mode for typical development/testing.
    /// Simplest user experience (no external setup required).
    /// </summary>
    Managed = 0,

    /// <summary>
    /// Monitored mode: External service manager (e.g., systemd) controls lifecycle.
    /// Acode does NOT start/stop vLLM.
    /// Acode monitors health and reports issues (but doesn't restart).
    /// SystemD/Docker/Kubernetes manages process lifecycle.
    /// User responsible for starting vLLM before Acode.
    /// Suitable for production deployments with container orchestration.
    /// </summary>
    Monitored = 1,

    /// <summary>
    /// External mode: Assumes vLLM always running, minimal management.
    /// Acode does NOT start/stop vLLM.
    /// Acode does NOT monitor health (assume always healthy).
    /// Minimal overhead (just use API).
    /// User fully responsible for vLLM lifecycle.
    /// Fastest startup, minimal resource usage.
    /// Suitable when vLLM managed by separate system.
    /// </summary>
    External = 2
}
