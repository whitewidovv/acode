# Task 019.a: Detect Solution/Package Layouts

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 019 (Language Runners), Task 014 (RepoFS)  

---

## Description

Task 019.a implements project layout detection. The agent must understand repository structure. Detection finds solution files, project files, and package manifests.

Layout detection is automatic. The agent scans the repository. It identifies project types. It maps the structure.

.NET layouts have specific patterns. Solution files (.sln) at root. Project files (.csproj) in subdirectories. src/tests folder conventions.

Node.js layouts follow conventions. package.json at root or subdirectories. node_modules for dependencies. src/dist for code.

Monorepo layouts are common. Multiple projects in one repository. workspaces in package.json. Multiple .sln files possible.

Detection caches results. Scanning is expensive. Cache invalidates on file changes. Incremental updates for efficiency.

The detection result model is rich. Project paths, types, relationships. Test projects identified. Entry points located.

Detection uses Task 014 RepoFS. File system abstraction enables testing. Works with local and Docker mounts.

Failure handling is graceful. Corrupted manifests reported. Partial results returned. Agent can proceed with available information.

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

### Detection Interface

- FR-001: Define ILayoutDetector interface
- FR-002: DetectAsync method
- FR-003: Accept root path
- FR-004: Return DetectionResult
- FR-005: Support cancellation

### Detection Result

- FR-006: Define DetectionResult record
- FR-007: List of projects
- FR-008: Project relationships
- FR-009: Detection timestamp
- FR-010: Cache validity

### Project Info

- FR-011: Define ProjectInfo record
- FR-012: Project path
- FR-013: Project type (dotnet, node)
- FR-014: Project name
- FR-015: Is test project
- FR-016: Entry point path
- FR-017: Dependencies list

### .NET Detection

- FR-018: Find .sln files
- FR-019: Parse .sln for projects
- FR-020: Find standalone .csproj
- FR-021: Find .fsproj files
- FR-022: Identify test projects
- FR-023: Parse project references
- FR-024: Detect target framework

### Test Project Identification

- FR-025: Check project name (*Tests, *.Test)
- FR-026: Check for test SDK reference
- FR-027: Check for xUnit/NUnit/MSTest
- FR-028: Mark as test project

### Node.js Detection

- FR-029: Find package.json files
- FR-030: Parse package.json
- FR-031: Detect workspaces
- FR-032: Identify entry points
- FR-033: Detect test scripts
- FR-034: Detect build scripts

### Monorepo Detection

- FR-035: Detect npm workspaces
- FR-036: Detect yarn workspaces
- FR-037: Detect lerna
- FR-038: Map workspace structure
- FR-039: Handle nested package.json

### Caching

- FR-040: Cache detection results
- FR-041: Invalidate on file change
- FR-042: Time-based expiry
- FR-043: Force refresh option
- FR-044: Persist cache

### Scanning

- FR-045: Recursive directory scan
- FR-046: Respect gitignore
- FR-047: Limit depth option
- FR-048: Skip node_modules
- FR-049: Skip bin/obj

---

## Non-Functional Requirements

### Performance

- NFR-001: Initial scan < 500ms for 1000 files
- NFR-002: Cached lookup < 5ms
- NFR-003: Incremental update < 100ms

### Reliability

- NFR-004: Handle corrupted files
- NFR-005: Partial results on error
- NFR-006: Clear error messages

### Accuracy

- NFR-007: All projects found
- NFR-008: Correct type detection
- NFR-009: Accurate relationships

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

### Detection

- [ ] AC-001: .sln files found
- [ ] AC-002: .csproj files found
- [ ] AC-003: package.json found
- [ ] AC-004: Test projects identified

### Parsing

- [ ] AC-005: .sln parsed correctly
- [ ] AC-006: .csproj parsed correctly
- [ ] AC-007: package.json parsed

### Caching

- [ ] AC-008: Results cached
- [ ] AC-009: Cache invalidates
- [ ] AC-010: Refresh works

### CLI

- [ ] AC-011: detect command works
- [ ] AC-012: JSON output works
- [ ] AC-013: Verbose output works

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Detection/
├── LayoutDetectorTests.cs
│   ├── Should_Detect_Solution()
│   ├── Should_Detect_Project()
│   └── Should_Detect_Package_Json()
│
├── DotNetParserTests.cs
│   ├── Should_Parse_Sln()
│   ├── Should_Parse_Csproj()
│   └── Should_Detect_Test_Project()
│
├── NodeParserTests.cs
│   ├── Should_Parse_Package_Json()
│   ├── Should_Detect_Workspaces()
│   └── Should_Find_Entry_Point()
│
└── CacheTests.cs
    ├── Should_Cache_Results()
    └── Should_Invalidate()
```

### Integration Tests

```
Tests/Integration/Detection/
├── LayoutDetectorIntegrationTests.cs
│   └── Should_Scan_Real_Repo()
```

### E2E Tests

```
Tests/E2E/Detection/
├── DetectionE2ETests.cs
│   └── Should_Detect_Via_CLI()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Scan 1000 files | 300ms | 500ms |
| Cached lookup | 2ms | 5ms |
| Parse .sln | 10ms | 20ms |

---

## User Verification Steps

### Scenario 1: Detect .NET Solution

1. Create .NET solution
2. Run `acode detect`
3. Verify: Solution and projects listed

### Scenario 2: Detect Node.js

1. Create Node.js project
2. Run `acode detect`
3. Verify: package.json found

### Scenario 3: Monorepo

1. Create monorepo with workspaces
2. Run `acode detect`
3. Verify: All workspaces listed

### Scenario 4: Cache Refresh

1. Run `acode detect`
2. Add new project
3. Run `acode detect --refresh`
4. Verify: New project found

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Detection/
│   ├── ILayoutDetector.cs
│   ├── DetectionResult.cs
│   └── ProjectInfo.cs
│
src/AgenticCoder.Infrastructure/
├── Detection/
│   ├── LayoutDetector.cs
│   ├── DotNetDetector.cs
│   ├── NodeDetector.cs
│   ├── SlnParser.cs
│   ├── CsprojParser.cs
│   ├── PackageJsonParser.cs
│   └── DetectionCache.cs
```

### ILayoutDetector Interface

```csharp
namespace AgenticCoder.Domain.Detection;

public interface ILayoutDetector
{
    Task<DetectionResult> DetectAsync(
        string rootPath,
        DetectionOptions? options = null,
        CancellationToken ct = default);
}
```

### ProjectInfo Record

```csharp
public record ProjectInfo
{
    public required string Path { get; init; }
    public required ProjectType Type { get; init; }
    public required string Name { get; init; }
    public bool IsTestProject { get; init; }
    public string? EntryPoint { get; init; }
    public string? Framework { get; init; }
    public IReadOnlyList<string> References { get; init; } = [];
    public IReadOnlyList<string> Scripts { get; init; } = [];
}

public enum ProjectType
{
    DotNetSolution,
    DotNetProject,
    DotNetTestProject,
    NodePackage,
    NodeWorkspace
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-DET-001 | Scan failed |
| ACODE-DET-002 | Parse failed |
| ACODE-DET-003 | Cache error |
| ACODE-DET-004 | Path not found |

### Implementation Checklist

1. [ ] Create detection interface
2. [ ] Create result models
3. [ ] Implement .NET detection
4. [ ] Implement Node detection
5. [ ] Add parsers
6. [ ] Add caching
7. [ ] Add CLI command
8. [ ] Add unit tests

### Rollout Plan

1. **Phase 1:** Detection interface
2. **Phase 2:** .NET detection
3. **Phase 3:** Node detection
4. **Phase 4:** Caching
5. **Phase 5:** CLI integration

---

**End of Task 019.a Specification**