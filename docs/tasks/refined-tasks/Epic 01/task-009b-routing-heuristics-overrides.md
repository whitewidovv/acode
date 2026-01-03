# Task 009.b: Routing Heuristics + Overrides

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 009, Task 009.a, Task 004 (Model Provider Interface)  

---

## Description

Task 009.b implements routing heuristics and override mechanisms for intelligent model selection. Heuristics provide automated guidance for routing decisions based on task characteristics, while overrides enable users to take explicit control when needed. This combination balances smart defaults with user agency.

Heuristics analyze task characteristics to inform routing decisions. A simple renaming task differs from a complex refactoring task. A one-file change differs from a multi-file architecture change. Heuristics estimate task complexity based on observable signals, enabling the routing policy to select appropriate models without user intervention.

Complexity estimation considers multiple factors. File count indicates scope—single-file changes are simpler than multi-file changes. Language matters—some languages have more complex semantics than others. Task type matters—new feature development differs from bug fixes. The heuristic engine combines these signals into a complexity score.

The complexity score influences model selection. In adaptive routing mode (from Task 009), low complexity tasks route to smaller, faster models. High complexity tasks route to larger, more capable models. This optimization maximizes throughput for simple tasks while preserving quality for complex ones.

Override mechanisms enable user control. Sometimes heuristics are wrong—a task that seems simple actually requires sophisticated reasoning. Users can override routing decisions at multiple levels: per-request, per-session, or in configuration. Overrides take precedence over heuristics.

Request-level overrides are immediate and temporary. The CLI provides flags like `--model` to specify a model for a single request. This enables experimentation and quick fixes without changing configuration.

Session-level overrides persist for the current session. Setting an environment variable or using a CLI command locks routing to a specific model until the session ends. This is useful when working on a series of related complex tasks.

Configuration overrides are persistent. Settings in `.agent/config.yml` define default routing behavior. These are the baseline; heuristics and runtime overrides layer on top. Configuration overrides define policy; request overrides handle exceptions.

The heuristic system is pluggable. The default heuristics cover common cases, but users can extend or replace them for specific workflows. Custom heuristics can integrate project-specific knowledge—for example, always using large models for changes in critical modules.

Heuristic confidence levels indicate certainty. A heuristic might be highly confident that a task is simple, or uncertain about a task's complexity. Low confidence triggers more conservative routing (defaulting to capable models) while high confidence enables optimization.

Observability includes logging of heuristic evaluations. Each routing decision logs which heuristics were consulted, what signals they observed, and how they influenced the decision. This transparency helps users understand and tune routing behavior.

The override system respects operating mode constraints. Even explicit model overrides cannot violate Task 001 constraints—if a user requests a cloud model in air-gapped mode, the override is rejected with a clear error.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Routing Heuristic | Rule for estimating routing factors |
| Complexity Score | Numeric estimate of task difficulty |
| Complexity Factor | Individual signal in complexity estimation |
| Override | User-specified routing decision |
| Request Override | Single-request model specification |
| Session Override | Session-wide model lock |
| Config Override | Persistent routing configuration |
| Heuristic Engine | Component that runs heuristics |
| Heuristic Plugin | Custom heuristic implementation |
| Confidence Level | Certainty of heuristic estimate |
| Signal | Observable characteristic of task |
| File Count | Number of files affected |
| Task Type | Category of task (feature, bug, refactor) |
| Adaptive Mode | Complexity-aware routing strategy |
| Precedence | Order of override application |

---

## Out of Scope

The following items are explicitly excluded from Task 009.b:

- **Role definitions** - Covered in Task 009.a
- **Fallback handling** - Covered in Task 009.c
- **Model provider logic** - Covered in Tasks 004-006
- **Machine learning for heuristics** - Not in MVP
- **Historical performance tracking** - Post-MVP
- **User preference learning** - Post-MVP
- **Cost-based optimization** - Not applicable (local)
- **Multi-model ensemble** - Post-MVP
- **Model capability detection** - Future enhancement
- **Benchmark-based routing** - Post-MVP

---

## Functional Requirements

### IRoutingHeuristic Interface

- FR-001: Interface MUST be in Application layer
- FR-002: MUST have Evaluate(RoutingContext) method
- FR-003: Evaluate MUST return HeuristicResult
- FR-004: HeuristicResult MUST include score
- FR-005: HeuristicResult MUST include confidence
- FR-006: HeuristicResult MUST include reasoning
- FR-007: Interface MUST have Name property
- FR-008: Interface MUST have Priority property

### HeuristicEngine Implementation

- FR-009: Engine MUST be in Infrastructure layer
- FR-010: Engine MUST run all registered heuristics
- FR-011: Engine MUST aggregate results
- FR-012: Engine MUST weight by confidence
- FR-013: Engine MUST return combined score
- FR-014: Engine MUST log heuristic evaluations

### Complexity Score

- FR-015: Score MUST be 0-100 range
- FR-016: 0-30 MUST be considered "low"
- FR-017: 31-70 MUST be considered "medium"
- FR-018: 71-100 MUST be considered "high"
- FR-019: Score MUST map to routing tiers
- FR-020: Tier mapping MUST be configurable

### Built-in Heuristics

- FR-021: FileCountHeuristic MUST be included
- FR-022: FileCount < 3 = low complexity
- FR-023: FileCount 3-10 = medium complexity
- FR-024: FileCount > 10 = high complexity
- FR-025: TaskTypeHeuristic MUST be included
- FR-026: Bug fixes = lower complexity baseline
- FR-027: New features = medium complexity baseline
- FR-028: Refactoring = higher complexity baseline
- FR-029: LanguageHeuristic MUST be included
- FR-030: Language complexity ratings defined

### Override Precedence

- FR-031: Request override MUST take highest precedence
- FR-032: Session override MUST override config
- FR-033: Config override MUST override heuristics
- FR-034: Heuristics MUST be lowest precedence
- FR-035: Precedence MUST be documented

### Request-Level Override

- FR-036: CLI MUST support --model flag
- FR-037: Flag value MUST be validated model ID
- FR-038: Override MUST apply to single request
- FR-039: Override MUST be logged
- FR-040: Override MUST respect mode constraints

### Session-Level Override

- FR-041: ACODE_MODEL env var MUST be supported
- FR-042: CLI MUST support `acode config set-session`
- FR-043: Session override MUST persist until exit
- FR-044: Session override MUST be clearable
- FR-045: Session override MUST be logged

### Configuration Override

- FR-046: Config section: models.override
- FR-047: override.model MUST force specific model
- FR-048: override.strategy MUST force strategy
- FR-049: override.disable_heuristics MUST skip heuristics
- FR-050: Config MUST be reloadable

### Heuristic Configuration

- FR-051: Config section: models.heuristics
- FR-052: heuristics.enabled MUST enable/disable
- FR-053: Default MUST be enabled=true
- FR-054: heuristics.weights MUST allow custom weights
- FR-055: heuristics.thresholds MUST be configurable

### Adaptive Routing Integration

- FR-056: Adaptive strategy MUST use heuristics
- FR-057: Low score MUST route to light model
- FR-058: Medium score MUST route to default model
- FR-059: High score MUST route to capable model
- FR-060: Model tiers MUST be configurable

### Logging

- FR-061: Heuristic evaluation MUST be logged
- FR-062: Log MUST include each heuristic score
- FR-063: Log MUST include combined score
- FR-064: Override application MUST be logged
- FR-065: Override source MUST be logged

### CLI Integration

- FR-066: `acode routing heuristics` MUST show state
- FR-067: MUST show enabled heuristics
- FR-068: MUST show current weights
- FR-069: `acode routing override` MUST show overrides
- FR-070: MUST show precedence chain

---

## Non-Functional Requirements

### Performance

- NFR-001: Heuristic evaluation MUST complete < 50ms
- NFR-002: All heuristics combined MUST complete < 100ms
- NFR-003: Override lookup MUST complete < 1ms
- NFR-004: Results MUST be cached per request

### Reliability

- NFR-005: Heuristic failure MUST not block routing
- NFR-006: Failed heuristic MUST be skipped
- NFR-007: Invalid override MUST fail fast
- NFR-008: Partial heuristics MUST still produce score

### Security

- NFR-009: Overrides MUST respect mode constraints
- NFR-010: Model IDs MUST be validated
- NFR-011: Config parsing MUST be safe

### Observability

- NFR-012: All heuristics MUST log results
- NFR-013: Score calculation MUST be auditable
- NFR-014: Override chain MUST be visible
- NFR-015: Metrics SHOULD track score distribution

### Maintainability

- NFR-016: Heuristics MUST be pluggable
- NFR-017: New heuristics MUST be addable
- NFR-018: All public APIs MUST have XML docs
- NFR-019: Tests MUST cover all heuristics

---

## User Manual Documentation

### Overview

Routing heuristics analyze tasks to inform model selection. Overrides enable explicit control when needed. This guide covers both mechanisms.

### Quick Start

Heuristics are enabled by default:

```yaml
# .agent/config.yml
models:
  routing:
    strategy: adaptive  # Uses heuristics
```

### Heuristics

#### How Heuristics Work

1. Task arrives for routing
2. Heuristics analyze task characteristics
3. Each heuristic produces a complexity score (0-100)
4. Scores are weighted and combined
5. Final score determines model tier

#### Built-in Heuristics

**FileCountHeuristic:**
- Files < 3: Low complexity (+10)
- Files 3-10: Medium complexity (+30)
- Files > 10: High complexity (+50)

**TaskTypeHeuristic:**
- Bug fix: Lower baseline (+10)
- Enhancement: Medium baseline (+25)
- New feature: Higher baseline (+35)
- Refactoring: Highest baseline (+45)

**LanguageHeuristic:**
- Simple languages (Markdown, JSON): +5
- Standard languages (JS, Python): +20
- Complex languages (C++, Rust): +35

#### Score Interpretation

| Score | Complexity | Recommended Model |
|-------|------------|-------------------|
| 0-30 | Low | Small/fast model |
| 31-70 | Medium | Default model |
| 71-100 | High | Large/capable model |

#### Heuristic Configuration

```yaml
models:
  heuristics:
    enabled: true
    
    weights:
      file_count: 1.0      # Default weight
      task_type: 1.2       # Slightly more important
      language: 0.8        # Slightly less important
    
    thresholds:
      low: 30              # Below this = low
      high: 70             # Above this = high
```

### Overrides

#### Precedence Order

1. **Request override** (highest) - `--model` flag
2. **Session override** - Environment variable
3. **Config override** - Configuration file
4. **Heuristics** (lowest) - Automatic calculation

#### Request-Level Override

Use for single requests:

```bash
# Force specific model for this request
acode run --model llama3.2:70b

# Useful for complex one-off tasks
acode analyze --model llama3.2:70b "Review architecture"
```

#### Session-Level Override

Use for a series of related tasks:

```bash
# Set for entire session
export ACODE_MODEL=llama3.2:70b
acode run  # Uses llama3.2:70b

# Or via CLI
acode config set-session --model llama3.2:70b
acode run  # Uses llama3.2:70b

# Clear session override
acode config clear-session
```

#### Configuration Override

Use for persistent preferences:

```yaml
# .agent/config.yml
models:
  override:
    # Force specific model always
    model: llama3.2:70b
    
    # Or disable heuristics
    disable_heuristics: true
```

### Adaptive Routing

Adaptive routing uses heuristics to select models:

```yaml
models:
  routing:
    strategy: adaptive
    
    # Model tiers
    tiers:
      low: llama3.2:7b      # Fast, for simple tasks
      medium: llama3.2:7b   # Default
      high: llama3.2:70b    # Capable, for complex tasks
```

### CLI Commands

```bash
# Show heuristic state
$ acode routing heuristics
Heuristics: enabled

Registered Heuristics:
  FileCountHeuristic (priority: 1)
  TaskTypeHeuristic (priority: 2)
  LanguageHeuristic (priority: 3)

Current Weights:
  file_count: 1.0
  task_type: 1.2
  language: 0.8

# Show override state
$ acode routing override
Active Overrides:

  Request: (none)
  Session: llama3.2:70b (via ACODE_MODEL)
  Config: (none)

Effective Model: llama3.2:70b (from session)

# Test heuristics on a task
$ acode routing evaluate "Add input validation"
Evaluating task: "Add input validation"

Heuristic Results:
  FileCountHeuristic: 25 (3 files, confidence: 0.8)
  TaskTypeHeuristic: 35 (new feature, confidence: 0.9)
  LanguageHeuristic: 20 (TypeScript, confidence: 1.0)

Combined Score: 27 (Low complexity)
Recommended Tier: low
Recommended Model: llama3.2:7b
```

### Best Practices

1. **Start with heuristics** - Let automatic routing work first
2. **Override for complex tasks** - Use `--model` when needed
3. **Session override for focus work** - Lock model for related tasks
4. **Tune weights gradually** - Adjust based on experience
5. **Check logs** - Review routing decisions to understand behavior

### Troubleshooting

#### Heuristics Choosing Wrong Model

```
Task seems complex but got small model
```

**Cause:** Heuristics underestimate complexity.  
**Solution:** Use `--model` override, or adjust weights:

```yaml
models:
  heuristics:
    weights:
      task_type: 1.5  # Increase weight
```

#### Override Not Working

```
Specified --model but different model used
```

**Cause:** Mode constraint violation or invalid model ID.  
**Check:** Verify model ID and operating mode.

#### Heuristics Disabled

```
Score always same regardless of task
```

**Cause:** Heuristics disabled or overridden.  
**Check:** `acode routing heuristics` output.

---

## Acceptance Criteria

### Interface

- [ ] AC-001: IRoutingHeuristic in Application
- [ ] AC-002: Evaluate method exists
- [ ] AC-003: Returns HeuristicResult
- [ ] AC-004: Result has score
- [ ] AC-005: Result has confidence
- [ ] AC-006: Result has reasoning
- [ ] AC-007: Name property exists
- [ ] AC-008: Priority property exists

### Engine

- [ ] AC-009: HeuristicEngine in Infrastructure
- [ ] AC-010: Runs all heuristics
- [ ] AC-011: Aggregates results
- [ ] AC-012: Weights by confidence
- [ ] AC-013: Returns combined score
- [ ] AC-014: Logs evaluations

### Score

- [ ] AC-015: Range 0-100
- [ ] AC-016: 0-30 = low
- [ ] AC-017: 31-70 = medium
- [ ] AC-018: 71-100 = high
- [ ] AC-019: Maps to tiers
- [ ] AC-020: Tiers configurable

### Built-in Heuristics

- [ ] AC-021: FileCountHeuristic works
- [ ] AC-022: TaskTypeHeuristic works
- [ ] AC-023: LanguageHeuristic works
- [ ] AC-024: All return valid scores
- [ ] AC-025: All include reasoning

### Precedence

- [ ] AC-026: Request override highest
- [ ] AC-027: Session overrides config
- [ ] AC-028: Config overrides heuristics
- [ ] AC-029: Heuristics lowest
- [ ] AC-030: Documented

### Request Override

- [ ] AC-031: --model flag works
- [ ] AC-032: Validates model ID
- [ ] AC-033: Single request only
- [ ] AC-034: Logged
- [ ] AC-035: Respects constraints

### Session Override

- [ ] AC-036: ACODE_MODEL works
- [ ] AC-037: set-session works
- [ ] AC-038: Persists in session
- [ ] AC-039: Clearable
- [ ] AC-040: Logged

### Config Override

- [ ] AC-041: models.override section
- [ ] AC-042: override.model works
- [ ] AC-043: disable_heuristics works
- [ ] AC-044: Reloadable

### CLI

- [ ] AC-045: heuristics command works
- [ ] AC-046: override command works
- [ ] AC-047: evaluate command works
- [ ] AC-048: Shows all details

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Application/Heuristics/
├── FileCountHeuristicTests.cs
│   ├── Should_Return_Low_For_Few_Files()
│   ├── Should_Return_Medium_For_Some_Files()
│   └── Should_Return_High_For_Many_Files()
│
├── TaskTypeHeuristicTests.cs
│   ├── Should_Score_Bug_Fix_Lower()
│   └── Should_Score_Refactor_Higher()
│
├── HeuristicEngineTests.cs
│   ├── Should_Run_All_Heuristics()
│   ├── Should_Weight_By_Confidence()
│   └── Should_Handle_Failed_Heuristic()
│
└── OverrideTests.cs
    ├── Should_Apply_Request_Override()
    ├── Should_Apply_Session_Override()
    └── Should_Respect_Precedence()
```

### Integration Tests

```
Tests/Integration/Heuristics/
├── HeuristicIntegrationTests.cs
│   ├── Should_Evaluate_Real_Task()
│   └── Should_Route_Based_On_Score()
```

### E2E Tests

```
Tests/E2E/Heuristics/
├── AdaptiveRoutingE2ETests.cs
│   ├── Should_Use_Small_Model_For_Simple_Task()
│   └── Should_Use_Large_Model_For_Complex_Task()
```

### Performance Tests

- PERF-001: Heuristic evaluation < 50ms
- PERF-002: All heuristics < 100ms
- PERF-003: Override lookup < 1ms

---

## User Verification Steps

### Scenario 1: View Heuristics

1. Run `acode routing heuristics`
2. Verify: Lists all heuristics
3. Verify: Shows weights

### Scenario 2: Evaluate Task

1. Run `acode routing evaluate "Fix typo"`
2. Verify: Shows low complexity
3. Verify: Recommends fast model

### Scenario 3: Request Override

1. Run `acode run --model llama3.2:70b`
2. Verify: Uses specified model
3. Verify: Override logged

### Scenario 4: Session Override

1. Export ACODE_MODEL=llama3.2:70b
2. Run `acode run`
3. Verify: Uses specified model

### Scenario 5: Override Precedence

1. Set config override
2. Set session override
3. Use --model flag
4. Verify: --model wins

### Scenario 6: Disable Heuristics

1. Set disable_heuristics: true
2. Run routing
3. Verify: Uses default model

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/Heuristics/
├── IRoutingHeuristic.cs
├── HeuristicResult.cs
├── HeuristicContext.cs
└── RoutingOverride.cs

src/AgenticCoder.Infrastructure/Heuristics/
├── HeuristicEngine.cs
├── FileCountHeuristic.cs
├── TaskTypeHeuristic.cs
├── LanguageHeuristic.cs
├── OverrideResolver.cs
└── HeuristicConfiguration.cs
```

### IRoutingHeuristic Interface

```csharp
namespace AgenticCoder.Application.Heuristics;

public interface IRoutingHeuristic
{
    string Name { get; }
    int Priority { get; }
    HeuristicResult Evaluate(HeuristicContext context);
}

public sealed class HeuristicResult
{
    public required int Score { get; init; }  // 0-100
    public required double Confidence { get; init; }  // 0.0-1.0
    public required string Reasoning { get; init; }
}
```

### HeuristicEngine

```csharp
namespace AgenticCoder.Infrastructure.Heuristics;

public sealed class HeuristicEngine
{
    private readonly IEnumerable<IRoutingHeuristic> _heuristics;
    
    public ComplexityScore Evaluate(HeuristicContext context)
    {
        var results = _heuristics
            .OrderBy(h => h.Priority)
            .Select(h => (h.Name, h.Evaluate(context)))
            .ToList();
        
        // Weight by confidence and combine
        var weightedSum = results.Sum(r => 
            r.Item2.Score * r.Item2.Confidence);
        var totalWeight = results.Sum(r => r.Item2.Confidence);
        
        return new ComplexityScore(
            (int)(weightedSum / totalWeight),
            results);
    }
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-HEU-001 | Invalid model in override |
| ACODE-HEU-002 | Override violates mode constraint |
| ACODE-HEU-003 | Heuristic evaluation failed |
| ACODE-HEU-004 | Invalid heuristic configuration |

### Logging Fields

```json
{
  "event": "heuristic_evaluation",
  "task": "Add validation",
  "heuristics": [
    {"name": "FileCount", "score": 25, "confidence": 0.8},
    {"name": "TaskType", "score": 35, "confidence": 0.9}
  ],
  "combined_score": 29,
  "complexity": "low",
  "override_active": false
}
```

### Implementation Checklist

1. [ ] Create IRoutingHeuristic interface
2. [ ] Create HeuristicResult class
3. [ ] Create HeuristicContext class
4. [ ] Implement FileCountHeuristic
5. [ ] Implement TaskTypeHeuristic
6. [ ] Implement LanguageHeuristic
7. [ ] Implement HeuristicEngine
8. [ ] Implement OverrideResolver
9. [ ] Add CLI commands
10. [ ] Add configuration schema
11. [ ] Write unit tests
12. [ ] Write integration tests
13. [ ] Add XML documentation

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~Heuristics"
```

---

**End of Task 009.b Specification**