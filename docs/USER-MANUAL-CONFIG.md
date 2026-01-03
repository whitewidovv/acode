# Acode Configuration User Manual

## Overview

Acode uses `.agent/config.yml` to configure its behavior, operating modes, model selection, and project-specific settings. This manual covers configuration file creation, validation, and troubleshooting.

## Quick Start

### 1. Create Configuration File

Create `.agent/config.yml` in your project root:

```yaml
schema_version: "1.0.0"

project:
  name: my-project
  type: dotnet
  languages:
    - csharp

mode:
  default: local-only

model:
  provider: ollama
  name: codellama:7b
  endpoint: http://localhost:11434
```

### 2. Validate Configuration

```bash
acode config validate
```

Expected output:
```
Validating configuration...

  ✓ Schema version: 1.0.0
  ✓ Project: my-project (dotnet)
  ✓ Mode: local-only
  ✓ Model: ollama/codellama:7b

  ✓ Configuration valid
```

### 3. View Configuration

```bash
# View as YAML (default)
acode config show

# View as JSON
acode config show --format json
```

## Configuration Reference

### Required Fields

#### schema_version
- **Type**: string
- **Required**: Yes
- **Supported values**: `"1.0.0"`
- **Description**: Configuration schema version

### Optional Sections

#### project
Project metadata and language configuration.

```yaml
project:
  name: my-project           # Project name (required)
  type: dotnet               # Project type (optional)
  languages:                 # Programming languages (optional)
    - csharp
    - typescript
```

#### mode
Operating mode configuration.

```yaml
mode:
  default: local-only        # Default: local-only
  allowed:                   # Optional: restrict allowed modes
    - local-only
    - airgapped
```

**Supported modes:**
- `local-only`: No network access, local models only
- `airgapped`: Complete network isolation
- `burst`: Cloud compute allowed (coming soon)

#### model
LLM model configuration.

```yaml
model:
  provider: ollama           # Model provider (required)
  name: codellama:7b         # Model name (required)
  endpoint: http://localhost:11434  # Optional endpoint override
  timeout_seconds: 120       # Optional timeout (default: 120)
  retry_count: 3             # Optional retry count (default: 3)
  parameters:                # Optional model parameters
    temperature: 0.7
    top_p: 0.9
```

**Supported providers:**
- `ollama`: Local Ollama models
- `vllm`: vLLM inference server (coming soon)
- `custom`: Custom inference endpoint (coming soon)

#### paths
Directory path configuration.

```yaml
paths:
  workspace: .acode          # Working directory for Acode
  output: ./output           # Output directory for artifacts
```

#### security
Security and sandbox configuration.

```yaml
security:
  sandbox:
    enabled: true            # Enable sandboxing (default: true)
    type: docker             # Sandbox type: docker (coming soon)
  deny_paths:                # Paths to deny access to
    - .git
    - .env
    - secrets
```

## CLI Commands

### acode config validate

Validates the configuration file and displays results.

```bash
acode config validate
```

**Exit codes:**
- `0`: Configuration is valid
- `1`: Configuration is invalid or file not found

**Output example:**
```
Validating configuration...

  ✓ Schema version: 1.0.0
  ✓ Project: my-project (dotnet)
  ✓ Mode: local-only
  ✓ Model: ollama/codellama:7b

  ✓ Configuration valid
```

### acode config show

Displays the effective configuration.

```bash
# Show as YAML (default)
acode config show

# Show as JSON
acode config show --format json
```

**Output formats:**
- `yaml`: YAML format (default)
- `json`: JSON format with snake_case keys

## Troubleshooting

### Error: Configuration file not found

**Symptoms:**
```
Error: Configuration file not found: .agent/config.yml
```

**Solution:**
1. Check that `.agent/config.yml` exists in your project root
2. Verify you're running commands from the project root directory
3. Create a minimal config file if missing

### Error: Invalid YAML syntax

**Symptoms:**
```
YAML parsing error at line 5, column 3: invalid mapping

Suggestion: Check indentation levels. YAML requires consistent indentation (use 2 spaces per level).
```

**Solution:**
1. Use 2 spaces for indentation (not tabs)
2. Check for unclosed quotes
3. Verify colons are followed by spaces
4. Use a YAML validator to check syntax

### Error: Unsupported schema version

**Symptoms:**
```
[ERROR] schema_version: Schema version '2.0.0' is not supported. Supported: 1.0.0
```

**Solution:**
Change `schema_version` to a supported version:
```yaml
schema_version: "1.0.0"
```

### Warning: Unknown field

**Symptoms:**
```
[WARNING] experimental_feature: Unknown field 'experimental_feature'
```

**Solution:**
Remove the unknown field or check the documentation for correct field names. Unknown fields generate warnings but don't fail validation.

### Error: Invalid mode

**Symptoms:**
```
[ERROR] mode.default: Invalid operating mode 'burst'. Supported: local-only, airgapped
```

**Solution:**
Use a supported operating mode:
```yaml
mode:
  default: local-only
```

### Error: Configuration file exceeds maximum size

**Symptoms:**
```
Error: Configuration file exceeds maximum size of 1MB (actual: 1048577 bytes)
```

**Solution:**
- Config files are limited to 1MB for security
- Reduce file size by removing unnecessary data
- Use external files for large datasets instead of embedding in config

### Error: YAML nesting depth exceeds maximum

**Symptoms:**
```
Error: YAML nesting depth exceeds maximum of 20 levels (found: 25)
```

**Solution:**
- Simplify nested structures
- Maximum nesting depth is 20 levels
- Flatten deeply nested configurations

## Security Considerations

### Do Not Store Secrets

**Never** store secrets directly in `.agent/config.yml`:

❌ **Bad:**
```yaml
model:
  api_key: sk-1234567890abcdef  # NEVER DO THIS
```

✅ **Good:**
```yaml
model:
  api_key_env: OLLAMA_API_KEY   # Reference environment variable
```

### File Permissions

Ensure `.agent/config.yml` has appropriate permissions:

```bash
# Linux/macOS
chmod 600 .agent/config.yml

# Verify
ls -la .agent/config.yml
```

### Sensitive Path Denial

Configure `security.deny_paths` to prevent access to sensitive directories:

```yaml
security:
  deny_paths:
    - .git
    - .env
    - secrets
    - ~/.ssh
    - ~/.aws
```

## Best Practices

### 1. Version Control

✅ **Do:** Commit `.agent/config.yml` to version control
❌ **Don't:** Commit files with secrets or API keys

Use `.agent/config.local.yml` (gitignored) for local overrides containing sensitive data.

### 2. Documentation

Document project-specific configuration in your README:

```markdown
## Acode Setup

1. Install Ollama: https://ollama.ai
2. Pull model: `ollama pull codellama:7b`
3. Validate config: `acode config validate`
```

### 3. Team Standards

Establish team standards for configuration:
- Required fields for all projects
- Approved models and providers
- Security baseline configuration

### 4. Configuration Testing

Test configuration changes:

```bash
# Validate after editing
acode config validate

# Verify effective configuration
acode config show

# Test with your team's CI
acode config validate || exit 1
```

## Examples

### Minimal Configuration

```yaml
schema_version: "1.0.0"
project:
  name: my-project
```

### Full Configuration

```yaml
schema_version: "1.0.0"

project:
  name: enterprise-app
  type: dotnet
  languages:
    - csharp
    - typescript

mode:
  default: local-only
  allowed:
    - local-only
    - airgapped

model:
  provider: ollama
  name: codellama:13b
  endpoint: http://localhost:11434
  timeout_seconds: 180
  retry_count: 3
  parameters:
    temperature: 0.7
    top_p: 0.9
    max_tokens: 4096

paths:
  workspace: .acode
  output: ./acode-output

security:
  sandbox:
    enabled: true
  deny_paths:
    - .git
    - .env
    - secrets
    - node_modules/.cache
```

## Support

For issues or questions:
- File a bug: https://github.com/whitewidovv/acode/issues
- Check documentation: `docs/` directory
- Run: `acode --help`
