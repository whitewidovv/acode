using Acode.Domain.Configuration;

namespace Acode.Application.Configuration;

/// <summary>
/// Validates configuration against semantic business rules.
/// Checks constraints that go beyond JSON Schema validation.
/// </summary>
/// <remarks>
/// Per FR-002b-51 through FR-002b-70.
/// Semantic validation occurs after parsing and schema validation.
/// </remarks>
public sealed class SemanticValidator
{
    /// <summary>
    /// Validates configuration against semantic business rules.
    /// </summary>
    /// <param name="config">Configuration to validate.</param>
    /// <returns>Validation result with any errors found.</returns>
    public ValidationResult Validate(AcodeConfig? config)
    {
        if (config == null)
        {
            return ValidationResult.Failure(new ValidationError
            {
                Code = "NULL_CONFIG",
                Message = "Configuration cannot be null",
                Severity = ValidationSeverity.Error
            });
        }

        var errors = new List<ValidationError>();

        // FR-002b-51: mode.default cannot be "burst"
        if (config.Mode?.Default?.Equals("burst", StringComparison.OrdinalIgnoreCase) == true)
        {
            errors.Add(new ValidationError
            {
                Code = "INVALID_DEFAULT_MODE",
                Message = "Default operating mode cannot be 'burst'. Use 'local-only' or 'airgapped'.",
                Severity = ValidationSeverity.Error,
                Path = "mode.default"
            });
        }

        // FR-002b-53: endpoint must be localhost in LocalOnly mode
        if (config.Mode?.Default?.Equals("local-only", StringComparison.OrdinalIgnoreCase) == true &&
            config.Model?.Endpoint != null)
        {
            var endpoint = config.Model.Endpoint.ToLowerInvariant();
            if (!endpoint.Contains("localhost", StringComparison.Ordinal) && !endpoint.Contains("127.0.0.1", StringComparison.Ordinal))
            {
                errors.Add(new ValidationError
                {
                    Code = "ENDPOINT_NOT_LOCALHOST",
                    Message = "Model endpoint must be localhost in 'local-only' mode",
                    Severity = ValidationSeverity.Error,
                    Path = "model.endpoint"
                });
            }
        }

        // FR-002b-54: provider must be "ollama" or "lmstudio" in LocalOnly mode
        if (config.Mode?.Default?.Equals("local-only", StringComparison.OrdinalIgnoreCase) == true &&
            config.Model?.Provider != null)
        {
            var provider = config.Model.Provider.ToLowerInvariant();
            if (provider != "ollama" && provider != "lmstudio")
            {
                errors.Add(new ValidationError
                {
                    Code = "INVALID_PROVIDER_FOR_MODE",
                    Message = "Model provider must be 'ollama' or 'lmstudio' in 'local-only' mode",
                    Severity = ValidationSeverity.Error,
                    Path = "model.provider"
                });
            }
        }

        // FR-002b-56: paths cannot include ".." traversal
        if (config.Paths?.Source != null)
        {
            foreach (var path in config.Paths.Source)
            {
                if (path.Contains("..", StringComparison.Ordinal))
                {
                    errors.Add(new ValidationError
                    {
                        Code = "PATH_TRAVERSAL",
                        Message = $"Path traversal ('..') is not allowed: {path}",
                        Severity = ValidationSeverity.Error,
                        Path = "paths.source"
                    });
                }
            }
        }

        // FR-002b-64: temperature must be in range 0.0-2.0
        if (config.Model?.Parameters != null)
        {
            var temp = config.Model.Parameters.Temperature;
            if (temp < 0.0 || temp > 2.0)
            {
                errors.Add(new ValidationError
                {
                    Code = "INVALID_TEMPERATURE",
                    Message = $"Temperature must be between 0.0 and 2.0 (got {temp})",
                    Severity = ValidationSeverity.Error,
                    Path = "model.parameters.temperature"
                });
            }
        }

        // FR-002b-65: max_tokens must be positive
        if (config.Model?.Parameters != null)
        {
            var maxTokens = config.Model.Parameters.MaxTokens;
            if (maxTokens <= 0)
            {
                errors.Add(new ValidationError
                {
                    Code = "INVALID_MAX_TOKENS",
                    Message = $"Max tokens must be positive (got {maxTokens})",
                    Severity = ValidationSeverity.Error,
                    Path = "model.parameters.max_tokens"
                });
            }
        }

        // FR-002b-66: timeout_seconds must be positive
        if (config.Model?.TimeoutSeconds is not null && config.Model.TimeoutSeconds <= 0)
        {
            errors.Add(new ValidationError
            {
                Code = "INVALID_TIMEOUT",
                Message = $"Model timeout_seconds must be positive (got {config.Model.TimeoutSeconds})",
                Severity = ValidationSeverity.Error,
                Path = "model.timeout_seconds"
            });
        }

        // FR-002b-67: retry_count must be non-negative
        if (config.Model?.RetryCount is not null && config.Model.RetryCount < 0)
        {
            errors.Add(new ValidationError
            {
                Code = "INVALID_RETRY_COUNT",
                Message = $"Model retry_count must be non-negative (got {config.Model.RetryCount})",
                Severity = ValidationSeverity.Error,
                Path = "model.retry_count"
            });
        }

        // FR-002b-59: project.type should match project.languages
        if (config.Project?.Type is not null && config.Project.Languages?.Count > 0)
        {
            var knownMappings = new Dictionary<string, string[]>
            {
                ["dotnet"] = new[] { "csharp", "fsharp", "vb" },
                ["node"] = new[] { "javascript", "typescript" },
                ["python"] = new[] { "python" },
                ["go"] = new[] { "go" },
                ["rust"] = new[] { "rust" },
                ["java"] = new[] { "java", "kotlin" }
            };

            if (knownMappings.TryGetValue(config.Project.Type.ToLowerInvariant(), out var expectedLanguages))
            {
                var hasMatch = config.Project.Languages.Any(lang =>
                    expectedLanguages.Contains(lang.ToLowerInvariant()));

                if (!hasMatch)
                {
                    errors.Add(new ValidationError
                    {
                        Code = "PROJECT_TYPE_LANGUAGE_MISMATCH",
                        Message = $"Project type '{config.Project.Type}' does not match languages. Expected: {string.Join(", ", expectedLanguages)}",
                        Severity = ValidationSeverity.Warning,
                        Path = "project"
                    });
                }
            }
        }

        // FR-002b-60: schema_version must be supported
        if (!string.IsNullOrWhiteSpace(config.SchemaVersion))
        {
            var supportedVersions = new[] { "1.0.0" };
            if (!supportedVersions.Contains(config.SchemaVersion))
            {
                errors.Add(new ValidationError
                {
                    Code = "UNSUPPORTED_SCHEMA_VERSION",
                    Message = $"Schema version '{config.SchemaVersion}' is not supported. Supported: {string.Join(", ", supportedVersions)}",
                    Severity = ValidationSeverity.Error,
                    Path = "schema_version"
                });
            }
        }

        // FR-002b-61: no duplicate entries in arrays
        if (config.Project?.Languages?.Count > 0)
        {
            var duplicates = config.Project.Languages
                .GroupBy(x => x.ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Count > 0)
            {
                errors.Add(new ValidationError
                {
                    Code = "DUPLICATE_LANGUAGES",
                    Message = $"Duplicate languages: {string.Join(", ", duplicates)}",
                    Severity = ValidationSeverity.Warning,
                    Path = "project.languages"
                });
            }
        }

        // FR-002b-68: endpoint URL format
        if (!string.IsNullOrWhiteSpace(config.Model?.Endpoint))
        {
            if (!Uri.TryCreate(config.Model.Endpoint, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                errors.Add(new ValidationError
                {
                    Code = "INVALID_ENDPOINT_URL",
                    Message = $"Endpoint must be valid HTTP/HTTPS URL: {config.Model.Endpoint}",
                    Severity = ValidationSeverity.Error,
                    Path = "model.endpoint"
                });
            }
        }

        // FR-002b-52: airgapped_lock prevents mode override
        if (config.Mode?.AirgappedLock == true)
        {
            if (!string.Equals(config.Mode.Default, "airgapped", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new ValidationError
                {
                    Code = "AIRGAPPED_LOCK_VIOLATION",
                    Message = "When airgapped_lock is true, mode.default must be 'airgapped'",
                    Severity = ValidationSeverity.Error,
                    Path = "mode"
                });
            }
        }

        // FR-002b-55: paths cannot escape repository root (absolute paths)
        if (config.Paths?.Source != null)
        {
            foreach (var path in config.Paths.Source)
            {
                if (IsAbsolutePath(path))
                {
                    errors.Add(new ValidationError
                    {
                        Code = "PATH_ESCAPE_ATTEMPT",
                        Message = $"Absolute paths are not allowed: {path}. Paths must be relative to repository root.",
                        Severity = ValidationSeverity.Error,
                        Path = "paths.source"
                    });
                }
            }
        }

        if (config.Paths?.Tests != null)
        {
            foreach (var path in config.Paths.Tests)
            {
                if (IsAbsolutePath(path))
                {
                    errors.Add(new ValidationError
                    {
                        Code = "PATH_ESCAPE_ATTEMPT",
                        Message = $"Absolute paths are not allowed: {path}. Paths must be relative to repository root.",
                        Severity = ValidationSeverity.Error,
                        Path = "paths.tests"
                    });
                }
            }
        }

        if (config.Paths?.Output != null)
        {
            foreach (var path in config.Paths.Output)
            {
                if (IsAbsolutePath(path))
                {
                    errors.Add(new ValidationError
                    {
                        Code = "PATH_ESCAPE_ATTEMPT",
                        Message = $"Absolute paths are not allowed: {path}. Paths must be relative to repository root.",
                        Severity = ValidationSeverity.Error,
                        Path = "paths.output"
                    });
                }
            }
        }

        if (config.Paths?.Docs != null)
        {
            foreach (var path in config.Paths.Docs)
            {
                if (IsAbsolutePath(path))
                {
                    errors.Add(new ValidationError
                    {
                        Code = "PATH_ESCAPE_ATTEMPT",
                        Message = $"Absolute paths are not allowed: {path}. Paths must be relative to repository root.",
                        Severity = ValidationSeverity.Error,
                        Path = "paths.docs"
                    });
                }
            }
        }

        // FR-002b-57: command strings checked for shell injection
        ValidateCommandForShellInjection(config.Commands?.Setup, "commands.setup", errors);
        ValidateCommandForShellInjection(config.Commands?.Build, "commands.build", errors);
        ValidateCommandForShellInjection(config.Commands?.Test, "commands.test", errors);
        ValidateCommandForShellInjection(config.Commands?.Lint, "commands.lint", errors);
        ValidateCommandForShellInjection(config.Commands?.Format, "commands.format", errors);
        ValidateCommandForShellInjection(config.Commands?.Start, "commands.start", errors);

        // FR-002b-58: network.allowlist only valid in Burst mode
        if (config.Network?.Allowlist?.Count > 0)
        {
            if (!string.Equals(config.Mode?.Default, "burst", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new ValidationError
                {
                    Code = "NETWORK_ALLOWLIST_INVALID_MODE",
                    Message = "network.allowlist is only valid in 'burst' mode",
                    Severity = ValidationSeverity.Error,
                    Path = "network.allowlist"
                });
            }
        }

        // FR-002b-62: ignore patterns are valid globs
        if (config.Ignore?.Patterns != null)
        {
            foreach (var pattern in config.Ignore.Patterns)
            {
                if (!IsValidGlobPattern(pattern))
                {
                    errors.Add(new ValidationError
                    {
                        Code = ConfigErrorCodes.InvalidGlob,
                        Message = $"Invalid glob pattern: {pattern}",
                        Severity = ValidationSeverity.Error,
                        Path = "ignore.patterns"
                    });
                }
            }
        }

        if (config.Ignore?.Additional != null)
        {
            foreach (var pattern in config.Ignore.Additional)
            {
                if (!IsValidGlobPattern(pattern))
                {
                    errors.Add(new ValidationError
                    {
                        Code = ConfigErrorCodes.InvalidGlob,
                        Message = $"Invalid glob pattern: {pattern}",
                        Severity = ValidationSeverity.Error,
                        Path = "ignore.additional"
                    });
                }
            }
        }

        // FR-002b-63: path patterns are valid globs
        if (config.Paths?.Source != null)
        {
            foreach (var pattern in config.Paths.Source)
            {
                if (pattern.Contains('*', StringComparison.Ordinal) && !IsValidGlobPattern(pattern))
                {
                    errors.Add(new ValidationError
                    {
                        Code = ConfigErrorCodes.InvalidGlob,
                        Message = $"Invalid glob pattern: {pattern}",
                        Severity = ValidationSeverity.Error,
                        Path = "paths.source"
                    });
                }
            }
        }

        if (config.Paths?.Tests != null)
        {
            foreach (var pattern in config.Paths.Tests)
            {
                if (pattern.Contains('*', StringComparison.Ordinal) && !IsValidGlobPattern(pattern))
                {
                    errors.Add(new ValidationError
                    {
                        Code = ConfigErrorCodes.InvalidGlob,
                        Message = $"Invalid glob pattern: {pattern}",
                        Severity = ValidationSeverity.Error,
                        Path = "paths.tests"
                    });
                }
            }
        }

        // FR-002b-69: referenced paths exist (warning if not)
        // Note: Filesystem checks are deferred to integration tests for unit testability
        // This would require injecting IFileSystem for proper unit testing

        // FR-002b-70: Return all errors (aggregate)
        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }

    /// <summary>
    /// Checks if a path is absolute (starts with / or contains Windows drive letter).
    /// </summary>
    private static bool IsAbsolutePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        // Unix absolute path
        if (path.StartsWith('/'))
        {
            return true;
        }

        // Windows absolute path (C:\ or similar)
        if (path.Length >= 2 && path[1] == ':' && char.IsLetter(path[0]))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Validates a command string for shell injection patterns.
    /// Per FR-002b-57: Check for dangerous patterns like semicolon, ampersand, pipe operators, command substitution, and backticks.
    /// </summary>
    private static void ValidateCommandForShellInjection(object? command, string path, List<ValidationError> errors)
    {
        if (command == null)
        {
            return;
        }

        // Commands can be string, List<string>, or Dictionary<string, object>
        // For now, only validate string commands
        if (command is not string commandString)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(commandString))
        {
            return;
        }

        var dangerousPatterns = new[]
        {
            (";", "semicolon command separator"),
            ("&&", "AND command chaining"),
            ("||", "OR command chaining"),
            ("|", "pipe operator"),
            ("$(", "command substitution"),
            ("`", "backtick command substitution")
        };

        foreach (var (pattern, description) in dangerousPatterns)
        {
            if (commandString.Contains(pattern, StringComparison.Ordinal))
            {
                errors.Add(new ValidationError
                {
                    Code = "SHELL_INJECTION_DETECTED",
                    Message = $"Dangerous shell pattern detected in command: {description} ('{pattern}'). Commands should not contain shell operators.",
                    Severity = ValidationSeverity.Error,
                    Path = path
                });
                break; // Only report first dangerous pattern found per command
            }
        }
    }

    /// <summary>
    /// Validates if a glob pattern is well-formed.
    /// Per FR-002b-62 and FR-002b-63: Basic validation for common glob errors.
    /// </summary>
    private static bool IsValidGlobPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        // Check for unclosed brackets
        var openBrackets = 0;
        foreach (var ch in pattern)
        {
            if (ch == '[')
            {
                openBrackets++;
            }
            else if (ch == ']')
            {
                openBrackets--;
            }

            if (openBrackets < 0)
            {
                return false; // Closing bracket without opening
            }
        }

        if (openBrackets != 0)
        {
            return false; // Unclosed brackets
        }

        // Check for unclosed braces
        var openBraces = 0;
        foreach (var ch in pattern)
        {
            if (ch == '{')
            {
                openBraces++;
            }
            else if (ch == '}')
            {
                openBraces--;
            }

            if (openBraces < 0)
            {
                return false;
            }
        }

        if (openBraces != 0)
        {
            return false;
        }

        return true;
    }
}
