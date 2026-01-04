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

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Task 032.a Discovery | ICapabilityDiscovery | Caps → Matcher | Provides target capabilities |
| Task 032 Placement | ICapabilityMatcher | Requirements → Match | Matcher used by placement |
| Task 025 Task Spec | TaskRequirements | Spec → Parser | Task requirements definition |
| Strategy Engine | IPlacementStrategy | Score → Strategy | Scores used for selection |
| agent-config.yml | Config parser | Weights → Matcher | Configurable scoring weights |
| CLI Output | MatchReportGenerator | Result → Display | Human-readable reports |
| Metrics System | IMetrics | Stats → Metrics | Match/mismatch telemetry |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Unknown capability | Key not found | Skip match, log warning | Reduced match accuracy |
| Type mismatch | Cast exception | Try conversion, else fail | Match failure |
| Hard requirement miss | Score = 0 | Report mismatch | Target excluded |
| Invalid comparator | Operator not found | Use default equals | May produce wrong result |
| Malformed version | Parse failure | String compare fallback | Version match degraded |
| Missing weight config | Key not found | Use default weight 1.0 | Scoring slightly off |
| Threshold not met | Score < minimum | Fail match | Target excluded |
| Concurrent match | Race condition | Thread-safe design | No issue |

---

## Assumptions

1. Task requirements are well-formed and validated before matching
2. Target capabilities have been discovered or manually configured
3. Numeric comparisons use double precision with tolerance for floats
4. Version strings follow semver or major.minor.patch format
5. Hard requirements must all pass for score > 0
6. Soft requirements can all fail and still pass if score >= threshold
7. Weight configuration is optional with sensible defaults
8. Suggestions are best-effort and not guaranteed actionable

---

## Security Considerations

1. Match results MUST NOT leak internal capability values to unauthorized users
2. Suggestion strings MUST NOT include sensitive paths or credentials
3. Comparator regex patterns MUST be validated to prevent ReDoS
4. Match reports MUST be sanitized before display
5. Custom comparators MUST be sandboxed from system access
6. Weight configurations MUST be validated for reasonable ranges
7. Match duration MUST be bounded to prevent DoS
8. Serialized match reports MUST NOT include internal object references

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

### Matcher Interface

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032B-01 | `ICapabilityMatcher` interface MUST exist | P0 |
| FR-032B-02 | `Match` method MUST return `MatchResult` | P0 |
| FR-032B-03 | Input: `TaskRequirements` object | P0 |
| FR-032B-04 | Input: `TargetCapabilities` object | P0 |
| FR-032B-05 | Output: `MatchResult` with score and details | P0 |
| FR-032B-06 | Score MUST be included in result | P0 |
| FR-032B-07 | Score range: 0.0 to 1.0 (with bonus up to 1.1) | P0 |
| FR-032B-08 | Result MUST list all successful matches | P0 |
| FR-032B-09 | Result MUST list all mismatches | P0 |
| FR-032B-10 | Hard mismatch MUST set score to 0.0 | P0 |
| FR-032B-11 | Any single hard fail causes overall fail | P0 |
| FR-032B-12 | Soft mismatch MUST reduce score proportionally | P1 |
| FR-032B-13 | Each match MUST contribute to score | P0 |
| FR-032B-14 | Requirement weights MUST be configurable | P1 |
| FR-032B-15 | Default weights MUST be provided | P0 |
| FR-032B-16 | Match reasons MUST include expected vs actual | P1 |
| FR-032B-17 | Mismatch reasons MUST explain what failed | P1 |
| FR-032B-18 | Partial match (some pass, some fail) MUST work | P0 |
| FR-032B-19 | Unknown capability MUST be handled gracefully | P1 |
| FR-032B-20 | Match results MUST be deterministic | P0 |

### Requirement Types

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032B-21 | Numeric comparison (>=, <=, ==, !=) MUST work | P0 |
| FR-032B-22 | Greater-than-or-equal (>=) MUST work | P0 |
| FR-032B-23 | Less-than-or-equal (<=) MUST work | P0 |
| FR-032B-24 | Equal (==) MUST work with tolerance | P0 |
| FR-032B-25 | Range (min <= x <= max) MUST work | P1 |
| FR-032B-26 | Boolean comparison (true/false) MUST work | P0 |
| FR-032B-27 | Presence check (exists) MUST work | P0 |
| FR-032B-28 | String comparison (==, !=) MUST work | P0 |
| FR-032B-29 | Exact string match MUST be case-sensitive | P0 |
| FR-032B-30 | Contains substring MUST work | P1 |
| FR-032B-31 | Regex pattern matching MUST work | P1 |
| FR-032B-32 | Version comparison MUST work | P0 |
| FR-032B-33 | Semver comparison MUST work (1.2.3) | P0 |
| FR-032B-34 | Version >= (at least) MUST work | P0 |
| FR-032B-35 | List membership (in list) MUST work | P1 |
| FR-032B-36 | Any-of list (OR) MUST work | P1 |
| FR-032B-37 | All-of list (AND) MUST work | P1 |
| FR-032B-38 | Nested requirements (gpu.vram) MUST work | P0 |
| FR-032B-39 | GPU sub-requirements (type, vram, count) | P1 |
| FR-032B-40 | Custom comparators MUST be pluggable via DI | P2 |

### Scoring

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032B-41 | Base score MUST start at 1.0 | P0 |
| FR-032B-42 | Any hard fail MUST set final score to 0.0 | P0 |
| FR-032B-43 | Soft miss MUST reduce score by weighted amount | P1 |
| FR-032B-44 | Reduction MUST be proportional to weight | P1 |
| FR-032B-45 | Each requirement type has configurable weight | P1 |
| FR-032B-46 | Excess capacity MUST provide bonus | P1 |
| FR-032B-47 | Example: 8 CPUs when 4 required = bonus | P1 |
| FR-032B-48 | Bonus MUST be capped to prevent runaway | P1 |
| FR-032B-49 | Max bonus: +0.1 (score can reach 1.1) | P1 |
| FR-032B-50 | Cost MUST influence score negatively | P1 |
| FR-032B-51 | Lower cost targets MUST score higher | P1 |
| FR-032B-52 | Cost weight MUST be configurable | P1 |
| FR-032B-53 | Locality MUST influence score | P1 |
| FR-032B-54 | Local targets MUST score higher by default | P1 |
| FR-032B-55 | Locality weight MUST be configurable | P1 |
| FR-032B-56 | Score aggregation MUST handle all matches | P0 |
| FR-032B-57 | Weighted average for final score | P0 |
| FR-032B-58 | Minimum score threshold MUST exist | P0 |
| FR-032B-59 | Default threshold: 0.5 | P1 |
| FR-032B-60 | Below threshold MUST result in Passed=false | P0 |

### Diagnostics

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-032B-61 | Match report MUST be generatable | P1 |
| FR-032B-62 | Report MUST be human-readable text | P1 |
| FR-032B-63 | Report MUST list all requirement checks | P1 |
| FR-032B-64 | Report MUST show pass (✓) or fail (✗) | P1 |
| FR-032B-65 | Report MUST show expected and actual values | P1 |
| FR-032B-66 | Side-by-side expected vs actual display | P1 |
| FR-032B-67 | Score breakdown by requirement | P2 |
| FR-032B-68 | Suggestions MUST be provided for mismatches | P1 |
| FR-032B-69 | Example: "Add 4GB RAM to meet requirement" | P1 |
| FR-032B-70 | Report MUST serialize to JSON | P1 |
| FR-032B-71 | JSON format for programmatic use | P1 |
| FR-032B-72 | CLI display MUST render report cleanly | P1 |
| FR-032B-73 | Report MUST integrate with logging | P1 |
| FR-032B-74 | Metrics MUST track match counts | P1 |
| FR-032B-75 | Metrics MUST track mismatch counts by type | P1 |

---

## Non-Functional Requirements

### Performance

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-032B-01 | Single match operation | <10ms | P0 |
| NFR-032B-02 | Requirements per match | 100 supported | P1 |
| NFR-032B-03 | Deterministic results | Same input = same output | P0 |
| NFR-032B-04 | Memory allocation | <1KB per match | P1 |
| NFR-032B-05 | Comparator lookup | O(1) | P1 |
| NFR-032B-06 | Score calculation | O(n) requirements | P0 |
| NFR-032B-07 | Report generation | <5ms | P1 |
| NFR-032B-08 | Parallel match calls | Thread-safe | P0 |
| NFR-032B-09 | Regex compilation | Cached | P1 |
| NFR-032B-10 | Version parsing | Cached | P1 |

### Reliability

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-032B-11 | No side effects | Pure function | P0 |
| NFR-032B-12 | Exception safety | Catch and handle | P0 |
| NFR-032B-13 | Unknown capability handling | Graceful skip | P1 |
| NFR-032B-14 | Type conversion errors | Safe fallback | P1 |
| NFR-032B-15 | Invalid regex patterns | Caught with error | P1 |
| NFR-032B-16 | Null input handling | Validated | P0 |
| NFR-032B-17 | Empty requirements | Score 1.0 | P1 |
| NFR-032B-18 | Empty capabilities | All mismatches | P1 |
| NFR-032B-19 | Partial capability data | Works with available | P1 |
| NFR-032B-20 | Concurrent usage | No locks needed | P0 |

### Observability

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-032B-21 | Structured logging | All match operations | P0 |
| NFR-032B-22 | Match duration metric | Histogram | P1 |
| NFR-032B-23 | Match pass/fail counter | Counter | P1 |
| NFR-032B-24 | Score distribution metric | Histogram | P2 |
| NFR-032B-25 | Mismatch type breakdown | Counter by type | P1 |
| NFR-032B-26 | MatchCompletedEvent | Published on complete | P1 |
| NFR-032B-27 | MatchFailedEvent | Published on hard fail | P1 |
| NFR-032B-28 | Trace correlation | Request ID | P1 |
| NFR-032B-29 | Diagnostic report export | JSON | P1 |
| NFR-032B-30 | Debug verbose logging | Toggle | P2 |

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

### Matcher Core
- [ ] AC-001: `ICapabilityMatcher` interface exists with `Match` method
- [ ] AC-002: `Match` returns `MatchResult` with score
- [ ] AC-003: Score is between 0.0 and 1.1 (with bonus)
- [ ] AC-004: All matches listed in result
- [ ] AC-005: All mismatches listed in result
- [ ] AC-006: Hard mismatch sets score to 0.0
- [ ] AC-007: Soft mismatch reduces score proportionally
- [ ] AC-008: Match reasons include expected vs actual
- [ ] AC-009: Mismatch reasons explain what failed
- [ ] AC-010: Unknown capability handled gracefully

### Numeric Comparisons
- [ ] AC-011: Greater-than-or-equal (>=) works for CPU
- [ ] AC-012: Greater-than-or-equal (>=) works for memory
- [ ] AC-013: Less-than-or-equal (<=) works for cost
- [ ] AC-014: Equal (==) works with float tolerance
- [ ] AC-015: Range comparison works
- [ ] AC-016: Excess capacity provides bonus

### Boolean Comparisons
- [ ] AC-017: Boolean true/false comparison works
- [ ] AC-018: Presence check (exists) works
- [ ] AC-019: gpu.present = true works
- [ ] AC-020: docker.available = true works

### String Comparisons
- [ ] AC-021: Exact string match works
- [ ] AC-022: Contains substring works
- [ ] AC-023: Case-sensitive by default
- [ ] AC-024: Regex pattern matching works
- [ ] AC-025: OS comparison (linux, windows) works

### Version Comparisons
- [ ] AC-026: Semver comparison works
- [ ] AC-027: Version >= minimum works
- [ ] AC-028: python >= 3.10 works
- [ ] AC-029: Invalid version handled gracefully
- [ ] AC-030: Version parsing is cached

### List Comparisons
- [ ] AC-031: Any-of list (OR) works
- [ ] AC-032: All-of list (AND) works
- [ ] AC-033: tool in [git, docker] works
- [ ] AC-034: Empty list handled

### Scoring
- [ ] AC-035: Base score starts at 1.0
- [ ] AC-036: Weights applied correctly
- [ ] AC-037: Default weights used when not configured
- [ ] AC-038: Excess capacity bonus calculated
- [ ] AC-039: Bonus capped at 0.1
- [ ] AC-040: Cost reduces score for expensive targets
- [ ] AC-041: Locality boosts local targets
- [ ] AC-042: Weighted average calculated correctly
- [ ] AC-043: Minimum threshold enforced
- [ ] AC-044: Below threshold sets Passed = false

### Diagnostics
- [ ] AC-045: Match report generated
- [ ] AC-046: Report shows ✓ for passes
- [ ] AC-047: Report shows ✗ for failures
- [ ] AC-048: Expected vs actual displayed
- [ ] AC-049: Suggestions provided for mismatches
- [ ] AC-050: Report serializes to JSON
- [ ] AC-051: CLI renders report cleanly
- [ ] AC-052: MatchCompletedEvent published
- [ ] AC-053: Metrics track match counts
- [ ] AC-054: Metrics track mismatch types

---

## User Verification Scenarios

### Scenario 1: CPU Requirement Match
**Persona:** Developer with 4-CPU task  
**Preconditions:** Target has 8 CPUs  
**Steps:**
1. Define task requiring 4 CPUs (hard)
2. Run match against target
3. Check result score
4. Verify match details

**Verification Checklist:**
- [ ] CPU requirement satisfied (8 >= 4)
- [ ] Score includes match contribution
- [ ] Excess capacity bonus applied
- [ ] Match reason shows "has 8, needs 4"

### Scenario 2: Hard Requirement Failure
**Persona:** Developer with GPU task  
**Preconditions:** Target has no GPU  
**Steps:**
1. Define task requiring GPU (hard)
2. Run match against non-GPU target
3. Check score is 0.0
4. Check mismatch details

**Verification Checklist:**
- [ ] GPU requirement not met
- [ ] Score = 0.0 (hard fail)
- [ ] Mismatch explains GPU absent
- [ ] Suggestion provided

### Scenario 3: Soft Requirement Reduction
**Persona:** Developer preferring 32GB RAM  
**Preconditions:** Target has 16GB RAM  
**Steps:**
1. Define soft requirement: memory >= 32
2. Run match against 16GB target
3. Check score reduced but not zero
4. Verify mismatch logged

**Verification Checklist:**
- [ ] Memory mismatch detected (16 < 32)
- [ ] Score reduced by weighted amount
- [ ] Score > 0.0 (soft, not hard)
- [ ] Suggestion: "Upgrade to 32GB"

### Scenario 4: Version Comparison
**Persona:** Developer needing Python 3.10+  
**Preconditions:** Target has Python 3.9.7  
**Steps:**
1. Define requirement: python >= 3.10
2. Run match against 3.9.7 target
3. Check version mismatch
4. Verify semver comparison

**Verification Checklist:**
- [ ] Version comparison uses semver
- [ ] 3.9.7 < 3.10 detected
- [ ] Mismatch includes versions
- [ ] Suggestion: "Upgrade to 3.10+"

### Scenario 5: Multi-Target Scoring
**Persona:** Developer choosing best target  
**Preconditions:** 3 targets with varying specs  
**Steps:**
1. Define task requirements
2. Match against all 3 targets
3. Compare scores
4. Select highest scoring target

**Verification Checklist:**
- [ ] All 3 matches complete
- [ ] Scores are different
- [ ] Best match has highest score
- [ ] Scores are deterministic

### Scenario 6: Match Report Display
**Persona:** Developer debugging placement  
**Preconditions:** Match completed with mixed results  
**Steps:**
1. Run match with some passes, some failures
2. Generate report
3. View CLI display
4. Export JSON

**Verification Checklist:**
- [ ] Report shows all checks
- [ ] ✓ and ✗ symbols displayed
- [ ] Expected vs actual shown
- [ ] JSON export works

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-032B-01 | Numeric >= comparison | FR-032B-22 |
| UT-032B-02 | Numeric <= comparison | FR-032B-23 |
| UT-032B-03 | Numeric == with tolerance | FR-032B-24 |
| UT-032B-04 | Boolean comparison | FR-032B-26 |
| UT-032B-05 | Presence check | FR-032B-27 |
| UT-032B-06 | String exact match | FR-032B-29 |
| UT-032B-07 | String contains | FR-032B-30 |
| UT-032B-08 | Regex matching | FR-032B-31 |
| UT-032B-09 | Semver comparison | FR-032B-33 |
| UT-032B-10 | Version >= | FR-032B-34 |
| UT-032B-11 | Any-of list | FR-032B-36 |
| UT-032B-12 | All-of list | FR-032B-37 |
| UT-032B-13 | Hard fail sets score 0.0 | FR-032B-42 |
| UT-032B-14 | Soft miss reduces score | FR-032B-43 |
| UT-032B-15 | Excess capacity bonus | FR-032B-46 |
| UT-032B-16 | Bonus capped at 0.1 | FR-032B-49 |
| UT-032B-17 | Weighted average calculation | FR-032B-57 |
| UT-032B-18 | Threshold enforcement | FR-032B-60 |
| UT-032B-19 | Unknown capability handling | FR-032B-19 |
| UT-032B-20 | Deterministic results | NFR-032B-03 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-032B-01 | Full match with real requirements | E2E |
| IT-032B-02 | Multi-target comparison | Scoring |
| IT-032B-03 | Edge case: empty requirements | NFR-032B-17 |
| IT-032B-04 | Edge case: empty capabilities | NFR-032B-18 |
| IT-032B-05 | Report generation | FR-032B-61 |
| IT-032B-06 | JSON serialization | FR-032B-70 |
| IT-032B-07 | CLI display rendering | FR-032B-72 |
| IT-032B-08 | Event publishing | NFR-032B-26 |
| IT-032B-09 | Metrics emission | NFR-032B-22 |
| IT-032B-10 | Performance <10ms | NFR-032B-01 |
| IT-032B-11 | 100 requirements performance | NFR-032B-02 |
| IT-032B-12 | Thread safety | NFR-032B-08 |
| IT-032B-13 | Suggestion generation | FR-032B-68 |
| IT-032B-14 | Custom comparator pluggability | FR-032B-40 |
| IT-032B-15 | Nested requirement matching | FR-032B-38 |

---

## Implementation Prompt

### Part 1: File Structure + Domain Models

```
src/
├── Acode.Domain/
│   └── Compute/
│       └── Matching/
│           ├── ComparisonOperator.cs
│           ├── RequirementType.cs
│           └── Events/
│               ├── MatchCompletedEvent.cs
│               └── MatchFailedEvent.cs
├── Acode.Application/
│   └── Compute/
│       └── Matching/
│           ├── ICapabilityMatcher.cs
│           ├── IRequirementComparator.cs
│           ├── MatchOptions.cs
│           ├── MatchResult.cs
│           ├── RequirementMatch.cs
│           └── RequirementMismatch.cs
└── Acode.Infrastructure/
    └── Compute/
        └── Matching/
            ├── CapabilityMatcher.cs
            ├── MatchReportGenerator.cs
            └── Comparators/
                ├── NumericComparator.cs
                ├── VersionComparator.cs
                ├── StringComparator.cs
                ├── BooleanComparator.cs
                └── ListComparator.cs
```

```csharp
// src/Acode.Domain/Compute/Matching/ComparisonOperator.cs
namespace Acode.Domain.Compute.Matching;

public enum ComparisonOperator
{
    GreaterThanOrEqual,
    LessThanOrEqual,
    Equal,
    NotEqual,
    Contains,
    Regex,
    VersionAtLeast,
    AnyOf,
    AllOf
}

// src/Acode.Domain/Compute/Matching/RequirementType.cs
namespace Acode.Domain.Compute.Matching;

public enum RequirementType
{
    Hard,   // Must satisfy, fail = score 0
    Soft    // Prefer, miss = reduced score
}

// src/Acode.Domain/Compute/Matching/Events/MatchCompletedEvent.cs
namespace Acode.Domain.Compute.Matching.Events;

public sealed record MatchCompletedEvent(
    string TargetId,
    double Score,
    bool Passed,
    int MatchCount,
    int MismatchCount,
    TimeSpan Duration,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Matching/Events/MatchFailedEvent.cs
namespace Acode.Domain.Compute.Matching.Events;

public sealed record MatchFailedEvent(
    string TargetId,
    string Reason,
    IReadOnlyList<string> HardMismatches,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 032.b Specification - Part 1/3**

### Part 2: Application Interfaces

```csharp
// src/Acode.Application/Compute/Matching/MatchOptions.cs
namespace Acode.Application.Compute.Matching;

public sealed record MatchOptions
{
    public double MinimumScore { get; init; } = 0.5;
    public double ExcellentScore { get; init; } = 0.9;
    public IReadOnlyDictionary<string, double> Weights { get; init; } = new Dictionary<string, double>
    {
        ["cpu"] = 1.0, ["memory"] = 1.0, ["gpu"] = 2.0,
        ["tools"] = 0.5, ["cost"] = 1.5, ["locality"] = 1.0
    };
    public bool IncludeExcessBonus { get; init; } = true;
    public double MaxExcessBonus { get; init; } = 0.1;
}

// src/Acode.Application/Compute/Matching/RequirementMatch.cs
namespace Acode.Application.Compute.Matching;

public sealed record RequirementMatch
{
    public required string RequirementName { get; init; }
    public required ComparisonOperator Operator { get; init; }
    public required object ExpectedValue { get; init; }
    public required object ActualValue { get; init; }
    public double ScoreContribution { get; init; }
    public double? ExcessBonus { get; init; }
}

// src/Acode.Application/Compute/Matching/RequirementMismatch.cs
namespace Acode.Application.Compute.Matching;

public sealed record RequirementMismatch
{
    public required string RequirementName { get; init; }
    public required ComparisonOperator Operator { get; init; }
    public required object ExpectedValue { get; init; }
    public object? ActualValue { get; init; }
    public RequirementType Type { get; init; }
    public double ScorePenalty { get; init; }
    public string? Suggestion { get; init; }
}

// src/Acode.Application/Compute/Matching/MatchResult.cs
namespace Acode.Application.Compute.Matching;

public sealed record MatchResult
{
    public double Score { get; init; }
    public bool Passed { get; init; }
    public IReadOnlyList<RequirementMatch> Matches { get; init; } = [];
    public IReadOnlyList<RequirementMismatch> Mismatches { get; init; } = [];
    public IReadOnlyList<string> Suggestions { get; init; } = [];
    public TimeSpan Duration { get; init; }
}

// src/Acode.Application/Compute/Matching/IRequirementComparator.cs
namespace Acode.Application.Compute.Matching;

public interface IRequirementComparator
{
    ComparisonOperator Operator { get; }
    bool CanCompare(Type expectedType, Type actualType);
    bool Compare(object expected, object actual);
    double CalculateScore(object expected, object actual);
    string GenerateSuggestion(string name, object expected, object actual);
}

// src/Acode.Application/Compute/Matching/ICapabilityMatcher.cs
namespace Acode.Application.Compute.Matching;

public interface ICapabilityMatcher
{
    MatchResult Match(
        TaskRequirements requirements,
        TargetCapabilities capabilities,
        MatchOptions? options = null);
    
    string GenerateReport(MatchResult result, bool verbose = false);
}
```

**End of Task 032.b Specification - Part 2/3**

### Part 3: Infrastructure Implementation + Checklist

```csharp
// src/Acode.Infrastructure/Compute/Matching/Comparators/NumericComparator.cs
namespace Acode.Infrastructure.Compute.Matching.Comparators;

public sealed class NumericComparator : IRequirementComparator
{
    private readonly ComparisonOperator _op;
    public ComparisonOperator Operator => _op;
    
    public NumericComparator(ComparisonOperator op) => _op = op;
    
    public bool CanCompare(Type expected, Type actual) => 
        IsNumeric(expected) && IsNumeric(actual);
    
    public bool Compare(object expected, object actual)
    {
        var exp = Convert.ToDouble(expected);
        var act = Convert.ToDouble(actual);
        return _op switch
        {
            ComparisonOperator.GreaterThanOrEqual => act >= exp,
            ComparisonOperator.LessThanOrEqual => act <= exp,
            ComparisonOperator.Equal => Math.Abs(act - exp) < 0.001,
            ComparisonOperator.NotEqual => Math.Abs(act - exp) >= 0.001,
            _ => false
        };
    }
    
    public double CalculateScore(object expected, object actual)
    {
        if (!Compare(expected, actual)) return 0.0;
        var exp = Convert.ToDouble(expected);
        var act = Convert.ToDouble(actual);
        // Bonus for excess capacity, capped at 0.1
        var excess = (act - exp) / exp;
        return 1.0 + Math.Min(excess * 0.05, 0.1);
    }
    
    public string GenerateSuggestion(string name, object expected, object actual) =>
        $"Upgrade {name}: has {actual}, needs {expected}";
}

// src/Acode.Infrastructure/Compute/Matching/Comparators/VersionComparator.cs
namespace Acode.Infrastructure.Compute.Matching.Comparators;

public sealed class VersionComparator : IRequirementComparator
{
    public ComparisonOperator Operator => ComparisonOperator.VersionAtLeast;
    
    public bool CanCompare(Type expected, Type actual) => 
        expected == typeof(string) && actual == typeof(string);
    
    public bool Compare(object expected, object actual)
    {
        if (!Version.TryParse(expected.ToString(), out var exp) ||
            !Version.TryParse(actual.ToString(), out var act))
            return false;
        return act >= exp;
    }
    
    public double CalculateScore(object expected, object actual) => 
        Compare(expected, actual) ? 1.0 : 0.0;
    
    public string GenerateSuggestion(string name, object expected, object actual) =>
        $"Upgrade {name} from {actual} to >= {expected}";
}

// src/Acode.Infrastructure/Compute/Matching/CapabilityMatcher.cs
namespace Acode.Infrastructure.Compute.Matching;

public sealed class CapabilityMatcher : ICapabilityMatcher
{
    private readonly IEnumerable<IRequirementComparator> _comparators;
    private readonly ILogger<CapabilityMatcher> _logger;
    
    public MatchResult Match(
        TaskRequirements requirements,
        TargetCapabilities capabilities,
        MatchOptions? options = null)
    {
        options ??= new MatchOptions();
        var sw = Stopwatch.StartNew();
        var matches = new List<RequirementMatch>();
        var mismatches = new List<RequirementMismatch>();
        
        // Check each requirement
        CheckNumeric("cpu", requirements.MinCpu, capabilities.Hardware.CpuCount, RequirementType.Hard, options, matches, mismatches);
        CheckNumeric("memory", requirements.MinMemoryGb, capabilities.Hardware.MemoryGb, RequirementType.Hard, options, matches, mismatches);
        CheckGpu(requirements.Gpu, capabilities.Hardware.Gpu, options, matches, mismatches);
        CheckTools(requirements.RequiredTools, capabilities.Software.ToolVersions, matches, mismatches);
        
        // Calculate final score
        var hasHardFail = mismatches.Any(m => m.Type == RequirementType.Hard);
        var score = hasHardFail ? 0.0 : CalculateWeightedScore(matches, mismatches, options);
        
        return new MatchResult
        {
            Score = score,
            Passed = score >= options.MinimumScore,
            Matches = matches,
            Mismatches = mismatches,
            Suggestions = mismatches.Select(m => m.Suggestion).OfType<string>().ToList(),
            Duration = sw.Elapsed
        };
    }
}
```

### Implementation Checklist

| Step | Action | Verification |
|------|--------|--------------|
| 1 | Create domain enums (ComparisonOperator, RequirementType) | Enums compile |
| 2 | Add match events | Event serialization verified |
| 3 | Define MatchOptions, MatchResult records | Records compile |
| 4 | Create IRequirementComparator interface | Interface contract clear |
| 5 | Implement NumericComparator (>=, <=, ==, !=) | Numeric comparisons pass |
| 6 | Implement VersionComparator | Semver comparison works |
| 7 | Implement StringComparator (==, contains, regex) | String matching works |
| 8 | Implement BooleanComparator | Boolean checks work |
| 9 | Implement ListComparator (anyOf, allOf) | List checks work |
| 10 | Implement CapabilityMatcher | Full matching works |
| 11 | Implement MatchReportGenerator | Human-readable reports |
| 12 | Add suggestion generation | Actionable suggestions |
| 13 | Register comparators in DI | All comparators resolved |
| 14 | Performance verify <10ms | Benchmark passes |

### Rollout Plan

1. **Phase 1**: Implement comparator infrastructure
2. **Phase 2**: Add numeric and boolean comparators
3. **Phase 3**: Add version and string comparators
4. **Phase 4**: Build CapabilityMatcher with weighted scoring
5. **Phase 5**: Add report generation and suggestions

**End of Task 032.b Specification**