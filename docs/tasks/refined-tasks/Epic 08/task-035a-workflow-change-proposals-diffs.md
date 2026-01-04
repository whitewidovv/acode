# Task 035.a: Workflow Change Proposals and Diffs

**Priority:** P1 – High  
**Tier:** L – Feature Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 8 – CI/CD Integration  
**Dependencies:** Task 035 (CI Maintenance Mode)  

---

## Description

Task 035.a implements the workflow change proposal and diff generation system. Every proposed change MUST be presented as a unified diff with full context. Users MUST be able to review exactly what will change before approving.

Clear, accurate diffs are essential for safe CI maintenance. Developers need to understand the exact changes being proposed, including surrounding context, line numbers, and the rationale for each change.

The diff engine generates unified diffs compatible with standard diff tools. Diffs MUST show enough context (3+ lines) to understand the change location. Proposals MUST be structured with metadata for tracking and approval.

### Business Value

Change proposals with diffs provide:
- Transparency in automated changes
- Confidence in approving updates
- Audit trail with exact changes
- Reduced risk of unexpected modifications

### Scope Boundaries

This task covers diff generation and proposal structure. Pattern detection is in Task 035. Approval gates are in 035.b.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Maintenance Engine | Task 035 | Requests diffs | For proposals |
| Pattern Detectors | `IPatternDetector` | Issue details | Source of changes |
| Proposal Store | `IProposalStore` | Persist proposals | SQLite |
| Approval Gates | Task 035.b | Awaits approval | Before apply |
| CLI Display | `IProposalRenderer` | Show to user | Formatted output |
| Event Bus | `IEventPublisher` | Proposal events | Async |
| Audit Log | `IAuditLogger` | Record proposals | Required |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Diff generation fails | Exception | Skip change, log | Change not proposed |
| Context lines unavailable | File too short | Use available context | Smaller context |
| Line number mismatch | Parse error | Re-parse file | Retry with fresh read |
| Proposal serialization fails | JSON error | Log and skip | Proposal lost |
| Large diff (>1000 lines) | Size check | Split into chunks | Multiple proposals |
| Binary file detected | Content check | Skip with warning | No diff possible |
| Encoding issues | Charset error | Use UTF-8 fallback | May show incorrectly |
| Concurrent file edit | Mismatch hash | Regenerate diff | Fresh proposal |

### Assumptions

1. **Text files only**: YAML workflows are text, not binary
2. **UTF-8 encoding**: Workflow files use UTF-8
3. **Line-based changes**: Changes are line-oriented
4. **Context available**: Surrounding lines accessible
5. **File hash stable**: Hash for change detection
6. **Proposal ID unique**: UUID for proposal identification
7. **JSON serializable**: Proposals can be serialized
8. **Diff format standard**: Unified diff format

### Security Considerations

1. **No secrets in diffs**: Diffs never expose secret values
2. **Proposal integrity**: Proposals are immutable after creation
3. **Hash verification**: File hash verified before apply
4. **Audit trail**: All proposals logged with context
5. **User attribution**: Proposal creator recorded
6. **Expiration enforced**: Stale proposals cannot be applied
7. **Change isolation**: Each proposal independent
8. **No code execution**: Diff generation is pure parsing

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Unified Diff | Standard diff format with context |
| Hunk | Section of diff with changes |
| Context Lines | Unchanged lines around changes |
| Before/After | Original and proposed content |
| Line Number | 1-based line position |
| Hash | SHA-256 of file content |
| Proposal ID | Unique identifier (UUID) |
| Metadata | Proposal tracking information |

---

## Out of Scope

- Three-way merge diffs
- Interactive diff editing
- Binary file diffs
- Side-by-side diff view (CLI only)
- Semantic diff (AST-based)
- Word-level diff highlighting

---

## Functional Requirements

### FR-001 to FR-015: Diff Generation Interface

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-035A-01 | `IDiffGenerator` interface MUST exist | P0 |
| FR-035A-02 | `GenerateDiffAsync` MUST create unified diff | P0 |
| FR-035A-03 | Input MUST include file path and changes | P0 |
| FR-035A-04 | Output MUST be standard unified diff format | P0 |
| FR-035A-05 | Diff MUST include 3 context lines minimum | P0 |
| FR-035A-06 | Context lines MUST be configurable | P2 |
| FR-035A-07 | Line numbers MUST be accurate | P0 |
| FR-035A-08 | Hunks MUST be properly delimited | P0 |
| FR-035A-09 | Added lines MUST be prefixed with `+` | P0 |
| FR-035A-10 | Removed lines MUST be prefixed with `-` | P0 |
| FR-035A-11 | Context lines MUST be prefixed with space | P0 |
| FR-035A-12 | Header MUST include file paths | P0 |
| FR-035A-13 | Header MUST include timestamps | P2 |
| FR-035A-14 | Empty diff MUST be handled | P1 |
| FR-035A-15 | Multiple hunks MUST be supported | P0 |

### FR-016 to FR-030: Proposal Structure

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-035A-16 | `Proposal` record MUST exist | P0 |
| FR-035A-17 | Proposal MUST have unique ID (UUID) | P0 |
| FR-035A-18 | Proposal MUST include file path | P0 |
| FR-035A-19 | Proposal MUST include unified diff | P0 |
| FR-035A-20 | Proposal MUST include rationale | P0 |
| FR-035A-21 | Proposal MUST include severity | P0 |
| FR-035A-22 | Proposal MUST include issue type | P0 |
| FR-035A-23 | Proposal MUST include created timestamp | P0 |
| FR-035A-24 | Proposal MUST include expiration timestamp | P1 |
| FR-035A-25 | Proposal MUST include status | P0 |
| FR-035A-26 | Status: Pending, Approved, Rejected, Applied, Expired | P0 |
| FR-035A-27 | Proposal MUST include file hash (before) | P0 |
| FR-035A-28 | Proposal MUST include creator context | P1 |
| FR-035A-29 | Proposal MUST be JSON serializable | P0 |
| FR-035A-30 | Proposal MUST support custom metadata | P2 |

### FR-031 to FR-045: Proposal Rendering

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-035A-31 | `IProposalRenderer` interface MUST exist | P0 |
| FR-035A-32 | Renderer MUST output to console | P0 |
| FR-035A-33 | Diff MUST be syntax highlighted | P1 |
| FR-035A-34 | Added lines MUST be green | P1 |
| FR-035A-35 | Removed lines MUST be red | P1 |
| FR-035A-36 | Context lines MUST be default color | P1 |
| FR-035A-37 | Header MUST show proposal ID | P0 |
| FR-035A-38 | Header MUST show severity | P0 |
| FR-035A-39 | Header MUST show rationale | P0 |
| FR-035A-40 | Line numbers MUST be displayed | P1 |
| FR-035A-41 | File path MUST be displayed | P0 |
| FR-035A-42 | Status MUST be displayed | P0 |
| FR-035A-43 | Expiration MUST be shown if pending | P2 |
| FR-035A-44 | Compact mode MUST be available | P2 |
| FR-035A-45 | JSON output MUST be available | P1 |

### FR-046 to FR-060: Proposal Storage

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-035A-46 | `IProposalStore` interface MUST exist | P0 |
| FR-035A-47 | Store MUST persist proposals to SQLite | P0 |
| FR-035A-48 | `SaveAsync` MUST store proposal | P0 |
| FR-035A-49 | `GetByIdAsync` MUST retrieve by ID | P0 |
| FR-035A-50 | `GetPendingAsync` MUST list pending | P0 |
| FR-035A-51 | `UpdateStatusAsync` MUST change status | P0 |
| FR-035A-52 | `DeleteExpiredAsync` MUST cleanup | P1 |
| FR-035A-53 | Expired proposals MUST NOT be applicable | P0 |
| FR-035A-54 | Store MUST handle concurrent access | P1 |
| FR-035A-55 | Store MUST emit events on changes | P1 |
| FR-035A-56 | Store MUST support querying by file | P1 |
| FR-035A-57 | Store MUST support querying by status | P1 |
| FR-035A-58 | Store MUST support querying by severity | P2 |
| FR-035A-59 | Store MUST track modification time | P1 |
| FR-035A-60 | Store MUST support bulk operations | P2 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-035A-01 | Diff generation latency | <100ms | P1 |
| NFR-035A-02 | Proposal serialization | <20ms | P1 |
| NFR-035A-03 | Store write latency | <50ms | P1 |
| NFR-035A-04 | Store read latency | <20ms | P1 |
| NFR-035A-05 | Rendering latency | <50ms | P1 |
| NFR-035A-06 | Memory for diff | <10MB | P2 |
| NFR-035A-07 | Bulk query latency | <200ms for 100 | P2 |
| NFR-035A-08 | Concurrent access | 10 parallel | P2 |
| NFR-035A-09 | Context line calculation | <10ms | P2 |
| NFR-035A-10 | Hash calculation | <20ms | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-035A-11 | Accurate line numbers | 100% | P0 |
| NFR-035A-12 | Valid unified diff format | 100% | P0 |
| NFR-035A-13 | Proposal persistence durability | 99.99% | P0 |
| NFR-035A-14 | Hash collision prevention | 0% | P0 |
| NFR-035A-15 | Expiration enforcement | 100% | P0 |
| NFR-035A-16 | Status transition correctness | 100% | P0 |
| NFR-035A-17 | Concurrent access safety | No corruption | P0 |
| NFR-035A-18 | Graceful error handling | Always | P1 |
| NFR-035A-19 | UTF-8 encoding support | Full | P0 |
| NFR-035A-20 | Large file handling | Up to 10MB | P1 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-035A-21 | Structured logging | JSON format | P1 |
| NFR-035A-22 | Metrics on proposals created | Counter | P1 |
| NFR-035A-23 | Metrics on proposals by status | Counter | P1 |
| NFR-035A-24 | Events on proposal created | Async | P0 |
| NFR-035A-25 | Events on status change | Async | P0 |
| NFR-035A-26 | Diff size logged | Debug level | P2 |
| NFR-035A-27 | Expiration events | Warning level | P1 |
| NFR-035A-28 | Error events | Error level | P0 |
| NFR-035A-29 | Audit trail complete | Required | P0 |
| NFR-035A-30 | Performance metrics | Histogram | P2 |

---

## User Manual Documentation

### Proposal Display Example

```
┌─────────────────────────────────────────────────────────────┐
│ Proposal: PROP-abc123                                       │
│ Severity: CRITICAL                                          │
│ File: .github/workflows/ci.yml                              │
│ Issue: Unpinned action version                              │
│ Rationale: Pin actions/checkout to SHA for supply chain     │
│            security                                         │
│ Status: Pending                                             │
│ Expires: 2026-01-04 10:30:00                               │
├─────────────────────────────────────────────────────────────┤
│ --- a/.github/workflows/ci.yml                              │
│ +++ b/.github/workflows/ci.yml                              │
│ @@ -10,7 +10,7 @@                                           │
│      runs-on: ubuntu-latest                                 │
│      steps:                                                 │
│ -      - uses: actions/checkout@v4                          │
│ +      - uses: actions/checkout@b4ffde65f463... # v4.1.1    │
│        - name: Build                                        │
│          run: dotnet build                                  │
└─────────────────────────────────────────────────────────────┘
```

### CLI Commands

```bash
# View all pending proposals
acode ci maintain proposals

# View specific proposal details
acode ci maintain proposals PROP-abc123

# View proposals as JSON
acode ci maintain proposals --json

# Filter by severity
acode ci maintain proposals --severity critical

# Filter by file
acode ci maintain proposals --file ci.yml
```

---

## Acceptance Criteria / Definition of Done

### Diff Generation
- [ ] AC-001: `IDiffGenerator` interface exists
- [ ] AC-002: Unified diff format generated
- [ ] AC-003: 3+ context lines included
- [ ] AC-004: Line numbers accurate
- [ ] AC-005: Added lines prefixed `+`
- [ ] AC-006: Removed lines prefixed `-`
- [ ] AC-007: Multiple hunks supported
- [ ] AC-008: Header includes file path

### Proposal Structure
- [ ] AC-009: Proposal has UUID
- [ ] AC-010: Proposal includes file path
- [ ] AC-011: Proposal includes diff
- [ ] AC-012: Proposal includes rationale
- [ ] AC-013: Proposal includes severity
- [ ] AC-014: Proposal includes status
- [ ] AC-015: Proposal includes timestamps
- [ ] AC-016: Proposal includes file hash

### Rendering
- [ ] AC-017: Console output works
- [ ] AC-018: Diff syntax highlighted
- [ ] AC-019: Green for added
- [ ] AC-020: Red for removed
- [ ] AC-021: Header shows ID
- [ ] AC-022: Severity displayed
- [ ] AC-023: Rationale shown
- [ ] AC-024: JSON output available

### Storage
- [ ] AC-025: SQLite persistence works
- [ ] AC-026: Save and retrieve by ID
- [ ] AC-027: List pending proposals
- [ ] AC-028: Update status works
- [ ] AC-029: Expired cleanup works
- [ ] AC-030: Concurrent access safe
- [ ] AC-031: Query by file works
- [ ] AC-032: Events on changes

### Status Transitions
- [ ] AC-033: Pending → Approved works
- [ ] AC-034: Pending → Rejected works
- [ ] AC-035: Approved → Applied works
- [ ] AC-036: Pending → Expired (auto)
- [ ] AC-037: Invalid transitions blocked

### Validation
- [ ] AC-038: UTF-8 encoding handled
- [ ] AC-039: Large diffs handled
- [ ] AC-040: Binary files skipped

---

## User Verification Scenarios

### Scenario 1: View Proposal Diff
**Persona:** Developer reviewing change  
**Preconditions:** Proposal pending  
**Steps:**
1. Run `acode ci maintain proposals PROP-001`
2. See formatted diff
3. Review added/removed lines
4. Understand change context

**Verification Checklist:**
- [ ] Diff displayed
- [ ] Colors correct
- [ ] Context visible
- [ ] Rationale clear

### Scenario 2: Multiple Proposals for File
**Persona:** Developer with many issues  
**Preconditions:** Multiple issues in one file  
**Steps:**
1. Run `acode ci maintain proposals --file ci.yml`
2. See all proposals for file
3. Each has unique ID
4. Can approve individually

**Verification Checklist:**
- [ ] All proposals shown
- [ ] Unique IDs
- [ ] Diffs correct
- [ ] Selectable

### Scenario 3: Expired Proposal Blocked
**Persona:** Developer after 24 hours  
**Preconditions:** Proposal created > 24h ago  
**Steps:**
1. Try to approve old proposal
2. Error: proposal expired
3. Must re-analyze
4. New proposal generated

**Verification Checklist:**
- [ ] Expiration detected
- [ ] Clear error message
- [ ] Apply blocked
- [ ] Re-analysis suggested

### Scenario 4: JSON Export
**Persona:** Developer integrating with tools  
**Preconditions:** Proposals pending  
**Steps:**
1. Run `acode ci maintain proposals --json`
2. Get JSON output
3. Parse with jq/other tools
4. Integrate with automation

**Verification Checklist:**
- [ ] Valid JSON output
- [ ] All fields present
- [ ] Parseable
- [ ] Machine-readable

### Scenario 5: Large Diff Handling
**Persona:** Developer with major refactor  
**Preconditions:** Proposal with many changes  
**Steps:**
1. Analysis finds large change
2. Proposal generated
3. Diff renders correctly
4. All hunks visible

**Verification Checklist:**
- [ ] Large diff handled
- [ ] Multiple hunks
- [ ] No truncation (or clear indicator)
- [ ] Performance acceptable

### Scenario 6: Status Tracking
**Persona:** Developer tracking progress  
**Preconditions:** Mix of proposal states  
**Steps:**
1. View all proposals
2. See status of each
3. Filter by status
4. Track progress

**Verification Checklist:**
- [ ] Statuses displayed
- [ ] Filter works
- [ ] Applied/Rejected tracked
- [ ] History visible

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-035A-01 | Unified diff generation | FR-035A-02 |
| UT-035A-02 | Context lines included | FR-035A-05 |
| UT-035A-03 | Line number accuracy | FR-035A-07 |
| UT-035A-04 | Hunk delimiting | FR-035A-08 |
| UT-035A-05 | Add/remove prefixes | FR-035A-09 |
| UT-035A-06 | Multiple hunks | FR-035A-15 |
| UT-035A-07 | Proposal UUID uniqueness | FR-035A-17 |
| UT-035A-08 | Proposal serialization | FR-035A-29 |
| UT-035A-09 | Status transitions | FR-035A-26 |
| UT-035A-10 | Expiration check | FR-035A-53 |
| UT-035A-11 | Store CRUD operations | FR-035A-48 |
| UT-035A-12 | Query by filter | FR-035A-56 |
| UT-035A-13 | Rendering output | FR-035A-32 |
| UT-035A-14 | JSON output | FR-035A-45 |
| UT-035A-15 | Diff generation < 100ms | NFR-035A-01 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-035A-01 | Full proposal lifecycle | E2E |
| IT-035A-02 | Store persistence | FR-035A-47 |
| IT-035A-03 | Concurrent access | NFR-035A-17 |
| IT-035A-04 | Large diff handling | NFR-035A-20 |
| IT-035A-05 | Expiration enforcement | FR-035A-53 |
| IT-035A-06 | Event emission | NFR-035A-24 |
| IT-035A-07 | CLI rendering | AC-017 |
| IT-035A-08 | JSON export | AC-024 |
| IT-035A-09 | Filter by severity | FR-035A-58 |
| IT-035A-10 | Filter by file | FR-035A-56 |
| IT-035A-11 | Bulk operations | FR-035A-60 |
| IT-035A-12 | Hash verification | FR-035A-27 |
| IT-035A-13 | UTF-8 handling | NFR-035A-19 |
| IT-035A-14 | Audit logging | NFR-035A-29 |
| IT-035A-15 | Metrics emission | NFR-035A-22 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── CiCd/
│       └── Maintenance/
│           └── Proposals/
│               ├── Proposal.cs
│               ├── ProposalStatus.cs
│               └── Events/
│                   ├── ProposalCreatedEvent.cs
│                   └── ProposalStatusChangedEvent.cs
├── Acode.Application/
│   └── CiCd/
│       └── Maintenance/
│           └── Proposals/
│               ├── IDiffGenerator.cs
│               ├── IProposalStore.cs
│               └── IProposalRenderer.cs
└── Acode.Infrastructure/
    └── CiCd/
        └── Maintenance/
            └── Proposals/
                ├── UnifiedDiffGenerator.cs
                ├── SqliteProposalStore.cs
                └── ConsoleProposalRenderer.cs
```

**End of Task 035.a Specification**
