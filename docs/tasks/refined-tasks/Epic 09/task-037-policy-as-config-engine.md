# Task 037: Policy-as-Config Engine

**Priority:** P0 – Critical  
**Tier:** L – Feature Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 9 – Safety & Compliance  
**Dependencies:** Task 002 (agent-config.yml)  

---

## Description

Task 037 implements the Policy-as-Config Engine for Acode. All agent behaviors MUST be governed by configurable policies defined in YAML configuration files. Policies define what actions are allowed, required, or prohibited.

The policy engine supports a three-level hierarchy: global policies (shipped with Acode), repository overrides (`.agent/config.yml`), and per-task overrides (inline in task definitions). Each level can extend or restrict policies from the level above.

Policy evaluation occurs before every significant action. If a policy denies an action, the operation MUST be blocked with a clear error message explaining the policy violation and how to resolve it.

The engine is designed for performance - policy evaluation MUST complete in <10ms. Caching and hot-reload ensure configuration changes take effect without restart.

### Business Value

Policy-as-Config provides:
- Centralized governance of agent behavior
- Flexible customization at multiple levels
- Auditable policy enforcement
- Clear boundaries for agent actions

### Scope Boundaries

This task covers the core policy engine and evaluation. Global config is 037.a. Repo overrides are 037.b. Per-task overrides are 037.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Config Loader | Task 002 | Policy YAML | Input |
| Tool Executor | Epic 05 | Policy check | Before execution |
| Secrets Scanner | Task 038 | Policy rules | Shared |
| Audit System | Task 039 | Violations | Recorded |
| CLI | `PolicyCommands` | User queries | Status |
| Event Bus | `IEventPublisher` | Violations | Async |
| Cache | `IPolicyCache` | Compiled policies | Performance |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Invalid policy YAML | Parse error | Block startup | Must fix config |
| Conflicting policies | Merge error | Use higher precedence | Clear message |
| Evaluation timeout | Timer | Deny (safe) | Action blocked |
| Cache corruption | Hash check | Reload from source | Slight delay |
| Missing required policy | Schema check | Block startup | Must add |
| Circular reference | Graph check | Error | Fix config |
| Hot reload failure | Exception | Keep old | Warn user |
| Policy too complex | Depth limit | Error | Simplify |

### Assumptions

1. **YAML format**: Policies defined in YAML
2. **Hierarchical merge**: Levels override cleanly
3. **Default deny**: Unknown actions denied
4. **Precedence clear**: Task > Repo > Global
5. **Schema validated**: Invalid config rejected
6. **Performance critical**: <10ms evaluation
7. **Caching enabled**: Compiled policies cached
8. **Hot reload**: Changes detected without restart

### Security Considerations

1. **No code execution**: Policies are data, not code
2. **Safe defaults**: Restrictive by default
3. **Audit all violations**: Events logged
4. **No privilege escalation**: Task cannot exceed repo
5. **Tamper detection**: Config hash verified
6. **Clear attribution**: Policy source recorded
7. **Immutable evaluation**: Same input = same output
8. **Rate limit violations**: Prevent abuse

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Policy | Rule governing agent behavior |
| Policy Level | Global, Repository, or Task |
| Precedence | Order of policy application |
| Merge | Combining policies from levels |
| Evaluation | Checking action against policies |
| Violation | Action denied by policy |
| Hot Reload | Update without restart |
| Policy Cache | Compiled policy storage |

---

## Out of Scope

- Visual policy editor
- Policy marketplace
- Remote policy distribution
- Policy DSL (custom language)
- Machine learning for policy suggestions
- Role-based policy access

---

## Functional Requirements

### FR-001 to FR-020: Core Engine

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-037-01 | `IPolicyEngine` interface MUST exist | P0 |
| FR-037-02 | `EvaluateAsync` MUST check action against policies | P0 |
| FR-037-03 | Result MUST indicate allowed/denied | P0 |
| FR-037-04 | Result MUST include denial reason | P0 |
| FR-037-05 | Result MUST include policy source | P0 |
| FR-037-06 | Evaluation MUST complete in <10ms | P0 |
| FR-037-07 | Engine MUST load policies on startup | P0 |
| FR-037-08 | Engine MUST validate policy schema | P0 |
| FR-037-09 | Invalid policies MUST block startup | P0 |
| FR-037-10 | Engine MUST support hot reload | P1 |
| FR-037-11 | Hot reload MUST be atomic | P1 |
| FR-037-12 | Engine MUST cache compiled policies | P0 |
| FR-037-13 | Cache MUST invalidate on config change | P0 |
| FR-037-14 | Engine MUST emit violation events | P0 |
| FR-037-15 | Engine MUST log evaluations (debug) | P1 |
| FR-037-16 | Engine MUST support policy hierarchy | P0 |
| FR-037-17 | Precedence: Task > Repo > Global | P0 |
| FR-037-18 | Unknown actions MUST be denied | P0 |
| FR-037-19 | Engine MUST be thread-safe | P0 |
| FR-037-20 | Engine MUST handle concurrent evaluation | P0 |

### FR-021 to FR-040: Policy Structure

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-037-21 | Policies MUST use YAML format | P0 |
| FR-037-22 | Policy MUST have `name` field | P0 |
| FR-037-23 | Policy MUST have `effect` (allow/deny) | P0 |
| FR-037-24 | Policy MUST have `actions` list | P0 |
| FR-037-25 | Actions MUST support wildcards | P1 |
| FR-037-26 | Policy MUST support `conditions` | P1 |
| FR-037-27 | Conditions MUST support string matching | P1 |
| FR-037-28 | Conditions MUST support regex | P2 |
| FR-037-29 | Policy MUST support `priority` | P1 |
| FR-037-30 | Higher priority MUST win | P1 |
| FR-037-31 | Policy MUST support `description` | P1 |
| FR-037-32 | Policy MUST support `severity` | P1 |
| FR-037-33 | Severity levels: info, warning, error, critical | P1 |
| FR-037-34 | Policy MUST support `tags` | P2 |
| FR-037-35 | Tags MUST be filterable | P2 |
| FR-037-36 | Policy MUST support `enabled` flag | P1 |
| FR-037-37 | Disabled policies MUST be skipped | P1 |
| FR-037-38 | Policy MUST support `expiry` date | P2 |
| FR-037-39 | Expired policies MUST warn | P2 |
| FR-037-40 | Policies MUST be mergeable | P0 |

### FR-041 to FR-055: Policy Categories

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-037-41 | Tool policies MUST exist | P0 |
| FR-037-42 | Tool policies control tool execution | P0 |
| FR-037-43 | File policies MUST exist | P0 |
| FR-037-44 | File policies control file operations | P0 |
| FR-037-45 | Network policies MUST exist | P0 |
| FR-037-46 | Network policies control external access | P0 |
| FR-037-47 | Secret policies MUST exist | P0 |
| FR-037-48 | Secret policies control redaction | P0 |
| FR-037-49 | Commit policies MUST exist | P0 |
| FR-037-50 | Commit policies control git operations | P0 |
| FR-037-51 | Resource policies MUST exist | P1 |
| FR-037-52 | Resource policies control usage limits | P1 |
| FR-037-53 | Mode policies MUST exist | P0 |
| FR-037-54 | Mode policies enforce operating modes | P0 |
| FR-037-55 | Custom policies MUST be supported | P2 |

### FR-056 to FR-070: CLI Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-037-56 | `acode policy list` MUST show policies | P0 |
| FR-037-57 | `acode policy show <name>` MUST show details | P0 |
| FR-037-58 | `acode policy check <action>` MUST evaluate | P1 |
| FR-037-59 | `acode policy validate` MUST validate config | P0 |
| FR-037-60 | `acode policy reload` MUST hot reload | P1 |
| FR-037-61 | Exit code 0 on success | P0 |
| FR-037-62 | Exit code 1 on failure | P0 |
| FR-037-63 | Exit code 2 on validation error | P0 |
| FR-037-64 | `--json` output MUST work | P1 |
| FR-037-65 | `--verbose` MUST show details | P2 |
| FR-037-66 | Help text MUST be complete | P1 |
| FR-037-67 | `--level` filter by policy level | P2 |
| FR-037-68 | `--category` filter by category | P2 |
| FR-037-69 | `--enabled-only` filter flag | P2 |
| FR-037-70 | Clear error messages | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-037-01 | Policy evaluation latency | <10ms | P0 |
| NFR-037-02 | Policy loading time | <500ms | P1 |
| NFR-037-03 | Cache lookup latency | <1ms | P1 |
| NFR-037-04 | Hot reload latency | <200ms | P2 |
| NFR-037-05 | Concurrent evaluations | 100/s | P2 |
| NFR-037-06 | Memory for policy cache | <20MB | P2 |
| NFR-037-07 | Schema validation | <100ms | P1 |
| NFR-037-08 | CLI response | <200ms | P1 |
| NFR-037-09 | Startup impact | <300ms | P1 |
| NFR-037-10 | File watch overhead | Minimal | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-037-11 | Evaluation determinism | 100% | P0 |
| NFR-037-12 | Invalid config detection | 100% | P0 |
| NFR-037-13 | Hot reload atomicity | 100% | P0 |
| NFR-037-14 | Cache consistency | 100% | P0 |
| NFR-037-15 | Thread safety | No races | P0 |
| NFR-037-16 | Graceful degradation | On errors | P1 |
| NFR-037-17 | Recovery from corrupt cache | Automatic | P1 |
| NFR-037-18 | Evaluation stability | No flapping | P0 |
| NFR-037-19 | Policy merging accuracy | 100% | P0 |
| NFR-037-20 | Precedence correctness | 100% | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-037-21 | Evaluations logged | Debug level | P1 |
| NFR-037-22 | Violations logged | Warning level | P0 |
| NFR-037-23 | Policy load logged | Info level | P1 |
| NFR-037-24 | Metrics: evaluations | Counter | P1 |
| NFR-037-25 | Metrics: violations | Counter | P0 |
| NFR-037-26 | Metrics: evaluation latency | Histogram | P2 |
| NFR-037-27 | Events: violation | Published | P0 |
| NFR-037-28 | Events: policy reload | Published | P1 |
| NFR-037-29 | Structured logging | JSON | P0 |
| NFR-037-30 | Policy source in logs | Required | P0 |

---

## Mode Compliance

| Mode | Policy Engine Behavior |
|------|------------------------|
| Local-Only | Full policy enforcement |
| Burst | Full policy enforcement |
| Air-Gapped | Full enforcement, strict mode |

---

## Acceptance Criteria / Definition of Done

### Core Engine
- [ ] AC-001: `IPolicyEngine` interface exists
- [ ] AC-002: `EvaluateAsync` works
- [ ] AC-003: Returns allowed/denied
- [ ] AC-004: Returns denial reason
- [ ] AC-005: Returns policy source
- [ ] AC-006: Evaluates in <10ms
- [ ] AC-007: Loads on startup
- [ ] AC-008: Validates schema
- [ ] AC-009: Invalid blocks startup
- [ ] AC-010: Hot reload works

### Policy Structure
- [ ] AC-011: YAML format works
- [ ] AC-012: Name field required
- [ ] AC-013: Effect field works
- [ ] AC-014: Actions list works
- [ ] AC-015: Wildcards work
- [ ] AC-016: Conditions work
- [ ] AC-017: Priority works
- [ ] AC-018: Enabled flag works
- [ ] AC-019: Merging works
- [ ] AC-020: Precedence correct

### Categories
- [ ] AC-021: Tool policies work
- [ ] AC-022: File policies work
- [ ] AC-023: Network policies work
- [ ] AC-024: Secret policies work
- [ ] AC-025: Commit policies work
- [ ] AC-026: Mode policies work

### CLI
- [ ] AC-027: `policy list` works
- [ ] AC-028: `policy show` works
- [ ] AC-029: `policy check` works
- [ ] AC-030: `policy validate` works
- [ ] AC-031: `policy reload` works
- [ ] AC-032: Exit codes correct
- [ ] AC-033: JSON output works
- [ ] AC-034: Filters work

### Caching
- [ ] AC-035: Cache populated
- [ ] AC-036: Cache lookup <1ms
- [ ] AC-037: Cache invalidation works
- [ ] AC-038: Concurrent safe

### Events
- [ ] AC-039: Violations emitted
- [ ] AC-040: Reload emitted

---

## User Verification Scenarios

### Scenario 1: Policy Blocks Action
**Persona:** Developer triggering blocked tool  
**Preconditions:** Policy denies tool  
**Steps:**
1. Configure policy denying `exec` tool
2. Try to use exec tool
3. Operation blocked
4. Clear error message

**Verification Checklist:**
- [ ] Action blocked
- [ ] Error shows policy
- [ ] Error shows reason
- [ ] Suggests resolution

### Scenario 2: List Active Policies
**Persona:** Developer checking config  
**Preconditions:** Policies configured  
**Steps:**
1. Run `acode policy list`
2. See all active policies
3. Shows level and effect
4. Clear formatting

**Verification Checklist:**
- [ ] All policies shown
- [ ] Levels indicated
- [ ] Effects clear
- [ ] Format readable

### Scenario 3: Validate Config
**Persona:** Developer before deploy  
**Preconditions:** Config file present  
**Steps:**
1. Run `acode policy validate`
2. Schema checked
3. Conflicts detected
4. Report generated

**Verification Checklist:**
- [ ] Validation runs
- [ ] Errors reported
- [ ] Conflicts shown
- [ ] Exit code correct

### Scenario 4: Hot Reload
**Persona:** Developer updating policy  
**Preconditions:** Engine running  
**Steps:**
1. Edit config file
2. Run `acode policy reload`
3. New policies active
4. No restart needed

**Verification Checklist:**
- [ ] Reload works
- [ ] Atomic update
- [ ] New policy active
- [ ] Old replaced

### Scenario 5: Repo Override
**Persona:** Developer customizing project  
**Preconditions:** Global policy exists  
**Steps:**
1. Add override in .agent/config.yml
2. Reload policies
3. Override takes effect
4. Global still active for non-overridden

**Verification Checklist:**
- [ ] Override loaded
- [ ] Precedence correct
- [ ] Non-overridden work
- [ ] Merge correct

### Scenario 6: Check Specific Action
**Persona:** Developer pre-checking  
**Preconditions:** Policies configured  
**Steps:**
1. Run `acode policy check tool.exec`
2. Shows evaluation result
3. Indicates allowed/denied
4. Shows which policy

**Verification Checklist:**
- [ ] Check works
- [ ] Result shown
- [ ] Policy source shown
- [ ] Details available

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-037-01 | Evaluate returns result | FR-037-02 |
| UT-037-02 | Allowed action passes | FR-037-03 |
| UT-037-03 | Denied action blocked | FR-037-03 |
| UT-037-04 | Reason included | FR-037-04 |
| UT-037-05 | Source included | FR-037-05 |
| UT-037-06 | Schema validation | FR-037-08 |
| UT-037-07 | Hierarchy merging | FR-037-40 |
| UT-037-08 | Precedence order | FR-037-17 |
| UT-037-09 | Wildcard matching | FR-037-25 |
| UT-037-10 | Condition evaluation | FR-037-26 |
| UT-037-11 | Priority ordering | FR-037-30 |
| UT-037-12 | Enabled/disabled | FR-037-36 |
| UT-037-13 | Cache lookup | FR-037-12 |
| UT-037-14 | Thread safety | FR-037-19 |
| UT-037-15 | Evaluation < 10ms | NFR-037-01 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-037-01 | Full evaluation flow | E2E |
| IT-037-02 | Config file loading | FR-037-07 |
| IT-037-03 | Hot reload | FR-037-10 |
| IT-037-04 | Cache invalidation | FR-037-13 |
| IT-037-05 | CLI list command | FR-037-56 |
| IT-037-06 | CLI check command | FR-037-58 |
| IT-037-07 | CLI validate command | FR-037-59 |
| IT-037-08 | Violation events | FR-037-14 |
| IT-037-09 | Concurrent evaluation | FR-037-20 |
| IT-037-10 | Tool policy | FR-037-41 |
| IT-037-11 | File policy | FR-037-43 |
| IT-037-12 | Mode policy | FR-037-53 |
| IT-037-13 | Metrics emission | NFR-037-24 |
| IT-037-14 | Structured logging | NFR-037-29 |
| IT-037-15 | Performance benchmark | NFR-037-01 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Policy/
│       ├── Policy.cs
│       ├── PolicyEffect.cs
│       ├── PolicyLevel.cs
│       ├── PolicyCategory.cs
│       ├── PolicyResult.cs
│       ├── PolicyCondition.cs
│       └── Events/
│           ├── PolicyViolationEvent.cs
│           └── PolicyReloadedEvent.cs
├── Acode.Application/
│   └── Policy/
│       ├── IPolicyEngine.cs
│       ├── IPolicyLoader.cs
│       ├── IPolicyEvaluator.cs
│       └── IPolicyCache.cs
├── Acode.Infrastructure/
│   └── Policy/
│       ├── PolicyEngine.cs
│       ├── YamlPolicyLoader.cs
│       ├── PolicyEvaluator.cs
│       ├── InMemoryPolicyCache.cs
│       └── PolicyFileWatcher.cs
└── Acode.Cli/
    └── Commands/
        └── Policy/
            ├── ListCommand.cs
            ├── ShowCommand.cs
            ├── CheckCommand.cs
            ├── ValidateCommand.cs
            └── ReloadCommand.cs
```

**End of Task 037 Specification**
