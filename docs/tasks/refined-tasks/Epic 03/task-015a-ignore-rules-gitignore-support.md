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

- FR-001: Parse .gitignore files
- FR-002: Handle comments (#)
- FR-003: Handle blank lines
- FR-004: Handle trailing spaces
- FR-005: Handle escape characters

### Pattern Syntax

- FR-006: Simple patterns MUST work
- FR-007: Wildcard (*) MUST work
- FR-008: Double wildcard (**) MUST work
- FR-009: Question mark (?) MUST work
- FR-010: Character class ([]) MUST work
- FR-011: Negation (!) MUST work
- FR-012: Directory slash (/) MUST work

### Pattern Matching

- FR-013: Match file names
- FR-014: Match full paths
- FR-015: Match directories
- FR-016: Case handling per OS
- FR-017: Slash normalization

### Multiple Gitignores

- FR-018: Root .gitignore MUST work
- FR-019: Nested .gitignore MUST work
- FR-020: Precedence correct
- FR-021: Inherit parent rules

### Additional Sources

- FR-022: Config ignores MUST work
- FR-023: Global ignores MUST work
- FR-024: Command-line ignores MUST work
- FR-025: Source precedence correct

### Binary Detection

- FR-026: Detect by extension
- FR-027: Detect by magic number
- FR-028: Auto-ignore binaries
- FR-029: Override detection

### API

- FR-030: IsIgnored(path) MUST work
- FR-031: GetIgnores() MUST work
- FR-032: AddIgnore() MUST work
- FR-033: Refresh() MUST work

### Performance

- FR-034: Compile patterns
- FR-035: Cache results
- FR-036: Lazy loading
- FR-037: Batch checking

---

## Non-Functional Requirements

### Performance

- NFR-001: Single check < 1ms
- NFR-002: Batch 1K < 50ms
- NFR-003: Load ignores < 50ms

### Reliability

- NFR-004: Invalid patterns handled
- NFR-005: Missing files handled
- NFR-006: Encoding handled

### Compatibility

- NFR-007: Match git behavior
- NFR-008: Cross-platform paths
- NFR-009: Case sensitivity

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