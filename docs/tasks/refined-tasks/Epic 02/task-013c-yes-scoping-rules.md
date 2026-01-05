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

**Complete Mitigation Implementation:**

```csharp
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.Approvals.Scoping;

/// <summary>
/// Validates scope input for injection attacks before parsing.
/// Rejects any scope containing shell metacharacters or injection patterns.
/// </summary>
public sealed class ScopeInjectionGuard
{
    private readonly ILogger<ScopeInjectionGuard> _logger;

    /// <summary>
    /// Characters that could enable shell injection or argument splitting.
    /// </summary>
    private static readonly char[] DangerousCharacters = new[]
    {
        ' ',   // Space - could split arguments
        '\t',  // Tab - could split arguments
        '\n',  // Newline - could inject commands
        '\r',  // Carriage return - could inject commands
        ';',   // Semicolon - shell command separator
        '|',   // Pipe - shell command chaining
        '&',   // Ampersand - background execution
        '$',   // Dollar - variable expansion
        '`',   // Backtick - command substitution
        '(',   // Open paren - subshell
        ')',   // Close paren - subshell
        '{',   // Open brace - brace expansion
        '}',   // Close brace - brace expansion
        '<',   // Less than - input redirect
        '>',   // Greater than - output redirect
        '\\',  // Backslash - escape sequences
        '"',   // Double quote - could escape parsing
        '\''   // Single quote - could escape parsing
    };

    /// <summary>
    /// Regex patterns that indicate injection attempts.
    /// </summary>
    private static readonly Regex[] InjectionPatterns = new[]
    {
        new Regex(@"--\w+", RegexOptions.Compiled),           // Embedded flags
        new Regex(@"\$\{.*\}", RegexOptions.Compiled),        // Variable expansion
        new Regex(@"\$\(.*\)", RegexOptions.Compiled),        // Command substitution
        new Regex(@"(?:^|,)all(?:$|,)", RegexOptions.Compiled | RegexOptions.IgnoreCase) // 'all' requires special handling
    };

    public ScopeInjectionGuard(ILogger<ScopeInjectionGuard> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates raw scope input for potential injection attacks.
    /// </summary>
    /// <param name="rawScopeInput">The raw scope string from command line.</param>
    /// <returns>Validation result with details if rejected.</returns>
    public ScopeValidationResult ValidateForInjection(string rawScopeInput)
    {
        if (string.IsNullOrEmpty(rawScopeInput))
        {
            return ScopeValidationResult.Valid();
        }

        // Check for dangerous characters
        foreach (var dangerousChar in DangerousCharacters)
        {
            var index = rawScopeInput.IndexOf(dangerousChar);
            if (index >= 0)
            {
                var charName = GetCharacterName(dangerousChar);
                _logger.LogWarning(
                    "Scope injection attempt detected: {Character} at position {Position} in scope '{Scope}'",
                    charName, index, rawScopeInput);

                return ScopeValidationResult.Rejected(
                    ScopeRejectionReason.DangerousCharacter,
                    $"Scope contains dangerous character '{charName}' at position {index}. " +
                    "Scopes must not contain shell metacharacters.");
            }
        }

        // Check for injection patterns
        foreach (var pattern in InjectionPatterns)
        {
            var match = pattern.Match(rawScopeInput);
            if (match.Success)
            {
                // Special case: 'all' is allowed but requires --ack-danger flag
                if (pattern.ToString().Contains("all"))
                {
                    return ScopeValidationResult.RequiresAcknowledgment(
                        "Scope 'all' requires explicit --ack-danger flag for safety.");
                }

                _logger.LogWarning(
                    "Scope injection pattern detected: '{Pattern}' matched '{Match}' in scope '{Scope}'",
                    pattern.ToString(), match.Value, rawScopeInput);

                return ScopeValidationResult.Rejected(
                    ScopeRejectionReason.InjectionPattern,
                    $"Scope contains suspicious pattern '{match.Value}'. " +
                    "This appears to be an injection attempt.");
            }
        }

        // Validate overall length to prevent buffer-based attacks
        if (rawScopeInput.Length > 500)
        {
            _logger.LogWarning(
                "Scope length {Length} exceeds maximum 500 characters",
                rawScopeInput.Length);

            return ScopeValidationResult.Rejected(
                ScopeRejectionReason.ExcessiveLength,
                $"Scope length {rawScopeInput.Length} exceeds maximum of 500 characters.");
        }

        // Validate number of scope entries
        var entryCount = rawScopeInput.Split(',').Length;
        if (entryCount > 20)
        {
            _logger.LogWarning(
                "Scope entry count {Count} exceeds maximum 20 entries",
                entryCount);

            return ScopeValidationResult.Rejected(
                ScopeRejectionReason.TooManyEntries,
                $"Scope contains {entryCount} entries, maximum is 20.");
        }

        return ScopeValidationResult.Valid();
    }

    /// <summary>
    /// Sanitizes scope input by removing any potential injection attempts.
    /// Only used as a fallback when strict validation is not possible.
    /// </summary>
    public string SanitizeScope(string rawInput)
    {
        if (string.IsNullOrEmpty(rawInput))
        {
            return string.Empty;
        }

        var sanitized = rawInput;

        // Remove all dangerous characters
        foreach (var c in DangerousCharacters)
        {
            sanitized = sanitized.Replace(c.ToString(), string.Empty);
        }

        // Remove any remaining non-alphanumeric except allowed: comma, colon, underscore, asterisk, dot
        sanitized = Regex.Replace(sanitized, @"[^a-zA-Z0-9,:\-_\*\.]", string.Empty);

        // Truncate to maximum length
        if (sanitized.Length > 500)
        {
            sanitized = sanitized.Substring(0, 500);
        }

        _logger.LogInformation(
            "Sanitized scope from '{Original}' to '{Sanitized}'",
            rawInput, sanitized);

        return sanitized;
    }

    private static string GetCharacterName(char c) => c switch
    {
        ' ' => "space",
        '\t' => "tab",
        '\n' => "newline",
        '\r' => "carriage return",
        ';' => "semicolon",
        '|' => "pipe",
        '&' => "ampersand",
        '$' => "dollar sign",
        '`' => "backtick",
        '(' => "open parenthesis",
        ')' => "close parenthesis",
        '{' => "open brace",
        '}' => "close brace",
        '<' => "less than",
        '>' => "greater than",
        '\\' => "backslash",
        '"' => "double quote",
        '\'' => "single quote",
        _ => c.ToString()
    };
}

public sealed record ScopeValidationResult
{
    public bool IsValid { get; }
    public bool RequiresAck { get; }
    public ScopeRejectionReason? RejectionReason { get; }
    public string? Message { get; }

    private ScopeValidationResult(bool isValid, bool requiresAck,
        ScopeRejectionReason? reason, string? message)
    {
        IsValid = isValid;
        RequiresAck = requiresAck;
        RejectionReason = reason;
        Message = message;
    }

    public static ScopeValidationResult Valid() =>
        new(true, false, null, null);

    public static ScopeValidationResult RequiresAcknowledgment(string message) =>
        new(true, true, null, message);

    public static ScopeValidationResult Rejected(ScopeRejectionReason reason, string message) =>
        new(false, false, reason, message);
}

public enum ScopeRejectionReason
{
    DangerousCharacter,
    InjectionPattern,
    ExcessiveLength,
    TooManyEntries,
    InvalidSyntax
}
```

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

**Complete Mitigation Implementation:**

```csharp
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.Approvals.Scoping;

/// <summary>
/// Maintains hardcoded list of critical operations that can NEVER be downgraded
/// from Level 4, regardless of any configuration. This is the last line of defense
/// against risk level downgrade attacks.
/// </summary>
public sealed class HardcodedCriticalOperations
{
    private readonly ILogger<HardcodedCriticalOperations> _logger;

    /// <summary>
    /// Operations that are ALWAYS Level 4 (Critical) and can NEVER be bypassed.
    /// These are hardcoded and cannot be modified by configuration.
    /// </summary>
    private static readonly CriticalOperation[] ImmutableCriticalOperations = new[]
    {
        // Git internals - deletion is catastrophic and unrecoverable
        new CriticalOperation(
            OperationCategory.FileDelete,
            ".git/**",
            "Git repository internals - deletion destroys version history"),
        new CriticalOperation(
            OperationCategory.FileDelete,
            ".git",
            "Git repository root - deletion destroys version history"),
        new CriticalOperation(
            OperationCategory.DirDelete,
            ".git",
            "Git repository directory - deletion destroys version history"),

        // Environment files - contain secrets
        new CriticalOperation(
            OperationCategory.FileDelete,
            ".env",
            "Environment file - may contain secrets"),
        new CriticalOperation(
            OperationCategory.FileDelete,
            ".env.*",
            "Environment file variants - may contain secrets"),
        new CriticalOperation(
            OperationCategory.FileWrite,
            ".env",
            "Environment file modification - security sensitive"),

        // Acode configuration - agent self-modification
        new CriticalOperation(
            OperationCategory.FileDelete,
            ".agent/**",
            "Agent configuration - self-modification dangerous"),
        new CriticalOperation(
            OperationCategory.FileDelete,
            ".acode/**",
            "Agent configuration - self-modification dangerous"),

        // Destructive terminal commands
        new CriticalOperation(
            OperationCategory.Terminal,
            "rm -rf *",
            "Recursive force delete all - catastrophic"),
        new CriticalOperation(
            OperationCategory.Terminal,
            "rm -rf /",
            "Recursive force delete root - catastrophic"),
        new CriticalOperation(
            OperationCategory.Terminal,
            "rm -rf ~",
            "Recursive force delete home - catastrophic"),
        new CriticalOperation(
            OperationCategory.Terminal,
            "git push --force",
            "Force push - rewrites remote history"),
        new CriticalOperation(
            OperationCategory.Terminal,
            "git push -f",
            "Force push - rewrites remote history"),
        new CriticalOperation(
            OperationCategory.Terminal,
            "git reset --hard",
            "Hard reset - discards uncommitted changes"),
        new CriticalOperation(
            OperationCategory.Terminal,
            "git clean -fd",
            "Clean force - removes untracked files"),

        // Credential files
        new CriticalOperation(
            OperationCategory.FileDelete,
            "credentials.json",
            "Credential file - authentication data"),
        new CriticalOperation(
            OperationCategory.FileDelete,
            "**/*credentials*",
            "Credential files - authentication data"),
        new CriticalOperation(
            OperationCategory.FileDelete,
            "**/*secret*",
            "Secret files - sensitive data"),
        new CriticalOperation(
            OperationCategory.FileDelete,
            "**/*.pem",
            "Certificate files - encryption keys"),
        new CriticalOperation(
            OperationCategory.FileDelete,
            "**/*.key",
            "Key files - encryption keys"),
    };

    public HardcodedCriticalOperations(ILogger<HardcodedCriticalOperations> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if an operation is in the hardcoded critical list.
    /// Returns true if the operation can NEVER be bypassed.
    /// </summary>
    public bool IsCriticalOperation(OperationCategory category, string pattern)
    {
        foreach (var critical in ImmutableCriticalOperations)
        {
            if (critical.Category == category && MatchesPattern(pattern, critical.Pattern))
            {
                _logger.LogInformation(
                    "Operation {Category}:{Pattern} matched hardcoded critical: {Reason}",
                    category, pattern, critical.Reason);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Validates configuration risk levels against hardcoded critical operations.
    /// Rejects any configuration that attempts to downgrade critical operations.
    /// </summary>
    public ConfigurationValidationResult ValidateRiskLevelConfiguration(
        IEnumerable<RiskLevelOverride> configuredOverrides)
    {
        var violations = new List<string>();

        foreach (var configOverride in configuredOverrides)
        {
            // Check if this override targets a critical operation
            if (IsCriticalOperation(configOverride.Category, configOverride.Pattern))
            {
                // Any risk level other than 4 is a violation
                if (configOverride.RiskLevel != RiskLevel.Critical)
                {
                    var violation = $"Configuration attempted to downgrade critical operation " +
                        $"{configOverride.Category}:{configOverride.Pattern} from Level 4 to Level {(int)configOverride.RiskLevel}. " +
                        "This is a hardcoded protection and cannot be overridden.";

                    _logger.LogError(
                        "SECURITY: Risk level downgrade attack detected! {Violation}",
                        violation);

                    violations.Add(violation);
                }
            }
        }

        if (violations.Any())
        {
            return ConfigurationValidationResult.Rejected(
                "Risk level configuration contains security violations",
                violations);
        }

        return ConfigurationValidationResult.Valid();
    }

    /// <summary>
    /// Returns the enforced risk level for an operation, checking hardcoded
    /// critical operations first before falling back to configuration.
    /// </summary>
    public RiskLevel GetEnforcedRiskLevel(
        OperationCategory category,
        string pattern,
        RiskLevel configuredLevel)
    {
        // Hardcoded critical operations ALWAYS return Level 4
        if (IsCriticalOperation(category, pattern))
        {
            if (configuredLevel != RiskLevel.Critical)
            {
                _logger.LogWarning(
                    "Overriding configured risk level {Configured} with hardcoded Critical for {Category}:{Pattern}",
                    configuredLevel, category, pattern);
            }
            return RiskLevel.Critical;
        }

        return configuredLevel;
    }

    private static bool MatchesPattern(string input, string pattern)
    {
        // Direct match
        if (input.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            return true;

        // Glob pattern matching
        if (pattern.Contains('*'))
        {
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*\\*", ".*")
                .Replace("\\*", "[^/]*") + "$";
            return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
        }

        return false;
    }
}

public sealed record CriticalOperation(
    OperationCategory Category,
    string Pattern,
    string Reason);

public sealed record RiskLevelOverride(
    OperationCategory Category,
    string Pattern,
    RiskLevel RiskLevel);

public sealed record ConfigurationValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }
    public IReadOnlyList<string> Violations { get; }

    private ConfigurationValidationResult(bool isValid, string? errorMessage, IReadOnlyList<string> violations)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        Violations = violations;
    }

    public static ConfigurationValidationResult Valid() =>
        new(true, null, Array.Empty<string>());

    public static ConfigurationValidationResult Rejected(string errorMessage, IEnumerable<string> violations) =>
        new(false, errorMessage, violations.ToList());
}
```

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

**Complete Mitigation Implementation:**

```csharp
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.Approvals.Scoping;

/// <summary>
/// Validates scope patterns for complexity to prevent DoS via pattern exhaustion.
/// Enforces limits on pattern depth, length, and matching time.
/// </summary>
public sealed class ScopePatternComplexityValidator
{
    private readonly ILogger<ScopePatternComplexityValidator> _logger;

    /// <summary>
    /// Maximum allowed depth of recursive glob patterns (**).
    /// Example: **/**/** has depth 3 (allowed), **/**/**/** has depth 4 (rejected).
    /// </summary>
    public const int MaxRecursiveGlobDepth = 3;

    /// <summary>
    /// Maximum allowed length of a single pattern in characters.
    /// </summary>
    public const int MaxPatternLength = 100;

    /// <summary>
    /// Maximum allowed time for pattern matching in milliseconds.
    /// </summary>
    public const int MaxMatchTimeMs = 100;

    /// <summary>
    /// Maximum number of wildcards allowed in a single pattern.
    /// </summary>
    public const int MaxWildcards = 10;

    /// <summary>
    /// Patterns known to cause exponential backtracking.
    /// </summary>
    private static readonly Regex[] CatastrophicPatterns = new[]
    {
        // Nested quantifiers with overlapping character classes
        new Regex(@"\*\*[/\\]\*\*[/\\]\*\*[/\\]\*\*", RegexOptions.Compiled),
        // Character class with nested repetition
        new Regex(@"\[.+\]\+\*", RegexOptions.Compiled),
        // Multiple consecutive wildcards
        new Regex(@"\*{3,}", RegexOptions.Compiled),
    };

    public ScopePatternComplexityValidator(ILogger<ScopePatternComplexityValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates a scope pattern for complexity issues.
    /// </summary>
    public PatternComplexityResult ValidatePattern(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return PatternComplexityResult.Valid(0);
        }

        // Check pattern length
        if (pattern.Length > MaxPatternLength)
        {
            _logger.LogWarning(
                "Pattern '{Pattern}' exceeds maximum length {Max}",
                pattern, MaxPatternLength);

            return PatternComplexityResult.TooComplex(
                $"Pattern length {pattern.Length} exceeds maximum of {MaxPatternLength} characters.");
        }

        // Count recursive glob depth
        var recursiveGlobCount = CountOccurrences(pattern, "**");
        if (recursiveGlobCount > MaxRecursiveGlobDepth)
        {
            _logger.LogWarning(
                "Pattern '{Pattern}' has recursive glob depth {Depth}, max is {Max}",
                pattern, recursiveGlobCount, MaxRecursiveGlobDepth);

            return PatternComplexityResult.TooComplex(
                $"Pattern contains {recursiveGlobCount} recursive globs (**), maximum is {MaxRecursiveGlobDepth}.");
        }

        // Count total wildcards
        var wildcardCount = CountWildcards(pattern);
        if (wildcardCount > MaxWildcards)
        {
            _logger.LogWarning(
                "Pattern '{Pattern}' has {Count} wildcards, max is {Max}",
                pattern, wildcardCount, MaxWildcards);

            return PatternComplexityResult.TooComplex(
                $"Pattern contains {wildcardCount} wildcards, maximum is {MaxWildcards}.");
        }

        // Check for known catastrophic patterns
        foreach (var catastrophic in CatastrophicPatterns)
        {
            if (catastrophic.IsMatch(pattern))
            {
                _logger.LogWarning(
                    "Pattern '{Pattern}' matches known catastrophic pattern",
                    pattern);

                return PatternComplexityResult.TooComplex(
                    "Pattern contains a known complexity issue that could cause performance problems.");
            }
        }

        // Estimate complexity score
        var complexityScore = CalculateComplexityScore(pattern);

        return PatternComplexityResult.Valid(complexityScore);
    }

    /// <summary>
    /// Tests pattern matching performance with a timeout.
    /// Used to catch patterns that pass static analysis but are still slow.
    /// </summary>
    public PatternPerformanceResult TestPatternPerformance(string pattern, string testInput)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Convert glob to regex for matching
            var regexPattern = ConvertGlobToRegex(pattern);

            // Use a regex with a timeout
            var regex = new Regex(
                regexPattern,
                RegexOptions.Compiled | RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(MaxMatchTimeMs));

            var isMatch = regex.IsMatch(testInput);

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > MaxMatchTimeMs / 2)
            {
                _logger.LogWarning(
                    "Pattern '{Pattern}' took {Ms}ms to match (warning threshold: {Threshold}ms)",
                    pattern, stopwatch.ElapsedMilliseconds, MaxMatchTimeMs / 2);
            }

            return PatternPerformanceResult.Success(
                isMatch,
                stopwatch.ElapsedMilliseconds);
        }
        catch (RegexMatchTimeoutException)
        {
            stopwatch.Stop();

            _logger.LogError(
                "Pattern '{Pattern}' timed out after {Ms}ms",
                pattern, MaxMatchTimeMs);

            return PatternPerformanceResult.TimedOut(
                $"Pattern matching timed out after {MaxMatchTimeMs}ms. " +
                "Please use a simpler pattern.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex,
                "Pattern '{Pattern}' produced invalid regex",
                pattern);

            return PatternPerformanceResult.InvalidPattern(
                $"Pattern is invalid: {ex.Message}");
        }
    }

    /// <summary>
    /// Safely matches a pattern against input with timeout protection.
    /// </summary>
    public bool SafeMatch(string pattern, string input)
    {
        // First validate complexity
        var complexityResult = ValidatePattern(pattern);
        if (!complexityResult.IsValid)
        {
            _logger.LogWarning(
                "Pattern '{Pattern}' rejected for complexity: {Reason}",
                pattern, complexityResult.Message);
            return false;
        }

        // Then test performance
        var performanceResult = TestPatternPerformance(pattern, input);
        if (!performanceResult.IsSuccess)
        {
            _logger.LogWarning(
                "Pattern '{Pattern}' performance issue: {Reason}",
                pattern, performanceResult.Message);
            return false;
        }

        return performanceResult.IsMatch;
    }

    private static int CountOccurrences(string input, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = input.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    private static int CountWildcards(string pattern)
    {
        var count = 0;
        foreach (var c in pattern)
        {
            if (c == '*' || c == '?' || c == '[' || c == ']')
            {
                count++;
            }
        }
        return count;
    }

    private static int CalculateComplexityScore(string pattern)
    {
        var score = 0;
        score += CountOccurrences(pattern, "**") * 10;  // Recursive glob is expensive
        score += CountOccurrences(pattern, "*") * 2;    // Single glob less so
        score += CountOccurrences(pattern, "?") * 1;    // Single char is cheap
        score += pattern.Length / 10;                    // Length contributes
        return score;
    }

    private static string ConvertGlobToRegex(string glob)
    {
        var regex = "^";
        regex += Regex.Escape(glob)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".");
        regex += "$";
        return regex;
    }
}

public sealed record PatternComplexityResult
{
    public bool IsValid { get; }
    public int ComplexityScore { get; }
    public string? Message { get; }

    private PatternComplexityResult(bool isValid, int score, string? message)
    {
        IsValid = isValid;
        ComplexityScore = score;
        Message = message;
    }

    public static PatternComplexityResult Valid(int score) =>
        new(true, score, null);

    public static PatternComplexityResult TooComplex(string message) =>
        new(false, int.MaxValue, message);
}

public sealed record PatternPerformanceResult
{
    public bool IsSuccess { get; }
    public bool IsMatch { get; }
    public long ElapsedMs { get; }
    public string? Message { get; }

    private PatternPerformanceResult(bool success, bool match, long elapsed, string? message)
    {
        IsSuccess = success;
        IsMatch = match;
        ElapsedMs = elapsed;
        Message = message;
    }

    public static PatternPerformanceResult Success(bool isMatch, long elapsedMs) =>
        new(true, isMatch, elapsedMs, null);

    public static PatternPerformanceResult TimedOut(string message) =>
        new(false, false, -1, message);

    public static PatternPerformanceResult InvalidPattern(string message) =>
        new(false, false, -1, message);
}
```

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

**Complete Mitigation Implementation:**

```csharp
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.Approvals.Scoping;

/// <summary>
/// Classifies terminal operations with explicit deny-list checking to prevent
/// dangerous commands from being misclassified as safe.
/// </summary>
public sealed class TerminalOperationClassifier
{
    private readonly ILogger<TerminalOperationClassifier> _logger;

    /// <summary>
    /// Dangerous command patterns that are ALWAYS classified as high risk,
    /// regardless of how they might otherwise be parsed.
    /// </summary>
    private static readonly DangerousCommandPattern[] DangerousPatterns = new[]
    {
        // Force push - rewrites remote history
        new DangerousCommandPattern(
            @"\bgit\s+push\s+.*(-f|--force)",
            RiskLevel.Critical,
            "git push --force rewrites remote history"),
        new DangerousCommandPattern(
            @"\bgit\s+push\s+--force-with-lease",
            RiskLevel.High,
            "git push --force-with-lease can still rewrite history"),

        // Hard reset - discards changes
        new DangerousCommandPattern(
            @"\bgit\s+reset\s+--hard",
            RiskLevel.Critical,
            "git reset --hard discards uncommitted changes"),
        new DangerousCommandPattern(
            @"\bgit\s+checkout\s+\.\s*$",
            RiskLevel.High,
            "git checkout . discards all uncommitted changes"),

        // Clean operations - remove files
        new DangerousCommandPattern(
            @"\bgit\s+clean\s+.*-f",
            RiskLevel.High,
            "git clean -f removes untracked files"),

        // Recursive force delete
        new DangerousCommandPattern(
            @"\brm\s+.*-r.*-f|\brm\s+.*-f.*-r|\brm\s+-rf",
            RiskLevel.Critical,
            "rm -rf can delete entire directories"),
        new DangerousCommandPattern(
            @"\brm\s+.*\*",
            RiskLevel.High,
            "rm with wildcard can delete multiple files"),

        // Dangerous operations
        new DangerousCommandPattern(
            @"\bsudo\s+",
            RiskLevel.Critical,
            "sudo elevates privileges"),
        new DangerousCommandPattern(
            @"\bchmod\s+777",
            RiskLevel.High,
            "chmod 777 makes files world-writable"),
        new DangerousCommandPattern(
            @"\bchown\s+",
            RiskLevel.High,
            "chown changes file ownership"),

        // Package operations
        new DangerousCommandPattern(
            @"\bnpm\s+publish",
            RiskLevel.Critical,
            "npm publish releases packages publicly"),
        new DangerousCommandPattern(
            @"\bdotnet\s+nuget\s+push",
            RiskLevel.Critical,
            "dotnet nuget push releases packages"),

        // Database operations
        new DangerousCommandPattern(
            @"\bDROP\s+(TABLE|DATABASE|SCHEMA)",
            RiskLevel.Critical,
            "DROP destroys database objects"),
        new DangerousCommandPattern(
            @"\bTRUNCATE\s+TABLE",
            RiskLevel.Critical,
            "TRUNCATE removes all data from table"),
        new DangerousCommandPattern(
            @"\bDELETE\s+FROM\s+\w+\s*;?\s*$",
            RiskLevel.Critical,
            "DELETE without WHERE removes all rows"),
    };

    /// <summary>
    /// Commands explicitly whitelisted as safe for terminal:safe scope.
    /// These are read-only or harmless operations.
    /// </summary>
    private static readonly Regex[] SafeCommandPatterns = new[]
    {
        // Git informational commands
        new Regex(@"^\s*git\s+status\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*git\s+log\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*git\s+diff\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*git\s+show\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*git\s+branch\s*(-a|-r|--list)?\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*git\s+remote\s+-v\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase),

        // List/read operations
        new Regex(@"^\s*ls\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*dir\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*cat\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*head\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*tail\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*less\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*more\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*pwd\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*whoami\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase),

        // Search operations
        new Regex(@"^\s*grep\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*find\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*which\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*whereis\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),

        // Build/test read-only
        new Regex(@"^\s*dotnet\s+--version\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*node\s+--version\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*npm\s+--version\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*npm\s+ls\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"^\s*npm\s+outdated\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    public TerminalOperationClassifier(ILogger<TerminalOperationClassifier> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Classifies a terminal command, checking dangerous patterns first.
    /// </summary>
    public TerminalClassificationResult Classify(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return TerminalClassificationResult.Safe("Empty command");
        }

        // FIRST: Check dangerous patterns (deny list takes priority)
        foreach (var dangerous in DangerousPatterns)
        {
            if (Regex.IsMatch(command, dangerous.Pattern, RegexOptions.IgnoreCase))
            {
                _logger.LogWarning(
                    "Command '{Command}' matched dangerous pattern: {Reason}",
                    command, dangerous.Reason);

                return TerminalClassificationResult.Dangerous(
                    dangerous.RiskLevel,
                    dangerous.Reason);
            }
        }

        // SECOND: Check safe patterns (allow list)
        foreach (var safe in SafeCommandPatterns)
        {
            if (safe.IsMatch(command))
            {
                _logger.LogDebug(
                    "Command '{Command}' matched safe pattern",
                    command);

                return TerminalClassificationResult.Safe(
                    "Matched whitelisted safe command pattern");
            }
        }

        // DEFAULT: Unknown commands require explicit approval
        _logger.LogInformation(
            "Command '{Command}' not in safe list, requiring approval",
            command);

        return TerminalClassificationResult.Unknown(
            RiskLevel.High,
            "Command not in safe list, requires explicit approval");
    }

    /// <summary>
    /// Checks if a command qualifies for terminal:safe scope.
    /// </summary>
    public bool IsSafeCommand(string command)
    {
        var result = Classify(command);
        return result.IsSafe;
    }

    /// <summary>
    /// Gets the risk level for a command.
    /// </summary>
    public RiskLevel GetRiskLevel(string command)
    {
        var result = Classify(command);
        return result.RiskLevel;
    }
}

public sealed record DangerousCommandPattern(
    string Pattern,
    RiskLevel RiskLevel,
    string Reason);

public sealed record TerminalClassificationResult
{
    public bool IsSafe { get; }
    public bool IsDangerous { get; }
    public RiskLevel RiskLevel { get; }
    public string Reason { get; }

    private TerminalClassificationResult(bool safe, bool dangerous, RiskLevel level, string reason)
    {
        IsSafe = safe;
        IsDangerous = dangerous;
        RiskLevel = level;
        Reason = reason;
    }

    public static TerminalClassificationResult Safe(string reason) =>
        new(true, false, RiskLevel.Low, reason);

    public static TerminalClassificationResult Dangerous(RiskLevel level, string reason) =>
        new(false, true, level, reason);

    public static TerminalClassificationResult Unknown(RiskLevel level, string reason) =>
        new(false, false, level, reason);
}
```

---

### Threat 5: Scope Persistence Leading to Unintended Bypass

**Risk Level:** Low
**CVSS Score:** 4.0 (Medium)
**Attack Vector:** State confusion

**Description:**
If scopes persist between sessions unexpectedly, a broad scope used for one task could remain active for a sensitive task. The user thinks they're running with default scopes but actually has `--yes=all` active from yesterday.

**Complete Mitigation Implementation:**

```csharp
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.Approvals.Scoping;

/// <summary>
/// Manages session-scoped --yes configurations. Explicitly designed to NEVER
/// persist scopes between sessions to prevent unintended bypass attacks.
/// </summary>
public sealed class SessionScopeManager : IDisposable
{
    private readonly ILogger<SessionScopeManager> _logger;
    private readonly Guid _sessionId;
    private readonly DateTimeOffset _sessionStartTime;
    private readonly object _lock = new();

    private YesScope _currentScope;
    private YesScope? _nextOperationScope;
    private int _bypassCount;
    private bool _isDisposed;

    /// <summary>
    /// Creates a new session scope manager. Scope is ALWAYS empty at creation.
    /// There is NO way to restore scope from a previous session.
    /// </summary>
    public SessionScopeManager(
        Guid sessionId,
        ILogger<SessionScopeManager> logger)
    {
        _sessionId = sessionId;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionStartTime = DateTimeOffset.UtcNow;

        // CRITICAL: Always start with empty scope
        // This is by design - no persistence, no restoration
        _currentScope = YesScope.Default;
        _nextOperationScope = null;
        _bypassCount = 0;

        _logger.LogInformation(
            "Session {SessionId} initialized with default scope at {Time}",
            _sessionId, _sessionStartTime);
    }

    /// <summary>
    /// Gets the current effective scope for the session.
    /// Returns default scope if no scope is set.
    /// </summary>
    public YesScope CurrentScope
    {
        get
        {
            ThrowIfDisposed();
            lock (_lock)
            {
                return _currentScope;
            }
        }
    }

    /// <summary>
    /// Sets the session scope from CLI argument.
    /// This scope applies to the entire session.
    /// </summary>
    public void SetSessionScope(YesScope scope)
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            _logger.LogInformation(
                "Session {SessionId} scope set to: {Scope}",
                _sessionId, scope);

            _currentScope = scope;
        }
    }

    /// <summary>
    /// Sets a one-time scope for the next operation only.
    /// This scope is consumed after one use.
    /// </summary>
    public void SetNextOperationScope(YesScope scope)
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            _logger.LogInformation(
                "Session {SessionId} next-operation scope set to: {Scope}",
                _sessionId, scope);

            _nextOperationScope = scope;
        }
    }

    /// <summary>
    /// Gets the scope for the current operation and consumes any one-time scope.
    /// </summary>
    public YesScope GetScopeForOperation()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            // If there's a one-time scope, use it and clear it
            if (_nextOperationScope != null)
            {
                var oneTimeScope = _nextOperationScope;
                _nextOperationScope = null;

                _logger.LogDebug(
                    "Session {SessionId} using one-time scope: {Scope}",
                    _sessionId, oneTimeScope);

                return oneTimeScope;
            }

            return _currentScope;
        }
    }

    /// <summary>
    /// Records a bypass for audit purposes.
    /// </summary>
    public void RecordBypass(OperationCategory category, string target, YesScope scopeUsed)
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            _bypassCount++;

            _logger.LogInformation(
                "Session {SessionId} bypass #{Count}: {Category}:{Target} using scope {Scope}",
                _sessionId, _bypassCount, category, target, scopeUsed);
        }
    }

    /// <summary>
    /// Gets session statistics for audit.
    /// </summary>
    public SessionScopeStatistics GetStatistics()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            return new SessionScopeStatistics(
                _sessionId,
                _sessionStartTime,
                DateTimeOffset.UtcNow,
                _currentScope,
                _bypassCount);
        }
    }

    /// <summary>
    /// Resets the scope to default. Used when user wants to clear all --yes.
    /// </summary>
    public void Reset()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            _logger.LogInformation(
                "Session {SessionId} scope reset to default",
                _sessionId);

            _currentScope = YesScope.Default;
            _nextOperationScope = null;
        }
    }

    /// <summary>
    /// Disposes the session, clearing all scope information.
    /// This is called automatically at session end.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }

            // Log final statistics
            _logger.LogInformation(
                "Session {SessionId} ending. Total bypasses: {Count}. Duration: {Duration}",
                _sessionId,
                _bypassCount,
                DateTimeOffset.UtcNow - _sessionStartTime);

            // Clear all state
            _currentScope = YesScope.None;
            _nextOperationScope = null;

            _isDisposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(SessionScopeManager),
                "Session has ended. Scope information is no longer available. " +
                "Start a new session to set scopes.");
        }
    }
}

public sealed record SessionScopeStatistics(
    Guid SessionId,
    DateTimeOffset StartTime,
    DateTimeOffset CurrentTime,
    YesScope CurrentScope,
    int TotalBypasses)
{
    public TimeSpan Duration => CurrentTime - StartTime;
}

/// <summary>
/// Factory for creating session scope managers.
/// Ensures each session gets a fresh, empty scope manager.
/// </summary>
public sealed class SessionScopeManagerFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public SessionScopeManagerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Creates a new session scope manager with empty scope.
    /// CRITICAL: This is the ONLY way to get a scope manager.
    /// There is NO method to restore from previous session.
    /// </summary>
    public SessionScopeManager CreateForSession(Guid sessionId)
    {
        var logger = _loggerFactory.CreateLogger<SessionScopeManager>();
        return new SessionScopeManager(sessionId, logger);
    }
}
```

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

### Scope Syntax and Parsing

- [ ] AC-001: `--yes` flag without value uses default scope (file_read, dir_list, search)
- [ ] AC-002: `--yes=scope` parses single scope value correctly
- [ ] AC-003: `--yes=scope1,scope2,scope3` parses comma-separated scopes
- [ ] AC-004: Scope modifiers parse correctly: `--yes=file_write:test`
- [ ] AC-005: Pattern modifiers parse correctly: `--yes=file_write:*.test.ts`
- [ ] AC-006: Combined scopes with mixed modifiers parse: `--yes=file_read,file_write:test,terminal:safe`
- [ ] AC-007: Invalid scope syntax produces clear error message with position indicator
- [ ] AC-008: Scope parser rejects shell metacharacters (semicolon, pipe, ampersand, backtick)
- [ ] AC-009: Scope parser rejects embedded flags (`--yes=file_read --ack-danger` as single value)
- [ ] AC-010: Maximum 20 scope entries enforced per specification
- [ ] AC-011: Maximum 100 character pattern length enforced per pattern

### Category Coverage

- [ ] AC-012: `file_read` scope bypasses approval for all file read operations
- [ ] AC-013: `file_write` scope bypasses approval for file create/modify operations
- [ ] AC-014: `file_delete` scope bypasses approval for file deletion operations
- [ ] AC-015: `dir_create` scope bypasses approval for directory creation
- [ ] AC-016: `dir_delete` scope bypasses approval for directory deletion
- [ ] AC-017: `terminal` scope bypasses approval for shell command execution
- [ ] AC-018: `terminal:safe` scope only bypasses whitelisted safe commands
- [ ] AC-019: `config` scope bypasses approval for configuration file modifications
- [ ] AC-020: `all` scope requires explicit `--ack-danger` flag to activate
- [ ] AC-021: Unknown category produces helpful error with "Did you mean?" suggestions

### Scope Modifiers

- [ ] AC-022: `:safe` modifier restricts to operations marked as safe
- [ ] AC-023: `:test` modifier restricts to paths containing `/test/` or `/tests/` or `*.test.*`
- [ ] AC-024: `:generated` modifier restricts to paths in generated directories
- [ ] AC-025: `:pattern` modifier with glob pattern filters paths correctly
- [ ] AC-026: Glob patterns support `*` for single directory level matching
- [ ] AC-027: Glob patterns support `**` for recursive directory matching
- [ ] AC-028: Glob patterns support character classes `[abc]`
- [ ] AC-029: Glob patterns support negation `!pattern` in scope exclusions

### Risk Level Classification

- [ ] AC-030: Level 1 (Low) operations auto-approve with bare `--yes` flag
- [ ] AC-031: Level 2 (Medium) operations require explicit scope in `--yes=scope`
- [ ] AC-032: Level 3 (High) operations require explicit scope AND display warning
- [ ] AC-033: Level 4 (Critical) operations NEVER bypass, always prompt regardless of `--yes`
- [ ] AC-034: Risk levels are correctly assigned per operation type table in spec
- [ ] AC-035: Hardcoded critical operations cannot be downgraded via configuration
- [ ] AC-036: Configuration-based risk overrides are validated against hardcoded protections

### Precedence and Resolution

- [ ] AC-037: CLI `--yes` scope overrides config `yes.default_scope` for allow/prompt decisions
- [ ] AC-038: Config `yes.default_scope` overrides built-in defaults
- [ ] AC-039: More specific scope rules override general scope rules
- [ ] AC-040: Deny rules ALWAYS override allow rules regardless of specificity
- [ ] AC-041: `--no` flag takes highest precedence, blocks all auto-approvals
- [ ] AC-042: `--interactive` flag forces prompts regardless of `--yes` scope
- [ ] AC-043: Precedence: `--no` > protected_operations > deny_rules > `--yes` scope > config > defaults

### Protected Operations (Never Bypassable)

- [ ] AC-044: `.git/**` deletion is NEVER bypassable, always prompts
- [ ] AC-045: `.git` directory deletion is NEVER bypassable
- [ ] AC-046: `.env*` deletion is NEVER bypassable (all environment files)
- [ ] AC-047: `.agent/**` and `.acode/**` deletion NEVER bypassable
- [ ] AC-048: `git push --force` and `git push -f` are NEVER bypassable
- [ ] AC-049: `rm -rf /`, `rm -rf ~`, `rm -rf *` are NEVER bypassable
- [ ] AC-050: `git reset --hard` is NEVER bypassable
- [ ] AC-051: Custom protected operations from config are enforced
- [ ] AC-052: Protected operation warning displays full reason for protection

### Rate Limiting

- [ ] AC-053: Default rate limit of 100 bypasses per minute is enforced
- [ ] AC-054: Rate limit exceeded triggers automatic 30-second pause
- [ ] AC-055: Rate limit is configurable via `yes.rate_limit.max_per_minute`
- [ ] AC-056: Pause duration is configurable via `yes.rate_limit.pause_seconds`
- [ ] AC-057: Rate limit message shows current count, limit, and time until reset
- [ ] AC-058: Rate limit applies per session, resets when session ends
- [ ] AC-059: Rate limit can be disabled with `yes.rate_limit.enabled: false`

### Audit Logging

- [ ] AC-060: Every bypass is logged with timestamp, session ID, operation, scope used
- [ ] AC-061: Scope specification is logged in full when session starts
- [ ] AC-062: Operation details logged: category, target path, risk level
- [ ] AC-063: Protected operation blocks are logged with reason
- [ ] AC-064: Rate limit triggers are logged
- [ ] AC-065: Session summary logged at session end: total bypasses, duration
- [ ] AC-066: Audit logs include structured JSON format for machine parsing
- [ ] AC-067: Audit logs are written even when operation is blocked

### CLI Integration

- [ ] AC-068: `--yes` flag is accepted on all commands that trigger approvals
- [ ] AC-069: `--yes-next=scope` sets one-time scope for next operation only
- [ ] AC-070: One-time scope is consumed after single use
- [ ] AC-071: `--no` flag explicitly denies all operations (no auto-approval)
- [ ] AC-072: `--interactive` flag forces interactive mode (always prompt)
- [ ] AC-073: `--ack-danger` flag required for `--yes=all` scope
- [ ] AC-074: `--yes-exclude=scope` excludes specific operations from default scope
- [ ] AC-075: Help text for `--yes` includes complete scope syntax documentation

### Error Handling and User Feedback

- [ ] AC-076: Invalid scope error shows exact position of syntax error
- [ ] AC-077: Unknown category error suggests closest valid category
- [ ] AC-078: Invalid modifier error lists valid modifiers for the category
- [ ] AC-079: Pattern complexity error explains specific limitation exceeded
- [ ] AC-080: Protected operation warning is clearly distinguished from regular prompts
- [ ] AC-081: Rate limit warning shows countdown to next available bypass
- [ ] AC-082: Acknowledgment prompt for `--yes=all` requires typing "I UNDERSTAND"

### Configuration Integration

- [ ] AC-083: `yes.default_scope` in `.agent/config.yml` sets default scopes
- [ ] AC-084: `yes.protected_operations` defines additional protected patterns
- [ ] AC-085: `yes.rate_limit` configures rate limiting parameters
- [ ] AC-086: `yes.require_ack_for_all` can be set to false to skip acknowledgment (not recommended)
- [ ] AC-087: Configuration validation rejects invalid scope syntax
- [ ] AC-088: Configuration validation warns when downgrading risk levels

### Session Management

- [ ] AC-089: Scope is session-scoped, never persisted between sessions
- [ ] AC-090: Session scope manager initializes with empty/default scope
- [ ] AC-091: Scope cannot be restored from previous session (intentional design)
- [ ] AC-092: `--yes` flag must be provided on each command invocation
- [ ] AC-093: Session statistics available for audit: bypass count, duration, scope used

### Performance Requirements

- [ ] AC-094: Scope parsing completes in < 1ms for typical scope strings
- [ ] AC-095: Scope validation completes in < 5ms including all checks
- [ ] AC-096: Pattern matching for single operation completes in < 100ms with timeout
- [ ] AC-097: Pattern complexity validation prevents DoS via exponential backtracking
- [ ] AC-098: Risk level lookup is O(1) from in-memory cache

### Security Mitigations

- [ ] AC-099: Scope injection guard rejects all shell metacharacters
- [ ] AC-100: Risk level downgrade attacks detected and blocked with security warning
- [ ] AC-101: Pattern complexity validator rejects patterns with > 3 recursive globs
- [ ] AC-102: Terminal command classifier checks deny list before allow list
- [ ] AC-103: Session scope manager disposed properly at session end

---

## Testing Requirements

### Unit Tests

```csharp
// Tests/Unit/Approvals/YesScope/ScopeParserTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Infrastructure.Approvals.Scoping;

namespace AgenticCoder.Tests.Unit.Approvals.YesScope;

public class ScopeParserTests
{
    private readonly ScopeParser _parser;

    public ScopeParserTests()
    {
        _parser = new ScopeParser();
    }

    [Fact]
    public void Should_Parse_Single_Scope()
    {
        // Arrange
        var input = "file_read";

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().HaveCount(1);
        result.Value.Entries[0].Category.Should().Be(OperationCategory.FileRead);
        result.Value.Entries[0].Modifier.Should().BeNull();
        result.Value.Entries[0].Pattern.Should().BeNull();
    }

    [Fact]
    public void Should_Parse_Multiple_Scopes_Comma_Separated()
    {
        // Arrange
        var input = "file_read,file_write,terminal";

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().HaveCount(3);
        result.Value.Entries[0].Category.Should().Be(OperationCategory.FileRead);
        result.Value.Entries[1].Category.Should().Be(OperationCategory.FileWrite);
        result.Value.Entries[2].Category.Should().Be(OperationCategory.Terminal);
    }

    [Fact]
    public void Should_Parse_Scope_With_Safe_Modifier()
    {
        // Arrange
        var input = "terminal:safe";

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().HaveCount(1);
        result.Value.Entries[0].Category.Should().Be(OperationCategory.Terminal);
        result.Value.Entries[0].Modifier.Should().Be("safe");
    }

    [Fact]
    public void Should_Parse_Scope_With_Glob_Pattern()
    {
        // Arrange
        var input = "file_write:*.test.ts";

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Entries.Should().HaveCount(1);
        result.Value.Entries[0].Category.Should().Be(OperationCategory.FileWrite);
        result.Value.Entries[0].Pattern.Should().Be("*.test.ts");
    }

    [Fact]
    public void Should_Reject_Invalid_Category()
    {
        // Arrange
        var input = "invalid_category";

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Unknown category");
    }

    [Fact]
    public void Should_Suggest_Correction_For_Typo()
    {
        // Arrange
        var input = "file_writ"; // Missing 'e'

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Did you mean 'file_write'");
    }

    [Fact]
    public void Should_Reject_Shell_Metacharacters()
    {
        // Arrange
        var input = "file_read;rm -rf /"; // Injection attempt

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("dangerous character");
    }

    [Fact]
    public void Should_Reject_Embedded_Flags()
    {
        // Arrange
        var input = "file_read --ack-danger"; // Space indicates injection

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("dangerous character 'space'");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Should_Return_Default_Scope_For_Empty_Input(string input)
    {
        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(YesScope.Default);
    }

    [Fact]
    public void Should_Enforce_Maximum_Entry_Count()
    {
        // Arrange - 21 entries exceeds limit of 20
        var input = string.Join(",", Enumerable.Repeat("file_read", 21));

        // Act
        var result = _parser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("maximum is 20");
    }
}

// Tests/Unit/Approvals/YesScope/RiskLevelClassifierTests.cs
public class RiskLevelClassifierTests
{
    private readonly RiskLevelClassifier _classifier;

    public RiskLevelClassifierTests()
    {
        _classifier = new RiskLevelClassifier();
    }

    [Theory]
    [InlineData(OperationCategory.FileRead, RiskLevel.Low)]
    [InlineData(OperationCategory.DirCreate, RiskLevel.Low)]
    [InlineData(OperationCategory.DirList, RiskLevel.Low)]
    public void Should_Classify_Level_1_Operations(OperationCategory category, RiskLevel expected)
    {
        // Act
        var result = _classifier.GetRiskLevel(category, "any/path");

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(OperationCategory.FileWrite)]
    public void Should_Classify_Level_2_Operations(OperationCategory category)
    {
        // Act
        var result = _classifier.GetRiskLevel(category, "src/file.ts");

        // Assert
        result.Should().Be(RiskLevel.Medium);
    }

    [Theory]
    [InlineData(OperationCategory.FileDelete)]
    [InlineData(OperationCategory.DirDelete)]
    [InlineData(OperationCategory.Config)]
    public void Should_Classify_Level_3_Operations(OperationCategory category)
    {
        // Act
        var result = _classifier.GetRiskLevel(category, "src/file.ts");

        // Assert
        result.Should().Be(RiskLevel.High);
    }

    [Theory]
    [InlineData(OperationCategory.FileDelete, ".git/config")]
    [InlineData(OperationCategory.FileDelete, ".git/HEAD")]
    [InlineData(OperationCategory.FileDelete, ".env")]
    [InlineData(OperationCategory.FileDelete, ".env.local")]
    public void Should_Always_Return_Critical_For_Protected_Paths(
        OperationCategory category, string path)
    {
        // Act
        var result = _classifier.GetRiskLevel(category, path);

        // Assert
        result.Should().Be(RiskLevel.Critical);
    }

    [Fact]
    public void Should_Prevent_Downgrade_Of_Critical_Operations()
    {
        // Arrange - pretend config tries to override
        var configOverride = new RiskLevelOverride(
            OperationCategory.FileDelete,
            ".git/**",
            RiskLevel.Low);

        // Act
        var result = _classifier.GetRiskLevel(
            OperationCategory.FileDelete,
            ".git/config",
            configOverride);

        // Assert - should still be Critical, not Low
        result.Should().Be(RiskLevel.Critical);
    }
}

// Tests/Unit/Approvals/YesScope/ScopePrecedenceTests.cs
public class ScopePrecedenceTests
{
    private readonly ScopeResolver _resolver;

    public ScopePrecedenceTests()
    {
        _resolver = new ScopeResolver(
            NullLogger<ScopeResolver>.Instance,
            new HardcodedCriticalOperations(NullLogger<HardcodedCriticalOperations>.Instance));
    }

    [Fact]
    public void Should_CLI_Override_Config_For_Allow_Decisions()
    {
        // Arrange
        var cliScope = YesScope.Parse("file_write").Value;
        var configScope = YesScope.Parse("file_read").Value;
        var operation = new Operation(OperationCategory.FileWrite, "src/test.ts");

        // Act
        var result = _resolver.CanBypass(operation, cliScope, configScope);

        // Assert
        result.Should().BeTrue("CLI scope includes file_write");
    }

    [Fact]
    public void Should_Config_Override_Default_Scope()
    {
        // Arrange
        var configScope = YesScope.Parse("file_write").Value;
        var defaultScope = YesScope.Default; // Only file_read
        var operation = new Operation(OperationCategory.FileWrite, "src/test.ts");

        // Act
        var result = _resolver.CanBypass(operation, null, configScope, defaultScope);

        // Assert
        result.Should().BeTrue("Config scope includes file_write");
    }

    [Fact]
    public void Should_Deny_Override_Allow_Always()
    {
        // Arrange
        var cliScope = YesScope.Parse("file_write").Value;
        var denyRule = new DenyRule(OperationCategory.FileWrite, "*.config");
        var operation = new Operation(OperationCategory.FileWrite, "app.config");

        // Act
        var result = _resolver.CanBypass(operation, cliScope, denyRules: new[] { denyRule });

        // Assert
        result.Should().BeFalse("Deny rules always override allow");
    }

    [Fact]
    public void Should_No_Flag_Override_All_Scopes()
    {
        // Arrange
        var cliScope = YesScope.Parse("all").Value;
        var operation = new Operation(OperationCategory.FileRead, "README.md");
        var noFlagSet = true;

        // Act
        var result = _resolver.CanBypass(operation, cliScope, noFlagSet: noFlagSet);

        // Assert
        result.Should().BeFalse("--no flag overrides all scopes");
    }

    [Fact]
    public void Should_Interactive_Flag_Force_Prompt()
    {
        // Arrange
        var cliScope = YesScope.Parse("file_read").Value;
        var operation = new Operation(OperationCategory.FileRead, "README.md");
        var interactiveMode = true;

        // Act
        var result = _resolver.CanBypass(operation, cliScope, interactiveMode: interactiveMode);

        // Assert
        result.Should().BeFalse("--interactive forces prompts");
    }
}

// Tests/Unit/Approvals/YesScope/TerminalClassifierTests.cs
public class TerminalClassifierTests
{
    private readonly TerminalOperationClassifier _classifier;

    public TerminalClassifierTests()
    {
        _classifier = new TerminalOperationClassifier(
            NullLogger<TerminalOperationClassifier>.Instance);
    }

    [Theory]
    [InlineData("git status")]
    [InlineData("git log")]
    [InlineData("git diff")]
    [InlineData("ls -la")]
    [InlineData("cat file.txt")]
    [InlineData("pwd")]
    public void Should_Classify_Safe_Commands_As_Low_Risk(string command)
    {
        // Act
        var result = _classifier.Classify(command);

        // Assert
        result.IsSafe.Should().BeTrue();
        result.RiskLevel.Should().Be(RiskLevel.Low);
    }

    [Theory]
    [InlineData("git push --force")]
    [InlineData("git push -f")]
    [InlineData("rm -rf /")]
    [InlineData("rm -rf ~")]
    [InlineData("sudo rm file")]
    public void Should_Classify_Dangerous_Commands_As_Critical(string command)
    {
        // Act
        var result = _classifier.Classify(command);

        // Assert
        result.IsDangerous.Should().BeTrue();
        result.RiskLevel.Should().Be(RiskLevel.Critical);
    }

    [Fact]
    public void Should_Check_Deny_List_Before_Allow_List()
    {
        // Arrange - command that looks safe but has dangerous flags
        var command = "git push origin main --force";

        // Act
        var result = _classifier.Classify(command);

        // Assert - deny list catches --force even though "git push" might seem normal
        result.IsDangerous.Should().BeTrue();
        result.Reason.Should().Contain("force");
    }

    [Fact]
    public void Should_Require_Approval_For_Unknown_Commands()
    {
        // Arrange
        var command = "some_custom_script.sh";

        // Act
        var result = _classifier.Classify(command);

        // Assert
        result.IsSafe.Should().BeFalse();
        result.IsDangerous.Should().BeFalse();
        result.RiskLevel.Should().Be(RiskLevel.High);
    }
}
```

### Integration Tests

```csharp
// Tests/Integration/Approvals/YesScope/ScopeApplicationTests.cs
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using AgenticCoder.Infrastructure.Approvals.Scoping;

namespace AgenticCoder.Tests.Integration.Approvals.YesScope;

public class ScopeApplicationTests : IDisposable
{
    private readonly SessionScopeManager _sessionManager;
    private readonly ScopeResolver _resolver;

    public ScopeApplicationTests()
    {
        _sessionManager = new SessionScopeManager(
            Guid.NewGuid(),
            NullLogger<SessionScopeManager>.Instance);
        _resolver = new ScopeResolver(
            NullLogger<ScopeResolver>.Instance,
            new HardcodedCriticalOperations(NullLogger<HardcodedCriticalOperations>.Instance));
    }

    [Fact]
    public async Task Should_Apply_Session_Scope_To_Multiple_Operations()
    {
        // Arrange
        var scope = YesScope.Parse("file_read,file_write").Value;
        _sessionManager.SetSessionScope(scope);

        var operations = new[]
        {
            new Operation(OperationCategory.FileRead, "src/test.ts"),
            new Operation(OperationCategory.FileWrite, "src/test.ts"),
            new Operation(OperationCategory.FileRead, "README.md"),
        };

        // Act & Assert
        foreach (var op in operations)
        {
            var currentScope = _sessionManager.GetScopeForOperation();
            var canBypass = _resolver.CanBypass(op, currentScope);
            canBypass.Should().BeTrue($"Session scope should cover {op.Category}");
        }

        // Verify session statistics
        var stats = _sessionManager.GetStatistics();
        stats.CurrentScope.Should().Be(scope);
    }

    [Fact]
    public async Task Should_Apply_OneTime_Scope_Then_Revert()
    {
        // Arrange
        var sessionScope = YesScope.Parse("file_read").Value;
        var oneTimeScope = YesScope.Parse("file_delete").Value;
        _sessionManager.SetSessionScope(sessionScope);

        // Act - set one-time scope
        _sessionManager.SetNextOperationScope(oneTimeScope);

        // First operation should use one-time scope
        var firstScope = _sessionManager.GetScopeForOperation();
        firstScope.Should().Be(oneTimeScope);

        // Second operation should use session scope (one-time consumed)
        var secondScope = _sessionManager.GetScopeForOperation();
        secondScope.Should().Be(sessionScope);
    }

    [Fact]
    public async Task Should_Clear_Operation_Scope_After_Use()
    {
        // Arrange
        var oneTimeScope = YesScope.Parse("file_delete").Value;
        _sessionManager.SetNextOperationScope(oneTimeScope);

        // Act - consume the one-time scope
        _ = _sessionManager.GetScopeForOperation();

        // Assert - subsequent calls return default
        var nextScope = _sessionManager.GetScopeForOperation();
        nextScope.Should().Be(YesScope.Default);
    }

    public void Dispose()
    {
        _sessionManager.Dispose();
    }
}

// Tests/Integration/Approvals/YesScope/ProtectedOperationTests.cs
public class ProtectedOperationTests
{
    private readonly HardcodedCriticalOperations _protections;
    private readonly ScopeResolver _resolver;

    public ProtectedOperationTests()
    {
        _protections = new HardcodedCriticalOperations(
            NullLogger<HardcodedCriticalOperations>.Instance);
        _resolver = new ScopeResolver(
            NullLogger<ScopeResolver>.Instance,
            _protections);
    }

    [Theory]
    [InlineData(".git/config")]
    [InlineData(".git/HEAD")]
    [InlineData(".git/objects/pack/pack-123.pack")]
    public void Should_Protect_Git_Directory_From_Bypass(string path)
    {
        // Arrange
        var scope = YesScope.Parse("all").Value; // Even "all" shouldn't bypass
        var operation = new Operation(OperationCategory.FileDelete, path);

        // Act
        var canBypass = _resolver.CanBypass(operation, scope);

        // Assert
        canBypass.Should().BeFalse($"Git path {path} should be protected");
    }

    [Theory]
    [InlineData(".agent/config.yml")]
    [InlineData(".acode/settings.json")]
    public void Should_Protect_Agent_Config_From_Bypass(string path)
    {
        // Arrange
        var scope = YesScope.Parse("all").Value;
        var operation = new Operation(OperationCategory.FileDelete, path);

        // Act
        var canBypass = _resolver.CanBypass(operation, scope);

        // Assert
        canBypass.Should().BeFalse($"Agent config {path} should be protected");
    }

    [Theory]
    [InlineData(".env")]
    [InlineData(".env.local")]
    [InlineData(".env.production")]
    public void Should_Protect_Environment_Files_From_Bypass(string path)
    {
        // Arrange
        var scope = YesScope.Parse("file_delete").Value;
        var operation = new Operation(OperationCategory.FileDelete, path);

        // Act
        var canBypass = _resolver.CanBypass(operation, scope);

        // Assert
        canBypass.Should().BeFalse($"Environment file {path} should be protected");
    }

    [Fact]
    public void Should_Block_Critical_Terminal_Commands()
    {
        // Arrange
        var scope = YesScope.Parse("terminal").Value;
        var operations = new[]
        {
            new Operation(OperationCategory.Terminal, "git push --force"),
            new Operation(OperationCategory.Terminal, "rm -rf /"),
            new Operation(OperationCategory.Terminal, "git reset --hard"),
        };

        // Act & Assert
        foreach (var op in operations)
        {
            var canBypass = _resolver.CanBypass(op, scope);
            canBypass.Should().BeFalse($"Critical command '{op.Target}' should be blocked");
        }
    }
}

// Tests/Integration/Approvals/YesScope/RateLimitTests.cs
public class RateLimitTests
{
    private readonly RateLimiter _rateLimiter;

    public RateLimitTests()
    {
        _rateLimiter = new RateLimiter(new RateLimitConfig
        {
            MaxPerMinute = 5, // Low limit for testing
            PauseSeconds = 1
        });
    }

    [Fact]
    public async Task Should_Allow_Bypasses_Within_Limit()
    {
        // Act & Assert - 5 bypasses should succeed
        for (int i = 0; i < 5; i++)
        {
            var result = _rateLimiter.TryBypass();
            result.IsAllowed.Should().BeTrue($"Bypass {i + 1} should be within limit");
        }
    }

    [Fact]
    public async Task Should_Block_When_Limit_Exceeded()
    {
        // Arrange - exhaust the limit
        for (int i = 0; i < 5; i++)
        {
            _rateLimiter.TryBypass();
        }

        // Act - 6th bypass should be blocked
        var result = _rateLimiter.TryBypass();

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.RetryAfter.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task Should_Reset_After_Pause_Period()
    {
        // Arrange - exhaust the limit
        for (int i = 0; i < 5; i++)
        {
            _rateLimiter.TryBypass();
        }

        // Wait for reset
        await Task.Delay(TimeSpan.FromSeconds(1.1));

        // Act - should be allowed again
        var result = _rateLimiter.TryBypass();

        // Assert
        result.IsAllowed.Should().BeTrue();
    }
}
```

### E2E Tests

```csharp
// Tests/E2E/Approvals/YesScope/YesScopingE2ETests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.CLI;

namespace AgenticCoder.Tests.E2E.Approvals.YesScope;

public class YesScopingE2ETests : IClassFixture<AcodeTestFixture>
{
    private readonly AcodeTestFixture _fixture;

    public YesScopingE2ETests(AcodeTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_Bypass_File_Read_With_Default_Yes()
    {
        // Arrange
        var testFile = _fixture.CreateTestFile("test.txt", "content");

        // Act
        var result = await _fixture.RunAcode(
            $"run \"Read {testFile}\" --yes");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Stdout.Should().NotContain("Approval required");
        result.AuditLog.Should().Contain("AUTO_APPROVED");
    }

    [Fact]
    public async Task Should_Prompt_For_File_Write_Without_Explicit_Scope()
    {
        // Arrange
        var testFile = _fixture.GetTestFilePath("new-file.txt");

        // Act - use bare --yes without file_write scope
        var result = await _fixture.RunAcode(
            $"run \"Create file {testFile}\" --yes",
            timeout: TimeSpan.FromSeconds(2)); // Will timeout waiting for prompt

        // Assert
        result.WasPromptShown.Should().BeTrue();
        result.PromptOperation.Should().Contain("file_write");
    }

    [Fact]
    public async Task Should_Bypass_File_Write_With_Explicit_Scope()
    {
        // Arrange
        var testFile = _fixture.GetTestFilePath("new-file.txt");

        // Act
        var result = await _fixture.RunAcode(
            $"run \"Create file {testFile}\" --yes=file_write");

        // Assert
        result.ExitCode.Should().Be(0);
        result.WasPromptShown.Should().BeFalse();
        result.AuditLog.Should().Contain("scope: file_write");
    }

    [Fact]
    public async Task Should_Block_Protected_Operation_Even_With_Yes_All()
    {
        // Arrange - .git directory always protected
        _fixture.CreateGitDirectory();

        // Act
        var result = await _fixture.RunAcode(
            "run \"Delete .git directory\" --yes=all --ack-danger");

        // Assert
        result.WasPromptShown.Should().BeTrue("Protected ops always prompt");
        result.PromptMessage.Should().Contain("PROTECTED");
        result.AuditLog.Should().Contain("BLOCKED_PROTECTED");
    }

    [Fact]
    public async Task Should_Enforce_Rate_Limit()
    {
        // Arrange
        _fixture.ConfigureRateLimit(maxPerMinute: 3, pauseSeconds: 5);

        // Act - try 5 quick bypasses
        for (int i = 0; i < 5; i++)
        {
            var result = await _fixture.RunAcode(
                $"run \"Read file{i}.txt\" --yes");

            if (i < 3)
            {
                result.ExitCode.Should().Be(0);
            }
            else
            {
                result.Stdout.Should().Contain("Rate limit exceeded");
                result.Stdout.Should().Contain("Pausing");
            }
        }
    }

    [Fact]
    public async Task Should_Require_Acknowledgment_For_Yes_All()
    {
        // Act - --yes=all without --ack-danger
        var result = await _fixture.RunAcode(
            "run \"Do everything\" --yes=all",
            timeout: TimeSpan.FromSeconds(2));

        // Assert
        result.Stderr.Should().Contain("--yes=all requires --ack-danger");
        result.ExitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task Should_Reject_Invalid_Scope_Syntax()
    {
        // Act
        var result = await _fixture.RunAcode(
            "run \"Test\" --yes=invalid;rm -rf /");

        // Assert
        result.ExitCode.Should().NotBe(0);
        result.Stderr.Should().Contain("dangerous character");
        // Command injection should not have executed
        _fixture.DirectoryExists("/").Should().BeTrue();
    }

    [Fact]
    public async Task Should_Log_All_Bypasses_To_Audit()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _fixture.SetSessionId(sessionId);

        // Act
        await _fixture.RunAcode("run \"Read file1.txt\" --yes");
        await _fixture.RunAcode("run \"Read file2.txt\" --yes");
        await _fixture.RunAcode("run \"Read file3.txt\" --yes");

        // Assert
        var auditEntries = _fixture.GetAuditEntriesForSession(sessionId);
        auditEntries.Should().HaveCount(3);
        auditEntries.All(e => e.Decision == "AUTO_APPROVED").Should().BeTrue();
        auditEntries.All(e => e.ScopeUsed == "default").Should().BeTrue();
    }
}
```

### Performance Benchmarks

```csharp
// Tests/Performance/Approvals/YesScope/ScopeBenchmarks.cs
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using AgenticCoder.Infrastructure.Approvals.Scoping;

namespace AgenticCoder.Tests.Performance.Approvals.YesScope;

[MemoryDiagnoser]
public class ScopeBenchmarks
{
    private ScopeParser _parser;
    private ScopePatternComplexityValidator _validator;
    private RiskLevelClassifier _classifier;
    private string _simpleScope;
    private string _complexScope;

    [GlobalSetup]
    public void Setup()
    {
        _parser = new ScopeParser();
        _validator = new ScopePatternComplexityValidator(
            NullLogger<ScopePatternComplexityValidator>.Instance);
        _classifier = new RiskLevelClassifier();
        _simpleScope = "file_read";
        _complexScope = "file_read,file_write:*.test.ts,terminal:safe,config";
    }

    [Benchmark]
    public void ParseSimpleScope()
    {
        _ = _parser.Parse(_simpleScope);
    }

    [Benchmark]
    public void ParseComplexScope()
    {
        _ = _parser.Parse(_complexScope);
    }

    [Benchmark]
    public void ValidatePattern()
    {
        _ = _validator.ValidatePattern("src/**/*.ts");
    }

    [Benchmark]
    public void ClassifyRiskLevel()
    {
        _ = _classifier.GetRiskLevel(OperationCategory.FileWrite, "src/test.ts");
    }
}

// Performance Targets:
// | Benchmark           | Target   | Maximum  |
// |---------------------|----------|----------|
// | ParseSimpleScope    | 0.3ms    | 1ms      |
// | ParseComplexScope   | 0.8ms    | 2ms      |
// | ValidatePattern     | 1ms      | 5ms      |
// | ClassifyRiskLevel   | 0.05ms   | 0.5ms    |
```

### Test Coverage Requirements

| Component | Target Coverage | Test Types |
|-----------|-----------------|------------|
| ScopeParser | 95% | Unit, Integration |
| ScopeValidator | 90% | Unit |
| ScopeResolver | 95% | Unit, Integration |
| RiskLevelClassifier | 95% | Unit |
| TerminalClassifier | 90% | Unit |
| SessionScopeManager | 90% | Unit, Integration |
| RateLimiter | 85% | Unit, Integration |
| HardcodedCriticalOperations | 100% | Unit |

### Regression Tests

```csharp
// Tests/Regression/YesScopeRegressionTests.cs
public class YesScopeRegressionTests
{
    [Fact]
    public void Regression_ScopeInjection_CVE2024001()
    {
        // This test ensures the scope injection vulnerability is mitigated
        var parser = new ScopeParser();

        // Attack vector: embedded flag injection
        var maliciousInput = "file_read,all --ack-danger";

        var result = parser.Parse(maliciousInput);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("dangerous");
    }

    [Fact]
    public void Regression_RiskLevelDowngrade_CVE2024002()
    {
        // This test ensures critical operations cannot be downgraded
        var protections = new HardcodedCriticalOperations(
            NullLogger<HardcodedCriticalOperations>.Instance);

        var maliciousConfig = new RiskLevelOverride(
            OperationCategory.FileDelete,
            ".git/**",
            RiskLevel.Low);

        var result = protections.ValidateRiskLevelConfiguration(
            new[] { maliciousConfig });

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("downgrade"));
    }

    [Fact]
    public void Regression_PatternDenialOfService_CVE2024003()
    {
        // This test ensures complex patterns are rejected
        var validator = new ScopePatternComplexityValidator(
            NullLogger<ScopePatternComplexityValidator>.Instance);

        var maliciousPattern = "**/**/**/**/**/*.ts"; // 5 recursive globs

        var result = validator.ValidatePattern(maliciousPattern);

        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("recursive globs");
    }
}
```

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

### Step 1: Create Domain Types (src/AgenticCoder.Domain/Approvals/)

#### OperationCategory Enum

```csharp
// src/AgenticCoder.Domain/Approvals/OperationCategory.cs
namespace AgenticCoder.Domain.Approvals;

/// <summary>
/// Categories of operations that can be scoped for --yes bypass.
/// </summary>
public enum OperationCategory
{
    /// <summary>Reading files - Risk Level 1 (Low)</summary>
    FileRead,

    /// <summary>Creating or modifying files - Risk Level 2 (Medium)</summary>
    FileWrite,

    /// <summary>Deleting files - Risk Level 3 (High)</summary>
    FileDelete,

    /// <summary>Creating directories - Risk Level 1 (Low)</summary>
    DirCreate,

    /// <summary>Deleting directories - Risk Level 3 (High)</summary>
    DirDelete,

    /// <summary>Listing directory contents - Risk Level 1 (Low)</summary>
    DirList,

    /// <summary>Executing shell commands - Risk Level 2-4 (varies)</summary>
    Terminal,

    /// <summary>Modifying configuration files - Risk Level 3 (High)</summary>
    Config,

    /// <summary>Searching codebase - Risk Level 1 (Low)</summary>
    Search
}
```

#### RiskLevel Enum

```csharp
// src/AgenticCoder.Domain/Approvals/RiskLevel.cs
namespace AgenticCoder.Domain.Approvals;

/// <summary>
/// Risk classification for operations. Higher levels require more explicit consent.
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Low risk - implicitly approved with bare --yes flag.
    /// Examples: file_read, dir_list, search
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium risk - requires explicit scope in --yes=scope.
    /// Examples: file_write, terminal:safe
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High risk - requires explicit scope AND displays warning.
    /// Examples: file_delete, dir_delete, config, terminal
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical - NEVER bypassable, always prompts regardless of --yes.
    /// Examples: .git deletion, .env deletion, git push --force
    /// </summary>
    Critical = 4
}
```

#### ScopeEntry Record

```csharp
// src/AgenticCoder.Domain/Approvals/ScopeEntry.cs
namespace AgenticCoder.Domain.Approvals;

/// <summary>
/// A single entry in a --yes scope specification.
/// Format: category[:modifier][:pattern]
/// </summary>
public sealed record ScopeEntry
{
    public OperationCategory Category { get; }
    public string? Modifier { get; }
    public string? Pattern { get; }

    public ScopeEntry(OperationCategory category, string? modifier = null, string? pattern = null)
    {
        Category = category;
        Modifier = modifier;
        Pattern = pattern;
    }

    /// <summary>
    /// Checks if this scope entry covers the given operation.
    /// </summary>
    public bool Covers(Operation operation)
    {
        // Category must match
        if (Category != operation.Category)
            return false;

        // If modifier is "safe", operation must be marked safe
        if (Modifier == "safe" && !operation.IsSafe)
            return false;

        // If modifier is "test", path must be in test directory
        if (Modifier == "test" && !IsTestPath(operation.Target))
            return false;

        // If pattern specified, path must match glob
        if (!string.IsNullOrEmpty(Pattern) && !MatchesGlob(operation.Target, Pattern))
            return false;

        return true;
    }

    private static bool IsTestPath(string path)
    {
        return path.Contains("/test/", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/tests/", StringComparison.OrdinalIgnoreCase) ||
               path.Contains(".test.", StringComparison.OrdinalIgnoreCase) ||
               path.Contains(".spec.", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesGlob(string path, string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*")
            .Replace("\\?", ".") + "$";
        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }

    public override string ToString()
    {
        var parts = new List<string> { Category.ToString().ToLowerInvariant() };
        if (!string.IsNullOrEmpty(Modifier)) parts.Add(Modifier);
        if (!string.IsNullOrEmpty(Pattern)) parts.Add(Pattern);
        return string.Join(":", parts);
    }
}
```

#### YesScope Value Object

```csharp
// src/AgenticCoder.Domain/Approvals/YesScope.cs
namespace AgenticCoder.Domain.Approvals;

/// <summary>
/// Immutable value object representing a --yes scope specification.
/// </summary>
public sealed record YesScope
{
    private readonly IReadOnlyList<ScopeEntry> _entries;

    public IReadOnlyList<ScopeEntry> Entries => _entries;
    public bool IsAll { get; }
    public bool IsNone { get; }

    private YesScope(IReadOnlyList<ScopeEntry> entries, bool isAll = false, bool isNone = false)
    {
        _entries = entries;
        IsAll = isAll;
        IsNone = isNone;
    }

    /// <summary>
    /// Default scope: file_read, dir_list, dir_create, search (Level 1 only)
    /// </summary>
    public static YesScope Default { get; } = new(new[]
    {
        new ScopeEntry(OperationCategory.FileRead),
        new ScopeEntry(OperationCategory.DirList),
        new ScopeEntry(OperationCategory.DirCreate),
        new ScopeEntry(OperationCategory.Search)
    });

    /// <summary>
    /// All scope - covers everything except Critical (Level 4) operations.
    /// Requires --ack-danger flag.
    /// </summary>
    public static YesScope All { get; } = new(Array.Empty<ScopeEntry>(), isAll: true);

    /// <summary>
    /// None scope - bypasses nothing, all operations prompt.
    /// </summary>
    public static YesScope None { get; } = new(Array.Empty<ScopeEntry>(), isNone: true);

    /// <summary>
    /// Creates a scope from a list of entries.
    /// </summary>
    public static YesScope From(IEnumerable<ScopeEntry> entries)
    {
        return new YesScope(entries.ToList());
    }

    /// <summary>
    /// Checks if this scope covers the given operation.
    /// </summary>
    public bool Covers(Operation operation)
    {
        if (IsNone) return false;
        if (IsAll) return operation.RiskLevel != RiskLevel.Critical;

        return _entries.Any(e => e.Covers(operation));
    }

    /// <summary>
    /// Combines this scope with another, returning a new scope with both entries.
    /// </summary>
    public YesScope Combine(YesScope other)
    {
        if (IsAll || other.IsAll) return All;
        if (IsNone) return other;
        if (other.IsNone) return this;

        return new YesScope(_entries.Concat(other._entries).ToList());
    }

    public override string ToString()
    {
        if (IsAll) return "all";
        if (IsNone) return "none";
        if (!_entries.Any()) return "default";
        return string.Join(",", _entries.Select(e => e.ToString()));
    }
}
```

#### Operation Record

```csharp
// src/AgenticCoder.Domain/Approvals/Operation.cs
namespace AgenticCoder.Domain.Approvals;

/// <summary>
/// Represents an operation that may require approval.
/// </summary>
public sealed record Operation
{
    public OperationCategory Category { get; }
    public string Target { get; }
    public RiskLevel RiskLevel { get; }
    public bool IsSafe { get; }
    public string Description { get; }

    public Operation(
        OperationCategory category,
        string target,
        RiskLevel? riskLevel = null,
        bool isSafe = false,
        string description = "")
    {
        Category = category;
        Target = target;
        RiskLevel = riskLevel ?? GetDefaultRiskLevel(category, target);
        IsSafe = isSafe;
        Description = description;
    }

    private static RiskLevel GetDefaultRiskLevel(OperationCategory category, string target)
    {
        // Check for critical paths first
        if (IsCriticalPath(target))
            return RiskLevel.Critical;

        return category switch
        {
            OperationCategory.FileRead => RiskLevel.Low,
            OperationCategory.DirCreate => RiskLevel.Low,
            OperationCategory.DirList => RiskLevel.Low,
            OperationCategory.Search => RiskLevel.Low,
            OperationCategory.FileWrite => RiskLevel.Medium,
            OperationCategory.FileDelete => RiskLevel.High,
            OperationCategory.DirDelete => RiskLevel.High,
            OperationCategory.Config => RiskLevel.High,
            OperationCategory.Terminal => RiskLevel.High,
            _ => RiskLevel.High
        };
    }

    private static bool IsCriticalPath(string path)
    {
        var criticalPatterns = new[]
        {
            ".git", ".git/", ".git\\",
            ".env", ".env.",
            ".agent", ".agent/", ".agent\\",
            ".acode", ".acode/", ".acode\\",
            "credentials", "secret", ".pem", ".key"
        };

        return criticalPatterns.Any(p =>
            path.Contains(p, StringComparison.OrdinalIgnoreCase));
    }
}
```

### Step 2: Create Application Interfaces (src/AgenticCoder.Application/Approvals/Scoping/)

```csharp
// src/AgenticCoder.Application/Approvals/Scoping/IScopeParser.cs
namespace AgenticCoder.Application.Approvals.Scoping;

/// <summary>
/// Parses --yes scope specifications from string format.
/// </summary>
public interface IScopeParser
{
    /// <summary>
    /// Parses a scope string into a YesScope object.
    /// </summary>
    /// <param name="input">Scope specification like "file_read,file_write:*.test.ts"</param>
    /// <returns>Result containing YesScope or error details</returns>
    Result<YesScope> Parse(string? input);
}

// src/AgenticCoder.Application/Approvals/Scoping/IScopeResolver.cs
namespace AgenticCoder.Application.Approvals.Scoping;

/// <summary>
/// Resolves effective scope from CLI, config, and defaults with precedence rules.
/// </summary>
public interface IScopeResolver
{
    /// <summary>
    /// Determines if an operation can be bypassed under the given scope.
    /// </summary>
    bool CanBypass(
        Operation operation,
        YesScope? cliScope,
        YesScope? configScope = null,
        YesScope? defaultScope = null,
        IEnumerable<DenyRule>? denyRules = null,
        bool noFlagSet = false,
        bool interactiveMode = false);

    /// <summary>
    /// Checks if an operation is protected (never bypassable).
    /// </summary>
    bool IsProtected(Operation operation);

    /// <summary>
    /// Gets the effective scope after applying precedence rules.
    /// </summary>
    YesScope GetEffectiveScope(
        YesScope? cliScope,
        YesScope? configScope,
        YesScope? defaultScope);
}

// src/AgenticCoder.Application/Approvals/Scoping/IRateLimiter.cs
namespace AgenticCoder.Application.Approvals.Scoping;

/// <summary>
/// Rate limits --yes bypasses to prevent runaway automation.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Attempts to record a bypass. Returns whether allowed.
    /// </summary>
    RateLimitResult TryBypass();

    /// <summary>
    /// Gets current rate limit status.
    /// </summary>
    RateLimitStatus GetStatus();

    /// <summary>
    /// Resets the rate limiter (e.g., at session end).
    /// </summary>
    void Reset();
}
```

### Step 3: Implement Infrastructure (src/AgenticCoder.Infrastructure/Approvals/Scoping/)

```csharp
// src/AgenticCoder.Infrastructure/Approvals/Scoping/ScopeParser.cs
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AgenticCoder.Infrastructure.Approvals.Scoping;

public sealed class ScopeParser : IScopeParser
{
    private readonly ILogger<ScopeParser> _logger;
    private readonly ScopeInjectionGuard _injectionGuard;

    private static readonly Dictionary<string, OperationCategory> CategoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["file_read"] = OperationCategory.FileRead,
        ["file_write"] = OperationCategory.FileWrite,
        ["file_delete"] = OperationCategory.FileDelete,
        ["dir_create"] = OperationCategory.DirCreate,
        ["dir_delete"] = OperationCategory.DirDelete,
        ["dir_list"] = OperationCategory.DirList,
        ["terminal"] = OperationCategory.Terminal,
        ["config"] = OperationCategory.Config,
        ["search"] = OperationCategory.Search,
        ["all"] = OperationCategory.FileRead, // Special handling
        ["none"] = OperationCategory.FileRead, // Special handling
        ["default"] = OperationCategory.FileRead // Special handling
    };

    public ScopeParser(ILogger<ScopeParser> logger)
    {
        _logger = logger;
        _injectionGuard = new ScopeInjectionGuard(
            NullLogger<ScopeInjectionGuard>.Instance);
    }

    public Result<YesScope> Parse(string? input)
    {
        // Empty input = default scope
        if (string.IsNullOrWhiteSpace(input))
        {
            return Result<YesScope>.Success(YesScope.Default);
        }

        // Validate for injection attacks first
        var injectionCheck = _injectionGuard.ValidateForInjection(input);
        if (!injectionCheck.IsValid)
        {
            return Result<YesScope>.Failure(
                "ACODE-YES-001",
                injectionCheck.Message ?? "Invalid scope syntax");
        }

        // Handle special scopes
        if (input.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (!injectionCheck.RequiresAck)
            {
                return Result<YesScope>.Failure(
                    "ACODE-YES-006",
                    "Scope 'all' requires --ack-danger flag");
            }
            return Result<YesScope>.Success(YesScope.All);
        }

        if (input.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return Result<YesScope>.Success(YesScope.None);
        }

        if (input.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            return Result<YesScope>.Success(YesScope.Default);
        }

        // Parse comma-separated entries
        var entries = new List<ScopeEntry>();
        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var entryResult = ParseEntry(part.Trim());
            if (!entryResult.IsSuccess)
            {
                return Result<YesScope>.Failure(entryResult.ErrorCode!, entryResult.ErrorMessage!);
            }
            entries.Add(entryResult.Value!);
        }

        _logger.LogDebug("Parsed scope: {Scope} with {Count} entries", input, entries.Count);

        return Result<YesScope>.Success(YesScope.From(entries));
    }

    private Result<ScopeEntry> ParseEntry(string entry)
    {
        // Format: category[:modifier][:pattern]
        var colonParts = entry.Split(':', 3);
        var categoryStr = colonParts[0];

        // Validate category
        if (!CategoryMap.TryGetValue(categoryStr, out var category))
        {
            var suggestion = FindClosestCategory(categoryStr);
            var message = $"Unknown category '{categoryStr}'.";
            if (suggestion != null)
            {
                message += $" Did you mean '{suggestion}'?";
            }
            return Result<ScopeEntry>.Failure("ACODE-YES-002", message);
        }

        string? modifier = null;
        string? pattern = null;

        if (colonParts.Length >= 2)
        {
            var second = colonParts[1];
            // Check if it's a known modifier or a pattern
            if (IsKnownModifier(second))
            {
                modifier = second;
                if (colonParts.Length >= 3)
                {
                    pattern = colonParts[2];
                }
            }
            else
            {
                // Treat as pattern
                pattern = second;
            }
        }

        return Result<ScopeEntry>.Success(new ScopeEntry(category, modifier, pattern));
    }

    private static bool IsKnownModifier(string value)
    {
        return value.Equals("safe", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("test", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("generated", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("all", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FindClosestCategory(string input)
    {
        var candidates = CategoryMap.Keys.ToList();
        return candidates
            .Select(c => (Category: c, Distance: LevenshteinDistance(input.ToLower(), c.ToLower())))
            .Where(x => x.Distance <= 3)
            .OrderBy(x => x.Distance)
            .Select(x => x.Category)
            .FirstOrDefault();
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        var m = s1.Length;
        var n = s2.Length;
        var d = new int[m + 1, n + 1];

        for (var i = 0; i <= m; i++) d[i, 0] = i;
        for (var j = 0; j <= n; j++) d[0, j] = j;

        for (var j = 1; j <= n; j++)
        {
            for (var i = 1; i <= m; i++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[m, n];
    }
}
```

### Step 4: Implement Scope Resolver

```csharp
// src/AgenticCoder.Infrastructure/Approvals/Scoping/ScopeResolver.cs
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.Approvals.Scoping;

public sealed class ScopeResolver : IScopeResolver
{
    private readonly ILogger<ScopeResolver> _logger;
    private readonly HardcodedCriticalOperations _criticalOps;

    public ScopeResolver(
        ILogger<ScopeResolver> logger,
        HardcodedCriticalOperations criticalOps)
    {
        _logger = logger;
        _criticalOps = criticalOps;
    }

    public bool CanBypass(
        Operation operation,
        YesScope? cliScope,
        YesScope? configScope = null,
        YesScope? defaultScope = null,
        IEnumerable<DenyRule>? denyRules = null,
        bool noFlagSet = false,
        bool interactiveMode = false)
    {
        // Precedence 1: --no flag blocks everything
        if (noFlagSet)
        {
            _logger.LogDebug("Bypass blocked: --no flag set");
            return false;
        }

        // Precedence 2: --interactive forces prompts
        if (interactiveMode)
        {
            _logger.LogDebug("Bypass blocked: --interactive mode");
            return false;
        }

        // Precedence 3: Protected operations never bypass
        if (IsProtected(operation))
        {
            _logger.LogDebug("Bypass blocked: protected operation {Target}", operation.Target);
            return false;
        }

        // Precedence 4: Critical risk level never bypasses
        if (operation.RiskLevel == RiskLevel.Critical)
        {
            _logger.LogDebug("Bypass blocked: critical risk level for {Target}", operation.Target);
            return false;
        }

        // Precedence 5: Deny rules block
        if (denyRules != null)
        {
            foreach (var rule in denyRules)
            {
                if (rule.Matches(operation))
                {
                    _logger.LogDebug("Bypass blocked: deny rule {Rule} matched", rule);
                    return false;
                }
            }
        }

        // Precedence 6: Apply scope hierarchy (CLI > config > default)
        var effectiveScope = GetEffectiveScope(cliScope, configScope, defaultScope ?? YesScope.Default);

        // Check if scope covers the operation
        var canBypass = effectiveScope.Covers(operation);

        _logger.LogDebug(
            "Bypass {Result} for {Category}:{Target} with scope {Scope}",
            canBypass ? "allowed" : "blocked",
            operation.Category,
            operation.Target,
            effectiveScope);

        return canBypass;
    }

    public bool IsProtected(Operation operation)
    {
        return _criticalOps.IsCriticalOperation(operation.Category, operation.Target);
    }

    public YesScope GetEffectiveScope(
        YesScope? cliScope,
        YesScope? configScope,
        YesScope? defaultScope)
    {
        // CLI scope takes precedence if provided
        if (cliScope != null && !cliScope.IsNone)
        {
            return cliScope;
        }

        // Config scope next
        if (configScope != null && !configScope.IsNone)
        {
            return configScope;
        }

        // Fall back to default
        return defaultScope ?? YesScope.Default;
    }
}
```

### Step 5: Implement Rate Limiter

```csharp
// src/AgenticCoder.Infrastructure/Approvals/Scoping/RateLimiter.cs
namespace AgenticCoder.Infrastructure.Approvals.Scoping;

public sealed class RateLimiter : IRateLimiter
{
    private readonly RateLimitConfig _config;
    private readonly object _lock = new();
    private int _count;
    private DateTimeOffset _windowStart;

    public RateLimiter(RateLimitConfig config)
    {
        _config = config;
        _windowStart = DateTimeOffset.UtcNow;
        _count = 0;
    }

    public RateLimitResult TryBypass()
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;

            // Check if window has expired and reset
            if (now - _windowStart > TimeSpan.FromMinutes(1))
            {
                _windowStart = now;
                _count = 0;
            }

            // Check if within limit
            if (_count < _config.MaxPerMinute)
            {
                _count++;
                return RateLimitResult.Allowed(_count, _config.MaxPerMinute);
            }

            // Rate limit exceeded
            var retryAfter = TimeSpan.FromSeconds(_config.PauseSeconds);
            return RateLimitResult.Exceeded(_count, _config.MaxPerMinute, retryAfter);
        }
    }

    public RateLimitStatus GetStatus()
    {
        lock (_lock)
        {
            return new RateLimitStatus(_count, _config.MaxPerMinute, _windowStart);
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _count = 0;
            _windowStart = DateTimeOffset.UtcNow;
        }
    }
}

public sealed record RateLimitConfig
{
    public int MaxPerMinute { get; init; } = 100;
    public int PauseSeconds { get; init; } = 30;
    public bool Enabled { get; init; } = true;
}

public sealed record RateLimitResult
{
    public bool IsAllowed { get; }
    public int CurrentCount { get; }
    public int MaxAllowed { get; }
    public TimeSpan RetryAfter { get; }

    private RateLimitResult(bool allowed, int count, int max, TimeSpan retry)
    {
        IsAllowed = allowed;
        CurrentCount = count;
        MaxAllowed = max;
        RetryAfter = retry;
    }

    public static RateLimitResult Allowed(int count, int max) =>
        new(true, count, max, TimeSpan.Zero);

    public static RateLimitResult Exceeded(int count, int max, TimeSpan retry) =>
        new(false, count, max, retry);
}

public sealed record RateLimitStatus(int CurrentCount, int MaxPerMinute, DateTimeOffset WindowStart);
```

### Step 6: Add CLI Options Integration

```csharp
// src/AgenticCoder.CLI/Options/YesOptions.cs
using System.CommandLine;

namespace AgenticCoder.CLI.Options;

public static class YesOptions
{
    public static Option<string?> YesOption { get; } = new(
        aliases: new[] { "--yes", "-y" },
        description: "Auto-approve operations matching scope. Default: file_read,dir_list,search");

    public static Option<string?> YesNextOption { get; } = new(
        aliases: new[] { "--yes-next" },
        description: "Auto-approve next operation only with specified scope");

    public static Option<string?> YesExcludeOption { get; } = new(
        aliases: new[] { "--yes-exclude" },
        description: "Exclude operations from auto-approval");

    public static Option<bool> NoOption { get; } = new(
        aliases: new[] { "--no", "-n" },
        description: "Deny all operations (no auto-approval)");

    public static Option<bool> InteractiveOption { get; } = new(
        aliases: new[] { "--interactive", "-i" },
        description: "Force interactive mode (always prompt)");

    public static Option<bool> AckDangerOption { get; } = new(
        aliases: new[] { "--ack-danger" },
        description: "Acknowledge danger for --yes=all");

    public static void AddYesOptionsToCommand(Command command)
    {
        command.AddOption(YesOption);
        command.AddOption(YesNextOption);
        command.AddOption(YesExcludeOption);
        command.AddOption(NoOption);
        command.AddOption(InteractiveOption);
        command.AddOption(AckDangerOption);
    }
}
```

### Error Codes Reference

| Code | Meaning | User Action |
|------|---------|-------------|
| ACODE-YES-001 | Invalid scope syntax | Check scope format: category[:modifier][:pattern] |
| ACODE-YES-002 | Unknown category | Use: file_read, file_write, file_delete, terminal, config |
| ACODE-YES-003 | Invalid modifier | Use: safe, test, generated, or a glob pattern |
| ACODE-YES-004 | Protected operation | Cannot bypass, must approve manually |
| ACODE-YES-005 | Rate limit exceeded | Wait for cooldown period |
| ACODE-YES-006 | Missing --ack-danger | Add --ack-danger flag with --yes=all |

### DI Registration

```csharp
// src/AgenticCoder.Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddYesScopingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register scoping services
        services.AddSingleton<IScopeParser, ScopeParser>();
        services.AddSingleton<IScopeResolver, ScopeResolver>();
        services.AddSingleton<HardcodedCriticalOperations>();
        services.AddSingleton<ScopeInjectionGuard>();
        services.AddSingleton<ScopePatternComplexityValidator>();
        services.AddSingleton<TerminalOperationClassifier>();

        // Configure rate limiting from config
        services.Configure<RateLimitConfig>(
            configuration.GetSection("Yes:RateLimit"));
        services.AddSingleton<IRateLimiter, RateLimiter>();

        // Session scope manager is scoped per session
        services.AddScoped<SessionScopeManager>();
        services.AddSingleton<SessionScopeManagerFactory>();

        return services;
    }
}
```

### Implementation Checklist

1. [ ] Create OperationCategory enum (src/AgenticCoder.Domain/Approvals/OperationCategory.cs)
2. [ ] Create RiskLevel enum (src/AgenticCoder.Domain/Approvals/RiskLevel.cs)
3. [ ] Create ScopeEntry record (src/AgenticCoder.Domain/Approvals/ScopeEntry.cs)
4. [ ] Create YesScope value object (src/AgenticCoder.Domain/Approvals/YesScope.cs)
5. [ ] Create Operation record (src/AgenticCoder.Domain/Approvals/Operation.cs)
6. [ ] Create IScopeParser interface (src/AgenticCoder.Application/Approvals/Scoping/)
7. [ ] Create IScopeResolver interface (src/AgenticCoder.Application/Approvals/Scoping/)
8. [ ] Create IRateLimiter interface (src/AgenticCoder.Application/Approvals/Scoping/)
9. [ ] Implement ScopeParser (src/AgenticCoder.Infrastructure/Approvals/Scoping/)
10. [ ] Implement ScopeResolver (src/AgenticCoder.Infrastructure/Approvals/Scoping/)
11. [ ] Implement RateLimiter (src/AgenticCoder.Infrastructure/Approvals/Scoping/)
12. [ ] Implement ScopeInjectionGuard (from Security section)
13. [ ] Implement HardcodedCriticalOperations (from Security section)
14. [ ] Implement ScopePatternComplexityValidator (from Security section)
15. [ ] Implement TerminalOperationClassifier (from Security section)
16. [ ] Implement SessionScopeManager (from Security section)
17. [ ] Add CLI options (src/AgenticCoder.CLI/Options/YesOptions.cs)
18. [ ] Register DI services
19. [ ] Write unit tests for all components
20. [ ] Write integration tests
21. [ ] Write E2E tests
22. [ ] Verify performance benchmarks meet targets

### Validation Checklist Before Merge

- [ ] All scope parsing tests pass
- [ ] All precedence tests pass
- [ ] All protection tests pass
- [ ] All rate limit tests pass
- [ ] All security mitigation tests pass
- [ ] Performance benchmarks meet targets
- [ ] Unit test coverage > 90%
- [ ] No security vulnerabilities detected
- [ ] Documentation updated

---

**End of Task 013.c Specification**