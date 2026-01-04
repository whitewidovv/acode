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

## Assumptions

### Technical Assumptions

1. **Configuration available** - Message rules in agent-config.yml
2. **Regex support** - .NET regex for pattern matching
3. **UTF-8 handling** - Unicode in messages supported
4. **Line parsing** - Can extract subject line from message

### Rule Assumptions

5. **Subject line is first line** - Separated by blank line from body
6. **Length counts characters** - Not bytes
7. **Pattern is optional** - Can allow any message format
8. **Conventional commits supported** - Standard format available
9. **Issue references parsed** - Can extract #123 style refs
10. **Scope is optional** - Only checked if requireScope true

### Validation Assumptions

11. **Synchronous validation** - Fast enough to not need async
12. **Clear error messages** - Explain what's wrong and how to fix
13. **Multiple rules** - All configured rules checked
14. **First failure reported** - Or all failures, configurable

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

## Best Practices

### Message Format

1. **Subject line concise** - Under 72 characters
2. **Imperative mood** - "Add feature" not "Added feature"
3. **Separate subject and body** - Blank line between
4. **Explain why, not what** - Body explains reasoning

### Validation Rules

5. **Start permissive** - Don't over-constrain initially
6. **Conventional commits optional** - Only if team uses them
7. **Issue reference flexible** - Support multiple formats
8. **Provide examples** - Show valid message format

### Error Handling

9. **Clear error messages** - Explain what's wrong
10. **Show expected format** - Example of valid message
11. **Highlight specific issue** - Point to exact problem
12. **Suggest fix** - How to correct the message

---

## Troubleshooting

### Issue: Valid message rejected

**Symptoms:** Message that looks correct is rejected

**Causes:**
- Hidden characters (non-breaking space, etc.)
- Line ending issues (CRLF vs LF)
- Pattern too strict

**Solutions:**
1. Check for hidden characters in message
2. Normalize line endings
3. Review and relax pattern if needed

### Issue: Pattern matching too slow

**Symptoms:** Validation takes noticeable time

**Causes:**
- Catastrophic regex backtracking
- Very long message with complex pattern
- Multiple patterns evaluated

**Solutions:**
1. Simplify regex pattern
2. Add length check before pattern
3. Use possessive quantifiers if available

### Issue: Issue reference not detected

**Symptoms:** #123 in message but "missing issue reference" error

**Causes:**
- Reference pattern mismatch
- Reference in wrong location
- Unicode number characters used

**Solutions:**
1. Check issuePattern configuration
2. Verify reference is in checked location
3. Use ASCII digits only

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

### File Structure

```
src/
├── Acode.Core/
│   └── Domain/
│       └── Workflow/
│           ├── CommitMessageRules.cs      # Rule configuration
│           └── ValidationResult.cs        # Validation outcome
│
├── Acode.Application/
│   └── Services/
│       └── Workflow/
│           ├── ICommitMessageValidator.cs # Validator interface
│           ├── CommitMessageValidator.cs  # Implementation
│           ├── ConventionalCommitParser.cs # Parse conventional format
│           └── Rules/
│               ├── LengthRule.cs          # Length validation
│               ├── PatternRule.cs         # Regex matching
│               ├── IssueReferenceRule.cs  # Issue number detection
│               └── ScopeRule.cs           # Scope validation
│
└── Acode.Cli/
    └── Commands/
        └── Workflow/
            └── ValidateMessageCommand.cs

tests/
├── Acode.Application.Tests/
│   └── Services/
│       └── Workflow/
│           ├── CommitMessageValidatorTests.cs
│           ├── ConventionalCommitParserTests.cs
│           └── Rules/
│               ├── LengthRuleTests.cs
│               ├── PatternRuleTests.cs
│               └── IssueReferenceRuleTests.cs
```

### Domain Models

```csharp
// Acode.Core/Domain/Workflow/CommitMessageRules.cs
namespace Acode.Core.Domain.Workflow;

/// <summary>
/// Configuration for commit message validation rules.
/// </summary>
public sealed record CommitMessageRules
{
    /// <summary>
    /// Whether message validation is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;
    
    /// <summary>
    /// Regex pattern for the commit message (null = any message allowed).
    /// </summary>
    public string? Pattern { get; init; }
    
    /// <summary>
    /// Maximum length for the subject line.
    /// </summary>
    public int MaxLength { get; init; } = 72;
    
    /// <summary>
    /// Minimum length for the subject line.
    /// </summary>
    public int MinLength { get; init; } = 10;
    
    /// <summary>
    /// Whether an issue reference is required.
    /// </summary>
    public bool RequireIssueReference { get; init; }
    
    /// <summary>
    /// Pattern for detecting issue references.
    /// </summary>
    public string IssuePattern { get; init; } = @"#\d+";
    
    /// <summary>
    /// Whether a scope is required (for conventional commits).
    /// </summary>
    public bool RequireScope { get; init; }
    
    /// <summary>
    /// Allowed scope values (null = any scope allowed).
    /// </summary>
    public IReadOnlyList<string>? AllowedScopes { get; init; }
    
    /// <summary>
    /// Whether a body is required.
    /// </summary>
    public bool RequireBody { get; init; }
    
    /// <summary>
    /// Use conventional commit format validation.
    /// </summary>
    public bool UseConventionalCommit { get; init; }
    
    /// <summary>
    /// Allowed commit types for conventional commit.
    /// </summary>
    public IReadOnlyList<string> AllowedTypes { get; init; } = 
        ["feat", "fix", "docs", "style", "refactor", "test", "chore", "perf", "ci", "build"];
    
    /// <summary>
    /// Default configuration.
    /// </summary>
    public static CommitMessageRules Default => new();
    
    /// <summary>
    /// Conventional commit configuration.
    /// </summary>
    public static CommitMessageRules ConventionalCommit => new()
    {
        UseConventionalCommit = true,
        Pattern = @"^(feat|fix|docs|style|refactor|test|chore|perf|ci|build)(\(.+\))?: .+"
    };
}

// Acode.Core/Domain/Workflow/ValidationResult.cs
namespace Acode.Core.Domain.Workflow;

/// <summary>
/// Result of commit message validation.
/// </summary>
public sealed record ValidationResult
{
    /// <summary>
    /// Whether the message is valid.
    /// </summary>
    public bool IsValid { get; init; }
    
    /// <summary>
    /// Validation errors.
    /// </summary>
    public required IReadOnlyList<ValidationError> Errors { get; init; }
    
    /// <summary>
    /// Suggestions for fixing the message.
    /// </summary>
    public required IReadOnlyList<string> Suggestions { get; init; }
    
    /// <summary>
    /// Warnings that don't fail validation.
    /// </summary>
    public IReadOnlyList<ValidationWarning>? Warnings { get; init; }
    
    /// <summary>
    /// Parsed conventional commit if applicable.
    /// </summary>
    public ConventionalCommit? ParsedCommit { get; init; }
    
    public static ValidationResult Valid(ConventionalCommit? parsed = null) => new()
    {
        IsValid = true,
        Errors = [],
        Suggestions = [],
        ParsedCommit = parsed
    };
    
    public static ValidationResult Invalid(
        IEnumerable<ValidationError> errors,
        IEnumerable<string>? suggestions = null) => new()
    {
        IsValid = false,
        Errors = errors.ToList(),
        Suggestions = suggestions?.ToList() ?? []
    };
}

/// <summary>
/// A validation error.
/// </summary>
public sealed record ValidationError
{
    /// <summary>Error code for programmatic handling.</summary>
    public required string Code { get; init; }
    
    /// <summary>Human-readable error message.</summary>
    public required string Message { get; init; }
    
    /// <summary>Expected value or format.</summary>
    public string? Expected { get; init; }
    
    /// <summary>Actual value found.</summary>
    public string? Actual { get; init; }
    
    /// <summary>Line number if applicable (1-based).</summary>
    public int? Line { get; init; }
}

/// <summary>
/// A validation warning (doesn't fail validation).
/// </summary>
public sealed record ValidationWarning(string Code, string Message);

/// <summary>
/// Parsed conventional commit.
/// </summary>
public sealed record ConventionalCommit
{
    /// <summary>Commit type (feat, fix, etc.).</summary>
    public required string Type { get; init; }
    
    /// <summary>Scope (optional).</summary>
    public string? Scope { get; init; }
    
    /// <summary>Whether this is a breaking change.</summary>
    public bool IsBreaking { get; init; }
    
    /// <summary>The commit description.</summary>
    public required string Description { get; init; }
    
    /// <summary>Commit body (optional).</summary>
    public string? Body { get; init; }
    
    /// <summary>Footer lines.</summary>
    public IReadOnlyList<CommitFooter>? Footers { get; init; }
}

/// <summary>
/// A footer in a conventional commit.
/// </summary>
public sealed record CommitFooter(string Token, string Value);
```

### Validator Interface

```csharp
// Acode.Application/Services/Workflow/ICommitMessageValidator.cs
namespace Acode.Application.Services.Workflow;

/// <summary>
/// Validates commit messages according to configured rules.
/// </summary>
public interface ICommitMessageValidator
{
    /// <summary>
    /// Validates a commit message.
    /// </summary>
    /// <param name="message">The commit message to validate.</param>
    /// <returns>Validation result with errors and suggestions.</returns>
    ValidationResult Validate(string message);
    
    /// <summary>
    /// Gets the current validation rules.
    /// </summary>
    CommitMessageRules GetRules();
}
```

### Conventional Commit Parser

```csharp
// Acode.Application/Services/Workflow/ConventionalCommitParser.cs
namespace Acode.Application.Services.Workflow;

/// <summary>
/// Parses commit messages in conventional commit format.
/// </summary>
public static class ConventionalCommitParser
{
    // Pattern: type(scope)!: description
    private static readonly Regex HeaderPattern = new(
        @"^(?<type>[a-z]+)(\((?<scope>[^)]+)\))?(?<breaking>!)?: (?<description>.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    // Footer pattern: Token: Value or Token #Value (for issue references)
    private static readonly Regex FooterPattern = new(
        @"^(?<token>[\w-]+|BREAKING CHANGE)(?:: | #)(?<value>.+)$",
        RegexOptions.Compiled);
    
    /// <summary>
    /// Attempts to parse a commit message as a conventional commit.
    /// </summary>
    /// <param name="message">The commit message.</param>
    /// <param name="result">The parsed result if successful.</param>
    /// <returns>True if parsed successfully.</returns>
    public static bool TryParse(string message, out ConventionalCommit? result)
    {
        result = null;
        
        if (string.IsNullOrWhiteSpace(message))
            return false;
        
        // Split into lines
        var lines = message.Split(['\r', '\n'], StringSplitOptions.None);
        if (lines.Length == 0)
            return false;
        
        // Parse header (first line)
        var headerLine = lines[0].Trim();
        var headerMatch = HeaderPattern.Match(headerLine);
        if (!headerMatch.Success)
            return false;
        
        var type = headerMatch.Groups["type"].Value.ToLowerInvariant();
        var scope = headerMatch.Groups["scope"].Success 
            ? headerMatch.Groups["scope"].Value 
            : null;
        var isBreaking = headerMatch.Groups["breaking"].Success;
        var description = headerMatch.Groups["description"].Value;
        
        // Parse body and footers
        string? body = null;
        var footers = new List<CommitFooter>();
        var bodyLines = new List<string>();
        var inFooter = false;
        
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            
            // Empty line separates header from body, body from footer
            if (string.IsNullOrWhiteSpace(line))
            {
                if (bodyLines.Count > 0 && !inFooter)
                {
                    body = string.Join("\n", bodyLines).Trim();
                    bodyLines.Clear();
                    inFooter = true;
                }
                continue;
            }
            
            // Check if this is a footer
            var footerMatch = FooterPattern.Match(line);
            if (footerMatch.Success)
            {
                inFooter = true;
                var token = footerMatch.Groups["token"].Value;
                var value = footerMatch.Groups["value"].Value;
                
                // BREAKING CHANGE footer
                if (token.Equals("BREAKING CHANGE", StringComparison.OrdinalIgnoreCase) ||
                    token.Equals("BREAKING-CHANGE", StringComparison.OrdinalIgnoreCase))
                {
                    isBreaking = true;
                }
                
                footers.Add(new CommitFooter(token, value));
            }
            else if (!inFooter)
            {
                bodyLines.Add(line);
            }
        }
        
        // Trailing body content
        if (bodyLines.Count > 0 && body == null)
        {
            body = string.Join("\n", bodyLines).Trim();
        }
        
        result = new ConventionalCommit
        {
            Type = type,
            Scope = scope,
            IsBreaking = isBreaking,
            Description = description,
            Body = body,
            Footers = footers.Count > 0 ? footers : null
        };
        
        return true;
    }
    
    /// <summary>
    /// Gets the subject line (first line) from a commit message.
    /// </summary>
    public static string GetSubjectLine(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "";
        
        var newlineIndex = message.IndexOfAny(['\r', '\n']);
        return newlineIndex >= 0 ? message[..newlineIndex].Trim() : message.Trim();
    }
    
    /// <summary>
    /// Checks if a message has a body (content after first blank line).
    /// </summary>
    public static bool HasBody(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;
        
        var lines = message.Split(['\r', '\n'], StringSplitOptions.None);
        var foundBlankLine = false;
        
        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                foundBlankLine = true;
            }
            else if (foundBlankLine)
            {
                return true; // Found content after blank line
            }
        }
        
        return false;
    }
}
```

### Validator Implementation

```csharp
// Acode.Application/Services/Workflow/CommitMessageValidator.cs
namespace Acode.Application.Services.Workflow;

/// <summary>
/// Validates commit messages against configured rules.
/// </summary>
public sealed class CommitMessageValidator : ICommitMessageValidator
{
    private readonly IOptions<CommitMessageRules> _rulesOptions;
    private readonly Regex? _patternRegex;
    private readonly Regex? _issueRegex;
    
    public CommitMessageValidator(IOptions<CommitMessageRules> rules)
    {
        _rulesOptions = rules;
        
        var config = rules.Value;
        
        // Pre-compile regexes
        if (!string.IsNullOrEmpty(config.Pattern))
        {
            try
            {
                _patternRegex = new Regex(config.Pattern, 
                    RegexOptions.Compiled, 
                    TimeSpan.FromSeconds(1)); // Timeout to prevent ReDoS
            }
            catch (RegexParseException)
            {
                // Invalid regex - will report in validation
            }
        }
        
        if (!string.IsNullOrEmpty(config.IssuePattern))
        {
            try
            {
                _issueRegex = new Regex(config.IssuePattern,
                    RegexOptions.Compiled,
                    TimeSpan.FromSeconds(1));
            }
            catch (RegexParseException)
            {
                // Invalid regex
            }
        }
    }
    
    public CommitMessageRules GetRules() => _rulesOptions.Value;
    
    public ValidationResult Validate(string message)
    {
        var rules = _rulesOptions.Value;
        
        if (!rules.Enabled)
        {
            return ValidationResult.Valid();
        }
        
        var errors = new List<ValidationError>();
        var suggestions = new List<string>();
        var warnings = new List<ValidationWarning>();
        ConventionalCommit? parsedCommit = null;
        
        // Normalize whitespace
        message = NormalizeMessage(message);
        
        // Empty message check
        if (string.IsNullOrWhiteSpace(message))
        {
            errors.Add(new ValidationError
            {
                Code = "EMPTY_MESSAGE",
                Message = "Commit message cannot be empty"
            });
            return ValidationResult.Invalid(errors, suggestions);
        }
        
        var subjectLine = ConventionalCommitParser.GetSubjectLine(message);
        
        // Length validation
        ValidateLength(subjectLine, rules, errors, suggestions);
        
        // Pattern validation
        ValidatePattern(subjectLine, rules, errors, suggestions);
        
        // Conventional commit validation
        if (rules.UseConventionalCommit)
        {
            ValidateConventionalCommit(message, rules, errors, suggestions, 
                warnings, out parsedCommit);
        }
        
        // Issue reference validation
        if (rules.RequireIssueReference)
        {
            ValidateIssueReference(message, rules, errors, suggestions);
        }
        
        // Body requirement
        if (rules.RequireBody)
        {
            if (!ConventionalCommitParser.HasBody(message))
            {
                errors.Add(new ValidationError
                {
                    Code = "MISSING_BODY",
                    Message = "Commit message body is required",
                    Expected = "Body text after blank line"
                });
                suggestions.Add("Add a blank line after the subject, then add body text");
            }
        }
        
        // Trailing whitespace warning
        if (subjectLine != subjectLine.TrimEnd())
        {
            warnings.Add(new ValidationWarning(
                "TRAILING_WHITESPACE",
                "Subject line has trailing whitespace"));
        }
        
        if (errors.Count > 0)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = errors,
                Suggestions = suggestions,
                Warnings = warnings,
                ParsedCommit = parsedCommit
            };
        }
        
        return new ValidationResult
        {
            IsValid = true,
            Errors = [],
            Suggestions = [],
            Warnings = warnings.Count > 0 ? warnings : null,
            ParsedCommit = parsedCommit
        };
    }
    
    private void ValidateLength(
        string subjectLine,
        CommitMessageRules rules,
        List<ValidationError> errors,
        List<string> suggestions)
    {
        if (subjectLine.Length < rules.MinLength)
        {
            errors.Add(new ValidationError
            {
                Code = "TOO_SHORT",
                Message = $"Subject line too short: {subjectLine.Length} characters",
                Expected = $"At least {rules.MinLength} characters",
                Actual = $"{subjectLine.Length} characters"
            });
            suggestions.Add("Provide a more descriptive commit message");
        }
        
        if (subjectLine.Length > rules.MaxLength)
        {
            errors.Add(new ValidationError
            {
                Code = "TOO_LONG",
                Message = $"Subject line too long: {subjectLine.Length} characters",
                Expected = $"At most {rules.MaxLength} characters",
                Actual = $"{subjectLine.Length} characters",
                Line = 1
            });
            suggestions.Add($"Shorten subject to {rules.MaxLength} characters or less");
            suggestions.Add("Move details to the commit body");
        }
    }
    
    private void ValidatePattern(
        string subjectLine,
        CommitMessageRules rules,
        List<ValidationError> errors,
        List<string> suggestions)
    {
        if (string.IsNullOrEmpty(rules.Pattern))
            return;
        
        if (_patternRegex == null)
        {
            errors.Add(new ValidationError
            {
                Code = "INVALID_PATTERN_CONFIG",
                Message = "Configured pattern is not a valid regex"
            });
            return;
        }
        
        try
        {
            if (!_patternRegex.IsMatch(subjectLine))
            {
                errors.Add(new ValidationError
                {
                    Code = "PATTERN_MISMATCH",
                    Message = "Subject line does not match required pattern",
                    Expected = $"Pattern: {rules.Pattern}",
                    Actual = subjectLine
                });
                
                if (rules.UseConventionalCommit)
                {
                    suggestions.Add("Use format: type(scope): description");
                    suggestions.Add("Examples: feat(api): add endpoint, fix: handle null");
                }
            }
        }
        catch (RegexMatchTimeoutException)
        {
            errors.Add(new ValidationError
            {
                Code = "PATTERN_TIMEOUT",
                Message = "Pattern matching timed out"
            });
        }
    }
    
    private void ValidateConventionalCommit(
        string message,
        CommitMessageRules rules,
        List<ValidationError> errors,
        List<string> suggestions,
        List<ValidationWarning> warnings,
        out ConventionalCommit? parsed)
    {
        if (!ConventionalCommitParser.TryParse(message, out parsed))
        {
            errors.Add(new ValidationError
            {
                Code = "INVALID_CONVENTIONAL_FORMAT",
                Message = "Message does not follow conventional commit format",
                Expected = "type(scope): description"
            });
            suggestions.Add($"Valid types: {string.Join(", ", rules.AllowedTypes)}");
            return;
        }
        
        // Validate type
        if (!rules.AllowedTypes.Contains(parsed!.Type, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add(new ValidationError
            {
                Code = "INVALID_TYPE",
                Message = $"Invalid commit type: '{parsed.Type}'",
                Expected = $"One of: {string.Join(", ", rules.AllowedTypes)}",
                Actual = parsed.Type
            });
        }
        
        // Validate scope requirement
        if (rules.RequireScope && string.IsNullOrEmpty(parsed.Scope))
        {
            errors.Add(new ValidationError
            {
                Code = "MISSING_SCOPE",
                Message = "Scope is required",
                Expected = "type(scope): description"
            });
            
            if (rules.AllowedScopes?.Count > 0)
            {
                suggestions.Add($"Valid scopes: {string.Join(", ", rules.AllowedScopes)}");
            }
        }
        
        // Validate allowed scopes
        if (!string.IsNullOrEmpty(parsed.Scope) && rules.AllowedScopes?.Count > 0)
        {
            if (!rules.AllowedScopes.Contains(parsed.Scope, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add(new ValidationError
                {
                    Code = "INVALID_SCOPE",
                    Message = $"Invalid scope: '{parsed.Scope}'",
                    Expected = $"One of: {string.Join(", ", rules.AllowedScopes)}",
                    Actual = parsed.Scope
                });
            }
        }
        
        // Breaking change notice
        if (parsed.IsBreaking)
        {
            warnings.Add(new ValidationWarning(
                "BREAKING_CHANGE",
                "This is a breaking change commit"));
        }
    }
    
    private void ValidateIssueReference(
        string message,
        CommitMessageRules rules,
        List<ValidationError> errors,
        List<string> suggestions)
    {
        if (_issueRegex == null)
        {
            return; // Can't validate without pattern
        }
        
        try
        {
            if (!_issueRegex.IsMatch(message))
            {
                errors.Add(new ValidationError
                {
                    Code = "MISSING_ISSUE_REFERENCE",
                    Message = "Issue reference is required",
                    Expected = $"Pattern: {rules.IssuePattern}"
                });
                suggestions.Add("Include an issue reference like #123");
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // Skip validation on timeout
        }
    }
    
    private static string NormalizeMessage(string message)
    {
        // Normalize line endings
        return message.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}
```

### CLI Command

```csharp
// Acode.Cli/Commands/Workflow/ValidateMessageCommand.cs
namespace Acode.Cli.Commands.Workflow;

[Command("validate-message", Description = "Validate a commit message")]
public sealed class ValidateMessageCommand : ICommand
{
    [CommandParameter(0, Description = "Commit message to validate")]
    public string? Message { get; init; }
    
    [CommandOption("file|f", Description = "Read message from file")]
    public string? FilePath { get; init; }
    
    [CommandOption("json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var validator = GetValidator(); // DI
        
        // Get message from argument or file
        string message;
        
        if (!string.IsNullOrEmpty(FilePath))
        {
            if (!File.Exists(FilePath))
            {
                console.Error.WriteLine($"File not found: {FilePath}");
                Environment.ExitCode = ExitCodes.FileNotFound;
                return;
            }
            message = await File.ReadAllTextAsync(FilePath);
        }
        else if (!string.IsNullOrEmpty(Message))
        {
            message = Message;
        }
        else
        {
            console.Error.WriteLine("Provide a message or use --file");
            Environment.ExitCode = ExitCodes.InvalidArgument;
            return;
        }
        
        var result = validator.Validate(message);
        
        if (Json)
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            console.Output.WriteLine(json);
        }
        else
        {
            if (result.IsValid)
            {
                console.Output.WriteLine("✓ Commit message is valid");
                
                if (result.ParsedCommit != null)
                {
                    console.Output.WriteLine();
                    console.Output.WriteLine("Parsed commit:");
                    console.Output.WriteLine($"  Type:        {result.ParsedCommit.Type}");
                    if (result.ParsedCommit.Scope != null)
                        console.Output.WriteLine($"  Scope:       {result.ParsedCommit.Scope}");
                    console.Output.WriteLine($"  Description: {result.ParsedCommit.Description}");
                    if (result.ParsedCommit.IsBreaking)
                        console.Output.WriteLine($"  ⚠ Breaking change");
                }
                
                if (result.Warnings?.Count > 0)
                {
                    console.Output.WriteLine();
                    console.Output.WriteLine("Warnings:");
                    foreach (var warning in result.Warnings)
                    {
                        console.Output.WriteLine($"  ⚠ {warning.Message}");
                    }
                }
            }
            else
            {
                console.Error.WriteLine("✗ Commit message validation failed:");
                console.Error.WriteLine();
                
                foreach (var error in result.Errors)
                {
                    console.Error.WriteLine($"  ✗ [{error.Code}] {error.Message}");
                    if (error.Expected != null)
                        console.Error.WriteLine($"    Expected: {error.Expected}");
                    if (error.Actual != null)
                        console.Error.WriteLine($"    Actual:   {error.Actual}");
                }
                
                if (result.Suggestions.Count > 0)
                {
                    console.Error.WriteLine();
                    console.Error.WriteLine("Suggestions:");
                    foreach (var suggestion in result.Suggestions)
                    {
                        console.Error.WriteLine($"  → {suggestion}");
                    }
                }
                
                Environment.ExitCode = ExitCodes.ValidationFailed;
            }
        }
    }
}

[Command("commit-format", Description = "Show commit message format help")]
public sealed class CommitFormatHelpCommand : ICommand
{
    public ValueTask ExecuteAsync(IConsole console)
    {
        var validator = GetValidator();
        var rules = validator.GetRules();
        
        console.Output.WriteLine("Commit Message Format");
        console.Output.WriteLine("=====================");
        console.Output.WriteLine();
        
        if (rules.UseConventionalCommit)
        {
            console.Output.WriteLine("Using Conventional Commit format:");
            console.Output.WriteLine();
            console.Output.WriteLine("  <type>(<scope>): <description>");
            console.Output.WriteLine("  [blank line]");
            console.Output.WriteLine("  [body]");
            console.Output.WriteLine("  [blank line]");
            console.Output.WriteLine("  [footer]");
            console.Output.WriteLine();
            console.Output.WriteLine($"Valid types: {string.Join(", ", rules.AllowedTypes)}");
            
            if (rules.AllowedScopes?.Count > 0)
            {
                console.Output.WriteLine($"Valid scopes: {string.Join(", ", rules.AllowedScopes)}");
            }
            else if (rules.RequireScope)
            {
                console.Output.WriteLine("Scope: required (any value)");
            }
            else
            {
                console.Output.WriteLine("Scope: optional");
            }
        }
        
        console.Output.WriteLine();
        console.Output.WriteLine("Rules:");
        console.Output.WriteLine($"  Subject line: {rules.MinLength}-{rules.MaxLength} characters");
        console.Output.WriteLine($"  Issue reference: {(rules.RequireIssueReference ? "required" : "optional")}");
        console.Output.WriteLine($"  Body: {(rules.RequireBody ? "required" : "optional")}");
        
        if (!string.IsNullOrEmpty(rules.Pattern))
        {
            console.Output.WriteLine($"  Pattern: {rules.Pattern}");
        }
        
        console.Output.WriteLine();
        console.Output.WriteLine("Examples:");
        console.Output.WriteLine("  feat(api): add user authentication");
        console.Output.WriteLine("  fix: handle missing config file");
        console.Output.WriteLine("  docs: update installation guide");
        console.Output.WriteLine("  chore(deps): bump dependencies");
        
        return ValueTask.CompletedTask;
    }
}
```

### Error Codes

```csharp
// Error codes for commit message validation
public static class MessageErrorCodes
{
    public const string EmptyMessage = "EMPTY_MESSAGE";
    public const string TooShort = "TOO_SHORT";
    public const string TooLong = "TOO_LONG";
    public const string PatternMismatch = "PATTERN_MISMATCH";
    public const string InvalidType = "INVALID_TYPE";
    public const string MissingScope = "MISSING_SCOPE";
    public const string InvalidScope = "INVALID_SCOPE";
    public const string MissingIssueReference = "MISSING_ISSUE_REFERENCE";
    public const string MissingBody = "MISSING_BODY";
    public const string InvalidConventionalFormat = "INVALID_CONVENTIONAL_FORMAT";
}
```

### Implementation Checklist

- [ ] Create `CommitMessageRules` record with defaults
- [ ] Create `ValidationResult` with factory methods
- [ ] Create `ValidationError` and `ValidationWarning` records
- [ ] Create `ConventionalCommit` and `CommitFooter` records
- [ ] Define `ICommitMessageValidator` interface
- [ ] Implement `ConventionalCommitParser.TryParse`
- [ ] Implement `ConventionalCommitParser.GetSubjectLine`
- [ ] Implement `ConventionalCommitParser.HasBody`
- [ ] Implement `CommitMessageValidator.Validate`
- [ ] Add length validation with clear messages
- [ ] Add pattern validation with regex timeout
- [ ] Add conventional commit parsing and validation
- [ ] Add scope validation against allowed list
- [ ] Add issue reference detection
- [ ] Add body requirement check
- [ ] Add trailing whitespace warning
- [ ] Create `ValidateMessageCommand` CLI
- [ ] Create `CommitFormatHelpCommand` CLI
- [ ] Add JSON output support
- [ ] Register validator in DI
- [ ] Write unit tests for parser
- [ ] Write unit tests for validator
- [ ] Test ReDoS prevention

### Rollout Plan

1. **Phase 1: Domain Models** (Day 1)
   - Create all records
   - Unit test defaults

2. **Phase 2: Parser** (Day 1)
   - Implement conventional commit parser
   - Test various formats

3. **Phase 3: Validator** (Day 2)
   - Implement validation rules
   - Add error messages
   - Test edge cases

4. **Phase 4: CLI** (Day 2)
   - Implement commands
   - Add file input support
   - Manual testing

5. **Phase 5: Polish** (Day 3)
   - Improve error messages
   - Add examples
   - Documentation

---

**End of Task 024.b Specification**