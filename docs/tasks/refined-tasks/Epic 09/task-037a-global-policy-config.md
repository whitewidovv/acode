# Task 037.a: Global Policy Config

**Priority:** P0 – Critical  
**Tier:** L – Feature Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 9 – Safety & Compliance  
**Dependencies:** Task 037  

---

## Description

Task 037.a implements the global policy configuration layer. Global policies are shipped with Acode and define baseline security and behavior rules. These policies apply to ALL repositories and tasks unless explicitly overridden.

Global policies establish the minimum security baseline. They define what actions are allowed by default, what requires explicit permission, and what is absolutely prohibited. Organizations can rely on global policies as a foundation without configuration.

The global policy file is embedded in the Acode distribution but can be extended via an external global config file at a well-known location (`~/.acode/global-policies.yml`).

### Business Value

Global policies provide:
- Secure defaults out-of-the-box
- Consistent baseline across all projects
- Foundation for organizational standards
- Reduced configuration burden

### Scope Boundaries

This task covers global policy definition and loading. Policy engine core is Task 037. Repo overrides are 037.b. Per-task overrides are 037.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Policy Engine | Task 037 | Load globals | Lowest precedence |
| Embedded Config | `Resources` | Built-in | Shipped |
| User Global | `~/.acode/` | Optional extension | User-level |
| Schema Validator | `ISchemaValidator` | Validate | Before load |
| Merge Engine | `IPolicyMerger` | Combine | With hierarchy |
| CLI | `PolicyCommands` | List/show | User query |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Embedded missing | Startup check | Fatal error | Cannot start |
| User global parse error | YAML error | Warn, use embedded | Degraded |
| Schema invalid | Validation | Warn, skip user | Clear message |
| File permission denied | IO error | Warn, use embedded | Clear message |
| Conflict in globals | Merge conflict | Priority order | Document order |
| Circular ref | Graph check | Error | Fix config |
| Performance issue | Timer | Log warning | Slower startup |
| Hot reload fails | Exception | Keep old | Warn user |

### Assumptions

1. **Embedded always present**: Shipped with distribution
2. **User global optional**: `~/.acode/global-policies.yml`
3. **User extends, not replaces**: Merged with embedded
4. **Priority order**: User global > Embedded
5. **Schema validated**: Both sources validated
6. **Performance**: Load < 200ms
7. **Cache after load**: No repeated parsing
8. **Cross-platform paths**: Handle Windows/Unix

### Security Considerations

1. **Embedded immutable**: Cannot be modified
2. **User global read-only**: Only read at startup
3. **No code execution**: Pure data
4. **Audit load sources**: Log what was loaded
5. **Checksum validation**: Embedded integrity
6. **Fallback to safe**: On error, use embedded
7. **Clear precedence**: Document merge order
8. **No secrets in policies**: Never contain secrets

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Embedded | Policies in distribution |
| User Global | ~/.acode/global-policies.yml |
| Merge | Combining policy sources |
| Baseline | Minimum security level |
| Extension | User additions to embedded |
| Override | Replacing embedded policy |
| Precedence | Order of policy priority |
| Schema | Policy structure definition |

---

## Out of Scope

- Remote global policy distribution
- Signed policy packages
- Policy versioning
- Organization-level policies
- Policy templates
- Policy inheritance chains

---

## Functional Requirements

### FR-001 to FR-015: Embedded Policies

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-037A-01 | Embedded policies MUST exist in distribution | P0 |
| FR-037A-02 | Embedded MUST be loaded on startup | P0 |
| FR-037A-03 | Embedded MUST be validated against schema | P0 |
| FR-037A-04 | Invalid embedded MUST be fatal error | P0 |
| FR-037A-05 | Embedded MUST define tool policies | P0 |
| FR-037A-06 | Embedded MUST define file policies | P0 |
| FR-037A-07 | Embedded MUST define network policies | P0 |
| FR-037A-08 | Embedded MUST define secret policies | P0 |
| FR-037A-09 | Embedded MUST define commit policies | P0 |
| FR-037A-10 | Embedded MUST define mode policies | P0 |
| FR-037A-11 | Embedded MUST be read-only | P0 |
| FR-037A-12 | Embedded checksum MUST be verified | P1 |
| FR-037A-13 | Embedded version MUST be tracked | P1 |
| FR-037A-14 | Embedded MUST be documented | P1 |
| FR-037A-15 | Embedded MUST have descriptions | P1 |

### FR-016 to FR-030: User Global Policies

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-037A-16 | User global path MUST be `~/.acode/global-policies.yml` | P0 |
| FR-037A-17 | User global MUST be optional | P0 |
| FR-037A-18 | Missing user global MUST NOT error | P0 |
| FR-037A-19 | User global MUST be validated | P0 |
| FR-037A-20 | Invalid user global MUST warn | P0 |
| FR-037A-21 | Invalid user global MUST fall back to embedded | P0 |
| FR-037A-22 | User global MUST extend embedded | P0 |
| FR-037A-23 | User global MUST be able to override embedded | P0 |
| FR-037A-24 | User global precedence MUST be higher | P0 |
| FR-037A-25 | User global MUST use same schema | P0 |
| FR-037A-26 | User global MUST support all policy types | P1 |
| FR-037A-27 | User global parsing MUST be performant | P1 |
| FR-037A-28 | User global MUST support hot reload | P2 |
| FR-037A-29 | User global location MUST be configurable | P2 |
| FR-037A-30 | Environment variable override MUST work | P2 |

### FR-031 to FR-045: Policy Merge

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-037A-31 | `IGlobalPolicyMerger` interface MUST exist | P0 |
| FR-037A-32 | Merge MUST combine embedded and user | P0 |
| FR-037A-33 | Same-name policies MUST use precedence | P0 |
| FR-037A-34 | User policies MUST win over embedded | P0 |
| FR-037A-35 | Merge MUST preserve non-conflicting | P0 |
| FR-037A-36 | Merge MUST handle missing properties | P0 |
| FR-037A-37 | Merge MUST validate result | P0 |
| FR-037A-38 | Merge conflicts MUST be logged | P0 |
| FR-037A-39 | Merge MUST be deterministic | P0 |
| FR-037A-40 | Merge order MUST be documented | P1 |
| FR-037A-41 | Merge MUST support additive | P1 |
| FR-037A-42 | Merge MUST support override | P1 |
| FR-037A-43 | Merge strategy MUST be per-policy | P2 |
| FR-037A-44 | Merge MUST emit events | P2 |
| FR-037A-45 | Merge MUST be performant | P1 |

### FR-046 to FR-060: Default Policies

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-037A-46 | Default: allow file read | P0 |
| FR-037A-47 | Default: allow file write in workspace | P0 |
| FR-037A-48 | Default: deny file write outside workspace | P0 |
| FR-037A-49 | Default: deny network in air-gapped | P0 |
| FR-037A-50 | Default: allow safe tools | P0 |
| FR-037A-51 | Default: deny dangerous tools | P0 |
| FR-037A-52 | Default: require secret redaction | P0 |
| FR-037A-53 | Default: block commit with secrets | P0 |
| FR-037A-54 | Default: enforce mode constraints | P0 |
| FR-037A-55 | Default: audit all actions | P0 |
| FR-037A-56 | Defaults MUST be documented | P0 |
| FR-037A-57 | Defaults MUST be overridable | P0 |
| FR-037A-58 | Dangerous tool list MUST be defined | P0 |
| FR-037A-59 | Safe tool list MUST be defined | P0 |
| FR-037A-60 | Defaults MUST be security-first | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-037A-01 | Embedded load time | <50ms | P1 |
| NFR-037A-02 | User global load time | <100ms | P1 |
| NFR-037A-03 | Merge time | <50ms | P1 |
| NFR-037A-04 | Total global load | <200ms | P0 |
| NFR-037A-05 | Schema validation | <50ms | P1 |
| NFR-037A-06 | Checksum verify | <10ms | P2 |
| NFR-037A-07 | Memory for globals | <10MB | P2 |
| NFR-037A-08 | Startup impact | <200ms | P1 |
| NFR-037A-09 | Hot reload time | <100ms | P2 |
| NFR-037A-10 | File watch overhead | Minimal | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-037A-11 | Embedded always loads | 100% | P0 |
| NFR-037A-12 | Fallback on user error | 100% | P0 |
| NFR-037A-13 | Merge correctness | 100% | P0 |
| NFR-037A-14 | Checksum integrity | 100% | P1 |
| NFR-037A-15 | Schema validation accuracy | 100% | P0 |
| NFR-037A-16 | Deterministic merge | 100% | P0 |
| NFR-037A-17 | No startup crashes | From invalid user | P0 |
| NFR-037A-18 | Graceful degradation | Always | P1 |
| NFR-037A-19 | Cross-platform paths | All OS | P0 |
| NFR-037A-20 | Hot reload atomicity | 100% | P1 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-037A-21 | Global load logged | Info level | P0 |
| NFR-037A-22 | User global logged | Info level | P0 |
| NFR-037A-23 | Merge logged | Debug level | P1 |
| NFR-037A-24 | Fallback logged | Warning level | P0 |
| NFR-037A-25 | Metrics: load time | Histogram | P2 |
| NFR-037A-26 | Metrics: policy count | Gauge | P2 |
| NFR-037A-27 | Events: globals loaded | Published | P1 |
| NFR-037A-28 | Schema errors logged | Error level | P0 |
| NFR-037A-29 | Structured logging | JSON | P0 |
| NFR-037A-30 | Source in logs | Required | P0 |

---

## Acceptance Criteria / Definition of Done

### Embedded
- [ ] AC-001: Embedded policies exist
- [ ] AC-002: Loaded on startup
- [ ] AC-003: Schema validated
- [ ] AC-004: Invalid is fatal
- [ ] AC-005: Tool policies present
- [ ] AC-006: File policies present
- [ ] AC-007: Network policies present
- [ ] AC-008: Secret policies present
- [ ] AC-009: Commit policies present
- [ ] AC-010: Mode policies present

### User Global
- [ ] AC-011: Path is ~/.acode/global-policies.yml
- [ ] AC-012: Optional, no error if missing
- [ ] AC-013: Validated on load
- [ ] AC-014: Invalid warns and falls back
- [ ] AC-015: Extends embedded
- [ ] AC-016: Can override embedded
- [ ] AC-017: Higher precedence
- [ ] AC-018: Same schema

### Merge
- [ ] AC-019: Merger interface exists
- [ ] AC-020: Combines sources
- [ ] AC-021: Precedence correct
- [ ] AC-022: Non-conflicting preserved
- [ ] AC-023: Deterministic
- [ ] AC-024: Conflicts logged
- [ ] AC-025: Result validated

### Defaults
- [ ] AC-026: File read allowed
- [ ] AC-027: File write in workspace allowed
- [ ] AC-028: File write outside denied
- [ ] AC-029: Network in air-gapped denied
- [ ] AC-030: Secret redaction required
- [ ] AC-031: Commit with secrets blocked
- [ ] AC-032: Defaults documented

---

## User Verification Scenarios

### Scenario 1: Fresh Install Defaults
**Persona:** New Acode user  
**Preconditions:** Fresh install, no user global  
**Steps:**
1. Start Acode
2. Check policies loaded
3. Embedded policies active
4. Safe defaults applied

**Verification Checklist:**
- [ ] Startup succeeds
- [ ] Embedded loaded
- [ ] Defaults active
- [ ] No errors

### Scenario 2: User Global Extension
**Persona:** Developer with custom policies  
**Preconditions:** User global file exists  
**Steps:**
1. Create ~/.acode/global-policies.yml
2. Add custom policy
3. Start Acode
4. Custom policy active

**Verification Checklist:**
- [ ] User file found
- [ ] Custom loaded
- [ ] Merged with embedded
- [ ] Both active

### Scenario 3: User Global Override
**Persona:** Developer relaxing restriction  
**Preconditions:** Override embedded policy  
**Steps:**
1. Define policy with same name as embedded
2. Set different effect
3. Start Acode
4. User policy wins

**Verification Checklist:**
- [ ] Override detected
- [ ] User wins
- [ ] Logged
- [ ] Works correctly

### Scenario 4: Invalid User Global
**Persona:** Developer with typo  
**Preconditions:** User global has syntax error  
**Steps:**
1. Create invalid YAML
2. Start Acode
3. Warning shown
4. Falls back to embedded

**Verification Checklist:**
- [ ] Error detected
- [ ] Warning logged
- [ ] Fallback works
- [ ] System stable

### Scenario 5: List Global Policies
**Persona:** Developer checking config  
**Preconditions:** Policies loaded  
**Steps:**
1. Run `acode policy list --level global`
2. See all global policies
3. Source indicated
4. Clear output

**Verification Checklist:**
- [ ] All shown
- [ ] Sources marked
- [ ] Format clear
- [ ] Complete

### Scenario 6: View Default Policy
**Persona:** Developer understanding defaults  
**Preconditions:** Default policies loaded  
**Steps:**
1. Run `acode policy show file-write-outside-workspace`
2. See policy details
3. Effect: deny
4. Description explains

**Verification Checklist:**
- [ ] Policy shown
- [ ] Effect clear
- [ ] Description helpful
- [ ] Source indicated

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-037A-01 | Embedded policies load | FR-037A-02 |
| UT-037A-02 | Embedded schema valid | FR-037A-03 |
| UT-037A-03 | User global optional | FR-037A-17 |
| UT-037A-04 | Invalid user falls back | FR-037A-21 |
| UT-037A-05 | Merge combines sources | FR-037A-32 |
| UT-037A-06 | User precedence wins | FR-037A-34 |
| UT-037A-07 | Non-conflicting preserved | FR-037A-35 |
| UT-037A-08 | Merge is deterministic | FR-037A-39 |
| UT-037A-09 | Default file policies | FR-037A-47 |
| UT-037A-10 | Default deny outside workspace | FR-037A-48 |
| UT-037A-11 | Default secret redaction | FR-037A-52 |
| UT-037A-12 | Checksum verification | FR-037A-12 |
| UT-037A-13 | Cross-platform paths | NFR-037A-19 |
| UT-037A-14 | Load < 200ms | NFR-037A-04 |
| UT-037A-15 | Structured logging | NFR-037A-29 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-037A-01 | Full global load flow | E2E |
| IT-037A-02 | User global extension | FR-037A-22 |
| IT-037A-03 | User global override | FR-037A-23 |
| IT-037A-04 | Invalid user fallback | FR-037A-21 |
| IT-037A-05 | CLI list global | FR-037A-56 |
| IT-037A-06 | Hot reload | FR-037A-28 |
| IT-037A-07 | Environment variable path | FR-037A-30 |
| IT-037A-08 | All default policies | FR-037A-46 |
| IT-037A-09 | Windows path handling | NFR-037A-19 |
| IT-037A-10 | Unix path handling | NFR-037A-19 |
| IT-037A-11 | Merge events | FR-037A-44 |
| IT-037A-12 | Startup with globals | NFR-037A-08 |
| IT-037A-13 | Schema validation | FR-037A-03 |
| IT-037A-14 | Performance benchmark | NFR-037A-04 |
| IT-037A-15 | Logging complete | NFR-037A-21 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Policy/
│       └── Global/
│           └── GlobalPolicySources.cs
├── Acode.Application/
│   └── Policy/
│       └── Global/
│           ├── IGlobalPolicyLoader.cs
│           └── IGlobalPolicyMerger.cs
├── Acode.Infrastructure/
│   └── Policy/
│       └── Global/
│           ├── EmbeddedPolicyLoader.cs
│           ├── UserGlobalPolicyLoader.cs
│           ├── GlobalPolicyMerger.cs
│           └── Resources/
│               └── embedded-policies.yml
└── Acode.Cli/
    └── Commands/
        └── Policy/
            └── (integrated into ListCommand)
```

### Embedded Policies Reference

```yaml
# embedded-policies.yml
version: "1.0"
policies:
  - name: "allow-file-read"
    effect: allow
    category: file
    actions: ["file.read"]
    description: "Allow reading files in workspace"
    
  - name: "allow-file-write-workspace"
    effect: allow
    category: file
    actions: ["file.write"]
    conditions:
      path: "starts_with:${workspace}"
    description: "Allow writing files within workspace"
    
  - name: "deny-file-write-outside"
    effect: deny
    category: file
    actions: ["file.write"]
    conditions:
      path: "not:starts_with:${workspace}"
    description: "Deny writing files outside workspace"
    priority: 100
    
  - name: "deny-network-airgapped"
    effect: deny
    category: network
    actions: ["network.*"]
    conditions:
      mode: "air-gapped"
    description: "Deny all network in air-gapped mode"
    priority: 100
    
  - name: "require-secret-redaction"
    effect: deny
    category: secret
    actions: ["output.unredacted"]
    description: "Always redact secrets from output"
    priority: 100
```

**End of Task 037.a Specification**
