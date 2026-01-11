// src/Acode.Domain/Conversation/RunId.cs
namespace Acode.Domain.Conversation;

using System;
using Acode.Domain.Common;

/// <summary>
/// Strongly-typed identifier for Run entities using ULID format.
/// </summary>
public readonly record struct RunId : IComparable<RunId>
{
    private readonly string _value;

    private RunId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("RunId cannot be empty", nameof(value));
        }

        if (value.Length != 26)
        {
            throw new ArgumentException("RunId must be 26 characters (ULID format)", nameof(value));
        }

        _value = value;
    }

    /// <summary>
    /// Gets the ULID string value.
    /// </summary>
    public string Value => _value ?? throw new InvalidOperationException("RunId not initialized");

    /// <summary>
    /// Gets an empty RunId (all zeros).
    /// </summary>
    public static RunId Empty => new("00000000000000000000000000");

    /// <summary>
    /// Implicit conversion to string for convenience.
    /// </summary>
    /// <param name="id">The RunId to convert.</param>
    public static implicit operator string(RunId id) => id.Value;

    /// <summary>
    /// Generates a new RunId with a generated ULID.
    /// </summary>
    /// <returns>A new RunId instance.</returns>
    public static RunId NewId() => new(Ulid.NewUlid());

    /// <summary>
    /// Creates a RunId from an existing ULID string.
    /// </summary>
    /// <param name="value">The 26-character ULID string.</param>
    /// <returns>A new RunId instance.</returns>
    public static RunId From(string value) => new(value);

    /// <summary>
    /// Attempts to parse a string as a RunId.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="runId">The parsed RunId if successful.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParse(string? value, out RunId runId)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 26)
        {
            runId = Empty;
            return false;
        }

        runId = new RunId(value);
        return true;
    }

    /// <summary>
    /// Compares this RunId to another for ordering.
    /// </summary>
    /// <param name="other">The other RunId to compare to.</param>
    /// <returns>A value indicating the relative order.</returns>
    public int CompareTo(RunId other) => string.CompareOrdinal(_value, other._value);

    /// <summary>
    /// Returns the ULID string representation.
    /// </summary>
    /// <returns>The ULID string.</returns>
    public override string ToString() => _value;
}
