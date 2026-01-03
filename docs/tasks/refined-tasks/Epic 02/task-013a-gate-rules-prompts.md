# Task 013.a: Gate Rules + Prompts

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 013 (Human Approval Gates), Task 002 (Config Schema)  

---

## Description

Task 013.a implements the rule engine and prompt system for human approval gates. Rules determine when approval is required and what policy applies. Prompts are the user-facing interface for requesting and capturing approval decisions. Together, they form the configurable, user-friendly approval experience.

The rule engine evaluates operations against configured rules to determine the appropriate approval policy. Rules can match on operation category, file patterns, command patterns, and other criteria. This flexibility allows users to fine-tune approval behavior to their workflow and risk tolerance.

Rule precedence is deterministic: rules are evaluated in definition order, and the first matching rule wins. This allows specific rules to override general ones. A rule for "*.test.ts" can auto-approve test file writes while the default rule prompts for all file writes.

Built-in rules provide sensible defaults. File reads are auto-approved. File writes and deletes prompt for approval. Terminal commands prompt. These defaults work for most users while allowing customization.

Custom rules are defined in `.agent/config.yml` (Task 002). The schema supports glob patterns for file paths, regex patterns for commands, and operation categories. Rules can specify any approval policy: auto, prompt, deny, or skip.

The prompt system renders approval requests in the CLI. Prompts are informative but not overwhelming. They show what the operation is, what will happen, and what the user's options are. Users can approve, deny, skip, view details, or get help.

Prompt design follows CLI best practices. Key bindings are single characters. Default actions are highlighted. Timeouts show remaining time. Colors and formatting improve readability. Accessibility is considered.

Operation previews show relevant details without overwhelming. File writes show the first N lines with option to view all. Terminal commands show the command and working directory. Binary files show type and size rather than content.

Secret redaction protects sensitive data. API keys, passwords, and tokens in file content are detected and replaced with [REDACTED] in previews. This prevents accidental exposure during approval review.

Prompt localization is designed for but not implemented in this task. The prompt system uses resource strings that could be localized in the future. Current implementation is English only.

Error handling ensures prompts never crash. Invalid input re-prompts. Terminal issues fall back to simpler display. The prompt system degrades gracefully under adverse conditions.

Testing rules requires comprehensive scenarios. Every pattern type, every operation category, and every edge case. The rule engine must be reliable—incorrect rule evaluation could block legitimate work or allow dangerous operations.

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

```
Tests/Unit/Approvals/Rules/
├── RuleParserTests.cs
│   ├── Should_Parse_Valid_Rules()
│   ├── Should_Reject_Invalid_Rules()
│   └── Should_Handle_All_Fields()
│
├── PatternMatcherTests.cs
│   ├── Should_Match_Glob_Patterns()
│   ├── Should_Match_Regex_Patterns()
│   └── Should_Handle_Negation()
│
├── RuleEvaluatorTests.cs
│   ├── Should_Evaluate_In_Order()
│   ├── Should_Use_First_Match()
│   └── Should_Apply_Default()
│
└── SecretRedactorTests.cs
    ├── Should_Redact_API_Keys()
    ├── Should_Redact_Passwords()
    └── Should_Count_Redactions()
```

### Integration Tests

```
Tests/Integration/Approvals/Rules/
├── RuleIntegrationTests.cs
│   ├── Should_Load_From_Config()
│   ├── Should_Merge_With_Defaults()
│   └── Should_Evaluate_Real_Operations()
│
└── PromptIntegrationTests.cs
    ├── Should_Render_Correctly()
    └── Should_Handle_Input()
```

### E2E Tests

```
Tests/E2E/Approvals/Rules/
├── RuleE2ETests.cs
│   ├── Should_Apply_Custom_Rules()
│   ├── Should_Override_Defaults()
│   └── Should_Show_Correct_Prompts()
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
    ApprovalPolicy Policy { get; }
}

public sealed record Rule(
    string Name,
    OperationCategory? Category,
    string? PathPattern,
    string? CommandPattern,
    ApprovalPolicy Policy) : IRule;
```

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