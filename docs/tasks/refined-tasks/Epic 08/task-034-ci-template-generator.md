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

### Interface

```csharp
public interface ICiTemplateGenerator
{
    Task<CiWorkflow> GenerateAsync(
        CiTemplateRequest request,
        CancellationToken ct = default);
    
    IReadOnlyList<string> SupportedPlatforms { get; }
    IReadOnlyList<string> SupportedStacks { get; }
    
    Task<ValidationResult> ValidateAsync(
        CiWorkflow workflow,
        CancellationToken ct = default);
}

public record CiTemplateRequest(
    string Platform,
    string Stack,
    string ProjectPath,
    CiOptions Options = null);

public record CiOptions(
    string Name = null,
    IReadOnlyList<string> Branches = null,
    IReadOnlyList<string> PathFilters = null,
    bool IncludeMatrix = false,
    string Runner = null,
    IReadOnlyDictionary<string, string> Variables = null);

public record CiWorkflow(
    string Name,
    string Filename,
    string Content,
    IReadOnlyList<string> Triggers,
    IReadOnlyList<CiJob> Jobs);

public record CiJob(
    string Id,
    string Name,
    string Runner,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> Dependencies);
```

### Registry

```csharp
public interface ICiPlatformRegistry
{
    void Register(ICiPlatformProvider provider);
    ICiPlatformProvider Get(string platform);
}

public interface ICiPlatformProvider
{
    string Platform { get; }
    IReadOnlyList<string> SupportedStacks { get; }
    Task<string> RenderAsync(CiWorkflow workflow);
}
```

---

**End of Task 034 Specification**