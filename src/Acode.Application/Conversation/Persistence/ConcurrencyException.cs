// src/Acode.Application/Conversation/Persistence/ConcurrencyException.cs
namespace Acode.Application.Conversation.Persistence;

using System;

/// <summary>
/// Exception thrown when an optimistic concurrency conflict occurs during update operations.
/// Indicates that the entity was modified by another process since it was retrieved.
/// </summary>
public sealed class ConcurrencyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
    /// </summary>
    public ConcurrencyException()
        : base("A concurrency conflict occurred. The entity was modified by another process.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ConcurrencyException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyException"/> class with a custom message and inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
