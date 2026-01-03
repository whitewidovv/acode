# Acode Configuration Examples

This directory contains example `.agent/config.yml` files for various project types.

## Quick Start

Copy the example for your project type to `.agent/config.yml` in your repository root:

```bash
# For .NET projects
cp docs/config-examples/dotnet.yml .agent/config.yml

# For Node.js projects
cp docs/config-examples/node.yml .agent/config.yml

# For Python projects
cp docs/config-examples/python.yml .agent/config.yml

# Minimal configuration
cp docs/config-examples/minimal.yml .agent/config.yml
```

## Available Examples

| File | Description | Use Case |
|------|-------------|----------|
| `minimal.yml` | Absolute minimum configuration | Quick start, testing |
| `full.yml` | All available options with comments | Reference, advanced setup |
| `dotnet.yml` | .NET 8+ project | C#/F# web apps, APIs, libraries |
| `node.yml` | Node.js/TypeScript project | JavaScript/TypeScript apps |
| `python.yml` | Python project | Python web services, scripts |
| `go.yml` | Go project | Go microservices, CLIs |
| `rust.yml` | Rust project | Rust applications |
| `java.yml` | Java/Maven project | Spring Boot, Java apps |
| `invalid.yml` | Common errors | Learning validation |

## Schema Reference

All examples validate against the JSON Schema at `data/config-schema.json`.

### Schema Features

- **IDE Integration**: Auto-completion and inline validation in VS Code, JetBrains IDEs
- **Validation**: Catch configuration errors before runtime
- **Documentation**: Inline descriptions for all properties

### Using the Schema in VS Code

Add this line to the top of your `.agent/config.yml`:

```yaml
# yaml-language-server: $schema=../data/config-schema.json
schema_version: "1.0.0"
```

Or configure globally in VS Code settings:

```json
{
  "yaml.schemas": {
    "./data/config-schema.json": ".agent/config.yml"
  }
}
```

## Configuration Sections

### Project

Basic project metadata:

```yaml
project:
  name: my-app  # Required: lowercase, alphanumeric, hyphens, underscores
  type: dotnet  # Optional: dotnet|node|python|go|rust|java|other
  languages: [csharp]  # Optional: programming languages
  description: My application  # Optional: human-readable description
```

### Mode

Operating mode configuration (see CONSTRAINTS.md for details):

```yaml
mode:
  default: local-only  # Default mode (local-only|airgapped, NOT burst)
  allow_burst: true  # Allow switching to burst mode
  airgapped_lock: false  # Lock to airgapped mode permanently
```

### Model

LLM model configuration:

```yaml
model:
  provider: ollama  # LLM provider
  name: codellama:7b  # Model identifier
  endpoint: http://localhost:11434  # Must be localhost in LocalOnly mode
  parameters:
    temperature: 0.7  # 0.0 (deterministic) to 2.0 (creative)
    max_tokens: 4096  # Maximum response tokens
    top_p: 0.95  # Nucleus sampling (0.0 to 1.0)
  timeout_seconds: 120  # Request timeout
  retry_count: 3  # Retry attempts on failure
```

### Commands

Six standard command groups:

```yaml
commands:
  setup: npm install  # Initialize development environment
  build: npm run build  # Compile/bundle project
  test: npm test  # Run test suite
  lint: npm run lint  # Check code quality (read-only)
  format: npm run format  # Auto-format code (modifies files)
  start: npm start  # Run the application
```

See `full.yml` for advanced command options (cwd, env, timeout, retry, platforms).

### Paths

Directory classifications for context management:

```yaml
paths:
  source: [src/]  # Source code directories
  tests: [tests/]  # Test directories
  output: [dist/, build/]  # Build output directories
  docs: [docs/]  # Documentation directories
```

### Ignore

Files and patterns to exclude from context:

```yaml
ignore:
  patterns:
    - "**/node_modules/**"
    - "**/.git/**"
    - "**/dist/**"
  additional:
    - "**/temp/**"  # Additional patterns beyond defaults
```

### Network (Burst Mode Only)

Network allowlist for external hosts:

```yaml
network:
  allowlist:
    - host: api.example.com
      ports: [443]
      reason: Company API server
```

### Storage

Storage and sync configuration:

```yaml
storage:
  mode: local_cache_only  # local_cache_only|offline_first_sync|remote_required
  local:
    type: sqlite
    sqlite_path: .acode/workspace.db
  # See full.yml for remote and sync configuration
```

## Validation

Validate your configuration:

```bash
# Validate syntax and schema
acode config validate

# Show parsed configuration
acode config show

# Show configuration as JSON
acode config show --json
```

## Common Patterns

### Environment Variables

Use environment variable interpolation:

```yaml
model:
  endpoint: ${OLLAMA_HOST:-http://localhost:11434}
  name: ${ACODE_MODEL:-codellama:7b}

storage:
  remote:
    postgres:
      dsn: ${ACODE_POSTGRES_DSN:?PostgreSQL DSN required}
```

Syntax:
- `${VAR}` - Required variable (error if undefined)
- `${VAR:-default}` - Optional with default value
- `${VAR:?error message}` - Required with custom error

### Multi-Step Commands

Use array format for command sequences:

```yaml
commands:
  setup:
    - npm install
    - npm run postinstall
    - npm run generate-types
```

### Platform-Specific Commands

Use platform variants:

```yaml
commands:
  build:
    run: make build
    platforms:
      windows: msbuild /p:Configuration=Release
      linux: make build
      macos: make build
```

### Complex Commands

Use object format for full control:

```yaml
commands:
  test:
    run: pytest
    cwd: backend
    env:
      PYTHONPATH: src
      TEST_ENV: local
    timeout: 600  # 10 minutes
    retry: 2
```

## Troubleshooting

### "Unknown field" warnings

Check for typos in property names. Unknown fields generate warnings but don't fail validation.

### "Pattern violation" errors

- **Project name**: Must be lowercase, alphanumeric, with hyphens/underscores
- **Schema version**: Must be semantic version string (e.g., "1.0.0")
- **Glob patterns**: Must be valid glob syntax

### "Enum violation" errors

Check allowed values:
- `mode.default`: `local-only` or `airgapped` (NOT `burst`)
- `project.type`: `dotnet`, `node`, `python`, `go`, `rust`, `java`, `other`
- `storage.mode`: `local_cache_only`, `offline_first_sync`, `remote_required`

### Path traversal errors

Paths must be within repository. Cannot use `..` or absolute paths.

## Further Reading

- JSON Schema: `data/config-schema.json`
- Constraints Reference: `CONSTRAINTS.md`
- Task Specifications: `docs/tasks/refined-tasks/Epic 00/task-002*.md`
