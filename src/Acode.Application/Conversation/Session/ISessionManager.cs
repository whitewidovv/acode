// src/Acode.Application/Conversation/Session/ISessionManager.cs
namespace Acode.Application.Conversation.Session;

using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Conversation;

/// <summary>
/// Manages session state for the current CLI session, including active chat tracking.
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Sets the active chat for the current session.
    /// </summary>
    /// <param name="chatId">The chat ID to set as active.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetActiveChatAsync(ChatId chatId, CancellationToken ct = default);

    /// <summary>
    /// Gets the currently active chat ID.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The active chat ID, or null if no chat is active.</returns>
    Task<ChatId?> GetActiveChatAsync(CancellationToken ct = default);

    /// <summary>
    /// Clears the active chat (no chat is active after this call).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearActiveChatAsync(CancellationToken ct = default);
}
