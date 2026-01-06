// src/Acode.Application/Database/LockInfo.cs
namespace Acode.Application.Database;

/// <summary>
/// Represents information about an acquired migration lock.
/// </summary>
/// <param name="LockId">The unique identifier for the lock.</param>
/// <param name="HolderId">The identifier of the process holding the lock.</param>
/// <param name="AcquiredAt">The timestamp when the lock was acquired.</param>
/// <param name="MachineName">The name of the machine holding the lock (optional).</param>
public sealed record LockInfo(
    string LockId,
    string HolderId,
    DateTime AcquiredAt,
    string? MachineName);
