# Task 025.c: Human-Readable Errors

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 025 (Task Spec), Task 025.a (Schema), Task 025.b (CLI)  

---

## Description

Task 025.c implements human-readable error formatting for task spec operations. Errors MUST be clear, actionable, and helpful. Technical details MUST be available but not overwhelming.

Error messages MUST explain what went wrong, why it matters, and how to fix it. Validation errors MUST point to the exact location in the input. Parse errors MUST show context around the problem.

The error formatting MUST support multiple output modes. Terminal output MUST use colors and formatting. JSON output MUST be structured. Log output MUST be grep-friendly.

### Business Value

Clear errors enable:
- Faster problem resolution
- Reduced support burden
- Better user experience
- Self-service debugging
- Fewer repeat mistakes

### Scope Boundaries

This task covers error formatting only. Error generation is in Tasks 025 and 025.a. CLI integration is in Task 025.b.

### Integration Points

- Task 025: Parse errors
- Task 025.a: Validation errors
- Task 025.b: CLI display
- Task 009: Error framework

### Failure Modes

- Formatter crash → Fallback to raw error
- Missing context → Show partial info
- Color not supported → Plain text fallback

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Error Code | Machine-readable error identifier |
| Error Message | Human-readable description |
| Context | Surrounding content for location |
| Caret | ^ pointer to error position |
| Suggestion | Recommended fix action |
| Severity | Error, Warning, Info levels |
| Source | File/line/column of error |
| Snippet | Code/content extract |

---

## Out of Scope

- Error recovery/auto-fix
- Interactive error resolution
- Error statistics/analytics
- Error translation/i18n
- Error learning system

---

## Assumptions

### Technical Assumptions

1. **Terminal Capabilities**: Terminal supports ANSI escape codes for color output
2. **Fallback Rendering**: Plain text fallback available for non-ANSI terminals
3. **Source Access**: Source files can be read for context snippet extraction
4. **Screen Width**: Terminal width can be detected for proper formatting
5. **Unicode Support**: Terminal can render Unicode characters (arrows, checkmarks)
6. **Diagnostic Patterns**: Common error patterns from compilers (rustc, gcc) inform design

### Error Model Assumptions

7. **Error Codes**: All errors have unique, stable error codes (e.g., TASK-001)
8. **Severity Levels**: Three severity levels: Error, Warning, Info
9. **Context Lines**: Source context shows 2-3 lines around error location
10. **Caret Positioning**: Column-accurate caret (^) positioning for error location
11. **Suggestion System**: Errors can include actionable fix suggestions

### Integration Assumptions

12. **Validation Errors**: Schema validation errors are mapped to human-readable format
13. **Stack Trace Filtering**: Internal stack frames are filtered from user-facing output
14. **Log Correlation**: Errors include correlation IDs for log matching
15. **Accessibility**: Output is compatible with screen readers (no emoji-only indicators)

---

## Functional Requirements

### FR-001 to FR-025: Error Structure

- FR-001: Error MUST have code (e.g., TASK-001)
- FR-002: Error MUST have message
- FR-003: Error MUST have severity
- FR-004: Error MAY have source location
- FR-005: Error MAY have context snippet
- FR-006: Error MAY have suggestion
- FR-007: Error MAY have documentation URL
- FR-008: Error MAY have related errors
- FR-009: Multiple errors MUST be grouped
- FR-010: Errors MUST be sorted by location
- FR-011: Duplicate errors MUST be deduplicated
- FR-012: Error count MUST be shown
- FR-013: Warning count MUST be shown
- FR-014: Summary MUST appear at end
- FR-015: First error MUST be highlighted
- FR-016: Critical errors MUST appear first
- FR-017: Errors MUST include field path
- FR-018: Nested paths MUST use dot notation
- FR-019: Array indices MUST be bracketed
- FR-020: Root errors MUST show "(root)"
- FR-021: Expected value MUST be shown
- FR-022: Actual value MUST be shown
- FR-023: Value MUST be truncated if long
- FR-024: Truncation MUST show length
- FR-025: Secrets MUST be redacted

### FR-026 to FR-050: Display Formatting

- FR-026: Terminal MUST use ANSI colors
- FR-027: Error MUST be red
- FR-028: Warning MUST be yellow
- FR-029: Info MUST be blue
- FR-030: Path MUST be cyan
- FR-031: Code MUST be bold
- FR-032: NO_COLOR MUST disable colors
- FR-033: TERM=dumb MUST disable colors
- FR-034: Context snippet MUST show 3 lines
- FR-035: Error line MUST be highlighted
- FR-036: Line numbers MUST be shown
- FR-037: Caret MUST point to column
- FR-038: Caret line MUST be colored
- FR-039: Suggestion MUST be prefixed
- FR-040: URL MUST be clickable (OSC 8)
- FR-041: Width MUST respect terminal
- FR-042: Long lines MUST wrap
- FR-043: Indent MUST be consistent
- FR-044: Spacing MUST aid readability
- FR-045: Icons MAY be used (✗, ⚠, ℹ)
- FR-046: Icons MUST have text fallback
- FR-047: Output MUST be UTF-8
- FR-048: Box drawing MAY enhance
- FR-049: Plain mode MUST be available
- FR-050: Plain MUST be grep-friendly

### FR-051 to FR-070: JSON Format

- FR-051: JSON MUST be valid
- FR-052: JSON MUST have `errors` array
- FR-053: Each error MUST have `code`
- FR-054: Each error MUST have `message`
- FR-055: Each error MUST have `severity`
- FR-056: Each error MAY have `source`
- FR-057: Source MUST have `file`
- FR-058: Source MUST have `line`
- FR-059: Source MUST have `column`
- FR-060: Each error MAY have `context`
- FR-061: Context MUST have `snippet`
- FR-062: Context MUST have `highlightLine`
- FR-063: Each error MAY have `suggestion`
- FR-064: Each error MAY have `url`
- FR-065: JSON MUST have `summary`
- FR-066: Summary MUST have `errorCount`
- FR-067: Summary MUST have `warningCount`
- FR-068: Summary MUST have `valid` boolean
- FR-069: JSON MUST be compact by default
- FR-070: `--pretty` MUST format JSON

---

## Non-Functional Requirements

- NFR-001: Formatting MUST complete in <10ms
- NFR-002: Memory MUST be bounded
- NFR-003: Large files MUST stream
- NFR-004: Fallback MUST always work
- NFR-005: No crashes on malformed input
- NFR-006: Thread-safe formatting
- NFR-007: Testable output
- NFR-008: Consistent across platforms
- NFR-009: Accessible color choices
- NFR-010: Screen reader compatible

---

## User Manual Documentation

### Error Display Examples

**Validation Error:**
```
✗ TASK-002: Missing required field

  --> task.yaml:1:1
   |
 1 | priority: 2
 2 | files:
   |
   = missing field: title
   
  Suggestion: Add a 'title' field (1-200 characters)
  
  See: https://acode.dev/docs/errors/TASK-002
```

**Parse Error:**
```
✗ TASK-001: Invalid YAML syntax

  --> task.yaml:5:3
   |
 4 |   - src/file.cs
 5 |   - bad indent
   |   ^^^
 6 | tags:
   |
   = expected proper indentation
   
  Suggestion: Check indentation matches surrounding lines
```

**Multiple Errors:**
```
Found 3 errors and 1 warning in task.yaml

✗ TASK-002: Missing required field 'title' at (root)
✗ TASK-002: Missing required field 'description' at (root)
✗ TASK-004: Value out of range at 'priority' (got: 10, expected: 1-5)
⚠ TASK-010: Unknown field 'custom' will be stored in metadata

Summary: 3 errors, 1 warning - validation failed
```

### JSON Output

```json
{
  "errors": [
    {
      "code": "TASK-002",
      "message": "Missing required field",
      "severity": "error",
      "source": {
        "file": "task.yaml",
        "line": 1,
        "column": 1
      },
      "context": {
        "snippet": "priority: 2\nfiles:",
        "highlightLine": 1
      },
      "suggestion": "Add a 'title' field (1-200 characters)",
      "url": "https://acode.dev/docs/errors/TASK-002"
    }
  ],
  "summary": {
    "errorCount": 1,
    "warningCount": 0,
    "valid": false
  }
}
```

### Error Codes

| Code | Category | Description |
|------|----------|-------------|
| TASK-001 | Parse | YAML/JSON syntax error |
| TASK-002 | Validation | Missing required field |
| TASK-003 | Validation | Invalid field type |
| TASK-004 | Validation | Value out of range |
| TASK-005 | Validation | Invalid ULID format |
| TASK-006 | Validation | Circular dependency |
| TASK-007 | Validation | Invalid file path |
| TASK-008 | Limit | Size limit exceeded |
| TASK-009 | Limit | Nesting too deep |
| TASK-010 | Warning | Unknown field |

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Error code shown
- [ ] AC-002: Message is clear
- [ ] AC-003: Location shown
- [ ] AC-004: Context snippet shown
- [ ] AC-005: Suggestion provided
- [ ] AC-006: Colors work
- [ ] AC-007: NO_COLOR respected
- [ ] AC-008: JSON format correct
- [ ] AC-009: Summary shown
- [ ] AC-010: Multiple errors grouped
- [ ] AC-011: Secrets redacted
- [ ] AC-012: Long values truncated
- [ ] AC-013: Fallback works
- [ ] AC-014: Screen reader OK
- [ ] AC-015: Documentation URL works

---

## Best Practices

### Error Message Design

1. **Lead with What**: Start with what went wrong, not what triggered it
2. **Be Specific**: "Field 'priority' must be 1-5, got 7" not "Invalid value"
3. **Actionable Suggestions**: Always include what user can do to fix the issue
4. **Avoid Jargon**: Use user-facing terms, not internal class/method names

### Formatting Guidelines

5. **Consistent Color Scheme**: Error=red, warning=yellow, info=blue, success=green
6. **Source Context**: Show 2-3 lines around error with line numbers
7. **Caret Positioning**: Use ^ to point to exact error column when known
8. **Truncate Long Values**: Show first/last 50 chars of long strings with ellipsis

### Error Infrastructure

9. **Stable Error Codes**: Error codes (TASK-001) never change meaning once released
10. **Documentation Links**: Include URL to error documentation for complex issues
11. **Redact Secrets**: Never include passwords, tokens, or keys in error output
12. **Aggregate Related**: Group multiple related errors under single heading

---

## Troubleshooting

### Issue: Colors Not Displaying

**Symptoms:** Error output shows ANSI escape codes instead of colors

**Possible Causes:**
- Terminal doesn't support ANSI escape sequences
- NO_COLOR environment variable is set
- Output is being piped/redirected

**Solutions:**
1. Set TERM environment variable appropriately (xterm-256color)
2. Unset NO_COLOR if color output is desired
3. Use --color=always to force color even when piped

### Issue: Context Snippet Not Showing

**Symptoms:** Error messages don't include source code context

**Possible Causes:**
- Source file not accessible (deleted, moved, permissions)
- Error occurred before file was parsed (network error)
- Context extraction disabled for performance

**Solutions:**
1. Verify source file exists at reported path
2. Check file read permissions for acode process
3. Enable verbose mode (--verbose) for additional context

### Issue: Screen Reader Reads Formatting Characters

**Symptoms:** Accessibility tools read "dash dash dash" or escape sequences

**Possible Causes:**
- ANSI codes not stripped for accessibility output
- Unicode box-drawing characters confusing reader
- Missing aria labels or semantic structure

**Solutions:**
1. Set --no-color and --plain-text for accessibility mode
2. Configure ACODE_ACCESSIBLE=1 environment variable
3. File accessibility bug report with specific screen reader version

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Single error format
- [ ] UT-002: Multiple errors format
- [ ] UT-003: Context extraction
- [ ] UT-004: Color codes
- [ ] UT-005: JSON output
- [ ] UT-006: Plain text fallback

### Integration Tests

- [ ] IT-001: Full validation error flow
- [ ] IT-002: Parse error display
- [ ] IT-003: Terminal detection

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Application/
│   └── Services/
│       └── Errors/
│           ├── TaskError.cs            # Error model
│           ├── ErrorSeverity.cs        # Severity enum
│           ├── SourceLocation.cs       # Source location
│           ├── ErrorFormatOptions.cs   # Formatter options
│           ├── IErrorFormatter.cs      # Formatter interface
│           ├── TerminalErrorFormatter.cs
│           ├── JsonErrorFormatter.cs
│           ├── PlainTextErrorFormatter.cs
│           ├── ContextExtractor.cs     # Snippet extraction
│           └── ErrorCodes.cs           # Error code registry
│
└── Acode.Cli/
    └── Infrastructure/
        └── TerminalCapabilities.cs     # Color/width detection

tests/
└── Acode.Application.Tests/
    └── Services/
        └── Errors/
            ├── TerminalErrorFormatterTests.cs
            ├── JsonErrorFormatterTests.cs
            └── ContextExtractorTests.cs
```

### Domain Models

```csharp
// Acode.Application/Services/Errors/TaskError.cs
namespace Acode.Application.Services.Errors;

/// <summary>
/// Represents a validation or parse error.
/// </summary>
public sealed record TaskError
{
    /// <summary>Machine-readable error code (e.g., TASK-002).</summary>
    public required string Code { get; init; }
    
    /// <summary>Human-readable error message.</summary>
    public required string Message { get; init; }
    
    /// <summary>Error severity level.</summary>
    public required ErrorSeverity Severity { get; init; }
    
    /// <summary>Source file location (optional).</summary>
    public SourceLocation? Source { get; init; }
    
    /// <summary>Field path in dot notation (e.g., dependencies[0]).</summary>
    public string? Path { get; init; }
    
    /// <summary>Context snippet around the error.</summary>
    public ErrorContext? Context { get; init; }
    
    /// <summary>Suggested fix action.</summary>
    public string? Suggestion { get; init; }
    
    /// <summary>Documentation URL for this error.</summary>
    public string? DocumentationUrl { get; init; }
    
    /// <summary>Expected value or type.</summary>
    public string? Expected { get; init; }
    
    /// <summary>Actual value (truncated if necessary).</summary>
    public string? Actual { get; init; }
    
    /// <summary>Related errors (for grouping).</summary>
    public IReadOnlyList<TaskError> RelatedErrors { get; init; } = Array.Empty<TaskError>();
}

// Acode.Application/Services/Errors/ErrorSeverity.cs
namespace Acode.Application.Services.Errors;

public enum ErrorSeverity
{
    Error,
    Warning,
    Info
}

// Acode.Application/Services/Errors/SourceLocation.cs
namespace Acode.Application.Services.Errors;

public sealed record SourceLocation
{
    public required string File { get; init; }
    public required int Line { get; init; }
    public required int Column { get; init; }
    
    public override string ToString() => $"{File}:{Line}:{Column}";
}

// Acode.Application/Services/Errors/ErrorContext.cs
namespace Acode.Application.Services.Errors;

public sealed record ErrorContext
{
    /// <summary>Lines of source content.</summary>
    public required IReadOnlyList<ContextLine> Lines { get; init; }
    
    /// <summary>Line number of the error.</summary>
    public required int ErrorLineNumber { get; init; }
    
    /// <summary>Column number for caret position.</summary>
    public int? CaretColumn { get; init; }
    
    /// <summary>Length of error span.</summary>
    public int CaretLength { get; init; } = 1;
}

public sealed record ContextLine(int LineNumber, string Content, bool IsError);
```

### Error Format Options

```csharp
// Acode.Application/Services/Errors/ErrorFormatOptions.cs
namespace Acode.Application.Services.Errors;

public sealed record ErrorFormatOptions
{
    /// <summary>Output format.</summary>
    public OutputFormat Format { get; init; } = OutputFormat.Terminal;
    
    /// <summary>Whether to use ANSI colors.</summary>
    public bool UseColors { get; init; } = true;
    
    /// <summary>Number of context lines before/after error.</summary>
    public int ContextLines { get; init; } = 3;
    
    /// <summary>Maximum output width (0 for no limit).</summary>
    public int MaxWidth { get; init; } = 120;
    
    /// <summary>Whether to use Unicode icons.</summary>
    public bool UseIcons { get; init; } = true;
    
    /// <summary>Whether to show documentation URLs.</summary>
    public bool ShowUrls { get; init; } = true;
    
    /// <summary>Whether to pretty-print JSON.</summary>
    public bool PrettyJson { get; init; } = false;
    
    /// <summary>Maximum value length before truncation.</summary>
    public int MaxValueLength { get; init; } = 100;
    
    public static ErrorFormatOptions Default => new();
    
    public static ErrorFormatOptions FromEnvironment()
    {
        var noColor = Environment.GetEnvironmentVariable("NO_COLOR") != null;
        var termDumb = Environment.GetEnvironmentVariable("TERM") == "dumb";
        
        return new ErrorFormatOptions
        {
            UseColors = !noColor && !termDumb,
            UseIcons = !termDumb && Console.OutputEncoding.EncodingName.Contains("Unicode"),
            MaxWidth = Console.IsOutputRedirected ? 0 : Math.Max(80, Console.WindowWidth)
        };
    }
}

public enum OutputFormat
{
    Terminal,
    Plain,
    Json
}
```

### Error Formatter Interface

```csharp
// Acode.Application/Services/Errors/IErrorFormatter.cs
namespace Acode.Application.Services.Errors;

/// <summary>
/// Formats task errors for display.
/// </summary>
public interface IErrorFormatter
{
    /// <summary>
    /// Formats a list of errors.
    /// </summary>
    string Format(IReadOnlyList<TaskError> errors, ErrorFormatOptions? options = null);
    
    /// <summary>
    /// Formats a single error.
    /// </summary>
    string FormatSingle(TaskError error, ErrorFormatOptions? options = null);
    
    /// <summary>
    /// Formats a summary line.
    /// </summary>
    string FormatSummary(int errorCount, int warningCount, bool isValid);
}
```

### Terminal Error Formatter

```csharp
// Acode.Application/Services/Errors/TerminalErrorFormatter.cs
namespace Acode.Application.Services.Errors;

public sealed class TerminalErrorFormatter : IErrorFormatter
{
    public string Format(IReadOnlyList<TaskError> errors, ErrorFormatOptions? options = null)
    {
        options ??= ErrorFormatOptions.FromEnvironment();
        
        if (errors.Count == 0)
            return string.Empty;
        
        var sb = new StringBuilder();
        
        // Sort: errors first, then by location
        var sorted = errors
            .OrderByDescending(e => e.Severity == ErrorSeverity.Error)
            .ThenBy(e => e.Source?.Line ?? int.MaxValue)
            .ToList();
        
        // Deduplicate
        var unique = sorted
            .DistinctBy(e => (e.Code, e.Source?.Line, e.Source?.Column, e.Path))
            .ToList();
        
        // Header
        var firstFile = unique.FirstOrDefault(e => e.Source != null)?.Source?.File;
        var errorCount = unique.Count(e => e.Severity == ErrorSeverity.Error);
        var warningCount = unique.Count(e => e.Severity == ErrorSeverity.Warning);
        
        if (firstFile != null)
        {
            sb.AppendLine(FormatHeader(firstFile, errorCount, warningCount, options));
            sb.AppendLine();
        }
        
        // Format each error
        foreach (var error in unique)
        {
            sb.AppendLine(FormatSingle(error, options));
        }
        
        // Summary
        sb.AppendLine(FormatSummary(errorCount, warningCount, errorCount == 0));
        
        return sb.ToString();
    }
    
    public string FormatSingle(TaskError error, ErrorFormatOptions? options = null)
    {
        options ??= ErrorFormatOptions.FromEnvironment();
        
        var sb = new StringBuilder();
        var colors = options.UseColors ? ErrorColors.Instance : ErrorColors.NoColor;
        
        // Icon and code
        var icon = options.UseIcons ? GetIcon(error.Severity) : GetTextIcon(error.Severity);
        var severityColor = GetSeverityColor(error.Severity, colors);
        
        sb.Append(severityColor);
        sb.Append(icon);
        sb.Append(' ');
        sb.Append(colors.Bold);
        sb.Append(error.Code);
        sb.Append(colors.Reset);
        sb.Append(": ");
        sb.Append(error.Message);
        
        // Path
        if (error.Path != null)
        {
            sb.Append(" at ");
            sb.Append(colors.Cyan);
            sb.Append(error.Path);
            sb.Append(colors.Reset);
        }
        
        sb.AppendLine();
        
        // Source location
        if (error.Source != null)
        {
            sb.AppendLine();
            sb.Append("  ");
            sb.Append(colors.Cyan);
            sb.Append("--> ");
            sb.Append(error.Source);
            sb.Append(colors.Reset);
            sb.AppendLine();
        }
        
        // Context snippet
        if (error.Context != null)
        {
            sb.Append(FormatContext(error.Context, options, colors));
        }
        
        // Expected/Actual
        if (error.Expected != null || error.Actual != null)
        {
            sb.AppendLine();
            if (error.Expected != null)
            {
                sb.Append("   = expected: ");
                sb.Append(colors.Green);
                sb.Append(error.Expected);
                sb.Append(colors.Reset);
                sb.AppendLine();
            }
            if (error.Actual != null)
            {
                var truncated = TruncateValue(error.Actual, options.MaxValueLength);
                sb.Append("   = got: ");
                sb.Append(colors.Red);
                sb.Append(truncated);
                sb.Append(colors.Reset);
                sb.AppendLine();
            }
        }
        
        // Suggestion
        if (error.Suggestion != null)
        {
            sb.AppendLine();
            sb.Append("  ");
            sb.Append(colors.Blue);
            sb.Append("Suggestion: ");
            sb.Append(colors.Reset);
            sb.AppendLine(error.Suggestion);
        }
        
        // Documentation URL
        if (error.DocumentationUrl != null && options.ShowUrls)
        {
            sb.AppendLine();
            sb.Append("  See: ");
            if (options.UseColors)
            {
                // OSC 8 hyperlink
                sb.Append($"\x1b]8;;{error.DocumentationUrl}\x1b\\");
                sb.Append(colors.Cyan);
                sb.Append(error.DocumentationUrl);
                sb.Append(colors.Reset);
                sb.Append("\x1b]8;;\x1b\\");
            }
            else
            {
                sb.Append(error.DocumentationUrl);
            }
            sb.AppendLine();
        }
        
        return sb.ToString();
    }
    
    public string FormatSummary(int errorCount, int warningCount, bool isValid)
    {
        var options = ErrorFormatOptions.FromEnvironment();
        var colors = options.UseColors ? ErrorColors.Instance : ErrorColors.NoColor;
        
        var sb = new StringBuilder();
        
        if (isValid)
        {
            sb.Append(colors.Green);
            sb.Append(options.UseIcons ? "✓ " : "");
            sb.Append("Validation passed");
            sb.Append(colors.Reset);
        }
        else
        {
            sb.Append("Summary: ");
            sb.Append(colors.Red);
            sb.Append(errorCount);
            sb.Append(" error");
            if (errorCount != 1) sb.Append('s');
            sb.Append(colors.Reset);
            
            if (warningCount > 0)
            {
                sb.Append(", ");
                sb.Append(colors.Yellow);
                sb.Append(warningCount);
                sb.Append(" warning");
                if (warningCount != 1) sb.Append('s');
                sb.Append(colors.Reset);
            }
            
            sb.Append(" - ");
            sb.Append(colors.Red);
            sb.Append("validation failed");
            sb.Append(colors.Reset);
        }
        
        return sb.ToString();
    }
    
    private static string FormatHeader(string file, int errors, int warnings, ErrorFormatOptions options)
    {
        var colors = options.UseColors ? ErrorColors.Instance : ErrorColors.NoColor;
        return $"Found {colors.Red}{errors} error{(errors != 1 ? "s" : "")}{colors.Reset} and {colors.Yellow}{warnings} warning{(warnings != 1 ? "s" : "")}{colors.Reset} in {colors.Cyan}{file}{colors.Reset}";
    }
    
    private static string FormatContext(ErrorContext context, ErrorFormatOptions options, ErrorColors colors)
    {
        var sb = new StringBuilder();
        var gutterWidth = context.Lines.Max(l => l.LineNumber).ToString().Length;
        
        foreach (var line in context.Lines)
        {
            sb.Append("   ");
            if (line.IsError)
            {
                sb.Append(colors.Red);
            }
            else
            {
                sb.Append(colors.Gray);
            }
            sb.Append(line.LineNumber.ToString().PadLeft(gutterWidth));
            sb.Append(" | ");
            sb.Append(line.Content);
            sb.Append(colors.Reset);
            sb.AppendLine();
            
            // Caret line
            if (line.IsError && context.CaretColumn.HasValue)
            {
                sb.Append("   ");
                sb.Append(new string(' ', gutterWidth));
                sb.Append(" | ");
                sb.Append(new string(' ', context.CaretColumn.Value - 1));
                sb.Append(colors.Red);
                sb.Append(new string('^', Math.Max(1, context.CaretLength)));
                sb.Append(colors.Reset);
                sb.AppendLine();
            }
        }
        
        return sb.ToString();
    }
    
    private static string GetIcon(ErrorSeverity severity) => severity switch
    {
        ErrorSeverity.Error => "✗",
        ErrorSeverity.Warning => "⚠",
        ErrorSeverity.Info => "ℹ",
        _ => "•"
    };
    
    private static string GetTextIcon(ErrorSeverity severity) => severity switch
    {
        ErrorSeverity.Error => "ERROR",
        ErrorSeverity.Warning => "WARN",
        ErrorSeverity.Info => "INFO",
        _ => "NOTE"
    };
    
    private static string GetSeverityColor(ErrorSeverity severity, ErrorColors colors) => severity switch
    {
        ErrorSeverity.Error => colors.Red,
        ErrorSeverity.Warning => colors.Yellow,
        ErrorSeverity.Info => colors.Blue,
        _ => colors.Reset
    };
    
    private static string TruncateValue(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;
        
        return $"{value[..(maxLength - 20)]}... ({value.Length} chars)";
    }
}
```

### Color Scheme

```csharp
// Acode.Application/Services/Errors/ErrorColors.cs
namespace Acode.Application.Services.Errors;

public sealed class ErrorColors
{
    public string Red { get; init; } = "\x1b[31m";
    public string Green { get; init; } = "\x1b[32m";
    public string Yellow { get; init; } = "\x1b[33m";
    public string Blue { get; init; } = "\x1b[34m";
    public string Cyan { get; init; } = "\x1b[36m";
    public string Gray { get; init; } = "\x1b[90m";
    public string Bold { get; init; } = "\x1b[1m";
    public string Reset { get; init; } = "\x1b[0m";
    
    public static ErrorColors Instance { get; } = new();
    
    public static ErrorColors NoColor { get; } = new()
    {
        Red = "",
        Green = "",
        Yellow = "",
        Blue = "",
        Cyan = "",
        Gray = "",
        Bold = "",
        Reset = ""
    };
}
```

### JSON Error Formatter

```csharp
// Acode.Application/Services/Errors/JsonErrorFormatter.cs
namespace Acode.Application.Services.Errors;

public sealed class JsonErrorFormatter : IErrorFormatter
{
    public string Format(IReadOnlyList<TaskError> errors, ErrorFormatOptions? options = null)
    {
        options ??= ErrorFormatOptions.Default;
        
        var errorCount = errors.Count(e => e.Severity == ErrorSeverity.Error);
        var warningCount = errors.Count(e => e.Severity == ErrorSeverity.Warning);
        
        var output = new
        {
            errors = errors.Select(e => new
            {
                code = e.Code,
                message = e.Message,
                severity = e.Severity.ToString().ToLowerInvariant(),
                path = e.Path,
                source = e.Source == null ? null : new
                {
                    file = e.Source.File,
                    line = e.Source.Line,
                    column = e.Source.Column
                },
                context = e.Context == null ? null : new
                {
                    snippet = string.Join("\n", e.Context.Lines.Select(l => l.Content)),
                    highlightLine = e.Context.ErrorLineNumber
                },
                expected = e.Expected,
                actual = e.Actual,
                suggestion = e.Suggestion,
                url = e.DocumentationUrl
            }),
            summary = new
            {
                errorCount,
                warningCount,
                valid = errorCount == 0
            }
        };
        
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = options.PrettyJson,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        
        return JsonSerializer.Serialize(output, jsonOptions);
    }
    
    public string FormatSingle(TaskError error, ErrorFormatOptions? options = null)
    {
        return Format(new[] { error }, options);
    }
    
    public string FormatSummary(int errorCount, int warningCount, bool isValid)
    {
        return JsonSerializer.Serialize(new { errorCount, warningCount, valid = isValid });
    }
}
```

### Context Extractor

```csharp
// Acode.Application/Services/Errors/ContextExtractor.cs
namespace Acode.Application.Services.Errors;

public sealed class ContextExtractor
{
    /// <summary>
    /// Extracts context lines around an error location.
    /// </summary>
    public ErrorContext Extract(
        string content, 
        int errorLine, 
        int errorColumn, 
        int contextLines = 3)
    {
        var lines = content.Split('\n');
        
        if (errorLine < 1 || errorLine > lines.Length)
        {
            return new ErrorContext
            {
                Lines = Array.Empty<ContextLine>(),
                ErrorLineNumber = errorLine,
                CaretColumn = errorColumn
            };
        }
        
        var startLine = Math.Max(1, errorLine - contextLines);
        var endLine = Math.Min(lines.Length, errorLine + contextLines);
        
        var contextLinesList = new List<ContextLine>();
        
        for (var i = startLine; i <= endLine; i++)
        {
            var lineContent = lines[i - 1].TrimEnd('\r'); // Handle CRLF
            contextLinesList.Add(new ContextLine(i, lineContent, i == errorLine));
        }
        
        return new ErrorContext
        {
            Lines = contextLinesList,
            ErrorLineNumber = errorLine,
            CaretColumn = Math.Max(1, errorColumn),
            CaretLength = 1
        };
    }
    
    /// <summary>
    /// Extracts context with a span for the error.
    /// </summary>
    public ErrorContext ExtractWithSpan(
        string content,
        int errorLine,
        int startColumn,
        int endColumn,
        int contextLines = 3)
    {
        var context = Extract(content, errorLine, startColumn, contextLines);
        
        return context with
        {
            CaretLength = Math.Max(1, endColumn - startColumn + 1)
        };
    }
}
```

### Error Codes Registry

```csharp
// Acode.Application/Services/Errors/ErrorCodes.cs
namespace Acode.Application.Services.Errors;

public static class ErrorCodes
{
    public const string ParseError = "TASK-001";
    public const string MissingRequired = "TASK-002";
    public const string InvalidType = "TASK-003";
    public const string OutOfRange = "TASK-004";
    public const string InvalidFormat = "TASK-005";
    public const string CircularDependency = "TASK-006";
    public const string InvalidPath = "TASK-007";
    public const string SizeExceeded = "TASK-008";
    public const string NestingTooDeep = "TASK-009";
    public const string UnknownField = "TASK-010";
    
    private static readonly Dictionary<string, ErrorCodeInfo> _registry = new()
    {
        [ParseError] = new("Parse", "YAML/JSON syntax error", "https://acode.dev/docs/errors/TASK-001"),
        [MissingRequired] = new("Validation", "Missing required field", "https://acode.dev/docs/errors/TASK-002"),
        [InvalidType] = new("Validation", "Invalid field type", "https://acode.dev/docs/errors/TASK-003"),
        [OutOfRange] = new("Validation", "Value out of range", "https://acode.dev/docs/errors/TASK-004"),
        [InvalidFormat] = new("Validation", "Invalid ULID/path format", "https://acode.dev/docs/errors/TASK-005"),
        [CircularDependency] = new("Validation", "Circular dependency detected", "https://acode.dev/docs/errors/TASK-006"),
        [InvalidPath] = new("Validation", "Invalid file path", "https://acode.dev/docs/errors/TASK-007"),
        [SizeExceeded] = new("Limit", "Size limit exceeded", "https://acode.dev/docs/errors/TASK-008"),
        [NestingTooDeep] = new("Limit", "Nesting too deep", "https://acode.dev/docs/errors/TASK-009"),
        [UnknownField] = new("Warning", "Unknown field", "https://acode.dev/docs/errors/TASK-010")
    };
    
    public static ErrorCodeInfo? GetInfo(string code) =>
        _registry.TryGetValue(code, out var info) ? info : null;
    
    public static string? GetDocUrl(string code) =>
        GetInfo(code)?.DocumentationUrl;
}

public sealed record ErrorCodeInfo(
    string Category,
    string Description,
    string DocumentationUrl);
```

### Implementation Checklist

- [ ] Create `TaskError` and related models
- [ ] Create `ErrorFormatOptions` with environment detection
- [ ] Define `IErrorFormatter` interface
- [ ] Implement `TerminalErrorFormatter` with colors
- [ ] Implement caret positioning logic
- [ ] Implement OSC 8 hyperlinks
- [ ] Implement `JsonErrorFormatter`
- [ ] Implement `PlainTextErrorFormatter`
- [ ] Create `ContextExtractor` for snippets
- [ ] Create `ErrorCodes` registry
- [ ] Add error deduplication
- [ ] Add error sorting (severity, location)
- [ ] Handle NO_COLOR environment variable
- [ ] Handle TERM=dumb
- [ ] Add value truncation with length display
- [ ] Register formatters in DI
- [ ] Write unit tests for formatters
- [ ] Write unit tests for context extraction
- [ ] Test color output manually

### Rollout Plan

1. **Phase 1: Models** (Day 1)
   - TaskError and related types
   - ErrorFormatOptions
   - ErrorCodes registry

2. **Phase 2: Terminal Formatter** (Day 2)
   - Color support
   - Context snippets
   - Caret positioning

3. **Phase 3: JSON Formatter** (Day 2)
   - Structured output
   - Pretty printing

4. **Phase 4: Context Extractor** (Day 3)
   - Line extraction
   - Span calculation

5. **Phase 5: Integration** (Day 3)
   - DI registration
   - CLI integration
   - Manual testing

---

**End of Task 025.c Specification**