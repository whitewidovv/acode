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

## Use Cases

### Use Case 1: Samantha the DevOps Engineer — Automating Build Pipeline Discovery

**Persona:** Samantha is a DevOps engineer setting up CI/CD pipelines for a newly acquired codebase. She needs to quickly identify which projects to build, which are tests, and their dependencies without manually reading every .csproj file.

**Before (Manual Discovery):**
Samantha must manually:
1. Search the repository for .sln files (15 minutes)
2. Open each solution in Visual Studio to see contained projects (10 minutes)
3. Click through each project to check if it's a test project (20 minutes)
4. Manually map project dependencies by reading .csproj XML (30 minutes)
5. Document findings in a spreadsheet for CI pipeline configuration (15 minutes)

**Total time:** 90 minutes per repository

**Challenges:**
- Prone to human error (missing projects, wrong test identification)
- Time-consuming for large repositories with many solutions
- Manual mapping of dependencies error-prone
- Documentation becomes stale as projects are added/removed

**After (Automated Layout Detection):**
Samantha runs:
```bash
$ acode detect --json > layout.json
Detection completed: 25 projects found (8 test projects)
Duration: 1.2s
```

Then uses the JSON output to automatically configure the CI pipeline:
```yaml
# .github/workflows/build.yml (auto-generated from layout.json)
jobs:
  build:
    steps:
      - run: dotnet build MySolution.sln --configuration Release
  test:
    needs: build
    strategy:
      matrix:
        test-project:
          - tests/MyApp.UnitTests/MyApp.UnitTests.csproj
          - tests/MyApp.IntegrationTests/MyApp.IntegrationTests.csproj
          # ... 6 more test projects auto-discovered
    steps:
      - run: dotnet test ${{ matrix.test-project }}
```

**Benefits:**
- Discovery time: 90 minutes → 1.2 seconds (99.9% reduction)
- Error rate: ~10% human errors → 0% (automated)
- CI pipeline configuration: auto-generated from detection results
- Stays current: re-run detection when projects added/removed

**Quantified Improvement:**
- **Time saved:** 88.98 minutes per repository × 12 repositories/year = 17.8 hours/year
- **Cost savings:** 17.8 hours × $100/hour = **$1,780/year**
- **Accuracy:** 100% vs 90% manual accuracy
- **Setup speed:** New repository CI ready in <5 minutes vs 2 hours

---

### Use Case 2: Marcus the AI Coding Agent — Understanding Codebase Structure

**Persona:** Marcus is an AI coding agent (like Agentic Coding Bot) tasked with implementing a new feature across a monorepo containing both .NET microservices and React frontends. He needs to understand the project structure to know where to make code changes.

**Before (No Layout Detection):**
Marcus has hardcoded heuristics:
```python
# agent_understanding.py (brittle and incomplete)
def find_project_for_feature(feature_name):
    # Hardcoded assumptions - breaks easily
    if "api" in feature_name.lower():
        return "src/Backend/Api/Api.csproj"  # Might not exist!
    elif "frontend" in feature_name.lower():
        return "frontend/package.json"  # Wrong path!
    else:
        raise Exception("Can't determine project location")
```

**Challenges:**
- Hardcoded paths break when repository restructures
- No understanding of project dependencies (can't build in correct order)
- No test project awareness (can't run relevant tests after changes)
- Monorepo workspaces not understood (applies changes to wrong package)

**After (Layout Detection API):**
Marcus queries the detection API:
```csharp
// agent_understanding.cs (adaptive and accurate)
async Task<ProjectInfo> FindProjectForFeature(string featureName)
{
    var layout = await _layoutDetector.DetectAsync(_repoRoot, ct: default);

    // Intelligent search using actual repository structure
    if (featureName.Contains("auth", StringComparison.OrdinalIgnoreCase))
    {
        // Find project by actual name/type, not hardcoded path
        return layout.Projects.FirstOrDefault(p =>
            p.Name.Contains("Auth") &&
            p.Type == ProjectType.DotNetWebProject);
    }

    // Can also find test projects to validate changes
    var testProjects = layout.TestProjects
        .Where(t => t.ProjectReferences.Contains(mainProject.Path));

    return mainProject;
}
```

**Benefits:**
- Adapts to repository structure automatically (no hardcoded paths)
- Understands project dependencies (can build in correct order)
- Finds related test projects (can run relevant tests)
- Works with monorepos (understands workspace structure)

**Quantified Improvement:**
- **Correctness:** 60% success rate (hardcoded paths) → 98% success rate (dynamic detection)
- **Repository changes handled:** 0% (breaks on restructure) → 100% (adapts automatically)
- **Time to fix broken assumptions:** 2 hours/month (fixing hardcoded paths) → 0 hours (self-healing)
- **Task completion rate:** 6 successful tasks/10 attempts (60%) → 9.8 successful tasks/10 attempts (98%)

---

### Use Case 3: Jordan the Junior Developer — Onboarding to Complex Monorepo

**Persona:** Jordan just joined a company with a large .NET/Node.js monorepo containing 50+ projects across 8 solutions. They need to understand the codebase structure to contribute but the documentation is outdated.

**Before (Manual Exploration + Outdated Docs):**
Jordan's onboarding involves:
1. Read the "Repository Structure" wiki page (last updated 6 months ago) - 30 minutes
2. Discover page lists projects that no longer exist - confusion ensues
3. Manually explore directories looking for .sln files - 1 hour
4. Open each solution in VS to see what's inside - 1.5 hours
5. Try to figure out which projects are tests - 30 minutes
6. Ask senior developer "Where's the authentication code?" - 30 minutes (blocking senior)
7. Build wrong project, wait for build, realize mistake - 15 minutes

**Total onboarding time:** 4.5 hours + 30 minutes senior dev time

**After (Detection-Driven Onboarding):**
Jordan runs:
```bash
$ acode detect --verbose

Repository Layout
─────────────────

.NET Projects:
  Backend.sln (Solution)
  ├── src/AuthService/AuthService.csproj
  │   Type: Web API
  │   Framework: net8.0
  │   Dependencies: IdentityCore, DatabaseLayer
  ├── src/PaymentService/PaymentService.csproj
  │   Type: Web API
  │   Framework: net8.0
  │   Dependencies: DatabaseLayer
  ... (48 more projects with clear hierarchy)

Node.js Projects:
  frontend/package.json (Workspace Root)
  ├── Workspaces: packages/*
  └── Members:
      ├── packages/customer-portal/package.json (React app)
      ├── packages/admin-dashboard/package.json (React app)
      └── packages/shared-components/package.json (Library)

Summary:
  .NET: 3 solutions, 42 projects (15 test projects)
  Node: 1 workspace with 8 packages
```

Jordan immediately understands:
- AuthService is in src/AuthService (found authentication code in 10 seconds)
- Test projects clearly marked (knows where to add tests)
- Project dependencies shown (understands architecture)
- Frontend is workspace-based (knows to use workspace commands)

**Total onboarding time:** 15 minutes + 0 senior dev time

**Quantified Improvement:**
- **Onboarding time:** 4.5 hours → 15 minutes (94.4% reduction)
- **Senior dev interruptions:** 30 minutes → 0 minutes (100% elimination)
- **Time to first correct code change:** Day 2 → Day 1 (2x faster)
- **Accuracy of mental model:** 70% (outdated docs) → 100% (real-time accurate)

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

## Assumptions

### Technical Assumptions

1. **SDK-Style Projects:** Repository uses modern .NET SDK-style project files (`.csproj` with `<Project Sdk="...">`) rather than legacy `.csproj` format. Task 019a parsers are optimized for SDK-style XML structure.

2. **Standard package.json Format:** Node.js projects use standard `package.json` format compliant with npm specification. Non-standard or corrupted JSON will cause parser failures.

3. **UTF-8 Encoding:** All project files (.sln, .csproj, package.json) use UTF-8 encoding. Files with other encodings (UTF-16, ANSI) may not parse correctly.

4. **File System Access:** File system is accessible via Task-014 RepoFS abstraction. Direct file system calls are not used, relying on RepoFS for reads.

5. **Bounded Scan Depth:** Repository directory structure does not exceed configured `max_depth` (default 10 levels). Extremely nested structures may not be fully scanned.

6. **Text File Sizes:** Project manifest files (.sln, .csproj, package.json) are under 1MB each. Files larger than 1MB are skipped to prevent memory exhaustion.

7. **Valid XML/JSON:** Project files are well-formed XML or JSON. Malformed files will throw parse exceptions and be reported as errors, but detection continues with other files.

8. **Standard Project Extensions:** .NET projects use standard file extensions (`.sln`, `.csproj`, `.fsproj`, `.vbproj`). Custom or non-standard extensions will not be detected unless explicitly configured.

9. **No Code Execution Required:** Detection is passive and does not require compiling or executing any code. All information is extracted from static file analysis.

10. **Symlinks Resolve Safely:** Symbolic links in the repository resolve to valid targets and do not create circular references. Circular symlink detection prevents infinite loops.

### Operational Assumptions

11. **Read Permissions:** The runtime user has read permissions for all project files and directories being scanned. Permission-denied errors are logged but do not stop detection.

12. **Single Repository Root:** Each detection operation scans a single repository root path. Multi-root scenarios (e.g., detecting across multiple cloned repositories) require multiple detection calls.

13. **Stable File System:** The file system is relatively stable during detection. Concurrent file modifications (e.g., build outputs being written) may cause inconsistent results but won't crash detection.

14. **Cache Storage Available:** Sufficient memory is available for in-memory cache. Optional disk cache requires write permissions to cache directory (default: `.agent/cache/`).

15. **Time-Based Cache Expiry:** Cached results expire based on configured TTL (default: 300 seconds). File watchers may invalidate cache earlier if project files change, but watchers are not guaranteed to trigger.

### Integration Assumptions

16. **RepoFS Availability:** Task-014 RepoFS abstraction is initialized and functional. Detection delegates all file operations to RepoFS rather than using direct System.IO.

17. **Configuration Loaded:** Task-002 configuration system has loaded `.agent/config.yml` successfully. Detection options default to safe values if configuration is missing or invalid.

18. **Logging Infrastructure:** Structured logging is available via dependency injection. Detection logs progress, errors, and warnings to configured logger (Microsoft.Extensions.Logging).

19. **No External Dependencies for Parsing:** Parsers do not depend on external tools (e.g., `dotnet` CLI, `npm` command) for parsing. All parsing is in-process using .NET libraries (System.Xml, System.Text.Json).

20. **Test Framework Conventions:** Test projects follow standard naming conventions (`.Tests`, `.UnitTests` suffixes) or include well-known test framework packages (xUnit, NUnit, MSTest, Jest). Non-standard test setups may not be detected correctly.

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

## Security Considerations

### Threat 1: Path Traversal via Malicious Project References

**Risk Description:** A malicious .csproj file contains project references with path traversal sequences (`../../etc/passwd`) that, when resolved, cause the detector to read files outside the repository.

**Attack Scenario:**
```xml
<!-- Malicious.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="../../../etc/passwd" />
    <ProjectReference Include="..\..\Windows\System32\config\SAM" />
  </ItemGroup>
</Project>
```

When detection resolves these references, it attempts to read system files outside the repository boundaries.

**Mitigation (Complete C# Implementation):**

```csharp
namespace AgenticCoder.Infrastructure.Detection.Security;

public sealed class PathTraversalValidator
{
    private readonly string _repositoryRoot;
    private readonly ILogger<PathTraversalValidator> _logger;

    public PathTraversalValidator(string repositoryRoot, ILogger<PathTraversalValidator> logger)
    {
        _repositoryRoot = Path.GetFullPath(repositoryRoot);
        _logger = logger;
    }

    public ValidationResult ValidateProjectReference(string baseProjectPath, string referencePath)
    {
        try
        {
            // Get directory of base project
            var baseDir = Path.GetDirectoryName(Path.GetFullPath(baseProjectPath));
            if (baseDir == null)
                return ValidationResult.Fail("Invalid base project path");

            // Resolve reference path relative to base project
            var absoluteReferencePath = Path.GetFullPath(Path.Combine(baseDir, referencePath));

            // Check if resolved path is within repository boundaries
            if (!absoluteReferencePath.StartsWith(_repositoryRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Path traversal detected: {Reference} resolves to {Absolute} outside repo {Root}",
                    referencePath, absoluteReferencePath, _repositoryRoot);

                return ValidationResult.Fail(
                    $"Project reference '{referencePath}' resolves outside repository boundaries");
            }

            // Check for explicit traversal patterns
            if (referencePath.Contains("..") && CountTraversals(referencePath) > 3)
            {
                _logger.LogWarning(
                    "Suspicious path with excessive traversals: {Reference}",
                    referencePath);

                return ValidationResult.Warn(
                    $"Project reference '{referencePath}' contains excessive parent directory traversals");
            }

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Path validation error for {Reference}", referencePath);
            return ValidationResult.Fail($"Path validation failed: {ex.Message}");
        }
    }

    private static int CountTraversals(string path)
    {
        return path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
            .Count(segment => segment == "..");
    }
}
```

---

### Threat 2: XML Entity Expansion (Billion Laughs Attack) in .csproj

**Risk Description:** A malicious .csproj file contains recursive XML entity definitions that expand exponentially during parsing, consuming all available memory and causing denial of service.

**Attack Scenario:**
```xml
<!DOCTYPE csproj [
  <!ENTITY lol "lol">
  <!ENTITY lol1 "&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;">
  <!ENTITY lol2 "&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;">
  <!ENTITY lol3 "&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;">
]>
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>&lol3;</TargetFramework>
  </PropertyGroup>
</Project>
```

Parsing this file causes exponential expansion: `lol3` expands to 10³ = 1,000 "lol" strings, consuming gigabytes of memory.

**Mitigation (Complete C# Implementation):**

```csharp
namespace AgenticCoder.Infrastructure.Detection.Security;

public sealed class SafeXmlParser
{
    private readonly ILogger<SafeXmlParser> _logger;
    private const int MaxDocumentSize = 1_048_576; // 1MB
    private const int MaxElementDepth = 50;

    public SafeXmlParser(ILogger<SafeXmlParser> logger)
    {
        _logger = logger;
    }

    public XDocument ParseProjectFile(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > MaxDocumentSize)
        {
            throw new XmlException(
                $"Project file exceeds maximum size of {MaxDocumentSize} bytes: {fileInfo.Length}");
        }

        var settings = new XmlReaderSettings
        {
            // Disable DTD processing to prevent entity expansion attacks
            DtdProcessing = DtdProcessing.Prohibit,

            // Prevent external resource resolution
            XmlResolver = null,

            // Limit document complexity
            MaxCharactersInDocument = MaxDocumentSize,
            MaxCharactersFromEntities = 1024,

            // Ignore whitespace and comments
            IgnoreWhitespace = true,
            IgnoreComments = true
        };

        try
        {
            using var fileStream = File.OpenRead(filePath);
            using var reader = XmlReader.Create(fileStream, settings);

            var doc = XDocument.Load(reader, LoadOptions.None);

            // Validate element depth
            var maxDepth = GetMaxDepth(doc.Root);
            if (maxDepth > MaxElementDepth)
            {
                _logger.LogWarning(
                    "Project file has excessive nesting depth: {Depth} (max: {Max})",
                    maxDepth, MaxElementDepth);

                throw new XmlException(
                    $"Project file nesting depth {maxDepth} exceeds maximum {MaxElementDepth}");
            }

            return doc;
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, "XML parsing failed for {File}", filePath);
            throw;
        }
    }

    private static int GetMaxDepth(XElement? element)
    {
        if (element == null || !element.HasElements)
            return 0;

        return 1 + element.Elements().Max(child => GetMaxDepth(child));
    }
}
```

---

### Threat 3: Malicious JSON Payload in package.json Causing Resource Exhaustion

**Risk Description:** A malicious package.json contains deeply nested objects or enormous arrays that cause the JSON parser to consume excessive memory or CPU during deserialization.

**Attack Scenario:**
```json
{
  "name": "malicious-package",
  "scripts": {
    "nested": "{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{...10000 levels deep...}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}"
  },
  "dependencies": {
    "a": "1.0.0",
    "b": "1.0.0",
    ... 50,000 more dependencies ...
  }
}
```

Parsing this file consumes gigabytes of memory as the JSON deserializer creates deeply nested object graphs.

**Mitigation (Complete C# Implementation):**

```csharp
namespace AgenticCoder.Infrastructure.Detection.Security;

public sealed class SafeJsonParser
{
    private readonly ILogger<SafeJsonParser> _logger;
    private const int MaxJsonSize = 524_288; // 512KB
    private const int MaxDepth = 32;

    public SafeJsonParser(ILogger<SafeJsonParser> logger)
    {
        _logger = logger;
    }

    public PackageJsonModel ParsePackageJson(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > MaxJsonSize)
        {
            throw new JsonException(
                $"package.json exceeds maximum size of {MaxJsonSize} bytes: {fileInfo.Length}");
        }

        var options = new JsonSerializerOptions
        {
            // Limit recursion depth to prevent stack overflow
            MaxDepth = MaxDepth,

            // Allow trailing commas (common in package.json)
            AllowTrailingCommas = true,

            // Ignore comments (JSON5-style)
            ReadCommentHandling = JsonCommentHandling.Skip,

            // Case-insensitive property names
            PropertyNameCaseInsensitive = true
        };

        try
        {
            var json = File.ReadAllText(filePath);

            // Pre-validation: check for suspiciously long lines
            var lines = json.Split('\n');
            var maxLineLength = lines.Max(l => l.Length);
            if (maxLineLength > 10_000)
            {
                _logger.LogWarning(
                    "package.json contains very long line: {Length} chars",
                    maxLineLength);
            }

            var package = JsonSerializer.Deserialize<PackageJsonModel>(json, options);
            if (package == null)
            {
                throw new JsonException("Failed to deserialize package.json");
            }

            // Post-validation: check for excessive dependencies
            var depCount = (package.Dependencies?.Count ?? 0) +
                          (package.DevDependencies?.Count ?? 0);
            if (depCount > 1000)
            {
                _logger.LogWarning(
                    "package.json declares {Count} dependencies (unusually high)",
                    depCount);
            }

            return package;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing failed for {File}", filePath);
            throw;
        }
    }
}
```

---

### Threat 4: Symlink Loop Causing Infinite Recursion

**Risk Description:** A malicious repository contains symlink loops (directory A → B → C → A) that cause the file system scanner to recurse indefinitely, consuming all stack space and crashing.

**Attack Scenario:**
```bash
# Attacker creates circular symlink structure
mkdir /repo/dir1
mkdir /repo/dir2
ln -s /repo/dir1 /repo/dir2/link_to_dir1
ln -s /repo/dir2 /repo/dir1/link_to_dir2
```

When detection scans `/repo`, it follows symlinks infinitely: `dir1 → dir2 → dir1 → dir2 → ...`

**Mitigation (Complete C# Implementation):**

```csharp
namespace AgenticCoder.Infrastructure.Detection.Security;

public sealed class SymlinkLoopDetector
{
    private readonly HashSet<string> _visitedPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<SymlinkLoopDetector> _logger;

    public SymlinkLoopDetector(ILogger<SymlinkLoopDetector> logger)
    {
        _logger = logger;
    }

    public bool IsLoopDetected(string path)
    {
        try
        {
            // Resolve symlink to absolute path
            var realPath = new DirectoryInfo(path).FullName;

            // Check if we've visited this path before
            if (_visitedPaths.Contains(realPath))
            {
                _logger.LogWarning(
                    "Symlink loop detected: {Path} already visited",
                    realPath);
                return true;
            }

            _visitedPaths.Add(realPath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking for symlink loop at {Path}", path);
            return true; // Assume loop to be safe
        }
    }

    public void Reset()
    {
        _visitedPaths.Clear();
    }

    public int GetVisitedPathCount() => _visitedPaths.Count;
}

public sealed class SafeDirectoryScanner
{
    private readonly SymlinkLoopDetector _loopDetector;
    private readonly ILogger<SafeDirectoryScanner> _logger;

    public SafeDirectoryScanner(
        SymlinkLoopDetector loopDetector,
        ILogger<SafeDirectoryScanner> logger)
    {
        _loopDetector = loopDetector;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> ScanDirectoryAsync(
        string rootPath,
        string pattern,
        int maxDepth,
        [EnumeratorCancellation] CancellationToken ct)
    {
        _loopDetector.Reset();
        await foreach (var file in ScanDirectoryRecursiveAsync(rootPath, pattern, 0, maxDepth, ct))
        {
            yield return file;
        }
    }

    private async IAsyncEnumerable<string> ScanDirectoryRecursiveAsync(
        string currentPath,
        string pattern,
        int currentDepth,
        int maxDepth,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (currentDepth > maxDepth)
        {
            _logger.LogDebug("Max depth {MaxDepth} reached at {Path}", maxDepth, currentPath);
            yield break;
        }

        // Check for symlink loops
        if (_loopDetector.IsLoopDetected(currentPath))
        {
            _logger.LogWarning("Skipping directory due to symlink loop: {Path}", currentPath);
            yield break;
        }

        // Return matching files in current directory
        foreach (var file in Directory.EnumerateFiles(currentPath, pattern))
        {
            yield return file;
        }

        // Recurse into subdirectories
        foreach (var dir in Directory.EnumerateDirectories(currentPath))
        {
            await foreach (var file in ScanDirectoryRecursiveAsync(dir, pattern, currentDepth + 1, maxDepth, ct))
            {
                yield return file;
            }
        }
    }
}
```

---

### Threat 5: Cache Poisoning via Race Condition

**Risk Description:** An attacker modifies project files while detection is running, causing inconsistent cache entries. Subsequent cache reads return partial or corrupted detection results.

**Attack Scenario:**
1. Detection starts scanning repository (takes 2 seconds)
2. At 1 second, attacker modifies `App.csproj` to add malicious references
3. Detection completes at 2 seconds with mixed state (some projects scanned before modification, some after)
4. Corrupted detection result is cached
5. Future operations use poisoned cache showing incorrect project references

**Mitigation (Complete C# Implementation):**

```csharp
namespace AgenticCoder.Infrastructure.Detection.Security;

public sealed class AtomicDetectionCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly ILogger<AtomicDetectionCache> _logger;

    public AtomicDetectionCache(ILogger<AtomicDetectionCache> logger)
    {
        _logger = logger;
    }

    public async Task<DetectionResult?> GetAsync(string rootPath, CancellationToken ct)
    {
        if (_cache.TryGetValue(rootPath, out var entry))
        {
            // Verify cache entry hasn't expired
            if (DateTimeOffset.UtcNow - entry.CachedAt < TimeSpan.FromSeconds(300))
            {
                // Verify file checksums haven't changed (anti-tampering)
                var currentChecksum = await ComputeRepositoryChecksumAsync(rootPath, ct);
                if (currentChecksum == entry.RepositoryChecksum)
                {
                    _logger.LogDebug("Cache hit for {Path}", rootPath);
                    return entry.Result;
                }

                _logger.LogInformation(
                    "Cache invalidated for {Path}: checksum mismatch (cached: {Cached}, current: {Current})",
                    rootPath, entry.RepositoryChecksum, currentChecksum);

                // Remove poisoned cache entry
                _cache.TryRemove(rootPath, out _);
            }
        }

        return null;
    }

    public async Task SetAsync(string rootPath, DetectionResult result, CancellationToken ct)
    {
        // Use lock to prevent concurrent writes
        await _writeLock.WaitAsync(ct);
        try
        {
            var checksum = await ComputeRepositoryChecksumAsync(rootPath, ct);

            var entry = new CacheEntry
            {
                Result = result,
                CachedAt = DateTimeOffset.UtcNow,
                RepositoryChecksum = checksum
            };

            _cache.AddOrUpdate(rootPath, entry, (_, _) => entry);

            _logger.LogDebug("Cached detection result for {Path} with checksum {Checksum}",
                rootPath, checksum);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static async Task<string> ComputeRepositoryChecksumAsync(string rootPath, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        var projectFiles = Directory.EnumerateFiles(rootPath, "*.*proj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(rootPath, "*.sln", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(rootPath, "package.json", SearchOption.AllDirectories))
            .Where(f => !f.Contains("node_modules"))
            .OrderBy(f => f);

        foreach (var file in projectFiles)
        {
            ct.ThrowIfCancellationRequested();

            var fileInfo = new FileInfo(file);
            var fileData = $"{file}:{fileInfo.LastWriteTimeUtc:O}:{fileInfo.Length}";
            var bytes = Encoding.UTF8.GetBytes(fileData);
            sha256.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha256.Hash!);
    }

    private sealed record CacheEntry
    {
        public required DetectionResult Result { get; init; }
        public required DateTimeOffset CachedAt { get; init; }
        public required string RepositoryChecksum { get; init; }
    }
}
```

---

## Troubleshooting

### Issue 1: Detection Fails to Find Projects

**Symptoms:**
- `acode detect` returns empty result despite projects existing
- Log shows "No .sln or .csproj files found"
- Manual `find` command shows files exist

**Causes:**
- Repository root path incorrect (scanning wrong directory)
- Detection excluded by `.gitignore` patterns (e.g., `src/` accidentally excluded)
- RepoFS permissions issue (read access denied)
- Files excluded by default ignore patterns (`node_modules/`, `bin/`, `obj/`)
- Case sensitivity mismatch on Linux (e.g., looking for `.csproj` but file is `.Csproj`)

**Solutions:**

**Solution 1: Verify Repository Root**
```bash
# Check current working directory
pwd

# Verify RepoFS is scanning correct path
acode config get repo.root

# Manually scan to verify files exist
find . -name "*.sln" -o -name "*.csproj" -o -name "package.json" | head -20
```

**Solution 2: Check Ignore Patterns**
```bash
# View current ignore patterns
acode config get detection.ignore-patterns

# Temporarily disable ignore patterns
acode detect --no-ignore

# Add custom ignore patterns (YAML config)
cat >> .acode/config.yml << 'EOF'
detection:
  ignore-patterns:
    - node_modules/
    - "**/bin/"
    - "**/obj/"
    # DO NOT add src/ or your projects won't be found!
EOF
```

**Solution 3: Enable Verbose Logging**
```bash
# Run detection with debug logging
acode detect --verbose --log-level Debug

# Check logs for permission errors
grep -i "access denied\|permission" ~/.acode/logs/detection.log

# Fix permissions if needed
chmod -R u+r /path/to/repo
```

**Solution 4: Force Case-Insensitive Search**
```bash
# On Linux, ensure case-insensitive glob matching
# (Update DetectorOptions in code)
dotnet run -- detect --case-insensitive
```

**Solution 5: Bypass Cache**
```bash
# Force fresh detection (bypass cache)
acode detect --force-refresh

# Clear detection cache manually
rm -rf ~/.acode/cache/detection/
```

---

### Issue 2: Slow Detection Performance (>10 seconds for medium repo)

**Symptoms:**
- Detection takes >10 seconds for repository with <1000 files
- CPU usage spikes to 100% during scan
- Large monorepos time out (>60 seconds)
- Log shows "Scanned 50,000 files..."

**Causes:**
- Not excluding `node_modules/` (contains 100,000+ files in large projects)
- Scanning nested `.git/` subdirectories (submodules)
- Inefficient regex patterns in ignore matching
- Cache disabled or ineffective
- Symlink loops causing infinite traversal
- Parsing every file instead of just metadata scanning

**Solutions:**

**Solution 1: Add node_modules to Ignore Patterns**
```yaml
# .acode/config.yml
detection:
  ignore-patterns:
    - "**/node_modules/**"
    - "**/.git/**"
    - "**/bower_components/**"
    - "**/vendor/**"
    - "**/__pycache__/**"
```

**Solution 2: Reduce Max Depth**
```bash
# Limit directory traversal depth
acode detect --max-depth 5

# For monorepos, detect per workspace
cd packages/frontend && acode detect
cd packages/backend && acode detect
```

**Solution 3: Enable Parallel Scanning**
```csharp
// In LayoutDetector.cs - enable parallel file enumeration
var detectionOptions = new EnumerationOptions
{
    RecurseSubdirectories = true,
    IgnoreInaccessible = true,
    MaxRecursionDepth = _options.MaxDepth,
    // Enable parallel enumeration for large directories
    ReturnSpecialDirectories = false
};

var tasks = directories.Select(async dir =>
{
    await foreach (var file in _repoFs.EnumerateFilesAsync(dir, "*.sln", detectionOptions, ct))
    {
        // Process in parallel
    }
}).ToArray();

await Task.WhenAll(tasks);
```

**Solution 4: Optimize Cache Strategy**
```bash
# Verify cache is enabled
acode config get detection.cache-enabled  # Should be true

# Increase cache TTL for stable repos
acode config set detection.cache-ttl-seconds 3600  # 1 hour

# Monitor cache hit rate
acode detect --verbose | grep "cache hit"
```

**Solution 5: Profile with Diagnostics**
```bash
# Run with performance profiling
dotnet run --configuration Release -- detect --profile

# Output:
# Detection Performance Report:
#   - File enumeration: 450ms (5,234 files)
#   - .sln parsing: 120ms (3 solutions)
#   - .csproj parsing: 890ms (47 projects)
#   - package.json parsing: 230ms (12 packages)
#   - Total: 1,690ms
```

---

### Issue 3: Cache Never Invalidates (Stale Detection Results)

**Symptoms:**
- `acode detect` returns old results after adding new projects
- Recently deleted projects still appear in detection output
- Modified `.csproj` changes not reflected
- `--force-refresh` flag has no effect

**Causes:**
- File checksum validation disabled
- Cache TTL set to infinity (`-1` or very large value)
- Cache key not considering file modification times
- Race condition in cache write operations
- Cache stored in read-only directory

**Solutions:**

**Solution 1: Verify Cache Invalidation Logic**
```csharp
// In AtomicDetectionCache.cs - ensure checksum validation is enabled
public async Task<DetectionResult?> GetAsync(string rootPath, CancellationToken ct)
{
    if (_cache.TryGetValue(rootPath, out var entry))
    {
        // CRITICAL: Always validate checksum
        var currentChecksum = await ComputeRepositoryChecksumAsync(rootPath, ct);
        if (currentChecksum != entry.RepositoryChecksum)
        {
            _logger.LogInformation("Cache invalidated: checksum mismatch for {Root}", rootPath);
            _cache.TryRemove(rootPath, out _);
            return null;  // Force re-detection
        }

        // Verify TTL hasn't expired
        if (DateTimeOffset.UtcNow - entry.Timestamp > _options.CacheTtl)
        {
            _logger.LogInformation("Cache invalidated: TTL expired for {Root}", rootPath);
            _cache.TryRemove(rootPath, out _);
            return null;
        }

        return entry.Result;
    }
    return null;
}
```

**Solution 2: Clear Cache Manually**
```bash
# Delete cache directory
rm -rf ~/.acode/cache/detection/

# Or use CLI command
acode cache clear --type detection

# Verify cache is empty
acode cache stats
# Output: Detection cache: 0 entries, 0 KB
```

**Solution 3: Reduce Cache TTL**
```bash
# Set shorter TTL for active development
acode config set detection.cache-ttl-seconds 300  # 5 minutes

# Disable cache entirely for troubleshooting
acode config set detection.cache-enabled false
```

**Solution 4: Check File System Permissions**
```bash
# Verify cache directory is writable
ls -ld ~/.acode/cache/detection/
# Should show: drwxr-xr-x (user writable)

# Fix permissions if needed
chmod u+w ~/.acode/cache/detection/
```

**Solution 5: Monitor Cache Operations**
```bash
# Enable cache debug logging
acode detect --log-level Trace | grep -i cache

# Example output:
# [TRACE] DetectionCache: GetAsync(/repo) - MISS
# [DEBUG] DetectionCache: SetAsync(/repo) - storing 47 projects, checksum abc123def
# [TRACE] DetectionCache: GetAsync(/repo) - HIT (checksum validated)
```

---

### Issue 4: XML Parsing Errors ("Invalid XML" for Valid .csproj)

**Symptoms:**
- Detection fails with "XmlException: Invalid XML"
- Error: "DTD processing is prohibited"
- `.csproj` files open fine in Visual Studio
- Manual `xmllint` validation passes

**Causes:**
- `.csproj` contains DTD declaration (legacy format)
- `.csproj` has XML comments with `--` or `<!DOCTYPE>`
- Encoding mismatch (UTF-16 file read as UTF-8)
- BOM (Byte Order Mark) causing parse failure
- XML entity references not escaped (`&`, `<`, `>` in strings)

**Solutions:**

**Solution 1: Handle Legacy Project Files**
```csharp
// In SafeXmlParser.cs - allow DTD for legacy .csproj only
public XDocument ParseProjectFile(string filePath, bool allowDtd = false)
{
    var settings = new XmlReaderSettings
    {
        DtdProcessing = allowDtd ? DtdProcessing.Ignore : DtdProcessing.Prohibit,
        XmlResolver = null,  // Still prevent external entities
        MaxCharactersInDocument = MaxDocumentSize,
        MaxCharactersFromEntities = 1024,
        IgnoreComments = true,  // Skip problematic comments
        IgnoreWhitespace = true
    };

    try
    {
        using var fileStream = File.OpenRead(filePath);
        using var reader = XmlReader.Create(fileStream, settings);
        return XDocument.Load(reader, LoadOptions.None);
    }
    catch (XmlException ex) when (ex.Message.Contains("DTD"))
    {
        // Retry with DTD allowed for legacy .csproj
        _logger.LogWarning("Retrying {File} with DTD processing enabled (legacy format)", filePath);
        return ParseProjectFile(filePath, allowDtd: true);
    }
}
```

**Solution 2: Detect and Handle BOM**
```csharp
// In CsprojParser.cs - auto-detect encoding
public ProjectMetadata Parse(string filePath)
{
    // Read raw bytes to detect BOM
    var bytes = File.ReadAllBytes(filePath);
    var encoding = DetectEncoding(bytes);

    var content = encoding.GetString(bytes);
    // Remove BOM if present
    if (content.StartsWith("\uFEFF"))
    {
        content = content.Substring(1);
    }

    using var stringReader = new StringReader(content);
    using var xmlReader = XmlReader.Create(stringReader, _xmlSettings);
    var doc = XDocument.Load(xmlReader);
    // ... parse
}

private static Encoding DetectEncoding(byte[] bytes)
{
    if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        return Encoding.Unicode;  // UTF-16 LE
    if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        return Encoding.BigEndianUnicode;  // UTF-16 BE
    if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);  // UTF-8 with BOM
    return Encoding.UTF8;
}
```

**Solution 3: Skip Invalid Files with Warning**
```bash
# Configure detection to skip unparseable files instead of failing
acode config set detection.skip-invalid-xml true

# Detection will log warnings but continue:
# [WARN] Skipping invalid project file: src/Legacy/Old.csproj (XmlException)
# [INFO] Detection complete: 46 projects (1 skipped)
```

**Solution 4: Validate XML Before Parsing**
```bash
# Manually validate problematic .csproj files
xmllint --noout src/MyProject/MyProject.csproj

# Check for common issues
grep -E "<!DOCTYPE|<!ENTITY" **/*.csproj

# Remove DTD declarations from legacy files
sed -i '/<\!DOCTYPE/d' src/Legacy/*.csproj
```

**Solution 5: Increase Error Detail Logging**
```bash
# Get full stack trace for XML errors
acode detect --log-level Debug --log-exceptions

# Example output:
# [ERROR] Failed to parse /repo/src/Legacy/Old.csproj
#   XmlException: The 'Project' start tag on line 2 does not match the end tag of 'PropertyGroup'. Line 45, position 3.
#   at System.Xml.XmlTextReaderImpl.Throw(Exception e)
#   at Acode.Infrastructure.ProjectDetection.SafeXmlParser.ParseProjectFile(String filePath)
```

---

### Issue 5: Workspace Detection Fails for npm/yarn Monorepo

**Symptoms:**
- Node.js monorepo detected as single package
- Nested `package.json` files not discovered
- Workspace members not linked to root
- `acode detect --verbose` shows "0 workspaces found"

**Causes:**
- `package.json` missing `workspaces` field
- Glob patterns in `workspaces` not expanded correctly
- `yarn.lock` exists but workspace config in `package.json` is malformed
- pnpm workspace using `pnpm-workspace.yaml` (separate file)
- Lerna config in `lerna.json` not detected

**Solutions:**

**Solution 1: Verify Workspace Configuration**
```bash
# Check if workspaces field exists
jq '.workspaces' package.json

# Valid npm/yarn workspaces format:
{
  "workspaces": [
    "packages/*",
    "apps/*"
  ]
}

# Also supports object format:
{
  "workspaces": {
    "packages": ["packages/*"],
    "nohoist": ["**/react-native"]
  }
}
```

**Solution 2: Add pnpm Workspace Support**
```csharp
// In WorkspaceDetector.cs - check pnpm-workspace.yaml
public async Task<WorkspaceInfo?> DetectWorkspaceAsync(string rootPath, CancellationToken ct)
{
    // Check npm/yarn workspaces in package.json
    var packageJsonPath = Path.Combine(rootPath, "package.json");
    if (File.Exists(packageJsonPath))
    {
        var package = await _packageJsonParser.ParseAsync(packageJsonPath, ct);
        if (package.Workspaces?.Any() == true)
        {
            return await BuildWorkspaceInfoAsync(rootPath, package.Workspaces, ct);
        }
    }

    // Check pnpm-workspace.yaml
    var pnpmWorkspacePath = Path.Combine(rootPath, "pnpm-workspace.yaml");
    if (File.Exists(pnpmWorkspacePath))
    {
        var workspaces = await ParsePnpmWorkspaceYamlAsync(pnpmWorkspacePath, ct);
        return await BuildWorkspaceInfoAsync(rootPath, workspaces, ct);
    }

    // Check lerna.json
    var lernaPath = Path.Combine(rootPath, "lerna.json");
    if (File.Exists(lernaPath))
    {
        var lernaConfig = await ParseLernaConfigAsync(lernaPath, ct);
        return await BuildWorkspaceInfoAsync(rootPath, lernaConfig.Packages, ct);
    }

    return null;  // Not a workspace
}

private async Task<List<string>> ParsePnpmWorkspaceYamlAsync(string path, CancellationToken ct)
{
    var yaml = await File.ReadAllTextAsync(path, ct);
    var deserializer = new DeserializerBuilder().Build();
    var config = deserializer.Deserialize<PnpmWorkspaceConfig>(yaml);
    return config.Packages ?? new List<string>();
}

public class PnpmWorkspaceConfig
{
    public List<string>? Packages { get; set; }
}
```

**Solution 3: Expand Glob Patterns Correctly**
```csharp
// In WorkspaceDetector.cs - use Glob library for pattern matching
private async Task<List<string>> ExpandGlobPatternAsync(string rootPath, string pattern, CancellationToken ct)
{
    // Handle both Unix-style (packages/*) and Windows-style (packages\*)
    var normalizedPattern = pattern.Replace('\\', '/');

    var matcher = new Matcher();
    matcher.AddInclude(normalizedPattern);

    var results = matcher.Execute(
        new DirectoryInfoWrapper(new DirectoryInfo(rootPath))
    );

    return results.Files
        .Select(f => Path.Combine(rootPath, f.Path))
        .Where(p => File.Exists(Path.Combine(p, "package.json")))
        .ToList();
}
```

**Solution 4: Handle Workspace Hoisting**
```bash
# Ensure detection follows symlinks in node_modules (hoisted dependencies)
acode detect --follow-symlinks

# For yarn workspaces with .pnp.cjs (Plug'n'Play)
yarn dlx acode detect  # Run via yarn to use .pnp.cjs resolution
```

**Solution 5: Manually Verify Workspace Members**
```bash
# Use npm to list workspaces
npm query .workspace | jq '.[].name'

# Use yarn workspaces
yarn workspaces list

# Use pnpm
pnpm list -r --depth 0

# Compare with Acode detection output
acode detect --format json | jq '.nodeProjects[] | select(.isWorkspaceMember) | .name'
```

---

## Testing Requirements

### Unit Tests

```csharp
using Xunit;
using FluentAssertions;
using NSubstitute;
using Acode.Infrastructure.ProjectDetection;
using Acode.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Acode.Infrastructure.Tests.ProjectDetection
{
    public sealed class LayoutDetectorTests
    {
        private readonly ILayoutDetector _sut;
        private readonly IRepoFileSystem _repoFs;
        private readonly ISlnParser _slnParser;
        private readonly ICsprojParser _csprojParser;
        private readonly IPackageJsonParser _packageJsonParser;
        private readonly IDetectionCache _cache;
        private readonly DetectorOptions _options;

        public LayoutDetectorTests()
        {
            _repoFs = Substitute.For<IRepoFileSystem>();
            _slnParser = Substitute.For<ISlnParser>();
            _csprojParser = Substitute.For<ICsprojParser>();
            _packageJsonParser = Substitute.For<IPackageJsonParser>();
            _cache = Substitute.For<IDetectionCache>();
            _options = new DetectorOptions
            {
                MaxDepth = 10,
                IgnorePatterns = new List<string> { "node_modules/", "bin/", "obj/" },
                CacheEnabled = true,
                CacheTtlSeconds = 600
            };

            _sut = new LayoutDetector(
                _repoFs,
                _slnParser,
                _csprojParser,
                _packageJsonParser,
                _cache,
                _options
            );
        }

        [Fact]
        public async Task DetectAsync_EmptyDirectory_ReturnsEmptyResult()
        {
            // Arrange
            var rootPath = "/repo";
            _repoFs.EnumerateFilesAsync(rootPath, "*.sln", Arg.Any<EnumerationOptions>(), Arg.Any<CancellationToken>())
                .Returns(AsyncEnumerable.Empty<string>());
            _repoFs.EnumerateFilesAsync(rootPath, "*.csproj", Arg.Any<EnumerationOptions>(), Arg.Any<CancellationToken>())
                .Returns(AsyncEnumerable.Empty<string>());
            _repoFs.EnumerateFilesAsync(rootPath, "package.json", Arg.Any<EnumerationOptions>(), Arg.Any<CancellationToken>())
                .Returns(AsyncEnumerable.Empty<string>());
            _cache.GetAsync(rootPath, Arg.Any<CancellationToken>()).Returns((DetectionResult?)null);

            // Act
            var result = await _sut.DetectAsync(rootPath, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Solutions.Should().BeEmpty();
            result.DotNetProjects.Should().BeEmpty();
            result.NodeProjects.Should().BeEmpty();
        }

        [Fact]
        public async Task DetectAsync_SingleSolution_ReturnsSolution()
        {
            // Arrange
            var rootPath = "/repo";
            var slnPath = "/repo/MyApp.sln";
            var slnFiles = new[] { slnPath }.ToAsyncEnumerable();

            _repoFs.EnumerateFilesAsync(rootPath, "*.sln", Arg.Any<EnumerationOptions>(), Arg.Any<CancellationToken>())
                .Returns(slnFiles);
            _repoFs.EnumerateFilesAsync(rootPath, "*.csproj", Arg.Any<EnumerationOptions>(), Arg.Any<CancellationToken>())
                .Returns(AsyncEnumerable.Empty<string>());
            _repoFs.EnumerateFilesAsync(rootPath, "package.json", Arg.Any<EnumerationOptions>(), Arg.Any<CancellationToken>())
                .Returns(AsyncEnumerable.Empty<string>());
            _cache.GetAsync(rootPath, Arg.Any<CancellationToken>()).Returns((DetectionResult?)null);

            var solutionInfo = new SolutionInfo
            {
                Path = slnPath,
                Name = "MyApp",
                Projects = new List<ProjectReference>
                {
                    new ProjectReference { Name = "MyApp.Web", Path = "/repo/src/MyApp.Web/MyApp.Web.csproj" }
                }
            };
            _slnParser.ParseAsync(slnPath, Arg.Any<CancellationToken>()).Returns(solutionInfo);

            // Act
            var result = await _sut.DetectAsync(rootPath, CancellationToken.None);

            // Assert
            result.Solutions.Should().HaveCount(1);
            result.Solutions[0].Name.Should().Be("MyApp");
            result.Solutions[0].Projects.Should().HaveCount(1);
        }

        [Fact]
        public async Task DetectAsync_MultipleSolutions_ReturnsAll()
        {
            // Arrange
            var rootPath = "/repo";
            var slnFiles = new[] { "/repo/App1.sln", "/repo/App2.sln" }.ToAsyncEnumerable();

            _repoFs.EnumerateFilesAsync(rootPath, "*.sln", Arg.Any<EnumerationOptions>(), Arg.Any<CancellationToken>())
                .Returns(slnFiles);
            _repoFs.EnumerateFilesAsync(rootPath, "*.csproj", Arg.Any<EnumerationOptions>(), Arg.Any<CancellationToken>())
                .Returns(AsyncEnumerable.Empty<string>());
            _repoFs.EnumerateFilesAsync(rootPath, "package.json", Arg.Any<EnumerationOptions>(), Arg.Any<CancellationToken>())
                .Returns(AsyncEnumerable.Empty<string>());
            _cache.GetAsync(rootPath, Arg.Any<CancellationToken>()).Returns((DetectionResult?)null);

            _slnParser.ParseAsync("/repo/App1.sln", Arg.Any<CancellationToken>())
                .Returns(new SolutionInfo { Path = "/repo/App1.sln", Name = "App1", Projects = new List<ProjectReference>() });
            _slnParser.ParseAsync("/repo/App2.sln", Arg.Any<CancellationToken>())
                .Returns(new SolutionInfo { Path = "/repo/App2.sln", Name = "App2", Projects = new List<ProjectReference>() });

            // Act
            var result = await _sut.DetectAsync(rootPath, CancellationToken.None);

            // Assert
            result.Solutions.Should().HaveCount(2);
            result.Solutions.Select(s => s.Name).Should().Contain(new[] { "App1", "App2" });
        }

        [Fact]
        public async Task DetectAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var rootPath = "/repo";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            Func<Task> act = async () => await _sut.DetectAsync(rootPath, cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task DetectAsync_WithMaxDepth_RespectsLimit()
        {
            // Arrange
            var rootPath = "/repo";
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                MaxRecursionDepth = 3
            };

            _repoFs.EnumerateFilesAsync(rootPath, "*.sln", Arg.Any<EnumerationOptions>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var enumOptions = call.Arg<EnumerationOptions>();
                    enumOptions.MaxRecursionDepth.Should().Be(3);
                    return AsyncEnumerable.Empty<string>();
                });
            _repoFs.EnumerateFilesAsync(rootPath, "*.csproj", Arg.Any<EnumerationOptions>(), Arg.Any<CancellationToken>())
                .Returns(AsyncEnumerable.Empty<string>());
            _repoFs.EnumerateFilesAsync(rootPath, "package.json", Arg.Any<EnumerationOptions>(), Arg.Any<CancellationToken>())
                .Returns(AsyncEnumerable.Empty<string>());
            _cache.GetAsync(rootPath, Arg.Any<CancellationToken>()).Returns((DetectionResult?)null);

            var detectorWithMaxDepth = new LayoutDetector(
                _repoFs,
                _slnParser,
                _csprojParser,
                _packageJsonParser,
                _cache,
                new DetectorOptions { MaxDepth = 3, CacheEnabled = false }
            );

            // Act
            var result = await detectorWithMaxDepth.DetectAsync(rootPath, CancellationToken.None);

            // Assert
            await _repoFs.Received().EnumerateFilesAsync(
                rootPath,
                "*.sln",
                Arg.Is<EnumerationOptions>(o => o.MaxRecursionDepth == 3),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task DetectAsync_ForceRefresh_BypassesCache()
        {
            // Arrange
            var rootPath = "/repo";
            var cachedResult = new DetectionResult
            {
                Solutions = new List<SolutionInfo> { new SolutionInfo { Name = "Cached" } }
            };

            _cache.GetAsync(rootPath, Arg.Any<CancellationToken>()).Returns(cachedResult);
            _repoFs.EnumerateFilesAsync(rootPath, Arg.Any<string>(), Arg.Any<EnumerationOptions>(), Arg.Any<CancellationToken>())
                .Returns(AsyncEnumerable.Empty<string>());

            // Act
            var result = await _sut.DetectAsync(rootPath, CancellationToken.None, forceRefresh: true);

            // Assert
            await _cache.DidNotReceive().GetAsync(rootPath, Arg.Any<CancellationToken>());
            result.Solutions.Should().BeEmpty();  // Fresh detection, not cached
        }

        [Fact]
        public async Task DetectAsync_ParallelCalls_ThreadSafe()
        {
            // Arrange
            var rootPath = "/repo";
            _repoFs.EnumerateFilesAsync(rootPath, Arg.Any<string>(), Arg.Any<EnumerationOptions>(), Arg.Any<CancellationToken>())
                .Returns(AsyncEnumerable.Empty<string>());
            _cache.GetAsync(rootPath, Arg.Any<CancellationToken>()).Returns((DetectionResult?)null);

            // Act
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => _sut.DetectAsync(rootPath, CancellationToken.None))
                .ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(10);
            results.Should().AllSatisfy(r => r.Should().NotBeNull());
        }
    }

    public sealed class CsprojParserTests
    {
        private readonly ICsprojParser _sut;
        private readonly IPathTraversalValidator _pathValidator;

        public CsprojParserTests()
        {
            _pathValidator = Substitute.For<IPathTraversalValidator>();
            _pathValidator.ValidateProjectReference(Arg.Any<string>(), Arg.Any<string>())
                .Returns(ValidationResult.Success());

            _sut = new CsprojParser(_pathValidator);
        }

        [Fact]
        public void Parse_ValidSdkStyleCsproj_ExtractsMetadata()
        {
            // Arrange
            var csprojPath = "/repo/src/MyApp/MyApp.csproj";
            var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <RootNamespace>MyCompany.MyApp</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />
  </ItemGroup>
</Project>";
            File.WriteAllText(csprojPath, csprojContent);

            // Act
            var metadata = _sut.Parse(csprojPath);

            // Assert
            metadata.Name.Should().Be("MyApp");
            metadata.TargetFramework.Should().Be("net8.0");
            metadata.OutputType.Should().Be("Exe");
            metadata.RootNamespace.Should().Be("MyCompany.MyApp");
            metadata.IsSdkStyle.Should().BeTrue();
            metadata.PackageReferences.Should().ContainKey("Newtonsoft.Json");
            metadata.PackageReferences["Newtonsoft.Json"].Should().Be("13.0.3");

            // Cleanup
            File.Delete(csprojPath);
        }

        [Fact]
        public void Parse_TargetFrameworks_ExtractsMultiple()
        {
            // Arrange
            var csprojPath = "/repo/src/MultiTarget/MultiTarget.csproj";
            var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net7.0;net48</TargetFrameworks>
  </PropertyGroup>
</Project>";
            File.WriteAllText(csprojPath, csprojContent);

            // Act
            var metadata = _sut.Parse(csprojPath);

            // Assert
            metadata.TargetFrameworks.Should().HaveCount(3);
            metadata.TargetFrameworks.Should().Contain(new[] { "net8.0", "net7.0", "net48" });

            // Cleanup
            File.Delete(csprojPath);
        }

        [Fact]
        public void Parse_ProjectReferences_Extracts()
        {
            // Arrange
            var csprojPath = "/repo/src/MyApp/MyApp.csproj";
            var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""../MyApp.Core/MyApp.Core.csproj"" />
    <ProjectReference Include=""../MyApp.Data/MyApp.Data.csproj"" />
  </ItemGroup>
</Project>";
            File.WriteAllText(csprojPath, csprojContent);

            // Act
            var metadata = _sut.Parse(csprojPath);

            // Assert
            metadata.ProjectReferences.Should().HaveCount(2);
            metadata.ProjectReferences.Should().Contain("../MyApp.Core/MyApp.Core.csproj");
            metadata.ProjectReferences.Should().Contain("../MyApp.Data/MyApp.Data.csproj");

            // Cleanup
            File.Delete(csprojPath);
        }

        [Fact]
        public void Parse_IsTestProject_DetectsXUnit()
        {
            // Arrange
            var csprojPath = "/repo/tests/MyApp.Tests/MyApp.Tests.csproj";
            var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""xunit"" Version=""2.6.2"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.5.4"" />
  </ItemGroup>
</Project>";
            File.WriteAllText(csprojPath, csprojContent);

            // Act
            var metadata = _sut.Parse(csprojPath);

            // Assert
            metadata.IsTestProject.Should().BeTrue();
            metadata.TestFramework.Should().Be("xunit");

            // Cleanup
            File.Delete(csprojPath);
        }

        [Fact]
        public void Parse_TestSdkReference_DetectsTest()
        {
            // Arrange
            var csprojPath = "/repo/tests/MyApp.Tests/MyApp.Tests.csproj";
            var csprojContent = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.8.0"" />
    <PackageReference Include=""NUnit"" Version=""4.0.1"" />
  </ItemGroup>
</Project>";
            File.WriteAllText(csprojPath, csprojContent);

            // Act
            var metadata = _sut.Parse(csprojPath);

            // Assert
            metadata.IsTestProject.Should().BeTrue();
            metadata.TestFramework.Should().Be("nunit");

            // Cleanup
            File.Delete(csprojPath);
        }

        [Fact]
        public void Parse_InvalidXml_ThrowsXmlException()
        {
            // Arrange
            var csprojPath = "/repo/src/Invalid/Invalid.csproj";
            var invalidXml = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  <!-- Missing closing tag -->";
            File.WriteAllText(csprojPath, invalidXml);

            // Act
            Action act = () => _sut.Parse(csprojPath);

            // Assert
            act.Should().Throw<System.Xml.XmlException>();

            // Cleanup
            File.Delete(csprojPath);
        }
    }

    public sealed class PackageJsonParserTests
    {
        private readonly IPackageJsonParser _sut;

        public PackageJsonParserTests()
        {
            _sut = new PackageJsonParser();
        }

        [Fact]
        public async Task Parse_ValidPackageJson_ExtractsMetadata()
        {
            // Arrange
            var packageJsonPath = "/repo/package.json";
            var packageJsonContent = @"{
  ""name"": ""my-app"",
  ""version"": ""1.0.0"",
  ""main"": ""index.js"",
  ""scripts"": {
    ""start"": ""node index.js"",
    ""test"": ""jest""
  },
  ""dependencies"": {
    ""express"": ""^4.18.2""
  },
  ""devDependencies"": {
    ""jest"": ""^29.7.0""
  }
}";
            File.WriteAllText(packageJsonPath, packageJsonContent);

            // Act
            var metadata = await _sut.ParseAsync(packageJsonPath, CancellationToken.None);

            // Assert
            metadata.Name.Should().Be("my-app");
            metadata.Version.Should().Be("1.0.0");
            metadata.Main.Should().Be("index.js");
            metadata.Scripts.Should().ContainKey("start");
            metadata.Scripts.Should().ContainKey("test");
            metadata.Dependencies.Should().ContainKey("express");
            metadata.DevDependencies.Should().ContainKey("jest");

            // Cleanup
            File.Delete(packageJsonPath);
        }

        [Fact]
        public async Task Parse_TestScript_DetectsTest()
        {
            // Arrange
            var packageJsonPath = "/repo/package.json";
            var packageJsonContent = @"{
  ""name"": ""my-lib"",
  ""scripts"": {
    ""test"": ""jest --coverage"",
    ""test:watch"": ""jest --watch""
  }
}";
            File.WriteAllText(packageJsonPath, packageJsonContent);

            // Act
            var metadata = await _sut.ParseAsync(packageJsonPath, CancellationToken.None);

            // Assert
            metadata.HasTestScript.Should().BeTrue();
            metadata.TestCommand.Should().Be("jest --coverage");

            // Cleanup
            File.Delete(packageJsonPath);
        }

        [Fact]
        public async Task Parse_NpmWorkspaces_DetectsWorkspaces()
        {
            // Arrange
            var packageJsonPath = "/repo/package.json";
            var packageJsonContent = @"{
  ""name"": ""monorepo"",
  ""private"": true,
  ""workspaces"": [
    ""packages/*"",
    ""apps/*""
  ]
}";
            File.WriteAllText(packageJsonPath, packageJsonContent);

            // Act
            var metadata = await _sut.ParseAsync(packageJsonPath, CancellationToken.None);

            // Assert
            metadata.IsWorkspaceRoot.Should().BeTrue();
            metadata.Workspaces.Should().HaveCount(2);
            metadata.Workspaces.Should().Contain("packages/*");
            metadata.Workspaces.Should().Contain("apps/*");

            // Cleanup
            File.Delete(packageJsonPath);
        }

        [Fact]
        public async Task Parse_YarnWorkspaces_DetectsWorkspaces()
        {
            // Arrange
            var packageJsonPath = "/repo/package.json";
            var packageJsonContent = @"{
  ""name"": ""yarn-monorepo"",
  ""private"": true,
  ""workspaces"": {
    ""packages"": [""packages/*""],
    ""nohoist"": [""**/react-native""]
  }
}";
            File.WriteAllText(packageJsonPath, packageJsonContent);

            // Act
            var metadata = await _sut.ParseAsync(packageJsonPath, CancellationToken.None);

            // Assert
            metadata.IsWorkspaceRoot.Should().BeTrue();
            metadata.Workspaces.Should().Contain("packages/*");

            // Cleanup
            File.Delete(packageJsonPath);
        }

        [Fact]
        public async Task Parse_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            var packageJsonPath = "/repo/package.json";
            var invalidJson = @"{
  ""name"": ""broken"",
  ""version"": ""1.0.0""
  // Missing closing brace";
            File.WriteAllText(packageJsonPath, invalidJson);

            // Act
            Func<Task> act = async () => await _sut.ParseAsync(packageJsonPath, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<System.Text.Json.JsonException>();

            // Cleanup
            File.Delete(packageJsonPath);
        }

        [Fact]
        public async Task Parse_MissingName_UsesDirectoryName()
        {
            // Arrange
            var packageJsonPath = "/repo/my-project/package.json";
            Directory.CreateDirectory("/repo/my-project");
            var packageJsonContent = @"{
  ""version"": ""1.0.0""
}";
            File.WriteAllText(packageJsonPath, packageJsonContent);

            // Act
            var metadata = await _sut.ParseAsync(packageJsonPath, CancellationToken.None);

            // Assert
            metadata.Name.Should().Be("my-project");

            // Cleanup
            File.Delete(packageJsonPath);
            Directory.Delete("/repo/my-project");
        }
    }

    public sealed class DetectionCacheTests
    {
        private readonly IDetectionCache _sut;
        private readonly TimeProvider _timeProvider;

        public DetectionCacheTests()
        {
            _timeProvider = Substitute.For<TimeProvider>();
            _timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);

            _sut = new DetectionCache(_timeProvider, new CacheOptions { TtlSeconds = 600 });
        }

        [Fact]
        public async Task Get_CachedResult_ReturnsCached()
        {
            // Arrange
            var rootPath = "/repo";
            var cachedResult = new DetectionResult
            {
                Solutions = new List<SolutionInfo> { new SolutionInfo { Name = "App" } }
            };

            await _sut.SetAsync(rootPath, cachedResult, "checksum123", CancellationToken.None);

            // Act
            var result = await _sut.GetAsync(rootPath, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Solutions.Should().HaveCount(1);
            result.Solutions[0].Name.Should().Be("App");
        }

        [Fact]
        public async Task Get_ExpiredResult_ReturnsNull()
        {
            // Arrange
            var rootPath = "/repo";
            var cachedResult = new DetectionResult { Solutions = new List<SolutionInfo>() };

            var initialTime = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
            _timeProvider.GetUtcNow().Returns(initialTime);

            await _sut.SetAsync(rootPath, cachedResult, "checksum123", CancellationToken.None);

            // Advance time past TTL (600 seconds)
            var expiredTime = initialTime.AddSeconds(601);
            _timeProvider.GetUtcNow().Returns(expiredTime);

            // Act
            var result = await _sut.GetAsync(rootPath, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Set_NewResult_Stores()
        {
            // Arrange
            var rootPath = "/repo";
            var result = new DetectionResult
            {
                DotNetProjects = new List<ProjectMetadata>
                {
                    new ProjectMetadata { Name = "MyApp" }
                }
            };

            // Act
            await _sut.SetAsync(rootPath, result, "checksum456", CancellationToken.None);
            var retrieved = await _sut.GetAsync(rootPath, CancellationToken.None);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.DotNetProjects.Should().HaveCount(1);
            retrieved.DotNetProjects[0].Name.Should().Be("MyApp");
        }

        [Fact]
        public async Task Invalidate_RemovesEntry()
        {
            // Arrange
            var rootPath = "/repo";
            var result = new DetectionResult();
            await _sut.SetAsync(rootPath, result, "checksum789", CancellationToken.None);

            // Act
            await _sut.InvalidateAsync(rootPath, CancellationToken.None);
            var retrieved = await _sut.GetAsync(rootPath, CancellationToken.None);

            // Assert
            retrieved.Should().BeNull();
        }

        [Fact]
        public async Task Get_ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            var rootPath = "/repo";
            var result = new DetectionResult();
            await _sut.SetAsync(rootPath, result, "checksumABC", CancellationToken.None);

            // Act
            var tasks = Enumerable.Range(0, 100)
                .Select(_ => _sut.GetAsync(rootPath, CancellationToken.None))
                .ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(100);
            results.Should().AllSatisfy(r => r.Should().NotBeNull());
        }
    }
}
```

### Integration Tests

```csharp
using Xunit;
using FluentAssertions;
using Acode.Infrastructure.ProjectDetection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Acode.Infrastructure.Tests.ProjectDetection.Integration
{
    [Collection("FileSystem")]
    public sealed class LayoutDetectorIntegrationTests : IAsyncLifetime
    {
        private string _testRepoPath = null!;
        private ILayoutDetector _sut = null!;

        public async Task InitializeAsync()
        {
            _testRepoPath = Path.Combine(Path.GetTempPath(), "acode-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testRepoPath);

            var repoFs = new RepoFileSystem(_testRepoPath);
            var slnParser = new SlnParser();
            var csprojParser = new CsprojParser(new PathTraversalValidator(_testRepoPath));
            var packageJsonParser = new PackageJsonParser();
            var cache = new InMemoryDetectionCache();

            _sut = new LayoutDetector(
                repoFs,
                slnParser,
                csprojParser,
                packageJsonParser,
                cache,
                new DetectorOptions { MaxDepth = 10, CacheEnabled = false }
            );

            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (Directory.Exists(_testRepoPath))
            {
                Directory.Delete(_testRepoPath, recursive: true);
            }
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Detect_RealDotNetRepo_FindsAllProjects()
        {
            // Arrange
            var srcDir = Path.Combine(_testRepoPath, "src");
            Directory.CreateDirectory(srcDir);

            var webProjDir = Path.Combine(srcDir, "MyApp.Web");
            Directory.CreateDirectory(webProjDir);
            var webCsproj = Path.Combine(webProjDir, "MyApp.Web.csproj");
            File.WriteAllText(webCsproj, @"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

            var coreProjDir = Path.Combine(srcDir, "MyApp.Core");
            Directory.CreateDirectory(coreProjDir);
            var coreCsproj = Path.Combine(coreProjDir, "MyApp.Core.csproj");
            File.WriteAllText(coreCsproj, @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");

            // Act
            var result = await _sut.DetectAsync(_testRepoPath, CancellationToken.None);

            // Assert
            result.DotNetProjects.Should().HaveCount(2);
            result.DotNetProjects.Should().Contain(p => p.Name == "MyApp.Web");
            result.DotNetProjects.Should().Contain(p => p.Name == "MyApp.Core");
        }

        [Fact]
        public async Task Detect_RealNodeRepo_FindsAllPackages()
        {
            // Arrange
            var rootPackageJson = Path.Combine(_testRepoPath, "package.json");
            File.WriteAllText(rootPackageJson, @"{
  ""name"": ""my-node-app"",
  ""version"": ""1.0.0"",
  ""scripts"": {
    ""start"": ""node index.js"",
    ""test"": ""jest""
  }
}");

            // Act
            var result = await _sut.DetectAsync(_testRepoPath, CancellationToken.None);

            // Assert
            result.NodeProjects.Should().HaveCount(1);
            result.NodeProjects[0].Name.Should().Be("my-node-app");
            result.NodeProjects[0].HasTestScript.Should().BeTrue();
        }

        [Fact]
        public async Task Detect_MonorepoWithWorkspaces_MapsHierarchy()
        {
            // Arrange
            var rootPackageJson = Path.Combine(_testRepoPath, "package.json");
            File.WriteAllText(rootPackageJson, @"{
  ""name"": ""monorepo"",
  ""private"": true,
  ""workspaces"": [""packages/*""]
}");

            var packagesDir = Path.Combine(_testRepoPath, "packages");
            Directory.CreateDirectory(packagesDir);

            var package1Dir = Path.Combine(packagesDir, "package1");
            Directory.CreateDirectory(package1Dir);
            File.WriteAllText(Path.Combine(package1Dir, "package.json"), @"{
  ""name"": ""@monorepo/package1"",
  ""version"": ""1.0.0""
}");

            // Act
            var result = await _sut.DetectAsync(_testRepoPath, CancellationToken.None);

            // Assert
            result.NodeProjects.Should().HaveCount(2);  // Root + package1
            result.NodeProjects.Should().Contain(p => p.Name == "monorepo" && p.IsWorkspaceRoot);
            result.NodeProjects.Should().Contain(p => p.Name == "@monorepo/package1" && p.IsWorkspaceMember);
        }

        [Fact]
        public async Task Detect_MixedRepo_FindsBothTypes()
        {
            // Arrange
            var srcDir = Path.Combine(_testRepoPath, "src");
            Directory.CreateDirectory(srcDir);

            var backendDir = Path.Combine(srcDir, "Backend");
            Directory.CreateDirectory(backendDir);
            File.WriteAllText(Path.Combine(backendDir, "Backend.csproj"), @"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
</Project>");

            var frontendDir = Path.Combine(srcDir, "Frontend");
            Directory.CreateDirectory(frontendDir);
            File.WriteAllText(Path.Combine(frontendDir, "package.json"), @"{
  ""name"": ""frontend"",
  ""scripts"": {""build"": ""vite build""}
}");

            // Act
            var result = await _sut.DetectAsync(_testRepoPath, CancellationToken.None);

            // Assert
            result.DotNetProjects.Should().HaveCount(1);
            result.NodeProjects.Should().HaveCount(1);
            result.DotNetProjects[0].Name.Should().Be("Backend");
            result.NodeProjects[0].Name.Should().Be("frontend");
        }

        [Fact]
        public async Task Detect_LargeRepo_CompletesInTime()
        {
            // Arrange - Create 100 projects
            for (int i = 0; i < 100; i++)
            {
                var projDir = Path.Combine(_testRepoPath, $"Project{i}");
                Directory.CreateDirectory(projDir);
                File.WriteAllText(Path.Combine(projDir, $"Project{i}.csproj"), @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
</Project>");
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var result = await _sut.DetectAsync(_testRepoPath, CancellationToken.None);

            sw.Stop();

            // Assert
            result.DotNetProjects.Should().HaveCount(100);
            sw.ElapsedMilliseconds.Should().BeLessThan(5000);  // Should complete in < 5 seconds
        }
    }
}
```

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