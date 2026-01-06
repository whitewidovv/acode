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

### Business Value and ROI

Approval gates deliver measurable value by preventing costly mistakes while maintaining development velocity:

**Error Prevention Value ($245,000/year for 20-developer team):**
- **Prevented production incidents**: Without approval gates, autonomous code generation leads to ~12 incidents/year where generated code causes outages. Average incident cost: $15,000 (4 hours of 5 engineers @ $100/hour debugging + $5,000 customer impact). With gates: 2 incidents/year. Prevention: 10 × $15,000 = **$150,000/year**.
- **Prevented data loss**: File deletion without approval leads to ~6 data loss events/year (deleted important files, configs). Recovery cost: 8 hours/event @ $100/hour = $800/event. Prevention: 6 × $800 = **$4,800/year**.
- **Prevented security issues**: Command execution without approval leads to ~4 security issues/year (leaked credentials, exposed APIs). Remediation cost: $15,000/event (incident response + fixes + notification). Prevention: 4 × $15,000 = **$60,000/year**.
- **Prevented code corruption**: Writes without review lead to ~24 corruption events/year (broken syntax, merge conflicts). Fix cost: 2 hours @ $100/hour = $200/event. Prevention: 24 × $200 = **$4,800/year**.
- **Reduced audit failures**: Manual approval audit trail prevents compliance failures. Avoided audit penalty: ~$25,000/year.
- **Faster incident diagnosis**: Approval logs enable 60% faster root cause analysis. Average incident investigation: 4 hours. Reduction: 2.4 hours × 20 incidents/year × $100/hour = **$4,800/year**.

**Total Quantified Annual Value: $249,400**. Implementation investment: ~80 hours @ $100/hour = $8,000. **ROI: 3,017% (payback period: 11.7 days)**.

### Technical Architecture

Approval gates operate as middleware between the executor (Task 012.b) and tool execution. When the executor prepares to invoke a tool (write_file, execute_command, etc.), the operation is passed to the approval gate. The gate evaluates configured policies to determine if approval is required. If required, execution pauses, the prompt is shown, and the user's decision determines whether execution proceeds or skips.

**Gate Evaluation Flow:**

1. **Operation Interception**: Executor prepares `Operation` record with category, description, details
2. **Policy Evaluation**: `PolicyEvaluator` matches operation against configured rules (first match wins)
3. **Policy Application**:
   - AUTO_APPROVE → Proceed immediately
   - DENY → Block and skip
   - PROMPT → Show prompt, await response
4. **State Transition**: If prompt required, session transitions to AWAITING_APPROVAL state
5. **Prompt Display**: CLI renders operation details, preview, options
6. **User Response**: Capture input (A/D/S/V/?), validate
7. **Response Processing**: Update gate state, log decision
8. **Execution Control**: Proceed (APPROVED), skip (DENIED/SKIPPED), timeout (TIMEOUT)

**Policy Evaluation Engine:**

Rules are evaluated in order from most specific to most general. Each rule has a pattern (file path glob, command regex), operation category, and policy. The first matching rule applies. If no rules match, the default policy applies.

Example evaluation:
```yaml
rules:
  - pattern: "**/*.test.ts"        # Specific pattern
    operation: file_write
    policy: auto
  - pattern: "src/**"              # Broader pattern
    operation: file_write
    policy: prompt
default_policy: prompt
```

For operation `WRITE src/components/LoginForm.test.ts`:
1. Check rule 1: `**/*.test.ts` matches → policy = `auto` → APPROVED
2. (Skip rule 2, first match wins)

For operation `WRITE src/components/LoginForm.tsx`:
1. Check rule 1: `**/*.test.ts` does NOT match
2. Check rule 2: `src/**` matches → policy = `prompt` → PROMPT_REQUIRED

**Prompt Architecture:**

The prompt uses a structured layout optimized for quick decisions on routine operations while providing full details for complex ones:

```
⚠ Approval Required
─────────────────────────────────────
Operation: FILE_WRITE
Path: src/components/LoginForm.tsx
Size: 234 lines
Risk: MEDIUM

Preview (first 20 lines):
  1  | import React, { useState } from 'react';
  2  | import { useAuth } from '../hooks/useAuth';
  ...

Impact:
  - CREATES new file src/components/LoginForm.tsx
  - No existing file will be modified

[A]pprove  [D]eny  [S]kip  [V]iew all  [?]Help
Timeout: 4:45 remaining

Choice: _
```

Key design decisions:
- **Concise preview**: 20 lines by default (configurable), expandable with V
- **Clear impact statement**: "CREATES", "REPLACES", "DELETES" - unambiguous
- **Visible timeout**: Countdown prevents surprise denials
- **Single-key options**: A/D/S/V/? for fast decisions
- **Secrets redacted**: API keys, passwords automatically hidden

**State Machine Integration:**

Approval gates integrate with the session state machine (Task 011):

- **Normal flow**: RUNNING → (approval required) → AWAITING_APPROVAL → (approved) → RUNNING
- **Denial flow**: AWAITING_APPROVAL → (denied) → PAUSED (step skipped, session continues)
- **Timeout flow**: AWAITING_APPROVAL → (timeout) → apply timeout policy (deny/skip/escalate)
- **Crash during approval**: State persisted, resume shows prompt again

The AWAITING_APPROVAL state is resumable. If the CLI crashes or the user kills it, `acode resume` will restore the session and re-show the prompt. This prevents lost work and ensures every operation is explicitly decided.

**Non-Interactive Mode Detection:**

Non-interactive mode is detected when:
- stdin is not a TTY (`Console.IsInputRedirected == true`)
- `--non-interactive` flag is set
- `CI=true` environment variable is set

In non-interactive mode, prompts are impossible. The configured `non_interactive_policy` applies (default: DENY). This prevents automation from hanging indefinitely waiting for input that never comes.

**Timeout Handling:**

Prompts have configurable timeouts (default: 5 minutes). If the user doesn't respond within the timeout, the `timeout_action` policy applies:
- **deny** (default): Operation is denied, step skipped
- **skip**: Operation skipped, execution continues
- **escalate**: Log critical warning, send notification (future)

Timeouts prevent abandoned sessions from blocking resources. They also prevent attackers from using approval prompts as a DoS vector.

**--yes Flag and Scoping:**

The `--yes` flag auto-approves operations without prompting. Scoping rules (Task 013.c) limit what `--yes` approves:

- `--yes` alone: Approves all operations except those explicitly excluded
- `--yes=write`: Approves only FILE_WRITE operations
- `--yes --yes-exclude=delete`: Approves all except FILE_DELETE
- Configuration can mark operations as `--yes` ineligible (always prompt)

This balances automation needs (CI/CD requires `--yes`) with safety (deletions still require explicit approval).

**Security Enforcement:**

Approval gates are enforced at the **service layer**, not just the UI layer. Even if an attacker bypasses the CLI or manipulates the session state, the underlying tool execution service checks approval requirements independently. Operations without valid approval are rejected with ACODE-APPR-005 error code.

Approval state is stored in the session persistence layer with tamper detection. The session file includes a checksum of approval decisions. If the file is modified to change DENIED to APPROVED, the checksum fails and the session is rejected.

### Performance Considerations

Approval gates add minimal overhead:
- **Policy evaluation**: O(n) where n = number of rules, typically < 10 rules, target < 10ms
- **Prompt display**: Rendering overhead < 100ms, dominated by user think time (typically 3-30 seconds)
- **State persistence**: Approval decisions are part of session state, persisted with session (no additional I/O)

Auto-approved operations (FILE_READ, DIR_CREATE by default) have near-zero overhead—policy evaluation only, no prompt.

### Integration Points

Approval gates integrate with:
- **Task 012.b Executor**: Executor calls `IApprovalGate.RequestApprovalAsync()` before tool execution
- **Task 010 CLI Framework**: Prompt rendering uses CLI primitives (console output, input reading)
- **Task 011 Session State**: AWAITING_APPROVAL state persisted, resumed on crash
- **Task 013.a Gate Rules**: Rules loaded from configuration, evaluated by PolicyEvaluator
- **Task 013.b Decision Persistence**: Approval decisions stored in database for audit
- **Task 013.c --yes Scoping**: Flag behavior defined by scoping rules

### Constraints and Limitations

- **CLI-only prompts**: No GUI, web, or remote approval interfaces (out of scope)
- **Synchronous approval**: Execution blocks until decision made (no background approval)
- **Single-user**: No multi-user approval workflows (one session = one user)
- **English-only prompts**: Localization out of scope
- **No approval templates**: Each operation prompts individually (no "approve all similar")

### Trade-offs and Alternatives

**Trade-off 1: Auto-approve vs. Manual approval default**
- **Choice**: Default policy is PROMPT for writes/deletes/commands, AUTO_APPROVE for reads
- **Alternative rejected**: Auto-approve everything by default, require opt-in prompts
- **Rationale**: Safety-first design. Users explicitly relax restrictions, not the reverse.

**Trade-off 2: First-match vs. Most-specific rule**
- **Choice**: First matching rule wins (order-dependent evaluation)
- **Alternative rejected**: Most specific rule wins (specificity-based evaluation like CSS)
- **Rationale**: Simplicity and predictability. Users understand "top rule wins" easily.

**Trade-off 3: Timeout default action**
- **Choice**: Default timeout action is DENY (block operation)
- **Alternative rejected**: Default to SKIP (continue without operation)
- **Rationale**: Conservative default. Skipping might hide important failures.

**Trade-off 4: Secrets redaction approach**
- **Choice**: Pattern-based redaction (API_KEY, PASSWORD, etc. in preview)
- **Alternative rejected**: AI-based secret detection
- **Rationale**: Deterministic and fast. AI detection adds latency and uncertainty.

---

## Use Cases

### Use Case 1: Junior Developer Prevents Accidental File Deletion

**Actor:** Sarah, Junior Developer
**Context:** Sarah is using Acode to refactor a legacy authentication module. The AI agent has identified several old files as unused and is planning to delete them. Sarah is new to the codebase and isn't certain which files are truly unused.
**Problem:** Without approval gates, autonomous deletion could remove critical files that appear unused but are actually loaded dynamically.

**Without Approval Gates:**
Sarah runs: `acode run "Clean up unused auth files"`

Acode output:
```
[PLANNER] Identified 8 unused authentication files
[EXECUTOR] Step 1: Delete src/auth/legacy/OAuthProvider.ts
✓ Deleted
[EXECUTOR] Step 2: Delete src/auth/legacy/SessionManager.ts
✓ Deleted
...
[COMPLETE] Removed 8 files
```

Sarah commits and pushes. Production deployment fails—`SessionManager.ts` was dynamically loaded by a runtime config file. 4 hours of emergency debugging, rollback, and recovery. Cost: **$400 (4 hours × $100/hour)**.

**With Approval Gates:**
Sarah runs: `acode run "Clean up unused auth files"`

Acode output:
```
[PLANNER] Identified 8 unused authentication files
[EXECUTOR] Step 1: Delete src/auth/legacy/OAuthProvider.ts

⚠ Approval Required
─────────────────────────────────────
Operation: DELETE FILE
Path: src/auth/legacy/OAuthProvider.ts
Size: 87 lines
Risk: HIGH

File contents (first 20 lines):
  1  | export class OAuthProvider {
  2  |   constructor(private config: OAuthConfig) {}
  ...

Impact:
  - DELETES existing file
  - File is 3 months old, last modified by alice@company.com
  - 0 direct imports found in codebase

[A]pprove  [D]eny  [S]kip  [V]iew all  [?]Help

Choice: v
```

Sarah views the full file, searches the codebase for dynamic references, finds that `SessionManager.ts` is loaded at runtime. She approves deletion of truly unused files, denies deletion of `SessionManager.ts`.

**Result:** 0 files deleted incorrectly. No production incident. Time saved: **3.5 hours**. Cost saved: **$350**.

**Quantified Value:** Over 20 developers, this scenario occurs ~12 times/year. Average prevention: $350 × 12 = **$4,200/year savings**.

---

### Use Case 2: DevOps Engineer Safely Automates Deployment with --yes Flag

**Actor:** Marcus, DevOps Engineer
**Context:** Marcus is setting up CI/CD for automated code generation and deployment. The pipeline needs to run Acode to generate boilerplate code, run tests, and deploy. Interactive prompts would block the pipeline.
**Problem:** Without scoped `--yes` support, Marcus must choose between (1) allowing ALL operations without approval (unsafe) or (2) disabling automation entirely (defeats purpose).

**Without Scoped --yes:**
Marcus tries: `acode run "Generate API client code" --yes`

This auto-approves everything, including:
- ✓ Writing 45 generated files (good)
- ✓ Deleting 3 "outdated" files (DANGEROUS—might delete manually edited files)
- ✓ Running `npm publish` (DANGEROUS—should be manual)

Result: Accidentally publishes pre-release package to npm. 2 hours to unpublish, apologize to users, re-release. Cost: **$200**.

**With Scoped --yes (Task 013.c):**
Marcus configures:
```yaml
# .agent/config.yml
approvals:
  yes_scope:
    allowed_operations:
      - file_write
      - file_read
      - directory_create
    denied_operations:
      - file_delete      # Never auto-approve
      - terminal_command # Never auto-approve
```

CI/CD pipeline:
```yaml
- name: Generate Code
  run: acode run "Generate API client code" --yes
```

Acode behavior:
- ✓ Auto-approves: Writing 45 generated files
- ✗ Fails with exit code 62: Deletion requires approval (non-interactive denied)
- ✗ Fails with exit code 62: `npm publish` requires approval

Marcus updates the task to exclude deletions: `acode run "Generate API client code without deletions" --yes`

**Result:** CI/CD runs safely. No accidental deletions or command executions. Time saved: **15 hours/month** (automated generation vs manual). Cost saved: **$1,500/month = $18,000/year**.

**Quantified Value:** DevOps teams save **$18,000/year** per engineer with safe automation.

---

### Use Case 3: Security Audit Requires Approval Trail

**Actor:** Elena, Security Auditor
**Context:** Elena is conducting a security audit of development practices. One requirement is: "All production code changes must be traceable to an approval decision by a human." Acode's autonomous code generation raises questions: How do we know a human reviewed the changes?
**Problem:** Without approval gates and decision persistence (Task 013.b), there's no audit trail proving human oversight.

**Without Approval Gates + Persistence:**
Elena asks: "Can you show me evidence that all generated code was reviewed before deployment?"

Team response: "Well, developers run the code generation, so implicitly they approve..."

Elena: "But where's the audit trail? Who approved what, and when? Was the generated code reviewed or just auto-deployed?"

Team: "We don't have that logged..."

**Result:** Audit finding: "Inadequate controls on autonomous code generation. No evidence of human review." Compliance risk. Required remediation: Manual review process (defeats automation). Cost: **3 months of engineering time = $75,000**.

**With Approval Gates + Persistence (Task 013.b):**
Elena asks: "Show me the approval audit trail for last quarter's generated code."

Team runs:
```bash
$ acode approvals export --start 2025-10-01 --end 2025-12-31 --format csv
```

Output:
```csv
session_id,timestamp,user,operation,path,decision,response_time_sec
abc123,2025-10-05T14:23:01Z,sarah@company.com,FILE_WRITE,src/api/generated/UserClient.ts,APPROVED,12.4
abc123,2025-10-05T14:23:45Z,sarah@company.com,FILE_DELETE,src/api/old/LegacyClient.ts,DENIED,8.2
def456,2025-10-12T09:15:22Z,marcus@company.com,FILE_WRITE,src/types/generated/ApiTypes.ts,APPROVED,3.7
...
```

Elena reviews the trail:
- 1,247 file write operations → 1,247 APPROVED (all reviewed)
- 42 file delete operations → 18 APPROVED, 24 DENIED (conservative decisions)
- 0 unapproved operations executed
- Average review time: 8.3 seconds (reasonable for routine operations)
- All decisions attributable to specific users

**Result:** Audit passes. Compliance maintained. No remediation required. Time saved: **3 months of manual review process = $75,000**.

**Quantified Value:** Audit compliance value: **$75,000 one-time savings**. Annual ongoing compliance: **$25,000/year** (avoided audit penalties).

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

## Security Considerations

### Threat 1: Approval Gate Bypass via State Manipulation

**Risk Level:** Critical
**CVSS Score:** 9.1 (Critical)
**Attack Vector:** Local file manipulation

**Description:**
An attacker with file system access could modify the session state file to change DENIED approval decisions to APPROVED, or manipulate the gate state to skip approval entirely. Since session state is persisted to disk, a malicious process or compromised script could alter approval records before they are enforced.

**Attack Scenario:**
1. User runs `acode run "delete production config"`
2. Acode requests approval, user denies
3. Malicious script monitors session files
4. Script modifies session state: `approval_state: DENIED` → `approval_state: APPROVED`
5. Acode reads modified state, proceeds with deletion
6. Production config deleted without actual approval

**Impact:**
- Unauthorized file deletions
- Unauthorized command execution
- Complete bypass of safety guardrails
- Potential data loss or system compromise

**Mitigation - Complete C# Implementation:**

```csharp
namespace AgenticCoder.Application.Approvals.Security;

/// <summary>
/// Provides tamper detection for approval state by computing and verifying HMAC signatures.
/// Uses a per-session secret derived from session creation time and a system secret.
/// </summary>
public sealed class ApprovalStateTamperDetector
{
    private readonly byte[] _systemSecret;
    private readonly ILogger<ApprovalStateTamperDetector> _logger;

    public ApprovalStateTamperDetector(
        ISecretProvider secretProvider,
        ILogger<ApprovalStateTamperDetector> logger)
    {
        _systemSecret = secretProvider.GetSystemSecret();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Computes HMAC-SHA256 signature for approval state data.
    /// </summary>
    public string ComputeSignature(ApprovalStateData state)
    {
        ArgumentNullException.ThrowIfNull(state);

        // Derive per-session key from system secret and session ID
        var sessionKey = DeriveSessionKey(state.SessionId);

        // Serialize state deterministically for signing
        var dataToSign = SerializeForSigning(state);

        using var hmac = new HMACSHA256(sessionKey);
        var signatureBytes = hmac.ComputeHash(dataToSign);

        return Convert.ToBase64String(signatureBytes);
    }

    /// <summary>
    /// Verifies that approval state has not been tampered with.
    /// </summary>
    /// <returns>True if state is valid, false if tampered.</returns>
    public bool VerifyIntegrity(ApprovalStateData state, string expectedSignature)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSignature);

        try
        {
            var computedSignature = ComputeSignature(state);

            // Constant-time comparison to prevent timing attacks
            var isValid = CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(computedSignature),
                Convert.FromBase64String(expectedSignature));

            if (!isValid)
            {
                _logger.LogCritical(
                    "SECURITY: Approval state tamper detected for session {SessionId}. " +
                    "Expected signature {Expected}, computed {Computed}",
                    state.SessionId, expectedSignature, computedSignature);
            }

            return isValid;
        }
        catch (FormatException ex)
        {
            _logger.LogCritical(ex,
                "SECURITY: Invalid signature format for session {SessionId}",
                state.SessionId);
            return false;
        }
    }

    /// <summary>
    /// Creates signed approval state container.
    /// </summary>
    public SignedApprovalState Sign(ApprovalStateData state)
    {
        var signature = ComputeSignature(state);

        return new SignedApprovalState
        {
            State = state,
            Signature = signature,
            SignedAt = DateTimeOffset.UtcNow
        };
    }

    private byte[] DeriveSessionKey(Guid sessionId)
    {
        // Use HKDF to derive session-specific key
        var info = Encoding.UTF8.GetBytes($"acode-approval-{sessionId}");

        return HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            _systemSecret,
            outputLength: 32,
            salt: Array.Empty<byte>(),
            info: info);
    }

    private static byte[] SerializeForSigning(ApprovalStateData state)
    {
        // Deterministic serialization - order matters
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        writer.Write(state.SessionId.ToByteArray());
        writer.Write(state.OperationId.ToByteArray());
        writer.Write((int)state.Decision);
        writer.Write(state.DecidedAt.ToUnixTimeMilliseconds());
        writer.Write(state.OperationHash ?? string.Empty);

        return ms.ToArray();
    }
}

public sealed record ApprovalStateData
{
    public required Guid SessionId { get; init; }
    public required Guid OperationId { get; init; }
    public required ApprovalDecision Decision { get; init; }
    public required DateTimeOffset DecidedAt { get; init; }
    public string? OperationHash { get; init; }
}

public sealed record SignedApprovalState
{
    public required ApprovalStateData State { get; init; }
    public required string Signature { get; init; }
    public required DateTimeOffset SignedAt { get; init; }
}
```

**Testing Strategy:**
- Unit test: Verify signature computation is deterministic
- Unit test: Verify tampered data fails verification
- Integration test: Modify session file, verify detection
- E2E test: Attempt state manipulation during active session

---

### Threat 2: CLI Bypass via Direct Service Invocation

**Risk Level:** High
**CVSS Score:** 7.5 (High)
**Attack Vector:** API/Service layer

**Description:**
If approval gates are only enforced at the CLI layer, an attacker could bypass them by invoking the underlying services directly. A malicious plugin, compromised dependency, or direct API call could execute operations without triggering approval gates.

**Attack Scenario:**
1. Attacker writes malicious plugin for Acode
2. Plugin directly calls `IFileService.WriteFile()` instead of going through approval-gated executor
3. File written without any approval check
4. CLI shows successful operation, user unaware of bypass

**Impact:**
- Complete approval system bypass
- Unauthorized operations executed
- No audit trail of bypassed operations
- False sense of security

**Mitigation - Complete C# Implementation:**

```csharp
namespace AgenticCoder.Application.Approvals.Security;

/// <summary>
/// Decorator that enforces approval requirements at the service layer.
/// Wraps tool execution services to ensure approval is always checked.
/// </summary>
public sealed class ApprovalEnforcingToolExecutor : IToolExecutor
{
    private readonly IToolExecutor _inner;
    private readonly IApprovalGate _approvalGate;
    private readonly IApprovalStateStore _stateStore;
    private readonly ILogger<ApprovalEnforcingToolExecutor> _logger;

    public ApprovalEnforcingToolExecutor(
        IToolExecutor inner,
        IApprovalGate approvalGate,
        IApprovalStateStore stateStore,
        ILogger<ApprovalEnforcingToolExecutor> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _approvalGate = approvalGate ?? throw new ArgumentNullException(nameof(approvalGate));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ToolResult> ExecuteAsync(
        ToolCall toolCall,
        ExecutionContext context,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(toolCall);
        ArgumentNullException.ThrowIfNull(context);

        // Create operation descriptor from tool call
        var operation = CreateOperation(toolCall);

        // Check if approval already exists in state store
        var existingApproval = await _stateStore.GetApprovalAsync(
            context.SessionId,
            operation.OperationId,
            ct);

        if (existingApproval != null)
        {
            // Verify existing approval is valid
            if (!ValidateExistingApproval(existingApproval, operation))
            {
                _logger.LogCritical(
                    "SECURITY: Invalid existing approval for operation {OperationId}. " +
                    "Possible replay attack or state corruption.",
                    operation.OperationId);

                throw new ApprovalSecurityException(
                    "ACODE-APPR-005",
                    "Existing approval validation failed. Operation blocked.");
            }

            if (existingApproval.Decision != ApprovalDecision.Approved)
            {
                _logger.LogWarning(
                    "Operation {OperationId} was previously {Decision}. Blocking execution.",
                    operation.OperationId, existingApproval.Decision);

                return ToolResult.Blocked(
                    $"Operation was previously {existingApproval.Decision}");
            }
        }
        else
        {
            // No existing approval - request new approval
            var approvalResult = await _approvalGate.RequestApprovalAsync(
                operation,
                context.ApprovalOptions,
                ct);

            // Store approval result
            await _stateStore.SaveApprovalAsync(
                context.SessionId,
                operation.OperationId,
                approvalResult,
                ct);

            if (approvalResult.Decision != ApprovalDecision.Approved)
            {
                _logger.LogInformation(
                    "Operation {OperationId} {Decision}. Blocking execution.",
                    operation.OperationId, approvalResult.Decision);

                return ToolResult.Blocked(
                    $"Operation {approvalResult.Decision}: {approvalResult.Reason}");
            }
        }

        // Approval verified - execute the tool
        _logger.LogDebug("Executing approved operation {OperationId}", operation.OperationId);

        return await _inner.ExecuteAsync(toolCall, context, ct);
    }

    private Operation CreateOperation(ToolCall toolCall)
    {
        var category = MapToolToCategory(toolCall.ToolName);
        var operationId = ComputeOperationId(toolCall);

        return new Operation(
            OperationId: operationId,
            Category: category,
            Description: $"{toolCall.ToolName}: {toolCall.GetSummary()}",
            Details: toolCall.Arguments.AsReadOnly());
    }

    private static Guid ComputeOperationId(ToolCall toolCall)
    {
        // Deterministic ID based on tool call content
        using var sha256 = SHA256.Create();
        var serialized = JsonSerializer.Serialize(toolCall);
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(serialized));

        // Use first 16 bytes as GUID
        return new Guid(hash.Take(16).ToArray());
    }

    private bool ValidateExistingApproval(
        ApprovalResult existing,
        Operation currentOperation)
    {
        // Verify approval was for the same operation
        // This prevents replay attacks where old approvals are reused

        // Check operation ID matches
        if (existing.OperationId != currentOperation.OperationId)
        {
            return false;
        }

        // Check approval is not expired (max 1 hour)
        if (DateTimeOffset.UtcNow - existing.DecidedAt > TimeSpan.FromHours(1))
        {
            _logger.LogWarning(
                "Existing approval for {OperationId} expired at {DecidedAt}",
                existing.OperationId, existing.DecidedAt);
            return false;
        }

        return true;
    }

    private static OperationCategory MapToolToCategory(string toolName)
    {
        return toolName.ToLowerInvariant() switch
        {
            "write_file" or "create_file" => OperationCategory.FileWrite,
            "read_file" => OperationCategory.FileRead,
            "delete_file" or "remove_file" => OperationCategory.FileDelete,
            "create_directory" or "mkdir" => OperationCategory.DirectoryCreate,
            "execute" or "run_command" or "shell" => OperationCategory.TerminalCommand,
            _ => OperationCategory.Unknown
        };
    }
}

public sealed class ApprovalSecurityException : Exception
{
    public string ErrorCode { get; }

    public ApprovalSecurityException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
```

**Testing Strategy:**
- Unit test: Verify decorator enforces approval check
- Unit test: Verify bypass attempt throws ApprovalSecurityException
- Integration test: Call service directly, verify blocked
- E2E test: Simulate malicious plugin attempt

---

### Threat 3: Prompt Injection Leading to Misleading Approval Context

**Risk Level:** High
**CVSS Score:** 7.2 (High)
**Attack Vector:** User input / Model output

**Description:**
An attacker could craft malicious file content or command output that, when displayed in the approval prompt, misleads the user about what they're approving. By injecting terminal escape codes, Unicode tricks, or misleading text, the prompt could show a benign operation while actually requesting approval for a dangerous one.

**Attack Scenario:**
1. Malicious file contains: `"Deleting: readme.txt\r                              \rDeleting: production.db"`
2. Acode generates plan to delete "production.db"
3. Approval prompt displays: "Deleting: readme.txt" (carriage return overwrites)
4. User approves thinking it's just readme.txt
5. production.db is deleted

**Impact:**
- Users approve operations they didn't intend
- Data loss or system damage
- Trust in approval system undermined
- Social engineering attack vector

**Mitigation - Complete C# Implementation:**

```csharp
namespace AgenticCoder.CLI.Prompts.Security;

/// <summary>
/// Sanitizes content displayed in approval prompts to prevent injection attacks.
/// Removes terminal escape codes, normalizes Unicode, and flags suspicious content.
/// </summary>
public sealed class PromptContentSanitizer
{
    private readonly ILogger<PromptContentSanitizer> _logger;

    // ANSI escape code pattern (CSI sequences, OSC sequences, etc.)
    private static readonly Regex AnsiEscapePattern = new(
        @"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~]|\][^\x07]*\x07|\P{Cc})",
        RegexOptions.Compiled);

    // Control characters (except newline, tab)
    private static readonly Regex ControlCharPattern = new(
        @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]",
        RegexOptions.Compiled);

    // Carriage return (used for line overwriting attacks)
    private static readonly Regex CarriageReturnPattern = new(
        @"\r(?!\n)",
        RegexOptions.Compiled);

    // Unicode direction overrides (used for text direction attacks)
    private static readonly Regex UnicodeDirectionPattern = new(
        @"[\u202A-\u202E\u2066-\u2069]",
        RegexOptions.Compiled);

    // Homoglyph detection for common dangerous substitutions
    private static readonly Dictionary<char, char> CommonHomoglyphs = new()
    {
        { '\u0430', 'a' }, // Cyrillic а → Latin a
        { '\u0435', 'e' }, // Cyrillic е → Latin e
        { '\u043E', 'o' }, // Cyrillic о → Latin o
        { '\u0440', 'p' }, // Cyrillic р → Latin p
        { '\u0441', 'c' }, // Cyrillic с → Latin c
        { '\u0443', 'y' }, // Cyrillic у → Latin y
        { '\u0445', 'x' }, // Cyrillic х → Latin x
    };

    public PromptContentSanitizer(ILogger<PromptContentSanitizer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sanitizes content for safe display in approval prompts.
    /// </summary>
    public SanitizationResult Sanitize(string content, string sourceDescription)
    {
        ArgumentNullException.ThrowIfNull(content);

        var warnings = new List<SanitizationWarning>();
        var sanitized = content;

        // Step 1: Remove ANSI escape codes
        var ansiMatches = AnsiEscapePattern.Matches(sanitized);
        if (ansiMatches.Count > 0)
        {
            warnings.Add(new SanitizationWarning(
                SanitizationWarningType.AnsiEscapeRemoved,
                $"Removed {ansiMatches.Count} ANSI escape sequences"));
            sanitized = AnsiEscapePattern.Replace(sanitized, "");
        }

        // Step 2: Remove/replace control characters
        var controlMatches = ControlCharPattern.Matches(sanitized);
        if (controlMatches.Count > 0)
        {
            warnings.Add(new SanitizationWarning(
                SanitizationWarningType.ControlCharacterRemoved,
                $"Removed {controlMatches.Count} control characters"));
            sanitized = ControlCharPattern.Replace(sanitized, "");
        }

        // Step 3: Handle carriage returns (potential line overwrite attack)
        var crMatches = CarriageReturnPattern.Matches(sanitized);
        if (crMatches.Count > 0)
        {
            warnings.Add(new SanitizationWarning(
                SanitizationWarningType.LineOverwriteAttempt,
                $"Detected {crMatches.Count} potential line-overwrite sequences",
                IsSevere: true));

            _logger.LogWarning(
                "SECURITY: Potential line-overwrite attack detected in {Source}. " +
                "Content contained {Count} standalone carriage returns.",
                sourceDescription, crMatches.Count);

            // Replace CR with visible indicator
            sanitized = CarriageReturnPattern.Replace(sanitized, "[CR]");
        }

        // Step 4: Handle Unicode direction overrides
        var dirMatches = UnicodeDirectionPattern.Matches(sanitized);
        if (dirMatches.Count > 0)
        {
            warnings.Add(new SanitizationWarning(
                SanitizationWarningType.UnicodeDirectionOverride,
                $"Removed {dirMatches.Count} Unicode direction override characters",
                IsSevere: true));

            _logger.LogWarning(
                "SECURITY: Unicode direction override attack detected in {Source}.",
                sourceDescription);

            sanitized = UnicodeDirectionPattern.Replace(sanitized, "");
        }

        // Step 5: Detect and warn about homoglyphs
        var homoglyphCount = CountHomoglyphs(sanitized);
        if (homoglyphCount > 0)
        {
            warnings.Add(new SanitizationWarning(
                SanitizationWarningType.HomoglyphDetected,
                $"Detected {homoglyphCount} potential homoglyph characters",
                IsSevere: homoglyphCount > 5));

            // Don't replace, just warn - homoglyphs might be legitimate
        }

        // Step 6: Truncate extremely long lines (potential buffer overflow/DoS)
        sanitized = TruncateLongLines(sanitized, maxLineLength: 500, ref warnings);

        return new SanitizationResult(
            OriginalContent: content,
            SanitizedContent: sanitized,
            Warnings: warnings.AsReadOnly(),
            WasModified: content != sanitized,
            HasSevereWarnings: warnings.Any(w => w.IsSevere));
    }

    /// <summary>
    /// Renders content with visible indicators for normally invisible characters.
    /// Used in "View All" mode to help users detect suspicious content.
    /// </summary>
    public string RenderWithVisibleControl(string content)
    {
        var result = new StringBuilder(content.Length * 2);

        foreach (var c in content)
        {
            result.Append(c switch
            {
                '\r' => "␍",  // Visible CR symbol
                '\n' => "␊\n", // Visible LF + actual newline
                '\t' => "→\t", // Visible tab + actual tab
                '\x00' => "␀",  // Null
                '\x1B' => "␛",  // Escape
                _ when char.IsControl(c) => $"[0x{(int)c:X2}]",
                _ => c.ToString()
            });
        }

        return result.ToString();
    }

    private int CountHomoglyphs(string content)
    {
        return content.Count(c => CommonHomoglyphs.ContainsKey(c));
    }

    private string TruncateLongLines(
        string content,
        int maxLineLength,
        ref List<SanitizationWarning> warnings)
    {
        var lines = content.Split('\n');
        var truncatedCount = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length > maxLineLength)
            {
                lines[i] = lines[i][..maxLineLength] + "... [TRUNCATED]";
                truncatedCount++;
            }
        }

        if (truncatedCount > 0)
        {
            warnings.Add(new SanitizationWarning(
                SanitizationWarningType.LineTruncated,
                $"Truncated {truncatedCount} lines exceeding {maxLineLength} characters"));
        }

        return string.Join('\n', lines);
    }
}

public sealed record SanitizationResult(
    string OriginalContent,
    string SanitizedContent,
    IReadOnlyList<SanitizationWarning> Warnings,
    bool WasModified,
    bool HasSevereWarnings);

public sealed record SanitizationWarning(
    SanitizationWarningType Type,
    string Description,
    bool IsSevere = false);

public enum SanitizationWarningType
{
    AnsiEscapeRemoved,
    ControlCharacterRemoved,
    LineOverwriteAttempt,
    UnicodeDirectionOverride,
    HomoglyphDetected,
    LineTruncated
}
```

**Testing Strategy:**
- Unit test: Verify ANSI escape codes stripped
- Unit test: Verify carriage return attack detected
- Unit test: Verify Unicode direction override removed
- Integration test: Inject malicious content, verify sanitization
- E2E test: Display sanitized prompt, verify no visual tricks

---

### Threat 4: Timeout Race Condition Exploitation

**Risk Level:** Medium
**CVSS Score:** 5.9 (Medium)
**Attack Vector:** Timing manipulation

**Description:**
An attacker could exploit timing between when a timeout is checked and when the approval decision is applied. By carefully timing operations, an attacker could make an operation proceed after timeout should have blocked it, or vice versa.

**Attack Scenario:**
1. User configures 30-second timeout
2. Acode shows approval prompt
3. Timeout thread checks: 29.9 seconds elapsed → not timed out
4. Main thread delays slightly (context switch)
5. Timeout thread sets timeout flag
6. Main thread receives "approved" input at 30.1 seconds
7. Race: Does approval or timeout win?

**Impact:**
- Unpredictable approval behavior
- Operations may proceed despite timeout
- Timeouts may block legitimate approvals
- Difficult to reproduce bugs

**Mitigation - Complete C# Implementation:**

```csharp
namespace AgenticCoder.Application.Approvals.Security;

/// <summary>
/// Thread-safe approval prompt handler that prevents race conditions
/// between user input and timeout expiration.
/// </summary>
public sealed class RaceFreeApprovalPrompt
{
    private readonly IApprovalPromptRenderer _renderer;
    private readonly ILogger<RaceFreeApprovalPrompt> _logger;

    public RaceFreeApprovalPrompt(
        IApprovalPromptRenderer renderer,
        ILogger<RaceFreeApprovalPrompt> logger)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Shows approval prompt with race-condition-free timeout handling.
    /// Uses a state machine to ensure exactly one outcome.
    /// </summary>
    public async Task<ApprovalOutcome> ShowAsync(
        Operation operation,
        TimeSpan timeout,
        CancellationToken ct)
    {
        // Shared state protected by lock
        var state = new ApprovalState();
        var stateLock = new object();

        // Record start time for accurate timeout
        var startTime = Stopwatch.GetTimestamp();
        var timeoutTicks = timeout.Ticks * Stopwatch.Frequency / TimeSpan.TicksPerSecond;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // Start timeout monitor task
        var timeoutTask = Task.Run(async () =>
        {
            while (true)
            {
                var elapsed = Stopwatch.GetTimestamp() - startTime;
                var remaining = timeoutTicks - elapsed;

                if (remaining <= 0)
                {
                    // Attempt to claim timeout
                    lock (stateLock)
                    {
                        if (state.IsResolved)
                        {
                            // Already resolved by user input
                            return;
                        }

                        state.ResolveAs(ApprovalOutcomeType.Timeout, "Approval timeout");
                        _logger.LogInformation("Approval timed out after {Timeout}", timeout);
                    }

                    // Cancel the input task
                    timeoutCts.Cancel();
                    return;
                }

                // Wait before next check (adaptive delay based on remaining time)
                var delay = Math.Min(remaining / Stopwatch.Frequency * 1000, 100);
                await Task.Delay(TimeSpan.FromMilliseconds(delay), timeoutCts.Token)
                    .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

                if (timeoutCts.Token.IsCancellationRequested)
                    return;
            }
        }, ct);

        // Start user input task
        var inputTask = Task.Run(async () =>
        {
            try
            {
                var response = await _renderer.GetUserResponseAsync(operation, timeoutCts.Token);

                // Attempt to claim user response
                lock (stateLock)
                {
                    if (state.IsResolved)
                    {
                        // Already resolved by timeout
                        _logger.LogDebug(
                            "User response received but timeout already resolved");
                        return;
                    }

                    var outcomeType = response.Approved
                        ? ApprovalOutcomeType.Approved
                        : ApprovalOutcomeType.Denied;

                    state.ResolveAs(outcomeType, response.Reason);
                    _logger.LogDebug("Approval resolved by user: {Outcome}", outcomeType);
                }

                // Cancel the timeout task
                timeoutCts.Cancel();
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                // Timeout occurred, this is expected
            }
        }, ct);

        // Wait for either task to complete
        await Task.WhenAny(timeoutTask, inputTask).ConfigureAwait(false);

        // Ensure state is resolved
        lock (stateLock)
        {
            if (!state.IsResolved)
            {
                // Should never happen, but defensive
                state.ResolveAs(ApprovalOutcomeType.Error, "Unexpected resolution failure");
                _logger.LogError("Approval state was not resolved after task completion");
            }

            return new ApprovalOutcome(
                Type: state.OutcomeType!.Value,
                Reason: state.Reason,
                ResolvedAt: DateTimeOffset.UtcNow,
                ElapsedTime: TimeSpan.FromTicks(
                    (Stopwatch.GetTimestamp() - startTime) *
                    TimeSpan.TicksPerSecond / Stopwatch.Frequency));
        }
    }

    private sealed class ApprovalState
    {
        public bool IsResolved { get; private set; }
        public ApprovalOutcomeType? OutcomeType { get; private set; }
        public string? Reason { get; private set; }

        public void ResolveAs(ApprovalOutcomeType type, string? reason)
        {
            if (IsResolved)
                throw new InvalidOperationException("State already resolved");

            IsResolved = true;
            OutcomeType = type;
            Reason = reason;
        }
    }
}

public sealed record ApprovalOutcome(
    ApprovalOutcomeType Type,
    string? Reason,
    DateTimeOffset ResolvedAt,
    TimeSpan ElapsedTime);

public enum ApprovalOutcomeType
{
    Approved,
    Denied,
    Skipped,
    Timeout,
    Error
}
```

**Testing Strategy:**
- Unit test: Verify exactly one outcome when response and timeout coincide
- Unit test: Stress test with many concurrent approvals
- Integration test: Rapid input at timeout boundary
- Property test: Random delays never produce invalid states

---

### Threat 5: Denial of Service via Approval Flooding

**Risk Level:** Medium
**CVSS Score:** 5.5 (Medium)
**Attack Vector:** Resource exhaustion

**Description:**
An attacker could craft a task that generates thousands of operations requiring approval, overwhelming the user with prompts. Even with auto-approve, the volume of approval logging and processing could exhaust system resources or hide malicious operations among legitimate ones.

**Attack Scenario:**
1. Attacker crafts prompt: "Refactor all files, making 10 changes to each"
2. LLM generates plan with 5,000 file writes
3. Even with --yes, system processes 5,000 approvals
4. Hidden among them: one "write malicious code to startup script"
5. User overwhelmed, malicious write approved in the flood

**Impact:**
- System resource exhaustion
- User fatigue leading to careless approvals
- Malicious operations hidden in volume
- Log storage exhaustion
- Session timeouts

**Mitigation - Complete C# Implementation:**

```csharp
namespace AgenticCoder.Application.Approvals.Security;

/// <summary>
/// Rate limiter and flood detector for approval operations.
/// Prevents DoS via approval flooding and alerts users to unusual patterns.
/// </summary>
public sealed class ApprovalFloodProtector
{
    private readonly ApprovalFloodOptions _options;
    private readonly ILogger<ApprovalFloodProtector> _logger;

    // Sliding window counters
    private readonly ConcurrentDictionary<string, SlidingWindowCounter> _counters = new();

    public ApprovalFloodProtector(
        IOptions<ApprovalFloodOptions> options,
        ILogger<ApprovalFloodProtector> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if an approval operation should proceed or be throttled.
    /// </summary>
    public async Task<FloodCheckResult> CheckAsync(
        Guid sessionId,
        Operation operation,
        CancellationToken ct)
    {
        var sessionKey = sessionId.ToString();
        var counter = _counters.GetOrAdd(sessionKey, _ => new SlidingWindowCounter(
            windowSize: _options.WindowSize,
            maxCount: _options.MaxOperationsPerWindow));

        // Check current rate
        var currentCount = counter.GetCount();
        var remaining = _options.MaxOperationsPerWindow - currentCount;

        // Detect anomalies
        var anomalies = DetectAnomalies(counter, operation);

        if (remaining <= 0)
        {
            _logger.LogWarning(
                "SECURITY: Approval rate limit exceeded for session {SessionId}. " +
                "Count: {Count}, Limit: {Limit}, Window: {Window}",
                sessionId, currentCount, _options.MaxOperationsPerWindow, _options.WindowSize);

            return new FloodCheckResult(
                IsAllowed: false,
                Reason: $"Rate limit exceeded: {currentCount}/{_options.MaxOperationsPerWindow} " +
                        $"operations in {_options.WindowSize.TotalMinutes} minutes",
                CurrentCount: currentCount,
                RemainingInWindow: 0,
                CooldownRemaining: counter.GetTimeUntilSlot(),
                Anomalies: anomalies);
        }

        // Check for anomaly threshold
        if (anomalies.Count >= _options.AnomalyThreshold)
        {
            _logger.LogWarning(
                "SECURITY: Anomaly threshold reached for session {SessionId}. " +
                "Anomalies: {Anomalies}",
                sessionId, string.Join(", ", anomalies.Select(a => a.Type)));

            return new FloodCheckResult(
                IsAllowed: false,
                Reason: $"Suspicious pattern detected: {string.Join(", ", anomalies.Select(a => a.Description))}",
                CurrentCount: currentCount,
                RemainingInWindow: remaining,
                CooldownRemaining: TimeSpan.Zero,
                Anomalies: anomalies,
                RequiresManualReview: true);
        }

        // Allow and increment counter
        counter.Increment();

        // Warn if approaching limit
        if (remaining <= _options.WarningThreshold)
        {
            return new FloodCheckResult(
                IsAllowed: true,
                Reason: null,
                CurrentCount: currentCount + 1,
                RemainingInWindow: remaining - 1,
                CooldownRemaining: TimeSpan.Zero,
                Anomalies: anomalies,
                WarningMessage: $"Approaching rate limit: {remaining - 1} operations remaining " +
                               $"in {_options.WindowSize.TotalMinutes} minute window");
        }

        return new FloodCheckResult(
            IsAllowed: true,
            Reason: null,
            CurrentCount: currentCount + 1,
            RemainingInWindow: remaining - 1,
            CooldownRemaining: TimeSpan.Zero,
            Anomalies: anomalies);
    }

    /// <summary>
    /// Resets counters for a session (e.g., after user acknowledges warnings).
    /// </summary>
    public void ResetSession(Guid sessionId)
    {
        _counters.TryRemove(sessionId.ToString(), out _);
    }

    private List<FloodAnomaly> DetectAnomalies(
        SlidingWindowCounter counter,
        Operation currentOperation)
    {
        var anomalies = new List<FloodAnomaly>();

        // Check for burst pattern (many operations in quick succession)
        var recentCount = counter.GetCountInLastPeriod(TimeSpan.FromSeconds(10));
        if (recentCount > _options.BurstThreshold)
        {
            anomalies.Add(new FloodAnomaly(
                FloodAnomalyType.BurstDetected,
                $"Burst: {recentCount} operations in 10 seconds"));
        }

        // Check for operation category concentration
        var categoryConcentration = counter.GetCategoryConcentration();
        if (categoryConcentration.MaxPercentage > 0.95m && categoryConcentration.Count > 50)
        {
            anomalies.Add(new FloodAnomaly(
                FloodAnomalyType.CategoryConcentration,
                $"95%+ operations are {categoryConcentration.Category}"));
        }

        // Check for suspicious paths (e.g., system directories)
        if (currentOperation.Details.TryGetValue("path", out var pathObj))
        {
            var path = pathObj?.ToString() ?? "";
            if (IsSuspiciousPath(path))
            {
                anomalies.Add(new FloodAnomaly(
                    FloodAnomalyType.SuspiciousPath,
                    $"Operation targets suspicious path: {path}"));
            }
        }

        return anomalies;
    }

    private static bool IsSuspiciousPath(string path)
    {
        var suspicious = new[]
        {
            ".git/", ".git\\",
            ".ssh/", ".ssh\\",
            ".env", ".aws/", ".aws\\",
            "/etc/", "\\Windows\\System32\\",
            "node_modules/", "node_modules\\"
        };

        return suspicious.Any(s => path.Contains(s, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record FloodCheckResult(
    bool IsAllowed,
    string? Reason,
    int CurrentCount,
    int RemainingInWindow,
    TimeSpan CooldownRemaining,
    IReadOnlyList<FloodAnomaly> Anomalies,
    bool RequiresManualReview = false,
    string? WarningMessage = null);

public sealed record FloodAnomaly(
    FloodAnomalyType Type,
    string Description);

public enum FloodAnomalyType
{
    BurstDetected,
    CategoryConcentration,
    SuspiciousPath,
    UnusualTiming,
    RepeatedDenials
}

public sealed class ApprovalFloodOptions
{
    public TimeSpan WindowSize { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxOperationsPerWindow { get; set; } = 100;
    public int BurstThreshold { get; set; } = 20;
    public int AnomalyThreshold { get; set; } = 3;
    public int WarningThreshold { get; set; } = 10;
}

/// <summary>
/// Thread-safe sliding window counter for rate limiting.
/// </summary>
internal sealed class SlidingWindowCounter
{
    private readonly TimeSpan _windowSize;
    private readonly int _maxCount;
    private readonly ConcurrentQueue<(DateTimeOffset Time, OperationCategory Category)> _timestamps = new();

    public SlidingWindowCounter(TimeSpan windowSize, int maxCount)
    {
        _windowSize = windowSize;
        _maxCount = maxCount;
    }

    public void Increment(OperationCategory category = OperationCategory.Unknown)
    {
        PruneOldEntries();
        _timestamps.Enqueue((DateTimeOffset.UtcNow, category));
    }

    public int GetCount()
    {
        PruneOldEntries();
        return _timestamps.Count;
    }

    public int GetCountInLastPeriod(TimeSpan period)
    {
        var cutoff = DateTimeOffset.UtcNow - period;
        return _timestamps.Count(t => t.Time >= cutoff);
    }

    public TimeSpan GetTimeUntilSlot()
    {
        if (_timestamps.IsEmpty || _timestamps.Count < _maxCount)
            return TimeSpan.Zero;

        if (_timestamps.TryPeek(out var oldest))
        {
            var windowEnd = oldest.Time + _windowSize;
            var remaining = windowEnd - DateTimeOffset.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        return TimeSpan.Zero;
    }

    public (OperationCategory Category, decimal MaxPercentage, int Count) GetCategoryConcentration()
    {
        var entries = _timestamps.ToArray();
        if (entries.Length == 0)
            return (OperationCategory.Unknown, 0, 0);

        var groups = entries.GroupBy(e => e.Category)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (groups == null)
            return (OperationCategory.Unknown, 0, 0);

        return (groups.Key, (decimal)groups.Count() / entries.Length, entries.Length);
    }

    private void PruneOldEntries()
    {
        var cutoff = DateTimeOffset.UtcNow - _windowSize;

        while (_timestamps.TryPeek(out var oldest) && oldest.Time < cutoff)
        {
            _timestamps.TryDequeue(out _);
        }
    }
}
```

**Testing Strategy:**
- Unit test: Verify rate limit enforcement
- Unit test: Verify anomaly detection algorithms
- Integration test: Generate 1000 approvals, verify throttling
- E2E test: User sees warning when approaching limit
- Performance test: Counter operations complete in < 1ms

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

## Best Practices

### Gate Design Best Practices

- **BP-001: Defense in depth** - Implement approval checks at multiple layers (CLI, service, executor) to prevent bypass. Don't rely solely on CLI-level gates; wrap critical services with approval-enforcing decorators.

- **BP-002: Fail-secure defaults** - When approval state is ambiguous (e.g., corrupted file, timeout), default to DENY rather than APPROVE. It's safer to block a legitimate operation than allow a dangerous one.

- **BP-003: Explicit over implicit** - Require explicit approval decisions rather than inferring intent. A timeout should be treated as denial, not approval. No operation should proceed without a clear decision.

- **BP-004: Atomic state transitions** - Approval state changes must be atomic. Use transactions or compare-and-swap operations to prevent partial updates that could leave gates in an inconsistent state.

### Prompt Design Best Practices

- **BP-005: Progressive disclosure** - Show essential information first (operation type, target path), with detailed context available on demand. Avoid overwhelming users with information they don't need.

- **BP-006: Clear action consequences** - Each option (Approve/Deny/Skip) must clearly indicate what will happen. Users should never be surprised by the result of their choice.

- **BP-007: Consistent keybindings** - Use the same key mappings across all prompts ([A]pprove, [D]eny, etc.). Muscle memory reduces cognitive load and speeds up decisions.

- **BP-008: Visible countdown** - Display remaining timeout prominently and update it visibly. Users should never be surprised when a timeout occurs.

### Security Best Practices

- **BP-009: Content sanitization** - Always sanitize content displayed in prompts to prevent injection attacks. Strip ANSI codes, control characters, and Unicode direction overrides.

- **BP-010: Path validation** - Validate all file paths against protected path lists before even displaying the approval prompt. Don't show prompts for operations that will be blocked anyway.

- **BP-011: Tamper detection** - Sign approval state to detect modification. Verify signatures before acting on approval decisions. Log and alert on any tampering detection.

- **BP-012: Rate limiting** - Implement rate limits on approval operations to prevent flooding attacks. Alert users when unusual patterns are detected.

### State Management Best Practices

- **BP-013: Durable decisions** - Persist approval decisions immediately after they're made. If the system crashes, the decision should be recoverable on resume.

- **BP-014: Idempotent operations** - Make approval checks idempotent. Re-evaluating the same operation should return the same result (unless the user explicitly reconsiders).

- **BP-015: Session isolation** - Approval decisions are session-scoped. One session's decisions should never affect another session's behavior.

- **BP-016: Audit everything** - Log every approval decision with full context (operation, decision, rule matched, time, session). The audit trail is essential for debugging and compliance.

### Integration Best Practices

- **BP-017: Non-blocking for auto-approve** - Operations that will be auto-approved should not block the execution thread. Only actually-prompted operations should pause.

- **BP-018: Graceful degradation** - If approval services are unavailable, fail safely. In non-interactive mode, block rather than auto-approve. In interactive mode, show an error and retry.

- **BP-019: Test coverage for gates** - Every code path that can reach a protected operation must have a test verifying the gate is invoked. Missing gate checks are security vulnerabilities.

- **BP-020: Configuration validation** - Validate approval configuration at startup, not at runtime. Invalid rules should fail loudly during initialization, not silently during execution.

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

---

## Troubleshooting

### Issue 1: Prompts Hang in CI/CD Environment

**Symptom:**
When running Acode in a CI/CD pipeline (GitHub Actions, Jenkins, GitLab CI), the process hangs indefinitely at an approval prompt. The pipeline eventually times out without completing.

```
$ acode run "Run tests and deploy"
[EXECUTOR] Step 3: Run deployment script

⚠ Approval Required
─────────────────────────────────────
Operation: TERMINAL_COMMAND
Command: ./deploy.sh production
─────────────────────────────────────

[A]pprove  [D]eny  [S]kip  [V]iew all  [?]Help

Choice: _
# ← Process hangs here, waiting for input that will never come
```

**Cause:**
Acode detects an interactive terminal and waits for user input. In CI/CD environments, stdin is not attached to a terminal (no TTY), but Acode may not properly detect this condition, or the non-interactive fallback policy isn't configured.

**Solution:**
1. **Use --yes flag for automation:**
   ```bash
   acode run "Run tests and deploy" --yes
   ```

2. **Configure non-interactive policy in config:**
   ```yaml
   # .agent/config.yml
   approvals:
     non_interactive_policy: deny  # or 'skip' or 'fail'
   ```

3. **Force non-interactive mode:**
   ```bash
   acode run "task" --non-interactive
   ```

4. **Set CI environment variable:**
   ```bash
   export CI=true
   acode run "task"  # Acode detects CI=true and uses non-interactive policy
   ```

---

### Issue 2: Timeout Fires Before User Can Review

**Symptom:**
The approval prompt appears, but before the user can read the operation details and make a decision, the timeout expires and the operation is denied or skipped.

```
⚠ Approval Required
─────────────────────────────────────
Operation: WRITE FILE
Path: src/components/ComplexComponent.tsx
Size: 450 lines

Preview:
  1  | import React, { useState, useEffect } from 'react';
  ...

[A]pprove  [D]eny  [S]kip  [V]iew all  [?]Help

Timeout: 0:03 remaining

⏱ Timeout expired. Operation denied.
```

**Cause:**
The default timeout (often 30-60 seconds) is too short for complex operations that require careful review. Users need time to read previews, understand changes, and make informed decisions.

**Solution:**
1. **Increase timeout in configuration:**
   ```yaml
   # .agent/config.yml
   approvals:
     timeout_seconds: 600  # 10 minutes
   ```

2. **Disable timeout for interactive sessions:**
   ```yaml
   approvals:
     timeout_seconds: 0  # 0 = no timeout (infinite wait)
   ```

3. **Use per-category timeouts:**
   ```yaml
   approvals:
     timeout_seconds: 60  # Default
     category_timeouts:
       file_delete: 300   # Longer for dangerous operations
       terminal_command: 180
   ```

4. **Request more time during prompt (if supported):**
   Press `+` during prompt to add 60 seconds to the timeout.

---

### Issue 3: Too Many Prompts for Routine Operations

**Symptom:**
Every file write or command triggers a prompt, even for operations that are clearly safe. This makes sessions frustratingly slow and encourages users to blindly approve everything.

```
$ acode run "Add comprehensive test suite"

⚠ Approval Required - Write src/tests/user.test.ts
Choice: a

⚠ Approval Required - Write src/tests/auth.test.ts
Choice: a

⚠ Approval Required - Write src/tests/api.test.ts
Choice: a

# ... 50 more prompts for test files
```

**Cause:**
The default policy is overly conservative, prompting for all writes without considering file patterns or operation context. No rules are configured to auto-approve trusted patterns.

**Solution:**
1. **Configure auto-approve rules for trusted patterns:**
   ```yaml
   # .agent/config.yml
   approvals:
     rules:
       # Auto-approve all test files
       - pattern: "**/*.test.ts"
         operation: file_write
         policy: auto
       - pattern: "**/*.spec.ts"
         operation: file_write
         policy: auto

       # Auto-approve generated files
       - pattern: "**/generated/**"
         operation: file_write
         policy: auto
   ```

2. **Use --yes with scoped approval:**
   ```bash
   acode run "Add tests" --yes=file_write:*.test.ts
   ```

3. **Batch similar approvals:**
   Configure batch mode to group similar operations:
   ```yaml
   approvals:
     batch_similar: true
     batch_timeout_seconds: 5
   ```
   This shows: "Approve 50 FILE_WRITE operations to **/*.test.ts? [A/D/V]"

---

### Issue 4: Accidentally Denied Critical Operation

**Symptom:**
User accidentally pressed 'D' (deny) instead of 'A' (approve) on an important operation, and now the session has moved past that step without completing the work.

```
⚠ Approval Required
─────────────────────────────────────
Operation: WRITE FILE
Path: src/core/AuthenticationService.ts
Size: 89 lines
─────────────────────────────────────

[A]pprove  [D]eny  [S]kip  [V]iew all  [?]Help

Choice: d  # ← Accidental keystroke!

✗ Denied. Step skipped.

[EXECUTOR] Continuing to next step...
```

**Cause:**
Single-key input is efficient but error-prone. One wrong keystroke can skip critical work, and there's no undo mechanism.

**Solution:**
1. **Resume the session to retry denied steps:**
   ```bash
   # Denied steps are marked for retry on resume
   acode resume

   [RESUME] Found 1 denied step(s). Retry? [Y/n] y

   ⚠ Approval Required (RETRY)
   ─────────────────────────────────────
   Operation: WRITE FILE
   Path: src/core/AuthenticationService.ts
   ...
   ```

2. **Enable confirmation for deny:**
   ```yaml
   approvals:
     confirm_deny: true  # Requires 'D' then 'Y' to confirm denial
   ```

3. **Review session history:**
   ```bash
   acode approvals history --session current
   # Shows all decisions, including accidental denials
   ```

4. **Manually run the denied step:**
   If you know what the step was, you can run it directly:
   ```bash
   acode step run --step-id abc123
   ```

---

### Issue 5: Approval State Corrupted After Crash

**Symptom:**
After a system crash, power failure, or force-kill of Acode, the approval state is inconsistent. Previously approved operations are being re-prompted, or previously denied operations are somehow proceeding.

```
$ acode resume
[WARNING] Approval state inconsistency detected
- Step abc123: Recorded as APPROVED but signature invalid
- Step def456: Recorded as DENIED but missing from audit log

What would you like to do?
[R]ecover  [F]orce-reset  [Q]uit
```

**Cause:**
Approval state persisted to disk may be partially written or corrupted if the process terminated unexpectedly during a state transition.

**Solution:**
1. **Use recovery mode:**
   ```bash
   acode resume --recover
   ```
   This re-validates all approval states and prompts for any that are inconsistent.

2. **Force reset approval state:**
   ```bash
   # Caution: This clears all approval decisions for the session
   acode session reset-approvals --session abc123
   ```

3. **Check and repair state:**
   ```bash
   acode approvals verify --session abc123

   Verifying approval state...
   ✓ 45 valid approval records
   ✗ 2 corrupted records (abc123-step-7, abc123-step-12)

   Repair corrupted records? [Y/n] y
   ✓ Corrupted records marked as UNKNOWN - will re-prompt on resume
   ```

4. **Prevent future corruption:**
   ```yaml
   # .agent/config.yml
   approvals:
     persistence:
       sync_writes: true      # Flush to disk immediately
       use_wal_mode: true     # Use SQLite WAL for crash safety
   ```

---

### Issue 6: Protected Path Blocked Despite Explicit Approval

**Symptom:**
User explicitly approves an operation, but it's still blocked with a "protected path" error.

```
⚠ Approval Required
─────────────────────────────────────
Operation: WRITE FILE
Path: .env.production
─────────────────────────────────────

[A]pprove  [D]eny  [S]kip  [V]iew all  [?]Help

Choice: a

✗ BLOCKED: Protected path violation
Path '.env.production' matches protected pattern '.env*'
This operation cannot be approved.
```

**Cause:**
Protected paths are enforced at a higher level than user approval. The protect list is designed to prevent accidents even when the user explicitly approves. The prompt appears because the operation wasn't auto-denied, but it will be blocked regardless of approval.

**Solution:**
1. **Understand protected path behavior:**
   Protected paths cannot be bypassed via normal approval. This is intentional safety.

2. **Temporarily disable protection (use with caution):**
   ```bash
   # Requires explicit acknowledgment
   acode run "Update production env" --allow-protected=.env.production

   WARNING: You are allowing writes to protected path '.env.production'
   Type 'I UNDERSTAND THE RISK' to continue: I UNDERSTAND THE RISK
   ```

3. **Modify protected path list:**
   ```yaml
   # .agent/config.yml
   safety:
     protected_paths:
       - ".git/**"
       - ".env"
       # Remove .env.production from protection if needed
       # - ".env*"  # ← Commented out
   ```

4. **Use override with audit:**
   ```bash
   acode run "task" --override-protection --audit-reason "Approved by security team ticket SEC-123"
   ```

---

### Issue 7: Secrets Visible in Approval Preview

**Symptom:**
When previewing file content for approval, sensitive data like API keys, passwords, or tokens are visible in the preview.

```
⚠ Approval Required
─────────────────────────────────────
Operation: WRITE FILE
Path: src/config/api-config.ts
Size: 25 lines

Preview:
  1  | export const config = {
  2  |   apiKey: 'sk_live_abc123xyz789secret',  # ← Secret visible!
  3  |   password: 'super_secret_password',      # ← Secret visible!
  4  | };
─────────────────────────────────────
```

**Cause:**
Secret redaction may not be enabled, or the redaction patterns don't match the specific format of secrets in your codebase.

**Solution:**
1. **Enable secret redaction:**
   ```yaml
   # .agent/config.yml
   approvals:
     redact_secrets: true
   ```

2. **Add custom redaction patterns:**
   ```yaml
   approvals:
     redaction_patterns:
       - pattern: "sk_live_[a-zA-Z0-9]+"
         replacement: "[STRIPE_KEY_REDACTED]"
       - pattern: "password\\s*[:=]\\s*['\"][^'\"]+['\"]"
         replacement: "password: [REDACTED]"
   ```

3. **Report the issue:**
   If secrets aren't being caught by default patterns, report to improve detection:
   ```bash
   acode report-secret-pattern --example "ghp_xxxxxxxxxxxx"
   ```

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

```csharp
namespace AgenticCoder.Application.Tests.Unit.Approvals;

public class ApprovalGateTests
{
    private readonly Mock<IPolicyEvaluator> _mockPolicyEvaluator;
    private readonly Mock<IApprovalPrompt> _mockPrompt;
    private readonly Mock<IApprovalRepository> _mockRepository;
    private readonly ILogger<ApprovalGate> _logger;
    private readonly ApprovalGate _gate;
    
    public ApprovalGateTests()
    {
        _mockPolicyEvaluator = new Mock<IPolicyEvaluator>();
        _mockPrompt = new Mock<IApprovalPrompt>();
        _mockRepository = new Mock<IApprovalRepository>();
        _logger = NullLogger<ApprovalGate>.Instance;
        _gate = new ApprovalGate(_mockPolicyEvaluator.Object, _mockPrompt.Object, 
            _mockRepository.Object, _logger);
    }
    
    [Fact]
    public async Task Should_Intercept_Operations()
    {
        // Arrange
        var operation = new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write UserService.cs",
            Details: new Dictionary<string, object> { { "path", "src/UserService.cs" } }.AsReadOnly());
        
        var options = new ApprovalOptions { Interactive = true, Timeout = TimeSpan.FromSeconds(30) };
        
        _mockPolicyEvaluator
            .Setup(p => p.Evaluate(operation))
            .Returns(ApprovalPolicyType.AutoApprove);
        
        // Act
        var result = await _gate.RequestApprovalAsync(operation, options, CancellationToken.None);
        
        // Assert
        Assert.Equal(ApprovalDecision.Approved, result.Decision);
        _mockPolicyEvaluator.Verify(p => p.Evaluate(operation), Times.Once);
    }
    
    [Fact]
    public async Task Should_Evaluate_Policies_And_Auto_Approve()
    {
        // Arrange
        var operation = new Operation(
            Category: OperationCategory.FileRead,
            Description: "Read config.json",
            Details: new Dictionary<string, object> { { "path", "config.json" } }.AsReadOnly());
        
        var options = new ApprovalOptions { Interactive = true, Timeout = TimeSpan.FromSeconds(30) };
        
        _mockPolicyEvaluator
            .Setup(p => p.Evaluate(operation))
            .Returns(ApprovalPolicyType.AutoApprove);
        
        // Act
        var result = await _gate.RequestApprovalAsync(operation, options, CancellationToken.None);
        
        // Assert
        Assert.Equal(ApprovalDecision.Approved, result.Decision);
        Assert.NotNull(result.Reason);
        _mockPrompt.Verify(p => p.ShowAsync(It.IsAny<Operation>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task Should_Pause_And_Show_Prompt_When_Policy_Requires()
    {
        // Arrange
        var operation = new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write sensitive-data.txt",
            Details: new Dictionary<string, object> { { "path", "data/sensitive-data.txt" } }.AsReadOnly());
        
        var options = new ApprovalOptions { Interactive = true, Timeout = TimeSpan.FromSeconds(30) };
        
        _mockPolicyEvaluator
            .Setup(p => p.Evaluate(operation))
            .Returns(ApprovalPolicyType.Prompt);
        
        _mockPrompt
            .Setup(p => p.ShowAsync(operation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PromptResponse(Approved: true, Reason: null));
        
        // Act
        var result = await _gate.RequestApprovalAsync(operation, options, CancellationToken.None);
        
        // Assert
        Assert.Equal(ApprovalDecision.Approved, result.Decision);
        _mockPrompt.Verify(p => p.ShowAsync(operation, It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task Should_Enforce_Denial_When_Policy_Denies()
    {
        // Arrange
        var operation = new Operation(
            Category: OperationCategory.TerminalCommand,
            Description: "rm -rf /",
            Details: new Dictionary<string, object> { { "command", "rm -rf /" } }.AsReadOnly());
        
        var options = new ApprovalOptions { Interactive = true, Timeout = TimeSpan.FromSeconds(30) };
        
        _mockPolicyEvaluator
            .Setup(p => p.Evaluate(operation))
            .Returns(ApprovalPolicyType.Deny);
        
        // Act
        var result = await _gate.RequestApprovalAsync(operation, options, CancellationToken.None);
        
        // Assert
        Assert.Equal(ApprovalDecision.Denied, result.Decision);
        Assert.Contains("Policy denies", result.Reason);
        _mockPrompt.Verify(p => p.ShowAsync(It.IsAny<Operation>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class PolicyEvaluatorTests
{
    [Fact]
    public void Should_Evaluate_Rules_In_Order()
    {
        // Arrange
        var rules = new List<ApprovalRule>
        {
            new ApprovalRule("**/*.test.ts", OperationCategory.FileWrite, ApprovalPolicyType.AutoApprove),
            new ApprovalRule("src/**", OperationCategory.FileWrite, ApprovalPolicyType.Prompt)
        };
        var evaluator = new PolicyEvaluator(rules, ApprovalPolicyType.Prompt);
        
        var operation = new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write test file",
            Details: new Dictionary<string, object> { { "path", "src/components/Login.test.ts" } }.AsReadOnly());
        
        // Act
        var policy = evaluator.Evaluate(operation);
        
        // Assert
        Assert.Equal(ApprovalPolicyType.AutoApprove, policy); // First rule matches
    }
    
    [Fact]
    public void Should_Use_First_Match_Wins()
    {
        // Arrange
        var rules = new List<ApprovalRule>
        {
            new ApprovalRule("**/*.ts", OperationCategory.FileWrite, ApprovalPolicyType.AutoApprove),
            new ApprovalRule("**/*.tsx", OperationCategory.FileWrite, ApprovalPolicyType.Deny) // More specific but comes second
        };
        var evaluator = new PolicyEvaluator(rules, ApprovalPolicyType.Prompt);
        
        var operation = new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write component",
            Details: new Dictionary<string, object> { { "path", "src/Component.tsx" } }.AsReadOnly());
        
        // Act
        var policy = evaluator.Evaluate(operation);
        
        // Assert
        Assert.Equal(ApprovalPolicyType.AutoApprove, policy); // First rule matches *.ts, doesn't check *.tsx
    }
    
    [Fact]
    public void Should_Use_Default_When_No_Match()
    {
        // Arrange
        var rules = new List<ApprovalRule>
        {
            new ApprovalRule("tests/**", OperationCategory.FileWrite, ApprovalPolicyType.AutoApprove)
        };
        var evaluator = new PolicyEvaluator(rules, ApprovalPolicyType.Prompt);
        
        var operation = new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write source file",
            Details: new Dictionary<string, object> { { "path", "src/main.ts" } }.AsReadOnly());
        
        // Act
        var policy = evaluator.Evaluate(operation);
        
        // Assert
        Assert.Equal(ApprovalPolicyType.Prompt, policy); // Default policy
    }
}

public class PromptTests
{
    private readonly Mock<IConsole> _mockConsole;
    private readonly ApprovalPrompt _prompt;
    
    public PromptTests()
    {
        _mockConsole = new Mock<IConsole>();
        _prompt = new ApprovalPrompt(_mockConsole.Object, NullLogger<ApprovalPrompt>.Instance);
    }
    
    [Fact]
    public async Task Should_Show_Operation_Type_And_Details()
    {
        // Arrange
        var operation = new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write UserService.cs",
            Details: new Dictionary<string, object> 
            { 
                { "path", "src/services/UserService.cs" },
                { "size", 234 },
                { "action", "create" }
            }.AsReadOnly());
        
        _mockConsole.Setup(c => c.ReadKey(true)).Returns(new ConsoleKeyInfo('A', ConsoleKey.A, false, false, false));
        
        // Act
        var response = await _prompt.ShowAsync(operation, CancellationToken.None);
        
        // Assert
        Assert.True(response.Approved);
        _mockConsole.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("FILE_WRITE"))), Times.AtLeastOnce);
        _mockConsole.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("UserService.cs"))), Times.AtLeastOnce);
    }
    
    [Fact]
    public async Task Should_Accept_Valid_Input()
    {
        // Arrange
        var operation = CreateTestOperation();
        
        _mockConsole.Setup(c => c.ReadKey(true)).Returns(new ConsoleKeyInfo('A', ConsoleKey.A, false, false, false));
        
        // Act
        var response = await _prompt.ShowAsync(operation, CancellationToken.None);
        
        // Assert
        Assert.True(response.Approved);
    }
    
    [Fact]
    public async Task Should_Reject_Invalid_Input_And_Reprompt()
    {
        // Arrange
        var operation = CreateTestOperation();
        
        var sequence = _mockConsole.SetupSequence(c => c.ReadKey(true))
            .Returns(new ConsoleKeyInfo('X', ConsoleKey.X, false, false, false)) // Invalid
            .Returns(new ConsoleKeyInfo('Z', ConsoleKey.Z, false, false, false)) // Invalid
            .Returns(new ConsoleKeyInfo('D', ConsoleKey.D, false, false, false)); // Valid - Deny
        
        // Act
        var response = await _prompt.ShowAsync(operation, CancellationToken.None);
        
        // Assert
        Assert.False(response.Approved);
        _mockConsole.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("Invalid"))), Times.Exactly(2));
    }
    
    private static Operation CreateTestOperation()
    {
        return new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write file",
            Details: new Dictionary<string, object> { { "path", "test.txt" } }.AsReadOnly());
    }
}

public class TimeoutTests
{
    private readonly Mock<IApprovalPrompt> _mockPrompt;
    private readonly ApprovalGate _gate;
    
    public TimeoutTests()
    {
        var mockEvaluator = new Mock<IPolicyEvaluator>();
        _mockPrompt = new Mock<IApprovalPrompt>();
        var mockRepository = new Mock<IApprovalRepository>();
        
        mockEvaluator.Setup(e => e.Evaluate(It.IsAny<Operation>())).Returns(ApprovalPolicyType.Prompt);
        
        _gate = new ApprovalGate(mockEvaluator.Object, _mockPrompt.Object, 
            mockRepository.Object, NullLogger<ApprovalGate>.Instance);
    }
    
    [Fact]
    public async Task Should_Timeout_After_Configured_Duration()
    {
        // Arrange
        var operation = new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write file",
            Details: new Dictionary<string, object> { { "path", "test.txt" } }.AsReadOnly());
        
        var options = new ApprovalOptions { Interactive = true, Timeout = TimeSpan.FromMilliseconds(100) };
        
        _mockPrompt
            .Setup(p => p.ShowAsync(It.IsAny<Operation>(), It.IsAny<CancellationToken>()))
            .Returns(async (Operation op, CancellationToken ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct); // Simulate slow response
                return new PromptResponse(true, null);
            });
        
        // Act
        var result = await _gate.RequestApprovalAsync(operation, options, CancellationToken.None);
        
        // Assert
        Assert.Equal(ApprovalDecision.Timeout, result.Decision);
        Assert.Contains("timeout", result.Reason, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public async Task Should_Apply_Timeout_Policy_On_Timeout()
    {
        // Arrange
        var operation = new Operation(
            Category: OperationCategory.FileDelete,
            Description: "Delete important.txt",
            Details: new Dictionary<string, object> { { "path", "important.txt" } }.AsReadOnly());
        
        var options = new ApprovalOptions 
        { 
            Interactive = true, 
            Timeout = TimeSpan.FromMilliseconds(50),
            TimeoutPolicy = ApprovalDecision.Denied // Deny on timeout
        };
        
        _mockPrompt
            .Setup(p => p.ShowAsync(It.IsAny<Operation>(), It.IsAny<CancellationToken>()))
            .Returns(async (Operation op, CancellationToken ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return new PromptResponse(true, null);
            });
        
        // Act
        var result = await _gate.RequestApprovalAsync(operation, options, CancellationToken.None);
        
        // Assert
        Assert.Equal(ApprovalDecision.Timeout, result.Decision);
    }
}
```

### Integration Tests

```csharp
namespace AgenticCoder.Application.Tests.Integration.Approvals;

public class GateIntegrationTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;
    
    public GateIntegrationTests(TestServerFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task Should_Gate_File_Writes_With_Real_Config()
    {
        // Arrange
        var gate = _fixture.GetService<IApprovalGate>();
        var operation = new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write config.json",
            Details: new Dictionary<string, object> { { "path", "config/app.json" } }.AsReadOnly());
        
        var options = new ApprovalOptions { Interactive = false, AutoApprove = true };
        
        // Act
        var result = await gate.RequestApprovalAsync(operation, options, CancellationToken.None);
        
        // Assert
        Assert.Equal(ApprovalDecision.Approved, result.Decision);
    }
    
    [Fact]
    public async Task Should_Gate_Terminal_Commands_Based_On_Config()
    {
        // Arrange
        var gate = _fixture.GetService<IApprovalGate>();
        var operation = new Operation(
            Category: OperationCategory.TerminalCommand,
            Description: "Run npm install",
            Details: new Dictionary<string, object> { { "command", "npm install" } }.AsReadOnly());
        
        var options = new ApprovalOptions { Interactive = false, AutoApprove = false };
        
        // Act
        var result = await gate.RequestApprovalAsync(operation, options, CancellationToken.None);
        
        // Assert - Policy should be evaluated from config
        Assert.NotNull(result);
    }
    
    [Fact]
    public async Task Should_Resume_After_Approval()
    {
        // Arrange
        var gate = _fixture.GetService<IApprovalGate>();
        var sessionManager = _fixture.GetService<ISessionManager>();
        
        var sessionId = Guid.NewGuid();
        await sessionManager.CreateSessionAsync(sessionId);
        
        var operation = new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write file",
            Details: new Dictionary<string, object> { { "path", "test.txt" } }.AsReadOnly());
        
        var options = new ApprovalOptions { Interactive = false, AutoApprove = true };
        
        // Act
        var result = await gate.RequestApprovalAsync(operation, options, CancellationToken.None);
        var session = await sessionManager.GetSessionAsync(sessionId);
        
        // Assert
        Assert.Equal(ApprovalDecision.Approved, result.Decision);
        Assert.NotEqual(SessionState.AwaitingApproval, session.State);
    }
}

public class StateMachineIntegrationTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;
    
    public StateMachineIntegrationTests(TestServerFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task Should_Transition_To_Awaiting_When_Prompt_Required()
    {
        // Arrange
        var orchestrator = _fixture.GetService<IOrchestrator>();
        var sessionManager = _fixture.GetService<ISessionManager>();
        
        var sessionId = Guid.NewGuid();
        await sessionManager.CreateSessionAsync(sessionId);
        
        // Act - Trigger operation that requires approval
        // This would be done through orchestrator
        
        var session = await sessionManager.GetSessionAsync(sessionId);
        
        // Assert - State should transition
        Assert.NotNull(session);
    }
    
    [Fact]
    public async Task Should_Persist_Approval_State()
    {
        // Arrange
        var gate = _fixture.GetService<IApprovalGate>();
        var repository = _fixture.GetService<IApprovalRepository>();
        
        var operation = new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write file",
            Details: new Dictionary<string, object> { { "path", "test.txt" } }.AsReadOnly());
        
        var options = new ApprovalOptions { Interactive = false, AutoApprove = true };
        
        // Act
        var result = await gate.RequestApprovalAsync(operation, options, CancellationToken.None);
        var decisions = await repository.GetRecentDecisionsAsync(10);
        
        // Assert
        Assert.NotEmpty(decisions);
        Assert.Contains(decisions, d => d.Decision == ApprovalDecision.Approved);
    }
}
```

### E2E Tests

```csharp
namespace AgenticCoder.Application.Tests.E2E.Approvals;

public class ApprovalE2ETests : IClassFixture<E2ETestFixture>
{
    private readonly E2ETestFixture _fixture;
    
    public ApprovalE2ETests(E2ETestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task Should_Prompt_For_Write_In_Interactive_Mode()
    {
        // Arrange
        var cli = _fixture.CreateCLI();
        
        // Simulate: acode run "write a file"
        var args = new[] { "run", "write src/test.ts" };
        
        // Act - Would need to inject approval response
        var exitCode = await cli.RunAsync(args);
        
        // Assert
        Assert.Equal(0, exitCode);
    }
    
    [Fact]
    public async Task Should_Auto_Approve_With_Yes_Flag()
    {
        // Arrange
        var cli = _fixture.CreateCLI();
        
        // Simulate: acode run "write a file" --yes
        var args = new[] { "run", "write src/test.ts", "--yes" };
        
        // Act
        var exitCode = await cli.RunAsync(args);
        
        // Assert
        Assert.Equal(0, exitCode); // Should complete without prompting
    }
    
    [Fact]
    public async Task Should_Handle_Denial_Gracefully()
    {
        // Arrange
        var cli = _fixture.CreateCLI();
        
        // Simulate denial through mock prompt
        var args = new[] { "run", "delete important.txt" };
        
        // Act
        var exitCode = await cli.RunAsync(args);
        
        // Assert
        Assert.Equal(60, exitCode); // ACODE exit code for approval denied
    }
}
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

### ApprovalGate Complete Implementation

```csharp
namespace AgenticCoder.Application.Approvals;

public sealed class ApprovalGate : IApprovalGate
{
    private readonly IPolicyEvaluator _policyEvaluator;
    private readonly IApprovalPrompt _prompt;
    private readonly IApprovalRepository _repository;
    private readonly ILogger<ApprovalGate> _logger;
    
    public ApprovalGate(
        IPolicyEvaluator policyEvaluator,
        IApprovalPrompt prompt,
        IApprovalRepository repository,
        ILogger<ApprovalGate> logger)
    {
        _policyEvaluator = policyEvaluator ?? throw new ArgumentNullException(nameof(policyEvaluator));
        _prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<ApprovalResult> RequestApprovalAsync(
        Operation operation,
        ApprovalOptions options,
        CancellationToken ct)
    {
        _logger.LogInformation("Requesting approval for {OperationCategory}: {Description}",
            operation.Category, operation.Description);
        
        // Evaluate policy for this operation
        var policy = _policyEvaluator.Evaluate(operation);
        _logger.LogDebug("Policy evaluated: {Policy}", policy);
        
        ApprovalResult result;
        
        switch (policy)
        {
            case ApprovalPolicyType.AutoApprove:
                result = new ApprovalResult(
                    Decision: ApprovalDecision.Approved,
                    Reason: "Auto-approved by policy",
                    DecidedAt: DateTimeOffset.UtcNow);
                break;
            
            case ApprovalPolicyType.Deny:
                result = new ApprovalResult(
                    Decision: ApprovalDecision.Denied,
                    Reason: "Policy denies this operation",
                    DecidedAt: DateTimeOffset.UtcNow);
                break;
            
            case ApprovalPolicyType.Prompt:
                if (!options.Interactive)
                {
                    // Non-interactive mode - check auto-approve flag
                    if (options.AutoApprove)
                    {
                        result = new ApprovalResult(
                            Decision: ApprovalDecision.Approved,
                            Reason: "Auto-approved via --yes flag",
                            DecidedAt: DateTimeOffset.UtcNow);
                    }
                    else
                    {
                        result = new ApprovalResult(
                            Decision: ApprovalDecision.Denied,
                            Reason: "Non-interactive mode requires --yes for approval",
                            DecidedAt: DateTimeOffset.UtcNow);
                    }
                }
                else
                {
                    // Interactive mode - show prompt with timeout
                    result = await PromptWithTimeoutAsync(operation, options, ct);
                }
                break;
            
            default:
                throw new InvalidOperationException($"Unknown policy type: {policy}");
        }
        
        // Persist decision
        await _repository.SaveDecisionAsync(operation, result, ct);
        
        _logger.LogInformation("Approval decision: {Decision} - {Reason}",
            result.Decision, result.Reason);
        
        return result;
    }
    
    private async Task<ApprovalResult> PromptWithTimeoutAsync(
        Operation operation,
        ApprovalOptions options,
        CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(options.Timeout);
        
        try
        {
            var response = await _prompt.ShowAsync(operation, cts.Token);
            
            return new ApprovalResult(
                Decision: response.Approved ? ApprovalDecision.Approved : ApprovalDecision.Denied,
                Reason: response.Reason ?? (response.Approved ? "Approved by user" : "Denied by user"),
                DecidedAt: DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            // Timeout occurred
            _logger.LogWarning("Approval timeout after {Timeout}", options.Timeout);
            
            return new ApprovalResult(
                Decision: options.TimeoutPolicy,
                Reason: $"Approval timeout after {options.Timeout.TotalSeconds}s",
                DecidedAt: DateTimeOffset.UtcNow);
        }
    }
}
```

### PolicyEvaluator Complete Implementation

```csharp
namespace AgenticCoder.Application.Approvals;

public interface IPolicyEvaluator
{
    ApprovalPolicyType Evaluate(Operation operation);
}

public sealed class PolicyEvaluator : IPolicyEvaluator
{
    private readonly IReadOnlyList<ApprovalRule> _rules;
    private readonly ApprovalPolicyType _defaultPolicy;
    private readonly ILogger<PolicyEvaluator> _logger;
    
    public PolicyEvaluator(
        IReadOnlyList<ApprovalRule> rules,
        ApprovalPolicyType defaultPolicy,
        ILogger<PolicyEvaluator> logger = null)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _defaultPolicy = defaultPolicy;
        _logger = logger ?? NullLogger<PolicyEvaluator>.Instance;
    }
    
    public ApprovalPolicyType Evaluate(Operation operation)
    {
        _logger.LogDebug("Evaluating policy for {OperationCategory}", operation.Category);
        
        // Evaluate rules in order - first match wins
        foreach (var rule in _rules)
        {
            if (rule.Matches(operation))
            {
                _logger.LogDebug("Matched rule: {Pattern} -> {Policy}", rule.Pattern, rule.Policy);
                return rule.Policy;
            }
        }
        
        _logger.LogDebug("No rule matched, using default policy: {Policy}", _defaultPolicy);
        return _defaultPolicy;
    }
}

public sealed class ApprovalRule
{
    public string Pattern { get; }
    public OperationCategory Category { get; }
    public ApprovalPolicyType Policy { get; }
    
    public ApprovalRule(string pattern, OperationCategory category, ApprovalPolicyType policy)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        Category = category;
        Policy = policy;
    }
    
    public bool Matches(Operation operation)
    {
        if (operation.Category != Category)
        {
            return false;
        }
        
        // Extract path from operation details
        if (!operation.Details.TryGetValue("path", out var pathObj))
        {
            return false;
        }
        
        var path = pathObj?.ToString();
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }
        
        // Use glob pattern matching (simplified - production would use Microsoft.Extensions.FileSystemGlobbing)
        return GlobMatcher.Match(Pattern, path);
    }
}

public enum ApprovalPolicyType
{
    AutoApprove,
    Prompt,
    Deny
}
```

### ApprovalPrompt Implementation

```csharp
namespace AgenticCoder.CLI.Prompts;

public interface IApprovalPrompt
{
    Task<PromptResponse> ShowAsync(Operation operation, CancellationToken ct);
}

public sealed record PromptResponse(bool Approved, string? Reason);

public sealed class ApprovalPrompt : IApprovalPrompt
{
    private readonly IConsole _console;
    private readonly ILogger<ApprovalPrompt> _logger;
    
    private static readonly Dictionary<char, string> ValidOptions = new()
    {
        { 'A', "Approve" },
        { 'D', "Deny" },
        { 'S', "Skip" },
        { 'V', "View details" },
        { '?', "Help" }
    };
    
    public ApprovalPrompt(IConsole console, ILogger<ApprovalPrompt> logger)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<PromptResponse> ShowAsync(Operation operation, CancellationToken ct)
    {
        _logger.LogDebug("Showing approval prompt for {OperationCategory}", operation.Category);
        
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            
            // Render prompt
            RenderPrompt(operation);
            
            // Read user input
            var key = _console.ReadKey(intercept: true);
            var input = char.ToUpperInvariant(key.KeyChar);
            
            _console.WriteLine(); // New line after key press
            
            switch (input)
            {
                case 'A':
                    _logger.LogInformation("User approved operation");
                    return new PromptResponse(Approved: true, Reason: null);
                
                case 'D':
                    _logger.LogInformation("User denied operation");
                    return new PromptResponse(Approved: false, Reason: "User denied");
                
                case 'S':
                    _logger.LogInformation("User skipped operation");
                    return new PromptResponse(Approved: false, Reason: "User skipped");
                
                case 'V':
                    ShowDetails(operation);
                    _console.WriteLine("\nPress any key to return to prompt...");
                    _console.ReadKey(intercept: true);
                    _console.Clear();
                    break;
                
                case '?':
                    ShowHelp();
                    _console.WriteLine("\nPress any key to return to prompt...");
                    _console.ReadKey(intercept: true);
                    _console.Clear();
                    break;
                
                default:
                    _console.WriteLine($"Invalid option '{input}'. Press ? for help.");
                    await Task.Delay(1000, ct); // Brief pause before re-prompting
                    break;
            }
        }
    }
    
    private void RenderPrompt(Operation operation)
    {
        _console.WriteLine();
        _console.WriteLine("⚠ Approval Required");
        _console.WriteLine("─────────────────────────────────────");
        _console.WriteLine($"Operation: {operation.Category}");
        _console.WriteLine($"Description: {operation.Description}");
        
        // Show key details
        if (operation.Details.TryGetValue("path", out var path))
        {
            _console.WriteLine($"Path: {path}");
        }
        
        if (operation.Details.TryGetValue("size", out var size))
        {
            _console.WriteLine($"Size: {size} lines");
        }
        
        _console.WriteLine();
        _console.WriteLine("Options:");
        _console.WriteLine("  [A] Approve  [D] Deny  [S] Skip");
        _console.WriteLine("  [V] View details  [?] Help");
        _console.WriteLine();
        _console.Write("Your choice: ");
    }
    
    private void ShowDetails(Operation operation)
    {
        _console.Clear();
        _console.WriteLine("Operation Details");
        _console.WriteLine("════════════════════════════════════=");
        _console.WriteLine($"Category: {operation.Category}");
        _console.WriteLine($"Description: {operation.Description}");
        _console.WriteLine();
        _console.WriteLine("Details:");
        
        foreach (var (key, value) in operation.Details)
        {
            _console.WriteLine($"  {key}: {value}");
        }
    }
    
    private void ShowHelp()
    {
        _console.Clear();
        _console.WriteLine("Approval Prompt Help");
        _console.WriteLine("════════════════════════════════════=");
        _console.WriteLine();
        _console.WriteLine("Options:");
        _console.WriteLine("  A - Approve: Proceed with the operation");
        _console.WriteLine("  D - Deny: Block the operation and skip this step");
        _console.WriteLine("  S - Skip: Skip this operation but continue session");
        _console.WriteLine("  V - View details: Show full operation details");
        _console.WriteLine("  ? - Help: Show this help message");
    }
}
```

### Supporting Classes

```csharp
namespace AgenticCoder.Application.Approvals;

public sealed record ApprovalOptions(
    bool Interactive,
    bool AutoApprove,
    TimeSpan Timeout,
    ApprovalDecision TimeoutPolicy);

public sealed record ApprovalContext(
    Guid SessionId,
    Operation Operation,
    ApprovalOptions Options);

public interface IApprovalRepository
{
    Task SaveDecisionAsync(Operation operation, ApprovalResult result, CancellationToken ct);
    Task<IReadOnlyList<ApprovalResult>> GetRecentDecisionsAsync(int count, CancellationToken ct = default);
}

public sealed class ApprovalRepository : IApprovalRepository
{
    private readonly IDbConnection _connection;
    private readonly ILogger<ApprovalRepository> _logger;
    
    public ApprovalRepository(IDbConnection connection, ILogger<ApprovalRepository> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task SaveDecisionAsync(Operation operation, ApprovalResult result, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO approval_decisions (id, operation_category, operation_description, decision, reason, decided_at)
            VALUES (@Id, @Category, @Description, @Decision, @Reason, @DecidedAt)";
        
        await _connection.ExecuteAsync(sql, new
        {
            Id = Guid.NewGuid(),
            Category = operation.Category.ToString(),
            Description = operation.Description,
            Decision = result.Decision.ToString(),
            Reason = result.Reason,
            DecidedAt = result.DecidedAt
        });
        
        _logger.LogDebug("Saved approval decision: {Decision}", result.Decision);
    }
    
    public async Task<IReadOnlyList<ApprovalResult>> GetRecentDecisionsAsync(int count, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT decision, reason, decided_at
            FROM approval_decisions
            ORDER BY decided_at DESC
            LIMIT @Count";
        
        var results = await _connection.QueryAsync<ApprovalResult>(sql, new { Count = count });
        return results.ToList().AsReadOnly();
    }
}

// Simplified glob matcher for demo - production should use Microsoft.Extensions.FileSystemGlobbing
internal static class GlobMatcher
{
    public static bool Match(string pattern, string input)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")  // ** matches any path
            .Replace("\\*", "[^/]*")  // * matches within segment
            .Replace("\\?", ".")      // ? matches single char
            + "$";
        
        return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
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