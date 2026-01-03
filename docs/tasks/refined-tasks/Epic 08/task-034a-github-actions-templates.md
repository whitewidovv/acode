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

- Task 034: Part of generator
- Task 034.b: Security settings applied
- Task 034.c: Caching added

### Failure Modes

- Project detection fails → Ask user
- SDK version unknown → Use latest
- Missing project file → Error

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

- FR-001: .NET template MUST exist
- FR-002: Detect .NET version from project
- FR-003: Parse `<TargetFramework>` element
- FR-004: Support .NET 6, 7, 8
- FR-005: `actions/setup-dotnet` MUST be used
- FR-006: Version from project or latest
- FR-007: `dotnet restore` MUST run
- FR-008: `dotnet build` MUST run
- FR-009: `dotnet test` MUST run
- FR-010: Test results MUST upload
- FR-011: Code coverage MUST be optional
- FR-012: `dotnet publish` MUST be optional
- FR-013: Publish artifacts MUST upload
- FR-014: NuGet pack MUST be optional
- FR-015: Multi-project solutions MUST work
- FR-016: Solution file detected
- FR-017: Project dependencies resolved
- FR-018: Global.json respected
- FR-019: Directory.Build.props respected
- FR-020: Windows runner option

### FR-021 to FR-040: Node.js Templates

- FR-021: Node.js template MUST exist
- FR-022: Detect Node version
- FR-023: From `.nvmrc` or `package.json`
- FR-024: `actions/setup-node` MUST be used
- FR-025: npm MUST be supported
- FR-026: yarn MUST be supported
- FR-027: pnpm MUST be supported
- FR-028: Package manager auto-detected
- FR-029: From lockfile presence
- FR-030: `npm ci` preferred over install
- FR-031: `npm run build` MUST run
- FR-032: `npm test` MUST run
- FR-033: Lint step MUST be optional
- FR-034: TypeScript builds supported
- FR-035: Monorepo support MUST work
- FR-036: Workspace detection
- FR-037: Build artifacts MUST upload
- FR-038: Coverage reports MUST upload
- FR-039: E2E tests MUST be optional
- FR-040: Browser testing setup optional

### FR-041 to FR-055: Project Detection

- FR-041: Auto-detect stack MUST work
- FR-042: Scan for project files
- FR-043: `.csproj` → .NET
- FR-044: `package.json` → Node.js
- FR-045: Both present → Ask user
- FR-046: Override MUST work
- FR-047: Version detection MUST work
- FR-048: Multiple projects MUST work
- FR-049: Monorepo patterns MUST detect
- FR-050: Test project detection
- FR-051: `.Tests` suffix for .NET
- FR-052: `__tests__` for Node.js
- FR-053: Entry point detection
- FR-054: Main project identified
- FR-055: Build order determined

---

## Non-Functional Requirements

- NFR-001: Detection <1 second
- NFR-002: Template rendering <500ms
- NFR-003: Valid for current GitHub
- NFR-004: Actions up to date
- NFR-005: Best practices followed
- NFR-006: Clear structure
- NFR-007: Helpful comments
- NFR-008: Extensible templates
- NFR-009: Easy customization
- NFR-010: Version matrix optional

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

- [ ] AC-001: .NET template generates
- [ ] AC-002: .NET version detected
- [ ] AC-003: Build and test work
- [ ] AC-004: Node.js template generates
- [ ] AC-005: Node version detected
- [ ] AC-006: npm/yarn/pnpm work
- [ ] AC-007: Project auto-detected
- [ ] AC-008: Monorepo works
- [ ] AC-009: Artifacts upload
- [ ] AC-010: Comments helpful

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: .NET version parsing
- [ ] UT-002: Node version parsing
- [ ] UT-003: Project detection
- [ ] UT-004: Package manager detection

### Integration Tests

- [ ] IT-001: Real .NET project
- [ ] IT-002: Real Node project
- [ ] IT-003: Monorepo detection
- [ ] IT-004: Multi-stack repo

---

## Implementation Prompt

### Templates

```csharp
public class DotNetTemplateProvider : ICiStackProvider
{
    public string Stack => "dotnet";
    
    public async Task<CiWorkflow> GenerateAsync(
        CiTemplateRequest request,
        ProjectInfo project,
        CancellationToken ct);
}

public class NodeJsTemplateProvider : ICiStackProvider
{
    public string Stack => "node";
    
    public async Task<CiWorkflow> GenerateAsync(
        CiTemplateRequest request,
        ProjectInfo project,
        CancellationToken ct);
}

public record ProjectInfo(
    string Stack,
    string Version,
    string ProjectPath,
    string PackageManager,
    IReadOnlyList<string> Projects,
    IReadOnlyList<string> TestProjects);
```

### Detector

```csharp
public interface IProjectDetector
{
    Task<ProjectInfo> DetectAsync(
        string path,
        CancellationToken ct);
}

public class CompositeProjectDetector : IProjectDetector
{
    private readonly IEnumerable<IStackDetector> _detectors;
}

public interface IStackDetector
{
    string Stack { get; }
    int Priority { get; }
    Task<ProjectInfo> DetectAsync(string path, CancellationToken ct);
}
```

---

**End of Task 034.a Specification**