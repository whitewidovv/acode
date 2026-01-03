namespace Acode.Domain.Commands;

/// <summary>
/// Defines the six command groups for project lifecycle operations.
/// These groups enable Acode to interact with any repository's development workflow.
/// </summary>
/// <remarks>
/// Per Task 002.c FR-002c-01 and FR-002c-02.
/// All six groups are mandatory in the enum but optional in configuration.
/// </remarks>
public enum CommandGroup
{
    /// <summary>
    /// Setup commands initialize the development environment and install dependencies.
    /// Must be idempotent and safe to run multiple times.
    /// </summary>
    Setup = 0,

    /// <summary>
    /// Build commands compile or bundle the project and produce deployment artifacts.
    /// Should support incremental builds where possible.
    /// </summary>
    Build = 1,

    /// <summary>
    /// Test commands run the test suite.
    /// Must return non-zero on any test failure.
    /// </summary>
    Test = 2,

    /// <summary>
    /// Lint commands check code quality and style.
    /// Must not modify files and return non-zero on violations.
    /// </summary>
    Lint = 3,

    /// <summary>
    /// Format commands auto-format source code in place.
    /// Must be idempotent.
    /// </summary>
    Format = 4,

    /// <summary>
    /// Start commands run the application.
    /// Support long-running processes that are terminable via signal.
    /// </summary>
    Start = 5
}
