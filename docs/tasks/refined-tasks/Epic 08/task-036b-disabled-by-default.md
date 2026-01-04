# Task 036.b: Disabled by Default

**Priority:** P0 – Critical  
**Tier:** L – Feature Layer  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 8 – CI/CD Integration  
**Dependencies:** Task 036, Task 036.a  

---

## Description

Task 036.b implements the "disabled by default" safety mechanism for deployment hooks. All deployment hooks MUST be disabled by default and require explicit enablement. This prevents accidental or unintended deployment actions.

The disabled-by-default pattern is a critical safety feature. Users MUST consciously opt-in to deployment hooks by setting `enabled: true` in configuration. This decision MUST be auditable with who/when/why recorded.

Enablement decisions are recorded as structured DB events in the Workspace DB (Task 050). This provides a complete audit trail of when deployment hooks were enabled and by whom.

### Business Value

Disabled-by-default provides:
- Safety against accidental deployment actions
- Explicit opt-in requirement
- Audit trail of enablement decisions
- Compliance with secure-by-default principles

### Scope Boundaries

This task covers enablement/disablement logic and audit. Hook execution is Task 036. Schema is 036.a. Approvals are 036.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Hook Engine | Task 036 | Check enabled | Before exec |
| Config Parser | `IConfigParser` | Enabled flag | Input |
| Workspace DB | Task 050 | Audit events | Persistence |
| Audit System | Task 039 | Event records | Compliance |
| CLI Commands | Enable/Disable | User action | Interactive |
| Event Bus | `IEventPublisher` | State events | Async |
| Redaction | `IRedactionService` | Log redaction | Required |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Config parse error | YAML error | Error message | Fix config |
| DB audit write fails | Exception | Retry queue | Degraded audit |
| Enablement conflict | Multiple sources | Priority order | Document order |
| Stale enablement cache | TTL expired | Refresh | Slight delay |
| Permission denied | Access check | Error message | Escalate |
| Invalid reason | Validation | Error message | Must provide |
| Event publish fails | Exception | Retry | Delayed audit |
| Concurrent enable | Lock check | Queue | Sequential |

### Assumptions

1. **Config file primary**: Enablement primarily via config
2. **CLI override possible**: Can enable via CLI
3. **Audit required**: All changes logged
4. **Reason optional**: Reason encouraged but not required
5. **Default false**: If not specified, disabled
6. **Cached state**: Enablement cached for performance
7. **Event sourced**: Changes are events
8. **Idempotent**: Repeated enable is no-op

### Security Considerations

1. **Default off**: Hooks disabled if not explicit
2. **Audit immutable**: Enablement events permanent
3. **No global override**: Each hook individually controlled
4. **Reason captured**: Why enabled recorded
5. **Timestamp recorded**: When enabled tracked
6. **User context**: Who enabled captured
7. **Redaction applied**: Sensitive context redacted
8. **No silent enable**: Enable always logged

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Enabled | Hook can execute |
| Disabled | Hook blocked from execution |
| Enablement Event | Audit record of enable/disable |
| Default State | Disabled unless explicit |
| Override | CLI or API enablement |
| Audit Trail | History of state changes |
| Reason | Why state was changed |
| Context | Who/when/where of change |

---

## Out of Scope

- Scheduled auto-enable
- Role-based enablement
- Temporary enablement windows
- Enablement delegation
- Remote enablement API
- Enablement policies

---

## Functional Requirements

### FR-001 to FR-015: Default State

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036B-01 | Hooks MUST be disabled by default | P0 |
| FR-036B-02 | Missing `enabled` field MUST mean disabled | P0 |
| FR-036B-03 | `enabled: false` MUST disable hook | P0 |
| FR-036B-04 | `enabled: true` MUST enable hook | P0 |
| FR-036B-05 | Null `enabled` MUST mean disabled | P0 |
| FR-036B-06 | Empty string `enabled` MUST error | P0 |
| FR-036B-07 | Non-boolean `enabled` MUST error | P0 |
| FR-036B-08 | Default state MUST be logged on first access | P1 |
| FR-036B-09 | State MUST be queryable | P0 |
| FR-036B-10 | State query MUST be fast (<10ms) | P1 |
| FR-036B-11 | State MUST be cached | P1 |
| FR-036B-12 | Cache TTL MUST be configurable | P2 |
| FR-036B-13 | Default cache TTL: 60 seconds | P2 |
| FR-036B-14 | Cache invalidation on config change | P1 |
| FR-036B-15 | Per-hook enabled state | P0 |

### FR-016 to FR-030: Enablement Logic

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036B-16 | `IHookEnabledChecker` interface MUST exist | P0 |
| FR-036B-17 | `IsEnabledAsync` MUST return boolean | P0 |
| FR-036B-18 | Check MUST include hook name | P0 |
| FR-036B-19 | Check MUST be fast | P0 |
| FR-036B-20 | Check MUST log result | P1 |
| FR-036B-21 | Disabled hook MUST block execution | P0 |
| FR-036B-22 | Block MUST return clear error | P0 |
| FR-036B-23 | Error MUST suggest how to enable | P1 |
| FR-036B-24 | Multiple hooks MUST be checked individually | P0 |
| FR-036B-25 | Bulk check MUST be supported | P1 |
| FR-036B-26 | Check result MUST be deterministic | P0 |
| FR-036B-27 | Check MUST respect config precedence | P0 |
| FR-036B-28 | CLI override MUST be highest precedence | P1 |
| FR-036B-29 | Config file MUST be second precedence | P1 |
| FR-036B-30 | Default disabled MUST be lowest precedence | P0 |

### FR-031 to FR-045: Audit Events

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036B-31 | Enable action MUST create audit event | P0 |
| FR-036B-32 | Disable action MUST create audit event | P0 |
| FR-036B-33 | Audit event MUST include hook name | P0 |
| FR-036B-34 | Audit event MUST include new state | P0 |
| FR-036B-35 | Audit event MUST include old state | P0 |
| FR-036B-36 | Audit event MUST include timestamp | P0 |
| FR-036B-37 | Audit event MUST include user context | P1 |
| FR-036B-38 | Audit event MUST include reason (if provided) | P1 |
| FR-036B-39 | Audit event MUST include source (config/CLI) | P0 |
| FR-036B-40 | Audit events MUST be persisted to DB | P0 |
| FR-036B-41 | Audit events MUST be immutable | P0 |
| FR-036B-42 | Audit events MUST have sync_state | P1 |
| FR-036B-43 | Sensitive context MUST be redacted | P0 |
| FR-036B-44 | Audit history MUST be queryable | P1 |
| FR-036B-45 | Audit query MUST support time range | P2 |

### FR-046 to FR-060: CLI Commands

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036B-46 | `acode deploy hooks status` MUST show states | P0 |
| FR-036B-47 | Status MUST show each hook enabled/disabled | P0 |
| FR-036B-48 | `acode deploy hooks enable <name>` MUST work | P1 |
| FR-036B-49 | `acode deploy hooks disable <name>` MUST work | P1 |
| FR-036B-50 | Enable MUST prompt for confirmation | P0 |
| FR-036B-51 | Enable MUST support `--reason` flag | P1 |
| FR-036B-52 | Enable MUST support `--yes` to skip confirm | P2 |
| FR-036B-53 | Disable MUST NOT require confirmation | P1 |
| FR-036B-54 | CLI changes MUST be session-only by default | P1 |
| FR-036B-55 | `--persist` flag to save to config | P2 |
| FR-036B-56 | Exit code 0 on success | P0 |
| FR-036B-57 | Exit code 1 on failure | P0 |
| FR-036B-58 | `--json` output MUST work | P1 |
| FR-036B-59 | Help text MUST be complete | P1 |
| FR-036B-60 | Verbose mode with `-v` | P2 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-036B-01 | Enabled check latency | <10ms | P0 |
| NFR-036B-02 | Cached check latency | <1ms | P1 |
| NFR-036B-03 | Audit write latency | <50ms | P1 |
| NFR-036B-04 | Bulk check (10 hooks) | <50ms | P2 |
| NFR-036B-05 | CLI response | <100ms | P1 |
| NFR-036B-06 | Config parse | <50ms | P1 |
| NFR-036B-07 | Cache refresh | <20ms | P2 |
| NFR-036B-08 | Memory overhead | <1MB | P2 |
| NFR-036B-09 | Startup impact | <50ms | P1 |
| NFR-036B-10 | Concurrent checks | Supported | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-036B-11 | Default disabled guarantee | 100% | P0 |
| NFR-036B-12 | Audit persistence | 99.99% | P0 |
| NFR-036B-13 | Check determinism | 100% | P0 |
| NFR-036B-14 | Cache consistency | Always valid | P0 |
| NFR-036B-15 | Config precedence correct | 100% | P0 |
| NFR-036B-16 | No false enabled | 0% | P0 |
| NFR-036B-17 | Graceful error handling | Always | P1 |
| NFR-036B-18 | Idempotent operations | Yes | P1 |
| NFR-036B-19 | No silent state changes | 0% | P0 |
| NFR-036B-20 | Audit completeness | 100% | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-036B-21 | Enabled checks logged | Debug level | P1 |
| NFR-036B-22 | State changes logged | Info level | P0 |
| NFR-036B-23 | Blocks logged | Warning level | P0 |
| NFR-036B-24 | Metrics: enabled count | Gauge | P1 |
| NFR-036B-25 | Metrics: checks performed | Counter | P2 |
| NFR-036B-26 | Metrics: blocks count | Counter | P1 |
| NFR-036B-27 | Events: state changed | Published | P0 |
| NFR-036B-28 | Cache hit/miss logged | Debug level | P2 |
| NFR-036B-29 | Performance warnings | > threshold | P2 |
| NFR-036B-30 | Structured log output | JSON | P0 |

---

## Mode Compliance

| Mode | Disabled-by-Default Behavior |
|------|------------------------------|
| Local-Only | Full enforcement |
| Burst | Same as Local-Only |
| Air-Gapped | Full enforcement |

---

## Acceptance Criteria / Definition of Done

### Default State
- [ ] AC-001: Hooks disabled by default
- [ ] AC-002: Missing enabled = disabled
- [ ] AC-003: `enabled: false` works
- [ ] AC-004: `enabled: true` works
- [ ] AC-005: State cached
- [ ] AC-006: Cache invalidated on change
- [ ] AC-007: Per-hook state
- [ ] AC-008: Query < 10ms

### Enablement Logic
- [ ] AC-009: `IHookEnabledChecker` exists
- [ ] AC-010: `IsEnabledAsync` works
- [ ] AC-011: Disabled blocks execution
- [ ] AC-012: Clear error on block
- [ ] AC-013: Suggestion in error
- [ ] AC-014: Bulk check works
- [ ] AC-015: Deterministic results
- [ ] AC-016: Precedence respected

### Audit
- [ ] AC-017: Enable creates event
- [ ] AC-018: Disable creates event
- [ ] AC-019: Hook name in event
- [ ] AC-020: State change in event
- [ ] AC-021: Timestamp in event
- [ ] AC-022: Context captured
- [ ] AC-023: Reason captured
- [ ] AC-024: Source captured
- [ ] AC-025: Events persisted
- [ ] AC-026: Events immutable
- [ ] AC-027: Redaction applied
- [ ] AC-028: History queryable

### CLI
- [ ] AC-029: `hooks status` works
- [ ] AC-030: Status shows each hook
- [ ] AC-031: Enable command works
- [ ] AC-032: Disable command works
- [ ] AC-033: Confirmation required
- [ ] AC-034: `--reason` accepted
- [ ] AC-035: Exit codes correct
- [ ] AC-036: JSON output works

---

## User Verification Scenarios

### Scenario 1: Hook Blocked by Default
**Persona:** New user with hooks  
**Preconditions:** Hooks defined but not enabled  
**Steps:**
1. Define hook in config
2. Try to run hook
3. Error: hook is disabled
4. Suggestion to enable

**Verification Checklist:**
- [ ] Hook blocked
- [ ] Clear error
- [ ] Suggestion shown
- [ ] No execution

### Scenario 2: Enable Hook
**Persona:** Developer enabling hook  
**Preconditions:** Hook disabled  
**Steps:**
1. Run `acode deploy hooks enable cleanup`
2. Confirmation prompt
3. Confirm yes
4. Hook enabled

**Verification Checklist:**
- [ ] Confirmation shown
- [ ] Hook enabled
- [ ] Audit logged
- [ ] Can now execute

### Scenario 3: View Hook Status
**Persona:** Developer checking state  
**Preconditions:** Mix of enabled/disabled hooks  
**Steps:**
1. Run `acode deploy hooks status`
2. See all hooks
3. Each shows enabled/disabled
4. Clear output

**Verification Checklist:**
- [ ] All hooks shown
- [ ] State visible
- [ ] Clear format
- [ ] Accurate

### Scenario 4: Audit Trail
**Persona:** Compliance reviewer  
**Preconditions:** Enablement events exist  
**Steps:**
1. Query enablement history
2. See all state changes
3. Who/when/why visible
4. Context complete

**Verification Checklist:**
- [ ] History shows all
- [ ] Context complete
- [ ] Timestamps accurate
- [ ] Reasons visible

### Scenario 5: Enable with Reason
**Persona:** Developer with policy  
**Preconditions:** Must document why  
**Steps:**
1. Run `acode deploy hooks enable cleanup --reason "Release 2.0"`
2. Confirm
3. Reason recorded
4. Visible in audit

**Verification Checklist:**
- [ ] Reason accepted
- [ ] In audit event
- [ ] Queryable
- [ ] Not redacted

### Scenario 6: Config Enablement
**Persona:** Developer via config  
**Preconditions:** Config file method  
**Steps:**
1. Set `enabled: true` in config
2. Save config
3. Hook now enabled
4. State reflects change

**Verification Checklist:**
- [ ] Config parsed
- [ ] State updated
- [ ] Audit logged
- [ ] Source = config

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-036B-01 | Default disabled | FR-036B-01 |
| UT-036B-02 | Missing enabled = disabled | FR-036B-02 |
| UT-036B-03 | enabled: false | FR-036B-03 |
| UT-036B-04 | enabled: true | FR-036B-04 |
| UT-036B-05 | Null enabled | FR-036B-05 |
| UT-036B-06 | Invalid type error | FR-036B-07 |
| UT-036B-07 | IsEnabledAsync | FR-036B-17 |
| UT-036B-08 | Disabled blocks | FR-036B-21 |
| UT-036B-09 | Enable audit event | FR-036B-31 |
| UT-036B-10 | Disable audit event | FR-036B-32 |
| UT-036B-11 | Event fields | FR-036B-33 |
| UT-036B-12 | Redaction applied | FR-036B-43 |
| UT-036B-13 | Cache works | FR-036B-11 |
| UT-036B-14 | Precedence order | FR-036B-27 |
| UT-036B-15 | Check < 10ms | NFR-036B-01 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-036B-01 | Full enable flow | E2E |
| IT-036B-02 | Full disable flow | E2E |
| IT-036B-03 | Audit persistence | FR-036B-40 |
| IT-036B-04 | CLI enable command | FR-036B-48 |
| IT-036B-05 | CLI disable command | FR-036B-49 |
| IT-036B-06 | CLI status command | FR-036B-46 |
| IT-036B-07 | Config enablement | FR-036B-04 |
| IT-036B-08 | Cache invalidation | FR-036B-14 |
| IT-036B-09 | Bulk check | FR-036B-25 |
| IT-036B-10 | History query | FR-036B-44 |
| IT-036B-11 | Workspace DB integration | Task 050 |
| IT-036B-12 | Concurrent checks | NFR-036B-10 |
| IT-036B-13 | Exit codes | FR-036B-56 |
| IT-036B-14 | JSON output | FR-036B-58 |
| IT-036B-15 | Performance benchmark | NFR-036B-01 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Deployment/
│       └── Hooks/
│           └── Enablement/
│               ├── HookEnabledState.cs
│               └── Events/
│                   ├── HookEnabledEvent.cs
│                   └── HookDisabledEvent.cs
├── Acode.Application/
│   └── Deployment/
│       └── Hooks/
│           └── Enablement/
│               └── IHookEnabledChecker.cs
├── Acode.Infrastructure/
│   └── Deployment/
│       └── Hooks/
│           └── Enablement/
│               ├── HookEnabledChecker.cs
│               └── EnablementStateCache.cs
└── Acode.Cli/
    └── Commands/
        └── Deploy/
            └── Hooks/
                ├── StatusCommand.cs
                ├── EnableCommand.cs
                └── DisableCommand.cs
```

**End of Task 036.b Specification**
