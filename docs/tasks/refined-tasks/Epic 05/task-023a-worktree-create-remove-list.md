# Task 023.a: worktree create/remove/list

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Task 023 (Worktree-per-Task), Task 022 (Git Tool Layer)  

---

## Description

Task 023.a implements the core worktree operations: create, remove, and list. These operations wrap Git's worktree commands with structured output and error handling.

Worktree creation MUST create both the worktree directory and a new branch. The path MUST be within the configured base directory. The branch name MUST follow naming conventions.

Worktree removal MUST delete the worktree directory and optionally the associated branch. Uncommitted changes MUST be detected before removal. Force removal MUST be available for abandoned work.

Worktree listing MUST enumerate all linked worktrees. Each entry MUST include path, branch, commit SHA, and status. Pruning stale entries MUST be supported.

### Business Value

These operations enable the worktree-per-task pattern. The agent can create isolated environments, work in them, and clean up afterward.

### Scope Boundaries

This task covers the Git worktree operations. Task mapping is in 023.b. Cleanup policies are in 023.c.

### Integration Points

- Task 023: IWorktreeService interface
- Task 022: Git branch operations
- Task 018: Command execution

### Failure Modes

- Path already exists → PathExistsException
- Branch in use → BranchInUseException
- Uncommitted changes → UncommittedChangesException
- Stale worktree → Prune or error

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Create | Add a new linked worktree |
| Remove | Delete a linked worktree |
| List | Enumerate all worktrees |
| Prune | Clean up stale worktree entries |
| Stale | Worktree with missing directory |
| Locked | Worktree protected from removal |

---

## Out of Scope

- Task mapping persistence (Task 023.b)
- Automatic cleanup policies (Task 023.c)
- Cross-worktree operations
- Worktree locking management

---

## Functional Requirements

### FR-001 to FR-025: Create Operation

- FR-001: `CreateAsync` MUST create linked worktree
- FR-002: Path MUST be specified
- FR-003: Branch MUST be specified or generated
- FR-004: New branch MUST be created if not exists
- FR-005: Existing branch MUST be checked out
- FR-006: `--detach` MUST create detached HEAD
- FR-007: `--force` MUST override safety checks
- FR-008: Path MUST be validated
- FR-009: Path MUST NOT exist before creation
- FR-010: Path MUST be within base directory
- FR-011: Branch name MUST be validated
- FR-012: Creation MUST be atomic
- FR-013: Failed creation MUST cleanup
- FR-014: Created worktree MUST be returned
- FR-015: Creation MUST be logged
- FR-016: Progress MUST be reportable
- FR-017: Submodule init MUST be optional
- FR-018: Lock MUST be settable on creation
- FR-019: Lock reason MUST be stored
- FR-020: Worktree MUST be immediately usable
- FR-021: Index MUST be initialized
- FR-022: Working tree MUST match branch HEAD
- FR-023: `.git` file MUST point to main repo
- FR-024: Concurrent creates MUST be safe
- FR-025: Create in progress MUST be detected

### FR-026 to FR-045: Remove Operation

- FR-026: `RemoveAsync` MUST remove worktree
- FR-027: Directory MUST be deleted
- FR-028: Git metadata MUST be cleaned
- FR-029: Uncommitted changes MUST block removal
- FR-030: `--force` MUST override safety check
- FR-031: Branch deletion MUST be optional
- FR-032: `--delete-branch` MUST remove branch
- FR-033: Merged branch MUST delete without force
- FR-034: Unmerged branch MUST require force
- FR-035: Locked worktree MUST block removal
- FR-036: `--unlock` MUST remove lock first
- FR-037: Non-existent worktree MUST return success
- FR-038: Stale worktree MUST be prunable
- FR-039: Removal MUST be logged
- FR-040: Removal MUST update internal state
- FR-041: Concurrent removals MUST be safe
- FR-042: Partial removal MUST be recoverable
- FR-043: Related refs MUST be cleaned
- FR-044: Admin directory MUST be cleaned
- FR-045: Parent directories MUST NOT be deleted

### FR-046 to FR-060: List Operation

- FR-046: `ListAsync` MUST return all worktrees
- FR-047: Main worktree MUST be included
- FR-048: Linked worktrees MUST be included
- FR-049: Each entry MUST include path
- FR-050: Each entry MUST include branch
- FR-051: Each entry MUST include commit SHA
- FR-052: Each entry MUST include locked status
- FR-053: Each entry MUST include prunable status
- FR-054: `--prune` MUST remove stale entries
- FR-055: Stale detection MUST check path exists
- FR-056: Listing MUST be fast (<500ms)
- FR-057: Large worktree counts MUST handle
- FR-058: Broken worktrees MUST be flagged
- FR-059: Listing MUST NOT modify state
- FR-060: Porcelain output MUST be parsed

---

## Non-Functional Requirements

- NFR-001: Create MUST complete in <5s
- NFR-002: Remove MUST complete in <2s
- NFR-003: List MUST complete in <500ms
- NFR-004: Concurrent operations MUST be safe
- NFR-005: Path sanitization MUST prevent injection
- NFR-006: Symlinks MUST NOT escape containment
- NFR-007: File permissions MUST be respected
- NFR-008: Large file handling MUST work
- NFR-009: Network paths MUST be blocked
- NFR-010: Temp files MUST be cleaned up

---

## User Manual Documentation

### Commands

```bash
# Create worktree
acode worktree create <path> --branch <name>

# Remove worktree
acode worktree remove <path> [--force] [--delete-branch]

# List worktrees
acode worktree list [--prune]
```

### Examples

```bash
# Create isolated worktree
acode worktree create .acode/worktrees/task-123 --branch feature/task-123

# List all worktrees
acode worktree list

# Remove with branch cleanup
acode worktree remove .acode/worktrees/task-123 --delete-branch
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Create worktree works
- [ ] AC-002: Branch created automatically
- [ ] AC-003: Path validation works
- [ ] AC-004: Remove worktree works
- [ ] AC-005: Uncommitted check works
- [ ] AC-006: Force removal works
- [ ] AC-007: List shows all worktrees
- [ ] AC-008: Stale detection works
- [ ] AC-009: Prune removes stale entries
- [ ] AC-010: CLI commands work

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test path validation
- [ ] UT-002: Test branch name generation
- [ ] UT-003: Test list parsing
- [ ] UT-004: Test stale detection

### Integration Tests

- [ ] IT-001: Create on real repo
- [ ] IT-002: Remove on real repo
- [ ] IT-003: List on real repo
- [ ] IT-004: Prune stale entries

### End-to-End Tests

- [ ] E2E-001: CLI create
- [ ] E2E-002: CLI remove
- [ ] E2E-003: CLI list

---

## Implementation Prompt

### Git Commands

```bash
# Create
git worktree add <path> -b <branch>

# Remove
git worktree remove <path> [--force]

# List
git worktree list --porcelain

# Prune
git worktree prune
```

### Validation Checklist

- [ ] Path containment enforced
- [ ] Concurrent safety tested
- [ ] Error messages clear
- [ ] All edge cases handled

---

**End of Task 023.a Specification**