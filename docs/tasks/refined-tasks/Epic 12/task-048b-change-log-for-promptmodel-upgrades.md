# Task 048.b: Change Log for Prompt/Model Upgrades

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 12 – Hardening  
**Dependencies:** Task 048 (Baseline), Task 048.a (Recording)  

---

## Description

Task 048.b implements change logging for prompt and model upgrades—the systematic tracking of changes to prompts, model versions, and configurations that may impact benchmark results. Every time a prompt is modified, a model version is updated, or benchmark configuration changes, a log entry is created linking the change to baseline updates.

Change logging is essential for regression triage. When baselines differ, developers need to know what changed. This task provides: (1) automatic change detection via file hashes, (2) manual change annotations for external changes, (3) change categorization (prompt, model, config, suite), (4) change correlation with baselines, and (5) change history queries.

### Business Value

Change logging provides:
- Root cause tracing
- Change correlation
- Upgrade tracking
- Regression investigation
- Audit compliance

### Scope Boundaries

This task covers tracking changes. Recording baselines is Task 048.a. Triage workflow is Task 048.c. Baseline management is Task 048.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Baseline | Task 048 | Correlation | Links |
| Recording | Task 048.a | Trigger | Events |
| Triage | Task 048.c | Query | Analysis |
| Config | Task 002 | Detection | Source |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Missing detection | Hash mismatch | Manual entry | Gap in log |
| Corrupt log | Parse error | Rebuild | Lost history |
| Storage full | Write error | Prune old | Cannot log |
| Version conflict | Parse fail | Manual fix | Log error |

### Assumptions

1. **File access**: Prompt/config files readable
2. **Storage**: Log storage writable
3. **Git available**: Optional for version info
4. **Baselines exist**: For correlation
5. **Manual entries**: Allowed for external changes

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Change log | Record of modifications |
| Prompt change | Modification to prompt files |
| Model upgrade | Change in model version |
| Config change | Modification to settings |
| Suite change | Modification to task specs |
| Correlation | Linking change to baseline |
| Detection | Identifying a change |
| Hash | File content checksum |
| Annotation | Manual change note |
| Entry | Single log record |

---

## Out of Scope

- Automatic rollback
- Change approval workflow
- Diff visualization
- Real-time change detection
- External system integration
- Prompt version control

---

## Functional Requirements

### FR-001 to FR-020: Change Detection

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048b-01 | Detection MUST be automatic | P0 |
| FR-048b-02 | Prompt file changes MUST detect | P0 |
| FR-048b-03 | Model version changes MUST detect | P0 |
| FR-048b-04 | Config changes MUST detect | P0 |
| FR-048b-05 | Suite changes MUST detect | P0 |
| FR-048b-06 | Hash-based detection MUST work | P0 |
| FR-048b-07 | SHA-256 MUST be algorithm | P0 |
| FR-048b-08 | Previous hash MUST be stored | P0 |
| FR-048b-09 | Compare on baseline create MUST occur | P0 |
| FR-048b-10 | Compare on benchmark run MUST occur | P0 |
| FR-048b-11 | No change MUST not log | P0 |
| FR-048b-12 | Changed files MUST list | P0 |
| FR-048b-13 | Multiple changes MUST log once | P0 |
| FR-048b-14 | Batch changes MUST group | P0 |
| FR-048b-15 | Detection path MUST be configurable | P0 |
| FR-048b-16 | Default paths MUST exist | P0 |
| FR-048b-17 | Prompt path: .agent/prompts | P0 |
| FR-048b-18 | Config path: .agent/config.yml | P0 |
| FR-048b-19 | Suite path: .benchmarks/tasks | P0 |
| FR-048b-20 | Path override MUST work | P0 |

### FR-021 to FR-040: Change Categories

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048b-21 | Category MUST be assigned | P0 |
| FR-048b-22 | Category: prompt MUST exist | P0 |
| FR-048b-23 | Category: model MUST exist | P0 |
| FR-048b-24 | Category: config MUST exist | P0 |
| FR-048b-25 | Category: suite MUST exist | P0 |
| FR-048b-26 | Category: environment MUST exist | P1 |
| FR-048b-27 | Category: other MUST exist | P1 |
| FR-048b-28 | Multi-category MUST be allowed | P0 |
| FR-048b-29 | Primary category MUST be set | P0 |
| FR-048b-30 | Severity MUST be assigned | P1 |
| FR-048b-31 | Severity: breaking MUST exist | P1 |
| FR-048b-32 | Severity: major MUST exist | P1 |
| FR-048b-33 | Severity: minor MUST exist | P1 |
| FR-048b-34 | Severity: patch MUST exist | P1 |
| FR-048b-35 | Auto-severity MUST default | P1 |
| FR-048b-36 | Manual severity MUST override | P1 |
| FR-048b-37 | Impact estimate MUST allow | P1 |
| FR-048b-38 | Affected tasks MUST list | P1 |
| FR-048b-39 | Affected metrics MUST list | P1 |
| FR-048b-40 | Category rules MUST be configurable | P1 |

### FR-041 to FR-055: Log Entries

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048b-41 | Entry ID MUST be generated | P0 |
| FR-048b-42 | ID format: change-{date}-{seq} | P0 |
| FR-048b-43 | Timestamp MUST be recorded | P0 |
| FR-048b-44 | Author MUST be recorded | P0 |
| FR-048b-45 | Description MUST be required | P0 |
| FR-048b-46 | Category MUST be recorded | P0 |
| FR-048b-47 | Changed files MUST list | P0 |
| FR-048b-48 | Previous hash MUST record | P0 |
| FR-048b-49 | New hash MUST record | P0 |
| FR-048b-50 | Baseline ID MUST link | P0 |
| FR-048b-51 | Git commit MUST record | P1 |
| FR-048b-52 | Git diff MUST allow | P1 |
| FR-048b-53 | Model version MUST record | P0 |
| FR-048b-54 | Metadata MUST allow | P1 |
| FR-048b-55 | Tags MUST allow | P1 |

### FR-056 to FR-070: Storage

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048b-56 | Storage MUST be JSON | P0 |
| FR-048b-57 | Log file: change-log.json | P0 |
| FR-048b-58 | Storage path: .benchmarks/changes | P0 |
| FR-048b-59 | Append-only MUST be default | P0 |
| FR-048b-60 | Atomic writes MUST occur | P0 |
| FR-048b-61 | Backup on write MUST occur | P0 |
| FR-048b-62 | Retention MUST be configurable | P0 |
| FR-048b-63 | Default retention: 365 days | P0 |
| FR-048b-64 | Auto-prune MUST work | P0 |
| FR-048b-65 | Archive old MUST work | P1 |
| FR-048b-66 | Export MUST work | P0 |
| FR-048b-67 | Import MUST work | P0 |
| FR-048b-68 | Compression MUST allow | P1 |
| FR-048b-69 | Max size MUST be configurable | P1 |
| FR-048b-70 | Size warning MUST trigger | P1 |

### FR-071 to FR-085: Query

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048b-71 | Query by date MUST work | P0 |
| FR-048b-72 | Query by date range MUST work | P0 |
| FR-048b-73 | Query by category MUST work | P0 |
| FR-048b-74 | Query by baseline MUST work | P0 |
| FR-048b-75 | Query by author MUST work | P0 |
| FR-048b-76 | Query by file MUST work | P0 |
| FR-048b-77 | Query by ID MUST work | P0 |
| FR-048b-78 | Latest N MUST work | P0 |
| FR-048b-79 | Since baseline MUST work | P0 |
| FR-048b-80 | Between baselines MUST work | P0 |
| FR-048b-81 | Full-text search MUST work | P1 |
| FR-048b-82 | Tag filter MUST work | P1 |
| FR-048b-83 | Combine filters MUST work | P0 |
| FR-048b-84 | Sort options MUST work | P0 |
| FR-048b-85 | Pagination MUST work | P0 |

### FR-086 to FR-095: CLI

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-048b-86 | CLI commands MUST exist | P0 |
| FR-048b-87 | `acode changes list` MUST work | P0 |
| FR-048b-88 | `acode changes show <id>` MUST work | P0 |
| FR-048b-89 | `acode changes add` MUST work | P0 |
| FR-048b-90 | `acode changes detect` MUST work | P0 |
| FR-048b-91 | `acode changes since <baseline>` MUST work | P0 |
| FR-048b-92 | `--category` filter MUST work | P0 |
| FR-048b-93 | `--format` MUST work | P0 |
| FR-048b-94 | `--output` MUST work | P0 |
| FR-048b-95 | `--json` MUST work | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-048b-01 | Hash calculation | <50ms/file | P0 |
| NFR-048b-02 | Detection scan | <200ms | P0 |
| NFR-048b-03 | Log append | <50ms | P0 |
| NFR-048b-04 | Query by ID | <20ms | P0 |
| NFR-048b-05 | Query by date | <100ms | P0 |
| NFR-048b-06 | Range query | <200ms | P0 |
| NFR-048b-07 | Full scan | <500ms | P0 |
| NFR-048b-08 | Memory usage | <30MB | P0 |
| NFR-048b-09 | Log size | <10MB/year | P0 |
| NFR-048b-10 | Export speed | 1000 entries/s | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-048b-11 | Data integrity | 100% | P0 |
| NFR-048b-12 | Detection accuracy | 100% | P0 |
| NFR-048b-13 | No data loss | Guaranteed | P0 |
| NFR-048b-14 | Atomic writes | Always | P0 |
| NFR-048b-15 | Cross-platform | All OS | P0 |
| NFR-048b-16 | Concurrent access | Safe | P0 |
| NFR-048b-17 | Backup reliability | 100% | P0 |
| NFR-048b-18 | Recovery | Graceful | P0 |
| NFR-048b-19 | Corruption detection | Checksum | P0 |
| NFR-048b-20 | Hash consistency | 100% | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-048b-21 | Detection logged | Info | P0 |
| NFR-048b-22 | Changes logged | Info | P0 |
| NFR-048b-23 | Queries logged | Debug | P0 |
| NFR-048b-24 | Errors logged | Error | P0 |
| NFR-048b-25 | Structured logging | JSON | P0 |
| NFR-048b-26 | Metrics: changes | Counter | P1 |
| NFR-048b-27 | Metrics: by category | Histogram | P1 |
| NFR-048b-28 | Trace ID | Included | P1 |
| NFR-048b-29 | Audit events | Complete | P0 |
| NFR-048b-30 | Secrets redacted | Always | P0 |

---

## Acceptance Criteria / Definition of Done

### Detection
- [ ] AC-001: Automatic detection
- [ ] AC-002: Prompt detection
- [ ] AC-003: Model detection
- [ ] AC-004: Config detection
- [ ] AC-005: Suite detection
- [ ] AC-006: Hash-based
- [ ] AC-007: Path configurable
- [ ] AC-008: Batch grouping

### Categories
- [ ] AC-009: All categories
- [ ] AC-010: Multi-category
- [ ] AC-011: Severity levels
- [ ] AC-012: Impact estimate
- [ ] AC-013: Affected tasks
- [ ] AC-014: Affected metrics

### Entries
- [ ] AC-015: ID generated
- [ ] AC-016: Timestamp
- [ ] AC-017: Author
- [ ] AC-018: Description
- [ ] AC-019: Files listed
- [ ] AC-020: Hashes recorded
- [ ] AC-021: Baseline linked
- [ ] AC-022: Git info

### Storage
- [ ] AC-023: JSON format
- [ ] AC-024: Append-only
- [ ] AC-025: Atomic
- [ ] AC-026: Backup
- [ ] AC-027: Retention
- [ ] AC-028: Export/import

### Query
- [ ] AC-029: By date
- [ ] AC-030: By category
- [ ] AC-031: By baseline
- [ ] AC-032: Since baseline
- [ ] AC-033: Between baselines
- [ ] AC-034: Combined filters
- [ ] AC-035: Pagination

### CLI
- [ ] AC-036: List command
- [ ] AC-037: Show command
- [ ] AC-038: Add command
- [ ] AC-039: Detect command
- [ ] AC-040: Filters work
- [ ] AC-041: Formats work
- [ ] AC-042: Tests pass
- [ ] AC-043: Documented
- [ ] AC-044: Cross-platform

---

## User Verification Scenarios

### Scenario 1: Detect Prompt Change
**Persona:** Developer  
**Preconditions:** Prompt file modified  
**Steps:**
1. Run detect command
2. Change detected
3. Entry created
4. Baseline linked

**Verification Checklist:**
- [ ] Detection works
- [ ] Entry created
- [ ] Category: prompt
- [ ] Hash updated

### Scenario 2: Model Upgrade Log
**Persona:** Tech Lead  
**Preconditions:** Model version updated  
**Steps:**
1. Update model
2. Run benchmark
3. Change detected
4. Log entry created

**Verification Checklist:**
- [ ] Model change detected
- [ ] Version recorded
- [ ] Category: model
- [ ] Linked to run

### Scenario 3: Query Changes Since Baseline
**Persona:** Developer  
**Preconditions:** Multiple changes exist  
**Steps:**
1. Run since command
2. Changes listed
3. Filter by category
4. Review entries

**Verification Checklist:**
- [ ] Query works
- [ ] Correct range
- [ ] Filter works
- [ ] Complete list

### Scenario 4: Manual Change Entry
**Persona:** Tech Lead  
**Preconditions:** External change occurred  
**Steps:**
1. Run add command
2. Provide description
3. Set category
4. Entry created

**Verification Checklist:**
- [ ] Manual entry works
- [ ] Description saved
- [ ] Category set
- [ ] Logged

### Scenario 5: Changes Between Baselines
**Persona:** Developer  
**Preconditions:** Two baselines exist  
**Steps:**
1. Query between baselines
2. All changes listed
3. Group by category
4. Correlate with delta

**Verification Checklist:**
- [ ] Range query works
- [ ] All changes included
- [ ] Grouping works
- [ ] Correlation clear

### Scenario 6: Export Change Log
**Persona:** Auditor  
**Preconditions:** Change history exists  
**Steps:**
1. Run export command
2. Select format
3. File created
4. Verify complete

**Verification Checklist:**
- [ ] Export works
- [ ] Format correct
- [ ] Complete data
- [ ] Importable

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-048b-01 | Hash calculation | FR-048b-06 |
| UT-048b-02 | Change detection | FR-048b-01 |
| UT-048b-03 | Category assignment | FR-048b-21 |
| UT-048b-04 | ID generation | FR-048b-41 |
| UT-048b-05 | Entry creation | FR-048b-43 |
| UT-048b-06 | JSON serialization | FR-048b-56 |
| UT-048b-07 | Query by date | FR-048b-71 |
| UT-048b-08 | Query by category | FR-048b-73 |
| UT-048b-09 | Query by baseline | FR-048b-74 |
| UT-048b-10 | Range query | FR-048b-80 |
| UT-048b-11 | Filter combination | FR-048b-83 |
| UT-048b-12 | Pagination | FR-048b-85 |
| UT-048b-13 | Retention calculation | FR-048b-62 |
| UT-048b-14 | Export formatting | FR-048b-66 |
| UT-048b-15 | Import parsing | FR-048b-67 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-048b-01 | Full detection E2E | E2E |
| IT-048b-02 | Baseline integration | Task 048 |
| IT-048b-03 | Recording integration | Task 048.a |
| IT-048b-04 | CLI integration | FR-048b-86 |
| IT-048b-05 | Cross-platform | NFR-048b-15 |
| IT-048b-06 | Atomic writes | NFR-048b-14 |
| IT-048b-07 | Concurrent access | NFR-048b-16 |
| IT-048b-08 | Logging | NFR-048b-21 |
| IT-048b-09 | Git integration | FR-048b-51 |
| IT-048b-10 | Large log | NFR-048b-09 |
| IT-048b-11 | Retention pruning | FR-048b-64 |
| IT-048b-12 | Export/import cycle | FR-048b-66 |
| IT-048b-13 | Query performance | NFR-048b-04 |
| IT-048b-14 | Detection performance | NFR-048b-02 |
| IT-048b-15 | Audit trail | NFR-048b-29 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Changes/
│       ├── ChangeEntry.cs
│       ├── ChangeCategory.cs
│       ├── ChangeSeverity.cs
│       └── FileHash.cs
├── Acode.Application/
│   └── Changes/
│       ├── IChangeDetector.cs
│       ├── IChangeLog.cs
│       ├── ChangeQuery.cs
│       └── DetectionConfig.cs
├── Acode.Infrastructure/
│   └── Changes/
│       ├── ChangeDetector.cs
│       ├── ChangeLogStore.cs
│       ├── HashCalculator.cs
│       └── ChangeLogExporter.cs
├── Acode.Cli/
│   └── Commands/
│       └── Changes/
│           ├── ChangesListCommand.cs
│           ├── ChangesShowCommand.cs
│           ├── ChangesAddCommand.cs
│           └── ChangesDetectCommand.cs
```

### Change Entry Schema

```json
{
  "id": "change-2025-01-15-001",
  "timestamp": "2025-01-15T10:30:00Z",
  "author": "developer@example.com",
  "description": "Updated system prompt for better tool selection",
  "category": ["prompt"],
  "severity": "major",
  "files": [
    {
      "path": ".agent/prompts/system.md",
      "previousHash": "abc123...",
      "newHash": "def456..."
    }
  ],
  "baselineId": "baseline-2025-01-15-001",
  "gitCommit": "a1b2c3d4",
  "modelVersion": null,
  "metadata": {},
  "tags": ["system-prompt"]
}
```

### Detection Flow

```
1. Identify files to check (paths from config)
   ↓
2. Calculate current hashes
   ↓
3. Load previous hashes
   ↓
4. Compare hashes
   ↓
5. If changed:
   a. Create change entry
   b. Assign category
   c. Link to baseline
   d. Store entry
   e. Update stored hashes
```

### CLI Examples

```bash
# Detect changes
acode changes detect

# List all changes
acode changes list

# Show specific change
acode changes show change-2025-01-15-001

# Changes since baseline
acode changes since baseline-2025-01-01-001

# Changes between baselines
acode changes list --from baseline-2025-01-01-001 --to baseline-2025-01-15-001

# Filter by category
acode changes list --category prompt

# Add manual entry
acode changes add --category model --description "Upgraded to GPT-4.1"

# Export to file
acode changes list --format json --output changes.json
```

**End of Task 048.b Specification**
