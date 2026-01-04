# Task 037.c: Per-Task Overrides

**Priority:** P0 – Critical  
**Tier:** L – Feature Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 9 – Safety & Compliance  
**Dependencies:** Task 037, Task 037.a, Task 037.b  

---

## Description

Task 037.c implements per-task policy overrides. Tasks can include inline policy directives that temporarily adjust permissions for that specific execution scope. This enables fine-grained control while maintaining safety boundaries.

Per-task overrides are the highest precedence level: Task > Repo > Global. A task that requires network access for integration testing can request it inline. The override is scoped to that task's execution only. Once the task completes, the override expires.

This mechanism supports dynamic workflows where different tasks legitimately need different permissions. The system enforces that task-level overrides cannot bypass non-overridable policies or escalate beyond what the repo/global levels allow.

### Business Value

Per-task overrides provide:
- Fine-grained permission scoping
- Task-specific customization
- Automatic cleanup after task
- Audit trail per task execution

### Scope Boundaries

This task covers task-level inline overrides. Global policies are 037.a. Repo overrides are 037.b. Core engine is 037.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Task Parser | `ITaskParser` | Extract overrides | From task YAML |
| Policy Engine | Task 037 | Merge result | Highest precedence |
| Repo Policies | Task 037.b | Base layer | Lower precedence |
| Global Policies | Task 037.a | Base layer | Lowest precedence |
| Task Executor | `ITaskExecutor` | Scope binding | During execution |
| Schema Validator | `ISchemaValidator` | Validate | Before apply |
| Audit | Task 039 | Log override | Per task |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Invalid override syntax | Parser | Error | Task fails |
| Non-overridable violated | Constraint | Error | Clear message |
| Escalation attempt | Check | Error | Denied |
| Override leaks scope | Isolation check | Terminate | Fatal error |
| Parse fails | Exception | No override | Use repo/global |
| Conflict in task | Merge check | Error | Fix task |
| Performance overhead | Timer | Log | Warn if slow |
| Missing task context | State check | Error | Cannot apply |

### Assumptions

1. **Tasks are YAML-based**: Override section in task file
2. **Execution context exists**: Task executor provides scope
3. **Transient by design**: Override lives only during execution
4. **Same schema**: Uses same policy schema as global/repo
5. **Constrained**: Cannot violate non-overridable
6. **Performance**: Minimal overhead per task
7. **Audit required**: All overrides logged
8. **Isolation**: Override cannot affect other tasks

### Security Considerations

1. **Scope isolation**: Override only affects this task
2. **Non-overridable enforced**: Critical policies protected
3. **Cannot escalate**: Task cannot grant more than repo allows
4. **Audit every override**: Full trail
5. **Expiration**: Override dies with task
6. **No persistence**: Not saved beyond execution
7. **Clear attribution**: Log which task requested what
8. **Rollback on error**: Failed override = no change

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Per-Task Override | Inline policy in task definition |
| Task Scope | Duration of task execution |
| Transient | Lives only during execution |
| Inline Directive | Override in task YAML |
| Precedence | Task > Repo > Global |
| Scope Binding | Associate override with execution |
| Expiration | Override ends with task |
| Isolation | Override cannot leak to other tasks |

---

## Out of Scope

- Persistent task-level policies
- Cross-task override sharing
- Override templates
- Override approval workflows
- Time-based overrides
- User-specific task overrides

---

## Functional Requirements

### FR-001 to FR-015: Override Parsing

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-037C-01 | Task MUST support `policy_overrides:` section | P0 |
| FR-037C-02 | Override section MUST be optional | P0 |
| FR-037C-03 | Missing section MUST use repo/global | P0 |
| FR-037C-04 | Schema MUST match global policy schema | P0 |
| FR-037C-05 | Invalid override MUST fail task | P0 |
| FR-037C-06 | Error MUST identify invalid field | P0 |
| FR-037C-07 | Multiple overrides MUST be supported | P0 |
| FR-037C-08 | Override order MUST be document order | P1 |
| FR-037C-09 | Conflict detection MUST work | P0 |
| FR-037C-10 | Conflict MUST error | P0 |
| FR-037C-11 | Override name MUST be specified | P0 |
| FR-037C-12 | Override effect MUST be specified | P0 |
| FR-037C-13 | Actions list MUST be specified | P0 |
| FR-037C-14 | Conditions MUST be optional | P0 |
| FR-037C-15 | Description MUST be optional | P1 |

### FR-016 to FR-030: Scope Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-037C-16 | Override MUST bind to task scope | P0 |
| FR-037C-17 | Scope MUST be created on task start | P0 |
| FR-037C-18 | Scope MUST be destroyed on task end | P0 |
| FR-037C-19 | Override MUST not leak to other tasks | P0 |
| FR-037C-20 | Parallel tasks MUST have isolated scopes | P0 |
| FR-037C-21 | Child tasks MUST NOT inherit overrides | P0 |
| FR-037C-22 | Scope ID MUST be unique | P0 |
| FR-037C-23 | Scope MUST be traceable in logs | P0 |
| FR-037C-24 | Scope cleanup MUST be guaranteed | P0 |
| FR-037C-25 | Exception MUST still cleanup | P0 |
| FR-037C-26 | Timeout MUST still cleanup | P0 |
| FR-037C-27 | Cancellation MUST still cleanup | P0 |
| FR-037C-28 | Scope start MUST be logged | P0 |
| FR-037C-29 | Scope end MUST be logged | P0 |
| FR-037C-30 | Scope duration MUST be recorded | P1 |

### FR-031 to FR-045: Precedence and Merge

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-037C-31 | Task MUST have highest precedence | P0 |
| FR-037C-32 | Task > Repo > Global | P0 |
| FR-037C-33 | Same-name policy uses task version | P0 |
| FR-037C-34 | Different-name coexist | P0 |
| FR-037C-35 | Merge MUST be deterministic | P0 |
| FR-037C-36 | Merge result MUST be cached for scope | P1 |
| FR-037C-37 | Non-overridable MUST be respected | P0 |
| FR-037C-38 | Non-overridable attempt MUST error | P0 |
| FR-037C-39 | Escalation MUST be prevented | P0 |
| FR-037C-40 | Escalation attempt MUST error | P0 |
| FR-037C-41 | Error MUST name policy | P0 |
| FR-037C-42 | Error MUST explain constraint | P0 |
| FR-037C-43 | Merge events MUST be published | P1 |
| FR-037C-44 | Merge time MUST be measured | P2 |
| FR-037C-45 | Merge MUST be atomic | P0 |

### FR-046 to FR-060: Audit Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-037C-46 | Override MUST be logged to audit | P0 |
| FR-037C-47 | Log MUST include task ID | P0 |
| FR-037C-48 | Log MUST include override name | P0 |
| FR-037C-49 | Log MUST include effect | P0 |
| FR-037C-50 | Log MUST include actions | P0 |
| FR-037C-51 | Log MUST include timestamp | P0 |
| FR-037C-52 | Log MUST include scope ID | P0 |
| FR-037C-53 | Scope end MUST log duration | P1 |
| FR-037C-54 | Denied overrides MUST be logged | P0 |
| FR-037C-55 | Denial reason MUST be logged | P0 |
| FR-037C-56 | Structured log format | P0 |
| FR-037C-57 | Audit export MUST include | P1 |
| FR-037C-58 | Searchable by task ID | P2 |
| FR-037C-59 | Searchable by override name | P2 |
| FR-037C-60 | Retention MUST match task audit | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-037C-01 | Parse time | <50ms | P1 |
| NFR-037C-02 | Merge time | <20ms | P1 |
| NFR-037C-03 | Scope creation | <5ms | P1 |
| NFR-037C-04 | Scope cleanup | <5ms | P1 |
| NFR-037C-05 | Total overhead | <100ms | P0 |
| NFR-037C-06 | Memory per scope | <1MB | P2 |
| NFR-037C-07 | Parallel scopes | 100+ | P2 |
| NFR-037C-08 | GC cleanup | Immediate | P1 |
| NFR-037C-09 | Constraint check | <10ms | P1 |
| NFR-037C-10 | Audit write | <10ms | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-037C-11 | Scope isolation | 100% | P0 |
| NFR-037C-12 | Cleanup guaranteed | 100% | P0 |
| NFR-037C-13 | Non-overridable enforcement | 100% | P0 |
| NFR-037C-14 | No escalation | 100% | P0 |
| NFR-037C-15 | Parse correctness | 100% | P0 |
| NFR-037C-16 | Merge correctness | 100% | P0 |
| NFR-037C-17 | Audit completeness | 100% | P0 |
| NFR-037C-18 | Graceful error handling | Always | P0 |
| NFR-037C-19 | Thread safety | No races | P0 |
| NFR-037C-20 | Deterministic | Same input = same output | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-037C-21 | Override logged | Info level | P0 |
| NFR-037C-22 | Scope logged | Info level | P0 |
| NFR-037C-23 | Denial logged | Warning level | P0 |
| NFR-037C-24 | Merge logged | Debug level | P1 |
| NFR-037C-25 | Metrics: overrides | Counter | P2 |
| NFR-037C-26 | Metrics: denials | Counter | P1 |
| NFR-037C-27 | Events: scope created | Published | P1 |
| NFR-037C-28 | Events: scope destroyed | Published | P1 |
| NFR-037C-29 | Structured logging | JSON | P0 |
| NFR-037C-30 | Correlation ID | Required | P0 |

---

## Acceptance Criteria / Definition of Done

### Parsing
- [ ] AC-001: policy_overrides section supported
- [ ] AC-002: Section optional
- [ ] AC-003: Missing uses repo/global
- [ ] AC-004: Schema validated
- [ ] AC-005: Invalid fails task
- [ ] AC-006: Error identifies field
- [ ] AC-007: Multiple overrides work
- [ ] AC-008: Conflicts detected

### Scope
- [ ] AC-009: Override binds to scope
- [ ] AC-010: Scope created on start
- [ ] AC-011: Scope destroyed on end
- [ ] AC-012: No leakage
- [ ] AC-013: Parallel isolation
- [ ] AC-014: Cleanup guaranteed
- [ ] AC-015: Exception cleanup
- [ ] AC-016: Timeout cleanup

### Precedence
- [ ] AC-017: Task highest precedence
- [ ] AC-018: Same-name uses task
- [ ] AC-019: Different-name coexist
- [ ] AC-020: Deterministic merge
- [ ] AC-021: Non-overridable respected
- [ ] AC-022: Escalation prevented
- [ ] AC-023: Error names policy
- [ ] AC-024: Error explains constraint

### Audit
- [ ] AC-025: Override logged
- [ ] AC-026: Task ID included
- [ ] AC-027: Override name included
- [ ] AC-028: Effect included
- [ ] AC-029: Actions included
- [ ] AC-030: Scope ID included
- [ ] AC-031: Denial logged
- [ ] AC-032: Structured format

---

## User Verification Scenarios

### Scenario 1: Add Task Override
**Persona:** Developer adding permission  
**Preconditions:** Task needs network  
**Steps:**
1. Add policy_overrides to task
2. Define allow network policy
3. Run task
4. Network access granted

**Verification Checklist:**
- [ ] Override parsed
- [ ] Merged with repo/global
- [ ] Network allowed
- [ ] Logged to audit

### Scenario 2: Override Scoped Correctly
**Persona:** Developer checking isolation  
**Preconditions:** Override in one task  
**Steps:**
1. Run task with override
2. Run second task without
3. Second task normal policy
4. No leakage

**Verification Checklist:**
- [ ] First task has override
- [ ] Second task normal
- [ ] Scopes isolated
- [ ] Both logged correctly

### Scenario 3: Non-Overridable Blocked
**Persona:** Developer trying critical override  
**Preconditions:** Policy marked non-overridable  
**Steps:**
1. Add override for non-overridable
2. Task fails to start
3. Error identifies policy
4. Constraint explained

**Verification Checklist:**
- [ ] Task rejected
- [ ] Error clear
- [ ] Policy named
- [ ] Resolution suggested

### Scenario 4: Escalation Prevented
**Persona:** Developer trying more access  
**Preconditions:** Repo restricts network  
**Steps:**
1. Task tries to allow network
2. Beyond repo scope
3. Error shown
4. Escalation denied

**Verification Checklist:**
- [ ] Escalation blocked
- [ ] Error clear
- [ ] Constraint explained
- [ ] Repo policy enforced

### Scenario 5: Cleanup After Exception
**Persona:** Developer checking reliability  
**Preconditions:** Task with override  
**Steps:**
1. Task starts with override
2. Task throws exception
3. Override cleaned up
4. Next task normal

**Verification Checklist:**
- [ ] Override applied
- [ ] Exception occurs
- [ ] Scope destroyed
- [ ] No leakage

### Scenario 6: Parallel Task Isolation
**Persona:** Developer checking concurrency  
**Preconditions:** Multiple tasks  
**Steps:**
1. Run task A with override X
2. Run task B with override Y in parallel
3. Each has own scope
4. No cross-contamination

**Verification Checklist:**
- [ ] Both run
- [ ] Different scopes
- [ ] Correct overrides each
- [ ] Isolated logging

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-037C-01 | Override section parsed | FR-037C-01 |
| UT-037C-02 | Missing section ok | FR-037C-03 |
| UT-037C-03 | Invalid fails | FR-037C-05 |
| UT-037C-04 | Multiple overrides | FR-037C-07 |
| UT-037C-05 | Scope creation | FR-037C-17 |
| UT-037C-06 | Scope destruction | FR-037C-18 |
| UT-037C-07 | No leakage | FR-037C-19 |
| UT-037C-08 | Task highest precedence | FR-037C-31 |
| UT-037C-09 | Same-name uses task | FR-037C-33 |
| UT-037C-10 | Non-overridable blocked | FR-037C-38 |
| UT-037C-11 | Escalation blocked | FR-037C-40 |
| UT-037C-12 | Deterministic merge | FR-037C-35 |
| UT-037C-13 | Exception cleanup | FR-037C-25 |
| UT-037C-14 | Audit logging | FR-037C-46 |
| UT-037C-15 | Thread safety | NFR-037C-19 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-037C-01 | Full task flow | E2E |
| IT-037C-02 | Merge with repo/global | FR-037C-32 |
| IT-037C-03 | Non-overridable enforcement | FR-037C-37 |
| IT-037C-04 | Escalation prevention | FR-037C-39 |
| IT-037C-05 | Scope isolation | FR-037C-19 |
| IT-037C-06 | Parallel tasks | NFR-037C-07 |
| IT-037C-07 | Exception handling | FR-037C-25 |
| IT-037C-08 | Timeout handling | FR-037C-26 |
| IT-037C-09 | Audit export | FR-037C-57 |
| IT-037C-10 | Performance benchmark | NFR-037C-05 |
| IT-037C-11 | Cleanup guarantee | NFR-037C-12 |
| IT-037C-12 | Logging complete | NFR-037C-21 |
| IT-037C-13 | Events published | FR-037C-43 |
| IT-037C-14 | Cancellation cleanup | FR-037C-27 |
| IT-037C-15 | Correlation tracing | NFR-037C-30 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Policy/
│       └── Task/
│           ├── TaskPolicyOverride.cs
│           ├── TaskPolicyScope.cs
│           └── ScopeId.cs
├── Acode.Application/
│   └── Policy/
│       └── Task/
│           ├── ITaskPolicyParser.cs
│           ├── ITaskPolicyScopeManager.cs
│           └── ITaskPolicyMerger.cs
├── Acode.Infrastructure/
│   └── Policy/
│       └── Task/
│           ├── TaskPolicyParser.cs
│           ├── TaskPolicyScopeManager.cs
│           └── TaskPolicyMerger.cs
```

### Task Example

```yaml
# task.yml
name: integration-test
description: Run integration tests
policy_overrides:
  - name: "allow-network-test"
    effect: allow
    category: network
    actions: ["network.http", "network.https"]
    conditions:
      target: "localhost"
    description: "Allow network for integration tests"
    
  - name: "allow-temp-files"
    effect: allow
    category: file
    actions: ["file.write"]
    conditions:
      path: "/tmp/*"
    description: "Allow temp file writes"

steps:
  - run: integration-tests
```

### Key Interfaces

```csharp
public interface ITaskPolicyScopeManager
{
    TaskPolicyScope CreateScope(TaskId taskId, IEnumerable<TaskPolicyOverride> overrides);
    void DestroyScope(ScopeId scopeId);
    TaskPolicyScope? GetScope(ScopeId scopeId);
}

public interface ITaskPolicyMerger
{
    MergedPolicySet Merge(
        IEnumerable<TaskPolicyOverride> taskOverrides,
        IEnumerable<Policy> repoPolicies,
        IEnumerable<Policy> globalPolicies);
}
```

**End of Task 037.c Specification**
