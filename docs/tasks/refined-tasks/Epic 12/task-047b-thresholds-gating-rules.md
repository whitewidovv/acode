# Task 047.b: Thresholds + Gating Rules

**Priority:** P0 – Critical  
**Tier:** F – Foundation Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 12 – Hardening  
**Dependencies:** Task 047 (Scoring), Task 047.a (Metrics)  

---

## Description

Task 047.b defines thresholds and gating rules—the configurable boundaries that determine pass/fail decisions. Numbers are meaningless without context; thresholds provide that context. An 80% pass rate might be excellent for experimental features but unacceptable for core functionality.

Thresholds are configurable per-metric, per-category, and per-criticality level. Gating rules combine multiple thresholds using logical operations (AND, OR) to produce final verdicts. The rules engine is the decision-making core of the promotion gate system.

### Business Value

Thresholds and rules provide:
- Configurable quality standards
- Context-aware decisions
- Graduated enforcement
- Policy expression
- Consistent enforcement

### Scope Boundaries

This task covers threshold configuration and gating rules. Metrics are Task 047.a. Historical comparison is Task 047.c. Override is Task 047.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Metrics | Task 047.a | Input values | Source |
| Scoring | Task 047 | Rule application | Consumer |
| Config | File | Threshold source | Configuration |
| CLI | Task 046.b | Override | Interface |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Invalid threshold | Validation | Default | May be too lenient |
| Missing config | Check | Defaults | Unknown standard |
| Rule conflict | Validation | Error | Cannot evaluate |
| Threshold too low | Policy check | Warn | Quality risk |

### Assumptions

1. **Config format**: YAML or JSON
2. **Metrics available**: From Task 047.a
3. **Rules deterministic**: Same input = same output
4. **Defaults exist**: For all thresholds
5. **Override possible**: CLI or env

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Threshold | Pass/fail boundary |
| Rule | Decision logic |
| Gate | Quality checkpoint |
| Condition | Single check |
| Operator | AND/OR logic |
| Default | Fallback value |
| Override | Runtime change |
| Policy | Set of rules |
| Graduated | Increasing strictness |
| Precedence | Priority order |

---

## Out of Scope

- Metric calculation (Task 047.a)
- Historical comparison (Task 047.c)
- Override workflow (Task 047)
- Dynamic thresholds
- ML-based thresholds
- Threshold recommendations

---

## Functional Requirements

### FR-001 to FR-025: Threshold Definition

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-047b-01 | Thresholds MUST be configurable | P0 |
| FR-047b-02 | Config file MUST be supported | P0 |
| FR-047b-03 | Config format: YAML | P0 |
| FR-047b-04 | Config format: JSON MAY work | P1 |
| FR-047b-05 | Pass rate threshold MUST exist | P0 |
| FR-047b-06 | Default pass rate = 80% | P0 |
| FR-047b-07 | Runtime threshold MAY exist | P1 |
| FR-047b-08 | Iteration threshold MAY exist | P1 |
| FR-047b-09 | Regression threshold MUST exist | P0 |
| FR-047b-10 | Default regression = 5% | P0 |
| FR-047b-11 | Per-category thresholds MUST work | P0 |
| FR-047b-12 | Per-difficulty thresholds MAY work | P2 |
| FR-047b-13 | Critical task threshold MUST exist | P0 |
| FR-047b-14 | Critical default = 100% | P0 |
| FR-047b-15 | Warn threshold MAY exist | P1 |
| FR-047b-16 | Default warn = 90% | P1 |
| FR-047b-17 | Threshold validation MUST occur | P0 |
| FR-047b-18 | Invalid MUST error | P0 |
| FR-047b-19 | Out of range MUST error | P0 |
| FR-047b-20 | Threshold range: 0-100 for % | P0 |
| FR-047b-21 | Threshold range: 0-1 for score | P0 |
| FR-047b-22 | Environment override MUST work | P0 |
| FR-047b-23 | CLI override MUST work | P0 |
| FR-047b-24 | Precedence: CLI > env > file | P0 |
| FR-047b-25 | Defaults MUST be documented | P0 |

### FR-026 to FR-050: Gating Rules

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-047b-26 | Rules MUST be definable | P0 |
| FR-047b-27 | Rule MUST have conditions | P0 |
| FR-047b-28 | Condition: metric >= threshold | P0 |
| FR-047b-29 | Condition: metric < threshold | P0 |
| FR-047b-30 | Condition: metric == value | P1 |
| FR-047b-31 | AND operator MUST work | P0 |
| FR-047b-32 | OR operator MUST work | P0 |
| FR-047b-33 | NOT operator MAY work | P2 |
| FR-047b-34 | Nested rules MUST work | P1 |
| FR-047b-35 | Default rule: AND all | P0 |
| FR-047b-36 | Rule evaluation order MUST define | P0 |
| FR-047b-37 | Short-circuit MAY optimize | P2 |
| FR-047b-38 | Rule result: pass/fail | P0 |
| FR-047b-39 | Rule result: warn optional | P1 |
| FR-047b-40 | Multiple rules MUST combine | P0 |
| FR-047b-41 | All rules pass = overall pass | P0 |
| FR-047b-42 | Any rule fail = overall fail | P0 |
| FR-047b-43 | Rule naming MUST exist | P0 |
| FR-047b-44 | Rule description MUST exist | P0 |
| FR-047b-45 | Failed rule MUST report | P0 |
| FR-047b-46 | Reason MUST be captured | P0 |
| FR-047b-47 | Rule priority MAY exist | P2 |
| FR-047b-48 | Disabled rules MUST be skipped | P1 |
| FR-047b-49 | Rule versioning MAY exist | P2 |
| FR-047b-50 | Rule logging MUST occur | P0 |

### FR-051 to FR-065: Graduated Thresholds

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-047b-51 | Stage thresholds MUST work | P0 |
| FR-047b-52 | Development stage = lenient | P0 |
| FR-047b-53 | Staging stage = moderate | P0 |
| FR-047b-54 | Production stage = strict | P0 |
| FR-047b-55 | Stage from config MUST work | P0 |
| FR-047b-56 | Stage from env MUST work | P0 |
| FR-047b-57 | Stage from CLI MUST work | P0 |
| FR-047b-58 | Default stage: development | P0 |
| FR-047b-59 | Stage names MUST be configurable | P1 |
| FR-047b-60 | Per-stage thresholds MUST work | P0 |
| FR-047b-61 | Stage inheritance MAY work | P2 |
| FR-047b-62 | Stage validation MUST occur | P0 |
| FR-047b-63 | Unknown stage MUST error | P0 |
| FR-047b-64 | Stage MUST be logged | P0 |
| FR-047b-65 | Stage MUST be in results | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-047b-01 | Config load | <50ms | P0 |
| NFR-047b-02 | Threshold lookup | <1ms | P0 |
| NFR-047b-03 | Rule evaluation | <10ms | P0 |
| NFR-047b-04 | Complex rules | <50ms | P0 |
| NFR-047b-05 | Validation | <20ms | P0 |
| NFR-047b-06 | Override apply | <5ms | P0 |
| NFR-047b-07 | Memory usage | <5MB | P0 |
| NFR-047b-08 | Config parsing | <30ms | P0 |
| NFR-047b-09 | Stage switch | <10ms | P0 |
| NFR-047b-10 | Logging overhead | <5ms | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-047b-11 | Rule determinism | 100% | P0 |
| NFR-047b-12 | Config validation | 100% | P0 |
| NFR-047b-13 | Default coverage | 100% | P0 |
| NFR-047b-14 | Error messages | Clear | P0 |
| NFR-047b-15 | Cross-platform | All OS | P0 |
| NFR-047b-16 | Config reload | Safe | P1 |
| NFR-047b-17 | Partial config | Merged | P0 |
| NFR-047b-18 | Invalid field | Rejected | P0 |
| NFR-047b-19 | Override validation | 100% | P0 |
| NFR-047b-20 | Precedence consistency | 100% | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-047b-21 | Config load | Info | P0 |
| NFR-047b-22 | Threshold values | Debug | P0 |
| NFR-047b-23 | Rule evaluation | Debug | P0 |
| NFR-047b-24 | Override applied | Warning | P0 |
| NFR-047b-25 | Failures logged | Error | P0 |
| NFR-047b-26 | Stage logged | Info | P0 |
| NFR-047b-27 | Structured logging | JSON | P0 |
| NFR-047b-28 | Metrics: checks | Counter | P1 |
| NFR-047b-29 | Metrics: failures | Counter | P0 |
| NFR-047b-30 | Config source | Logged | P0 |

---

## Acceptance Criteria / Definition of Done

### Thresholds
- [ ] AC-001: Config file works
- [ ] AC-002: YAML format
- [ ] AC-003: Pass rate threshold
- [ ] AC-004: Default 80%
- [ ] AC-005: Per-category
- [ ] AC-006: Critical threshold
- [ ] AC-007: Validation
- [ ] AC-008: Override works

### Rules
- [ ] AC-009: Rules definable
- [ ] AC-010: Conditions work
- [ ] AC-011: AND operator
- [ ] AC-012: OR operator
- [ ] AC-013: Nested rules
- [ ] AC-014: Result capture
- [ ] AC-015: Reason capture
- [ ] AC-016: Logging

### Stages
- [ ] AC-017: Stage config
- [ ] AC-018: Per-stage thresholds
- [ ] AC-019: Stage from env
- [ ] AC-020: Stage from CLI
- [ ] AC-021: Default stage
- [ ] AC-022: Validation
- [ ] AC-023: Logged
- [ ] AC-024: In results

### Quality
- [ ] AC-025: Deterministic
- [ ] AC-026: Defaults documented
- [ ] AC-027: Precedence works
- [ ] AC-028: Cross-platform
- [ ] AC-029: Tests pass
- [ ] AC-030: Documented
- [ ] AC-031: Reviewed
- [ ] AC-032: Examples

---

## User Verification Scenarios

### Scenario 1: Configure Threshold
**Persona:** Tech Lead  
**Preconditions:** Project setup  
**Steps:**
1. Create gates.yml
2. Set pass rate = 85%
3. Run gate
4. Threshold applied

**Verification Checklist:**
- [ ] Config loaded
- [ ] Threshold 85%
- [ ] Applied correctly
- [ ] Logged

### Scenario 2: Override via CLI
**Persona:** Developer  
**Preconditions:** Config exists  
**Steps:**
1. Run with --threshold 70%
2. CLI overrides config
3. New threshold used
4. Original preserved

**Verification Checklist:**
- [ ] Override works
- [ ] CLI precedence
- [ ] Threshold changed
- [ ] Logged

### Scenario 3: Category Thresholds
**Persona:** Tech Lead  
**Preconditions:** Categories defined  
**Steps:**
1. Set file-ops = 95%
2. Set code-gen = 80%
3. Run gate
4. Per-category applied

**Verification Checklist:**
- [ ] Categories work
- [ ] Different thresholds
- [ ] Correct evaluation
- [ ] Breakdown shown

### Scenario 4: Production Stage
**Persona:** Release Manager  
**Preconditions:** Staged thresholds  
**Steps:**
1. Set stage = production
2. Stricter thresholds
3. Run gate
4. Production rules applied

**Verification Checklist:**
- [ ] Stage detected
- [ ] Stricter applied
- [ ] Correct thresholds
- [ ] Logged

### Scenario 5: Complex Rule
**Persona:** Tech Lead  
**Preconditions:** Custom rules needed  
**Steps:**
1. Define AND rule
2. Multiple conditions
3. Run gate
4. All evaluated

**Verification Checklist:**
- [ ] Rule defined
- [ ] Conditions work
- [ ] AND logic
- [ ] Result correct

### Scenario 6: Validation Error
**Persona:** Developer  
**Preconditions:** Invalid config  
**Steps:**
1. Set threshold = 150%
2. Load config
3. Validation error
4. Clear message

**Verification Checklist:**
- [ ] Error detected
- [ ] Message clear
- [ ] Location shown
- [ ] Fix suggested

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-047b-01 | Config loading | FR-047b-02 |
| UT-047b-02 | YAML parsing | FR-047b-03 |
| UT-047b-03 | Threshold lookup | FR-047b-05 |
| UT-047b-04 | Default values | FR-047b-06 |
| UT-047b-05 | Validation | FR-047b-17 |
| UT-047b-06 | Range check | FR-047b-19 |
| UT-047b-07 | Override apply | FR-047b-22 |
| UT-047b-08 | Precedence | FR-047b-24 |
| UT-047b-09 | Rule evaluation | FR-047b-26 |
| UT-047b-10 | AND operator | FR-047b-31 |
| UT-047b-11 | OR operator | FR-047b-32 |
| UT-047b-12 | Nested rules | FR-047b-34 |
| UT-047b-13 | Stage selection | FR-047b-51 |
| UT-047b-14 | Per-category | FR-047b-11 |
| UT-047b-15 | Critical threshold | FR-047b-13 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-047b-01 | Full config E2E | E2E |
| IT-047b-02 | Metrics integration | Task 047.a |
| IT-047b-03 | Scoring integration | Task 047 |
| IT-047b-04 | CLI override | FR-047b-23 |
| IT-047b-05 | Env override | FR-047b-22 |
| IT-047b-06 | Stage switching | FR-047b-55 |
| IT-047b-07 | Cross-platform | NFR-047b-15 |
| IT-047b-08 | Logging | NFR-047b-21 |
| IT-047b-09 | Complex rules | FR-047b-40 |
| IT-047b-10 | Failed rule report | FR-047b-45 |
| IT-047b-11 | Config reload | NFR-047b-16 |
| IT-047b-12 | Partial config | NFR-047b-17 |
| IT-047b-13 | Validation errors | FR-047b-18 |
| IT-047b-14 | Stage in results | FR-047b-65 |
| IT-047b-15 | Rule logging | FR-047b-50 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Gates/
│       ├── Threshold.cs
│       ├── GatingRule.cs
│       ├── RuleCondition.cs
│       ├── LogicalOperator.cs
│       └── Stage.cs
├── Acode.Application/
│   └── Gates/
│       ├── IThresholdProvider.cs
│       ├── IRuleEngine.cs
│       └── GateConfiguration.cs
├── Acode.Infrastructure/
│   └── Gates/
│       ├── YamlThresholdProvider.cs
│       ├── RuleEngine.cs
│       ├── ThresholdValidator.cs
│       └── StageResolver.cs
```

### Configuration Format

```yaml
# .agent/gates.yml
version: "1.0"

thresholds:
  default:
    passRate: 80
    criticalPassRate: 100
    regression: 5
    warn: 90
    
  byCategory:
    file-ops:
      passRate: 95
    code-gen:
      passRate: 80
    debug:
      passRate: 75

stages:
  development:
    passRate: 70
    regression: 10
  staging:
    passRate: 80
    regression: 5
  production:
    passRate: 90
    regression: 3

rules:
  - name: "Core Quality Gate"
    description: "Must pass all core quality checks"
    conditions:
      operator: AND
      items:
        - metric: passRate
          operator: ">="
          threshold: passRate
        - metric: criticalPassRate
          operator: "=="
          value: 100
        - metric: regression
          operator: "<="
          threshold: regression
```

### CLI Override

```bash
# Override via CLI
acode bench run --threshold passRate=75

# Override via environment
ACODE_THRESHOLD_PASSRATE=75 acode bench run

# Set stage
acode bench run --stage production
ACODE_STAGE=production acode bench run
```

**End of Task 047.b Specification**
