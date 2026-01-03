# Task 001 Implementation Plan - Operating Modes & Hard Constraints

**Task:** Define Operating Modes & Hard Constraints
**Branch:** `feature/task-001-operating-modes`
**Started:** 2026-01-03
**Status:** In Progress

---

## Strategic Approach

Task 001 defines the three operating modes (LocalOnly, Burst, Airgapped) and establishes the hard constraints that enforce Acode's privacy-first architecture. This is implemented across three subtasks:

- **Task 001.a** - Define mode matrix (capabilities, permissions, mode-capability lookups)
- **Task 001.b** - Define validation rules (denylist, allowlist, endpoint validation)
- **Task 001.c** - Write constraints documentation and enforcement checklists

Following strict TDD with one commit per logical unit of work.

---

## Task 001.a - Mode Matrix

### Completed
✅ OperatingMode enum (3 values: LocalOnly, Burst, Airgapped)
  - File: `src/Acode.Domain/Modes/OperatingMode.cs`
  - Tests: `tests/Acode.Domain.Tests/Modes/OperatingModeTests.cs` (3 tests passing)
  - Commit: `18a4681` - feat(task-001a): implement OperatingMode enum

✅ Capability enum (27 capabilities across 5 categories)
  - File: `src/Acode.Domain/Modes/Capability.cs`
  - Tests: `tests/Acode.Domain.Tests/Modes/CapabilityTests.cs` (6 tests passing)
  - Commit: `fe3bf0a` - feat(task-001a): implement Capability enum

✅ Permission enum (5 permission levels)
  - File: `src/Acode.Domain/Modes/Permission.cs`
  - Tests: `tests/Acode.Domain.Tests/Modes/PermissionTests.cs` (7 tests passing)
  - Commit: `dfc5f3b` - feat(task-001a): implement Permission enum

✅ MatrixEntry record type
  - File: `src/Acode.Domain/Modes/MatrixEntry.cs`
  - Tests: `tests/Acode.Domain.Tests/Modes/MatrixEntryTests.cs` (5 tests passing)
  - Commit: `014c8ea` - feat(task-001a): implement MatrixEntry record

✅ ModeMatrix static class with all 81 entries
  - File: `src/Acode.Domain/Modes/ModeMatrix.cs`
  - Tests: `tests/Acode.Domain.Tests/Modes/ModeMatrixTests.cs` (12 tests passing)
  - Commit: `b15d3d1` - feat(task-001a): implement ModeMatrix with all 81 entries
  - HC-01, HC-02, HC-03 enforced
  - FrozenDictionary for O(1) lookups
  - Performance: < 1ms for 10k lookups

**TASK 001.A COMPLETE** ✅

---

## Task 001.b - Validation Rules

### In Progress
🔄 IEndpointValidator interface

### Remaining
- EndpointValidationResult record
- EndpointPattern record with pattern matching
- DenylistProvider with all major LLM APIs
- AllowlistProvider with localhost patterns
- EndpointValidator implementation
- HTTP client integration points
- Comprehensive tests for all validation scenarios

---

## Task 001.c - Constraints Documentation

### Remaining
- CONSTRAINTS.md at repository root
  - Quick reference table
  - All hard constraints (HC-01 through HC-07+)
  - Severity levels
  - Enforcement mechanisms
  - Compliance mapping
- Pull request template with checklist
- Architecture Decision Records (ADRs):
  - ADR-001: No External LLM API by default
  - ADR-002: Three Operating Modes
  - ADR-003: Airgapped mode permanence
  - ADR-004: Burst mode consent
  - ADR-005: Secrets redaction
- Security audit checklist
- Code documentation standards

---

## Test Coverage

### Current Coverage
- Task 001.a: 33 tests passing (OperatingMode: 3, Capability: 6, Permission: 7, MatrixEntry: 5, ModeMatrix: 12)
- Task 001.b: In progress
- Task 001.c: Not started

### Target Coverage
- Task 001.a: ✅ COMPLETE - 33/33 tests passing
- Task 001.b: 30+ unit tests for validation, 10+ integration tests
- Task 001.c: Documentation validation tests

---

## Build Status

✅ Build: 0 warnings, 0 errors
✅ Tests: 33/33 passing
✅ All commits pushed to remote
✅ Task 001.a COMPLETE

---

## Next Steps

1. Implement Capability enum with comprehensive XML docs
2. Implement Permission enum
3. Build ModeMatrix with all mode-capability combinations
4. Continue through 001.b and 001.c autonomously
5. Create PR when entire Task 001 (a, b, c) is complete

---

## Notes

- Following strict TDD (RED-GREEN-REFACTOR)
- One commit per logical unit (not waiting for user input between commits)
- All code references constraint IDs (HC-01, HC-02, etc.)
- Performance targets: < 1ms for all validation checks
