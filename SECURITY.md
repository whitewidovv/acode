# Security Policy

## Reporting Security Vulnerabilities

Security is a top priority for Acode. If you discover a security vulnerability, please follow responsible disclosure practices.

### DO NOT

- ❌ Open a public GitHub issue
- ❌ Disclose the vulnerability publicly before it's been addressed
- ❌ Exploit the vulnerability beyond what's necessary to demonstrate it

### DO

- ✅ Email security concerns to: [TBD - security contact email]
- ✅ Provide detailed information about the vulnerability
- ✅ Allow reasonable time for the issue to be addressed before public disclosure
- ✅ Work with the maintainers to verify the fix

### What to Include

When reporting a security vulnerability, please include:

1. **Description** - Clear description of the vulnerability
2. **Impact** - What could an attacker accomplish?
3. **Reproduction** - Step-by-step instructions to reproduce
4. **Version** - Which version(s) are affected?
5. **Suggested Fix** - If you have one (optional)

## Security Model

Acode's security model is based on three key principles:

### 1. Local-First, Privacy-First

- **No external LLM APIs** - Your code never leaves your infrastructure via LLM APIs
- **Operating modes** enforce network access controls
- **Audit logging** tracks all operations

See [docs/OPERATING_MODES.md](docs/OPERATING_MODES.md) for details.

### 2. Opt-Out Safety (Not Opt-In)

- **Default deny** for sensitive operations
- **Protected paths** prevent modification of `.git/`, credentials, etc.
- **Explicit user confirmation** required for risky operations

### 3. Defense in Depth

- **Mode enforcement** at multiple layers
- **Audit logging** cannot be disabled
- **Static blocklists** for external LLM endpoints
- **Validation before execution** for all operations

## Scope

### In Scope

- Vulnerabilities in Acode code
- Bypass of operating mode restrictions
- Bypass of safety controls (protected paths, denylists)
- Unauthorized access to files or data
- Code injection or command injection
- Audit log tampering or evasion
- Privilege escalation

### Out of Scope

- Vulnerabilities in third-party dependencies (report to the dependency maintainers, but do let us know)
- Social engineering attacks
- Physical access attacks
- Denial of service via resource exhaustion (unless caused by a specific bug)
- Issues requiring physical access to the machine

## Supported Versions

| Version | Supported |
|---------|-----------|
| 0.1.0-alpha | ✅ Yes (current) |

As Acode matures, this table will be updated with LTS and maintenance policies.

## Security Updates

Security updates will be released as soon as possible after a vulnerability is confirmed. We aim for:

- **Critical vulnerabilities**: Patch within 7 days
- **High severity**: Patch within 30 days
- **Medium/Low severity**: Patch in next regular release

## Known Security Considerations

### By Design

The following are intentional design decisions, not vulnerabilities:

1. **Local model execution is not sandboxed** - Running local models (Ollama, vLLM, etc.) is the user's responsibility. Acode does not sandbox model execution.

2. **File system access** - Acode can read and write files within the configured working directory. This is required for its function.

3. **Command execution** - Acode can execute commands configured in `.agent/config.yml`. Users must ensure these commands are safe.

### Mitigations

- Use **LocalOnly or Airgapped modes** for maximum isolation
- Review `.agent/config.yml` before running Acode in a new project
- Check **audit logs** regularly
- Use **protected_paths** to safeguard critical files

## Threat Model

For detailed threat analysis, see:
- [docs/tasks/refined-tasks/Epic 00/task-003-threat-model.md](docs/tasks/refined-tasks/Epic 00/task-003-threat-model.md) (will be implemented in Task 003)

## Audit Logging

All security-relevant operations are logged. Audit logs include:

- Operating mode changes
- Attempted mode violations
- File modifications
- Command executions
- Model interactions

Audit log format and schema will be defined in Epic 9.

## Disclosure Timeline

Our target disclosure timeline:

1. **Day 0**: Vulnerability reported
2. **Day 1-3**: Acknowledgment and initial assessment
3. **Day 3-7**: Investigation and fix development
4. **Day 7-14**: Testing and verification
5. **Day 14-30**: Release and coordinated disclosure

This timeline may vary based on complexity and severity.

## Attribution

We believe in recognizing security researchers. With your permission, we will:

- Credit you in release notes
- Add you to our security acknowledgments page
- Thank you publicly (if you wish)

## Contact

Security Team: [TBD - will be updated once project has maintainers]

For non-security-related issues, please use GitHub Issues.

---

**Thank you for helping keep Acode and its users safe!**
