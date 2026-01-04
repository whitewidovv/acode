# Task 013: Human Approval Gates

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 21 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 012 (Multi-Stage Loop), Task 012.b (Executor), Task 010 (CLI Framework)  

---

## Description

Task 013 implements human approval gates—the checkpoints where Acode pauses to request user confirmation before proceeding with potentially impactful operations. Approval gates are the primary mechanism for maintaining human control over agentic execution. They ensure the user remains in the loop for consequential decisions.

Approval gates exist because autonomous agents make mistakes. An LLM might misunderstand the request, generate incorrect code, or attempt an operation the user didn't intend. Without approval gates, these mistakes could result in deleted files, corrupted code, or other harm. Gates provide a safety net—a chance for humans to review and approve before actions become permanent.

The approval model is flexible and configurable. Different operations have different risk levels. File reads are low-risk and typically auto-approved. File writes are medium-risk and may require approval. File deletions are high-risk and often require approval. Terminal commands vary by command. Users configure policies to match their comfort level.

Gate triggers are determined by rules (Task 013.a). Rules match operations against patterns: file paths, command types, operation categories. When a rule matches, the associated policy applies: auto-approve, prompt, or deny. Rules are evaluated in order, with first match wins.

The approval prompt is a CLI interaction (Task 010). It shows the operation details, context, and options. Users can approve, deny, view more details, or skip. The prompt is informative but not overwhelming—users should be able to make quick decisions for routine operations while having full details available for complex ones.

Approval decisions are persisted (Task 013.b). This provides an audit trail of what was approved and when. It also enables patterns: if a user consistently approves a certain operation, perhaps the policy should be adjusted. Persisted decisions support analysis and policy refinement.

The `--yes` flag and scoping rules (Task 013.c) enable automation. In non-interactive contexts (CI/CD, scripts), prompts are impossible. The `--yes` flag auto-approves operations, but with configurable scope. Some operations might be excluded from auto-approval even with `--yes`. This balances automation needs with safety requirements.

Approval gates integrate with the state machine (Task 011). When an operation requires approval, the session transitions to AWAITING_APPROVAL state. The session is paused, the prompt is shown, and the response is awaited. Upon approval, execution resumes. Upon denial, the step is skipped or the session is paused.

Timeout handling ensures gates don't block indefinitely. In interactive mode, a timeout may prompt again or escalate. In non-interactive mode, timeout triggers the configured policy (typically deny or skip). No operation should wait forever for approval that never comes.

Security is paramount. Approval gates must not be bypassable through clever input or state manipulation. The approval requirement is enforced at the operation level, not just the UI level. Even if the CLI is bypassed, the underlying service enforces approval requirements.

Observability tracks all approval interactions. Every gate trigger, prompt shown, response received, and timeout is logged. This enables auditing and debugging. Users can review what was approved, what was denied, and when.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Approval Gate | Checkpoint requiring user confirmation |
| Gate Trigger | Condition that activates a gate |
| Approval Policy | Rule for handling operations |
| Auto-Approve | Proceed without prompting |
| Prompt | Request user decision |
| Deny | Block operation |
| Skip | Skip but continue session |
| Audit Trail | Record of decisions |
| --yes Flag | Auto-approve CLI option |
| Scope | What --yes applies to |
| Operation | Action requiring potential approval |
| Risk Level | Categorization of potential harm |
| Timeout | Maximum wait for response |
| Escalation | Action when timeout occurs |
| Policy Precedence | Order of rule evaluation |

---

## Out of Scope

The following items are explicitly excluded from Task 013:

- **Gate rule definition** - Task 013.a
- **Decision persistence** - Task 013.b
- **--yes scoping** - Task 013.c
- **UI-based approvals** - CLI only
- **Remote approvals** - Local only
- **Multi-user approval** - Single user
- **Approval workflows** - Simple approve/deny
- **Time-limited approvals** - Immediate only
- **Approval templates** - Per-operation only
- **External approval services** - Local only

---

## Assumptions

### Technical Assumptions

- ASM-001: Approval gates integrate at operation level (before tool execution)
- ASM-002: Gate decisions are synchronous - execution waits for response
- ASM-003: stdin is available for interactive approval input
- ASM-004: Approval prompts can display operation context clearly
- ASM-005: Gate framework supports multiple operation types

### Behavioral Assumptions

- ASM-006: Users make informed decisions based on displayed context
- ASM-007: Approve/Deny are the only valid responses (no partial approval)
- ASM-008: Denial halts the current operation but not the entire session
- ASM-009: Timeout on approval request results in implicit denial
- ASM-010: Gates can be bypassed only via explicit --yes flag

### Dependency Assumptions

- ASM-011: Task 012.b executor triggers approval gates
- ASM-012: Task 013.a provides rule definitions for when gates apply
- ASM-013: Task 013.b persists approval decisions
- ASM-014: Task 010 CLI provides interactive prompt primitives

### Safety Assumptions

- ASM-015: High-risk operations always require approval by default
- ASM-016: File deletion, command execution are high-risk categories
- ASM-017: Users understand implications of approval decisions

---

## Functional Requirements

### Gate Architecture

- FR-001: Gates MUST intercept operations before execution
- FR-002: Gates MUST evaluate approval policies
- FR-003: Gates MUST pause execution when approval required
- FR-004: Gates MUST resume execution on approval
- FR-005: Gates MUST enforce denial

### Operation Categories

- FR-006: FILE_READ category
- FR-007: FILE_WRITE category
- FR-008: FILE_DELETE category
- FR-009: DIRECTORY_CREATE category
- FR-010: TERMINAL_COMMAND category
- FR-011: EXTERNAL_REQUEST category

### Approval Policies

- FR-012: AUTO_APPROVE policy - proceed without prompt
- FR-013: PROMPT policy - require user decision
- FR-014: DENY policy - block always
- FR-015: SKIP policy - skip without blocking

### Policy Evaluation

- FR-016: Policies MUST be evaluated per operation
- FR-017: Rules MUST be evaluated in order
- FR-018: First matching rule MUST apply
- FR-019: Default policy MUST exist
- FR-020: Evaluation MUST be logged

### Approval Prompt

- FR-021: Prompt MUST show operation type
- FR-022: Prompt MUST show operation details
- FR-023: Prompt MUST show available options
- FR-024: Prompt MUST accept user input
- FR-025: Prompt MUST validate input

### Prompt Options

- FR-026: [A]pprove - proceed with operation
- FR-027: [D]eny - block operation
- FR-028: [S]kip - skip this operation
- FR-029: [V]iew - show more details
- FR-030: [?] - show help

### Operation Details

- FR-031: File operations show path
- FR-032: File writes show content preview
- FR-033: File deletes show file info
- FR-034: Terminal commands show command
- FR-035: Details MUST be readable

### Content Preview

- FR-036: Preview MUST be limited (50 lines default)
- FR-037: Full content MUST be viewable
- FR-038: Binary files MUST show type only
- FR-039: Secrets MUST be redacted

### Approval Response

- FR-040: Response MUST be captured
- FR-041: Response MUST be validated
- FR-042: Invalid response MUST re-prompt
- FR-043: Response MUST update gate state

### Gate State

- FR-044: PENDING - awaiting response
- FR-045: APPROVED - proceed
- FR-046: DENIED - block
- FR-047: SKIPPED - skip
- FR-048: TIMEOUT - time limit reached

### State Machine Integration

- FR-049: Approval required MUST transition to AWAITING_APPROVAL
- FR-050: AWAITING_APPROVAL MUST pause execution
- FR-051: Approval MUST resume execution
- FR-052: Denial MUST handle gracefully

### Timeout Handling

- FR-053: Timeout MUST be configurable
- FR-054: Default timeout: 5 minutes
- FR-055: Timeout MUST trigger policy
- FR-056: Timeout policy configurable (deny/skip/escalate)

### Non-Interactive Mode

- FR-057: Non-interactive MUST detect (stdin not TTY)
- FR-058: Non-interactive MUST not prompt
- FR-059: Non-interactive MUST use configured policy
- FR-060: Default non-interactive policy: DENY

### --yes Flag

- FR-061: --yes MUST auto-approve eligible operations
- FR-062: Scope MUST be configurable
- FR-063: Some operations MAY be excluded from --yes
- FR-064: --yes MUST be logged

### Logging

- FR-065: Gate triggers MUST be logged
- FR-066: Prompts MUST be logged
- FR-067: Responses MUST be logged
- FR-068: Timeouts MUST be logged
- FR-069: Policy decisions MUST be logged

### Security

- FR-070: Gates MUST be enforced at service level
- FR-071: CLI bypass MUST NOT bypass gates
- FR-072: Approval state MUST be tamper-resistant
- FR-073: Audit trail MUST be complete

---

## Non-Functional Requirements

### Performance

- NFR-001: Policy evaluation < 10ms
- NFR-002: Prompt display < 100ms
- NFR-003: No blocking on non-prompted operations

### Reliability

- NFR-004: Gates MUST never be bypassed accidentally
- NFR-005: Crash during prompt MUST preserve state
- NFR-006: Resume MUST restore prompt state

### Usability

- NFR-007: Prompts MUST be clear
- NFR-008: Options MUST be discoverable
- NFR-009: Details MUST be accessible

### Security

- NFR-010: No unauthorized approval bypass
- NFR-011: Complete audit trail
- NFR-012: Secrets never shown in prompts

### Observability

- NFR-013: All decisions logged
- NFR-014: Timing tracked
- NFR-015: Patterns analyzable

---

## User Manual Documentation

### Overview

Human approval gates ensure you stay in control of what Acode does to your codebase. Before making changes, Acode can pause and ask for your confirmation.

### Quick Start

By default, Acode prompts for potentially impactful operations:

```bash
$ acode run "Refactor login component"

[EXECUTOR] Step 3: Write LoginComponent.tsx

⚠ Approval Required
─────────────────────────────────────
Operation: WRITE FILE
Path: src/components/LoginComponent.tsx
Size: 156 lines

Preview:
  1  | import React from 'react';
  2  | import { useAuth } from '../hooks';
  3  | 
  4  | export const LoginComponent: React.FC = () => {
  ...

[A]pprove  [D]eny  [S]kip  [V]iew all  [?]Help

Choice: a

✓ Approved. Writing file...
```

### Approval Options

| Key | Action | Description |
|-----|--------|-------------|
| A | Approve | Proceed with operation |
| D | Deny | Block operation, skip step |
| S | Skip | Skip this operation |
| V | View | Show full content |
| ? | Help | Show help text |

### Operation Categories

| Category | Default Policy | Examples |
|----------|---------------|----------|
| FILE_READ | Auto | Reading source files |
| FILE_WRITE | Prompt | Creating/modifying files |
| FILE_DELETE | Prompt | Removing files |
| DIR_CREATE | Auto | Creating directories |
| TERMINAL | Prompt | Running commands |

### Configuration

```yaml
# .agent/config.yml
approvals:
  # Default policy when no rule matches
  default_policy: prompt
  
  # Timeout for prompts (seconds)
  timeout_seconds: 300
  
  # Action on timeout
  timeout_action: deny  # deny | skip | escalate
  
  # Category-level policies
  policies:
    file_read: auto
    file_write: prompt
    file_delete: prompt
    directory_create: auto
    terminal_command: prompt
    
  # Non-interactive mode policy
  non_interactive_policy: deny
```

### Auto-Approve with --yes

For automation, use `--yes` to auto-approve:

```bash
# Auto-approve all eligible operations
$ acode run "Add tests" --yes

# --yes with scope limit
$ acode run "Add tests" --yes=write

# --yes excluding deletes
$ acode run "Cleanup" --yes --yes-exclude=delete
```

### Rule-Based Policies

Fine-grained control via rules:

```yaml
# .agent/config.yml
approvals:
  rules:
    # Auto-approve test file writes
    - pattern: "**/*.test.ts"
      operation: file_write
      policy: auto
      
    # Always prompt for config changes
    - pattern: "**/*.config.*"
      operation: file_write
      policy: prompt
      
    # Deny deletion of source files
    - pattern: "src/**"
      operation: file_delete
      policy: deny
      
    # Auto-approve npm commands
    - command: "npm *"
      operation: terminal_command
      policy: auto
```

### Viewing Details

Press V to see full operation details:

```
[V]iew selected

Full File Content:
═══════════════════════════════════════════════════════
  1  | import React from 'react';
  2  | import { useAuth } from '../hooks';
  3  | import { Button, Input } from '../ui';
  ...
 156 | export default LoginComponent;
═══════════════════════════════════════════════════════

Existing file will be REPLACED
Current size: 142 lines
New size: 156 lines

Press any key to return to prompt...
```

### Terminal Command Approval

Commands show what will be executed:

```
⚠ Approval Required
─────────────────────────────────────
Operation: TERMINAL COMMAND
Command: npm test -- --coverage
Working Dir: /project

This command will:
  - Run test suite
  - Generate coverage report

[A]pprove  [D]eny  [S]kip  [V]iew  [?]Help
```

### Non-Interactive Mode

When stdin is not a TTY:

```bash
# Fails by default (can't prompt)
$ echo "task" | acode run -

Error: Approval required but running non-interactively.
Use --yes to auto-approve or configure non_interactive_policy.

# Auto-approve in CI
$ acode run "Add feature" --yes
```

### Timeout Handling

If you don't respond:

```
⚠ Approval Required
...
[A]pprove  [D]eny  [S]kip  [V]iew  [?]Help

Waiting for response (timeout in 5:00)...

[4:45 remaining] ...
[4:30 remaining] ...

⚠ Timeout reached - Operation DENIED

[EXECUTOR] Step skipped due to approval timeout
```

### Approval History

View past decisions:

```bash
$ acode approvals history

Session  Operation    Path/Command             Decision  Time
abc123   FILE_WRITE   src/Login.tsx            APPROVED  2m ago
abc123   TERMINAL     npm test                 APPROVED  1m ago
def456   FILE_DELETE  src/legacy/old.ts        DENIED    1h ago
```

### Troubleshooting

#### Can't Approve in CI

**Problem:** Prompts hang in CI/CD

**Solution:**
Use `--yes` flag: `acode run "task" --yes`

#### Timeout Too Short

**Problem:** Timeouts before you can review

**Solution:**
Increase timeout: `approvals.timeout_seconds: 600`

#### Too Many Prompts

**Problem:** Constantly prompted for routine operations

**Solution:**
Configure auto-approve rules for trusted patterns

#### Accidentally Denied

**Problem:** Denied important operation

**Solution:**
Use `acode resume` - denied steps can be retried

---

## Acceptance Criteria

### Gate Architecture

- [ ] AC-001: Intercepts operations
- [ ] AC-002: Evaluates policies
- [ ] AC-003: Pauses when required
- [ ] AC-004: Resumes on approval
- [ ] AC-005: Enforces denial

### Categories

- [ ] AC-006: FILE_READ works
- [ ] AC-007: FILE_WRITE works
- [ ] AC-008: FILE_DELETE works
- [ ] AC-009: DIRECTORY_CREATE works
- [ ] AC-010: TERMINAL_COMMAND works

### Policies

- [ ] AC-011: AUTO_APPROVE works
- [ ] AC-012: PROMPT works
- [ ] AC-013: DENY works
- [ ] AC-014: SKIP works

### Evaluation

- [ ] AC-015: Per-operation evaluation
- [ ] AC-016: Order respected
- [ ] AC-017: First match wins
- [ ] AC-018: Default applies

### Prompt

- [ ] AC-019: Shows operation type
- [ ] AC-020: Shows details
- [ ] AC-021: Shows options
- [ ] AC-022: Accepts input
- [ ] AC-023: Validates input

### Options

- [ ] AC-024: Approve works
- [ ] AC-025: Deny works
- [ ] AC-026: Skip works
- [ ] AC-027: View works
- [ ] AC-028: Help works

### Details

- [ ] AC-029: Path shown
- [ ] AC-030: Preview shown
- [ ] AC-031: Binary handled
- [ ] AC-032: Secrets redacted

### State

- [ ] AC-033: AWAITING_APPROVAL transition
- [ ] AC-034: Pause works
- [ ] AC-035: Resume works
- [ ] AC-036: Denial handled

### Timeout

- [ ] AC-037: Configurable
- [ ] AC-038: Triggers policy
- [ ] AC-039: Logged

### Non-Interactive

- [ ] AC-040: Detected
- [ ] AC-041: No prompt
- [ ] AC-042: Policy applies

### --yes

- [ ] AC-043: Auto-approves
- [ ] AC-044: Scope works
- [ ] AC-045: Exclusions work
- [ ] AC-046: Logged

### Logging

- [ ] AC-047: Triggers logged
- [ ] AC-048: Prompts logged
- [ ] AC-049: Responses logged
- [ ] AC-050: Complete audit

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Approvals/
├── ApprovalGateTests.cs
│   ├── Should_Intercept_Operations()
│   ├── Should_Evaluate_Policies()
│   ├── Should_Pause_On_Prompt()
│   └── Should_Enforce_Denial()
│
├── PolicyEvaluatorTests.cs
│   ├── Should_Evaluate_In_Order()
│   ├── Should_Use_First_Match()
│   └── Should_Use_Default()
│
├── PromptTests.cs
│   ├── Should_Show_Operation_Type()
│   ├── Should_Accept_Valid_Input()
│   └── Should_Reject_Invalid_Input()
│
└── TimeoutTests.cs
    ├── Should_Timeout_After_Config()
    └── Should_Apply_Timeout_Policy()
```

### Integration Tests

```
Tests/Integration/Approvals/
├── GateIntegrationTests.cs
│   ├── Should_Gate_File_Writes()
│   ├── Should_Gate_Terminal_Commands()
│   └── Should_Resume_After_Approval()
│
└── StateMachineIntegrationTests.cs
    ├── Should_Transition_To_Awaiting()
    └── Should_Persist_Approval_State()
```

### E2E Tests

```
Tests/E2E/Approvals/
├── ApprovalE2ETests.cs
│   ├── Should_Prompt_For_Write()
│   ├── Should_Auto_Approve_With_Yes()
│   └── Should_Handle_Denial()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Policy evaluation | 5ms | 10ms |
| Prompt display | 50ms | 100ms |
| Response processing | 10ms | 50ms |

---

## User Verification Steps

### Scenario 1: File Write Prompt

1. Run task that writes file
2. Observe: Prompt shown
3. Press A to approve
4. Verify: File written

### Scenario 2: Deny Operation

1. Run task that writes file
2. Observe: Prompt shown
3. Press D to deny
4. Verify: File NOT written
5. Verify: Step skipped

### Scenario 3: View Details

1. Trigger approval prompt
2. Press V
3. Verify: Full content shown
4. Press any key
5. Verify: Return to prompt

### Scenario 4: Auto-Approve

1. Configure policy: auto
2. Run task with matching operation
3. Verify: No prompt
4. Verify: Operation proceeds

### Scenario 5: --yes Flag

1. Run `acode run "task" --yes`
2. Verify: No prompts
3. Verify: Operations proceed

### Scenario 6: Timeout

1. Configure short timeout
2. Trigger prompt
3. Wait for timeout
4. Verify: Timeout action applies

### Scenario 7: Non-Interactive

1. Run without TTY
2. Verify: No prompt shown
3. Verify: Policy applied

### Scenario 8: Rule Matching

1. Configure specific rule
2. Run matching operation
3. Verify: Rule policy applies

### Scenario 9: Secret Redaction

1. Trigger prompt for file with secrets
2. Verify: Secrets redacted in preview

### Scenario 10: Resume After Denial

1. Deny operation
2. Resume session
3. Verify: Re-prompted

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/
├── Approvals/
│   ├── IApprovalGate.cs
│   ├── ApprovalGate.cs
│   ├── PolicyEvaluator.cs
│   ├── ApprovalContext.cs
│   └── Policies/
│       ├── IApprovalPolicy.cs
│       ├── AutoApprovePolicy.cs
│       ├── PromptPolicy.cs
│       └── DenyPolicy.cs
│
src/AgenticCoder.CLI/
├── Prompts/
│   ├── IApprovalPrompt.cs
│   ├── ApprovalPrompt.cs
│   └── OperationPreview.cs
```

### IApprovalGate Interface

```csharp
namespace AgenticCoder.Application.Approvals;

public interface IApprovalGate
{
    Task<ApprovalResult> RequestApprovalAsync(
        Operation operation,
        ApprovalOptions options,
        CancellationToken ct);
}

public sealed record ApprovalResult(
    ApprovalDecision Decision,
    string? Reason,
    DateTimeOffset DecidedAt);

public enum ApprovalDecision
{
    Approved,
    Denied,
    Skipped,
    Timeout
}
```

### Operation Record

```csharp
namespace AgenticCoder.Application.Approvals;

public sealed record Operation(
    OperationCategory Category,
    string Description,
    IReadOnlyDictionary<string, object> Details);

public enum OperationCategory
{
    FileRead,
    FileWrite,
    FileDelete,
    DirectoryCreate,
    TerminalCommand,
    ExternalRequest
}
```

### PolicyEvaluator

```csharp
namespace AgenticCoder.Application.Approvals;

public sealed class PolicyEvaluator
{
    private readonly IReadOnlyList<ApprovalRule> _rules;
    private readonly ApprovalPolicy _defaultPolicy;
    
    public ApprovalPolicy Evaluate(Operation operation)
    {
        foreach (var rule in _rules)
        {
            if (rule.Matches(operation))
            {
                return rule.Policy;
            }
        }
        return _defaultPolicy;
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-APPR-001 | Approval denied |
| ACODE-APPR-002 | Approval timeout |
| ACODE-APPR-003 | Non-interactive denied |
| ACODE-APPR-004 | Invalid response |
| ACODE-APPR-005 | Gate bypassed (security) |

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | General failure |
| 60 | Approval denied |
| 61 | Approval timeout |
| 62 | Non-interactive blocked |

### Logging Fields

```json
{
  "event": "approval_decision",
  "session_id": "abc123",
  "operation_category": "file_write",
  "operation_path": "src/file.ts",
  "policy_evaluated": "prompt",
  "decision": "approved",
  "response_time_ms": 2340,
  "timeout": false
}
```

### Implementation Checklist

1. [ ] Create IApprovalGate interface
2. [ ] Implement ApprovalGate
3. [ ] Create Operation record
4. [ ] Implement operation categories
5. [ ] Create PolicyEvaluator
6. [ ] Implement approval rules
7. [ ] Create IApprovalPrompt
8. [ ] Implement CLI prompt
9. [ ] Implement timeout handling
10. [ ] Implement non-interactive mode
11. [ ] Implement --yes flag
12. [ ] Add state machine integration
13. [ ] Add logging
14. [ ] Write unit tests
15. [ ] Write integration tests
16. [ ] Write E2E tests

### Validation Checklist Before Merge

- [ ] Gates intercept operations
- [ ] Policies evaluate correctly
- [ ] Prompts work
- [ ] All options work
- [ ] Timeout works
- [ ] Non-interactive works
- [ ] --yes works
- [ ] Secrets redacted
- [ ] Audit trail complete
- [ ] Unit test coverage > 90%

### Rollout Plan

1. **Phase 1:** Gate architecture
2. **Phase 2:** Policy evaluation
3. **Phase 3:** CLI prompts
4. **Phase 4:** Timeout handling
5. **Phase 5:** Non-interactive
6. **Phase 6:** --yes flag
7. **Phase 7:** State integration
8. **Phase 8:** Logging

---

**End of Task 013 Specification**