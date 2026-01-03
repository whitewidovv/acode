using Acode.Application.Configuration;
using NJsonSchema;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Acode.Infrastructure.Configuration;

/// <summary>
/// Validates YAML configuration against JSON Schema.
/// </summary>
public sealed class JsonSchemaValidator
{
    private readonly JsonSchema _schema;
    private readonly ISerializer _yamlToJsonSerializer;

    public JsonSchemaValidator(string schemaPath)
    {
        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Schema file not found: {schemaPath}", schemaPath);
        }

        var schemaJson = File.ReadAllText(schemaPath);
        _schema = JsonSchema.FromJsonAsync(schemaJson).GetAwaiter().GetResult();

        // Serializer to convert YAML to JSON for validation
        _yamlToJsonSerializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .JsonCompatible()
            .Build();
    }

    /// <summary>
    /// Validates a YAML configuration file against the schema.
    /// </summary>
    /// <param name="yamlFilePath">Path to the YAML configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with any schema violations.</returns>
    public async Task<ValidationResult> ValidateAsync(string yamlFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(yamlFilePath))
        {
            return ValidationResult.Failure(new ValidationError
            {
                Code = ConfigErrorCodes.FileNotFound,
                Message = $"Configuration file not found: {yamlFilePath}",
                Severity = ValidationSeverity.Error
            });
        }

        var yaml = await File.ReadAllTextAsync(yamlFilePath, cancellationToken).ConfigureAwait(false);
        return ValidateYaml(yaml);
    }

    /// <summary>
    /// Validates YAML content against the schema.
    /// </summary>
    /// <param name="yamlContent">The YAML content to validate.</param>
    /// <returns>Validation result with any schema violations.</returns>
    public ValidationResult ValidateYaml(string yamlContent)
    {
        try
        {
            // Convert YAML to JSON-compatible object
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var yamlObject = deserializer.Deserialize(new StringReader(yamlContent));

            // Serialize to JSON
            var json = _yamlToJsonSerializer.Serialize(yamlObject);

            // Validate against schema
            var schemaErrors = _schema.Validate(json);

            if (schemaErrors.Count == 0)
            {
                return ValidationResult.Success();
            }

            // Convert schema validation errors to our format
            var errors = schemaErrors.Select(e => new ValidationError
            {
                Code = ConfigErrorCodes.SchemaViolation,
                Message = e.ToString(),
                Severity = ValidationSeverity.Error,
                Path = e.Path
            }).ToList();

            return ValidationResult.Failure(errors);
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure(new ValidationError
            {
                Code = ConfigErrorCodes.YamlParseError,
                Message = $"YAML parse error: {ex.Message}",
                Severity = ValidationSeverity.Error
            });
        }
    }
}
