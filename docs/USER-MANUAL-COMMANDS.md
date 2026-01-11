# Acode Command Groups User Manual

## Overview

Acode uses six standard command groups to interact with your project. This manual covers command group configuration, execution, troubleshooting, and best practices.

## Command Groups Overview

Acode uses six standard command groups to interact with your project:

| Group | Purpose | Typical Commands |
|-------|---------|------------------|
| **setup** | Initialize development environment | `npm install`, `dotnet restore` |
| **build** | Compile or bundle project | `npm run build`, `dotnet build` |
| **test** | Run test suite | `npm test`, `dotnet test` |
| **lint** | Check code quality | `npm run lint`, `dotnet format --verify-no-changes` |
| **format** | Auto-format code | `npm run format`, `dotnet format` |
| **start** | Run the application | `npm start`, `dotnet run` |

## Configuration Syntax

### Simple String Format

The simplest way to define commands is using a string for each group:

```yaml
commands:
  setup: npm install
  build: npm run build
  test: npm test
  lint: npm run lint
  format: npm run format
  start: npm start
```

### Array Format (Multiple Commands)

When you need to run multiple commands in sequence, use an array:

```yaml
commands:
  setup:
    - npm install
    - npm run postinstall
  build:
    - npm run clean
    - npm run build
```

Commands in an array execute sequentially. If any command fails, subsequent commands are skipped (unless `continue_on_error` is set).

### Object Format (Full Options)

For advanced configuration with timeouts, retries, and environment variables, use object format:

```yaml
commands:
  build:
    run: npm run build
    cwd: src/frontend
    env:
      NODE_ENV: production
    timeout: 600
    retry: 2
```

### Mixed Format

You can mix string and object formats within an array:

```yaml
commands:
  setup:
    - npm install                          # string
    - run: npm run generate                # object
      timeout: 120
    - npm run postinstall                  # string
```

## Command Object Properties

When using object format, the following properties are available:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `run` | string | (required) | The shell command to execute |
| `cwd` | string | `.` | Working directory (relative to repo root) |
| `env` | object | `{}` | Additional environment variables |
| `timeout` | integer | 300 | Timeout in seconds (0 = no timeout) |
| `retry` | integer | 0 | Number of retry attempts |
| `continue_on_error` | boolean | false | Continue sequence on failure |
| `platforms` | object | null | Platform-specific variants |

### Property Details

#### run (required)

The shell command to execute. Commands are executed via the system shell (`/bin/sh` on Unix, `cmd.exe` on Windows).

```yaml
commands:
  build:
    run: npm run build
```

#### cwd (optional)

Working directory for command execution, relative to repository root. Absolute paths and path traversal (`..`) are rejected for security.

```yaml
commands:
  build:
    run: npm run build
    cwd: src/frontend  # Executes in <repo>/src/frontend
```

#### env (optional)

Additional environment variables to set for the command. Variable names must be alphanumeric with underscores.

```yaml
commands:
  build:
    run: npm run build
    env:
      NODE_ENV: production
      BUILD_TARGET: release
```

#### timeout (optional)

Timeout in seconds. Default is 300 seconds (5 minutes). Set to `0` for no timeout.

```yaml
commands:
  build:
    run: npm run build
    timeout: 600  # 10 minutes

  watch:
    run: npm run watch
    timeout: 0  # No timeout (runs indefinitely)
```

When a timeout occurs, the process is terminated and exit code 124 is returned.

#### retry (optional)

Number of retry attempts on failure. Default is 0 (no retries). Maximum is 10.

```yaml
commands:
  setup:
    run: npm install
    retry: 3  # Retry up to 3 times on failure
```

Retries use exponential backoff: 1s, 2s, 4s, 8s, 16s, ... (capped at 30s).

#### continue_on_error (optional)

When `true`, continue executing subsequent commands in an array even if this command fails. Default is `false`.

```yaml
commands:
  setup:
    - run: npm run optional-setup
      continue_on_error: true
    - npm install  # Runs even if optional-setup fails
```

#### platforms (optional)

Platform-specific command variants. See [Platform-Specific Commands](#platform-specific-commands) below.

## Platform-Specific Commands

Use the `platforms` property to define different commands for different operating systems:

```yaml
commands:
  build:
    run: make build
    platforms:
      windows: msbuild /p:Configuration=Release
      linux: make build
      macos: make build
```

Supported platform identifiers:
- `windows`: Windows (any version)
- `linux`: Linux (any distribution)
- `macos`: macOS

When a platform-specific variant exists for the current platform, it overrides the `run` property.

### Example: Cross-Platform Setup

```yaml
commands:
  setup:
    run: ./setup.sh
    platforms:
      windows: setup.bat
      linux: ./setup.sh
      macos: ./setup.sh
```

## Environment Variables

### Acode-Provided Variables

Acode automatically sets the following environment variables for all commands:

| Variable | Description |
|----------|-------------|
| `ACODE_MODE` | Current operating mode (local-only, burst, airgapped) |
| `ACODE_ROOT` | Absolute path to repository root |
| `ACODE_COMMAND` | Current command group (setup, build, etc.) |
| `ACODE_ATTEMPT` | Current retry attempt (1-based) |

### Custom Variables

Define custom environment variables using the `env` property:

```yaml
commands:
  build:
    run: npm run build
    env:
      NODE_ENV: production
      BUILD_NUMBER: ${BUILD_NUMBER:-0}
```

Variables can reference environment variables from the shell using `${VAR}` syntax. Use `${VAR:-default}` to provide a default value.

### Example: Build with Version

```yaml
commands:
  build:
    run: npm run build
    env:
      NODE_ENV: production
      VERSION: ${GIT_TAG:-dev}
      BUILD_TIME: ${BUILD_TIME:-unknown}
```

## Timeouts and Retries

### Timeout Configuration

Timeouts prevent hung processes from blocking workflows. The default timeout is 300 seconds (5 minutes).

```yaml
commands:
  build:
    run: npm run build
    timeout: 600  # 10 minutes

  test:
    run: npm test
    timeout: 0  # No timeout (use for long test suites)
```

When a timeout occurs:
1. The process is terminated (SIGTERM on Unix, terminate on Windows)
2. Exit code 124 is returned
3. Acode logs a timeout event

### Retry Configuration

Retries are useful for transient failures (network issues, flaky tests, etc.):

```yaml
commands:
  setup:
    run: npm install
    retry: 3  # Retry up to 3 times on failure
```

Retry uses exponential backoff: 1s, 2s, 4s, 8s, ... (max 30s).

**Retry attempt sequence:**
- Attempt 1: Runs immediately
- Attempt 2: Wait 1 second
- Attempt 3: Wait 2 seconds
- Attempt 4: Wait 4 seconds
- Attempt 5: Wait 8 seconds
- Attempt 6+: Wait 16 seconds, 30 seconds (capped)

**Best practices:**
- Use retry for network operations (`npm install`, `git clone`, etc.)
- Avoid retry for build failures (fix the build instead)
- Keep retry count low (2-3 attempts)
- Monitor retry logs to identify flaky operations

## Exit Codes

Commands return standard Unix exit codes:

| Code | Meaning | Acode Behavior |
|------|---------|----------------|
| 0 | Success | Continue, report success |
| 1 | General error | Stop, report failure |
| 2 | Misuse | Stop, check command syntax |
| 124 | Timeout | Process killed, report timeout |
| 126 | Not executable | Stop, check file permissions |
| 127 | Not found | Stop, check command exists |
| 130 | Interrupted | User cancelled (Ctrl+C) |

Exit codes are logged with every command execution and can be queried via `acode logs`.

## CLI Usage

### Run a Command Group

```bash
# Run a single command group
acode run setup
acode run build
acode run test
acode run lint
acode run format
acode run start
```

### Run with Options

```bash
# Override timeout
acode run build --timeout 600

# Override retry count
acode run test --retry 2

# Run in background
acode run start --background
```

### Run Multiple Groups

Execute multiple command groups in sequence:

```bash
# Run setup, build, and test in order
acode run setup build test

# Common workflow: install, build, lint, test
acode run setup build lint test
```

If any command group fails, subsequent groups are skipped (unless `continue_on_error` is set).

### Check Command Definition

View the effective configuration for a command group:

```bash
# Show build command configuration
acode config show commands.build

# Show all command groups
acode config show commands
```

## Best Practices

### 1. Keep Commands Idempotent

Running a command multiple times should be safe:

✅ **Good:**
```yaml
commands:
  setup: npm ci  # Removes node_modules first, then installs
```

❌ **Bad:**
```yaml
commands:
  setup: npm install && npm run postinstall  # May fail if already installed
```

### 2. Use Explicit Commands

Avoid complex shell scripting in configuration. Use scripts for complex logic:

✅ **Good:**
```yaml
commands:
  build:
    run: ./scripts/build.sh
    timeout: 600
```

❌ **Bad:**
```yaml
commands:
  build: |
    if [ -d dist ]; then rm -rf dist; fi
    mkdir -p dist
    npm run compile
    npm run bundle
    cp -r assets dist/
```

### 3. Set Appropriate Timeouts

Don't let hung processes block workflows:

```yaml
commands:
  test:
    run: npm test
    timeout: 300  # Reasonable for most test suites

  build:
    run: npm run build
    timeout: 600  # Longer for complex builds
```

### 4. Use Retry Sparingly

Only use retry for transient failures:

✅ **Good:**
```yaml
commands:
  setup:
    run: npm install  # Network issues are transient
    retry: 2
```

❌ **Bad:**
```yaml
commands:
  test:
    run: npm test  # Test failures should be fixed, not retried
    retry: 5
```

### 5. Test on All Platforms

Use platform variants for compatibility:

```yaml
commands:
  build:
    run: make build
    platforms:
      windows: msbuild /p:Configuration=Release
      linux: make build
      macos: make build
```

Test your configuration on all target platforms before committing.

### 6. Log Build Artifacts

Ensure build outputs are in known locations:

```yaml
commands:
  build:
    run: npm run build
    env:
      BUILD_OUTPUT: ./dist

  test:
    run: npm test
    env:
      TEST_OUTPUT: ./test-results
```

## Troubleshooting

### "Command not found"

**Symptoms:**
```
Error: Command failed with exit code 127
/bin/sh: npm: command not found
```

**Causes:**
- Command is not installed
- Command is not in PATH
- Typo in command name

**Solutions:**

1. Check if command exists:
```bash
# Linux/macOS
which npm

# Windows
where npm
```

2. Check PATH in configuration:
```bash
acode config show env
```

3. Install missing command:
```bash
# Example for npm
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt-get install -y nodejs
```

4. Use absolute path if needed:
```yaml
commands:
  build:
    run: /usr/bin/npm run build
```

### "Permission denied"

**Symptoms:**
```
Error: Command failed with exit code 126
/bin/sh: ./scripts/build.sh: Permission denied
```

**Causes:**
- Script file is not executable
- File permissions are too restrictive

**Solutions:**

1. Check file permissions:
```bash
ls -la ./scripts/build.sh
```

2. Make executable:
```bash
chmod +x ./scripts/build.sh
```

3. Verify ownership:
```bash
ls -la ./scripts/build.sh
# Should show your user as owner
```

### "Timeout exceeded"

**Symptoms:**
```
Error: Command failed with exit code 124
Build process timed out after 300 seconds
```

**Causes:**
- Build takes longer than timeout
- Process is hung/blocked

**Solutions:**

1. Increase timeout:
```yaml
commands:
  build:
    run: npm run build
    timeout: 1200  # 20 minutes
```

2. Check for hung processes:
```bash
# Run command manually to see where it hangs
npm run build
```

3. Optimize build:
```yaml
commands:
  build:
    run: npm run build
    env:
      NODE_OPTIONS: --max-old-space-size=4096  # Increase memory
```

### "Command failed with exit code X"

**Symptoms:**
```
Error: Command failed with exit code 1
npm ERR! Missing script: "build"
```

**Causes:**
- Command syntax error
- Missing dependencies
- Build failures

**Solutions:**

1. Run command manually to see full output:
```bash
npm run build
```

2. Check Acode logs for captured output:
```bash
acode logs show --last
```

3. Verify command definition:
```bash
acode config show commands.build
```

4. Check for typos in command name:
```yaml
commands:
  build: npm run build  # Correct
  # Not: npm run biuld (typo)
```

### "Working directory does not exist"

**Symptoms:**
```
Error: Working directory does not exist: src/frontend
```

**Causes:**
- Directory path is incorrect
- Directory has not been created yet
- Typo in path

**Solutions:**

1. Verify directory exists:
```bash
ls -la src/frontend
```

2. Check path is relative to repository root:
```yaml
commands:
  build:
    run: npm run build
    cwd: src/frontend  # Relative to repo root, not current directory
```

3. Create directory if needed:
```yaml
commands:
  setup:
    - mkdir -p src/frontend
    - run: npm install
      cwd: src/frontend
```

### "Invalid environment variable name"

**Symptoms:**
```
Error: Invalid environment variable name: MY-VAR
Variable names must be alphanumeric with underscores
```

**Causes:**
- Environment variable name contains invalid characters
- Hyphens, spaces, or special characters in name

**Solutions:**

Use only alphanumeric characters and underscores:

❌ **Bad:**
```yaml
commands:
  build:
    run: npm run build
    env:
      MY-VAR: value       # Hyphen not allowed
      MY VAR: value       # Space not allowed
      MY.VAR: value       # Dot not allowed
```

✅ **Good:**
```yaml
commands:
  build:
    run: npm run build
    env:
      MY_VAR: value       # Underscore is fine
      MYVAR: value        # All caps is fine
      my_var: value       # Lowercase is fine
```

## FAQ

**Q: Can I define custom command groups?**

A: Not currently. The six standard groups (setup, build, test, lint, format, start) cover most workflows. Custom commands can be run directly via shell or wrapped in one of the standard groups.

**Q: How do I run commands in parallel?**

A: Parallel execution is not yet supported in configuration. Use shell parallelization as a workaround:

```yaml
commands:
  test:
    run: npm run test:unit & npm run test:integration & wait
```

**Q: Can I use shell features like pipes?**

A: Yes, commands are executed via shell, so pipes, redirects, and other shell features work:

```yaml
commands:
  build:
    run: npm run build | tee build.log

  test:
    run: npm test 2>&1 | tee test.log
```

**Q: How do I pass arguments to commands?**

A: Use `acode run` with `--` to pass additional arguments:

```bash
acode run test -- --filter "MyTest"
acode run build -- --verbose
```

Arguments after `--` are appended to the configured command.

**Q: What shell is used?**

A: On Unix systems, `/bin/sh` is used. On Windows, `cmd.exe` is used (or `pwsh.exe` if configured).

You can verify the shell by running:

```yaml
commands:
  debug:
    run: echo $SHELL  # Unix
    platforms:
      windows: echo %COMSPEC%  # Windows
```

**Q: How do I debug command execution?**

A: Use verbose logging:

```bash
# Run with verbose output
acode run build --verbose

# View execution logs
acode logs show --last --filter commands

# Show effective configuration
acode config show commands.build
```

**Q: Can I disable a command group temporarily?**

A: Yes, comment it out or remove it from configuration:

```yaml
commands:
  setup: npm install
  build: npm run build
  test: npm test
  # lint: npm run lint  # Disabled temporarily
```

Or set it to a no-op:

```yaml
commands:
  lint: echo "Linting disabled"
```

## Examples

### Minimal Configuration

```yaml
schema_version: "1.0.0"

project:
  name: my-project

commands:
  setup: npm install
  build: npm run build
  test: npm test
```

### Full-Featured Configuration

```yaml
schema_version: "1.0.0"

project:
  name: enterprise-app
  type: nodejs
  languages:
    - typescript

commands:
  setup:
    - npm ci
    - run: npm run generate
      cwd: codegen
      timeout: 120

  build:
    run: npm run build
    cwd: src
    env:
      NODE_ENV: production
      BUILD_TARGET: release
    timeout: 600
    retry: 2

  test:
    - run: npm run test:unit
      timeout: 300
    - run: npm run test:integration
      timeout: 600
      retry: 1

  lint:
    run: npm run lint
    platforms:
      windows: npm run lint:windows
      linux: npm run lint

  format:
    run: npm run format
    continue_on_error: true

  start:
    run: npm start
    timeout: 0
```

### Cross-Platform Configuration

```yaml
schema_version: "1.0.0"

project:
  name: cross-platform-app

commands:
  setup:
    run: ./scripts/setup.sh
    platforms:
      windows: scripts\setup.bat
      linux: ./scripts/setup.sh
      macos: ./scripts/setup.sh

  build:
    run: make build
    platforms:
      windows: msbuild /p:Configuration=Release
      linux: make build
      macos: make build

  test:
    run: ./scripts/test.sh
    platforms:
      windows: scripts\test.bat
      linux: ./scripts/test.sh
      macos: ./scripts/test.sh
```

## Support

For issues or questions:
- File a bug: https://github.com/whitewidovv/acode/issues
- Check documentation: `docs/` directory
- Run: `acode --help`
