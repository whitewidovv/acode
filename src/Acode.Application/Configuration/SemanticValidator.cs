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

        // FR-002b-70: Return all errors (aggregate)
        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }
}
