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

- Task 034.a: GitHub Actions templates
- Task 034.b: Security configurations
- Task 034.c: Caching setup
- Epic 05: Git commit of workflows

### Mode Compliance

| Mode | Generation | Commit |
|------|------------|--------|
| local-only | ALLOWED | BLOCKED |
| airgapped | ALLOWED | BLOCKED |
| burst | ALLOWED | ALLOWED |

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

- FR-001: `ICiTemplateGenerator` MUST exist
- FR-002: `GenerateAsync` MUST create workflow
- FR-003: Input: template request
- FR-004: Output: complete workflow
- FR-005: Generator MUST be extensible
- FR-006: Platform-specific generators
- FR-007: Stack-specific templates
- FR-008: Supported platforms MUST list
- FR-009: Supported stacks MUST list
- FR-010: Invalid platform MUST error
- FR-011: Invalid stack MUST error
- FR-012: Template MUST be customizable
- FR-013: Options override defaults
- FR-014: Generated YAML MUST be valid
- FR-015: Syntax validation MUST run
- FR-016: Schema validation MUST run
- FR-017: Action references MUST validate
- FR-018: Secrets MUST not be inline
- FR-019: Generator MUST log
- FR-020: Generator MUST emit metrics

### FR-021 to FR-040: Workflow Structure

- FR-021: Workflow name MUST be set
- FR-022: Name from project or config
- FR-023: Triggers MUST be defined
- FR-024: Default: push and pull_request
- FR-025: Branch filters MUST work
- FR-026: Default: main, develop
- FR-027: Path filters MUST work
- FR-028: Jobs MUST be defined
- FR-029: Build job MUST exist
- FR-030: Test job MUST exist
- FR-031: Job dependencies MUST work
- FR-032: `needs` keyword used
- FR-033: Runner MUST be specified
- FR-034: Default: ubuntu-latest
- FR-035: Matrix MUST be optional
- FR-036: Matrix OS MUST work
- FR-037: Matrix version MUST work
- FR-038: Concurrency MUST be set
- FR-039: Prevent duplicate runs
- FR-040: Cancel in-progress MUST work

### FR-041 to FR-055: Output Handling

- FR-041: Output path MUST be configurable
- FR-042: Default: .github/workflows/
- FR-043: Filename MUST be generated
- FR-044: Filename from workflow name
- FR-045: Existing file MUST warn
- FR-046: Overwrite MUST be optional
- FR-047: Dry-run MUST be available
- FR-048: Dry-run shows output only
- FR-049: Template variables MUST resolve
- FR-050: Missing variables MUST error
- FR-051: Comments MUST be added
- FR-052: Comments explain sections
- FR-053: Output MUST be formatted
- FR-054: Consistent YAML style
- FR-055: Readable structure

---

## Non-Functional Requirements

- NFR-001: Generation <2 seconds
- NFR-002: Valid YAML always
- NFR-003: Security best practices
- NFR-004: Minimal permissions
- NFR-005: Pinned versions
- NFR-006: Human-readable output
- NFR-007: Extensible architecture
- NFR-008: Structured logging
- NFR-009: Metrics on generations
- NFR-010: Clear error messages

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

- [ ] AC-001: Generator interface works
- [ ] AC-002: GitHub Actions supported
- [ ] AC-003: .NET stack works
- [ ] AC-004: Node.js stack works
- [ ] AC-005: YAML is valid
- [ ] AC-006: Triggers configured
- [ ] AC-007: Jobs defined
- [ ] AC-008: Matrix works
- [ ] AC-009: Output path works
- [ ] AC-010: Dry-run works

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Template rendering
- [ ] UT-002: YAML validation
- [ ] UT-003: Option handling
- [ ] UT-004: Variable resolution

### Integration Tests

- [ ] IT-001: Full generation
- [ ] IT-002: File output
- [ ] IT-003: Git commit
- [ ] IT-004: Multiple stacks

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