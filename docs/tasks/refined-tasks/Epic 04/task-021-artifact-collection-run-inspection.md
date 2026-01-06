# Task 021: Artifact Collection + Run Inspection

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 018 (Command Runner), Task 050 (Workspace Database)  

---

## Description

### Overview

Task 021 implements the artifact collection and run inspection subsystem for Agentic Coding Bot. Every command execution, build operation, test run, and code generation produces artifacts that must be captured, organized, stored, and made accessible for later inspection. This system provides the historical record of all agent activities, enabling debugging, auditing, and continuous improvement.

The artifact collection system operates transparently during every agent operation, automatically capturing outputs without requiring explicit user action. Run records create a queryable history of all executions, while artifact storage provides structured access to the actual outputs, logs, and generated files.

### Business Value

Comprehensive artifact collection and run inspection deliver critical value across multiple dimensions:

1. **Debugging Capability** — When builds fail or tests break, developers can inspect exactly what happened, including full command output, exit codes, and timing information
2. **Audit Trail** — Enterprise environments require complete records of all automated operations for compliance and security review
3. **Reproducibility** — Captured artifacts enable reproduction of issues by providing the exact context of failed operations
4. **Performance Analysis** — Historical run data enables identification of performance trends, slow operations, and optimization opportunities
5. **Learning Opportunity** — By reviewing what the agent did, users can understand its decision-making and improve their workflows
6. **Rollback Support** — Artifacts preserve previous states, enabling recovery when changes cause problems
7. **Team Collaboration** — Sharable run bundles allow team members to review and discuss agent operations
8. **Cost Tracking** — Run records with duration and resource usage support cost analysis for agent operations

### Scope

This task delivers the following capabilities:

1. **RunRecord Model** — Immutable record of each execution with unique ID, command, result, timing, and artifact references
2. **IRunStore Interface** — Abstraction for run persistence with CRUD operations and query support
3. **Artifact Collection Pipeline** — Automatic capture of stdout, stderr, logs, test results, and output files
4. **Artifact Storage** — Organized file-based storage with configurable retention policies
5. **Run Queries** — Flexible filtering by time range, status, command pattern, session, and task
6. **Run Inspection CLI** — Commands to list, show, and export runs and their artifacts
7. **Retention Management** — Automatic cleanup of old runs based on configurable policies
8. **Export Bundles** — Portable packages containing run records and artifacts for sharing

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 018 Command Runner | Upstream | Receives execution results and captures artifacts |
| Task 019 Language Runners | Upstream | Collects language-specific build/test artifacts |
| Task 020 Docker Sandbox | Upstream | Extracts artifacts from container execution |
| Task 050 Workspace Database | Downstream | Persists run records in workspace SQLite |
| Task 014 RepoFS | Sibling | Reads generated files for artifact collection |
| Epic 07 Output Modes | Downstream | Uses run data for summary reports |
| Epic 10 Telemetry | Downstream | Aggregates run metrics for dashboards |
| CLI Layer | Downstream | Exposes inspection commands to users |
| Session Manager | Sibling | Correlates runs with sessions |

### Failure Modes

| Failure | Detection | Impact | Recovery |
|---------|-----------|--------|----------|
| Disk full during artifact write | IOException | Artifacts lost for current run | Alert user, skip artifact, continue |
| Database write failure | SqliteException | Run record not persisted | Retry with backoff, log warning |
| Corrupt artifact file | Checksum mismatch | Artifact unreadable | Mark as corrupt, exclude from queries |
| Query timeout | Operation timeout | Results not returned | Return partial results with warning |
| Retention job fails | Job exception | Old artifacts accumulate | Retry next cycle, alert if repeated |
| Export bundle too large | Size limit exceeded | Bundle creation fails | Split into multiple bundles |
| Artifact path collision | Duplicate path | Overwrite risk | Generate unique paths with timestamp |
| Missing referenced artifact | File not found | Incomplete run data | Return run with missing artifact flag |
| Concurrent write conflict | Optimistic lock failure | Write rejected | Retry with new version |
| Invalid query parameters | Validation failure | Query not executed | Return validation error message |

### Assumptions

1. Workspace database (Task 050) is available for run record persistence
2. Local filesystem has sufficient space for artifact storage
3. Command execution (Task 018) provides structured result objects
4. Session management provides correlation IDs
5. Artifact directories are writable by the agent process
6. File paths use forward slashes for cross-platform compatibility
7. Timestamps use UTC for consistency
8. Run IDs are globally unique (GUID-based)
9. Artifact content is treated as binary (no encoding assumptions)
10. Retention policies are configured in agent-config.yml

### Security Considerations

1. **Sensitive Content** — Artifacts may contain secrets; storage must respect .gitignore patterns
2. **Access Control** — Run inspection should respect workspace permissions
3. **Export Sanitization** — Export bundles should have option to redact sensitive content
4. **Path Traversal** — Artifact paths must be validated to prevent directory traversal attacks
5. **Size Limits** — Individual artifacts and total storage must have configurable limits
6. **Retention Compliance** — Some artifacts may need retention for compliance; others deletion
7. **Audit Logging** — All artifact access should be logged for security review
8. **Encryption at Rest** — Sensitive artifacts should support optional encryption

### Subtask Decomposition

| Subtask | Title | Scope |
|---------|-------|-------|
| Task 021.a | Artifact Directory Standards | Directory structure, naming conventions, metadata format |
| Task 021.b | Run Inspection CLI | CLI commands for listing, showing, and filtering runs |
| Task 021.c | Export Bundle Format | Bundle specification, creation, and import commands |

---

## Use Cases

### Use Case 1: DevBot Debugging Failed CI/CD Build

**Persona:** DevBot (Automated CI/CD Agent)
**Scenario:** Production build fails in CI pipeline with cryptic error

**Before Artifact Collection:**
- CI logs show "Build failed with exit code 1" without details
- Developer must re-run build locally with verbose logging enabled
- Reproduction attempts fail due to environment differences
- Investigation time: 45 minutes average per build failure
- Cost: $37.50 per incident × 48 incidents/year = $1,800/year per developer

**After Artifact Collection:**
- Run record captures full stdout/stderr, exit code, timing
- Artifacts include build logs, test results, intermediate artifacts
- Developer uses `acode run show <run-id> --artifacts` to see everything
- Identifies issue in 5 minutes by reviewing artifact details
- Investigation time: 5 minutes (89% reduction)
- Cost: $4.16 per incident × 48 incidents/year = $200/year

**Savings:** $1,600/year per developer, $16,000/year for 10-developer team

**Commands Used:**
```bash
# CI pipeline captures run automatically
acode task run build

# Developer inspects failed run
acode run list --status failed --last 24h
acode run show <run-id>
acode run show <run-id> --artifacts
acode artifact cat <run-id> build.log
```

---

### Use Case 2: Jordan Security Audit Trail Compliance

**Persona:** Jordan (Security Engineer)
**Scenario:** Annual SOC 2 audit requires proof of all automated code changes

**Before Artifact Collection:**
- Manual tracking of agent operations in spreadsheets
- No automatic capture of what the agent did
- Audit preparation requires 40 hours to reconstruct history
- Missing data leads to audit findings requiring remediation
- Cost: 40 hours × $100/hour = $4,000/audit

**After Artifact Collection:**
- Every agent operation automatically creates immutable run record
- Artifacts include commands executed, files modified, results
- Export bundle creates portable audit trail with signatures
- Audit queries answered in minutes with `acode run list --export`
- Audit preparation: 2 hours (95% reduction)
- Cost: 2 hours × $100/hour = $200/audit

**Savings:** $3,800/audit, $3,800/year assuming 1 audit/year

**Commands Used:**
```bash
# Query all runs in audit period
acode run list --from 2024-01-01 --to 2024-12-31 --format jsonl > audit.jsonl

# Export bundle for specific session
acode run export --session <session-id> --output audit-bundle.zip

# Verify bundle integrity
acode run verify audit-bundle.zip

# List all file modifications
acode run list --command "write" --format csv > file-changes.csv
```

---

### Use Case 3: Alex Performance Optimization Analysis

**Persona:** Alex (DevOps Engineer)
**Scenario:** Identify slow operations to optimize agent performance

**Before Artifact Collection:**
- No historical performance data for agent operations
- Optimization decisions based on guesswork and anecdotes
- Performance regressions go unnoticed until severe
- Manual timing instrumentation required for each investigation
- Cost: 8 hours/month × $125/hour = $1,000/month investigation time

**After Artifact Collection:**
- Run records include start time, end time, duration for every operation
- Historical data enables trend analysis and regression detection
- Queries identify consistently slow operations: `acode run list --duration-gt 60s`
- Performance dashboard shows P50/P95/P99 latencies
- Investigation time: 30 minutes/month (94% reduction)
- Cost: 30 min/month × $125/hour = $62.50/month

**Savings:** $937.50/month, $11,250/year per engineer

**Commands Used:**
```bash
# Find slowest operations
acode run list --duration-gt 60s --limit 10

# Analyze specific command performance
acode run list --command "dotnet test" --format csv | \
  awk -F',' '{sum+=$5; count++} END {print "Average:", sum/count "s"}'

# Export performance data for visualization
acode run list --from -7d --format jsonl | \
  jq '.duration_ms' | \
  python3 plot_distribution.py

# Detect regressions by comparing time periods
acode run stats --command "npm run build" --from -30d --to -15d
acode run stats --command "npm run build" --from -15d --to now
```

---

### Aggregate ROI Summary

| Use Case | Persona | Annual Savings | Team Savings (10) |
|----------|---------|----------------|-------------------|
| CI/CD Build Debugging | DevBot | $1,600 | $16,000 |
| Security Audit Compliance | Jordan | $3,800 | $3,800 |
| Performance Optimization | Alex | $11,250 | $112,500 |
| **Total** | | **$16,650** | **$132,300** |

---

## Glossary

| Term | Definition |
|------|------------|
| **Run Record** | Immutable database record capturing a single command execution with metadata (ID, command, result, timing, artifacts). Each run represents one agent operation. |
| **Artifact** | Output file, log, or data produced during a run (e.g., build.log, test-results.xml, generated code). Stored in artifact directory and referenced by run record. |
| **Run ID** | Globally unique identifier (GUID/ULID) for a run record. Used to query, retrieve, and correlate artifacts. Format: `01HQRS7TGKMWXY123ABC456DEF`. |
| **Artifact Collection** | Automated process of capturing artifacts during command execution. Includes stdout/stderr streaming, file copying, metadata extraction. Transparent to user. |
| **Artifact Directory** | Structured filesystem location storing artifacts. Default: `.agent/artifacts/<run-id>/`. Includes manifest.json metadata file. See Task 021a for standards. |
| **Run Store** | Persistence layer (SQLite database) storing run records. Provides CRUD operations, queries, and retention management. Implements IRunStore interface. |
| **Run Inspection** | Process of querying and viewing historical run records and artifacts. Accessed via `acode run list`, `acode run show`, `acode artifact cat` commands. |
| **Export Bundle** | Portable ZIP archive containing run records and artifacts. Used for sharing, auditing, or backup. Includes checksums and manifest. See Task 021c for format. |
| **Retention Policy** | Rules determining how long run records and artifacts are kept. Configurable by age (days), count (max runs), size (GB), or status (keep failures longer). |
| **Run Status** | Enumeration indicating execution outcome: Success (exit 0), Failure (exit ≠ 0), Timeout (exceeded limit), Cancelled (user/system interrupted). |
| **Run Query** | Filtering operation to find runs matching criteria (status, time range, command pattern, session, duration). Returns paginated result set. |
| **Artifact Reference** | Pointer from run record to artifact file. Includes path, size, checksum (SHA-256), and content type. Stored in run record metadata. |
| **Run Duration** | Elapsed time for command execution in milliseconds. Calculated as `end_time - start_time`. Used for performance analysis and timeout enforcement. |
| **Session Correlation** | Link between run record and parent session ID. Enables grouping runs by user interaction or CI pipeline. One session = many runs. |
| **Artifact Manifest** | JSON file (`manifest.json`) in artifact directory listing all artifacts with metadata (name, size, checksum, timestamp). Enables integrity verification. |
| **Run Metadata** | Additional key-value data attached to run record. Examples: agent version, workspace ID, task name, environment variables. Stored as JSON blob. |
| **Stdout/Stderr** | Standard output and error streams from command execution. Captured as artifacts `stdout.txt` and `stderr.txt` with interleaved `combined.txt` for chronological view. |
| **Exit Code** | Integer returned by command process indicating success (0) or failure (non-zero). Stored in run record for result determination. Null if timeout/cancel. |
| **Truncation** | Process of limiting artifact size to prevent disk exhaustion. Default: 10MB per artifact. Truncated files include marker and byte count. See Task 007c for rules. |
| **Redaction** | Removal of sensitive content (secrets, API keys, passwords) from artifacts before export or display. Pattern-based scanning with configurable rules. See Task 050e. |

---

## Out of Scope

This task explicitly does NOT include the following capabilities (deferred to future tasks or other components):

1. **Real-time Streaming UI** — No live-updating dashboard showing command progress. Use CLI polling or JSONL event stream (Task 010b) instead.
2. **Artifact Search Content** — No full-text search inside artifact files. Query by run metadata only. Content search deferred to Epic 03 (Repo Intelligence).
3. **Artifact Diffing** — No built-in comparison between artifacts from different runs. Users must export and use external diff tools.
4. **Artifact Compression** — No automatic compression (gzip, zstd) of individual artifacts. Export bundles (Task 021c) are compressed, but stored artifacts are plain files.
5. **Distributed Storage** — No object storage (S3, Azure Blob) integration. All artifacts stored on local filesystem. Cloud storage deferred to Epic 07 (Cloud Burst).
6. **Artifact Deduplication** — No content-addressed storage to eliminate duplicate artifacts. Each run stores full artifacts even if identical to previous runs.
7. **Artifact Annotations** — No user comments or tags on individual artifacts. Annotations on run records only, via metadata.
8. **Artifact Preview** — No syntax highlighting, image thumbnails, or HTML rendering of artifacts. Use `acode artifact cat <run-id> <file>` and pipe to external viewers.
9. **Retention Policies by Artifact Type** — No granular rules like "keep test results 7 days, keep logs 30 days". Single policy applies to entire run and all artifacts.
10. **Concurrent Run Artifact Merging** — No automatic combining of artifacts from parallel task executions. Each run has isolated artifact directory.
11. **Artifact Encryption at Rest** — No built-in encryption for stored artifacts. Use filesystem-level encryption (LUKS, FileVault, BitLocker) instead.
12. **Artifact Signing** — No digital signatures on individual artifacts. Export bundles (Task 021c) include manifest checksum, but not per-artifact signatures.
13. **Artifact Quota Enforcement per User** — No per-user or per-workspace storage limits. Global retention policy only. User quotas deferred to Epic 09 (Policy Engine).
14. **Artifact Access Control Lists** — No fine-grained permissions on who can view artifacts. Workspace-level access only. ACLs deferred to Epic 09 (Safety/Policy).
15. **Artifact Telemetry Aggregation** — No automatic metrics extraction from artifacts (e.g., test pass rates, code coverage). Manual analysis required. Telemetry deferred to Epic 10.

---

## Functional Requirements

### Run Record Model (FR-021-01 through FR-021-20)

| ID | Requirement |
|----|-------------|
| FR-021-01 | RunRecord MUST have a unique GUID identifier |
| FR-021-02 | RunRecord MUST store the executed command string |
| FR-021-03 | RunRecord MUST store command arguments array |
| FR-021-04 | RunRecord MUST store working directory path |
| FR-021-05 | RunRecord MUST store execution result (Success/Failure/Timeout/Cancelled) |
| FR-021-06 | RunRecord MUST store exit code as nullable integer |
| FR-021-07 | RunRecord MUST store start timestamp in UTC |
| FR-021-08 | RunRecord MUST store end timestamp in UTC |
| FR-021-09 | RunRecord MUST store duration in milliseconds |
| FR-021-10 | RunRecord MUST store session ID for correlation |
| FR-021-11 | RunRecord MUST store optional task ID for correlation |
| FR-021-12 | RunRecord MUST store optional parent run ID for nested runs |
| FR-021-13 | RunRecord MUST store run type (Command/Build/Test/Generate) |
| FR-021-14 | RunRecord MUST store execution context (Host/Docker/Remote) |
| FR-021-15 | RunRecord MUST store list of artifact IDs |
| FR-021-16 | RunRecord MUST store optional error message |
| FR-021-17 | RunRecord MUST store optional error code |
| FR-021-18 | RunRecord MUST store tags as key-value pairs |
| FR-021-19 | RunRecord MUST be immutable after creation |
| FR-021-20 | RunRecord MUST serialize to JSON for export |

### Artifact Model (FR-021-21 through FR-021-40)

| ID | Requirement |
|----|-------------|
| FR-021-21 | Artifact MUST have a unique GUID identifier |
| FR-021-22 | Artifact MUST have an artifact type (Stdout/Stderr/Log/TestResult/Output/Diff) |
| FR-021-23 | Artifact MUST store file path relative to artifact root |
| FR-021-24 | Artifact MUST store original file name |
| FR-021-25 | Artifact MUST store content size in bytes |
| FR-021-26 | Artifact MUST store content hash (SHA256) |
| FR-021-27 | Artifact MUST store MIME type |
| FR-021-28 | Artifact MUST store creation timestamp |
| FR-021-29 | Artifact MUST store optional description |
| FR-021-30 | Artifact MUST reference parent run ID |
| FR-021-31 | Artifact MUST support binary content |
| FR-021-32 | Artifact MUST support text content with encoding |
| FR-021-33 | Artifact MUST support streaming for large files |
| FR-021-34 | Artifact MUST track whether content is truncated |
| FR-021-35 | Artifact MUST store truncation reason if applicable |
| FR-021-36 | Artifact MUST support compression for storage |
| FR-021-37 | Artifact MUST validate content hash on read |
| FR-021-38 | Artifact MUST support metadata tags |
| FR-021-39 | Artifact MUST track sensitive content flag |
| FR-021-40 | Artifact MUST serialize to JSON for export |

### Run Store Interface (FR-021-41 through FR-021-60)

| ID | Requirement |
|----|-------------|
| FR-021-41 | IRunStore MUST define CreateAsync method |
| FR-021-42 | IRunStore MUST define GetByIdAsync method |
| FR-021-43 | IRunStore MUST define ListAsync with filtering |
| FR-021-44 | IRunStore MUST define DeleteAsync method |
| FR-021-45 | IRunStore MUST define CountAsync method |
| FR-021-46 | IRunStore MUST define ExistsAsync method |
| FR-021-47 | IRunStore MUST support pagination parameters |
| FR-021-48 | IRunStore MUST support sorting parameters |
| FR-021-49 | IRunStore MUST define GetLatestAsync method |
| FR-021-50 | IRunStore MUST define GetBySessionAsync method |
| FR-021-51 | IRunStore MUST define GetByTaskAsync method |
| FR-021-52 | IRunStore MUST define GetFailedAsync method |
| FR-021-53 | IRunStore MUST define DeleteOlderThanAsync method |
| FR-021-54 | IRunStore MUST define GetStatisticsAsync method |
| FR-021-55 | IRunStore MUST persist to workspace database |
| FR-021-56 | IRunStore MUST support transactions |
| FR-021-57 | IRunStore MUST handle concurrent access |
| FR-021-58 | IRunStore MUST emit events on create/delete |
| FR-021-59 | IRunStore MUST validate run records before save |
| FR-021-60 | IRunStore MUST enforce storage limits |

### Artifact Store Interface (FR-021-61 through FR-021-80)

| ID | Requirement |
|----|-------------|
| FR-021-61 | IArtifactStore MUST define SaveAsync method |
| FR-021-62 | IArtifactStore MUST define GetByIdAsync method |
| FR-021-63 | IArtifactStore MUST define GetByRunIdAsync method |
| FR-021-64 | IArtifactStore MUST define DeleteAsync method |
| FR-021-65 | IArtifactStore MUST define GetContentAsync method |
| FR-021-66 | IArtifactStore MUST define GetContentStreamAsync method |
| FR-021-67 | IArtifactStore MUST define ExistsAsync method |
| FR-021-68 | IArtifactStore MUST store content on filesystem |
| FR-021-69 | IArtifactStore MUST store metadata in database |
| FR-021-70 | IArtifactStore MUST compute and verify hashes |
| FR-021-71 | IArtifactStore MUST compress large artifacts |
| FR-021-72 | IArtifactStore MUST truncate oversized artifacts |
| FR-021-73 | IArtifactStore MUST support artifact size limits |
| FR-021-74 | IArtifactStore MUST cleanup orphaned files |
| FR-021-75 | IArtifactStore MUST handle special characters in names |
| FR-021-76 | IArtifactStore MUST create directories as needed |
| FR-021-77 | IArtifactStore MUST respect filesystem permissions |
| FR-021-78 | IArtifactStore MUST support atomic writes |
| FR-021-79 | IArtifactStore MUST emit events on save/delete |
| FR-021-80 | IArtifactStore MUST provide storage statistics |

### Artifact Collection (FR-021-81 through FR-021-100)

| ID | Requirement |
|----|-------------|
| FR-021-81 | Collector MUST capture stdout from command execution |
| FR-021-82 | Collector MUST capture stderr from command execution |
| FR-021-83 | Collector MUST capture combined output if configured |
| FR-021-84 | Collector MUST capture log files from working directory |
| FR-021-85 | Collector MUST capture test result files (*.trx, *.xml) |
| FR-021-86 | Collector MUST capture coverage reports if generated |
| FR-021-87 | Collector MUST capture build outputs if configured |
| FR-021-88 | Collector MUST capture diff files if generated |
| FR-021-89 | Collector MUST respect .gitignore patterns |
| FR-021-90 | Collector MUST respect artifact exclusion patterns |
| FR-021-91 | Collector MUST limit individual artifact size |
| FR-021-92 | Collector MUST limit total artifacts per run |
| FR-021-93 | Collector MUST detect and skip binary files if configured |
| FR-021-94 | Collector MUST preserve file timestamps |
| FR-021-95 | Collector MUST handle file access errors gracefully |
| FR-021-96 | Collector MUST run asynchronously without blocking |
| FR-021-97 | Collector MUST support custom artifact patterns |
| FR-021-98 | Collector MUST deduplicate identical artifacts |
| FR-021-99 | Collector MUST tag artifacts by source (stdout/file/etc) |
| FR-021-100 | Collector MUST complete within configured timeout |

### Run Queries (FR-021-101 through FR-021-120)

| ID | Requirement |
|----|-------------|
| FR-021-101 | Query MUST support filtering by time range (start/end) |
| FR-021-102 | Query MUST support filtering by relative time (last N hours) |
| FR-021-103 | Query MUST support filtering by status (success/failure) |
| FR-021-104 | Query MUST support filtering by exit code |
| FR-021-105 | Query MUST support filtering by command pattern (wildcard) |
| FR-021-106 | Query MUST support filtering by command pattern (regex) |
| FR-021-107 | Query MUST support filtering by session ID |
| FR-021-108 | Query MUST support filtering by task ID |
| FR-021-109 | Query MUST support filtering by run type |
| FR-021-110 | Query MUST support filtering by execution context |
| FR-021-111 | Query MUST support filtering by tags |
| FR-021-112 | Query MUST support filtering by duration range |
| FR-021-113 | Query MUST support filtering by has-artifacts |
| FR-021-114 | Query MUST support multiple filters (AND logic) |
| FR-021-115 | Query MUST support pagination (skip/take) |
| FR-021-116 | Query MUST support sorting by any field |
| FR-021-117 | Query MUST support ascending/descending order |
| FR-021-118 | Query MUST return total count with paginated results |
| FR-021-119 | Query MUST timeout after configurable duration |
| FR-021-120 | Query MUST return empty result for no matches (not error) |

### Retention Management (FR-021-121 through FR-021-135)

| ID | Requirement |
|----|-------------|
| FR-021-121 | Retention MUST support time-based cleanup (delete after N days) |
| FR-021-122 | Retention MUST support count-based cleanup (keep last N runs) |
| FR-021-123 | Retention MUST support size-based cleanup (max storage size) |
| FR-021-124 | Retention MUST support status-based rules (keep failures longer) |
| FR-021-125 | Retention MUST support tagged-runs exclusion (never delete tagged) |
| FR-021-126 | Retention MUST run on configurable schedule |
| FR-021-127 | Retention MUST run on startup if enabled |
| FR-021-128 | Retention MUST be triggerable manually via CLI |
| FR-021-129 | Retention MUST delete artifacts when deleting runs |
| FR-021-130 | Retention MUST log all deletions for audit |
| FR-021-131 | Retention MUST respect minimum retention period |
| FR-021-132 | Retention MUST handle large cleanup batches efficiently |
| FR-021-133 | Retention MUST not block other operations during cleanup |
| FR-021-134 | Retention MUST report cleanup statistics |
| FR-021-135 | Retention MUST support dry-run mode |

---

## Non-Functional Requirements

### Performance (NFR-021-01 through NFR-021-15)

| ID | Requirement |
|----|-------------|
| NFR-021-01 | Run record creation MUST complete in under 50ms |
| NFR-021-02 | Artifact save MUST achieve 100MB/s throughput |
| NFR-021-03 | Run query with 10,000 records MUST return in under 500ms |
| NFR-021-04 | Artifact content read MUST stream without loading full content |
| NFR-021-05 | Concurrent artifact writes MUST be supported (10+ parallel) |
| NFR-021-06 | Database queries MUST use indexes for all filter fields |
| NFR-021-07 | Retention cleanup MUST process 1000 runs in under 60 seconds |
| NFR-021-08 | Memory usage during collection MUST stay under 100MB |
| NFR-021-09 | Large artifact streaming MUST not buffer entire content |
| NFR-021-10 | Export bundle creation MUST achieve 50MB/s throughput |
| NFR-021-11 | Run list pagination MUST return first page in under 100ms |
| NFR-021-12 | Artifact hash computation MUST be parallelized |
| NFR-021-13 | Compression MUST reduce storage by 50%+ for text artifacts |
| NFR-021-14 | Cold query (no cache) MUST complete in under 1 second |
| NFR-021-15 | Hot query (cached) MUST complete in under 100ms |

### Reliability (NFR-021-16 through NFR-021-28)

| ID | Requirement |
|----|-------------|
| NFR-021-16 | Run records MUST survive application restart |
| NFR-021-17 | Artifacts MUST survive application restart |
| NFR-021-18 | Database corruption MUST be recoverable |
| NFR-021-19 | Orphaned artifacts MUST be detected and cleaned |
| NFR-021-20 | Partial writes MUST be rolled back atomically |
| NFR-021-21 | Disk full errors MUST be handled gracefully |
| NFR-021-22 | Database locked errors MUST be retried with backoff |
| NFR-021-23 | File permission errors MUST be reported clearly |
| NFR-021-24 | Hash verification failures MUST be logged and reported |
| NFR-021-25 | Missing artifacts MUST not break run queries |
| NFR-021-26 | Concurrent access MUST not corrupt data |
| NFR-021-27 | Export bundles MUST include integrity verification |
| NFR-021-28 | Import MUST validate bundle integrity before applying |

### Security (NFR-021-29 through NFR-021-38)

| ID | Requirement |
|----|-------------|
| NFR-021-29 | Artifact paths MUST be validated against traversal attacks |
| NFR-021-30 | Sensitive artifacts MUST be flagged and protected |
| NFR-021-31 | Export bundles MUST support content redaction |
| NFR-021-32 | Database MUST be protected from SQL injection |
| NFR-021-33 | File operations MUST use safe path handling |
| NFR-021-34 | Artifact content MUST not be executed |
| NFR-021-35 | Access to artifacts MUST be logged for audit |
| NFR-021-36 | Deletion events MUST be logged for audit |
| NFR-021-37 | Encrypted artifacts MUST use AES-256 |
| NFR-021-38 | Secrets in stdout/stderr SHOULD be detected and masked |

### Maintainability (NFR-021-39 through NFR-021-48)

| ID | Requirement |
|----|-------------|
| NFR-021-39 | All components MUST have interfaces for testing |
| NFR-021-40 | Database schema MUST support migrations |
| NFR-021-41 | Configuration MUST be external (agent-config.yml) |
| NFR-021-42 | Logging MUST use structured format with correlation |
| NFR-021-43 | Error messages MUST include actionable guidance |
| NFR-021-44 | Storage format MUST be versioned for compatibility |
| NFR-021-45 | Export bundle format MUST be versioned |
| NFR-021-46 | Unit tests MUST achieve 90% coverage |
| NFR-021-47 | Integration tests MUST cover all query patterns |
| NFR-021-48 | Documentation MUST include schema diagrams |

### Observability (NFR-021-49 through NFR-021-60)

| ID | Requirement |
|----|-------------|
| NFR-021-49 | Run creation MUST emit metric |
| NFR-021-50 | Artifact save MUST emit metric with size |
| NFR-021-51 | Query duration MUST emit metric |
| NFR-021-52 | Retention cleanup MUST emit metric with count |
| NFR-021-53 | Storage usage MUST be exposed as metric |
| NFR-021-54 | Failed operations MUST emit error metric |
| NFR-021-55 | Run success rate MUST be calculable from metrics |
| NFR-021-56 | Average run duration MUST be calculable from metrics |
| NFR-021-57 | Health check MUST report storage availability |
| NFR-021-58 | Health check MUST report database connectivity |
| NFR-021-59 | Alerts MUST fire when storage exceeds threshold |
| NFR-021-60 | Alerts MUST fire when retention fails |

---

## Acceptance Criteria

### Run Record Model (AC-021-01 to AC-021-15)

- [ ] AC-021-01: RunRecord MUST have unique GUID Id property
- [ ] AC-021-02: RunRecord MUST have Command string property
- [ ] AC-021-03: RunRecord MUST have Arguments array property
- [ ] AC-021-04: RunRecord MUST have WorkingDirectory property
- [ ] AC-021-05: RunRecord MUST have Result enum property
- [ ] AC-021-06: RunRecord MUST have ExitCode nullable int property
- [ ] AC-021-07: RunRecord MUST have StartTime DateTimeOffset property
- [ ] AC-021-08: RunRecord MUST have EndTime DateTimeOffset property
- [ ] AC-021-09: RunRecord MUST have Duration TimeSpan property
- [ ] AC-021-10: RunRecord MUST have SessionId string property
- [ ] AC-021-11: RunRecord MUST have TaskId optional property
- [ ] AC-021-12: RunRecord MUST have ArtifactIds list property
- [ ] AC-021-13: RunRecord MUST have Tags dictionary property
- [ ] AC-021-14: RunRecord MUST serialize to JSON
- [ ] AC-021-15: RunRecord MUST be immutable (init-only setters)

### Artifact Model (AC-021-16 to AC-021-30)

- [ ] AC-021-16: Artifact MUST have unique GUID Id property
- [ ] AC-021-17: Artifact MUST have Type enum property
- [ ] AC-021-18: Artifact MUST have RelativePath string property
- [ ] AC-021-19: Artifact MUST have FileName string property
- [ ] AC-021-20: Artifact MUST have Size long property
- [ ] AC-021-21: Artifact MUST have Hash string property
- [ ] AC-021-22: Artifact MUST have MimeType string property
- [ ] AC-021-23: Artifact MUST have CreatedAt DateTimeOffset property
- [ ] AC-021-24: Artifact MUST have RunId GUID property
- [ ] AC-021-25: Artifact MUST have IsTruncated bool property
- [ ] AC-021-26: Artifact MUST have IsCompressed bool property
- [ ] AC-021-27: Artifact MUST have IsSensitive bool property
- [ ] AC-021-28: Artifact content MUST be retrievable as stream
- [ ] AC-021-29: Artifact content MUST be retrievable as string for text
- [ ] AC-021-30: Artifact MUST serialize to JSON

### Run Store (AC-021-31 to AC-021-45)

- [ ] AC-021-31: IRunStore MUST define CreateAsync method
- [ ] AC-021-32: IRunStore MUST define GetByIdAsync method
- [ ] AC-021-33: IRunStore MUST define ListAsync with RunQuery parameter
- [ ] AC-021-34: IRunStore MUST define DeleteAsync method
- [ ] AC-021-35: IRunStore MUST define CountAsync method
- [ ] AC-021-36: CreateAsync MUST return created RunRecord
- [ ] AC-021-37: GetByIdAsync MUST return null for non-existent ID
- [ ] AC-021-38: ListAsync MUST return PagedResult with items and total
- [ ] AC-021-39: DeleteAsync MUST return bool indicating success
- [ ] AC-021-40: Implementation MUST persist to SQLite database
- [ ] AC-021-41: Implementation MUST handle concurrent writes
- [ ] AC-021-42: Implementation MUST validate records before save
- [ ] AC-021-43: Implementation MUST create database tables on first use
- [ ] AC-021-44: Implementation MUST support migrations for schema changes
- [ ] AC-021-45: Implementation MUST dispose resources properly

### Artifact Store (AC-021-46 to AC-021-60)

- [ ] AC-021-46: IArtifactStore MUST define SaveAsync method
- [ ] AC-021-47: IArtifactStore MUST define GetByIdAsync method
- [ ] AC-021-48: IArtifactStore MUST define GetByRunIdAsync method
- [ ] AC-021-49: IArtifactStore MUST define DeleteAsync method
- [ ] AC-021-50: IArtifactStore MUST define GetContentStreamAsync method
- [ ] AC-021-51: SaveAsync MUST compute and store hash
- [ ] AC-021-52: SaveAsync MUST compress text artifacts
- [ ] AC-021-53: SaveAsync MUST truncate oversized artifacts
- [ ] AC-021-54: GetContentStreamAsync MUST decompress on read
- [ ] AC-021-55: GetContentStreamAsync MUST verify hash on read
- [ ] AC-021-56: Implementation MUST store files in artifact directory
- [ ] AC-021-57: Implementation MUST create subdirectories by date
- [ ] AC-021-58: Implementation MUST handle special characters in names
- [ ] AC-021-59: Implementation MUST cleanup orphaned files
- [ ] AC-021-60: Implementation MUST respect size limits

### Artifact Collection (AC-021-61 to AC-021-70)

- [ ] AC-021-61: Collector MUST capture stdout automatically
- [ ] AC-021-62: Collector MUST capture stderr automatically
- [ ] AC-021-63: Collector MUST capture log files matching patterns
- [ ] AC-021-64: Collector MUST capture test result files
- [ ] AC-021-65: Collector MUST respect .gitignore patterns
- [ ] AC-021-66: Collector MUST respect exclusion patterns
- [ ] AC-021-67: Collector MUST limit artifact size per item
- [ ] AC-021-68: Collector MUST limit total artifacts per run
- [ ] AC-021-69: Collector MUST handle file access errors gracefully
- [ ] AC-021-70: Collector MUST run without blocking command execution

### Run Queries (AC-021-71 to AC-021-85)

- [ ] AC-021-71: RunQuery MUST support TimeRange filter
- [ ] AC-021-72: RunQuery MUST support Status filter
- [ ] AC-021-73: RunQuery MUST support ExitCode filter
- [ ] AC-021-74: RunQuery MUST support CommandPattern filter
- [ ] AC-021-75: RunQuery MUST support SessionId filter
- [ ] AC-021-76: RunQuery MUST support TaskId filter
- [ ] AC-021-77: RunQuery MUST support RunType filter
- [ ] AC-021-78: RunQuery MUST support Tags filter
- [ ] AC-021-79: RunQuery MUST support Skip/Take pagination
- [ ] AC-021-80: RunQuery MUST support SortBy field
- [ ] AC-021-81: RunQuery MUST support SortDescending flag
- [ ] AC-021-82: Queries MUST use database indexes
- [ ] AC-021-83: Queries MUST return PagedResult with TotalCount
- [ ] AC-021-84: Queries MUST handle empty results gracefully
- [ ] AC-021-85: Queries MUST timeout after configured duration

### Retention (AC-021-86 to AC-021-95)

- [ ] AC-021-86: Retention MUST support MaxAge configuration
- [ ] AC-021-87: Retention MUST support MaxCount configuration
- [ ] AC-021-88: Retention MUST support MaxSize configuration
- [ ] AC-021-89: Retention MUST exclude tagged runs from deletion
- [ ] AC-021-90: Retention MUST delete artifacts when deleting runs
- [ ] AC-021-91: Retention MUST run on configurable schedule
- [ ] AC-021-92: Retention MUST be triggerable via CLI
- [ ] AC-021-93: Retention MUST log all deletions
- [ ] AC-021-94: Retention MUST support dry-run mode
- [ ] AC-021-95: Retention MUST report cleanup statistics

### CLI Commands (AC-021-96 to AC-021-110)

- [ ] AC-021-96: `acode runs list` MUST list recent runs
- [ ] AC-021-97: `acode runs list --status` MUST filter by status
- [ ] AC-021-98: `acode runs list --since` MUST filter by time
- [ ] AC-021-99: `acode runs list --session` MUST filter by session
- [ ] AC-021-100: `acode runs show <id>` MUST show run details
- [ ] AC-021-101: `acode runs show <id>` MUST list artifacts
- [ ] AC-021-102: `acode runs artifacts <id>` MUST list run artifacts
- [ ] AC-021-103: `acode runs artifact <id>` MUST show artifact content
- [ ] AC-021-104: `acode runs delete <id>` MUST delete run and artifacts
- [ ] AC-021-105: `acode runs cleanup` MUST trigger retention
- [ ] AC-021-106: `acode runs cleanup --dry-run` MUST show what would delete
- [ ] AC-021-107: `acode runs export <id>` MUST create export bundle
- [ ] AC-021-108: `acode runs stats` MUST show run statistics
- [ ] AC-021-109: CLI MUST support --json output format
- [ ] AC-021-110: CLI MUST handle errors gracefully with codes

---

## User Manual Documentation

### Overview

The run inspection system provides complete visibility into all agent operations. Every command executed, build performed, and test run creates a run record with associated artifacts. You can query, inspect, and export these records to understand what the agent did and debug any issues.

### Configuration

Configure run collection in `agent-config.yml`:

```yaml
runs:
  enabled: true
  
  # Artifact collection settings
  artifacts:
    enabled: true
    directory: ".acode/artifacts"
    maxSizePerArtifact: "10MB"
    maxArtifactsPerRun: 50
    compressText: true
    collectPatterns:
      - "*.log"
      - "*.trx"
      - "TestResults/**"
      - "coverage/**"
    excludePatterns:
      - "*.exe"
      - "*.dll"
      - "node_modules/**"
  
  # Retention settings
  retention:
    enabled: true
    maxAge: "30d"        # Delete runs older than 30 days
    maxCount: 1000       # Keep at most 1000 runs
    maxSize: "1GB"       # Total storage limit
    keepFailures: "90d"  # Keep failures longer
    preserveTags:        # Never delete runs with these tags
      - "important"
      - "investigation"
    schedule: "0 2 * * *"  # Run cleanup at 2 AM daily
```

### CLI Commands

#### List Runs

```bash
# List the 20 most recent runs
acode runs list

# List with more results
acode runs list --limit 50

# Filter by status
acode runs list --status failed
acode runs list --status success

# Filter by time
acode runs list --since "1 hour ago"
acode runs list --since "2024-01-15"
acode runs list --until "2024-01-16"

# Filter by session
acode runs list --session abc123

# Filter by command pattern
acode runs list --command "dotnet*"
acode runs list --command "*test*"

# Filter by run type
acode runs list --type build
acode runs list --type test

# Combine filters
acode runs list --status failed --since "1 day ago" --type test

# Output as JSON
acode runs list --json

# Sort by duration
acode runs list --sort duration --desc
```

#### Show Run Details

```bash
# Show detailed information about a run
acode runs show <run-id>

# Example output:
# Run: a1b2c3d4-e5f6-7890-abcd-ef1234567890
# ──────────────────────────────────────────
# Status:    Failed
# Command:   dotnet test
# Arguments: --no-build --logger trx
# Directory: /projects/myapp
# Exit Code: 1
# Started:   2024-01-15 14:30:00 UTC
# Ended:     2024-01-15 14:30:45 UTC
# Duration:  45.2 seconds
# Session:   session-abc123
# Task:      task-456
#
# Artifacts (3):
#   1. stdout.txt (12.5 KB) - Standard output
#   2. stderr.txt (1.2 KB) - Standard error
#   3. TestResults/results.trx (45.0 KB) - Test results

# Show with artifact content preview
acode runs show <run-id> --preview
```

#### View Artifacts

```bash
# List artifacts for a run
acode runs artifacts <run-id>

# View artifact content
acode runs artifact <artifact-id>

# Save artifact to file
acode runs artifact <artifact-id> --output ./saved.log

# View stdout from a run
acode runs stdout <run-id>

# View stderr from a run
acode runs stderr <run-id>
```

#### Delete Runs

```bash
# Delete a specific run and its artifacts
acode runs delete <run-id>

# Delete with confirmation prompt
acode runs delete <run-id> --confirm

# Force delete without confirmation
acode runs delete <run-id> --force
```

#### Cleanup and Retention

```bash
# Manually trigger retention cleanup
acode runs cleanup

# Dry run - show what would be deleted
acode runs cleanup --dry-run

# Force cleanup with custom criteria
acode runs cleanup --older-than "7 days"
acode runs cleanup --keep-last 100

# Show storage statistics
acode runs stats

# Example output:
# Run Statistics
# ──────────────
# Total Runs:      1,234
# Successful:      1,100 (89.1%)
# Failed:          134 (10.9%)
# Total Artifacts: 5,678
# Storage Used:    256.7 MB
# Oldest Run:      2024-01-01 10:00:00
# Latest Run:      2024-01-15 14:30:00
# Avg Duration:    23.4 seconds
```

#### Export and Import

```bash
# Export a run to a bundle file
acode runs export <run-id> --output ./run-bundle.zip

# Export multiple runs
acode runs export --since "1 day ago" --output ./daily-runs.zip

# Export with sensitive content redacted
acode runs export <run-id> --redact --output ./safe-bundle.zip

# Import a run bundle (for review, not execution)
acode runs import ./run-bundle.zip
```

#### Tagging Runs

```bash
# Add a tag to a run
acode runs tag <run-id> important

# Add multiple tags
acode runs tag <run-id> important investigation

# Remove a tag
acode runs untag <run-id> investigation

# List runs by tag
acode runs list --tag important
```

### Programmatic Access

Access run data from code:

```csharp
// Inject IRunStore
public class MyService
{
    private readonly IRunStore _runStore;
    private readonly IArtifactStore _artifactStore;
    
    public MyService(IRunStore runStore, IArtifactStore artifactStore)
    {
        _runStore = runStore;
        _artifactStore = artifactStore;
    }
    
    public async Task AnalyzeRecentFailuresAsync()
    {
        // Query failed runs from last 24 hours
        var query = new RunQuery
        {
            Status = RunStatus.Failed,
            Since = DateTimeOffset.UtcNow.AddDays(-1),
            SortBy = "StartTime",
            SortDescending = true,
            Take = 100
        };
        
        var result = await _runStore.ListAsync(query);
        
        foreach (var run in result.Items)
        {
            Console.WriteLine($"{run.StartTime}: {run.Command} (exit {run.ExitCode})");
            
            // Get artifacts for this run
            var artifacts = await _artifactStore.GetByRunIdAsync(run.Id);
            foreach (var artifact in artifacts)
            {
                Console.WriteLine($"  - {artifact.FileName} ({artifact.Size} bytes)");
            }
        }
    }
}
```

### Artifact Directory Structure

Artifacts are stored in a structured directory hierarchy:

```
.acode/artifacts/
├── 2024/
│   ├── 01/
│   │   ├── 15/
│   │   │   ├── a1b2c3d4-e5f6-7890-abcd-ef1234567890/
│   │   │   │   ├── stdout.txt.gz
│   │   │   │   ├── stderr.txt.gz
│   │   │   │   ├── TestResults/
│   │   │   │   │   └── results.trx.gz
│   │   │   │   └── manifest.json
│   │   │   └── [other run directories]
│   │   └── 16/
│   │       └── [...]
│   └── 02/
│       └── [...]
└── manifest.db  # SQLite database with metadata
```

### Troubleshooting

#### "Artifact not found" Error

**Cause:** The artifact file was deleted but the database record remains.

**Solution:**
```bash
# Run cleanup to sync database with filesystem
acode runs cleanup --orphans
```

#### "Storage limit exceeded" Warning

**Cause:** Total artifact storage has reached the configured limit.

**Solution:**
```bash
# Check current usage
acode runs stats

# Manually cleanup old runs
acode runs cleanup --older-than "7 days"

# Or increase limit in configuration
# runs.retention.maxSize: "2GB"
```

#### Slow Queries

**Cause:** Database indexes may need rebuilding after many deletions.

**Solution:**
```bash
# Optimize the database
acode runs optimize
```

#### Missing stdout/stderr

**Cause:** Artifact collection may have failed or been disabled.

**Solution:**
1. Check `runs.artifacts.enabled` is `true` in config
2. Check artifact size limits aren't too small
3. Verify filesystem permissions on artifact directory

---

## Assumptions

This task makes the following assumptions about the system, environment, dependencies, and operational context:

### Technical Assumptions

1. **Workspace Database Available** — Task 050 (Workspace Database) is implemented and accessible for run record persistence with SQLite as the default provider
2. **Command Runner Integration** — Task 018 (Structured Command Runner) provides structured result objects including exit code, stdout, stderr, and timing information
3. **Filesystem Access** — Local filesystem is writable for artifact storage with sufficient permissions (user read/write access to `.agent/artifacts/` directory)
4. **GUID Generation** — System can generate globally unique identifiers (GUIDs/ULIDs) for run IDs with collision probability <1 in 10^18
5. **File System Paths** — All file paths use forward slashes (`/`) for cross-platform compatibility, even on Windows (converted internally as needed)
6. **Timestamp Precision** — System clock provides UTC timestamps with millisecond precision for accurate duration calculations and time-range queries
7. **JSON Serialization** — .NET System.Text.Json is available for serializing run metadata, artifact manifests, and export bundle metadata
8. **SHA-256 Hashing** — Cryptographic hash functions (SHA-256) are available for artifact integrity verification and checksum generation
9. **Async I/O Support** — .NET async file I/O (FileStream with async) is used for non-blocking artifact writes and reads to prevent thread pool exhaustion
10. **SQLite Version** — SQLite 3.35+ with FTS5 support is available (required for future artifact content search, but not used in this task)

### Operational Assumptions

11. **Artifact Directory Persistence** — `.agent/artifacts/` directory is persistent across agent restarts and not automatically cleaned by external processes
12. **Retention Policy Configuration** — Retention policies are defined in `agent-config.yml` with defaults: 30 days for successful runs, 90 days for failures
13. **Disk Space Monitoring** — External monitoring (not implemented in this task) alerts administrators before disk space exhaustion (e.g., <10% free)
14. **Single Agent Instance** — Only one agent instance per workspace runs at a time (concurrent writes to run store protected by database locking)
15. **User Permissions** — Agent process runs with user-level permissions, not elevated (no root/admin), with access limited to workspace directory
16. **Artifact Size Limits** — Default artifact size limit is 10MB per file (configurable), enforced by Task 007c (Truncation Rules)
17. **Retention Job Scheduling** — Retention cleanup runs daily at 2 AM local time (configurable), scheduled by background service (not implemented here)
18. **Export Bundle Size** — Export bundles are limited to 1GB compressed size to prevent memory exhaustion during ZIP creation
19. **Query Performance** — SQLite run store is indexed on `start_time`, `status`, and `session_id` columns for fast queries (indexes created by migration in Task 050a)
20. **Workspace Scoping** — All run records and artifacts are scoped to a single workspace (no cross-workspace queries or artifact sharing)

---

## Security Considerations

This section identifies security threats specific to artifact collection and run inspection, with complete C# mitigation implementations.

---

### Security Threat 1: Path Traversal in Artifact Storage

**Risk:** Malicious command output contains paths like `../../etc/passwd`, allowing artifacts to be written outside artifact directory and overwrite system files.

**Attack Scenario:**
1. Attacker controls command output (e.g., malicious npm package generates files)
2. Command stdout includes path: `../../../../../../tmp/malicious.sh`
3. Artifact collector naively writes to calculated path
4. Malicious script written to system temp directory
5. Script executed by other process or scheduled task
6. System compromise achieved

**Mitigation:**

Implement path validation and sanitization to ensure all artifact paths resolve within the artifact directory. Reject or sanitize any paths attempting traversal.

```csharp
// ArtifactPathValidator.cs
using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Artifacts.Security;

public sealed class ArtifactPathValidator
{
    private readonly ILogger<ArtifactPathValidator> _logger;
    private readonly string _artifactRootDirectory;

    public ArtifactPathValidator(
        ILogger<ArtifactPathValidator> logger,
        string artifactRootDirectory)
    {
        _logger = logger;
        _artifactRootDirectory = Path.GetFullPath(artifactRootDirectory); // Normalize root
    }

    public (bool IsValid, string SanitizedPath) ValidateAndSanitize(string runId, string relativePath)
    {
        // Remove dangerous characters
        var sanitized = relativePath
            .Replace("..", "__")          // Replace path traversal attempts
            .Replace(":", "_")            // Remove drive letter colons (Windows)
            .Replace("|", "_")            // Remove pipe characters
            .Replace("<", "_")            // Remove redirection
            .Replace(">", "_")
            .Replace("\"", "_")
            .Replace("*", "_")
            .Replace("?", "_");

        // Construct full path: root/runId/sanitizedRelativePath
        var runDirectory = Path.Combine(_artifactRootDirectory, runId);
        var fullPath = Path.Combine(runDirectory, sanitized);
        var normalizedPath = Path.GetFullPath(fullPath);

        // CRITICAL: Verify the normalized path is still inside artifact root
        if (!normalizedPath.StartsWith(_artifactRootDirectory, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "Path traversal attempt detected: {OriginalPath} -> {NormalizedPath}. " +
                "Path escapes artifact root: {ArtifactRoot}",
                relativePath, normalizedPath, _artifactRootDirectory);

            return (false, null);
        }

        // Verify it's inside the run directory
        if (!normalizedPath.StartsWith(runDirectory, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "Path attempts to escape run directory: {OriginalPath} -> {NormalizedPath}. " +
                "Run directory: {RunDirectory}",
                relativePath, normalizedPath, runDirectory);

            return (false, null);
        }

        _logger.LogDebug(
            "Artifact path validated: {OriginalPath} -> {SanitizedPath}",
            relativePath, normalizedPath);

        return (true, normalizedPath);
    }
}
```

**Validation Test:**

```bash
# Test path traversal prevention
cat > test-artifact.txt << 'EOF'
../../etc/passwd
../../../tmp/malicious.sh
C:\Windows\System32\malware.exe
normal-file.log
EOF

# Attempt to collect artifacts with traversal paths
acode artifact collect --run-id test-123 --file test-artifact.txt

# Expected: Traversal paths rejected, normal-file.log accepted
acode artifact list test-123
# Expected: Only "normal-file.log" in artifacts

# Check logs for rejection
acode log query --message "Path traversal attempt detected" --last 5m
# Expected: Logs show rejected paths with details
```

---

### Security Threat 2: Sensitive Data Exposure in Artifacts

**Risk:** Artifacts contain secrets (API keys, passwords, tokens) that are exported in bundles or displayed in CLI, leading to credential leakage.

**Attack Scenario:**
1. Build process includes API key in environment variable
2. Command stdout logs: `Using API_KEY=sk-abc123def456...`
3. Artifact collector captures full stdout
4. User exports bundle: `acode run export --run test-456 --output bundle.zip`
5. Bundle shared with teammate via Slack/email
6. Attacker intercepts bundle, extracts API key
7. Attacker uses key to access production API

**Mitigation:**

Implement content redaction scanner that detects and masks sensitive patterns in artifact content before storage or export.

```csharp
// SensitiveContentRedactor.cs
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Artifacts.Security;

public sealed class SensitiveContentRedactor
{
    private readonly ILogger<SensitiveContentRedactor> _logger;

    // Regex patterns for common secret formats
    private static readonly List<(Regex Pattern, string Name)> SecretPatterns = new()
    {
        (new Regex(@"(?i)(api[_-]?key|apikey)\s*[:=]\s*['""]?([a-z0-9_\-]{20,})['""]?", RegexOptions.Compiled), "API Key"),
        (new Regex(@"(?i)(password|passwd|pwd)\s*[:=]\s*['""]?([^\s'"";]{8,})['""]?", RegexOptions.Compiled), "Password"),
        (new Regex(@"(?i)(token|auth[_-]?token)\s*[:=]\s*['""]?([a-z0-9_\-\.]{20,})['""]?", RegexOptions.Compiled), "Auth Token"),
        (new Regex(@"(?i)(secret[_-]?key|secret)\s*[:=]\s*['""]?([a-z0-9_\-]{16,})['""]?", RegexOptions.Compiled), "Secret Key"),
        (new Regex(@"sk-[a-zA-Z0-9]{48}", RegexOptions.Compiled), "OpenAI API Key"),
        (new Regex(@"ghp_[a-zA-Z0-9]{36}", RegexOptions.Compiled), "GitHub Personal Access Token"),
        (new Regex(@"-----BEGIN (RSA |DSA |EC |OPENSSH )?PRIVATE KEY-----", RegexOptions.Compiled), "Private Key"),
        (new Regex(@"(?i)(aws[_-]?access[_-]?key[_-]?id)\s*[:=]\s*['""]?([A-Z0-9]{20})['""]?", RegexOptions.Compiled), "AWS Access Key"),
        (new Regex(@"(?i)(aws[_-]?secret[_-]?access[_-]?key)\s*[:=]\s*['""]?([A-Za-z0-9/+=]{40})['""]?", RegexOptions.Compiled), "AWS Secret Key"),
    };

    public SensitiveContentRedactor(ILogger<SensitiveContentRedactor> logger)
    {
        _logger = logger;
    }

    public (string RedactedContent, int RedactionCount) Redact(string content, string artifactPath)
    {
        if (string.IsNullOrEmpty(content))
        {
            return (content, 0);
        }

        var redacted = content;
        var totalRedactions = 0;

        foreach (var (pattern, name) in SecretPatterns)
        {
            var matches = pattern.Matches(redacted);
            if (matches.Count > 0)
            {
                _logger.LogWarning(
                    "Detected {Count} {SecretType} pattern(s) in artifact: {Path}. Redacting.",
                    matches.Count, name, artifactPath);

                redacted = pattern.Replace(redacted, match =>
                {
                    // Keep the key name, redact the value
                    var keyPart = match.Groups[1].Value;
                    var valuePart = match.Groups.Count > 2 ? match.Groups[2].Value : match.Value;

                    // Show first 4 chars, redact rest
                    var preview = valuePart.Length > 4
                        ? valuePart.Substring(0, 4) + "***REDACTED***"
                        : "***REDACTED***";

                    return match.Groups.Count > 2
                        ? $"{keyPart}={preview}"
                        : preview;
                });

                totalRedactions += matches.Count;
            }
        }

        if (totalRedactions > 0)
        {
            _logger.LogInformation(
                "Artifact redaction complete: {Path}. Total redactions: {Count}",
                artifactPath, totalRedactions);
        }

        return (redacted, totalRedactions);
    }
}
```

**Validation Test:**

```bash
# Create artifact with secrets
cat > test-secrets.log << 'EOF'
Connecting with API_KEY=sk-abc123def456ghi789jkl012mno345pqr678stu901vwx234
Password: SuperSecret123!
GitHub Token: ghp_1234567890abcdefghijklmnopqrstuv12345
EOF

# Collect artifact (automatic redaction)
acode artifact collect --run-id test-789 --file test-secrets.log

# Verify redaction
acode artifact cat test-789 test-secrets.log
# Expected:
# Connecting with API_KEY=sk-a***REDACTED***
# Password: Supe***REDACTED***
# GitHub Token: ghp_***REDACTED***

# Check redaction audit log
acode log query --message "Detected.*pattern.*Redacting" --last 5m
# Expected: Shows 3 redactions (API Key, Password, GitHub Token)
```

---

### Security Threat 3: Denial of Service via Large Artifacts

**Risk:** Malicious command generates massive output (GB-sized logs, infinite loops), filling disk and causing system-wide failures.

**Attack Scenario:**
1. Malicious npm package includes postinstall script: `while true; do echo "AAAA..."; done`
2. Command runner executes script, stdout produces infinite output
3. Artifact collector attempts to capture all stdout
4. Disk fills to 100% capacity
5. Database writes fail, agent crashes
6. System becomes unresponsive

**Mitigation:**

Implement size limits and quota enforcement at multiple levels: per-artifact, per-run, and workspace-wide. Truncate or reject artifacts exceeding limits.

```csharp
// ArtifactSizeEnforcer.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Artifacts.Security;

public sealed class ArtifactSizeEnforcer
{
    private readonly ILogger<ArtifactSizeEnforcer> _logger;
    private readonly long _maxArtifactSizeBytes;
    private readonly long _maxRunTotalSizeBytes;

    public ArtifactSizeEnforcer(
        ILogger<ArtifactSizeEnforcer> logger,
        long maxArtifactSizeBytes = 10 * 1024 * 1024,      // 10MB per artifact
        long maxRunTotalSizeBytes = 100 * 1024 * 1024)     // 100MB per run
    {
        _logger = logger;
        _maxArtifactSizeBytes = maxArtifactSizeBytes;
        _maxRunTotalSizeBytes = maxRunTotalSizeBytes;
    }

    public async Task<(bool IsAccepted, long BytesWritten, bool WasTruncated)> WriteWithSizeLimit(
        Stream sourceStream,
        string destinationPath,
        long currentRunTotalSize)
    {
        // Check if run already exceeds total size limit
        if (currentRunTotalSize >= _maxRunTotalSizeBytes)
        {
            _logger.LogError(
                "Run total size limit exceeded: {CurrentSize}MB >= {MaxSize}MB. " +
                "Artifact rejected: {Path}",
                currentRunTotalSize / (1024.0 * 1024.0),
                _maxRunTotalSizeBytes / (1024.0 * 1024.0),
                destinationPath);

            return (false, 0, false);
        }

        var remainingRunQuota = _maxRunTotalSizeBytes - currentRunTotalSize;
        var effectiveLimit = Math.Min(_maxArtifactSizeBytes, remainingRunQuota);

        using var destinationStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 8192,
            useAsync: true);

        var buffer = new byte[8192];
        long totalBytesWritten = 0;
        var wasTruncated = false;

        int bytesRead;
        while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            var bytesToWrite = bytesRead;

            // Check if writing these bytes would exceed limit
            if (totalBytesWritten + bytesToWrite > effectiveLimit)
            {
                // Write only up to the limit
                bytesToWrite = (int)(effectiveLimit - totalBytesWritten);
                wasTruncated = true;
            }

            await destinationStream.WriteAsync(buffer, 0, bytesToWrite);
            totalBytesWritten += bytesToWrite;

            if (wasTruncated)
            {
                // Append truncation marker
                var marker = System.Text.Encoding.UTF8.GetBytes(
                    $"\n\n[TRUNCATED: Artifact exceeded {effectiveLimit / (1024.0 * 1024.0):F2}MB limit. " +
                    $"Original size unknown. Truncated at {totalBytesWritten} bytes.]\n");

                await destinationStream.WriteAsync(marker, 0, marker.Length);
                totalBytesWritten += marker.Length;

                _logger.LogWarning(
                    "Artifact truncated due to size limit: {Path}. " +
                    "Limit: {LimitMB}MB, Written: {WrittenMB}MB",
                    destinationPath,
                    effectiveLimit / (1024.0 * 1024.0),
                    totalBytesWritten / (1024.0 * 1024.0));

                break;
            }
        }

        _logger.LogDebug(
            "Artifact written: {Path}. Size: {SizeMB}MB, Truncated: {Truncated}",
            destinationPath,
            totalBytesWritten / (1024.0 * 1024.0),
            wasTruncated);

        return (true, totalBytesWritten, wasTruncated);
    }
}
```

**Validation Test:**

```bash
# Generate large file (15MB, exceeds 10MB limit)
dd if=/dev/urandom of=large-artifact.bin bs=1M count=15

# Attempt to collect large artifact
acode artifact collect --run-id test-size --file large-artifact.bin

# Verify truncation
ls -lh .agent/artifacts/test-size/large-artifact.bin
# Expected: ~10MB (truncated)

tail -n 5 .agent/artifacts/test-size/large-artifact.bin
# Expected: [TRUNCATED: Artifact exceeded 10.00MB limit...]

# Check logs
acode log query --message "Artifact truncated" --last 5m
# Expected: Warning log with size details
```

---

### Security Threat 4: Unauthorized Access to Run Records

**Risk:** User queries run records from other workspaces or sessions, accessing sensitive command history and artifacts from other projects.

**Attack Scenario:**
1. Attacker knows or guesses run ID from another workspace
2. Executes: `acode run show <other-workspace-run-id>`
3. CLI does not validate workspace ownership
4. Attacker sees sensitive commands, API endpoints, infrastructure details
5. Attacker downloads artifacts: `acode artifact cat <other-run-id> credentials.json`
6. Credentials stolen, lateral movement to other systems

**Mitigation:**

Implement workspace scoping in all run queries and artifact access operations. Validate that requested run belongs to current workspace before returning data.

```csharp
// WorkspaceScopedRunStore.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Artifacts.Security;

public sealed class WorkspaceScopedRunStore : IRunStore
{
    private readonly IRunStore _innerStore;
    private readonly IWorkspaceContext _workspaceContext;
    private readonly ILogger<WorkspaceScopedRunStore> _logger;

    public WorkspaceScopedRunStore(
        IRunStore innerStore,
        IWorkspaceContext workspaceContext,
        ILogger<WorkspaceScopedRunStore> logger)
    {
        _innerStore = innerStore;
        _workspaceContext = workspaceContext;
        _logger = logger;
    }

    public async Task<RunRecord> GetByIdAsync(string runId)
    {
        var run = await _innerStore.GetByIdAsync(runId);

        if (run == null)
        {
            return null;
        }

        // CRITICAL: Verify run belongs to current workspace
        if (run.WorkspaceId != _workspaceContext.CurrentWorkspaceId)
        {
            _logger.LogWarning(
                "Unauthorized access attempt: User in workspace {CurrentWorkspace} " +
                "attempted to access run {RunId} from workspace {RunWorkspace}",
                _workspaceContext.CurrentWorkspaceId,
                runId,
                run.WorkspaceId);

            // Return null (not found) rather than permission error
            // Prevents workspace enumeration attacks
            return null;
        }

        return run;
    }

    public async Task<IReadOnlyList<RunRecord>> QueryAsync(RunQuery query)
    {
        // CRITICAL: Force workspace filter on all queries
        var scopedQuery = query with
        {
            WorkspaceId = _workspaceContext.CurrentWorkspaceId
        };

        _logger.LogDebug(
            "Run query scoped to workspace: {WorkspaceId}",
            _workspaceContext.CurrentWorkspaceId);

        return await _innerStore.QueryAsync(scopedQuery);
    }

    public async Task SaveAsync(RunRecord run)
    {
        // Ensure run is tagged with current workspace on creation
        if (string.IsNullOrEmpty(run.WorkspaceId))
        {
            run = run with { WorkspaceId = _workspaceContext.CurrentWorkspaceId };
        }

        // Verify workspace match
        if (run.WorkspaceId != _workspaceContext.CurrentWorkspaceId)
        {
            throw new InvalidOperationException(
                $"Cannot save run {run.Id} with workspace {run.WorkspaceId} " +
                $"when current workspace is {_workspaceContext.CurrentWorkspaceId}");
        }

        await _innerStore.SaveAsync(run);
    }
}
```

**Validation Test:**

```bash
# In workspace A, create a run
cd /workspace-a
acode task run build
# Note the run ID from output: run-abc-123

# Switch to workspace B
cd /workspace-b

# Attempt to access workspace A's run
acode run show run-abc-123
# Expected: Error - "Run not found" (not permission denied, prevents enumeration)

acode artifact cat run-abc-123 build.log
# Expected: Error - "Run not found"

# Verify current workspace runs are accessible
acode run list
# Expected: Only shows runs from workspace B

# Check audit logs for unauthorized access attempt
acode log query --message "Unauthorized access attempt" --last 5m
# Expected: Warning log showing workspace mismatch
```

---

### Security Threat 5: Artifact Tampering and Integrity Violation

**Risk:** Attacker modifies artifacts on disk after collection, altering evidence for audit trails or injecting malicious content into exported bundles.

**Attack Scenario:**
1. Agent collects artifacts for security audit run
2. Attacker gains filesystem access (compromised user account)
3. Modifies `.agent/artifacts/<run-id>/build.log` to remove evidence of malicious activity
4. Audit export bundle created: `acode run export --run audit-run-456`
5. Bundle contains tampered artifacts, audit finds no issues
6. Security incident goes undetected

**Mitigation:**

Implement artifact integrity verification using checksums. Store SHA-256 hashes in artifact manifest and database. Verify checksums before displaying or exporting artifacts.

```csharp
// ArtifactIntegrityVerifier.cs
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Artifacts.Security;

public sealed class ArtifactIntegrityVerifier
{
    private readonly ILogger<ArtifactIntegrityVerifier> _logger;

    public ArtifactIntegrityVerifier(ILogger<ArtifactIntegrityVerifier> logger)
    {
        _logger = logger;
    }

    public async Task<string> ComputeChecksumAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 8192,
            useAsync: true);

        var hashBytes = await sha256.ComputeHashAsync(fileStream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    public async Task<(bool IsValid, string ActualChecksum)> VerifyAsync(
        string filePath,
        string expectedChecksum)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogError(
                "Integrity verification failed: File not found: {Path}",
                filePath);

            return (false, null);
        }

        var actualChecksum = await ComputeChecksumAsync(filePath);

        // Use constant-time comparison to prevent timing attacks
        var isValid = ConstantTimeEquals(actualChecksum, expectedChecksum);

        if (!isValid)
        {
            _logger.LogError(
                "Artifact integrity violation detected: {Path}. " +
                "Expected checksum: {Expected}, Actual: {Actual}",
                filePath, expectedChecksum, actualChecksum);
        }
        else
        {
            _logger.LogDebug(
                "Artifact integrity verified: {Path}. Checksum: {Checksum}",
                filePath, actualChecksum);
        }

        return (isValid, actualChecksum);
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        if (a == null || b == null || a.Length != b.Length)
        {
            return false;
        }

        var result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}
```

**Validation Test:**

```bash
# Collect artifact with checksum
echo "Original content" > test.txt
acode artifact collect --run-id test-integrity --file test.txt

# Verify original checksum
acode artifact verify test-integrity test.txt
# Expected: ✓ Integrity verified (checksum matches)

# Tamper with artifact
echo "Modified content" > .agent/artifacts/test-integrity/test.txt

# Attempt to verify tampered artifact
acode artifact verify test-integrity test.txt
# Expected: ✗ Integrity violation detected (checksum mismatch)

# Attempt to export bundle with tampered artifact
acode run export --run test-integrity --output bundle.zip
# Expected: Error - "Artifact integrity violation: test.txt"

# Check audit logs
acode log query --message "integrity violation" --last 5m
# Expected: Error log with expected vs actual checksums
```

---

## Best Practices

### Artifact Collection

1. **Collect selectively** - Don't store everything; focus on relevant outputs
2. **Compress artifacts** - Reduce storage requirements
3. **Unique identifiers** - Every artifact has traceable ID
4. **Retain metadata** - Store source, timestamp, size alongside content

### Run Inspection

5. **Query by multiple criteria** - Filter by date, status, task type
6. **Paginate results** - Handle large run histories efficiently
7. **Structured output** - JSON format for automation
8. **Human-readable option** - Pretty output for manual inspection

### Storage Management

9. **Retention policies** - Auto-delete old runs and artifacts
10. **Size limits per run** - Prevent individual runs from consuming all storage
11. **Cleanup on failure** - Remove partial artifacts on failed runs
12. **Export for archival** - Bundle runs for long-term storage

---

## Troubleshooting

This section provides solutions to common issues encountered with artifact collection and run inspection.

---

### Issue 1: Run Records Not Being Created

**Symptoms:**
- Commands execute successfully but no run records appear in `acode run list`
- Database query returns empty result set
- Artifact directory `.agent/artifacts/` is empty or missing runs
- No run ID returned after command execution

**Causes:**
1. Run store not initialized or database connection failed
2. Workspace database (Task 050) not set up correctly
3. Command runner (Task 018) not integrated with artifact collector
4. Database write permissions denied
5. Run store disabled in configuration
6. Exception during run record creation silently caught

**Solutions:**

```bash
# Solution 1: Verify database exists and is accessible
ls -la .agent/workspace.db
# Expected: File exists with read/write permissions

# Solution 2: Check database connection
acode db status
# Expected: Connected, migrations applied

# Solution 3: Enable debug logging
export ACODE_LOG_LEVEL=Debug
acode task run test
# Check logs for "RunStore.SaveAsync" entries

# Solution 4: Verify run store configuration
acode config get runs.store.enabled
# Expected: true

# If disabled, enable it:
acode config set runs.store.enabled true

# Solution 5: Check database permissions
chmod 644 .agent/workspace.db
chmod 755 .agent/

# Solution 6: Test database write directly
acode db query "INSERT INTO runs (id, command, status) VALUES ('test-1', 'test', 'Success')"
# Expected: 1 row inserted

# Solution 7: Check for database locks
lsof .agent/workspace.db
# Expected: Only acode process, or empty

# Kill stale locks if found:
kill -9 <PID>
```

---

### Issue 2: Artifacts Not Being Collected from Command Output

**Symptoms:**
- Run record exists but artifact count is 0
- `acode artifact list <run-id>` shows no artifacts
- `stdout.txt` and `stderr.txt` files missing from artifact directory
- Command produces output but artifacts directory is empty

**Causes:**
1. Artifact collection disabled in configuration
2. Command output too large, exceeds size limits, silently truncated to zero
3. Artifact directory not writable
4. Path traversal validation rejected all artifact paths
5. Content redaction removed all content (entire artifact was secrets)
6. Artifact collector not subscribed to command output streams

**Solutions:**

```bash
# Solution 1: Verify artifact collection is enabled
acode config get runs.artifacts.enabled
# Expected: true

# If disabled, enable it:
acode config set runs.artifacts.enabled true

# Solution 2: Check artifact size limits
acode config get runs.artifacts.max-size-mb
# Expected: 10 or higher

# Increase if needed:
acode config set runs.artifacts.max-size-mb 50

# Solution 3: Verify artifact directory permissions
ls -ld .agent/artifacts/
# Expected: drwxr-xr-x (755)

chmod 755 .agent/artifacts/

# Solution 4: Check for path traversal rejections in logs
acode log query --message "Path traversal attempt" --last 1h
# If found, review artifact paths for "../" patterns

# Solution 5: Test artifact collection manually
echo "test output" | acode artifact collect --run-id test-manual --name test.txt
acode artifact list test-manual
# Expected: test.txt appears

# Solution 6: Check redaction is not removing everything
acode config get runs.artifacts.redaction-patterns
# Review patterns, ensure not too aggressive

# Temporarily disable redaction for testing:
acode config set runs.artifacts.redaction-enabled false

# Solution 7: Enable verbose command output
acode --verbose task run test 2>&1 | tee debug.log
# Review debug.log for "ArtifactCollector" entries
```

---

### Issue 3: Query Performance Degradation with Large Run History

**Symptoms:**
- `acode run list` takes >5 seconds to return results
- Database query timeouts after 30 seconds
- High CPU usage during run queries
- Paginated queries slow even with limits

**Causes:**
1. Missing database indexes on frequently queried columns
2. Run table has millions of rows without retention policy
3. Full table scans due to unoptimized queries
4. Database fragmentation after many deletions
5. Artifact directory has thousands of subdirectories causing filesystem slowness
6. SQLite journal mode is DELETE instead of WAL

**Solutions:**

```bash
# Solution 1: Verify indexes exist
acode db query "SELECT name FROM sqlite_master WHERE type='index' AND tbl_name='runs'"
# Expected: idx_runs_start_time, idx_runs_status, idx_runs_session_id

# If missing, create indexes (should be in migration, but can add manually):
acode db query "CREATE INDEX IF NOT EXISTS idx_runs_start_time ON runs(start_time DESC)"
acode db query "CREATE INDEX IF NOT EXISTS idx_runs_status ON runs(status)"
acode db query "CREATE INDEX IF NOT EXISTS idx_runs_session_id ON runs(session_id)"

# Solution 2: Check row count
acode db query "SELECT COUNT(*) FROM runs"
# If >100,000, implement retention policy

# Solution 3: Configure aggressive retention
acode config set runs.retention.max-age-days 30
acode config set runs.retention.max-count 10000

# Trigger manual retention cleanup:
acode run cleanup --dry-run
acode run cleanup --confirm

# Solution 4: Vacuum database to reclaim space
acode db vacuum
# Expected: Database size reduced, fragmentation removed

# Solution 5: Enable WAL mode for better concurrency
acode db query "PRAGMA journal_mode=WAL"
# Expected: wal

# Solution 6: Add composite indexes for common queries
acode db query "CREATE INDEX IF NOT EXISTS idx_runs_status_time ON runs(status, start_time DESC)"

# Solution 7: Use query filtering to reduce result set
acode run list --status failed --from -7d --limit 50
# Much faster than full scan
```

---

### Issue 4: Export Bundle Creation Fails with "File Too Large"

**Symptoms:**
- `acode run export` fails with error message
- Error: "Export bundle exceeds 1GB limit"
- ZIP creation process terminated mid-creation
- Export bundle file is corrupt or partially created

**Causes:**
1. Run has >1GB of artifacts (e.g., large build outputs, Docker images)
2. Export includes multiple runs with combined size >1GB
3. Compression ineffective for binary artifacts
4. Temporary directory insufficient space during ZIP creation
5. Export process out of memory

**Solutions:**

```bash
# Solution 1: Check artifact sizes before export
acode artifact summary <run-id>
# Shows total size, count, breakdown by file

# Solution 2: Export individual runs instead of session
acode run export --run <run-id> --output single-run.zip
# Avoids bundling multiple large runs

# Solution 3: Exclude large artifacts from export
acode run export --run <run-id> --exclude "*.bin,*.iso,*.tar" --output filtered.zip

# Solution 4: Use compression level 0 (store only) for speed
acode run export --run <run-id> --compression-level 0 --output uncompressed.zip

# Solution 5: Split into multiple bundles by time range
acode run export --from 2024-01-01 --to 2024-01-15 --output part1.zip
acode run export --from 2024-01-16 --to 2024-01-31 --output part2.zip

# Solution 6: Clean up large artifacts before export
acode artifact delete <run-id> large-file.bin
acode run export --run <run-id> --output cleaned.zip

# Solution 7: Increase export size limit in config
acode config set runs.export.max-size-gb 5
# Warning: May cause memory issues

# Solution 8: Free up temp directory space
df -h /tmp
# Ensure >5GB free

# Clean temp files:
rm -rf /tmp/acode-export-*
```

---

### Issue 5: Artifact Integrity Verification Fails After System Restore

**Symptoms:**
- `acode artifact verify` reports checksum mismatches
- Many artifacts show integrity violations
- Error: "Expected checksum: abc123, Actual: def456"
- Artifacts appear intact but verification fails

**Causes:**
1. Backup/restore process modified file timestamps, triggering re-computation
2. Filesystem corruption during system crash
3. Backup software altered line endings (LF ↔ CRLF) in text files
4. Checksums stored in database but artifacts restored from older backup
5. Database and artifact directory restored from different points in time
6. Symbolic links resolved differently after restore

**Solutions:**

```bash
# Solution 1: Recompute all checksums from current artifacts
acode artifact recompute-checksums --all
# Warning: This trusts current artifact content as source of truth

# Solution 2: Verify database and artifact timestamps match
acode db query "SELECT id, created_at FROM runs ORDER BY created_at DESC LIMIT 10"
ls -lt .agent/artifacts/ | head -10
# Check if timestamps are consistent

# Solution 3: Restore artifacts and database together
# Always backup together:
tar czf backup.tar.gz .agent/workspace.db .agent/artifacts/

# Restore together:
tar xzf backup.tar.gz -C .agent/

# Solution 4: Check for line ending changes
file .agent/artifacts/*/stdout.txt | grep CRLF
# If found, standardize line endings:
find .agent/artifacts/ -name "*.txt" -exec dos2unix {} \;

# Then recompute checksums:
acode artifact recompute-checksums --run <run-id>

# Solution 5: Compare database run count vs artifact directories
run_count=$(acode db query "SELECT COUNT(*) FROM runs" --format csv | tail -1)
artifact_dirs=$(ls .agent/artifacts/ | wc -l)
echo "Runs in DB: $run_count, Artifact directories: $artifact_dirs"
# If mismatch, determine which is authoritative and sync

# Solution 6: Use export bundles for archival instead
# Bundles include checksums in manifest, self-verifying:
acode run export --all --output full-backup-$(date +%Y%m%d).zip

# Solution 7: Disable strict verification temporarily
acode config set runs.artifacts.strict-verification false
# Allows access despite checksum mismatches (use with caution)
```

---

### Issue 6: Redaction Patterns Too Aggressive, Removing Valid Content

**Symptoms:**
- Artifacts displayed as `***REDACTED***` for non-sensitive content
- Legitimate configuration values masked in exports
- Error messages in logs show: "Detected 50+ redactions"
- Exported bundles unusable due to excessive redaction

**Causes:**
1. Redaction regex patterns too broad (e.g., matching all `key=` patterns)
2. Custom patterns added without sufficient testing
3. False positive matches on common words (e.g., "secret" in "secretariat")
4. Redaction applied to metadata fields unnecessarily
5. Redaction enabled for internal-only artifacts that won't be shared

**Solutions:**

```bash
# Solution 1: Review current redaction patterns
acode config get runs.artifacts.redaction-patterns
# Check for overly broad patterns

# Solution 2: Test patterns against sample data
echo "API_KEY=abc123 and secret meetings" > test.txt
acode artifact collect --run-id test-redaction --file test.txt
acode artifact cat test-redaction test.txt
# Review what was redacted

# Solution 3: Adjust patterns to be more specific
# Bad pattern: key=.*
# Good pattern: (?i)api[_-]?key[:=]\s*[a-z0-9]{20,}

acode config set runs.artifacts.redaction-patterns "[
  '(?i)api[_-]?key[:=]\\s*[a-z0-9]{20,}',
  '(?i)password[:=]\\s*[^\\s]{8,}',
  'sk-[a-zA-Z0-9]{48}'
]"

# Solution 4: Use word boundaries to prevent partial matches
# Bad: "secret" matches "secretariat"
# Good: "\bsecret\b" matches only full word "secret"

# Solution 5: Disable redaction for specific runs (internal use)
acode artifact collect --run-id internal-debug --no-redaction --file debug.log

# Solution 6: Use dry-run mode to preview redactions
acode artifact preview-redaction --file test.log
# Shows what would be redacted without actually redacting

# Solution 7: Export with original (unredacted) content for authorized users
acode run export --run <run-id> --no-redaction --output unredacted.zip
# Requires confirmation: "Export contains unredacted secrets. Continue? (yes/no)"
```

---

## Testing Requirements

### Unit Tests

#### RunRecordTests
- RunRecord_Constructor_SetsAllRequiredProperties
- RunRecord_Constructor_GeneratesUniqueId
- RunRecord_Duration_CalculatedFromStartAndEndTime
- RunRecord_ArtifactIds_DefaultsToEmptyList
- RunRecord_Tags_DefaultsToEmptyDictionary
- RunRecord_Serialize_ProducesValidJson
- RunRecord_Deserialize_ReconstructsRecord
- RunRecord_Immutable_PropertiesCannotBeModified
- RunRecord_Equals_ComparesById
- RunRecord_GetHashCode_ConsistentWithEquals

#### ArtifactTests
- Artifact_Constructor_SetsAllRequiredProperties
- Artifact_Constructor_GeneratesUniqueId
- Artifact_Hash_ComputedFromContent
- Artifact_MimeType_InferredFromFileName
- Artifact_Size_MatchesContentLength
- Artifact_IsTruncated_TrueWhenContentCut
- Artifact_IsCompressed_TrueWhenGzipped
- Artifact_Serialize_ProducesValidJson
- Artifact_Deserialize_ReconstructsArtifact
- Artifact_SensitiveFlag_DefaultsFalse

#### RunStoreTests
- RunStore_CreateAsync_PersistsRecord
- RunStore_CreateAsync_ReturnsCreatedRecord
- RunStore_CreateAsync_ValidatesRequiredFields
- RunStore_CreateAsync_RejectsDuplicateId
- RunStore_GetByIdAsync_ReturnsExistingRecord
- RunStore_GetByIdAsync_ReturnsNullForMissing
- RunStore_ListAsync_ReturnsAllRecords
- RunStore_ListAsync_AppliesStatusFilter
- RunStore_ListAsync_AppliesTimeRangeFilter
- RunStore_ListAsync_AppliesCommandPatternFilter
- RunStore_ListAsync_AppliesSessionFilter
- RunStore_ListAsync_AppliesTagsFilter
- RunStore_ListAsync_AppliesPagination
- RunStore_ListAsync_AppliesSorting
- RunStore_ListAsync_ReturnsTotalCount
- RunStore_ListAsync_ReturnsEmptyForNoMatches
- RunStore_DeleteAsync_RemovesRecord
- RunStore_DeleteAsync_ReturnsFalseForMissing
- RunStore_CountAsync_ReturnsCorrectCount
- RunStore_ExistsAsync_ReturnsTrueForExisting
- RunStore_ExistsAsync_ReturnsFalseForMissing
- RunStore_GetLatestAsync_ReturnsNewestRun
- RunStore_GetBySessionAsync_ReturnsSessionRuns
- RunStore_GetFailedAsync_ReturnsOnlyFailures
- RunStore_DeleteOlderThanAsync_RemovesOldRecords

#### ArtifactStoreTests
- ArtifactStore_SaveAsync_WritesFile
- ArtifactStore_SaveAsync_ComputesHash
- ArtifactStore_SaveAsync_CompressesText
- ArtifactStore_SaveAsync_TruncatesOversized
- ArtifactStore_SaveAsync_CreatesDirectory
- ArtifactStore_SaveAsync_HandlesSpecialCharacters
- ArtifactStore_GetByIdAsync_ReturnsExisting
- ArtifactStore_GetByIdAsync_ReturnsNullForMissing
- ArtifactStore_GetByRunIdAsync_ReturnsRunArtifacts
- ArtifactStore_GetContentStreamAsync_ReturnsStream
- ArtifactStore_GetContentStreamAsync_Decompresses
- ArtifactStore_GetContentStreamAsync_VerifiesHash
- ArtifactStore_GetContentStreamAsync_ThrowsOnHashMismatch
- ArtifactStore_DeleteAsync_RemovesFileAndRecord
- ArtifactStore_DeleteAsync_ReturnsFalseForMissing
- ArtifactStore_CleanupOrphans_RemovesOrphanedFiles

#### ArtifactCollectorTests
- Collector_Collect_CapturesStdout
- Collector_Collect_CapturesStderr
- Collector_Collect_CapturesLogFiles
- Collector_Collect_CapturesTestResults
- Collector_Collect_RespectsGitignore
- Collector_Collect_RespectsExcludePatterns
- Collector_Collect_RespectsIncludePatterns
- Collector_Collect_LimitsArtifactSize
- Collector_Collect_LimitsArtifactCount
- Collector_Collect_HandlesFileAccessError
- Collector_Collect_DeduplicatesIdenticalContent
- Collector_Collect_PreservesTimestamps
- Collector_Collect_TagsArtifactsBySource
- Collector_Collect_CompletesWithinTimeout

#### RetentionManagerTests
- Retention_Execute_DeletesOldRuns
- Retention_Execute_RespectsMaxAge
- Retention_Execute_RespectsMaxCount
- Retention_Execute_RespectsMaxSize
- Retention_Execute_KeepsFailuresLonger
- Retention_Execute_PreservesTaggedRuns
- Retention_Execute_DeletesAssociatedArtifacts
- Retention_Execute_LogsDeletions
- Retention_Execute_ReportsStatistics
- Retention_DryRun_DoesNotDelete
- Retention_DryRun_ReportsWhatWouldDelete

#### RunQueryTests
- RunQuery_Validate_AcceptsValidQuery
- RunQuery_Validate_RejectsNegativeSkip
- RunQuery_Validate_RejectsZeroTake
- RunQuery_Validate_RejectsInvalidSortField
- RunQuery_TimeRange_FiltersByStartTime
- RunQuery_TimeRange_FiltersByEndTime
- RunQuery_CommandPattern_SupportsWildcard
- RunQuery_CommandPattern_SupportsRegex
- RunQuery_Tags_MatchesAnyTag
- RunQuery_Combine_AndLogicForMultipleFilters

### Integration Tests

#### RunStoreIntegrationTests
- RunStore_PersistsThroughRestart
- RunStore_HandlesThousandsOfRecords
- RunStore_ConcurrentWrites_NoDataLoss
- RunStore_ConcurrentReadsWrites_NoCorruption
- RunStore_QueryPerformance_Under500ms
- RunStore_DatabaseMigration_PreservesData

#### ArtifactStoreIntegrationTests
- ArtifactStore_SavesAndRetrievesLargeFile
- ArtifactStore_StreamsWithoutFullLoad
- ArtifactStore_CompressionReducesSize
- ArtifactStore_HandlesParallelWrites
- ArtifactStore_OrphanCleanupWorks
- ArtifactStore_DirectoryStructureCorrect

#### EndToEndTests
- E2E_CommandExecution_CreatesRunAndArtifacts
- E2E_BuildExecution_CollectsOutputFiles
- E2E_TestExecution_CollectsTrxFile
- E2E_QueryByStatus_ReturnsCorrectRuns
- E2E_DeleteRun_RemovesArtifacts
- E2E_RetentionCleanup_EnforcesPolicy
- E2E_ExportBundle_ContainsAllData
- E2E_ImportBundle_RestoresRunData

### Benchmark Tests

| Benchmark | Target | Description |
|-----------|--------|-------------|
| RunRecord_Create | <50ms | Time to create and persist run record |
| Artifact_Save_1MB | <100ms | Time to save 1MB artifact |
| Artifact_Save_10MB | <500ms | Time to save 10MB artifact |
| Query_10kRecords | <500ms | Query with 10,000 run records |
| Query_Paginated | <100ms | Paginated query with large dataset |
| Retention_Cleanup_1000 | <60s | Delete 1000 runs with artifacts |
| Export_Bundle_100MB | <5s | Create 100MB export bundle |
| Hash_Computation_1MB | <50ms | SHA256 hash of 1MB content |

### Coverage Requirements

| Component | Minimum Coverage |
|-----------|-----------------|
| RunRecord | 95% |
| Artifact | 95% |
| RunStore | 90% |
| ArtifactStore | 90% |
| ArtifactCollector | 85% |
| RetentionManager | 90% |
| RunQuery | 95% |
| CLI Commands | 80% |

---

## User Verification Steps

### Scenario 1: Verify Run Record Creation

**Objective:** Confirm command execution creates run records

**Steps:**
1. Run a simple command: `acode exec -- echo "hello"`
2. Run: `acode runs list`
3. Observe the run appears in the list
4. Run: `acode runs show <run-id>`
5. Observe all run details

**Expected Results:**
- Run list shows the echo command
- Run details show exit code 0, status Success
- Duration is recorded
- Session ID is present
- No artifacts for simple echo (stdout may be captured)

### Scenario 2: Verify Artifact Collection

**Objective:** Confirm artifacts are captured from command output

**Steps:**
1. Navigate to a .NET test project
2. Run: `acode exec -- dotnet test --logger trx`
3. Run: `acode runs list`
4. Run: `acode runs show <run-id>`
5. Run: `acode runs artifacts <run-id>`
6. Run: `acode runs artifact <artifact-id>` for stdout

**Expected Results:**
- Run record created with test command
- Artifacts list shows stdout, stderr, and .trx file
- Stdout artifact contains test output
- .trx file artifact contains test results XML

### Scenario 3: Verify Query Filtering

**Objective:** Confirm queries filter correctly

**Steps:**
1. Run several commands (some succeed, some fail)
2. Run: `acode runs list --status failed`
3. Run: `acode runs list --status success`
4. Run: `acode runs list --since "5 minutes ago"`
5. Run: `acode runs list --command "dotnet*"`

**Expected Results:**
- Status filter shows only matching runs
- Time filter shows only recent runs
- Command filter shows only matching commands
- Filters can be combined

### Scenario 4: Verify Run Deletion

**Objective:** Confirm runs and artifacts are deleted together

**Steps:**
1. Run a command that produces artifacts
2. Note the run ID
3. Run: `acode runs artifacts <run-id>` to confirm artifacts
4. Run: `acode runs delete <run-id>`
5. Run: `acode runs show <run-id>`
6. Check filesystem for artifact directory

**Expected Results:**
- Delete confirms the operation
- Show returns "Run not found"
- Artifact directory is removed
- Database records are cleaned up

### Scenario 5: Verify Retention Cleanup

**Objective:** Confirm retention policies are enforced

**Steps:**
1. Configure retention with short maxAge (e.g., 1 minute) for testing
2. Run several commands
3. Wait for retention period
4. Run: `acode runs cleanup --dry-run`
5. Observe what would be deleted
6. Run: `acode runs cleanup`
7. Run: `acode runs list`

**Expected Results:**
- Dry run shows runs that would be deleted
- Cleanup actually deletes old runs
- Runs list shows only recent runs
- Cleanup reports statistics (N runs, M artifacts deleted)

### Scenario 6: Verify Export Bundle

**Objective:** Confirm runs can be exported and imported

**Steps:**
1. Run a command with artifacts
2. Run: `acode runs export <run-id> --output ./test-bundle.zip`
3. Examine the bundle contents
4. Delete the original run: `acode runs delete <run-id>`
5. Run: `acode runs import ./test-bundle.zip`
6. Run: `acode runs show <run-id>`

**Expected Results:**
- Export creates a zip file
- Bundle contains manifest.json and artifact files
- Import restores the run record
- Imported run shows correct details
- Artifacts are accessible after import

### Scenario 7: Verify Storage Statistics

**Objective:** Confirm storage statistics are accurate

**Steps:**
1. Run: `acode runs stats`
2. Note total runs and storage used
3. Run a command with output
4. Run: `acode runs stats` again
5. Verify counts increased

**Expected Results:**
- Stats show total run count
- Stats show success/failure percentages
- Stats show total storage used
- Stats show oldest and newest run dates
- Stats update after new runs

### Scenario 8: Verify Tagging

**Objective:** Confirm runs can be tagged and filtered by tag

**Steps:**
1. Run a command and note the run ID
2. Run: `acode runs tag <run-id> important`
3. Run: `acode runs list --tag important`
4. Configure retention to delete old runs
5. Run cleanup
6. Verify tagged run is preserved

**Expected Results:**
- Tag command succeeds
- Tag filter shows only tagged runs
- Retention preserves tagged runs
- Untag command removes tag

### Scenario 9: Verify Large Artifact Handling

**Objective:** Confirm large artifacts are truncated appropriately

**Steps:**
1. Create a command that outputs large content (> 10MB)
2. Run: `acode exec -- <large-output-command>`
3. Run: `acode runs artifacts <run-id>`
4. Check artifact size and truncation status
5. Run: `acode runs artifact <artifact-id>`

**Expected Results:**
- Artifact is stored (not rejected)
- Artifact size respects configured limit
- IsTruncated flag is true
- Content shows truncation message
- Original size is recorded

### Scenario 10: Verify Concurrent Run Handling

**Objective:** Confirm multiple concurrent runs are tracked separately

**Steps:**
1. Open two terminal windows
2. Start long-running commands in both (e.g., `sleep 10`)
3. Run: `acode runs list` in a third terminal
4. Observe both runs in progress
5. Wait for completion
6. Verify both runs have distinct records

**Expected Results:**
- Both runs appear in list
- Each has unique ID
- Start times may overlap
- Artifacts are correctly associated with each run
- No data corruption or mixing

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/Runs/
├── RunRecord.cs                    # Immutable run record model
├── RunResult.cs                    # Enum: Success, Failure, Timeout, Cancelled
├── RunType.cs                      # Enum: Command, Build, Test, Generate
├── ExecutionContext.cs             # Enum: Host, Docker, Remote
├── Artifact.cs                     # Artifact model
├── ArtifactType.cs                 # Enum: Stdout, Stderr, Log, TestResult, etc.
├── IRunStore.cs                    # Run persistence interface
├── IArtifactStore.cs               # Artifact persistence interface
├── RunQuery.cs                     # Query parameters model
├── PagedResult.cs                  # Paginated query result

src/AgenticCoder.Infrastructure/Runs/
├── RunStore.cs                     # SQLite-backed run store
├── ArtifactStore.cs                # Filesystem-backed artifact store
├── ArtifactCollector.cs            # Collects artifacts from execution
├── RetentionManager.cs             # Handles cleanup policies
├── RunStoreConfiguration.cs        # Configuration model
├── Database/
│   ├── RunsDbContext.cs            # EF Core or Dapper context
│   ├── Migrations/
│   │   └── CreateRunsSchema.cs     # Initial schema migration
│   └── RunRepository.cs            # Data access layer

src/AgenticCoder.CLI/Commands/
└── RunsCommand.cs                  # CLI subcommands for runs

tests/AgenticCoder.Infrastructure.Tests/Runs/
├── RunRecordTests.cs
├── ArtifactTests.cs
├── RunStoreTests.cs
├── ArtifactStoreTests.cs
├── ArtifactCollectorTests.cs
├── RetentionManagerTests.cs
└── Integration/
    ├── RunStoreIntegrationTests.cs
    └── ArtifactStoreIntegrationTests.cs
```

### RunRecord Model

```csharp
namespace AgenticCoder.Domain.Runs;

/// <summary>
/// Immutable record of a command execution.
/// </summary>
public sealed record RunRecord
{
    public required Guid Id { get; init; }
    public required string Command { get; init; }
    public required IReadOnlyList<string> Arguments { get; init; }
    public required string WorkingDirectory { get; init; }
    public required RunResult Result { get; init; }
    public int? ExitCode { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public TimeSpan Duration => EndTime - StartTime;
    public required string SessionId { get; init; }
    public string? TaskId { get; init; }
    public Guid? ParentRunId { get; init; }
    public required RunType Type { get; init; }
    public required ExecutionContext Context { get; init; }
    public IReadOnlyList<Guid> ArtifactIds { get; init; } = [];
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    public IReadOnlyDictionary<string, string> Tags { get; init; } = 
        new Dictionary<string, string>();
}

public enum RunResult { Success, Failure, Timeout, Cancelled }
public enum RunType { Command, Build, Test, Generate }
public enum ExecutionContext { Host, Docker, Remote }
```

### IRunStore Interface

```csharp
namespace AgenticCoder.Domain.Runs;

public interface IRunStore
{
    Task<RunRecord> CreateAsync(RunRecord record, CancellationToken ct = default);
    Task<RunRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<RunRecord>> ListAsync(RunQuery query, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> CountAsync(RunQuery? query = null, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<RunRecord?> GetLatestAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RunRecord>> GetBySessionAsync(string sessionId, CancellationToken ct = default);
    Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default);
    Task<RunStatistics> GetStatisticsAsync(CancellationToken ct = default);
}
```

### RunQuery Model

```csharp
namespace AgenticCoder.Domain.Runs;

public sealed record RunQuery
{
    public DateTimeOffset? Since { get; init; }
    public DateTimeOffset? Until { get; init; }
    public RunResult? Status { get; init; }
    public int? ExitCode { get; init; }
    public string? CommandPattern { get; init; }
    public bool CommandPatternIsRegex { get; init; }
    public string? SessionId { get; init; }
    public string? TaskId { get; init; }
    public RunType? Type { get; init; }
    public ExecutionContext? Context { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public TimeSpan? MinDuration { get; init; }
    public TimeSpan? MaxDuration { get; init; }
    public bool? HasArtifacts { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 20;
    public string SortBy { get; init; } = "StartTime";
    public bool SortDescending { get; init; } = true;
}
```

### Artifact Model

```csharp
namespace AgenticCoder.Domain.Runs;

public sealed record Artifact
{
    public required Guid Id { get; init; }
    public required Guid RunId { get; init; }
    public required ArtifactType Type { get; init; }
    public required string RelativePath { get; init; }
    public required string FileName { get; init; }
    public required long Size { get; init; }
    public required string Hash { get; init; }  // SHA256
    public required string MimeType { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? Description { get; init; }
    public bool IsTruncated { get; init; }
    public string? TruncationReason { get; init; }
    public bool IsCompressed { get; init; }
    public bool IsSensitive { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = 
        new Dictionary<string, string>();
}

public enum ArtifactType { Stdout, Stderr, Log, TestResult, Coverage, Output, Diff, Other }
```

### Error Codes

| Code | Meaning | User Message |
|------|---------|--------------|
| ACODE-RUN-001 | Run not found | "Run {0} not found. Use 'acode runs list' to see available runs." |
| ACODE-RUN-002 | Artifact not found | "Artifact {0} not found. It may have been deleted or corrupted." |
| ACODE-RUN-003 | Query timeout | "Query timed out. Try narrowing your filter criteria." |
| ACODE-RUN-004 | Storage limit exceeded | "Storage limit exceeded. Run 'acode runs cleanup' to free space." |
| ACODE-RUN-005 | Database error | "Database error occurred. Check logs for details." |
| ACODE-RUN-006 | Artifact write failed | "Failed to write artifact. Check disk space and permissions." |
| ACODE-RUN-007 | Hash mismatch | "Artifact corrupted: hash mismatch. Original data may be lost." |
| ACODE-RUN-008 | Export failed | "Failed to create export bundle: {0}" |
| ACODE-RUN-009 | Import failed | "Failed to import bundle: {0}" |
| ACODE-RUN-010 | Retention failed | "Retention cleanup failed: {0}" |

### CLI Implementation Pattern

```csharp
namespace AgenticCoder.CLI.Commands;

[Command("runs", Description = "Inspect command execution history")]
public sealed class RunsCommand
{
    [Command("list", Description = "List execution runs")]
    public async Task<int> ListAsync(
        [Option("status", Description = "Filter by status")] RunResult? status,
        [Option("since", Description = "Show runs since (e.g., '1 hour ago')")] string? since,
        [Option("until", Description = "Show runs until")] string? until,
        [Option("session", Description = "Filter by session ID")] string? session,
        [Option("command", Description = "Filter by command pattern")] string? command,
        [Option("type", Description = "Filter by run type")] RunType? type,
        [Option("tag", Description = "Filter by tag")] string? tag,
        [Option("limit", Description = "Max results")] int limit = 20,
        [Option("json", Description = "Output as JSON")] bool json,
        IRunStore runStore)
    {
        var query = new RunQuery
        {
            Status = status,
            Since = ParseTime(since),
            Until = ParseTime(until),
            SessionId = session,
            CommandPattern = command,
            Type = type,
            Tags = tag != null ? new[] { tag } : null,
            Take = limit,
            SortDescending = true
        };
        
        var result = await runStore.ListAsync(query);
        
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions.Pretty));
        }
        else
        {
            PrintRunTable(result.Items);
            Console.WriteLine($"\nShowing {result.Items.Count} of {result.TotalCount} runs");
        }
        
        return 0;
    }
    
    [Command("show", Description = "Show run details")]
    public async Task<int> ShowAsync(
        [Argument] Guid runId,
        [Option("preview", Description = "Preview artifact content")] bool preview,
        IRunStore runStore,
        IArtifactStore artifactStore)
    {
        var run = await runStore.GetByIdAsync(runId);
        if (run == null)
        {
            Console.Error.WriteLine($"Run {runId} not found");
            return 1;
        }
        
        PrintRunDetails(run);
        
        var artifacts = await artifactStore.GetByRunIdAsync(runId);
        PrintArtifactList(artifacts, preview);
        
        return 0;
    }
}
```

### Implementation Checklist

| Step | Task | Verification |
|------|------|--------------|
| 1 | Create RunRecord and related enums in Domain | Models compile, tests pass |
| 2 | Create Artifact model in Domain | Model compiles, tests pass |
| 3 | Create IRunStore interface in Domain | Interface compiles |
| 4 | Create IArtifactStore interface in Domain | Interface compiles |
| 5 | Create RunQuery and PagedResult in Domain | Models compile |
| 6 | Create database schema and migrations | Migration applies |
| 7 | Implement RunStore with SQLite | Unit tests pass |
| 8 | Implement ArtifactStore with filesystem | Unit tests pass |
| 9 | Implement ArtifactCollector | Collection works |
| 10 | Implement RetentionManager | Cleanup works |
| 11 | Add CLI commands | All commands functional |
| 12 | Integrate with CommandRunner | Runs auto-recorded |
| 13 | Write integration tests | All scenarios pass |
| 14 | Write benchmarks | Performance meets targets |
| 15 | Document in user manual | Documentation complete |

### Rollout Plan

| Phase | Action | Success Criteria |
|-------|--------|------------------|
| 1 | Implement Domain models | Models compile, unit tests pass |
| 2 | Implement RunStore | CRUD operations work |
| 3 | Implement ArtifactStore | File save/load works |
| 4 | Implement ArtifactCollector | Artifacts captured from runs |
| 5 | Implement CLI commands | List/show/delete work |
| 6 | Implement RetentionManager | Cleanup enforces policy |
| 7 | Implement Export/Import | Bundles create and import |
| 8 | Integration testing | All E2E tests pass |
| 9 | Documentation | User manual complete |
| 10 | Release | Feature available in CLI |

### Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.Data.Sqlite | 8.0.* | SQLite database access |
| System.IO.Compression | Built-in | Artifact compression |
| System.Text.Json | Built-in | JSON serialization |

---

**End of Task 021 Specification**