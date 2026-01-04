# Task 034.a: GitHub Actions Templates (.NET/Node)

**Priority:** P1 – High  
**Tier:** L – Feature Layer  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 8 – CI/CD Integration  
**Dependencies:** Task 034 (CI Template Generator)  

---

## Description

Task 034.a implements GitHub Actions templates for .NET and Node.js. Templates MUST be production-ready. Best practices MUST be followed.

Each stack has specific requirements. .NET uses dotnet CLI. Node.js uses npm/yarn. Templates MUST adapt to project structure.

This task covers template content. Security settings are in 034.b. Caching is in 034.c.

### Business Value

Stack-specific templates provide:
- Correct build commands
- Appropriate tooling
- Framework-specific optimizations
- Reduced configuration errors

### Scope Boundaries

This task covers template content. Pinned versions are in 034.b. Caching is in 034.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Template Generator | `ICiTemplateGenerator` | Requests stack template | From 034 |
| Stack Registry | `ICiStackProvider` | Provides stack-specific config | Per-stack |
| Project Detector | `IProjectDetector` | Scans for project files | Auto-detect |
| Version Detector | `IVersionDetector` | Extracts SDK/runtime versions | From project |
| Security Module | Task 034.b | Applies pinned versions, permissions | Required |
| Caching Module | Task 034.c | Adds dependency caching | Optional |
| YAML Renderer | `IYamlRenderer` | Produces final YAML | Common |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Project detection fails | No recognized files | Prompt user for stack | Interactive fallback |
| SDK version unknown | Cannot parse project file | Use latest stable version | Warning logged |
| Missing project file | File not found | Error with instructions | Clear message |
| Multiple stacks detected | >1 stack type | Prompt user to choose | Interactive selection |
| Invalid package.json | JSON parse error | Error with file path | User fixes file |
| Unsupported .NET version | Version < 6.0 | Suggest upgrade | Warning with docs link |
| Monorepo too complex | Too many entry points | Simplified template | Manual adjustment needed |
| Test project not found | No test patterns | Skip test step | Warning logged |

### Mode Compliance

| Operating Mode | Template Generation | Project Detection |
|----------------|---------------------|-------------------|
| Local-Only | ALLOWED | ALLOWED |
| Burst | ALLOWED | ALLOWED |
| Air-Gapped | ALLOWED | ALLOWED |

### Assumptions

1. **Standard project structure**: Projects follow conventional directory layouts
2. **Single SDK per project**: One primary SDK version per project
3. **Standard test patterns**: Test projects use `.Tests` or `__tests__` naming
4. **Package manager lockfiles present**: npm-lock.json, yarn.lock, or pnpm-lock.yaml exist
5. **GitHub Actions runner available**: Ubuntu-latest has required SDKs
6. **Network access for packages**: CI can download NuGet/npm packages
7. **Repository root is workspace**: Generator runs from repo root
8. **Single CI workflow per stack**: One build workflow per technology stack

### Security Considerations

1. **No credentials in templates**: Templates use secrets references only
2. **Minimal checkout depth**: Use shallow clone for speed
3. **Locked dependency versions**: Use lockfiles in CI
4. **No arbitrary script execution**: Only predefined build commands
5. **Safe environment variables**: No user input in run commands
6. **Trusted actions only**: Use verified GitHub actions
7. **Version pinning enforced**: All actions use SHA or version tag
8. **Read-only permissions default**: Write only when needed

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| SDK | Software Development Kit |
| Runtime | Execution environment |
| Package Manager | Dependency tool (NuGet, npm) |
| Build Matrix | Multi-config builds |
| Artifact | Build output |

---

## Out of Scope

- Python templates
- Java/Gradle templates
- Rust templates
- Go templates
- Ruby templates
- PHP templates

---

## Functional Requirements

### FR-001 to FR-020: .NET Templates

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-034A-01 | .NET template provider MUST exist implementing `ICiStackProvider` | P0 |
| FR-034A-02 | .NET version MUST be detected from project file | P0 |
| FR-034A-03 | `<TargetFramework>` element MUST be parsed from .csproj | P1 |
| FR-034A-04 | .NET 6, 7, 8 MUST be supported as target frameworks | P0 |
| FR-034A-05 | `actions/setup-dotnet` action MUST be used in template | P0 |
| FR-034A-06 | .NET version MUST be from project or use latest stable | P1 |
| FR-034A-07 | `dotnet restore` step MUST be included | P0 |
| FR-034A-08 | `dotnet build` step MUST be included | P0 |
| FR-034A-09 | `dotnet test` step MUST be included | P0 |
| FR-034A-10 | Test results MUST support upload to GitHub | P1 |
| FR-034A-11 | Code coverage MUST be optional step | P2 |
| FR-034A-12 | `dotnet publish` MUST be optional step | P2 |
| FR-034A-13 | Publish artifacts MUST support upload | P2 |
| FR-034A-14 | NuGet pack MUST be optional step | P2 |
| FR-034A-15 | Multi-project solutions MUST be supported | P1 |
| FR-034A-16 | Solution file (.sln) MUST be auto-detected | P1 |
| FR-034A-17 | Project dependencies MUST be resolved correctly | P1 |
| FR-034A-18 | `global.json` MUST be respected for SDK version | P1 |
| FR-034A-19 | `Directory.Build.props` MUST be respected | P2 |
| FR-034A-20 | Windows runner MUST be optional for Windows-only projects | P2 |

### FR-021 to FR-040: Node.js Templates

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-034A-21 | Node.js template provider MUST exist implementing `ICiStackProvider` | P0 |
| FR-034A-22 | Node.js version MUST be detected from project | P0 |
| FR-034A-23 | Version MUST be read from `.nvmrc` or `package.json` engines | P1 |
| FR-034A-24 | `actions/setup-node` action MUST be used in template | P0 |
| FR-034A-25 | npm package manager MUST be supported | P0 |
| FR-034A-26 | yarn package manager MUST be supported | P1 |
| FR-034A-27 | pnpm package manager MUST be supported | P2 |
| FR-034A-28 | Package manager MUST be auto-detected from lockfile | P1 |
| FR-034A-29 | Detection MUST check for package-lock.json, yarn.lock, pnpm-lock.yaml | P1 |
| FR-034A-30 | `npm ci` MUST be preferred over `npm install` | P1 |
| FR-034A-31 | `npm run build` step MUST be included | P0 |
| FR-034A-32 | `npm test` step MUST be included | P0 |
| FR-034A-33 | Lint step MUST be optional | P2 |
| FR-034A-34 | TypeScript builds MUST be supported | P1 |
| FR-034A-35 | Monorepo support MUST be available | P2 |
| FR-034A-36 | Workspace detection for npm/yarn/pnpm MUST work | P2 |
| FR-034A-37 | Build artifacts MUST support upload | P2 |
| FR-034A-38 | Coverage reports MUST support upload | P2 |
| FR-034A-39 | E2E tests MUST be optional step | P2 |
| FR-034A-40 | Browser testing setup MUST be optional | P2 |

### FR-041 to FR-055: Project Detection

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-034A-41 | Auto-detect stack MUST scan repository root | P0 |
| FR-034A-42 | Scan MUST check for known project file patterns | P0 |
| FR-034A-43 | `.csproj` file MUST indicate .NET stack | P0 |
| FR-034A-44 | `package.json` file MUST indicate Node.js stack | P0 |
| FR-034A-45 | Both present MUST prompt user for selection | P1 |
| FR-034A-46 | User override MUST be available via `--stack` flag | P1 |
| FR-034A-47 | Version detection MUST extract from project files | P1 |
| FR-034A-48 | Multiple projects MUST be detected and listed | P1 |
| FR-034A-49 | Monorepo patterns MUST be recognized | P2 |
| FR-034A-50 | Test projects MUST be detected separately | P1 |
| FR-034A-51 | `.Tests` suffix MUST indicate .NET test project | P1 |
| FR-034A-52 | `__tests__` directory MUST indicate Node.js tests | P1 |
| FR-034A-53 | Entry point project MUST be identified | P1 |
| FR-034A-54 | Main project MUST be used for build commands | P1 |
| FR-034A-55 | Build order MUST be determined from dependencies | P2 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-034A-01 | Project detection latency | <1 second | P0 |
| NFR-034A-02 | Template rendering latency | <500ms | P0 |
| NFR-034A-03 | Version extraction latency | <200ms | P1 |
| NFR-034A-04 | File scanning for detection | <50 files | P2 |
| NFR-034A-05 | Memory during detection | <20MB | P2 |
| NFR-034A-06 | Parallel project scanning | Supported | P2 |
| NFR-034A-07 | Monorepo detection timeout | <5 seconds | P2 |
| NFR-034A-08 | Package manager detection | <100ms | P1 |
| NFR-034A-09 | Solution parsing time | <500ms | P1 |
| NFR-034A-10 | Template cache hits | >90% | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-034A-11 | Valid for current GitHub Actions | Always | P0 |
| NFR-034A-12 | Actions use up-to-date versions | Latest stable | P0 |
| NFR-034A-13 | Best practices followed | Always | P0 |
| NFR-034A-14 | Clear structure for editing | Human-readable | P1 |
| NFR-034A-15 | Helpful comments in output | Yes | P1 |
| NFR-034A-16 | Extensible template system | Plugin-based | P1 |
| NFR-034A-17 | Easy customization support | Override options | P1 |
| NFR-034A-18 | Version matrix optional | Configurable | P2 |
| NFR-034A-19 | Graceful fallback on error | Use defaults | P1 |
| NFR-034A-20 | Detection accuracy | >95% | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-034A-21 | Structured logging for detection | JSON format | P1 |
| NFR-034A-22 | Metrics on stack detection | Per-stack | P2 |
| NFR-034A-23 | Events on project detected | Async publish | P1 |
| NFR-034A-24 | Clear error on detection fail | Actionable | P0 |
| NFR-034A-25 | Version extraction logged | Debug level | P2 |
| NFR-034A-26 | Package manager logged | Info level | P1 |
| NFR-034A-27 | Monorepo detection logged | Info level | P2 |
| NFR-034A-28 | Template selection logged | Info level | P1 |
| NFR-034A-29 | Test project count logged | Debug level | P2 |
| NFR-034A-30 | Build order logged | Debug level | P2 |

---

## User Manual Documentation

### .NET Workflow Example

```yaml
name: .NET Build and Test
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
permissions:
  contents: read
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal
```

### Node.js Workflow Example

```yaml
name: Node.js Build and Test
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
permissions:
  contents: read
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
      - name: Install dependencies
        run: npm ci
      - name: Build
        run: npm run build
      - name: Test
        run: npm test
```

---

## Acceptance Criteria / Definition of Done

### .NET Template Provider
- [ ] AC-001: `DotNetTemplateProvider` implements `ICiStackProvider`
- [ ] AC-002: .NET version detected from .csproj
- [ ] AC-003: `<TargetFramework>` parsed correctly
- [ ] AC-004: .NET 6, 7, 8 all supported
- [ ] AC-005: `actions/setup-dotnet` included
- [ ] AC-006: Version from project or latest stable
- [ ] AC-007: `dotnet restore` step present
- [ ] AC-008: `dotnet build` step present
- [ ] AC-009: `dotnet test` step present
- [ ] AC-010: Test results upload optional
- [ ] AC-011: Coverage step optional

### Node.js Template Provider
- [ ] AC-012: `NodeJsTemplateProvider` implements `ICiStackProvider`
- [ ] AC-013: Node version detected from project
- [ ] AC-014: `.nvmrc` parsed correctly
- [ ] AC-015: `package.json` engines.node parsed
- [ ] AC-016: `actions/setup-node` included
- [ ] AC-017: npm package manager works
- [ ] AC-018: yarn package manager works
- [ ] AC-019: pnpm package manager works
- [ ] AC-020: Package manager auto-detected
- [ ] AC-021: `npm ci` used instead of install
- [ ] AC-022: `npm run build` step present
- [ ] AC-023: `npm test` step present

### Project Detection
- [ ] AC-024: Auto-detect scans repo root
- [ ] AC-025: .csproj detected as .NET
- [ ] AC-026: package.json detected as Node.js
- [ ] AC-027: Both present prompts user
- [ ] AC-028: `--stack` override works
- [ ] AC-029: Version extraction works
- [ ] AC-030: Multiple projects detected
- [ ] AC-031: Test projects identified

### Solution Support
- [ ] AC-032: .sln file auto-detected
- [ ] AC-033: All projects in solution found
- [ ] AC-034: Project dependencies resolved
- [ ] AC-035: `global.json` respected
- [ ] AC-036: Build targets correct

### Package Manager Detection
- [ ] AC-037: package-lock.json → npm
- [ ] AC-038: yarn.lock → yarn
- [ ] AC-039: pnpm-lock.yaml → pnpm
- [ ] AC-040: Correct install command used

### Monorepo Support
- [ ] AC-041: npm workspaces detected
- [ ] AC-042: yarn workspaces detected
- [ ] AC-043: .NET solution with multiple projects
- [ ] AC-044: Entry point identified

### Template Output
- [ ] AC-045: Workflow name correct
- [ ] AC-046: Steps in correct order
- [ ] AC-047: Comments helpful
- [ ] AC-048: YAML valid
- [ ] AC-049: Artifacts upload works
- [ ] AC-050: Coverage reports work

---

## User Verification Scenarios

### Scenario 1: .NET Console App
**Persona:** Developer with simple .NET project  
**Preconditions:** Repository with single .csproj  
**Steps:**
1. Run `acode ci generate --stack dotnet`
2. Check setup-dotnet action
3. Verify restore/build/test steps
4. Confirm .NET version correct

**Verification Checklist:**
- [ ] .NET version from .csproj
- [ ] `dotnet restore` present
- [ ] `dotnet build --no-restore`
- [ ] `dotnet test --no-build`

### Scenario 2: .NET Solution with Tests
**Persona:** Developer with multi-project solution  
**Preconditions:** Repository with .sln and Test project  
**Steps:**
1. Run `acode ci generate`
2. Stack auto-detected as .NET
3. Solution file used
4. Test project included

**Verification Checklist:**
- [ ] .sln detected
- [ ] All projects built
- [ ] Tests run
- [ ] Correct build order

### Scenario 3: Node.js with npm
**Persona:** Developer with package.json  
**Preconditions:** Repository with package-lock.json  
**Steps:**
1. Run `acode ci generate`
2. Stack auto-detected as Node.js
3. npm detected from lockfile
4. `npm ci` used

**Verification Checklist:**
- [ ] Node.js detected
- [ ] npm selected
- [ ] `npm ci` not `npm install`
- [ ] Build and test steps

### Scenario 4: Node.js with yarn
**Persona:** Developer using yarn  
**Preconditions:** Repository with yarn.lock  
**Steps:**
1. Run `acode ci generate`
2. Stack auto-detected
3. yarn detected from lockfile
4. `yarn install --frozen-lockfile`

**Verification Checklist:**
- [ ] yarn selected
- [ ] Frozen lockfile flag
- [ ] Correct build command
- [ ] Correct test command

### Scenario 5: TypeScript Project
**Persona:** Developer with TS project  
**Preconditions:** tsconfig.json present  
**Steps:**
1. Run `acode ci generate --stack node`
2. TypeScript detected
3. Build step includes compile
4. Tests run on compiled code

**Verification Checklist:**
- [ ] TypeScript recognized
- [ ] Build compiles TS
- [ ] Tests pass
- [ ] Output correct

### Scenario 6: Mixed Stack Repository
**Persona:** Developer with .NET and Node.js  
**Preconditions:** Both .csproj and package.json  
**Steps:**
1. Run `acode ci generate`
2. Prompted to choose stack
3. Select .NET
4. Only .NET workflow generated

**Verification Checklist:**
- [ ] Both detected
- [ ] User prompted
- [ ] Selection respected
- [ ] Single workflow output

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-034A-01 | .NET version parsing from TargetFramework | FR-034A-03 |
| UT-034A-02 | Node version parsing from .nvmrc | FR-034A-23 |
| UT-034A-03 | Node version parsing from package.json | FR-034A-23 |
| UT-034A-04 | Package manager detection npm | FR-034A-28 |
| UT-034A-05 | Package manager detection yarn | FR-034A-28 |
| UT-034A-06 | Package manager detection pnpm | FR-034A-28 |
| UT-034A-07 | .csproj indicates .NET | FR-034A-43 |
| UT-034A-08 | package.json indicates Node | FR-034A-44 |
| UT-034A-09 | .sln file parsing | FR-034A-16 |
| UT-034A-10 | Test project detection .NET | FR-034A-51 |
| UT-034A-11 | Test project detection Node | FR-034A-52 |
| UT-034A-12 | global.json parsing | FR-034A-18 |
| UT-034A-13 | Monorepo detection | FR-034A-49 |
| UT-034A-14 | Template rendering .NET | NFR-034A-02 |
| UT-034A-15 | Template rendering Node | NFR-034A-02 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-034A-01 | Real .NET project generation | E2E |
| IT-034A-02 | Real Node.js project generation | E2E |
| IT-034A-03 | Multi-project .NET solution | FR-034A-15 |
| IT-034A-04 | Monorepo detection npm workspaces | FR-034A-35 |
| IT-034A-05 | Mixed stack detection | FR-034A-45 |
| IT-034A-06 | TypeScript project | FR-034A-34 |
| IT-034A-07 | yarn project | FR-034A-26 |
| IT-034A-08 | pnpm project | FR-034A-27 |
| IT-034A-09 | Artifacts upload step | FR-034A-13 |
| IT-034A-10 | Coverage step .NET | FR-034A-11 |
| IT-034A-11 | Coverage step Node | FR-034A-38 |
| IT-034A-12 | Windows runner .NET | FR-034A-20 |
| IT-034A-13 | E2E tests step | FR-034A-39 |
| IT-034A-14 | Detection under 1 second | NFR-034A-01 |
| IT-034A-15 | Template valid for GitHub | NFR-034A-11 |

---

## Implementation Prompt

### Part 1: File Structure + Domain Models

```
src/
├── Acode.Domain/
│   └── CiCd/
│       └── Templates/
│           └── Stacks/
│               ├── PackageManager.cs
│               └── Events/
│                   ├── ProjectDetectedEvent.cs
│                   └── StackDetectionFailedEvent.cs
├── Acode.Application/
│   └── CiCd/
│       └── Templates/
│           └── Stacks/
│               ├── ICiStackProvider.cs
│               ├── IProjectDetector.cs
│               ├── IStackDetector.cs
│               └── ProjectInfo.cs
└── Acode.Infrastructure/
    └── CiCd/
        └── Templates/
            └── Stacks/
                ├── DotNetTemplateProvider.cs
                ├── NodeJsTemplateProvider.cs
                ├── CompositeProjectDetector.cs
                └── Detectors/
                    ├── DotNetDetector.cs
                    └── NodeJsDetector.cs
```

```csharp
// src/Acode.Domain/CiCd/Templates/Stacks/PackageManager.cs
namespace Acode.Domain.CiCd.Templates.Stacks;

public enum PackageManager
{
    NuGet,
    Npm,
    Yarn,
    Pnpm
}

// src/Acode.Domain/CiCd/Templates/Stacks/Events/ProjectDetectedEvent.cs
namespace Acode.Domain.CiCd.Templates.Stacks.Events;

public sealed record ProjectDetectedEvent(
    TechStack Stack,
    string Version,
    PackageManager PackageManager,
    int ProjectCount,
    int TestProjectCount,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 034.a Specification - Part 1/3**

### Part 2: Application Interfaces

```csharp
// src/Acode.Application/CiCd/Templates/Stacks/ProjectInfo.cs
namespace Acode.Application.CiCd.Templates.Stacks;

public sealed record ProjectInfo
{
    public required TechStack Stack { get; init; }
    public required string Version { get; init; }
    public required string ProjectPath { get; init; }
    public PackageManager PackageManager { get; init; }
    public IReadOnlyList<string> Projects { get; init; } = [];
    public IReadOnlyList<string> TestProjects { get; init; } = [];
    public bool IsMonorepo { get; init; } = false;
    public string? SolutionFile { get; init; }
}

// src/Acode.Application/CiCd/Templates/Stacks/IStackDetector.cs
namespace Acode.Application.CiCd.Templates.Stacks;

public interface IStackDetector
{
    TechStack Stack { get; }
    int Priority { get; }
    bool CanDetect(string path);
    Task<ProjectInfo?> DetectAsync(string path, CancellationToken ct = default);
}

// src/Acode.Application/CiCd/Templates/Stacks/IProjectDetector.cs
namespace Acode.Application.CiCd.Templates.Stacks;

public interface IProjectDetector
{
    Task<ProjectInfo?> DetectAsync(string path, CancellationToken ct = default);
    Task<IReadOnlyList<ProjectInfo>> DetectAllAsync(string path, CancellationToken ct = default);
}

// src/Acode.Application/CiCd/Templates/Stacks/ICiStackProvider.cs
namespace Acode.Application.CiCd.Templates.Stacks;

public interface ICiStackProvider
{
    TechStack Stack { get; }
    
    Task<CiWorkflow> GenerateAsync(
        CiTemplateRequest request,
        ProjectInfo project,
        CancellationToken ct = default);
    
    IReadOnlyList<CiJob> GetDefaultJobs(ProjectInfo project);
}
```

**End of Task 034.a Specification - Part 2/3**

### Part 3: Infrastructure Implementation + Checklist

```csharp
// src/Acode.Infrastructure/CiCd/Templates/Stacks/Detectors/DotNetDetector.cs
namespace Acode.Infrastructure.CiCd.Templates.Stacks.Detectors;

public sealed class DotNetDetector : IStackDetector
{
    public TechStack Stack => TechStack.DotNet;
    public int Priority => 10;
    
    public bool CanDetect(string path) =>
        Directory.EnumerateFiles(path, "*.csproj", SearchOption.AllDirectories).Any() ||
        Directory.EnumerateFiles(path, "*.sln", SearchOption.TopDirectoryOnly).Any();
    
    public async Task<ProjectInfo?> DetectAsync(string path, CancellationToken ct)
    {
        var slnFiles = Directory.GetFiles(path, "*.sln", SearchOption.TopDirectoryOnly);
        var csprojFiles = Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories);
        
        if (csprojFiles.Length == 0) return null;
        
        var version = await ParseTargetFrameworkAsync(csprojFiles[0], ct);
        var testProjects = csprojFiles.Where(f => f.Contains(".Tests") || f.Contains(".Test")).ToList();
        
        return new ProjectInfo
        {
            Stack = TechStack.DotNet,
            Version = version ?? "8.0",
            ProjectPath = path,
            PackageManager = PackageManager.NuGet,
            Projects = csprojFiles.ToList(),
            TestProjects = testProjects,
            SolutionFile = slnFiles.FirstOrDefault()
        };
    }
    
    private static async Task<string?> ParseTargetFrameworkAsync(string csprojPath, CancellationToken ct)
    {
        var content = await File.ReadAllTextAsync(csprojPath, ct);
        var match = Regex.Match(content, @"<TargetFramework>net(\d+\.\d+)</TargetFramework>");
        return match.Success ? match.Groups[1].Value : null;
    }
}

// src/Acode.Infrastructure/CiCd/Templates/Stacks/Detectors/NodeJsDetector.cs
namespace Acode.Infrastructure.CiCd.Templates.Stacks.Detectors;

public sealed class NodeJsDetector : IStackDetector
{
    public TechStack Stack => TechStack.Node;
    public int Priority => 10;
    
    public bool CanDetect(string path) =>
        File.Exists(Path.Combine(path, "package.json"));
    
    public async Task<ProjectInfo?> DetectAsync(string path, CancellationToken ct)
    {
        var packageJsonPath = Path.Combine(path, "package.json");
        if (!File.Exists(packageJsonPath)) return null;
        
        var version = await ParseNodeVersionAsync(path, ct);
        var packageManager = DetectPackageManager(path);
        var hasTests = Directory.Exists(Path.Combine(path, "__tests__")) ||
                       Directory.Exists(Path.Combine(path, "tests"));
        
        return new ProjectInfo
        {
            Stack = TechStack.Node,
            Version = version ?? "20",
            ProjectPath = path,
            PackageManager = packageManager,
            Projects = [packageJsonPath],
            TestProjects = hasTests ? ["tests"] : [],
            IsMonorepo = File.Exists(Path.Combine(path, "pnpm-workspace.yaml")) ||
                        File.Exists(Path.Combine(path, "lerna.json"))
        };
    }
    
    private static PackageManager DetectPackageManager(string path)
    {
        if (File.Exists(Path.Combine(path, "pnpm-lock.yaml"))) return PackageManager.Pnpm;
        if (File.Exists(Path.Combine(path, "yarn.lock"))) return PackageManager.Yarn;
        return PackageManager.Npm;
    }
}

// src/Acode.Infrastructure/CiCd/Templates/Stacks/DotNetTemplateProvider.cs
namespace Acode.Infrastructure.CiCd.Templates.Stacks;

public sealed class DotNetTemplateProvider : ICiStackProvider
{
    public TechStack Stack => TechStack.DotNet;
    
    public IReadOnlyList<CiJob> GetDefaultJobs(ProjectInfo project) =>
    [
        new CiJob
        {
            Id = "build",
            Name = "Build and Test",
            Runner = "ubuntu-latest",
            Steps =
            [
                "uses: actions/checkout@v4",
                $"uses: actions/setup-dotnet@v4\n        with:\n          dotnet-version: '{project.Version}.x'",
                "run: dotnet restore",
                "run: dotnet build --no-restore",
                "run: dotnet test --no-build --verbosity normal"
            ]
        }
    ];
}
```

### Implementation Checklist

| Step | Action | Verification |
|------|--------|--------------|
| 1 | Create PackageManager enum | Enum compiles |
| 2 | Add detection events | Event serialization verified |
| 3 | Define ProjectInfo record | Record compiles |
| 4 | Create IStackDetector, IProjectDetector, ICiStackProvider | Interfaces clear |
| 5 | Implement DotNetDetector | .csproj parsing works |
| 6 | Parse TargetFramework | .NET version extracted |
| 7 | Implement NodeJsDetector | package.json detection works |
| 8 | Detect package manager from lockfile | npm/yarn/pnpm detected |
| 9 | Implement DotNetTemplateProvider | .NET workflow renders |
| 10 | Implement NodeJsTemplateProvider | Node workflow renders |
| 11 | Implement CompositeProjectDetector | Multi-stack detection works |
| 12 | Add test project detection | .Tests suffix, __tests__ folder |
| 13 | Add monorepo detection | Workspaces detected |
| 14 | Register detectors and providers in DI | All resolved |

### Rollout Plan

1. **Phase 1**: Implement DotNetDetector with .csproj parsing
2. **Phase 2**: Implement NodeJsDetector with package manager detection
3. **Phase 3**: Build DotNetTemplateProvider with full workflow
4. **Phase 4**: Build NodeJsTemplateProvider with npm/yarn/pnpm support
5. **Phase 5**: Add CompositeProjectDetector and multi-stack support

**End of Task 034.a Specification**