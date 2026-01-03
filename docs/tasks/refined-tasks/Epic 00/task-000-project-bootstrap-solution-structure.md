# Task 000: Project Bootstrap & Solution Structure (Agentic Coding Bot)

**Priority:** 1 / 49  
**Tier:** Foundation  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 0 — Foundation  
**Dependencies:** None (This is the first task)  

---

## Description

### Overview

Task 000 establishes the foundational repository structure and .NET solution architecture for the Agentic Coding Bot (Acode). This task creates the skeleton upon which all subsequent features will be built. The output of this task is a fully buildable, testable, and properly organized codebase that enforces Clean Architecture principles from day one.

### Business Value

A well-structured project foundation provides exponential value over the lifetime of the project. By investing in proper architecture from the start, we:

1. **Reduce technical debt accumulation** — Clear layer boundaries prevent "quick fixes" that create coupling
2. **Accelerate onboarding** — New contributors can navigate the codebase predictably
3. **Enable parallel development** — Teams can work on different layers without conflicts
4. **Facilitate testing** — Clean boundaries enable proper unit testing with mocked dependencies
5. **Support future scaling** — The architecture can accommodate growth without major restructuring

The Agentic Coding Bot is a complex system that will evolve significantly. Starting with Clean Architecture ensures that complexity remains manageable as features are added across Epics 1-12.

### Scope Boundaries

**In Scope:**
- Git repository initialization with appropriate ignore files
- .NET 8.0+ solution file creation
- Project structure following Clean Architecture (Domain, Application, Infrastructure, CLI, Tests)
- Project references configured correctly
- Central package management setup
- Build configuration (Directory.Build.props, Directory.Packages.props)
- Basic project files with namespace declarations
- Placeholder classes to verify compilation

**Out of Scope:**
- Documentation files (handled by Task 000.b)
- Tooling and formatting (handled by Task 000.c)
- Any business logic implementation
- CI/CD pipeline configuration (handled by Epic 8)
- Docker containerization (handled by Epic 4)

### Integration Points

| Task | Integration Type | Description |
|------|------------------|-------------|
| Task 000.a | Parent-Child | Subtask that implements the core solution creation |
| Task 000.b | Sibling | Adds documentation to structure created here |
| Task 000.c | Sibling | Adds tooling to structure created here |
| Task 001 | Downstream | Will add operating mode interfaces to Domain layer |
| Task 002 | Downstream | Will add config parsing to Infrastructure layer |
| Task 003 | Downstream | Will add safety interfaces to Domain layer |
| Epic 1+ | Downstream | All epics build upon this structure |

### Assumptions

1. .NET 8.0 SDK or later is installed on the development machine
2. Git is installed and configured
3. The developer has write access to the target directory
4. No conflicting solution or project files exist in the target location
5. Internet access is available for NuGet package restoration (or packages are cached locally)

### Failure Modes

| Failure | Detection | Recovery |
|---------|-----------|----------|
| .NET SDK not installed | `dotnet --version` fails | Install .NET 8.0 SDK |
| Insufficient permissions | Directory creation fails | Run with elevated permissions or choose different directory |
| Conflicting files exist | Solution creation fails | Clean directory or choose new location |
| NuGet restore fails | Build fails with package errors | Check network/cache; use offline mode |
| Project reference errors | Build fails | Verify project paths; regenerate solution |

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **Clean Architecture** | Software architecture pattern separating concerns into Domain, Application, Infrastructure, and Presentation layers |
| **Domain Layer** | Innermost layer containing business entities, value objects, and domain services; has no external dependencies |
| **Application Layer** | Contains use cases, DTOs, and orchestration logic; depends only on Domain |
| **Infrastructure Layer** | Contains implementations of interfaces defined in Domain/Application; handles external concerns |
| **CLI Layer** | Command-line interface presentation layer; entry point for the application |
| **Central Package Management** | .NET feature using Directory.Packages.props to manage NuGet versions centrally |
| **Directory.Build.props** | MSBuild file that applies settings to all projects in a directory tree |
| **Solution File (.sln)** | Visual Studio solution file that groups related projects |
| **Project File (.csproj)** | MSBuild project file defining a .NET project's configuration |
| **TreatWarningsAsErrors** | Compiler option that fails builds when warnings are present |
| **Nullable Reference Types** | C# feature that enables static analysis for null safety |
| **Implicit Usings** | .NET feature that automatically includes common using statements |
| **xUnit** | Open-source testing framework for .NET |
| **FluentAssertions** | Library providing fluent assertion syntax for tests |
| **NSubstitute** | Mocking library for creating test doubles |

---

## Out of Scope

- Writing any documentation files (README.md, CONTRIBUTING.md, etc.) — Task 000.b
- Configuring code formatting tools (.editorconfig, dotnet format) — Task 000.c
- Setting up test frameworks beyond project references — Task 000.c
- Creating CI/CD pipelines — Epic 8
- Implementing any business logic or interfaces
- Creating Docker configurations — Epic 4
- Setting up code coverage tools — Task 000.c
- Creating any configuration files for the agent itself
- Implementing logging infrastructure
- Setting up dependency injection containers
- Creating any executable functionality

---

## Functional Requirements

### Repository Initialization (FR-000-01 through FR-000-10)

| ID | Requirement |
|----|-------------|
| FR-000-01 | System MUST initialize a new Git repository in the target directory |
| FR-000-02 | System MUST create a `.gitignore` file with patterns for .NET, Node.js, and Python artifacts |
| FR-000-03 | System MUST create a `.gitattributes` file enforcing LF line endings for source files |
| FR-000-04 | System MUST create an initial commit with the message "Initial commit: project structure" |
| FR-000-05 | System MUST NOT include any sensitive files in the initial commit |
| FR-000-06 | `.gitignore` MUST include patterns: `bin/`, `obj/`, `*.user`, `*.suo`, `.vs/`, `node_modules/`, `__pycache__/`, `.env` |
| FR-000-07 | `.gitattributes` MUST specify `* text=auto` for automatic line ending handling |
| FR-000-08 | `.gitattributes` MUST specify `*.cs text eol=lf` for C# source files |
| FR-000-09 | `.gitattributes` MUST specify `*.sln text eol=crlf` for solution files (Windows compatibility) |
| FR-000-10 | Repository MUST have a clean working tree after initialization (no uncommitted changes) |

### Solution Structure (FR-000-11 through FR-000-25)

| ID | Requirement |
|----|-------------|
| FR-000-11 | System MUST create `Acode.sln` solution file at repository root |
| FR-000-12 | Solution MUST contain project `src/Acode.Domain/Acode.Domain.csproj` |
| FR-000-13 | Solution MUST contain project `src/Acode.Application/Acode.Application.csproj` |
| FR-000-14 | Solution MUST contain project `src/Acode.Infrastructure/Acode.Infrastructure.csproj` |
| FR-000-15 | Solution MUST contain project `src/Acode.Cli/Acode.Cli.csproj` |
| FR-000-16 | Solution MUST contain project `tests/Acode.Domain.Tests/Acode.Domain.Tests.csproj` |
| FR-000-17 | Solution MUST contain project `tests/Acode.Application.Tests/Acode.Application.Tests.csproj` |
| FR-000-18 | Solution MUST contain project `tests/Acode.Infrastructure.Tests/Acode.Infrastructure.Tests.csproj` |
| FR-000-19 | Solution MUST contain project `tests/Acode.Cli.Tests/Acode.Cli.Tests.csproj` |
| FR-000-20 | Solution MUST contain project `tests/Acode.Integration.Tests/Acode.Integration.Tests.csproj` |
| FR-000-21 | Solution MUST organize projects into solution folders: `src`, `tests` |
| FR-000-22 | All projects MUST target `net8.0` or later |
| FR-000-23 | `dotnet build Acode.sln` MUST succeed with zero errors |
| FR-000-24 | `dotnet test Acode.sln` MUST execute without failures (placeholder tests) |
| FR-000-25 | Solution MUST load correctly in Visual Studio 2022 and VS Code with C# extension |

### Project Dependencies (FR-000-26 through FR-000-40)

| ID | Requirement |
|----|-------------|
| FR-000-26 | `Acode.Domain` MUST NOT reference any other Acode projects |
| FR-000-27 | `Acode.Domain` MUST NOT reference any NuGet packages except pure abstractions |
| FR-000-28 | `Acode.Application` MUST reference only `Acode.Domain` |
| FR-000-29 | `Acode.Application` MAY reference abstraction packages (e.g., Microsoft.Extensions.Logging.Abstractions) |
| FR-000-30 | `Acode.Infrastructure` MUST reference `Acode.Domain` |
| FR-000-31 | `Acode.Infrastructure` MUST reference `Acode.Application` |
| FR-000-32 | `Acode.Infrastructure` MAY reference implementation packages |
| FR-000-33 | `Acode.Cli` MUST reference `Acode.Domain` |
| FR-000-34 | `Acode.Cli` MUST reference `Acode.Application` |
| FR-000-35 | `Acode.Cli` MUST reference `Acode.Infrastructure` |
| FR-000-36 | `Acode.Cli` MUST be an executable project (OutputType=Exe) |
| FR-000-37 | Test projects MUST reference their corresponding production projects |
| FR-000-38 | Test projects MUST reference xUnit, FluentAssertions, and NSubstitute packages |
| FR-000-39 | `Acode.Integration.Tests` MUST reference all production projects |
| FR-000-40 | Circular dependencies MUST NOT exist between any projects |

### Build Configuration (FR-000-41 through FR-000-55)

| ID | Requirement |
|----|-------------|
| FR-000-41 | `Directory.Build.props` MUST exist at repository root |
| FR-000-42 | `Directory.Build.props` MUST enable `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` |
| FR-000-43 | `Directory.Build.props` MUST enable `<Nullable>enable</Nullable>` |
| FR-000-44 | `Directory.Build.props` MUST enable `<ImplicitUsings>enable</ImplicitUsings>` |
| FR-000-45 | `Directory.Build.props` MUST set `<LangVersion>latest</LangVersion>` |
| FR-000-46 | `Directory.Build.props` MUST set consistent `<Company>`, `<Authors>`, `<Copyright>` |
| FR-000-47 | `Directory.Packages.props` MUST exist at repository root |
| FR-000-48 | `Directory.Packages.props` MUST enable `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` |
| FR-000-49 | `Directory.Packages.props` MUST define versions for all referenced packages |
| FR-000-50 | Individual project files MUST NOT specify package versions (use `VersionOverride` only if necessary) |
| FR-000-51 | Build MUST produce artifacts in `bin/{Configuration}/{TargetFramework}/` |
| FR-000-52 | Build MUST produce XML documentation files for all projects |
| FR-000-53 | Release build MUST enable optimizations |
| FR-000-54 | Debug build MUST include full debug symbols |
| FR-000-55 | Build time for full solution MUST be under 30 seconds on reference hardware |

### Namespace Conventions (FR-000-56 through FR-000-65)

| ID | Requirement |
|----|-------------|
| FR-000-56 | Root namespace for Domain MUST be `Acode.Domain` |
| FR-000-57 | Root namespace for Application MUST be `Acode.Application` |
| FR-000-58 | Root namespace for Infrastructure MUST be `Acode.Infrastructure` |
| FR-000-59 | Root namespace for CLI MUST be `Acode.Cli` |
| FR-000-60 | Namespace MUST match folder structure (e.g., `Acode.Domain.Entities`) |
| FR-000-61 | Each project MUST have a placeholder class to verify compilation |
| FR-000-62 | Placeholder classes MUST be marked with `[Obsolete("Placeholder for initial structure")]` |
| FR-000-63 | File names MUST match class names exactly (PascalCase) |
| FR-000-64 | One class per file MUST be enforced (except nested classes) |
| FR-000-65 | Global usings MUST be defined in `GlobalUsings.cs` per project if needed |

---

## Non-Functional Requirements

### Performance (NFR-000-01 through NFR-000-08)

| ID | Requirement |
|----|-------------|
| NFR-000-01 | Full solution build time MUST be under 30 seconds on machine with 4 cores, 16GB RAM, SSD |
| NFR-000-02 | Incremental build time MUST be under 5 seconds for single-file changes |
| NFR-000-03 | Solution load time in VS 2022 MUST be under 10 seconds |
| NFR-000-04 | IntelliSense response time MUST be under 500ms |
| NFR-000-05 | NuGet restore time MUST be under 30 seconds (warm cache) |
| NFR-000-06 | Test discovery time MUST be under 5 seconds |
| NFR-000-07 | Solution file size MUST be under 50KB |
| NFR-000-08 | Total project file sizes MUST be under 100KB combined |

### Reliability (NFR-000-09 through NFR-000-15)

| ID | Requirement |
|----|-------------|
| NFR-000-09 | Build MUST be reproducible given same source and package versions |
| NFR-000-10 | Build MUST succeed on Windows, macOS, and Linux |
| NFR-000-11 | Build MUST succeed with .NET 8.0, 8.1, and 9.0 SDKs |
| NFR-000-12 | Build MUST NOT require Visual Studio (CLI build must work) |
| NFR-000-13 | Solution MUST handle concurrent builds without corruption |
| NFR-000-14 | Package restore MUST work in offline mode with populated cache |
| NFR-000-15 | Build MUST fail fast on first error (stop on first failure) |

### Maintainability (NFR-000-16 through NFR-000-25)

| ID | Requirement |
|----|-------------|
| NFR-000-16 | Project structure MUST follow established .NET conventions |
| NFR-000-17 | All configuration MUST be centralized in Directory.Build.props |
| NFR-000-18 | Package versions MUST be centralized in Directory.Packages.props |
| NFR-000-19 | No duplicate configuration across project files |
| NFR-000-20 | Project files MUST use SDK-style format |
| NFR-000-21 | Solution folders MUST logically group related projects |
| NFR-000-22 | Naming conventions MUST be consistent across all files |
| NFR-000-23 | Layer dependencies MUST be enforceable via build (no illegal references) |
| NFR-000-24 | Adding new projects MUST require minimal configuration |
| NFR-000-25 | Removing projects MUST not leave orphaned references |

### Security (NFR-000-26 through NFR-000-32)

| ID | Requirement |
|----|-------------|
| NFR-000-26 | `.gitignore` MUST exclude all sensitive file patterns |
| NFR-000-27 | No credentials MUST be committed to repository |
| NFR-000-28 | Package sources MUST be limited to nuget.org (or configured private feeds) |
| NFR-000-29 | Package signature verification MUST be enabled |
| NFR-000-30 | Vulnerable packages MUST be detectable via `dotnet list package --vulnerable` |
| NFR-000-31 | Build output MUST NOT include source paths in release mode |
| NFR-000-32 | Debug symbols MUST NOT be included in release packages |

### Compatibility (NFR-000-33 through NFR-000-40)

| ID | Requirement |
|----|-------------|
| NFR-000-33 | Solution MUST work with Visual Studio 2022 (17.0+) |
| NFR-000-34 | Solution MUST work with VS Code + C# extension |
| NFR-000-35 | Solution MUST work with JetBrains Rider 2023.2+ |
| NFR-000-36 | Solution MUST work with .NET CLI on all platforms |
| NFR-000-37 | Solution MUST work with GitHub Codespaces |
| NFR-000-38 | Solution MUST work with GitPod |
| NFR-000-39 | Solution MUST work in devcontainers |
| NFR-000-40 | Solution MUST be compatible with central package management |

---

## User Manual Documentation

### Quick Start

```bash
# Clone the repository
git clone https://github.com/your-org/acode.git
cd acode

# Restore packages
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the CLI (after implementation)
dotnet run --project src/Acode.Cli
```

### Solution Structure

```
acode/
├── Acode.sln                          # Solution file
├── Directory.Build.props              # Shared MSBuild properties
├── Directory.Packages.props           # Central package versions
├── .gitignore                         # Git ignore patterns
├── .gitattributes                     # Git attributes
├── src/
│   ├── Acode.Domain/                  # Domain layer
│   │   ├── Acode.Domain.csproj
│   │   ├── Entities/                  # Domain entities
│   │   ├── ValueObjects/              # Value objects
│   │   ├── Services/                  # Domain services
│   │   └── Interfaces/                # Repository/service interfaces
│   ├── Acode.Application/             # Application layer
│   │   ├── Acode.Application.csproj
│   │   ├── UseCases/                  # Use case implementations
│   │   ├── DTOs/                      # Data transfer objects
│   │   └── Interfaces/                # Application service interfaces
│   ├── Acode.Infrastructure/          # Infrastructure layer
│   │   ├── Acode.Infrastructure.csproj
│   │   ├── Persistence/               # Database/file storage
│   │   ├── External/                  # External service adapters
│   │   └── Services/                  # Infrastructure services
│   └── Acode.Cli/                     # CLI layer
│       ├── Acode.Cli.csproj
│       ├── Program.cs                 # Entry point
│       └── Commands/                  # CLI commands
└── tests/
    ├── Acode.Domain.Tests/            # Domain unit tests
    ├── Acode.Application.Tests/       # Application unit tests
    ├── Acode.Infrastructure.Tests/    # Infrastructure unit tests
    ├── Acode.Cli.Tests/               # CLI unit tests
    └── Acode.Integration.Tests/       # Integration tests
```

### Project Dependencies Diagram

```
                    ┌─────────────────┐
                    │   Acode.Cli     │
                    │   (Executable)  │
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
              ▼              ▼              ▼
    ┌─────────────────┐ ┌─────────────────┐
    │ Acode.          │ │ Acode.          │
    │ Infrastructure  │ │ Application     │
    └────────┬────────┘ └────────┬────────┘
             │                   │
             │         ┌─────────┘
             │         │
             ▼         ▼
        ┌─────────────────┐
        │  Acode.Domain   │
        │  (No deps)      │
        └─────────────────┘
```

### Configuration Files

#### Directory.Build.props

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <Company>Acode Project</Company>
    <Authors>Acode Contributors</Authors>
    <Copyright>Copyright © 2025 Acode Project</Copyright>
  </PropertyGroup>
</Project>
```

#### Directory.Packages.props

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- Test packages -->
    <PackageVersion Include="xunit" Version="2.6.6" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.5.6" />
    <PackageVersion Include="FluentAssertions" Version="6.12.0" />
    <PackageVersion Include="NSubstitute" Version="5.1.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageVersion Include="coverlet.collector" Version="6.0.0" />
    
    <!-- Application packages -->
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### Build Commands

| Command | Description |
|---------|-------------|
| `dotnet restore` | Restore all NuGet packages |
| `dotnet build` | Build the entire solution |
| `dotnet build -c Release` | Build in Release configuration |
| `dotnet test` | Run all tests |
| `dotnet test --no-build` | Run tests without rebuilding |
| `dotnet test --filter "Category=Unit"` | Run only unit tests |
| `dotnet clean` | Clean build artifacts |
| `dotnet publish -c Release` | Publish for deployment |

### Best Practices

1. **Always build from the solution root** — Running `dotnet build` from the repository root ensures all projects build correctly with proper dependencies.

2. **Use central package management** — Never specify package versions in individual .csproj files. Add versions to Directory.Packages.props.

3. **Respect layer boundaries** — Domain MUST NOT reference Application/Infrastructure. Application MUST NOT reference Infrastructure. Use interfaces and dependency injection.

4. **Keep Domain pure** — No external NuGet packages in Domain except pure abstractions (no implementations).

5. **One class per file** — Each public class gets its own file with a matching name. Exception: nested classes and test fixtures.

6. **Match namespace to folder** — The namespace should exactly match the folder path from the project root.

7. **Run tests before committing** — Always run `dotnet test` before pushing changes to catch regressions early.

### Troubleshooting

#### Build fails with "project not found"

**Cause:** Solution file references a project that was moved or renamed.

**Solution:**
```bash
# Regenerate solution
dotnet sln Acode.sln remove src/OldProject/OldProject.csproj
dotnet sln Acode.sln add src/NewProject/NewProject.csproj
```

#### Package version conflicts

**Cause:** A package is referenced with different versions across projects.

**Solution:**
```bash
# Check for conflicts
dotnet list package --include-transitive

# Ensure all versions are in Directory.Packages.props
# Individual projects should only use <PackageReference Include="Package" />
```

#### Build warnings treated as errors

**Cause:** TreatWarningsAsErrors is enabled by design.

**Solution:**
Fix the warning. Common warnings:
- CS8618: Non-nullable property not initialized — Add `= null!;` or make nullable
- CS1591: Missing XML comment — Add `/// <summary>` documentation

#### Tests not discovered

**Cause:** Test project missing required packages.

**Solution:**
Ensure test project has:
```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio" />
```

### FAQ

**Q: Why Clean Architecture?**
A: Clean Architecture enforces separation of concerns, makes testing easier, and allows infrastructure to be swapped without affecting business logic. This is critical for Acode where we need to support multiple model providers, storage backends, and execution environments.

**Q: Why .NET 8.0?**
A: .NET 8.0 is the current LTS release with excellent performance, native AOT support, and long-term Microsoft support. It provides the best balance of features and stability.

**Q: Can I add a new project?**
A: Yes. Use `dotnet new classlib -n Acode.NewProject -o src/Acode.NewProject`, then add to solution with `dotnet sln add src/Acode.NewProject/Acode.NewProject.csproj`. Remember to add it to the appropriate solution folder.

**Q: Why are warnings treated as errors?**
A: To maintain code quality from the start. Warnings often indicate real issues that become bugs later. Fixing them immediately is cheaper than fixing them after they cause problems.

**Q: Can I use a different testing framework?**
A: xUnit is the standard for this project. Changing it would require updating Task 000.c and all existing tests. Submit an ADR if you have a compelling reason.

---

## Acceptance Criteria / Definition of Done

### Repository Initialization (20 items)
- [ ] Git repository initialized in target directory
- [ ] `.gitignore` file exists at repository root
- [ ] `.gitignore` includes `bin/` pattern
- [ ] `.gitignore` includes `obj/` pattern
- [ ] `.gitignore` includes `*.user` pattern
- [ ] `.gitignore` includes `*.suo` pattern
- [ ] `.gitignore` includes `.vs/` pattern
- [ ] `.gitignore` includes `node_modules/` pattern
- [ ] `.gitignore` includes `__pycache__/` pattern
- [ ] `.gitignore` includes `.env` pattern
- [ ] `.gitignore` includes `*.log` pattern
- [ ] `.gitattributes` file exists at repository root
- [ ] `.gitattributes` includes `* text=auto`
- [ ] `.gitattributes` includes `*.cs text eol=lf`
- [ ] `.gitattributes` includes `*.sln text eol=crlf`
- [ ] Initial commit exists with appropriate message
- [ ] Initial commit does not include sensitive files
- [ ] Repository has clean working tree after init
- [ ] `git status` shows no untracked files
- [ ] `git log` shows at least one commit

### Solution File (15 items)
- [ ] `Acode.sln` exists at repository root
- [ ] Solution loads in Visual Studio 2022
- [ ] Solution loads in VS Code with C# extension
- [ ] Solution contains src/ solution folder
- [ ] Solution contains tests/ solution folder
- [ ] `Acode.Domain` project in src/ folder
- [ ] `Acode.Application` project in src/ folder
- [ ] `Acode.Infrastructure` project in src/ folder
- [ ] `Acode.Cli` project in src/ folder
- [ ] `Acode.Domain.Tests` project in tests/ folder
- [ ] `Acode.Application.Tests` project in tests/ folder
- [ ] `Acode.Infrastructure.Tests` project in tests/ folder
- [ ] `Acode.Cli.Tests` project in tests/ folder
- [ ] `Acode.Integration.Tests` project in tests/ folder
- [ ] Solution file is valid XML

### Project Structure (25 items)
- [ ] `src/Acode.Domain/Acode.Domain.csproj` exists
- [ ] `src/Acode.Application/Acode.Application.csproj` exists
- [ ] `src/Acode.Infrastructure/Acode.Infrastructure.csproj` exists
- [ ] `src/Acode.Cli/Acode.Cli.csproj` exists
- [ ] `tests/Acode.Domain.Tests/Acode.Domain.Tests.csproj` exists
- [ ] `tests/Acode.Application.Tests/Acode.Application.Tests.csproj` exists
- [ ] `tests/Acode.Infrastructure.Tests/Acode.Infrastructure.Tests.csproj` exists
- [ ] `tests/Acode.Cli.Tests/Acode.Cli.Tests.csproj` exists
- [ ] `tests/Acode.Integration.Tests/Acode.Integration.Tests.csproj` exists
- [ ] Each project has at least one .cs file
- [ ] Domain project has Entities/ folder
- [ ] Domain project has Interfaces/ folder
- [ ] Application project has UseCases/ folder
- [ ] Application project has DTOs/ folder
- [ ] Infrastructure project has Services/ folder
- [ ] CLI project has Commands/ folder
- [ ] CLI project has Program.cs
- [ ] All folders contain placeholder files or .gitkeep
- [ ] Namespace matches folder structure in all files
- [ ] All .cs files compile without errors
- [ ] All .cs files have proper using statements
- [ ] PascalCase naming used for all public types
- [ ] File names match class names
- [ ] One public class per file
- [ ] All placeholder classes marked [Obsolete]

### Project Configuration (25 items)
- [ ] All projects target net8.0
- [ ] `Directory.Build.props` exists at root
- [ ] `Directory.Build.props` sets TreatWarningsAsErrors=true
- [ ] `Directory.Build.props` sets Nullable=enable
- [ ] `Directory.Build.props` sets ImplicitUsings=enable
- [ ] `Directory.Build.props` sets LangVersion=latest
- [ ] `Directory.Build.props` sets GenerateDocumentationFile=true
- [ ] `Directory.Build.props` sets Company property
- [ ] `Directory.Build.props` sets Authors property
- [ ] `Directory.Packages.props` exists at root
- [ ] `Directory.Packages.props` enables central package management
- [ ] xunit version defined in Directory.Packages.props
- [ ] FluentAssertions version defined in Directory.Packages.props
- [ ] NSubstitute version defined in Directory.Packages.props
- [ ] Microsoft.NET.Test.Sdk version defined in Directory.Packages.props
- [ ] No package versions in individual .csproj files
- [ ] Acode.Cli has OutputType=Exe
- [ ] All other projects are class libraries
- [ ] Domain project has no ProjectReferences
- [ ] Application project references only Domain
- [ ] Infrastructure references Domain and Application
- [ ] CLI references all production projects
- [ ] Test projects reference corresponding production projects
- [ ] No circular dependencies exist
- [ ] All projects use SDK-style format

### Build Verification (20 items)
- [ ] `dotnet restore` succeeds
- [ ] `dotnet restore` completes in under 60 seconds (warm cache)
- [ ] `dotnet build` succeeds with zero errors
- [ ] `dotnet build` completes with zero warnings
- [ ] `dotnet build` completes in under 30 seconds
- [ ] `dotnet build -c Release` succeeds
- [ ] `dotnet test` executes without errors
- [ ] `dotnet test` finds all test projects
- [ ] `dotnet test` completes in under 60 seconds
- [ ] `dotnet clean` removes build artifacts
- [ ] Incremental build works correctly
- [ ] Build produces XML documentation files
- [ ] Build produces .dll files in bin/ directories
- [ ] Debug build includes .pdb files
- [ ] Release build optimizes output
- [ ] Cross-platform build works (if tested)
- [ ] Build works without Visual Studio
- [ ] Build works with dotnet CLI only
- [ ] No MSB warnings during build
- [ ] Package restore uses only allowed sources

### Layer Enforcement (15 items)
- [ ] Domain has zero external dependencies
- [ ] Domain does not reference Application
- [ ] Domain does not reference Infrastructure
- [ ] Domain does not reference CLI
- [ ] Application does not reference Infrastructure
- [ ] Application does not reference CLI
- [ ] Infrastructure does not reference CLI
- [ ] Test projects only reference projects they test
- [ ] Integration.Tests can reference all production projects
- [ ] Adding illegal reference causes build error
- [ ] Layer violations detectable at compile time
- [ ] No runtime layer violations possible
- [ ] Dependency injection configured correctly
- [ ] Interfaces defined in appropriate layers
- [ ] Implementations in Infrastructure layer

### Test Infrastructure (15 items)
- [ ] xUnit packages installed in test projects
- [ ] FluentAssertions packages installed in test projects
- [ ] NSubstitute packages installed in test projects
- [ ] Microsoft.NET.Test.Sdk installed in test projects
- [ ] coverlet.collector installed in test projects
- [ ] At least one placeholder test per test project
- [ ] Placeholder tests pass
- [ ] Test discovery works
- [ ] Test execution works
- [ ] Test output is readable
- [ ] Test categories can be filtered
- [ ] Tests run in parallel by default
- [ ] Test isolation verified
- [ ] Mocking framework functional
- [ ] Assertion library functional

### Documentation (15 items)
- [ ] All public classes have XML documentation
- [ ] All public methods have XML documentation
- [ ] XML documentation builds without warnings
- [ ] Documentation file generated for each project
- [ ] Placeholder documentation indicates obsolete status
- [ ] Namespace documentation exists
- [ ] Assembly documentation exists
- [ ] No TODO comments in production code
- [ ] Code comments explain "why" not "what"
- [ ] Complex algorithms documented
- [ ] Architecture decisions documented in code where applicable
- [ ] Interface contracts documented
- [ ] Exception conditions documented
- [ ] Thread safety documented where applicable
- [ ] Nullability documented via annotations

### Performance Baseline (10 items)
- [ ] Full build under 30 seconds measured
- [ ] Incremental build under 5 seconds measured
- [ ] Test execution under 60 seconds measured
- [ ] Solution load time under 10 seconds measured
- [ ] Memory usage during build under 4GB
- [ ] Disk usage for solution under 500MB
- [ ] Package restore under 30 seconds (warm cache)
- [ ] Clean + build under 60 seconds
- [ ] Parallel build works correctly
- [ ] Resource usage acceptable

### Compatibility (20 items)
- [ ] Works with .NET 8.0 SDK
- [ ] Works with Visual Studio 2022
- [ ] Works with VS Code + C# extension
- [ ] Works with JetBrains Rider
- [ ] Works on Windows
- [ ] Works on macOS (if tested)
- [ ] Works on Linux (if tested)
- [ ] Works with central package management
- [ ] Works with NuGet.org packages
- [ ] Works with private feeds (if configured)
- [ ] Works in GitHub Codespaces (if tested)
- [ ] Works in devcontainers (if tested)
- [ ] Works with Git
- [ ] Works with common Git GUIs
- [ ] Works with CI systems (structure ready)
- [ ] Path lengths acceptable on Windows
- [ ] Case-sensitivity handled correctly
- [ ] Line endings handled correctly
- [ ] Encoding handled correctly (UTF-8)
- [ ] Special characters in paths avoided

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| UT-000-01 | Domain project compiles independently | Build succeeds with zero errors |
| UT-000-02 | Application project compiles with Domain reference | Build succeeds with zero errors |
| UT-000-03 | Infrastructure project compiles with all references | Build succeeds with zero errors |
| UT-000-04 | CLI project compiles as executable | Build succeeds, produces .exe/.dll |
| UT-000-05 | Placeholder class exists in Domain | Class found, marked [Obsolete] |
| UT-000-06 | Placeholder class exists in Application | Class found, marked [Obsolete] |
| UT-000-07 | Placeholder class exists in Infrastructure | Class found, marked [Obsolete] |
| UT-000-08 | Placeholder class exists in CLI | Class/Program found |
| UT-000-09 | Namespace matches folder structure | All namespaces correct |
| UT-000-10 | TreatWarningsAsErrors is enabled | Warning causes build failure |
| UT-000-11 | Nullable reference types enabled | Null warning generated for nullable |
| UT-000-12 | Central package management works | Package versions from Directory.Packages.props |
| UT-000-13 | XML documentation generated | .xml files in output |
| UT-000-14 | Test framework configured | Tests discovered and runnable |
| UT-000-15 | Mocking framework works | NSubstitute creates mocks |
| UT-000-16 | Assertion library works | FluentAssertions assertions pass |
| UT-000-17 | Layer dependencies enforced | Illegal reference fails build |
| UT-000-18 | Solution folders organize projects | Projects in correct folders |
| UT-000-19 | Clean removes artifacts | bin/obj cleared |
| UT-000-20 | Incremental build works | Unchanged files not recompiled |

### Integration Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| IT-000-01 | Full solution build from clean state | Build succeeds in under 60 seconds |
| IT-000-02 | Solution loads in Visual Studio | All projects load without errors |
| IT-000-03 | Solution loads in VS Code | C# extension recognizes all projects |
| IT-000-04 | Git operations work with solution | Clone, commit, push succeed |
| IT-000-05 | NuGet restore from nuget.org | All packages restored |
| IT-000-06 | Cross-project references resolve | No unresolved references |
| IT-000-07 | Test discovery finds all tests | All test projects discovered |
| IT-000-08 | Test execution runs all tests | All tests execute |
| IT-000-09 | Code coverage collection works | Coverage report generated |
| IT-000-10 | Debug build works | Debugging possible |
| IT-000-11 | Release build works | Optimized output produced |
| IT-000-12 | Publish produces deployable output | CLI runs from publish folder |

### End-to-End Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| E2E-000-01 | Fresh clone and build | Repository clones, builds, tests pass |
| E2E-000-02 | Multiple developer workflow | Two clones can build concurrently |
| E2E-000-03 | Add new project to solution | Project integrates correctly |
| E2E-000-04 | Remove project from solution | Solution still builds |
| E2E-000-05 | Upgrade NuGet package | Central management updates all |
| E2E-000-06 | Run CLI executable | Program executes without crash |
| E2E-000-07 | Build on Windows | Full workflow completes |
| E2E-000-08 | Build on Linux (CI) | Full workflow completes |
| E2E-000-09 | Build in Codespaces | Full workflow completes |
| E2E-000-10 | Git history preserved | All commits accessible |

### Performance Benchmarks

| ID | Metric | Target | Measurement Method |
|----|--------|--------|-------------------|
| PB-000-01 | Full build time | < 30 seconds | `time dotnet build` |
| PB-000-02 | Incremental build time | < 5 seconds | Modify one file, `time dotnet build` |
| PB-000-03 | Test execution time | < 60 seconds | `time dotnet test` |
| PB-000-04 | Solution load time | < 10 seconds | VS profiler |
| PB-000-05 | Package restore time | < 30 seconds | `time dotnet restore` (warm cache) |
| PB-000-06 | Memory usage (build) | < 4 GB | Process monitor |
| PB-000-07 | Disk usage (built) | < 500 MB | Folder size |

### Regression / Impacted Areas

| Area | Impact | Verification |
|------|--------|--------------|
| All future tasks | High | Any structural change requires full rebuild |
| CI/CD pipelines | Medium | Pipeline templates depend on solution structure |
| Developer tooling | Medium | IDE configurations depend on solution structure |
| Documentation | Low | Paths may need updates |

---

## User Verification Steps

### Scenario 1: Fresh Repository Clone and Build
1. Clone the repository to a new directory
2. Run `dotnet restore`
3. Run `dotnet build`
4. Run `dotnet test`
**Verify:** All commands succeed with zero errors and zero warnings

### Scenario 2: Visual Studio Solution Load
1. Open `Acode.sln` in Visual Studio 2022
2. Wait for solution to fully load
3. Expand Solution Explorer
**Verify:** All projects visible, no load errors, IntelliSense works

### Scenario 3: VS Code Development
1. Open repository folder in VS Code
2. Install C# extension if prompted
3. Wait for OmniSharp to initialize
4. Open any .cs file
**Verify:** Syntax highlighting, IntelliSense, error detection all work

### Scenario 4: Layer Dependency Enforcement
1. Open `src/Acode.Domain/Acode.Domain.csproj`
2. Add `<ProjectReference Include="..\Acode.Application\Acode.Application.csproj" />`
3. Run `dotnet build`
**Verify:** Build fails due to architecture violation (or succeeds but we document this is wrong - ideally add ArchUnit tests)

### Scenario 5: Add New NuGet Package
1. Add new PackageVersion to `Directory.Packages.props`
2. Add PackageReference (without version) to a project
3. Run `dotnet restore`
**Verify:** Package restored, version from central config used

### Scenario 6: Create New Project
1. Run `dotnet new classlib -n Acode.NewProject -o src/Acode.NewProject`
2. Run `dotnet sln Acode.sln add src/Acode.NewProject/Acode.NewProject.csproj`
3. Run `dotnet build`
**Verify:** New project builds, inherits central configuration

### Scenario 7: Run Tests
1. Run `dotnet test`
2. Check test output
**Verify:** Test discovery finds all test projects, placeholder tests pass

### Scenario 8: Generate Documentation
1. Run `dotnet build`
2. Check `bin/Debug/net8.0/` folders
**Verify:** XML documentation files present for each project

### Scenario 9: Clean Build
1. Run `dotnet clean`
2. Run `dotnet build`
**Verify:** Build succeeds, all artifacts regenerated

### Scenario 10: Cross-Platform Build (if applicable)
1. Run build on Windows
2. Run build on Linux/macOS (or CI)
**Verify:** Both builds succeed with identical logical output

### Scenario 11: Git Operations
1. Make a change to any file
2. Run `git status`
3. Run `git add .`
4. Run `git commit -m "test"`
**Verify:** Git operations work, .gitignore excludes build artifacts

### Scenario 12: CLI Executable
1. Run `dotnet run --project src/Acode.Cli`
**Verify:** Program executes without exceptions (even if it just exits)

---

## Implementation Prompt for Claude

### Overview

You are implementing Task 000: Project Bootstrap & Solution Structure for the Agentic Coding Bot (Acode). This task creates the foundational repository and .NET solution structure.

### Prerequisites Check

Before starting, verify:
```bash
# Check .NET SDK version (must be 8.0+)
dotnet --version

# Check Git version
git --version

# Verify target directory is empty or doesn't exist
ls -la <target-directory>
```

### Step 1: Create Repository Structure

```bash
# Create and enter directory
mkdir acode
cd acode

# Initialize Git repository
git init

# Create .gitignore
cat > .gitignore << 'EOF'
# Build results
[Bb]in/
[Oo]bj/
[Oo]ut/

# Visual Studio
.vs/
*.user
*.suo
*.userosscache
*.sln.docstates

# IDE
.idea/
*.swp
*.swo
*~

# Node
node_modules/
npm-debug.log*

# Python
__pycache__/
*.py[cod]
*$py.class
.Python
venv/
.env

# Logs
*.log
logs/

# OS
.DS_Store
Thumbs.db

# Test results
TestResults/
*.trx
coverage/

# Packages
*.nupkg
EOF

# Create .gitattributes
cat > .gitattributes << 'EOF'
* text=auto
*.cs text eol=lf
*.csproj text eol=lf
*.props text eol=lf
*.targets text eol=lf
*.sln text eol=crlf
*.md text eol=lf
*.json text eol=lf
*.yml text eol=lf
*.yaml text eol=lf
*.sh text eol=lf
*.ps1 text eol=crlf
*.cmd text eol=crlf
*.bat text eol=crlf
EOF
```

### Step 2: Create Directory.Build.props

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn> <!-- Remove this after adding docs -->
  </PropertyGroup>
  
  <PropertyGroup>
    <Company>Acode Project</Company>
    <Authors>Acode Contributors</Authors>
    <Copyright>Copyright © 2025 Acode Project</Copyright>
    <Product>Acode - Agentic Coding Bot</Product>
  </PropertyGroup>
  
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <DebugType>none</DebugType>
    <DebugSymbols>false</DebugSymbols>
  </PropertyGroup>
</Project>
```

### Step 3: Create Directory.Packages.props

```xml
<!-- Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Abstractions (allowed in Domain) -->
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    
    <!-- Testing -->
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageVersion Include="xunit" Version="2.6.6" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.5.6" />
    <PackageVersion Include="FluentAssertions" Version="6.12.0" />
    <PackageVersion Include="NSubstitute" Version="5.1.0" />
    <PackageVersion Include="coverlet.collector" Version="6.0.0" />
  </ItemGroup>
</Project>
```

### Step 4: Create Projects

```bash
# Create solution
dotnet new sln -n Acode

# Create production projects
dotnet new classlib -n Acode.Domain -o src/Acode.Domain
dotnet new classlib -n Acode.Application -o src/Acode.Application
dotnet new classlib -n Acode.Infrastructure -o src/Acode.Infrastructure
dotnet new console -n Acode.Cli -o src/Acode.Cli

# Create test projects
dotnet new xunit -n Acode.Domain.Tests -o tests/Acode.Domain.Tests
dotnet new xunit -n Acode.Application.Tests -o tests/Acode.Application.Tests
dotnet new xunit -n Acode.Infrastructure.Tests -o tests/Acode.Infrastructure.Tests
dotnet new xunit -n Acode.Cli.Tests -o tests/Acode.Cli.Tests
dotnet new xunit -n Acode.Integration.Tests -o tests/Acode.Integration.Tests

# Add to solution
dotnet sln add src/Acode.Domain/Acode.Domain.csproj --solution-folder src
dotnet sln add src/Acode.Application/Acode.Application.csproj --solution-folder src
dotnet sln add src/Acode.Infrastructure/Acode.Infrastructure.csproj --solution-folder src
dotnet sln add src/Acode.Cli/Acode.Cli.csproj --solution-folder src
dotnet sln add tests/Acode.Domain.Tests/Acode.Domain.Tests.csproj --solution-folder tests
dotnet sln add tests/Acode.Application.Tests/Acode.Application.Tests.csproj --solution-folder tests
dotnet sln add tests/Acode.Infrastructure.Tests/Acode.Infrastructure.Tests.csproj --solution-folder tests
dotnet sln add tests/Acode.Cli.Tests/Acode.Cli.Tests.csproj --solution-folder tests
dotnet sln add tests/Acode.Integration.Tests/Acode.Integration.Tests.csproj --solution-folder tests
```

### Step 5: Configure Project References

**src/Acode.Application/Acode.Application.csproj:**
```xml
<ItemGroup>
  <ProjectReference Include="..\Acode.Domain\Acode.Domain.csproj" />
</ItemGroup>
```

**src/Acode.Infrastructure/Acode.Infrastructure.csproj:**
```xml
<ItemGroup>
  <ProjectReference Include="..\Acode.Domain\Acode.Domain.csproj" />
  <ProjectReference Include="..\Acode.Application\Acode.Application.csproj" />
</ItemGroup>
```

**src/Acode.Cli/Acode.Cli.csproj:**
```xml
<ItemGroup>
  <ProjectReference Include="..\Acode.Domain\Acode.Domain.csproj" />
  <ProjectReference Include="..\Acode.Application\Acode.Application.csproj" />
  <ProjectReference Include="..\Acode.Infrastructure\Acode.Infrastructure.csproj" />
</ItemGroup>
```

### Step 6: Create Folder Structure

```bash
# Domain folders
mkdir -p src/Acode.Domain/Entities
mkdir -p src/Acode.Domain/ValueObjects
mkdir -p src/Acode.Domain/Services
mkdir -p src/Acode.Domain/Interfaces

# Application folders
mkdir -p src/Acode.Application/UseCases
mkdir -p src/Acode.Application/DTOs
mkdir -p src/Acode.Application/Interfaces

# Infrastructure folders
mkdir -p src/Acode.Infrastructure/Persistence
mkdir -p src/Acode.Infrastructure/Services
mkdir -p src/Acode.Infrastructure/External

# CLI folders
mkdir -p src/Acode.Cli/Commands
```

### Step 7: Create Placeholder Classes

**src/Acode.Domain/Entities/PlaceholderEntity.cs:**
```csharp
namespace Acode.Domain.Entities;

/// <summary>
/// Placeholder entity for initial project structure validation.
/// </summary>
[Obsolete("Placeholder for initial structure - replace with real entities")]
public sealed class PlaceholderEntity
{
    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
}
```

### Step 8: Update Test Projects

Each test project should reference its corresponding production project and have test packages:

```xml
<!-- Example: tests/Acode.Domain.Tests/Acode.Domain.Tests.csproj -->
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" />
  <PackageReference Include="xunit" />
  <PackageReference Include="xunit.runner.visualstudio" />
  <PackageReference Include="FluentAssertions" />
  <PackageReference Include="NSubstitute" />
  <PackageReference Include="coverlet.collector" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\..\src\Acode.Domain\Acode.Domain.csproj" />
</ItemGroup>
```

### Step 9: Create Placeholder Tests

**tests/Acode.Domain.Tests/PlaceholderTests.cs:**
```csharp
using FluentAssertions;

namespace Acode.Domain.Tests;

public class PlaceholderTests
{
    [Fact]
    public void PlaceholderTest_ShouldPass_WhenProjectStructureIsValid()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        actual.Should().Be(expected, "project structure should be valid");
    }
}
```

### Step 10: Verify and Commit

```bash
# Verify build
dotnet restore
dotnet build
dotnet test

# Commit
git add .
git commit -m "Initial commit: project structure

- Add Clean Architecture solution structure
- Configure central package management
- Add placeholder classes and tests
- Configure build settings"
```

### Validation Checklist Before Merge

- [ ] `dotnet restore` succeeds
- [ ] `dotnet build` succeeds with zero warnings
- [ ] `dotnet test` succeeds with all tests passing
- [ ] Solution loads in Visual Studio without errors
- [ ] Solution loads in VS Code without errors
- [ ] All projects target net8.0
- [ ] TreatWarningsAsErrors is enabled
- [ ] Nullable reference types are enabled
- [ ] Central package management is working
- [ ] Layer dependencies are correct
- [ ] All placeholder classes are marked [Obsolete]
- [ ] Git history is clean

### Rollout Plan

1. **Local Development:** Create structure on development machine
2. **Initial Commit:** Push to main branch
3. **Team Notification:** Announce new repository structure
4. **CI Setup:** Configure CI pipeline (Task in Epic 8)
5. **Documentation:** Complete Task 000.b for documentation

### Error Codes

| Code | Description | Resolution |
|------|-------------|------------|
| E000-01 | .NET SDK not found | Install .NET 8.0+ SDK |
| E000-02 | Git not found | Install Git |
| E000-03 | Directory not empty | Choose empty directory or clean existing |
| E000-04 | Permission denied | Run with appropriate permissions |
| E000-05 | NuGet restore failed | Check network; verify package sources |

### Logging Fields

| Field | Type | Description |
|-------|------|-------------|
| `task_id` | string | "000" |
| `operation` | string | Operation being performed |
| `timestamp` | datetime | UTC timestamp |
| `duration_ms` | long | Operation duration in milliseconds |
| `success` | boolean | Whether operation succeeded |
| `error_code` | string | Error code if failed |
| `error_message` | string | Error details if failed |

---

**END OF TASK 000**
