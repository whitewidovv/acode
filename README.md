# Acode - Agentic Coding Bot

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)]()
[![Version](https://img.shields.io/badge/version-0.1.0--alpha-blue)]()
[![.NET](https://img.shields.io/badge/.NET-8.0-purple)]()
[![License](https://img.shields.io/badge/license-MIT-green)]()

> A locally-hosted, privacy-first AI coding assistant that operates entirely within your infrastructure.

## Table of Contents

- [Features](#features)
- [Quick Start](#quick-start)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [First Run](#first-run)
- [Documentation](#documentation)
- [Operating Modes](#operating-modes)
- [Project Status](#project-status)
- [Contributing](#contributing)
- [License](#license)
- [Security](#security)

## Features

- 🔒 **Privacy-First**: All data stays on your machine - no external LLM API calls
- 🤖 **Local Models**: Works with Ollama, vLLM, and other local model providers
- 🛡️ **Safe by Default**: Conservative permissions with explicit approval gates
- 📊 **Fully Auditable**: Complete audit trail of all operations
- 🔧 **Highly Configurable**: Flexible configuration for your workflow
- 🏗️ **Clean Architecture**: Domain-driven design with strict layer separation
- ⚡ **Cross-Platform**: Runs on Windows, macOS, and Linux

## Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later
- [Git](https://git-scm.com/)
- Local model provider (e.g., [Ollama](https://ollama.ai/)) - required for LocalOnly and Airgapped modes

### Installation

```bash
git clone https://github.com/whitewidovv/acode.git
cd acode
dotnet restore
dotnet build
```

### First Run

```bash
dotnet run --project src/Acode.Cli
```

## Documentation

| Document | Description |
|----------|-------------|
| [USER-MANUAL-CONFIG](docs/USER-MANUAL-CONFIG.md) | Configuration guide, CLI commands, troubleshooting |
| [CONFIG](docs/CONFIG.md) | Configuration reference |
| [OPERATING_MODES](docs/OPERATING_MODES.md) | Operating mode definitions and constraints |
| [CONSTRAINTS](CONSTRAINTS.md) | Hard constraints, security guarantees, enforcement mechanisms |
| [Architecture Decisions](docs/adr/) | ADRs documenting key architectural decisions |
| [REPO_STRUCTURE](docs/REPO_STRUCTURE.md) | Project folder layout and architecture |
| [Task Specifications](docs/tasks/refined-tasks/) | Detailed task specifications by epic |
| [CONTRIBUTING](CONTRIBUTING.md) | How to contribute to the project |

## Operating Modes

Acode supports three operating modes for different security and privacy requirements:

| Mode | Network | External LLM APIs | Cloud Compute | Use Case |
|------|---------|-------------------|---------------|----------|
| **LocalOnly** | ❌ Blocked | ❌ Blocked | ❌ Blocked | Maximum privacy, local models only |
| **Burst** | ✅ Allowed | ❌ Blocked | ✅ Allowed | Temporary cloud compute, no external LLMs |
| **Airgapped** | ❌ Blocked | ❌ Blocked | ❌ Blocked | Complete network isolation |

**Default Mode**: LocalOnly

See [OPERATING_MODES.md](docs/OPERATING_MODES.md) for detailed information about each mode, enforcement mechanisms, and how to switch modes.

## Project Status

**Current Version**: 0.1.0-alpha

**Status**: Foundation Phase (Epic 0 - Project Bootstrap)

This project is in active development. The foundational repository structure, documentation, and tooling are being established. Core functionality for model providers, CLI commands, and agent orchestration will be implemented in subsequent epics.

**What Works Now**:
- ✅ Clean Architecture solution structure (.NET 8.0, Clean Architecture)
- ✅ Build and test infrastructure (xUnit, FluentAssertions, StyleCop)
- ✅ Repository contract system (`.agent/config.yml` with JSON Schema validation)
- ✅ Configuration loader with YAML 1.2 support (YamlDotNet)
- ✅ Semantic validation with 50+ validation rules
- ✅ YAML security features (file size, nesting depth, key count limits)
- ✅ Enhanced error messages with line numbers and suggestions
- ✅ CLI commands: `acode config validate`, `acode config show`
- ✅ 216+ passing tests (unit + integration)
- ✅ Code formatting and analysis tools (StyleCop, analyzers)

**Coming Soon** (see [docs/tasks/task-list.md](docs/tasks/task-list.md)):
- Model runtime and inference (Epic 1)
- CLI and agent orchestration (Epic 2)
- Repository intelligence and indexing (Epic 3)
- Execution and sandboxing (Epic 4)
- And much more...

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for:

- Development setup and workflow
- Coding standards and conventions
- PR process and review expectations
- Testing requirements
- Commit message format

## License

This project is licensed under the MIT License - see [LICENSE](LICENSE) for details.

## Security

Security is a top priority for Acode. If you discover a security vulnerability, please follow responsible disclosure:

- **Do NOT** open a public GitHub issue
- Email security concerns to: [security contact - TBD]
- See [SECURITY.md](SECURITY.md) for our security policy

For general security information, see:
- [CONSTRAINTS.md](CONSTRAINTS.md) - Hard constraints and security guarantees
- [Operating Modes](docs/OPERATING_MODES.md) - Mode-based security constraints
- [Security Audit Checklist](docs/security-audit-checklist.md) - Verification procedures for security audits
- [Architecture Decisions](docs/adr/) - ADRs explaining security design choices
- [Threat Model](docs/tasks/refined-tasks/Epic 00/task-003-threat-model.md) - Risk analysis (coming in Task 003)

---

**Built with ❤️ for developers who value privacy and control**
