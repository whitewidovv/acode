# Task 019.a: Detect Solution/Package Layouts

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 019 (Language Runners), Task 014 (RepoFS)  

---

## Description

### Overview

Task 019.a implements intelligent project layout detection for the Agentic Coding Bot's language runner subsystem. Before the agent can build, test, or run code, it must understand the repository structure—where solution files live, which projects are test projects, what Node.js packages exist, and how they relate to each other. This subtask creates the scanning, parsing, and caching infrastructure that transforms a raw directory tree into a rich semantic model of the codebase.

### Business Value

1. **Zero-Configuration Operation**: With accurate layout detection, the agent can work with any supported repository without explicit configuration, dramatically reducing setup time
2. **Intelligent Command Targeting**: Understanding which projects are tests vs. libraries vs. applications enables the agent to run appropriate commands automatically
3. **Dependency Awareness**: Knowing project references allows the agent to build in correct order and understand impact of changes
4. **Monorepo Support**: Modern codebases often contain multiple projects; detection handles workspaces, multi-solution repos, and nested structures
5. **Performance Optimization**: Cached detection results mean repeated operations don't re-scan the entire repository

### Scope

This task delivers:

1. **Layout Detector Interface**: Clean abstraction for detecting project layouts with async support and cancellation
2. **Detection Result Model**: Rich representation of discovered projects with types, relationships, and metadata
3. **.NET Detection**: Solution file (.sln) parsing, project file (.csproj, .fsproj) analysis, test project identification
4. **Node.js Detection**: package.json parsing, workspace detection (npm, yarn, pnpm), entry point identification
5. **Test Project Identification**: Heuristics to detect test projects via naming, SDK references, and test framework dependencies
6. **Caching Layer**: Detection results cached with invalidation on file changes and time-based expiry
7. **CLI Command**: `acode detect` command for manual layout inspection and debugging

### Integration Points

| Component | Integration Type | Purpose |
|-----------|------------------|---------|
| Task-014 (RepoFS) | Dependency | File system access abstraction |
| Task-019 (Language Runners) | Upstream consumer | Uses detection to select appropriate runner |
| Task-019b (Test Wrapper) | Consumer | Uses detection to find test projects |
| Task-019c (Repo Contract) | Related | Detection fallback when contract not specified |
| Task-002a (Config Schema) | Configuration | Detection settings in config.yml |
| Task-005 (CLI Architecture) | Integration | CLI detect command |

### Failure Modes

| Failure | Detection | Recovery |
|---------|-----------|----------|
| Malformed .sln file | Parse exception | Log error, skip file, continue with other detections |
| Invalid .csproj XML | XmlException | Log error, return partial project info |
| Corrupt package.json | JsonException | Log error, skip package, continue |
| Access denied to directory | IOException | Log warning, skip directory, continue |
| Detection timeout | CancellationToken | Return partial results with warning |
| Cache corruption | Read error | Clear cache, perform fresh detection |

### Assumptions

1. Repository uses standard .NET SDK-style projects (not legacy project.json)
2. Node.js projects use standard package.json format
3. File system is accessible via Task-014 RepoFS abstraction
4. Detection depth is bounded to prevent excessive scanning
5. Binary files and build outputs are excluded from scanning

### Security Considerations

1. **Path Validation**: All paths validated to prevent traversal outside repository
2. **File Size Limits**: Large files (> 1MB) skipped to prevent memory exhaustion
3. **Symlink Handling**: Symlinks resolved but validated to prevent circular references
4. **No Code Execution**: Detection is passive—only file reading, never execution

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Layout | Project file structure |
| Solution | .NET .sln file |
| Project | Compilable unit |
| Manifest | package.json, .csproj |
| Monorepo | Multiple projects in repo |
| Workspace | npm/yarn workspace |
| Entry Point | Main executable |
| Test Project | Unit test project |
| src | Source directory convention |
| tests | Test directory convention |
| Glob | File pattern matching |
| Cache | Stored detection result |

---

## Out of Scope

The following items are explicitly excluded from Task 019.a:

- **Build execution** - See Task 019
- **Test execution** - See Task 019.b
- **Repo contract** - See Task 019.c
- **Python/Go detection** - Future versions
- **IDE project files** - .sln and package.json only
- **Docker detection** - Separate concern
- **CI/CD detection** - Separate concern

---

## Functional Requirements

### Detection Interface (FR-019A-01 to FR-019A-12)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019A-01 | Define `ILayoutDetector` interface | Must Have |
| FR-019A-02 | `DetectAsync` method accepting root path and options | Must Have |
| FR-019A-03 | Return `DetectionResult` containing all discovered projects | Must Have |
| FR-019A-04 | Support `CancellationToken` for operation cancellation | Must Have |
| FR-019A-05 | Accept `DetectionOptions` for controlling scan behavior | Must Have |
| FR-019A-06 | Support limiting scan depth | Should Have |
| FR-019A-07 | Support custom file patterns | Should Have |
| FR-019A-08 | Support custom skip directories | Should Have |
| FR-019A-09 | Support force refresh (bypass cache) | Must Have |
| FR-019A-10 | Support multiple concurrent detections (thread-safe) | Should Have |
| FR-019A-11 | Report progress during long scans | Could Have |
| FR-019A-12 | Return partial results if cancelled | Should Have |

### Detection Result Model (FR-019A-13 to FR-019A-28)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019A-13 | Define `DetectionResult` sealed record | Must Have |
| FR-019A-14 | Include list of all detected projects | Must Have |
| FR-019A-15 | Include list of detected solutions | Must Have |
| FR-019A-16 | Include detection timestamp | Must Have |
| FR-019A-17 | Include cache validity information | Should Have |
| FR-019A-18 | Include scan duration | Should Have |
| FR-019A-19 | Include total files scanned count | Should Have |
| FR-019A-20 | Include any errors encountered | Must Have |
| FR-019A-21 | Include repository root path | Must Have |
| FR-019A-22 | Support querying projects by type | Should Have |
| FR-019A-23 | Support querying test projects | Should Have |
| FR-019A-24 | Support querying by path | Should Have |
| FR-019A-25 | Include project dependency graph | Should Have |
| FR-019A-26 | Result is immutable after creation | Must Have |
| FR-019A-27 | Result serializable to JSON | Should Have |
| FR-019A-28 | Include detection method (cached vs fresh) | Should Have |

### Project Info Model (FR-019A-29 to FR-019A-48)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019A-29 | Define `ProjectInfo` sealed record | Must Have |
| FR-019A-30 | Include relative path from repo root | Must Have |
| FR-019A-31 | Include project type enum | Must Have |
| FR-019A-32 | Include human-readable project name | Must Have |
| FR-019A-33 | Include `IsTestProject` boolean | Must Have |
| FR-019A-34 | Include entry point path (if applicable) | Should Have |
| FR-019A-35 | Include target framework (for .NET) | Should Have |
| FR-019A-36 | Include list of project references | Should Have |
| FR-019A-37 | Include list of package references | Could Have |
| FR-019A-38 | Include list of available scripts (for Node) | Should Have |
| FR-019A-39 | Include output type (library, exe, web) | Should Have |
| FR-019A-40 | Include parent solution path (if applicable) | Should Have |
| FR-019A-41 | Include SDK version used | Should Have |
| FR-019A-42 | Include detected test framework | Should Have |
| FR-019A-43 | Include source directories | Could Have |
| FR-019A-44 | Include workspace name (for monorepos) | Should Have |
| FR-019A-45 | Include build configuration (Debug/Release) | Could Have |
| FR-019A-46 | Include publish profiles | Could Have |
| FR-019A-47 | Implement equality based on path | Should Have |
| FR-019A-48 | Support custom metadata dictionary | Should Have |

### .NET Detection (FR-019A-49 to FR-019A-70)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019A-49 | Find all .sln files in repository | Must Have |
| FR-019A-50 | Parse .sln file format to extract project references | Must Have |
| FR-019A-51 | Find standalone .csproj files (not in solution) | Should Have |
| FR-019A-52 | Find standalone .fsproj files | Should Have |
| FR-019A-53 | Find .vbproj files | Could Have |
| FR-019A-54 | Parse .csproj XML to extract properties | Must Have |
| FR-019A-55 | Extract `TargetFramework` and `TargetFrameworks` | Must Have |
| FR-019A-56 | Extract `OutputType` (Library, Exe, WinExe) | Should Have |
| FR-019A-57 | Extract `ProjectReference` elements | Must Have |
| FR-019A-58 | Extract `PackageReference` elements | Should Have |
| FR-019A-59 | Identify `Microsoft.NET.Sdk` version | Should Have |
| FR-019A-60 | Identify `Microsoft.NET.Sdk.Web` projects | Should Have |
| FR-019A-61 | Identify `Microsoft.NET.Sdk.Worker` projects | Should Have |
| FR-019A-62 | Build project dependency graph from references | Should Have |
| FR-019A-63 | Resolve relative paths in project references | Must Have |
| FR-019A-64 | Handle Directory.Build.props inheritance | Could Have |
| FR-019A-65 | Handle Directory.Packages.props | Could Have |
| FR-019A-66 | Detect implicit usings and nullable context | Could Have |
| FR-019A-67 | Handle multi-targeting (TargetFrameworks) | Should Have |
| FR-019A-68 | Detect executable entry point (Program.cs, Main) | Should Have |
| FR-019A-69 | Parse solution folders and project hierarchy | Should Have |
| FR-019A-70 | Handle nested solution structure | Should Have |

### Test Project Detection (FR-019A-71 to FR-019A-82)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019A-71 | Check project name ends with `.Tests` | Must Have |
| FR-019A-72 | Check project name ends with `.Test` | Must Have |
| FR-019A-73 | Check project name ends with `.UnitTests` | Should Have |
| FR-019A-74 | Check project name ends with `.IntegrationTests` | Should Have |
| FR-019A-75 | Check for `IsTestProject` property in .csproj | Must Have |
| FR-019A-76 | Check for `Microsoft.NET.Test.Sdk` reference | Must Have |
| FR-019A-77 | Check for xUnit package reference | Must Have |
| FR-019A-78 | Check for NUnit package reference | Must Have |
| FR-019A-79 | Check for MSTest package reference | Must Have |
| FR-019A-80 | Store detected test framework in ProjectInfo | Should Have |
| FR-019A-81 | Check for TUnit package reference | Could Have |
| FR-019A-82 | Check for `testscript` in package.json | Must Have |

### Node.js Detection (FR-019A-83 to FR-019A-102)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019A-83 | Find all package.json files in repository | Must Have |
| FR-019A-84 | Parse package.json to extract metadata | Must Have |
| FR-019A-85 | Extract `name` property | Must Have |
| FR-019A-86 | Extract `version` property | Should Have |
| FR-019A-87 | Extract `main` entry point | Should Have |
| FR-019A-88 | Extract `module` entry point | Should Have |
| FR-019A-89 | Extract `types` or `typings` path | Should Have |
| FR-019A-90 | Extract `scripts` object | Must Have |
| FR-019A-91 | Identify `test` script presence | Must Have |
| FR-019A-92 | Identify `build` script presence | Should Have |
| FR-019A-93 | Identify `start` script presence | Should Have |
| FR-019A-94 | Detect npm workspaces configuration | Must Have |
| FR-019A-95 | Detect yarn workspaces configuration | Should Have |
| FR-019A-96 | Detect pnpm workspaces configuration | Should Have |
| FR-019A-97 | Detect lerna configuration | Should Have |
| FR-019A-98 | Map workspace structure with child packages | Must Have |
| FR-019A-99 | Handle nested package.json (workspaces) | Must Have |
| FR-019A-100 | Skip package.json inside node_modules | Must Have |
| FR-019A-101 | Detect TypeScript presence (tsconfig.json) | Should Have |
| FR-019A-102 | Identify application vs library packages | Should Have |

### Caching (FR-019A-103 to FR-019A-115)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019A-103 | Cache detection results in memory | Must Have |
| FR-019A-104 | Cache detection results to disk | Should Have |
| FR-019A-105 | Configurable cache TTL | Must Have |
| FR-019A-106 | Invalidate cache on relevant file changes | Must Have |
| FR-019A-107 | Watch for .sln file changes | Should Have |
| FR-019A-108 | Watch for .csproj file changes | Should Have |
| FR-019A-109 | Watch for package.json file changes | Should Have |
| FR-019A-110 | Force refresh option bypasses cache | Must Have |
| FR-019A-111 | Cache stores detection timestamp | Must Have |
| FR-019A-112 | Cache includes file checksums for validation | Could Have |
| FR-019A-113 | Concurrent reads safe | Must Have |
| FR-019A-114 | Single write at a time | Must Have |
| FR-019A-115 | Clear cache via API or CLI | Should Have |

### Scanning (FR-019A-116 to FR-019A-125)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019A-116 | Recursive directory scanning | Must Have |
| FR-019A-117 | Respect .gitignore patterns | Should Have |
| FR-019A-118 | Configurable maximum scan depth | Must Have |
| FR-019A-119 | Skip node_modules by default | Must Have |
| FR-019A-120 | Skip bin directories by default | Must Have |
| FR-019A-121 | Skip obj directories by default | Must Have |
| FR-019A-122 | Skip .git directory by default | Must Have |
| FR-019A-123 | Custom skip patterns configurable | Should Have |
| FR-019A-124 | Glob pattern matching for file search | Should Have |
| FR-019A-125 | Report progress during long scans | Could Have |

---

## Non-Functional Requirements

### Performance (NFR-019A-01 to NFR-019A-12)

| ID | Requirement | Target | Maximum |
|----|-------------|--------|---------|
| NFR-019A-01 | Initial scan (1000 files) | 300ms | 500ms |
| NFR-019A-02 | Initial scan (10000 files) | 1.5s | 3s |
| NFR-019A-03 | Cached lookup | 2ms | 5ms |
| NFR-019A-04 | Incremental update (10 changed files) | 50ms | 100ms |
| NFR-019A-05 | Parse single .sln file | 10ms | 20ms |
| NFR-019A-06 | Parse single .csproj file | 5ms | 15ms |
| NFR-019A-07 | Parse single package.json | 2ms | 5ms |
| NFR-019A-08 | Memory usage (1000 projects) | 10MB | 25MB |
| NFR-019A-09 | Cache file size (1000 projects) | 100KB | 500KB |
| NFR-019A-10 | Directory enumeration per directory | 1ms | 5ms |
| NFR-019A-11 | File pattern matching | 0.1ms | 0.5ms |
| NFR-019A-12 | Dependency graph construction | 50ms | 150ms |

### Reliability (NFR-019A-13 to NFR-019A-20)

| ID | Requirement | Description |
|----|-------------|-------------|
| NFR-019A-13 | Partial results on error | Continue detection despite individual file errors |
| NFR-019A-14 | Clear error messages | Describe exactly what failed and why |
| NFR-019A-15 | No crash on malformed files | Handle corrupt XML/JSON gracefully |
| NFR-019A-16 | Cache corruption recovery | Detect and clear corrupted cache automatically |
| NFR-019A-17 | Concurrent access safety | Multiple threads can read detection results |
| NFR-019A-18 | Cancellation support | Clean shutdown when cancelled |
| NFR-019A-19 | Symlink loop detection | Prevent infinite loops from circular symlinks |
| NFR-019A-20 | Permission handling | Skip inaccessible files with warning |

### Accuracy (NFR-019A-21 to NFR-019A-28)

| ID | Requirement | Description |
|----|-------------|-------------|
| NFR-019A-21 | 100% solution discovery | All .sln files in scannable paths found |
| NFR-019A-22 | 100% project discovery | All .csproj/.fsproj files in solutions found |
| NFR-019A-23 | 100% package discovery | All package.json outside node_modules found |
| NFR-019A-24 | Correct type classification | Projects correctly typed (library, exe, test) |
| NFR-019A-25 | Accurate test detection | 98%+ test projects correctly identified |
| NFR-019A-26 | Correct reference resolution | All project references resolved accurately |
| NFR-019A-27 | Workspace mapping | All workspace members correctly mapped |
| NFR-019A-28 | Framework detection | Target framework accurately extracted |

### Maintainability (NFR-019A-29 to NFR-019A-34)

| ID | Requirement | Description |
|----|-------------|-------------|
| NFR-019A-29 | Interface-driven design | All detectors behind interfaces |
| NFR-019A-30 | Strategy pattern for parsers | Easy to add new project types |
| NFR-019A-31 | Configuration-driven behavior | Scan patterns configurable without code |
| NFR-019A-32 | Comprehensive logging | Detection steps traceable in logs |
| NFR-019A-33 | Unit testable parsers | Each parser testable in isolation |
| NFR-019A-34 | Mock file system support | Uses RepoFS for testability |

### Observability (NFR-019A-35 to NFR-019A-40)

| ID | Requirement | Description |
|----|-------------|-------------|
| NFR-019A-35 | Metrics: scan duration | Track time for each detection |
| NFR-019A-36 | Metrics: files scanned | Track number of files processed |
| NFR-019A-37 | Metrics: projects found | Track count by type |
| NFR-019A-38 | Metrics: cache hit rate | Track cache effectiveness |
| NFR-019A-39 | Debug logging | Detailed logs at debug level |
| NFR-019A-40 | Structured events | Detection events for telemetry |

---

## User Manual Documentation

### Overview

Layout detection finds project files and understands repository structure. It identifies .NET solutions, Node.js packages, and their relationships.

### Configuration

```yaml
# .agent/config.yml
detection:
  # Enable auto-detection
  enabled: true
  
  # Detection cache TTL (seconds)
  cache_ttl_seconds: 300
  
  # Maximum scan depth
  max_depth: 10
  
  # Directories to skip
  skip_directories:
    - node_modules
    - bin
    - obj
    - dist
    - .git
    
  # File patterns to find
  patterns:
    dotnet:
      - "*.sln"
      - "*.csproj"
      - "*.fsproj"
    node:
      - "package.json"
```

### CLI Commands

```bash
# Detect project layout
acode detect

# Force refresh (ignore cache)
acode detect --refresh

# Detect specific path
acode detect ./src

# Show detailed output
acode detect --verbose

# Output as JSON
acode detect --json
```

### Detection Output

```bash
$ acode detect

Repository Layout
─────────────────

.NET Projects:
  MyApp.sln (Solution)
  ├── src/MyApp/MyApp.csproj
  │   Type: Library
  │   Framework: net8.0
  │   References: MyApp.Core
  ├── src/MyApp.Core/MyApp.Core.csproj
  │   Type: Library
  │   Framework: net8.0
  └── tests/MyApp.Tests/MyApp.Tests.csproj
      Type: Test (xUnit)
      Framework: net8.0
      References: MyApp

Node.js Projects:
  frontend/package.json
  ├── Name: @myapp/frontend
  │   Type: Application
  │   Entry: src/index.ts
  │   Scripts: build, test, start
  └── Workspaces: None

Summary:
  .NET: 1 solution, 3 projects (1 test)
  Node: 1 package
```

### JSON Output

```json
{
  "detectedAt": "2024-01-15T10:30:00Z",
  "projects": [
    {
      "path": "MyApp.sln",
      "type": "dotnet-solution",
      "name": "MyApp",
      "projects": ["src/MyApp/MyApp.csproj", "..."]
    },
    {
      "path": "src/MyApp/MyApp.csproj",
      "type": "dotnet-project",
      "name": "MyApp",
      "isTest": false,
      "framework": "net8.0",
      "references": ["MyApp.Core"]
    },
    {
      "path": "frontend/package.json",
      "type": "node-package",
      "name": "@myapp/frontend",
      "isTest": false,
      "entry": "src/index.ts",
      "scripts": ["build", "test", "start"]
    }
  ]
}
```

### Troubleshooting

#### Projects Not Found

**Problem:** Some projects not detected

**Solutions:**
1. Check file extensions
2. Increase max_depth
3. Check skip_directories
4. Verify file permissions

#### Slow Detection

**Problem:** Scanning takes too long

**Solutions:**
1. Reduce max_depth
2. Add more skip_directories
3. Exclude large folders
4. Use cached results

#### Cache Stale

**Problem:** New projects not showing

**Solutions:**
1. Use --refresh flag
2. Reduce cache_ttl_seconds
3. Delete cache manually

---

## Acceptance Criteria

### Detection Interface (AC-019A-01 to AC-019A-10)

- [ ] AC-019A-01: `ILayoutDetector` interface defined with `DetectAsync` method
- [ ] AC-019A-02: `DetectAsync` accepts root path and returns `DetectionResult`
- [ ] AC-019A-03: `DetectionOptions` supports scan depth limit
- [ ] AC-019A-04: `DetectionOptions` supports custom skip directories
- [ ] AC-019A-05: `DetectionOptions` supports force refresh flag
- [ ] AC-019A-06: Detection supports `CancellationToken`
- [ ] AC-019A-07: Cancelled detection returns partial results
- [ ] AC-019A-08: Detection is thread-safe for concurrent calls
- [ ] AC-019A-09: Detection uses RepoFS abstraction (not direct file system)
- [ ] AC-019A-10: Detection logs progress at debug level

### Detection Result (AC-019A-11 to AC-019A-22)

- [ ] AC-019A-11: `DetectionResult` is sealed record (immutable)
- [ ] AC-019A-12: Result includes list of all detected projects
- [ ] AC-019A-13: Result includes detection timestamp
- [ ] AC-019A-14: Result includes cache status (fresh vs cached)
- [ ] AC-019A-15: Result includes scan duration
- [ ] AC-019A-16: Result includes files scanned count
- [ ] AC-019A-17: Result includes any errors encountered
- [ ] AC-019A-18: Result supports query by project type
- [ ] AC-019A-19: Result supports query for test projects only
- [ ] AC-019A-20: Result supports query by path prefix
- [ ] AC-019A-21: Result serializable to JSON
- [ ] AC-019A-22: Result includes dependency graph

### Project Info (AC-019A-23 to AC-019A-35)

- [ ] AC-019A-23: `ProjectInfo` is sealed record (immutable)
- [ ] AC-019A-24: ProjectInfo includes relative path from repo root
- [ ] AC-019A-25: ProjectInfo includes project type enum
- [ ] AC-019A-26: ProjectInfo includes human-readable name
- [ ] AC-019A-27: ProjectInfo includes `IsTestProject` flag
- [ ] AC-019A-28: ProjectInfo includes entry point path (when applicable)
- [ ] AC-019A-29: ProjectInfo includes target framework (for .NET)
- [ ] AC-019A-30: ProjectInfo includes project references list
- [ ] AC-019A-31: ProjectInfo includes available scripts (for Node)
- [ ] AC-019A-32: ProjectInfo includes output type (library, exe, web)
- [ ] AC-019A-33: ProjectInfo includes detected test framework name
- [ ] AC-019A-34: ProjectInfo includes parent solution path (when applicable)
- [ ] AC-019A-35: ProjectInfo implements equality based on path

### .NET Detection (AC-019A-36 to AC-019A-52)

- [ ] AC-019A-36: All .sln files discovered
- [ ] AC-019A-37: .sln file parsed to extract project references
- [ ] AC-019A-38: Solution folder hierarchy captured
- [ ] AC-019A-39: All .csproj files in solutions discovered
- [ ] AC-019A-40: Standalone .csproj files discovered
- [ ] AC-019A-41: All .fsproj files discovered
- [ ] AC-019A-42: .csproj XML parsed correctly
- [ ] AC-019A-43: `TargetFramework` extracted
- [ ] AC-019A-44: `TargetFrameworks` (multi-targeting) extracted
- [ ] AC-019A-45: `OutputType` extracted
- [ ] AC-019A-46: `ProjectReference` elements extracted
- [ ] AC-019A-47: SDK type detected (Sdk, Sdk.Web, Sdk.Worker)
- [ ] AC-019A-48: Project references resolved to absolute paths
- [ ] AC-019A-49: Dependency graph built from references
- [ ] AC-019A-50: Entry point detection for executable projects
- [ ] AC-019A-51: Projects linked to parent solution
- [ ] AC-019A-52: Nested solution structures handled

### Test Project Detection (AC-019A-53 to AC-019A-62)

- [ ] AC-019A-53: Projects ending with `.Tests` marked as test
- [ ] AC-019A-54: Projects ending with `.Test` marked as test
- [ ] AC-019A-55: `IsTestProject` MSBuild property detected
- [ ] AC-019A-56: `Microsoft.NET.Test.Sdk` reference detected
- [ ] AC-019A-57: xUnit package reference detected and recorded
- [ ] AC-019A-58: NUnit package reference detected and recorded
- [ ] AC-019A-59: MSTest package reference detected and recorded
- [ ] AC-019A-60: Test framework name stored in ProjectInfo
- [ ] AC-019A-61: Node.js `test` script presence detected
- [ ] AC-019A-62: Jest/Mocha/Vitest detection for Node projects

### Node.js Detection (AC-019A-63 to AC-019A-78)

- [ ] AC-019A-63: All package.json files discovered
- [ ] AC-019A-64: package.json inside node_modules skipped
- [ ] AC-019A-65: package.json parsed as JSON
- [ ] AC-019A-66: `name` property extracted
- [ ] AC-019A-67: `main` entry point extracted
- [ ] AC-019A-68: `scripts` object extracted
- [ ] AC-019A-69: `test` script presence detected
- [ ] AC-019A-70: `build` script presence detected
- [ ] AC-019A-71: npm workspaces configuration detected
- [ ] AC-019A-72: yarn workspaces configuration detected
- [ ] AC-019A-73: Workspace member packages discovered
- [ ] AC-019A-74: Workspace hierarchy mapped correctly
- [ ] AC-019A-75: TypeScript detection (tsconfig.json presence)
- [ ] AC-019A-76: Application vs library classification
- [ ] AC-019A-77: pnpm workspaces detected
- [ ] AC-019A-78: Lerna configuration detected

### Caching (AC-019A-79 to AC-019A-88)

- [ ] AC-019A-79: Detection results cached in memory
- [ ] AC-019A-80: Cache respects configured TTL
- [ ] AC-019A-81: Cache invalidated when .sln files change
- [ ] AC-019A-82: Cache invalidated when .csproj files change
- [ ] AC-019A-83: Cache invalidated when package.json files change
- [ ] AC-019A-84: Force refresh bypasses cache
- [ ] AC-019A-85: Cache stores detection timestamp
- [ ] AC-019A-86: Concurrent cache reads are safe
- [ ] AC-019A-87: Cache persisted to disk (optional)
- [ ] AC-019A-88: Clear cache via API available

### CLI (AC-019A-89 to AC-019A-96)

- [ ] AC-019A-89: `acode detect` command implemented
- [ ] AC-019A-90: `--refresh` flag forces cache bypass
- [ ] AC-019A-91: `--verbose` flag shows detailed output
- [ ] AC-019A-92: `--json` flag outputs JSON format
- [ ] AC-019A-93: Command shows solutions and projects
- [ ] AC-019A-94: Command shows test projects distinctly
- [ ] AC-019A-95: Command shows workspace structure
- [ ] AC-019A-96: Error messages are actionable

---

## Best Practices

### Detection Strategy

1. **Check common patterns first** - .sln, package.json in expected locations
2. **Recurse with limits** - Don't traverse entire filesystem
3. **Respect ignores** - Skip node_modules, bin, obj directories
4. **Cache results** - Store detection results for repeated queries

### Layout Analysis

5. **Parse solution files** - Extract project references from .sln
6. **Analyze package.json** - Detect workspaces, monorepo patterns
7. **Identify test projects** - Recognize test frameworks and patterns
8. **Map dependencies** - Build internal dependency graph

### User Experience

9. **Show clear summary** - What was detected and where
10. **Handle ambiguity** - When multiple layouts possible, ask user
11. **Suggest actions** - What commands are available for detected layout
12. **Support manual override** - Config can specify layout if auto-detect fails

---

## Testing Requirements

### Unit Tests

#### LayoutDetectorTests
- DetectAsync_EmptyDirectory_ReturnsEmptyResult
- DetectAsync_SingleSolution_ReturnsSolution
- DetectAsync_MultipleSolutions_ReturnsAll
- DetectAsync_WithCancellation_ReturnsPartialResult
- DetectAsync_WithMaxDepth_RespectsLimit
- DetectAsync_SkipsConfiguredDirectories
- DetectAsync_ForceRefresh_BypassesCache
- DetectAsync_ParallelCalls_ThreadSafe

#### SlnParserTests
- Parse_ValidSln_ExtractsProjects
- Parse_EmptySln_ReturnsEmptyList
- Parse_SolutionFolders_ParsesHierarchy
- Parse_RelativePaths_ResolvesCorrectly
- Parse_InvalidFormat_ThrowsParseException
- Parse_MissingProjects_ReportsErrors
- Parse_MultiplePlatforms_HandlesAll
- Parse_GuidExtraction_Works

#### CsprojParserTests
- Parse_ValidCsproj_ExtractsMetadata
- Parse_SdkStyle_DetectsSdk
- Parse_LegacyStyle_Handles
- Parse_TargetFramework_Extracts
- Parse_TargetFrameworks_ExtractsMultiple
- Parse_OutputType_Extracts
- Parse_ProjectReferences_Extracts
- Parse_PackageReferences_Extracts
- Parse_IsTestProject_Detects
- Parse_TestSdkReference_DetectsTest
- Parse_InvalidXml_ThrowsException

#### TestProjectDetectorTests
- IsTest_NameEndsWithTests_ReturnsTrue
- IsTest_NameEndsWithTest_ReturnsTrue
- IsTest_HasTestSdkReference_ReturnsTrue
- IsTest_HasXUnitReference_ReturnsTrue
- IsTest_HasNUnitReference_ReturnsTrue
- IsTest_HasMSTestReference_ReturnsTrue
- IsTest_IsTestProjectProperty_ReturnsTrue
- IsTest_RegularProject_ReturnsFalse
- GetFramework_XUnit_ReturnsXUnit
- GetFramework_NUnit_ReturnsNUnit
- GetFramework_MSTest_ReturnsMSTest

#### PackageJsonParserTests
- Parse_ValidPackageJson_ExtractsMetadata
- Parse_WithName_ExtractsName
- Parse_WithMain_ExtractsEntry
- Parse_WithScripts_ExtractsScripts
- Parse_TestScript_DetectsTest
- Parse_NpmWorkspaces_DetectsWorkspaces
- Parse_YarnWorkspaces_DetectsWorkspaces
- Parse_PnpmWorkspaces_DetectsWorkspaces
- Parse_LernaConfig_DetectsLerna
- Parse_InvalidJson_ThrowsException
- Parse_MissingName_UsesDirectoryName

#### WorkspaceDetectorTests
- Detect_NpmWorkspaces_MapsMembers
- Detect_YarnWorkspaces_MapsMembers
- Detect_NestedPackages_LinksToParent
- Detect_GlobPatterns_ExpandsCorrectly
- Detect_NoWorkspaces_ReturnsEmpty

#### DetectionCacheTests
- Get_CachedResult_ReturnsCached
- Get_ExpiredResult_ReturnsNull
- Get_NoResult_ReturnsNull
- Set_NewResult_Stores
- Set_ExistingResult_Overwrites
- Invalidate_RemovesEntry
- InvalidateAll_ClearsCache
- Get_ConcurrentAccess_ThreadSafe

### Integration Tests

#### LayoutDetectorIntegrationTests
- Detect_RealDotNetRepo_FindsAllProjects
- Detect_RealNodeRepo_FindsAllPackages
- Detect_MonorepoWithWorkspaces_MapsHierarchy
- Detect_MixedRepo_FindsBothTypes
- Detect_WithGitignore_RespectsPatterns
- Detect_LargeRepo_CompletesInTime

### End-to-End Tests

#### DetectionE2ETests
- CLI_Detect_OutputsProjectList
- CLI_DetectJson_ValidJsonOutput
- CLI_DetectVerbose_ShowsDetails
- CLI_DetectRefresh_BypassesCache
- CLI_Detect_ShowsTestProjects
- CLI_Detect_ShowsWorkspaces

### Performance Benchmarks

| Benchmark | Target | Maximum | Notes |
|-----------|--------|---------|-------|
| Scan 100 files | 30ms | 50ms | Small project |
| Scan 1000 files | 300ms | 500ms | Medium project |
| Scan 10000 files | 1.5s | 3s | Large project |
| Cached lookup | 2ms | 5ms | Memory cache |
| Parse .sln file | 10ms | 20ms | Single file |
| Parse .csproj file | 5ms | 15ms | Single file |
| Parse package.json | 2ms | 5ms | Single file |
| Build dependency graph (50 projects) | 30ms | 100ms | Graph construction |

### Coverage Requirements

| Component | Minimum Coverage |
|-----------|-----------------|
| LayoutDetector | 90% |
| SlnParser | 95% |
| CsprojParser | 95% |
| PackageJsonParser | 95% |
| TestProjectDetector | 98% |
| DetectionCache | 90% |
| WorkspaceDetector | 90% |

---

## User Verification Steps

### Scenario 1: Detect .NET Solution
```powershell
# Step 1: Create a .NET solution with projects
dotnet new sln -n MySolution
dotnet new classlib -o src/MyLib
dotnet new xunit -o tests/MyLib.Tests
dotnet sln add src/MyLib tests/MyLib.Tests
dotnet add tests/MyLib.Tests reference src/MyLib

# Step 2: Run detection
acode detect

# Expected Output:
# Repository Layout
# ─────────────────
# 
# .NET Projects:
#   MySolution.sln (Solution)
#   ├── src/MyLib/MyLib.csproj
#   │   Type: Library
#   │   Framework: net8.0
#   └── tests/MyLib.Tests/MyLib.Tests.csproj
#       Type: Test (xUnit)
#       Framework: net8.0
#       References: MyLib
# 
# Summary:
#   .NET: 1 solution, 2 projects (1 test)

# Verification: Solution and all projects detected with correct types
```

### Scenario 2: Detect Node.js Project
```powershell
# Step 1: Create a Node.js project
mkdir frontend
cd frontend
npm init -y

# Add scripts to package.json
# { "scripts": { "build": "tsc", "test": "jest", "start": "node dist/index.js" } }

# Step 2: Run detection
cd ..
acode detect

# Expected Output:
# Node.js Projects:
#   frontend/package.json
#   ├── Name: frontend
#   │   Type: Application
#   │   Scripts: build, test, start
#   │   Has Test: Yes (test script)

# Verification: package.json detected with scripts identified
```

### Scenario 3: Detect Monorepo with Workspaces
```powershell
# Step 1: Create monorepo structure
mkdir monorepo && cd monorepo
npm init -y

# Add workspaces to package.json:
# { "workspaces": ["packages/*"] }

mkdir -p packages/core packages/web
cd packages/core && npm init -y && cd ../..
cd packages/web && npm init -y && cd ../..

# Step 2: Run detection
acode detect

# Expected Output:
# Node.js Projects:
#   package.json (Root)
#   ├── Workspaces: packages/*
#   └── Members:
#       ├── packages/core/package.json
#       │   Name: core
#       └── packages/web/package.json
#           Name: web

# Verification: Workspace structure correctly mapped
```

### Scenario 4: Detect Test Projects
```powershell
# Step 1: Create projects with different test frameworks
dotnet new xunit -o tests/XUnitTests
dotnet new nunit -o tests/NUnitTests
dotnet new mstest -o tests/MSTestTests

# Step 2: Run detection with verbose
acode detect --verbose

# Expected Output (for test projects):
# tests/XUnitTests/XUnitTests.csproj
#   Type: Test Project
#   Framework: net8.0
#   Test Framework: xUnit
#   Detected By: xunit package reference
#
# tests/NUnitTests/NUnitTests.csproj
#   Type: Test Project
#   Framework: net8.0
#   Test Framework: NUnit
#   Detected By: NUnit package reference
#
# tests/MSTestTests/MSTestTests.csproj
#   Type: Test Project
#   Framework: net8.0
#   Test Framework: MSTest
#   Detected By: Microsoft.VisualStudio.TestPlatform.TestFramework

# Verification: All test frameworks correctly identified
```

### Scenario 5: JSON Output for Automation
```powershell
# Step 1: Run detection with JSON output
acode detect --json > detection.json

# Step 2: Verify JSON structure
Get-Content detection.json | ConvertFrom-Json | Select-Object -ExpandProperty projects

# Expected: Valid JSON array with project objects containing:
# - path
# - type
# - name
# - isTest
# - framework
# - references

# Verification: JSON output is valid and contains all project information
```

### Scenario 6: Cache Behavior
```powershell
# Step 1: Initial detection (creates cache)
acode detect
# Note the scan duration

# Step 2: Second detection (uses cache)
acode detect
# Note: Should show "Cached" and faster

# Step 3: Add new project
dotnet new classlib -o src/NewLib

# Step 4: Detection without refresh
acode detect
# May not show NewLib (depending on file watcher)

# Step 5: Force refresh
acode detect --refresh
# Should show NewLib

# Verification: Cache works and refresh bypasses it
```

### Scenario 7: Handle Corrupted Files
```powershell
# Step 1: Create valid project
dotnet new classlib -o src/ValidLib

# Step 2: Create corrupted .csproj
mkdir src/BadLib
echo "this is not valid xml" > src/BadLib/BadLib.csproj

# Step 3: Run detection
acode detect

# Expected Output:
# .NET Projects:
#   src/ValidLib/ValidLib.csproj
#   Type: Library
#   Framework: net8.0
#
# Errors:
#   src/BadLib/BadLib.csproj: Parse error - Invalid XML

# Verification: Valid projects still detected, error reported for invalid
```

### Scenario 8: Multi-Framework Projects
```powershell
# Step 1: Create multi-targeting project
dotnet new classlib -o src/MultiLib

# Step 2: Edit .csproj to multi-target:
# <TargetFrameworks>net6.0;net7.0;net8.0</TargetFrameworks>

# Step 3: Run detection
acode detect --verbose

# Expected Output:
# src/MultiLib/MultiLib.csproj
#   Type: Library
#   Frameworks: net6.0, net7.0, net8.0 (multi-targeting)

# Verification: Multiple frameworks detected
```

### Scenario 9: Mixed Repository
```powershell
# Step 1: Create mixed repo with .NET and Node
dotnet new sln -n FullStack
dotnet new webapi -o src/Api
dotnet sln add src/Api
mkdir frontend && cd frontend && npm init -y && cd ..

# Step 2: Run detection
acode detect

# Expected Output:
# .NET Projects:
#   FullStack.sln (Solution)
#   └── src/Api/Api.csproj
#       Type: Web API
#       Framework: net8.0
#
# Node.js Projects:
#   frontend/package.json
#   Name: frontend
#
# Summary:
#   .NET: 1 solution, 1 project
#   Node: 1 package

# Verification: Both .NET and Node.js projects detected
```

### Scenario 10: Performance on Large Repository
```powershell
# Step 1: Clone a large open-source repo
git clone https://github.com/dotnet/runtime runtime-test
cd runtime-test

# Step 2: Run detection and time it
Measure-Command { acode detect }

# Expected: Completes within performance targets (< 3s for 10000 files)

# Step 3: Run cached detection
Measure-Command { acode detect }

# Expected: Cached lookup < 5ms

# Verification: Detection scales to large repositories
```

---

## Implementation Prompt

You are implementing Task-019a: Detect Solution/Package Layouts for the Agentic Coding Bot. This subtask creates the project layout detection infrastructure that scans repositories to discover .NET solutions, project files, Node.js packages, and their relationships, enabling the language runners to intelligently select appropriate build and test commands.

### File Structure

```
src/
├── AgenticCoder.Domain/
│   └── Detection/
│       ├── ILayoutDetector.cs
│       ├── DetectionResult.cs
│       ├── ProjectInfo.cs
│       ├── ProjectType.cs
│       ├── DetectionOptions.cs
│       └── DetectionError.cs
│
├── AgenticCoder.Application/
│   └── Detection/
│       ├── ISlnParser.cs
│       ├── ICsprojParser.cs
│       ├── IPackageJsonParser.cs
│       ├── ITestProjectDetector.cs
│       └── IDetectionCache.cs
│
├── AgenticCoder.Infrastructure/
│   └── Detection/
│       ├── LayoutDetector.cs
│       ├── DotNetDetector.cs
│       ├── NodeDetector.cs
│       ├── SlnParser.cs
│       ├── CsprojParser.cs
│       ├── PackageJsonParser.cs
│       ├── TestProjectDetector.cs
│       ├── WorkspaceDetector.cs
│       ├── DetectionCache.cs
│       └── DependencyInjection/
│           └── DetectionServiceExtensions.cs
│
└── tests/
    └── AgenticCoder.Infrastructure.Tests/
        └── Detection/
            ├── LayoutDetectorTests.cs
            ├── SlnParserTests.cs
            ├── CsprojParserTests.cs
            ├── PackageJsonParserTests.cs
            ├── TestProjectDetectorTests.cs
            └── DetectionCacheTests.cs
```

### Domain Models

```csharp
namespace AgenticCoder.Domain.Detection;

/// <summary>
/// Types of projects that can be detected
/// </summary>
public enum ProjectType
{
    DotNetSolution,
    DotNetProject,
    DotNetWebProject,
    DotNetWorkerProject,
    NodePackage,
    NodeWorkspaceRoot,
    NodeWorkspaceMember
}

/// <summary>
/// Test frameworks that can be detected
/// </summary>
public enum TestFramework
{
    None,
    XUnit,
    NUnit,
    MSTest,
    TUnit,
    Jest,
    Mocha,
    Vitest
}

/// <summary>
/// Options for controlling detection behavior
/// </summary>
public sealed record DetectionOptions
{
    public int MaxDepth { get; init; } = 10;
    public bool ForceRefresh { get; init; } = false;
    public IReadOnlyList<string> SkipDirectories { get; init; } = 
        new[] { "node_modules", "bin", "obj", "dist", ".git", ".vs" };
    public IReadOnlyList<string> DotNetPatterns { get; init; } = 
        new[] { "*.sln", "*.csproj", "*.fsproj" };
    public IReadOnlyList<string> NodePatterns { get; init; } = 
        new[] { "package.json" };
    public bool RespectGitignore { get; init; } = true;
}

/// <summary>
/// Information about a detected project
/// </summary>
public sealed record ProjectInfo
{
    public required string Path { get; init; }
    public required ProjectType Type { get; init; }
    public required string Name { get; init; }
    public bool IsTestProject { get; init; }
    public TestFramework TestFramework { get; init; } = TestFramework.None;
    public string? EntryPoint { get; init; }
    public string? TargetFramework { get; init; }
    public IReadOnlyList<string>? TargetFrameworks { get; init; }
    public string? OutputType { get; init; }
    public IReadOnlyList<string> ProjectReferences { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Scripts { get; init; } = Array.Empty<string>();
    public string? ParentSolution { get; init; }
    public string? SdkType { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = 
        new Dictionary<string, string>();
}

/// <summary>
/// Result of layout detection
/// </summary>
public sealed record DetectionResult
{
    public required string RepositoryRoot { get; init; }
    public required DateTimeOffset DetectedAt { get; init; }
    public required TimeSpan Duration { get; init; }
    public required bool FromCache { get; init; }
    public required int FilesScanned { get; init; }
    public required IReadOnlyList<ProjectInfo> Projects { get; init; }
    public required IReadOnlyList<DetectionError> Errors { get; init; }
    
    public IEnumerable<ProjectInfo> Solutions => 
        Projects.Where(p => p.Type == ProjectType.DotNetSolution);
    
    public IEnumerable<ProjectInfo> TestProjects => 
        Projects.Where(p => p.IsTestProject);
    
    public IEnumerable<ProjectInfo> DotNetProjects => 
        Projects.Where(p => p.Type is ProjectType.DotNetProject 
            or ProjectType.DotNetWebProject or ProjectType.DotNetWorkerProject);
    
    public IEnumerable<ProjectInfo> NodePackages => 
        Projects.Where(p => p.Type is ProjectType.NodePackage 
            or ProjectType.NodeWorkspaceRoot or ProjectType.NodeWorkspaceMember);
}

/// <summary>
/// Error encountered during detection
/// </summary>
public sealed record DetectionError
{
    public required string Path { get; init; }
    public required string Message { get; init; }
    public required string ErrorCode { get; init; }
}
```

### Core Interfaces

```csharp
namespace AgenticCoder.Domain.Detection;

public interface ILayoutDetector
{
    /// <summary>
    /// Detect project layout in repository
    /// </summary>
    Task<DetectionResult> DetectAsync(
        string rootPath,
        DetectionOptions? options = null,
        CancellationToken cancellationToken = default);
}

public interface ISlnParser
{
    /// <summary>
    /// Parse .sln file to extract project references
    /// </summary>
    Task<SlnParseResult> ParseAsync(string slnPath, CancellationToken ct = default);
}

public interface ICsprojParser
{
    /// <summary>
    /// Parse .csproj file to extract metadata
    /// </summary>
    Task<CsprojParseResult> ParseAsync(string csprojPath, CancellationToken ct = default);
}

public interface IPackageJsonParser
{
    /// <summary>
    /// Parse package.json to extract metadata
    /// </summary>
    Task<PackageJsonParseResult> ParseAsync(string packageJsonPath, CancellationToken ct = default);
}

public interface ITestProjectDetector
{
    /// <summary>
    /// Determine if a project is a test project
    /// </summary>
    (bool IsTest, TestFramework Framework) Detect(CsprojParseResult csproj);
    
    /// <summary>
    /// Determine if a Node package has tests
    /// </summary>
    (bool HasTests, TestFramework Framework) Detect(PackageJsonParseResult package);
}

public interface IDetectionCache
{
    Task<DetectionResult?> GetAsync(string rootPath, CancellationToken ct = default);
    Task SetAsync(string rootPath, DetectionResult result, CancellationToken ct = default);
    Task InvalidateAsync(string rootPath, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
}
```

### Infrastructure Implementations

```csharp
namespace AgenticCoder.Infrastructure.Detection;

public sealed class LayoutDetector : ILayoutDetector
{
    private readonly IRepoFileSystem _fileSystem;
    private readonly ISlnParser _slnParser;
    private readonly ICsprojParser _csprojParser;
    private readonly IPackageJsonParser _packageJsonParser;
    private readonly ITestProjectDetector _testDetector;
    private readonly IDetectionCache _cache;
    private readonly DetectionConfiguration _config;
    private readonly ILogger<LayoutDetector> _logger;
    
    public async Task<DetectionResult> DetectAsync(
        string rootPath,
        DetectionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new DetectionOptions();
        var stopwatch = Stopwatch.StartNew();
        
        // Check cache first (unless force refresh)
        if (!options.ForceRefresh)
        {
            var cached = await _cache.GetAsync(rootPath, cancellationToken);
            if (cached != null && !IsExpired(cached))
            {
                _logger.LogDebug("Using cached detection result for {Path}", rootPath);
                return cached with { FromCache = true };
            }
        }
        
        var projects = new List<ProjectInfo>();
        var errors = new List<DetectionError>();
        var filesScanned = 0;
        
        // Scan for .NET projects
        await foreach (var slnPath in FindFilesAsync(rootPath, "*.sln", options, cancellationToken))
        {
            filesScanned++;
            try
            {
                var slnResult = await _slnParser.ParseAsync(slnPath, cancellationToken);
                projects.Add(CreateSolutionInfo(slnPath, slnResult));
                
                foreach (var projectPath in slnResult.ProjectPaths)
                {
                    await ProcessDotNetProjectAsync(projectPath, slnPath, projects, errors, cancellationToken);
                    filesScanned++;
                }
            }
            catch (Exception ex)
            {
                errors.Add(new DetectionError
                {
                    Path = slnPath,
                    Message = ex.Message,
                    ErrorCode = "DET-019A-01"
                });
            }
        }
        
        // Find standalone .csproj files (not in any solution)
        await foreach (var csprojPath in FindFilesAsync(rootPath, "*.csproj", options, cancellationToken))
        {
            if (!projects.Any(p => p.Path.Equals(csprojPath, StringComparison.OrdinalIgnoreCase)))
            {
                await ProcessDotNetProjectAsync(csprojPath, null, projects, errors, cancellationToken);
                filesScanned++;
            }
        }
        
        // Scan for Node.js projects
        await foreach (var packagePath in FindFilesAsync(rootPath, "package.json", options, cancellationToken))
        {
            // Skip node_modules
            if (packagePath.Contains("node_modules")) continue;
            
            filesScanned++;
            try
            {
                var packageResult = await _packageJsonParser.ParseAsync(packagePath, cancellationToken);
                var (hasTests, framework) = _testDetector.Detect(packageResult);
                
                projects.Add(new ProjectInfo
                {
                    Path = GetRelativePath(rootPath, packagePath),
                    Type = packageResult.Workspaces?.Any() == true 
                        ? ProjectType.NodeWorkspaceRoot 
                        : ProjectType.NodePackage,
                    Name = packageResult.Name ?? Path.GetDirectoryName(packagePath) ?? "unknown",
                    IsTestProject = false,
                    TestFramework = framework,
                    EntryPoint = packageResult.Main,
                    Scripts = packageResult.Scripts.Keys.ToList()
                });
            }
            catch (Exception ex)
            {
                errors.Add(new DetectionError
                {
                    Path = packagePath,
                    Message = ex.Message,
                    ErrorCode = "DET-019A-02"
                });
            }
        }
        
        stopwatch.Stop();
        
        var result = new DetectionResult
        {
            RepositoryRoot = rootPath,
            DetectedAt = DateTimeOffset.UtcNow,
            Duration = stopwatch.Elapsed,
            FromCache = false,
            FilesScanned = filesScanned,
            Projects = projects,
            Errors = errors
        };
        
        // Cache result
        await _cache.SetAsync(rootPath, result, cancellationToken);
        
        _logger.LogInformation(
            "Detection completed: {ProjectCount} projects, {ErrorCount} errors, {Duration}ms",
            projects.Count, errors.Count, stopwatch.ElapsedMilliseconds);
        
        return result;
    }
    
    private async Task ProcessDotNetProjectAsync(
        string projectPath,
        string? solutionPath,
        List<ProjectInfo> projects,
        List<DetectionError> errors,
        CancellationToken ct)
    {
        try
        {
            var csprojResult = await _csprojParser.ParseAsync(projectPath, ct);
            var (isTest, framework) = _testDetector.Detect(csprojResult);
            
            var projectType = csprojResult.SdkType switch
            {
                "Microsoft.NET.Sdk.Web" => ProjectType.DotNetWebProject,
                "Microsoft.NET.Sdk.Worker" => ProjectType.DotNetWorkerProject,
                _ => ProjectType.DotNetProject
            };
            
            projects.Add(new ProjectInfo
            {
                Path = projectPath,
                Type = projectType,
                Name = Path.GetFileNameWithoutExtension(projectPath),
                IsTestProject = isTest,
                TestFramework = framework,
                TargetFramework = csprojResult.TargetFramework,
                TargetFrameworks = csprojResult.TargetFrameworks,
                OutputType = csprojResult.OutputType,
                ProjectReferences = csprojResult.ProjectReferences,
                ParentSolution = solutionPath,
                SdkType = csprojResult.SdkType
            });
        }
        catch (Exception ex)
        {
            errors.Add(new DetectionError
            {
                Path = projectPath,
                Message = ex.Message,
                ErrorCode = "DET-019A-03"
            });
        }
    }
}

public sealed class TestProjectDetector : ITestProjectDetector
{
    private static readonly string[] TestProjectSuffixes = 
        { ".Tests", ".Test", ".UnitTests", ".IntegrationTests" };
    
    private static readonly string[] XUnitPackages = 
        { "xunit", "xunit.v3" };
    
    private static readonly string[] NUnitPackages = 
        { "NUnit", "NUnit3TestAdapter" };
    
    private static readonly string[] MSTestPackages = 
        { "Microsoft.VisualStudio.TestPlatform.TestFramework", "MSTest.TestAdapter" };
    
    public (bool IsTest, TestFramework Framework) Detect(CsprojParseResult csproj)
    {
        // Check explicit IsTestProject property
        if (csproj.IsTestProject == true)
        {
            return (true, DetectFramework(csproj.PackageReferences));
        }
        
        // Check for test SDK
        if (csproj.PackageReferences.Any(p => 
            p.Equals("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase)))
        {
            return (true, DetectFramework(csproj.PackageReferences));
        }
        
        // Check project name
        var name = csproj.ProjectName;
        if (TestProjectSuffixes.Any(s => 
            name.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
        {
            return (true, DetectFramework(csproj.PackageReferences));
        }
        
        return (false, TestFramework.None);
    }
    
    private static TestFramework DetectFramework(IEnumerable<string> packages)
    {
        var packageList = packages.ToList();
        
        if (packageList.Any(p => XUnitPackages.Contains(p, StringComparer.OrdinalIgnoreCase)))
            return TestFramework.XUnit;
            
        if (packageList.Any(p => NUnitPackages.Contains(p, StringComparer.OrdinalIgnoreCase)))
            return TestFramework.NUnit;
            
        if (packageList.Any(p => MSTestPackages.Contains(p, StringComparer.OrdinalIgnoreCase)))
            return TestFramework.MSTest;
            
        return TestFramework.None;
    }
}
```

### Error Codes

| Code | Category | Description |
|------|----------|-------------|
| DET-019A-01 | Parse | Failed to parse .sln file |
| DET-019A-02 | Parse | Failed to parse package.json |
| DET-019A-03 | Parse | Failed to parse .csproj file |
| DET-019A-04 | Scan | Directory access denied |
| DET-019A-05 | Scan | Scan depth exceeded |
| DET-019A-06 | Cache | Cache read error |
| DET-019A-07 | Cache | Cache write error |
| DET-019A-08 | Validation | Invalid root path |

### Implementation Checklist

1. [ ] Create domain models (ProjectInfo, DetectionResult, ProjectType, DetectionOptions)
2. [ ] Create interfaces (ILayoutDetector, ISlnParser, ICsprojParser, IPackageJsonParser)
3. [ ] Implement SlnParser to parse .sln file format
4. [ ] Implement CsprojParser to parse XML project files
5. [ ] Implement PackageJsonParser for JSON parsing
6. [ ] Implement TestProjectDetector with all framework detection
7. [ ] Implement WorkspaceDetector for npm/yarn/pnpm workspaces
8. [ ] Implement DetectionCache with memory and optional disk storage
9. [ ] Implement LayoutDetector orchestrating all components
10. [ ] Add file system scanning with pattern matching
11. [ ] Add gitignore support
12. [ ] Add CLI `detect` command
13. [ ] Write unit tests for all parsers
14. [ ] Write unit tests for TestProjectDetector
15. [ ] Write unit tests for cache
16. [ ] Write integration tests with real repos
17. [ ] Add performance benchmarks
18. [ ] Update documentation

### Rollout Plan

| Phase | Components | Validation |
|-------|------------|------------|
| 1 | Domain models and interfaces | Compiles |
| 2 | SlnParser | .sln files parse correctly |
| 3 | CsprojParser | .csproj files parse correctly |
| 4 | TestProjectDetector | Test projects identified |
| 5 | PackageJsonParser | package.json parses correctly |
| 6 | WorkspaceDetector | Workspaces mapped correctly |
| 7 | DetectionCache | Caching works |
| 8 | LayoutDetector | Full detection works |
| 9 | CLI integration | Command works |

### Dependencies

- **Task-014 (RepoFS)**: File system abstraction for scanning
- **Task-002a (Config)**: Detection configuration in config.yml
- **Task-005 (CLI)**: CLI command integration

---

**End of Task 019.a Specification**