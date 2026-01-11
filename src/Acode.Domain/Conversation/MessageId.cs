// src/Acode.Domain/Conversation/MessageId.cs
namespace Acode.Domain.Conversation;

using System;
using Acode.Domain.Common;

/// <summary>
/// Strongly-typed identifier for Message entities using ULID format.
/// </summary>
public readonly record struct MessageId : IComparable<MessageId>
{
    private readonly string _value;

    private MessageId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("MessageId cannot be empty", nameof(value));
        }

        if (value.Length != 26)
        {
            throw new ArgumentException("MessageId must be 26 characters (ULID format)", nameof(value));
        }

        _value = value;
    }

    /// <summary>
    /// Gets the ULID string value.
    /// </summary>
    public string Value => _value ?? throw new InvalidOperationException("MessageId not initialized");

    /// <summary>
    /// Gets an empty MessageId (all zeros).
    /// </summary>
    public static MessageId Empty => new("00000000000000000000000000");

    /// <summary>
    /// Implicit conversion to string for convenience.
    /// </summary>
    /// <param name="id">The MessageId to convert.</param>
    public static implicit operator string(MessageId id) => id.Value;

    /// <summary>
    /// Generates a new MessageId with a generated ULID.
    /// </summary>
    /// <returns>A new MessageId instance.</returns>
    public static MessageId NewId() => new(Ulid.NewUlid());

    /// <summary>
    /// Creates a MessageId from an existing ULID string.
    /// </summary>
    /// <param name="value">The 26-character ULID string.</param>
    /// <returns>A new MessageId instance.</returns>
    public static MessageId From(string value) => new(value);

    /// <summary>
    /// Attempts to parse a string as a MessageId.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="messageId">The parsed MessageId if successful.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParse(string? value, out MessageId messageId)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 26)
        {
            messageId = Empty;
            return false;
        }

        messageId = new MessageId(value);
        return true;
    }

    /// <summary>
    /// Compares this MessageId to another for ordering.
    /// </summary>
    /// <param name="other">The other MessageId to compare to.</param>
    /// <returns>A value indicating the relative order.</returns>
    public int CompareTo(MessageId other) => string.CompareOrdinal(_value, other._value);

    /// <summary>
    /// Returns the ULID string representation.
    /// </summary>
    /// <returns>The ULID string.</returns>
    public override string ToString() => _value;
}
