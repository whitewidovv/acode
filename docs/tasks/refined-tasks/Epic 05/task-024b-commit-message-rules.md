# Task 024.b: commit message rules

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Task 024 (Safe Workflow)  

---

## Description

Task 024.b implements commit message validation rules. Messages MUST conform to configured patterns before commit proceeds. Invalid messages MUST be rejected with clear guidance.

Conventional commit format MAY be enforced. This requires type prefixes (feat, fix, docs, etc.) and optional scope. The format enables automated changelog generation.

Length limits MUST be enforced. Subject line length MUST be configurable. Default MUST be 72 characters. Long messages MUST be rejected.

Issue references MAY be required. Pattern matching MUST detect issue numbers. Missing references MAY block commit.

### Business Value

Consistent commit messages improve repository history. Automated tooling can parse standardized formats. Team conventions are enforced automatically.

### Scope Boundaries

This task covers message validation only. Pre-commit verification is in 024.a. Push gating is in 024.c.

### Integration Points

- Task 024: Workflow orchestration
- Task 002: Configuration

### Failure Modes

- Invalid format → Clear error with expected format
- Too long → Show length and limit
- Missing reference → Show required pattern

---

## Functional Requirements

### FR-001 to FR-030: Validation Rules

- FR-001: `ICommitMessageValidator` interface MUST be defined
- FR-002: `ValidateAsync` MUST check message
- FR-003: Result MUST indicate valid/invalid
- FR-004: Result MUST include error messages
- FR-005: Result MUST include suggestions
- FR-006: Pattern matching MUST be configurable
- FR-007: Default pattern MUST be null (any message)
- FR-008: Conventional commit pattern MUST be available
- FR-009: Conventional pattern MUST be `^(feat|fix|docs|chore|refactor|test|style|perf|ci|build)(\(.+\))?: .+`
- FR-010: Custom pattern MUST be regex
- FR-011: Invalid regex MUST produce config error
- FR-012: `maxLength` MUST limit subject line
- FR-013: Default `maxLength` MUST be 72
- FR-014: Length check MUST count first line only
- FR-015: `minLength` MUST require minimum
- FR-016: Default `minLength` MUST be 10
- FR-017: `requireIssueReference` MUST control reference check
- FR-018: Default `requireIssueReference` MUST be false
- FR-019: Issue pattern MUST be configurable
- FR-020: Default issue pattern MUST be `#\d+`
- FR-021: Multiple issue references MUST be allowed
- FR-022: `requireScope` MUST control scope presence
- FR-023: Default `requireScope` MUST be false
- FR-024: Scope MUST match allowed values if configured
- FR-025: `allowedScopes` MUST list valid scopes
- FR-026: Unrecognized scope MUST be rejected if enforced
- FR-027: `requireBody` MUST control body presence
- FR-028: Default `requireBody` MUST be false
- FR-029: Body MUST be separated by blank line
- FR-030: Trailing whitespace MUST be warned

### FR-031 to FR-040: Error Reporting

- FR-031: Pattern mismatch MUST show expected format
- FR-032: Length violation MUST show current vs limit
- FR-033: Missing reference MUST show expected pattern
- FR-034: Invalid scope MUST list allowed values
- FR-035: Error messages MUST be actionable
- FR-036: Auto-fix suggestions MAY be provided
- FR-037: Examples MUST be shown for complex rules
- FR-038: Conventional commit types MUST be explained
- FR-039: Validation MUST be fast (<10ms)
- FR-040: Validation MUST be pure (no side effects)

---

## Non-Functional Requirements

- NFR-001: Validation MUST complete in <10ms
- NFR-002: Memory MUST be <1MB for validation
- NFR-003: Regex MUST not be ReDoS vulnerable
- NFR-004: Long messages MUST not cause issues
- NFR-005: Unicode MUST be handled correctly
- NFR-006: Newlines MUST be normalized
- NFR-007: Empty message MUST be rejected
- NFR-008: Whitespace-only MUST be rejected

---

## User Manual Documentation

### Configuration

```yaml
workflow:
  commitMessage:
    enabled: true
    pattern: "^(feat|fix|docs|chore|refactor|test)\\(.*\\): .+"
    maxLength: 72
    minLength: 10
    requireIssueReference: false
    issuePattern: "#\\d+"
    requireScope: false
    allowedScopes:
      - api
      - cli
      - core
      - docs
    requireBody: false
```

### Examples

**Valid conventional commits:**

```
feat(api): add user authentication
fix(cli): handle missing config file
docs: update installation guide
chore(deps): bump dependencies
```

**Invalid examples:**

```
Added feature          # No type prefix
feat: x               # Too short
feat(api): This is a very long commit message that exceeds the maximum allowed length for the subject line
```

### Error Messages

```
Commit message validation failed:

✗ Pattern mismatch
  Expected format: type(scope): description
  Examples: feat(api): add endpoint, fix: handle null
  
✗ Subject too long
  Current: 95 characters
  Maximum: 72 characters
  
✗ Missing issue reference
  Expected pattern: #123
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Pattern validation works
- [ ] AC-002: Length validation works
- [ ] AC-003: Issue reference detection works
- [ ] AC-004: Scope validation works
- [ ] AC-005: Error messages are clear
- [ ] AC-006: Examples shown
- [ ] AC-007: Conventional commit supported
- [ ] AC-008: Custom patterns work
- [ ] AC-009: Configuration respected
- [ ] AC-010: Performance <10ms

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test pattern matching
- [ ] UT-002: Test length validation
- [ ] UT-003: Test issue reference
- [ ] UT-004: Test scope validation
- [ ] UT-005: Test error messages

### Integration Tests

- [ ] IT-001: Full validation cycle
- [ ] IT-002: Config loading
- [ ] IT-003: Edge cases

---

## Implementation Prompt

### Interface

```csharp
public interface ICommitMessageValidator
{
    ValidationResult Validate(string message);
}

public record ValidationResult(
    bool IsValid,
    IReadOnlyList<ValidationError> Errors,
    IReadOnlyList<string> Suggestions);

public record ValidationError(
    string Code,
    string Message,
    string? Expected,
    string? Actual);

public record CommitMessageRules(
    bool Enabled,
    string? Pattern,
    int MaxLength,
    int MinLength,
    bool RequireIssueReference,
    string IssuePattern,
    bool RequireScope,
    IReadOnlyList<string>? AllowedScopes,
    bool RequireBody);
```

---

**End of Task 024.b Specification**