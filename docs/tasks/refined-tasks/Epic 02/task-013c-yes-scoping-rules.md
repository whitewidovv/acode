# Task 013.c: --yes Scoping Rules

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 013 (Human Approval Gates), Task 013.a (Rules/Prompts), Task 013.b (Persistence)  

---

## Description

Task 013.c implements the --yes flag scoping system—controlling when and how automated approval bypasses prompts. The --yes flag provides convenience for experienced users, but unscoped --yes is dangerous. Scoping rules define exactly what --yes approves.

The --yes flag is a productivity feature for interactive sessions. Users who understand the risk can bypass prompts. But --yes without scope is like running `rm -rf` without looking—convenient until catastrophic. Scoping rules provide guardrails.

By default, --yes applies only to low-risk operations: reading files, listing directories, and similar read-only actions. High-risk operations—file deletion, terminal commands, config changes—require explicit scoping or remain prompted.

Scoping uses a domain-specific syntax. `--yes=file_write` approves file writes. `--yes=terminal:safe` approves terminal commands marked safe. `--yes=all` approves everything (requires explicit acknowledgment). Scopes can be combined: `--yes=file_write,file_read`.

Scope inheritance follows precedence rules. Command-line --yes overrides config file. Config file overrides defaults. More specific scopes override general. Deny always wins—if any rule denies, the operation is denied regardless of --yes.

Risk levels gate what scopes are available. Level 1 (low) operations can be approved with basic --yes. Level 2 (medium) requires explicit scope. Level 3 (high) requires explicit acknowledgment. Level 4 (critical) cannot use --yes at all.

Audit logging records all --yes usage. Every bypassed prompt is logged with the scope that allowed it. Analytics identify patterns—operations frequently auto-approved might merit policy changes.

Error handling addresses invalid scopes. Typos are caught: `--yes=filwrite` is rejected with suggestions. Unknown operations in scope are rejected. Conflicting scopes are flagged.

Session-level vs operation-level scoping provides flexibility. `acode run --yes=file_write` applies to the entire session. `--yes-next=file_write` applies only to the next operation. This granularity supports cautious automation.

The system protects against footguns. `--yes=all` requires additional confirmation. Certain operations (like deleting .git) cannot be --yes'd. Rate limiting prevents runaway automation.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Scope | What operations --yes applies to |
| Risk Level | Danger classification (1-4) |
| Bypass | Skip approval prompt |
| Precedence | Order of scope application |
| Domain Syntax | Scope specification format |
| Session Scope | Applies to whole session |
| Operation Scope | Applies to one operation |
| Implicit Scope | Default --yes coverage |
| Explicit Scope | User-specified coverage |
| Deny Override | Deny trumps approve |
| Footgun | Self-damaging action |
| Rate Limit | Max bypasses per period |
| Audit | Record of bypasses |
| Acknowledgment | Explicit danger acceptance |
| Scope Validation | Checking scope syntax |

---

## Out of Scope

The following items are explicitly excluded from Task 013.c:

- **Rule definition** - Task 013.a
- **Persistence** - Task 013.b
- **Prompt rendering** - Task 013.a
- **Custom risk levels** - Predefined only
- **Remote scope management** - Local only
- **Machine learning** - Rule-based only
- **Scope sharing** - Single user
- **Scope versioning** - No history
- **Scope templates** - Direct specification
- **Third-party integrations** - Native only

---

## Assumptions

### Technical Assumptions

- ASM-001: --yes flag accepts optional scope specifier
- ASM-002: Scope syntax is parseable and validatable
- ASM-003: Risk levels are predefined (low, medium, high, critical)
- ASM-004: Scope can include operation types and file patterns
- ASM-005: Invalid scopes result in clear error messages

### Behavioral Assumptions

- ASM-006: --yes alone approves low-risk operations only
- ASM-007: Explicit scope required for high-risk auto-approval
- ASM-008: Critical operations cannot be auto-approved
- ASM-009: Users must acknowledge danger of broad scopes
- ASM-010: Scopes are validated before session starts

### Dependency Assumptions

- ASM-011: Task 013 gate framework consults --yes scopes
- ASM-012: Task 013.a rules define risk levels
- ASM-013: Task 010 CLI provides --yes flag parsing

### Safety Assumptions

- ASM-014: Default behavior is safe (minimal auto-approval)
- ASM-015: Dangerous operations require explicit user action
- ASM-016: Scope documentation clearly explains implications

---

## Functional Requirements

### Scope Specification

- FR-001: --yes MUST accept scope argument
- FR-002: Empty --yes MUST use defaults
- FR-003: Scope MUST be comma-separated
- FR-004: Scope MUST support categories
- FR-005: Scope MUST support modifiers

### Scope Syntax

- FR-006: Format: category[:modifier]
- FR-007: Categories: file_read, file_write, file_delete, terminal, config
- FR-008: Modifiers: safe, all, pattern
- FR-009: Pattern modifier MUST support globs
- FR-010: Invalid syntax MUST error

### Default Scope

- FR-011: Default MUST include file_read
- FR-012: Default MUST exclude file_delete
- FR-013: Default MUST exclude terminal
- FR-014: Defaults MUST be configurable

### Risk Levels

- FR-015: Level 1: Low risk, implicit --yes
- FR-016: Level 2: Medium, explicit scope required
- FR-017: Level 3: High, acknowledgment required
- FR-018: Level 4: Critical, no --yes allowed

### Scope Application

- FR-019: --yes applies to session
- FR-020: --yes-next applies to next operation
- FR-021: Session scope MUST persist
- FR-022: Operation scope MUST clear after use

### Precedence

- FR-023: CLI overrides config for non-deny decisions (e.g., allow vs prompt)
- FR-024: Config overrides defaults for non-deny decisions
- FR-025: Specific overrides general
- FR-026: Deny overrides allow across all sources and levels of specificity. After applying FR-023–FR-025 to determine the most specific non-deny behavior, if any applicable rule is an explicit deny, the final result MUST be deny.

**Precedence Example:**

If the defaults say `file_write = prompt`, the config file says `file_write = deny`, and the CLI is invoked with `--yes=file_write` (allow), the operation **MUST be denied**.

Explanation:
1. FR-023 allows CLI to override config for non-deny behaviors
2. FR-026 and the global rule "Deny always wins" mean that an explicit deny in any source cannot be bypassed by CLI `--yes`
3. This prevents `--yes` from becoming a security bypass mechanism

**Conflict Resolution Order:**
1. Check all sources (defaults, config, CLI) for explicit deny → If found, **deny wins**
2. If no deny found, apply FR-023–FR-025 to find most specific non-deny behavior
3. Use the resulting behavior (allow, prompt, or reject)

### Validation

- FR-027: Invalid scope MUST error
- FR-028: Unknown category MUST error
- FR-029: Invalid modifier MUST error
- FR-030: MUST suggest corrections

### Special Scopes

- FR-031: --yes=all MUST require confirmation
- FR-032: --yes=none MUST disable all
- FR-033: --yes=default MUST use defaults

### Protected Operations

- FR-034: .git deletion MUST NOT be bypassable
- FR-035: Config deletion MUST NOT be bypassable
- FR-036: Protected list MUST be configurable

### Rate Limiting

- FR-037: Max bypasses per minute
- FR-038: Default: 100 per minute
- FR-039: Exceeded MUST pause session
- FR-040: Configurable limits

### Logging

- FR-041: Every bypass MUST be logged
- FR-042: Scope used MUST be logged
- FR-043: Operation details MUST be logged
- FR-044: Risk level MUST be logged

### Config Integration

- FR-045: Config MUST support yes.default_scope
- FR-046: Config MUST support yes.protected_operations
- FR-047: Config MUST support yes.rate_limit
- FR-048: Config MUST support yes.require_ack_for_all

### CLI Options

- FR-049: --yes MUST work on all commands
- FR-050: --yes-next MUST work
- FR-051: --no MUST deny all
- FR-052: --interactive MUST force prompts

### Error Handling

- FR-053: Invalid scope MUST show error
- FR-054: Protected operation MUST show warning
- FR-055: Rate limit MUST show message
- FR-056: Acknowledgment MUST be explicit

---

## Non-Functional Requirements

### Performance

- NFR-001: Scope parsing < 1ms
- NFR-002: Scope validation < 5ms
- NFR-003: No blocking on logging

### Security

- NFR-004: Protected operations MUST NOT be bypassed
- NFR-005: Rate limiting MUST prevent runaway
- NFR-006: Audit trail MUST be complete

### Usability

- NFR-007: Clear error messages
- NFR-008: Helpful suggestions
- NFR-009: Consistent syntax

### Reliability

- NFR-010: Defaults MUST always work
- NFR-011: Invalid scope MUST NOT crash
- NFR-012: Graceful degradation

### Compliance

- NFR-013: Complete bypass audit
- NFR-014: Risk level tracking
- NFR-015: Policy enforcement

---

## User Manual Documentation

### Overview

The --yes flag bypasses approval prompts for convenience. Scoping rules control exactly what --yes approves, providing safety guardrails for automated workflows.

### Basic Usage

```bash
# Default scope (low-risk only)
$ acode run --yes "Read all TypeScript files"

# Explicit scope
$ acode run --yes=file_write "Update README"

# Multiple scopes
$ acode run --yes=file_read,file_write "Refactor code"

# All operations (requires acknowledgment)
$ acode run --yes=all "Complete refactoring"
WARNING: --yes=all bypasses ALL approval prompts.
Type 'I UNDERSTAND' to continue: I UNDERSTAND
```

### Scope Syntax

Format: `--yes=category[:modifier][,category[:modifier]]...`

| Category | Description | Risk Level |
|----------|-------------|------------|
| file_read | Read files | 1 |
| file_write | Write files | 2 |
| file_delete | Delete files | 3 |
| terminal | Execute commands | 3 |
| terminal:safe | Safe commands only | 2 |
| config | Modify config | 3 |
| all | Everything | 4 |

### Modifiers

```bash
# Safe terminal commands only
$ acode run --yes=terminal:safe "Run tests"

# All terminal commands
$ acode run --yes=terminal:all "Build project"

# Pattern-based
$ acode run --yes=file_write:*.test.ts "Update tests"
```

### Risk Levels

| Level | Name | --yes Behavior |
|-------|------|----------------|
| 1 | Low | Implicit (no scope needed) |
| 2 | Medium | Explicit scope required |
| 3 | High | Explicit scope + warning |
| 4 | Critical | Cannot bypass |

### Default Scope

Default --yes (no explicit scope) covers:
- file_read: Reading files
- dir_list: Listing directories
- search: Searching codebase

Does NOT cover:
- file_write: Writing files
- file_delete: Deleting files
- terminal: Running commands

### Session vs Operation Scope

```bash
# Session scope (whole session)
$ acode run --yes=file_write "Update all files"
# All file writes are auto-approved

# Operation scope (next only)
$ acode run "Update files"
# Agent requests file write...
$ --yes-next file_write
# Only THIS write is approved
```

### Protected Operations

Some operations cannot be bypassed:
- Deleting .git directory
- Deleting .agent config
- Modifying protected files

```bash
$ acode run --yes=all "Clean everything"
# Agent tries to delete .git...
WARNING: Cannot bypass protected operation: .git deletion
Approve manually? [y/N] 
```

### Rate Limiting

```bash
$ acode run --yes=file_write "Update 500 files"
# After 100 bypasses...
RATE LIMIT: 100 bypasses per minute exceeded.
Pausing for 30 seconds...
Continue? [Y/n]
```

### Configuration

```yaml
# .agent/config.yml
yes:
  # Default scope when no explicit scope given
  default_scope:
    - file_read
    - dir_list
    - search
    
  # Operations that can never be bypassed
  protected_operations:
    - delete:.git/**
    - delete:.agent/**
    - write:.env*
    
  # Rate limiting
  rate_limit:
    max_per_minute: 100
    pause_seconds: 30
    
  # Require acknowledgment for --yes=all
  require_ack_for_all: true
  
  # Custom risk overrides
  risk_overrides:
    - pattern: "*.test.ts"
      operation: file_write
      risk_level: 1  # Low risk for test files
```

### Precedence Rules

1. `--no` flag (highest priority)
2. Protected operations
3. Rate limits
4. CLI --yes scope
5. Config default_scope
6. Built-in defaults

### Error Messages

```bash
# Invalid scope
$ acode run --yes=filwrite "Update"
ERROR: Unknown scope 'filwrite'. Did you mean 'file_write'?

# Protected operation
$ acode run --yes=file_delete "Clean .git"
ERROR: Cannot bypass protected operation: .git/**

# Rate limit
$ acode run --yes=file_write "Mass update"
WARNING: Rate limit exceeded (100/min). Pausing...
```

### Best Practices

1. **Start restrictive:** Use explicit scopes
2. **Review bypasses:** Check audit logs
3. **Test first:** Use without --yes initially
4. **Scope narrowly:** Prefer specific to general
5. **Never --yes=all:** Unless absolutely necessary

### Troubleshooting

#### Operations Still Prompting

**Problem:** --yes not working for operation

**Solutions:**
1. Check scope covers operation: `--yes=file_write`
2. Check risk level: May require explicit scope
3. Check protected list: Some can't be bypassed

#### Too Many Prompts

**Problem:** Want more automation

**Solutions:**
1. Expand scope: `--yes=file_write,terminal:safe`
2. Adjust config defaults
3. Review risk overrides

#### Bypass Not Recorded

**Problem:** Audit log missing bypasses

**Solutions:**
1. Check logging enabled
2. Check log level
3. Verify persistence working

---

## Acceptance Criteria

### Scope Syntax

- [ ] AC-001: --yes works
- [ ] AC-002: --yes=scope works
- [ ] AC-003: Comma-separated works
- [ ] AC-004: Modifiers work

### Categories

- [ ] AC-005: file_read works
- [ ] AC-006: file_write works
- [ ] AC-007: file_delete works
- [ ] AC-008: terminal works
- [ ] AC-009: config works
- [ ] AC-010: all works

### Modifiers

- [ ] AC-011: safe works
- [ ] AC-012: all works
- [ ] AC-013: pattern works

### Risk Levels

- [ ] AC-014: Level 1 implicit
- [ ] AC-015: Level 2 explicit
- [ ] AC-016: Level 3 acknowledged
- [ ] AC-017: Level 4 blocked

### Precedence

- [ ] AC-018: CLI > config
- [ ] AC-019: Config > default
- [ ] AC-020: Specific > general
- [ ] AC-021: Deny > allow

### Protection

- [ ] AC-022: .git protected
- [ ] AC-023: .agent protected
- [ ] AC-024: Custom protected

### Rate Limiting

- [ ] AC-025: Limit enforced
- [ ] AC-026: Pause works
- [ ] AC-027: Configurable

### Logging

- [ ] AC-028: Bypasses logged
- [ ] AC-029: Scope logged
- [ ] AC-030: Operation logged

### CLI

- [ ] AC-031: --yes works
- [ ] AC-032: --yes-next works
- [ ] AC-033: --no works
- [ ] AC-034: --interactive works

### Errors

- [ ] AC-035: Invalid scope error
- [ ] AC-036: Protected warning
- [ ] AC-037: Rate limit message

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Approvals/YesScope/
├── ScopeParserTests.cs
│   ├── Should_Parse_Single_Scope()
│   ├── Should_Parse_Multiple_Scopes()
│   ├── Should_Parse_Modifiers()
│   ├── Should_Reject_Invalid()
│   └── Should_Suggest_Corrections()
│
├── RiskLevelTests.cs
│   ├── Should_Classify_Low_Risk()
│   ├── Should_Classify_Medium_Risk()
│   ├── Should_Classify_High_Risk()
│   └── Should_Block_Critical()
│
└── PrecedenceTests.cs
    ├── Should_CLI_Override_Config()
    ├── Should_Config_Override_Default()
    └── Should_Deny_Override_Allow()
```

### Integration Tests

```
Tests/Integration/Approvals/YesScope/
├── ScopeApplicationTests.cs
│   ├── Should_Apply_Session_Scope()
│   ├── Should_Apply_Operation_Scope()
│   └── Should_Clear_Operation_Scope()
│
└── ProtectionTests.cs
    ├── Should_Protect_Git()
    └── Should_Protect_Config()
```

### E2E Tests

```
Tests/E2E/Approvals/YesScope/
├── YesScopingE2ETests.cs
│   ├── Should_Bypass_With_Scope()
│   ├── Should_Prompt_Without_Scope()
│   ├── Should_Block_Protected()
│   └── Should_Rate_Limit()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Scope parsing | 0.5ms | 1ms |
| Scope validation | 2ms | 5ms |
| Risk lookup | 0.1ms | 0.5ms |

---

## User Verification Steps

### Scenario 1: Basic --yes

1. Run `acode run --yes "Read all files"`
2. Agent reads files
3. Verify: No prompts for reads

### Scenario 2: Explicit Scope

1. Run `acode run --yes=file_write "Update README"`
2. Agent writes file
3. Verify: No prompt for write

### Scenario 3: Missing Scope

1. Run `acode run --yes "Update README"`
2. Agent attempts write
3. Verify: Prompt appears

### Scenario 4: Protected Operation

1. Run `acode run --yes=all "Delete .git"`
2. Agent attempts .git delete
3. Verify: Manual prompt required

### Scenario 5: Rate Limit

1. Configure low rate limit
2. Run with many bypasses
3. Verify: Pause after limit

### Scenario 6: --yes-next

1. Run without --yes
2. At prompt, enter `--yes-next file_write`
3. Verify: Only that operation approved

### Scenario 7: Invalid Scope

1. Run `acode run --yes=filwrite "Update"`
2. Verify: Error with suggestion

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Approvals/
│   ├── YesScope.cs
│   └── RiskLevel.cs
│
src/AgenticCoder.Application/
├── Approvals/
│   └── Scoping/
│       ├── IScopeParser.cs
│       ├── IScopeValidator.cs
│       └── IScopeResolver.cs
│
src/AgenticCoder.Infrastructure/
├── Approvals/
│   └── Scoping/
│       ├── ScopeParser.cs
│       ├── ScopeValidator.cs
│       └── ScopeResolver.cs
│
src/AgenticCoder.CLI/
└── Options/
    └── YesOptions.cs
```

### YesScope Value Object

```csharp
namespace AgenticCoder.Domain.Approvals;

public sealed record YesScope
{
    public IReadOnlyList<ScopeEntry> Entries { get; }
    public bool IsAll { get; }
    public bool IsNone { get; }
    
    public static YesScope Parse(string input);
    public static YesScope Default { get; }
    public static YesScope All { get; }
    public static YesScope None { get; }
    
    public bool Covers(Operation operation);
}

public sealed record ScopeEntry(
    OperationCategory Category,
    string? Modifier,
    string? Pattern);
```

### RiskLevel Enum

```csharp
namespace AgenticCoder.Domain.Approvals;

public enum RiskLevel
{
    Low = 1,      // Implicit --yes
    Medium = 2,   // Explicit scope required
    High = 3,     // Acknowledgment required
    Critical = 4  // Cannot bypass
}
```

### IScopeResolver Interface

```csharp
namespace AgenticCoder.Application.Approvals.Scoping;

public interface IScopeResolver
{
    YesScope Resolve(
        YesScope? cliScope,
        YesScope? configScope,
        YesScope defaultScope);
        
    bool CanBypass(Operation operation, YesScope scope);
    bool IsProtected(Operation operation);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-YES-001 | Invalid scope syntax |
| ACODE-YES-002 | Unknown category |
| ACODE-YES-003 | Invalid modifier |
| ACODE-YES-004 | Protected operation |
| ACODE-YES-005 | Rate limit exceeded |

### Logging Fields

```json
{
  "event": "approval_bypassed",
  "operation_category": "file_write",
  "scope_used": "file_write",
  "risk_level": 2,
  "session_id": "abc123",
  "path": "src/readme.md"
}
```

### Implementation Checklist

1. [ ] Create YesScope value object
2. [ ] Create RiskLevel enum
3. [ ] Implement scope parser
4. [ ] Implement scope validator
5. [ ] Implement scope resolver
6. [ ] Add protected operations
7. [ ] Implement rate limiting
8. [ ] Add CLI options
9. [ ] Integrate with approval gates
10. [ ] Add bypass logging
11. [ ] Write unit tests
12. [ ] Write integration tests
13. [ ] Write E2E tests

### Validation Checklist Before Merge

- [ ] Scope parsing works
- [ ] Validation catches errors
- [ ] Precedence correct
- [ ] Protection enforced
- [ ] Rate limiting works
- [ ] Logging complete
- [ ] Unit test coverage > 90%

### Rollout Plan

1. **Phase 1:** Domain types
2. **Phase 2:** Parser/validator
3. **Phase 3:** Resolver
4. **Phase 4:** Protection
5. **Phase 5:** Rate limiting
6. **Phase 6:** CLI integration
7. **Phase 7:** Logging
8. **Phase 8:** Configuration

---

**End of Task 013.c Specification**