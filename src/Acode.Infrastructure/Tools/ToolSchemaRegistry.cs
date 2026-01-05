namespace Acode.Infrastructure.Tools;

using System.Collections.Concurrent;
using System.Text.Json;
using Acode.Application.Tools;
using Acode.Domain.Models.Inference;
using Acode.Domain.Tools;
using Microsoft.Extensions.Logging;
using NJsonSchema;
using NJsonSchema.Validation;

/// <summary>
/// Thread-safe registry for tool definitions with JSON Schema validation.
/// </summary>
/// <remarks>
/// FR-007: Tool Schema Registry requirements.
/// AC-010 to AC-030: Registration behavior.
/// AC-037 to AC-055: Validation behavior.
/// </remarks>
public sealed class ToolSchemaRegistry : IToolSchemaRegistry
{
    private readonly ConcurrentDictionary<string, ToolDefinition> tools = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, JsonSchema> compiledSchemas = new(StringComparer.Ordinal);
    private readonly ILogger<ToolSchemaRegistry> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolSchemaRegistry"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ToolSchemaRegistry(ILogger<ToolSchemaRegistry> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public int Count => this.tools.Count;

    /// <inheritdoc />
    public void RegisterTool(ToolDefinition tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (this.tools.TryGetValue(tool.Name, out var existing))
        {
            // Idempotent - same definition is OK
            if (this.AreDefinitionsEqual(existing, tool))
            {
                this.logger.LogDebug("Tool '{ToolName}' already registered with identical definition", tool.Name);
                return;
            }

            // Conflicting definition - error
            throw new InvalidOperationException(
                $"Tool '{tool.Name}' is already registered with a different definition. " +
                $"Cannot register conflicting definitions.");
        }

        // Pre-compile the schema for validation
        var schema = this.CompileSchema(tool.Name, tool.Parameters);

        if (this.tools.TryAdd(tool.Name, tool))
        {
            this.compiledSchemas.TryAdd(tool.Name, schema);
            this.logger.LogInformation("Registered tool '{ToolName}'", tool.Name);
        }
        else
        {
            // Race condition - another thread added it first
            // Check if it's the same definition (idempotent) or conflicting
            if (this.tools.TryGetValue(tool.Name, out existing) && !this.AreDefinitionsEqual(existing, tool))
            {
                throw new InvalidOperationException(
                    $"Tool '{tool.Name}' is already registered with a different definition. " +
                    $"Cannot register conflicting definitions.");
            }
        }
    }

    /// <inheritdoc />
    public ToolDefinition GetToolDefinition(string toolName)
    {
        if (this.tools.TryGetValue(toolName, out var tool))
        {
            return tool;
        }

        throw new KeyNotFoundException($"Tool '{toolName}' is not registered in the schema registry.");
    }

    /// <inheritdoc />
    public bool TryGetToolDefinition(string toolName, out ToolDefinition? tool)
    {
        return this.tools.TryGetValue(toolName, out tool);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ToolDefinition> GetAllTools()
    {
        return this.tools.Values.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public bool IsRegistered(string toolName)
    {
        return this.tools.ContainsKey(toolName);
    }

    /// <inheritdoc />
    public JsonElement ValidateArguments(string toolName, JsonElement arguments)
    {
        if (!this.TryValidateArguments(toolName, arguments, out var errors, out var validated))
        {
            throw new SchemaValidationException(toolName, errors);
        }

        return validated;
    }

    /// <inheritdoc />
    public bool TryValidateArguments(
        string toolName,
        JsonElement arguments,
        out IReadOnlyCollection<SchemaValidationError> errors,
        out JsonElement validated)
    {
        if (!this.compiledSchemas.TryGetValue(toolName, out var schema))
        {
            throw new KeyNotFoundException($"Tool '{toolName}' is not registered in the schema registry.");
        }

        var argumentsJson = arguments.GetRawText();
        var validationErrors = schema.Validate(argumentsJson);

        if (validationErrors.Count == 0)
        {
            errors = Array.Empty<SchemaValidationError>();
            validated = arguments;
            return true;
        }

        // Convert NJsonSchema errors to our domain errors
        var domainErrors = new List<SchemaValidationError>();
        foreach (var error in validationErrors)
        {
            var path = error.Path ?? error.Property ?? string.Empty;
            var code = MapErrorKindToCode(error.Kind);
            var message = FormatErrorMessage(error);
            var severity = ErrorSeverity.Error;

            domainErrors.Add(new SchemaValidationError(path, code, message, severity));
        }

        errors = domainErrors.AsReadOnly();
        validated = default;
        return false;
    }

    private static string MapErrorKindToCode(ValidationErrorKind kind)
    {
        return kind switch
        {
            ValidationErrorKind.StringExpected => "VAL-002",
            ValidationErrorKind.IntegerExpected => "VAL-002",
            ValidationErrorKind.NumberExpected => "VAL-002",
            ValidationErrorKind.BooleanExpected => "VAL-002",
            ValidationErrorKind.ArrayExpected => "VAL-002",
            ValidationErrorKind.ObjectExpected => "VAL-002",
            ValidationErrorKind.PropertyRequired => "VAL-001",
            ValidationErrorKind.StringTooShort => "VAL-003",
            ValidationErrorKind.StringTooLong => "VAL-003",
            ValidationErrorKind.PatternMismatch => "VAL-004",
            ValidationErrorKind.NumberTooSmall => "VAL-003",
            ValidationErrorKind.NumberTooBig => "VAL-003",
            ValidationErrorKind.TooManyItems => "VAL-003",
            ValidationErrorKind.TooFewItems => "VAL-003",
            ValidationErrorKind.TooManyProperties => "VAL-003",
            ValidationErrorKind.TooFewProperties => "VAL-003",
            ValidationErrorKind.NotInEnumeration => "VAL-006",
            ValidationErrorKind.AdditionalPropertiesNotValid => "VAL-007",
            ValidationErrorKind.AdditionalItemNotValid => "VAL-007",
            _ => "VAL-010"
        };
    }

    private static string FormatErrorMessage(NJsonSchema.Validation.ValidationError error)
    {
        var property = error.Property ?? error.Path ?? "root";

        return error.Kind switch
        {
            ValidationErrorKind.PropertyRequired => $"Required property '{property}' is missing.",
            ValidationErrorKind.StringExpected => $"Property '{property}' expected string but got different type.",
            ValidationErrorKind.IntegerExpected => $"Property '{property}' expected integer but got different type.",
            ValidationErrorKind.NumberExpected => $"Property '{property}' expected number but got different type.",
            ValidationErrorKind.BooleanExpected => $"Property '{property}' expected boolean but got different type.",
            ValidationErrorKind.ArrayExpected => $"Property '{property}' expected array but got different type.",
            ValidationErrorKind.ObjectExpected => $"Property '{property}' expected object but got different type.",
            ValidationErrorKind.PatternMismatch => $"Property '{property}' does not match required pattern.",
            ValidationErrorKind.NotInEnumeration => $"Property '{property}' value is not in allowed enumeration.",
            _ => $"Validation error at '{property}': {error.Kind}"
        };
    }

    private JsonSchema CompileSchema(string toolName, JsonElement schemaElement)
    {
        try
        {
            var schemaJson = schemaElement.GetRawText();
            return JsonSchema.FromJsonAsync(schemaJson).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to compile schema for tool '{ToolName}'", toolName);
            throw new InvalidOperationException($"Invalid JSON Schema for tool '{toolName}': {ex.Message}", ex);
        }
    }

    private bool AreDefinitionsEqual(ToolDefinition a, ToolDefinition b)
    {
        // Compare name, description, and schema
        if (a.Name != b.Name || a.Description != b.Description)
        {
            return false;
        }

        // Compare schema by JSON string representation
        var aSchema = a.Parameters.GetRawText();
        var bSchema = b.Parameters.GetRawText();
        return string.Equals(aSchema, bSchema, StringComparison.Ordinal);
    }
}
