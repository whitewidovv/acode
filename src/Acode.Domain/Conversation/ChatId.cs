// src/Acode.Domain/Conversation/ChatId.cs
namespace Acode.Domain.Conversation;

using System;
using Acode.Domain.Common;

/// <summary>
/// Strongly-typed identifier for Chat entities using ULID format.
/// </summary>
public readonly record struct ChatId : IComparable<ChatId>
{
    private readonly string _value;

    private ChatId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ChatId cannot be empty", nameof(value));
        }

        if (value.Length != 26)
        {
            throw new ArgumentException("ChatId must be 26 characters (ULID format)", nameof(value));
        }

        _value = value;
    }

    /// <summary>
    /// Gets the ULID string value.
    /// </summary>
    public string Value => _value ?? throw new InvalidOperationException("ChatId not initialized");

    /// <summary>
    /// Gets an empty ChatId (all zeros).
    /// </summary>
    public static ChatId Empty => new("00000000000000000000000000");

    /// <summary>
    /// Implicit conversion to string for convenience.
    /// </summary>
    /// <param name="id">The ChatId to convert.</param>
    public static implicit operator string(ChatId id) => id.Value;

    /// <summary>
    /// Generates a new ChatId with a generated ULID.
    /// </summary>
    /// <returns>A new ChatId instance.</returns>
    public static ChatId NewId() => new(Ulid.NewUlid());

    /// <summary>
    /// Creates a ChatId from an existing ULID string.
    /// </summary>
    /// <param name="value">The 26-character ULID string.</param>
    /// <returns>A new ChatId instance.</returns>
    public static ChatId From(string value) => new(value);

    /// <summary>
    /// Attempts to parse a string as a ChatId.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="chatId">The parsed ChatId if successful.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParse(string? value, out ChatId chatId)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 26)
        {
            chatId = Empty;
            return false;
        }

        chatId = new ChatId(value);
        return true;
    }

    /// <summary>
    /// Compares this ChatId to another for ordering.
    /// </summary>
    /// <param name="other">The other ChatId to compare to.</param>
    /// <returns>A value indicating the relative order.</returns>
    public int CompareTo(ChatId other) => string.CompareOrdinal(_value, other._value);

    /// <summary>
    /// Returns the ULID string representation.
    /// </summary>
    /// <returns>The ULID string.</returns>
    public override string ToString() => _value;
}
