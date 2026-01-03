# Task 028.a: Conflict Heuristics

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 028 (Merge Coordinator)  

---

## Description

Task 028.a implements conflict detection heuristics. The system MUST predict merge conflicts before they occur. Heuristics MUST be fast and reasonably accurate.

Conflict heuristics analyze file changes from parallel tasks. Overlapping modifications MUST be detected. Semantic proximity (same function, same class) MUST be considered.

Heuristics MUST be tunable. False positives reduce throughput. False negatives cause merge failures. The balance MUST be configurable.

### Business Value

Conflict heuristics enable:
- Early conflict detection
- Reduced merge failures
- Better task ordering
- Parallel execution confidence
- Predictable merges

### Scope Boundaries

This task covers detection heuristics. Merge execution is in Task 028. Dependency ordering is in Task 028.b.

### Integration Points

- Task 028: Uses heuristics for planning
- Task 022: Git provides diff data
- Task 027: Worker changes analyzed

### Failure Modes

- False positive → Unnecessary blocking
- False negative → Merge failure
- Slow analysis → Bottleneck
- Parse failure → Conservative estimate

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Heuristic | Educated guess algorithm |
| Overlap | Shared modification region |
| Proximity | Closeness of changes |
| Scope | Function/class boundary |
| Hotspot | Frequently modified area |
| Sensitivity | True positive rate |
| Specificity | True negative rate |

---

## Out of Scope

- Machine learning models
- Historical conflict prediction
- Semantic code understanding
- Cross-file dependency analysis
- Language-specific AST parsing

---

## Functional Requirements

### FR-001 to FR-025: Line-Level Analysis

- FR-001: Line ranges MUST be extracted
- FR-002: Added lines MUST be tracked
- FR-003: Removed lines MUST be tracked
- FR-004: Modified lines MUST be tracked
- FR-005: Context lines MUST be considered
- FR-006: Default context: 5 lines
- FR-007: Overlap detection MUST run
- FR-008: Direct overlap MUST be flagged
- FR-009: Adjacent overlap MUST be flagged
- FR-010: Near overlap MUST warn
- FR-011: Near threshold MUST be configurable
- FR-012: Default near: 10 lines
- FR-013: Overlap score MUST be computed
- FR-014: Score = overlap / change size
- FR-015: High score MUST be critical
- FR-016: Medium score MUST warn
- FR-017: Low score MUST be info
- FR-018: Score thresholds MUST be configurable
- FR-019: Line endings MUST be normalized
- FR-020: Whitespace MUST be optionally ignored
- FR-021: Moved lines MUST be detected
- FR-022: Moves MUST reduce conflict score
- FR-023: Renamed files MUST be tracked
- FR-024: Renames MUST be matched
- FR-025: Unmatched renames MUST warn

### FR-026 to FR-045: Scope Analysis

- FR-026: Function boundaries MUST be detected
- FR-027: Detection MUST be regex-based
- FR-028: C# method pattern MUST work
- FR-029: Python def pattern MUST work
- FR-030: JS function pattern MUST work
- FR-031: Class boundaries MUST be detected
- FR-032: Same function MUST increase severity
- FR-033: Same class MUST increase severity
- FR-034: Different scope MUST decrease severity
- FR-035: Scope detection MUST be fast
- FR-036: Fallback to line-only MUST work
- FR-037: Scope patterns MUST be configurable
- FR-038: Custom patterns MUST be addable
- FR-039: Pattern file MUST be loadable
- FR-040: Import sections MUST be detected
- FR-041: Import conflicts MUST auto-merge
- FR-042: Using statements MUST auto-merge
- FR-043: Namespace changes MUST warn
- FR-044: Comment changes MUST be low priority
- FR-045: Documentation changes MUST be low

### FR-046 to FR-065: File Type Rules

- FR-046: File type MUST affect analysis
- FR-047: Code files MUST use full analysis
- FR-048: Config files MUST be cautious
- FR-049: Lock files MUST use special rules
- FR-050: package-lock.json MUST regenerate
- FR-051: yarn.lock MUST regenerate
- FR-052: Binary files MUST NOT merge
- FR-053: Image files MUST use last-wins
- FR-054: Generated files MUST regenerate
- FR-055: Generated patterns MUST be configurable
- FR-056: Test files MAY be lenient
- FR-057: Lenient mode MUST be optional
- FR-058: Snapshot files MUST be cautious
- FR-059: Database migrations MUST be strict
- FR-060: API contracts MUST be strict
- FR-061: Rules MUST be pattern-based
- FR-062: Rules MUST be prioritized
- FR-063: First match MUST apply
- FR-064: Default rule MUST exist
- FR-065: Rule override MUST be logged

---

## Non-Functional Requirements

- NFR-001: Analysis MUST be <500ms per file
- NFR-002: 100 files MUST analyze in <10s
- NFR-003: Memory MUST be bounded
- NFR-004: Pattern matching MUST be cached
- NFR-005: Results MUST be deterministic
- NFR-006: Same inputs MUST yield same output
- NFR-007: Errors MUST not crash
- NFR-008: Fallback MUST always work
- NFR-009: Logging MUST explain decisions
- NFR-010: Metrics MUST track accuracy

---

## User Manual Documentation

### Configuration

```yaml
heuristics:
  lineOverlap:
    contextLines: 5
    nearThresholdLines: 10
    ignoreWhitespace: true
    
  severity:
    criticalThreshold: 0.8
    highThreshold: 0.5
    mediumThreshold: 0.2
    
  scope:
    enabled: true
    sameFunctionMultiplier: 2.0
    sameClassMultiplier: 1.5
    
  fileRules:
    - pattern: "*.lock"
      action: regenerate
    - pattern: "*.generated.cs"
      action: regenerate
    - pattern: "**/migrations/*.cs"
      action: strict
    - pattern: "**/*.test.cs"
      mode: lenient
```

### Severity Levels

| Severity | Score | Meaning |
|----------|-------|---------|
| Critical | ≥0.8 | Direct conflict, blocks merge |
| High | ≥0.5 | Likely conflict, needs review |
| Medium | ≥0.2 | Possible conflict, warning |
| Low | <0.2 | Unlikely conflict, auto-merge |

### Scope Patterns

```yaml
scopePatterns:
  csharp:
    method: '^\s*(public|private|protected|internal).*\w+\s*\('
    class: '^\s*(public|private|protected|internal)?\s*class\s+\w+'
  python:
    method: '^\s*def\s+\w+\s*\('
    class: '^\s*class\s+\w+'
  javascript:
    method: '^\s*(function|async function|const\s+\w+\s*=.*=>)'
    class: '^\s*class\s+\w+'
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Line overlap detected
- [ ] AC-002: Context considered
- [ ] AC-003: Severity computed
- [ ] AC-004: Scope detected
- [ ] AC-005: Scope affects severity
- [ ] AC-006: File rules applied
- [ ] AC-007: Lock files handled
- [ ] AC-008: Binary files blocked
- [ ] AC-009: Config tunable
- [ ] AC-010: Performance OK
- [ ] AC-011: Logging clear
- [ ] AC-012: Tests comprehensive

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Line overlap detection
- [ ] UT-002: Severity calculation
- [ ] UT-003: Scope detection
- [ ] UT-004: File rule matching
- [ ] UT-005: Edge cases

### Integration Tests

- [ ] IT-001: Real diff analysis
- [ ] IT-002: Multi-file changes
- [ ] IT-003: Mixed file types
- [ ] IT-004: Performance benchmarks

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Merge/
│       ├── FileChange.cs
│       ├── LineDiff.cs
│       ├── ConflictSeverity.cs
│       └── ScopeType.cs
├── Acode.Application/
│   └── Merge/
│       ├── IConflictHeuristics.cs
│       ├── IScopeDetector.cs
│       └── IFileRuleEngine.cs
├── Acode.Infrastructure/
│   └── Merge/
│       ├── Heuristics/
│       │   ├── ConflictHeuristics.cs
│       │   ├── LineOverlapAnalyzer.cs
│       │   ├── ScopeDetector.cs
│       │   ├── FileRuleEngine.cs
│       │   └── HeuristicsOptions.cs
│       └── Patterns/
│           ├── ScopePatterns.cs
│           └── LanguagePatterns.cs
└── Acode.Cli/
    └── Commands/
        └── Merge/
            └── AnalyzeConflictsCommand.cs
tests/
└── Acode.Infrastructure.Tests/
    └── Merge/
        └── Heuristics/
            ├── LineOverlapAnalyzerTests.cs
            ├── ScopeDetectorTests.cs
            └── FileRuleEngineTests.cs
```

### Part 1: Domain Models

```csharp
// File: src/Acode.Domain/Merge/ConflictSeverity.cs
namespace Acode.Domain.Merge;

/// <summary>
/// Severity level of detected conflicts.
/// </summary>
public enum ConflictSeverity
{
    /// <summary>Unlikely conflict, auto-merge safe.</summary>
    Low = 0,
    
    /// <summary>Possible conflict, warning issued.</summary>
    Medium = 1,
    
    /// <summary>Likely conflict, needs review.</summary>
    High = 2,
    
    /// <summary>Direct conflict, blocks merge.</summary>
    Critical = 3
}

// File: src/Acode.Domain/Merge/FileChange.cs
namespace Acode.Domain.Merge;

/// <summary>
/// Represents changes to a file.
/// </summary>
public sealed record FileChange
{
    public required string Path { get; init; }
    public required ChangeType Type { get; init; }
    public required IReadOnlyList<LineDiff> Diffs { get; init; }
    public string? OldPath { get; init; } // For renames
    public bool IsBinary { get; init; } = false;
    
    /// <summary>
    /// Total lines changed (added + removed + modified).
    /// </summary>
    public int TotalLinesChanged => Diffs.Sum(d => d.LineCount);
}

public enum ChangeType
{
    Added,
    Modified,
    Deleted,
    Renamed
}

// File: src/Acode.Domain/Merge/LineDiff.cs
namespace Acode.Domain.Merge;

/// <summary>
/// A contiguous diff hunk within a file.
/// </summary>
public sealed record LineDiff
{
    public required int StartLine { get; init; }
    public required int EndLine { get; init; }
    public required DiffType Type { get; init; }
    public string? Content { get; init; }
    
    public int LineCount => EndLine - StartLine + 1;
    
    /// <summary>
    /// Expand range by context lines.
    /// </summary>
    public LineDiff WithContext(int contextLines) => this with
    {
        StartLine = Math.Max(1, StartLine - contextLines),
        EndLine = EndLine + contextLines
    };
    
    /// <summary>
    /// Check if this diff overlaps with another.
    /// </summary>
    public bool Overlaps(LineDiff other) =>
        StartLine <= other.EndLine && EndLine >= other.StartLine;
    
    /// <summary>
    /// Calculate overlap size with another diff.
    /// </summary>
    public int OverlapSize(LineDiff other)
    {
        if (!Overlaps(other)) return 0;
        
        var overlapStart = Math.Max(StartLine, other.StartLine);
        var overlapEnd = Math.Min(EndLine, other.EndLine);
        return overlapEnd - overlapStart + 1;
    }
}

public enum DiffType
{
    Add,
    Remove,
    Modify
}

// File: src/Acode.Domain/Merge/ScopeType.cs
namespace Acode.Domain.Merge;

public enum ScopeType
{
    Unknown,
    File,
    Namespace,
    Class,
    Method,
    Property,
    Constructor,
    Import,
    Comment
}
```

### Part 2: Analysis Result Models

```csharp
// File: src/Acode.Domain/Merge/ConflictAnalysis.cs
namespace Acode.Domain.Merge;

/// <summary>
/// Result of conflict heuristic analysis.
/// </summary>
public sealed record ConflictAnalysis
{
    public required IReadOnlyList<FileConflict> Conflicts { get; init; }
    public required ConflictSeverity MaxSeverity { get; init; }
    public required TimeSpan AnalysisDuration { get; init; }
    public required int FilesAnalyzed { get; init; }
    
    public bool HasCriticalConflicts => MaxSeverity == ConflictSeverity.Critical;
    public bool HasHighConflicts => MaxSeverity >= ConflictSeverity.High;
    public bool IsSafeToMerge => MaxSeverity <= ConflictSeverity.Medium;
    
    public IReadOnlyList<FileConflict> GetByMinSeverity(ConflictSeverity min) =>
        Conflicts.Where(c => c.Severity >= min).ToList();
}

/// <summary>
/// Conflict analysis for a single file.
/// </summary>
public sealed record FileConflict
{
    public required string Path { get; init; }
    public required double OverlapScore { get; init; }
    public required ConflictSeverity Severity { get; init; }
    public required IReadOnlyList<LineOverlap> Overlaps { get; init; }
    public ScopeMatch? Scope { get; init; }
    public required string AppliedRule { get; init; }
    public string? Details { get; init; }
}

/// <summary>
/// Overlapping line ranges between local and remote changes.
/// </summary>
public sealed record LineOverlap
{
    public required LineRange Local { get; init; }
    public required LineRange Remote { get; init; }
    public required double Score { get; init; }
}

public sealed record LineRange(int Start, int End)
{
    public int Length => End - Start + 1;
}

/// <summary>
/// Scope match information for increased severity.
/// </summary>
public sealed record ScopeMatch
{
    public required ScopeType Type { get; init; }
    public required string Name { get; init; }
    public required double SeverityMultiplier { get; init; }
}
```

### Part 3: Application Interfaces

```csharp
// File: src/Acode.Application/Merge/IConflictHeuristics.cs
namespace Acode.Application.Merge;

/// <summary>
/// Predicts merge conflicts using heuristics.
/// </summary>
public interface IConflictHeuristics
{
    /// <summary>
    /// Analyze changes for potential conflicts.
    /// </summary>
    Task<ConflictAnalysis> AnalyzeAsync(
        IReadOnlyList<FileChange> localChanges,
        IReadOnlyList<FileChange> remoteChanges,
        HeuristicsOptions? options = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Quick check if any conflicts are critical.
    /// </summary>
    Task<bool> HasCriticalConflictsAsync(
        IReadOnlyList<FileChange> localChanges,
        IReadOnlyList<FileChange> remoteChanges,
        CancellationToken ct = default);
}

// File: src/Acode.Application/Merge/IScopeDetector.cs
namespace Acode.Application.Merge;

/// <summary>
/// Detects code scope (function, class) at line positions.
/// </summary>
public interface IScopeDetector
{
    /// <summary>
    /// Detect scope at a specific line in a file.
    /// </summary>
    Task<ScopeInfo?> DetectScopeAsync(
        string filePath,
        int lineNumber,
        CancellationToken ct = default);
    
    /// <summary>
    /// Get all scopes in a file.
    /// </summary>
    Task<IReadOnlyList<ScopeInfo>> GetScopesAsync(
        string filePath,
        CancellationToken ct = default);
}

public sealed record ScopeInfo
{
    public required ScopeType Type { get; init; }
    public required string Name { get; init; }
    public required int StartLine { get; init; }
    public required int EndLine { get; init; }
}

// File: src/Acode.Application/Merge/IFileRuleEngine.cs
namespace Acode.Application.Merge;

/// <summary>
/// Applies file-specific rules for conflict handling.
/// </summary>
public interface IFileRuleEngine
{
    /// <summary>
    /// Get the rule that applies to a file.
    /// </summary>
    FileRule GetRule(string filePath);
    
    /// <summary>
    /// Check if file should block on any conflict.
    /// </summary>
    bool IsStrictFile(string filePath);
    
    /// <summary>
    /// Check if file should use lenient rules.
    /// </summary>
    bool IsLenientFile(string filePath);
    
    /// <summary>
    /// Check if file should be regenerated instead of merged.
    /// </summary>
    bool ShouldRegenerate(string filePath);
}

public sealed record FileRule
{
    public required string Pattern { get; init; }
    public required FileRuleAction Action { get; init; }
    public ConflictSeverity? MaxSeverity { get; init; }
    public string? Description { get; init; }
}

public enum FileRuleAction
{
    Normal,
    Strict,
    Lenient,
    Regenerate,
    Block
}
```

### Part 4: Heuristics Implementation

```csharp
// File: src/Acode.Infrastructure/Merge/Heuristics/HeuristicsOptions.cs
namespace Acode.Infrastructure.Merge.Heuristics;

public sealed record HeuristicsOptions
{
    public int ContextLines { get; init; } = 5;
    public int NearThresholdLines { get; init; } = 10;
    public bool IgnoreWhitespace { get; init; } = true;
    
    public double CriticalThreshold { get; init; } = 0.8;
    public double HighThreshold { get; init; } = 0.5;
    public double MediumThreshold { get; init; } = 0.2;
    
    public bool ScopeAnalysisEnabled { get; init; } = true;
    public double SameFunctionMultiplier { get; init; } = 2.0;
    public double SameClassMultiplier { get; init; } = 1.5;
    
    public IReadOnlyList<FileRule> FileRules { get; init; } = DefaultRules;
    
    public static IReadOnlyList<FileRule> DefaultRules => new[]
    {
        new FileRule { Pattern = "*.lock", Action = FileRuleAction.Regenerate },
        new FileRule { Pattern = "*.generated.cs", Action = FileRuleAction.Regenerate },
        new FileRule { Pattern = "**/migrations/*.cs", Action = FileRuleAction.Strict },
        new FileRule { Pattern = "**/*.test.cs", Action = FileRuleAction.Lenient }
    };
}

// File: src/Acode.Infrastructure/Merge/Heuristics/LineOverlapAnalyzer.cs
namespace Acode.Infrastructure.Merge.Heuristics;

public sealed class LineOverlapAnalyzer
{
    private readonly HeuristicsOptions _options;
    
    public LineOverlapAnalyzer(HeuristicsOptions options)
    {
        _options = options;
    }
    
    public IReadOnlyList<LineOverlap> FindOverlaps(
        IReadOnlyList<LineDiff> localDiffs,
        IReadOnlyList<LineDiff> remoteDiffs)
    {
        var overlaps = new List<LineOverlap>();
        
        // Expand diffs with context
        var localExpanded = localDiffs
            .Select(d => d.WithContext(_options.ContextLines))
            .ToList();
        var remoteExpanded = remoteDiffs
            .Select(d => d.WithContext(_options.ContextLines))
            .ToList();
        
        foreach (var local in localExpanded)
        {
            foreach (var remote in remoteExpanded)
            {
                if (local.Overlaps(remote))
                {
                    var overlapSize = local.OverlapSize(remote);
                    var unionSize = (local.EndLine - local.StartLine + 1) +
                                   (remote.EndLine - remote.StartLine + 1) -
                                   overlapSize;
                    var score = (double)overlapSize / unionSize;
                    
                    overlaps.Add(new LineOverlap
                    {
                        Local = new LineRange(local.StartLine, local.EndLine),
                        Remote = new LineRange(remote.StartLine, remote.EndLine),
                        Score = score
                    });
                }
            }
        }
        
        return overlaps;
    }
    
    public double ComputeOverlapScore(IReadOnlyList<LineOverlap> overlaps)
    {
        if (overlaps.Count == 0) return 0;
        return overlaps.Max(o => o.Score);
    }
    
    public ConflictSeverity ScoreToSeverity(double score)
    {
        if (score >= _options.CriticalThreshold) return ConflictSeverity.Critical;
        if (score >= _options.HighThreshold) return ConflictSeverity.High;
        if (score >= _options.MediumThreshold) return ConflictSeverity.Medium;
        return ConflictSeverity.Low;
    }
}

// File: src/Acode.Infrastructure/Merge/Heuristics/ConflictHeuristics.cs
namespace Acode.Infrastructure.Merge.Heuristics;

public sealed class ConflictHeuristics : IConflictHeuristics
{
    private readonly IScopeDetector _scopeDetector;
    private readonly IFileRuleEngine _ruleEngine;
    private readonly ILogger<ConflictHeuristics> _logger;
    
    public ConflictHeuristics(
        IScopeDetector scopeDetector,
        IFileRuleEngine ruleEngine,
        ILogger<ConflictHeuristics> logger)
    {
        _scopeDetector = scopeDetector;
        _ruleEngine = ruleEngine;
        _logger = logger;
    }
    
    public async Task<ConflictAnalysis> AnalyzeAsync(
        IReadOnlyList<FileChange> localChanges,
        IReadOnlyList<FileChange> remoteChanges,
        HeuristicsOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new HeuristicsOptions();
        var stopwatch = Stopwatch.StartNew();
        var conflicts = new List<FileConflict>();
        
        // Group changes by file
        var localByPath = localChanges.ToDictionary(c => c.Path);
        var remoteByPath = remoteChanges.ToDictionary(c => c.Path);
        
        // Find files modified in both
        var commonPaths = localByPath.Keys.Intersect(remoteByPath.Keys);
        
        foreach (var path in commonPaths)
        {
            ct.ThrowIfCancellationRequested();
            
            var local = localByPath[path];
            var remote = remoteByPath[path];
            
            var conflict = await AnalyzeFileAsync(
                path, local, remote, options, ct);
            
            if (conflict != null)
            {
                conflicts.Add(conflict);
                _logger.LogDebug(
                    "Conflict detected in {Path}: {Severity} (score: {Score:F2})",
                    path, conflict.Severity, conflict.OverlapScore);
            }
        }
        
        stopwatch.Stop();
        
        return new ConflictAnalysis
        {
            Conflicts = conflicts,
            MaxSeverity = conflicts.Count > 0 
                ? conflicts.Max(c => c.Severity) 
                : ConflictSeverity.Low,
            AnalysisDuration = stopwatch.Elapsed,
            FilesAnalyzed = commonPaths.Count()
        };
    }
    
    private async Task<FileConflict?> AnalyzeFileAsync(
        string path,
        FileChange local,
        FileChange remote,
        HeuristicsOptions options,
        CancellationToken ct)
    {
        // Check file rules first
        var rule = _ruleEngine.GetRule(path);
        
        if (rule.Action == FileRuleAction.Regenerate)
        {
            return new FileConflict
            {
                Path = path,
                OverlapScore = 0,
                Severity = ConflictSeverity.Low,
                Overlaps = [],
                AppliedRule = $"Regenerate: {rule.Pattern}",
                Details = "File will be regenerated, no conflict analysis needed"
            };
        }
        
        // Binary files always block
        if (local.IsBinary || remote.IsBinary)
        {
            return new FileConflict
            {
                Path = path,
                OverlapScore = 1.0,
                Severity = ConflictSeverity.Critical,
                Overlaps = [],
                AppliedRule = "Binary file",
                Details = "Binary files cannot be merged"
            };
        }
        
        // Line overlap analysis
        var analyzer = new LineOverlapAnalyzer(options);
        var overlaps = analyzer.FindOverlaps(local.Diffs, remote.Diffs);
        
        if (overlaps.Count == 0)
            return null; // No conflict
        
        var score = analyzer.ComputeOverlapScore(overlaps);
        var severity = analyzer.ScoreToSeverity(score);
        
        // Scope analysis for severity adjustment
        ScopeMatch? scopeMatch = null;
        if (options.ScopeAnalysisEnabled)
        {
            scopeMatch = await CheckScopeOverlapAsync(
                path, overlaps, options, ct);
            
            if (scopeMatch != null)
            {
                score = Math.Min(1.0, score * scopeMatch.SeverityMultiplier);
                severity = analyzer.ScoreToSeverity(score);
            }
        }
        
        // Apply rule severity limits
        if (rule.MaxSeverity.HasValue && severity > rule.MaxSeverity.Value)
        {
            severity = rule.MaxSeverity.Value;
        }
        
        return new FileConflict
        {
            Path = path,
            OverlapScore = score,
            Severity = severity,
            Overlaps = overlaps,
            Scope = scopeMatch,
            AppliedRule = rule.Pattern
        };
    }
    
    private async Task<ScopeMatch?> CheckScopeOverlapAsync(
        string path,
        IReadOnlyList<LineOverlap> overlaps,
        HeuristicsOptions options,
        CancellationToken ct)
    {
        try
        {
            var scopes = await _scopeDetector.GetScopesAsync(path, ct);
            
            foreach (var overlap in overlaps)
            {
                // Find scope containing the overlap
                var matchingScope = scopes.FirstOrDefault(s =>
                    s.StartLine <= overlap.Local.Start &&
                    s.EndLine >= overlap.Local.End);
                
                if (matchingScope != null)
                {
                    var multiplier = matchingScope.Type switch
                    {
                        ScopeType.Method => options.SameFunctionMultiplier,
                        ScopeType.Class => options.SameClassMultiplier,
                        _ => 1.0
                    };
                    
                    return new ScopeMatch
                    {
                        Type = matchingScope.Type,
                        Name = matchingScope.Name,
                        SeverityMultiplier = multiplier
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, 
                "Scope detection failed for {Path}, continuing without", path);
        }
        
        return null;
    }
    
    public async Task<bool> HasCriticalConflictsAsync(
        IReadOnlyList<FileChange> localChanges,
        IReadOnlyList<FileChange> remoteChanges,
        CancellationToken ct = default)
    {
        var analysis = await AnalyzeAsync(
            localChanges, remoteChanges, ct: ct);
        return analysis.HasCriticalConflicts;
    }
}
```

### Part 5: Scope Detector & File Rules

```csharp
// File: src/Acode.Infrastructure/Merge/Heuristics/ScopeDetector.cs
namespace Acode.Infrastructure.Merge.Heuristics;

public sealed class ScopeDetector : IScopeDetector
{
    private readonly Dictionary<string, LanguagePatterns> _patterns;
    
    public ScopeDetector()
    {
        _patterns = new Dictionary<string, LanguagePatterns>(
            StringComparer.OrdinalIgnoreCase)
        {
            [".cs"] = new LanguagePatterns
            {
                MethodPattern = new Regex(
                    @"^\s*(public|private|protected|internal|static|\s)*" +
                    @"(\w+\s+)+(\w+)\s*\(", RegexOptions.Compiled),
                ClassPattern = new Regex(
                    @"^\s*(public|private|protected|internal|static|sealed|abstract|\s)*" +
                    @"class\s+(\w+)", RegexOptions.Compiled),
                ImportPattern = new Regex(
                    @"^\s*using\s+", RegexOptions.Compiled)
            },
            [".py"] = new LanguagePatterns
            {
                MethodPattern = new Regex(
                    @"^\s*def\s+(\w+)\s*\(", RegexOptions.Compiled),
                ClassPattern = new Regex(
                    @"^\s*class\s+(\w+)", RegexOptions.Compiled),
                ImportPattern = new Regex(
                    @"^\s*(import|from)\s+", RegexOptions.Compiled)
            },
            [".js"] = new LanguagePatterns
            {
                MethodPattern = new Regex(
                    @"^\s*(async\s+)?function\s+(\w+)|" +
                    @"^\s*(const|let|var)\s+(\w+)\s*=\s*(async\s+)?\(",
                    RegexOptions.Compiled),
                ClassPattern = new Regex(
                    @"^\s*class\s+(\w+)", RegexOptions.Compiled),
                ImportPattern = new Regex(
                    @"^\s*(import|require)\s*", RegexOptions.Compiled)
            },
            [".ts"] = new LanguagePatterns
            {
                MethodPattern = new Regex(
                    @"^\s*(public|private|protected|async|static|\s)*" +
                    @"(\w+)\s*\(", RegexOptions.Compiled),
                ClassPattern = new Regex(
                    @"^\s*(export\s+)?(abstract\s+)?class\s+(\w+)",
                    RegexOptions.Compiled),
                ImportPattern = new Regex(
                    @"^\s*import\s+", RegexOptions.Compiled)
            }
        };
    }
    
    public async Task<ScopeInfo?> DetectScopeAsync(
        string filePath,
        int lineNumber,
        CancellationToken ct = default)
    {
        var scopes = await GetScopesAsync(filePath, ct);
        return scopes.FirstOrDefault(s => 
            lineNumber >= s.StartLine && lineNumber <= s.EndLine);
    }
    
    public async Task<IReadOnlyList<ScopeInfo>> GetScopesAsync(
        string filePath,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(filePath);
        if (!_patterns.TryGetValue(ext, out var patterns))
            return [];
        
        var lines = await File.ReadAllLinesAsync(filePath, ct);
        var scopes = new List<ScopeInfo>();
        var scopeStack = new Stack<(ScopeType Type, string Name, int Start)>();
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNum = i + 1;
            
            // Check for import
            if (patterns.ImportPattern?.IsMatch(line) == true)
            {
                scopes.Add(new ScopeInfo
                {
                    Type = ScopeType.Import,
                    Name = "imports",
                    StartLine = lineNum,
                    EndLine = lineNum
                });
                continue;
            }
            
            // Check for class
            var classMatch = patterns.ClassPattern?.Match(line);
            if (classMatch?.Success == true)
            {
                var name = classMatch.Groups[^1].Value;
                scopeStack.Push((ScopeType.Class, name, lineNum));
            }
            
            // Check for method
            var methodMatch = patterns.MethodPattern?.Match(line);
            if (methodMatch?.Success == true)
            {
                var name = methodMatch.Groups
                    .Cast<Group>()
                    .LastOrDefault(g => g.Success && !string.IsNullOrEmpty(g.Value))
                    ?.Value ?? "unknown";
                scopeStack.Push((ScopeType.Method, name, lineNum));
            }
            
            // Check for scope end (simple brace counting)
            // Note: This is a simplified implementation
            if (line.TrimEnd().EndsWith("}") && scopeStack.Count > 0)
            {
                var (type, name, start) = scopeStack.Pop();
                scopes.Add(new ScopeInfo
                {
                    Type = type,
                    Name = name,
                    StartLine = start,
                    EndLine = lineNum
                });
            }
        }
        
        return scopes;
    }
}

public sealed record LanguagePatterns
{
    public Regex? MethodPattern { get; init; }
    public Regex? ClassPattern { get; init; }
    public Regex? ImportPattern { get; init; }
}

// File: src/Acode.Infrastructure/Merge/Heuristics/FileRuleEngine.cs
namespace Acode.Infrastructure.Merge.Heuristics;

public sealed class FileRuleEngine : IFileRuleEngine
{
    private readonly IReadOnlyList<FileRule> _rules;
    private readonly FileRule _defaultRule;
    
    public FileRuleEngine(IReadOnlyList<FileRule>? rules = null)
    {
        _rules = rules ?? HeuristicsOptions.DefaultRules;
        _defaultRule = new FileRule 
        { 
            Pattern = "*", 
            Action = FileRuleAction.Normal 
        };
    }
    
    public FileRule GetRule(string filePath)
    {
        foreach (var rule in _rules)
        {
            if (MatchesPattern(filePath, rule.Pattern))
                return rule;
        }
        return _defaultRule;
    }
    
    public bool IsStrictFile(string filePath) =>
        GetRule(filePath).Action == FileRuleAction.Strict;
    
    public bool IsLenientFile(string filePath) =>
        GetRule(filePath).Action == FileRuleAction.Lenient;
    
    public bool ShouldRegenerate(string filePath) =>
        GetRule(filePath).Action == FileRuleAction.Regenerate;
    
    private static bool MatchesPattern(string path, string pattern)
    {
        // Convert glob pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace(@"\*\*", ".*")
            .Replace(@"\*", "[^/]*")
            .Replace(@"\?", ".") + "$";
        
        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }
}
```

### Implementation Checklist

- [ ] Create `ConflictSeverity`, `FileChange`, `LineDiff` domain models
- [ ] Create `ConflictAnalysis`, `FileConflict`, `LineOverlap` result models
- [ ] Create `IConflictHeuristics` interface
- [ ] Create `IScopeDetector` interface with language patterns
- [ ] Create `IFileRuleEngine` interface with glob matching
- [ ] Create `HeuristicsOptions` with configurable thresholds
- [ ] Implement `LineOverlapAnalyzer` with context expansion
- [ ] Implement `ConflictHeuristics` with full analysis pipeline
- [ ] Implement `ScopeDetector` for C#, Python, JS, TS
- [ ] Implement `FileRuleEngine` with pattern priorities
- [ ] Add binary file detection
- [ ] Add rename tracking
- [ ] Add lock file regeneration rules
- [ ] Write unit tests for overlap detection
- [ ] Write unit tests for scope detection
- [ ] Write unit tests for file rules
- [ ] Write integration tests with real diffs

### Rollout Plan

1. **Day 1**: Domain models and interfaces
2. **Day 2**: LineOverlapAnalyzer with scoring
3. **Day 3**: ConflictHeuristics main logic
4. **Day 4**: ScopeDetector with language patterns
5. **Day 5**: FileRuleEngine with glob matching
6. **Day 6**: Configuration and CLI
7. **Day 7**: Unit tests
8. **Day 8**: Integration tests with real scenarios

---

**End of Task 028.a Specification**