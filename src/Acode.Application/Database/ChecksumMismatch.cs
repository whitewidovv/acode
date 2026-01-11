// src/Acode.Application/Database/ChecksumMismatch.cs
namespace Acode.Application.Database;

/// <summary>
/// Represents a checksum mismatch for an applied migration.
/// </summary>
/// <param name="Version">The migration version with the mismatch.</param>
/// <param name="ExpectedChecksum">The checksum stored in the database.</param>
/// <param name="ActualChecksum">The current file checksum.</param>
/// <param name="AppliedAt">The timestamp when the migration was applied.</param>
public sealed record ChecksumMismatch(
    string Version,
    string ExpectedChecksum,
    string ActualChecksum,
    DateTime AppliedAt);
