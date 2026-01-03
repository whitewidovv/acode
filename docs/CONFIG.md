# Configuration Reference

**Last Updated**: 2025-01-03

This document describes all configuration options for Acode and how they are applied.

## Table of Contents

- [Configuration Sources](#configuration-sources)
- [Precedence Order](#precedence-order)
- [Environment Variables](#environment-variables)
- [Configuration File](#configuration-file)
- [CLI Flags](#cli-flags)
- [Examples](#examples)
- [Troubleshooting](#troubleshooting)

## Configuration Sources

Acode can be configured through multiple sources:

1. **CLI Flags** - Command-line arguments (highest precedence)
2. **Environment Variables** - OS-level configuration
3. **Configuration File** - `.agent/config.yml` in your project
4. **Defaults** - Built-in safe defaults (lowest precedence)

## Precedence Order

When the same setting is defined in multiple places, the following order determines which value is used:

```
CLI Flags > Environment Variables > Config File > Defaults
```

**Example**: If `ACODE_MODE=Burst` is set as an environment variable, but you run `acode --mode LocalOnly`, the CLI flag wins and LocalOnly mode is used.

## Environment Variables

### Core Settings

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `ACODE_MODE` | string | `LocalOnly` | Operating mode: `LocalOnly`, `Burst`, or `Airgapped` |
| `ACODE_CONFIG_PATH` | string | `.agent/config.yml` | Path to configuration file |
| `ACODE_LOG_LEVEL` | string | `Information` | Logging level: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical` |
| `ACODE_MODEL_PROVIDER` | string | (none) | Model provider: `ollama`, `vllm`, `llamacpp` |
| `ACODE_MODEL_URL` | string | `http://localhost:11434` | URL for local model endpoint |

### Model Provider Settings

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `ACODE_MODEL_NAME` | string | (none) | Name of the model to use (e.g., `codellama:13b`) |
| `ACODE_MODEL_TEMPERATURE` | float | `0.7` | Sampling temperature (0.0-1.0) |
| `ACODE_MODEL_MAX_TOKENS` | int | `2048` | Maximum tokens in model response |
| `ACODE_MODEL_TIMEOUT_MS` | int | `30000` | Model request timeout in milliseconds |

### Safety Settings

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `ACODE_ENABLE_AUDIT_LOG` | bool | `true` | Enable audit logging |
| `ACODE_AUDIT_LOG_PATH` | string | `.acode/audit.log` | Path to audit log file |
| `ACODE_MAX_FILE_SIZE_BYTES` | int | `102400` | Maximum file size to modify (100KB default) |
| `ACODE_ALLOW_FILE_DELETION` | bool | `false` | Allow the agent to delete files |

### Setting Environment Variables

**Linux/macOS**:
```bash
export ACODE_MODE=LocalOnly
export ACODE_MODEL_PROVIDER=ollama
export ACODE_MODEL_NAME=codellama:13b
```

**Windows (PowerShell)**:
```powershell
$env:ACODE_MODE = "LocalOnly"
$env:ACODE_MODEL_PROVIDER = "ollama"
$env:ACODE_MODEL_NAME = "codellama:13b"
```

**Windows (CMD)**:
```cmd
set ACODE_MODE=LocalOnly
set ACODE_MODEL_PROVIDER=ollama
set ACODE_MODEL_NAME=codellama:13b
```

## Configuration File

The configuration file uses YAML format and should be placed at `.agent/config.yml` in your project root.

### Schema

```yaml
version: "1.0"

# Project metadata
project:
  name: "MyProject"
  type: "dotnet"  # dotnet, node, python, other

# Operating mode
mode: "LocalOnly"  # LocalOnly, Burst, Airgapped

# Model configuration
model:
  provider: "ollama"
  name: "codellama:13b"
  url: "http://localhost:11434"
  temperature: 0.7
  max_tokens: 2048
  timeout_ms: 30000

# Commands for different operations
commands:
  setup:
    - "dotnet restore"
  build:
    - "dotnet build"
  test:
    - "dotnet test"
  lint:
    - "dotnet format --verify-no-changes"
  format:
    - "dotnet format"
  start:
    - "dotnet run --project src/MyProject.Cli"

# Safety configuration
safety:
  protected_paths:
    - ".git/**"
    - ".github/**"
    - "**/secrets/**"
    - "**/*.pem"
    - "**/*.key"
  denylist_patterns:
    - "*.env"
    - "*secret*"
    - "*credential*"
  max_file_size_bytes: 102400
  allow_file_deletion: false

# Audit configuration
audit:
  enabled: true
  log_path: ".acode/audit.log"
  log_level: "Information"
```

### Minimal Configuration

```yaml
version: "1.0"
project:
  name: "MyProject"
  type: "dotnet"
commands:
  build:
    - "dotnet build"
  test:
    - "dotnet test"
```

All other settings will use defaults.

## CLI Flags

CLI flags override all other configuration sources.

### Global Flags

| Flag | Type | Description |
|------|------|-------------|
| `--mode <mode>` | string | Set operating mode |
| `--config <path>` | string | Path to configuration file |
| `--log-level <level>` | string | Set logging level |
| `--verbose` | bool | Enable verbose output |
| `--dry-run` | bool | Preview actions without executing |

### Model Flags

| Flag | Type | Description |
|------|------|-------------|
| `--model-provider <provider>` | string | Model provider |
| `--model-name <name>` | string | Model name |
| `--model-url <url>` | string | Model endpoint URL |
| `--temperature <float>` | float | Sampling temperature |

### Example Usage

```bash
# Use Burst mode with specific model
acode --mode Burst --model-provider ollama --model-name codellama:13b

# Dry run to preview changes
acode generate --dry-run --verbose

# Use custom config file
acode --config ~/my-acode-config.yml
```

## Examples

### Example 1: .NET Project

**.agent/config.yml**:
```yaml
version: "1.0"
project:
  name: "MyDotNetApp"
  type: "dotnet"
mode: "LocalOnly"
model:
  provider: "ollama"
  name: "codellama:13b"
commands:
  setup:
    - "dotnet restore"
  build:
    - "dotnet build"
  test:
    - "dotnet test --no-build"
  format:
    - "dotnet format"
safety:
  protected_paths:
    - ".git/**"
    - "bin/**"
    - "obj/**"
```

### Example 2: Node.js Project

**.agent/config.yml**:
```yaml
version: "1.0"
project:
  name: "MyNodeApp"
  type: "node"
mode: "LocalOnly"
model:
  provider: "ollama"
  name: "codellama:7b"
commands:
  setup:
    - "npm install"
  build:
    - "npm run build"
  test:
    - "npm test"
  lint:
    - "npm run lint"
  format:
    - "npm run format"
safety:
  protected_paths:
    - ".git/**"
    - "node_modules/**"
    - ".env"
```

### Example 3: Python Project

**.agent/config.yml**:
```yaml
version: "1.0"
project:
  name: "MyPythonApp"
  type: "python"
mode: "LocalOnly"
model:
  provider: "vllm"
  name: "deepseek-coder-6.7b"
  url: "http://localhost:8000"
commands:
  setup:
    - "pip install -r requirements.txt"
  test:
    - "pytest"
  lint:
    - "ruff check ."
  format:
    - "black ."
safety:
  protected_paths:
    - ".git/**"
    - "venv/**"
    - ".env"
  max_file_size_bytes: 51200  # 50KB
```

### Example 4: Environment Variable Override

```bash
# Use config file defaults but override mode
export ACODE_MODE=Burst
acode generate

# This will:
# 1. Load .agent/config.yml
# 2. Override mode with Burst (from env var)
# 3. Keep all other settings from config file
```

## Troubleshooting

### Configuration Not Loading

**Problem**: Acode doesn't seem to use your configuration file.

**Solutions**:
1. Verify file exists at `.agent/config.yml` in project root
2. Check YAML syntax with a validator
3. Run with `--verbose` to see config loading process
4. Check file path with `ACODE_CONFIG_PATH` environment variable

### Mode Violations

**Problem**: Getting errors about blocked operations.

**Solutions**:
1. Check current mode: Review your config file and environment variables
2. Understand mode constraints: See [OPERATING_MODES.md](OPERATING_MODES.md)
3. Switch mode if appropriate: Use `--mode` flag or update config

### Invalid Configuration Values

**Problem**: Configuration values are ignored or cause errors.

**Solutions**:
1. Check value types: Ensure numbers are numbers, booleans are true/false
2. Validate YAML: Use a YAML linter
3. Review schema: Ensure your config matches the schema above
4. Check for typos: Configuration keys are case-sensitive

### Protected Path Conflicts

**Problem**: Can't modify a file you need to change.

**Solutions**:
1. Review `safety.protected_paths` in your config
2. Check if file matches a denylist pattern
3. If safe to do so, adjust denylist in config
4. Always document why you're removing protection

## Configuration Validation

To validate your configuration without running Acode:

```bash
# Future feature - not yet implemented
acode config validate

# Current workaround: Try a dry run
acode --dry-run info
```

## Version Compatibility

| Config Version | Acode Version | Notes |
|----------------|---------------|-------|
| 1.0 | 0.1.0+ | Initial schema |

When Acode updates, check this document for schema changes and migration guides.

---

For operating mode-specific configuration, see [OPERATING_MODES.md](OPERATING_MODES.md).
