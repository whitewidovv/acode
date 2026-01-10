// src/Acode.Domain/Concurrency/ContextSwitchedEvent.cs
namespace Acode.Domain.Concurrency;

using System;
using Acode.Domain.Worktree;

/// <summary>
/// Domain event raised when the active worktree context switches.
/// Used for telemetry, logging, and triggering context-dependent behaviors.
/// </summary>
public sealed record ContextSwitchedEvent(
    WorktreeId FromWorktree,
    WorktreeId ToWorktree,
    DateTimeOffset OccurredAt);
