# Task 046.b: Runner CLI

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 12 – Hardening  
**Dependencies:** Task 046 (Benchmark Suite), Task 046.a (Specs), Task 030 (CLI Framework)  

---

## Description

Task 046.b implements the runner CLI—the command-line interface for executing benchmark tasks. The runner is how developers and CI pipelines execute the benchmark suite, single tasks, or filtered subsets. A good CLI makes the difference between a benchmark suite that gets used and one that collects dust.

The runner CLI provides: (1) commands to run suites, tasks, or categories, (2) options to control execution (timeout, parallelism, verbosity), (3) progress reporting during execution, (4) structured output for scripting, and (5) exit codes for CI integration.

### Business Value

Runner CLI provides:
- Developer accessibility
- CI integration
- Scripting support
- Progress visibility
- Flexible execution

### Scope Boundaries

This task covers the CLI interface. Core runner logic is Task 046. Spec format is Task 046.a. Results format is Task 046.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| CLI Framework | Task 030 | Command patterns | Parent |
| Benchmark Suite | Task 046 | Execution | Core |
| Specs | Task 046.a | Task definitions | Input |
| Results | Task 046.c | Output format | Output |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Invalid command | Parse error | Help shown | Guidance |
| Suite not found | File check | Error message | Cannot run |
| Task not found | Lookup | Error message | Cannot run |
| Execution error | Exception | Continue/abort | Partial results |
| Timeout | Timer | Abort task | Task failure |

### Assumptions

1. **CLI framework exists**: Task 030 complete
2. **Suites defined**: Task 046 complete
3. **Output defined**: Task 046.c complete
4. **Exit codes standard**: Unix conventions
5. **Terminal available**: Interactive or CI

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Runner | CLI execution tool |
| Suite | Task collection |
| Task | Single benchmark |
| Filter | Task selection |
| Progress | Execution status |
| Verbosity | Output detail level |
| Exit code | Process return value |
| Parallel | Concurrent execution |
| Dry run | Validation without execution |
| Watch | Continuous execution |

---

## Out of Scope

- Core execution logic (Task 046)
- Spec format (Task 046.a)
- Results format (Task 046.c)
- Scoring (Task 047)
- GUI interface
- Remote execution

---

## Functional Requirements

### FR-001 to FR-020: Commands

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046b-01 | `acode bench` command MUST exist | P0 |
| FR-046b-02 | `acode bench run` MUST run suite | P0 |
| FR-046b-03 | `acode bench run --suite <name>` | P0 |
| FR-046b-04 | `acode bench run --task <id>` | P0 |
| FR-046b-05 | `acode bench run --category <cat>` | P0 |
| FR-046b-06 | `acode bench run --tag <tag>` | P1 |
| FR-046b-07 | `acode bench list` MUST list suites | P0 |
| FR-046b-08 | `acode bench list --suite <name>` | P0 |
| FR-046b-09 | `acode bench validate` MUST work | P1 |
| FR-046b-10 | `acode bench validate --suite <name>` | P1 |
| FR-046b-11 | Default suite MUST be used | P0 |
| FR-046b-12 | Multiple filters MUST AND | P0 |
| FR-046b-13 | `--all` MUST run all tasks | P0 |
| FR-046b-14 | `--dry-run` MUST validate only | P1 |
| FR-046b-15 | Help MUST be available | P0 |
| FR-046b-16 | Help per command MUST work | P0 |
| FR-046b-17 | Version MUST be shown | P1 |
| FR-046b-18 | Unknown command MUST error | P0 |
| FR-046b-19 | Abbreviations MUST work | P2 |
| FR-046b-20 | Aliases MUST work | P2 |

### FR-021 to FR-040: Options

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046b-21 | `--timeout <duration>` MUST work | P0 |
| FR-046b-22 | `--parallel <n>` MAY work | P2 |
| FR-046b-23 | `--verbose` / `-v` MUST work | P0 |
| FR-046b-24 | Multiple `-v` = more verbose | P1 |
| FR-046b-25 | `--quiet` / `-q` MUST work | P0 |
| FR-046b-26 | `--output <path>` MUST work | P0 |
| FR-046b-27 | `--format <type>` MUST work | P0 |
| FR-046b-28 | Format: json | P0 |
| FR-046b-29 | Format: text | P0 |
| FR-046b-30 | Format: markdown | P1 |
| FR-046b-31 | `--no-progress` MUST work | P0 |
| FR-046b-32 | `--fail-fast` MUST work | P0 |
| FR-046b-33 | `--continue-on-error` default | P0 |
| FR-046b-34 | `--retry <n>` MUST work | P1 |
| FR-046b-35 | `--filter <pattern>` MUST work | P1 |
| FR-046b-36 | `--exclude <pattern>` MUST work | P1 |
| FR-046b-37 | `--seed <value>` for randomization | P2 |
| FR-046b-38 | `--shuffle` to randomize order | P2 |
| FR-046b-39 | `--config <file>` MUST work | P1 |
| FR-046b-40 | Environment override MUST work | P0 |

### FR-041 to FR-055: Progress

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046b-41 | Progress MUST be shown | P0 |
| FR-046b-42 | Current task MUST show | P0 |
| FR-046b-43 | Task count MUST show | P0 |
| FR-046b-44 | Completed count MUST show | P0 |
| FR-046b-45 | Pass/fail count MUST show | P0 |
| FR-046b-46 | Elapsed time MUST show | P0 |
| FR-046b-47 | ETA MAY show | P2 |
| FR-046b-48 | Progress bar MUST show | P0 |
| FR-046b-49 | Task result MUST show inline | P0 |
| FR-046b-50 | ✓ for pass | P0 |
| FR-046b-51 | ✗ for fail | P0 |
| FR-046b-52 | ⏱ for timeout | P0 |
| FR-046b-53 | ⊘ for skip | P0 |
| FR-046b-54 | Color output MUST work | P1 |
| FR-046b-55 | `--no-color` MUST work | P0 |

### FR-056 to FR-065: Exit Codes

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-046b-56 | Exit 0 = all passed | P0 |
| FR-046b-57 | Exit 1 = some failed | P0 |
| FR-046b-58 | Exit 2 = execution error | P0 |
| FR-046b-59 | Exit 3 = invalid input | P0 |
| FR-046b-60 | Exit 4 = suite not found | P0 |
| FR-046b-61 | Exit 5 = cancelled | P0 |
| FR-046b-62 | Exit code MUST be documented | P0 |
| FR-046b-63 | `--ignore-exit` MAY exist | P2 |
| FR-046b-64 | Partial exit codes MAY exist | P2 |
| FR-046b-65 | Exit code MUST be consistent | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-046b-01 | CLI startup | <100ms | P0 |
| NFR-046b-02 | Command parse | <10ms | P0 |
| NFR-046b-03 | Suite load | <500ms | P0 |
| NFR-046b-04 | Progress update | <50ms | P0 |
| NFR-046b-05 | Result write | <100ms | P0 |
| NFR-046b-06 | Help display | <50ms | P0 |
| NFR-046b-07 | Memory usage | <50MB | P0 |
| NFR-046b-08 | Cancel response | <500ms | P0 |
| NFR-046b-09 | Large output | Streamed | P0 |
| NFR-046b-10 | Parallel overhead | <10% | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-046b-11 | Cross-platform | All OS | P0 |
| NFR-046b-12 | Shell compatibility | bash/zsh/ps | P0 |
| NFR-046b-13 | CI compatibility | Major CI | P0 |
| NFR-046b-14 | Interrupt handling | SIGINT | P0 |
| NFR-046b-15 | Partial results | Saved | P0 |
| NFR-046b-16 | Error messages | Clear | P0 |
| NFR-046b-17 | Suggestion on error | When possible | P1 |
| NFR-046b-18 | Cleanup on exit | Always | P0 |
| NFR-046b-19 | No terminal = no progress | Handled | P0 |
| NFR-046b-20 | Pipe output | Works | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-046b-21 | Command logged | Info | P0 |
| NFR-046b-22 | Options logged | Debug | P0 |
| NFR-046b-23 | Errors logged | Error | P0 |
| NFR-046b-24 | Exit code logged | Info | P0 |
| NFR-046b-25 | Duration logged | Info | P0 |
| NFR-046b-26 | Structured logging | JSON | P0 |
| NFR-046b-27 | Trace ID | Per run | P1 |
| NFR-046b-28 | Metrics: runs | Counter | P1 |
| NFR-046b-29 | Metrics: pass rate | Gauge | P0 |
| NFR-046b-30 | Metrics: duration | Histogram | P1 |

---

## Acceptance Criteria / Definition of Done

### Commands
- [ ] AC-001: bench command exists
- [ ] AC-002: run command works
- [ ] AC-003: list command works
- [ ] AC-004: validate command works
- [ ] AC-005: suite filter works
- [ ] AC-006: task filter works
- [ ] AC-007: category filter works
- [ ] AC-008: help works

### Options
- [ ] AC-009: timeout works
- [ ] AC-010: verbose works
- [ ] AC-011: quiet works
- [ ] AC-012: output works
- [ ] AC-013: format works
- [ ] AC-014: fail-fast works
- [ ] AC-015: retry works
- [ ] AC-016: filter works

### Progress
- [ ] AC-017: Progress shown
- [ ] AC-018: Current task
- [ ] AC-019: Pass/fail count
- [ ] AC-020: Progress bar
- [ ] AC-021: Symbols work
- [ ] AC-022: Color works
- [ ] AC-023: No-color works
- [ ] AC-024: No-progress works

### Exit Codes
- [ ] AC-025: Exit 0 = pass
- [ ] AC-026: Exit 1 = fail
- [ ] AC-027: Exit 2 = error
- [ ] AC-028: Documented
- [ ] AC-029: Cross-platform
- [ ] AC-030: CI works
- [ ] AC-031: Tests pass
- [ ] AC-032: Documented

---

## User Verification Scenarios

### Scenario 1: Run Full Suite
**Persona:** Developer  
**Preconditions:** Suite exists  
**Steps:**
1. Run `acode bench run`
2. Progress shown
3. Results displayed
4. Exit code reflects outcome

**Verification Checklist:**
- [ ] Command works
- [ ] Progress visible
- [ ] Results correct
- [ ] Exit code correct

### Scenario 2: Run Single Task
**Persona:** Developer  
**Preconditions:** Task exists  
**Steps:**
1. Run `acode bench run --task BENCH-001`
2. Single task runs
3. Result shown
4. Details available

**Verification Checklist:**
- [ ] Task runs
- [ ] Result shown
- [ ] Details available
- [ ] Exit correct

### Scenario 3: Filter by Category
**Persona:** Developer  
**Preconditions:** Category exists  
**Steps:**
1. Run `acode bench run --category file-ops`
2. Only file-ops run
3. Count matches
4. Results correct

**Verification Checklist:**
- [ ] Filter works
- [ ] Correct tasks
- [ ] Count matches
- [ ] Results correct

### Scenario 4: Fail Fast
**Persona:** Developer in CI  
**Preconditions:** Suite with failures  
**Steps:**
1. Run `acode bench run --fail-fast`
2. First failure stops
3. Exit code 1
4. Time saved

**Verification Checklist:**
- [ ] Stops on first
- [ ] Exit code 1
- [ ] Time saved
- [ ] Clear message

### Scenario 5: JSON Output
**Persona:** CI Pipeline  
**Preconditions:** Suite exists  
**Steps:**
1. Run `acode bench run --format json`
2. JSON output
3. Parse output
4. Process results

**Verification Checklist:**
- [ ] JSON valid
- [ ] Parseable
- [ ] All data present
- [ ] No noise

### Scenario 6: Quiet Mode
**Persona:** CI Pipeline  
**Preconditions:** Suite exists  
**Steps:**
1. Run `acode bench run -q`
2. Minimal output
3. Exit code only
4. Logs available

**Verification Checklist:**
- [ ] Minimal output
- [ ] Exit code correct
- [ ] Logs exist
- [ ] Scriptable

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-046b-01 | Command parsing | FR-046b-01 |
| UT-046b-02 | Suite option | FR-046b-03 |
| UT-046b-03 | Task option | FR-046b-04 |
| UT-046b-04 | Category option | FR-046b-05 |
| UT-046b-05 | Timeout parsing | FR-046b-21 |
| UT-046b-06 | Verbose levels | FR-046b-24 |
| UT-046b-07 | Output path | FR-046b-26 |
| UT-046b-08 | Format option | FR-046b-27 |
| UT-046b-09 | Exit code 0 | FR-046b-56 |
| UT-046b-10 | Exit code 1 | FR-046b-57 |
| UT-046b-11 | Exit code 2 | FR-046b-58 |
| UT-046b-12 | Help generation | FR-046b-15 |
| UT-046b-13 | Filter parsing | FR-046b-35 |
| UT-046b-14 | Progress symbols | FR-046b-50 |
| UT-046b-15 | Color detection | FR-046b-54 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-046b-01 | Full run E2E | E2E |
| IT-046b-02 | Single task | FR-046b-04 |
| IT-046b-03 | Category filter | FR-046b-05 |
| IT-046b-04 | Fail fast | FR-046b-32 |
| IT-046b-05 | JSON output | FR-046b-28 |
| IT-046b-06 | Text output | FR-046b-29 |
| IT-046b-07 | Cancel | FR-046b-61 |
| IT-046b-08 | Progress display | FR-046b-41 |
| IT-046b-09 | Cross-platform | NFR-046b-11 |
| IT-046b-10 | CI mode | NFR-046b-13 |
| IT-046b-11 | Pipe output | NFR-046b-20 |
| IT-046b-12 | Logging | NFR-046b-21 |
| IT-046b-13 | Suite not found | FR-046b-60 |
| IT-046b-14 | Retry | FR-046b-34 |
| IT-046b-15 | Dry run | FR-046b-14 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Cli/
│   └── Commands/
│       └── Benchmark/
│           ├── BenchCommand.cs
│           ├── BenchRunCommand.cs
│           ├── BenchListCommand.cs
│           ├── BenchValidateCommand.cs
│           └── BenchProgressReporter.cs
├── Acode.Application/
│   └── Evaluation/
│       └── RunnerOptions.cs
```

### CLI Usage

```
acode bench run [options]

Options:
  --suite <name>          Suite to run (default: default)
  --task <id>             Run specific task
  --category <cat>        Filter by category
  --tag <tag>             Filter by tag
  --timeout <duration>    Override timeout (e.g., 30s, 5m)
  --output <path>         Output file path
  --format <type>         Output format: json|text|markdown
  -v, --verbose           Increase verbosity
  -q, --quiet             Minimal output
  --fail-fast             Stop on first failure
  --no-progress           Disable progress display
  --no-color              Disable color output
  --dry-run               Validate without running

Exit Codes:
  0  All tasks passed
  1  Some tasks failed
  2  Execution error
  3  Invalid input
  4  Suite not found
  5  Cancelled
```

**End of Task 046.b Specification**
