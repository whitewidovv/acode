// src/Acode.Infrastructure/Concurrency/LockData.cs
namespace Acode.Infrastructure.Concurrency;

using System;

/// <summary>
/// Lock data serialized to lock file.
/// Internal DTO - not part of public API.
/// </summary>
internal sealed record LockData(
    int ProcessId,
    DateTimeOffset LockedAt,
    string Hostname,
    string Terminal);
