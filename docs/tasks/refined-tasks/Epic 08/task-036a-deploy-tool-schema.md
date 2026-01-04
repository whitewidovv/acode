# Task 036.a: Deploy Tool Schema

**Priority:** P0 – Critical  
**Tier:** L – Feature Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 8 – CI/CD Integration  
**Dependencies:** Task 036  

---

## Description

Task 036.a defines the schema for deployment hook definitions. All hooks MUST be validated against this schema before execution. The schema ensures type safety, parameter validation, and clear documentation of hook behavior.

The schema is JSON Schema-based with YAML support for authoring. Schema validation occurs before any hook execution, preventing malformed or dangerous hook definitions from running.

The schema MUST include optional `provenance` fields (repo_sha, run_id, worktree_id) and MUST support `audit_event_id` outputs. These fields enable complete traceability of hook execution.

### Business Value

Schema-driven hooks provide:
- Type-safe hook definitions
- Validation before execution
- Clear documentation of capabilities
- Reduced runtime errors

### Scope Boundaries

This task covers schema definition and validation. Hook execution is Task 036. Disabled-by-default is 036.b. Approvals are 036.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Hook Engine | Task 036 | Schema validation | Before exec |
| Config Parser | `IConfigParser` | Hook YAML | Input |
| Schema Validator | `ISchemaValidator` | Validation | JSON Schema |
| Audit System | Task 039 | audit_event_id | Output field |
| Provenance | Task 036 | Provenance fields | Optional |
| CLI Validate | `ValidateCommand` | User trigger | Manual |
| IDE Integration | Future | Autocomplete | P2 |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Invalid schema syntax | Parse error | Error message | Must fix schema |
| Missing required field | Validation error | Error message | Must add field |
| Invalid field type | Type check | Error message | Must fix type |
| Unknown hook type | Enum check | Error message | Use valid type |
| Invalid action | Action validation | Error message | Fix action |
| Circular reference | Graph check | Error message | Flatten |
| Schema version mismatch | Version check | Upgrade path | May need update |
| Large schema (>1MB) | Size check | Error | Simplify |

### Assumptions

1. **JSON Schema v7+**: Modern schema support
2. **YAML authoring**: Users write YAML, validated as JSON Schema
3. **Schema registry**: Built-in schemas available
4. **Extension points**: Custom types possible
5. **Version field**: Schema evolution supported
6. **Error messages clear**: Actionable validation errors
7. **Schema cached**: Validation fast after first parse
8. **IDE-friendly**: Enables autocomplete (future)

### Security Considerations

1. **No code in schema**: Schema is data, not code
2. **Safe defaults**: Missing optional fields use safe defaults
3. **Redaction patterns**: Secrets defined but not exposed
4. **Type enforcement**: Prevents injection
5. **Size limits**: Prevents DoS via large schemas
6. **No external refs**: Schemas self-contained
7. **Version pinning**: Schema changes tracked
8. **Validation mandatory**: Cannot skip validation

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| JSON Schema | Schema definition standard |
| Hook Type | Category of hook (cleanup, notify, etc.) |
| Action | Operation within a hook |
| Provenance Fields | Origin tracking (SHA, run_id, etc.) |
| audit_event_id | ID linking to audit record |
| Required Field | Mandatory schema property |
| Optional Field | Schema property with default |
| Schema Version | Evolution tracking |

---

## Out of Scope

- Visual schema editor
- Schema inheritance
- Dynamic schema generation
- Remote schema fetching
- Schema marketplace
- Schema migration tools

---

## Functional Requirements

### FR-001 to FR-015: Schema Definition

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036A-01 | Schema MUST use JSON Schema format | P0 |
| FR-036A-02 | Schema MUST support YAML authoring | P0 |
| FR-036A-03 | Schema MUST have version field | P0 |
| FR-036A-04 | Current schema version: 1.0 | P0 |
| FR-036A-05 | Schema MUST define hook types | P0 |
| FR-036A-06 | Hook types: cleanup, notification, script, artifact, cache | P0 |
| FR-036A-07 | Schema MUST define action types per hook | P0 |
| FR-036A-08 | Schema MUST have required `name` field | P0 |
| FR-036A-09 | Schema MUST have required `type` field | P0 |
| FR-036A-10 | Schema MUST have required `on` trigger field | P0 |
| FR-036A-11 | Trigger values: pre-release, release, post-release | P0 |
| FR-036A-12 | Schema MUST support optional `enabled` field | P0 |
| FR-036A-13 | Default `enabled`: true | P0 |
| FR-036A-14 | Schema MUST support optional `timeout` field | P1 |
| FR-036A-15 | Default `timeout`: 300 (seconds) | P1 |

### FR-016 to FR-030: Provenance Fields

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036A-16 | Schema MUST support optional `provenance` object | P0 |
| FR-036A-17 | Provenance MUST include optional `repo_sha` | P0 |
| FR-036A-18 | Provenance MUST include optional `run_id` | P0 |
| FR-036A-19 | Provenance MUST include optional `worktree_id` | P0 |
| FR-036A-20 | Provenance MUST include optional `session_id` | P1 |
| FR-036A-21 | Provenance MUST include optional `timestamp` | P1 |
| FR-036A-22 | Provenance fields auto-populated at runtime | P0 |
| FR-036A-23 | Manual provenance overrides MUST be allowed | P2 |
| FR-036A-24 | Schema MUST define `audit_event_id` output | P0 |
| FR-036A-25 | `audit_event_id` MUST be UUID format | P0 |
| FR-036A-26 | `audit_event_id` MUST be read-only | P0 |
| FR-036A-27 | Provenance MUST be included in exports | P0 |
| FR-036A-28 | Missing provenance MUST NOT block execution | P1 |
| FR-036A-29 | Provenance validation MUST be lenient | P1 |
| FR-036A-30 | Provenance MUST support extension fields | P2 |

### FR-031 to FR-045: Hook Type Schemas

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036A-31 | Cleanup hook MUST define `actions` array | P0 |
| FR-036A-32 | Cleanup actions: delete, archive, move | P0 |
| FR-036A-33 | Delete action MUST have `path` glob pattern | P0 |
| FR-036A-34 | Archive action MUST have `source` and `destination` | P1 |
| FR-036A-35 | Notification hook MUST define `channels` array | P0 |
| FR-036A-36 | Channel types: file, webhook (future), console | P0 |
| FR-036A-37 | File channel MUST have `path` | P0 |
| FR-036A-38 | Console channel MUST have `level` | P1 |
| FR-036A-39 | Script hook MUST have `command` field | P0 |
| FR-036A-40 | Script hook MUST have optional `args` array | P1 |
| FR-036A-41 | Script hook MUST have optional `workdir` | P1 |
| FR-036A-42 | Artifact hook MUST have `source` and `destination` | P1 |
| FR-036A-43 | Cache hook MUST have `action` (invalidate, clear) | P1 |
| FR-036A-44 | Cache hook MUST have `targets` array | P1 |
| FR-036A-45 | All hooks MUST support `condition` field | P2 |

### FR-046 to FR-060: Validation

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-036A-46 | `ISchemaValidator` interface MUST exist | P0 |
| FR-036A-47 | `ValidateAsync` MUST validate hook definition | P0 |
| FR-036A-48 | Validation MUST return structured errors | P0 |
| FR-036A-49 | Errors MUST include field path | P0 |
| FR-036A-50 | Errors MUST include expected type | P0 |
| FR-036A-51 | Errors MUST include actual value | P0 |
| FR-036A-52 | Errors MUST include suggestion | P1 |
| FR-036A-53 | Multiple errors MUST be collected | P0 |
| FR-036A-54 | Validation MUST be fail-fast optional | P2 |
| FR-036A-55 | Validation MUST check required fields | P0 |
| FR-036A-56 | Validation MUST check types | P0 |
| FR-036A-57 | Validation MUST check enums | P0 |
| FR-036A-58 | Validation MUST check patterns | P1 |
| FR-036A-59 | Validation MUST check min/max | P1 |
| FR-036A-60 | Validation latency MUST be <100ms | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-036A-01 | Schema validation latency | <100ms | P1 |
| NFR-036A-02 | Schema parsing latency | <50ms | P1 |
| NFR-036A-03 | Error collection | <10ms | P2 |
| NFR-036A-04 | Schema caching | <1ms lookup | P1 |
| NFR-036A-05 | Large hook validation | <200ms for 50 hooks | P2 |
| NFR-036A-06 | Memory for schema | <5MB | P2 |
| NFR-036A-07 | Concurrent validation | Supported | P2 |
| NFR-036A-08 | Cold start validation | <200ms | P1 |
| NFR-036A-09 | Incremental validation | Supported | P2 |
| NFR-036A-10 | CLI response | <100ms | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-036A-11 | Validation accuracy | 100% | P0 |
| NFR-036A-12 | No false positives | 0% | P0 |
| NFR-036A-13 | No false negatives | 0% | P0 |
| NFR-036A-14 | Error message clarity | Actionable | P0 |
| NFR-036A-15 | Schema stability | No breaking changes | P0 |
| NFR-036A-16 | Version compatibility | Documented | P0 |
| NFR-036A-17 | Graceful degradation | On parse errors | P1 |
| NFR-036A-18 | Schema evolution | Forward compatible | P1 |
| NFR-036A-19 | Default value reliability | Consistent | P0 |
| NFR-036A-20 | Type coercion | Predictable | P1 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-036A-21 | Validation logged | Debug level | P1 |
| NFR-036A-22 | Errors logged | Warning level | P0 |
| NFR-036A-23 | Metrics: validations | Counter | P2 |
| NFR-036A-24 | Metrics: validation errors | Counter | P1 |
| NFR-036A-25 | Metrics: validation latency | Histogram | P2 |
| NFR-036A-26 | Schema version logged | Info level | P1 |
| NFR-036A-27 | Hook count logged | Debug level | P2 |
| NFR-036A-28 | Cache hit/miss logged | Debug level | P2 |
| NFR-036A-29 | Performance warnings | > threshold | P2 |
| NFR-036A-30 | Structured error output | JSON | P1 |

---

## Mode Compliance

| Mode | Schema Validation Behavior |
|------|---------------------------|
| Local-Only | Full schema validation |
| Burst | Same as Local-Only |
| Air-Gapped | Full validation, no remote refs |

---

## Acceptance Criteria / Definition of Done

### Schema Definition
- [ ] AC-001: JSON Schema format used
- [ ] AC-002: YAML authoring supported
- [ ] AC-003: Version field present
- [ ] AC-004: Hook types defined
- [ ] AC-005: Required `name` field
- [ ] AC-006: Required `type` field
- [ ] AC-007: Required `on` trigger
- [ ] AC-008: Optional `enabled` with default
- [ ] AC-009: Optional `timeout` with default
- [ ] AC-010: Trigger values validated

### Provenance
- [ ] AC-011: Optional `provenance` object
- [ ] AC-012: `repo_sha` field
- [ ] AC-013: `run_id` field
- [ ] AC-014: `worktree_id` field
- [ ] AC-015: `audit_event_id` output
- [ ] AC-016: UUID format enforced
- [ ] AC-017: Auto-population works
- [ ] AC-018: Export includes provenance

### Hook Types
- [ ] AC-019: Cleanup hook schema
- [ ] AC-020: Cleanup actions defined
- [ ] AC-021: Notification hook schema
- [ ] AC-022: Channel types defined
- [ ] AC-023: Script hook schema
- [ ] AC-024: Command field required
- [ ] AC-025: Artifact hook schema
- [ ] AC-026: Cache hook schema

### Validation
- [ ] AC-027: `ISchemaValidator` exists
- [ ] AC-028: `ValidateAsync` works
- [ ] AC-029: Structured errors returned
- [ ] AC-030: Field path in errors
- [ ] AC-031: Expected type in errors
- [ ] AC-032: Multiple errors collected
- [ ] AC-033: Required field check
- [ ] AC-034: Type check works
- [ ] AC-035: Enum check works
- [ ] AC-036: Validation < 100ms

### CLI
- [ ] AC-037: `acode deploy hooks validate` works
- [ ] AC-038: Error output clear
- [ ] AC-039: JSON error output
- [ ] AC-040: Exit codes correct

---

## User Verification Scenarios

### Scenario 1: Valid Hook Validation
**Persona:** Developer creating hook  
**Preconditions:** Valid hook YAML  
**Steps:**
1. Create hook in config.yml
2. Run `acode deploy hooks validate`
3. Success message
4. No errors

**Verification Checklist:**
- [ ] Validation passes
- [ ] Success message shown
- [ ] Exit code 0
- [ ] Ready to execute

### Scenario 2: Missing Required Field
**Persona:** Developer with incomplete hook  
**Preconditions:** Hook missing `type` field  
**Steps:**
1. Create hook without type
2. Run validation
3. Error: missing required field 'type'
4. Fix and re-validate

**Verification Checklist:**
- [ ] Error detected
- [ ] Field path shown
- [ ] Suggestion provided
- [ ] Clear message

### Scenario 3: Invalid Type
**Persona:** Developer with wrong type  
**Preconditions:** Hook with invalid type value  
**Steps:**
1. Use `type: invalid`
2. Run validation
3. Error: 'invalid' not in [cleanup, notification, ...]
4. See valid options

**Verification Checklist:**
- [ ] Type validated
- [ ] Enum values shown
- [ ] Clear error
- [ ] Actionable

### Scenario 4: Provenance Fields
**Persona:** Developer adding provenance  
**Preconditions:** Hook with provenance  
**Steps:**
1. Add provenance object
2. Include repo_sha, run_id
3. Validate
4. Passes

**Verification Checklist:**
- [ ] Provenance accepted
- [ ] Optional fields work
- [ ] UUID format checked
- [ ] Auto-populate documented

### Scenario 5: Multiple Errors
**Persona:** Developer with many issues  
**Preconditions:** Hook with multiple errors  
**Steps:**
1. Create hook with 3 errors
2. Run validation
3. All 3 errors shown
4. Can fix in one pass

**Verification Checklist:**
- [ ] All errors collected
- [ ] Not fail-fast
- [ ] Each with path
- [ ] Efficient fix

### Scenario 6: Schema Version Check
**Persona:** Developer with old schema  
**Preconditions:** Hook using old schema version  
**Steps:**
1. Use version 0.9
2. Run validation
3. Warning: version outdated
4. Upgrade guidance

**Verification Checklist:**
- [ ] Version detected
- [ ] Warning shown
- [ ] Upgrade path clear
- [ ] Backward compat noted

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-036A-01 | JSON Schema format | FR-036A-01 |
| UT-036A-02 | YAML parsing | FR-036A-02 |
| UT-036A-03 | Version field | FR-036A-03 |
| UT-036A-04 | Hook type enum | FR-036A-06 |
| UT-036A-05 | Required name | FR-036A-08 |
| UT-036A-06 | Required type | FR-036A-09 |
| UT-036A-07 | Required on | FR-036A-10 |
| UT-036A-08 | Default enabled | FR-036A-13 |
| UT-036A-09 | Provenance fields | FR-036A-16 |
| UT-036A-10 | audit_event_id format | FR-036A-25 |
| UT-036A-11 | Cleanup schema | FR-036A-31 |
| UT-036A-12 | Notification schema | FR-036A-35 |
| UT-036A-13 | Structured errors | FR-036A-48 |
| UT-036A-14 | Multiple errors | FR-036A-53 |
| UT-036A-15 | Validation < 100ms | NFR-036A-01 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-036A-01 | Full validation flow | E2E |
| IT-036A-02 | Valid cleanup hook | FR-036A-31 |
| IT-036A-03 | Valid notification hook | FR-036A-35 |
| IT-036A-04 | Valid script hook | FR-036A-39 |
| IT-036A-05 | Provenance auto-populate | FR-036A-22 |
| IT-036A-06 | CLI validate command | FR-036A-47 |
| IT-036A-07 | Error JSON output | NFR-036A-30 |
| IT-036A-08 | Schema caching | NFR-036A-04 |
| IT-036A-09 | Version compatibility | NFR-036A-16 |
| IT-036A-10 | Large hook set | NFR-036A-05 |
| IT-036A-11 | Default values | NFR-036A-19 |
| IT-036A-12 | Type coercion | NFR-036A-20 |
| IT-036A-13 | Concurrent validation | NFR-036A-07 |
| IT-036A-14 | Export includes provenance | FR-036A-27 |
| IT-036A-15 | Performance benchmark | NFR-036A-01 |

---

## Schema Definition (Reference)

```yaml
# Hook Schema v1.0
$schema: "http://json-schema.org/draft-07/schema#"
type: object
required:
  - name
  - type
  - on
properties:
  name:
    type: string
    pattern: "^[a-z][a-z0-9-]*$"
  type:
    type: string
    enum: [cleanup, notification, script, artifact, cache]
  on:
    type: string
    enum: [pre-release, release, post-release]
  enabled:
    type: boolean
    default: true
  timeout:
    type: integer
    minimum: 1
    maximum: 3600
    default: 300
  provenance:
    type: object
    properties:
      repo_sha:
        type: string
        pattern: "^[a-f0-9]{40}$"
      run_id:
        type: string
        format: uuid
      worktree_id:
        type: string
      session_id:
        type: string
      timestamp:
        type: string
        format: date-time
  actions:
    type: array
    items:
      $ref: "#/definitions/action"
  channels:
    type: array
    items:
      $ref: "#/definitions/channel"
```

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Deployment/
│       └── Hooks/
│           └── Schema/
│               ├── HookSchema.cs
│               ├── HookType.cs
│               ├── TriggerType.cs
│               ├── ProvenanceSchema.cs
│               └── ValidationError.cs
├── Acode.Application/
│   └── Deployment/
│       └── Hooks/
│           └── Schema/
│               └── ISchemaValidator.cs
├── Acode.Infrastructure/
│   └── Deployment/
│       └── Hooks/
│           └── Schema/
│               ├── JsonSchemaValidator.cs
│               ├── SchemaCache.cs
│               └── Schemas/
│                   └── hook-schema-v1.json
└── Acode.Cli/
    └── Commands/
        └── Deploy/
            └── Hooks/
                └── ValidateCommand.cs
```

**End of Task 036.a Specification**
