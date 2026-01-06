# Task 010 Implementation Plan: CLI Command Framework

## Status: Planning

**Started:** 2026-01-06
**Epic:** Epic 02 - CLI + Agent Orchestration Core
**Priority:** P0 (Critical Path)
**Complexity:** 21 Fibonacci Points

---

## Executive Summary

Task 010 implements the CLI Command Framework - the foundational user interface that transforms Acode from a library into a practical developer tool. This includes command parsing, routing, help system, output formatting (human-readable + JSONL), configuration management, error handling, and all core CLI commands.

**Scope Breakdown:**
- Task 010 (Parent): Command infrastructure, parser, router, output formatters
- Task 010a: Command routing, help system, standard output
- Task 010b: JSONL event stream mode for automation
- Task 010c: Non-interactive mode behaviors

---

## Core Principles

1. **Strict TDD** - Red → Green → Refactor for every behavior
2. **Incremental Commits** - One logical unit per commit
3. **Subtask Completion** - Task 010 NOT complete until ALL subtasks done
4. **Layer Boundaries** - Domain → Application → Infrastructure → CLI
5. **No Shortcuts** - All FRs must be implemented

---

## Implementation Strategy

### Dependency Order

```
Task 010 (CLI Infrastructure)
    ↓
Task 010a (Routing + Help)
    ↓
Task 010b (JSONL Output)
    ↓
Task 010c (Non-interactive Mode)
```

---

## Phase 1: Task 010 Parent - CLI Infrastructure

### Phase 1.1: Core CLI Types (Domain)

- [ ] **Test:** ExitCode enum tests
- [ ] **Impl:** ExitCode enum (Success=0, GeneralError=1, InvalidArgs=2, etc.)
- [ ] **Commit:** "feat(task-010): implement ExitCode enum"

- [ ] **Test:** CommandContext record tests
- [ ] **Impl:** CommandContext record (Config, OutputWriter, CancellationToken)
- [ ] **Commit:** "feat(task-010): implement CommandContext record"

### Phase 1.2: Command Interface (Application)

- [ ] **Test:** (Will test via concrete implementations)
- [ ] **Impl:** ICommand interface (Name, Aliases, Description, ExecuteAsync)
- [ ] **Commit:** "feat(task-010): define ICommand interface"

- [ ] **Test:** (Will test via concrete implementations)
- [ ] **Impl:** ICommandRouter interface
- [ ] **Commit:** "feat(task-010): define ICommandRouter interface"

### Phase 1.3: Output Formatters (Application → Infrastructure)

- [ ] **Test:** (Interface tests)
- [ ] **Impl:** IOutputFormatter interface
- [ ] **Commit:** "feat(task-010): define IOutputFormatter interface"

- [ ] **Test:** ConsoleFormatterTests
- [ ] **Impl:** ConsoleFormatter implementation (human-readable output)
- [ ] **Commit:** "feat(task-010): implement ConsoleFormatter"

### Phase 1.4: Basic Commands (Infrastructure → CLI)

- [ ] **Test:** HelpCommandTests
- [ ] **Impl:** HelpCommand implementation
- [ ] **Commit:** "feat(task-010): implement HelpCommand"

- [ ] **Test:** VersionCommandTests
- [ ] **Impl:** VersionCommand implementation
- [ ] **Commit:** "feat(task-010): implement VersionCommand"

### Task 010 Parent Completion

- [ ] All core infrastructure implemented
- [ ] Build succeeds with zero warnings
- [ ] All tests pass
- [ ] **Task 010 Parent marked COMPLETE**

---

## Phase 2: Task 010a - Command Routing + Help

### Phase 2.1: Command Router (Infrastructure)

- [ ] **Test:** CommandRouterTests - Routes to correct command
- [ ] **Impl:** CommandRouter with command registry
- [ ] **Commit:** "feat(task-010a): implement CommandRouter"

### Phase 2.2: Help System

- [ ] **Test:** HelpFormatterTests - Formats help output
- [ ] **Impl:** HelpFormatter with terminal width detection
- [ ] **Commit:** "feat(task-010a): implement HelpFormatter"

### Phase 2.3: Command Suggestions (Fuzzy Matching)

- [ ] **Test:** SuggestionTests - Levenshtein distance suggestions
- [ ] **Impl:** Command suggestion engine
- [ ] **Commit:** "feat(task-010a): implement command suggestions"

### Task 010a Completion

- [ ] Routing and help complete
- [ ] Build succeeds with zero warnings
- [ ] All tests pass
- [ ] **Task 010a marked COMPLETE**

---

## Phase 3: Task 010b - JSONL Event Stream Mode

### Phase 3.1: JSONL Formatter

- [ ] **Test:** JsonLinesFormatterTests
- [ ] **Impl:** JsonLinesFormatter implementation
- [ ] **Commit:** "feat(task-010b): implement JsonLinesFormatter"

### Phase 3.2: Event Types

- [ ] **Test:** EventTypeTests - All event types defined
- [ ] **Impl:** Event type records (MessageEvent, ProgressEvent, etc.)
- [ ] **Commit:** "feat(task-010b): implement event types"

### Task 010b Completion

- [ ] JSONL output complete
- [ ] Build succeeds with zero warnings
- [ ] All tests pass
- [ ] **Task 010b marked COMPLETE**

---

## Phase 4: Task 010c - Non-Interactive Mode

### Phase 4.1: TTY Detection

- [ ] **Test:** TtyDetectorTests
- [ ] **Impl:** TTY detector for stdin/stdout
- [ ] **Commit:** "feat(task-010c): implement TTY detection"

### Phase 4.2: Non-Interactive Behaviors

- [ ] **Test:** NonInteractiveModeTests - No prompts in non-TTY
- [ ] **Impl:** Non-interactive mode handler
- [ ] **Commit:** "feat(task-010c): implement non-interactive mode"

### Task 010c Completion

- [ ] Non-interactive mode complete
- [ ] Build succeeds with zero warnings
- [ ] All tests pass
- [ ] **Task 010c marked COMPLETE**

---

## Phase 5: Final Audit and PR

### Phase 5.1: Subtask Verification

- [ ] **Verify:** Task 010 parent COMPLETE
- [ ] **Verify:** Task 010a COMPLETE
- [ ] **Verify:** Task 010b COMPLETE
- [ ] **Verify:** Task 010c COMPLETE

### Phase 5.2: Comprehensive Audit

- [ ] **TDD Compliance:** Every source file has tests
- [ ] **Build Quality:** 0 errors, 0 warnings
- [ ] **Layer Boundaries:** Domain → Application → Infrastructure → CLI
- [ ] **Integration:** All interfaces implemented
- [ ] **Create:** docs/TASK-010-AUDIT.md

### Phase 5.3: Pull Request

- [ ] **Create:** PR with title "feat(epic-02): Task 010 - CLI Command Framework"
- [ ] **Include:** Summary, test coverage, audit link
- [ ] **Verify:** All tests pass on CI

---

## Progress Tracking

### Current Status
- **Phase:** Phase 1.1 - Core CLI Types
- **Next Action:** Implement ExitCode enum with tests

### Metrics
- **Commits:** 0
- **Tests Written:** 0
- **Tests Passing:** 0/0
- **Code Quality:** Not started

### Components Completed
- ❌ Task 010 Parent: CLI Infrastructure
- ❌ Task 010a: Command Routing + Help
- ❌ Task 010b: JSONL Event Stream Mode
- ❌ Task 010c: Non-Interactive Mode

### Next Steps
- Create feature branch
- Implement ExitCode enum (Phase 1.1)
- Continue with CLI infrastructure

---

## Notes

### Key Design Decisions
1. **System.CommandLine library** for command parsing (industry standard)
2. **Abstraction over output** (IOutputFormatter) for testability
3. **Configuration precedence** (CLI > Env > File > Defaults)
4. **TTY detection** for smart default behaviors

### Dependencies
- **Task 001:** Operating mode constraints
- **Task 002:** Configuration loading (.agent/config.yml)
- **Task 004:** Model provider interface (for `acode models` command)
- **Task 009:** Routing policy (for model selection)

---

**Last Updated:** 2026-01-06
