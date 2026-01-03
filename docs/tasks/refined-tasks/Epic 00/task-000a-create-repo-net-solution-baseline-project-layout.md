# Task 000.a: Create Repo + .NET Solution + Baseline Project Layout

**Priority:** 1 / 49  
**Tier:** Foundation  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 0 — Foundation  
**Dependencies:** None (First implementation subtask)  

---

## Description

### Overview

Task 000.a is the primary implementation subtask for creating the physical repository structure and .NET solution for the Agentic Coding Bot (Acode). While Task 000 defines the overall vision and requirements, Task 000.a focuses on the hands-on creation of files, directories, and project configurations. This task produces the actual artifacts that subsequent tasks and epics will build upon.

### Business Value

This subtask delivers immediate, tangible value by creating a working codebase:

1. **Enables parallel development** — Once the structure exists, multiple developers can work on different layers simultaneously
2. **Establishes conventions** — The initial structure sets precedents that all future code will follow
3. **Validates architecture decisions** — A buildable solution proves the architecture is sound
4. **Reduces friction** — Developers can start coding immediately instead of setting up infrastructure
5. **Prevents drift** — Central configuration prevents projects from diverging

The output of this task is a fully functional, buildable, testable .NET solution that serves as the skeleton for the entire Acode project.

### Scope Boundaries

**In Scope:**
- Git repository initialization
- `.gitignore` and `.gitattributes` file creation
- Solution file (Acode.sln) creation
- All five production projects (Domain, Application, Infrastructure, CLI)
- All five test projects
- `Directory.Build.props` with shared settings
- `Directory.Packages.props` with centralized package versions
- Project references between layers
- Folder structure within each project
- Placeholder classes for compilation verification
- Placeholder tests for test framework verification
- Initial Git commit

**Out of Scope:**
- README.md or any documentation files (Task 000.b)
- EditorConfig or formatting configuration (Task 000.c)
- CI/CD configuration
- Any actual business logic implementation
- Docker configuration
- IDE-specific settings files (.vscode/, .idea/)

### Integration Points

| Task | Relationship | Description |
|------|--------------|-------------|
| Task 000 | Parent | Defines requirements this task implements |
| Task 000.b | Sibling | Will add documentation to this structure |
| Task 000.c | Sibling | Will add tooling to this structure |
| Task 001 | Consumer | Will add OperatingMode interfaces |
| Task 002 | Consumer | Will add config parser interfaces |
| Task 003 | Consumer | Will add safety policy interfaces |

### Assumptions

1. The target directory is empty or does not exist
2. .NET 8.0 SDK is installed and available in PATH
3. Git is installed and configured with user.name and user.email
4. The developer has sufficient permissions to create directories and files
5. At least 1GB of free disk space is available
6. NuGet.org is accessible (or packages are cached locally)

### Failure Modes

| Failure Mode | Symptom | Detection | Recovery |
|--------------|---------|-----------|----------|
| SDK missing | `dotnet` command not found | Check PATH | Install .NET 8.0 SDK |
| Old SDK version | TargetFramework error | Check `dotnet --version` | Install/update SDK |
| Permissions error | Access denied | File operation fails | Adjust permissions |
| Directory exists | Files already present | Check before start | Choose new directory |
| Disk full | Write fails | Disk space check | Free space |
| Git not configured | Commit fails | Check git config | Set user.name/email |
| Network issues | Restore fails | NuGet errors | Check network/cache |

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **SDK-style project** | Modern .NET project format with simplified csproj files |
| **TargetFramework** | The .NET version a project targets (e.g., net8.0) |
| **ClassLib** | Class library project type (produces .dll) |
| **Console** | Console application project type (produces .exe) |
| **xUnit** | Test framework used in this project |
| **ProjectReference** | MSBuild element linking one project to another |
| **PackageReference** | MSBuild element referencing a NuGet package |
| **Solution Folder** | Virtual folder in solution for organizing projects |
| **Global.json** | File that locks SDK version for the solution |
| **NuGet restore** | Process of downloading referenced packages |
| **Incremental build** | Build that only recompiles changed files |
| **Assembly** | Compiled .NET output (.dll or .exe) |
| **PDB** | Program Database file containing debug symbols |
| **RootNamespace** | Default namespace for files in a project |

---

## Out of Scope

- Creating README.md, CONTRIBUTING.md, or other documentation files
- Creating .editorconfig or other formatting configuration
- Setting up code coverage tooling
- Setting up mutation testing
- Creating CI/CD pipeline files (.github/workflows/)
- Creating Docker files (Dockerfile, docker-compose.yml)
- Creating IDE configuration files (.vscode/, .idea/)
- Implementing any interfaces or business logic
- Creating database schemas or migrations
- Setting up logging infrastructure
- Configuring dependency injection
- Creating any configuration files for the agent

---

## Functional Requirements

### Git Repository (FR-000a-01 to FR-000a-12)

| ID | Requirement |
|----|-------------|
| FR-000a-01 | MUST create a new Git repository with `git init` |
| FR-000a-02 | MUST create `.gitignore` at repository root |
| FR-000a-03 | `.gitignore` MUST include patterns for .NET build artifacts |
| FR-000a-04 | `.gitignore` MUST include patterns for Visual Studio files |
| FR-000a-05 | `.gitignore` MUST include patterns for common IDE files |
| FR-000a-06 | `.gitignore` MUST include patterns for Node.js artifacts |
| FR-000a-07 | `.gitignore` MUST include patterns for Python artifacts |
| FR-000a-08 | `.gitignore` MUST include `.env` files |
| FR-000a-09 | MUST create `.gitattributes` at repository root |
| FR-000a-10 | `.gitattributes` MUST normalize line endings |
| FR-000a-11 | `.gitattributes` MUST use LF for source files |
| FR-000a-12 | `.gitattributes` MUST use CRLF for .sln files |

### Solution File (FR-000a-13 to FR-000a-20)

| ID | Requirement |
|----|-------------|
| FR-000a-13 | MUST create `Acode.sln` at repository root |
| FR-000a-14 | Solution MUST include all nine projects |
| FR-000a-15 | Solution MUST have `src` solution folder |
| FR-000a-16 | Solution MUST have `tests` solution folder |
| FR-000a-17 | Production projects MUST be in `src` folder |
| FR-000a-18 | Test projects MUST be in `tests` folder |
| FR-000a-19 | Solution MUST be valid Visual Studio solution format |
| FR-000a-20 | Solution MUST load without errors in VS 2022 |

### Production Projects (FR-000a-21 to FR-000a-35)

| ID | Requirement |
|----|-------------|
| FR-000a-21 | MUST create `src/Acode.Domain/Acode.Domain.csproj` |
| FR-000a-22 | MUST create `src/Acode.Application/Acode.Application.csproj` |
| FR-000a-23 | MUST create `src/Acode.Infrastructure/Acode.Infrastructure.csproj` |
| FR-000a-24 | MUST create `src/Acode.Cli/Acode.Cli.csproj` |
| FR-000a-25 | Domain project MUST be a class library |
| FR-000a-26 | Application project MUST be a class library |
| FR-000a-27 | Infrastructure project MUST be a class library |
| FR-000a-28 | CLI project MUST be a console application |
| FR-000a-29 | All projects MUST target `net8.0` |
| FR-000a-30 | All projects MUST use SDK-style format |
| FR-000a-31 | Project file size MUST be minimal (SDK style) |
| FR-000a-32 | Projects MUST NOT specify TargetFramework (use Directory.Build.props) |
| FR-000a-33 | Projects MUST NOT specify LangVersion (use Directory.Build.props) |
| FR-000a-34 | Projects MUST NOT specify package versions (use central management) |
| FR-000a-35 | CLI project MUST specify `<OutputType>Exe</OutputType>` |

### Test Projects (FR-000a-36 to FR-000a-48)

| ID | Requirement |
|----|-------------|
| FR-000a-36 | MUST create `tests/Acode.Domain.Tests/Acode.Domain.Tests.csproj` |
| FR-000a-37 | MUST create `tests/Acode.Application.Tests/Acode.Application.Tests.csproj` |
| FR-000a-38 | MUST create `tests/Acode.Infrastructure.Tests/Acode.Infrastructure.Tests.csproj` |
| FR-000a-39 | MUST create `tests/Acode.Cli.Tests/Acode.Cli.Tests.csproj` |
| FR-000a-40 | MUST create `tests/Acode.Integration.Tests/Acode.Integration.Tests.csproj` |
| FR-000a-41 | All test projects MUST reference xunit |
| FR-000a-42 | All test projects MUST reference xunit.runner.visualstudio |
| FR-000a-43 | All test projects MUST reference Microsoft.NET.Test.Sdk |
| FR-000a-44 | All test projects MUST reference FluentAssertions |
| FR-000a-45 | All test projects MUST reference NSubstitute |
| FR-000a-46 | All test projects MUST reference coverlet.collector |
| FR-000a-47 | Each test project MUST reference its corresponding production project |
| FR-000a-48 | Integration.Tests MUST reference all production projects |

### Project References (FR-000a-49 to FR-000a-58)

| ID | Requirement |
|----|-------------|
| FR-000a-49 | Domain MUST NOT reference any other Acode project |
| FR-000a-50 | Application MUST reference only Domain |
| FR-000a-51 | Infrastructure MUST reference Domain and Application |
| FR-000a-52 | CLI MUST reference Domain, Application, and Infrastructure |
| FR-000a-53 | Domain.Tests MUST reference Domain |
| FR-000a-54 | Application.Tests MUST reference Application |
| FR-000a-55 | Infrastructure.Tests MUST reference Infrastructure |
| FR-000a-56 | Cli.Tests MUST reference Cli |
| FR-000a-57 | Integration.Tests MUST reference all production projects |
| FR-000a-58 | No circular references MUST exist |

### Build Configuration (FR-000a-59 to FR-000a-72)

| ID | Requirement |
|----|-------------|
| FR-000a-59 | MUST create `Directory.Build.props` at repository root |
| FR-000a-60 | Directory.Build.props MUST set TargetFramework to net8.0 |
| FR-000a-61 | Directory.Build.props MUST set LangVersion to latest |
| FR-000a-62 | Directory.Build.props MUST enable Nullable |
| FR-000a-63 | Directory.Build.props MUST enable ImplicitUsings |
| FR-000a-64 | Directory.Build.props MUST enable TreatWarningsAsErrors |
| FR-000a-65 | Directory.Build.props MUST enable GenerateDocumentationFile |
| FR-000a-66 | MUST create `Directory.Packages.props` at repository root |
| FR-000a-67 | Directory.Packages.props MUST enable ManagePackageVersionsCentrally |
| FR-000a-68 | Directory.Packages.props MUST define all package versions |
| FR-000a-69 | Package versions MUST be pinned (not floating) |
| FR-000a-70 | MUST create `global.json` to lock SDK version |
| FR-000a-71 | global.json MUST specify SDK version 8.0.x |
| FR-000a-72 | global.json MUST allow roll-forward for patches |

### Folder Structure (FR-000a-73 to FR-000a-88)

| ID | Requirement |
|----|-------------|
| FR-000a-73 | Domain MUST have Entities/ folder |
| FR-000a-74 | Domain MUST have ValueObjects/ folder |
| FR-000a-75 | Domain MUST have Services/ folder |
| FR-000a-76 | Domain MUST have Interfaces/ folder |
| FR-000a-77 | Application MUST have UseCases/ folder |
| FR-000a-78 | Application MUST have DTOs/ folder |
| FR-000a-79 | Application MUST have Interfaces/ folder |
| FR-000a-80 | Infrastructure MUST have Persistence/ folder |
| FR-000a-81 | Infrastructure MUST have Services/ folder |
| FR-000a-82 | Infrastructure MUST have External/ folder |
| FR-000a-83 | CLI MUST have Commands/ folder |
| FR-000a-84 | Each folder MUST contain a placeholder file or .gitkeep |
| FR-000a-85 | Folder names MUST use PascalCase |
| FR-000a-86 | Folder structure MUST match namespace structure |
| FR-000a-87 | No empty folders without .gitkeep |
| FR-000a-88 | No nested folders beyond two levels initially |

### Placeholder Files (FR-000a-89 to FR-000a-100)

| ID | Requirement |
|----|-------------|
| FR-000a-89 | Each project MUST have at least one .cs file |
| FR-000a-90 | Placeholder classes MUST be marked [Obsolete] |
| FR-000a-91 | Placeholder classes MUST have XML documentation |
| FR-000a-92 | Placeholder tests MUST pass |
| FR-000a-93 | Placeholder tests MUST use FluentAssertions |
| FR-000a-94 | CLI MUST have Program.cs with Main method |
| FR-000a-95 | Program.cs MUST exit cleanly |
| FR-000a-96 | Namespaces MUST match folder paths |
| FR-000a-97 | File names MUST match class names |
| FR-000a-98 | One public class per file |
| FR-000a-99 | All files MUST compile without errors |
| FR-000a-100 | All files MUST compile without warnings |

---

## Non-Functional Requirements

### Performance (NFR-000a-01 to NFR-000a-10)

| ID | Requirement |
|----|-------------|
| NFR-000a-01 | Repository creation MUST complete in under 5 minutes |
| NFR-000a-02 | NuGet restore MUST complete in under 2 minutes (with cache) |
| NFR-000a-03 | Full build MUST complete in under 30 seconds |
| NFR-000a-04 | Incremental build MUST complete in under 5 seconds |
| NFR-000a-05 | Test execution MUST complete in under 30 seconds |
| NFR-000a-06 | Solution load in VS MUST complete in under 10 seconds |
| NFR-000a-07 | IntelliSense activation MUST complete in under 5 seconds |
| NFR-000a-08 | Git operations MUST complete in under 5 seconds |
| NFR-000a-09 | Total disk usage MUST be under 200MB after build |
| NFR-000a-10 | Memory usage during build MUST be under 2GB |

### Reliability (NFR-000a-11 to NFR-000a-18)

| ID | Requirement |
|----|-------------|
| NFR-000a-11 | Creation script MUST be idempotent |
| NFR-000a-12 | Creation MUST work on Windows 10/11 |
| NFR-000a-13 | Creation MUST work on macOS 12+ |
| NFR-000a-14 | Creation MUST work on Ubuntu 22.04+ |
| NFR-000a-15 | Build MUST be deterministic |
| NFR-000a-16 | Build MUST succeed offline (with cached packages) |
| NFR-000a-17 | No file corruption on interrupted build |
| NFR-000a-18 | Partial creation MUST be cleanable |

### Maintainability (NFR-000a-19 to NFR-000a-26)

| ID | Requirement |
|----|-------------|
| NFR-000a-19 | Configuration centralized in two files |
| NFR-000a-20 | No duplicate settings across projects |
| NFR-000a-21 | Adding a project requires minimal changes |
| NFR-000a-22 | Removing a project requires minimal changes |
| NFR-000a-23 | Upgrading TargetFramework is single change |
| NFR-000a-24 | Upgrading packages is centralized |
| NFR-000a-25 | All magic numbers documented |
| NFR-000a-26 | All configuration choices documented |

### Security (NFR-000a-27 to NFR-000a-32)

| ID | Requirement |
|----|-------------|
| NFR-000a-27 | No secrets in repository |
| NFR-000a-28 | .gitignore prevents secret commits |
| NFR-000a-29 | No credentials in configuration |
| NFR-000a-30 | Package sources verified |
| NFR-000a-31 | No unsigned packages |
| NFR-000a-32 | No known vulnerable packages at creation |

---

## User Manual Documentation

### Quick Start Guide

#### Prerequisites

Before running the creation script, ensure you have:

1. **.NET 8.0 SDK** or later
   ```powershell
   # Windows - Check version
   dotnet --version
   # Should output 8.0.x or higher
   
   # Install if needed
   winget install Microsoft.DotNet.SDK.8
   ```

2. **Git** configured with user identity
   ```bash
   git --version
   git config --global user.name "Your Name"
   git config --global user.email "your.email@example.com"
   ```

3. **Empty target directory** or permission to create one

#### Creation Steps

```powershell
# Step 1: Create and enter directory
New-Item -ItemType Directory -Path "C:\dev\acode" -Force
Set-Location "C:\dev\acode"

# Step 2: Initialize Git
git init

# Step 3: Run creation script (or manual steps below)
# If automated script exists:
.\scripts\create-solution.ps1

# Or manually follow the implementation steps
```

### File Structure After Creation

```
acode/
├── .git/                           # Git repository data
├── .gitignore                      # Git ignore patterns
├── .gitattributes                  # Git attributes
├── global.json                     # SDK version lock
├── Acode.sln                       # Solution file
├── Directory.Build.props           # Shared MSBuild properties
├── Directory.Packages.props        # Central package versions
├── src/
│   ├── Acode.Domain/
│   │   ├── Acode.Domain.csproj
│   │   ├── Entities/
│   │   │   └── PlaceholderEntity.cs
│   │   ├── ValueObjects/
│   │   │   └── .gitkeep
│   │   ├── Services/
│   │   │   └── .gitkeep
│   │   └── Interfaces/
│   │       └── .gitkeep
│   ├── Acode.Application/
│   │   ├── Acode.Application.csproj
│   │   ├── UseCases/
│   │   │   └── PlaceholderUseCase.cs
│   │   ├── DTOs/
│   │   │   └── .gitkeep
│   │   └── Interfaces/
│   │       └── .gitkeep
│   ├── Acode.Infrastructure/
│   │   ├── Acode.Infrastructure.csproj
│   │   ├── Persistence/
│   │   │   └── .gitkeep
│   │   ├── Services/
│   │   │   └── PlaceholderService.cs
│   │   └── External/
│   │       └── .gitkeep
│   └── Acode.Cli/
│       ├── Acode.Cli.csproj
│       ├── Program.cs
│       └── Commands/
│           └── .gitkeep
└── tests/
    ├── Acode.Domain.Tests/
    │   ├── Acode.Domain.Tests.csproj
    │   └── PlaceholderEntityTests.cs
    ├── Acode.Application.Tests/
    │   ├── Acode.Application.Tests.csproj
    │   └── PlaceholderUseCaseTests.cs
    ├── Acode.Infrastructure.Tests/
    │   ├── Acode.Infrastructure.Tests.csproj
    │   └── PlaceholderServiceTests.cs
    ├── Acode.Cli.Tests/
    │   ├── Acode.Cli.Tests.csproj
    │   └── ProgramTests.cs
    └── Acode.Integration.Tests/
        ├── Acode.Integration.Tests.csproj
        └── SmokeTests.cs
```

### Configuration Reference

#### Directory.Build.props

| Property | Value | Purpose |
|----------|-------|---------|
| `TargetFramework` | net8.0 | Target .NET 8.0 runtime |
| `LangVersion` | latest | Enable latest C# features |
| `Nullable` | enable | Enable nullable reference types |
| `ImplicitUsings` | enable | Auto-import common namespaces |
| `TreatWarningsAsErrors` | true | Fail build on warnings |
| `GenerateDocumentationFile` | true | Generate XML docs |

#### Directory.Packages.props

| Package | Version | Purpose |
|---------|---------|---------|
| `xunit` | 2.6.6 | Test framework |
| `xunit.runner.visualstudio` | 2.5.6 | VS test integration |
| `Microsoft.NET.Test.Sdk` | 17.9.0 | Test SDK |
| `FluentAssertions` | 6.12.0 | Readable assertions |
| `NSubstitute` | 5.1.0 | Mocking framework |
| `coverlet.collector` | 6.0.0 | Code coverage |

#### global.json

```json
{
  "sdk": {
    "version": "8.0.100",
    "rollForward": "latestPatch"
  }
}
```

### Common Operations

#### Building the Solution

```bash
# Restore packages (usually automatic)
dotnet restore

# Build all projects
dotnet build

# Build specific project
dotnet build src/Acode.Domain

# Build in Release mode
dotnet build -c Release

# Build with detailed output
dotnet build -v detailed
```

#### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Acode.Domain.Tests

# Run with detailed output
dotnet test -v normal

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~PlaceholderTest"
```

#### Running the CLI

```bash
# Run directly
dotnet run --project src/Acode.Cli

# Build and run
dotnet build
./src/Acode.Cli/bin/Debug/net8.0/Acode.Cli.exe

# Publish and run
dotnet publish src/Acode.Cli -c Release -o ./publish
./publish/Acode.Cli.exe
```

### Best Practices

1. **Always restore before first build**
   ```bash
   dotnet restore
   dotnet build
   ```

2. **Use solution-level commands**
   ```bash
   # Good: From repository root
   dotnet build
   
   # Avoid: From project directory
   cd src/Acode.Domain
   dotnet build  # May miss dependencies
   ```

3. **Clean before Release builds**
   ```bash
   dotnet clean
   dotnet build -c Release
   ```

4. **Check for outdated packages regularly**
   ```bash
   dotnet list package --outdated
   ```

5. **Verify no vulnerable packages**
   ```bash
   dotnet list package --vulnerable
   ```

6. **Run tests before committing**
   ```bash
   dotnet test
   git add .
   git commit -m "Your message"
   ```

7. **Use meaningful commit messages**
   ```bash
   git commit -m "feat(domain): add User entity with validation"
   ```

### Troubleshooting Guide

#### Problem: `dotnet` command not found

**Symptom:** Command not recognized

**Cause:** .NET SDK not installed or not in PATH

**Solution:**
```powershell
# Windows - Install via winget
winget install Microsoft.DotNet.SDK.8

# Or download from https://dotnet.microsoft.com/download

# Verify installation
dotnet --version
```

#### Problem: Wrong SDK version

**Symptom:** Error about unsupported target framework

**Cause:** Older SDK installed, global.json pointing to specific version

**Solution:**
```bash
# Check installed SDKs
dotnet --list-sdks

# Install correct version
# global.json will pick it up automatically
```

#### Problem: NuGet restore fails

**Symptom:** Package restore errors

**Cause:** Network issues, package source problems

**Solution:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore with source explicitly
dotnet restore --source https://api.nuget.org/v3/index.json

# Check for proxy issues
echo %HTTP_PROXY%
```

#### Problem: Build warnings treated as errors

**Symptom:** Build fails on warnings

**Cause:** TreatWarningsAsErrors=true (intentional)

**Solution:**
```csharp
// Fix the warning! Common ones:

// CS8618: Non-nullable property not initialized
public string Name { get; set; } = string.Empty; // Add default

// CS1591: Missing XML comment
/// <summary>
/// Describe the member here.
/// </summary>
public void MyMethod() { }

// For suppressions (use sparingly):
#pragma warning disable CS8618
public string Name { get; set; }
#pragma warning restore CS8618
```

#### Problem: Tests not discovered

**Symptom:** `dotnet test` finds 0 tests

**Cause:** Missing test SDK packages

**Solution:**
```xml
<!-- Ensure test project has ALL of these -->
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio" />
```

#### Problem: Project reference errors

**Symptom:** "Could not resolve project reference"

**Cause:** Incorrect relative path in csproj

**Solution:**
```xml
<!-- Verify path is correct -->
<ProjectReference Include="..\Acode.Domain\Acode.Domain.csproj" />

<!-- Regenerate references -->
dotnet remove reference ..\Acode.Domain\Acode.Domain.csproj
dotnet add reference ..\Acode.Domain\Acode.Domain.csproj
```

### FAQ

**Q: Why use central package management?**

A: Central package management (Directory.Packages.props) ensures all projects use the same package versions. This prevents version conflicts, reduces csproj file size, and makes upgrades easier. When you update a version in one place, all projects get the update.

**Q: Why is TreatWarningsAsErrors enabled?**

A: Warnings often indicate real issues that become bugs later. By treating them as errors from the start, we maintain code quality and catch issues early. This is a best practice for professional projects.

**Q: Can I use a different test framework?**

A: The project is standardized on xUnit. Changing test frameworks would require significant updates to test infrastructure and is not recommended. If you have a compelling reason, submit an Architecture Decision Record (ADR).

**Q: Why are placeholder classes marked [Obsolete]?**

A: The [Obsolete] attribute generates a compile-time warning when the class is used. This reminds developers to replace placeholders with real implementations and makes it easy to find all placeholder code.

**Q: How do I add a new project?**

A: Follow these steps:
```bash
# Create the project
dotnet new classlib -n Acode.NewProject -o src/Acode.NewProject

# Add to solution
dotnet sln add src/Acode.NewProject/Acode.NewProject.csproj --solution-folder src

# Add references as needed
cd src/Acode.NewProject
dotnet add reference ..\Acode.Domain\Acode.Domain.csproj
```

**Q: Why net8.0 and not net9.0?**

A: .NET 8.0 is the current Long-Term Support (LTS) release with support until November 2026. .NET 9.0 is a Standard-Term Support release. For a production system, LTS provides better stability guarantees.

---

## Acceptance Criteria / Definition of Done

### Git Repository (25 items)

- [ ] Git repository initialized (`git rev-parse --is-inside-work-tree` returns true)
- [ ] `.gitignore` exists at repository root
- [ ] `.gitignore` contains `bin/` pattern
- [ ] `.gitignore` contains `obj/` pattern
- [ ] `.gitignore` contains `*.user` pattern
- [ ] `.gitignore` contains `.vs/` pattern
- [ ] `.gitignore` contains `node_modules/` pattern
- [ ] `.gitignore` contains `__pycache__/` pattern
- [ ] `.gitignore` contains `.env` pattern
- [ ] `.gitattributes` exists at repository root
- [ ] `.gitattributes` contains `* text=auto`
- [ ] `.gitattributes` contains `*.cs text eol=lf`
- [ ] `.gitattributes` contains `*.sln text eol=crlf`
- [ ] `global.json` exists at repository root
- [ ] `global.json` specifies SDK version 8.0.x
- [ ] `global.json` allows rollForward
- [ ] Initial commit created
- [ ] Initial commit message is descriptive
- [ ] No uncommitted changes after creation
- [ ] No untracked files after commit
- [ ] `.git/config` exists
- [ ] Repository is valid (`git status` succeeds)
- [ ] No sensitive files in repository
- [ ] File line endings normalized
- [ ] Binary files handled correctly

### Solution Structure (20 items)

- [ ] `Acode.sln` exists at repository root
- [ ] Solution file is valid XML format
- [ ] Solution contains 9 projects total
- [ ] Solution contains `src` solution folder
- [ ] Solution contains `tests` solution folder
- [ ] `Acode.Domain` in src folder
- [ ] `Acode.Application` in src folder
- [ ] `Acode.Infrastructure` in src folder
- [ ] `Acode.Cli` in src folder
- [ ] `Acode.Domain.Tests` in tests folder
- [ ] `Acode.Application.Tests` in tests folder
- [ ] `Acode.Infrastructure.Tests` in tests folder
- [ ] `Acode.Cli.Tests` in tests folder
- [ ] `Acode.Integration.Tests` in tests folder
- [ ] Solution loads in Visual Studio 2022
- [ ] Solution loads in VS Code
- [ ] Solution loads in JetBrains Rider
- [ ] No load errors reported
- [ ] All projects show in Solution Explorer
- [ ] Project hierarchy matches folder structure

### Production Projects (30 items)

- [ ] `src/Acode.Domain/Acode.Domain.csproj` exists
- [ ] Domain project is class library
- [ ] Domain project compiles
- [ ] Domain has no ProjectReferences
- [ ] `src/Acode.Application/Acode.Application.csproj` exists
- [ ] Application project is class library
- [ ] Application project compiles
- [ ] Application references only Domain
- [ ] `src/Acode.Infrastructure/Acode.Infrastructure.csproj` exists
- [ ] Infrastructure project is class library
- [ ] Infrastructure project compiles
- [ ] Infrastructure references Domain
- [ ] Infrastructure references Application
- [ ] `src/Acode.Cli/Acode.Cli.csproj` exists
- [ ] CLI project has OutputType=Exe
- [ ] CLI project compiles
- [ ] CLI references Domain
- [ ] CLI references Application
- [ ] CLI references Infrastructure
- [ ] All projects target net8.0
- [ ] All projects use SDK-style format
- [ ] No TargetFramework in individual csproj
- [ ] No LangVersion in individual csproj
- [ ] No package versions in csproj files
- [ ] All projects have XML documentation enabled
- [ ] All projects have nullable enabled
- [ ] All projects have warnings as errors
- [ ] CLI produces executable
- [ ] CLI runs without crash
- [ ] No circular references

### Test Projects (30 items)

- [ ] `tests/Acode.Domain.Tests/Acode.Domain.Tests.csproj` exists
- [ ] Domain.Tests references Domain
- [ ] Domain.Tests has xunit package
- [ ] Domain.Tests has xunit.runner.visualstudio
- [ ] Domain.Tests has Microsoft.NET.Test.Sdk
- [ ] Domain.Tests has FluentAssertions
- [ ] Domain.Tests has NSubstitute
- [ ] Domain.Tests has coverlet.collector
- [ ] `tests/Acode.Application.Tests/Acode.Application.Tests.csproj` exists
- [ ] Application.Tests references Application
- [ ] Application.Tests has all test packages
- [ ] `tests/Acode.Infrastructure.Tests/Acode.Infrastructure.Tests.csproj` exists
- [ ] Infrastructure.Tests references Infrastructure
- [ ] Infrastructure.Tests has all test packages
- [ ] `tests/Acode.Cli.Tests/Acode.Cli.Tests.csproj` exists
- [ ] Cli.Tests references Cli
- [ ] Cli.Tests has all test packages
- [ ] `tests/Acode.Integration.Tests/Acode.Integration.Tests.csproj` exists
- [ ] Integration.Tests references all production projects
- [ ] Integration.Tests has all test packages
- [ ] All test projects compile
- [ ] All test projects are discoverable
- [ ] Test execution works
- [ ] Coverage collection works
- [ ] At least one test per test project
- [ ] All placeholder tests pass
- [ ] Tests run in under 30 seconds
- [ ] Tests can run in parallel
- [ ] Tests are isolated
- [ ] No test has external dependencies

### Build Configuration (25 items)

- [ ] `Directory.Build.props` exists at root
- [ ] Directory.Build.props sets TargetFramework
- [ ] Directory.Build.props sets LangVersion=latest
- [ ] Directory.Build.props sets Nullable=enable
- [ ] Directory.Build.props sets ImplicitUsings=enable
- [ ] Directory.Build.props sets TreatWarningsAsErrors=true
- [ ] Directory.Build.props sets GenerateDocumentationFile=true
- [ ] Directory.Build.props sets Company
- [ ] Directory.Build.props sets Authors
- [ ] `Directory.Packages.props` exists at root
- [ ] ManagePackageVersionsCentrally enabled
- [ ] xunit version defined
- [ ] xunit.runner.visualstudio version defined
- [ ] Microsoft.NET.Test.Sdk version defined
- [ ] FluentAssertions version defined
- [ ] NSubstitute version defined
- [ ] coverlet.collector version defined
- [ ] All versions are pinned (not floating)
- [ ] `dotnet restore` succeeds
- [ ] `dotnet build` succeeds
- [ ] `dotnet build` has zero warnings
- [ ] `dotnet build -c Release` succeeds
- [ ] Incremental build works
- [ ] Clean build works
- [ ] XML docs generated

### Folder Structure (20 items)

- [ ] `src/Acode.Domain/Entities/` exists
- [ ] `src/Acode.Domain/ValueObjects/` exists
- [ ] `src/Acode.Domain/Services/` exists
- [ ] `src/Acode.Domain/Interfaces/` exists
- [ ] `src/Acode.Application/UseCases/` exists
- [ ] `src/Acode.Application/DTOs/` exists
- [ ] `src/Acode.Application/Interfaces/` exists
- [ ] `src/Acode.Infrastructure/Persistence/` exists
- [ ] `src/Acode.Infrastructure/Services/` exists
- [ ] `src/Acode.Infrastructure/External/` exists
- [ ] `src/Acode.Cli/Commands/` exists
- [ ] All empty folders have .gitkeep
- [ ] No nested folders beyond specification
- [ ] Folder names use PascalCase
- [ ] Folder structure matches namespaces
- [ ] No unnecessary folders created
- [ ] No temp files in folders
- [ ] Folder permissions correct
- [ ] Folder paths valid on all platforms
- [ ] No special characters in folder names

### Placeholder Files (25 items)

- [ ] Domain has PlaceholderEntity.cs
- [ ] Application has PlaceholderUseCase.cs
- [ ] Infrastructure has PlaceholderService.cs
- [ ] CLI has Program.cs
- [ ] All placeholder classes marked [Obsolete]
- [ ] All placeholders have XML documentation
- [ ] Namespaces match folder structure
- [ ] File names match class names
- [ ] One public class per file
- [ ] All files compile without errors
- [ ] All files compile without warnings
- [ ] Domain.Tests has PlaceholderEntityTests.cs
- [ ] Application.Tests has PlaceholderUseCaseTests.cs
- [ ] Infrastructure.Tests has PlaceholderServiceTests.cs
- [ ] Cli.Tests has ProgramTests.cs
- [ ] Integration.Tests has SmokeTests.cs
- [ ] All test files have at least one test
- [ ] All tests use FluentAssertions
- [ ] All tests pass
- [ ] No TODO comments in code
- [ ] Proper using statements
- [ ] Proper access modifiers
- [ ] Follows C# naming conventions
- [ ] No magic numbers/strings
- [ ] Consistent code style

### Cross-Platform (15 items)

- [ ] Repository works on Windows
- [ ] Repository works on macOS (if tested)
- [ ] Repository works on Linux (if tested)
- [ ] Path separators handled correctly
- [ ] Line endings handled by .gitattributes
- [ ] Case sensitivity handled
- [ ] No Windows-specific paths
- [ ] No Unix-specific paths
- [ ] File permissions appropriate
- [ ] Executable bit set on scripts (if any)
- [ ] No BOM in UTF-8 files
- [ ] UTF-8 encoding throughout
- [ ] No path length issues on Windows
- [ ] Symlinks not required
- [ ] Works in WSL

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Input | Expected Output |
|----|-----------|-------|-----------------|
| UT-000a-01 | Verify .gitignore exists | Check file | File exists |
| UT-000a-02 | Verify .gitignore patterns | Read file | Contains required patterns |
| UT-000a-03 | Verify .gitattributes exists | Check file | File exists |
| UT-000a-04 | Verify global.json exists | Check file | File exists |
| UT-000a-05 | Verify global.json SDK version | Parse JSON | Contains 8.0.x |
| UT-000a-06 | Verify solution file exists | Check file | Acode.sln exists |
| UT-000a-07 | Verify Domain project exists | Check csproj | File exists |
| UT-000a-08 | Verify Application project exists | Check csproj | File exists |
| UT-000a-09 | Verify Infrastructure project exists | Check csproj | File exists |
| UT-000a-10 | Verify CLI project exists | Check csproj | File exists |
| UT-000a-11 | Verify Domain.Tests exists | Check csproj | File exists |
| UT-000a-12 | Verify Application.Tests exists | Check csproj | File exists |
| UT-000a-13 | Verify Infrastructure.Tests exists | Check csproj | File exists |
| UT-000a-14 | Verify Cli.Tests exists | Check csproj | File exists |
| UT-000a-15 | Verify Integration.Tests exists | Check csproj | File exists |
| UT-000a-16 | Verify Directory.Build.props exists | Check file | File exists |
| UT-000a-17 | Verify TreatWarningsAsErrors | Parse props | Property = true |
| UT-000a-18 | Verify Directory.Packages.props exists | Check file | File exists |
| UT-000a-19 | Verify central package management | Parse props | Property = true |
| UT-000a-20 | Verify placeholder tests pass | Run tests | All pass |

### Integration Tests

| ID | Test Case | Setup | Actions | Expected |
|----|-----------|-------|---------|----------|
| IT-000a-01 | Full build succeeds | Fresh clone | `dotnet build` | Exit code 0 |
| IT-000a-02 | All tests pass | Fresh clone | `dotnet test` | All tests pass |
| IT-000a-03 | CLI runs | Build solution | Run CLI exe | Exit code 0 |
| IT-000a-04 | VS opens solution | Fresh clone | Open in VS | No errors |
| IT-000a-05 | VS Code opens | Fresh clone | Open folder | Extensions work |
| IT-000a-06 | References resolve | Build | Check output | No unresolved refs |
| IT-000a-07 | Coverage works | Run with coverage | Check report | Report generated |
| IT-000a-08 | Release build works | `dotnet build -c Release` | Check output | Build succeeds |
| IT-000a-09 | Clean works | `dotnet clean` | Check folders | bin/obj empty |
| IT-000a-10 | Incremental build | Modify file, build | Check time | < 5 seconds |

### End-to-End Tests

| ID | Test Case | Scenario | Expected Outcome |
|----|-----------|----------|------------------|
| E2E-000a-01 | Fresh clone workflow | Clone, restore, build, test | All succeed |
| E2E-000a-02 | Two developer scenario | Two clones, both build | Both succeed |
| E2E-000a-03 | Add new project | Create project, add to sln | Builds correctly |
| E2E-000a-04 | Upgrade package | Update version, restore | All projects updated |
| E2E-000a-05 | Cross-platform build | Build on different OS | Same logical output |
| E2E-000a-06 | CI simulation | Build in clean container | All steps pass |
| E2E-000a-07 | Offline build | Disconnect, build | Cached packages used |
| E2E-000a-08 | Publish CLI | `dotnet publish` | Runnable output |

### Performance Benchmarks

| ID | Metric | Target | Method |
|----|--------|--------|--------|
| PB-000a-01 | Creation time | < 5 minutes | Stopwatch |
| PB-000a-02 | Restore time (warm) | < 60 seconds | `time dotnet restore` |
| PB-000a-03 | Full build time | < 30 seconds | `time dotnet build` |
| PB-000a-04 | Incremental build | < 5 seconds | Modify one file, time build |
| PB-000a-05 | Test execution | < 30 seconds | `time dotnet test` |
| PB-000a-06 | Solution load (VS) | < 10 seconds | Stopwatch |
| PB-000a-07 | Disk usage | < 200 MB | Folder size after build |

### Regression / Impacted Areas

| Area | Risk | Verification |
|------|------|--------------|
| All downstream tasks | High | Any structure change requires verification |
| Documentation (000.b) | Medium | Paths must match structure |
| Tooling (000.c) | Medium | Must integrate with structure |
| CI/CD (Epic 8) | Low | Pipeline paths depend on structure |

---

## User Verification Steps

### Verification 1: Repository Creation
1. Navigate to target directory
2. Run `git status`
3. **Verify:** Output shows valid Git repository with clean working tree

### Verification 2: Solution File
1. Run `dotnet sln list`
2. **Verify:** All 9 projects listed with correct paths

### Verification 3: Build Success
1. Run `dotnet build`
2. **Verify:** Exit code 0, zero warnings, zero errors

### Verification 4: Test Success
1. Run `dotnet test`
2. **Verify:** All tests discovered and pass

### Verification 5: CLI Execution
1. Run `dotnet run --project src/Acode.Cli`
2. **Verify:** Program executes without exceptions

### Verification 6: Visual Studio Load
1. Open `Acode.sln` in Visual Studio 2022
2. Wait for full load
3. **Verify:** All projects visible, no errors in Error List

### Verification 7: VS Code Load
1. Open repository folder in VS Code
2. Wait for C# extension to initialize
3. Open any .cs file
4. **Verify:** IntelliSense works, no errors shown

### Verification 8: Layer References
1. Open `src/Acode.Domain/Acode.Domain.csproj`
2. Verify no ProjectReference elements
3. Open `src/Acode.Application/Acode.Application.csproj`
4. **Verify:** Only references Domain

### Verification 9: Central Package Management
1. Open any test project csproj
2. Check PackageReference elements
3. **Verify:** No Version attributes, only Include

### Verification 10: Warnings as Errors
1. Add a null warning to a file
2. Run `dotnet build`
3. **Verify:** Build fails with error

### Verification 11: Folder Structure
1. List contents of `src/Acode.Domain/`
2. **Verify:** Entities/, ValueObjects/, Services/, Interfaces/ folders exist

### Verification 12: Placeholder Files
1. Open `src/Acode.Domain/Entities/PlaceholderEntity.cs`
2. **Verify:** Class marked [Obsolete] with documentation

---

## Implementation Prompt for Claude

### Context

You are implementing Task 000.a: Create Repo + .NET Solution + Baseline Project Layout for the Agentic Coding Bot (Acode). This creates the physical repository structure.

### File: .gitignore

```gitignore
# Build artifacts
bin/
obj/
out/
publish/
artifacts/

# IDE and editors
.vs/
.vscode/
.idea/
*.suo
*.user
*.userosscache
*.sln.docstates
*.swp
*.swo
*~

# Rider
.idea/
*.sln.iml

# Visual Studio
*.rsuser
*.userprefs

# Node.js
node_modules/
npm-debug.log*
yarn-debug.log*
yarn-error.log*

# Python
__pycache__/
*.py[cod]
*$py.class
.Python
venv/
.venv/
ENV/

# Logs
logs/
*.log
npm-debug.log*

# Test results
TestResults/
coverage/
*.trx

# NuGet
*.nupkg
*.snupkg
.nuget/
packages/

# OS
.DS_Store
.DS_Store?
._*
.Spotlight-V100
.Trashes
ehthumbs.db
Thumbs.db
desktop.ini

# Secrets - NEVER commit
.env
.env.*
!.env.example
*.pem
*.key
*.pfx
**/secrets/
**/credentials/
appsettings.*.json
!appsettings.json
!appsettings.Development.json.example

# Local tools
.config/dotnet-tools.json
```

### File: .gitattributes

```gitattributes
# Set default behavior to automatically normalize line endings
* text=auto

# Source code - LF
*.cs text eol=lf
*.csx text eol=lf
*.csproj text eol=lf
*.props text eol=lf
*.targets text eol=lf
*.json text eol=lf
*.xml text eol=lf
*.config text eol=lf
*.md text eol=lf
*.yml text eol=lf
*.yaml text eol=lf

# Solution files - CRLF for Visual Studio
*.sln text eol=crlf

# Scripts
*.sh text eol=lf
*.bash text eol=lf
*.ps1 text eol=crlf
*.psm1 text eol=crlf
*.cmd text eol=crlf
*.bat text eol=crlf

# Binary files
*.png binary
*.jpg binary
*.jpeg binary
*.gif binary
*.ico binary
*.pdf binary
*.zip binary
*.dll binary
*.exe binary
```

### File: global.json

```json
{
  "sdk": {
    "version": "8.0.100",
    "rollForward": "latestPatch",
    "allowPrerelease": false
  }
}
```

### File: Directory.Build.props

```xml
<Project>
  <!-- Common properties for all projects -->
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

  <!-- Company/Author information -->
  <PropertyGroup>
    <Company>Acode Project</Company>
    <Authors>Acode Contributors</Authors>
    <Copyright>Copyright © 2025 Acode Project. All rights reserved.</Copyright>
    <Product>Acode - Agentic Coding Bot</Product>
    <RepositoryType>git</RepositoryType>
  </PropertyGroup>

  <!-- Release configuration -->
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <DebugType>none</DebugType>
    <DebugSymbols>false</DebugSymbols>
    <Optimize>true</Optimize>
  </PropertyGroup>

  <!-- Suppress XML comment warnings initially -->
  <PropertyGroup>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>
</Project>
```

### File: Directory.Packages.props

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <!-- Abstractions (safe for Domain layer) -->
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Options" Version="8.0.0" />
    
    <!-- Testing -->
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageVersion Include="xunit" Version="2.6.6" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.5.6" />
    <PackageVersion Include="FluentAssertions" Version="6.12.0" />
    <PackageVersion Include="NSubstitute" Version="5.1.0" />
    <PackageVersion Include="NSubstitute.Analyzers.CSharp" Version="1.0.17" />
    <PackageVersion Include="coverlet.collector" Version="6.0.0" />
    <PackageVersion Include="coverlet.msbuild" Version="6.0.0" />
  </ItemGroup>
</Project>
```

### File: src/Acode.Domain/Acode.Domain.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!-- Domain layer: No external dependencies -->
  <!-- All configuration inherited from Directory.Build.props -->
</Project>
```

### File: src/Acode.Application/Acode.Application.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\Acode.Domain\Acode.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  </ItemGroup>
</Project>
```

### File: src/Acode.Infrastructure/Acode.Infrastructure.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\Acode.Domain\Acode.Domain.csproj" />
    <ProjectReference Include="..\Acode.Application\Acode.Application.csproj" />
  </ItemGroup>
</Project>
```

### File: src/Acode.Cli/Acode.Cli.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Acode.Domain\Acode.Domain.csproj" />
    <ProjectReference Include="..\Acode.Application\Acode.Application.csproj" />
    <ProjectReference Include="..\Acode.Infrastructure\Acode.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
  </ItemGroup>
</Project>
```

### File: src/Acode.Domain/Entities/PlaceholderEntity.cs

```csharp
namespace Acode.Domain.Entities;

/// <summary>
/// Placeholder entity to verify Domain layer compilation.
/// This class should be replaced with actual domain entities.
/// </summary>
[Obsolete("Placeholder for initial structure. Replace with actual domain entities.")]
public sealed class PlaceholderEntity
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the name of this entity.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the date and time when this entity was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
```

### File: src/Acode.Cli/Program.cs

```csharp
namespace Acode.Cli;

/// <summary>
/// Entry point for the Acode CLI application.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code (0 for success).</returns>
    public static int Main(string[] args)
    {
        Console.WriteLine("Acode - Agentic Coding Bot");
        Console.WriteLine("Version: 0.1.0-alpha");
        Console.WriteLine();
        Console.WriteLine("This is a placeholder. Implementation coming in future tasks.");
        
        return 0;
    }
}
```

### File: tests/Acode.Domain.Tests/Acode.Domain.Tests.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

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
</Project>
```

### File: tests/Acode.Domain.Tests/PlaceholderEntityTests.cs

```csharp
using Acode.Domain.Entities;
using FluentAssertions;

namespace Acode.Domain.Tests;

/// <summary>
/// Placeholder tests to verify test infrastructure.
/// </summary>
public class PlaceholderEntityTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithNewGuid_WhenCalled()
    {
        // Arrange & Act
#pragma warning disable CS0618 // Obsolete warning expected for placeholder
        var entity = new PlaceholderEntity();
#pragma warning restore CS0618

        // Assert
        entity.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithEmptyName_WhenCalled()
    {
        // Arrange & Act
#pragma warning disable CS0618
        var entity = new PlaceholderEntity();
#pragma warning restore CS0618

        // Assert
        entity.Name.Should().BeEmpty();
    }

    [Fact]
    public void CreatedAt_ShouldBeRecentUtcTime_WhenEntityCreated()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
#pragma warning disable CS0618
        var entity = new PlaceholderEntity();
#pragma warning restore CS0618
        var after = DateTime.UtcNow;

        // Assert
        entity.CreatedAt.Should().BeOnOrAfter(before);
        entity.CreatedAt.Should().BeOnOrBefore(after);
    }
}
```

### Validation Checklist Before Merge

- [ ] `git status` shows clean working tree
- [ ] `git log` shows initial commit
- [ ] `dotnet restore` completes without errors
- [ ] `dotnet build` completes with 0 errors and 0 warnings
- [ ] `dotnet test` discovers and passes all tests
- [ ] `dotnet run --project src/Acode.Cli` executes without errors
- [ ] All 9 projects are in solution
- [ ] Layer dependencies are correct (no illegal references)
- [ ] Central package management is functional
- [ ] All folders have .gitkeep or placeholder files
- [ ] No sensitive data in repository

### Rollout Plan

1. Create repository structure locally
2. Verify all checks pass
3. Push to remote origin
4. Notify team of availability
5. Begin Task 000.b (documentation)

---

**END OF TASK 000.a**
