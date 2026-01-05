# Task 013.c: --yes Scoping Rules

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 013 (Human Approval Gates), Task 013.a (Rules/Prompts), Task 013.b (Persistence)  

---

## Description

Task 013.c implements the `--yes` flag scoping system—a carefully designed automation feature that allows experienced users to bypass approval prompts while maintaining strong safety guardrails. The scoping system transforms `--yes` from a dangerous all-or-nothing flag into a precise, auditable automation tool that balances productivity with safety.

### Business Value and ROI

**Quantified Benefits:**

1. **Automation Time Savings: $125,000/year**
   - Without scoped --yes: Every CI/CD run requires interactive approval or unsafe `--yes`
   - With scoped --yes: Safe operations auto-approve, dangerous ones blocked
   - Average CI/CD session: 50 prompts × 3 seconds = 150 seconds of prompting
   - With `--yes=file_write:*.test.ts,terminal:npm`: 2 prompts × 3 seconds = 6 seconds
   - Time savings per session: 144 seconds
   - Sessions per day: 100 (CI + developer automation)
   - 144 seconds × 100 sessions × 250 days = 1,000 hours/year
   - 1,000 hours × $125/hour = **$125,000/year**

2. **Prevented Automation Accidents: $80,000/year**
   - Unscoped `--yes` in automation: ~4 incidents/year (file deletions, bad commits)
   - Average incident cost: $20,000 (recovery, debugging, downtime)
   - With scoped --yes: 0 incidents (dangerous ops still blocked)
   - Savings: 4 × $20,000 = **$80,000/year**

3. **Reduced Context Switching: $45,000/year**
   - With prompts in automation: Developers monitor pipelines for approval
   - Average monitoring time: 15 minutes/day/developer
   - With scoped --yes: No monitoring needed
   - 15 minutes × 250 days × 8 developers × $60/hour = **$45,000/year**

4. **Compliance Confidence: $30,000/year**
   - Auditors concerned about --yes bypass: "How do you ensure nothing dangerous auto-approves?"
   - With scoping: Clear documentation of what can/cannot bypass
   - Audit findings reduced: 2/year → 0/year
   - 2 findings × $15,000 remediation = **$30,000/year** avoided

**Total ROI: $280,000/year for a 10-person team with CI/CD automation**

### Technical Architecture

#### Scope Evaluation Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     --yes Scope Evaluation Pipeline                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Operation    ┌───────────────┐    ┌───────────────┐    Decision        │
│  ─────────────│  Scope        │────│  Risk Level   │─────────────▶      │
│               │  Parser       │    │  Evaluator    │                    │
│               └───────┬───────┘    └───────┬───────┘                    │
│                       │                    │                            │
│                       ▼                    ▼                            │
│               ┌───────────────┐    ┌───────────────┐                    │
│               │  Command Line │    │  Level 1-4    │                    │
│               │  --yes=scope  │    │  Classification│                   │
│               └───────────────┘    └───────────────┘                    │
│                       │                    │                            │
│                       ▼                    ▼                            │
│               ┌───────────────────────────────────────┐                 │
│               │         Scope Matcher                  │                │
│               │  - Check operation against scope       │                │
│               │  - Check risk level allows bypass      │                │
│               │  - Check no deny rule overrides        │                │
│               │  - Check rate limits not exceeded      │                │
│               └───────────────────────────────────────┘                 │
│                       │                                                  │
│                       ▼                                                  │
│               ┌─────────────────┐                                        │
│               │ AUTO_APPROVE or │                                        │
│               │ PROMPT          │                                        │
│               └─────────────────┘                                        │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Scope Syntax Specification

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      Scope Syntax Grammar                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Scope Specification:                                                    │
│  ────────────────────                                                    │
│  --yes[=scope_list]                                                      │
│  --yes-next[=scope_list]                                                 │
│  --yes-exclude=scope_list                                                │
│                                                                          │
│  Scope List:                                                             │
│  ───────────                                                             │
│  scope_list := scope (',' scope)*                                        │
│  scope      := category [':' modifier] [':' pattern]                     │
│                                                                          │
│  Categories:                                                             │
│  ───────────                                                             │
│  file_read     - Reading files (Risk Level 1)                           │
│  file_write    - Creating/modifying files (Risk Level 2)                │
│  file_delete   - Removing files (Risk Level 3)                          │
│  dir_create    - Creating directories (Risk Level 1)                    │
│  dir_delete    - Removing directories (Risk Level 3)                    │
│  terminal      - Running shell commands (Risk Level 2-4)                │
│  terminal:safe - Only whitelisted commands (Risk Level 2)               │
│  config        - Modifying config files (Risk Level 3)                  │
│  all           - Everything (Risk Level 4, requires ack)                │
│                                                                          │
│  Modifiers:                                                              │
│  ──────────                                                              │
│  :safe         - Only operations marked safe                            │
│  :test         - Only in test directories                               │
│  :generated    - Only in generated directories                          │
│  :pattern      - Custom glob pattern follows                            │
│                                                                          │
│  Examples:                                                               │
│  ─────────                                                               │
│  --yes                        # Default: file_read, dir_create only     │
│  --yes=file_write             # Add file writes                         │
│  --yes=file_write:*.test.ts   # Only test file writes                   │
│  --yes=terminal:safe          # Only whitelisted commands               │
│  --yes=file_write,terminal:safe  # Combined scopes                      │
│  --yes=all --ack-danger       # Everything (explicit danger ack)        │
│  --yes-next=file_delete       # One-time scope                          │
│  --yes-exclude=file_delete    # Exclude from default scopes             │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Risk Level System

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Risk Level Classification                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Level 1 (Low Risk) - Default --yes approved                            │
│  ──────────────────────────────────────────────                         │
│  - file_read: Reading any file                                          │
│  - dir_create: Creating directories                                     │
│  - dir_list: Listing directory contents                                 │
│  Rationale: Read-only operations, no data loss possible                 │
│                                                                          │
│  Level 2 (Medium Risk) - Requires explicit scope                        │
│  ──────────────────────────────────────────────                         │
│  - file_write: Creating/modifying files                                 │
│  - terminal:safe: Whitelisted shell commands                            │
│  - git:status: Git informational commands                               │
│  Rationale: Can modify state but typically reversible                   │
│                                                                          │
│  Level 3 (High Risk) - Requires explicit scope + warning                │
│  ──────────────────────────────────────────────────────                 │
│  - file_delete: Removing files                                          │
│  - dir_delete: Removing directories                                     │
│  - config: Modifying configuration                                      │
│  - git:commit: Git state-changing commands                              │
│  - terminal:* (non-safe): Arbitrary shell commands                      │
│  Rationale: Can cause data loss or system state changes                 │
│                                                                          │
│  Level 4 (Critical) - Cannot use --yes, always prompt                   │
│  ──────────────────────────────────────────────────                     │
│  - file_delete:.git/** - Deleting git internals                         │
│  - file_delete:.env* - Deleting environment files                       │
│  - terminal:rm -rf - Recursive force delete                             │
│  - terminal:git push --force - Force push                               │
│  Rationale: Potentially catastrophic, unrecoverable operations          │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Integration Points

#### Integration with Task 013 (Human Approval Gates)
- Gate framework queries scope system before prompting
- If scope allows, bypasses prompt entirely
- Decision recorded as AUTO_APPROVED with scope reference

#### Integration with Task 013.a (Gate Rules/Prompts)
- Rules define risk levels for operations
- Scopes override rules for matching operations
- Deny rules still block even with --yes

#### Integration with Task 013.b (Persistence)
- All --yes bypasses recorded in audit trail
- Records include: scope used, operation matched, risk level

### Design Decisions and Trade-offs

**Decision 1: Opt-in vs Opt-out Scoping**
- Default --yes is minimal (Level 1 only)
- Users must explicitly expand scope
- Trade-off: More typing for automation, but safer defaults

**Decision 2: Cannot --yes Level 4 Operations**
- Critical operations always prompt regardless of --yes
- No override mechanism exists
- Trade-off: Slightly inconvenient, but prevents catastrophic accidents

**Decision 3: Deny Always Wins**
- If any rule denies, --yes cannot override
- Policy layer > automation layer
- Trade-off: Automation may be blocked, but safety guaranteed

**Decision 4: Session vs Operation Scope**
- `--yes` applies to whole session
- `--yes-next` applies to next operation only
- Trade-off: Complexity, but enables fine-grained control

### Constraints and Limitations

**Technical Constraints:**
- Maximum scope list: 20 items
- Pattern complexity limit: 100 characters
- No regex in scope patterns (glob only)

**Operational Constraints:**
- Rate limit: 100 --yes bypasses per minute
- Cooldown after hitting rate limit: 60 seconds
- No persistent scope storage (session only)

**Safety Constraints:**
- Level 4 operations never bypassable
- `--yes=all` requires `--ack-danger` flag
- Protected paths never bypassable

### Performance Characteristics

- Scope parsing: < 1ms per specification
- Scope matching: < 0.5ms per operation
- Risk level lookup: O(1) from cache
- No network calls in scope evaluation

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

## Use Cases

### Use Case 1: Mike the CI/CD Engineer

**Persona:** Mike Chen, DevOps Engineer responsible for CI/CD pipelines that use Acode to automate code generation, testing, and deployment preparation. His pipelines run hundreds of times per day and must be fully automated.

**Before Acode with --yes Scoping:**
Mike's CI/CD pipeline uses Acode for automated test generation. Without `--yes`, pipelines hang waiting for approval. With unscoped `--yes`, everything auto-approves—including a job that once accidentally deleted the build directory. Mike is stuck between broken automation and dangerous automation.

**After Acode with --yes Scoping:**
Mike configures precisely scoped automation:

```bash
# In CI/CD pipeline
acode run "Generate tests for new endpoints" \
  --yes=file_write:*.test.ts,file_read,terminal:safe

# Result:
# ✓ file_read any file - AUTO (Level 1)
# ✓ file_write *.test.ts - AUTO (scope match)
# ✗ file_write src/api.ts - PROMPT (not in scope)
# ✓ terminal: npm test - AUTO (whitelisted)
# ✗ terminal: rm -rf build/ - BLOCKED (Level 4, cannot bypass)
```

Safe operations auto-approve, dangerous ones are blocked, and the pipeline never deletes important directories.

**Measurable Improvement:**
- Pipeline execution time: 15 minutes → 8 minutes (47% faster)
- Automation incidents: 2/quarter → 0/quarter
- Developer on-call interrupts for pipeline approvals: 20/week → 0/week
- Annual value: **$95,000** (time + incident prevention)

---

### Use Case 2: Lisa the Power User

**Persona:** Lisa Park, Staff Engineer who uses Acode extensively for rapid prototyping and refactoring. She's very comfortable with the tool and finds constant approval prompts disruptive to her flow state.

**Before Acode with --yes Scoping:**
Lisa uses Acode for 6+ hours daily. Without `--yes`, she approves 150+ prompts per day. She starts using `--yes` everywhere, which works great until she accidentally auto-approves a deletion of a migration file she needed to keep. Recovery takes 2 hours.

**After Acode with --yes Scoping:**
Lisa configures her workflow with graduated trust:

```bash
# Interactive development (high trust)
acode run "Refactor authentication module" --yes=file_write,file_read

# Operations Lisa sees during session:
# ✓ Read src/auth/*.ts - AUTO
# ✓ Write src/auth/login.ts - AUTO
# ⚠ Delete src/auth/legacy.ts - PROMPT (Level 3, not in scope)
# ✓ Write tests/auth/*.test.ts - AUTO

# She can expand scope mid-session when needed:
# > Approval required: Delete src/auth/legacy.ts
# > [A]pprove [D]eny [Y]es-rest (add file_delete to scope)
#
# Lisa presses 'Y' to auto-approve remaining deletions in this session
```

Lisa maintains flow for safe operations while dangerous ones still pause for confirmation.

**Measurable Improvement:**
- Prompts per day: 150 → 25 (83% reduction)
- Flow state interruptions: 50/day → 10/day
- Accidents from blind approval: 2/month → 0/month
- Developer satisfaction: "I can work fast AND safe"
- Annual productivity value: **$35,000** (recovered flow time)

---

### Use Case 3: Omar the Cautious Junior Developer

**Persona:** Omar Rodriguez, Junior Developer in his first month at the company. He's still learning the codebase and wants to use Acode but is nervous about accidentally breaking things.

**Before Acode with --yes Scoping:**
Omar is afraid to use Acode's `--yes` flag at all because he's heard horror stories. This means every operation prompts him, which is actually good for learning but very slow. Some senior developers tell him to "just use --yes, it's fine"—but he's hesitant.

**After Acode with --yes Scoping:**
Omar uses scoping to create a safe learning environment:

```bash
# Omar's cautious configuration
acode run "Add validation to user form" --yes=file_read

# This means:
# ✓ Read any file - AUTO (can't break anything)
# ⚠ Write anything - PROMPT (Omar reviews each write)
# ⚠ Delete anything - PROMPT (Omar reviews each delete)
# ⚠ Terminal commands - PROMPT (Omar reviews each command)

# As Omar gains confidence, he gradually expands:
acode run "Add tests" --yes=file_read,file_write:*.test.ts

# Later, with mentor approval:
acode run "Refactor utils" --yes=file_read,file_write:src/utils/**
```

Omar learns by reviewing prompts for dangerous operations while not being overwhelmed by safe ones.

**Measurable Improvement:**
- Onboarding time: 4 weeks → 2.5 weeks (graduated autonomy)
- Junior developer accidents: 3 in first month → 0 (prompts catch mistakes)
- Mentor intervention time: 10 hours → 4 hours (fewer mistakes to fix)
- Junior developer confidence: "I can use this safely"
- Annual value: **$15,000** (faster onboarding, fewer accidents)

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

## Security Considerations

### Threat 1: Scope Injection via Command Line Arguments

**Risk Level:** High
**CVSS Score:** 7.5 (High)
**Attack Vector:** Command injection

**Description:**
An attacker could craft malicious input that expands a narrow scope into a broad one. By exploiting shell expansion, environment variables, or scope parsing vulnerabilities, `--yes=file_read` could become `--yes=all`.

**Attack Scenario:**
1. Script constructs scope from user input: `--yes=$USER_SCOPE`
2. Attacker sets `USER_SCOPE="file_read,all --ack-danger"`
3. Shell splits arguments, adding `--ack-danger` flag
4. `--yes=all` now active, all operations auto-approve

**Mitigation:** Scope argument is parsed as a single string value, not subject to shell expansion after initial argument parsing. The `ScopeParser` validates syntax strictly and rejects any scope containing spaces or shell metacharacters. The `--ack-danger` flag requires separate interactive confirmation, not a command-line value.

---

### Threat 2: Risk Level Downgrade Attack

**Risk Level:** High
**CVSS Score:** 7.8 (High)
**Attack Vector:** Configuration manipulation

**Description:**
An attacker could modify the risk level configuration to downgrade dangerous operations from Level 4 (never bypass) to Level 1 (default bypass). This would allow `--yes` to approve previously protected operations.

**Attack Scenario:**
1. Attacker gains write access to configuration
2. Modifies risk level: `file_delete:.git/** → Level 1`
3. User runs `acode --yes` (innocent intent)
4. `.git/` deletion auto-approved (catastrophic)

**Mitigation:** Level 4 classifications are hardcoded, not configurable. The `HardcodedCriticalOperations` list defines operations that can never be downgraded regardless of configuration. Any configuration attempting to modify Level 4 operations is rejected with a security warning.

---

### Threat 3: Scope Exhaustion via Pattern Complexity

**Risk Level:** Medium
**CVSS Score:** 5.5 (Medium)
**Attack Vector:** Resource exhaustion

**Description:**
An attacker could craft complex scope patterns that cause exponential matching time. A carefully constructed glob pattern like `**/**/**/**/*.ts` could cause each operation check to take seconds, effectively freezing Acode.

**Attack Scenario:**
1. Attacker provides scope: `--yes=file_write:**/**/**/**/**/*.ts`
2. For each file write, pattern matching takes 5+ seconds
3. Session becomes unusably slow
4. User forced to kill process

**Mitigation:** Scope patterns are validated for complexity before use. Maximum pattern depth (number of `**` segments) is limited to 3. Pattern matching uses a timeout of 100ms per operation. The `ScopePatternValidator` rejects patterns that could cause performance issues.

---

### Threat 4: Bypass via Operation Misclassification

**Risk Level:** Medium
**CVSS Score:** 6.1 (Medium)
**Attack Vector:** Logic manipulation

**Description:**
If operations are not correctly classified, a dangerous operation might match a safe scope. For example, if `git push --force` is misclassified as `git:status` (Level 2), it could auto-approve under `--yes=terminal:safe`.

**Attack Scenario:**
1. Bug in operation classifier misidentifies commands
2. `git push --force` classified as git informational
3. User runs `--yes=terminal:safe`
4. Force push auto-approved, remote history rewritten

**Mitigation:** Operation classification uses strict pattern matching with explicit deny lists. The `OperationClassifier` has a hardcoded `DangerousCommandPatterns` list that takes precedence over general classification. All terminal commands are cross-checked against this list before any scope matching.

---

### Threat 5: Scope Persistence Leading to Unintended Bypass

**Risk Level:** Low
**CVSS Score:** 4.0 (Medium)
**Attack Vector:** State confusion

**Description:**
If scopes persist between sessions unexpectedly, a broad scope used for one task could remain active for a sensitive task. The user thinks they're running with default scopes but actually has `--yes=all` active from yesterday.

**Mitigation:** Scopes are strictly session-scoped and never persisted. The `ScopeManager` initializes with empty scope each session. The `--yes` flag must be explicitly provided on each command. No configuration option exists to set default scopes (intentional design to prevent this attack).

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