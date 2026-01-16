# Task-013c Completion Checklist: Yes Scoping Rules (--yes Flag Implementation)

**Date Created:** 2026-01-16

**Total Estimated Effort:** 20-28 hours to 100% completion (8 phases)

**Status:** READY FOR IMPLEMENTATION

---

## HOW TO USE THIS CHECKLIST

This checklist guides implementation of task-013c from 0% to 100% completion. Each gap includes:
- **Current State**: What exists (❌ MISSING, ⚠️ INCOMPLETE, or ✅ COMPLETE)
- **Spec Reference**: Where in the 4,196-line spec to find details
- **What Exists**: Currently implemented (if anything)
- **What's Missing**: Exactly what needs to be created
- **Implementation Details**: Code examples and patterns from spec
- **Acceptance Criteria Covered**: Which ACs this gap addresses
- **Test Requirements**: Test cases to write
- **Success Criteria**: Verification checklist
- **Gap Checklist Item**: Mark [✅] when complete

**Important**: Follow TDD strictly:
1. **RED**: Write test first (will fail)
2. **GREEN**: Write minimal implementation (test passes)
3. **REFACTOR**: Clean up code while keeping tests green
4. **COMMIT**: One commit per gap

---

# PHASE 1: DOMAIN MODEL (OperationCategory, RiskLevel, ScopeEntry, YesScope, Operation)

**Hours: 3-4**

**Objective**: Create immutable domain types representing scopes, operations, and risk levels

**Acceptance Criteria Covered**: AC-030-036, AC-044-052 (foundation for all others)

## Gap 1.1: OperationCategory Enum

- **Current State**: ❌ MISSING
- **Spec Reference**: Lines 3289-3328 (Implementation Prompt)
- **What Exists**: Nothing
- **What's Missing**: OperationCategory enum with 9 values (FileRead, FileWrite, FileDelete, DirCreate, DirDelete, DirList, Terminal, Config, Search)
- **Implementation Details (from spec)**:
```csharp
// src/Acode.Domain/Approvals/OperationCategory.cs
namespace Acode.Domain.Approvals;

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
- **Acceptance Criteria Covered**: AC-012-021 (category coverage)
- **Test Requirements**:
  - [ ] Test that all 9 enum values exist
  - [ ] Test enum values are distinct
- **Success Criteria**:
  - [ ] File created at correct path
  - [ ] All 9 values present
  - [ ] XML documentation present
  - [ ] Compiles without errors
- **Gap Checklist Item**: [ ] 🔄 OperationCategory enum created with all 9 values

## Gap 1.2: RiskLevel Enum

- **Current State**: ❌ MISSING
- **Spec Reference**: Lines 3331-3365
- **What Exists**: Nothing
- **What's Missing**: RiskLevel enum with 4 values (Low=1, Medium=2, High=3, Critical=4) with numeric values for comparison
- **Implementation Details (from spec)**:
```csharp
// src/Acode.Domain/Approvals/RiskLevel.cs
namespace Acode.Domain.Approvals;

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
- **Acceptance Criteria Covered**: AC-030-033 (risk level classification), AC-044-052 (protected operations)
- **Test Requirements**:
  - [ ] Test all 4 enum values exist
  - [ ] Test numeric values are correct (Low=1, Medium=2, High=3, Critical=4)
  - [ ] Test values can be compared (Critical > High > Medium > Low)
- **Success Criteria**:
  - [ ] File created at correct path
  - [ ] All 4 values with correct numeric assignments
  - [ ] XML documentation on each value
  - [ ] Compiles without errors
- **Gap Checklist Item**: [ ] 🔄 RiskLevel enum created with correct numeric values

## Gap 1.3: ScopeEntry Record

- **Current State**: ❌ MISSING
- **Spec Reference**: Lines 3368-3439
- **What Exists**: Nothing
- **What's Missing**: ScopeEntry record with Category, Modifier, Pattern properties and Covers() method implementing glob matching
- **Implementation Details (from spec)**:
```csharp
// src/Acode.Domain/Approvals/ScopeEntry.cs
namespace Acode.Domain.Approvals;

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
- **Acceptance Criteria Covered**: AC-022-029 (scope modifiers, glob patterns), AC-006 (combined modifiers)
- **Test Requirements**:
  - [ ] Test Covers() returns true when category matches and no modifier/pattern
  - [ ] Test Covers() returns false when category doesn't match
  - [ ] Test :safe modifier requires operation.IsSafe
  - [ ] Test :test modifier matches /test/, /tests/, .test.*, .spec.*
  - [ ] Test glob pattern matching: *, **, ?, [abc]
  - [ ] Test ToString() generates correct format
- **Success Criteria**:
  - [ ] File created at correct path
  - [ ] All properties present and immutable
  - [ ] Covers() method works for all modifier types
  - [ ] Glob matching works with *, **, ?, [abc]
  - [ ] ToString() returns category:modifier:pattern format
  - [ ] No NotImplementedException
- **Gap Checklist Item**: [ ] 🔄 ScopeEntry record created with Covers() and glob matching

## Gap 1.4: YesScope Value Object

- **Current State**: ❌ MISSING
- **Spec Reference**: Lines 3442-3526
- **What Exists**: Nothing
- **What's Missing**: YesScope immutable value object with Default, All, None static properties and Covers()/Combine() methods
- **Implementation Details (from spec)**:
```csharp
// src/Acode.Domain/Approvals/YesScope.cs
namespace Acode.Domain.Approvals;

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
- **Acceptance Criteria Covered**: AC-001 (default scope), AC-020 (all scope), AC-030 (Level 1 auto-approve), AC-039 (scope combination)
- **Test Requirements**:
  - [ ] Test YesScope.Default covers file_read, dir_list, dir_create, search
  - [ ] Test YesScope.All covers everything except Critical
  - [ ] Test YesScope.None covers nothing
  - [ ] Test Covers() returns true for covered operations
  - [ ] Test Covers() returns false for uncovered operations
  - [ ] Test Combine() merges entries correctly
  - [ ] Test ToString() generates correct format
- **Success Criteria**:
  - [ ] File created at correct path
  - [ ] Default, All, None static properties work correctly
  - [ ] Covers() implements correct logic
  - [ ] Combine() returns new scope with merged entries
  - [ ] All scope blocks Critical operations
  - [ ] No NotImplementedException
- **Gap Checklist Item**: [ ] 🔄 YesScope value object created with Default/All/None and Covers()

## Gap 1.5: Operation Record

- **Current State**: ❌ MISSING
- **Spec Reference**: Lines 3529-3595
- **What Exists**: Nothing
- **What's Missing**: Operation record with Category, Target, RiskLevel, IsSafe, Description; includes GetDefaultRiskLevel() and IsCriticalPath() logic
- **Implementation Details (from spec)**:
```csharp
// src/Acode.Domain/Approvals/Operation.cs
namespace Acode.Domain.Approvals;

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
- **Acceptance Criteria Covered**: AC-030-036 (risk level assignment), AC-044-052 (critical paths)
- **Test Requirements**:
  - [ ] Test FileRead/DirCreate/DirList/Search are Low risk
  - [ ] Test FileWrite is Medium risk
  - [ ] Test FileDelete/DirDelete/Config/Terminal are High risk
  - [ ] Test .git paths are Critical risk
  - [ ] Test .env paths are Critical risk
  - [ ] Test .agent, .acode, credentials, secret, .pem, .key are Critical
  - [ ] Test custom riskLevel parameter overrides default
  - [ ] Test IsSafe property can be set
- **Success Criteria**:
  - [ ] File created at correct path
  - [ ] All properties present and immutable
  - [ ] GetDefaultRiskLevel() covers all categories and critical paths
  - [ ] IsCriticalPath() detects all protected patterns
  - [ ] Risk level can be explicitly provided
  - [ ] No NotImplementedException
- **Gap Checklist Item**: [ ] 🔄 Operation record created with risk level logic

---

# PHASE 2: UNIT TESTS FOR DOMAIN MODEL

**Hours: 1-2**

**Objective**: Write comprehensive tests for domain types

## Gap 2.1: Create ScopeEntry Tests

- **Current State**: ❌ MISSING
- **File**: tests/Acode.Application.Tests/Approvals/Scoping/ScopeEntryTests.cs
- **What's Missing**: 8 test methods
- **Test Methods**:
```csharp
[Fact]
public void Covers_CategoryMustMatch()
{
    // Arrange
    var entry = new ScopeEntry(OperationCategory.FileRead);
    var operation = new Operation(OperationCategory.FileWrite, "test.txt");

    // Act
    var result = entry.Covers(operation);

    // Assert
    result.Should().BeFalse();
}

[Fact]
public void Covers_SafeModifierRequiresIsSafe()
{
    // Arrange
    var entry = new ScopeEntry(OperationCategory.Terminal, "safe");
    var safeOp = new Operation(OperationCategory.Terminal, "git status", isSafe: true);
    var unsafeOp = new Operation(OperationCategory.Terminal, "rm -rf /", isSafe: false);

    // Act & Assert
    entry.Covers(safeOp).Should().BeTrue();
    entry.Covers(unsafeOp).Should().BeFalse();
}

[Theory]
[InlineData("src/test/file.ts")]
[InlineData("tests/unit/test.ts")]
[InlineData("file.test.ts")]
[InlineData("file.spec.ts")]
public void Covers_TestModifierMatchesTestPaths(string path)
{
    // Arrange
    var entry = new ScopeEntry(OperationCategory.FileWrite, "test");
    var operation = new Operation(OperationCategory.FileWrite, path);

    // Act & Assert
    entry.Covers(operation).Should().BeTrue();
}

[Theory]
[InlineData("*.txt", "file.txt", true)]
[InlineData("*.txt", "file.ts", false)]
[InlineData("src/**/*.ts", "src/nested/file.ts", true)]
[InlineData("src/**/*.ts", "other/file.ts", false)]
public void Covers_GlobPatternMatching(string pattern, string path, bool expected)
{
    // Arrange
    var entry = new ScopeEntry(OperationCategory.FileRead, pattern: pattern);
    var operation = new Operation(OperationCategory.FileRead, path);

    // Act & Assert
    entry.Covers(operation).Should().Be(expected);
}
```
- **Success Criteria**:
  - [ ] All 8 test methods exist
  - [ ] All tests pass
  - [ ] Coverage > 95% for ScopeEntry
- **Gap Checklist Item**: [ ] 🔄 ScopeEntry tests created and passing

## Gap 2.2: Create Operation Tests

- **Current State**: ❌ MISSING
- **File**: tests/Acode.Application.Tests/Approvals/Scoping/OperationTests.cs
- **What's Missing**: 12 test methods
- **Test Methods** (sample):
```csharp
[Theory]
[InlineData(OperationCategory.FileRead, RiskLevel.Low)]
[InlineData(OperationCategory.FileWrite, RiskLevel.Medium)]
[InlineData(OperationCategory.FileDelete, RiskLevel.High)]
public void GetDefaultRiskLevel_ReturnsCorrectLevel(OperationCategory category, RiskLevel expected)
{
    // Arrange & Act
    var operation = new Operation(category, "test.txt");

    // Assert
    operation.RiskLevel.Should().Be(expected);
}

[Theory]
[InlineData(".git/config")]
[InlineData(".env")]
[InlineData(".agent/config.yml")]
public void IsCriticalPath_IdentifiesCriticalPaths(string path)
{
    // Arrange & Act
    var operation = new Operation(OperationCategory.FileDelete, path);

    // Assert
    operation.RiskLevel.Should().Be(RiskLevel.Critical);
}
```
- **Success Criteria**:
  - [ ] All 12 test methods exist
  - [ ] All tests pass
  - [ ] Coverage > 95% for Operation
- **Gap Checklist Item**: [ ] 🔄 Operation tests created and passing

---

# PHASE 3: SCOPE PARSING (IScopeParser, ScopeParser, ScopeInjectionGuard)

**Hours: 4-5**

**Objective**: Parse --yes scope strings and prevent injection attacks

**Acceptance Criteria Covered**: AC-001-011, AC-076-082, AC-099

## Gap 3.1: Create IScopeParser Interface

- **Current State**: ❌ MISSING
- **File**: src/Acode.Application/Approvals/Scoping/IScopeParser.cs
- **What's Missing**: Interface with Parse() method
- **Implementation Details (from spec)**:
```csharp
namespace Acode.Application.Approvals.Scoping;

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
```
- **Success Criteria**:
  - [ ] Interface created with correct method signature
  - [ ] XML documentation present
  - [ ] Returns Result<YesScope> for error handling
- **Gap Checklist Item**: [ ] 🔄 IScopeParser interface created

## Gap 3.2: Create ScopeInjectionGuard

- **Current State**: ❌ MISSING
- **File**: src/Acode.Infrastructure/Approvals/Scoping/ScopeInjectionGuard.cs
- **What's Missing**: Security class that validates input for shell metacharacters
- **Implementation Details (from spec concept)**:
```csharp
namespace Acode.Infrastructure.Approvals.Scoping;

/// <summary>
/// Guards against scope injection attacks via shell metacharacters.
/// </summary>
public class ScopeInjectionGuard
{
    private readonly ILogger<ScopeInjectionGuard> _logger;
    private static readonly char[] DangerousChars = { ';', '|', '&', '`', ' ', '\t', '\n' };

    public ScopeInjectionGuard(ILogger<ScopeInjectionGuard> logger)
    {
        _logger = logger;
    }

    public InjectionCheckResult ValidateForInjection(string input)
    {
        // Check for dangerous characters
        var dangerousCharFound = DangerousChars.FirstOrDefault(c => input.Contains(c));
        if (dangerousCharFound != '\0')
        {
            var charName = dangerousCharFound == ' ' ? "space" : dangerousCharFound.ToString();
            return InjectionCheckResult.Invalid($"Dangerous character '{charName}' detected in scope");
        }

        // Check for embedded flags (-- pattern)
        if (input.Contains("--", StringComparison.OrdinalIgnoreCase))
        {
            return InjectionCheckResult.Invalid("Embedded flags not allowed in scope");
        }

        // Check if requires ack
        var requiresAck = input.Equals("all", StringComparison.OrdinalIgnoreCase);

        _logger.LogDebug("Scope injection check passed: {Input}", input);
        return InjectionCheckResult.Valid(requiresAck);
    }
}

public record InjectionCheckResult
{
    public bool IsValid { get; }
    public string? Message { get; }
    public bool RequiresAck { get; }

    private InjectionCheckResult(bool isValid, string? message = null, bool requiresAck = false)
    {
        IsValid = isValid;
        Message = message;
        RequiresAck = requiresAck;
    }

    public static InjectionCheckResult Valid(bool requiresAck = false)
        => new(true, null, requiresAck);

    public static InjectionCheckResult Invalid(string message)
        => new(false, message);
}
```
- **Acceptance Criteria Covered**: AC-008-009, AC-099 (injection prevention)
- **Test Requirements**:
  - [ ] Test rejection of semicolon
  - [ ] Test rejection of pipe
  - [ ] Test rejection of ampersand
  - [ ] Test rejection of backtick
  - [ ] Test rejection of space (embedded flag)
  - [ ] Test rejection of -- pattern
  - [ ] Test valid input passes
  - [ ] Test --yes=all sets RequiresAck
- **Success Criteria**:
  - [ ] All dangerous characters blocked
  - [ ] Embedded flags detected
  - [ ] Valid scopes pass through
  - [ ] No NotImplementedException
- **Gap Checklist Item**: [ ] 🔄 ScopeInjectionGuard created with security checks

## Gap 3.3: Implement ScopeParser

- **Current State**: ❌ MISSING
- **File**: src/Acode.Infrastructure/Approvals/Scoping/ScopeParser.cs
- **What's Missing**: Full implementation from spec (350+ lines) with Levenshtein distance for suggestions
- **Key Features**:
  - Parse comma-separated entries
  - Parse category:modifier:pattern format
  - Inject guard validation
  - Levenshtein distance for "Did you mean?" suggestions
  - Special handling for "all", "none", "default"
  - Maximum 20 entries enforcement
- **Acceptance Criteria Covered**: AC-001-011, AC-076-082
- **Test Requirements**:
  - [ ] Parse single scope ("file_read")
  - [ ] Parse multiple comma-separated scopes
  - [ ] Parse scope with modifier ("terminal:safe")
  - [ ] Parse scope with glob pattern ("file_write:*.test.ts")
  - [ ] Reject invalid category with suggestion
  - [ ] Reject shell metacharacters
  - [ ] Reject embedded flags
  - [ ] Reject entry count > 20
  - [ ] Default scope for empty input
  - [ ] "all" requires --ack-danger
  - [ ] Levenshtein suggestions work
- **Success Criteria**:
  - [ ] Parse() method returns Result<YesScope>
  - [ ] All parsing tests pass
  - [ ] All error messages clear
  - [ ] Suggestions helpful (distance <= 3)
  - [ ] Coverage > 90%
  - [ ] No NotImplementedException
- **Gap Checklist Item**: [ ] 🔄 ScopeParser fully implemented with validation and suggestions

---

# PHASE 4: RISK CLASSIFICATION & SECURITY PROTECTIONS (RiskLevel, Critical Operations, Pattern Validator, TerminalClassifier)

**Hours: 4-5**

**Objective**: Classify risks and protect dangerous operations

**Acceptance Criteria Covered**: AC-030-036, AC-044-052, AC-099-103

## Gap 4.1: Risk Level Classifier Logic

- **Current State**: ❌ MISSING (in RiskLevelClassifier tests - tests reference classifier that doesn't exist yet)
- **What's Missing**: RiskLevelClassifier class that determines operation risk from category and path
- **Key Logic**:
```csharp
public class RiskLevelClassifier
{
    public RiskLevel GetRiskLevel(OperationCategory category, string target)
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

    private bool IsCriticalPath(string path) { ... }
}
```
- **Acceptance Criteria Covered**: AC-030-036
- **Test Requirements** (from Testing Requirements section):
  - [ ] Level 1 (Low): FileRead, DirCreate, DirList, Search
  - [ ] Level 2 (Medium): FileWrite
  - [ ] Level 3 (High): FileDelete, DirDelete, Config, Terminal
  - [ ] Level 4 (Critical): .git paths, .env, .agent, .acode, credentials, secret
  - [ ] Cannot downgrade critical via config
- **Success Criteria**:
  - [ ] All categories correctly classified
  - [ ] All critical paths detected
  - [ ] Config overrides work but can't downgrade Critical
  - [ ] Coverage > 95%
- **Gap Checklist Item**: [ ] 🔄 RiskLevelClassifier created with category mapping

## Gap 4.2: HardcodedCriticalOperations

- **Current State**: ❌ MISSING
- **File**: src/Acode.Infrastructure/Approvals/Scoping/HardcodedCriticalOperations.cs
- **What's Missing**: Class that defines protected paths and terminal commands
- **Implementation Details**:
```csharp
public class HardcodedCriticalOperations
{
    private readonly ILogger<HardcodedCriticalOperations> _logger;

    private static readonly string[] CriticalPaths =
    {
        ".git", ".git/", ".git\\",
        ".env", ".env.",
        ".agent", ".agent/", ".agent\\",
        ".acode", ".acode/", ".acode\\",
        "credentials", "secret", ".pem", ".key"
    };

    private static readonly string[] CriticalTerminalPatterns =
    {
        "git push --force",
        "git push -f",
        "git reset --hard",
        "rm -rf /",
        "rm -rf ~",
        "sudo rm"
    };

    public HardcodedCriticalOperations(ILogger<HardcodedCriticalOperations> logger)
    {
        _logger = logger;
    }

    public bool IsCriticalOperation(OperationCategory category, string target)
    {
        if (category == OperationCategory.Terminal)
            return IsCriticalTerminalCommand(target);

        if (category == OperationCategory.FileDelete || category == OperationCategory.DirDelete)
            return IsCriticalPath(target);

        return false;
    }

    private bool IsCriticalPath(string path) { ... }

    private bool IsCriticalTerminalCommand(string command) { ... }

    public ValidationResult ValidateRiskLevelConfiguration(IEnumerable<RiskLevelOverride> overrides)
    {
        // Prevent downgrading critical operations
        var violations = overrides
            .Where(o => IsCriticalOperation(o.Category, o.Pattern) && o.NewLevel != RiskLevel.Critical)
            .Select(o => $"Cannot downgrade critical operation {o.Pattern} to {o.NewLevel}")
            .ToList();

        return new ValidationResult(!violations.Any(), violations);
    }
}
```
- **Acceptance Criteria Covered**: AC-044-052 (protected operations)
- **Test Requirements**:
  - [ ] Test all .git paths are protected
  - [ ] Test all .env variants protected
  - [ ] Test .agent and .acode protected
  - [ ] Test git push --force is critical
  - [ ] Test git push -f is critical
  - [ ] Test git reset --hard is critical
  - [ ] Test rm -rf / is critical
  - [ ] Test rm -rf ~ is critical
  - [ ] Test configuration cannot downgrade critical
- **Success Criteria**:
  - [ ] All hardcoded critical paths identified
  - [ ] All dangerous terminal commands blocked
  - [ ] Cannot be downgraded via config
  - [ ] Coverage > 95%
- **Gap Checklist Item**: [ ] 🔄 HardcodedCriticalOperations created with all protections

## Gap 4.3: ScopePatternComplexityValidator

- **Current State**: ❌ MISSING
- **File**: src/Acode.Infrastructure/Approvals/Scoping/ScopePatternComplexityValidator.cs
- **What's Missing**: DoS prevention validator that rejects complex glob patterns
- **Implementation Details**:
```csharp
public class ScopePatternComplexityValidator
{
    private readonly ILogger<ScopePatternComplexityValidator> _logger;
    private const int MaxRecursiveGlobs = 3;
    private const int MaxPatternLength = 100;

    public ScopePatternComplexityValidator(ILogger<ScopePatternComplexityValidator> logger)
    {
        _logger = logger;
    }

    public PatternValidationResult ValidatePattern(string pattern)
    {
        // Check length
        if (pattern.Length > MaxPatternLength)
            return PatternValidationResult.Invalid($"Pattern length exceeds {MaxPatternLength} characters");

        // Count recursive globs (**)
        var recursiveGlobCount = (pattern.Length - pattern.Replace("**", "").Length) / 2;
        if (recursiveGlobCount > MaxRecursiveGlobs)
            return PatternValidationResult.Invalid(
                $"Pattern contains too many recursive globs (max {MaxRecursiveGlobs}, found {recursiveGlobCount})");

        _logger.LogDebug("Pattern complexity validated: {Pattern}", pattern);
        return PatternValidationResult.Valid();
    }
}

public record PatternValidationResult
{
    public bool IsValid { get; }
    public string? Message { get; }

    private PatternValidationResult(bool isValid, string? message = null)
    {
        IsValid = isValid;
        Message = message;
    }

    public static PatternValidationResult Valid() => new(true);
    public static PatternValidationResult Invalid(string message) => new(false, message);
}
```
- **Acceptance Criteria Covered**: AC-097, AC-101 (DoS prevention)
- **Test Requirements**:
  - [ ] Test rejection of > 3 recursive globs
  - [ ] Test rejection of > 100 character patterns
  - [ ] Test valid patterns pass
  - [ ] Test specific error messages
- **Success Criteria**:
  - [ ] Prevents exponential backtracking attacks
  - [ ] Validates all patterns
  - [ ] Clear error messages
- **Gap Checklist Item**: [ ] 🔄 ScopePatternComplexityValidator created with DoS checks

## Gap 4.4: TerminalOperationClassifier

- **Current State**: ❌ MISSING
- **File**: src/Acode.Infrastructure/Approvals/Scoping/TerminalOperationClassifier.cs
- **What's Missing**: Classifier that determines if terminal commands are safe, dangerous, or unknown
- **Implementation Details**:
```csharp
public class TerminalOperationClassifier
{
    private readonly ILogger<TerminalOperationClassifier> _logger;
    private readonly HardcodedCriticalOperations _criticalOps;

    private static readonly string[] SafeCommands =
    {
        "git status", "git log", "git diff", "git branch",
        "ls", "pwd", "cat", "head", "tail", "grep"
    };

    public TerminalOperationClassifier(
        ILogger<TerminalOperationClassifier> logger,
        HardcodedCriticalOperations criticalOps = null)
    {
        _logger = logger;
        _criticalOps = criticalOps ?? new HardcodedCriticalOperations(logger);
    }

    public TerminalClassificationResult Classify(string command)
    {
        // Check deny list (critical operations) FIRST
        if (_criticalOps.IsCriticalTerminalCommand(command))
        {
            return TerminalClassificationResult.Dangerous(
                RiskLevel.Critical,
                $"Command contains dangerous operation");
        }

        // Check allow list (safe commands)
        if (IsSafeCommand(command))
        {
            return TerminalClassificationResult.Safe(RiskLevel.Low);
        }

        // Unknown command
        return TerminalClassificationResult.Unknown(RiskLevel.High);
    }

    private bool IsSafeCommand(string command) { ... }
}

public record TerminalClassificationResult
{
    public bool IsSafe { get; }
    public bool IsDangerous { get; }
    public RiskLevel RiskLevel { get; }
    public string? Reason { get; }

    // Factory methods...
}
```
- **Acceptance Criteria Covered**: AC-018, AC-102 (deny list before allow list)
- **Test Requirements**:
  - [ ] Test safe commands return Low risk
  - [ ] Test dangerous commands return Critical
  - [ ] Test unknown commands return High
  - [ ] Test deny list checked before allow list
  - [ ] Test git push --force is caught
  - [ ] Test rm -rf is caught
- **Success Criteria**:
  - [ ] All safe commands identified
  - [ ] All dangerous commands blocked
  - [ ] Deny list checked first (security critical)
  - [ ] Coverage > 90%
- **Gap Checklist Item**: [ ] 🔄 TerminalOperationClassifier created with allow/deny lists

---

# PHASE 5: SCOPE RESOLUTION (IScopeResolver, ScopeResolver)

**Hours: 3-4**

**Objective**: Implement precedence rules and scope resolution

**Acceptance Criteria Covered**: AC-037-043 (precedence)

## Gap 5.1: Create IScopeResolver Interface

- **Current State**: ❌ MISSING
- **File**: src/Acode.Application/Approvals/Scoping/IScopeResolver.cs
- **What's Missing**: Interface with CanBypass(), IsProtected(), GetEffectiveScope() methods
- **Implementation Details (from spec)**:
```csharp
namespace Acode.Application.Approvals.Scoping;

/// <summary>
/// Resolves effective scope from CLI, config, and defaults with precedence rules.
/// </summary>
public interface IScopeResolver
{
    /// <summary>
    /// Determines if an operation can be bypassed under the given scope.
    /// Precedence: --no > protected > deny > scope > config > default
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

public record DenyRule(OperationCategory Category, string Pattern);
```
- **Success Criteria**:
  - [ ] Interface created with correct method signatures
  - [ ] Parameters for all precedence levels
  - [ ] XML documentation present
- **Gap Checklist Item**: [ ] 🔄 IScopeResolver interface created

## Gap 5.2: Implement ScopeResolver with Precedence

- **Current State**: ❌ MISSING
- **File**: src/Acode.Infrastructure/Approvals/Scoping/ScopeResolver.cs
- **What's Missing**: Full implementation with precedence logic (200+ lines)
- **Precedence Chain** (from spec):
  1. --no flag blocks ALL
  2. --interactive forces prompts
  3. Protected operations never bypass
  4. Critical risk level never bypasses
  5. Deny rules block
  6. Scope hierarchy: CLI > Config > Default
- **Implementation Details**:
```csharp
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
        // 1: --no flag blocks everything
        if (noFlagSet)
        {
            _logger.LogDebug("Bypass blocked: --no flag set");
            return false;
        }

        // 2: --interactive forces prompts
        if (interactiveMode)
        {
            _logger.LogDebug("Bypass blocked: --interactive mode");
            return false;
        }

        // 3: Protected operations never bypass
        if (IsProtected(operation))
        {
            _logger.LogDebug("Bypass blocked: protected operation {Target}", operation.Target);
            return false;
        }

        // 4: Critical risk level never bypasses
        if (operation.RiskLevel == RiskLevel.Critical)
        {
            _logger.LogDebug("Bypass blocked: critical risk level for {Target}", operation.Target);
            return false;
        }

        // 5: Deny rules block
        if (denyRules != null)
        {
            foreach (var rule in denyRules)
            {
                if (rule.Matches(operation))
                {
                    _logger.LogDebug("Bypass blocked: deny rule matched");
                    return false;
                }
            }
        }

        // 6: Apply scope hierarchy (CLI > config > default)
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
        if (cliScope != null && !cliScope.IsNone)
            return cliScope;

        if (configScope != null && !configScope.IsNone)
            return configScope;

        return defaultScope ?? YesScope.Default;
    }
}
```
- **Acceptance Criteria Covered**: AC-037-043 (precedence and resolution)
- **Test Requirements**:
  - [ ] --no flag overrides all scopes
  - [ ] --interactive forces prompts
  - [ ] Protected operations always block
  - [ ] Critical risk always blocks
  - [ ] Deny rules block
  - [ ] CLI scope overrides config
  - [ ] Config scope overrides default
  - [ ] Correct logging at each level
- **Success Criteria**:
  - [ ] Precedence chain implemented exactly as specified
  - [ ] All test scenarios pass
  - [ ] Logging shows decision points
  - [ ] Coverage > 95%
- **Gap Checklist Item**: [ ] 🔄 ScopeResolver created with full precedence logic

---

# PHASE 6: RATE LIMITING & SESSION MANAGEMENT

**Hours: 2-3**

**Objective**: Implement rate limiting and per-session scope management

**Acceptance Criteria Covered**: AC-053-059, AC-089-093

## Gap 6.1: Create IRateLimiter Interface and RateLimiter Implementation

- **Current State**: ❌ MISSING
- **File**: src/Acode.Infrastructure/Approvals/Scoping/RateLimiter.cs
- **What's Missing**: Rate limiter with 100/minute default, 30-second pause
- **Implementation Details** (from spec):
```csharp
namespace Acode.Infrastructure.Approvals.Scoping;

/// <summary>
/// Rate limits --yes bypasses to prevent runaway automation.
/// </summary>
public interface IRateLimiter
{
    RateLimitResult TryBypass();
    RateLimitStatus GetStatus();
    void Reset();
}

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
- **Acceptance Criteria Covered**: AC-053-059 (rate limiting)
- **Test Requirements**:
  - [ ] Allows bypasses within limit
  - [ ] Blocks after limit exceeded
  - [ ] Resets after pause period
  - [ ] Thread-safe (concurrent calls)
  - [ ] GetStatus() returns correct values
  - [ ] Reset() clears count
- **Success Criteria**:
  - [ ] Thread-safe counter
  - [ ] One-minute rolling window
  - [ ] All tests pass
  - [ ] Coverage > 90%
- **Gap Checklist Item**: [ ] 🔄 RateLimiter created with one-minute rolling window

## Gap 6.2: Create SessionScopeManager

- **Current State**: ❌ MISSING
- **File**: src/Acode.Infrastructure/Approvals/Scoping/SessionScopeManager.cs
- **What's Missing**: Per-session scope manager handling --yes-next one-time scopes
- **Implementation Details**:
```csharp
public class SessionScopeManager : IDisposable
{
    private readonly Guid _sessionId;
    private readonly ILogger<SessionScopeManager> _logger;
    private YesScope? _sessionScope = YesScope.Default;
    private YesScope? _nextOperationScope = null;
    private int _operationCount = 0;
    private DateTimeOffset _sessionStarted = DateTimeOffset.UtcNow;

    public SessionScopeManager(Guid sessionId, ILogger<SessionScopeManager> logger)
    {
        _sessionId = sessionId;
        _logger = logger;
        _logger.LogInformation("Session started: {SessionId}", sessionId);
    }

    public void SetSessionScope(YesScope scope)
    {
        _sessionScope = scope;
        _logger.LogInformation("Session scope set: {Scope}", scope);
    }

    public void SetNextOperationScope(YesScope scope)
    {
        _nextOperationScope = scope;
        _logger.LogInformation("One-time scope set for next operation: {Scope}", scope);
    }

    public YesScope GetScopeForOperation()
    {
        _operationCount++;

        // If one-time scope is set, use it once then clear
        if (_nextOperationScope != null)
        {
            var scope = _nextOperationScope;
            _nextOperationScope = null; // Consume it
            _logger.LogDebug("Using one-time scope for operation {Count}", _operationCount);
            return scope;
        }

        // Fall back to session scope
        _logger.LogDebug("Using session scope for operation {Count}", _operationCount);
        return _sessionScope ?? YesScope.Default;
    }

    public SessionStatistics GetStatistics()
    {
        var duration = DateTimeOffset.UtcNow - _sessionStarted;
        return new SessionStatistics(
            _sessionId,
            _sessionScope ?? YesScope.Default,
            _operationCount,
            duration);
    }

    public void Dispose()
    {
        var stats = GetStatistics();
        _logger.LogInformation(
            "Session ended: {SessionId}, Operations: {Count}, Duration: {Duration}",
            _sessionId,
            stats.OperationCount,
            stats.Duration);
    }
}

public record SessionStatistics(
    Guid SessionId,
    YesScope CurrentScope,
    int OperationCount,
    TimeSpan Duration);
```
- **Acceptance Criteria Covered**: AC-069-070, AC-089-093 (session management, one-time scope)
- **Test Requirements**:
  - [ ] GetScopeForOperation() returns session scope by default
  - [ ] SetNextOperationScope() takes effect for one operation
  - [ ] One-time scope consumed after use
  - [ ] Second call returns session scope
  - [ ] GetStatistics() returns correct values
  - [ ] Session never persists between sessions
  - [ ] Dispose logs statistics
- **Success Criteria**:
  - [ ] One-time scope consumed correctly
  - [ ] Session statistics tracked
  - [ ] No persistence between sessions
  - [ ] Logging at key points
- **Gap Checklist Item**: [ ] 🔄 SessionScopeManager created with one-time scope support

---

# PHASE 7: CLI INTEGRATION (YesOptions, DependencyInjection)

**Hours: 1-2**

**Objective**: Integrate into CLI command line parsing

**Acceptance Criteria Covered**: AC-068-075

## Gap 7.1: Create YesOptions CLI Class

- **Current State**: ❌ MISSING
- **File**: src/Acode.Cli/Options/YesOptions.cs
- **What's Missing**: CLI option definitions from spec (lines 4072-4112)
- **Implementation Details**:
```csharp
// src/Acode.Cli/Options/YesOptions.cs
using System.CommandLine;

namespace Acode.Cli.Options;

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
- **Acceptance Criteria Covered**: AC-068-075 (CLI integration)
- **Test Requirements**:
  - [ ] All options exist
  - [ ] Aliases are correct
  - [ ] Descriptions clear
  - [ ] AddYesOptionsToCommand() adds all
- **Success Criteria**:
  - [ ] File created at correct path
  - [ ] All 6 options defined
  - [ ] AddYesOptionsToCommand() helper works
  - [ ] No NotImplementedException
- **Gap Checklist Item**: [ ] 🔄 YesOptions CLI class created

## Gap 7.2: Update DependencyInjection Registration

- **Current State**: ⚠️ PARTIAL (File exists but needs AddYesScopingServices method)
- **File**: src/Acode.Infrastructure/DependencyInjection.cs
- **What's Missing**: AddYesScopingServices extension method
- **Implementation Details** (from spec):
```csharp
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
        services.AddSingleton<RiskLevelClassifier>();

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
- **Test Requirements**:
  - [ ] All services registered
  - [ ] Singletons for stateless services
  - [ ] Scoped for SessionScopeManager
  - [ ] Configuration applied to RateLimitConfig
- **Success Criteria**:
  - [ ] Method added to DependencyInjection class
  - [ ] All services registered correctly
  - [ ] Configuration applied
  - [ ] Compiles without errors
- **Gap Checklist Item**: [ ] 🔄 DependencyInjection.AddYesScopingServices() added

---

# PHASE 8: INTEGRATION & E2E TESTS, BENCHMARKS, REGRESSION TESTS

**Hours: 3-4**

**Objective**: Full system testing and performance verification

**Test Count: 23+ tests (8 Integration, 8 E2E, 4 Performance, 3 Regression)**

## Gap 8.1: Integration Tests (ScopeApplicationTests, ProtectedOperationTests, RateLimitTests)

- **Current State**: ❌ MISSING (3 test files from spec, lines 2708-2948)
- **What's Missing**: 10 integration test methods across 3 files
- **Key Tests**:
  - ScopeApplicationTests (3): Apply session scope, one-time scope, clear operation scope
  - ProtectedOperationTests (4): Git, agent config, env files, critical terminal commands
  - RateLimitTests (3): Allow within limit, block when exceeded, reset after pause
- **Success Criteria**:
  - [ ] All 10 integration tests pass
  - [ ] Real scope application verified
  - [ ] Session behavior verified
  - [ ] Rate limit window verified
- **Gap Checklist Item**: [ ] 🔄 Integration tests created and passing (10 tests)

## Gap 8.2: E2E Tests (YesScopingE2ETests)

- **Current State**: ❌ MISSING (1 test file from spec, lines 2953-3103)
- **What's Missing**: 8 E2E test methods covering full workflows
- **Key Tests**:
  - Bypass file read with default --yes
  - Prompt for file write without explicit scope
  - Bypass file write with explicit scope
  - Block protected operation even with --yes=all
  - Enforce rate limit
  - Require acknowledgment for --yes=all
  - Reject invalid scope syntax
  - Log all bypasses to audit
- **Success Criteria**:
  - [ ] All 8 E2E tests pass
  - [ ] Full workflows verified
  - [ ] Audit logging verified
  - [ ] Error handling verified
- **Gap Checklist Item**: [ ] 🔄 E2E tests created and passing (8 tests)

## Gap 8.3: Performance Benchmarks

- **Current State**: ❌ MISSING (benchmark code from spec, lines 3108-3168)
- **What's Missing**: 4 performance benchmarks with targets
- **Benchmarks**:
  - ParseSimpleScope: < 1ms (target 0.3ms)
  - ParseComplexScope: < 2ms (target 0.8ms)
  - ValidatePattern: < 5ms (target 1ms)
  - ClassifyRiskLevel: < 0.5ms (target 0.05ms)
- **Success Criteria**:
  - [ ] All benchmarks created
  - [ ] Baseline measurements taken
  - [ ] Targets achievable with optimization
  - [ ] No performance regressions
- **Gap Checklist Item**: [ ] 🔄 Performance benchmarks created and analyzed

## Gap 8.4: Regression Tests

- **Current State**: ❌ MISSING (test code from spec, lines 3185-3237)
- **What's Missing**: 3 regression tests for security CVEs
- **Tests**:
  - CVE2024-001: Scope injection (embedded flags)
  - CVE2024-002: Risk level downgrade
  - CVE2024-003: Pattern denial of service
- **Success Criteria**:
  - [ ] All 3 regression tests pass
  - [ ] Vulnerabilities mitigated
  - [ ] Injection prevented
  - [ ] DoS prevented
- **Gap Checklist Item**: [ ] 🔄 Regression tests created for security CVEs

---

# SUMMARY TABLE

| Phase | Description | Hours | Files | Test Methods | AC Coverage | Status |
|-------|-------------|-------|-------|--------------|-------------|--------|
| 1 | Domain Model (5 types) | 3-4 | 5 prod | 0 | AC-030-052 | [ ] |
| 2 | Domain Unit Tests | 1-2 | 2 test | 20+ | AC-030-052 | [ ] |
| 3 | Scope Parsing & Validation | 4-5 | 3 prod | 12 | AC-001-011, -082, -099 | [ ] |
| 4 | Risk Classification & Security | 4-5 | 4 prod | 15+ | AC-030-036, -044-052, -099-103 | [ ] |
| 5 | Scope Resolution | 3-4 | 2 prod | 5+ | AC-037-043 | [ ] |
| 6 | Rate Limiting & Sessions | 2-3 | 2 prod | 6+ | AC-053-059, -089-093 | [ ] |
| 7 | CLI Integration | 1-2 | 2 prod | 2+ | AC-068-075 | [ ] |
| 8 | Integration, E2E, Benchmarks | 3-4 | 5 test | 25+ | AC-001-103 | [ ] |
| **TOTAL** | **Yes Scoping Rules** | **20-28** | **17 prod, 10 test** | **50+** | **ALL 103** | **[ ] COMPLETE** |

---

# IMPLEMENTATION INSTRUCTIONS

## Before Starting Any Phase

1. Create a test file FIRST (RED state)
2. Write minimal implementation to pass tests (GREEN state)
3. Refactor for clarity while tests remain green (REFACTOR state)
4. Commit after each complete gap
5. Update this checklist with [✅] when gap complete

## Verify Completion Before Moving to Next Phase

After each phase, run:
```bash
dotnet build  # Must succeed with 0 errors, 0 warnings
dotnet test --filter "FullyQualifiedName~YesScope"  # All tests must pass
```

## After All Phases Complete

1. Run full test suite
2. Verify all 103 ACs implemented
3. Run performance benchmarks
4. Review audit logging
5. Create PR with all 8 phases
6. Verify all 50+ tests passing in CI

---

**Checklist Usage Notes:**
- Mark phases as IN PROGRESS when starting
- Mark gaps as [✅] COMPLETE when tests pass
- Update effort estimates if actual differs from estimate
- Note any blockers or dependencies
- Move to next phase only when current phase 100% complete

---
