using System.Reflection;
using Acode.Application.Configuration;
using Newtonsoft.Json;
using NJsonSchema;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Acode.Infrastructure.Configuration;

/// <summary>
/// Validates YAML configuration against JSON Schema.
/// </summary>
public sealed class JsonSchemaValidator : ISchemaValidator
{
    private const string EmbeddedResourceName = "Acode.Infrastructure.Resources.config-schema.json";
    private readonly JsonSchema _schema;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchemaValidator"/> class.
    /// </summary>
    /// <param name="schema">The JSON Schema to validate against.</param>
    private JsonSchemaValidator(JsonSchema schema)
    {
        _schema = schema;
    }

    /// <summary>
    /// Creates a new instance from embedded resource.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new validator instance.</returns>
    public static async Task<JsonSchemaValidator> CreateFromEmbeddedResourceAsync(
        CancellationToken cancellationToken = default)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {EmbeddedResourceName}");

        using var reader = new StreamReader(stream);
        var schemaJson = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        // Write to temp file for proper $ref resolution (FromFileAsync handles this better)
        var tempFile = Path.Combine(Path.GetTempPath(), $"config-schema-{Guid.NewGuid()}.json");
        try
        {
            await File.WriteAllTextAsync(tempFile, schemaJson, cancellationToken).ConfigureAwait(false);
            var schema = await JsonSchema.FromFileAsync(tempFile, cancellationToken).ConfigureAwait(false);
            return new JsonSchemaValidator(schema);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    /// <summary>
    /// Creates a new instance of the <see cref="JsonSchemaValidator"/> asynchronously from a file path.
    /// </summary>
    /// <param name="schemaPath">Path to the JSON Schema file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new validator instance.</returns>
    public static async Task<JsonSchemaValidator> CreateAsync(
        string schemaPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Schema file not found: {schemaPath}", schemaPath);
        }

        // Load schema from file - this handles $ref resolution better than FromJsonAsync
        var schema = await JsonSchema.FromFileAsync(schemaPath, cancellationToken).ConfigureAwait(false);

        return new JsonSchemaValidator(schema);
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
            // Convert YAML to JSON-compatible object with type inference
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithAttemptingUnquotedStringTypeDeserialization()
                .Build();

            object? yamlObject;
            using (var reader = new StringReader(yamlContent))
            {
                yamlObject = deserializer.Deserialize(reader);
            }

            // Serialize to JSON using Newtonsoft.Json (preserves types better)
            var json = JsonConvert.SerializeObject(yamlObject, Formatting.None);

            // Parse JSON to ensure it's well-formed before validation
            var jsonObject = Newtonsoft.Json.Linq.JToken.Parse(json);

            // Validate against schema using the schema's built-in validator
            var schemaErrors = _schema.Validate(jsonObject);

            if (schemaErrors.Count == 0)
            {
                return ValidationResult.Success();
            }

            // Convert schema validation errors to our format
            var errors = schemaErrors.Select(e => new ValidationError
            {
                Code = ConfigErrorCodes.TypeMismatch,
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
                Code = ConfigErrorCodes.YamlSyntaxError,
                Message = $"YAML parse error: {ex.Message}",
                Severity = ValidationSeverity.Error
            });
        }
    }
}
