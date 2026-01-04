# Task 015.a: Ignore Rules + Gitignore Support

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 015 (Indexing v1)  

---

## Description

Task 015.a implements the ignore rule system for indexing. Ignore rules filter what gets indexed. They exclude build artifacts, dependencies, and binary files.

Gitignore is the primary ignore source. Most projects already have .gitignore files. Respecting these means zero configuration for typical projects. The existing rules just work.

The gitignore format is well-defined. Patterns use glob syntax. Negation is supported. Directory-specific rules are supported. The implementation must match git's behavior.

Additional ignore sources are supported. The .agent/config.yml can add patterns. Global ignores can be configured. These layer on top of gitignore.

Pattern precedence follows git conventions. Later patterns override earlier ones. More specific patterns override general ones. Negation patterns re-include excluded files.

Multiple .gitignore files are supported. The root .gitignore applies everywhere. Subdirectory .gitignore files add local rules. This matches how git handles ignores.

Binary file detection is automatic. Images, executables, and archives are detected. They are excluded by default. No explicit patterns needed for common binary types.

The ignore service is used by multiple systems. Indexing uses it. File enumeration uses it. Context packing uses it. A consistent API serves all consumers.

Performance is critical. Ignore checks happen for every file. Compiled patterns enable fast matching. Caching reduces repeated work.

### Business Value

Effective ignore rule support is fundamental to the quality and usability of the indexing system. Without proper ignore rules, the index would be polluted with build artifacts, dependency folders like node_modules, binary files, and other non-essential content that dramatically degrades search relevance and inflates index size. By implementing comprehensive gitignore support, the agent leverages the project's existing configuration, requiring zero additional setup for most repositories.

The business value extends beyond mere file filtering. Proper ignore handling ensures that context provided to the LLM is focused on actual source code and meaningful project files rather than generated or vendored content. This directly impacts response quality, token efficiency, and the agent's ability to understand project structure. Teams already invested in maintaining .gitignore files see immediate benefit without configuration overhead.

Furthermore, the ignore service serves as a foundational component for multiple downstream systems. Search results, context packing, file enumeration, and agent tool responses all depend on consistent ignore behavior. A single, well-tested ignore implementation prevents fragmented logic and ensures uniform behavior across all agent operations.

### Scope

1. **Gitignore Parser** - Full-fidelity parser for .gitignore files supporting all standard syntax including comments, blank lines, wildcards, double wildcards, character classes, negation, and directory-specific patterns
2. **Pattern Matcher** - High-performance glob pattern matching engine with compiled pattern support for fast evaluation against file paths
3. **Binary Detection** - Automatic binary file detection via file extension lookup and magic number inspection for common binary formats
4. **Ignore Service** - Unified API that aggregates rules from multiple sources (.gitignore files, config, global settings) with proper precedence ordering
5. **CLI Commands** - `acode ignore check` and `acode ignore list` commands for debugging and visibility into ignore behavior

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Index Service | Consumer | Calls IsIgnored() during file enumeration to filter indexed content |
| File Enumerator | Consumer | Uses ignore service to exclude files from directory traversal |
| Context Packer | Consumer | Filters context candidates through ignore rules before inclusion |
| Configuration Service | Provider | Supplies additional ignore patterns from .agent/config.yml |
| Search Tools | Consumer | Ensures search results respect ignore rules for consistency |
| CLI Commands | Consumer | Exposes ignore checking and listing for debugging |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Malformed gitignore pattern | Invalid pattern skipped, file may be incorrectly included | Log warning with line number, continue processing remaining patterns |
| Missing .gitignore file | No project-specific ignores applied | Graceful handling with empty rule set, log at debug level only |
| Encoding errors in gitignore | Parser fails to read patterns | Attempt UTF-8 fallback, then Latin-1, log warning and skip problematic lines |
| Recursive pattern explosion | Pattern matching becomes slow | Set maximum recursion depth, timeout on pattern compilation |
| Binary detection false positive | Text file incorrectly excluded | Provide override mechanism via explicit negation patterns |
| Binary detection false negative | Binary file incorrectly indexed | Accept as low-impact; index size increases but functionality preserved |

### Assumptions

1. The .gitignore format follows the specification documented in the Git manual and behavior matches git version 2.x
2. Most repositories will have existing .gitignore files that cover the majority of files that should be excluded
3. Binary file detection by extension covers 95%+ of binary files encountered in typical repositories
4. Magic number detection is only needed for files without extensions or with misleading extensions
5. Pattern matching performance is critical since every file enumeration triggers ignore checks
6. Users expect gitignore behavior to match git exactly - any deviation will cause confusion
7. The .agent/config.yml ignore patterns supplement rather than replace gitignore rules
8. Case sensitivity of pattern matching follows the operating system conventions

### Security Considerations

1. **Path Traversal Prevention** - Pattern matching must not allow patterns that could match outside the repository root directory
2. **Symbolic Link Handling** - Ignore checking must handle symlinks carefully to prevent escaping repository boundaries
3. **Denial of Service** - Maliciously crafted patterns with excessive backtracking must be rejected or timeout protected
4. **Sensitive File Exposure** - Binary detection must not read excessive file content; limit magic number inspection to first 512 bytes
5. **Configuration Injection** - Patterns from configuration must be validated to prevent regex injection if patterns are compiled to regex

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Gitignore | Git ignore file |
| Ignore Rule | Exclusion pattern |
| Glob Pattern | Wildcard matching |
| Negation | Re-include pattern |
| Precedence | Rule priority order |
| Binary File | Non-text file |
| Magic Number | Binary file signature |
| Pattern Matching | Glob evaluation |
| Anchor | Path-specific pattern |
| Directory Pattern | Match directories |
| Compiled Pattern | Optimized matcher |
| Cascade | Rule layering |
| Global Ignore | System-wide rules |
| Local Ignore | Directory rules |
| Inherit | Parent rules apply |

---

## Out of Scope

The following items are explicitly excluded from Task 015.a:

- **.gitattributes** - Not processed
- **Git LFS** - Not supported
- **Sparse checkout** - Not supported
- **Submodule ignores** - Limited support
- **Custom binary detection** - Magic numbers only
- **Regex patterns** - Glob only

---

## Functional Requirements

### Gitignore Parsing

| ID | Requirement |
|----|-------------|
| FR-015a-01 | The system MUST parse .gitignore files from the repository root and any subdirectories |
| FR-015a-02 | The parser MUST correctly handle comment lines beginning with # character |
| FR-015a-03 | The parser MUST skip blank lines without error |
| FR-015a-04 | The parser MUST handle trailing spaces in patterns correctly per git specification |
| FR-015a-05 | The parser MUST support escape characters for special characters in patterns |

### Pattern Syntax

| ID | Requirement |
|----|-------------|
| FR-015a-06 | Simple literal patterns MUST match files with exact names |
| FR-015a-07 | Single wildcard (*) MUST match any sequence of characters within a path component |
| FR-015a-08 | Double wildcard (**) MUST match zero or more directories in the path |
| FR-015a-09 | Question mark (?) MUST match exactly one character |
| FR-015a-10 | Character class ([abc], [a-z]) MUST match any single character in the class |
| FR-015a-11 | Negation patterns (!) MUST re-include previously excluded files |
| FR-015a-12 | Trailing slash (/) MUST indicate the pattern matches directories only |

### Pattern Matching

| ID | Requirement |
|----|-------------|
| FR-015a-13 | The matcher MUST support matching against file names only |
| FR-015a-14 | The matcher MUST support matching against full relative paths |
| FR-015a-15 | The matcher MUST correctly identify directory matches when pattern ends with slash |
| FR-015a-16 | Case sensitivity MUST follow the operating system conventions (case-insensitive on Windows) |
| FR-015a-17 | Path separators MUST be normalized to forward slashes internally |

### Multiple Gitignores

| ID | Requirement |
|----|-------------|
| FR-015a-18 | The root .gitignore MUST apply to all files in the repository |
| FR-015a-19 | Nested .gitignore files MUST apply only to their directory subtree |
| FR-015a-20 | Pattern precedence MUST follow git rules: later patterns override earlier ones |
| FR-015a-21 | Child directory patterns MUST inherit parent directory patterns |

### Additional Sources

| ID | Requirement |
|----|-------------|
| FR-015a-22 | The system MUST support additional ignore patterns from .agent/config.yml |
| FR-015a-23 | The system MUST support a global ignore file configured by the user |
| FR-015a-24 | The system MUST support command-line specified ignore patterns |
| FR-015a-25 | Source precedence MUST be: gitignore < global < config < command-line |

### Binary Detection

| ID | Requirement |
|----|-------------|
| FR-015a-26 | The system MUST detect binary files by file extension |
| FR-015a-27 | The system MUST detect binary files by magic number signature in file header |
| FR-015a-28 | Detected binary files MUST be automatically added to the ignore list |
| FR-015a-29 | Binary detection MUST be overridable via explicit negation patterns |

### API

| ID | Requirement |
|----|-------------|
| FR-015a-30 | IsIgnored(path) MUST return true if the path matches any ignore pattern |
| FR-015a-31 | GetIgnores() MUST return all currently active ignore patterns with their sources |
| FR-015a-32 | AddIgnore(pattern, source) MUST add a new pattern to the ignore list at runtime |
| FR-015a-33 | Refresh() MUST reload all ignore patterns from disk sources |

### Performance

| ID | Requirement |
|----|-------------|
| FR-015a-34 | Patterns MUST be compiled to optimized form on first load |
| FR-015a-35 | IsIgnored results MUST be cached for repeated calls with the same path |
| FR-015a-36 | Ignore sources MUST be loaded lazily when first accessed |
| FR-015a-37 | The API MUST support batch checking of multiple paths in a single call |

---

## Non-Functional Requirements

### Performance

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015a-01 | Performance | Single IsIgnored() check MUST complete in less than 1ms average |
| NFR-015a-02 | Performance | Batch checking of 1,000 paths MUST complete in less than 50ms |
| NFR-015a-03 | Performance | Loading and compiling all ignore patterns MUST complete in less than 50ms |

### Reliability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015a-04 | Reliability | Invalid patterns MUST be skipped with warning rather than failing the entire parse |
| NFR-015a-05 | Reliability | Missing .gitignore files MUST be handled gracefully without errors |
| NFR-015a-06 | Reliability | Non-UTF-8 encoded gitignore files MUST be handled with fallback encoding |

### Compatibility

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015a-07 | Compatibility | Pattern matching behavior MUST match git's behavior for all documented patterns |
| NFR-015a-08 | Compatibility | Path handling MUST work correctly on Windows, macOS, and Linux |
| NFR-015a-09 | Compatibility | Case sensitivity MUST respect operating system file system conventions |

### Maintainability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015a-10 | Maintainability | The ignore service MUST have a clear interface for extension with new pattern sources |
| NFR-015a-11 | Maintainability | Pattern matching logic MUST be unit testable in isolation from file system |
| NFR-015a-12 | Maintainability | All gitignore parsing edge cases MUST be covered by unit tests |

### Observability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015a-13 | Observability | Pattern compilation failures MUST be logged with file and line number |
| NFR-015a-14 | Observability | Cache hit/miss ratios MUST be available for performance monitoring |
| NFR-015a-15 | Observability | The ignore check command MUST show which pattern caused a file to be ignored |

---

## User Manual Documentation

### Overview

The ignore system filters files from indexing. It uses gitignore patterns and additional configuration.

### Gitignore Patterns

Standard gitignore syntax is supported:

```gitignore
# Comments start with #

# Ignore all .log files
*.log

# Ignore build directory
build/

# Ignore node_modules anywhere
**/node_modules

# But not this specific file
!important.log

# Ignore files in root only
/config.local.json

# Ignore .env in any directory
**/.env
```

### Configuration

```yaml
# .agent/config.yml
index:
  ignore:
    # Additional patterns
    patterns:
      - "*.generated.cs"
      - ".idea/**"
      - ".vs/**"
      
    # Use gitignore (default: true)
    use_gitignore: true
    
    # Global ignore file
    global_file: ~/.config/acode/ignore
    
    # Auto-ignore binaries (default: true)
    auto_ignore_binaries: true
```

### Binary File Detection

These extensions are auto-ignored:
- Images: .png, .jpg, .gif, .ico, .svg
- Archives: .zip, .tar, .gz, .7z
- Binaries: .exe, .dll, .so, .dylib
- Media: .mp3, .mp4, .wav
- Office: .pdf, .doc, .xls

### Checking Ignores

```bash
$ acode ignore check src/Program.cs

src/Program.cs: NOT ignored

$ acode ignore check node_modules/lodash/package.json

node_modules/lodash/package.json: IGNORED
  Matched: **/node_modules

$ acode ignore list

Ignore Rules
────────────────────
Source: .gitignore
  node_modules/
  *.log
  build/
  
Source: .agent/config.yml
  *.generated.cs
  
Source: auto-binary
  (37 extensions)
```

### Troubleshooting

#### File Not Ignored

**Problem:** File should be ignored but isn't

**Solutions:**
1. Check pattern syntax
2. Check pattern order (later wins)
3. Check for negation patterns

#### File Unexpectedly Ignored

**Problem:** File is ignored but shouldn't be

**Solutions:**
1. Check parent .gitignore files
2. Use `acode ignore check <path>`
3. Add negation pattern: `!path/to/file`

#### Binary Not Detected

**Problem:** Binary file is being indexed

**Solutions:**
1. Add extension to config
2. Or add explicit pattern

---

## Acceptance Criteria

### Parsing

- [ ] AC-001: .gitignore parsed
- [ ] AC-002: Comments handled
- [ ] AC-003: Blank lines handled

### Patterns

- [ ] AC-004: Simple patterns work
- [ ] AC-005: Wildcards work
- [ ] AC-006: Negation works
- [ ] AC-007: Directories work

### Sources

- [ ] AC-008: Config ignores work
- [ ] AC-009: Nested gitignores work
- [ ] AC-010: Precedence correct

### Binary

- [ ] AC-011: Extensions detected
- [ ] AC-012: Magic numbers detected
- [ ] AC-013: Auto-ignore works

### API

- [ ] AC-014: IsIgnored works
- [ ] AC-015: GetIgnores works
- [ ] AC-016: Check command works

---

## Best Practices

### Gitignore Parsing

1. **Follow git semantics exactly** - Match git's behavior for edge cases
2. **Support nested gitignores** - Respect .gitignore at each directory level
3. **Handle negation patterns** - Support ! prefix to un-ignore files
4. **Cache parsed rules** - Parse .gitignore once per directory

### Rule Evaluation

5. **Evaluate in order** - Later rules override earlier; last match wins
6. **Use anchoring correctly** - Leading / anchors to root; trailing / matches directories only
7. **Support glob patterns** - *, **, ?, and character classes
8. **Handle directory vs file** - Distinguish between dir and file for trailing slash rules

### Performance

9. **Minimize filesystem calls** - Batch path existence checks
10. **Pre-compile patterns** - Convert globs to regex once, reuse
11. **Short-circuit evaluation** - Skip expensive patterns if already matched
12. **Cache decisions** - Remember ignore status for paths already evaluated

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Ignore/
├── GitignoreParserTests.cs
│   ├── Should_Parse_Empty_File()
│   ├── Should_Parse_Comment_Lines()
│   ├── Should_Parse_Blank_Lines()
│   ├── Should_Parse_Simple_Pattern()
│   ├── Should_Parse_Directory_Pattern()
│   ├── Should_Parse_Negation_Pattern()
│   ├── Should_Parse_Escaped_Characters()
│   ├── Should_Parse_Trailing_Spaces()
│   ├── Should_Handle_BOM()
│   ├── Should_Handle_Different_Line_Endings()
│   └── Should_Return_Line_Numbers()
│
├── PatternMatcherTests.cs
│   ├── Should_Match_Exact_Filename()
│   ├── Should_Match_Single_Wildcard()
│   ├── Should_Match_Double_Wildcard()
│   ├── Should_Match_Question_Mark()
│   ├── Should_Match_Character_Class()
│   ├── Should_Match_Negated_Class()
│   ├── Should_Match_Directory_Only()
│   ├── Should_Match_Rooted_Pattern()
│   ├── Should_Match_Deeply_Nested()
│   ├── Should_Handle_Case_Sensitivity()
│   ├── Should_Handle_Leading_Slash()
│   ├── Should_Handle_Trailing_Slash()
│   └── Should_Not_Match_Non_Matching()
│
├── NegationTests.cs
│   ├── Should_Negate_Previous_Pattern()
│   ├── Should_Handle_Multiple_Negations()
│   ├── Should_Apply_Order_Correctly()
│   ├── Should_Handle_Directory_Negation()
│   └── Should_Handle_Nested_Negation()
│
├── BinaryDetectorTests.cs
│   ├── Should_Detect_Image_Extensions()
│   ├── Should_Detect_Archive_Extensions()
│   ├── Should_Detect_Executable_Extensions()
│   ├── Should_Detect_Media_Extensions()
│   ├── Should_Detect_Office_Extensions()
│   ├── Should_Detect_By_Magic_Number()
│   ├── Should_Detect_ELF_Binary()
│   ├── Should_Detect_PE_Binary()
│   ├── Should_Detect_Mach_O_Binary()
│   ├── Should_Handle_Text_File()
│   ├── Should_Handle_Unknown_Extension()
│   └── Should_Handle_No_Extension()
│
├── IgnoreServiceTests.cs
│   ├── Should_Load_Gitignore()
│   ├── Should_Load_Nested_Gitignores()
│   ├── Should_Load_Config_Ignores()
│   ├── Should_Load_Global_Ignores()
│   ├── Should_Combine_All_Sources()
│   ├── Should_Apply_Precedence_Order()
│   ├── Should_Cache_Results()
│   ├── Should_Refresh_On_Demand()
│   ├── Should_Handle_Missing_Gitignore()
│   └── Should_Handle_Invalid_Pattern()
│
└── IgnoreCacheTests.cs
    ├── Should_Cache_Check_Results()
    ├── Should_Invalidate_On_Pattern_Change()
    ├── Should_Invalidate_On_Refresh()
    └── Should_Handle_Large_Path_Count()
```

### Integration Tests

```
Tests/Integration/Ignore/
├── IgnoreIntegrationTests.cs
│   ├── Should_Work_With_Real_Gitignore()
│   ├── Should_Work_With_Nested_Directories()
│   ├── Should_Handle_Large_Gitignore()
│   ├── Should_Handle_Many_Ignore_Files()
│   └── Should_Work_With_Index_Build()
│
└── BinaryIntegrationTests.cs
    ├── Should_Detect_Real_Binaries()
    └── Should_Not_False_Positive()
```

### E2E Tests

```
Tests/E2E/Ignore/
├── IgnoreE2ETests.cs
│   ├── Should_Filter_Index_Build()
│   ├── Should_Filter_Search_Results()
│   ├── Should_Show_Check_Command()
│   └── Should_Show_List_Command()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Single check | 0.5ms | 1ms |
| Batch 1K | 25ms | 50ms |
| Load ignores | 25ms | 50ms |

---

## User Verification Steps

### Scenario 1: Gitignore

1. Create .gitignore with patterns
2. Check ignored file
3. Verify: Matched

### Scenario 2: Negation

1. Add pattern then negation
2. Check negated file
3. Verify: Not ignored

### Scenario 3: Config Ignore

1. Add pattern to config
2. Check file
3. Verify: Ignored

### Scenario 4: Binary

1. Check .exe file
2. Verify: Auto-ignored

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Ignore/
│   ├── IIgnoreService.cs
│   ├── IgnorePattern.cs
│   └── IgnoreSource.cs
│
src/AgenticCoder.Infrastructure/
├── Ignore/
│   ├── IgnoreService.cs
│   ├── GitignoreParser.cs
│   ├── PatternMatcher.cs
│   └── BinaryDetector.cs
│
src/AgenticCoder.CLI/
└── Commands/
    └── IgnoreCommand.cs
```

### IIgnoreService Interface

```csharp
namespace AgenticCoder.Domain.Ignore;

public interface IIgnoreService
{
    bool IsIgnored(string path);
    IReadOnlyList<IgnorePattern> GetPatterns();
    void AddPattern(string pattern, IgnoreSource source);
    void Refresh();
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-IGN-001 | Invalid pattern |
| ACODE-IGN-002 | Parse error |
| ACODE-IGN-003 | File not found |

### Implementation Checklist

1. [ ] Create gitignore parser
2. [ ] Create pattern matcher
3. [ ] Create binary detector
4. [ ] Create ignore service
5. [ ] Combine sources
6. [ ] Add caching
7. [ ] Add CLI command
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Parser
2. **Phase 2:** Matcher
3. **Phase 3:** Binary detection
4. **Phase 4:** Service
5. **Phase 5:** CLI

---

**End of Task 015.a Specification**