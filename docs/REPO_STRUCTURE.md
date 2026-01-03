# Repository Structure

**Last Updated**: 2025-01-03

This document describes the canonical folder layout and architecture for the Acode project.

## Table of Contents

- [Overview](#overview)
- [Clean Architecture Layers](#clean-architecture-layers)
- [Directory Tree](#directory-tree)
- [Project Descriptions](#project-descriptions)
- [Naming Conventions](#naming-conventions)
- [Where to Add New Code](#where-to-add-new-code)

## Overview

Acode follows **Clean Architecture** principles with strict layer separation. The solution is organized into:

- **src/** - Production code organized by architectural layer
- **tests/** - Test projects matching production structure
- **docs/** - Documentation, architecture decisions, and task specifications

## Clean Architecture Layers

```
┌────────────────────────────────────────────────────────┐
│                      CLI Layer                         │
│                   (Acode.Cli)                          │
│          Entry point, commands, output                 │
└───────────────────────┬────────────────────────────────┘
                        │
                        ▼
┌────────────────────────────────────────────────────────┐
│                 Application Layer                      │
│                (Acode.Application)                     │
│          Use cases, DTOs, orchestration                │
└───────────────────────┬────────────────────────────────┘
                        │
                        ▼
┌────────────────────────────────────────────────────────┐
│                   Domain Layer                         │
│                  (Acode.Domain)                        │
│     Entities, value objects, business logic            │
│               **No dependencies**                      │
└────────────────────────▲───────────────────────────────┘
                         │
                         │ (implements interfaces from)
                         │
┌────────────────────────────────────────────────────────┐
│              Infrastructure Layer                      │
│              (Acode.Infrastructure)                    │
│    External services, file I/O, persistence            │
└────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

#### Domain Layer (`src/Acode.Domain/`)
- **Purpose**: Core business logic and domain model
- **Contains**: Entities, value objects, domain services, domain interfaces
- **Dependencies**: **None** - The Domain has no external dependencies
- **Rules**:
  - No references to other Acode projects
  - No references to infrastructure concerns (file I/O, HTTP, databases)
  - Only pure .NET and approved abstractions (e.g., `Microsoft.Extensions.Logging.Abstractions`)

#### Application Layer (`src/Acode.Application/`)
- **Purpose**: Use cases and application-specific business logic
- **Contains**: Use cases, DTOs, application service interfaces
- **Dependencies**: Domain layer only
- **Rules**:
  - References only `Acode.Domain`
  - Defines interfaces that Infrastructure implements
  - Orchestrates domain objects to fulfill use cases
  - May use abstractions like `ILogger<T>`

#### Infrastructure Layer (`src/Acode.Infrastructure/`)
- **Purpose**: External integrations and technical implementations
- **Contains**: Repositories, file system access, external API clients, persistence
- **Dependencies**: Domain and Application layers
- **Rules**:
  - Implements interfaces defined in Domain/Application
  - All external I/O happens here
  - Model provider adapters go here
  - File system operations go here

#### CLI Layer (`src/Acode.Cli/`)
- **Purpose**: Command-line interface and entry point
- **Contains**: CLI commands, argument parsing, console output formatting
- **Dependencies**: All other layers
- **Rules**:
  - Contains `Main()` entry point
  - Wires up dependency injection
  - Handles user input/output
  - Thin layer that delegates to Application

## Directory Tree

```
acode/
├── Acode.sln                           # Solution file
├── Directory.Build.props                # Shared MSBuild properties
├── Directory.Packages.props             # Central package version management
├── global.json                          # SDK version lock
├── .gitignore                           # Git ignore patterns
├── .gitattributes                       # Git attributes
├── .editorconfig                        # Code style configuration
├── README.md                            # Project overview
├── LICENSE                              # License file
├── SECURITY.md                          # Security policy
├── CONTRIBUTING.md                      # Contribution guidelines
│
├── src/                                 # Production code
│   ├── Acode.Domain/                    # Domain layer
│   │   ├── Acode.Domain.csproj
│   │   ├── Entities/                    # Domain entities
│   │   ├── ValueObjects/                # Value objects
│   │   ├── Services/                    # Domain services
│   │   └── Interfaces/                  # Repository and service interfaces
│   │
│   ├── Acode.Application/               # Application layer
│   │   ├── Acode.Application.csproj
│   │   ├── UseCases/                    # Use case implementations
│   │   ├── DTOs/                        # Data transfer objects
│   │   └── Interfaces/                  # Application service interfaces
│   │
│   ├── Acode.Infrastructure/            # Infrastructure layer
│   │   ├── Acode.Infrastructure.csproj
│   │   ├── Persistence/                 # Database/file storage implementations
│   │   ├── Services/                    # Infrastructure service implementations
│   │   └── External/                    # External service adapters
│   │
│   └── Acode.Cli/                       # CLI layer
│       ├── Acode.Cli.csproj
│       ├── Program.cs                   # Entry point
│       └── Commands/                    # CLI command implementations
│
├── tests/                               # Test projects
│   ├── Acode.Domain.Tests/              # Domain layer unit tests
│   ├── Acode.Application.Tests/         # Application layer unit tests
│   ├── Acode.Infrastructure.Tests/      # Infrastructure layer unit tests
│   ├── Acode.Cli.Tests/                 # CLI layer unit tests
│   └── Acode.Integration.Tests/         # Integration tests
│
└── docs/                                # Documentation
    ├── REPO_STRUCTURE.md                # This file
    ├── CONFIG.md                        # Configuration reference
    ├── OPERATING_MODES.md               # Operating mode documentation
    ├── architecture/                    # Architecture documentation
    ├── adr/                             # Architecture Decision Records
    ├── tasks/                           # Task specifications
    └── scripts/                         # Documentation generation scripts
```

## Project Descriptions

### Production Projects

| Project | Type | Purpose |
|---------|------|---------|
| `Acode.Domain` | Class Library | Core business entities and domain logic |
| `Acode.Application` | Class Library | Use cases and application orchestration |
| `Acode.Infrastructure` | Class Library | External integrations and technical implementations |
| `Acode.Cli` | Console App | Command-line interface and entry point |

### Test Projects

| Project | Type | Purpose |
|---------|------|---------|
| `Acode.Domain.Tests` | xUnit Test | Unit tests for Domain layer |
| `Acode.Application.Tests` | xUnit Test | Unit tests for Application layer |
| `Acode.Infrastructure.Tests` | xUnit Test | Unit tests for Infrastructure layer |
| `Acode.Cli.Tests` | xUnit Test | Unit tests for CLI layer |
| `Acode.Integration.Tests` | xUnit Test | Integration tests across layers |

## Naming Conventions

### Namespaces

Namespaces MUST match the folder structure:

```csharp
// File: src/Acode.Domain/Entities/User.cs
namespace Acode.Domain.Entities;

// File: src/Acode.Application/UseCases/CreateUserUseCase.cs
namespace Acode.Application.UseCases;

// File: tests/Acode.Domain.Tests/Entities/UserTests.cs
namespace Acode.Domain.Tests.Entities;
```

### Files

- **One public class per file** (nested classes are allowed)
- **File name MUST match class name** exactly (PascalCase)
- **Interfaces**: Prefix with `I` (e.g., `IUserRepository.cs`)
- **Tests**: Suffix with `Tests` (e.g., `UserTests.cs`)

### Test Naming

Tests MUST follow the pattern:
```
MethodName_Scenario_ExpectedResult
```

Examples:
- `CreateUser_WithValidData_ReturnsUserId`
- `CreateUser_WithNullEmail_ThrowsArgumentNullException`
- `GetUserById_WhenUserDoesNotExist_ReturnsNull`

## Where to Add New Code

### Adding a New Entity

1. Create file in `src/Acode.Domain/Entities/`
2. Namespace: `Acode.Domain.Entities`
3. Create corresponding test in `tests/Acode.Domain.Tests/Entities/`

Example:
```
src/Acode.Domain/Entities/User.cs
tests/Acode.Domain.Tests/Entities/UserTests.cs
```

### Adding a New Use Case

1. Create file in `src/Acode.Application/UseCases/`
2. Namespace: `Acode.Application.UseCases`
3. Create corresponding test in `tests/Acode.Application.Tests/UseCases/`
4. If needed, create DTO in `src/Acode.Application/DTOs/`

Example:
```
src/Acode.Application/UseCases/CreateUserUseCase.cs
src/Acode.Application/DTOs/CreateUserRequest.cs
tests/Acode.Application.Tests/UseCases/CreateUserUseCaseTests.cs
```

### Adding a Repository Interface

1. Define interface in `src/Acode.Domain/Interfaces/`
2. Implement in `src/Acode.Infrastructure/Persistence/`
3. Test implementation in `tests/Acode.Infrastructure.Tests/Persistence/`

Example:
```
src/Acode.Domain/Interfaces/IUserRepository.cs
src/Acode.Infrastructure/Persistence/UserRepository.cs
tests/Acode.Infrastructure.Tests/Persistence/UserRepositoryTests.cs
```

### Adding a CLI Command

1. Create command class in `src/Acode.Cli/Commands/`
2. Register command in `Program.cs`
3. Create tests in `tests/Acode.Cli.Tests/Commands/`

Example:
```
src/Acode.Cli/Commands/GenerateCommand.cs
tests/Acode.Cli.Tests/Commands/GenerateCommandTests.cs
```

### Adding a New Project

If you need to add an entirely new project:

1. Create project: `dotnet new classlib -n Acode.NewProject -o src/Acode.NewProject`
2. Add to solution: `dotnet sln add src/Acode.NewProject/Acode.NewProject.csproj --solution-folder src`
3. Create matching test project
4. Add project references as appropriate
5. Update this document

## Build Order

Projects build in this dependency order:

1. `Acode.Domain` (no dependencies)
2. `Acode.Application` (depends on Domain)
3. `Acode.Infrastructure` (depends on Domain, Application)
4. `Acode.Cli` (depends on all above)
5. Test projects (depend on their corresponding production projects)

## Important Notes

- **Layer boundaries are enforced at compile time** via project references
- **Breaking layer separation will cause build failures** (this is intentional)
- **All folders should follow PascalCase naming**
- **Empty folders require .gitkeep files** to be tracked by Git
- **Documentation lives in `docs/`**, not scattered in code folders
- **Tests mirror production structure** for easy navigation

---

For questions about where code belongs, refer to the Clean Architecture diagram above or ask in a GitHub Discussion.
