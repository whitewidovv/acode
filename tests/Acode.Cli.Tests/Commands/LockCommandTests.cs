// tests/Acode.Cli.Tests/Commands/LockCommandTests.cs
#pragma warning disable CA2007 // Do not directly await a Task

namespace Acode.Cli.Tests.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Concurrency;
using Acode.Cli.Commands;
using Acode.Domain.Worktree;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for LockCommand - worktree lock management CLI.
/// Covers status, unlock, cleanup subcommands per AC-054, AC-063-065.
/// </summary>
public sealed class LockCommandTests
{
    private readonly ILockService _lockService;
    private readonly LockCommand _command;

    public LockCommandTests()
    {
        _lockService = Substitute.For<ILockService>();
        _command = new LockCommand(_lockService);
    }

    [Fact]
    public void Name_ShouldBe_Lock()
    {
        _command.Name.Should().Be("lock");
    }

    [Fact]
    public void Description_ShouldDescribe_LockManagement()
    {
        _command.Description.Should().Contain("lock");
        _command.Description.Should().Contain("worktree");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoArgs_ReturnsInvalidArguments()
    {
        // Arrange
        var context = CreateContext(Array.Empty<string>());

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.InvalidArguments);
        context.Output.ToString().Should().Contain("Usage:");
    }

    [Fact]
    public async Task StatusAsync_WithNoLock_ShowsNotLocked()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var config = new Dictionary<string, object> { ["CurrentWorktree"] = worktreeId };
        var context = CreateContext(new[] { "status" }, config);

        _lockService.GetStatusAsync(worktreeId, Arg.Any<CancellationToken>())
            .Returns(new LockStatus(IsLocked: false, IsStale: false, TimeSpan.Zero, null, null, null));

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        context.Output.ToString().Should().Contain("not locked");
    }

    [Fact]
    public async Task StatusAsync_WithActiveLock_ShowsLockDetails()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var config = new Dictionary<string, object> { ["CurrentWorktree"] = worktreeId };
        var context = CreateContext(new[] { "status" }, config);

        var lockStatus = new LockStatus(
            IsLocked: true,
            IsStale: false,
            Age: TimeSpan.FromMinutes(3),
            ProcessId: 12345,
            Hostname: "dev-machine",
            Terminal: "/dev/pts/0");

        _lockService.GetStatusAsync(worktreeId, Arg.Any<CancellationToken>())
            .Returns(lockStatus);

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        context.Output.ToString().Should().Contain("is locked");
        context.Output.ToString().Should().Contain("12345");
        context.Output.ToString().Should().Contain("dev-machine");
    }

    [Fact]
    public async Task StatusAsync_WithStaleLock_IndicatesStale()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var config = new Dictionary<string, object> { ["CurrentWorktree"] = worktreeId };
        var context = CreateContext(new[] { "status" }, config);

        var lockStatus = new LockStatus(
            IsLocked: true,
            IsStale: true,
            Age: TimeSpan.FromMinutes(10),
            ProcessId: 99999,
            Hostname: "old-machine",
            Terminal: "/dev/pts/1");

        _lockService.GetStatusAsync(worktreeId, Arg.Any<CancellationToken>())
            .Returns(lockStatus);

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        context.Output.ToString().Should().Contain("STALE");
    }

    [Fact]
    public async Task UnlockAsync_WithForceFlag_RemovesLock()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var config = new Dictionary<string, object> { ["CurrentWorktree"] = worktreeId };
        var context = CreateContext(new[] { "unlock", "--force" }, config);

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        await _lockService.Received(1).ForceUnlockAsync(worktreeId, Arg.Any<CancellationToken>());
        context.Output.ToString().Should().Contain("unlocked");
    }

    [Fact]
    public async Task UnlockAsync_WithoutForceFlag_RequiresConfirmation()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var config = new Dictionary<string, object> { ["CurrentWorktree"] = worktreeId };
        var context = CreateContext(new[] { "unlock" }, config);

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.InvalidArguments);
        await _lockService.DidNotReceive().ForceUnlockAsync(Arg.Any<WorktreeId>(), Arg.Any<CancellationToken>());
        context.Output.ToString().Should().Contain("--force");
    }

    [Fact]
    public async Task CleanupAsync_RemovesStaleLocksAndReportsCount()
    {
        // Arrange
        var context = CreateContext(new[] { "cleanup" });

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.Success);
        await _lockService.Received(1).ReleaseStaleLocksAsync(
            Arg.Is<TimeSpan>(ts => ts.TotalMinutes == 5),
            Arg.Any<CancellationToken>());
        context.Output.ToString().Should().Contain("Stale locks cleaned");
    }

    [Fact]
    public async Task StatusAsync_WhenNotInWorktree_ReturnsError()
    {
        // Arrange - No CurrentWorktree in config
        var context = CreateContext(new[] { "status" });

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.GeneralError);
        context.Output.ToString().Should().Contain("Not in a worktree");
    }

    [Fact]
    public async Task UnlockAsync_WhenNotInWorktree_ReturnsError()
    {
        // Arrange - No CurrentWorktree in config
        var context = CreateContext(new[] { "unlock", "--force" });

        // Act
        var result = await _command.ExecuteAsync(context);

        // Assert
        result.Should().Be(ExitCode.GeneralError);
        context.Output.ToString().Should().Contain("Not in a worktree");
    }

    private static CommandContext CreateContext(string[] args, Dictionary<string, object>? config = null)
    {
        return new CommandContext
        {
            Args = args,
            Output = new StringWriter(),
            Formatter = Substitute.For<IOutputFormatter>(),
            Configuration = config ?? new Dictionary<string, object>(),
            CancellationToken = CancellationToken.None,
        };
    }
}
