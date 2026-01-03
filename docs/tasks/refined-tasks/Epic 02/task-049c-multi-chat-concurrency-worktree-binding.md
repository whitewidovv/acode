# Task 049.c: Multi-Chat Concurrency Model + Run/Worktree Binding

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – CLI + Orchestration Core  
**Dependencies:** Task 049.b (CLI Commands), Task 023 (Events), Task 027 (Git Integration)  

---

## Description

Task 049.c implements the multi-chat concurrency model and worktree binding system. Developers work on multiple features simultaneously—each feature branch deserves its own conversation context. This task ensures chats are properly isolated, bound to worktrees, and safely concurrent.

Multi-chat concurrency means multiple chats can exist and be accessed without interference. Each worktree has its own active chat. Switching worktrees automatically switches conversation context. The agent never confuses contexts.

Worktree binding associates a chat with a Git worktree. When you enter a worktree directory, the bound chat becomes active. This enables natural workflows: `cd feature/auth && acode run "Continue auth work"` automatically uses the auth chat.

Run binding tracks which runs belong to which chat and worktree. When a run completes, it's permanently associated with its originating chat. Historical runs can be queried by chat or worktree.

Concurrency safety prevents race conditions. Multiple terminal sessions can access the same workspace. Locking ensures only one run executes per worktree at a time. Other sessions queue or receive busy errors.

The binding model is hierarchical. Workspace contains worktrees. Worktrees contain (optionally bound) chats. Chats contain runs. Runs contain messages. Each level has its own locking and isolation semantics.

Automatic binding simplifies workflows. Create a chat while in a worktree—it's automatically bound. Create a worktree—option to create a bound chat. Unbind and rebind as needed.

Binding persistence survives sessions. The binding relationship is stored in the workspace database. Restarting the machine, the CLI, or VS Code preserves bindings.

Conflict resolution handles edge cases. What if a bound worktree is deleted? The chat becomes unbound but persists. What if a bound chat is purged? The worktree becomes unbound.

The model supports both bound and unbound workflows. Some chats are global—not tied to any worktree. Some developers prefer manual switching. Both patterns are first-class.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Worktree | Git working directory |
| Binding | Chat-to-worktree association |
| Active Chat | Currently selected conversation |
| Concurrency | Multiple simultaneous operations |
| Locking | Exclusive access control |
| Run Isolation | Preventing context bleed |
| Auto-Bind | Automatic association |
| Unbound | No worktree association |
| Context Switch | Changing active chat |
| Queue | Waiting for lock |
| Busy Error | Lock unavailable |
| Hierarchy | Workspace → Worktree → Chat |
| Cascade | Related entity cleanup |
| Race Condition | Concurrent conflict |
| Session | Terminal/process instance |

---

## Out of Scope

The following items are explicitly excluded from Task 049.c:

- **Data model** - Task 049.a
- **CLI commands** - Task 049.b
- **Search** - Task 049.d
- **Retention** - Task 049.e
- **Sync** - Task 049.f
- **Distributed locking** - Local only
- **Cross-machine binding** - Single machine
- **Real-time sync** - Async only
- **Worktree creation** - Git operations
- **Chat merging** - Not supported

---

## Functional Requirements

### Worktree Binding

- FR-001: Chat MAY be bound to worktree
- FR-002: Binding MUST be one-to-one
- FR-003: Worktree MAY have one bound chat
- FR-004: Binding MUST persist
- FR-005: Binding MUST survive restart

### Auto-Binding

- FR-006: `chat new` in worktree MUST auto-bind
- FR-007: `chat new --no-bind` MUST skip
- FR-008: Unbound chat MAY be bound later
- FR-009: `chat bind <id>` MUST work

### Unbinding

- FR-010: `chat unbind` MUST work
- FR-011: Unbind MUST preserve chat
- FR-012: Unbind MUST clear worktree reference
- FR-013: Chat MAY be rebound

### Context Switching

- FR-014: `cd` to worktree MUST switch context
- FR-015: Active chat MUST follow worktree
- FR-016: Switch MUST be instant
- FR-017: Switch MUST log event

### Run Isolation

- FR-018: Run MUST belong to one chat
- FR-019: Run MUST record worktree
- FR-020: Run context MUST NOT bleed
- FR-021: Messages MUST NOT cross runs

### Concurrency Locking

- FR-022: One run per worktree at a time
- FR-023: Lock MUST be acquired before run
- FR-024: Lock MUST be released after run
- FR-025: Lock timeout MUST be configurable

### Lock Behavior

- FR-026: Blocked request MUST queue OR error
- FR-027: Default MUST be error
- FR-028: `--wait` MUST queue
- FR-029: Queue timeout MUST be configurable

### Lock Storage

- FR-030: Locks MUST use file system
- FR-031: Lock files MUST be in .agent/
- FR-032: Stale locks MUST be detected
- FR-033: Stale lock threshold: 5 minutes

### Multi-Session

- FR-034: Multiple terminals MUST work
- FR-035: Each session MUST see consistent state
- FR-036: Lock conflicts MUST be clear

### Binding Queries

- FR-037: Get chat by worktree MUST work
- FR-038: Get worktree by chat MUST work
- FR-039: List bound pairs MUST work
- FR-040: List unbound chats MUST work

### Cascade Handling

- FR-041: Deleted worktree MUST unbind chat
- FR-042: Purged chat MUST unbind worktree
- FR-043: Cascade MUST be atomic

### Status Commands

- FR-044: `acode status` MUST show binding
- FR-045: Status MUST show lock state
- FR-046: Status MUST show queued count

---

## Non-Functional Requirements

### Performance

- NFR-001: Context switch < 50ms
- NFR-002: Lock acquire < 10ms
- NFR-003: Binding query < 5ms

### Reliability

- NFR-004: No lost bindings
- NFR-005: No orphaned locks
- NFR-006: Crash-safe state

### Consistency

- NFR-007: One-to-one enforced
- NFR-008: No race conditions
- NFR-009: Eventual cleanup

### Usability

- NFR-010: Clear lock messages
- NFR-011: Obvious context
- NFR-012: Easy unbind

---

## User Manual Documentation

### Overview

Multi-chat concurrency enables working on multiple features simultaneously. Each Git worktree can have its own conversation context, automatically switching as you navigate.

### Quick Start

```bash
# Create worktree with bound chat
$ git worktree add ../feature-auth feature/auth
$ cd ../feature-auth
$ acode chat new "Auth Feature"
Created chat: chat_abc123 (bound to feature/auth)

# Work in worktree
$ acode run "Design login flow"
# Uses auth chat context

# Switch worktree, switch context
$ cd ../feature-payments
$ acode chat new "Payments Feature"
Created chat: chat_def456 (bound to feature/payments)

# Context follows directory
$ cd ../feature-auth
$ acode run "Continue login"
# Back to auth chat context
```

### Binding Commands

```bash
# Bind chat to current worktree
$ acode chat bind chat_abc123
Bound chat_abc123 to: feature/auth

# Unbind chat from worktree
$ acode chat unbind
Unbound chat from: feature/auth

# Create without binding
$ acode chat new --no-bind "General Discussion"
Created chat: chat_xyz789 (unbound)

# View binding status
$ acode chat bindings
Worktree          Bound Chat
feature/auth      chat_abc123 (Auth Feature)
feature/payments  chat_def456 (Payments Feature)
main              (no binding)
```

### Concurrency

```bash
# Terminal 1
$ cd feature/auth
$ acode run "Long operation"
Running...

# Terminal 2 (same worktree)
$ cd feature/auth
$ acode run "Another request"
ERROR: Worktree is locked by another session.
Lock held since: 30s ago
Use --wait to queue, or work in another worktree.

# With --wait
$ acode run --wait "Another request"
Waiting for lock... (timeout: 5m)
Lock acquired!
Running...
```

### Status

```bash
$ acode status

Workspace: /projects/myapp
────────────────────────────────────
Current Worktree: feature/auth
Active Chat: chat_abc123 (Auth Feature)
Binding: Bound

Run Status: Idle
Lock: Available

Other Sessions:
  Terminal 2: Running (feature/payments)
```

### Configuration

```yaml
# .agent/config.yml
concurrency:
  # Lock behavior when busy
  on_busy: error  # or 'wait'
  
  # Wait timeout
  wait_timeout_seconds: 300
  
  # Stale lock detection
  stale_lock_seconds: 300
  
  # Auto-bind new chats
  auto_bind: true
```

### Troubleshooting

#### Stuck Lock

**Problem:** Lock not releasing

**Solution:**
```bash
# Check lock status
$ acode lock status
Lock file: .agent/locks/worktree_feature_auth.lock
Held by: PID 12345
Since: 10m ago
Status: STALE (process not found)

# Force release
$ acode lock release --force
Lock released.
```

#### Wrong Context

**Problem:** Commands using wrong chat

**Solution:**
1. Check current worktree: `pwd`
2. Check binding: `acode chat bindings`
3. Explicitly open: `acode chat open <id>`

#### Orphaned Binding

**Problem:** Binding to deleted worktree

**Solution:**
```bash
$ acode chat bindings --cleanup
Found 1 orphaned binding:
  chat_old123 → deleted_worktree (not found)
  
Clean up? [Y/n] y
Cleaned 1 orphaned binding.
```

---

## Acceptance Criteria

### Binding

- [ ] AC-001: Bind works
- [ ] AC-002: Unbind works
- [ ] AC-003: Auto-bind works
- [ ] AC-004: One-to-one enforced
- [ ] AC-005: Persists across restart

### Context Switching

- [ ] AC-006: cd switches context
- [ ] AC-007: Switch is instant
- [ ] AC-008: Status shows binding

### Concurrency

- [ ] AC-009: Lock acquired
- [ ] AC-010: Lock released
- [ ] AC-011: Busy error shown
- [ ] AC-012: --wait queues

### Multi-Session

- [ ] AC-013: Multiple terminals work
- [ ] AC-014: Lock conflicts detected
- [ ] AC-015: Stale locks cleaned

### Cascade

- [ ] AC-016: Deleted worktree unbinds
- [ ] AC-017: Purged chat unbinds

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Concurrency/
├── BindingTests.cs
│   ├── Should_Bind_Chat_To_Worktree()
│   ├── Should_Unbind_Chat()
│   ├── Should_Enforce_OneToOne()
│   └── Should_Persist_Binding()
│
├── LockTests.cs
│   ├── Should_Acquire_Lock()
│   ├── Should_Release_Lock()
│   ├── Should_Detect_Stale()
│   └── Should_Block_Concurrent()
│
└── ContextTests.cs
    ├── Should_Switch_On_Directory()
    └── Should_Isolate_Runs()
```

### Integration Tests

```
Tests/Integration/Concurrency/
├── MultiSessionTests.cs
│   ├── Should_Handle_Multiple_Terminals()
│   └── Should_Queue_With_Wait()
│
└── BindingPersistenceTests.cs
    └── Should_Survive_Restart()
```

### E2E Tests

```
Tests/E2E/Concurrency/
├── WorktreeWorkflowE2ETests.cs
│   ├── Should_Auto_Bind_New_Chat()
│   ├── Should_Switch_Context_On_Cd()
│   └── Should_Handle_Lock_Conflict()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Context switch | 25ms | 50ms |
| Lock acquire | 5ms | 10ms |
| Binding query | 2ms | 5ms |

---

## User Verification Steps

### Scenario 1: Auto-Bind

1. Create worktree
2. cd into worktree
3. Create chat
4. Verify: Chat bound to worktree

### Scenario 2: Context Switch

1. Create two bound worktrees
2. cd between them
3. Verify: Active chat changes

### Scenario 3: Lock Conflict

1. Start run in worktree
2. Open second terminal
3. Try to run in same worktree
4. Verify: Lock error shown

### Scenario 4: Wait Queue

1. Start run in worktree
2. In second terminal, run with --wait
3. Complete first run
4. Verify: Second run starts

### Scenario 5: Stale Lock

1. Create lock file manually
2. Wait for stale timeout
3. Try to acquire lock
4. Verify: Stale lock released

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Concurrency/
│   ├── WorktreeBinding.cs
│   └── WorktreeLock.cs
│
src/AgenticCoder.Application/
├── Concurrency/
│   ├── IBindingService.cs
│   ├── ILockService.cs
│   └── IContextResolver.cs
│
src/AgenticCoder.Infrastructure/
├── Concurrency/
│   ├── BindingService.cs
│   ├── FileLockService.cs
│   └── WorktreeContextResolver.cs
```

### WorktreeBinding Entity

```csharp
namespace AgenticCoder.Domain.Concurrency;

public sealed class WorktreeBinding
{
    public WorktreeId WorktreeId { get; }
    public ChatId ChatId { get; }
    public DateTimeOffset CreatedAt { get; }
    
    public static WorktreeBinding Create(
        WorktreeId worktree,
        ChatId chat);
}
```

### ILockService Interface

```csharp
namespace AgenticCoder.Application.Concurrency;

public interface ILockService
{
    Task<IAsyncDisposable> AcquireAsync(
        WorktreeId worktree,
        TimeSpan? timeout,
        CancellationToken ct);
        
    Task<LockStatus> GetStatusAsync(
        WorktreeId worktree,
        CancellationToken ct);
        
    Task ReleaseStaleAsync(
        TimeSpan threshold,
        CancellationToken ct);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-CONC-001 | Worktree locked |
| ACODE-CONC-002 | Wait timeout |
| ACODE-CONC-003 | Binding exists |
| ACODE-CONC-004 | Worktree not found |
| ACODE-CONC-005 | Chat already bound |

### Implementation Checklist

1. [ ] Create binding entity
2. [ ] Create lock entity
3. [ ] Implement binding service
4. [ ] Implement lock service
5. [ ] Implement context resolver
6. [ ] Add CLI integration
7. [ ] Add stale detection
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Binding model
2. **Phase 2:** Lock service
3. **Phase 3:** Context resolver
4. **Phase 4:** CLI integration
5. **Phase 5:** Stale cleanup

---

**End of Task 049.c Specification**