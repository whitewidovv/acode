# Task 013.a: Gate Rules + Prompts

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 013 (Human Approval Gates), Task 002 (Config Schema)  

---

## Description

Task 013.a implements the rule engine and prompt system for human approval gates—the configurable intelligence layer that determines when operations require human oversight and how users interact with approval requests. This is a critical infrastructure component that bridges policy definition with user experience, enabling Acode to be both safe-by-default and highly customizable for different workflows.

### Business Value and ROI

**Quantified Benefits:**

1. **Reduced Approval Fatigue Costs: $45,000/year**
   - Without configurable rules, every file operation prompts for approval
   - Average developer sees 200+ prompts per day without rules
   - Each unnecessary prompt costs 3 seconds of attention = 600 seconds/day wasted
   - With rule engine: 85% of prompts eliminated via auto-approve rules for safe patterns
   - 600 seconds × 0.85 × 250 workdays × $50/hour = $45,625/year saved per developer
   - For a 10-person team: **$456,250/year** in recovered productivity

2. **Prevented Accidental Damage: $75,000/year**
   - Configurable deny rules prevent common mistakes
   - Rules like `deny: delete src/**/*.ts` prevent accidental source deletion
   - Average cost of recovering from accidental file deletion: $2,500 (developer time, git recovery, testing)
   - Rules prevent approximately 30 incidents per year across a team
   - 30 × $2,500 = **$75,000/year** in incident prevention

3. **Improved Security Posture: $120,000/year**
   - Secret redaction in prompts prevents accidental exposure
   - Pattern-based detection catches API keys, passwords, tokens
   - Average cost of a credential exposure incident: $15,000 (rotation, audit, potential breach)
   - Redaction prevents approximately 8 exposures per year
   - 8 × $15,000 = **$120,000/year** in security incident savings

4. **Compliance and Audit Efficiency: $35,000/year**
   - Rule configurations serve as documented approval policies
   - Auditors can review `.agent/config.yml` to understand approval behavior
   - Reduces audit preparation time from 40 hours to 8 hours per audit
   - 32 hours × 4 audits/year × $275/hour = **$35,200/year** saved

**Total ROI: $686,250/year for a 10-person development team**

### Technical Architecture

#### Rule Engine Architecture

The rule engine is the decision-making core that evaluates each operation against a set of configured rules to determine the appropriate approval policy. It operates on a "first match wins" principle, evaluating rules in definition order until a match is found.

```
┌─────────────────────────────────────────────────────────────────┐
│                      Rule Engine Pipeline                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Operation     ┌─────────────┐    ┌─────────────┐    Policy     │
│  ──────────────│  Rule       │────│  Pattern    │──────────────│
│                │  Loader     │    │  Matcher    │               │
│                └──────┬──────┘    └──────┬──────┘               │
│                       │                  │                       │
│                       ▼                  ▼                       │
│                ┌─────────────┐    ┌─────────────┐               │
│                │  Built-in   │    │  Glob       │               │
│                │  Rules      │    │  Matcher    │               │
│                └─────────────┘    └─────────────┘               │
│                                                                  │
│                ┌─────────────┐    ┌─────────────┐               │
│                │  Custom     │    │  Regex      │               │
│                │  Rules      │    │  Matcher    │               │
│                └─────────────┘    └─────────────┘               │
│                                                                  │
│                       │                  │                       │
│                       ▼                  ▼                       │
│                ┌──────────────────────────────────┐             │
│                │        Rule Evaluator            │             │
│                │  - Iterate rules in order        │             │
│                │  - First match determines policy │             │
│                │  - Log evaluation for audit      │             │
│                └──────────────────────────────────┘             │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

**Key Components:**

1. **RuleLoader**: Parses rules from YAML configuration, validates schemas, merges with built-in defaults
2. **PatternMatcher Interface**: Abstracts pattern matching strategies (glob, regex, exact)
3. **GlobMatcher**: Implements file path pattern matching using glob syntax (`**/*.ts`, `src/*`)
4. **RegexMatcher**: Implements command pattern matching using regular expressions
5. **RuleEvaluator**: Orchestrates rule evaluation, implements first-match-wins logic

#### Prompt System Architecture

The prompt system is the user-facing interface for approval interactions. It renders informative prompts, captures user input, and integrates with the approval gate framework.

```
┌─────────────────────────────────────────────────────────────────┐
│                      Prompt System Architecture                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                  ApprovalPromptRenderer                   │   │
│  │  - Orchestrates prompt display                           │   │
│  │  - Manages timeout countdown                             │   │
│  │  - Captures user input                                   │   │
│  └────────────────────────┬─────────────────────────────────┘   │
│                           │                                      │
│           ┌───────────────┼───────────────┐                     │
│           ▼               ▼               ▼                     │
│  ┌────────────┐   ┌────────────┐   ┌────────────┐              │
│  │  Header    │   │  Preview   │   │  Options   │              │
│  │  Component │   │  Component │   │  Component │              │
│  └────────────┘   └────────────┘   └────────────┘              │
│           │               │               │                     │
│           │               ▼               │                     │
│           │       ┌────────────┐          │                     │
│           │       │  Secret    │          │                     │
│           │       │  Redactor  │          │                     │
│           │       └────────────┘          │                     │
│           │               │               │                     │
│           └───────────────┼───────────────┘                     │
│                           ▼                                      │
│                   ┌────────────┐                                 │
│                   │  Console   │                                 │
│                   │  Output    │                                 │
│                   └────────────┘                                 │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

**Key Components:**

1. **ApprovalPromptRenderer**: Main orchestrator that composes all prompt elements and manages the approval flow
2. **HeaderComponent**: Renders the warning header with operation type and status indicators
3. **PreviewComponent**: Displays operation-specific content (file previews, command details)
4. **OptionsComponent**: Shows available actions ([A]pprove, [D]eny, etc.) with visual emphasis
5. **TimeoutComponent**: Displays countdown timer with visual warnings as time runs low
6. **SecretRedactor**: Scans preview content and replaces detected secrets with [REDACTED]

#### Rule Evaluation Flow

```
┌──────────────────────────────────────────────────────────────────────┐
│                    Rule Evaluation Sequence                           │
├──────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  1. Operation Received                                                │
│     │                                                                 │
│     ▼                                                                 │
│  2. Extract Operation Attributes                                      │
│     - Category (FILE_WRITE, TERMINAL_COMMAND, etc.)                  │
│     - Path (for file operations)                                      │
│     - Command (for terminal operations)                               │
│     │                                                                 │
│     ▼                                                                 │
│  3. Load Applicable Rules                                             │
│     - Custom rules from .agent/config.yml                            │
│     - Built-in default rules                                         │
│     - Order: Custom first, then defaults                             │
│     │                                                                 │
│     ▼                                                                 │
│  4. Iterate Rules (First Match Wins)                                  │
│     FOR each rule in order:                                          │
│       IF rule.category matches operation.category:                   │
│         IF rule.pattern matches operation.path/command:              │
│           RETURN rule.policy                                         │
│     │                                                                 │
│     ▼                                                                 │
│  5. No Match - Apply Default Policy                                   │
│     - Default is configurable (usually PROMPT)                       │
│     │                                                                 │
│     ▼                                                                 │
│  6. Return ApprovalPolicy                                             │
│     - AUTO_APPROVE: Operation proceeds without prompt                │
│     - PROMPT: Show approval prompt, wait for user                    │
│     - DENY: Block operation, no prompt                               │
│     - SKIP: Skip operation, continue session                         │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘
```

#### Pattern Matching Strategies

**Glob Patterns (for file paths):**
- `**` - Matches any path segments (recursive)
- `*` - Matches any characters within a single path segment
- `?` - Matches exactly one character
- `{a,b}` - Matches either 'a' or 'b' (alternation)
- `[abc]` - Matches any character in the set

Examples:
- `**/*.ts` - All TypeScript files in any directory
- `src/**` - Everything under the src directory
- `*.test.{ts,js}` - Test files in root with .ts or .js extension
- `config.[jy][sa]ml` - Matches config.yaml, config.yml, config.json, config.jsml

**Regex Patterns (for commands):**
- Full regex syntax supported
- Anchors (`^`, `$`) for exact matching
- Character classes for flexible matching
- Groups for complex patterns

Examples:
- `^npm (install|ci)$` - Exactly "npm install" or "npm ci"
- `^git push` - Any git push command
- `^rm -rf` - Any recursive force remove (dangerous!)

#### Prompt Lifecycle

```
┌──────────────────────────────────────────────────────────────────────┐
│                       Prompt Display Lifecycle                        │
├──────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  1. Render Header                                                     │
│     ⚠ Approval Required                                              │
│     ─────────────────────────────────────                            │
│                                                                       │
│  2. Render Operation Details                                          │
│     Operation: WRITE FILE                                            │
│     Path: src/components/Button.tsx                                  │
│     Size: 45 lines (new file)                                        │
│                                                                       │
│  3. Render Preview (with redaction)                                   │
│     Preview:                                                         │
│        1 | import React from 'react';                                │
│        2 | const apiKey = '[REDACTED]';                              │
│        3 | export const Button = () => {                             │
│      ... | (42 more lines)                                           │
│                                                                       │
│  4. Render Options                                                    │
│     [A]pprove  [D]eny  [S]kip  [V]iew all  [?]Help                  │
│                                                                       │
│  5. Render Timeout                                                    │
│     Timeout: 4:32 remaining                                          │
│                                                                       │
│  6. Wait for Input                                                    │
│     Choice: _                                                        │
│                                                                       │
│  7. Handle Input                                                      │
│     - Valid key → Return decision                                    │
│     - Invalid key → Re-prompt                                        │
│     - Timeout → Apply timeout policy                                 │
│     - Ctrl+C → Treat as deny                                         │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘
```

### Integration Points

#### Integration with Task 013 (Human Approval Gates)
- Rule engine is called by the gate framework to determine policy
- Prompt system is invoked when policy is PROMPT
- Results flow back to gate for decision enforcement

#### Integration with Task 002 (Config Schema)
- Rules are stored in `.agent/config.yml` under `approvals.rules`
- Schema validation ensures rule syntax is correct
- Config hot-reloading updates rules without restart

#### Integration with Task 010 (CLI Framework)
- Prompt components use CLI rendering primitives
- Color and formatting consistent with CLI theme
- Terminal capabilities detected for graceful degradation

### Design Decisions and Trade-offs

**Decision 1: First-Match-Wins vs. Most-Specific-Wins**
- Chose first-match-wins for simplicity and predictability
- Users control precedence by ordering rules
- More intuitive than complex specificity calculations
- Trade-off: Users must carefully order rules

**Decision 2: Pattern Types - Glob vs. Regex**
- Glob for file paths (familiar from .gitignore, shells)
- Regex for commands (more power needed for command matching)
- Trade-off: Two syntaxes to learn, but each is natural for its domain

**Decision 3: Prompt Timeout Behavior**
- Timeout defaults to deny (fail-secure)
- Configurable per rule or globally
- Trade-off: Timeout can block automation, but safety is paramount

**Decision 4: Secret Redaction Scope**
- Redaction in preview only, not in actual operation
- User can still view full content via [V]iew option
- Trade-off: Curious users can bypass, but usability preserved

### Constraints and Limitations

**Technical Constraints:**
- Rules are stateless - no memory of previous evaluations
- Pattern matching is per-operation, no cross-operation rules
- Prompt requires TTY - non-interactive environments must use --yes

**Operational Constraints:**
- Maximum 1000 custom rules (performance consideration)
- Rule names must be unique within configuration
- Patterns must be valid glob/regex (validated at config load)

**Security Constraints:**
- Rules cannot grant permissions beyond category defaults
- Deny rules cannot be overridden by subsequent rules
- Protected paths block even when rules say auto-approve

### Performance Characteristics

- Rule loading: O(n) where n = number of rules, < 50ms for 1000 rules
- Rule evaluation: O(n) worst case, typically early match
- Pattern compilation: One-time cost at config load
- Prompt rendering: < 50ms including redaction

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Rule | Condition + policy mapping |
| Rule Engine | Evaluator for approval rules |
| Pattern | Matching expression (glob/regex) |
| Glob Pattern | File path matching syntax |
| Regex Pattern | Regular expression matching |
| Prompt | User interaction for approval |
| Preview | Abbreviated content display |
| Redaction | Hiding sensitive data |
| Key Binding | Single-key command |
| Default Action | Pre-selected option |
| Help Text | Explanation of options |
| Precedence | Order of rule evaluation |
| First Match Wins | First matching rule applies |
| Built-in Rules | Provided default rules |
| Custom Rules | User-defined rules |

---

## Use Cases

### Use Case 1: Sarah the Security-Conscious Developer

**Persona:** Sarah Chen, Senior Software Engineer at a fintech startup. She works on payment processing code that handles credit card data. She's meticulous about security and wants to ensure no sensitive data is accidentally committed or exposed.

**Before Acode with Rules/Prompts:**
Sarah uses Acode without custom rules configured. Every single file operation prompts for approval—reading config files, writing test stubs, even creating empty directories. She sees 150+ prompts per day, causing "approval fatigue." Eventually she starts blindly pressing 'A' to approve everything, defeating the purpose of the safety system. One day, she accidentally approves writing an API key into a source file, which gets committed and pushed before she notices.

**After Acode with Rules/Prompts:**
Sarah configures rules tailored to her security needs:

```yaml
approvals:
  rules:
    # Auto-approve reading any file (safe)
    - name: allow-reads
      operation: file_read
      pattern: "**/*"
      policy: auto

    # Auto-approve test files (isolated)
    - name: allow-tests
      operation: file_write
      pattern: "**/*.test.ts"
      policy: auto

    # ALWAYS prompt for payment-related code
    - name: prompt-payments
      operation: file_write
      pattern: "src/payments/**"
      policy: prompt

    # DENY any write to env files
    - name: deny-env
      operation: file_write
      pattern: "**/.env*"
      policy: deny
```

Now Sarah only sees prompts for operations that genuinely need review—writes to payment code and other sensitive areas. When a prompt appears, she pays attention because she knows it's important. The secret redaction shows "[REDACTED]" for any API keys in previews, so even if she accidentally generates code with secrets, she'll notice before approving.

**Measurable Improvement:**
- Prompts per day: 150 → 15 (90% reduction)
- False approvals due to fatigue: ~5/month → 0
- Security incidents prevented: 1 credential exposure prevented ($15,000 saved)
- Developer satisfaction: "Approvals are meaningful now, not noise"

---

### Use Case 2: Marcus the Automation Engineer

**Persona:** Marcus Johnson, DevOps Engineer responsible for CI/CD pipelines and infrastructure automation. He runs Acode in automated pipelines but also uses it interactively for infrastructure changes.

**Before Acode with Rules/Prompts:**
Marcus's CI/CD pipelines hang whenever Acode encounters an approval prompt. The pipeline just waits forever for input that will never come. He uses `--yes` everywhere, but this bypasses all safety checks. One night, an automated job deletes an important configuration directory because `--yes` approved the deletion without any human review. The incident takes 4 hours to diagnose and recover.

**After Acode with Rules/Prompts:**
Marcus configures environment-specific rules:

```yaml
approvals:
  rules:
    # In CI: auto-approve safe operations
    - name: ci-reads
      operation: file_read
      pattern: "**/*"
      policy: auto

    - name: ci-test-writes
      operation: file_write
      pattern: "**/*.test.ts"
      policy: auto

    # In CI: DENY deletions (must be explicit)
    - name: ci-deny-delete
      operation: file_delete
      pattern: "**/*"
      policy: deny

    # In CI: auto-approve whitelisted commands
    - name: ci-npm
      operation: terminal_command
      command: "^npm (install|ci|test|build)$"
      policy: auto

  # Non-interactive behavior
  non_interactive_policy: deny
```

When running in CI (detected via `CI=true` environment variable), Acode applies these rules. Safe operations auto-approve, dangerous operations are denied (failing the pipeline rather than waiting forever), and only whitelisted commands run. Interactive sessions still prompt for review.

**Measurable Improvement:**
- CI pipeline hangs: Eliminated (was 2-3/week)
- Accidental deletions in automation: 0 (was 1/quarter)
- Pipeline execution time: 15% faster (no unnecessary prompts)
- Mean time to recovery from automation incidents: 0 hours (no incidents)

---

### Use Case 3: Jordan the Team Lead

**Persona:** Jordan Rivera, Engineering Team Lead managing a team of 8 developers working on a large TypeScript codebase. Jordan needs to balance productivity with consistent safety practices across the team.

**Before Acode with Rules/Prompts:**
Each developer on Jordan's team configures Acode differently. Some have no rules, approving everything manually. Others use `--yes` for everything. There's no consistency, and code reviews reveal that different team members have different risk tolerances. A junior developer accidentally approves a file delete that removes a critical utility library, causing a 2-hour debugging session for the whole team.

**After Acode with Rules/Prompts:**
Jordan creates a team-standard `.agent/config.yml` that's committed to the repository:

```yaml
# Team-wide approval rules (committed to repo)
approvals:
  rules:
    # Standard safe operations
    - name: team-reads
      operation: file_read
      pattern: "**/*"
      policy: auto

    - name: team-tests
      operation: file_write
      pattern: "**/*.{test,spec}.{ts,tsx}"
      policy: auto

    - name: team-generated
      operation: file_write
      pattern: "**/generated/**"
      policy: auto

    # Always prompt for source changes
    - name: team-src
      operation: file_write
      pattern: "src/**"
      policy: prompt

    # Always prompt for config changes
    - name: team-config
      operation: file_write
      pattern: "**/*.config.*"
      policy: prompt

    # DENY deletion of source files
    - name: team-no-delete-src
      operation: file_delete
      pattern: "src/**"
      policy: deny

    # DENY modification of package-lock
    - name: team-lock-packagelock
      operation: file_write
      pattern: "package-lock.json"
      policy: deny
      prompt_message: "Direct package-lock.json edits are forbidden. Use npm install."
```

Every team member gets the same rules via version control. New team members automatically inherit the team's safety practices. The deny rule for source deletion prevents the utility library incident from recurring. Custom prompt messages explain *why* certain operations are blocked.

**Measurable Improvement:**
- Approval configuration consistency: 100% (was 0%)
- Onboarding time for safety practices: 0 hours (automatic via repo)
- Team incidents from bad approvals: 0/quarter (was 2/quarter)
- Code review comments about unsafe operations: Eliminated
- Estimated annual savings: $25,000 (avoided debugging, faster onboarding)

---

## Out of Scope

The following items are explicitly excluded from Task 013.a:

- **Approval persistence** - Task 013.b
- **--yes scoping** - Task 013.c
- **Remote prompts** - Local CLI only
- **GUI prompts** - CLI only
- **Localization** - English only
- **Voice prompts** - Text only
- **Custom prompt themes** - Fixed style
- **Rule inheritance** - Flat rules only
- **Rule conditions (AND/OR)** - Single match
- **Rule groups** - Individual rules

---

## Assumptions

### Technical Assumptions

- ASM-001: Rules are expressed in YAML configuration format
- ASM-002: Rule matching uses glob patterns for file paths
- ASM-003: Rule evaluation is fast (< 10ms per operation)
- ASM-004: Built-in rules provide sensible defaults
- ASM-005: Prompt text is customizable per rule

### Behavioral Assumptions

- ASM-006: First matching rule determines the outcome
- ASM-007: Rules are evaluated in definition order
- ASM-008: No matching rule means operation is allowed
- ASM-009: Prompts include enough context for informed decisions
- ASM-010: Users can understand prompt text without technical expertise

### Dependency Assumptions

- ASM-011: Task 013 gate framework calls rule evaluation
- ASM-012: Task 002 config provides rule storage in .agent/config.yml
- ASM-013: Task 010 CLI provides prompt display

### Configuration Assumptions

- ASM-014: Default rules are reasonable for most users
- ASM-015: Custom rules are opt-in, not required
- ASM-016: Rule precedence is clear and documented

---

## Security Considerations

### Threat 1: Rule Injection via Malicious Configuration

**Risk Level:** High
**CVSS Score:** 7.8 (High)
**Attack Vector:** Configuration file manipulation

**Description:**
An attacker who gains write access to `.agent/config.yml` could inject malicious rules that auto-approve dangerous operations or deny legitimate ones. By crafting rules that appear innocuous but have subtle pattern matching flaws, an attacker could create a backdoor for bypassing approval gates.

**Attack Scenario:**
1. Attacker compromises developer's machine or gains repo access
2. Attacker adds rule: `{ name: "safe-audit", pattern: "**/*", operation: file_write, policy: auto }`
3. Rule appears legitimate (named "safe-audit") but auto-approves ALL file writes
4. Acode now writes any file without approval
5. Malicious code injected into production systems

**Impact:**
- Complete bypass of approval security
- Arbitrary code execution on target systems
- Supply chain compromise
- Potential data exfiltration

**Mitigation - Complete C# Implementation:**

```csharp
namespace AgenticCoder.Application.Approvals.Rules.Security;

/// <summary>
/// Validates rules for security issues before they are loaded.
/// Detects overly permissive patterns and potentially malicious configurations.
/// </summary>
public sealed class RuleSecurityValidator
{
    private readonly ILogger<RuleSecurityValidator> _logger;

    // Patterns that are too permissive for auto-approve
    private static readonly string[] DangerousAutoPatterns = new[]
    {
        "**/*",
        "**",
        "*",
        ".",
        ".*"
    };

    // Operations that should never be blanket auto-approved
    private static readonly OperationCategory[] HighRiskCategories = new[]
    {
        OperationCategory.FileDelete,
        OperationCategory.TerminalCommand,
        OperationCategory.ExternalRequest
    };

    public RuleSecurityValidator(ILogger<RuleSecurityValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates a rule set for security issues.
    /// </summary>
    public RuleValidationResult Validate(IReadOnlyList<IRule> rules)
    {
        var warnings = new List<RuleSecurityWarning>();
        var errors = new List<RuleSecurityError>();

        foreach (var rule in rules)
        {
            ValidateRule(rule, warnings, errors);
        }

        // Check for rule shadowing (rules that never match)
        DetectShadowedRules(rules, warnings);

        // Check for conflicting rules
        DetectConflictingRules(rules, warnings);

        if (errors.Count > 0)
        {
            _logger.LogError(
                "SECURITY: Rule validation failed with {ErrorCount} errors",
                errors.Count);
        }

        if (warnings.Count > 0)
        {
            _logger.LogWarning(
                "SECURITY: Rule validation found {WarningCount} security warnings",
                warnings.Count);
        }

        return new RuleValidationResult(
            IsValid: errors.Count == 0,
            Errors: errors.AsReadOnly(),
            Warnings: warnings.AsReadOnly());
    }

    private void ValidateRule(
        IRule rule,
        List<RuleSecurityWarning> warnings,
        List<RuleSecurityError> errors)
    {
        // Check for overly permissive auto-approve rules
        if (rule.Policy == ApprovalPolicyType.AutoApprove)
        {
            if (DangerousAutoPatterns.Contains(rule.PathPattern))
            {
                if (HighRiskCategories.Contains(rule.Category))
                {
                    errors.Add(new RuleSecurityError(
                        rule.Name,
                        RuleSecurityErrorType.DangerousAutoApprove,
                        $"Rule '{rule.Name}' auto-approves ALL {rule.Category} operations. " +
                        "This is a critical security vulnerability."));
                }
                else
                {
                    warnings.Add(new RuleSecurityWarning(
                        rule.Name,
                        RuleSecurityWarningType.OverlyPermissive,
                        $"Rule '{rule.Name}' auto-approves all {rule.Category} operations. " +
                        "Consider a more specific pattern."));
                }
            }
        }

        // Check for suspicious rule names (could be hiding malicious intent)
        if (IsSuspiciousRuleName(rule.Name))
        {
            warnings.Add(new RuleSecurityWarning(
                rule.Name,
                RuleSecurityWarningType.SuspiciousName,
                $"Rule name '{rule.Name}' may be attempting to appear legitimate. " +
                "Review this rule carefully."));
        }

        // Check for ReDoS vulnerabilities in regex patterns
        if (!string.IsNullOrEmpty(rule.CommandPattern))
        {
            if (IsReDoSVulnerable(rule.CommandPattern))
            {
                errors.Add(new RuleSecurityError(
                    rule.Name,
                    RuleSecurityErrorType.ReDoSVulnerable,
                    $"Rule '{rule.Name}' has a regex pattern vulnerable to ReDoS attacks."));
            }
        }
    }

    private void DetectShadowedRules(
        IReadOnlyList<IRule> rules,
        List<RuleSecurityWarning> warnings)
    {
        // Check if any rule is completely shadowed by an earlier rule
        for (int i = 0; i < rules.Count; i++)
        {
            for (int j = i + 1; j < rules.Count; j++)
            {
                if (IsShadowedBy(rules[j], rules[i]))
                {
                    warnings.Add(new RuleSecurityWarning(
                        rules[j].Name,
                        RuleSecurityWarningType.ShadowedRule,
                        $"Rule '{rules[j].Name}' is shadowed by earlier rule '{rules[i].Name}' " +
                        "and will never match."));
                }
            }
        }
    }

    private void DetectConflictingRules(
        IReadOnlyList<IRule> rules,
        List<RuleSecurityWarning> warnings)
    {
        // Check for rules with same pattern but different policies
        var patternGroups = rules
            .GroupBy(r => (r.Category, r.PathPattern, r.CommandPattern))
            .Where(g => g.Count() > 1);

        foreach (var group in patternGroups)
        {
            var policies = group.Select(r => r.Policy).Distinct().ToList();
            if (policies.Count > 1)
            {
                warnings.Add(new RuleSecurityWarning(
                    group.First().Name,
                    RuleSecurityWarningType.ConflictingPolicies,
                    $"Multiple rules match the same pattern with different policies: " +
                    $"{string.Join(", ", group.Select(r => r.Name))}"));
            }
        }
    }

    private static bool IsSuspiciousRuleName(string name)
    {
        var suspicious = new[]
        {
            "safe", "audit", "security", "default", "system",
            "admin", "root", "allow-all", "bypass"
        };

        var lowerName = name.ToLowerInvariant();
        return suspicious.Any(s => lowerName.Contains(s)) &&
               lowerName.Length < 20; // Short names with these words are suspicious
    }

    private static bool IsShadowedBy(IRule candidate, IRule earlier)
    {
        // Simple shadow detection: same category and earlier pattern is superset
        if (candidate.Category != earlier.Category)
            return false;

        if (earlier.PathPattern == "**/*" || earlier.PathPattern == "**")
            return true;

        return false;
    }

    private static bool IsReDoSVulnerable(string pattern)
    {
        // Simple ReDoS detection: nested quantifiers
        return Regex.IsMatch(pattern, @"(\+|\*)\s*(\+|\*)") ||
               Regex.IsMatch(pattern, @"\([^)]*(\+|\*)[^)]*\)(\+|\*)");
    }
}

public sealed record RuleValidationResult(
    bool IsValid,
    IReadOnlyList<RuleSecurityError> Errors,
    IReadOnlyList<RuleSecurityWarning> Warnings);

public sealed record RuleSecurityError(
    string RuleName,
    RuleSecurityErrorType Type,
    string Description);

public sealed record RuleSecurityWarning(
    string RuleName,
    RuleSecurityWarningType Type,
    string Description);

public enum RuleSecurityErrorType
{
    DangerousAutoApprove,
    ReDoSVulnerable,
    InvalidPattern
}

public enum RuleSecurityWarningType
{
    OverlyPermissive,
    SuspiciousName,
    ShadowedRule,
    ConflictingPolicies
}
```

**Testing Strategy:**
- Unit test: Detect overly permissive rules
- Unit test: Detect ReDoS patterns
- Integration test: Block dangerous rule sets
- E2E test: Verify warnings shown to user

---

### Threat 2: Pattern Escape Sequence Attacks

**Risk Level:** Medium
**CVSS Score:** 6.5 (Medium)
**Attack Vector:** Crafted file paths

**Description:**
An attacker could create files with paths designed to exploit pattern matching edge cases. By using escape sequences, special characters, or Unicode tricks, file paths could bypass intended rule matches.

**Attack Scenario:**
1. Rule configured: `deny: delete src/**/*.ts`
2. Attacker creates file: `src/evil\x00.ts` (null byte in name)
3. Pattern matcher fails to match due to null byte
4. Deletion proceeds, bypassing deny rule

**Impact:**
- Rule bypass for protected paths
- Unauthorized file deletions
- Unauthorized command execution

**Mitigation - Complete C# Implementation:**

```csharp
namespace AgenticCoder.Application.Approvals.Rules.Patterns;

/// <summary>
/// Sanitizes paths before pattern matching to prevent bypass attacks.
/// Normalizes paths and detects suspicious characters.
/// </summary>
public sealed class PathSanitizer
{
    private readonly ILogger<PathSanitizer> _logger;

    // Characters that should never appear in file paths
    private static readonly char[] ProhibitedChars = new[]
    {
        '\0',  // Null byte
        '\x1B', // Escape
        '\x7F', // DEL
    };

    // Unicode characters used for path spoofing
    private static readonly char[] SpoofingChars = new[]
    {
        '\u202A', // Left-to-right embedding
        '\u202B', // Right-to-left embedding
        '\u202C', // Pop directional formatting
        '\u202D', // Left-to-right override
        '\u202E', // Right-to-left override
        '\u2066', // Left-to-right isolate
        '\u2067', // Right-to-left isolate
        '\u2068', // First strong isolate
        '\u2069', // Pop directional isolate
    };

    public PathSanitizer(ILogger<PathSanitizer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sanitizes and validates a path for safe pattern matching.
    /// </summary>
    public PathSanitizationResult Sanitize(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var issues = new List<PathSecurityIssue>();
        var sanitized = path;

        // Check for prohibited characters
        foreach (var c in ProhibitedChars)
        {
            if (sanitized.Contains(c))
            {
                issues.Add(new PathSecurityIssue(
                    PathSecurityIssueType.ProhibitedCharacter,
                    $"Path contains prohibited character 0x{(int)c:X2}"));
                sanitized = sanitized.Replace(c.ToString(), "");
            }
        }

        // Check for Unicode spoofing characters
        foreach (var c in SpoofingChars)
        {
            if (sanitized.Contains(c))
            {
                issues.Add(new PathSecurityIssue(
                    PathSecurityIssueType.UnicodeSpoofing,
                    $"Path contains Unicode direction override U+{(int)c:X4}"));
                sanitized = sanitized.Replace(c.ToString(), "");
            }
        }

        // Normalize path separators
        sanitized = sanitized.Replace('\\', '/');

        // Remove path traversal attempts
        if (sanitized.Contains(".."))
        {
            var normalized = NormalizePath(sanitized);
            if (normalized != sanitized)
            {
                issues.Add(new PathSecurityIssue(
                    PathSecurityIssueType.PathTraversal,
                    "Path contains traversal sequences"));
                sanitized = normalized;
            }
        }

        // Remove leading/trailing whitespace (could hide path)
        var trimmed = sanitized.Trim();
        if (trimmed != sanitized)
        {
            issues.Add(new PathSecurityIssue(
                PathSecurityIssueType.HiddenWhitespace,
                "Path contains leading/trailing whitespace"));
            sanitized = trimmed;
        }

        // Log security issues
        if (issues.Count > 0)
        {
            _logger.LogWarning(
                "SECURITY: Path sanitization found {IssueCount} issues in path: {Path}",
                issues.Count, path);
        }

        return new PathSanitizationResult(
            OriginalPath: path,
            SanitizedPath: sanitized,
            Issues: issues.AsReadOnly(),
            WasModified: path != sanitized);
    }

    private static string NormalizePath(string path)
    {
        var parts = path.Split('/').ToList();
        var result = new List<string>();

        foreach (var part in parts)
        {
            if (part == "..")
            {
                if (result.Count > 0 && result[^1] != "..")
                {
                    result.RemoveAt(result.Count - 1);
                }
            }
            else if (part != "." && !string.IsNullOrEmpty(part))
            {
                result.Add(part);
            }
        }

        return string.Join("/", result);
    }
}

public sealed record PathSanitizationResult(
    string OriginalPath,
    string SanitizedPath,
    IReadOnlyList<PathSecurityIssue> Issues,
    bool WasModified);

public sealed record PathSecurityIssue(
    PathSecurityIssueType Type,
    string Description);

public enum PathSecurityIssueType
{
    ProhibitedCharacter,
    UnicodeSpoofing,
    PathTraversal,
    HiddenWhitespace
}
```

**Testing Strategy:**
- Unit test: Null bytes stripped
- Unit test: Unicode overrides stripped
- Unit test: Path traversal blocked
- Integration test: Sanitized paths match correctly

---

### Threat 3: Secret Redaction Bypass

**Risk Level:** Medium
**CVSS Score:** 5.5 (Medium)
**Attack Vector:** Obfuscated secrets

**Description:**
The secret redaction system uses pattern matching to detect and hide sensitive data. An attacker could craft secrets that bypass detection patterns—using unusual formats, encoding, or character substitution.

**Attack Scenario:**
1. Secret redactor pattern: `apiKey\s*[:=]\s*['"][^'"]+['"]`
2. Code contains: `apiKey /* hello */ = /* world */ 'sk_live_secret'`
3. Comment injection breaks pattern match
4. Secret displayed in approval preview

**Impact:**
- Credential exposure during review
- Accidental commit of secrets
- Compliance violations

**Mitigation - Complete C# Implementation:**

```csharp
namespace AgenticCoder.CLI.Prompts.Security;

/// <summary>
/// Multi-layer secret detection that catches obfuscated secrets.
/// Uses pattern matching, entropy analysis, and known secret formats.
/// </summary>
public sealed class EnhancedSecretRedactor
{
    private readonly ILogger<EnhancedSecretRedactor> _logger;

    // Known secret formats (prefixes)
    private static readonly Dictionary<string, string> KnownSecretPrefixes = new()
    {
        { "sk_live_", "Stripe Live Key" },
        { "sk_test_", "Stripe Test Key" },
        { "pk_live_", "Stripe Public Live Key" },
        { "pk_test_", "Stripe Public Test Key" },
        { "ghp_", "GitHub Personal Token" },
        { "gho_", "GitHub OAuth Token" },
        { "github_pat_", "GitHub PAT" },
        { "xoxb-", "Slack Bot Token" },
        { "xoxp-", "Slack User Token" },
        { "AKIA", "AWS Access Key ID" },
        { "eyJ", "JWT Token" },
        { "AIza", "Google API Key" },
        { "npm_", "NPM Token" },
    };

    // Patterns for variable assignments
    private static readonly Regex[] AssignmentPatterns = new[]
    {
        // Standard assignments: apiKey = "value"
        new Regex(
            @"(?i)(api[_-]?key|password|passwd|pwd|secret|token|auth|credential|private[_-]?key)\s*[:=]\s*['""]([^'""]{8,})['""]",
            RegexOptions.Compiled),

        // JSON: "apiKey": "value"
        new Regex(
            @"(?i)[""'](api[_-]?key|password|passwd|pwd|secret|token|auth|credential|private[_-]?key)[""]?\s*:\s*['""]([^'""]{8,})['""]",
            RegexOptions.Compiled),

        // Connection strings
        new Regex(
            @"(?i)(password|pwd)\s*=\s*([^;]{8,})",
            RegexOptions.Compiled),
    };

    public EnhancedSecretRedactor(ILogger<EnhancedSecretRedactor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Redacts secrets using multiple detection strategies.
    /// </summary>
    public RedactionResult Redact(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var redactions = new List<SecretRedaction>();
        var result = content;

        // Strategy 1: Known secret prefixes
        result = RedactKnownPrefixes(result, redactions);

        // Strategy 2: Assignment patterns
        result = RedactAssignmentPatterns(result, redactions);

        // Strategy 3: High-entropy strings
        result = RedactHighEntropyStrings(result, redactions);

        // Strategy 4: Base64-encoded secrets
        result = RedactBase64Secrets(result, redactions);

        if (redactions.Count > 0)
        {
            _logger.LogDebug(
                "Redacted {Count} secrets using strategies: {Strategies}",
                redactions.Count,
                string.Join(", ", redactions.Select(r => r.Strategy).Distinct()));
        }

        return new RedactionResult(
            OriginalContent: content,
            RedactedContent: result,
            Redactions: redactions.AsReadOnly(),
            TotalRedacted: redactions.Count);
    }

    private string RedactKnownPrefixes(string content, List<SecretRedaction> redactions)
    {
        var result = content;

        foreach (var (prefix, secretType) in KnownSecretPrefixes)
        {
            var startIndex = 0;
            while ((startIndex = result.IndexOf(prefix, startIndex, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                // Find the end of the secret (next whitespace or quote)
                var endIndex = startIndex + prefix.Length;
                while (endIndex < result.Length && IsSecretChar(result[endIndex]))
                {
                    endIndex++;
                }

                var secretLength = endIndex - startIndex;
                if (secretLength > prefix.Length + 5) // Minimum secret length
                {
                    var original = result.Substring(startIndex, secretLength);
                    result = result.Remove(startIndex, secretLength)
                                   .Insert(startIndex, $"[{secretType.ToUpper()}_REDACTED]");

                    redactions.Add(new SecretRedaction(
                        Strategy: "KnownPrefix",
                        SecretType: secretType,
                        OriginalLength: secretLength));
                }

                startIndex++;
            }
        }

        return result;
    }

    private string RedactAssignmentPatterns(string content, List<SecretRedaction> redactions)
    {
        var result = content;

        foreach (var pattern in AssignmentPatterns)
        {
            result = pattern.Replace(result, m =>
            {
                var key = m.Groups[1].Value;
                var value = m.Groups[2].Value;

                // Don't redact obvious non-secrets
                if (IsLikelyNotSecret(value))
                {
                    return m.Value;
                }

                redactions.Add(new SecretRedaction(
                    Strategy: "AssignmentPattern",
                    SecretType: key,
                    OriginalLength: value.Length));

                return m.Value.Replace(value, "[REDACTED]");
            });
        }

        return result;
    }

    private string RedactHighEntropyStrings(string content, List<SecretRedaction> redactions)
    {
        // Find quoted strings with high entropy
        var stringPattern = new Regex(@"['""]([A-Za-z0-9+/=_-]{20,})['""]");

        return stringPattern.Replace(content, m =>
        {
            var value = m.Groups[1].Value;
            var entropy = CalculateEntropy(value);

            // High entropy suggests random/encrypted data
            if (entropy > 4.0 && !IsLikelyNotSecret(value))
            {
                redactions.Add(new SecretRedaction(
                    Strategy: "HighEntropy",
                    SecretType: "UnknownSecret",
                    OriginalLength: value.Length));

                return m.Value.Replace(value, "[HIGH_ENTROPY_REDACTED]");
            }

            return m.Value;
        });
    }

    private string RedactBase64Secrets(string content, List<SecretRedaction> redactions)
    {
        // Look for base64 that might be encoding secrets
        var base64Pattern = new Regex(@"(?<![A-Za-z0-9])([A-Za-z0-9+/]{40,}={0,2})(?![A-Za-z0-9])");

        return base64Pattern.Replace(content, m =>
        {
            var value = m.Groups[1].Value;

            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value));

                // Check if decoded content looks like a secret
                if (KnownSecretPrefixes.Keys.Any(p => decoded.Contains(p, StringComparison.OrdinalIgnoreCase)))
                {
                    redactions.Add(new SecretRedaction(
                        Strategy: "Base64Encoded",
                        SecretType: "EncodedSecret",
                        OriginalLength: value.Length));

                    return "[BASE64_SECRET_REDACTED]";
                }
            }
            catch
            {
                // Not valid base64, ignore
            }

            return m.Value;
        });
    }

    private static bool IsSecretChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_' || c == '-';
    }

    private static bool IsLikelyNotSecret(string value)
    {
        // Common non-secret patterns
        var nonSecrets = new[]
        {
            "localhost", "127.0.0.1", "example", "test", "sample",
            "placeholder", "your_", "YOUR_", "xxx", "XXX"
        };

        return nonSecrets.Any(ns => value.Contains(ns, StringComparison.OrdinalIgnoreCase));
    }

    private static double CalculateEntropy(string s)
    {
        var freq = s.GroupBy(c => c)
                    .ToDictionary(g => g.Key, g => (double)g.Count() / s.Length);

        return -freq.Values.Sum(p => p * Math.Log2(p));
    }
}

public sealed record RedactionResult(
    string OriginalContent,
    string RedactedContent,
    IReadOnlyList<SecretRedaction> Redactions,
    int TotalRedacted);

public sealed record SecretRedaction(
    string Strategy,
    string SecretType,
    int OriginalLength);
```

**Testing Strategy:**
- Unit test: Each detection strategy works
- Unit test: Combined strategies catch obfuscated secrets
- Unit test: Non-secrets not redacted
- Integration test: Real-world secret formats detected

---

### Threat 4: Prompt Rendering Denial of Service

**Risk Level:** Low
**CVSS Score:** 4.3 (Medium)
**Attack Vector:** Malformed content

**Description:**
An attacker could craft file content designed to overwhelm the prompt rendering system—extremely long lines, deeply nested structures, or content that triggers expensive regex operations.

**Attack Scenario:**
1. User runs Acode on repository with malicious file
2. File contains line with 1 million characters
3. Prompt renderer attempts to display preview
4. Memory exhaustion or hang during rendering
5. Acode becomes unresponsive

**Impact:**
- Application denial of service
- Memory exhaustion
- Poor user experience
- Potential system instability

**Mitigation - Complete C# Implementation:**

```csharp
namespace AgenticCoder.CLI.Prompts.Security;

/// <summary>
/// Protects prompt rendering from denial of service via malformed content.
/// Implements limits on content size, line length, and rendering time.
/// </summary>
public sealed class PromptRenderingProtector
{
    private readonly PromptRenderingLimits _limits;
    private readonly ILogger<PromptRenderingProtector> _logger;

    public PromptRenderingProtector(
        IOptions<PromptRenderingLimits> limits,
        ILogger<PromptRenderingProtector> logger)
    {
        _limits = limits?.Value ?? new PromptRenderingLimits();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes content for safe rendering with timeout protection.
    /// </summary>
    public async Task<SafeRenderContent> PrepareForRenderingAsync(
        string content,
        CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_limits.MaxRenderTimeMs);

        try
        {
            return await Task.Run(() => PrepareContent(content), timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogWarning(
                "SECURITY: Content preparation timed out after {Ms}ms",
                _limits.MaxRenderTimeMs);

            return new SafeRenderContent(
                Content: "[CONTENT TOO COMPLEX TO DISPLAY]",
                WasTruncated: true,
                WasTimedOut: true,
                Warnings: new[] { "Content processing timed out" });
        }
    }

    private SafeRenderContent PrepareContent(string content)
    {
        var warnings = new List<string>();

        // Check total size
        if (content.Length > _limits.MaxTotalBytes)
        {
            warnings.Add($"Content truncated from {content.Length:N0} to {_limits.MaxTotalBytes:N0} bytes");
            content = content[.._limits.MaxTotalBytes] + "\n... [TRUNCATED]";
        }

        // Process lines
        var lines = content.Split('\n');
        var processedLines = new List<string>();
        var linesTruncated = 0;

        for (int i = 0; i < Math.Min(lines.Length, _limits.MaxLines); i++)
        {
            var line = lines[i];

            if (line.Length > _limits.MaxLineLength)
            {
                linesTruncated++;
                line = line[.._limits.MaxLineLength] + "... [LINE TRUNCATED]";
            }

            processedLines.Add(line);
        }

        if (lines.Length > _limits.MaxLines)
        {
            warnings.Add($"Showing {_limits.MaxLines} of {lines.Length} lines");
            processedLines.Add($"... [{lines.Length - _limits.MaxLines} more lines]");
        }

        if (linesTruncated > 0)
        {
            warnings.Add($"{linesTruncated} lines truncated to {_limits.MaxLineLength} chars");
        }

        return new SafeRenderContent(
            Content: string.Join('\n', processedLines),
            WasTruncated: warnings.Count > 0,
            WasTimedOut: false,
            Warnings: warnings.ToArray());
    }
}

public sealed class PromptRenderingLimits
{
    public int MaxTotalBytes { get; set; } = 1_000_000; // 1MB
    public int MaxLines { get; set; } = 1000;
    public int MaxLineLength { get; set; } = 500;
    public int MaxRenderTimeMs { get; set; } = 5000; // 5 seconds
}

public sealed record SafeRenderContent(
    string Content,
    bool WasTruncated,
    bool WasTimedOut,
    string[] Warnings);
```

**Testing Strategy:**
- Unit test: Long content truncated
- Unit test: Long lines truncated
- Unit test: Timeout triggers
- Performance test: Large files render quickly

---

### Threat 5: User Input Injection in Prompts

**Risk Level:** Low
**CVSS Score:** 3.7 (Low)
**Attack Vector:** Crafted operation descriptions

**Description:**
The prompt system displays operation descriptions that may come from untrusted sources (LLM output, file names, etc.). An attacker could craft descriptions containing terminal escape sequences or control characters to manipulate the prompt display.

**Attack Scenario:**
1. LLM generates operation: "Write file: \x1B[2J\x1B[H<fake prompt content>"
2. Escape sequence clears screen and moves cursor to home
3. Fake prompt content appears, potentially tricking user
4. User approves thinking they're approving something else

**Impact:**
- User confusion
- Potential social engineering
- Incorrect approval decisions

**Mitigation:** Use the `PromptContentSanitizer` class from Task 013 Security Considerations, which strips ANSI escape sequences, control characters, and Unicode direction overrides from all content before display.

---

## Functional Requirements

### Rule Definition

- FR-001: Rules MUST be definable in config
- FR-002: Rules MUST have unique identifier
- FR-003: Rules MUST have match criteria
- FR-004: Rules MUST have policy
- FR-005: Rules MUST be validatable

### Match Criteria

- FR-006: Category matching MUST work
- FR-007: Glob pattern matching MUST work
- FR-008: Regex pattern matching MUST work
- FR-009: Combined criteria MUST work
- FR-010: Negative patterns MUST work

### Pattern Types

- FR-011: Glob patterns for file paths
- FR-012: ** for recursive matching
- FR-013: * for single level matching
- FR-014: ? for single character
- FR-015: Regex for commands

### Rule Evaluation

- FR-016: Rules MUST evaluate in order
- FR-017: First match MUST win
- FR-018: No match MUST use default
- FR-019: Evaluation MUST be fast
- FR-020: Evaluation MUST log

### Built-in Rules

- FR-021: FILE_READ → AUTO
- FR-022: FILE_WRITE → PROMPT
- FR-023: FILE_DELETE → PROMPT
- FR-024: DIRECTORY_CREATE → AUTO
- FR-025: TERMINAL_COMMAND → PROMPT

### Custom Rule Schema

- FR-026: Pattern field for file matching
- FR-027: Command field for command matching
- FR-028: Operation field for category
- FR-029: Policy field for action
- FR-030: Name field for identification

### Prompt Display

- FR-031: Prompt MUST show header
- FR-032: Prompt MUST show operation type
- FR-033: Prompt MUST show operation details
- FR-034: Prompt MUST show preview
- FR-035: Prompt MUST show options
- FR-036: Prompt MUST show timeout

### Header Format

- FR-037: Warning symbol (⚠)
- FR-038: "Approval Required" text
- FR-039: Separator line
- FR-040: Clear visual distinction

### Operation Display

- FR-041: Operation type in CAPS
- FR-042: Relevant path or command
- FR-043: Additional context (size, etc.)

### Preview Content

- FR-044: Max 50 lines default
- FR-045: Truncation indicator
- FR-046: Line numbers
- FR-047: Syntax highlighting (if available)

### Option Display

- FR-048: [A]pprove highlighted
- FR-049: [D]eny
- FR-050: [S]kip
- FR-051: [V]iew all
- FR-052: [?]Help

### Input Handling

- FR-053: Single character input
- FR-054: Case insensitive
- FR-055: Invalid input re-prompts
- FR-056: Enter for default
- FR-057: Ctrl+C for cancel

### View All Feature

- FR-058: Show full content
- FR-059: Pager for long content
- FR-060: Return to prompt after

### Help Feature

- FR-061: Explain each option
- FR-062: Show current operation
- FR-063: Return to prompt after

### Secret Redaction

- FR-064: Detect API keys
- FR-065: Detect passwords
- FR-066: Detect tokens
- FR-067: Replace with [REDACTED]
- FR-068: Log redaction count

### Timeout Display

- FR-069: Show remaining time
- FR-070: Update periodically
- FR-071: Warning at low time
- FR-072: Clear timeout indicator

---

## Non-Functional Requirements

### Performance

- NFR-001: Rule evaluation < 5ms
- NFR-002: Prompt render < 50ms
- NFR-003: Input response < 10ms

### Reliability

- NFR-004: No crashes on bad input
- NFR-005: Graceful terminal degradation
- NFR-006: Rule parsing errors caught

### Usability

- NFR-007: Clear, readable prompts
- NFR-008: Intuitive key bindings
- NFR-009: Helpful error messages

### Security

- NFR-010: Secrets never shown
- NFR-011: Redaction reliable
- NFR-012: No sensitive data in logs

### Compatibility

- NFR-013: Works in standard terminals
- NFR-014: Works without color
- NFR-015: Works with screen readers

---

## Best Practices

### Rule Design Best Practices

- **BP-001: Order rules from specific to general** - Place your most specific rules first, followed by more general patterns. Since first-match-wins, a general rule placed early will shadow all subsequent specific rules for the same category.

- **BP-002: Use meaningful rule names** - Name rules descriptively (e.g., `allow-test-files`, `deny-env-writes`). Avoid generic names like `rule1` or `custom`. Good names make rule sets self-documenting.

- **BP-003: Prefer deny over prompt for truly forbidden operations** - If an operation should never happen (writing to `.env`, deleting `src/**`), use `deny` policy. Don't rely on users to remember to deny—automate the safety.

- **BP-004: Test rules with dry-run mode** - Before deploying a new rule set, use `acode rules test --dry-run` to see what policy each operation would receive. Catch rule mistakes before they impact real sessions.

### Pattern Design Best Practices

- **BP-005: Use glob patterns for paths, regex for commands** - Glob is more readable and familiar for file paths. Regex provides the power needed for command matching. Don't mix them inappropriately.

- **BP-006: Anchor command regex patterns** - Use `^` and `$` to prevent partial matches. `^npm test$` matches only "npm test", while `npm test` also matches "npm test && rm -rf /".

- **BP-007: Test patterns against edge cases** - Verify patterns handle unusual but valid paths: spaces, special characters, deeply nested directories. Pattern failures are security vulnerabilities.

- **BP-008: Document complex patterns** - If a regex is not immediately obvious, add a comment explaining what it matches and why. Future maintainers (including you) will thank you.

### Prompt Usability Best Practices

- **BP-009: Keep previews concise** - Default to showing 10 lines of preview. Users can request more with [V]iew. Information overload leads to approval fatigue.

- **BP-010: Highlight critical information** - Use color and formatting to draw attention to operation type, target path, and risk level. Don't make users hunt for important details.

- **BP-011: Show diff for modifications** - When modifying existing files, show what's changing, not just the new content. Diffs are easier to review than full file contents.

- **BP-012: Time out safely** - Timeout should trigger deny by default. Never auto-approve on timeout—that creates a race condition attack vector.

### Security Best Practices

- **BP-013: Validate rules at load time** - Catch invalid patterns, dangerous configurations, and shadowed rules before they affect sessions. Fail fast with clear error messages.

- **BP-014: Sanitize all displayed content** - Strip escape sequences, control characters, and Unicode overrides from file content, paths, and commands before rendering in prompts.

- **BP-015: Redact conservatively** - When in doubt, redact. A false positive (redacting non-secret) is harmless. A false negative (showing actual secret) is a security incident.

- **BP-016: Log rule evaluations** - Record which rule matched each operation for audit and debugging. But never log the actual content being approved—that could contain secrets.

### Integration Best Practices

- **BP-017: Version control your rules** - Commit `.agent/config.yml` with your project. Share team-wide rules via the repository. Don't rely on individual developer configurations.

- **BP-018: Provide rule templates** - Create starter rule sets for common project types (Node.js, .NET, Python). Sensible defaults reduce configuration burden.

- **BP-019: Support environment-specific rules** - Allow different rules for CI vs local development. CI needs stricter automation rules; local needs interactive flexibility.

- **BP-020: Monitor rule effectiveness** - Track how often each rule matches, how often users override auto-decisions, and approval timing. Use data to refine rules.

---

## User Manual Documentation

### Overview

Gate rules determine when Acode asks for approval. Prompts are how you respond to approval requests. Configure rules to match your workflow.

### Default Rules

Out of the box:

| Operation | Default Policy |
|-----------|---------------|
| Read file | Auto-approve |
| Write file | Prompt |
| Delete file | Prompt |
| Create directory | Auto-approve |
| Terminal command | Prompt |

### Configuring Rules

Add custom rules in `.agent/config.yml`:

```yaml
approvals:
  rules:
    # Auto-approve test files
    - name: auto-tests
      pattern: "**/*.test.ts"
      operation: file_write
      policy: auto
      
    # Prompt for config changes
    - name: protect-config
      pattern: "**/*.config.*"
      operation: file_write
      policy: prompt
      
    # Deny source deletion
    - name: deny-source-delete
      pattern: "src/**"
      operation: file_delete
      policy: deny
```

### Rule Fields

| Field | Required | Description |
|-------|----------|-------------|
| name | Yes | Unique rule identifier |
| pattern | No | Glob pattern for paths |
| command | No | Regex for commands |
| operation | Yes | Category to match |
| policy | Yes | auto/prompt/deny/skip |

### Pattern Syntax

**Glob patterns for files:**

```yaml
# Match all TypeScript files
pattern: "**/*.ts"

# Match files in src directory
pattern: "src/**"

# Match specific file
pattern: "package.json"

# Match test files
pattern: "**/*.{test,spec}.ts"
```

**Regex patterns for commands:**

```yaml
# Match npm commands
command: "^npm\\s+"

# Match git commands
command: "^git\\s+"

# Match any command
command: ".*"
```

### Rule Order

Rules are evaluated top-to-bottom. First match wins:

```yaml
rules:
  # Specific rule first
  - name: allow-package-json
    pattern: "package.json"
    operation: file_write
    policy: auto
    
  # General rule second
  - name: prompt-writes
    pattern: "**/*"
    operation: file_write
    policy: prompt
```

In this example, `package.json` writes auto-approve, all other writes prompt.

### Prompt Anatomy

```
⚠ Approval Required
─────────────────────────────────────
Operation: WRITE FILE
Path: src/components/Button.tsx
Size: 45 lines (new file)

Preview:
   1 | import React from 'react';
   2 | 
   3 | interface ButtonProps {
   4 |   label: string;
   5 |   onClick: () => void;
 ... | (40 more lines)

[A]pprove  [D]eny  [S]kip  [V]iew all  [?]Help

Timeout: 4:32 remaining

Choice: _
```

### Key Bindings

| Key | Action |
|-----|--------|
| A / a / Enter | Approve operation |
| D / d | Deny operation |
| S / s | Skip this operation |
| V / v | View full content |
| ? | Show help |
| Ctrl+C | Cancel/Deny |

### Viewing Full Content

Press V to see everything:

```
─────────────────────────────────────
Full Content: src/components/Button.tsx
─────────────────────────────────────
   1 | import React from 'react';
   2 | 
   3 | interface ButtonProps {
   4 |   label: string;
   5 |   onClick: () => void;
   ...
  45 | export default Button;
─────────────────────────────────────

Press any key to return to prompt...
```

### Help Screen

Press ? for help:

```
─────────────────────────────────────
Approval Help
─────────────────────────────────────

You're being asked to approve: WRITE FILE

Options:
  [A]pprove  - Allow this operation to proceed
  [D]eny     - Block this operation
  [S]kip     - Skip and continue with session
  [V]iew     - See full content/details
  [?]        - Show this help

Current timeout: 4:32 remaining
If you don't respond, operation will be DENIED.

Press any key to return to prompt...
```

### Secret Redaction

Sensitive data is automatically hidden:

```
Preview:
   1 | const config = {
   2 |   apiKey: '[REDACTED]',
   3 |   password: '[REDACTED]',
   4 |   token: '[REDACTED]',
   5 | };

[3 secrets redacted for security]
```

### Troubleshooting

#### Rules Not Matching

**Problem:** Rule doesn't apply as expected

**Solution:**
1. Check rule order (first match wins)
2. Verify pattern syntax
3. Test pattern: `acode rules test "path/file.ts"`

#### Prompt Not Showing

**Problem:** Operation proceeds without prompt

**Solution:**
1. Check if rule with `auto` policy matches
2. Check if `--yes` flag is set
3. Review rule evaluation logs

#### Pattern Syntax Error

**Problem:** Config fails to load

**Solution:**
1. Validate YAML syntax
2. Escape special characters in regex
3. Use quotes around patterns

---

## Acceptance Criteria

### Rule Definition

- [ ] AC-001: Config parsing works
- [ ] AC-002: Rules have IDs
- [ ] AC-003: Rules validate
- [ ] AC-004: Invalid rules error

### Matching

- [ ] AC-005: Category match works
- [ ] AC-006: Glob patterns work
- [ ] AC-007: Regex patterns work
- [ ] AC-008: Combined criteria work
- [ ] AC-009: Negation works

### Evaluation

- [ ] AC-010: Order respected
- [ ] AC-011: First match wins
- [ ] AC-012: Default applies
- [ ] AC-013: Fast evaluation
- [ ] AC-014: Logged

### Built-in Rules

- [ ] AC-015: FILE_READ auto
- [ ] AC-016: FILE_WRITE prompt
- [ ] AC-017: FILE_DELETE prompt
- [ ] AC-018: DIRECTORY auto
- [ ] AC-019: TERMINAL prompt

### Prompt Display

- [ ] AC-020: Header shows
- [ ] AC-021: Operation shows
- [ ] AC-022: Details show
- [ ] AC-023: Preview shows
- [ ] AC-024: Options show
- [ ] AC-025: Timeout shows

### Input

- [ ] AC-026: Single char works
- [ ] AC-027: Case insensitive
- [ ] AC-028: Invalid re-prompts
- [ ] AC-029: Enter = default
- [ ] AC-030: Ctrl+C works

### Features

- [ ] AC-031: View all works
- [ ] AC-032: Help works
- [ ] AC-033: Timeout countdown

### Redaction

- [ ] AC-034: API keys hidden
- [ ] AC-035: Passwords hidden
- [ ] AC-036: Tokens hidden
- [ ] AC-037: Count shown

---

## Testing Requirements

### Unit Tests

```csharp
namespace AgenticCoder.Application.Tests.Unit.Approvals.Rules;

public class RuleParserTests
{
    private readonly RuleParser _parser;
    
    public RuleParserTests()
    {
        _parser = new RuleParser(NullLogger<RuleParser>.Instance);
    }
    
    [Fact]
    public void Should_Parse_Valid_Rules()
    {
        // Arrange
        var yaml = @"
approval_rules:
  - name: auto-tests
    category: file_write
    path_pattern: '**/*.test.ts'
    policy: auto
  - name: prompt-src
    category: file_write
    path_pattern: 'src/**'
    policy: prompt
";
        
        // Act
        var rules = _parser.Parse(yaml);
        
        // Assert
        Assert.Equal(2, rules.Count);
        Assert.Equal("auto-tests", rules[0].Name);
        Assert.Equal(ApprovalPolicyType.AutoApprove, rules[0].Policy);
    }
    
    [Fact]
    public void Should_Reject_Invalid_Rules()
    {
        // Arrange
        var yaml = @"
approval_rules:
  - name: bad-rule
    policy: invalid_policy
";
        
        // Act & Assert
        Assert.Throws<InvalidRuleException>(() => _parser.Parse(yaml));
    }
    
    [Fact]
    public void Should_Handle_All_Fields()
    {
        // Arrange
        var yaml = @"
approval_rules:
  - name: full-rule
    category: terminal_command
    command_pattern: '^npm (install|ci)$'
    policy: prompt
    prompt_message: 'Install npm packages?'
";
        
        // Act
        var rules = _parser.Parse(yaml);
        
        // Assert
        var rule = rules[0];
        Assert.Equal("full-rule", rule.Name);
        Assert.Equal(OperationCategory.TerminalCommand, rule.Category);
        Assert.Equal("^npm (install|ci)$", rule.CommandPattern);
        Assert.Equal(ApprovalPolicyType.Prompt, rule.Policy);
    }
}

public class PatternMatcherTests
{
    [Theory]
    [InlineData("**/*.ts", "src/components/App.ts", true)]
    [InlineData("**/*.ts", "src/components/App.tsx", false)]
    [InlineData("src/**", "src/main.ts", true)]
    [InlineData("src/**", "tests/test.ts", false)]
    [InlineData("*.test.ts", "App.test.ts", true)]
    [InlineData("*.test.ts", "src/App.test.ts", false)]
    public void Should_Match_Glob_Patterns(string pattern, string path, bool expectedMatch)
    {
        // Arrange
        var matcher = new GlobMatcher(pattern);
        
        // Act
        var matches = matcher.Matches(path);
        
        // Assert
        Assert.Equal(expectedMatch, matches);
    }
    
    [Theory]
    [InlineData("^npm (install|ci)$", "npm install", true)]
    [InlineData("^npm (install|ci)$", "npm run build", false)]
    [InlineData("^git push", "git push origin main", true)]
    [InlineData("^git push", "git pull", false)]
    public void Should_Match_Regex_Patterns(string pattern, string command, bool expectedMatch)
    {
        // Arrange
        var matcher = new RegexMatcher(pattern);
        
        // Act
        var matches = matcher.Matches(command);
        
        // Assert
        Assert.Equal(expectedMatch, matches);
    }
    
    [Fact]
    public void Should_Handle_Negation()
    {
        // Arrange
        var matcher = new GlobMatcher("**/*.ts", negate: true);
        
        // Act
        var matchTs = matcher.Matches("src/App.ts");
        var matchTsx = matcher.Matches("src/App.tsx");
        
        // Assert
        Assert.False(matchTs); // Negated - *.ts files don't match
        Assert.True(matchTsx);  // Non-*.ts files do match
    }
}

public class RuleEvaluatorTests
{
    [Fact]
    public void Should_Evaluate_In_Order()
    {
        // Arrange
        var rules = new List<IRule>
        {
            new Rule("first", OperationCategory.FileWrite, "**/*.ts", null, ApprovalPolicyType.AutoApprove),
            new Rule("second", OperationCategory.FileWrite, "src/**", null, ApprovalPolicyType.Prompt)
        };
        var evaluator = new RuleEvaluator(rules, ApprovalPolicyType.Prompt, NullLogger<RuleEvaluator>.Instance);
        
        var operation = new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write file",
            Details: new Dictionary<string, object> { { "path", "src/App.ts" } }.AsReadOnly());
        
        // Act
        var policy = evaluator.Evaluate(operation);
        
        // Assert
        Assert.Equal(ApprovalPolicyType.AutoApprove, policy); // First rule matches
    }
    
    [Fact]
    public void Should_Use_First_Match_Wins()
    {
        // Arrange
        var rules = new List<IRule>
        {
            new Rule("general", OperationCategory.FileWrite, "**/*", null, ApprovalPolicyType.AutoApprove),
            new Rule("specific", OperationCategory.FileWrite, "**/*.sensitive", null, ApprovalPolicyType.Deny)
        };
        var evaluator = new RuleEvaluator(rules, ApprovalPolicyType.Prompt, NullLogger<RuleEvaluator>.Instance);
        
        var operation = new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write sensitive file",
            Details: new Dictionary<string, object> { { "path", "data.sensitive" } }.AsReadOnly());
        
        // Act
        var policy = evaluator.Evaluate(operation);
        
        // Assert
        Assert.Equal(ApprovalPolicyType.AutoApprove, policy); // First (general) rule wins
    }
    
    [Fact]
    public void Should_Apply_Default_When_No_Match()
    {
        // Arrange
        var rules = new List<IRule>
        {
            new Rule("tests", OperationCategory.FileWrite, "**/*.test.ts", null, ApprovalPolicyType.AutoApprove)
        };
        var evaluator = new RuleEvaluator(rules, ApprovalPolicyType.Prompt, NullLogger<RuleEvaluator>.Instance);
        
        var operation = new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write file",
            Details: new Dictionary<string, object> { { "path", "src/App.ts" } }.AsReadOnly());
        
        // Act
        var policy = evaluator.Evaluate(operation);
        
        // Assert
        Assert.Equal(ApprovalPolicyType.Prompt, policy); // Default policy
    }
}

public class SecretRedactorTests
{
    private readonly SecretRedactor _redactor;
    
    public SecretRedactorTests()
    {
        _redactor = new SecretRedactor(NullLogger<SecretRedactor>.Instance);
    }
    
    [Fact]
    public void Should_Redact_API_Keys()
    {
        // Arrange
        var content = "apiKey = 'sk_test_abc123xyz'";
        
        // Act
        var (redacted, count) = _redactor.Redact(content);
        
        // Assert
        Assert.Contains("[REDACTED]", redacted);
        Assert.Equal(1, count);
    }
    
    [Fact]
    public void Should_Redact_Passwords()
    {
        // Arrange
        var content = "password: mySecretPass123";
        
        // Act
        var (redacted, count) = _redactor.Redact(content);
        
        // Assert
        Assert.Contains("[REDACTED]", redacted);
        Assert.DoesNotContain("mySecretPass123", redacted);
        Assert.Equal(1, count);
    }
    
    [Fact]
    public void Should_Count_Redactions()
    {
        // Arrange
        var content = @"
apiKey = 'abc123'
password = 'secret'
token = 'xyz789'
";
        
        // Act
        var (redacted, count) = _redactor.Redact(content);
        
        // Assert
        Assert.Equal(3, count);
        Assert.DoesNotContain("abc123", redacted);
        Assert.DoesNotContain("secret", redacted);
        Assert.DoesNotContain("xyz789", redacted);
    }
}
```

### Integration Tests

```csharp
namespace AgenticCoder.Application.Tests.Integration.Approvals.Rules;

public class RuleIntegrationTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;
    
    public RuleIntegrationTests(TestServerFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task Should_Load_Rules_From_Config()
    {
        // Arrange
        var configPath = Path.Combine(_fixture.WorkspaceRoot, ".agent", "config.yml");
        await File.WriteAllTextAsync(configPath, @"
approval_rules:
  - name: auto-tests
    category: file_write
    path_pattern: '**/*.test.ts'
    policy: auto
");
        
        var loader = _fixture.GetService<IRuleLoader>();
        
        // Act
        var rules = await loader.LoadAsync(CancellationToken.None);
        
        // Assert
        Assert.Single(rules);
        Assert.Equal("auto-tests", rules[0].Name);
    }
    
    [Fact]
    public async Task Should_Merge_With_Defaults()
    {
        // Arrange
        var loader = _fixture.GetService<IRuleLoader>();
        
        // Act
        var rules = await loader.LoadAsync(CancellationToken.None);
        
        // Assert - Should have built-in default rules
        Assert.NotEmpty(rules);
    }
    
    [Fact]
    public void Should_Evaluate_Real_Operations()
    {
        // Arrange
        var evaluator = _fixture.GetService<IRuleEvaluator>();
        var operation = new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write test file",
            Details: new Dictionary<string, object> { { "path", "src/App.test.ts" } }.AsReadOnly());
        
        // Act
        var policy = evaluator.Evaluate(operation);
        
        // Assert
        Assert.NotNull(policy);
    }
}

public class PromptIntegrationTests
{
    [Fact]
    public void Should_Render_Correctly()
    {
        // Arrange
        var mockConsole = new Mock<IConsole>();
        var renderer = new ApprovalPromptRenderer(mockConsole.Object, NullLogger<ApprovalPromptRenderer>.Instance);
        
        var operation = new Operation(
            Category: OperationCategory.FileWrite,
            Description: "Write UserService.cs",
            Details: new Dictionary<string, object> 
            { 
                { "path", "src/UserService.cs" },
                { "size", 120 }
            }.AsReadOnly());
        
        // Act
        renderer.Render(operation);
        
        // Assert
        mockConsole.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("FILE_WRITE"))), Times.AtLeastOnce);
        mockConsole.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("UserService.cs"))), Times.AtLeastOnce);
    }
    
    [Fact]
    public void Should_Handle_Input()
    {
        // Arrange
        var mockConsole = new Mock<IConsole>();
        mockConsole.Setup(c => c.ReadKey(true))
            .Returns(new ConsoleKeyInfo('A', ConsoleKey.A, false, false, false));
        
        var handler = new PromptInputHandler(mockConsole.Object);
        
        // Act
        var response = handler.GetResponse();
        
        // Assert
        Assert.True(response.Approved);
    }
}
```

### E2E Tests

```csharp
namespace AgenticCoder.Application.Tests.E2E.Approvals.Rules;

public class RuleE2ETests : IClassFixture<E2ETestFixture>
{
    private readonly E2ETestFixture _fixture;
    
    public RuleE2ETests(E2ETestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task Should_Apply_Custom_Rules_End_To_End()
    {
        // Arrange
        var cli = _fixture.CreateCLI();
        await _fixture.WriteConfigAsync(@"
approval_rules:
  - name: auto-tests
    category: file_write
    path_pattern: '**/*.test.ts'
    policy: auto
");
        
        // Act - Write a test file
        var exitCode = await cli.RunAsync(new[] { "run", "write src/App.test.ts" });
        
        // Assert - Should auto-approve without prompting
        Assert.Equal(0, exitCode);
    }
    
    [Fact]
    public async Task Should_Override_Defaults()
    {
        // Arrange
        var cli = _fixture.CreateCLI();
        await _fixture.WriteConfigAsync(@"
approval_rules:
  - name: deny-deletes
    category: file_delete
    path_pattern: '**/*'
    policy: deny
");
        
        // Act - Attempt file deletion
        var exitCode = await cli.RunAsync(new[] { "run", "delete temp.txt" });
        
        // Assert - Should be denied
        Assert.Equal(60, exitCode); // Approval denied exit code
    }
    
    [Fact]
    public async Task Should_Show_Correct_Prompts_For_Matched_Rules()
    {
        // Arrange
        var cli = _fixture.CreateCLI();
        await _fixture.WriteConfigAsync(@"
approval_rules:
  - name: prompt-src
    category: file_write
    path_pattern: 'src/**'
    policy: prompt
    prompt_message: 'Write to source directory?'
");
        
        // Act
        var exitCode = await cli.RunAsync(new[] { "run", "write src/App.ts" });
        
        // Assert - Would need to mock prompt interaction
        Assert.NotEqual(-1, exitCode);
    }
}
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Rule parsing | 10ms | 50ms |
| Pattern matching | 1ms | 5ms |
| Prompt render | 25ms | 50ms |

---

## User Verification Steps

### Scenario 1: Default Rules

1. Run task without custom rules
2. Trigger file write
3. Verify: Prompt shown
4. Trigger file read
5. Verify: No prompt

### Scenario 2: Custom Rule

1. Add auto rule for *.test.ts
2. Trigger test file write
3. Verify: No prompt
4. Trigger non-test write
5. Verify: Prompt shown

### Scenario 3: Rule Order

1. Add specific rule first
2. Add general rule second
3. Trigger specific match
4. Verify: Specific rule applies

### Scenario 4: Glob Pattern

1. Add rule with ** pattern
2. Trigger nested file match
3. Verify: Rule matches

### Scenario 5: View Full

1. Trigger prompt
2. Press V
3. Verify: Full content shown
4. Press key
5. Verify: Back to prompt

### Scenario 6: Secret Redaction

1. Create file with secrets
2. Trigger write prompt
3. Verify: Secrets redacted
4. Press V
5. Verify: Still redacted

### Scenario 7: Help

1. Trigger prompt
2. Press ?
3. Verify: Help shown
4. Press key
5. Verify: Back to prompt

### Scenario 8: Invalid Input

1. Trigger prompt
2. Press invalid key
3. Verify: Re-prompts
4. Press A
5. Verify: Approved

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/
├── Approvals/
│   └── Rules/
│       ├── IRule.cs
│       ├── Rule.cs
│       ├── RuleParser.cs
│       ├── RuleEvaluator.cs
│       └── Patterns/
│           ├── IPatternMatcher.cs
│           ├── GlobMatcher.cs
│           └── RegexMatcher.cs
│
src/AgenticCoder.CLI/
├── Prompts/
│   ├── ApprovalPromptRenderer.cs
│   ├── PromptComponents/
│   │   ├── HeaderComponent.cs
│   │   ├── PreviewComponent.cs
│   │   ├── OptionsComponent.cs
│   │   └── TimeoutComponent.cs
│   └── SecretRedactor.cs
```

### IRule Interface

```csharp
namespace AgenticCoder.Application.Approvals.Rules;

public interface IRule
{
    string Name { get; }
    bool Matches(Operation operation);
    ApprovalPolicyType Policy { get; }
}
```

### Rule Complete Implementation

```csharp\nnamespace AgenticCoder.Application.Approvals.Rules;\n\npublic sealed class Rule : IRule\n{\n    private readonly IPatternMatcher _pathMatcher;\n    private readonly IPatternMatcher _commandMatcher;\n    \n    public string Name { get; }\n    public OperationCategory? Category { get; }\n    public string PathPattern { get; }\n    public string CommandPattern { get; }\n    public ApprovalPolicyType Policy { get; }\n    public string PromptMessage { get; }\n    \n    public Rule(\n        string name,\n        OperationCategory? category,\n        string pathPattern,\n        string commandPattern,\n        ApprovalPolicyType policy,\n        string promptMessage = null)\n    {\n        Name = name ?? throw new ArgumentNullException(nameof(name));\n        Category = category;\n        PathPattern = pathPattern;\n        CommandPattern = commandPattern;\n        Policy = policy;\n        PromptMessage = promptMessage;\n        \n        if (!string.IsNullOrWhiteSpace(pathPattern))\n        {\n            _pathMatcher = new GlobMatcher(pathPattern);\n        }\n        \n        if (!string.IsNullOrWhiteSpace(commandPattern))\n        {\n            _commandMatcher = new RegexMatcher(commandPattern);\n        }\n    }\n    \n    public bool Matches(Operation operation)\n    {\n        // Check category if specified\n        if (Category.HasValue && operation.Category != Category.Value)\n        {\n            return false;\n        }\n        \n        // Check path pattern if specified\n        if (_pathMatcher != null)\n        {\n            if (!operation.Details.TryGetValue(\"path\", out var pathObj))\n            {\n                return false;\n            }\n            \n            var path = pathObj?.ToString();\n            if (string.IsNullOrEmpty(path) || !_pathMatcher.Matches(path))\n            {\n                return false;\n            }\n        }\n        \n        // Check command pattern if specified\n        if (_commandMatcher != null)\n        {\n            if (!operation.Details.TryGetValue(\"command\", out var commandObj))\n            {\n                return false;\n            }\n            \n            var command = commandObj?.ToString();\n            if (string.IsNullOrEmpty(command) || !_commandMatcher.Matches(command))\n            {\n                return false;\n            }\n        }\n        \n        return true;\n    }\n}\n```\n\n### RuleParser Complete Implementation\n\n```csharp\nnamespace AgenticCoder.Application.Approvals.Rules;\n\npublic interface IRuleParser\n{\n    IReadOnlyList<IRule> Parse(string yaml);\n}\n\npublic sealed class RuleParser : IRuleParser\n{\n    private readonly ILogger<RuleParser> _logger;\n    \n    public RuleParser(ILogger<RuleParser> logger)\n    {\n        _logger = logger ?? throw new ArgumentNullException(nameof(logger));\n    }\n    \n    public IReadOnlyList<IRule> Parse(string yaml)\n    {\n        _logger.LogDebug(\"Parsing approval rules from YAML\");\n        \n        try\n        {\n            var deserializer = new DeserializerBuilder()\n                .WithNamingConvention(UnderscoredNamingConvention.Instance)\n                .Build();\n            \n            var config = deserializer.Deserialize<ApprovalConfig>(yaml);\n            \n            if (config?.ApprovalRules == null)\n            {\n                return Array.Empty<IRule>();\n            }\n            \n            var rules = new List<IRule>();\n            \n            foreach (var ruleDto in config.ApprovalRules)\n            {\n                ValidateRule(ruleDto);\n                \n                var policy = ParsePolicy(ruleDto.Policy);\n                var category = ParseCategory(ruleDto.Category);\n                \n                var rule = new Rule(\n                    Name: ruleDto.Name,\n                    Category: category,\n                    PathPattern: ruleDto.PathPattern,\n                    CommandPattern: ruleDto.CommandPattern,\n                    Policy: policy,\n                    PromptMessage: ruleDto.PromptMessage);\n                \n                rules.Add(rule);\n                _logger.LogDebug(\"Parsed rule: {RuleName} -> {Policy}\", rule.Name, policy);\n            }\n            \n            return rules.AsReadOnly();\n        }\n        catch (YamlException ex)\n        {\n            throw new InvalidRuleException(\"Failed to parse YAML\", ex);\n        }\n    }\n    \n    private void ValidateRule(RuleDto rule)\n    {\n        if (string.IsNullOrWhiteSpace(rule.Name))\n        {\n            throw new InvalidRuleException(\"Rule name is required\");\n        }\n        \n        if (string.IsNullOrWhiteSpace(rule.Policy))\n        {\n            throw new InvalidRuleException($\"Policy is required for rule {rule.Name}\");\n        }\n        \n        if (rule.PathPattern == null && rule.CommandPattern == null)\n        {\n            throw new InvalidRuleException($\"Rule {rule.Name} must have either path_pattern or command_pattern\");\n        }\n    }\n    \n    private ApprovalPolicyType ParsePolicy(string policy)\n    {\n        return policy.ToLowerInvariant() switch\n        {\n            \"auto\" or \"auto_approve\" => ApprovalPolicyType.AutoApprove,\n            \"prompt\" => ApprovalPolicyType.Prompt,\n            \"deny\" => ApprovalPolicyType.Deny,\n            _ => throw new InvalidRuleException($\"Unknown policy: {policy}\")\n        };\n    }\n    \n    private OperationCategory? ParseCategory(string category)\n    {\n        if (string.IsNullOrWhiteSpace(category))\n        {\n            return null;\n        }\n        \n        return category.ToLowerInvariant() switch\n        {\n            \"file_read\" => OperationCategory.FileRead,\n            \"file_write\" => OperationCategory.FileWrite,\n            \"file_delete\" => OperationCategory.FileDelete,\n            \"directory_create\" => OperationCategory.DirectoryCreate,\n            \"terminal_command\" => OperationCategory.TerminalCommand,\n            \"external_request\" => OperationCategory.ExternalRequest,\n            _ => throw new InvalidRuleException($\"Unknown category: {category}\")\n        };\n    }\n}\n\npublic sealed class ApprovalConfig\n{\n    public List<RuleDto> ApprovalRules { get; set; }\n}\n\npublic sealed class RuleDto\n{\n    public string Name { get; set; }\n    public string Category { get; set; }\n    public string PathPattern { get; set; }\n    public string CommandPattern { get; set; }\n    public string Policy { get; set; }\n    public string PromptMessage { get; set; }\n}\n\npublic sealed class InvalidRuleException : Exception\n{\n    public InvalidRuleException(string message) : base(message) { }\n    public InvalidRuleException(string message, Exception inner) : base(message, inner) { }\n}\n```\n\n### Pattern Matchers Implementation\n\n```csharp\nnamespace AgenticCoder.Application.Approvals.Rules.Patterns;\n\npublic interface IPatternMatcher\n{\n    bool Matches(string input);\n}\n\npublic sealed class GlobMatcher : IPatternMatcher\n{\n    private readonly string _pattern;\n    private readonly bool _negate;\n    private readonly Regex _regex;\n    \n    public GlobMatcher(string pattern, bool negate = false)\n    {\n        _pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));\n        _negate = negate;\n        _regex = CompileGlobToRegex(pattern);\n    }\n    \n    public bool Matches(string input)\n    {\n        if (string.IsNullOrEmpty(input))\n        {\n            return false;\n        }\n        \n        var matches = _regex.IsMatch(input);\n        return _negate ? !matches : matches;\n    }\n    \n    private static Regex CompileGlobToRegex(string pattern)\n    {\n        // Convert glob pattern to regex\n        var regexPattern = \"^\" + Regex.Escape(pattern)\n            .Replace(\"\\\\*\\\\*\", \".*\")     // ** matches any path\n            .Replace(\"\\\\*\", \"[^/]*\")     // * matches within segment\n            .Replace(\"\\\\?\", \".\")          // ? matches single char\n            + \"$\";\n        \n        return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);\n    }\n}\n\npublic sealed class RegexMatcher : IPatternMatcher\n{\n    private readonly Regex _regex;\n    \n    public RegexMatcher(string pattern)\n    {\n        if (string.IsNullOrWhiteSpace(pattern))\n        {\n            throw new ArgumentException(\"Pattern cannot be null or empty\", nameof(pattern));\n        }\n        \n        try\n        {\n            _regex = new Regex(pattern, RegexOptions.Compiled);\n        }\n        catch (ArgumentException ex)\n        {\n            throw new InvalidRuleException($\"Invalid regex pattern: {pattern}\", ex);\n        }\n    }\n    \n    public bool Matches(string input)\n    {\n        if (string.IsNullOrEmpty(input))\n        {\n            return false;\n        }\n        \n        return _regex.IsMatch(input);\n    }\n}\n```

### RuleEvaluator

```csharp
namespace AgenticCoder.Application.Approvals.Rules;

public sealed class RuleEvaluator
{
    private readonly IReadOnlyList<IRule> _rules;
    private readonly ApprovalPolicy _defaultPolicy;
    
    public ApprovalPolicy Evaluate(Operation operation)
    {
        foreach (var rule in _rules)
        {
            if (rule.Matches(operation))
            {
                _logger.LogDebug("Rule {Name} matched", rule.Name);
                return rule.Policy;
            }
        }
        
        _logger.LogDebug("No rule matched, using default");
        return _defaultPolicy;
    }
}
```

### SecretRedactor

```csharp
namespace AgenticCoder.CLI.Prompts;

public sealed class SecretRedactor
{
    private static readonly Regex[] Patterns = new[]
    {
        new Regex(@"(?i)(api[_-]?key|apikey)\s*[:=]\s*['""]?[\w-]+", RegexOptions.Compiled),
        new Regex(@"(?i)(password|passwd|pwd)\s*[:=]\s*['""]?[^\s,;]+", RegexOptions.Compiled),
        new Regex(@"(?i)(token|secret|key)\s*[:=]\s*['""]?[\w-]+", RegexOptions.Compiled),
    };
    
    public (string content, int count) Redact(string content)
    {
        var count = 0;
        var result = content;
        
        foreach (var pattern in Patterns)
        {
            result = pattern.Replace(result, m => 
            {
                count++;
                return "[REDACTED]";
            });
        }
        
        return (result, count);
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-RULE-001 | Invalid rule syntax |
| ACODE-RULE-002 | Invalid pattern |
| ACODE-RULE-003 | Duplicate rule name |
| ACODE-RULE-004 | Invalid policy |
| ACODE-PROMPT-001 | Prompt render error |

### Logging Fields

```json
{
  "event": "rule_evaluation",
  "operation_category": "file_write",
  "operation_path": "src/file.ts",
  "rules_evaluated": 3,
  "matched_rule": "auto-tests",
  "policy": "auto",
  "evaluation_ms": 2
}
```

### Implementation Checklist

1. [ ] Create IRule interface
2. [ ] Implement Rule class
3. [ ] Create RuleParser
4. [ ] Implement config parsing
5. [ ] Create pattern matchers
6. [ ] Implement glob matching
7. [ ] Implement regex matching
8. [ ] Create RuleEvaluator
9. [ ] Implement evaluation logic
10. [ ] Create prompt renderer
11. [ ] Implement components
12. [ ] Create SecretRedactor
13. [ ] Implement redaction
14. [ ] Add input handling
15. [ ] Write unit tests
16. [ ] Write integration tests
17. [ ] Write E2E tests

### Validation Checklist Before Merge

- [ ] Rules parse correctly
- [ ] Patterns match correctly
- [ ] Evaluation order works
- [ ] Built-in rules work
- [ ] Custom rules work
- [ ] Prompts render
- [ ] All options work
- [ ] Secrets redacted
- [ ] Unit test coverage > 90%

### Rollout Plan

1. **Phase 1:** Rule interface
2. **Phase 2:** Pattern matching
3. **Phase 3:** Rule evaluation
4. **Phase 4:** Built-in rules
5. **Phase 5:** Prompt rendering
6. **Phase 6:** Input handling
7. **Phase 7:** Secret redaction
8. **Phase 8:** Integration

---

**End of Task 013.a Specification**