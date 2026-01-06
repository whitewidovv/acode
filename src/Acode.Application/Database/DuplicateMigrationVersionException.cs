// src/Acode.Application/Database/DuplicateMigrationVersionException.cs
namespace Acode.Application.Database;

/// <summary>
/// Exception thrown when duplicate migration versions are detected during discovery.
/// </summary>
public sealed class DuplicateMigrationVersionException : MigrationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateMigrationVersionException"/> class.
    /// </summary>
    /// <param name="version">The duplicate migration version.</param>
    public DuplicateMigrationVersionException(string version)
        : base("ACODE-MIG-009", $"Duplicate migration version detected: {version}")
    {
        Version = version;
    }

    /// <summary>
    /// Gets the duplicate migration version.
    /// </summary>
    public string Version { get; }
}
