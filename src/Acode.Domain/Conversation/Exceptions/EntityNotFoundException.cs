// src/Acode.Domain/Conversation/Exceptions/EntityNotFoundException.cs
namespace Acode.Domain.Conversation.Exceptions;

using System;

/// <summary>
/// Exception thrown when a requested entity is not found in the data store.
/// </summary>
public sealed class EntityNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
    /// </summary>
    /// <param name="entityType">The type of entity that was not found (e.g., "Chat", "Run", "Message").</param>
    /// <param name="entityId">The identifier of the entity that was not found.</param>
    public EntityNotFoundException(string entityType, string entityId)
        : base($"{entityType} with ID '{entityId}' was not found.")
    {
        EntityType = entityType;
        EntityId = entityId;

        ErrorCode = entityType switch
        {
            "Chat" => "ACODE-CONV-DATA-001",
            "Run" => "ACODE-CONV-DATA-002",
            "Message" => "ACODE-CONV-DATA-003",
            _ => "ACODE-CONV-DATA-004"
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with a custom message.
    /// </summary>
    /// <param name="entityType">The type of entity that was not found.</param>
    /// <param name="entityId">The identifier of the entity that was not found.</param>
    /// <param name="message">The exception message.</param>
    public EntityNotFoundException(string entityType, string entityId, string message)
        : base(message)
    {
        EntityType = entityType;
        EntityId = entityId;

        ErrorCode = entityType switch
        {
            "Chat" => "ACODE-CONV-DATA-001",
            "Run" => "ACODE-CONV-DATA-002",
            "Message" => "ACODE-CONV-DATA-003",
            _ => "ACODE-CONV-DATA-004"
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with a custom error code.
    /// </summary>
    /// <param name="entityType">The type of entity that was not found.</param>
    /// <param name="entityId">The identifier of the entity that was not found.</param>
    /// <param name="errorCode">The error code for this exception.</param>
    /// <param name="message">The exception message.</param>
    public EntityNotFoundException(string entityType, string entityId, string errorCode, string message)
        : base(message)
    {
        EntityType = entityType;
        EntityId = entityId;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the error code for this exception.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets the type of entity that was not found.
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    /// Gets the identifier of the entity that was not found.
    /// </summary>
    public string EntityId { get; }
}
