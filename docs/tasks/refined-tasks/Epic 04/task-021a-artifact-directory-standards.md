# Task 021.a: Artifact Directory Standards

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 021 (Artifact Collection)  

---

## Description

### Overview

Task 021.a establishes standardized artifact directory structures for the agentic coding bot. All execution artifacts—stdout/stderr captures, test results, build logs, coverage reports—MUST be stored in predictable, consistent locations. This standardization enables tooling automation, historical analysis, diffing between runs, and artifact export functionality defined in sibling tasks.

### Business Value

1. **Predictable Artifact Locations**: Tooling and scripts can reliably find artifacts without searching
2. **Run Isolation**: Each execution has its own directory, preventing collisions and enabling comparison
3. **Historical Audit Trail**: Stored artifacts support debugging, compliance, and trend analysis
4. **Export Enablement**: Consistent structure enables the export bundle functionality (Task 021.c)
5. **Disk Management**: Defined retention policies prevent unbounded disk growth
6. **Developer Experience**: Clear structure reduces cognitive load when inspecting results

### Scope

This task encompasses:

1. **Base Directory Definition**: Standard path for all artifacts (`.acode/artifacts/`)
2. **Run Directory Structure**: Per-run subdirectories with unique IDs
3. **Standard File Names**: Consistent naming for stdout, stderr, logs, test results
4. **Metadata Schema**: Run metadata JSON structure for tracking execution context
5. **Directory Creation**: Automatic creation of required directories
6. **Path Resolution API**: Programmatic access to artifact paths
7. **Retention Policy**: Disk limit enforcement and pruning rules
8. **Gitignore Integration**: Ensuring artifacts don't pollute version control

### Integration Points

| Component | Integration Type | Data Flow |
|-----------|------------------|-----------|
| TaskExecutionService | Artifact Storage | Executor → ArtifactWriter |
| TestRunner | Test Results | TestRunner → ArtifactWriter |
| ContainerLifecycleManager | Stdout/Stderr | Container → ArtifactWriter |
| CLI Commands | Artifact Retrieval | ArtifactReader → CLI |
| Export Bundle | Bundle Creation | ArtifactReader → Exporter |
| Disk Manager | Retention | RetentionPolicy → Pruner |

### Failure Modes

| Failure Mode | Detection | Recovery |
|--------------|-----------|----------|
| Disk full | Write exception | Prune old runs, alert user |
| Permission denied | Write exception | Log error, suggest remediation |
| Run ID collision | Directory exists | Use timestamped suffix |
| Corrupted metadata | JSON parse failure | Recreate from available artifacts |
| Missing base directory | Path not found | Auto-create with proper permissions |

### Assumptions

- Filesystem supports required directory depth
- User has write permissions to project directory
- Run IDs are unique or can be made unique
- Sufficient disk space exists for artifact storage
- `.gitignore` patterns are respected by version control

### Artifact Categories

| Category | File Pattern | Description |
|----------|--------------|-------------|
| Output | stdout.txt, stderr.txt | Process output streams |
| Logs | build.log, restore.log | Tool-specific logs |
| Test Results | test-results.json, test-results.trx | Test execution results |
| Coverage | coverage.json, lcov.info | Code coverage data |
| Metadata | run.json | Execution context and summary |
| Custom | *.txt, *.json | Task-specific artifacts |

---

## Use Cases

### Use Case 1: DevBot Automated Testing Artifact Inspection

**Persona:** DevBot (CI/CD Automation Agent)
**Scenario:** Test suite fails in CI, developer needs to inspect test results and logs

**Before Standardized Directory Structure:**
- Test outputs scattered: some in `./test-results/`, some in `./output/`, some in `/tmp/`
- Each test framework uses different directory structure
- Developer searches 4-5 different locations to find artifacts
- Time wasted: 5 minutes per test failure × 20 failures/day = 100 minutes/day
- Cost: 100 min/day × 21 days/month × $75/hour ÷ 60 min/hour = $2,625/month

**After Standardized Directory Structure:**
- All artifacts in `.acode/artifacts/{run-id}/`
- Predictable locations: `stdout.txt`, `stderr.txt`, `test-results.trx`
- Developer runs: `acode artifact list {run-id}` to see all artifacts
- Single command to view: `acode artifact cat {run-id} test-results.trx`
- Time wasted: 30 seconds per failure × 20 failures/day = 10 minutes/day
- Cost: 10 min/day × 21 days/month × $75/hour ÷ 60 min/hour = $262.50/month

**Savings:** $2,362.50/month per developer, $28,350/year

**Commands Used:**
```bash
# CI run fails, developer inspects
acode run list --status failed --last 1h
# Returns: run-abc-123 (2024-12-01 14:32:15, exit 1)

# List all artifacts for the failed run
acode artifact list run-abc-123
# Returns: stdout.txt, stderr.txt, test-results.trx, coverage.json

# View test results directly
acode artifact cat run-abc-123 test-results.trx
# Shows XML test results with failures highlighted

# View stderr for error details
acode artifact cat run-abc-123 stderr.txt
# Shows stack traces and error messages
```

---

### Use Case 2: Jordan Compliance Audit Trail Verification

**Persona:** Jordan (Security Compliance Officer)
**Scenario:** SOC 2 audit requires proof that all agent operations are logged and artifacts retained

**Before Standardized Directory Structure:**
- Audit log files mixed with application logs
- No clear mapping between runs and their outputs
- Manual reconstruction of "what happened when" from scattered logs
- Audit preparation time: 60 hours per audit
- Cost: 60 hours × $100/hour = $6,000 per audit

**After Standardized Directory Structure:**
- Each run has isolated directory with complete audit trail
- Metadata file (`run.json`) contains execution context, timing, command
- Artifacts immutable once written (append-only)
- Directory structure serves as self-documenting audit trail
- Audit preparation time: 3 hours per audit (automated export)
- Cost: 3 hours × $100/hour = $300 per audit

**Savings:** $5,700 per audit, $5,700/year (1 audit/year)

**Audit Workflow:**
```bash
# Export all runs from audit period
acode run list --from 2024-01-01 --to 2024-12-31 --format jsonl > audit-2024.jsonl

# Verify artifact completeness
for run_id in $(jq -r '.run_id' audit-2024.jsonl); do
  acode artifact verify $run_id || echo "AUDIT FAILURE: $run_id incomplete"
done

# Check directory structure compliance
ls -la .acode/artifacts/ | head -20
# Expected: Each directory is run-XXXXX with predictable structure

# Sample random run for detailed inspection
random_run=$(jq -r '.run_id' audit-2024.jsonl | shuf -n 1)
tree .acode/artifacts/$random_run/
# Expected: stdout.txt, stderr.txt, run.json, plus task-specific artifacts

# Verify metadata integrity
jq . .acode/artifacts/$random_run/run.json
# Expected: Complete execution context with timestamps, command, exit code
```

---

### Use Case 3: Alex Historical Performance Analysis

**Persona:** Alex (DevOps Performance Engineer)
**Scenario:** Analyze build performance trends over time to identify optimization opportunities

**Before Standardized Directory Structure:**
- Build logs in different formats across different periods
- No consistent metadata schema
- Manual parsing of logs to extract timing information
- Analysis script breaks when log format changes
- Analysis time: 2 days per month
- Cost: 16 hours × $125/hour = $2,000/month

**After Standardized Directory Structure:**
- Every run has `run.json` with consistent schema: start_time, end_time, duration_ms
- Artifacts in predictable locations enable automated analysis
- Historical data queryable without parsing log formats
- Analysis script works reliably across all runs
- Analysis time: 2 hours per month (automated)
- Cost: 2 hours × $125/hour = $250/month

**Savings:** $1,750/month, $21,000/year per engineer

**Analysis Workflow:**
```bash
# Extract build durations from last 90 days
for run_dir in .acode/artifacts/run-*; do
  if [[ -f "$run_dir/run.json" ]]; then
    jq -r '[.run_id, .command, .duration_ms] | @csv' "$run_dir/run.json"
  fi
done > build-durations.csv

# Analyze with standard tools
cat build-durations.csv | \
  awk -F',' '$2 ~ /dotnet build/ {sum+=$3; count++} END {print "Avg build time:", sum/count/1000 "s"}'

# Identify slowest builds
sort -t',' -k3 -n build-durations.csv | tail -10
# Returns: Top 10 slowest builds with run IDs for detailed inspection

# Deep-dive into slowest build
slowest_run=$(sort -t',' -k3 -n build-durations.csv | tail -1 | cut -d',' -f1)
acode artifact cat $slowest_run stdout.txt | grep "Time Elapsed"
# Shows MSBuild timing breakdown for optimization targets
```

---

### Aggregate ROI Summary

| Use Case | Persona | Annual Savings | Team Savings (10) |
|----------|---------|----------------|-------------------|
| Test Artifact Inspection | DevBot | $28,350 | $283,500 |
| Compliance Audit Trail | Jordan | $5,700 | $5,700 |
| Performance Analysis | Alex | $21,000 | $210,000 |
| **Total** | | **$55,050** | **$499,200** |

---

## Glossary

| Term | Definition |
|------|------------|
| **Artifact Directory** | Root directory `.acode/artifacts/` containing all run subdirectories. Must be in `.gitignore`. Created automatically on first use with 755 permissions. |
| **Run Directory** | Subdirectory `.acode/artifacts/{run-id}/` containing all artifacts for a single command execution. Named using unique run identifier (UUID/ULID). |
| **Run ID** | Unique identifier for a command execution. Format: `run-01HQRS7TGKMWXY123` (ULID) or UUID v4. Used as directory name and database primary key. |
| **Artifact** | Output file produced during command execution: logs, test results, coverage reports, build outputs. Stored in run directory with standard names. |
| **Standard Artifact** | Well-known artifact with reserved name: `stdout.txt` (process output), `stderr.txt` (error output), `run.json` (metadata), `combined.txt` (interleaved streams). |
| **Custom Artifact** | Task-specific artifact beyond standard set: test results XML, coverage JSON, build logs, generated code. Stored alongside standard artifacts. |
| **Metadata File** | JSON file `run.json` in each run directory containing execution context: command, timestamps, exit code, environment variables, workspace info. |
| **Retention Policy** | Rules determining when artifacts are deleted: age-based (30 days), count-based (max 10,000 runs), size-based (max 50GB), status-based (keep failures longer). |
| **Artifact Manifest** | Optional JSON file listing all artifacts in run directory with checksums (SHA-256), sizes, timestamps. Enables integrity verification and export. |
| **Path Resolution** | Process of converting relative artifact references to absolute filesystem paths. Handled by `IArtifactPathResolver` service. Cross-platform (Windows/Linux/Mac). |
| **Run Isolation** | Guarantee that artifacts from different runs never interfere. Each run has isolated directory preventing overwrites, race conditions, or cross-contamination. |
| **Artifact Pruning** | Automated deletion of old artifacts according to retention policy. Runs as scheduled background job. Removes run directory and database record atomically. |
| **Workspace Scoping** | Artifacts are scoped to workspace: different workspaces have separate artifact directories. Path: `{workspace-root}/.acode/artifacts/`. |
| **Artifact Collection** | Automatic capture of command outputs during execution. Implemented by `ArtifactCollector` service that streams stdout/stderr to files as process runs. |
| **Directory Layout** | Standard structure within run directory: flat (all files at root) vs nested (organized by type: `logs/`, `tests/`, `coverage/`). This task specifies flat layout. |
| **Gitignore Integration** | Ensuring `.acode/artifacts/` directory is excluded from version control. Agent adds entry to `.gitignore` on first run if not present. |
| **Artifact Reference** | Pointer from run record (database) to artifact file (filesystem). Includes relative path, size, content type, checksum. Stored in run metadata. |
| **Run Summary** | Lightweight view of run record with essential fields: run ID, timestamp, exit code, command. Used for listing runs without loading full artifacts. |

---

## Out of Scope

This task explicitly does NOT include the following capabilities (deferred to other tasks or future work):

1. **Artifact Compression** — No automatic gzip/brotli compression of artifacts. Files stored uncompressed. Compression happens only in export bundles (Task 021c).
2. **Artifact Deduplication** — No content-addressed storage or deduplication across runs. Each run stores complete artifacts even if identical to previous runs.
3. **Artifact Encryption** — No encryption of artifacts at rest. Use filesystem-level encryption (LUKS, BitLocker, FileVault) if needed. Export bundles (Task 021c) can be password-protected.
4. **Distributed Storage** — No object storage (S3, Azure Blob, GCS) integration. All artifacts stored on local filesystem. Cloud storage deferred to Epic 07 (Cloud Burst Compute).
5. **Artifact Streaming** — No real-time streaming of artifacts during execution (like `docker logs -f`). Artifacts written to files, then displayed after completion.
6. **Artifact Search Content** — No full-text search inside artifact files. Search by run metadata only (command, timestamp, status). Content search deferred to Epic 03 (Repo Intelligence).
7. **Artifact Annotations** — No user comments, tags, or labels on individual artifacts. Annotations on run records only, via metadata JSON.
8. **Artifact Versioning** — No version history for artifacts. Each run creates new artifacts; no updates or patches to existing artifacts.
9. **Artifact Access Control** — No fine-grained permissions per artifact or per run. Workspace-level access only. ACLs deferred to Epic 09 (Safety/Policy).
10. **Artifact Preview** — No syntax highlighting, image thumbnails, PDF rendering in CLI. Use `acode artifact cat` and pipe to external viewers (`bat`, `imgcat`, `pdftotext`).
11. **Artifact Diff UI** — No built-in diff visualization between artifacts from different runs. Export artifacts and use external diff tools (`diff`, `vimdiff`, `Beyond Compare`).
12. **Artifact Download Protocol** — No HTTP API or download protocol for remote artifact access. Local filesystem only. REST API deferred to Epic 10 (Telemetry/Dashboard).
13. **Artifact Quota Enforcement** — No per-user or per-workspace storage quotas. Global retention policy only. User quotas deferred to Epic 09 (Policy Engine).
14. **Artifact Lifecycle Hooks** — No custom scripts triggered on artifact creation, deletion, or access. Hooks deferred to Epic 08 (CI/CD Authoring).
15. **Artifact Replication** — No automatic replication to backup locations or secondary storage. Manual backup via export bundles (Task 021c) or filesystem tools (`rsync`, `tar`).

---

## Functional Requirements

### Base Directory Structure

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-021A-01 | Base artifact directory MUST be `.acode/artifacts/` | MUST |
| FR-021A-02 | Base directory MUST be relative to repository root | MUST |
| FR-021A-03 | Base directory MUST be created automatically on first use | MUST |
| FR-021A-04 | Base directory MUST have `.gitignore` entry to exclude from VCS | MUST |
| FR-021A-05 | System MUST verify base directory is writable | MUST |
| FR-021A-06 | System MUST support configurable base path via config | SHOULD |
| FR-021A-07 | Base directory permissions MUST be 755 (owner write) | MUST |
| FR-021A-08 | System MUST log base directory location at startup | MUST |

### Run Directory Structure

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-021A-09 | Each run MUST have directory `.acode/artifacts/{run-id}/` | MUST |
| FR-021A-10 | Run ID MUST be unique within the artifact store | MUST |
| FR-021A-11 | Run ID format MUST be `{task-name}-{timestamp}` | MUST |
| FR-021A-12 | Timestamp format MUST be `YYYYMMDD-HHmmss` | MUST |
| FR-021A-13 | Run ID collision MUST append sequence number | MUST |
| FR-021A-14 | Run directory MUST be created before any artifact write | MUST |
| FR-021A-15 | Run directory permissions MUST be 755 | MUST |
| FR-021A-16 | System MUST log run directory creation | MUST |

### Standard File Names

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-021A-17 | Stdout MUST be stored as `stdout.txt` | MUST |
| FR-021A-18 | Stderr MUST be stored as `stderr.txt` | MUST |
| FR-021A-19 | Combined output MUST be stored as `output.txt` | SHOULD |
| FR-021A-20 | Build log MUST be stored as `build.log` | MUST |
| FR-021A-21 | Restore log MUST be stored as `restore.log` | SHOULD |
| FR-021A-22 | Test results MUST be stored as `test-results.json` | MUST |
| FR-021A-23 | Test results TRX MUST be stored as `test-results.trx` | SHOULD |
| FR-021A-24 | Coverage data MUST be stored as `coverage.json` | SHOULD |
| FR-021A-25 | Run metadata MUST be stored as `run.json` | MUST |
| FR-021A-26 | Error details MUST be stored as `error.json` when failure | SHOULD |

### Metadata Schema

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-021A-27 | Run metadata MUST include run ID | MUST |
| FR-021A-28 | Run metadata MUST include task name | MUST |
| FR-021A-29 | Run metadata MUST include start timestamp (ISO 8601) | MUST |
| FR-021A-30 | Run metadata MUST include end timestamp | MUST |
| FR-021A-31 | Run metadata MUST include duration in milliseconds | MUST |
| FR-021A-32 | Run metadata MUST include exit code | MUST |
| FR-021A-33 | Run metadata MUST include status (success/failure/cancelled) | MUST |
| FR-021A-34 | Run metadata MUST include operating mode | MUST |
| FR-021A-35 | Run metadata SHOULD include command executed | SHOULD |
| FR-021A-36 | Run metadata SHOULD include container ID if sandboxed | SHOULD |
| FR-021A-37 | Run metadata SHOULD include artifact file list | SHOULD |
| FR-021A-38 | Run metadata MUST be valid JSON | MUST |

### Path Resolution API

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-021A-39 | System MUST provide `GetBaseDirectory()` method | MUST |
| FR-021A-40 | System MUST provide `GetRunDirectory(runId)` method | MUST |
| FR-021A-41 | System MUST provide `GetArtifactPath(runId, filename)` method | MUST |
| FR-021A-42 | System MUST provide standard path constants | MUST |
| FR-021A-43 | Path resolution MUST handle cross-platform separators | MUST |
| FR-021A-44 | Path resolution MUST return absolute paths | MUST |
| FR-021A-45 | System MUST validate paths before write | MUST |
| FR-021A-46 | Path resolution MUST be thread-safe | MUST |

### Retention and Cleanup

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-021A-47 | System MUST track total artifact storage size | MUST |
| FR-021A-48 | System MUST enforce configurable size limit (default 1GB) | MUST |
| FR-021A-49 | System MUST prune oldest runs when limit exceeded | MUST |
| FR-021A-50 | Pruning MUST remove entire run directories | MUST |
| FR-021A-51 | System MUST support `artifacts prune` CLI command | MUST |
| FR-021A-52 | Prune MUST confirm before deletion (unless --force) | MUST |
| FR-021A-53 | System SHOULD support retention by count (keep last N) | SHOULD |
| FR-021A-54 | System SHOULD support retention by age (keep last N days) | SHOULD |
| FR-021A-55 | Pruned runs MUST be logged | MUST |
| FR-021A-56 | System MUST handle corrupted/incomplete runs during prune | MUST |

### Artifact Writing

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-021A-57 | System MUST write artifacts atomically | MUST |
| FR-021A-58 | Partial writes MUST NOT leave corrupted artifacts | MUST |
| FR-021A-59 | System MUST support streaming writes for stdout/stderr | MUST |
| FR-021A-60 | System MUST support appending to existing files | MUST |
| FR-021A-61 | System MUST flush writes on task completion | MUST |
| FR-021A-62 | Write failures MUST be logged with details | MUST |
| FR-021A-63 | System MUST support custom artifact registration | SHOULD |
| FR-021A-64 | Large files SHOULD use buffered writing | SHOULD |

### Directory Structure

```
.acode/
├── artifacts/
│   ├── build-20240120-143052/
│   │   ├── run.json           # Metadata
│   │   ├── stdout.txt         # Standard output
│   │   ├── stderr.txt         # Standard error
│   │   └── build.log          # Build-specific log
│   ├── test-20240120-143215/
│   │   ├── run.json
│   │   ├── stdout.txt
│   │   ├── stderr.txt
│   │   ├── test-results.json  # Test results
│   │   ├── test-results.trx   # TRX format
│   │   └── coverage.json      # Coverage data
│   └── task-20240120-144000/
│       ├── run.json
│       ├── stdout.txt
│       ├── stderr.txt
│       └── error.json         # Error details (on failure)
└── .gitignore                 # Excludes artifacts/
```

---

## Non-Functional Requirements

### Performance

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-021A-01 | Path resolution | < 1ms |
| NFR-021A-02 | Directory creation | < 50ms |
| NFR-021A-03 | Metadata write | < 10ms |
| NFR-021A-04 | Size calculation (full store) | < 2s |
| NFR-021A-05 | Prune operation (per directory) | < 100ms |
| NFR-021A-06 | Streaming write throughput | > 100MB/s |
| NFR-021A-07 | Memory overhead per write | < 64KB buffer |

### Reliability

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-021A-08 | Artifact write success rate | 99.9% |
| NFR-021A-09 | Atomic writes prevent corruption | 100% |
| NFR-021A-10 | Run ID uniqueness guarantee | 100% |
| NFR-021A-11 | Graceful handling of disk full | Logged error |
| NFR-021A-12 | Recovery from partial writes | Auto-cleanup |
| NFR-021A-13 | Cross-platform path handling | Windows + Linux |

### Security

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-021A-14 | Artifacts readable by owner only | Configurable |
| NFR-021A-15 | Path traversal prevention | Validated |
| NFR-021A-16 | No secrets in artifact paths | Sanitized |
| NFR-021A-17 | Symlinks not followed outside boundary | Blocked |

### Maintainability

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-021A-18 | Artifact path code coverage | ≥ 95% |
| NFR-021A-19 | Path constants centralized | Single file |
| NFR-021A-20 | Extensible for new artifact types | Plugin ready |
| NFR-021A-21 | Clear documentation | Complete |

---

## Acceptance Criteria

### Base Directory

- [ ] AC-021A-01: Base directory `.acode/artifacts/` exists after first run
- [ ] AC-021A-02: Base directory is relative to repository root
- [ ] AC-021A-03: `.acode/.gitignore` excludes `artifacts/`
- [ ] AC-021A-04: Base directory has correct permissions (755)
- [ ] AC-021A-05: Base directory logged at startup
- [ ] AC-021A-06: Custom base path applied when configured

### Run Directory

- [ ] AC-021A-07: Run directory created for each execution
- [ ] AC-021A-08: Run ID format is `{task}-{YYYYMMDD-HHmmss}`
- [ ] AC-021A-09: No two runs have same directory
- [ ] AC-021A-10: Collision appends sequence number
- [ ] AC-021A-11: Run directory logged on creation
- [ ] AC-021A-12: Directory exists before any writes

### Standard Files

- [ ] AC-021A-13: stdout.txt contains process stdout
- [ ] AC-021A-14: stderr.txt contains process stderr
- [ ] AC-021A-15: build.log contains build output
- [ ] AC-021A-16: test-results.json contains test results
- [ ] AC-021A-17: run.json contains valid metadata
- [ ] AC-021A-18: All standard files use consistent encoding (UTF-8)

### Metadata

- [ ] AC-021A-19: run.json includes run ID
- [ ] AC-021A-20: run.json includes task name
- [ ] AC-021A-21: run.json includes start/end timestamps
- [ ] AC-021A-22: run.json includes duration
- [ ] AC-021A-23: run.json includes exit code
- [ ] AC-021A-24: run.json includes status
- [ ] AC-021A-25: run.json is valid JSON
- [ ] AC-021A-26: run.json parseable by standard tools

### Path Resolution

- [ ] AC-021A-27: `GetBaseDirectory()` returns absolute path
- [ ] AC-021A-28: `GetRunDirectory(runId)` returns correct path
- [ ] AC-021A-29: Paths work on Windows and Linux
- [ ] AC-021A-30: Path resolution is thread-safe
- [ ] AC-021A-31: Invalid characters sanitized from paths

### Retention

- [ ] AC-021A-32: Total size tracked accurately
- [ ] AC-021A-33: Oldest runs pruned when limit exceeded
- [ ] AC-021A-34: `acode artifacts prune` prompts before delete
- [ ] AC-021A-35: `acode artifacts prune --force` skips prompt
- [ ] AC-021A-36: Prune removes entire run directories
- [ ] AC-021A-37: Pruned runs logged
- [ ] AC-021A-38: Corrupted runs handled gracefully

### Error Handling

- [ ] AC-021A-39: Disk full logged with clear message
- [ ] AC-021A-40: Permission denied logged with remediation
- [ ] AC-021A-41: Partial writes cleaned up automatically
- [ ] AC-021A-42: Write failures don't crash execution

---

## User Manual Documentation

### Overview

The agentic coding bot stores all execution artifacts in a standardized directory structure under `.acode/artifacts/`. This enables easy inspection, diffing, and export of execution results.

### Directory Structure

```
.acode/
├── artifacts/
│   ├── build-20240120-143052/
│   │   ├── run.json           # Execution metadata
│   │   ├── stdout.txt         # Standard output
│   │   ├── stderr.txt         # Standard error
│   │   └── build.log          # Build log
│   ├── test-20240120-143215/
│   │   ├── run.json
│   │   ├── stdout.txt
│   │   ├── stderr.txt
│   │   ├── test-results.json
│   │   └── coverage.json
│   └── latest -> test-20240120-143215  # Symlink to latest run
└── .gitignore
```

### Standard File Names

| File | Description |
|------|-------------|
| `run.json` | Execution metadata (timestamps, status, exit code) |
| `stdout.txt` | Captured standard output |
| `stderr.txt` | Captured standard error |
| `output.txt` | Combined stdout/stderr (interleaved) |
| `build.log` | Build command output |
| `restore.log` | Package restore output |
| `test-results.json` | Test results in JSON format |
| `test-results.trx` | Test results in TRX format (.NET) |
| `coverage.json` | Code coverage data |
| `error.json` | Detailed error information (on failure) |

### Metadata Schema

```json
// run.json
{
  "runId": "build-20240120-143052",
  "taskName": "build",
  "command": "dotnet build",
  "startTime": "2024-01-20T14:30:52.123Z",
  "endTime": "2024-01-20T14:31:15.456Z",
  "durationMs": 23333,
  "exitCode": 0,
  "status": "success",
  "operatingMode": "local-only",
  "containerId": "abc123...",
  "artifacts": [
    "stdout.txt",
    "stderr.txt",
    "build.log"
  ]
}
```

### CLI Commands

```bash
# List all runs
acode artifacts list

# Output:
# RUN ID                    TASK    STATUS   DURATION  SIZE
# build-20240120-143052     build   success  23s       1.2 MB
# test-20240120-143215      test    success  45s       3.4 MB
# build-20240120-144000     build   failure  5s        0.5 MB

# Show artifact details for a run
acode artifacts show build-20240120-143052

# Output:
# Run: build-20240120-143052
# Task: build
# Status: success
# Duration: 23.3s
# 
# Artifacts:
#   - run.json (1.2 KB)
#   - stdout.txt (45 KB)
#   - stderr.txt (0 bytes)
#   - build.log (120 KB)

# View specific artifact
acode artifacts cat build-20240120-143052 stdout.txt

# Open artifact directory
acode artifacts open build-20240120-143052

# Show storage statistics
acode artifacts stats

# Output:
# Artifact Storage Statistics
# ===========================
# Total runs: 42
# Total size: 156 MB
# Size limit: 1 GB
# Usage: 15.6%
# Oldest run: build-20240115-091000
# Newest run: test-20240120-143215

# Prune old artifacts
acode artifacts prune

# Output:
# This will remove the following runs:
#   - build-20240115-091000 (12 MB)
#   - test-20240115-093000 (8 MB)
# 
# Total to remove: 20 MB
# Are you sure? [y/N]: y
# 
# ✓ Removed 2 runs, freed 20 MB

# Prune to specific limit
acode artifacts prune --max-size 500MB

# Prune to keep only last N runs
acode artifacts prune --keep 10

# Force prune without confirmation
acode artifacts prune --force
```

### Configuration

```yaml
# .agent/config.yml
artifacts:
  base_path: .acode/artifacts    # Default path
  max_size: 1GB                  # Storage limit
  retention:
    max_runs: 100                # Keep at most 100 runs
    max_age_days: 30             # Keep runs for 30 days
  auto_prune: true               # Prune automatically when limit hit
```

### Gitignore

The `.acode/` directory automatically includes a `.gitignore`:

```gitignore
# .acode/.gitignore
artifacts/
```

This ensures artifacts are not committed to version control.

---

## Assumptions

This task makes the following assumptions about the system, environment, dependencies, and operational context:

### Technical Assumptions

1. **Filesystem Hierarchy Supported** — Target filesystems (ext4, NTFS, APFS, Btrfs) support sufficient directory depth (`.acode/artifacts/{run-id}/` = 3 levels) without performance degradation
2. **Cross-Platform Path Handling** — .NET Path.Combine() correctly handles forward slashes on Windows, Linux, and macOS for consistent path resolution
3. **Unique Run IDs** — Run ID generation (ULID or UUID v4) provides collision-free identifiers with probability <1 in 10^18 for realistic workload (100k runs/workspace)
4. **Atomic Directory Creation** — Filesystem guarantees atomic directory creation preventing race conditions when multiple processes create `.acode/artifacts/` simultaneously
5. **Write Permissions Available** — Agent process has write permissions to workspace root directory enabling `.acode/` creation; users run agent with sufficient privileges (not sandboxed)
6. **Disk Space Monitoring** — Operating system or monitoring tools alert administrators before disk exhaustion; agent does not implement disk space checking beyond retention policy
7. **File Locking Semantics** — Filesystem supports advisory file locking for preventing concurrent writes to same artifact file; handles distributed filesystems (NFS, SMB) correctly
8. **Symlink Support** — Filesystem supports symbolic links if needed for artifact references; resolution follows symlinks correctly without infinite loops
9. **Gitignore Parsing** — Git version control respects `.gitignore` patterns including wildcards (`*.txt`) and negations (`!important.txt`); agent writes valid `.gitignore` syntax
10. **JSON Serialization** — .NET System.Text.Json correctly serializes run metadata including Unicode characters, timestamps (ISO 8601), and nested objects up to 64 levels depth

### Operational Assumptions

11. **Single Workspace Instance** — Only one agent instance per workspace runs commands concurrently; multiple instances accessing same workspace handled via database locking (Task 050b dependency)
12. **Workspace Root Stability** — Workspace root directory path remains constant during agent lifetime; moving workspaces requires re-initialization
13. **Retention Policy Configuration** — Default retention (30 days successful, 90 days failed) is acceptable for most users; custom policies configured in `agent-config.yml` before large-scale use
14. **Artifact Size Limits** — Default 10MB per artifact and 100MB per run are sufficient for typical builds/tests; larger artifacts (videos, binaries) manually excluded or limits increased
15. **No External Artifact Management** — Users do not manually edit/delete artifacts in `.acode/artifacts/` directory; all artifact operations go through agent CLI to maintain database consistency
16. **Run ID Display** — Users can copy/paste run IDs from CLI output (monospace-friendly ULIDs/UUIDs); screen readers correctly announce identifiers for accessibility
17. **Standard Output Streams** — Executed commands write output to stdout/stderr, not directly to files bypassing stream capture; frameworks like MSBuild, npm, pytest follow this convention
18. **Artifact Immutability** — Once written, artifacts are never modified by external processes; any tampering detected via checksums (if implemented, see Task 021 parent)
19. **Workspace Portability** — Workspaces can be archived/restored including `.acode/` directory; restoring workspace with artifacts maintains consistency between database (Task 050) and filesystem
20. **Pruning Job Reliability** — Background retention job runs reliably via OS scheduler (cron, Task Scheduler, systemd timer); failed pruning attempts retried next cycle without manual intervention

---

## Security Considerations

This section identifies security threats specific to artifact directory standards, with complete C# mitigation implementations.

---

### Security Threat 1: Path Traversal via Malicious Run IDs

**Risk:** Attacker controls run ID value (e.g., via API or CLI), injects path traversal sequences (`../../etc/passwd`) to write artifacts outside designated directory.

**Attack Scenario:**
1. Attacker crafts malicious run ID: `../../../../../../tmp/malicious`
2. Agent creates directory: `.acode/artifacts/../../../../../../tmp/malicious/`
3. Artifacts written to `/tmp/malicious/` instead of workspace artifacts directory
4. Attacker plants malicious files in system directories
5. System compromise via planted executable or configuration file

**Mitigation:**

Validate and sanitize run IDs before using them in filesystem operations. Reject invalid characters and path traversal patterns.

```csharp
// RunIdValidator.cs
using System;
using System.Text.RegularExpressions;

namespace Acode.Infrastructure.Artifacts.Security;

public sealed class RunIdValidator
{
    //  Valid run ID: alphanumeric, hyphens only (ULID/UUID format)
    private static readonly Regex ValidRunIdPattern = new(@"^[a-zA-Z0-9\-]{20,40}$", RegexOptions.Compiled);

    // Dangerous patterns that must be rejected
    private static readonly string[] ForbiddenPatterns = new[]
    {
        "..",           // Path traversal
        "/",            // Directory separator
        "\\",           // Windows separator
        ":",            // Drive letter (Windows)
        "*",            // Wildcard
        "?",            // Wildcard
        "<", ">",       // Redirection
        "|",            // Pipe
        "\"",           // Quote
        "\0",           // Null byte
    };

    public (bool IsValid, string Error) Validate(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            return (false, "Run ID cannot be null or empty");
        }

        // Check for forbidden patterns
        foreach (var pattern in ForbiddenPatterns)
        {
            if (runId.Contains(pattern))
            {
                return (false, $"Run ID contains forbidden pattern: '{pattern}'");
            }
        }

        // Validate format
        if (!ValidRunIdPattern.IsMatch(runId))
        {
            return (false, "Run ID must contain only alphanumeric characters and hyphens (20-40 chars)");
        }

        // Ensure it doesn't resolve to parent directories
        var normalizedPath = Path.GetFullPath(Path.Combine(".acode/artifacts", runId));
        var expectedPrefix = Path.GetFullPath(".acode/artifacts");

        if (!normalizedPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Run ID resolves outside artifacts directory");
        }

        return (true, null);
    }
}
```

**Validation Test:**

```bash
# Test path traversal prevention
acode artifact create --run-id "../../tmp/malicious" test.txt
# Expected: Error - "Run ID contains forbidden pattern: '..'"

acode artifact create --run-id "../escape" test.txt
# Expected: Error - "Run ID contains forbidden pattern: '..'"

# Test valid run IDs
acode artifact create --run-id "run-01HQRS7TGKMWXY123" test.txt
# Expected: Success - directory created at .acode/artifacts/run-01HQRS7TGKMWXY123/

acode artifact create --run-id "a1b2c3d4-e5f6-g7h8-i9j0-k1l2m3n4o5p6" test.txt
# Expected: Success - UUID format accepted
```

---

### Security Threat 2: Insufficient Directory Permissions Allowing Unauthorized Access

**Risk:** Artifact directories created with overly permissive permissions (777) allow any user on system to read sensitive artifacts or modify/delete them.

**Attack Scenario:**
1. Agent creates `.acode/artifacts/` with permissions 777 (read/write/execute for all)
2. Multi-user system with multiple developers or services
3. Attacker user reads artifacts from other users' workspaces
4. Sensitive data exposed: API keys in logs, credentials in test outputs
5. Attacker modifies artifacts to hide evidence or inject false data

**Mitigation:**

Create artifact directories with restrictive permissions (755 for directories, 644 for files) ensuring only owner can write.

```csharp
// ArtifactDirectoryInitializer.cs
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Acode.Infrastructure.Artifacts.Security;

public sealed class ArtifactDirectoryInitializer
{
    private readonly string _artifactsRootPath;
    private readonly ILogger<ArtifactDirectoryInitializer> _logger;

    public ArtifactDirectoryInitializer(
        string artifactsRootPath,
        ILogger<ArtifactDirectoryInitializer> logger)
    {
        _artifactsRootPath = artifactsRootPath;
        _logger = logger;
    }

    public void EnsureArtifactDirectoryExists(string runId)
    {
        var runDirectory = Path.Combine(_artifactsRootPath, runId);

        if (Directory.Exists(runDirectory))
        {
            _logger.LogDebug("Artifact directory already exists: {Path}", runDirectory);
            return;
        }

        // Create directory
        Directory.CreateDirectory(runDirectory);

        // Set restrictive permissions (Unix only, Windows uses ACLs)
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            SetUnixPermissions(runDirectory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                              UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                                              UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            _logger.LogInformation("Created artifact directory with 755 permissions: {Path}", runDirectory);
        }
        else
        {
            // Windows: Inherit permissions from parent, no world-writable
            _logger.LogInformation("Created artifact directory with inherited permissions: {Path}", runDirectory);
        }
    }

    public void WriteArtifactFile(string runId, string filename, byte[] content)
    {
        var filePath = Path.Combine(_artifactsRootPath, runId, filename);

        File.WriteAllBytes(filePath, content);

        // Set restrictive file permissions (644: owner read/write, others read-only)
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            SetUnixPermissions(filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite |
                                         UnixFileMode.GroupRead |
                                         UnixFileMode.OtherRead);
            _logger.LogDebug("Created artifact file with 644 permissions: {Path}", filePath);
        }
    }

    private void SetUnixPermissions(string path, UnixFileMode mode)
    {
        File.SetUnixFileMode(path, mode);
    }
}
```

**Validation Test:**

```bash
# Test directory permissions
acode artifact create --run-id test-perms test.txt

# Check directory permissions (Unix)
ls -ld .acode/artifacts/test-perms/
# Expected: drwxr-xr-x (755)

# Check file permissions
ls -l .acode/artifacts/test-perms/test.txt
# Expected: -rw-r--r-- (644)

# Test unauthorized modification (as different user)
su other-user
echo "tampered" > .acode/artifacts/test-perms/test.txt
# Expected: Permission denied

# Test unauthorized deletion
rm -rf .acode/artifacts/test-perms/
# Expected: Permission denied
```

---

### Security Threat 3: Race Condition in Directory Creation

**Risk:** Multiple concurrent processes create same run directory simultaneously, leading to race condition where one process overwrites another's artifacts.

**Attack Scenario:**
1. Two agent processes execute commands with same run ID simultaneously
2. Both check `Directory.Exists(runDir)` → returns false
3. Both call `Directory.CreateDirectory(runDir)` concurrently
4. Process A writes `stdout.txt`
5. Process B overwrites `stdout.txt` with different content
6. Artifact corruption and data loss

**Mitigation:**

Use atomic directory creation with exclusive locking to prevent race conditions.

```csharp
// AtomicDirectoryCreator.cs
using System;
using System.IO;
using System.Threading;

namespace Acode.Infrastructure.Artifacts.Security;

public sealed class AtomicDirectoryCreator
{
    private readonly ILogger<AtomicDirectoryCreator> _logger;
    private static readonly SemaphoreSlim CreationLock = new(1, 1);

    public AtomicDirectoryCreator(ILogger<AtomicDirectoryCreator> logger)
    {
        _logger = logger;
    }

    public async Task<string> CreateRunDirectoryAsync(string artifactsRoot, string runId)
    {
        var runDirectory = Path.Combine(artifactsRoot, runId);

        // Fast path: directory already exists
        if (Directory.Exists(runDirectory))
        {
            return runDirectory;
        }

        // Slow path: acquire lock for creation
        await CreationLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock (another thread may have created it)
            if (Directory.Exists(runDirectory))
            {
                _logger.LogDebug("Directory created by another thread: {Path}", runDirectory);
                return runDirectory;
            }

            // Create with lock file for inter-process synchronization
            var lockFilePath = Path.Combine(artifactsRoot, $".lock-{runId}");

            using var lockFile = new FileStream(
                lockFilePath,
                FileMode.CreateNew,  // Fails if file exists (atomic)
                FileAccess.Write,
                FileShare.None,      // Exclusive access
                bufferSize: 1,
                FileOptions.DeleteOnClose);

            // Lock acquired, safe to create directory
            Directory.CreateDirectory(runDirectory);

            _logger.LogInformation("Atomically created directory: {Path}", runDirectory);

            return runDirectory;
        }
        catch (IOException ex) when (ex.Message.Contains("already exists"))
        {
            // Another process created the directory concurrently
            _logger.LogWarning("Race condition detected, directory created by another process: {Path}", runDirectory);
            return runDirectory;
        }
        finally
        {
            CreationLock.Release();
        }
    }
}
```

**Validation Test:**

```bash
# Test concurrent directory creation
acode artifact create --run-id test-race test1.txt &
acode artifact create --run-id test-race test2.txt &
wait

# Verify both artifacts exist without corruption
ls .acode/artifacts/test-race/
# Expected: test1.txt, test2.txt (both present, no overwrites)

cat .acode/artifacts/test-race/test1.txt
cat .acode/artifacts/test-race/test2.txt
# Expected: Both files have correct content, no corruption

# Check logs for race condition warnings
acode log query --message "Race condition detected" --last 5m
# Expected: May show warning if race detected, but handled gracefully
```

---

### Security Threat 4: Gitignore Injection via Malicious Artifact Names

**Risk:** Attacker creates artifact with name designed to manipulate `.gitignore` patterns, causing sensitive files to be committed or legitimate files to be ignored.

**Attack Scenario:**
1. Attacker triggers artifact creation with malicious name: `!important.txt\n*.env`
2. Agent appends artifact pattern to `.gitignore`: `artifacts/!important.txt\n*.env`
3. Gitignore negation (`!`) exposes artifacts that should be hidden
4. Wildcard (`*.env`) causes all `.env` files in workspace to be ignored
5. Credentials accidentally committed to version control

**Mitigation:**

Sanitize artifact filenames and validate `.gitignore` patterns before writing. Escape special characters and reject invalid names.

```csharp
// GitignoreManager.cs
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Acode.Infrastructure.Artifacts.Security;

public sealed class GitignoreManager
{
    private readonly string _workspaceRoot;
    private readonly ILogger<GitignoreManager> _logger;

    // Valid filename: alphanumeric, hyphens, underscores, dots, no path separators or special chars
    private static readonly Regex ValidFilenamePattern = new(@"^[a-zA-Z0-9\-_\.]+$", RegexOptions.Compiled);

    public GitignoreManager(string workspaceRoot, ILogger<GitignoreManager> logger)
    {
        _workspaceRoot = workspaceRoot;
        _logger = logger;
    }

    public void EnsureArtifactsIgnored()
    {
        var gitignorePath = Path.Combine(_workspaceRoot, ".gitignore");

        const string artifactsPattern = ".acode/artifacts/";

        // Check if .gitignore exists
        if (!File.Exists(gitignorePath))
        {
            File.WriteAllLines(gitignorePath, new[] { artifactsPattern });
            _logger.LogInformation("Created .gitignore with artifacts pattern: {Pattern}", artifactsPattern);
            return;
        }

        // Check if pattern already exists
        var existingLines = File.ReadAllLines(gitignorePath);
        if (existingLines.Any(line => line.Trim() == artifactsPattern))
        {
            _logger.LogDebug(".gitignore already contains artifacts pattern");
            return;
        }

        // Append pattern (safe: no user input, hardcoded pattern)
        File.AppendAllLines(gitignorePath, new[] { "", "# Acode artifacts (auto-generated)", artifactsPattern });
        _logger.LogInformation("Appended artifacts pattern to .gitignore");
    }

    public (bool IsValid, string Error) ValidateArtifactFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return (false, "Filename cannot be null or empty");
        }

        // Reject path separators
        if (filename.Contains('/') || filename.Contains('\\'))
        {
            return (false, "Filename cannot contain path separators");
        }

        // Reject gitignore special characters
        var forbiddenChars = new[] { '!', '*', '?', '[', ']', '{', '}', '\n', '\r' };
        if (forbiddenChars.Any(filename.Contains))
        {
            return (false, $"Filename contains forbidden character (gitignore special chars not allowed)");
        }

        // Validate format
        if (!ValidFilenamePattern.IsMatch(filename))
        {
            return (false, "Filename must contain only alphanumeric, hyphens, underscores, and dots");
        }

        return (true, null);
    }
}
```

**Validation Test:**

```bash
# Test gitignore injection prevention
acode artifact create --run-id test-inject --name "!important.txt" content.txt
# Expected: Error - "Filename contains forbidden character"

acode artifact create --run-id test-inject --name "*.env" content.txt
# Expected: Error - "Filename contains forbidden character"

acode artifact create --run-id test-inject --name "test\nnewline.txt" content.txt
# Expected: Error - "Filename contains forbidden character"

# Test valid filenames
acode artifact create --run-id test-valid --name "build-log.txt" content.txt
# Expected: Success

acode artifact create --run-id test-valid --name "test_results.json" content.txt
# Expected: Success

# Verify .gitignore integrity
cat .gitignore
# Expected: Contains ".acode/artifacts/" with no malicious patterns
```

---

### Security Threat 5: Symlink Attack for Artifact Exfiltration

**Risk:** Attacker creates symlink in artifact directory pointing to sensitive file outside workspace, causing agent to expose or overwrite sensitive data when reading/writing artifacts.

**Attack Scenario:**
1. Attacker gains write access to `.acode/artifacts/run-123/` directory
2. Creates symlink: `ln -s /etc/passwd stdout.txt`
3. User runs `acode artifact cat run-123 stdout.txt`
4. Agent follows symlink, displays `/etc/passwd` contents
5. Sensitive system configuration exposed

**Mitigation:**

Detect and reject symlinks when reading/writing artifacts. Use `FileOptions.None` and check file attributes before operations.

```csharp
// SymlinkSafeFileReader.cs
using System;
using System.IO;

namespace Acode.Infrastructure.Artifacts.Security;

public sealed class SymlinkSafeFileReader
{
    private readonly ILogger<SymlinkSafeFileReader> _logger;

    public SymlinkSafeFileReader(ILogger<SymlinkSafeFileReader> logger)
    {
        _logger = logger;
    }

    public byte[] ReadArtifactFile(string artifactPath)
    {
        // Resolve to absolute path
        var absolutePath = Path.GetFullPath(artifactPath);

        // Check if file exists
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Artifact not found: {artifactPath}");
        }

        // Check if path is a symlink (Unix)
        var fileInfo = new FileInfo(absolutePath);
        if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            _logger.LogError("Symlink detected, refusing to read: {Path}", absolutePath);
            throw new SecurityException($"Artifact is a symlink (forbidden): {artifactPath}");
        }

        // Additional check: ensure resolved path is still within artifacts directory
        var artifactsRoot = Path.GetFullPath(".acode/artifacts");
        if (!absolutePath.StartsWith(artifactsRoot, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("Path escapes artifacts directory: {Path}", absolutePath);
            throw new SecurityException($"Artifact path escapes artifacts directory: {artifactPath}");
        }

        // Safe to read
        return File.ReadAllBytes(absolutePath);
    }

    public void WriteArtifactFile(string artifactPath, byte[] content)
    {
        var absolutePath = Path.GetFullPath(artifactPath);

        // Ensure parent directory exists and is not a symlink
        var directory = Path.GetDirectoryName(absolutePath);
        var dirInfo = new DirectoryInfo(directory);
        if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            _logger.LogError("Parent directory is a symlink: {Path}", directory);
            throw new SecurityException($"Parent directory is a symlink: {directory}");
        }

        // Write with FileOptions that prevent symlink following
        using var fileStream = new FileStream(
            absolutePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.None);  // No FILE_FLAG_OPEN_REPARSE_POINT

        fileStream.Write(content, 0, content.Length);

        _logger.LogDebug("Wrote artifact file (symlink-safe): {Path}", absolutePath);
    }
}
```

**Validation Test:**

```bash
# Test symlink attack prevention
mkdir -p .acode/artifacts/test-symlink

# Create symlink to sensitive file
ln -s /etc/passwd .acode/artifacts/test-symlink/passwd-link

# Attempt to read via symlink
acode artifact cat test-symlink passwd-link
# Expected: Error - "Artifact is a symlink (forbidden)"

# Attempt to write via symlink directory
ln -s /tmp/.acode/artifacts/test-symlink-dir
acode artifact create --run-id test-symlink-dir test.txt
# Expected: Error - "Parent directory is a symlink"

# Verify legitimate files work
acode artifact create --run-id test-legitimate test.txt
acode artifact cat test-legitimate test.txt
# Expected: Success, content displayed
```

---

## Best Practices

### Directory Structure

1. **Consistent hierarchy** - Same structure across all projects
2. **Predictable naming** - {runId}/{artifactType}/{filename}
3. **Shallow directories** - Avoid deeply nested structures
4. **Separate by type** - Logs, test results, coverage in own directories

### File Naming

5. **Include timestamps** - ISO8601 format in filenames when relevant
6. **Avoid special characters** - Alphanumeric, hyphen, underscore only
7. **Preserve extensions** - Keep original file extensions
8. **Unique names** - Prevent overwrites with UUID or sequence

### Maintenance

9. **Gitignore artifacts** - Never commit artifacts to source control
10. **Document structure** - README in artifacts directory explaining layout
11. **Size monitoring** - Warn when artifacts directory grows large
12. **Periodic cleanup** - Script or command to prune old artifacts

---

## Troubleshooting

### Issue: Artifacts not being collected

**Symptoms:** Run completes but artifacts directory is empty

**Causes:**
- Artifact collection disabled in config
- Output files written to unexpected locations
- File patterns don't match actual outputs

**Solutions:**
1. Check `runs.artifacts.enabled` in config
2. Verify output paths in build/test commands
3. Update artifact patterns to match actual outputs

### Issue: Artifact directory growing too large

**Symptoms:** Disk space warnings, slow file operations

**Causes:**
- No retention policy configured
- Large artifacts (videos, binaries) being collected
- Many runs accumulating over time

**Solutions:**
1. Configure retention policy to delete old artifacts
2. Exclude large file types from collection
3. Run `acode artifacts prune` periodically

### Issue: Artifacts not found by ID

**Symptoms:** "Artifact not found" errors when retrieving

**Causes:**
- Artifact was pruned by retention policy
- ID is from different workspace
- Database and filesystem out of sync

**Solutions:**
1. Check artifact retention settings
2. Verify correct workspace is active
3. Run `acode artifacts verify` to check consistency

---

## Testing Requirements

### Unit Tests

#### ArtifactPathResolverTests

```csharp
[Fact] GetBaseDirectory_ReturnsAbsolutePath()
[Fact] GetBaseDirectory_RelativeToRepoRoot()
[Fact] GetRunDirectory_ReturnsCorrectPath()
[Fact] GetRunDirectory_IncludesRunId()
[Fact] GetArtifactPath_ReturnsCorrectPath()
[Fact] GetArtifactPath_CombinesRunIdAndFilename()
[Fact] GetStdoutPath_ReturnsStdoutTxt()
[Fact] GetStderrPath_ReturnsStderrTxt()
[Fact] GetMetadataPath_ReturnsRunJson()
[Fact] PathResolution_HandlesWindowsPaths()
[Fact] PathResolution_HandlesLinuxPaths()
[Fact] PathResolution_SanitizesInvalidCharacters()
```

#### RunIdGeneratorTests

```csharp
[Fact] Generate_IncludesTaskName()
[Fact] Generate_IncludesTimestamp()
[Fact] Generate_TimestampFormatCorrect()
[Fact] Generate_UniqueAcrossCalls()
[Fact] Generate_HandlesCollision()
[Fact] Generate_CollisionAppendsSequence()
[Fact] Generate_SanitizesTaskName()
[Fact] Generate_ThreadSafe()
```

#### ArtifactDirectoryManagerTests

```csharp
[Fact] EnsureBaseDirectory_CreatesIfMissing()
[Fact] EnsureBaseDirectory_NoOpIfExists()
[Fact] EnsureBaseDirectory_SetsPermissions()
[Fact] EnsureRunDirectory_CreatesDirectory()
[Fact] EnsureRunDirectory_ReturnsPath()
[Fact] EnsureRunDirectory_LogsCreation()
[Fact] EnsureGitignore_CreatesIfMissing()
[Fact] EnsureGitignore_IncludesArtifacts()
```

#### ArtifactWriterTests

```csharp
[Fact] WriteAsync_CreatesFile()
[Fact] WriteAsync_AtomicWrite()
[Fact] WriteAsync_PartialWriteRolledBack()
[Fact] AppendAsync_AppendsToExisting()
[Fact] StreamWriteAsync_WritesChunks()
[Fact] WriteMetadata_ValidJson()
[Fact] WriteMetadata_AllFieldsPresent()
[Fact] Write_HandlesUtf8Correctly()
```

#### RunMetadataTests

```csharp
[Fact] Serialize_ValidJson()
[Fact] Serialize_IncludesAllFields()
[Fact] Serialize_Iso8601Timestamps()
[Fact] Deserialize_RoundTrip()
[Fact] CalculateDuration_Correct()
[Fact] SetStatus_FromExitCode()
```

#### RetentionPolicyTests

```csharp
[Fact] GetRunsToDelete_WhenUnderLimit_ReturnsEmpty()
[Fact] GetRunsToDelete_WhenOverLimit_ReturnsOldest()
[Fact] GetRunsToDelete_ByCount_KeepsNewest()
[Fact] GetRunsToDelete_ByAge_DeletesOld()
[Fact] CalculateTotalSize_SumsAllRuns()
[Fact] Prune_DeletesEntireDirectory()
[Fact] Prune_LogsDeletion()
```

### Integration Tests

#### ArtifactLifecycleIntegrationTests

```csharp
[Fact] FullLifecycle_CreateWriteReadDelete()
[Fact] ConcurrentWrites_NoCorruption()
[Fact] LargeFile_StreamedCorrectly()
[Fact] Metadata_PersistsAcrossRestart()
[Fact] Prune_RemovesOldestFirst()
```

#### CrossPlatformIntegrationTests

```csharp
[Fact] Windows_PathsResolveCorrectly()
[Fact] Linux_PathsResolveCorrectly()
[Fact] PathSeparators_NormalizedCorrectly()
```

### E2E Tests

#### ArtifactsCLIE2ETests

```csharp
[Fact] ArtifactsList_ShowsAllRuns()
[Fact] ArtifactsShow_DisplaysRunDetails()
[Fact] ArtifactsCat_OutputsFileContent()
[Fact] ArtifactsStats_ShowsStorageInfo()
[Fact] ArtifactsPrune_PromptsConfirmation()
[Fact] ArtifactsPrune_Force_SkipsPrompt()
[Fact] ArtifactsPrune_KeepCount_PreservesRecent()
```

### Performance Benchmarks

| Benchmark | Target | Threshold |
|-----------|--------|-----------|
| PathResolution | < 1ms | P99 |
| DirectoryCreation | < 50ms | P95 |
| MetadataWrite | < 10ms | P95 |
| SizeCalculation (100 runs) | < 2s | P95 |
| PruneOperation (per run) | < 100ms | P95 |
| StreamWrite (1MB) | < 100ms | P95 |

### Coverage Requirements

| Component | Minimum Coverage |
|-----------|------------------|
| ArtifactPathResolver | 95% |
| RunIdGenerator | 95% |
| ArtifactDirectoryManager | 90% |
| ArtifactWriter | 90% |
| RetentionPolicy | 90% |
| CLI Commands | 85% |
| **Overall** | **90%** |

---

## User Verification Steps

### Scenario 1: Verify Directory Creation

```bash
# Remove existing artifacts
rm -rf .acode/artifacts

# Run a task
acode task run build

# Verify directory structure
ls -la .acode/artifacts/

# Expected: Directory exists with run subdirectory
```

### Scenario 2: Verify Standard Files

```bash
# Run a task
acode task run test

# Check run directory
ls -la .acode/artifacts/test-*/

# Expected:
# run.json
# stdout.txt
# stderr.txt
# test-results.json
```

### Scenario 3: Verify Metadata Content

```bash
# View metadata
cat .acode/artifacts/test-*/run.json | jq .

# Expected: Valid JSON with all fields:
# runId, taskName, startTime, endTime, durationMs, exitCode, status
```

### Scenario 4: Verify Run ID Uniqueness

```bash
# Run same task twice quickly
acode task run build &
acode task run build &
wait

# List artifacts
ls .acode/artifacts/

# Expected: Two different run directories (not same name)
```

### Scenario 5: Verify Prune Operation

```bash
# Check current storage
acode artifacts stats

# Prune to keep only 5 runs
acode artifacts prune --keep 5 --force

# Verify
acode artifacts list

# Expected: Only 5 most recent runs remain
```

### Scenario 6: Verify Gitignore

```bash
# Check gitignore
cat .acode/.gitignore

# Expected: Contains "artifacts/"

# Verify not tracked
git status .acode/artifacts/

# Expected: Not shown (ignored)
```

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Artifacts/
│       ├── IArtifactPathResolver.cs
│       ├── IArtifactWriter.cs
│       ├── IArtifactReader.cs
│       ├── IRetentionPolicy.cs
│       ├── RunMetadata.cs
│       ├── RunStatus.cs
│       └── ArtifactConstants.cs
├── Acode.Infrastructure/
│   └── Artifacts/
│       ├── ArtifactPathResolver.cs
│       ├── ArtifactDirectoryManager.cs
│       ├── ArtifactWriter.cs
│       ├── ArtifactReader.cs
│       ├── RunIdGenerator.cs
│       └── RetentionPolicyEnforcer.cs
├── Acode.Cli/
│   └── Commands/
│       └── ArtifactsCommands.cs
└── tests/
    ├── Acode.Domain.Tests/
    │   └── Artifacts/
    │       └── RunMetadataTests.cs
    ├── Acode.Infrastructure.Tests/
    │   └── Artifacts/
    │       ├── ArtifactPathResolverTests.cs
    │       ├── RunIdGeneratorTests.cs
    │       ├── ArtifactWriterTests.cs
    │       └── RetentionPolicyTests.cs
    └── Acode.Integration.Tests/
        └── Artifacts/
            └── ArtifactLifecycleTests.cs
```

### Domain Models

```csharp
// IArtifactPathResolver.cs
namespace Acode.Domain.Artifacts;

public interface IArtifactPathResolver
{
    string GetBaseDirectory();
    string GetRunDirectory(string runId);
    string GetArtifactPath(string runId, string filename);
    string GetStdoutPath(string runId);
    string GetStderrPath(string runId);
    string GetMetadataPath(string runId);
    string GetTestResultsPath(string runId);
}

// IArtifactWriter.cs
namespace Acode.Domain.Artifacts;

public interface IArtifactWriter
{
    Task WriteAsync(string runId, string filename, string content, CancellationToken ct = default);
    Task WriteAsync(string runId, string filename, byte[] content, CancellationToken ct = default);
    Task AppendAsync(string runId, string filename, string content, CancellationToken ct = default);
    Task WriteMetadataAsync(string runId, RunMetadata metadata, CancellationToken ct = default);
    IAsyncDisposable OpenStreamWriter(string runId, string filename);
}

// RunMetadata.cs
namespace Acode.Domain.Artifacts;

public sealed record RunMetadata
{
    public required string RunId { get; init; }
    public required string TaskName { get; init; }
    public string? Command { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public DateTimeOffset? EndTime { get; init; }
    public long? DurationMs => EndTime.HasValue 
        ? (long)(EndTime.Value - StartTime).TotalMilliseconds 
        : null;
    public int? ExitCode { get; init; }
    public required RunStatus Status { get; init; }
    public required string OperatingMode { get; init; }
    public string? ContainerId { get; init; }
    public IReadOnlyList<string> Artifacts { get; init; } = Array.Empty<string>();
}

public enum RunStatus
{
    Running,
    Success,
    Failure,
    Cancelled,
    TimedOut
}

// ArtifactConstants.cs
namespace Acode.Domain.Artifacts;

public static class ArtifactConstants
{
    public const string BaseDirectory = ".acode/artifacts";
    public const string GitignoreFile = ".acode/.gitignore";
    
    public static class FileNames
    {
        public const string Stdout = "stdout.txt";
        public const string Stderr = "stderr.txt";
        public const string Output = "output.txt";
        public const string Metadata = "run.json";
        public const string BuildLog = "build.log";
        public const string RestoreLog = "restore.log";
        public const string TestResults = "test-results.json";
        public const string TestResultsTrx = "test-results.trx";
        public const string Coverage = "coverage.json";
        public const string Error = "error.json";
    }
}

// IRetentionPolicy.cs
namespace Acode.Domain.Artifacts;

public interface IRetentionPolicy
{
    Task<long> GetTotalSizeAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetRunsToDeleteAsync(
        long? maxSizeBytes = null,
        int? keepCount = null,
        int? maxAgeDays = null,
        CancellationToken ct = default);
    Task<long> PruneAsync(IEnumerable<string> runIds, CancellationToken ct = default);
}
```

### Infrastructure Implementation

```csharp
// ArtifactPathResolver.cs
namespace Acode.Infrastructure.Artifacts;

public sealed class ArtifactPathResolver : IArtifactPathResolver
{
    private readonly string _repoRoot;
    private readonly ArtifactConfiguration _config;
    
    public ArtifactPathResolver(
        IRepositoryDetector repoDetector,
        IOptions<ArtifactConfiguration> config)
    {
        _repoRoot = repoDetector.GetRepositoryRoot();
        _config = config.Value;
    }
    
    public string GetBaseDirectory()
    {
        var basePath = _config.BasePath ?? ArtifactConstants.BaseDirectory;
        return Path.GetFullPath(Path.Combine(_repoRoot, basePath));
    }
    
    public string GetRunDirectory(string runId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        var sanitized = SanitizePathComponent(runId);
        return Path.Combine(GetBaseDirectory(), sanitized);
    }
    
    public string GetArtifactPath(string runId, string filename)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filename);
        var sanitized = SanitizePathComponent(filename);
        return Path.Combine(GetRunDirectory(runId), sanitized);
    }
    
    public string GetStdoutPath(string runId) => 
        GetArtifactPath(runId, ArtifactConstants.FileNames.Stdout);
    
    public string GetStderrPath(string runId) => 
        GetArtifactPath(runId, ArtifactConstants.FileNames.Stderr);
    
    public string GetMetadataPath(string runId) => 
        GetArtifactPath(runId, ArtifactConstants.FileNames.Metadata);
    
    public string GetTestResultsPath(string runId) => 
        GetArtifactPath(runId, ArtifactConstants.FileNames.TestResults);
    
    private static string SanitizePathComponent(string component)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(component.Length);
        
        foreach (var c in component)
        {
            sanitized.Append(invalidChars.Contains(c) ? '_' : c);
        }
        
        return sanitized.ToString();
    }
}

// RunIdGenerator.cs
namespace Acode.Infrastructure.Artifacts;

public sealed class RunIdGenerator : IRunIdGenerator
{
    private readonly IArtifactPathResolver _pathResolver;
    private readonly IFileSystem _fileSystem;
    private readonly object _lock = new();
    
    public RunIdGenerator(
        IArtifactPathResolver pathResolver,
        IFileSystem fileSystem)
    {
        _pathResolver = pathResolver;
        _fileSystem = fileSystem;
    }
    
    public string Generate(string taskName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskName);
        
        var sanitizedTask = SanitizeTaskName(taskName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var baseId = $"{sanitizedTask}-{timestamp}";
        
        lock (_lock)
        {
            var runId = baseId;
            var sequence = 0;
            
            while (_fileSystem.Directory.Exists(_pathResolver.GetRunDirectory(runId)))
            {
                sequence++;
                runId = $"{baseId}-{sequence}";
            }
            
            return runId;
        }
    }
    
    private static string SanitizeTaskName(string taskName)
    {
        return Regex.Replace(taskName.ToLowerInvariant(), @"[ _]", "-");
    }
}

// ArtifactWriter.cs
namespace Acode.Infrastructure.Artifacts;

public sealed class ArtifactWriter : IArtifactWriter
{
    private readonly IArtifactPathResolver _pathResolver;
    private readonly IArtifactDirectoryManager _dirManager;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<ArtifactWriter> _logger;
    
    public async Task WriteAsync(
        string runId, 
        string filename, 
        string content, 
        CancellationToken ct = default)
    {
        var path = _pathResolver.GetArtifactPath(runId, filename);
        await _dirManager.EnsureRunDirectoryAsync(runId, ct);
        
        // Atomic write: write to temp, then move
        var tempPath = path + ".tmp";
        try
        {
            await _fileSystem.File.WriteAllTextAsync(tempPath, content, Encoding.UTF8, ct);
            _fileSystem.File.Move(tempPath, path, overwrite: true);
            _logger.LogDebug("Wrote artifact {Path}", path);
        }
        catch
        {
            if (_fileSystem.File.Exists(tempPath))
            {
                _fileSystem.File.Delete(tempPath);
            }
            throw;
        }
    }
    
    public async Task WriteMetadataAsync(
        string runId, 
        RunMetadata metadata, 
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(metadata, JsonOptions.Pretty);
        await WriteAsync(runId, ArtifactConstants.FileNames.Metadata, json, ct);
    }
    
    public async Task AppendAsync(
        string runId, 
        string filename, 
        string content, 
        CancellationToken ct = default)
    {
        var path = _pathResolver.GetArtifactPath(runId, filename);
        await _dirManager.EnsureRunDirectoryAsync(runId, ct);
        await _fileSystem.File.AppendAllTextAsync(path, content, Encoding.UTF8, ct);
    }
}
```

### Error Codes

| Code | Meaning | Recovery |
|------|---------|----------|
| ACODE-ART-001 | Base directory creation failed | Check permissions |
| ACODE-ART-002 | Run directory creation failed | Check disk space |
| ACODE-ART-003 | Artifact write failed | Check disk space and permissions |
| ACODE-ART-004 | Run ID collision | System handles automatically |
| ACODE-ART-005 | Disk space exhausted | Run `acode artifacts prune` |
| ACODE-ART-006 | Permission denied | Check file/directory permissions |
| ACODE-ART-007 | Corrupted metadata | Delete and recreate run |
| ACODE-ART-008 | Path too long | Shorten task names or paths |
| ACODE-ART-009 | Invalid artifact path | Check for special characters |
| ACODE-ART-010 | Prune failed | Check for locked files |

### Implementation Checklist

- [ ] Create domain models and interfaces
- [ ] Implement `ArtifactPathResolver` with cross-platform support
- [ ] Implement `RunIdGenerator` with collision handling
- [ ] Implement `ArtifactDirectoryManager` for directory creation
- [ ] Implement `ArtifactWriter` with atomic writes
- [ ] Implement `ArtifactReader` for artifact retrieval
- [ ] Implement `RetentionPolicyEnforcer` for cleanup
- [ ] Ensure `.gitignore` creation
- [ ] Create CLI `artifacts list` command
- [ ] Create CLI `artifacts show` command
- [ ] Create CLI `artifacts cat` command
- [ ] Create CLI `artifacts stats` command
- [ ] Create CLI `artifacts prune` command
- [ ] Add unit tests for all components
- [ ] Add integration tests for lifecycle
- [ ] Add E2E tests for CLI commands
- [ ] Document configuration in user manual

### Rollout Plan

| Phase | Action | Validation |
|-------|--------|------------|
| 1 | Implement path resolution | Unit tests pass |
| 2 | Implement run ID generator | Uniqueness tests pass |
| 3 | Implement directory management | Creation tests pass |
| 4 | Implement artifact writing | Write tests pass |
| 5 | Implement retention policy | Prune tests pass |
| 6 | Add CLI commands | E2E tests pass |
| 7 | Integration testing | Full lifecycle works |
| 8 | Documentation and release | User manual complete |

---

**End of Task 021.a Specification**