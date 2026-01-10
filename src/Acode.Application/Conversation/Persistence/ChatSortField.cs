// src/Acode.Application/Conversation/Persistence/ChatSortField.cs
namespace Acode.Application.Conversation.Persistence;

/// <summary>
/// Enum defining fields by which chats can be sorted.
/// </summary>
public enum ChatSortField
{
    /// <summary>
    /// Sort by creation timestamp.
    /// </summary>
    CreatedAt,

    /// <summary>
    /// Sort by last update timestamp.
    /// </summary>
    UpdatedAt,

    /// <summary>
    /// Sort by chat title alphabetically.
    /// </summary>
    Title,
}
