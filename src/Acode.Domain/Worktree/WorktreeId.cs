// src/Acode.Domain/Worktree/WorktreeId.cs
namespace Acode.Domain.Worktree;

using System;

/// <summary>
/// Strongly-typed identifier for Worktree entities using ULID format.
/// </summary>
public readonly record struct WorktreeId : IComparable<WorktreeId>
{
    private readonly string _value;

    private WorktreeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("WorktreeId cannot be empty", nameof(value));
        }

        if (value.Length != 26)
        {
            throw new ArgumentException("WorktreeId must be 26 characters (ULID format)", nameof(value));
        }

        _value = value;
    }

    /// <summary>
    /// Gets the ULID string value.
    /// </summary>
    public string Value => _value ?? throw new InvalidOperationException("WorktreeId not initialized");

    /// <summary>
    /// Implicit conversion to string for convenience.
    /// </summary>
    /// <param name="id">The WorktreeId to convert.</param>
    public static implicit operator string(WorktreeId id) => id.Value;

    /// <summary>
    /// Creates a WorktreeId from an existing ULID string.
    /// </summary>
    /// <param name="value">The 26-character ULID string.</param>
    /// <returns>A new WorktreeId instance.</returns>
    public static WorktreeId From(string value) => new(value);

    /// <summary>
    /// Compares this WorktreeId to another for ordering.
    /// </summary>
    /// <param name="other">The other WorktreeId to compare to.</param>
    /// <returns>A value indicating the relative order.</returns>
    public int CompareTo(WorktreeId other) => string.CompareOrdinal(_value, other._value);

    /// <summary>
    /// Returns the ULID string representation.
    /// </summary>
    /// <returns>The ULID string.</returns>
    public override string ToString() => _value;
}
