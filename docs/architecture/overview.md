# Architecture Overview

**Last Updated**: 2025-01-03

This document provides a high-level overview of Acode's architecture.

## System Context

```
┌─────────────┐
│    User     │
└──────┬──────┘
       │
       ▼
┌─────────────────────────────────────────────┐
│          Acode CLI Application              │
│                                             │
│  ┌──────────┐  ┌─────────────┐            │
│  │   CLI    │→ │ Application │→ ┌────────┐│
│  │  Layer   │  │   Layer     │  │ Domain ││
│  └──────────┘  └─────────────┘  └────────┘│
│       │              │                     │
│       ▼              ▼                     │
│  ┌─────────────────────────┐              │
│  │  Infrastructure Layer   │              │
│  └───────────┬─────────────┘              │
│              │                             │
└──────────────┼─────────────────────────────┘
               │
       ┌───────┴────────┐
       │                │
       ▼                ▼
┌─────────────┐  ┌─────────────┐
│Local Model  │  │File System  │
│(Ollama,etc) │  │   / Git     │
└─────────────┘  └─────────────┘
```

## Core Principles

1. **Local-First**: All inference runs locally (no external LLM APIs)
2. **Privacy-First**: Code never leaves your infrastructure
3. **Safe by Default**: Conservative permissions, explicit approvals
4. **Auditable**: Complete audit trail of all operations
5. **Clean Architecture**: Strict layer separation, testable business logic

## Layers

### CLI Layer

**Purpose**: User interaction

**Responsibilities**:
- Parse command-line arguments
- Format output for terminal
- Wire up dependency injection
- Delegate to Application layer

**Technology**: .NET Console App, command-line parser

### Application Layer

**Purpose**: Use cases and orchestration

**Responsibilities**:
- Implement use cases (e.g., "generate code", "run tests")
- Orchestrate domain objects
- Define application service interfaces
- Validate inputs

**Technology**: .NET Class Library

### Domain Layer

**Purpose**: Core business logic

**Responsibilities**:
- Define entities (e.g., CodeFile, OperatingMode)
- Define value objects (e.g., FilePath, ModelConfig)
- Domain services (e.g., SafetyChecker)
- Repository and service interfaces

**Technology**: Pure .NET, no external dependencies

### Infrastructure Layer

**Purpose**: External integrations

**Responsibilities**:
- File system operations
- Process execution
- Git operations
- Model provider adapters (Ollama, vLLM)
- Persistence (SQLite for audit logs)

**Technology**: .NET Class Library with external dependencies

## Key Subsystems

### Model Runtime (Epic 1)

Manages local model inference.

**Components**:
- Model provider abstractions
- Ollama adapter
- vLLM adapter
- llama.cpp adapter
- Response streaming
- Token counting

### Agent Orchestration (Epic 2)

Coordinates agent execution.

**Components**:
- Agent loop
- Tool invocation
- Context management
- Memory/state

### Repository Intelligence (Epic 3)

Understands codebases.

**Components**:
- Code indexer
- Semantic search
- Context builder
- Relevance ranking

### Safety & Policy (Epic 9)

Enforces security constraints.

**Components**:
- Operating mode enforcement
- Protected path checks
- Denylist validation
- Audit logger

## Data Flow

### Example: Generate Code Command

```
1. User: acode generate "add a User class"
         │
         ▼
2. CLI: Parse arguments, validate
         │
         ▼
3. Application: GenerateCodeUseCase
         │
         ├→ Get project context
         ├→ Check safety constraints
         ├→ Build prompt
         │
         ▼
4. Infrastructure: ModelProviderService
         │
         ├→ Call local model API
         ├→ Stream response
         │
         ▼
5. Application: Process model response
         │
         ├→ Extract file changes
         ├→ Validate changes
         │
         ▼
6. Infrastructure: FileSystemService
         │
         ├→ Write files
         ├→ Log to audit log
         │
         ▼
7. CLI: Display results to user
```

## Operating Modes

Three modes control network access:

- **LocalOnly** (default): No network except localhost
- **Burst**: Network allowed, but no external LLM APIs
- **Airgapped**: Complete network isolation

Mode enforcement happens at:
- Configuration loading (Application layer)
- Pre-execution validation (Application layer)
- Network call interception (Infrastructure layer)

See [OPERATING_MODES.md](../OPERATING_MODES.md) for details.

## Technology Stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 8.0+ |
| Language | C# 12 |
| Testing | xUnit, FluentAssertions, NSubstitute |
| CLI Parsing | TBD (Epic 2) |
| Local Models | Ollama, vLLM, llama.cpp |
| Persistence | SQLite |
| Sandboxing | Docker (Epic 4) |
| Git | LibGit2Sharp (Epic 5) |

## Deployment Model

Acode is distributed as a **single binary** (self-contained deployment):

```bash
# Install
dotnet tool install -g acode

# Run
acode generate "implement feature X"
```

Users must provide:
- Local model provider (Ollama, etc.)
- Model downloaded locally

## Future Enhancements

### Planned (see Epic structure)

- Cloud burst compute (Epic 7)
- Parallel task execution (Epic 6)
- CI/CD integration (Epic 8)
- Performance optimizations (Epic 11)
- Evaluation suite (Epic 12)

### Under Consideration

- Plugin system
- Language-specific adapters
- IDE integrations
- Web UI

## Further Reading

- [REPO_STRUCTURE.md](../REPO_STRUCTURE.md) - Detailed layer descriptions
- [ADR 001: Clean Architecture](../adr/001-clean-architecture.md) - Architecture decision rationale
- [Task List](../tasks/task-list.md) - Implementation roadmap
