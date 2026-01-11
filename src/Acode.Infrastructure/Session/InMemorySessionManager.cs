// src/Acode.Infrastructure/Session/InMemorySessionManager.cs
namespace Acode.Infrastructure.Session;

using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Conversation.Session;
using Acode.Domain.Conversation;

/// <summary>
/// In-memory implementation of ISessionManager.
/// Stores session state in memory (not persisted across CLI invocations).
/// </summary>
public sealed class InMemorySessionManager : ISessionManager
{
    private ChatId? _activeChatId;

    /// <inheritdoc/>
    public Task SetActiveChatAsync(ChatId chatId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(chatId);
        _activeChatId = chatId;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<ChatId?> GetActiveChatAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_activeChatId);
    }

    /// <inheritdoc/>
    public Task ClearActiveChatAsync(CancellationToken ct = default)
    {
        _activeChatId = null;
        return Task.CompletedTask;
    }
}
