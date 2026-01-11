// <copyright file="FileEvent.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.Events;

/// <summary>
/// Event for file operations.
/// </summary>
/// <remarks>
/// Emitted for read, write, delete, and other file operations.
/// Enables tracking file modifications and auditing.
/// FR-027: "file_event" for file operations.
/// </remarks>
public sealed record FileEvent : BaseEvent
{
    /// <summary>
    /// Gets the operation performed.
    /// </summary>
    /// <remarks>
    /// Examples: "read", "write", "delete", "create".
    /// </remarks>
    public required string Operation { get; init; }

    /// <summary>
    /// Gets the file path.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the result status.
    /// </summary>
    /// <remarks>
    /// Examples: "success", "failure", "skipped".
    /// </remarks>
    public required string Result { get; init; }

    /// <summary>
    /// Gets the diff information if available.
    /// </summary>
    public FileDiff? Diff { get; init; }

    /// <summary>
    /// Gets the operation duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long? FileSizeBytes { get; init; }
}
