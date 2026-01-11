// src/Acode.Infrastructure/Concurrency/WorktreeContextResolver.cs
namespace Acode.Infrastructure.Concurrency;

using System;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Concurrency;
using Acode.Application.Events;
using Acode.Domain.Concurrency;
using Acode.Domain.Conversation;
using Acode.Domain.Worktree;
using Microsoft.Extensions.Logging;

/// <summary>
/// Infrastructure implementation of <see cref="IContextResolver"/>.
/// Resolves active chat context based on current worktree and Git metadata.
/// </summary>
public sealed class WorktreeContextResolver : IContextResolver
{
    private readonly IBindingService _bindingService;
    private readonly IGitWorktreeDetector _worktreeDetector;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<WorktreeContextResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorktreeContextResolver"/> class.
    /// </summary>
    /// <param name="bindingService">The binding service for querying worktree-to-chat bindings.</param>
    /// <param name="worktreeDetector">The worktree detector for finding Git worktrees from filesystem.</param>
    /// <param name="eventPublisher">The event publisher for context switch notifications.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public WorktreeContextResolver(
        IBindingService bindingService,
        IGitWorktreeDetector worktreeDetector,
        IEventPublisher eventPublisher,
        ILogger<WorktreeContextResolver> logger)
    {
        _bindingService = bindingService ?? throw new ArgumentNullException(nameof(bindingService));
        _worktreeDetector = worktreeDetector ?? throw new ArgumentNullException(nameof(worktreeDetector));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ChatId?> ResolveActiveChatAsync(
        WorktreeId currentWorktree,
        CancellationToken ct)
    {
        var chatId = await _bindingService.GetBoundChatAsync(currentWorktree, ct).ConfigureAwait(false);

        if (chatId.HasValue)
        {
            _logger.LogDebug(
                "Resolved active chat: Worktree={Worktree}, Chat={Chat}",
                currentWorktree,
                chatId.Value);
        }
        else
        {
            _logger.LogDebug(
                "No bound chat for worktree {Worktree}, using global/manual selection",
                currentWorktree);
        }

        return chatId;
    }

    /// <inheritdoc/>
    public async Task<WorktreeId?> DetectCurrentWorktreeAsync(
        string currentDirectory,
        CancellationToken ct)
    {
        var worktree = await _worktreeDetector.DetectAsync(currentDirectory, ct).ConfigureAwait(false);

        if (worktree is not null)
        {
            _logger.LogDebug(
                "Detected worktree: {Worktree} at {Path}",
                worktree.Id,
                worktree.Path);
        }

        return worktree?.Id;
    }

    /// <inheritdoc/>
    public async Task NotifyContextSwitchAsync(
        WorktreeId from,
        WorktreeId toWorktree,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Context switch: {From} → {To}",
            from,
            toWorktree);

        var contextSwitchedEvent = new ContextSwitchedEvent(from, toWorktree, DateTimeOffset.UtcNow);
        await _eventPublisher.PublishAsync(contextSwitchedEvent, ct).ConfigureAwait(false);
    }
}
