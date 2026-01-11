// tests/Acode.Infrastructure.Tests/Concurrency/WorktreeContextResolverTests.cs
#pragma warning disable CA2007 // xUnit tests should use ConfigureAwait(true)

namespace Acode.Infrastructure.Tests.Concurrency;

using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Concurrency;
using Acode.Application.Events;
using Acode.Domain.Concurrency;
using Acode.Domain.Conversation;
using Acode.Domain.Worktree;
using Acode.Infrastructure.Concurrency;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for <see cref="WorktreeContextResolver"/>.
/// Verifies context resolution, worktree detection, and event publishing.
/// </summary>
public sealed class WorktreeContextResolverTests
{
    private readonly IBindingService _bindingService;
    private readonly IGitWorktreeDetector _worktreeDetector;
    private readonly IEventPublisher _eventPublisher;
    private readonly WorktreeContextResolver _resolver;

    public WorktreeContextResolverTests()
    {
        _bindingService = Substitute.For<IBindingService>();
        _worktreeDetector = Substitute.For<IGitWorktreeDetector>();
        _eventPublisher = Substitute.For<IEventPublisher>();
        _resolver = new WorktreeContextResolver(
            _bindingService,
            _worktreeDetector,
            _eventPublisher,
            NullLogger<WorktreeContextResolver>.Instance);
    }

    [Fact]
    public async Task ResolveActiveChatAsync_WithBinding_ReturnsBoundChat()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var chatId = ChatId.NewId();

        _bindingService.GetBoundChatAsync(worktreeId, Arg.Any<CancellationToken>())
            .Returns(chatId);

        // Act
        var result = await _resolver.ResolveActiveChatAsync(worktreeId, CancellationToken.None);

        // Assert
        result.Should().Be(chatId);
    }

    [Fact]
    public async Task ResolveActiveChatAsync_WithoutBinding_ReturnsNull()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");

        _bindingService.GetBoundChatAsync(worktreeId, Arg.Any<CancellationToken>())
            .Returns((ChatId?)null);

        // Act
        var result = await _resolver.ResolveActiveChatAsync(worktreeId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DetectCurrentWorktreeAsync_WithValidWorktree_ReturnsWorktreeId()
    {
        // Arrange
        var currentDirectory = "/home/user/project/feature/auth/src";
        var worktreeRoot = "/home/user/project/feature/auth";
        var worktreeId = WorktreeId.FromPath(worktreeRoot);
        var detectedWorktree = new DetectedWorktree(worktreeId, worktreeRoot);

        _worktreeDetector.DetectAsync(currentDirectory, Arg.Any<CancellationToken>())
            .Returns(detectedWorktree);

        // Act
        var result = await _resolver.DetectCurrentWorktreeAsync(currentDirectory, CancellationToken.None);

        // Assert
        result.Should().Be(worktreeId);
    }

    [Fact]
    public async Task DetectCurrentWorktreeAsync_WithoutWorktree_ReturnsNull()
    {
        // Arrange
        var currentDirectory = "/home/user/not-a-repo";

        _worktreeDetector.DetectAsync(currentDirectory, Arg.Any<CancellationToken>())
            .Returns((DetectedWorktree?)null);

        // Act
        var result = await _resolver.DetectCurrentWorktreeAsync(currentDirectory, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task NotifyContextSwitchAsync_PublishesContextSwitchedEvent()
    {
        // Arrange
        var fromWorktree = WorktreeId.FromPath("/home/user/project/feature/auth");
        var toWorktree = WorktreeId.FromPath("/home/user/project/feature/payments");

        // Act
        await _resolver.NotifyContextSwitchAsync(fromWorktree, toWorktree, CancellationToken.None);

        // Assert
        await _eventPublisher.Received(1).PublishAsync(
            Arg.Is<ContextSwitchedEvent>(e =>
                e.FromWorktree == fromWorktree &&
                e.ToWorktree == toWorktree),
            Arg.Any<CancellationToken>());
    }
}
