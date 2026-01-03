# Task 028.a: Conflict Heuristics

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 028 (Merge Coordinator)  

---

## Description

Task 028.a implements conflict detection heuristics. The system MUST predict merge conflicts before they occur. Heuristics MUST be fast and reasonably accurate.

Conflict heuristics analyze file changes from parallel tasks. Overlapping modifications MUST be detected. Semantic proximity (same function, same class) MUST be considered.

Heuristics MUST be tunable. False positives reduce throughput. False negatives cause merge failures. The balance MUST be configurable.

### Business Value

Conflict heuristics enable:
- Early conflict detection
- Reduced merge failures
- Better task ordering
- Parallel execution confidence
- Predictable merges

### Scope Boundaries

This task covers detection heuristics. Merge execution is in Task 028. Dependency ordering is in Task 028.b.

### Integration Points

- Task 028: Uses heuristics for planning
- Task 022: Git provides diff data
- Task 027: Worker changes analyzed

### Failure Modes

- False positive → Unnecessary blocking
- False negative → Merge failure
- Slow analysis → Bottleneck
- Parse failure → Conservative estimate

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Heuristic | Educated guess algorithm |
| Overlap | Shared modification region |
| Proximity | Closeness of changes |
| Scope | Function/class boundary |
| Hotspot | Frequently modified area |
| Sensitivity | True positive rate |
| Specificity | True negative rate |

---

## Out of Scope

- Machine learning models
- Historical conflict prediction
- Semantic code understanding
- Cross-file dependency analysis
- Language-specific AST parsing

---

## Functional Requirements

### FR-001 to FR-025: Line-Level Analysis

- FR-001: Line ranges MUST be extracted
- FR-002: Added lines MUST be tracked
- FR-003: Removed lines MUST be tracked
- FR-004: Modified lines MUST be tracked
- FR-005: Context lines MUST be considered
- FR-006: Default context: 5 lines
- FR-007: Overlap detection MUST run
- FR-008: Direct overlap MUST be flagged
- FR-009: Adjacent overlap MUST be flagged
- FR-010: Near overlap MUST warn
- FR-011: Near threshold MUST be configurable
- FR-012: Default near: 10 lines
- FR-013: Overlap score MUST be computed
- FR-014: Score = overlap / change size
- FR-015: High score MUST be critical
- FR-016: Medium score MUST warn
- FR-017: Low score MUST be info
- FR-018: Score thresholds MUST be configurable
- FR-019: Line endings MUST be normalized
- FR-020: Whitespace MUST be optionally ignored
- FR-021: Moved lines MUST be detected
- FR-022: Moves MUST reduce conflict score
- FR-023: Renamed files MUST be tracked
- FR-024: Renames MUST be matched
- FR-025: Unmatched renames MUST warn

### FR-026 to FR-045: Scope Analysis

- FR-026: Function boundaries MUST be detected
- FR-027: Detection MUST be regex-based
- FR-028: C# method pattern MUST work
- FR-029: Python def pattern MUST work
- FR-030: JS function pattern MUST work
- FR-031: Class boundaries MUST be detected
- FR-032: Same function MUST increase severity
- FR-033: Same class MUST increase severity
- FR-034: Different scope MUST decrease severity
- FR-035: Scope detection MUST be fast
- FR-036: Fallback to line-only MUST work
- FR-037: Scope patterns MUST be configurable
- FR-038: Custom patterns MUST be addable
- FR-039: Pattern file MUST be loadable
- FR-040: Import sections MUST be detected
- FR-041: Import conflicts MUST auto-merge
- FR-042: Using statements MUST auto-merge
- FR-043: Namespace changes MUST warn
- FR-044: Comment changes MUST be low priority
- FR-045: Documentation changes MUST be low

### FR-046 to FR-065: File Type Rules

- FR-046: File type MUST affect analysis
- FR-047: Code files MUST use full analysis
- FR-048: Config files MUST be cautious
- FR-049: Lock files MUST use special rules
- FR-050: package-lock.json MUST regenerate
- FR-051: yarn.lock MUST regenerate
- FR-052: Binary files MUST NOT merge
- FR-053: Image files MUST use last-wins
- FR-054: Generated files MUST regenerate
- FR-055: Generated patterns MUST be configurable
- FR-056: Test files MAY be lenient
- FR-057: Lenient mode MUST be optional
- FR-058: Snapshot files MUST be cautious
- FR-059: Database migrations MUST be strict
- FR-060: API contracts MUST be strict
- FR-061: Rules MUST be pattern-based
- FR-062: Rules MUST be prioritized
- FR-063: First match MUST apply
- FR-064: Default rule MUST exist
- FR-065: Rule override MUST be logged

---

## Non-Functional Requirements

- NFR-001: Analysis MUST be <500ms per file
- NFR-002: 100 files MUST analyze in <10s
- NFR-003: Memory MUST be bounded
- NFR-004: Pattern matching MUST be cached
- NFR-005: Results MUST be deterministic
- NFR-006: Same inputs MUST yield same output
- NFR-007: Errors MUST not crash
- NFR-008: Fallback MUST always work
- NFR-009: Logging MUST explain decisions
- NFR-010: Metrics MUST track accuracy

---

## User Manual Documentation

### Configuration

```yaml
heuristics:
  lineOverlap:
    contextLines: 5
    nearThresholdLines: 10
    ignoreWhitespace: true
    
  severity:
    criticalThreshold: 0.8
    highThreshold: 0.5
    mediumThreshold: 0.2
    
  scope:
    enabled: true
    sameFunctionMultiplier: 2.0
    sameClassMultiplier: 1.5
    
  fileRules:
    - pattern: "*.lock"
      action: regenerate
    - pattern: "*.generated.cs"
      action: regenerate
    - pattern: "**/migrations/*.cs"
      action: strict
    - pattern: "**/*.test.cs"
      mode: lenient
```

### Severity Levels

| Severity | Score | Meaning |
|----------|-------|---------|
| Critical | ≥0.8 | Direct conflict, blocks merge |
| High | ≥0.5 | Likely conflict, needs review |
| Medium | ≥0.2 | Possible conflict, warning |
| Low | <0.2 | Unlikely conflict, auto-merge |

### Scope Patterns

```yaml
scopePatterns:
  csharp:
    method: '^\s*(public|private|protected|internal).*\w+\s*\('
    class: '^\s*(public|private|protected|internal)?\s*class\s+\w+'
  python:
    method: '^\s*def\s+\w+\s*\('
    class: '^\s*class\s+\w+'
  javascript:
    method: '^\s*(function|async function|const\s+\w+\s*=.*=>)'
    class: '^\s*class\s+\w+'
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Line overlap detected
- [ ] AC-002: Context considered
- [ ] AC-003: Severity computed
- [ ] AC-004: Scope detected
- [ ] AC-005: Scope affects severity
- [ ] AC-006: File rules applied
- [ ] AC-007: Lock files handled
- [ ] AC-008: Binary files blocked
- [ ] AC-009: Config tunable
- [ ] AC-010: Performance OK
- [ ] AC-011: Logging clear
- [ ] AC-012: Tests comprehensive

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Line overlap detection
- [ ] UT-002: Severity calculation
- [ ] UT-003: Scope detection
- [ ] UT-004: File rule matching
- [ ] UT-005: Edge cases

### Integration Tests

- [ ] IT-001: Real diff analysis
- [ ] IT-002: Multi-file changes
- [ ] IT-003: Mixed file types
- [ ] IT-004: Performance benchmarks

---

## Implementation Prompt

### Interface

```csharp
public interface IConflictHeuristics
{
    Task<ConflictAnalysis> AnalyzeAsync(
        IReadOnlyList<FileChange> localChanges,
        IReadOnlyList<FileChange> remoteChanges,
        CancellationToken ct = default);
}

public record FileChange(
    string Path,
    ChangeType Type,
    IReadOnlyList<LineDiff> Diffs);

public record LineDiff(
    int StartLine,
    int EndLine,
    DiffType Type);

public enum DiffType { Add, Remove, Modify }

public record ConflictAnalysis(
    IReadOnlyList<FileConflict> Conflicts,
    ConflictSeverity MaxSeverity,
    TimeSpan AnalysisDuration);

public record FileConflict(
    string Path,
    double OverlapScore,
    ConflictSeverity Severity,
    IReadOnlyList<LineOverlap> Overlaps,
    ScopeMatch? Scope,
    string AppliedRule);

public record LineOverlap(
    LineRange Local,
    LineRange Remote,
    double Score);

public record ScopeMatch(
    string ScopeType,  // method, class, etc.
    string ScopeName,
    double SeverityMultiplier);
```

---

**End of Task 028.a Specification**