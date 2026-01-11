// src/Acode.Infrastructure/Concurrency/BindingService.cs
namespace Acode.Infrastructure.Concurrency;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Concurrency;
using Acode.Domain.Concurrency;
using Acode.Domain.Conversation;
using Acode.Domain.Worktree;
using Microsoft.Extensions.Logging;

/// <summary>
/// Infrastructure implementation of <see cref="IBindingService"/>.
/// Provides worktree-to-chat binding management with validation and logging.
/// </summary>
public sealed class BindingService : IBindingService
{
    private readonly IBindingRepository _repository;
    private readonly ILogger<BindingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BindingService"/> class.
    /// </summary>
    /// <param name="repository">The binding repository for persistence.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public BindingService(
        IBindingRepository repository,
        ILogger<BindingService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task CreateBindingAsync(
        WorktreeId worktreeId,
        ChatId chatId,
        CancellationToken ct)
    {
        // Check if worktree already bound
        var existingBinding = await _repository.GetByWorktreeAsync(worktreeId, ct).ConfigureAwait(false);
        if (existingBinding is not null)
        {
            throw new InvalidOperationException(
                $"Worktree {worktreeId.Value} is already bound to chat {existingBinding.ChatId}");
        }

        // Create binding
        var binding = WorktreeBinding.Create(worktreeId, chatId);
        await _repository.CreateAsync(binding, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Created binding: Worktree={Worktree}, Chat={Chat}",
            worktreeId,
            chatId);
    }

    /// <inheritdoc/>
    public async Task DeleteBindingAsync(
        WorktreeId worktreeId,
        CancellationToken ct)
    {
        await _repository.DeleteAsync(worktreeId, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Deleted binding: Worktree={Worktree}",
            worktreeId);
    }

    /// <inheritdoc/>
    public async Task<ChatId?> GetBoundChatAsync(
        WorktreeId worktreeId,
        CancellationToken ct)
    {
        var binding = await _repository.GetByWorktreeAsync(worktreeId, ct).ConfigureAwait(false);
        return binding?.ChatId;
    }

    /// <inheritdoc/>
    public async Task<WorktreeId?> GetBoundWorktreeAsync(
        ChatId chatId,
        CancellationToken ct)
    {
        var binding = await _repository.GetByChatAsync(chatId, ct).ConfigureAwait(false);
        return binding?.WorktreeId;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WorktreeBinding>> ListAllBindingsAsync(
        CancellationToken ct)
    {
        var bindings = await _repository.ListAllAsync(ct).ConfigureAwait(false);
        return bindings;
    }
}
