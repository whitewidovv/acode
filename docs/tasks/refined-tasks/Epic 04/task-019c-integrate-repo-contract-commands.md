# Task 019.c: Integrate Repo Contract Commands

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 019 (Language Runners), Task 002 (Config Contract)  

---

## Description

Task 019.c integrates repo contract commands from Task 002's `.agent/config.yml`. Repositories MAY define custom build, test, and run commands. Runners MUST respect these overrides.

The repo contract (Task 002) defines a `commands` section. This section specifies how to build, test, and run the project. When present, these commands MUST override default runner behavior.

Custom commands enable flexibility. Not all projects follow conventions. Some use make, others use custom scripts. The contract allows any command.

Command templates support variables. `${project_root}` resolves to repository root. `${configuration}` resolves to Debug/Release. Variables MUST be substituted before execution.

Fallback behavior MUST be defined. If no contract command exists, runners MUST use language defaults. If the contract specifies `null`, the operation MUST be disabled.

Validation MUST occur at load time. Invalid command templates MUST be rejected. Missing required variables MUST be reported.

This task bridges Task 002 configuration with Task 019 runners. Runners query the contract. Commands are resolved. Execution proceeds via Task 018.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Repo Contract | .agent/config.yml specification |
| Command Override | Custom command replacing default |
| Template Variable | Placeholder like ${var} |
| Fallback | Default when no override exists |

---

## Out of Scope

- **Contract schema definition** - See Task 002
- **Command execution mechanics** - See Task 018
- **Environment variables** - See Task 018.b

---

## Functional Requirements

### Contract Command Schema

- FR-001: Commands section MUST be optional in config
- FR-002: build command MUST be definable
- FR-003: test command MUST be definable
- FR-004: run command MUST be definable
- FR-005: restore command MUST be definable
- FR-006: Custom named commands MUST be supported

### Command Resolution

- FR-007: Runners MUST check contract before using defaults
- FR-008: Contract commands MUST take precedence
- FR-009: Null values MUST disable the operation
- FR-010: Empty string MUST be treated as error

### Template Variables

- FR-011: `${project_root}` MUST resolve to repo root
- FR-012: `${configuration}` MUST resolve to build config
- FR-013: `${project_path}` MUST resolve to project file
- FR-014: Unknown variables MUST cause error
- FR-015: Variable syntax MUST be `${name}`

### Validation

- FR-016: Commands MUST be validated at config load
- FR-017: Invalid templates MUST be rejected with clear error
- FR-018: Command strings MUST NOT be empty when defined

### Integration

- FR-019: ILanguageRunner MUST accept IRepoContract
- FR-020: Runners MUST query contract for commands
- FR-021: Fallback to defaults MUST occur when no contract command

---

## Non-Functional Requirements

- NFR-001: Variable resolution MUST complete < 1ms
- NFR-002: Config lookup MUST complete < 5ms
- NFR-003: Invalid config MUST produce actionable error message

---

## User Manual Documentation

### Configuration Example

```yaml
# .agent/config.yml
commands:
  build: "make build CONFIGURATION=${configuration}"
  test: "make test"
  run: "make run"
  restore: "make deps"
  lint: "make lint"  # Custom command
```

### Variable Reference

| Variable | Resolves To |
|----------|-------------|
| `${project_root}` | Repository root path |
| `${configuration}` | Debug or Release |
| `${project_path}` | Project file path |

### Disabling Operations

```yaml
commands:
  run: null  # Disables 'acode run' command
```

---

## Acceptance Criteria

- [ ] AC-001: Contract commands MUST override runner defaults
- [ ] AC-002: Template variables MUST resolve correctly
- [ ] AC-003: Null commands MUST disable operations
- [ ] AC-004: Missing contract MUST use defaults
- [ ] AC-005: Invalid templates MUST produce errors
- [ ] AC-006: Custom commands MUST be executable via CLI

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Contract/
├── CommandResolverTests.cs
│   ├── Should_Override_Default()
│   ├── Should_Resolve_Variables()
│   └── Should_Disable_On_Null()
```

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/Contract/
├── CommandResolver.cs
├── TemplateVariableResolver.cs
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-CMD-001 | Unknown template variable |
| ACODE-CMD-002 | Empty command string |
| ACODE-CMD-003 | Operation disabled by contract |

---

**End of Task 019.c Specification**