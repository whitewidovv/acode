# Task 034: CI Template Generator

**Priority:** P1 – High  
**Tier:** L – Feature Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 8 – CI/CD Integration  
**Dependencies:** Epic 05 (Git Automation)  

---

## Description

Task 034 implements the CI template generator. Workflows MUST be generated for common platforms. GitHub Actions is the primary target. Templates MUST follow best practices.

The generator creates complete, production-ready CI workflows. Templates are customizable. Generated workflows MUST be valid and secure.

This task provides the generation infrastructure. Subtasks cover specific platforms and optimizations.

### Business Value

CI template generation enables:
- Faster project setup
- Consistent CI practices
- Security by default
- Reduced manual configuration

### Scope Boundaries

This task covers template generation. Maintenance is in Task 035. Deployment hooks are in Task 036.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Platform Registry | `ICiPlatformRegistry` | Get available platforms | Extensible |
| GitHub Actions | `GitHubActionsProvider` | Generate YAML workflow | Primary target |
| YAML Validator | `IYamlValidator` | Validate generated output | Schema-based |
| Git Automation | `IGitService` | Commit generated files | From Epic 05 |
| Configuration | `IOptionsSnapshot<>` | Load template settings | Hot-reloadable |
| Event Bus | `IEventPublisher` | Publish generation events | Async |
| CLI Handler | `CiGenerateCommand` | User interface | Spectre.Console |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Unknown platform | Registry lookup fails | Error with supported list | Clear message |
| Unknown stack | Stack detector fails | Ask user to specify | Interactive prompt |
| Invalid YAML output | Schema validation fails | Log and abort | No file written |
| Output path not writable | IOException on write | Error with permission info | Manual fix needed |
| Template variable unresolved | Missing variable exception | Error with variable name | User provides value |
| Git commit blocked | Operating mode check | Skip commit, warn user | File saved locally |
| Action reference invalid | Action validator fails | Warn, use known-good | Degraded generation |
| Network unavailable | Version lookup timeout | Use cached versions | May be outdated |

### Mode Compliance

| Mode | Generation | Commit |
|------|------------|--------|
| local-only | ALLOWED | BLOCKED |
| airgapped | ALLOWED | BLOCKED |
| burst | ALLOWED | ALLOWED |

### Assumptions

1. **GitHub Actions primary target**: Initial implementation focuses on GitHub Actions only
2. **YAML output format**: All generated workflows are YAML files
3. **Repository root accessible**: Generator can write to `.github/workflows/`
4. **Network for version lookup**: GitHub API accessible for action SHA resolution
5. **Configuration file exists**: `agent-config.yml` provides template settings
6. **Git initialized**: Repository has `.git` directory for commit operations
7. **Single platform per generation**: One CI platform per `generate` command
8. **Supported stacks defined**: Known list of supported technology stacks

### Security Considerations

1. **No secrets in generated files**: Templates never include actual secret values
2. **Minimal permissions default**: Generated workflows use `contents: read` by default
3. **Action pinning enforced**: All action references use SHA or version tags
4. **Template injection prevention**: Variables are escaped before YAML rendering
5. **Output path validation**: Cannot write outside repository directory
6. **Audit trail for generation**: All generations logged with user identity
7. **No code execution**: Template rendering is pure string manipulation
8. **Dry-run safe**: Preview mode never modifies filesystem

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Workflow | CI pipeline definition |
| Job | Workflow unit |
| Step | Job action |
| Matrix | Multi-config builds |
| Trigger | Event that starts workflow |
| Action | Reusable step component |

---

## Out of Scope

- GitLab CI/CD
- Azure DevOps
- Jenkins pipelines
- CircleCI
- Travis CI
- Custom CI platforms

---

## Functional Requirements

### FR-001 to FR-020: Generator Infrastructure

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-034-01 | `ICiTemplateGenerator` interface MUST exist in Application layer | P0 |
| FR-034-02 | `GenerateAsync` MUST create complete workflow from request | P0 |
| FR-034-03 | Input MUST be `CiTemplateRequest` with platform, stack, options | P0 |
| FR-034-04 | Output MUST be `CiWorkflow` with content and metadata | P0 |
| FR-034-05 | Generator MUST support extensible platform providers | P1 |
| FR-034-06 | Platform-specific generators MUST implement `ICiPlatformProvider` | P1 |
| FR-034-07 | Stack-specific templates MUST be pluggable per platform | P1 |
| FR-034-08 | `ListSupportedPlatforms` MUST return available platforms | P1 |
| FR-034-09 | `ListSupportedStacks` MUST return available stacks per platform | P1 |
| FR-034-10 | Invalid platform MUST throw `UnsupportedPlatformException` | P0 |
| FR-034-11 | Invalid stack MUST throw `UnsupportedStackException` | P0 |
| FR-034-12 | Templates MUST support customization via options | P1 |
| FR-034-13 | User options MUST override default template values | P1 |
| FR-034-14 | Generated YAML MUST be syntactically valid | P0 |
| FR-034-15 | YAML syntax validation MUST run before output | P0 |
| FR-034-16 | Platform schema validation MUST verify workflow structure | P1 |
| FR-034-17 | Action references MUST be validated against known actions | P1 |
| FR-034-18 | Secrets MUST NOT be hardcoded in generated output | P0 |
| FR-034-19 | Generator MUST log all operations with structured data | P1 |
| FR-034-20 | Generator MUST emit metrics for monitoring dashboards | P2 |

### FR-021 to FR-040: Workflow Structure

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-034-21 | Workflow `name` field MUST be set in generated output | P0 |
| FR-034-22 | Name MUST be derived from project name or user config | P1 |
| FR-034-23 | Workflow triggers MUST be defined via `on` block | P0 |
| FR-034-24 | Default triggers MUST be `push` and `pull_request` | P1 |
| FR-034-25 | Branch filters MUST be configurable in triggers | P1 |
| FR-034-26 | Default branches MUST be `main` and `develop` | P2 |
| FR-034-27 | Path filters MUST be optional in triggers | P2 |
| FR-034-28 | Jobs section MUST contain at least one job | P0 |
| FR-034-29 | Build job MUST be included by default | P0 |
| FR-034-30 | Test job MUST be included by default | P0 |
| FR-034-31 | Job dependencies MUST be configurable | P1 |
| FR-034-32 | Dependencies MUST use `needs` keyword | P1 |
| FR-034-33 | Runner MUST be specified for each job | P0 |
| FR-034-34 | Default runner MUST be `ubuntu-latest` | P1 |
| FR-034-35 | Build matrix MUST be optional for multi-config | P2 |
| FR-034-36 | Matrix OS variants MUST be configurable | P2 |
| FR-034-37 | Matrix version variants MUST be configurable | P2 |
| FR-034-38 | Concurrency controls MUST be set in workflow | P1 |
| FR-034-39 | Duplicate runs MUST be prevented via concurrency | P1 |
| FR-034-40 | Cancel in-progress MUST be configurable | P2 |

### FR-041 to FR-055: Output Handling

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-034-41 | Output path MUST be configurable via options | P1 |
| FR-034-42 | Default output path MUST be `.github/workflows/` | P1 |
| FR-034-43 | Filename MUST be auto-generated from workflow name | P1 |
| FR-034-44 | Filename MUST be kebab-case with `.yml` extension | P1 |
| FR-034-45 | Existing file MUST trigger warning before overwrite | P1 |
| FR-034-46 | Overwrite MUST be opt-in via `--force` flag | P1 |
| FR-034-47 | Dry-run mode MUST be available via `--dry-run` | P1 |
| FR-034-48 | Dry-run MUST output to console without file write | P1 |
| FR-034-49 | Template variables MUST resolve before output | P0 |
| FR-034-50 | Missing required variables MUST throw exception | P0 |
| FR-034-51 | Helpful comments MUST be added to generated YAML | P2 |
| FR-034-52 | Comments MUST explain key configuration sections | P2 |
| FR-034-53 | Output MUST be formatted with consistent indentation | P1 |
| FR-034-54 | YAML style MUST be consistent across generators | P1 |
| FR-034-55 | Structure MUST be human-readable and editable | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-034-01 | Template generation latency | <2 seconds | P0 |
| NFR-034-02 | YAML validation latency | <500ms | P1 |
| NFR-034-03 | Platform registry lookup | <10ms | P2 |
| NFR-034-04 | File write operation | <100ms | P1 |
| NFR-034-05 | Memory usage during generation | <50MB | P2 |
| NFR-034-06 | Parallel generation support | 5 concurrent | P2 |
| NFR-034-07 | Variable resolution time | <50ms | P2 |
| NFR-034-08 | Schema validation | <200ms | P1 |
| NFR-034-09 | Action lookup (cached) | <10ms | P2 |
| NFR-034-10 | Action lookup (network) | <2 seconds | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-034-11 | Valid YAML output guarantee | 100% | P0 |
| NFR-034-12 | Security best practices applied | 100% | P0 |
| NFR-034-13 | Minimal permissions by default | Always | P0 |
| NFR-034-14 | Pinned action versions | Always | P0 |
| NFR-034-15 | No partial file writes | Atomic | P0 |
| NFR-034-16 | Dry-run never modifies files | 100% | P0 |
| NFR-034-17 | Extensible platform registry | Plugin-based | P1 |
| NFR-034-18 | Backward compatible output | Current GitHub | P1 |
| NFR-034-19 | Template versioning support | Configurable | P2 |
| NFR-034-20 | Graceful degradation on error | Fail-fast | P1 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-034-21 | Structured logging for generation | JSON format | P1 |
| NFR-034-22 | Metrics on generation count | Per-platform | P1 |
| NFR-034-23 | Metrics on generation latency | Histogram | P2 |
| NFR-034-24 | Event emission for success/failure | Async publish | P1 |
| NFR-034-25 | Human-readable output format | Comments | P1 |
| NFR-034-26 | Clear error messages | Actionable | P0 |
| NFR-034-27 | Trace correlation for generation | Request ID | P2 |
| NFR-034-28 | Audit log for file writes | Full history | P1 |
| NFR-034-29 | Dashboard metrics support | Prometheus | P2 |
| NFR-034-30 | Generation statistics in CLI | Summary | P2 |

---

## User Manual Documentation

### Configuration

```yaml
ciTemplates:
  defaultPlatform: github-actions
  defaultRunner: ubuntu-latest
  outputPath: .github/workflows
  pinVersions: true
  minimalPermissions: true
```

### CLI Usage

```bash
# Generate CI workflow
acode ci generate --stack dotnet

# Generate with options
acode ci generate --stack node \
  --name "Build and Test" \
  --branches main,develop

# Dry-run (preview only)
acode ci generate --stack dotnet --dry-run

# List supported stacks
acode ci templates list
```

### Generated Workflow Example

```yaml
name: Build and Test
on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]
permissions:
  contents: read
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet build
      - run: dotnet test
```

---

## Acceptance Criteria / Definition of Done

### Generator Interface
- [ ] AC-001: `ICiTemplateGenerator` interface exists in Application layer
- [ ] AC-002: `GenerateAsync` accepts `CiTemplateRequest` and returns `CiWorkflow`
- [ ] AC-003: `ListSupportedPlatforms` returns available platforms
- [ ] AC-004: `ListSupportedStacks` returns stacks per platform
- [ ] AC-005: Invalid platform throws `UnsupportedPlatformException`
- [ ] AC-006: Invalid stack throws `UnsupportedStackException`
- [ ] AC-007: Generator is extensible via `ICiPlatformProvider`
- [ ] AC-008: Options override default template values

### Platform Support
- [ ] AC-009: GitHub Actions platform is supported
- [ ] AC-010: Platform registry resolves providers correctly
- [ ] AC-011: Platform-specific templates render correctly
- [ ] AC-012: Unknown platform gives clear error message
- [ ] AC-013: Platform capabilities are queryable

### Stack Support
- [ ] AC-014: .NET stack template generates correctly
- [ ] AC-015: Node.js stack template generates correctly
- [ ] AC-016: Stack-specific build commands are correct
- [ ] AC-017: Stack version detection works
- [ ] AC-018: Unknown stack gives clear error message

### Workflow Structure
- [ ] AC-019: Workflow name is set from config
- [ ] AC-020: Triggers include push and pull_request
- [ ] AC-021: Branch filters are configurable
- [ ] AC-022: Default branches are main, develop
- [ ] AC-023: Build job exists
- [ ] AC-024: Test job exists
- [ ] AC-025: Job dependencies use `needs`
- [ ] AC-026: Runner is `ubuntu-latest` by default
- [ ] AC-027: Concurrency prevents duplicate runs

### Output Handling
- [ ] AC-028: Default output path is `.github/workflows/`
- [ ] AC-029: Filename is kebab-case with `.yml`
- [ ] AC-030: Existing file triggers warning
- [ ] AC-031: `--force` flag enables overwrite
- [ ] AC-032: `--dry-run` outputs to console only
- [ ] AC-033: Template variables resolve correctly
- [ ] AC-034: Missing variables throw exception

### Validation
- [ ] AC-035: Generated YAML is syntactically valid
- [ ] AC-036: Schema validation passes
- [ ] AC-037: Action references are validated
- [ ] AC-038: No hardcoded secrets in output

### Matrix Build
- [ ] AC-039: Matrix is optional
- [ ] AC-040: Matrix OS variants work
- [ ] AC-041: Matrix version variants work
- [ ] AC-042: Matrix generates correct YAML structure

### Observability
- [ ] AC-043: Generation logged with structured data
- [ ] AC-044: Metrics emitted for generation count
- [ ] AC-045: Events published on completion
- [ ] AC-046: Errors include actionable messages

### Documentation
- [ ] AC-047: Comments added to generated YAML
- [ ] AC-048: Comments explain key sections
- [ ] AC-049: Output is human-readable
- [ ] AC-050: CLI help is complete

---

## User Verification Scenarios

### Scenario 1: Generate .NET Workflow
**Persona:** Developer with .NET project  
**Preconditions:** Repository with .csproj file  
**Steps:**
1. Run `acode ci generate --stack dotnet`
2. Review generated workflow
3. Check YAML validity
4. Verify build/test steps

**Verification Checklist:**
- [ ] File created at `.github/workflows/`
- [ ] Workflow name set correctly
- [ ] `dotnet restore`, `build`, `test` steps present
- [ ] YAML is valid

### Scenario 2: Generate Node.js Workflow
**Persona:** Developer with Node.js project  
**Preconditions:** Repository with package.json  
**Steps:**
1. Run `acode ci generate --stack node`
2. Review generated workflow
3. Check npm commands
4. Verify test step

**Verification Checklist:**
- [ ] File created correctly
- [ ] `npm ci`, `npm run build`, `npm test` present
- [ ] Node version configured
- [ ] YAML is valid

### Scenario 3: Dry-Run Preview
**Persona:** Developer reviewing before commit  
**Preconditions:** Any repository  
**Steps:**
1. Run `acode ci generate --stack dotnet --dry-run`
2. Review console output
3. Verify no file written
4. Check for errors

**Verification Checklist:**
- [ ] YAML displayed in console
- [ ] No file created
- [ ] Output is formatted
- [ ] Can copy/paste if needed

### Scenario 4: Custom Options
**Persona:** Developer with specific requirements  
**Preconditions:** Repository with .csproj  
**Steps:**
1. Run `acode ci generate --stack dotnet --name "Custom Build" --branches main,develop,release`
2. Check workflow name
3. Check branch triggers
4. Verify customizations applied

**Verification Checklist:**
- [ ] Workflow name is "Custom Build"
- [ ] Branches include all three
- [ ] Filename reflects name
- [ ] Other defaults preserved

### Scenario 5: Matrix Build Setup
**Persona:** Library maintainer  
**Preconditions:** Cross-platform .NET library  
**Steps:**
1. Run `acode ci generate --stack dotnet --matrix-os ubuntu,windows,macos`
2. Check matrix structure
3. Verify all runners included
4. Confirm parallel jobs

**Verification Checklist:**
- [ ] Matrix block generated
- [ ] Three OS variants
- [ ] `${{ matrix.os }}` used
- [ ] Jobs run in parallel

### Scenario 6: Handle Existing Workflow
**Persona:** Developer updating CI  
**Preconditions:** Existing workflow file  
**Steps:**
1. Run `acode ci generate --stack dotnet`
2. See warning about existing file
3. Run with `--force` to overwrite
4. Verify new content

**Verification Checklist:**
- [ ] Warning displayed
- [ ] No overwrite without flag
- [ ] `--force` overwrites
- [ ] Content is new version

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-034-01 | Template rendering produces valid YAML | FR-034-14 |
| UT-034-02 | YAML validation catches syntax errors | FR-034-15 |
| UT-034-03 | Options override default values | FR-034-13 |
| UT-034-04 | Variable resolution works correctly | FR-034-49 |
| UT-034-05 | Missing variable throws exception | FR-034-50 |
| UT-034-06 | Platform registry returns providers | FR-034-08 |
| UT-034-07 | Unsupported platform throws | FR-034-10 |
| UT-034-08 | Unsupported stack throws | FR-034-11 |
| UT-034-09 | Filename generation kebab-case | FR-034-44 |
| UT-034-10 | Job dependencies use needs | FR-034-32 |
| UT-034-11 | Concurrency block generated | FR-034-38 |
| UT-034-12 | Matrix structure valid | FR-034-35 |
| UT-034-13 | Comments added to output | FR-034-51 |
| UT-034-14 | Default runner is ubuntu-latest | FR-034-34 |
| UT-034-15 | Generation completes <2 seconds | NFR-034-01 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-034-01 | Full .NET workflow generation | E2E |
| IT-034-02 | Full Node.js workflow generation | E2E |
| IT-034-03 | File output to correct path | FR-034-42 |
| IT-034-04 | Dry-run doesn't write file | FR-034-48 |
| IT-034-05 | Force flag overwrites | FR-034-46 |
| IT-034-06 | Existing file warning | FR-034-45 |
| IT-034-07 | Git commit integration | Epic 05 |
| IT-034-08 | Multiple stacks in sequence | Multiple |
| IT-034-09 | Matrix with 3 OS variants | FR-034-36 |
| IT-034-10 | GitHub Actions schema valid | NFR-034-18 |
| IT-034-11 | Event emission on generate | NFR-034-24 |
| IT-034-12 | Metrics recorded | NFR-034-22 |
| IT-034-13 | CLI help displays correctly | AC-050 |
| IT-034-14 | List templates command | FR-034-09 |
| IT-034-15 | Error handling for bad input | NFR-034-20 |

---

## Implementation Prompt

### Part 1: File Structure + Domain Models

```
src/
├── Acode.Domain/
│   └── CiCd/
│       └── Templates/
│           ├── CiPlatform.cs
│           ├── TechStack.cs
│           └── Events/
│               ├── WorkflowGeneratedEvent.cs
│               └── WorkflowValidationFailedEvent.cs
├── Acode.Application/
│   └── CiCd/
│       └── Templates/
│           ├── ICiTemplateGenerator.cs
│           ├── ICiPlatformProvider.cs
│           ├── ICiPlatformRegistry.cs
│           ├── CiTemplateRequest.cs
│           ├── CiOptions.cs
│           ├── CiWorkflow.cs
│           └── CiJob.cs
└── Acode.Infrastructure/
    └── CiCd/
        └── Templates/
            ├── CiTemplateGenerator.cs
            ├── CiPlatformRegistry.cs
            ├── YamlValidator.cs
            └── Providers/
                └── GitHubActionsProvider.cs
```

```csharp
// src/Acode.Domain/CiCd/Templates/CiPlatform.cs
namespace Acode.Domain.CiCd.Templates;

public enum CiPlatform
{
    GitHubActions,
    GitLabCi,
    AzureDevOps,
    Jenkins
}

// src/Acode.Domain/CiCd/Templates/TechStack.cs
namespace Acode.Domain.CiCd.Templates;

public enum TechStack
{
    DotNet,
    Node,
    Python,
    Java,
    Go,
    Rust
}

// src/Acode.Domain/CiCd/Templates/Events/WorkflowGeneratedEvent.cs
namespace Acode.Domain.CiCd.Templates.Events;

public sealed record WorkflowGeneratedEvent(
    string WorkflowName,
    CiPlatform Platform,
    TechStack Stack,
    string OutputPath,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/CiCd/Templates/Events/WorkflowValidationFailedEvent.cs
namespace Acode.Domain.CiCd.Templates.Events;

public sealed record WorkflowValidationFailedEvent(
    string WorkflowName,
    IReadOnlyList<string> Errors,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 034 Specification - Part 1/3**

### Part 2: Application Interfaces

```csharp
// src/Acode.Application/CiCd/Templates/CiOptions.cs
namespace Acode.Application.CiCd.Templates;

public sealed record CiOptions
{
    public string? Name { get; init; }
    public IReadOnlyList<string> Branches { get; init; } = ["main", "develop"];
    public IReadOnlyList<string> PathFilters { get; init; } = [];
    public bool IncludeMatrix { get; init; } = false;
    public string Runner { get; init; } = "ubuntu-latest";
    public bool PinVersions { get; init; } = true;
    public bool MinimalPermissions { get; init; } = true;
    public IReadOnlyDictionary<string, string> Variables { get; init; } = new Dictionary<string, string>();
}

// src/Acode.Application/CiCd/Templates/CiTemplateRequest.cs
namespace Acode.Application.CiCd.Templates;

public sealed record CiTemplateRequest
{
    public required CiPlatform Platform { get; init; }
    public required TechStack Stack { get; init; }
    public required string ProjectPath { get; init; }
    public CiOptions? Options { get; init; }
    public string? OutputPath { get; init; }
    public bool DryRun { get; init; } = false;
}

// src/Acode.Application/CiCd/Templates/CiJob.cs
namespace Acode.Application.CiCd.Templates;

public sealed record CiJob
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Runner { get; init; } = "ubuntu-latest";
    public IReadOnlyList<string> Steps { get; init; } = [];
    public IReadOnlyList<string> Dependencies { get; init; } = [];
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? Matrix { get; init; }
}

// src/Acode.Application/CiCd/Templates/CiWorkflow.cs
namespace Acode.Application.CiCd.Templates;

public sealed record CiWorkflow
{
    public required string Name { get; init; }
    public required string Filename { get; init; }
    public required string Content { get; init; }
    public IReadOnlyList<string> Triggers { get; init; } = ["push", "pull_request"];
    public IReadOnlyList<CiJob> Jobs { get; init; } = [];
    public IReadOnlyList<string> Permissions { get; init; } = ["contents: read"];
}

// src/Acode.Application/CiCd/Templates/ICiPlatformProvider.cs
namespace Acode.Application.CiCd.Templates;

public interface ICiPlatformProvider
{
    CiPlatform Platform { get; }
    IReadOnlyList<TechStack> SupportedStacks { get; }
    Task<string> RenderAsync(CiWorkflow workflow, CancellationToken ct = default);
}

// src/Acode.Application/CiCd/Templates/ICiPlatformRegistry.cs
namespace Acode.Application.CiCd.Templates;

public interface ICiPlatformRegistry
{
    void Register(ICiPlatformProvider provider);
    ICiPlatformProvider? Get(CiPlatform platform);
    IReadOnlyList<ICiPlatformProvider> GetAll();
}

// src/Acode.Application/CiCd/Templates/ICiTemplateGenerator.cs
namespace Acode.Application.CiCd.Templates;

public interface ICiTemplateGenerator
{
    Task<CiWorkflow> GenerateAsync(CiTemplateRequest request, CancellationToken ct = default);
    IReadOnlyList<CiPlatform> SupportedPlatforms { get; }
    IReadOnlyList<TechStack> SupportedStacks { get; }
    Task<ValidationResult> ValidateAsync(CiWorkflow workflow, CancellationToken ct = default);
}
```

**End of Task 034 Specification - Part 2/3**

### Part 3: Infrastructure Implementation + Checklist

```csharp
// src/Acode.Infrastructure/CiCd/Templates/YamlValidator.cs
namespace Acode.Infrastructure.CiCd.Templates;

public sealed class YamlValidator
{
    public ValidationResult Validate(string yamlContent, CiPlatform platform)
    {
        var errors = new List<string>();
        
        try
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(yamlContent));
            
            // Platform-specific validation
            if (platform == CiPlatform.GitHubActions)
                ValidateGitHubActionsSchema(yaml, errors);
        }
        catch (YamlException ex)
        {
            errors.Add($"YAML syntax error: {ex.Message}");
        }
        
        return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }
}

// src/Acode.Infrastructure/CiCd/Templates/Providers/GitHubActionsProvider.cs
namespace Acode.Infrastructure.CiCd.Templates.Providers;

public sealed class GitHubActionsProvider : ICiPlatformProvider
{
    public CiPlatform Platform => CiPlatform.GitHubActions;
    public IReadOnlyList<TechStack> SupportedStacks => [TechStack.DotNet, TechStack.Node, TechStack.Python, TechStack.Go];
    
    public Task<string> RenderAsync(CiWorkflow workflow, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"name: {workflow.Name}");
        sb.AppendLine();
        sb.AppendLine("on:");
        foreach (var trigger in workflow.Triggers)
            sb.AppendLine($"  {trigger}:");
        sb.AppendLine();
        sb.AppendLine("permissions:");
        foreach (var perm in workflow.Permissions)
            sb.AppendLine($"  {perm}");
        sb.AppendLine();
        sb.AppendLine("jobs:");
        foreach (var job in workflow.Jobs)
            RenderJob(sb, job);
        
        return Task.FromResult(sb.ToString());
    }
    
    private static void RenderJob(StringBuilder sb, CiJob job)
    {
        sb.AppendLine($"  {job.Id}:");
        sb.AppendLine($"    name: {job.Name}");
        sb.AppendLine($"    runs-on: {job.Runner}");
        if (job.Dependencies.Count > 0)
            sb.AppendLine($"    needs: [{string.Join(", ", job.Dependencies)}]");
        sb.AppendLine("    steps:");
        foreach (var step in job.Steps)
            sb.AppendLine($"      - {step}");
    }
}

// src/Acode.Infrastructure/CiCd/Templates/CiTemplateGenerator.cs
namespace Acode.Infrastructure.CiCd.Templates;

public sealed class CiTemplateGenerator : ICiTemplateGenerator
{
    private readonly ICiPlatformRegistry _registry;
    private readonly YamlValidator _validator;
    private readonly IEventPublisher _events;
    
    public IReadOnlyList<CiPlatform> SupportedPlatforms => _registry.GetAll().Select(p => p.Platform).ToList();
    public IReadOnlyList<TechStack> SupportedStacks => Enum.GetValues<TechStack>().ToList();
    
    public async Task<CiWorkflow> GenerateAsync(CiTemplateRequest request, CancellationToken ct)
    {
        var provider = _registry.Get(request.Platform) 
            ?? throw new NotSupportedException($"Platform {request.Platform} not supported");
        
        var options = request.Options ?? new CiOptions();
        var workflow = BuildWorkflow(request, options);
        workflow = workflow with { Content = await provider.RenderAsync(workflow, ct) };
        
        var validation = await ValidateAsync(workflow, ct);
        if (!validation.IsValid)
            await _events.PublishAsync(new WorkflowValidationFailedEvent(workflow.Name, validation.Errors, DateTimeOffset.UtcNow), ct);
        
        if (!request.DryRun)
            await _events.PublishAsync(new WorkflowGeneratedEvent(workflow.Name, request.Platform, request.Stack, request.OutputPath ?? ".github/workflows", DateTimeOffset.UtcNow), ct);
        
        return workflow;
    }
}
```

### Implementation Checklist

| Step | Action | Verification |
|------|--------|--------------|
| 1 | Create domain enums (CiPlatform, TechStack) | Enums compile |
| 2 | Add generation events | Event serialization verified |
| 3 | Define all records (CiTemplateRequest, CiWorkflow, CiJob, CiOptions) | Records compile |
| 4 | Create ICiTemplateGenerator, ICiPlatformProvider, ICiPlatformRegistry | Interface contracts clear |
| 5 | Implement YamlValidator | YAML syntax validation works |
| 6 | Implement GitHubActionsProvider | GitHub Actions YAML renders |
| 7 | Add .NET stack template | dotnet build/test steps |
| 8 | Add Node.js stack template | npm ci/test steps |
| 9 | Implement CiTemplateGenerator | Full generation works |
| 10 | Implement CiPlatformRegistry | Provider lookup works |
| 11 | Add pinned versions | Action versions pinned (v4) |
| 12 | Add minimal permissions | contents: read default |
| 13 | Add dry-run support | Preview without write |
| 14 | Register in DI | Generator resolved |

### Rollout Plan

1. **Phase 1**: Implement core interfaces and registry
2. **Phase 2**: Build GitHubActionsProvider with basic rendering
3. **Phase 3**: Add .NET and Node.js stack templates
4. **Phase 4**: Implement YAML validation
5. **Phase 5**: CLI integration and dry-run support

**End of Task 034 Specification**