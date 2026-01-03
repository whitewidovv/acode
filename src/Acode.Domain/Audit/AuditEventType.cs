namespace Acode.Domain.Audit;

/// <summary>
/// Types of audit events that can be logged.
/// All event types listed here are mandatory per FR-003c-21 through FR-003c-45.
/// </summary>
public enum AuditEventType
{
    /// <summary>
    /// Session started - FR-003c-21.
    /// Logged when Acode agent session begins.
    /// </summary>
    SessionStart,

    /// <summary>
    /// Session ended - FR-003c-22.
    /// Logged when Acode agent session terminates.
    /// </summary>
    SessionEnd,

    /// <summary>
    /// Configuration loaded - FR-003c-23.
    /// Logged when .agent/config.yml is successfully loaded.
    /// </summary>
    ConfigLoad,

    /// <summary>
    /// Configuration validation error - FR-003c-24.
    /// Logged when config parsing or validation fails.
    /// </summary>
    ConfigError,

    /// <summary>
    /// Operating mode selected - FR-003c-25.
    /// Logged when operating mode is determined (LocalOnly, Burst, Airgapped).
    /// </summary>
    ModeSelect,

    /// <summary>
    /// Command execution started - FR-003c-26.
    /// Logged when a user command begins execution.
    /// </summary>
    CommandStart,

    /// <summary>
    /// Command execution completed - FR-003c-27.
    /// Logged when a command finishes successfully.
    /// </summary>
    CommandEnd,

    /// <summary>
    /// Command execution failed - FR-003c-28.
    /// Logged when a command fails or errors.
    /// </summary>
    CommandError,

    /// <summary>
    /// File read operation - FR-003c-29.
    /// Logged when a file is read from disk.
    /// </summary>
    FileRead,

    /// <summary>
    /// File write operation - FR-003c-30.
    /// Logged when a file is written to disk.
    /// </summary>
    FileWrite,

    /// <summary>
    /// File delete operation - FR-003c-31.
    /// Logged when a file is deleted.
    /// </summary>
    FileDelete,

    /// <summary>
    /// Directory creation - FR-003c-32.
    /// Logged when a directory is created.
    /// </summary>
    DirCreate,

    /// <summary>
    /// Directory deletion - FR-003c-33.
    /// Logged when a directory is deleted.
    /// </summary>
    DirDelete,

    /// <summary>
    /// Protected path access attempt blocked - FR-003c-34.
    /// Logged when access to protected path (denylist) is denied.
    /// </summary>
    ProtectedPathBlocked,

    /// <summary>
    /// Security policy violation - FR-003c-35.
    /// Logged when a security control blocks an operation.
    /// </summary>
    SecurityViolation,

    /// <summary>
    /// Task execution started - FR-003c-36.
    /// Logged when an agent task begins.
    /// </summary>
    TaskStart,

    /// <summary>
    /// Task execution completed - FR-003c-37.
    /// Logged when a task completes successfully.
    /// </summary>
    TaskEnd,

    /// <summary>
    /// Task execution failed - FR-003c-38.
    /// Logged when a task fails or errors.
    /// </summary>
    TaskError,

    /// <summary>
    /// User approval requested - FR-003c-39.
    /// Logged when agent requests user consent for an operation.
    /// </summary>
    ApprovalRequest,

    /// <summary>
    /// User approval response received - FR-003c-40.
    /// Logged when user provides approval decision (approved/denied).
    /// </summary>
    ApprovalResponse,

    /// <summary>
    /// Code generation event - FR-003c-41.
    /// Logged when LLM generates code or file modifications.
    /// </summary>
    CodeGenerated,

    /// <summary>
    /// Test execution - FR-003c-42.
    /// Logged when tests are run.
    /// </summary>
    TestExecution,

    /// <summary>
    /// Build execution - FR-003c-43.
    /// Logged when build process is executed.
    /// </summary>
    BuildExecution,

    /// <summary>
    /// Error recovery attempt - FR-003c-44.
    /// Logged when agent attempts to recover from an error.
    /// </summary>
    ErrorRecovery,

    /// <summary>
    /// Graceful shutdown - FR-003c-45.
    /// Logged when agent performs graceful shutdown.
    /// </summary>
    Shutdown
}
