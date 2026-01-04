# Task 044.b: Stats and Clear Commands

**Priority:** P1 – High  
**Tier:** F – Foundation Layer  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 11 – Optimization  
**Dependencies:** Task 044 (Caching), Task 000 (CLI)  

---

## Description

Task 044.b implements cache management commands—the CLI interface for inspecting cache statistics and clearing cache entries. Users need visibility into cache behavior (size, hit rates, entry counts) and control over cache lifecycle (clear all, clear by pattern, force refresh).

The stats command provides a comprehensive view of cache health and efficiency. The clear command allows targeted or complete cache clearing, essential for troubleshooting, freeing disk space, or forcing fresh computation.

### Business Value

Management commands provide:
- Cache visibility
- Troubleshooting capability
- Disk space recovery
- Debug support
- User control

### Scope Boundaries

This task covers CLI commands for stats and clear. Core caching is Task 044. Key generation is Task 044.a. Telemetry is Task 044.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Cache | Task 044 | Stats/Clear | Consumer |
| CLI | Task 000 | Commands | Interface |
| Event Log | Task 040 | Log operations | Audit |
| Config | Settings | Display format | Options |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Stats unavailable | Query fail | Show partial | Degraded info |
| Clear fails | I/O error | Retry | May not clear |
| Partial clear | Error mid-op | Report | Inconsistent |
| Lock contention | Timeout | Retry | Slower |
| Concurrent access | Detect | Queue | Wait |

### Assumptions

1. **Stats queryable**: Cache supports
2. **Clear atomic**: Or report partial
3. **CLI available**: User can run
4. **Permissions OK**: Can delete
5. **Output readable**: Format clear

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Stats | Cache statistics |
| Clear | Remove entries |
| Hit Rate | Hits / total |
| Size | Disk/memory used |
| Count | Number of entries |
| Pattern | Key filter |
| Prefix | Key starts with |
| Prune | Remove expired |
| Force | Skip confirmation |
| Dry Run | Show what would clear |

---

## Out of Scope

- Cache warming commands
- Cache export/import
- Cache migration
- Remote cache management
- Cache analytics
- Scheduled clearing

---

## Functional Requirements

### FR-001 to FR-025: Stats Command

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-044b-01 | Stats command MUST exist | P0 |
| FR-044b-02 | Command MUST be `acode cache stats` | P0 |
| FR-044b-03 | Entry count MUST be shown | P0 |
| FR-044b-04 | Total size MUST be shown | P0 |
| FR-044b-05 | Size MUST be human-readable | P0 |
| FR-044b-06 | Hit count MUST be shown | P0 |
| FR-044b-07 | Miss count MUST be shown | P0 |
| FR-044b-08 | Hit rate MUST be shown | P0 |
| FR-044b-09 | Hit rate MUST be percentage | P0 |
| FR-044b-10 | Memory cache stats MUST show | P0 |
| FR-044b-11 | Disk cache stats MUST show | P0 |
| FR-044b-12 | Oldest entry MUST be shown | P1 |
| FR-044b-13 | Newest entry MUST be shown | P1 |
| FR-044b-14 | Average entry size MUST show | P1 |
| FR-044b-15 | Eviction count MUST show | P1 |
| FR-044b-16 | Size limit MUST be shown | P0 |
| FR-044b-17 | Size used % MUST be shown | P0 |
| FR-044b-18 | JSON output MUST be option | P0 |
| FR-044b-19 | `--json` flag MUST work | P0 |
| FR-044b-20 | Table format MUST be default | P0 |
| FR-044b-21 | By-type breakdown MUST work | P1 |
| FR-044b-22 | `--by-type` flag MUST work | P1 |
| FR-044b-23 | Verbose MUST show more | P1 |
| FR-044b-24 | `--verbose` flag MUST work | P1 |
| FR-044b-25 | Stats MUST be fast | P0 |

### FR-026 to FR-050: Clear Command

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-044b-26 | Clear command MUST exist | P0 |
| FR-044b-27 | Command MUST be `acode cache clear` | P0 |
| FR-044b-28 | Clear all MUST work | P0 |
| FR-044b-29 | Confirmation MUST be required | P0 |
| FR-044b-30 | `--force` MUST skip confirm | P0 |
| FR-044b-31 | Clear by prefix MUST work | P0 |
| FR-044b-32 | `--prefix` flag MUST work | P0 |
| FR-044b-33 | Clear by pattern MUST work | P1 |
| FR-044b-34 | `--pattern` flag MUST work | P1 |
| FR-044b-35 | Clear expired MUST work | P0 |
| FR-044b-36 | `--expired` flag MUST work | P0 |
| FR-044b-37 | Dry run MUST work | P0 |
| FR-044b-38 | `--dry-run` flag MUST work | P0 |
| FR-044b-39 | Dry run MUST show count | P0 |
| FR-044b-40 | Dry run MUST show size | P0 |
| FR-044b-41 | Progress MUST be shown | P0 |
| FR-044b-42 | Count cleared MUST show | P0 |
| FR-044b-43 | Size cleared MUST show | P0 |
| FR-044b-44 | Error count MUST show | P0 |
| FR-044b-45 | Clear MUST be logged | P0 |
| FR-044b-46 | Memory clear MUST work | P0 |
| FR-044b-47 | Disk clear MUST work | P0 |
| FR-044b-48 | Both tiers MUST clear | P0 |
| FR-044b-49 | `--memory-only` MUST work | P1 |
| FR-044b-50 | `--disk-only` MUST work | P1 |

### FR-051 to FR-060: Additional Commands

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-044b-51 | List command MUST exist | P1 |
| FR-044b-52 | `acode cache list` MUST work | P1 |
| FR-044b-53 | List MUST show keys | P1 |
| FR-044b-54 | List MUST support filter | P1 |
| FR-044b-55 | List MUST limit output | P0 |
| FR-044b-56 | Default limit MUST be 100 | P0 |
| FR-044b-57 | `--limit` MUST work | P1 |
| FR-044b-58 | Inspect command MUST exist | P2 |
| FR-044b-59 | `acode cache inspect <key>` | P2 |
| FR-044b-60 | Inspect MUST show metadata | P2 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-044b-01 | Stats query | <100ms | P0 |
| NFR-044b-02 | Clear small | <1s | P0 |
| NFR-044b-03 | Clear large | <30s | P0 |
| NFR-044b-04 | Progress update | 1/s | P0 |
| NFR-044b-05 | List query | <500ms | P1 |
| NFR-044b-06 | Memory impact | <50MB | P0 |
| NFR-044b-07 | Concurrent safe | Yes | P0 |
| NFR-044b-08 | Cancel support | Yes | P0 |
| NFR-044b-09 | Resume after cancel | Consistent | P0 |
| NFR-044b-10 | Batch delete | 100/batch | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-044b-11 | Stats accurate | 100% | P0 |
| NFR-044b-12 | Clear complete | 100% | P0 |
| NFR-044b-13 | Partial clear recovery | Yes | P0 |
| NFR-044b-14 | Concurrent access safe | Yes | P0 |
| NFR-044b-15 | Cross-platform | All OS | P0 |
| NFR-044b-16 | Disk full handling | Graceful | P0 |
| NFR-044b-17 | Permission error | Clear message | P0 |
| NFR-044b-18 | Lock handling | Wait/timeout | P0 |
| NFR-044b-19 | Consistency | Always | P0 |
| NFR-044b-20 | Idempotent clear | Yes | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-044b-21 | Stats logged | Debug | P0 |
| NFR-044b-22 | Clear logged | Info | P0 |
| NFR-044b-23 | Errors logged | Warning | P0 |
| NFR-044b-24 | Duration logged | Debug | P0 |
| NFR-044b-25 | Metrics: clears | Counter | P1 |
| NFR-044b-26 | Metrics: bytes cleared | Counter | P1 |
| NFR-044b-27 | Metrics: clear duration | Histogram | P1 |
| NFR-044b-28 | Structured logging | JSON | P0 |
| NFR-044b-29 | Audit trail | Event log | P0 |
| NFR-044b-30 | User action logged | Always | P0 |

---

## Acceptance Criteria / Definition of Done

### Stats Command
- [ ] AC-001: Command works
- [ ] AC-002: Entry count shown
- [ ] AC-003: Size shown
- [ ] AC-004: Hit/miss counts
- [ ] AC-005: Hit rate %
- [ ] AC-006: Memory stats
- [ ] AC-007: Disk stats
- [ ] AC-008: JSON output

### Clear Command
- [ ] AC-009: Command works
- [ ] AC-010: Confirmation required
- [ ] AC-011: Force skips confirm
- [ ] AC-012: Prefix filter works
- [ ] AC-013: Pattern filter works
- [ ] AC-014: Expired only works
- [ ] AC-015: Dry run works
- [ ] AC-016: Progress shown

### Output
- [ ] AC-017: Count cleared shown
- [ ] AC-018: Size cleared shown
- [ ] AC-019: Errors reported
- [ ] AC-020: Human-readable
- [ ] AC-021: Machine-parseable
- [ ] AC-022: Consistent format
- [ ] AC-023: Exit codes correct
- [ ] AC-024: Help text complete

### Quality
- [ ] AC-025: Fast stats
- [ ] AC-026: Cancel works
- [ ] AC-027: Concurrent safe
- [ ] AC-028: Cross-platform
- [ ] AC-029: Tests pass
- [ ] AC-030: Documented
- [ ] AC-031: Logged
- [ ] AC-032: Reviewed

---

## User Verification Scenarios

### Scenario 1: View Stats
**Persona:** Developer checking cache  
**Preconditions:** Cache has entries  
**Steps:**
1. Run `acode cache stats`
2. Stats displayed
3. Counts shown
4. Hit rate visible

**Verification Checklist:**
- [ ] Command works
- [ ] Clear output
- [ ] Accurate counts
- [ ] Human-readable

### Scenario 2: Clear All
**Persona:** Developer freeing space  
**Preconditions:** Cache populated  
**Steps:**
1. Run `acode cache clear`
2. Confirmation prompt
3. Confirm
4. Cache cleared

**Verification Checklist:**
- [ ] Confirmation shown
- [ ] Clear works
- [ ] Count reported
- [ ] Space freed

### Scenario 3: Dry Run
**Persona:** Developer checking impact  
**Preconditions:** Cache populated  
**Steps:**
1. Run `acode cache clear --dry-run`
2. Would-clear shown
3. Nothing deleted
4. Stats unchanged

**Verification Checklist:**
- [ ] Shows impact
- [ ] Nothing deleted
- [ ] Accurate counts
- [ ] Safe operation

### Scenario 4: Clear by Prefix
**Persona:** Developer targeting cache  
**Preconditions:** Mixed cache entries  
**Steps:**
1. Run `acode cache clear --prefix v1::index`
2. Only matching cleared
3. Others preserved
4. Count shown

**Verification Checklist:**
- [ ] Prefix works
- [ ] Selective clear
- [ ] Others safe
- [ ] Correct count

### Scenario 5: JSON Output
**Persona:** Developer scripting  
**Preconditions:** Cache exists  
**Steps:**
1. Run `acode cache stats --json`
2. JSON output
3. Parse with jq
4. Values correct

**Verification Checklist:**
- [ ] Valid JSON
- [ ] All fields present
- [ ] Parseable
- [ ] Correct values

### Scenario 6: Force Clear
**Persona:** Developer in script  
**Preconditions:** Cache populated  
**Steps:**
1. Run `acode cache clear --force`
2. No prompt
3. Immediate clear
4. Exit code 0

**Verification Checklist:**
- [ ] No prompt
- [ ] Immediate
- [ ] Correct exit code
- [ ] Scriptable

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-044b-01 | Stats command | FR-044b-01 |
| UT-044b-02 | Stats output format | FR-044b-20 |
| UT-044b-03 | Stats JSON | FR-044b-18 |
| UT-044b-04 | Clear command | FR-044b-26 |
| UT-044b-05 | Clear confirmation | FR-044b-29 |
| UT-044b-06 | Clear force | FR-044b-30 |
| UT-044b-07 | Clear prefix | FR-044b-31 |
| UT-044b-08 | Clear pattern | FR-044b-33 |
| UT-044b-09 | Clear expired | FR-044b-35 |
| UT-044b-10 | Dry run | FR-044b-37 |
| UT-044b-11 | Progress | FR-044b-41 |
| UT-044b-12 | Memory only | FR-044b-49 |
| UT-044b-13 | Disk only | FR-044b-50 |
| UT-044b-14 | List command | FR-044b-51 |
| UT-044b-15 | List limit | FR-044b-55 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-044b-01 | Cache integration | Task 044 |
| IT-044b-02 | CLI integration | Task 000 |
| IT-044b-03 | Real cache stats | E2E |
| IT-044b-04 | Real cache clear | E2E |
| IT-044b-05 | Large cache clear | NFR-044b-03 |
| IT-044b-06 | Concurrent access | NFR-044b-07 |
| IT-044b-07 | Cross-platform | NFR-044b-15 |
| IT-044b-08 | Cancel handling | NFR-044b-08 |
| IT-044b-09 | Performance | NFR-044b-01 |
| IT-044b-10 | Logging | NFR-044b-22 |
| IT-044b-11 | Exit codes | AC-023 |
| IT-044b-12 | Help text | AC-024 |
| IT-044b-13 | By-type stats | FR-044b-21 |
| IT-044b-14 | Partial clear | NFR-044b-13 |
| IT-044b-15 | Permission errors | NFR-044b-17 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Cli/
│   └── Commands/
│       └── Cache/
│           ├── CacheStatsCommand.cs
│           ├── CacheClearCommand.cs
│           ├── CacheListCommand.cs
│           └── CacheInspectCommand.cs
├── Acode.Application/
│   └── Cache/
│       ├── ICacheManager.cs
│       └── CacheStats.cs
```

### CLI Commands

```bash
# Stats
acode cache stats
acode cache stats --json
acode cache stats --by-type
acode cache stats --verbose

# Clear
acode cache clear
acode cache clear --force
acode cache clear --prefix v1::index
acode cache clear --pattern "*::embed::*"
acode cache clear --expired
acode cache clear --dry-run
acode cache clear --memory-only
acode cache clear --disk-only

# List
acode cache list
acode cache list --limit 50
acode cache list --filter "index"

# Inspect
acode cache inspect <key>
```

### Stats Output

```
Cache Statistics
================
Memory Cache:
  Entries:     1,234
  Size:        45.2 MB
  Hit Rate:    87.3%
  Hits:        12,456
  Misses:      1,824

Disk Cache:
  Entries:     8,567
  Size:        234.5 MB / 1.0 GB (23.5%)
  Hit Rate:    92.1%
  Hits:        45,678
  Misses:      3,921

Total:
  Entries:     9,801
  Size:        279.7 MB
  Combined Hit Rate: 90.4%
  Oldest Entry: 2024-01-15 10:23:45
  Newest Entry: 2024-01-22 14:56:12
  Evictions:   156
```

**End of Task 044.b Specification**
