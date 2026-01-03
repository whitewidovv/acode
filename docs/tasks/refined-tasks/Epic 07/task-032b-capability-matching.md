# Task 032.b: Capability Matching

**Priority:** P1 – High  
**Tier:** L – Cloud Integration  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 032 (Placement), Task 032.a (Discovery)  

---

## Description

Task 032.b implements capability matching. Task requirements MUST be compared against target capabilities. Match scores MUST be calculated. Mismatches MUST be reported.

Matching determines if a target can run a task. Hard requirements MUST be satisfied. Soft requirements influence scoring.

Match results inform placement decisions. Detailed mismatch reports help debugging.

### Business Value

Capability matching enables:
- Accurate task placement
- Clear failure reasons
- Requirement validation
- Optimal target selection

### Scope Boundaries

This task covers matching logic. Discovery is in 032.a. Placement strategy is in 032.c.

### Integration Points

- Task 032.a: Provides capabilities
- Task 032: Used by placement engine
- Task 025: Task spec requirements

### Failure Modes

- Unknown capability → Skip match
- Missing requirement → Hard fail
- Type mismatch → Convert or fail
- Partial match → Score reduction

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Match | Requirement satisfied |
| Mismatch | Requirement not met |
| Score | Quantified fit |
| Hard | Must satisfy |
| Soft | Prefer but optional |
| Threshold | Minimum to pass |

---

## Out of Scope

- Fuzzy matching
- Machine learning scoring
- Historical performance weighting
- User preference learning
- A/B testing of strategies

---

## Functional Requirements

### FR-001 to FR-020: Matcher Interface

- FR-001: `ICapabilityMatcher` MUST exist
- FR-002: `MatchAsync` MUST return result
- FR-003: Input: requirements
- FR-004: Input: capabilities
- FR-005: Output: match result
- FR-006: Result MUST include score
- FR-007: Score: 0.0 to 1.0
- FR-008: Result MUST include matches
- FR-009: Result MUST include mismatches
- FR-010: Hard mismatch MUST fail
- FR-011: Score 0.0 on hard fail
- FR-012: Soft mismatch MUST reduce score
- FR-013: Match MUST increase score
- FR-014: Weights MUST be configurable
- FR-015: Default weights MUST exist
- FR-016: Match reasons MUST be explained
- FR-017: Mismatch reasons MUST be explained
- FR-018: Partial match MUST work
- FR-019: Unknown capability MUST be handled
- FR-020: Match MUST be deterministic

### FR-021 to FR-040: Requirement Types

- FR-021: Numeric comparison MUST work
- FR-022: Greater than MUST work
- FR-023: Less than MUST work
- FR-024: Equal MUST work
- FR-025: Range MUST work
- FR-026: Boolean comparison MUST work
- FR-027: Presence check MUST work
- FR-028: String comparison MUST work
- FR-029: Exact match MUST work
- FR-030: Contains MUST work
- FR-031: Regex MUST work
- FR-032: Version comparison MUST work
- FR-033: Semver comparison MUST work
- FR-034: Version >= MUST work
- FR-035: List membership MUST work
- FR-036: Any of list MUST work
- FR-037: All of list MUST work
- FR-038: Nested requirements MUST work
- FR-039: GPU sub-requirements MUST work
- FR-040: Custom comparators MUST be pluggable

### FR-041 to FR-060: Scoring

- FR-041: Base score MUST start at 1.0
- FR-042: Hard fail MUST set 0.0
- FR-043: Soft miss MUST reduce
- FR-044: Reduction MUST be weighted
- FR-045: Weight per requirement type
- FR-046: Excess capacity MUST bonus
- FR-047: Example: more CPU than needed
- FR-048: Bonus MUST be capped
- FR-049: Max bonus: +0.1
- FR-050: Cost MUST influence score
- FR-051: Lower cost MUST score higher
- FR-052: Cost weight MUST be configurable
- FR-053: Locality MUST influence score
- FR-054: Local MUST score higher default
- FR-055: Locality weight configurable
- FR-056: Aggregation MUST work
- FR-057: Weighted average MUST be used
- FR-058: Min score threshold MUST exist
- FR-059: Default threshold: 0.5
- FR-060: Below threshold MUST fail

### FR-061 to FR-075: Diagnostics

- FR-061: Match report MUST be generated
- FR-062: Report MUST be human-readable
- FR-063: Report MUST list all checks
- FR-064: Report MUST show pass/fail
- FR-065: Report MUST show values
- FR-066: Expected vs actual MUST show
- FR-067: Score breakdown MUST show
- FR-068: Suggestions MUST be provided
- FR-069: Example: "Add 4GB RAM"
- FR-070: Report MUST be serializable
- FR-071: JSON format MUST work
- FR-072: CLI display MUST work
- FR-073: Log integration MUST work
- FR-074: Metrics MUST track matches
- FR-075: Metrics MUST track mismatches

---

## Non-Functional Requirements

- NFR-001: Match in <10ms
- NFR-002: 100 requirements supported
- NFR-003: Deterministic results
- NFR-004: No side effects
- NFR-005: Memory efficient
- NFR-006: Structured logging
- NFR-007: Metrics on match types
- NFR-008: Clear diagnostics
- NFR-009: Extensible comparators
- NFR-010: Thread-safe

---

## User Manual Documentation

### Match Result Example

```
Target: build-server
Score: 0.85

✓ cpu.count >= 4 (has 8)
✓ memory.gb >= 16 (has 32)
✓ gpu.present = true
✗ gpu.vram >= 16 (has 8) [SOFT]
✓ tools.docker = present
✓ os = linux

Hard Requirements: 5/5 passed
Soft Requirements: 0/1 passed

Suggestions:
- GPU has 8GB VRAM, task prefers 16GB
```

### Configuration

```yaml
matching:
  weights:
    cpu: 1.0
    memory: 1.0
    gpu: 2.0
    tools: 0.5
    cost: 1.5
    locality: 1.0
  thresholds:
    minimum: 0.5
    excellent: 0.9
  excessCapacityBonus: 0.1
```

### Comparison Operators

| Operator | Example | Meaning |
|----------|---------|---------|
| >= | cpu >= 4 | At least 4 |
| <= | cost <= 5.0 | At most 5.0 |
| == | os == linux | Exactly linux |
| != | arch != arm | Not arm |
| ~= | version ~= 3.x | Version pattern |
| in | tool in [a,b] | One of list |

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Numeric matching works
- [ ] AC-002: Boolean matching works
- [ ] AC-003: String matching works
- [ ] AC-004: Version matching works
- [ ] AC-005: Hard fail works
- [ ] AC-006: Soft reduction works
- [ ] AC-007: Scoring accurate
- [ ] AC-008: Report generated
- [ ] AC-009: Suggestions provided
- [ ] AC-010: Deterministic verified

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Numeric comparisons
- [ ] UT-002: Version comparisons
- [ ] UT-003: Score calculation
- [ ] UT-004: Report generation

### Integration Tests

- [ ] IT-001: Full match cycle
- [ ] IT-002: Multi-target matching
- [ ] IT-003: Edge cases
- [ ] IT-004: Performance test

---

## Implementation Prompt

### Interface

```csharp
public interface ICapabilityMatcher
{
    MatchResult Match(
        TaskRequirements requirements,
        TargetCapabilities capabilities,
        MatchOptions options = null);
}

public record MatchResult(
    double Score,
    bool Passed,
    IReadOnlyList<RequirementMatch> Matches,
    IReadOnlyList<RequirementMismatch> Mismatches,
    IReadOnlyList<string> Suggestions);

public record RequirementMatch(
    string RequirementName,
    string Operator,
    object ExpectedValue,
    object ActualValue,
    double ScoreContribution);

public record RequirementMismatch(
    string RequirementName,
    string Operator,
    object ExpectedValue,
    object ActualValue,
    bool IsHard,
    double ScorePenalty,
    string Suggestion);

public record MatchOptions(
    double MinimumScore = 0.5,
    IReadOnlyDictionary<string, double> Weights = null,
    bool IncludeExcessBonus = true);
```

### Comparators

```csharp
public interface IRequirementComparator
{
    string OperatorName { get; }
    bool Compare(object expected, object actual);
    double Score(object expected, object actual);
}

public class NumericComparator : IRequirementComparator { }
public class VersionComparator : IRequirementComparator { }
public class StringComparator : IRequirementComparator { }
public class ListComparator : IRequirementComparator { }
```

---

**End of Task 032.b Specification**