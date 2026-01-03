# Task 021.b: run show/logs/diff CLI Commands

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 021 (Artifact Collection), Task 050 (CLI), Task 011 (Workspace DB)  

---

## Description

Task 021.b implements CLI commands for inspecting past execution runs. Users MUST be able to view run history, read logs, and compare outputs. These commands provide visibility into execution behavior.

The `acode runs show` command MUST display run metadata. This includes start time, end time, exit code, command executed, and artifact paths. The Workspace DB (Task 011) MUST be the primary data source. Filesystem artifacts provide fallback when DB records are incomplete.

The `acode runs logs` command MUST stream stdout and stderr from artifact files. Users MUST be able to filter by stream (stdout only, stderr only, or both). Large logs MUST support pagination or tail modes.

The `acode runs diff` command MUST compare two runs. This enables detecting behavioral changes. Output differences MUST highlight additions and removals. Exit code differences MUST be clearly indicated.

All commands MUST respect Task 001 operating modes. In airgapped mode, commands MUST NOT attempt network operations. The commands MUST NOT require external services.

Integration with Task 021.a ensures artifact paths are predictable. The `.acode/artifacts/{run-id}/` structure MUST be assumed. Missing artifacts MUST produce clear error messages.

The CLI MUST provide machine-readable output via `--format json`. This enables scripting and automation. The default MUST be human-readable text.

Performance MUST be acceptable for repos with thousands of runs. Queries MUST use indexed fields. Log streaming MUST NOT load entire files into memory.

### Business Value

Run inspection enables debugging and auditing. When builds fail, users need access to previous outputs. Diff capabilities reveal what changed between successful and failing runs. This accelerates root cause analysis.

### Scope Boundaries

This task covers the CLI commands for run inspection. It does NOT cover the underlying storage (Task 021) or artifact structure (Task 021.a). Export functionality is covered by Task 021.c.

### Failure Modes

- Run ID not found in DB → Return exit code 1 with clear message
- Artifacts missing on disk → Return partial data with warning
- Invalid date filters → Return exit code 2 with usage help
- DB corruption → Suggest rebuild command

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Run | A single command execution with captured artifacts |
| Run ID | Unique identifier for a run (UUID or sequential) |
| Artifact | A file produced during execution (logs, results) |
| Workspace DB | SQLite database storing run metadata (Task 011) |
| Stdout | Standard output stream from executed command |
| Stderr | Standard error stream from executed command |
| Exit Code | Numeric return value from executed command |
| Diff | Comparison showing differences between two runs |
| Tail | Reading the last N lines of a log file |
| Pagination | Breaking large output into manageable pages |

---

## Out of Scope

- Run artifact storage (Task 021)
- Directory structure standards (Task 021.a)
- Export/bundle format (Task 021.c)
- Run deletion/cleanup
- Run re-execution
- Interactive log viewing (less/more style)
- Real-time log streaming during execution
- Remote run viewing

---

## Functional Requirements

### FR-001 to FR-020: `acode runs list` Command

- FR-001: `acode runs list` MUST list all runs in reverse chronological order
- FR-002: Output MUST include run ID, timestamp, exit code, command summary
- FR-003: `--limit N` MUST restrict output to N most recent runs
- FR-004: `--since DATE` MUST filter runs after specified date
- FR-005: `--until DATE` MUST filter runs before specified date
- FR-006: `--status success` MUST filter to exit code 0 runs
- FR-007: `--status failed` MUST filter to non-zero exit code runs
- FR-008: `--command PATTERN` MUST filter by command containing pattern
- FR-009: `--format json` MUST output JSON array
- FR-010: `--format table` MUST output ASCII table (default)
- FR-011: Empty results MUST display "No runs found" message
- FR-012: Date parsing MUST support ISO 8601 format
- FR-013: Date parsing MUST support relative formats (1h, 1d, 1w)
- FR-014: Output MUST truncate long commands to fit terminal width
- FR-015: `--no-truncate` MUST display full command text
- FR-016: Exit code 0 for successful query (even if no results)
- FR-017: Exit code 1 for query errors
- FR-018: Query MUST use Workspace DB indexes
- FR-019: Query MUST NOT load artifact contents
- FR-020: Query MUST complete in <100ms for <10000 runs

### FR-021 to FR-040: `acode runs show` Command

- FR-021: `acode runs show {run-id}` MUST display detailed run info
- FR-022: Output MUST include run ID
- FR-023: Output MUST include start timestamp
- FR-024: Output MUST include end timestamp
- FR-025: Output MUST include duration
- FR-026: Output MUST include exit code
- FR-027: Output MUST include command executed
- FR-028: Output MUST include working directory
- FR-029: Output MUST include environment variables (redacted secrets)
- FR-030: Output MUST include artifact paths
- FR-031: Output MUST indicate which artifacts exist on disk
- FR-032: `--format json` MUST output JSON object
- FR-033: `--format yaml` MUST output YAML document
- FR-034: Unknown run ID MUST return exit code 1
- FR-035: Unknown run ID MUST display "Run not found: {id}"
- FR-036: Partial UUID match MUST be supported (first 8 chars)
- FR-037: Ambiguous partial match MUST list matching runs
- FR-038: Environment redaction MUST mask patterns matching secrets
- FR-039: Environment redaction MUST mask known sensitive vars
- FR-040: Environment redaction MUST be configurable via config

### FR-041 to FR-060: `acode runs logs` Command

- FR-041: `acode runs logs {run-id}` MUST display stdout and stderr
- FR-042: `--stdout` MUST display only stdout
- FR-043: `--stderr` MUST display only stderr
- FR-044: Default MUST interleave stdout and stderr with prefixes
- FR-045: Prefix format MUST be `[stdout]` or `[stderr]`
- FR-046: `--no-prefix` MUST omit stream prefixes
- FR-047: `--tail N` MUST display only last N lines
- FR-048: `--head N` MUST display only first N lines
- FR-049: `--follow` MUST NOT be supported (runs are complete)
- FR-050: Missing log file MUST display "No {stream} captured"
- FR-051: Empty log file MUST display "(empty)"
- FR-052: Large logs MUST stream without full memory load
- FR-053: Binary content MUST be detected and rejected
- FR-054: Binary detection MUST check first 8KB for null bytes
- FR-055: `--timestamps` MUST prefix each line with timestamp if available
- FR-056: Exit code 0 for successful log display
- FR-057: Exit code 1 for run not found
- FR-058: Exit code 2 for log file missing
- FR-059: Logs MUST be read directly from artifact files
- FR-060: Log display MUST NOT require DB access beyond run lookup

### FR-061 to FR-080: `acode runs diff` Command

- FR-061: `acode runs diff {run-id-1} {run-id-2}` MUST compare runs
- FR-062: Exit code difference MUST be displayed
- FR-063: Duration difference MUST be displayed
- FR-064: Stdout diff MUST be displayed
- FR-065: Stderr diff MUST be displayed
- FR-066: `--stdout-only` MUST diff only stdout
- FR-067: `--stderr-only` MUST diff only stderr
- FR-068: `--exit-code-only` MUST show only exit code comparison
- FR-069: Diff format MUST use unified diff format
- FR-070: Added lines MUST be prefixed with `+`
- FR-071: Removed lines MUST be prefixed with `-`
- FR-072: Context lines MUST be configurable via `--context N`
- FR-073: Default context MUST be 3 lines
- FR-074: `--color` MUST enable ANSI color output
- FR-075: `--no-color` MUST disable color output
- FR-076: Color default MUST respect terminal detection
- FR-077: Exit code 0 if runs are identical
- FR-078: Exit code 1 if runs differ
- FR-079: Exit code 2 if either run not found
- FR-080: Large file diff MUST stream without full memory load

---

## Non-Functional Requirements

### NFR-001 to NFR-010: Performance

- NFR-001: `runs list` MUST complete in <100ms for <10000 runs
- NFR-002: `runs show` MUST complete in <50ms
- NFR-003: `runs logs` MUST start streaming in <20ms
- NFR-004: `runs diff` MUST start output in <100ms
- NFR-005: Memory usage MUST NOT exceed 50MB for any operation
- NFR-006: Log streaming MUST use <1MB buffer regardless of file size
- NFR-007: Diff MUST use streaming algorithm for large files
- NFR-008: DB queries MUST use prepared statements
- NFR-009: Index usage MUST be verified via query plan
- NFR-010: Cold start MUST NOT require full DB scan

### NFR-011 to NFR-020: Reliability

- NFR-011: Commands MUST NOT corrupt DB or artifacts
- NFR-012: Read operations MUST be safe for concurrent access
- NFR-013: Partial artifact availability MUST NOT crash commands
- NFR-014: DB connection MUST timeout after 5 seconds
- NFR-015: File read errors MUST produce clear error messages
- NFR-016: Ctrl+C MUST gracefully terminate output
- NFR-017: Pipe to closed process MUST NOT produce stack trace
- NFR-018: Commands MUST work without write permissions
- NFR-019: Locked artifact files MUST produce clear error
- NFR-020: UTF-8 decoding errors MUST be handled gracefully

### NFR-021 to NFR-030: Security

- NFR-021: Environment variables MUST be redacted in show output
- NFR-022: Redaction patterns MUST include PASSWORD, SECRET, KEY, TOKEN
- NFR-023: Redaction MUST apply to both name and value patterns
- NFR-024: Custom redaction patterns MUST be configurable
- NFR-025: Log content MUST NOT be modified (redaction only in metadata)
- NFR-026: File path MUST be validated against artifact directory
- NFR-027: Path traversal attempts MUST be blocked
- NFR-028: Symlink following MUST be disabled
- NFR-029: Commands MUST NOT execute arbitrary code
- NFR-030: JSON output MUST escape special characters

---

## User Manual Documentation

### Quick Start

```bash
# List recent runs
acode runs list

# Show details of a specific run
acode runs show abc123

# View logs from a run
acode runs logs abc123

# Compare two runs
acode runs diff abc123 def456
```

### Command Reference

#### `acode runs list`

Lists execution runs with summary information.

```bash
acode runs list [options]

Options:
  --limit N         Show only N most recent runs (default: 20)
  --since DATE      Filter runs after this date
  --until DATE      Filter runs before this date  
  --status STATUS   Filter by status: success, failed
  --command PATTERN Filter by command pattern
  --format FORMAT   Output format: table (default), json
  --no-truncate     Show full command text
```

**Examples:**

```bash
# List last 10 runs
acode runs list --limit 10

# List runs from the last hour
acode runs list --since 1h

# List only failed runs
acode runs list --status failed

# List runs of test commands
acode runs list --command "test"

# JSON output for scripting
acode runs list --format json | jq '.[].exitCode'
```

#### `acode runs show`

Displays detailed information about a specific run.

```bash
acode runs show <run-id> [options]

Options:
  --format FORMAT   Output format: text (default), json, yaml
```

**Examples:**

```bash
# Show run details
acode runs show abc12345

# Partial ID match (first 8 characters)
acode runs show abc12345

# JSON output
acode runs show abc12345 --format json
```

#### `acode runs logs`

Displays stdout and/or stderr from a run.

```bash
acode runs logs <run-id> [options]

Options:
  --stdout          Show only stdout
  --stderr          Show only stderr
  --no-prefix       Omit [stdout]/[stderr] prefixes
  --tail N          Show last N lines
  --head N          Show first N lines
  --timestamps      Include timestamps if available
```

**Examples:**

```bash
# View all output
acode runs logs abc12345

# View only errors
acode runs logs abc12345 --stderr

# View last 50 lines
acode runs logs abc12345 --tail 50
```

#### `acode runs diff`

Compares two runs to identify differences.

```bash
acode runs diff <run-id-1> <run-id-2> [options]

Options:
  --stdout-only     Compare only stdout
  --stderr-only     Compare only stderr
  --exit-code-only  Compare only exit codes
  --context N       Lines of context (default: 3)
  --color           Force color output
  --no-color        Disable color output
```

**Examples:**

```bash
# Full comparison
acode runs diff abc12345 def67890

# Compare only output
acode runs diff abc12345 def67890 --stdout-only

# More context
acode runs diff abc12345 def67890 --context 10
```

### Configuration

Configuration in `.agent/config.yml`:

```yaml
runs:
  list:
    defaultLimit: 20
    truncateWidth: 80
  show:
    redactPatterns:
      - PASSWORD
      - SECRET
      - KEY
      - TOKEN
      - API_KEY
  logs:
    defaultTail: 1000
    maxLineLength: 10000
  diff:
    defaultContext: 3
    colorEnabled: auto  # auto, always, never
```

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success / Runs identical (diff) |
| 1 | Run not found / Runs differ (diff) |
| 2 | Invalid arguments / Missing artifacts |
| 3 | Database error |

### Troubleshooting

**Q: "Run not found" but I just ran something**

The run may not be committed to the DB yet. Wait a moment and retry. If persists, check DB integrity:

```bash
acode db check
```

**Q: Logs show "(empty)" but command had output**

Output capture may have failed. Check if the command was run with capture disabled.

**Q: Diff shows no differences but exit codes differ**

Use `--exit-code-only` to see exit code comparison separately.

**Q: Getting "Binary content detected"**

The log file contains binary data. Use external tools to view:

```bash
hexdump -C .acode/artifacts/{run-id}/stdout.txt
```

---

## Acceptance Criteria / Definition of Done

### Functionality

- [ ] AC-001: `acode runs list` displays runs in reverse chronological order
- [ ] AC-002: `acode runs list --limit N` restricts to N runs
- [ ] AC-003: `acode runs list --since` filters by start date
- [ ] AC-004: `acode runs list --status` filters by exit code
- [ ] AC-005: `acode runs list --format json` outputs valid JSON
- [ ] AC-006: `acode runs show {id}` displays full run details
- [ ] AC-007: `acode runs show` partial ID match works
- [ ] AC-008: `acode runs show` redacts sensitive environment variables
- [ ] AC-009: `acode runs logs {id}` displays stdout and stderr
- [ ] AC-010: `acode runs logs --stdout` filters to stdout only
- [ ] AC-011: `acode runs logs --stderr` filters to stderr only
- [ ] AC-012: `acode runs logs --tail N` shows last N lines
- [ ] AC-013: `acode runs diff` shows unified diff format
- [ ] AC-014: `acode runs diff` exit code reflects similarity
- [ ] AC-015: `acode runs diff --color` enables color output

### Safety/Policy

- [ ] AC-016: Environment redaction masks PASSWORD patterns
- [ ] AC-017: Environment redaction masks SECRET patterns
- [ ] AC-018: Environment redaction masks KEY patterns
- [ ] AC-019: Environment redaction masks TOKEN patterns
- [ ] AC-020: Path traversal attempts are blocked
- [ ] AC-021: Symlinks outside artifact dir are not followed
- [ ] AC-022: Commands work in read-only filesystem
- [ ] AC-023: No arbitrary code execution paths exist

### CLI/UX

- [ ] AC-024: Commands provide `--help` documentation
- [ ] AC-025: Invalid options produce clear error messages
- [ ] AC-026: Exit codes follow documented conventions
- [ ] AC-027: Color output respects terminal detection
- [ ] AC-028: Long output is paginated appropriately
- [ ] AC-029: Ctrl+C terminates gracefully

### Logging/Audit

- [ ] AC-030: Commands log operation to debug log
- [ ] AC-031: Errors include run ID in log context
- [ ] AC-032: Performance metrics logged for slow operations

### Performance

- [ ] AC-033: `runs list` completes in <100ms for 10000 runs
- [ ] AC-034: `runs show` completes in <50ms
- [ ] AC-035: `runs logs` starts streaming in <20ms
- [ ] AC-036: Memory usage stays under 50MB

### Tests

- [ ] AC-037: Unit tests achieve 90% coverage
- [ ] AC-038: Integration tests cover DB interactions
- [ ] AC-039: E2E tests verify CLI behavior

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test RunListQuery with various filters
- [ ] UT-002: Test date parsing for ISO 8601
- [ ] UT-003: Test date parsing for relative formats
- [ ] UT-004: Test status filter mapping
- [ ] UT-005: Test command pattern matching
- [ ] UT-006: Test output truncation logic
- [ ] UT-007: Test partial ID matching
- [ ] UT-008: Test ambiguous ID detection
- [ ] UT-009: Test environment redaction patterns
- [ ] UT-010: Test stream prefix formatting
- [ ] UT-011: Test tail line extraction
- [ ] UT-012: Test head line extraction
- [ ] UT-013: Test binary content detection
- [ ] UT-014: Test unified diff generation
- [ ] UT-015: Test diff exit code logic

### Integration Tests

- [ ] IT-001: Test list query against populated DB
- [ ] IT-002: Test show query with real run data
- [ ] IT-003: Test logs reading from artifact files
- [ ] IT-004: Test diff with real artifact comparison
- [ ] IT-005: Test partial ID resolution
- [ ] IT-006: Test missing artifact handling
- [ ] IT-007: Test concurrent read access
- [ ] IT-008: Test large log file streaming
- [ ] IT-009: Test DB index usage
- [ ] IT-010: Test JSON output parsing

### End-to-End Tests

- [ ] E2E-001: Execute command, then list shows it
- [ ] E2E-002: Execute command, then show displays details
- [ ] E2E-003: Execute command, then logs displays output
- [ ] E2E-004: Execute two commands, then diff compares them
- [ ] E2E-005: Test full workflow with filtering
- [ ] E2E-006: Test scripting with JSON output
- [ ] E2E-007: Test error scenarios with clear messages
- [ ] E2E-008: Test with 1000+ runs in DB

### Performance/Benchmarks

- [ ] PB-001: `runs list` with 10000 runs in <100ms
- [ ] PB-002: `runs show` single lookup in <50ms
- [ ] PB-003: `runs logs` 100MB file streams in <5s
- [ ] PB-004: `runs diff` 10MB files completes in <3s
- [ ] PB-005: Memory stays under 50MB for all operations

### Regression

- [ ] RG-001: Verify Task 021 run storage compatibility
- [ ] RG-002: Verify Task 021.a path conventions
- [ ] RG-003: Verify Task 011 DB schema compatibility
- [ ] RG-004: Verify existing runs remain accessible

---

## User Verification Steps

1. **Verify list displays runs:**
   ```bash
   acode runs list
   ```
   Verify: Table shows run ID, timestamp, exit code, command

2. **Verify limit filter:**
   ```bash
   acode runs list --limit 5
   ```
   Verify: Exactly 5 runs displayed

3. **Verify status filter:**
   ```bash
   acode runs list --status failed
   ```
   Verify: Only non-zero exit code runs shown

4. **Verify show displays details:**
   ```bash
   acode runs show {run-id}
   ```
   Verify: Full run metadata displayed

5. **Verify partial ID:**
   ```bash
   acode runs show {first-8-chars}
   ```
   Verify: Run found with partial match

6. **Verify environment redaction:**
   ```bash
   acode runs show {run-id}
   ```
   Verify: PASSWORD variables show ********

7. **Verify logs display:**
   ```bash
   acode runs logs {run-id}
   ```
   Verify: Stdout and stderr displayed with prefixes

8. **Verify tail option:**
   ```bash
   acode runs logs {run-id} --tail 10
   ```
   Verify: Only last 10 lines shown

9. **Verify diff output:**
   ```bash
   acode runs diff {run-1} {run-2}
   ```
   Verify: Unified diff format with +/- prefixes

10. **Verify JSON output:**
    ```bash
    acode runs list --format json
    ```
    Verify: Valid JSON array returned

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Runs/
│       ├── RunId.cs
│       └── RunSummary.cs
├── Acode.Application/
│   └── Runs/
│       └── Queries/
│           ├── ListRunsQuery.cs
│           ├── GetRunQuery.cs
│           ├── GetRunLogsQuery.cs
│           └── DiffRunsQuery.cs
├── Acode.Infrastructure/
│   └── Runs/
│       ├── RunRepository.cs
│       └── ArtifactReader.cs
└── Acode.Cli/
    └── Commands/
        └── Runs/
            ├── RunsListCommand.cs
            ├── RunsShowCommand.cs
            ├── RunsLogsCommand.cs
            └── RunsDiffCommand.cs
```

### Core Interfaces

```csharp
public interface IRunRepository
{
    Task<IReadOnlyList<RunSummary>> ListAsync(RunListFilter filter);
    Task<RunDetails?> GetAsync(RunId id);
    Task<RunDetails?> GetByPartialIdAsync(string partialId);
}

public interface IArtifactReader
{
    Task<Stream?> OpenStdoutAsync(RunId id);
    Task<Stream?> OpenStderrAsync(RunId id);
    Task<bool> ExistsAsync(RunId id, string artifactName);
}

public record RunListFilter(
    int? Limit,
    DateTimeOffset? Since,
    DateTimeOffset? Until,
    int? ExitCode,
    string? CommandPattern);

public record RunSummary(
    RunId Id,
    DateTimeOffset StartTime,
    int ExitCode,
    string CommandPreview);

public record RunDetails(
    RunId Id,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    TimeSpan Duration,
    int ExitCode,
    string Command,
    string WorkingDirectory,
    IReadOnlyDictionary<string, string> Environment,
    IReadOnlyList<string> ArtifactPaths);
```

### Validation Checklist Before Merge

- [ ] All commands have `--help` output
- [ ] Exit codes match documentation
- [ ] Redaction patterns are configurable
- [ ] Performance benchmarks pass
- [ ] No stack traces on user errors
- [ ] JSON output is valid and parseable
- [ ] Partial ID matching tested
- [ ] Large file streaming tested
- [ ] Memory profiling completed

### Rollout Plan

1. Implement RunRepository queries
2. Implement ArtifactReader streaming
3. Implement CLI commands one at a time
4. Add integration tests with real DB
5. Add E2E tests for full workflows
6. Performance testing with large datasets
7. Documentation updates
8. Release as part of CLI bundle

---

**End of Task 021.b Specification**