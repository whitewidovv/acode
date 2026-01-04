# Task 037.b: Repo Overrides

**Priority:** P0 – Critical  
**Tier:** L – Feature Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 9 – Safety & Compliance  
**Dependencies:** Task 037, Task 037.a  

---

## Description

Task 037.b implements repository-level policy overrides. Policies defined in `.agent/config.yml` within a repository can extend or override global policies. This enables project-specific customization while maintaining organizational baselines.

Repository overrides allow development teams to tailor agent behavior for their specific project needs. A project that requires network access can enable it. A security-critical project can add stricter restrictions. All overrides are validated and constrained by global policies marked as non-overridable.

The override system supports both additive (add new policies) and replacement (change existing policy effects) modes. Clear precedence rules ensure predictable behavior: repo > global.

### Business Value

Repo overrides provide:
- Project-specific policy customization
- Team autonomy within organizational bounds
- Flexibility for diverse project needs
- Self-documenting security configuration

### Scope Boundaries

This task covers repo-level policy loading and merging. Global policies are 037.a. Per-task overrides are 037.c. Core engine is 037.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Config Parser | Task 002 | Policy section | From config.yml |
| Global Policies | Task 037.a | Base layer | Lower precedence |
| Policy Engine | Task 037 | Merged result | Evaluation |
| Schema Validator | `ISchemaValidator` | Validate | Before merge |
| Merge Engine | `IPolicyMerger` | Combine | With globals |
| Git Detection | `IGitRepoDetector` | Find config | In repo root |
| CLI | `PolicyCommands` | List repo | User query |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Config parse error | YAML error | Fall back to globals | Warning shown |
| Invalid policy section | Schema check | Skip repo policies | Clear message |
| Policy not found in config | Missing section | Use globals only | Normal |
| Non-overridable violated | Constraint check | Error | Must change config |
| Circular reference | Graph check | Error | Fix config |
| Precedence conflict | Merge conflict | Document order | Log warning |
| Performance issue | Timer | Log warning | Slower load |
| Hot reload fails | Exception | Keep old | Warn user |

### Assumptions

1. **Config file exists**: `.agent/config.yml` in repo root
2. **Policy section optional**: No section = use globals only
3. **Schema validated**: Same schema as globals
4. **Precedence: repo > global**: Repo wins on conflict
5. **Non-overridable respected**: Some globals cannot be changed
6. **Performance**: Load < 100ms
7. **Cache after load**: Avoid repeated parsing
8. **Hot reload supported**: Changes take effect without restart

### Security Considerations

1. **Non-overridable enforced**: Critical policies cannot be relaxed
2. **Cannot escalate**: Repo cannot grant more than global allows
3. **Audit override**: Log what was overridden
4. **Clear attribution**: Log policy source
5. **No code execution**: Pure data
6. **Validation required**: All policies validated
7. **Rollback on error**: Invalid = use globals
8. **Document constraints**: Clear what can be overridden

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Repo Override | Policy in .agent/config.yml |
| Config Section | policies: block in YAML |
| Non-Overridable | Global policy that cannot be changed |
| Additive | Adding new policies |
| Replacement | Changing existing policy effect |
| Precedence | Repo > Global |
| Constraint | Limit on what can be overridden |
| Merge | Combining repo and global |

---

## Out of Scope

- Remote policy distribution
- Multi-repo policy sharing
- Policy inheritance chains
- Branch-specific policies
- PR-specific policies
- Policy rollback UI

---

## Functional Requirements

### FR-001 to FR-015: Config Loading

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-037B-01 | `.agent/config.yml` MUST be checked for policies | P0 |
| FR-037B-02 | Policy section MUST be optional | P0 |
| FR-037B-03 | Missing section MUST use globals only | P0 |
| FR-037B-04 | Policy section MUST use `policies:` key | P0 |
| FR-037B-05 | Policies MUST be validated against schema | P0 |
| FR-037B-06 | Invalid policies MUST warn and skip | P0 |
| FR-037B-07 | Valid policies MUST be merged with globals | P0 |
| FR-037B-08 | Config location MUST be configurable | P2 |
| FR-037B-09 | Environment variable override MUST work | P2 |
| FR-037B-10 | Hot reload MUST be supported | P1 |
| FR-037B-11 | Hot reload MUST be atomic | P1 |
| FR-037B-12 | File watch MUST detect changes | P1 |
| FR-037B-13 | Cache MUST be invalidated on change | P1 |
| FR-037B-14 | Multiple repos MUST be independent | P0 |
| FR-037B-15 | Repo root MUST be auto-detected | P0 |

### FR-016 to FR-030: Policy Override

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-037B-16 | Repo policies MUST override globals | P0 |
| FR-037B-17 | Same-name policy MUST use repo version | P0 |
| FR-037B-18 | Different-name policies MUST coexist | P0 |
| FR-037B-19 | Override MUST be logged | P0 |
| FR-037B-20 | Override reason MUST be in log | P1 |
| FR-037B-21 | Additive mode MUST add new policies | P0 |
| FR-037B-22 | Replacement mode MUST change effect | P0 |
| FR-037B-23 | Partial override MUST work | P1 |
| FR-037B-24 | Partial: only change specified fields | P1 |
| FR-037B-25 | Priority field MUST work in override | P1 |
| FR-037B-26 | Override MUST respect conditions | P1 |
| FR-037B-27 | Override MUST respect actions list | P0 |
| FR-037B-28 | Override MUST support wildcards | P1 |
| FR-037B-29 | Override MUST emit events | P1 |
| FR-037B-30 | Override MUST be deterministic | P0 |

### FR-031 to FR-045: Non-Overridable Constraints

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-037B-31 | Non-overridable MUST be respected | P0 |
| FR-037B-32 | Global policies MUST mark overridable | P0 |
| FR-037B-33 | Default MUST be overridable: true | P0 |
| FR-037B-34 | Critical policies MUST be non-overridable | P0 |
| FR-037B-35 | Non-overridable attempt MUST error | P0 |
| FR-037B-36 | Error MUST name the policy | P0 |
| FR-037B-37 | Error MUST suggest resolution | P1 |
| FR-037B-38 | Non-overridable list MUST be documented | P0 |
| FR-037B-39 | Escalation MUST be prevented | P0 |
| FR-037B-40 | Cannot grant more than global | P0 |
| FR-037B-41 | Escalation attempt MUST error | P0 |
| FR-037B-42 | Error MUST explain constraint | P0 |
| FR-037B-43 | Constraint check MUST be fast | P1 |
| FR-037B-44 | Constraints MUST be cached | P1 |
| FR-037B-45 | Constraints MUST be queryable | P2 |

### FR-046 to FR-060: CLI Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-037B-46 | `acode policy list --level repo` MUST work | P0 |
| FR-037B-47 | List MUST show override status | P0 |
| FR-037B-48 | List MUST show source file | P0 |
| FR-037B-49 | `acode policy show` MUST show effective | P0 |
| FR-037B-50 | Show MUST indicate if overridden | P0 |
| FR-037B-51 | Show MUST show original and override | P1 |
| FR-037B-52 | `acode policy diff` MUST show changes | P1 |
| FR-037B-53 | Diff MUST compare repo vs global | P1 |
| FR-037B-54 | `acode policy validate` MUST check repo | P0 |
| FR-037B-55 | Validate MUST check constraints | P0 |
| FR-037B-56 | Exit code 0 on success | P0 |
| FR-037B-57 | Exit code 1 on failure | P0 |
| FR-037B-58 | `--json` output MUST work | P1 |
| FR-037B-59 | Help text MUST be complete | P1 |
| FR-037B-60 | Error messages MUST be clear | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-037B-01 | Config load time | <100ms | P1 |
| NFR-037B-02 | Merge time | <50ms | P1 |
| NFR-037B-03 | Constraint check | <10ms | P1 |
| NFR-037B-04 | Total repo load | <200ms | P0 |
| NFR-037B-05 | Schema validation | <50ms | P1 |
| NFR-037B-06 | Hot reload time | <100ms | P2 |
| NFR-037B-07 | Memory overhead | <5MB | P2 |
| NFR-037B-08 | File watch overhead | Minimal | P2 |
| NFR-037B-09 | CLI response | <200ms | P1 |
| NFR-037B-10 | Concurrent repos | Supported | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-037B-11 | Fallback on error | 100% | P0 |
| NFR-037B-12 | Constraint enforcement | 100% | P0 |
| NFR-037B-13 | Merge correctness | 100% | P0 |
| NFR-037B-14 | Schema validation | 100% | P0 |
| NFR-037B-15 | Deterministic merge | 100% | P0 |
| NFR-037B-16 | No escalation | 100% | P0 |
| NFR-037B-17 | Hot reload atomicity | 100% | P1 |
| NFR-037B-18 | Graceful degradation | Always | P1 |
| NFR-037B-19 | Cross-platform paths | All OS | P0 |
| NFR-037B-20 | Thread safety | No races | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-037B-21 | Repo load logged | Info level | P0 |
| NFR-037B-22 | Override logged | Info level | P0 |
| NFR-037B-23 | Constraint violation logged | Warning | P0 |
| NFR-037B-24 | Merge logged | Debug level | P1 |
| NFR-037B-25 | Metrics: overrides | Counter | P2 |
| NFR-037B-26 | Metrics: constraint violations | Counter | P1 |
| NFR-037B-27 | Events: repo loaded | Published | P1 |
| NFR-037B-28 | Events: override applied | Published | P1 |
| NFR-037B-29 | Structured logging | JSON | P0 |
| NFR-037B-30 | Source in logs | Required | P0 |

---

## Acceptance Criteria / Definition of Done

### Loading
- [ ] AC-001: Config.yml checked for policies
- [ ] AC-002: Policy section optional
- [ ] AC-003: Missing = globals only
- [ ] AC-004: Schema validated
- [ ] AC-005: Invalid warns and skips
- [ ] AC-006: Valid merged with globals
- [ ] AC-007: Hot reload works
- [ ] AC-008: Cache invalidated

### Override
- [ ] AC-009: Repo overrides globals
- [ ] AC-010: Same-name uses repo
- [ ] AC-011: Different-name coexist
- [ ] AC-012: Override logged
- [ ] AC-013: Additive works
- [ ] AC-014: Replacement works
- [ ] AC-015: Deterministic
- [ ] AC-016: Events emitted

### Constraints
- [ ] AC-017: Non-overridable respected
- [ ] AC-018: Attempt errors
- [ ] AC-019: Error names policy
- [ ] AC-020: Escalation prevented
- [ ] AC-021: Cannot grant more
- [ ] AC-022: Constraints documented

### CLI
- [ ] AC-023: List repo level works
- [ ] AC-024: Override status shown
- [ ] AC-025: Source file shown
- [ ] AC-026: Show effective works
- [ ] AC-027: Diff works
- [ ] AC-028: Validate checks constraints
- [ ] AC-029: Exit codes correct
- [ ] AC-030: JSON output works

---

## User Verification Scenarios

### Scenario 1: Add Repo Policy
**Persona:** Developer adding project policy  
**Preconditions:** Globals loaded  
**Steps:**
1. Add policies section to .agent/config.yml
2. Define new policy
3. Reload policies
4. New policy active

**Verification Checklist:**
- [ ] Section parsed
- [ ] Policy loaded
- [ ] Merged with globals
- [ ] Active in evaluation

### Scenario 2: Override Global Policy
**Persona:** Developer relaxing restriction  
**Preconditions:** Global policy restrictive  
**Steps:**
1. Define policy with same name
2. Set different effect
3. Reload policies
4. Repo version active

**Verification Checklist:**
- [ ] Override detected
- [ ] Repo wins
- [ ] Logged
- [ ] Works correctly

### Scenario 3: Non-Overridable Blocked
**Persona:** Developer trying to override critical  
**Preconditions:** Policy marked non-overridable  
**Steps:**
1. Try to override in repo config
2. Validation error
3. Policy named in error
4. Resolution suggested

**Verification Checklist:**
- [ ] Error shown
- [ ] Policy identified
- [ ] Constraint explained
- [ ] Globals still active

### Scenario 4: Escalation Prevented
**Persona:** Developer trying to grant more access  
**Preconditions:** Global restricts network  
**Steps:**
1. Try to allow network in repo
2. Error: cannot escalate
3. Constraint explained
4. Original policy active

**Verification Checklist:**
- [ ] Escalation blocked
- [ ] Error clear
- [ ] Original enforced
- [ ] Security maintained

### Scenario 5: View Policy Diff
**Persona:** Developer checking changes  
**Preconditions:** Overrides exist  
**Steps:**
1. Run `acode policy diff`
2. See globals vs repo
3. Changes highlighted
4. Clear output

**Verification Checklist:**
- [ ] Diff shows
- [ ] Changes clear
- [ ] Source indicated
- [ ] Format readable

### Scenario 6: Validate Repo Config
**Persona:** Developer before commit  
**Preconditions:** Repo policies defined  
**Steps:**
1. Run `acode policy validate`
2. Schema checked
3. Constraints checked
4. Report generated

**Verification Checklist:**
- [ ] Validation runs
- [ ] Errors reported
- [ ] Constraints checked
- [ ] Exit code correct

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-037B-01 | Policy section parsed | FR-037B-04 |
| UT-037B-02 | Missing section ok | FR-037B-03 |
| UT-037B-03 | Invalid warns | FR-037B-06 |
| UT-037B-04 | Override merges | FR-037B-07 |
| UT-037B-05 | Same-name uses repo | FR-037B-17 |
| UT-037B-06 | Different-name coexist | FR-037B-18 |
| UT-037B-07 | Non-overridable blocked | FR-037B-35 |
| UT-037B-08 | Escalation blocked | FR-037B-41 |
| UT-037B-09 | Additive works | FR-037B-21 |
| UT-037B-10 | Replacement works | FR-037B-22 |
| UT-037B-11 | Deterministic merge | FR-037B-30 |
| UT-037B-12 | Cache invalidation | FR-037B-13 |
| UT-037B-13 | Constraint check fast | FR-037B-43 |
| UT-037B-14 | Load < 200ms | NFR-037B-04 |
| UT-037B-15 | Thread safety | NFR-037B-20 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-037B-01 | Full repo load flow | E2E |
| IT-037B-02 | Override flow | FR-037B-16 |
| IT-037B-03 | Non-overridable enforcement | FR-037B-31 |
| IT-037B-04 | Escalation prevention | FR-037B-39 |
| IT-037B-05 | CLI list repo | FR-037B-46 |
| IT-037B-06 | CLI diff | FR-037B-52 |
| IT-037B-07 | CLI validate | FR-037B-54 |
| IT-037B-08 | Hot reload | FR-037B-10 |
| IT-037B-09 | File watch | FR-037B-12 |
| IT-037B-10 | Multiple repos | FR-037B-14 |
| IT-037B-11 | Override events | FR-037B-29 |
| IT-037B-12 | Cross-platform | NFR-037B-19 |
| IT-037B-13 | Performance benchmark | NFR-037B-04 |
| IT-037B-14 | Logging complete | NFR-037B-21 |
| IT-037B-15 | Fallback on error | NFR-037B-11 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Policy/
│       └── Repo/
│           ├── RepoPolicyOverride.cs
│           └── OverrideConstraint.cs
├── Acode.Application/
│   └── Policy/
│       └── Repo/
│           ├── IRepoPolicyLoader.cs
│           └── IOverrideConstraintChecker.cs
├── Acode.Infrastructure/
│   └── Policy/
│       └── Repo/
│           ├── RepoPolicyLoader.cs
│           ├── OverrideConstraintChecker.cs
│           └── RepoPolicyFileWatcher.cs
└── Acode.Cli/
    └── Commands/
        └── Policy/
            └── DiffCommand.cs
```

### Config Example

```yaml
# .agent/config.yml
policies:
  # Add new policy
  - name: "allow-network-dev"
    effect: allow
    category: network
    actions: ["network.http"]
    conditions:
      environment: "development"
    description: "Allow HTTP in dev environment"
    
  # Override existing (if overridable)
  - name: "file-size-limit"
    effect: allow
    category: file
    actions: ["file.write"]
    conditions:
      size: "<100MB"  # Changed from <50MB
    description: "Allow larger files for this project"
```

**End of Task 037.b Specification**
